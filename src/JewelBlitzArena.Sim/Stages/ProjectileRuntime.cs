using JewelBlitzArena.Data;

namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Stage-owned projectiles on <see cref="MatchState.Projectiles"/>.
    /// v1: Harvester Cutters gravel-shot (stages.md §3.1) while any leg is broken.
    /// </summary>
    public static class ProjectileRuntime
    {
        public const int SoftActiveCap = 4;
        public const int GravelDamage = 80;
        public const int GravelTtlFrames = 120;
        public const int CuttersPeriodFrames = 90; // 1.5 s

        public static readonly Fix GravelSpeed = Fix.FromInt(8);
        public static readonly Fix HitRadius =
            PhysicsConstants.ActorRadius + (Fix.One / 4);

        public static void InitializeMatch(ref MatchState state)
        {
            ClearAll(ref state);
        }

        public static int CountActive(ref MatchState state)
        {
            int n = 0;
            for (int i = 0; i < PoolCaps.Projectiles; i++)
            {
                if (MatchStateAccess.GetProjectile(ref state, i).IsActive)
                {
                    n++;
                }
            }

            return n;
        }

        /// <summary>
        /// Integrate active bolts (no gravity), TTL despawn, and XZ hits.
        /// Call after thrown-item ticks.
        /// </summary>
        public static void Tick(ref MatchState state, ref SimContactBuffer events)
        {
            for (int i = 0; i < PoolCaps.Projectiles; i++)
            {
                ProjectileSlot p = MatchStateAccess.GetProjectile(ref state, i);
                if (!p.IsActive)
                {
                    continue;
                }

                p.PosX += p.VelX;
                p.PosY += p.VelY;
                p.PosZ += p.VelZ;
                if (p.Reserved > 0)
                {
                    p.Reserved--;
                }

                if (p.Reserved == 0)
                {
                    MatchStateAccess.SetProjectile(ref state, i, default);
                    continue;
                }

                if (TryHitActor(ref state, i, ref p, ref events))
                {
                    continue;
                }

                MatchStateAccess.SetProjectile(ref state, i, p);
            }

            state.ActiveProjectileCount = CountActive(ref state);
        }

        /// <summary>
        /// Lazy Cutters cadence on BossSub[7]: fire while any leg is broken.
        /// Prefers mounted targets. Idle Harvester (no broken legs) leaves pool empty.
        /// </summary>
        public static void TickCuttersCadence(ref MatchState state, StageGraph graph)
        {
            if (graph.StageId != SampleContentFactory.HarvesterTerracesStageId)
            {
                return;
            }

            if (BossPartRuntime.CountBrokenLegs(ref state) < 1)
            {
                GenericPoolSlot idle = MatchStateAccess.GetBossSub(ref state, BossPartRuntime.IndexCutters);
                if (idle.TypeId == BossPartRuntime.KindCutters)
                {
                    MatchStateAccess.SetBossSub(ref state, BossPartRuntime.IndexCutters, default);
                }

                return;
            }

            GenericPoolSlot cut = MatchStateAccess.GetBossSub(ref state, BossPartRuntime.IndexCutters);
            if (cut.TypeId != BossPartRuntime.KindCutters)
            {
                cut = new GenericPoolSlot
                {
                    TypeId = BossPartRuntime.KindCutters,
                    Param0 = CuttersPeriodFrames,
                };
            }

            if (cut.Param0 <= 0)
            {
                cut.Param0 = CuttersPeriodFrames;
            }

            cut.Param0--;
            if (cut.Param0 > 0)
            {
                MatchStateAccess.SetBossSub(ref state, BossPartRuntime.IndexCutters, cut);
                return;
            }

            TrySpawnGravel(ref state);
            cut.Param0 = CuttersPeriodFrames;
            cut.TypeId = BossPartRuntime.KindCutters;
            MatchStateAccess.SetBossSub(ref state, BossPartRuntime.IndexCutters, cut);
        }

        public static bool TrySpawnGravel(ref MatchState state)
        {
            if (CountActive(ref state) >= SoftActiveCap)
            {
                return false;
            }

            Fix originX = Fix.Zero;
            Fix originY = BossPartRuntime.MountHeight;
            Fix originZ = Fix.Zero;
            int target = FindCuttersTarget(ref state, originX, originZ);
            if (target < 0)
            {
                return false;
            }

            int slot = FindFreeSlot(ref state);
            if (slot < 0)
            {
                return false;
            }

            ref ActorSlot aim = ref MatchStateAccess.Actor(ref state, target);
            Fix velX;
            Fix velZ;
            DominantAxisToward(aim.PosX - originX, aim.PosZ - originZ, out velX, out velZ);

            var proj = new ProjectileSlot
            {
                PosX = originX,
                PosY = originY,
                PosZ = originZ,
                VelX = velX,
                VelY = Fix.Zero,
                VelZ = velZ,
                Damage = GravelDamage,
                OwnerSlot = ProjectileSlot.NoOwner,
                Flags = ProjectileSlot.FlagActive,
                Reserved = (ushort)GravelTtlFrames,
            };
            MatchStateAccess.SetProjectile(ref state, slot, proj);
            state.ActiveProjectileCount = CountActive(ref state);
            return true;
        }

        /// <summary>Mounted-first nearest living actor; lowest slot wins ties. −1 if none.</summary>
        public static int FindCuttersTarget(ref MatchState state, Fix originX, Fix originZ)
        {
            int best = -1;
            long bestDistSq = long.MaxValue;
            for (int i = 0; i < state.ActorCount; i++)
            {
                ActorSlot other = MatchStateAccess.Actor(ref state, i);
                if (!other.IsAlive || other.IsEliminated || !other.IsMounted)
                {
                    continue;
                }

                long dx = other.PosX.Raw - originX.Raw;
                long dz = other.PosZ.Raw - originZ.Raw;
                long distSq = (dx * dx) + (dz * dz);
                if (distSq < bestDistSq || (distSq == bestDistSq && i < best))
                {
                    bestDistSq = distSq;
                    best = i;
                }
            }

            if (best >= 0)
            {
                return best;
            }

            return BossAiRuntime.FindNearestLivingActor(ref state, originX, originZ);
        }

        private static bool TryHitActor(
            ref MatchState state,
            int projIndex,
            ref ProjectileSlot p,
            ref SimContactBuffer events)
        {
            for (int a = 0; a < state.ActorCount; a++)
            {
                ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, a);
                if (!actor.IsAlive || actor.IsEliminated || actor.InvulnFrames > 0)
                {
                    continue;
                }

                if (!PhysicsWorld.OverlapsXZ(p.PosX, p.PosZ, actor.PosX, actor.PosZ, HitRadius))
                {
                    continue;
                }

                if (actor.DodgeArmed > 0)
                {
                    // Dodgeable gravel (combat-and-match): consume armed dodge, clear bolt.
                    actor.DodgeArmed = 0;
                    MatchStateAccess.SetProjectile(ref state, projIndex, default);
                    return true;
                }

                actor.Life -= p.Damage;
                if (actor.Life < 0)
                {
                    actor.Life = 0;
                }

                actor.StunFrames = MatchRules.HitStunBase;
                events.Add(SimEventKind.ProjectileHitActor, (byte)projIndex, (byte)a);
                MatchStateAccess.SetProjectile(ref state, projIndex, default);
                return true;
            }

            return false;
        }

        private static int FindFreeSlot(ref MatchState state)
        {
            for (int i = 0; i < PoolCaps.Projectiles; i++)
            {
                if (!MatchStateAccess.GetProjectile(ref state, i).IsActive)
                {
                    return i;
                }
            }

            return -1;
        }

        private static void DominantAxisToward(Fix dx, Fix dz, out Fix velX, out Fix velZ)
        {
            long ax = dx.Raw;
            long az = dz.Raw;
            if (ax == 0 && az == 0)
            {
                velX = GravelSpeed;
                velZ = Fix.Zero;
                return;
            }

            long absX = ax < 0 ? -ax : ax;
            long absZ = az < 0 ? -az : az;
            if (absX >= absZ)
            {
                velX = ax >= 0 ? GravelSpeed : -GravelSpeed;
                velZ = Fix.Zero;
            }
            else
            {
                velX = Fix.Zero;
                velZ = az >= 0 ? GravelSpeed : -GravelSpeed;
            }
        }

        private static void ClearAll(ref MatchState state)
        {
            for (int i = 0; i < PoolCaps.Projectiles; i++)
            {
                MatchStateAccess.SetProjectile(ref state, i, default);
            }

            state.ActiveProjectileCount = 0;
        }
    }
}
