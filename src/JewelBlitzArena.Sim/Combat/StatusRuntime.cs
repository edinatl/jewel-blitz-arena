namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Area status application for kit Surges (balance.md §4 / cast.md §5).
    /// Deterministic actor-index order; refreshes timers when the new duration is longer;
    /// respects Dizzy/Pin/Freeze immunities.
    /// </summary>
    public static class StatusRuntime
    {
        private enum Kind : byte
        {
            Stun = 0,
            Blind = 1,
            Dizzy = 2,
            Slow = 3,
            Pin = 4,
            Freeze = 5,
        }

        public static void ApplyAreaStun(
            ref MatchState state, int casterIndex, Fix range, int frames, bool groundedOnly)
        {
            Apply(ref state, casterIndex, range, CapHard(frames), groundedOnly, Kind.Stun);
        }

        public static void ApplyAreaBlind(
            ref MatchState state, int casterIndex, Fix range, int frames)
        {
            Apply(ref state, casterIndex, range, ClampUShort(frames), groundedOnly: false, Kind.Blind);
        }

        public static void ApplyAreaDizzy(
            ref MatchState state, int casterIndex, Fix range, int frames)
        {
            Apply(ref state, casterIndex, range, ClampUShort(CapHard(frames)), groundedOnly: false, Kind.Dizzy);
        }

        public static void ApplyAreaSlow(
            ref MatchState state, int casterIndex, Fix range, int frames)
        {
            Apply(ref state, casterIndex, range, ClampUShort(frames), groundedOnly: false, Kind.Slow);
        }

        public static void ApplyAreaPin(
            ref MatchState state, int casterIndex, Fix range, int frames)
        {
            Apply(ref state, casterIndex, range, ClampUShort(CapHard(frames)), groundedOnly: true, Kind.Pin);
        }

        public static void ApplyAreaFreeze(
            ref MatchState state, int casterIndex, Fix range, int frames)
        {
            Apply(ref state, casterIndex, range, ClampUShort(CapHard(frames)), groundedOnly: false, Kind.Freeze);
        }

        /// <summary>Single-target Slow (Adhesive Spray thrown hits).</summary>
        public static void ApplySlow(ref MatchState state, int victimIndex, int frames)
        {
            if (victimIndex < 0 || victimIndex >= state.ActorCount || frames <= 0)
            {
                return;
            }

            ref ActorSlot victim = ref MatchStateAccess.Actor(ref state, victimIndex);
            if (!victim.IsAlive || victim.IsEliminated || victim.InvulnFrames > 0)
            {
                return;
            }

            int f = ClampUShort(frames);
            if (f > victim.SlowFrames)
            {
                victim.SlowFrames = (ushort)f;
            }
        }

        /// <summary>Single-target Freeze / Petrify (Petrify Gas thrown hits).</summary>
        public static void ApplyFreeze(ref MatchState state, int victimIndex, int frames)
        {
            if (victimIndex < 0 || victimIndex >= state.ActorCount || frames <= 0)
            {
                return;
            }

            ref ActorSlot victim = ref MatchStateAccess.Actor(ref state, victimIndex);
            ApplyFreezeToVictim(ref victim, ClampUShort(CapHard(frames)));
        }

        /// <summary>
        /// Freeze all actors in radius of a world point (Ice Jar floor-patch stub).
        /// Deterministic actor-index order; <paramref name="excludeActor"/> skips the thrower.
        /// </summary>
        public static void ApplyFreezeInRadius(
            ref MatchState state, Fix centerX, Fix centerZ, Fix range, int frames, int excludeActor = -1)
        {
            if (frames <= 0 || range.Raw <= 0)
            {
                return;
            }

            int f = ClampUShort(CapHard(frames));
            long rangeSq = (range.Raw >> 8) * (range.Raw >> 8);

            for (int i = 0; i < state.ActorCount; i++)
            {
                if (i == excludeActor)
                {
                    continue;
                }

                ref ActorSlot victim = ref MatchStateAccess.Actor(ref state, i);
                if (!victim.IsAlive || victim.IsEliminated || victim.InvulnFrames > 0)
                {
                    continue;
                }

                if (DistSqScaled(centerX, centerZ, victim.PosX, victim.PosZ) > rangeSq)
                {
                    continue;
                }

                ApplyFreezeToVictim(ref victim, f);
            }
        }

        private static void Apply(
            ref MatchState state,
            int casterIndex,
            Fix range,
            int frames,
            bool groundedOnly,
            Kind kind)
        {
            if (casterIndex < 0 || casterIndex >= state.ActorCount || frames <= 0 || range.Raw <= 0)
            {
                return;
            }

            ref ActorSlot caster = ref MatchStateAccess.Actor(ref state, casterIndex);
            long rangeSq = (range.Raw >> 8) * (range.Raw >> 8);

            for (int i = 0; i < state.ActorCount; i++)
            {
                if (i == casterIndex)
                {
                    continue;
                }

                ref ActorSlot victim = ref MatchStateAccess.Actor(ref state, i);
                if (!victim.IsAlive || victim.IsEliminated || victim.InvulnFrames > 0)
                {
                    continue;
                }

                if (groundedOnly && victim.PosY.Raw > 0)
                {
                    continue;
                }

                if (DistSqScaled(caster.PosX, caster.PosZ, victim.PosX, victim.PosZ) > rangeSq)
                {
                    continue;
                }

                switch (kind)
                {
                    case Kind.Stun:
                        if (frames > victim.StunFrames)
                        {
                            victim.StunFrames = frames;
                        }

                        break;
                    case Kind.Blind:
                        if (frames > victim.BlindFrames)
                        {
                            victim.BlindFrames = (ushort)frames;
                        }

                        break;
                    case Kind.Dizzy:
                        if (victim.DizzyImmuneFrames > 0)
                        {
                            break;
                        }

                        if (frames > victim.DizzyFrames)
                        {
                            victim.DizzyFrames = (ushort)frames;
                        }

                        break;
                    case Kind.Slow:
                        if (frames > victim.SlowFrames)
                        {
                            victim.SlowFrames = (ushort)frames;
                        }

                        break;
                    case Kind.Pin:
                        if (victim.PinImmuneFrames > 0)
                        {
                            break;
                        }

                        if (frames > victim.PinFrames)
                        {
                            victim.PinFrames = (ushort)frames;
                        }

                        break;
                    case Kind.Freeze:
                        ApplyFreezeToVictim(ref victim, frames);
                        break;
                }
            }
        }

        private static void ApplyFreezeToVictim(ref ActorSlot victim, int frames)
        {
            if (!victim.IsAlive || victim.IsEliminated || victim.InvulnFrames > 0)
            {
                return;
            }

            if (victim.FreezeImmuneFrames > 0)
            {
                return;
            }

            if (frames > victim.FreezeFrames)
            {
                victim.FreezeFrames = (ushort)frames;
            }
        }

        private static int CapHard(int frames) =>
            frames > MatchRules.HardControlCapFrames ? MatchRules.HardControlCapFrames : frames;

        private static int ClampUShort(int frames) =>
            frames > ushort.MaxValue ? ushort.MaxValue : frames;

        private static long DistSqScaled(Fix ax, Fix az, Fix bx, Fix bz)
        {
            long dx = (bx.Raw - ax.Raw) >> 8;
            long dz = (bz.Raw - az.Raw) >> 8;
            return (dx * dx) + (dz * dz);
        }
    }
}
