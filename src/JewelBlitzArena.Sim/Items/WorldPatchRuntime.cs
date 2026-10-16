namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Lasting floor patches from thrown Ice Jar / Smoke Jar (items-and-economy.md).
    /// Stored in the unused GleamPickups generic pool (no MatchState layout growth).
    /// </summary>
    public static class WorldPatchRuntime
    {
        public const int KindIce = 1;
        public const int KindSmoke = 2;
        public const int KindSlow = 3; // Adhesive Spray puddle
        public const byte FlagActive = 1 << 0;

        public static void InitializeMatch(ref MatchState state)
        {
            for (int i = 0; i < PoolCaps.GleamPickups; i++)
            {
                MatchStateAccess.SetWorldPatch(ref state, i, default);
            }
        }

        /// <summary>Spawn Ice or Smoke patch at impact. Oldest active patch is replaced if full.</summary>
        public static int Spawn(
            ref MatchState state, int kind, Fix centerX, Fix centerZ, Fix radius, int lifetimeFrames)
        {
            if ((kind != KindIce && kind != KindSmoke && kind != KindSlow)
                || lifetimeFrames <= 0
                || radius.Raw <= 0)
            {
                return -1;
            }

            int slot = FindFreeSlot(ref state);
            if (slot < 0)
            {
                slot = FindOldestSlot(ref state);
            }

            if (slot < 0)
            {
                return -1;
            }

            var patch = new GenericPoolSlot
            {
                PosX = centerX,
                PosY = radius,
                PosZ = centerZ,
                TypeId = kind,
                Param0 = lifetimeFrames,
                Flags = FlagActive,
            };
            MatchStateAccess.SetWorldPatch(ref state, slot, patch);
            return slot;
        }

        public static void TrySpawnFromItem(
            ref MatchState state, int itemId, Fix x, Fix z)
        {
            if (itemId == MatchRules.IceJarItemId)
            {
                Spawn(
                    ref state,
                    KindIce,
                    x,
                    z,
                    MatchRules.IceJarFreezeRange,
                    MatchRules.IcePatchLifetimeFrames);
            }
            else if (itemId == MatchRules.SmokeJarItemId)
            {
                Spawn(
                    ref state,
                    KindSmoke,
                    x,
                    z,
                    MatchRules.SmokeJarBlindRange,
                    MatchRules.SmokePatchLifetimeFrames);
            }
            else if (itemId == MatchRules.AdhesiveSprayItemId)
            {
                Spawn(
                    ref state,
                    KindSlow,
                    x,
                    z,
                    MatchRules.AdhesiveSlowRange,
                    MatchRules.AdhesivePatchLifetimeFrames);
            }
        }

        /// <summary>
        /// Tick lifetimes and apply status pulses to actors standing in patches.
        /// Deterministic slot order then actor order.
        /// </summary>
        public static void Tick(ref MatchState state)
        {
            for (int i = 0; i < PoolCaps.GleamPickups; i++)
            {
                GenericPoolSlot patch = MatchStateAccess.GetWorldPatch(ref state, i);
                if ((patch.Flags & FlagActive) == 0)
                {
                    continue;
                }

                patch.Param0--;
                if (patch.Param0 <= 0)
                {
                    MatchStateAccess.SetWorldPatch(ref state, i, default);
                    continue;
                }

                ApplyPulse(ref state, in patch);
                MatchStateAccess.SetWorldPatch(ref state, i, patch);
            }
        }

        public static int CountActive(ref MatchState state)
        {
            int n = 0;
            for (int i = 0; i < PoolCaps.GleamPickups; i++)
            {
                GenericPoolSlot patch = MatchStateAccess.GetWorldPatch(ref state, i);
                if ((patch.Flags & FlagActive) != 0)
                {
                    n++;
                }
            }

            return n;
        }

        private static void ApplyPulse(ref MatchState state, in GenericPoolSlot patch)
        {
            Fix radius = patch.PosY;
            long rangeSq = (radius.Raw >> 8) * (radius.Raw >> 8);

            for (int a = 0; a < state.ActorCount; a++)
            {
                ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, a);
                if (!actor.IsAlive || actor.IsEliminated || actor.InvulnFrames > 0)
                {
                    continue;
                }

                if (DistSqScaled(patch.PosX, patch.PosZ, actor.PosX, actor.PosZ) > rangeSq)
                {
                    continue;
                }

                if (patch.TypeId == KindIce)
                {
                    StatusRuntime.ApplyFreeze(ref state, a, MatchRules.WorldPatchStatusPulseFrames);
                }
                else if (patch.TypeId == KindSmoke)
                {
                    StatusRuntime.ApplyBlind(ref state, a, MatchRules.WorldPatchStatusPulseFrames);
                }
                else if (patch.TypeId == KindSlow)
                {
                    StatusRuntime.ApplySlow(ref state, a, MatchRules.WorldPatchStatusPulseFrames);
                }
            }
        }

        private static int FindFreeSlot(ref MatchState state)
        {
            for (int i = 0; i < PoolCaps.GleamPickups; i++)
            {
                GenericPoolSlot patch = MatchStateAccess.GetWorldPatch(ref state, i);
                if ((patch.Flags & FlagActive) == 0)
                {
                    return i;
                }
            }

            return -1;
        }

        private static int FindOldestSlot(ref MatchState state)
        {
            int best = -1;
            int bestLife = int.MaxValue;
            for (int i = 0; i < PoolCaps.GleamPickups; i++)
            {
                GenericPoolSlot patch = MatchStateAccess.GetWorldPatch(ref state, i);
                if ((patch.Flags & FlagActive) == 0)
                {
                    continue;
                }

                if (patch.Param0 < bestLife || (patch.Param0 == bestLife && i < best))
                {
                    bestLife = patch.Param0;
                    best = i;
                }
            }

            return best;
        }

        private static long DistSqScaled(Fix ax, Fix az, Fix bx, Fix bz)
        {
            long dx = (bx.Raw - ax.Raw) >> 8;
            long dz = (bz.Raw - az.Raw) >> 8;
            return (dx * dx) + (dz * dz);
        }
    }
}
