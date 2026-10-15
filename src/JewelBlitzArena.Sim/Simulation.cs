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
        public const int DefaultLusterMax = 1200;

        private static readonly Fix DefaultSpeedWalk = Fix.FromInt(3);
        private static readonly Fix DefaultSpeedRun = Fix.FromInt(5);
        private static readonly Fix DefaultSpeedFull = Fix.FromInt(7);

        private MatchState _state;
        private ContentCatalog? _catalog;
        private readonly Fix[] _dtWalk = new Fix[PoolCaps.Actors];
        private readonly Fix[] _dtRun = new Fix[PoolCaps.Actors];
        private readonly Fix[] _dtFull = new Fix[PoolCaps.Actors];

        public int Frame => _state.Frame;

        public int ActorCount => _state.ActorCount;

        public ContentCatalog? Catalog => _catalog;

        public ref readonly MatchState State => ref _state;

        public void Reset(in MatchConfig config) => Reset(config, catalog: null);

        /// <summary>
        /// Reset from match config. When <paramref name="catalog"/> is provided, Life and
        /// movement speeds resolve from fighter kits (S5). Modes still never let Sim read a profile.
        /// </summary>
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

            _state.GameplayRng.Seed(config.Seed);
            for (int i = 0; i < PoolCaps.CpuBrains; i++)
            {
                MatchStateAccess.CpuRng(ref _state, i).Seed(
                    config.Seed ^ (0xC0FFEEUL + (ulong)(i + 1) * 0x9E3779B97F4A7C15UL));
            }

            unsafe
            {
                for (int i = 0; i < PoolCaps.Actors; i++)
                {
                    _dtWalk[i] = DefaultSpeedWalk / TickRateHz;
                    _dtRun[i] = DefaultSpeedRun / TickRateHz;
                    _dtFull[i] = DefaultSpeedFull / TickRateHz;
                }

                for (int i = 0; i < _state.ActorCount; i++)
                {
                    ref ActorSlot actor = ref MatchStateAccess.Actor(ref _state, i);
                    actor = default;
                    actor.FighterId = config.FighterIds[i];

                    int life = config.StartingLife[i];
                    if (life <= 0 && catalog != null
                        && catalog.TryGetFighter(actor.FighterId, out FighterKitRecord kit))
                    {
                        life = kit.MaxLife;
                        _dtWalk[i] = new Fix(kit.WalkSpeedRaw) / TickRateHz;
                        _dtRun[i] = new Fix(kit.RunSpeedRaw) / TickRateHz;
                        _dtFull[i] = new Fix(kit.FullSpeedRaw) / TickRateHz;
                    }

                    actor.Life = life > 0 ? life : DefaultLife;
                    actor.Luster = 0;
                    actor.IsAlive = true;
                    actor.Team = (byte)i;
                    actor.PosX = Fix.FromInt(i * 2 - 1);
                    actor.PosY = Fix.Zero;
                    actor.PosZ = Fix.Zero;
                }
            }
        }

        public void Step(InputFrame i0, InputFrame i1, InputFrame i2, InputFrame i3)
        {
            ApplyActorInput(0, i0);
            ApplyActorInput(1, i1);
            ApplyActorInput(2, i2);
            ApplyActorInput(3, i3);

            IntegrateActors();
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

            return result;
        }

        private void ApplyActorInput(int index, InputFrame input)
        {
            if (index >= _state.ActorCount)
            {
                return;
            }

            ref ActorSlot actor = ref MatchStateAccess.Actor(ref _state, index);
            if (!actor.IsAlive)
            {
                actor.VelX = Fix.Zero;
                actor.VelY = Fix.Zero;
                actor.VelZ = Fix.Zero;
                return;
            }

            Fix stepSpeed = MagnitudeToStepSpeed(index, input.Magnitude);
            if (stepSpeed.Raw == 0)
            {
                actor.VelX = Fix.Zero;
                actor.VelZ = Fix.Zero;
                return;
            }

            FixVec2 dir = DirectionLut.GetUnit(input.Direction);
            actor.VelX = dir.X * stepSpeed;
            actor.VelZ = dir.Y * stepSpeed;
            _ = input.Jump;
        }

        private void IntegrateActors()
        {
            for (int i = 0; i < _state.ActorCount; i++)
            {
                ref ActorSlot actor = ref MatchStateAccess.Actor(ref _state, i);
                if (!actor.IsAlive)
                {
                    continue;
                }

                actor.PosX += actor.VelX;
                actor.PosY += actor.VelY;
                actor.PosZ += actor.VelZ;
            }
        }

        private Fix MagnitudeToStepSpeed(int actorIndex, int magnitude)
        {
            switch (magnitude)
            {
                case 1: return _dtWalk[actorIndex];
                case 2: return _dtRun[actorIndex];
                case 3: return _dtFull[actorIndex];
                default: return Fix.Zero;
            }
        }
    }
}
