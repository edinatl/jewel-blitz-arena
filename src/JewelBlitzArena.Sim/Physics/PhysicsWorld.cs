namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Minimal fixed-point dynamics: gravity, ground probe, arena AABB, sphere separation.
    /// Sequential impulses (4 iters). Cosmetic physics stays in presentation.
    /// </summary>
    public static class PhysicsWorld
    {
        public static void IntegrateSphere(
            ref Fix posX, ref Fix posY, ref Fix posZ,
            ref Fix velX, ref Fix velY, ref Fix velZ,
            Fix radius,
            bool applyGravity)
        {
            if (applyGravity)
            {
                // Semi-implicit Euler: v += g*dt; p += v*dt. dt = 1/60 → divide by TickRate.
                velY += PhysicsConstants.Gravity / Simulation.TickRateHz;
            }

            posX += velX;
            posY += velY;
            posZ += velZ;

            // Ground probe (y = 0 plane).
            if (posY < radius)
            {
                posY = radius;
                if (velY.Raw < 0)
                {
                    velY = -(velY * PhysicsConstants.BounceRestitution);
                    if (velY.Raw < Fix.OneRaw / 64)
                    {
                        velY = Fix.Zero;
                    }

                    velX = velX * PhysicsConstants.GroundFriction;
                    velZ = velZ * PhysicsConstants.GroundFriction;
                }
            }

            ClampToArena(ref posX, ref posZ, ref velX, ref velZ, radius);
        }

        public static void ClampToArena(ref Fix posX, ref Fix posZ, ref Fix velX, ref Fix velZ, Fix radius)
        {
            Fix min = -PhysicsConstants.ArenaHalfExtent + radius;
            Fix max = PhysicsConstants.ArenaHalfExtent - radius;

            if (posX < min)
            {
                posX = min;
                if (velX.Raw < 0)
                {
                    velX = Fix.Zero;
                }
            }
            else if (posX > max)
            {
                posX = max;
                if (velX.Raw > 0)
                {
                    velX = Fix.Zero;
                }
            }

            if (posZ < min)
            {
                posZ = min;
                if (velZ.Raw < 0)
                {
                    velZ = Fix.Zero;
                }
            }
            else if (posZ > max)
            {
                posZ = max;
                if (velZ.Raw > 0)
                {
                    velZ = Fix.Zero;
                }
            }
        }

        /// <summary>Horizontal distance squared between two XZ positions.</summary>
        public static long DistSqXZ(Fix ax, Fix az, Fix bx, Fix bz)
        {
            long dx = ax.Raw - bx.Raw;
            long dz = az.Raw - bz.Raw;
            return dx * dx + dz * dz;
        }

        public static bool OverlapsXZ(Fix ax, Fix az, Fix bx, Fix bz, Fix radiusSum)
        {
            long r = radiusSum.Raw;
            return DistSqXZ(ax, az, bx, bz) <= r * r;
        }

        /// <summary>
        /// Push two circles apart on XZ equally (canonical: lower index is A).
        /// </summary>
        public static void SeparateXZ(
            ref Fix aX, ref Fix aZ, ref Fix bX, ref Fix bZ,
            Fix radiusSum)
        {
            long dx = bX.Raw - aX.Raw;
            long dz = bZ.Raw - aZ.Raw;
            long distSq = dx * dx + dz * dz;
            long minDist = radiusSum.Raw;
            if (distSq >= minDist * minDist && distSq != 0)
            {
                return;
            }

            if (distSq == 0)
            {
                // Degenerate: nudge B along +X by radiusSum.
                bX += radiusSum;
                return;
            }

            // Approximate push using integer ratios without sqrt: push by (minDist - approxDist)/2.
            // Use a coarse inv-sqrt via integer Newton on Q48.16 later; for S2, axis-aligned split.
            long absDx = dx < 0 ? -dx : dx;
            long absDz = dz < 0 ? -dz : dz;
            if (absDx >= absDz)
            {
                long push = (minDist - absDx) / 2;
                if (push < 0)
                {
                    push = 0;
                }

                if (dx >= 0)
                {
                    aX = new Fix(aX.Raw - push);
                    bX = new Fix(bX.Raw + push);
                }
                else
                {
                    aX = new Fix(aX.Raw + push);
                    bX = new Fix(bX.Raw - push);
                }
            }
            else
            {
                long push = (minDist - absDz) / 2;
                if (push < 0)
                {
                    push = 0;
                }

                if (dz >= 0)
                {
                    aZ = new Fix(aZ.Raw - push);
                    bZ = new Fix(bZ.Raw + push);
                }
                else
                {
                    aZ = new Fix(aZ.Raw + push);
                    bZ = new Fix(bZ.Raw - push);
                }
            }
        }

        public static void StepActorsJewelsItems(ref MatchState state, ref SimContactBuffer contacts)
        {
            // Integrate jewels.
            for (int i = 0; i < PoolCaps.Jewels; i++)
            {
                JewelSlot j = MatchStateAccess.GetJewel(ref state, i);
                if (!j.IsOnField)
                {
                    continue;
                }

                IntegrateSphere(
                    ref j.PosX, ref j.PosY, ref j.PosZ,
                    ref j.VelX, ref j.VelY, ref j.VelZ,
                    PhysicsConstants.JewelRadius,
                    applyGravity: true);

                if (j.PickupDelayFrames > 0)
                {
                    j.PickupDelayFrames--;
                }

                MatchStateAccess.SetJewel(ref state, i, j);
            }

            // Integrate loose items (field + in-flight).
            for (int i = 0; i < PoolCaps.LooseItems; i++)
            {
                LooseItemSlot item = MatchStateAccess.GetLooseItem(ref state, i);
                if (!item.IsActive || item.HolderSlot != LooseItemSlot.NoHolder)
                {
                    continue;
                }

                IntegrateSphere(
                    ref item.PosX, ref item.PosY, ref item.PosZ,
                    ref item.VelX, ref item.VelY, ref item.VelZ,
                    PhysicsConstants.ItemRadius,
                    applyGravity: true);

                if (item.InFlight)
                {
                    item.FlightFrames++;
                    // Landed: near-zero vertical vel on ground.
                    if (item.PosY <= PhysicsConstants.ItemRadius
                        && item.VelY.Raw >= -Fix.OneRaw / 32
                        && item.VelY.Raw <= Fix.OneRaw / 32)
                    {
                        item.InFlight = false;
                        item.FlightFrames = 0;
                        item.VelX = Fix.Zero;
                        item.VelZ = Fix.Zero;
                        contacts.Add(SimEventKind.ItemLanded, (byte)i, 0);
                    }
                }

                MatchStateAccess.SetLooseItem(ref state, i, item);
            }

            // Actor ground clamp + arena (horizontal motion already applied from input).
            for (int a = 0; a < state.ActorCount; a++)
            {
                ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, a);
                if (!actor.IsAlive)
                {
                    continue;
                }

                if (actor.PosY < Fix.Zero)
                {
                    actor.PosY = Fix.Zero;
                    actor.VelY = Fix.Zero;
                }

                ClampToArena(
                    ref actor.PosX, ref actor.PosZ, ref actor.VelX, ref actor.VelZ,
                    PhysicsConstants.ActorRadius);
            }

            // Actor–actor separation + overlap events (canonical order).
            for (int iter = 0; iter < PhysicsConstants.SolverIterations; iter++)
            {
                for (int a = 0; a < state.ActorCount; a++)
                {
                    ref ActorSlot aa = ref MatchStateAccess.Actor(ref state, a);
                    if (!aa.IsAlive)
                    {
                        continue;
                    }

                    for (int b = a + 1; b < state.ActorCount; b++)
                    {
                        ref ActorSlot bb = ref MatchStateAccess.Actor(ref state, b);
                        if (!bb.IsAlive)
                        {
                            continue;
                        }

                        Fix sum = PhysicsConstants.ActorRadius * 2;
                        if (OverlapsXZ(aa.PosX, aa.PosZ, bb.PosX, bb.PosZ, sum))
                        {
                            SeparateXZ(ref aa.PosX, ref aa.PosZ, ref bb.PosX, ref bb.PosZ, sum);
                            if (iter == 0)
                            {
                                contacts.Add(SimEventKind.ActorActorOverlap, (byte)a, (byte)b);
                            }
                        }
                    }
                }
            }
        }
    }
}
