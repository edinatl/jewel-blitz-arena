namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Minimal jewel circulation for match rules (S1). Full physics/scramble is S2.
    /// Pickup is distance-based against field jewel positions.
    /// </summary>
    public static class JewelSystem
    {
        public static void InitializeOpening(ref MatchState state)
        {
            // Clear jewel pool.
            for (int i = 0; i < PoolCaps.Jewels; i++)
            {
                MatchStateAccess.SetJewel(ref state, i, default);
                MatchStateAccess.SetJewelRespawnFrame(ref state, i, 0);
            }

            state.ActiveJewelCount = 0;

            if (state.ActorCount <= 2)
            {
                // Each fighter starts holding one jewel; third spawns at 3 s.
                for (int i = 0; i < state.ActorCount; i++)
                {
                    ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, i);
                    actor.JewelsHeld = 1;
                    actor.JewelsCollectedTotal = 1;
                    state.ActiveJewelCount++;
                }

                state.NextJewelSpawnFrame = MatchRules.OpeningJewelDelayFrames;
            }
            else
            {
                state.NextJewelSpawnFrame = MatchRules.OpeningJewelDelayFrames;
            }
        }

        public static void Tick(ref MatchState state)
        {
            // Tick pickup lockouts on field jewels.
            for (int i = 0; i < PoolCaps.Jewels; i++)
            {
                JewelSlot jewel = MatchStateAccess.GetJewel(ref state, i);
                if ((jewel.Flags & MatchState.JewelFlagActive) != 0 && jewel.Reserved > 0)
                {
                    jewel.Reserved--;
                    MatchStateAccess.SetJewel(ref state, i, jewel);
                }
            }

            // Scheduled respawns after Blitz revert.
            for (int i = 0; i < PoolCaps.Jewels; i++)
            {
                int when = MatchStateAccess.GetJewelRespawnFrame(ref state, i);
                if (when > 0 && state.Frame >= when)
                {
                    MatchStateAccess.SetJewelRespawnFrame(ref state, i, 0);
                    TrySpawnFieldJewel(ref state, preferredSlot: i);
                }
            }

            if (state.NextJewelSpawnFrame > 0 && state.Frame >= state.NextJewelSpawnFrame)
            {
                if (CirculationCount(ref state) < state.JewelCap)
                {
                    TrySpawnFieldJewel(ref state, preferredSlot: -1);
                }

                // 2p: only the opening third; further spawns wait for drops/reverts.
                state.NextJewelSpawnFrame = 0;
            }

            // Pickup: walk over field jewels.
            for (int a = 0; a < state.ActorCount; a++)
            {
                ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, a);
                if (!actor.IsAlive || actor.IsEliminated || actor.IsBlitzed)
                {
                    continue;
                }

                if (actor.DrossFrames > 0 || actor.JewelsHeld >= MatchRules.BlitzJewelCount)
                {
                    continue;
                }

                for (int j = 0; j < PoolCaps.Jewels; j++)
                {
                    JewelSlot jewel = MatchStateAccess.GetJewel(ref state, j);
                    if ((jewel.Flags & MatchState.JewelFlagActive) == 0)
                    {
                        continue;
                    }

                    if (jewel.OwnerSlot != MatchState.JewelOwnerOnField)
                    {
                        continue;
                    }

                    // Reserved = pickup lockout frames after a drop (anti instant re-grab).
                    if (jewel.Reserved > 0)
                    {
                        continue;
                    }

                    if (DistanceSq(actor.PosX, actor.PosZ, jewel.PosX, jewel.PosZ)
                        <= MatchRules.JewelPickupRangeRaw * MatchRules.JewelPickupRangeRaw)
                    {
                        CollectJewel(ref state, ref actor, j);
                        break;
                    }
                }
            }
        }

        public static void CollectJewel(ref MatchState state, ref ActorSlot actor, int jewelIndex)
        {
            JewelSlot jewel = MatchStateAccess.GetJewel(ref state, jewelIndex);
            jewel.Flags = 0;
            jewel.OwnerSlot = (byte)255;
            MatchStateAccess.SetJewel(ref state, jewelIndex, jewel);

            actor.JewelsHeld++;
            actor.JewelsCollectedTotal++;

            if (actor.JewelsHeld >= MatchRules.BlitzJewelCount)
            {
                BeginBlitz(ref state, ref actor);
            }
        }

        public static void BeginBlitz(ref MatchState state, ref ActorSlot actor)
        {
            actor.JewelsHeld = 0;
            actor.IsBlitzed = true;
            actor.Luster = MatchRules.LusterMax;
            actor.TransformFrames = MatchRules.BlitzTransformFrames;
            actor.InvulnFrames = MatchRules.BlitzTransformFrames;
            actor.Action = (byte)ActorAction.Transform;
            actor.ActionPhase = (byte)ActionPhase.Startup;
            actor.ActionTimer = MatchRules.BlitzTransformFrames;
            actor.HitApplied = 0;
        }

        public static void OnKnockdownJewelDrop(ref MatchState state, ref ActorSlot actor)
        {
            if (actor.IsBlitzed || actor.JewelsHeld == 0)
            {
                return;
            }

            actor.JewelsHeld--;
            // Drop near the fighter with short re-grab lock (S2 will add bounce physics).
            TrySpawnFieldJewelAt(ref state, actor.PosX + Fix.One, actor.PosZ, slot: -1, pickupLockout: 36);
        }

        public static void OnRevert(ref MatchState state, ref ActorSlot actor)
        {
            actor.IsBlitzed = false;
            actor.Luster = 0;
            actor.JewelsHeld = 0;
            actor.DrossFrames = MatchRules.DrossFrames;
            actor.RevertFrames = MatchRules.RevertFrames;
            actor.Action = (byte)ActorAction.Revert;
            actor.ActionTimer = MatchRules.RevertFrames;
            actor.ActionPhase = (byte)ActionPhase.Recovery;

            // Three jewels respawn after delay (balance.md §5).
            int scheduled = 0;
            for (int i = 0; i < PoolCaps.Jewels && scheduled < MatchRules.BlitzJewelCount; i++)
            {
                if (MatchStateAccess.GetJewelRespawnFrame(ref state, i) == 0)
                {
                    JewelSlot jewel = MatchStateAccess.GetJewel(ref state, i);
                    if ((jewel.Flags & MatchState.JewelFlagActive) == 0)
                    {
                        MatchStateAccess.SetJewelRespawnFrame(ref state, i, state.Frame + MatchRules.JewelRespawnDelayFrames);
                        scheduled++;
                    }
                }
            }
        }

        public static int CirculationCount(ref MatchState state)
        {
            int count = 0;
            for (int i = 0; i < state.ActorCount; i++)
            {
                ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, i);
                if (actor.IsBlitzed)
                {
                    // Locked Blitz jewels still count toward the cap until respawn.
                    count += MatchRules.BlitzJewelCount;
                }
                else
                {
                    count += actor.JewelsHeld;
                }
            }

            for (int j = 0; j < PoolCaps.Jewels; j++)
            {
                JewelSlot jewel = MatchStateAccess.GetJewel(ref state, j);
                if ((jewel.Flags & MatchState.JewelFlagActive) != 0
                    && jewel.OwnerSlot == MatchState.JewelOwnerOnField)
                {
                    count++;
                }
            }

            // Pending respawns also reserve cap slots.
            for (int j = 0; j < PoolCaps.Jewels; j++)
            {
                if (MatchStateAccess.GetJewelRespawnFrame(ref state, j) > 0)
                {
                    count++;
                }
            }

            return count;
        }

        private static void TrySpawnFieldJewel(ref MatchState state, int preferredSlot)
        {
            if (CirculationCount(ref state) >= state.JewelCap)
            {
                return;
            }

            int slot = preferredSlot;
            if (slot < 0 || slot >= PoolCaps.Jewels)
            {
                slot = FindFreeJewelSlot(ref state);
            }

            if (slot < 0)
            {
                return;
            }

            // Spawn points along +Z axis by slot index (placeholder stage points).
            Fix x = Fix.FromInt((slot % 3) - 1);
            Fix z = Fix.FromInt(2 + (slot % 4));
            TrySpawnFieldJewelAt(ref state, x, z, slot);
        }

        private static void TrySpawnFieldJewelAt(
            ref MatchState state,
            Fix x,
            Fix z,
            int slot = -1,
            ushort pickupLockout = 0)
        {
            if (CirculationCount(ref state) >= state.JewelCap)
            {
                return;
            }

            if (slot < 0)
            {
                slot = FindFreeJewelSlot(ref state);
            }

            if (slot < 0)
            {
                return;
            }

            var jewel = new JewelSlot
            {
                PosX = x,
                PosY = Fix.Zero,
                PosZ = z,
                OwnerSlot = MatchState.JewelOwnerOnField,
                Flags = MatchState.JewelFlagActive,
                Reserved = pickupLockout,
            };
            MatchStateAccess.SetJewel(ref state, slot, jewel);
            state.ActiveJewelCount = CountFieldJewels(ref state);
        }

        private static int FindFreeJewelSlot(ref MatchState state)
        {
            for (int i = 0; i < PoolCaps.Jewels; i++)
            {
                JewelSlot jewel = MatchStateAccess.GetJewel(ref state, i);
                if ((jewel.Flags & MatchState.JewelFlagActive) == 0 && MatchStateAccess.GetJewelRespawnFrame(ref state, i) == 0)
                {
                    return i;
                }
            }

            return -1;
        }

        private static int CountFieldJewels(ref MatchState state)
        {
            int n = 0;
            for (int i = 0; i < PoolCaps.Jewels; i++)
            {
                JewelSlot jewel = MatchStateAccess.GetJewel(ref state, i);
                if ((jewel.Flags & MatchState.JewelFlagActive) != 0)
                {
                    n++;
                }
            }

            return n;
        }

        private static long DistanceSq(Fix ax, Fix az, Fix bx, Fix bz)
        {
            long dx = ax.Raw - bx.Raw;
            long dz = az.Raw - bz.Raw;
            // Approximate in Q48.16-squared space (raw deltas already scaled).
            return dx * dx + dz * dz;
        }
    }
}
