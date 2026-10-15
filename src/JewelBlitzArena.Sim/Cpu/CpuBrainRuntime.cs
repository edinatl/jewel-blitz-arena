namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Deterministic CPU brain (architecture §2.6 / combat §10.1). Thinks on a staggered
    /// schedule keyed by CPU level 1–8; uses kit <see cref="ActorSlot.ReachRating"/> for
    /// in-range Attack pressure. Holds <see cref="CpuBrainSlot.LastDecision"/> between thinks.
    /// </summary>
    public static class CpuBrainRuntime
    {
        public const uint FlagActive = 1u;
        public const int DefaultLevel = 4;
        public const int MinLevel = 1;
        public const int MaxLevel = 8;

        private const int LevelShift = 8;
        private const uint LevelMask = 0xFFu << LevelShift;

        public static void Activate(ref MatchState state, int slot, int level = DefaultLevel)
        {
            if (slot < 0 || slot >= PoolCaps.CpuBrains)
            {
                return;
            }

            int clamped = ClampLevel(level);
            CpuBrainSlot brain = MatchStateAccess.GetCpuBrain(ref state, slot);
            brain.Reserved = FlagActive | ((uint)clamped << LevelShift);
            brain.ThinkCooldown = slot & 1; // stagger
            brain.TargetSlot = -1;
            brain.LastDecision = InputFrame.Neutral.Bits;
            MatchStateAccess.SetCpuBrain(ref state, slot, in brain);
        }

        public static void Deactivate(ref MatchState state, int slot)
        {
            if (slot < 0 || slot >= PoolCaps.CpuBrains)
            {
                return;
            }

            CpuBrainSlot brain = MatchStateAccess.GetCpuBrain(ref state, slot);
            brain.Reserved = 0;
            brain.ThinkCooldown = 0;
            brain.TargetSlot = -1;
            brain.LastDecision = 0;
            MatchStateAccess.SetCpuBrain(ref state, slot, in brain);
        }

        public static bool IsActive(ref MatchState state, int slot)
        {
            if (slot < 0 || slot >= PoolCaps.CpuBrains)
            {
                return false;
            }

            return (MatchStateAccess.GetCpuBrain(ref state, slot).Reserved & FlagActive) != 0;
        }

        public static int GetLevel(ref MatchState state, int slot)
        {
            if (!IsActive(ref state, slot))
            {
                return DefaultLevel;
            }

            int level = (int)((MatchStateAccess.GetCpuBrain(ref state, slot).Reserved & LevelMask) >> LevelShift);
            return ClampLevel(level == 0 ? DefaultLevel : level);
        }

        public static int ClampLevel(int level)
        {
            if (level < MinLevel)
            {
                return MinLevel;
            }

            if (level > MaxLevel)
            {
                return MaxLevel;
            }

            return level;
        }

        /// <summary>Produce this slot's input for the coming step (overwrites peer/local feed).</summary>
        public static InputFrame Think(ref MatchState state, int slot)
        {
            if (!IsActive(ref state, slot))
            {
                return InputFrame.Neutral;
            }

            CpuBrainSlot brain = MatchStateAccess.GetCpuBrain(ref state, slot);
            if (brain.ThinkCooldown > 0)
            {
                brain.ThinkCooldown--;
                MatchStateAccess.SetCpuBrain(ref state, slot, in brain);
                var held = new InputFrame { Bits = (ushort)brain.LastDecision };
                return held;
            }

            int level = GetLevel(ref state, slot);
            InputFrame decided = Decide(ref state, slot, level, ref brain);
            brain.LastDecision = decided.Bits;
            brain.ThinkCooldown = ThinkHoldFrames(level);
            MatchStateAccess.SetCpuBrain(ref state, slot, in brain);

            // Advance CPU RNG so takeover stays hash-stable and non-degenerate.
            _ = MatchStateAccess.CpuRng(ref state, slot).NextUInt64();
            return decided;
        }

        /// <summary>Frames to hold after a think (architecture: stagger + level reaction delay).</summary>
        public static int ThinkHoldFrames(int level)
        {
            int l = ClampLevel(level);
            if (l <= 2)
            {
                return 5; // think ~every 6f
            }

            if (l <= 4)
            {
                return 1; // every 2f
            }

            if (l <= 6)
            {
                return 1;
            }

            return 0; // every frame
        }

        public static int StickMagnitudeForLevel(int level)
        {
            int l = ClampLevel(level);
            if (l <= 2)
            {
                return 1; // walk
            }

            if (l <= 5)
            {
                return 2; // run
            }

            return 3; // full
        }

        private static InputFrame Decide(ref MatchState state, int slot, int level, ref CpuBrainSlot brain)
        {
            ref ActorSlot self = ref MatchStateAccess.Actor(ref state, slot);
            if (!self.IsAlive || self.IsEliminated)
            {
                brain.TargetSlot = -1;
                return InputFrame.Neutral;
            }

            int target = FindNearestOpponent(ref state, slot);
            brain.TargetSlot = target;
            if (target < 0)
            {
                return InputFrame.Neutral;
            }

            ActorSlot foe = MatchStateAccess.Actor(ref state, target);
            Fix dx = foe.PosX - self.PosX;
            Fix dz = foe.PosZ - self.PosZ;
            if (dx.Raw == 0 && dz.Raw == 0)
            {
                return InputFrame.FromStick(0, 0);
            }

            int dir = BestDirection(dx, dz);
            int mag = StickMagnitudeForLevel(level);
            var frame = InputFrame.FromStick(dir, mag);

            // Kit-aware in-range pressure: ReachRating metres (Q48.16) as attack radius.
            long reachRaw = Fix.FromInt(self.ReachRating > 0 ? self.ReachRating : 3).Raw;
            long distSq = ((dx.Raw >> 8) * (dx.Raw >> 8)) + ((dz.Raw >> 8) * (dz.Raw >> 8));
            long reachSq = (reachRaw >> 8) * (reachRaw >> 8);
            if (distSq <= reachSq && level >= 3)
            {
                ref Xoshiro256StarStar rng = ref MatchStateAccess.CpuRng(ref state, slot);
                uint roll = (uint)(rng.NextUInt64() & 0xFF);
                // Higher levels attack more often when in kit reach.
                int threshold = 40 + (level * 20); // L3≈100/255 … L8≈200/255
                if (threshold > 230)
                {
                    threshold = 230;
                }

                if (roll < (uint)threshold)
                {
                    frame.Attack = true;
                }

                if (level >= 6 && (roll & 0x7) == 0)
                {
                    frame.Strong = true;
                }
            }

            return frame;
        }

        private static int FindNearestOpponent(ref MatchState state, int slot)
        {
            ActorSlot self = MatchStateAccess.Actor(ref state, slot);
            int best = -1;
            long bestDistSq = long.MaxValue;
            for (int i = 0; i < state.ActorCount; i++)
            {
                if (i == slot)
                {
                    continue;
                }

                ActorSlot other = MatchStateAccess.Actor(ref state, i);
                if (!other.IsAlive || other.IsEliminated)
                {
                    continue;
                }

                if (self.Team != 0 && other.Team == self.Team)
                {
                    continue;
                }

                long dx = other.PosX.Raw - self.PosX.Raw;
                long dz = other.PosZ.Raw - self.PosZ.Raw;
                long distSq = (dx * dx) + (dz * dz);
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    best = i;
                }
            }

            return best;
        }

        private static int BestDirection(Fix dx, Fix dz)
        {
            int best = 0;
            long bestDot = long.MinValue;
            for (int i = 0; i < DirectionLut.Count; i++)
            {
                FixVec2 u = DirectionLut.GetUnit(i);
                long dot = ((dx.Raw * u.X.Raw) >> Fix.FractionBits) + ((dz.Raw * u.Y.Raw) >> Fix.FractionBits);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    best = i;
                }
            }

            return best;
        }
    }
}
