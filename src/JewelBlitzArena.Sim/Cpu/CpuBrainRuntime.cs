namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Deterministic CPU brain (architecture §2.6 / combat §10.1). Thinks on a staggered
    /// schedule keyed by CPU level 1–8; branches on named <see cref="KitBehavior"/> personalities
    /// (architecture §2.5) using kit reach and input buttons. Holds
    /// <see cref="CpuBrainSlot.LastDecision"/> between thinks.
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

            int chaseDir = BestDirection(dx, dz);
            int mag = StickMagnitudeForLevel(level);
            long reachRaw = Fix.FromInt(self.ReachRating > 0 ? self.ReachRating : 3).Raw;
            long distSq = ((dx.Raw >> 8) * (dx.Raw >> 8)) + ((dz.Raw >> 8) * (dz.Raw >> 8));
            long reachSq = (reachRaw >> 8) * (reachRaw >> 8);
            long midSq = reachSq * 4; // ~2× reach

            ref Xoshiro256StarStar rng = ref MatchStateAccess.CpuRng(ref state, slot);
            var behavior = (KitBehavior)self.KitBehavior;
            InputFrame frame = behavior switch
            {
                KitBehavior.VanishStrong => DecideVanishStrong(chaseDir, mag, distSq, reachSq, midSq, level, ref rng),
                KitBehavior.CommandGrabCarry => DecideCommandGrab(
                    ref self, chaseDir, mag, distSq, reachSq, level, ref rng),
                KitBehavior.RiposteStrong => DecideRiposte(dx, dz, chaseDir, mag, distSq, reachSq, midSq, level, ref rng),
                KitBehavior.ExtendedDodge => DecideExtendedDodge(chaseDir, mag, distSq, reachSq, level, ref rng),
                KitBehavior.TripleJumpGlide => DecideTripleJump(chaseDir, mag, distSq, reachSq, level, ref rng),
                KitBehavior.DoubleHold => DecideDoubleHold(ref self, chaseDir, mag, distSq, reachSq, level, ref rng),
                KitBehavior.SwapPartners => DecideSwapStrafe(dx, dz, mag, distSq, reachSq, level, ref rng),
                KitBehavior.InvoiceRepeatable => DecideInvoice(chaseDir, mag, distSq, reachSq, level, ref rng),
                _ => DecideDefaultPressure(chaseDir, mag, distSq, reachSq, level, ref rng),
            };

            // Blitzed: Attack → Flare; spend Luster on Surge A/B when affordable.
            ApplyBlitzPressure(ref frame, ref self, behavior, distSq, reachSq, level, ref rng);
            return frame;
        }

        /// <summary>
        /// When Blitzed, prefer Flare (Attack) in reach and occasionally fire Surge A/B.
        /// Invoice Surge A uses <see cref="MatchRules.InvoiceSurgeLusterCost"/>; other Surges use
        /// <see cref="MatchRules.SurgeLusterCost"/>. Clank (DoubleHold) also picks a stick
        /// direction so Combat resolves Toolbox / Spare Parts variants.
        /// Only consumes CPU RNG while Blitzed so non-Blitz paths stay hash-stable.
        /// </summary>
        private static void ApplyBlitzPressure(
            ref InputFrame frame,
            ref ActorSlot self,
            KitBehavior behavior,
            long distSq,
            long reachSq,
            int level,
            ref Xoshiro256StarStar rng)
        {
            if (!self.IsBlitzed || level < 3 || distSq > reachSq * 4)
            {
                return;
            }

            uint roll = (uint)(rng.NextUInt64() & 0xFF);
            bool preferSurgeA = behavior == KitBehavior.InvoiceRepeatable || (roll & 1) == 0;
            int surgeCost = MatchRules.SurgeLusterCostFor(behavior, surgeA: preferSurgeA);

            if (self.Luster >= surgeCost && roll < (uint)(24 + level * 10))
            {
                if (preferSurgeA)
                {
                    frame.SurgeA = true;
                }
                else
                {
                    frame.SurgeB = true;
                }

                if (behavior == KitBehavior.DoubleHold)
                {
                    ApplyClankSurgeStick(ref frame, self.Facing, roll);
                }

                return;
            }

            if (distSq <= reachSq && !frame.Strong && !frame.Action && !frame.Jump)
            {
                if (roll < (uint)(90 + level * 12))
                {
                    frame.Attack = true;
                }
            }
        }

        /// <summary>Set stick to a Clank Surge variant cardinal (N/F/U/D); forward uses facing.</summary>
        private static void ApplyClankSurgeStick(ref InputFrame frame, byte facing, uint roll)
        {
            int variant = (int)(roll >> 6) & 0x3; // 0..3
            switch (variant)
            {
                case 0: // neutral
                    frame.Magnitude = 0;
                    break;
                case 1: // facing-relative forward
                    frame.Direction = facing == 0 ? 0 : 16;
                    frame.Magnitude = 2;
                    break;
                case 2: // up +Z
                    frame.Direction = 8;
                    frame.Magnitude = 2;
                    break;
                default: // down −Z
                    frame.Direction = 24;
                    frame.Magnitude = 2;
                    break;
            }
        }

        private static InputFrame DecideDefaultPressure(
            int chaseDir,
            int mag,
            long distSq,
            long reachSq,
            int level,
            ref Xoshiro256StarStar rng)
        {
            var frame = InputFrame.FromStick(chaseDir, mag);
            if (distSq <= reachSq && level >= 3)
            {
                MaybePressAttack(ref frame, level, ref rng, preferStrong: false);
            }

            return frame;
        }

        /// <summary>Sable-style: approach then prefer Strong (vanish) at mid/close range.</summary>
        private static InputFrame DecideVanishStrong(
            int chaseDir,
            int mag,
            long distSq,
            long reachSq,
            long midSq,
            int level,
            ref Xoshiro256StarStar rng)
        {
            var frame = InputFrame.FromStick(chaseDir, mag);
            if (distSq <= midSq && level >= 3)
            {
                uint roll = (uint)(rng.NextUInt64() & 0xFF);
                int threshold = 50 + (level * 18);
                if (roll < (uint)threshold)
                {
                    frame.Strong = true;
                    if (distSq <= reachSq && (roll & 0x3) == 0)
                    {
                        frame.Attack = true;
                    }
                }
            }

            return frame;
        }

        /// <summary>Bulwark-style: close for Action (command grab / carry); Attack if already holding.</summary>
        private static InputFrame DecideCommandGrab(
            ref ActorSlot self,
            int chaseDir,
            int mag,
            long distSq,
            long reachSq,
            int level,
            ref Xoshiro256StarStar rng)
        {
            var frame = InputFrame.FromStick(chaseDir, mag);
            if (distSq > reachSq || level < 3)
            {
                return frame;
            }

            uint roll = (uint)(rng.NextUInt64() & 0xFF);
            if (!self.HasEmptyHands)
            {
                if (roll < (uint)(60 + level * 15))
                {
                    frame.Attack = true;
                }
            }
            else if (roll < (uint)(80 + level * 12))
            {
                frame.Action = true;
            }

            return frame;
        }

        /// <summary>Lune-style: space out beyond mid-range, Strong when the foe enters reach.</summary>
        private static InputFrame DecideRiposte(
            Fix dx,
            Fix dz,
            int chaseDir,
            int mag,
            long distSq,
            long reachSq,
            long midSq,
            int level,
            ref Xoshiro256StarStar rng)
        {
            int dir = chaseDir;
            int useMag = mag;
            if (distSq > midSq)
            {
                // Hold spacing — retreat.
                dir = BestDirection(-dx, -dz);
                useMag = StickMagnitudeForLevel(level <= 4 ? level : 4);
            }

            var frame = InputFrame.FromStick(dir, useMag);
            if (distSq <= reachSq && level >= 3)
            {
                uint roll = (uint)(rng.NextUInt64() & 0xFF);
                if (roll < (uint)(70 + level * 16))
                {
                    frame.Strong = true;
                }
            }

            return frame;
        }

        /// <summary>Extended dodge: Jump when pressured in reach; otherwise chase.</summary>
        private static InputFrame DecideExtendedDodge(
            int chaseDir,
            int mag,
            long distSq,
            long reachSq,
            int level,
            ref Xoshiro256StarStar rng)
        {
            var frame = InputFrame.FromStick(chaseDir, mag);
            if (distSq <= reachSq && level >= 2)
            {
                uint roll = (uint)(rng.NextUInt64() & 0xFF);
                if (roll < (uint)(90 + level * 10))
                {
                    frame.Jump = true;
                }
                else if (level >= 4 && roll < (uint)(40 + level * 8))
                {
                    frame.Attack = true;
                }
            }

            return frame;
        }

        /// <summary>Aerial approach: Jump while closing, Attack in reach.</summary>
        private static InputFrame DecideTripleJump(
            int chaseDir,
            int mag,
            long distSq,
            long reachSq,
            int level,
            ref Xoshiro256StarStar rng)
        {
            var frame = InputFrame.FromStick(chaseDir, mag);
            uint roll = (uint)(rng.NextUInt64() & 0xFF);
            if (distSq > reachSq && roll < (uint)(50 + level * 12))
            {
                frame.Jump = true;
            }
            else if (distSq <= reachSq && level >= 3)
            {
                MaybePressAttack(ref frame, level, ref rng, preferStrong: false);
            }

            return frame;
        }

        /// <summary>Clank-style: prioritize Action to fill hands, then Attack.</summary>
        private static InputFrame DecideDoubleHold(
            ref ActorSlot self,
            int chaseDir,
            int mag,
            long distSq,
            long reachSq,
            int level,
            ref Xoshiro256StarStar rng)
        {
            var frame = InputFrame.FromStick(chaseDir, mag);
            uint roll = (uint)(rng.NextUInt64() & 0xFF);
            if (self.HasEmptyHands && level >= 2 && roll < (uint)(100 + level * 10))
            {
                frame.Action = true;
                return frame;
            }

            if (distSq <= reachSq && level >= 3)
            {
                MaybePressAttack(ref frame, level, ref rng, preferStrong: false);
            }

            return frame;
        }

        /// <summary>Pip & Pepper-style: strafe (perpendicular) while pressuring.</summary>
        private static InputFrame DecideSwapStrafe(
            Fix dx,
            Fix dz,
            int mag,
            long distSq,
            long reachSq,
            int level,
            ref Xoshiro256StarStar rng)
        {
            uint roll = (uint)(rng.NextUInt64() & 0xFF);
            int dir = (roll & 1) == 0
                ? BestDirection(-dz, dx)
                : BestDirection(dz, -dx);
            if (distSq > reachSq * 9 && (roll & 0x3) == 0)
            {
                // Occasionally cut in.
                dir = BestDirection(dx, dz);
            }

            var frame = InputFrame.FromStick(dir, mag);
            if (distSq <= reachSq && level >= 3)
            {
                MaybePressAttack(ref frame, level, ref rng, preferStrong: false);
            }

            return frame;
        }

        /// <summary>Invoice-style: dense Attack strings in reach; rare Strong.</summary>
        private static InputFrame DecideInvoice(
            int chaseDir,
            int mag,
            long distSq,
            long reachSq,
            int level,
            ref Xoshiro256StarStar rng)
        {
            var frame = InputFrame.FromStick(chaseDir, mag);
            if (distSq <= reachSq && level >= 2)
            {
                uint roll = (uint)(rng.NextUInt64() & 0xFF);
                int threshold = 100 + (level * 18);
                if (threshold > 240)
                {
                    threshold = 240;
                }

                if (roll < (uint)threshold)
                {
                    frame.Attack = true;
                }

                if (level >= 5 && (roll & 0xF) == 0)
                {
                    frame.Strong = true;
                }
            }

            return frame;
        }

        private static void MaybePressAttack(
            ref InputFrame frame,
            int level,
            ref Xoshiro256StarStar rng,
            bool preferStrong)
        {
            uint roll = (uint)(rng.NextUInt64() & 0xFF);
            int threshold = 40 + (level * 20);
            if (threshold > 230)
            {
                threshold = 230;
            }

            if (roll >= (uint)threshold)
            {
                return;
            }

            if (preferStrong || (level >= 6 && (roll & 0x7) == 0))
            {
                frame.Strong = true;
            }
            else
            {
                frame.Attack = true;
            }
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
