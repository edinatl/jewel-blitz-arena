namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Loose-item pickup / throw / catch-window stubs. Damage from hits is S1.
    /// Clank (<see cref="KitBehavior.DoubleHold"/>) uses right + left hands;
    /// Attack throws right, Strong throws left.
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
        /// Process Action (pickup/catch), Attack (throw right), Strong (throw left for dual-hold).
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
                // Blinded: can't pick up or catch (balance.md §4).
                if (actor.BlindFrames == 0)
                {
                    if (actor.HasFreeHand)
                    {
                        TryPickupNearest(ref state, actorIndex, ref contacts);
                    }

                    TryCatchThrown(ref state, actorIndex, ref contacts);
                }
            }

            if (input.Attack && actor.HeldItemSlot != ActorSlot.NoHeldItem)
            {
                TryThrowHeld(ref state, actorIndex, input, leftHand: false, ref contacts);
            }

            if (input.Strong && actor.HeldItemSlotLeft != ActorSlot.NoHeldItem)
            {
                TryThrowHeld(ref state, actorIndex, input, leftHand: true, ref contacts);
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
            if (!AssignToFreeHand(ref actor, (byte)best))
            {
                // No free hand (race); put item back on field.
                chosen.HolderSlot = LooseItemSlot.NoHolder;
                MatchStateAccess.SetLooseItem(ref state, best, chosen);
                return;
            }

            contacts.Add(SimEventKind.ActorItemPickup, (byte)actorIndex, (byte)best);
        }

        private static void TryThrowHeld(
            ref MatchState state,
            int actorIndex,
            in InputFrame input,
            bool leftHand,
            ref SimContactBuffer contacts)
        {
            ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, actorIndex);
            int slot = leftHand ? actor.HeldItemSlotLeft : actor.HeldItemSlot;
            if (slot == ActorSlot.NoHeldItem || slot >= PoolCaps.LooseItems)
            {
                return;
            }

            LooseItemSlot item = MatchStateAccess.GetLooseItem(ref state, slot);
            if (!item.IsActive || item.HolderSlot != actorIndex)
            {
                ClearHand(ref actor, leftHand);
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

            ClearHand(ref actor, leftHand);
            contacts.Add(SimEventKind.ItemThrown, (byte)actorIndex, (byte)slot);
        }

        private static void TryCatchThrown(
            ref MatchState state, int actorIndex, ref SimContactBuffer contacts)
        {
            ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, actorIndex);
            if (!actor.HasFreeHand)
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
                if (!AssignToFreeHand(ref actor, (byte)i))
                {
                    item.HolderSlot = LooseItemSlot.NoHolder;
                    MatchStateAccess.SetLooseItem(ref state, i, item);
                    return;
                }

                contacts.Add(SimEventKind.ItemCaught, (byte)actorIndex, (byte)i);
                return;
            }
        }

        /// <summary>
        /// Clank Spare Parts magnet: strip held items from others in range, catch the
        /// nearest loose item into a free hand, and snap remaining field items to the
        /// caster's pickup ring (cast.md §2). Deterministic slot order.
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

            // Prefer catching the nearest in-range field item into a free hand.
            int catchSlot = -1;
            long catchDistSq = long.MaxValue;
            if (caster.HasFreeHand && caster.BlindFrames == 0)
            {
                for (int i = 0; i < PoolCaps.LooseItems; i++)
                {
                    LooseItemSlot item = MatchStateAccess.GetLooseItem(ref state, i);
                    if (!item.IsActive || item.HolderSlot != LooseItemSlot.NoHolder || item.InFlight)
                    {
                        continue;
                    }

                    long dSq = DistSqScaled(caster.PosX, caster.PosZ, item.PosX, item.PosZ);
                    if (dSq > rangeSq)
                    {
                        continue;
                    }

                    if (dSq < catchDistSq || (dSq == catchDistSq && i < catchSlot))
                    {
                        catchDistSq = dSq;
                        catchSlot = i;
                    }
                }
            }

            if (catchSlot >= 0)
            {
                LooseItemSlot caught = MatchStateAccess.GetLooseItem(ref state, catchSlot);
                caught.HolderSlot = (byte)casterIndex;
                caught.InFlight = false;
                caught.FlightFrames = 0;
                caught.VelX = Fix.Zero;
                caught.VelY = Fix.Zero;
                caught.VelZ = Fix.Zero;
                caught.PosX = caster.PosX;
                caught.PosY = caster.PosY + PhysicsConstants.ItemRadius;
                caught.PosZ = caster.PosZ;
                MatchStateAccess.SetLooseItem(ref state, catchSlot, caught);
                if (!AssignToFreeHand(ref caster, (byte)catchSlot))
                {
                    caught.HolderSlot = LooseItemSlot.NoHolder;
                    MatchStateAccess.SetLooseItem(ref state, catchSlot, caught);
                    catchSlot = -1;
                }
            }

            // Snap remaining field items in range to the pickup ring (not half-gap).
            int pile = 0;
            for (int i = 0; i < PoolCaps.LooseItems; i++)
            {
                if (i == catchSlot)
                {
                    continue;
                }

                LooseItemSlot item = MatchStateAccess.GetLooseItem(ref state, i);
                if (!item.IsActive || item.HolderSlot != LooseItemSlot.NoHolder)
                {
                    continue;
                }

                if (DistSqScaled(caster.PosX, caster.PosZ, item.PosX, item.PosZ) > rangeSq)
                {
                    continue;
                }

                // Offset slightly along +X by pile index so items don't occupy one cell.
                item.PosX = caster.PosX + (PhysicsConstants.PickupRange / 2)
                    + (Fix.FromInt(pile) / 8);
                item.PosY = caster.PosY + PhysicsConstants.ItemRadius;
                item.PosZ = caster.PosZ;
                item.VelX = Fix.Zero;
                item.VelY = Fix.Zero;
                item.VelZ = Fix.Zero;
                item.InFlight = false;
                item.FlightFrames = 0;
                MatchStateAccess.SetLooseItem(ref state, i, item);
                pile++;
            }
        }

        /// <summary>Assign item to right hand first, then left if dual-hold.</summary>
        private static bool AssignToFreeHand(ref ActorSlot actor, byte itemSlot)
        {
            if (actor.HeldItemSlot == ActorSlot.NoHeldItem)
            {
                actor.HeldItemSlot = itemSlot;
                return true;
            }

            if (actor.IsDualHold && actor.HeldItemSlotLeft == ActorSlot.NoHeldItem)
            {
                actor.HeldItemSlotLeft = itemSlot;
                return true;
            }

            return false;
        }

        private static void ClearHand(ref ActorSlot actor, bool leftHand)
        {
            if (leftHand)
            {
                actor.HeldItemSlotLeft = ActorSlot.NoHeldItem;
            }
            else
            {
                actor.HeldItemSlot = ActorSlot.NoHeldItem;
            }
        }

        private static void DropHeldToField(ref MatchState state, int actorIndex)
        {
            ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, actorIndex);
            DropOneHandToField(ref state, ref actor, actorIndex, leftHand: false);
            DropOneHandToField(ref state, ref actor, actorIndex, leftHand: true);
        }

        private static void DropOneHandToField(
            ref MatchState state, ref ActorSlot actor, int actorIndex, bool leftHand)
        {
            int slot = leftHand ? actor.HeldItemSlotLeft : actor.HeldItemSlot;
            if (slot == ActorSlot.NoHeldItem || slot >= PoolCaps.LooseItems)
            {
                ClearHand(ref actor, leftHand);
                return;
            }

            LooseItemSlot item = MatchStateAccess.GetLooseItem(ref state, slot);
            if (!item.IsActive || item.HolderSlot != actorIndex)
            {
                ClearHand(ref actor, leftHand);
                return;
            }

            item.HolderSlot = LooseItemSlot.NoHolder;
            item.PosX = actor.PosX;
            item.PosY = actor.PosY + PhysicsConstants.ItemRadius;
            item.PosZ = actor.PosZ;
            // Slight +X offset for left-hand drop so dual strip doesn't stack cells.
            if (leftHand)
            {
                item.PosX += PhysicsConstants.ItemRadius;
            }

            item.VelX = Fix.Zero;
            item.VelY = Fix.Zero;
            item.VelZ = Fix.Zero;
            item.InFlight = false;
            item.FlightFrames = 0;
            MatchStateAccess.SetLooseItem(ref state, slot, item);
            ClearHand(ref actor, leftHand);
        }

        private static long DistSqScaled(Fix ax, Fix az, Fix bx, Fix bz)
        {
            long dx = (bx.Raw - ax.Raw) >> 8;
            long dz = (bz.Raw - az.Raw) >> 8;
            return (dx * dx) + (dz * dz);
        }
    }
}
