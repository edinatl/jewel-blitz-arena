using System;

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

        /// <summary>Walk / run / full stick speeds (metres per second as Q48.16).</summary>
        private static readonly Fix SpeedWalk = Fix.FromInt(3);
        private static readonly Fix SpeedRun = Fix.FromInt(5);
        private static readonly Fix SpeedFull = Fix.FromInt(7);

        /// <summary>Per-frame scale: speed / TickRateHz.</summary>
        private static readonly Fix DtWalk = SpeedWalk / TickRateHz;
        private static readonly Fix DtRun = SpeedRun / TickRateHz;
        private static readonly Fix DtFull = SpeedFull / TickRateHz;

        private MatchState _state;

        public int Frame => _state.Frame;

        public int ActorCount => _state.ActorCount;

        public ref readonly MatchState State => ref _state;

        public void Reset(in MatchConfig config)
        {
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
                // Distinct streams so CPU brain changes cannot shift gameplay RNG.
                MatchStateAccess.CpuRng(ref _state, i).Seed(
                    config.Seed ^ (0xC0FFEEUL + (ulong)(i + 1) * 0x9E3779B97F4A7C15UL));
            }

            unsafe
            {
                for (int i = 0; i < _state.ActorCount; i++)
                {
                    ref ActorSlot actor = ref MatchStateAccess.Actor(ref _state, i);
                    actor = default;
                    actor.FighterId = config.FighterIds[i];
                    int life = config.StartingLife[i];
                    actor.Life = life > 0 ? life : DefaultLife;
                    actor.Luster = 0;
                    actor.IsAlive = true;
                    actor.Team = (byte)i;
                    // Spread actors along X so a neutral match is not fully degenerate.
                    actor.PosX = Fix.FromInt(i * 2 - 1);
                    actor.PosY = Fix.Zero;
                    actor.PosZ = Fix.Zero;
                }
            }
        }

        /// <summary>Advance one 60 Hz tick. Exactly four inputs (unused slots should be neutral).</summary>
        public void Step(InputFrame i0, InputFrame i1, InputFrame i2, InputFrame i3)
        {
            ApplyActorInput(0, i0);
            ApplyActorInput(1, i1);
            ApplyActorInput(2, i2);
            ApplyActorInput(3, i3);

            IntegrateActors();

            // Touch gameplay RNG once per frame so the stream is part of the hashed surface
            // even before crates/items consume it (S2).
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

                // S0 stub: living actors share place 1 by Life descending (stable by slot).
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

            Fix stepSpeed = MagnitudeToStepSpeed(input.Magnitude);
            if (stepSpeed.Raw == 0)
            {
                actor.VelX = Fix.Zero;
                actor.VelZ = Fix.Zero;
                return;
            }

            FixVec2 dir = DirectionLut.GetUnit(input.Direction);
            actor.VelX = dir.X * stepSpeed;
            actor.VelZ = dir.Y * stepSpeed;
            // Jump is recorded on the input surface for later slices; S0 does not leave ground.
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

        private static Fix MagnitudeToStepSpeed(int magnitude)
        {
            switch (magnitude)
            {
                case 1: return DtWalk;
                case 2: return DtRun;
                case 3: return DtFull;
                default: return Fix.Zero;
            }
        }
    }
}
