using System;
using JewelBlitzArena.Data;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Net
{
    /// <summary>
    /// GGPO-style session shell: input delay, rollback window, repeat-last prediction,
    /// snapshot restore, periodic hash probes, and dual-peer agree-frame CPU handoff.
    /// Transport is injected (loopback / LAN).
    /// </summary>
    public sealed class RollbackSession
    {
        private readonly SessionConfig _config;
        private readonly INetTransport _transport;
        private readonly Simulation _sim = new Simulation();
        private readonly InputRing[] _rings;
        private readonly SnapshotRing _snapshots;
        private readonly InputFramePacket[] _pollBuffer = new InputFramePacket[64];
        private readonly InputFrame[] _stepInputs = new InputFrame[SessionConfig.MaxPlayers];

        private int _simFrame; // frames completed (== Simulation.Frame after sync)
        private int _rollbackCount;
        private int _predictionMisses;
        private bool _desyncFlagged;
        private ulong _lastHash;
        private int _lastHashFrame = -1;
        private bool _isStalling;
        private int _stallFrames;
        private int _stalledRemotePlayer = -1;
        private int _cpuReplacedMask;

        // Agree-frame CPU handoff (modes §7): remaining peers vote on (target, agreeFrame).
        private int _handoffTarget = -1;
        private int _handoffAgreeFrame = -1;
        private int _handoffVotes;
        private bool _localHandoffProposed;

        public RollbackSession(SessionConfig config, INetTransport transport)
        {
            _config = config;
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _rings = new InputRing[config.PlayerCount];
            int ringCap = config.RollbackWindow + config.InputDelay + 4;
            for (int i = 0; i < config.PlayerCount; i++)
            {
                _rings[i] = new InputRing(ringCap);
            }

            _snapshots = new SnapshotRing(config.RollbackWindow + 1);
        }

        public SessionConfig Config => _config;

        public Simulation Sim => _sim;

        public int SimFrame => _simFrame;

        public int RollbackCount => _rollbackCount;

        public int PredictionMisses => _predictionMisses;

        public bool DesyncFlagged => _desyncFlagged;

        public ulong LastHash => _lastHash;

        public int LastHashFrame => _lastHashFrame;

        /// <summary>True when a remote peer is more than the rollback window behind (architecture §3 stalls).</summary>
        public bool IsStalling => _isStalling;

        /// <summary>Consecutive Advance calls that stalled without stepping sim.</summary>
        public int StallFrames => _stallFrames;

        /// <summary>Remote player index causing the stall, or -1.</summary>
        public int StalledRemotePlayer => _stalledRemotePlayer;

        /// <summary>True once stall has lasted ~3 s — lobby should show disconnect timer.</summary>
        public bool ShowDisconnectWarning => _stallFrames >= SessionConfig.StallDisconnectWarnFrames;

        /// <summary>True once stall has lasted ~10 s — local should propose a CPU handoff.</summary>
        public bool DisconnectReplaceReady => _stallFrames >= SessionConfig.StallDisconnectReplaceFrames;

        /// <summary>Bitmask of player indices handed to a CPU after disconnect.</summary>
        public int CpuReplacedMask => _cpuReplacedMask;

        public bool IsCpuReplaced(int playerIndex) =>
            playerIndex >= 0
            && playerIndex < SessionConfig.MaxPlayers
            && (_cpuReplacedMask & (1 << playerIndex)) != 0;

        /// <summary>Target slot of the pending agree-frame handoff, or -1.</summary>
        public int PendingHandoffTarget => _handoffTarget;

        /// <summary>Sim frame at which the pending handoff applies, or -1.</summary>
        public int PendingHandoffAgreeFrame => _handoffAgreeFrame;

        /// <summary>Bitmask of peers who voted for the pending handoff.</summary>
        public int HandoffVoteMask => _handoffVotes;

        public void Reset(in MatchConfig matchConfig, ContentCatalog? catalog = null)
        {
            _sim.Reset(matchConfig, catalog);
            _simFrame = 0;
            _rollbackCount = 0;
            _predictionMisses = 0;
            _desyncFlagged = false;
            _lastHash = _sim.Hash();
            _lastHashFrame = 0;
            _isStalling = false;
            _stallFrames = 0;
            _stalledRemotePlayer = -1;
            _cpuReplacedMask = 0;
            ClearHandoffProposal();
            _snapshots.Reset();
            _snapshots.Store(0, _sim.Snapshot());
            for (int i = 0; i < _rings.Length; i++)
            {
                _rings[i].Reset();
            }
        }

        /// <summary>
        /// Advance one sim frame. <paramref name="localInput"/> is scheduled at
        /// <c>simFrame + inputDelay</c> (architecture default delay = 2).
        /// Returns false when stalled (sim not stepped).
        /// </summary>
        public bool Advance(InputFrame localInput)
        {
            PollRemote();
            TryCommitAgreedHandoff();

            if (DisconnectReplaceReady
                && _stalledRemotePlayer >= 0
                && !IsCpuReplaced(_stalledRemotePlayer)
                && !_localHandoffProposed)
            {
                ProposeCpuHandoff(_stalledRemotePlayer, _simFrame);
            }

            TryCommitAgreedHandoff();

            int assignFrame = _simFrame + _config.InputDelay;
            _rings[_config.LocalPlayerIndex].SetLocalScheduled(assignFrame, localInput);
            _transport.Send(InputFramePacket.From(assignFrame, (byte)_config.LocalPlayerIndex, localInput));

            if (TryBeginStall())
            {
                return false;
            }

            ClearStall();
            StepConfirmedOrPredicted(_simFrame);
            _simFrame = _sim.Frame;
            _snapshots.Store(_simFrame, _sim.Snapshot());

            TrimRings();
            MaybeRecordHash();
            TryCommitAgreedHandoff();
            return true;
        }

        /// <summary>
        /// Propose handing <paramref name="targetPlayer"/> to a CPU at <paramref name="agreeFrame"/>
        /// (modes §7). Remaining peers must vote; when votes meet the quorum and
        /// <see cref="SimFrame"/> ≥ agree frame, the handoff applies.
        /// </summary>
        public bool ProposeCpuHandoff(int targetPlayer, int agreeFrame)
        {
            if (targetPlayer < 0 || targetPlayer >= _config.PlayerCount)
            {
                return false;
            }

            if (IsCpuReplaced(targetPlayer))
            {
                return false;
            }

            if (agreeFrame < _simFrame)
            {
                agreeFrame = _simFrame;
            }

            if (_handoffTarget >= 0 && _handoffTarget != targetPlayer)
            {
                return false; // one handoff in flight
            }

            if (_handoffTarget < 0)
            {
                _handoffTarget = targetPlayer;
                _handoffAgreeFrame = agreeFrame;
                _handoffVotes = 0;
            }
            else if (agreeFrame != _handoffAgreeFrame)
            {
                // Deterministic merge: later agree frame wins; votes reset.
                int merged = agreeFrame > _handoffAgreeFrame ? agreeFrame : _handoffAgreeFrame;
                if (merged != _handoffAgreeFrame)
                {
                    _handoffAgreeFrame = merged;
                    _handoffVotes = 0;
                }
            }

            RegisterHandoffVote(_config.LocalPlayerIndex);
            _localHandoffProposed = true;
            _transport.Send(InputFramePacket.CpuHandoffPropose(
                _handoffAgreeFrame,
                (byte)_handoffTarget,
                (byte)_config.LocalPlayerIndex));
            return TryCommitAgreedHandoff();
        }

        /// <summary>
        /// Disconnect-timer path: propose an agree-frame handoff for the stalled peer.
        /// For a solo remaining peer (2P), quorum is 1 and the handoff commits immediately.
        /// </summary>
        public bool TryReplaceStalledPeerWithCpu()
        {
            if (!DisconnectReplaceReady)
            {
                return false;
            }

            int peer = _stalledRemotePlayer;
            if (peer < 0 || peer >= _config.PlayerCount || peer == _config.LocalPlayerIndex)
            {
                return false;
            }

            if (IsCpuReplaced(peer))
            {
                return false;
            }

            return ProposeCpuHandoff(peer, _simFrame);
        }

        private bool TryBeginStall()
        {
            // Before any remote input is confirmed, allow prediction through the input-delay
            // fill window; stall only once a peer has gone silent past the rollback window.
            for (int p = 0; p < _config.PlayerCount; p++)
            {
                if (p == _config.LocalPlayerIndex || IsCpuReplaced(p))
                {
                    continue;
                }

                int latest = _rings[p].LatestConfirmedFrame;
                if (latest < 0)
                {
                    // No remote packets yet: stall only after we've predicted past the window.
                    if (_simFrame > _config.RollbackWindow)
                    {
                        _isStalling = true;
                        _stallFrames++;
                        _stalledRemotePlayer = p;
                        return true;
                    }

                    continue;
                }

                int behind = _simFrame - latest;
                if (behind > _config.RollbackWindow)
                {
                    _isStalling = true;
                    _stallFrames++;
                    _stalledRemotePlayer = p;
                    return true;
                }
            }

            return false;
        }

        private void ClearStall()
        {
            _isStalling = false;
            _stallFrames = 0;
            _stalledRemotePlayer = -1;
        }

        /// <summary>Inject a remote packet (tests / alternate transports).</summary>
        public void InjectRemote(InputFramePacket packet) => ApplyRemotePacket(packet);

        public bool TryCompareHash(int frame, ulong peerHash)
        {
            if (frame != _lastHashFrame)
            {
                return true; // not comparable yet
            }

            if (peerHash != _lastHash)
            {
                _desyncFlagged = true;
                return false;
            }

            return true;
        }

        private void PollRemote()
        {
            int n = _transport.Poll(_pollBuffer);
            for (int i = 0; i < n; i++)
            {
                ApplyRemotePacket(_pollBuffer[i]);
            }
        }

        private void ApplyRemotePacket(InputFramePacket packet)
        {
            if (packet.IsCpuHandoffPropose)
            {
                ApplyHandoffProposal(packet);
                return;
            }

            int player = packet.PlayerIndex;
            if (player < 0 || player >= _config.PlayerCount || player == _config.LocalPlayerIndex)
            {
                return;
            }

            InputFrame input = packet.ToInputFrame();
            bool mismatch = _rings[player].Confirm(packet.Frame, input);
            if (!mismatch)
            {
                return;
            }

            _predictionMisses++;
            if (packet.Frame < _simFrame)
            {
                RollbackAndResimulate(packet.Frame);
            }
        }

        private void ApplyHandoffProposal(InputFramePacket packet)
        {
            int target = packet.PlayerIndex;
            int agreeFrame = packet.Frame;
            int proposer = packet.Bits & 0xFF;
            if (target < 0 || target >= _config.PlayerCount)
            {
                return;
            }

            if (proposer < 0 || proposer >= _config.PlayerCount)
            {
                return;
            }

            if (IsCpuReplaced(target))
            {
                return;
            }

            if (_handoffTarget >= 0 && _handoffTarget != target)
            {
                return;
            }

            if (_handoffTarget < 0)
            {
                _handoffTarget = target;
                _handoffAgreeFrame = agreeFrame < _simFrame ? _simFrame : agreeFrame;
                _handoffVotes = 0;
            }
            else if (agreeFrame > _handoffAgreeFrame)
            {
                _handoffAgreeFrame = agreeFrame;
                _handoffVotes = 0;
            }

            RegisterHandoffVote(proposer);

            // Remaining peer auto-acks when also seeing the stalled target (dual-peer agree).
            if (!_localHandoffProposed
                && _config.LocalPlayerIndex != target
                && (DisconnectReplaceReady || _stalledRemotePlayer == target || _isStalling))
            {
                ProposeCpuHandoff(target, _handoffAgreeFrame);
            }
            else
            {
                TryCommitAgreedHandoff();
            }
        }

        private void RegisterHandoffVote(int voter)
        {
            if (voter < 0 || voter >= SessionConfig.MaxPlayers)
            {
                return;
            }

            _handoffVotes |= 1 << voter;
        }

        private int HandoffVotesNeeded(int target)
        {
            int n = 0;
            for (int i = 0; i < _config.PlayerCount; i++)
            {
                if (i == target || IsCpuReplaced(i))
                {
                    continue;
                }

                n++;
            }

            return n < 1 ? 1 : n;
        }

        private bool TryCommitAgreedHandoff()
        {
            if (_handoffTarget < 0 || _handoffAgreeFrame < 0 || IsCpuReplaced(_handoffTarget))
            {
                return false;
            }

            int needed = HandoffVotesNeeded(_handoffTarget);
            if (CountEligibleHandoffVotes(_handoffTarget) < needed)
            {
                return false;
            }

            if (_simFrame < _handoffAgreeFrame)
            {
                return false;
            }

            return ApplyCpuHandoffNow(_handoffTarget);
        }

        private int CountEligibleHandoffVotes(int target)
        {
            int n = 0;
            for (int i = 0; i < _config.PlayerCount; i++)
            {
                if (i == target || IsCpuReplaced(i))
                {
                    continue;
                }

                if ((_handoffVotes & (1 << i)) != 0)
                {
                    n++;
                }
            }

            return n;
        }

        private bool ApplyCpuHandoffNow(int peer)
        {
            _cpuReplacedMask |= 1 << peer;
            _sim.SetCpuControlled(peer, controlled: true);
            ClearStall();
            ClearHandoffProposal();
            return true;
        }

        private void ClearHandoffProposal()
        {
            _handoffTarget = -1;
            _handoffAgreeFrame = -1;
            _handoffVotes = 0;
            _localHandoffProposed = false;
        }

        private void RollbackAndResimulate(int fromFrame)
        {
            int restoreCompleted = fromFrame;
            if (!_snapshots.TryLoad(restoreCompleted, out MatchState snap))
            {
                bool found = false;
                for (int f = fromFrame; f >= Math.Max(0, _simFrame - _config.RollbackWindow); f--)
                {
                    if (_snapshots.TryLoad(f, out snap))
                    {
                        restoreCompleted = f;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return;
                }
            }

            _sim.Restore(snap);
            ReapplyCpuReplacements();
            _rollbackCount++;

            int target = _simFrame;
            _simFrame = restoreCompleted;
            for (int f = restoreCompleted; f < target; f++)
            {
                StepConfirmedOrPredicted(f);
                _simFrame = _sim.Frame;
                _snapshots.Store(_simFrame, _sim.Snapshot());
            }
        }

        private void ReapplyCpuReplacements()
        {
            for (int p = 0; p < _config.PlayerCount; p++)
            {
                if (IsCpuReplaced(p))
                {
                    _sim.SetCpuControlled(p, controlled: true);
                }
            }
        }

        private void StepConfirmedOrPredicted(int frameToStep)
        {
            for (int p = 0; p < _config.PlayerCount; p++)
            {
                _stepInputs[p] = _rings[p].GetOrPredict(frameToStep);
            }

            for (int p = _config.PlayerCount; p < SessionConfig.MaxPlayers; p++)
            {
                _stepInputs[p] = InputFrame.Neutral;
            }

            _sim.Step(_stepInputs[0], _stepInputs[1], _stepInputs[2], _stepInputs[3]);
        }

        private void TrimRings()
        {
            int baseFrame = Math.Max(0, _simFrame - _config.RollbackWindow);
            for (int i = 0; i < _rings.Length; i++)
            {
                _rings[i].AdvanceBase(baseFrame);
            }
        }

        private void MaybeRecordHash()
        {
            if (_simFrame % SessionConfig.HashIntervalFrames != 0)
            {
                return;
            }

            _lastHash = _sim.Hash();
            _lastHashFrame = _simFrame;
        }
    }
}
