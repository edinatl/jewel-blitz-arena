namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Area status application for kit Surges (balance.md §4 / cast.md §5).
    /// Deterministic actor-index order; refreshes timers when the new duration is longer.
    /// </summary>
    public static class StatusRuntime
    {
        public static void ApplyAreaStun(
            ref MatchState state,
            int casterIndex,
            Fix range,
            int frames,
            bool groundedOnly)
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

                if (frames > victim.StunFrames)
                {
                    victim.StunFrames = frames;
                }
            }
        }

        public static void ApplyAreaBlind(
            ref MatchState state,
            int casterIndex,
            Fix range,
            int frames)
        {
            if (casterIndex < 0 || casterIndex >= state.ActorCount || frames <= 0 || range.Raw <= 0)
            {
                return;
            }

            if (frames > ushort.MaxValue)
            {
                frames = ushort.MaxValue;
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

                if (DistSqScaled(caster.PosX, caster.PosZ, victim.PosX, victim.PosZ) > rangeSq)
                {
                    continue;
                }

                if (frames > victim.BlindFrames)
                {
                    victim.BlindFrames = (ushort)frames;
                }
            }
        }

        private static long DistSqScaled(Fix ax, Fix az, Fix bx, Fix bz)
        {
            long dx = (bx.Raw - ax.Raw) >> 8;
            long dz = (bz.Raw - az.Raw) >> 8;
            return (dx * dx) + (dz * dz);
        }
    }
}
