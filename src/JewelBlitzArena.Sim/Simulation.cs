using System;
using JewelBlitzArena.Data;

namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Deterministic match simulation. Presentation reads snapshots and writes inputs;
    /// only this type mutates <see cref="MatchState"/>.
    /// </summary>
    public sealed class Simulation
    {
        public const int TickRateHz = 60;
        public const int DefaultLife = 1000;
        public const int DefaultLusterMax = MatchRules.LusterMax;

        private static readonly Fix DefaultSpeedWalk = Fix.FromInt(3);
        private static readonly Fix DefaultSpeedRun = Fix.FromInt(5);
        private static readonly Fix DefaultSpeedFull = Fix.FromInt(7);

        private MatchState _state;
        private ContentCatalog? _catalog;
        private readonly KitRuntime[] _kits = new KitRuntime[PoolCaps.Actors];
        private readonly Fix[] _dtWalk = new Fix[PoolCaps.Actors];
        private readonly Fix[] _dtRun = new Fix[PoolCaps.Actors];
        private readonly Fix[] _dtFull = new Fix[PoolCaps.Actors];

        public int Frame => _state.Frame;

        public int ActorCount => _state.ActorCount;

        public bool MatchOver => _state.MatchOver;

        public ContentCatalog? Catalog => _catalog;

        public ref readonly MatchState State => ref _state;

        public void Reset(in MatchConfig config) => Reset(config, catalog: null);

        public void Reset(in MatchConfig config, ContentCatalog? catalog)
        {
            _catalog = catalog;
            _state = default;
            _state.Seed = config.Seed;
            _state.StageId = config.StageId;
            _state.ActorCount = config.ActorCount < 1
                ? 1
                : (config.ActorCount > PoolCaps.Actors ? PoolCaps.Actors : config.ActorCount);
            _state.Frame = 0;
            _state.HouseRulesBits = config.HouseRulesBits != 0
                ? config.HouseRulesBits
                : MatchRules.HouseLusterTimer;
            _state.JewelCap = config.JewelCap > 0
                ? config.JewelCap
                : MatchRules.DefaultJewelCap(_state.ActorCount);
            _state.NextPlacementFromBottom = _state.ActorCount;
            _state.MatchEndFrame = 0;

            for (int i = 0; i < PoolCaps.Actors; i++)
            {
                MatchStateAccess.SetPlacement(ref _state, i, 0);
            }

            _state.GameplayRng.Seed(config.Seed);
            for (int i = 0; i < PoolCaps.CpuBrains; i++)
            {
                MatchStateAccess.CpuRng(ref _state, i).Seed(
                    config.Seed ^ (0xC0FFEEUL + (ulong)(i + 1) * 0x9E3779B97F4A7C15UL));
            }

            for (int i = 0; i < PoolCaps.Actors; i++)
            {
                _kits[i] = KitRuntime.FromDefaults(i);
                _dtWalk[i] = DefaultSpeedWalk / TickRateHz;
                _dtRun[i] = DefaultSpeedRun / TickRateHz;
                _dtFull[i] = DefaultSpeedFull / TickRateHz;
            }

            unsafe
            {
                for (int i = 0; i < _state.ActorCount; i++)
                {
                    ref ActorSlot actor = ref MatchStateAccess.Actor(ref _state, i);
                    actor = default;
                    actor.FighterId = config.FighterIds[i];

                    KitRuntime kit;
                    if (catalog != null && catalog.TryGetFighter(actor.FighterId, out FighterKitRecord record))
                    {
                        kit = KitRuntime.FromRecord(record);
                    }
                    else
                    {
                        kit = KitRuntime.FromDefaults(actor.FighterId);
                    }

                    _kits[i] = kit;
                    _dtWalk[i] = new Fix(kit.WalkSpeedRaw) / TickRateHz;
                    _dtRun[i] = new Fix(kit.RunSpeedRaw) / TickRateHz;
                    _dtFull[i] = new Fix(kit.FullSpeedRaw) / TickRateHz;

                    int life = config.StartingLife[i];
                    if (life <= 0)
                    {
                        life = kit.MaxLife;
                    }

                    actor.Life = life > 0 ? life : DefaultLife;
                    actor.MaxLife = actor.Life;
                    actor.Luster = 0;
                    actor.IsAlive = true;
                    actor.Team = config.Teams[i];
                    actor.ReachRating = (byte)kit.ReachRating;
                    actor.KitBehavior = (byte)kit.Behavior;
                    actor.PosX = Fix.FromInt(i * 2 - 1);
                    actor.PosY = Fix.Zero;
                    actor.PosZ = Fix.Zero;
                    actor.Facing = i == 0 ? (byte)0 : (byte)1;
                }
            }

            JewelSystem.InitializeOpening(ref _state);
        }

        public void Step(InputFrame i0, InputFrame i1, InputFrame i2, InputFrame i3)
        {
            if (_state.MatchOver)
            {
                _state.Frame++;
                return;
            }

            CombatSystem.ApplyInput(ref _state, 0, i0, _kits[0], _dtWalk[0], _dtRun[0], _dtFull[0]);
            CombatSystem.ApplyInput(ref _state, 1, i1, _kits[1], _dtWalk[1], _dtRun[1], _dtFull[1]);
            CombatSystem.ApplyInput(ref _state, 2, i2, _kits[2], _dtWalk[2], _dtRun[2], _dtFull[2]);
            CombatSystem.ApplyInput(ref _state, 3, i3, _kits[3], _dtWalk[3], _dtRun[3], _dtFull[3]);

            IntegrateActors();
            CombatSystem.TickActors(ref _state, _kits);
            JewelSystem.Tick(ref _state);

            _ = _state.GameplayRng.NextUInt64();
            _state.Frame++;
        }

        public void Step(ReadOnlySpan<InputFrame> inputs)
        {
            InputFrame i0 = inputs.Length > 0 ? inputs[0] : InputFrame.Neutral;
            InputFrame i1 = inputs.Length > 1 ? inputs[1] : InputFrame.Neutral;
            InputFrame i2 = inputs.Length > 2 ? inputs[2] : InputFrame.Neutral;
            InputFrame i3 = inputs.Length > 3 ? inputs[3] : InputFrame.Neutral;
            Step(i0, i1, i2, i3);
        }

        /// <summary>Test/helper: grant field jewels by walking pickup after forcing holds (S1 scenarios).</summary>
        public void DebugGrantJewels(int actorIndex, int count)
        {
            if (actorIndex < 0 || actorIndex >= _state.ActorCount)
            {
                return;
            }

            ref ActorSlot actor = ref MatchStateAccess.Actor(ref _state, actorIndex);
            for (int n = 0; n < count; n++)
            {
                if (actor.IsBlitzed)
                {
                    break;
                }

                actor.JewelsHeld++;
                actor.JewelsCollectedTotal++;
                if (actor.JewelsHeld >= MatchRules.BlitzJewelCount)
                {
                    JewelSystem.BeginBlitz(ref _state, ref actor);
                    break;
                }
            }
        }

        public ulong Hash()
        {
            unsafe
            {
                fixed (MatchState* p = &_state)
                {
                    return XxHash64.Hash(p, MatchState.SizeOf);
                }
            }
        }

        public MatchState Snapshot() => _state;

        public void Restore(in MatchState snapshot) => _state = snapshot;

        public MatchResult BuildResult()
        {
            var result = new MatchResult
            {
                FramesPlayed = _state.Frame,
                ActorCount = _state.ActorCount,
            };

            unsafe
            {
                for (int i = 0; i < PoolCaps.Actors; i++)
                {
                    result.Placements[i] = 0;
                    result.Kos[i] = 0;
                    result.JewelsCollected[i] = 0;
                }

                for (int i = 0; i < _state.ActorCount; i++)
                {
                    ref ActorSlot actor = ref MatchStateAccess.Actor(ref _state, i);
                    result.Kos[i] = actor.KosDealt;
                    result.JewelsCollected[i] = actor.JewelsCollectedTotal;
                    result.Placements[i] = MatchStateAccess.GetPlacement(ref _state, i);
                }

                // If match never ended, fall back to Life ranking.
                bool anyPlacement = false;
                for (int i = 0; i < _state.ActorCount; i++)
                {
                    if (result.Placements[i] != 0)
                    {
                        anyPlacement = true;
                        break;
                    }
                }

                if (!anyPlacement)
                {
                    for (int place = 1; place <= _state.ActorCount; place++)
                    {
                        int best = -1;
                        int bestLife = int.MinValue;
                        for (int i = 0; i < _state.ActorCount; i++)
                        {
                            if (result.Placements[i] != 0)
                            {
                                continue;
                            }

                            int life = MatchStateAccess.Actor(ref _state, i).Life;
                            if (life > bestLife)
                            {
                                bestLife = life;
                                best = i;
                            }
                        }

                        if (best >= 0)
                        {
                            result.Placements[best] = place;
                        }
                    }
                }
            }

            return result;
        }

        private void IntegrateActors()
        {
            for (int i = 0; i < _state.ActorCount; i++)
            {
                ref ActorSlot actor = ref MatchStateAccess.Actor(ref _state, i);
                if (!actor.IsAlive || actor.IsEliminated)
                {
                    continue;
                }

                actor.PosX += actor.VelX;
                actor.PosY += actor.VelY;
                actor.PosZ += actor.VelZ;
            }
        }
    }
}
