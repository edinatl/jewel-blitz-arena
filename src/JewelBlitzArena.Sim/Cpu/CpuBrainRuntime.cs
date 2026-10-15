namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Minimal deterministic CPU brain (architecture §2.6). Thinks every 2nd frame on a
    /// staggered schedule; holds <see cref="CpuBrainSlot.LastDecision"/> between thinks.
    /// </summary>
    public static class CpuBrainRuntime
    {
        public const uint FlagActive = 1u;

        public static void Activate(ref MatchState state, int slot)
        {
            if (slot < 0 || slot >= PoolCaps.CpuBrains)
            {
                return;
            }

            CpuBrainSlot brain = MatchStateAccess.GetCpuBrain(ref state, slot);
            brain.Reserved |= FlagActive;
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
            brain.Reserved &= ~FlagActive;
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

            InputFrame decided = Decide(ref state, slot, ref brain);
            brain.LastDecision = decided.Bits;
            brain.ThinkCooldown = 1; // hold one frame (think every 2nd)
            MatchStateAccess.SetCpuBrain(ref state, slot, in brain);

            // Advance CPU RNG so takeover stays hash-stable and non-degenerate.
            _ = MatchStateAccess.CpuRng(ref state, slot).NextUInt64();
            return decided;
        }

        private static InputFrame Decide(ref MatchState state, int slot, ref CpuBrainSlot brain)
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
            return InputFrame.FromStick(dir, magnitude: 3);
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
