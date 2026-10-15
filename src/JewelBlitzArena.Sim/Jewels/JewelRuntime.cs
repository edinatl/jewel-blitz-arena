namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Jewel circulation, spawn, pickup, and drop APIs (balance.md §5).
    /// Blitz application is S1 — S2 only emits <see cref="SimEventKind.JewelReadyToBlitz"/>.
    /// </summary>
    public static class JewelRuntime
    {
        public static int DefaultCapForActorCount(int actorCount)
        {
            if (actorCount <= 2)
            {
                return 3;
            }

            return 5;
        }

        public static void InitializeMatch(ref MatchState state, int jewelCapOverride)
        {
            state.JewelCap = jewelCapOverride > 0
                ? jewelCapOverride
                : DefaultCapForActorCount(state.ActorCount);
            state.ActiveJewelCount = 0;
            state.NextJewelSpawnFrame = PhysicsConstants.JewelOpenSpawnFrame;

            for (int i = 0; i < PoolCaps.Jewels; i++)
            {
                MatchStateAccess.SetJewel(ref state, i, default);
            }

            // Opening: 2-actor matches start with one jewel each (balance.md §5.3).
            if (state.ActorCount == 2)
            {
                GiveJewelToActorSilent(ref state, 0);
                GiveJewelToActorSilent(ref state, 1);
            }
        }

        public static int CountTowardCap(ref MatchState state)
        {
            int count = 0;
            for (int i = 0; i < PoolCaps.Jewels; i++)
            {
                if (MatchStateAccess.GetJewel(ref state, i).IsActive)
                {
                    count++;
                }
            }

            return count;
        }

        public static void TickSpawns(ref MatchState state)
        {
            if (state.Frame < state.NextJewelSpawnFrame)
            {
                return;
            }

            if (CountTowardCap(ref state) >= state.JewelCap)
            {
                state.NextJewelSpawnFrame = state.Frame + PhysicsConstants.JewelFollowupSpawnFrames;
                return;
            }

            if (TrySpawnOnField(ref state, PickSpawnPoint(ref state)))
            {
                state.NextJewelSpawnFrame = state.Frame + PhysicsConstants.JewelFollowupSpawnFrames;
            }
        }

        public static bool TrySpawnOnField(ref MatchState state, FixVec3 pos)
        {
            if (CountTowardCap(ref state) >= state.JewelCap)
            {
                return false;
            }

            int slot = FindInactiveJewelSlot(ref state);
            if (slot < 0)
            {
                return false;
            }

            var jewel = new JewelSlot
            {
                PosX = pos.X,
                PosY = pos.Y + PhysicsConstants.JewelRadius,
                PosZ = pos.Z,
                OwnerSlot = JewelSlot.NoOwner,
                PickupDelayFrames = 0,
            };
            jewel.IsActive = true;
            MatchStateAccess.SetJewel(ref state, slot, jewel);
            state.ActiveJewelCount = CountTowardCap(ref state);
            return true;
        }

        public static void TickPickups(ref MatchState state, ref SimContactBuffer contacts)
        {
            for (int a = 0; a < state.ActorCount; a++)
            {
                ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, a);
                if (!actor.IsAlive || actor.IsBlitzed || actor.DrossFrames > 0 || actor.JewelsHeld >= 3)
                {
                    continue;
                }

                for (int i = 0; i < PoolCaps.Jewels; i++)
                {
                    JewelSlot j = MatchStateAccess.GetJewel(ref state, i);
                    if (!j.IsOnField || j.PickupDelayFrames > 0)
                    {
                        continue;
                    }

                    if (!PhysicsWorld.OverlapsXZ(
                            actor.PosX, actor.PosZ, j.PosX, j.PosZ,
                            PhysicsConstants.PickupRange))
                    {
                        continue;
                    }

                    CollectJewel(ref state, a, i, ref contacts);
                    break;
                }
            }

            for (int a = 0; a < state.ActorCount; a++)
            {
                ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, a);
                if (actor.DrossFrames > 0)
                {
                    actor.DrossFrames--;
                }
            }
        }

        /// <summary>S1 knockdown hook: drop one held jewel onto the field.</summary>
        public static bool TryDropOneHeldJewel(ref MatchState state, int actorIndex)
        {
            ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, actorIndex);
            if (actor.JewelsHeld == 0)
            {
                return false;
            }

            int jewelIndex = FindOwnedJewel(ref state, actorIndex);
            if (jewelIndex < 0)
            {
                actor.JewelsHeld = 0;
                return false;
            }

            ReleaseJewelToField(ref state, jewelIndex, actor.PosX, actor.PosY, actor.PosZ, scatter: true);
            actor.JewelsHeld--;
            return true;
        }

        /// <summary>S1 fall-out hook: drop all held jewels.</summary>
        public static void TryDropAllHeldJewels(ref MatchState state, int actorIndex)
        {
            ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, actorIndex);
            while (actor.JewelsHeld > 0)
            {
                if (!TryDropOneHeldJewel(ref state, actorIndex))
                {
                    break;
                }
            }
        }

        private static void CollectJewel(
            ref MatchState state, int actorIndex, int jewelIndex, ref SimContactBuffer contacts)
        {
            ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, actorIndex);
            JewelSlot j = MatchStateAccess.GetJewel(ref state, jewelIndex);
            j.OwnerSlot = (byte)actorIndex;
            j.VelX = Fix.Zero;
            j.VelY = Fix.Zero;
            j.VelZ = Fix.Zero;
            MatchStateAccess.SetJewel(ref state, jewelIndex, j);

            actor.JewelsHeld++;
            contacts.Add(SimEventKind.ActorJewelPickup, (byte)actorIndex, (byte)jewelIndex);
            if (actor.JewelsHeld >= 3)
            {
                contacts.Add(SimEventKind.JewelReadyToBlitz, (byte)actorIndex, (byte)jewelIndex);
            }
        }

        private static void GiveJewelToActorSilent(ref MatchState state, int actorIndex)
        {
            if (CountTowardCap(ref state) >= state.JewelCap)
            {
                return;
            }

            int slot = FindInactiveJewelSlot(ref state);
            if (slot < 0)
            {
                return;
            }

            ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, actorIndex);
            var jewel = new JewelSlot
            {
                PosX = actor.PosX,
                PosY = actor.PosY + PhysicsConstants.JewelRadius,
                PosZ = actor.PosZ,
                OwnerSlot = (byte)actorIndex,
            };
            jewel.IsActive = true;
            MatchStateAccess.SetJewel(ref state, slot, jewel);
            actor.JewelsHeld++;
            state.ActiveJewelCount = CountTowardCap(ref state);
        }

        private static void ReleaseJewelToField(
            ref MatchState state, int jewelIndex, Fix x, Fix y, Fix z, bool scatter)
        {
            JewelSlot j = MatchStateAccess.GetJewel(ref state, jewelIndex);
            j.OwnerSlot = JewelSlot.NoOwner;
            j.PosX = x;
            j.PosY = y + PhysicsConstants.JewelRadius;
            j.PosZ = z;
            j.PickupDelayFrames = (ushort)PhysicsConstants.JewelPickupDelayFrames;
            if (scatter)
            {
                ulong r = state.GameplayRng.NextUInt64();
                int dir = (int)(r & 31);
                FixVec2 unit = DirectionLut.GetUnit(dir);
                Fix speed = Fix.FromInt(3);
                j.VelX = unit.X * speed / Simulation.TickRateHz;
                j.VelZ = unit.Y * speed / Simulation.TickRateHz;
                j.VelY = Fix.FromInt(4) / Simulation.TickRateHz;
            }
            else
            {
                j.VelX = Fix.Zero;
                j.VelY = Fix.Zero;
                j.VelZ = Fix.Zero;
            }

            MatchStateAccess.SetJewel(ref state, jewelIndex, j);
        }

        private static int FindInactiveJewelSlot(ref MatchState state)
        {
            for (int i = 0; i < PoolCaps.Jewels; i++)
            {
                if (!MatchStateAccess.GetJewel(ref state, i).IsActive)
                {
                    return i;
                }
            }

            return -1;
        }

        private static int FindOwnedJewel(ref MatchState state, int actorIndex)
        {
            for (int i = 0; i < PoolCaps.Jewels; i++)
            {
                JewelSlot j = MatchStateAccess.GetJewel(ref state, i);
                if (j.IsActive && j.OwnerSlot == actorIndex)
                {
                    return i;
                }
            }

            return -1;
        }

        private static FixVec3 PickSpawnPoint(ref MatchState state)
        {
            int idx = state.GameplayRng.NextInt(4);
            Fix e = Fix.FromInt(4);
            switch (idx)
            {
                case 0: return new FixVec3(-e, Fix.Zero, -e);
                case 1: return new FixVec3(e, Fix.Zero, -e);
                case 2: return new FixVec3(-e, Fix.Zero, e);
                default: return new FixVec3(e, Fix.Zero, e);
            }
        }
    }
}
