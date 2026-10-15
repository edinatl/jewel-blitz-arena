namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Loose-item pickup / throw / catch-window stubs. Damage from hits is S1.
    /// </summary>
    public static class ItemRuntime
    {
        public static void InitializeMatch(ref MatchState state)
        {
            state.ActiveLooseItemCount = 0;
            for (int i = 0; i < PoolCaps.LooseItems; i++)
            {
                MatchStateAccess.SetLooseItem(ref state, i, default);
            }
        }

        /// <summary>
        /// Process Action (pickup/catch) and Attack (throw held) for one actor.
        /// Call after movement velocities are set, before physics integrate of items.
        /// </summary>
        public static void ProcessActorButtons(
            ref MatchState state,
            int actorIndex,
            in InputFrame input,
            ref SimContactBuffer contacts)
        {
            ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, actorIndex);
            if (!actor.IsAlive)
            {
                return;
            }

            if (input.Action)
            {
                if (actor.HasEmptyHands)
                {
                    TryPickupNearest(ref state, actorIndex, ref contacts);
                }

                TryCatchThrown(ref state, actorIndex, ref contacts);
            }

            if (input.Attack && !actor.HasEmptyHands)
            {
                TryThrowHeld(ref state, actorIndex, input, ref contacts);
            }
        }

        public static void TickThrownHits(ref MatchState state, ref SimContactBuffer contacts)
        {
            for (int i = 0; i < PoolCaps.LooseItems; i++)
            {
                LooseItemSlot item = MatchStateAccess.GetLooseItem(ref state, i);
                if (!item.IsActive || !item.InFlight)
                {
                    continue;
                }

                for (int a = 0; a < state.ActorCount; a++)
                {
                    // Don't hit the thrower on the first few frames.
                    if (item.FlightFrames < 4)
                    {
                        continue;
                    }

                    ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, a);
                    if (!actor.IsAlive)
                    {
                        continue;
                    }

                    if (!PhysicsWorld.OverlapsXZ(
                            actor.PosX, actor.PosZ, item.PosX, item.PosZ,
                            PhysicsConstants.ActorRadius + PhysicsConstants.ItemRadius))
                    {
                        continue;
                    }

                    contacts.Add(SimEventKind.ThrownItemHitActor, (byte)i, (byte)a);
                    // Knock item out of flight onto field; S1 applies damage.
                    item.InFlight = false;
                    item.VelX = Fix.Zero;
                    item.VelY = Fix.Zero;
                    item.VelZ = Fix.Zero;
                    item.FlightFrames = 0;
                    MatchStateAccess.SetLooseItem(ref state, i, item);
                    break;
                }
            }
        }

        private static void TryPickupNearest(
            ref MatchState state, int actorIndex, ref SimContactBuffer contacts)
        {
            ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, actorIndex);
            int best = -1;
            long bestDist = long.MaxValue;

            for (int i = 0; i < PoolCaps.LooseItems; i++)
            {
                LooseItemSlot item = MatchStateAccess.GetLooseItem(ref state, i);
                if (!item.IsOnField)
                {
                    continue;
                }

                if (!PhysicsWorld.OverlapsXZ(
                        actor.PosX, actor.PosZ, item.PosX, item.PosZ,
                        PhysicsConstants.PickupRange))
                {
                    continue;
                }

                long d = PhysicsWorld.DistSqXZ(actor.PosX, actor.PosZ, item.PosX, item.PosZ);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = i;
                }
            }

            if (best < 0)
            {
                return;
            }

            LooseItemSlot chosen = MatchStateAccess.GetLooseItem(ref state, best);
            chosen.HolderSlot = (byte)actorIndex;
            chosen.VelX = Fix.Zero;
            chosen.VelY = Fix.Zero;
            chosen.VelZ = Fix.Zero;
            chosen.InFlight = false;
            MatchStateAccess.SetLooseItem(ref state, best, chosen);
            actor.HeldItemSlot = (byte)best;
            contacts.Add(SimEventKind.ActorItemPickup, (byte)actorIndex, (byte)best);
        }

        private static void TryThrowHeld(
            ref MatchState state, int actorIndex, in InputFrame input, ref SimContactBuffer contacts)
        {
            ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, actorIndex);
            int slot = actor.HeldItemSlot;
            if (slot == ActorSlot.NoHeldItem || slot >= PoolCaps.LooseItems)
            {
                return;
            }

            LooseItemSlot item = MatchStateAccess.GetLooseItem(ref state, slot);
            if (!item.IsActive || item.HolderSlot != actorIndex)
            {
                actor.HeldItemSlot = ActorSlot.NoHeldItem;
                return;
            }

            FixVec2 dir = input.Magnitude > 0
                ? DirectionLut.GetUnit(input.Direction)
                : DirectionLut.GetUnit(0);

            item.HolderSlot = LooseItemSlot.NoHolder;
            item.PosX = actor.PosX;
            item.PosY = actor.PosY + PhysicsConstants.ItemRadius;
            item.PosZ = actor.PosZ;
            item.VelX = dir.X * PhysicsConstants.ThrowSpeed / Simulation.TickRateHz;
            item.VelZ = dir.Y * PhysicsConstants.ThrowSpeed / Simulation.TickRateHz;
            item.VelY = Fix.FromInt(2) / Simulation.TickRateHz;
            item.InFlight = true;
            item.FlightFrames = 0;
            MatchStateAccess.SetLooseItem(ref state, slot, item);

            actor.HeldItemSlot = ActorSlot.NoHeldItem;
            contacts.Add(SimEventKind.ItemThrown, (byte)actorIndex, (byte)slot);
        }

        private static void TryCatchThrown(
            ref MatchState state, int actorIndex, ref SimContactBuffer contacts)
        {
            ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, actorIndex);
            if (!actor.HasEmptyHands)
            {
                return;
            }

            for (int i = 0; i < PoolCaps.LooseItems; i++)
            {
                LooseItemSlot item = MatchStateAccess.GetLooseItem(ref state, i);
                if (!item.IsActive || !item.InFlight)
                {
                    continue;
                }

                if (item.FlightFrames > PhysicsConstants.CatchWindowFrames + 30)
                {
                    // Still catchable while flying; combat says 6f of contact — we use proximity.
                }

                if (!PhysicsWorld.OverlapsXZ(
                        actor.PosX, actor.PosZ, item.PosX, item.PosZ,
                        PhysicsConstants.PickupRange))
                {
                    continue;
                }

                // Catch window relative to approach: accept when Action held and overlapping.
                item.HolderSlot = (byte)actorIndex;
                item.InFlight = false;
                item.FlightFrames = 0;
                item.VelX = Fix.Zero;
                item.VelY = Fix.Zero;
                item.VelZ = Fix.Zero;
                MatchStateAccess.SetLooseItem(ref state, i, item);
                actor.HeldItemSlot = (byte)i;
                contacts.Add(SimEventKind.ItemCaught, (byte)actorIndex, (byte)i);
                return;
            }
        }

        /// <summary>
        /// Clank Spare Parts magnet: strip held items from others in range and pull loose
        /// on-field items toward the caster (cast.md §2). Deterministic slot order.
        /// </summary>
        public static void ApplyMagnetSurge(ref MatchState state, int casterIndex, Fix range)
        {
            if (casterIndex < 0 || casterIndex >= state.ActorCount)
            {
                return;
            }

            ref ActorSlot caster = ref MatchStateAccess.Actor(ref state, casterIndex);
            long rangeSq = (range.Raw >> 8) * (range.Raw >> 8);

            for (int a = 0; a < state.ActorCount; a++)
            {
                if (a == casterIndex)
                {
                    continue;
                }

                ref ActorSlot other = ref MatchStateAccess.Actor(ref state, a);
                if (!other.IsAlive || other.IsEliminated || other.HasEmptyHands)
                {
                    continue;
                }

                if (DistSqScaled(caster.PosX, caster.PosZ, other.PosX, other.PosZ) > rangeSq)
                {
                    continue;
                }

                DropHeldToField(ref state, a);
            }

            for (int i = 0; i < PoolCaps.LooseItems; i++)
            {
                LooseItemSlot item = MatchStateAccess.GetLooseItem(ref state, i);
                if (!item.IsActive || item.HolderSlot != LooseItemSlot.NoHolder)
                {
                    continue;
                }

                if (DistSqScaled(caster.PosX, caster.PosZ, item.PosX, item.PosZ) > rangeSq)
                {
                    continue;
                }

                // Pull to just outside the caster capsule — half the remaining gap.
                Fix dx = caster.PosX - item.PosX;
                Fix dz = caster.PosZ - item.PosZ;
                item.PosX += dx / 2;
                item.PosZ += dz / 2;
                item.VelX = Fix.Zero;
                item.VelY = Fix.Zero;
                item.VelZ = Fix.Zero;
                item.InFlight = false;
                item.FlightFrames = 0;
                MatchStateAccess.SetLooseItem(ref state, i, item);
            }
        }

        private static void DropHeldToField(ref MatchState state, int actorIndex)
        {
            ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, actorIndex);
            int slot = actor.HeldItemSlot;
            if (slot == ActorSlot.NoHeldItem || slot >= PoolCaps.LooseItems)
            {
                actor.HeldItemSlot = ActorSlot.NoHeldItem;
                return;
            }

            LooseItemSlot item = MatchStateAccess.GetLooseItem(ref state, slot);
            if (!item.IsActive || item.HolderSlot != actorIndex)
            {
                actor.HeldItemSlot = ActorSlot.NoHeldItem;
                return;
            }

            item.HolderSlot = LooseItemSlot.NoHolder;
            item.PosX = actor.PosX;
            item.PosY = actor.PosY + PhysicsConstants.ItemRadius;
            item.PosZ = actor.PosZ;
            item.VelX = Fix.Zero;
            item.VelY = Fix.Zero;
            item.VelZ = Fix.Zero;
            item.InFlight = false;
            item.FlightFrames = 0;
            MatchStateAccess.SetLooseItem(ref state, slot, item);
            actor.HeldItemSlot = ActorSlot.NoHeldItem;
        }

        private static long DistSqScaled(Fix ax, Fix az, Fix bx, Fix bz)
        {
            long dx = (bx.Raw - ax.Raw) >> 8;
            long dz = (bz.Raw - az.Raw) >> 8;
            return (dx * dx) + (dz * dz);
        }
    }
}
