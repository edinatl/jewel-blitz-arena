using JewelBlitzArena.Data;

namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Harvester anatomy on <see cref="MatchState.BossSubActors"/> (stages.md §3.1).
    /// Four legs @ 500 (repair 30 s), head @ 1200 (exposed after two legs broken),
    /// mountable tail @ 1000, Buck every 6 s while mounted, Cutters gravel via
    /// <see cref="ProjectileRuntime"/>.
    /// </summary>
    public static class BossPartRuntime
    {
        public const byte FlagActive = 1 << 0;
        public const byte FlagBroken = 1 << 1;
        public const byte FlagRepairing = 1 << 2;

        public const int KindLegFL = 1;
        public const int KindLegFR = 2;
        public const int KindLegBL = 3;
        public const int KindLegBR = 4;
        public const int KindHead = 5;
        public const int KindTail = 6;
        /// <summary>BossSub[6] Buck countdown controller (not a hittable part).</summary>
        public const int KindBuck = 7;
        /// <summary>BossSub[7] Cutters cadence controller (not a hittable part).</summary>
        public const int KindCutters = 8;

        public const int IndexTail = 5;
        public const int IndexBuck = 6;
        public const int IndexCutters = 7;

        public const int LegMaxLife = 500;
        public const int HeadMaxLife = 1200;
        public const int TailMaxLife = 1000;
        public const int LegRepairFrames = 30 * 60; // 30 s
        public const int LegsForHeadExpose = 2;

        public const int BuckPeriodFrames = 6 * 60; // 6 s
        public const int BuckTelegraphFrames = 40;
        public const int BuckDamage = 80;

        public static readonly Fix PartHitRadius = Fix.FromInt(2);
        public static readonly Fix MountRange = Fix.FromInt(3);
        public static readonly Fix BuckThrowSpeed = Fix.FromInt(4);
        public static readonly Fix MountHeight = Fix.One;

        public static void InitializeMatch(ref MatchState state, StageGraph graph)
        {
            Clear(ref state);
            if (graph.StageId != SampleContentFactory.HarvesterTerracesStageId)
            {
                return;
            }

            // Legs around the body; head forward (+X); tail aft (−X).
            ArmPart(ref state, 0, KindLegFL, Fix.FromInt(2), Fix.FromInt(2), LegMaxLife);
            ArmPart(ref state, 1, KindLegFR, Fix.FromInt(2), Fix.FromInt(-2), LegMaxLife);
            ArmPart(ref state, 2, KindLegBL, Fix.FromInt(-2), Fix.FromInt(2), LegMaxLife);
            ArmPart(ref state, 3, KindLegBR, Fix.FromInt(-2), Fix.FromInt(-2), LegMaxLife);
            ArmPart(ref state, 4, KindHead, Fix.FromInt(4), Fix.Zero, HeadMaxLife);
            ArmPart(ref state, 5, KindTail, Fix.FromInt(-4), Fix.Zero, TailMaxLife);
            // Buck controller (BossSub[6]) arms lazily on first mount so idle Harvester
            // corpus hashes stay bit-identical when nobody climbs.
        }

        public static int CountBrokenLegs(ref MatchState state)
        {
            int n = 0;
            for (int i = 0; i < PoolCaps.BossSubActors; i++)
            {
                GenericPoolSlot part = MatchStateAccess.GetBossSub(ref state, i);
                if (!IsLeg(part.TypeId))
                {
                    continue;
                }

                if ((part.Flags & FlagBroken) != 0)
                {
                    n++;
                }
            }

            return n;
        }

        public static bool IsHeadExposed(ref MatchState state) =>
            CountBrokenLegs(ref state) >= LegsForHeadExpose;

        public static bool AnyMounted(ref MatchState state)
        {
            for (int a = 0; a < state.ActorCount; a++)
            {
                ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, a);
                if (actor.IsAlive && !actor.IsEliminated && actor.IsMounted)
                {
                    return true;
                }
            }

            return false;
        }

        public static int CountArmed(ref MatchState state)
        {
            int n = 0;
            for (int i = 0; i < PoolCaps.BossSubActors; i++)
            {
                GenericPoolSlot part = MatchStateAccess.GetBossSub(ref state, i);
                if (part.TypeId == KindBuck)
                {
                    continue;
                }

                if ((part.Flags & FlagActive) != 0)
                {
                    n++;
                }
            }

            return n;
        }

        /// <summary>
        /// Action near a broken/repairing leg mounts onto the tail (stages.md burst route).
        /// Blinded fighters cannot mount (same rule as item pickup).
        /// </summary>
        public static void ProcessMountInput(
            ref MatchState state,
            int actorIndex,
            in InputFrame input,
            ref SimContactBuffer events)
        {
            if (actorIndex < 0 || actorIndex >= state.ActorCount)
            {
                return;
            }

            ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, actorIndex);
            if (!actor.IsAlive || actor.IsEliminated || actor.IsMounted)
            {
                return;
            }

            if (!input.Action || actor.BlindFrames > 0 || !actor.CanAct)
            {
                return;
            }

            if (CountBrokenLegs(ref state) < 1)
            {
                return;
            }

            if (!TryFindMountLeg(ref state, actor.PosX, actor.PosZ, out _))
            {
                return;
            }

            GenericPoolSlot tail = MatchStateAccess.GetBossSub(ref state, IndexTail);
            if (tail.TypeId != KindTail || (tail.Flags & FlagBroken) != 0)
            {
                return;
            }

            actor.IsMounted = true;
            actor.PosX = tail.PosX;
            actor.PosY = MountHeight;
            actor.PosZ = tail.PosZ;
            actor.VelX = Fix.Zero;
            actor.VelY = Fix.Zero;
            actor.VelZ = Fix.Zero;
            events.Add(SimEventKind.BossMounted, (byte)actorIndex, 0);
        }

        /// <summary>Prefer boss parts over crates for Attack hit priority.</summary>
        public static void ResolveAttackHits(
            ref MatchState state,
            KitRuntime[] kits,
            ref SimContactBuffer events)
        {
            bool any = false;
            for (int i = 0; i < PoolCaps.BossSubActors; i++)
            {
                GenericPoolSlot part = MatchStateAccess.GetBossSub(ref state, i);
                if (IsHarvesterPart(part.TypeId))
                {
                    any = true;
                    break;
                }
            }

            if (!any)
            {
                return;
            }

            bool headExposed = IsHeadExposed(ref state);

            for (int atk = 0; atk < state.ActorCount; atk++)
            {
                ref ActorSlot attacker = ref MatchStateAccess.Actor(ref state, atk);
                if (!attacker.IsAlive || attacker.IsEliminated)
                {
                    continue;
                }

                if (attacker.ActionPhase != (byte)ActionPhase.Active || attacker.HitApplied != 0)
                {
                    continue;
                }

                if (attacker.Action != (byte)ActorAction.Attack
                    && attacker.Action != (byte)ActorAction.Strong)
                {
                    continue;
                }

                KitRuntime kit = kits[atk];
                long rangeRaw = MatchRules.HitRangeRaw(kit.ReachRating);
                Fix radiusSum = Fix.FromRaw(rangeRaw) + PartHitRadius;
                long rangeLimitSq = radiusSum.Raw * radiusSum.Raw;
                bool mounted = attacker.IsMounted;

                int best = -1;
                long bestDistSq = long.MaxValue;
                for (int p = 0; p < PoolCaps.BossSubActors; p++)
                {
                    GenericPoolSlot part = MatchStateAccess.GetBossSub(ref state, p);
                    if (!IsHittable(in part, headExposed, mounted))
                    {
                        continue;
                    }

                    long dx = attacker.PosX.Raw - part.PosX.Raw;
                    long dz = attacker.PosZ.Raw - part.PosZ.Raw;
                    long distSq = (dx * dx) + (dz * dz);
                    if (distSq > rangeLimitSq)
                    {
                        continue;
                    }

                    if (distSq < bestDistSq || (distSq == bestDistSq && p < best))
                    {
                        bestDistSq = distSq;
                        best = p;
                    }
                }

                if (best < 0)
                {
                    continue;
                }

                GenericPoolSlot hit = MatchStateAccess.GetBossSub(ref state, best);
                int damage = attacker.MoveDamage;
                if (damage < 1)
                {
                    damage = 1;
                }

                hit.Param0 -= damage;
                attacker.HitApplied = 1;
                events.Add(SimEventKind.BossPartHit, (byte)best, (byte)atk);

                if (hit.Param0 <= 0)
                {
                    hit.Param0 = 0;
                    BreakPart(ref hit);
                    MatchStateAccess.SetBossSub(ref state, best, hit);
                    events.Add(SimEventKind.BossPartBroken, (byte)best, (byte)hit.TypeId);
                }
                else
                {
                    MatchStateAccess.SetBossSub(ref state, best, hit);
                }
            }
        }

        /// <summary>
        /// Leg repair, mount pin / auto-dismount, and Buck cadence while anyone is mounted.
        /// </summary>
        public static void Tick(
            ref MatchState state,
            InputFrame i0,
            InputFrame i1,
            InputFrame i2,
            InputFrame i3,
            ref SimContactBuffer events)
        {
            TickLegRepair(ref state);
            TickMounted(ref state);
            TickBuck(ref state, i0, i1, i2, i3, ref events);
        }

        public static bool IsLeg(int typeId) =>
            typeId == KindLegFL || typeId == KindLegFR
            || typeId == KindLegBL || typeId == KindLegBR;

        public static bool IsHarvesterPart(int typeId) =>
            IsLeg(typeId) || typeId == KindHead || typeId == KindTail;

        public static bool IsBuckTelegraphing(ref MatchState state)
        {
            GenericPoolSlot buck = MatchStateAccess.GetBossSub(ref state, IndexBuck);
            if (buck.TypeId != KindBuck || buck.Param0 <= 0)
            {
                return false;
            }

            return buck.Param0 <= BuckTelegraphFrames;
        }

        private static void TickLegRepair(ref MatchState state)
        {
            for (int i = 0; i < PoolCaps.BossSubActors; i++)
            {
                GenericPoolSlot part = MatchStateAccess.GetBossSub(ref state, i);
                if (part.TypeId == 0 || part.TypeId == KindBuck)
                {
                    continue;
                }

                if ((part.Flags & FlagRepairing) == 0)
                {
                    continue;
                }

                if (!IsLeg(part.TypeId))
                {
                    continue;
                }

                if (part.Param0 > 0)
                {
                    part.Param0--;
                }

                if (part.Param0 <= 0)
                {
                    // Repair complete.
                    part.Param0 = LegMaxLife;
                    part.Flags = FlagActive;
                    part.Reserved1 = (ushort)LegMaxLife;
                }

                MatchStateAccess.SetBossSub(ref state, i, part);
            }
        }

        private static void TickMounted(ref MatchState state)
        {
            if (!AnyMounted(ref state))
            {
                return;
            }

            int broken = CountBrokenLegs(ref state);
            GenericPoolSlot tail = MatchStateAccess.GetBossSub(ref state, IndexTail);
            bool tailOk = tail.TypeId == KindTail && (tail.Flags & FlagBroken) == 0;

            for (int a = 0; a < state.ActorCount; a++)
            {
                ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, a);
                if (!actor.IsMounted)
                {
                    continue;
                }

                if (!actor.IsAlive || actor.IsEliminated || broken < 1 || !tailOk)
                {
                    ClearMount(ref actor);
                    continue;
                }

                actor.PosX = tail.PosX;
                actor.PosY = MountHeight;
                actor.PosZ = tail.PosZ;
                actor.VelX = Fix.Zero;
                actor.VelY = Fix.Zero;
                actor.VelZ = Fix.Zero;
            }
        }

        private static void TickBuck(
            ref MatchState state,
            InputFrame i0,
            InputFrame i1,
            InputFrame i2,
            InputFrame i3,
            ref SimContactBuffer events)
        {
            if (!AnyMounted(ref state))
            {
                // Clear lazy Buck controller so unmatched attempts leave pool empty.
                GenericPoolSlot idle = MatchStateAccess.GetBossSub(ref state, IndexBuck);
                if (idle.TypeId == KindBuck)
                {
                    MatchStateAccess.SetBossSub(ref state, IndexBuck, default);
                }

                return;
            }

            GenericPoolSlot buck = MatchStateAccess.GetBossSub(ref state, IndexBuck);
            if (buck.TypeId != KindBuck)
            {
                ArmBuckController(ref state);
                buck = MatchStateAccess.GetBossSub(ref state, IndexBuck);
                buck.Param0 = BuckPeriodFrames;
            }

            if (buck.Param0 <= 0)
            {
                buck.Param0 = BuckPeriodFrames;
            }

            buck.Param0--;
            if (buck.Param0 > 0)
            {
                MatchStateAccess.SetBossSub(ref state, IndexBuck, buck);
                return;
            }

            // Fire Buck, then restart cadence while still mounted.
            FireBuck(ref state, i0, i1, i2, i3, ref events);
            buck.Param0 = AnyMounted(ref state) ? BuckPeriodFrames : 0;
            if (buck.Param0 <= 0)
            {
                MatchStateAccess.SetBossSub(ref state, IndexBuck, default);
            }
            else
            {
                buck.TypeId = KindBuck;
                MatchStateAccess.SetBossSub(ref state, IndexBuck, buck);
            }
        }

        private static void FireBuck(
            ref MatchState state,
            InputFrame i0,
            InputFrame i1,
            InputFrame i2,
            InputFrame i3,
            ref SimContactBuffer events)
        {
            for (int a = 0; a < state.ActorCount; a++)
            {
                ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, a);
                if (!actor.IsAlive || actor.IsEliminated || !actor.IsMounted)
                {
                    continue;
                }

                bool hanging = InputFor(a, i0, i1, i2, i3).Action;
                if (hanging)
                {
                    events.Add(SimEventKind.BossBuck, (byte)a, 1);
                    continue;
                }

                actor.Life -= BuckDamage;
                if (actor.Life < 0)
                {
                    actor.Life = 0;
                }

                actor.StunFrames = MatchRules.HitStunBase;
                actor.KnockdownFrames = MatchRules.KnockdownMaxFrames;
                actor.Action = (byte)ActorAction.Knockdown;
                actor.ActionTimer = MatchRules.KnockdownMaxFrames;
                actor.ActionPhase = (byte)ActionPhase.Recovery;
                ClearMount(ref actor);
                ApplyBuckThrow(ref actor);
                events.Add(SimEventKind.BossBuck, (byte)a, 0);
            }
        }

        private static InputFrame InputFor(
            int index, InputFrame i0, InputFrame i1, InputFrame i2, InputFrame i3)
        {
            switch (index)
            {
                case 0: return i0;
                case 1: return i1;
                case 2: return i2;
                case 3: return i3;
                default: return InputFrame.Neutral;
            }
        }

        private static void ApplyBuckThrow(ref ActorSlot actor)
        {
            // Throw outward from body origin on dominant axis (Ban-safe; no sqrt).
            long ax = actor.PosX.Raw;
            long az = actor.PosZ.Raw;
            if (ax == 0 && az == 0)
            {
                actor.VelX = BuckThrowSpeed;
                actor.VelZ = Fix.Zero;
                return;
            }

            if (AbsRaw(ax) >= AbsRaw(az))
            {
                actor.VelX = ax >= 0 ? BuckThrowSpeed : -BuckThrowSpeed;
                actor.VelZ = Fix.Zero;
            }
            else
            {
                actor.VelX = Fix.Zero;
                actor.VelZ = az >= 0 ? BuckThrowSpeed : -BuckThrowSpeed;
            }
        }

        private static void ClearMount(ref ActorSlot actor)
        {
            actor.IsMounted = false;
            if (actor.PosY.Raw > 0)
            {
                actor.PosY = Fix.Zero;
            }
        }

        private static bool TryFindMountLeg(
            ref MatchState state, Fix x, Fix z, out int legIndex)
        {
            legIndex = -1;
            long rangeLimitSq = MountRange.Raw * MountRange.Raw;
            long bestDistSq = long.MaxValue;
            for (int i = 0; i < PoolCaps.BossSubActors; i++)
            {
                GenericPoolSlot part = MatchStateAccess.GetBossSub(ref state, i);
                if (!IsLeg(part.TypeId))
                {
                    continue;
                }

                if ((part.Flags & FlagBroken) == 0 && (part.Flags & FlagRepairing) == 0)
                {
                    continue;
                }

                long dx = x.Raw - part.PosX.Raw;
                long dz = z.Raw - part.PosZ.Raw;
                long distSq = (dx * dx) + (dz * dz);
                if (distSq > rangeLimitSq)
                {
                    continue;
                }

                if (distSq < bestDistSq || (distSq == bestDistSq && i < legIndex))
                {
                    bestDistSq = distSq;
                    legIndex = i;
                }
            }

            return legIndex >= 0;
        }

        private static bool IsHittable(in GenericPoolSlot part, bool headExposed, bool attackerMounted)
        {
            if (!IsHarvesterPart(part.TypeId))
            {
                return false;
            }

            if ((part.Flags & FlagBroken) != 0 || (part.Flags & FlagRepairing) != 0)
            {
                return false;
            }

            if ((part.Flags & FlagActive) == 0)
            {
                return false;
            }

            if (part.TypeId == KindHead && !headExposed)
            {
                return false;
            }

            if (part.TypeId == KindTail && !attackerMounted)
            {
                return false;
            }

            return true;
        }

        private static void BreakPart(ref GenericPoolSlot part)
        {
            if (IsLeg(part.TypeId))
            {
                part.Flags = (byte)(FlagBroken | FlagRepairing);
                part.Param0 = LegRepairFrames;
            }
            else
            {
                // Head / tail: permanent break for v1 (no repair).
                part.Flags = FlagBroken;
                part.Param0 = 0;
            }
        }

        private static void ArmPart(
            ref MatchState state, int index, int kind, Fix x, Fix z, int maxLife)
        {
            if (index < 0 || index >= PoolCaps.BossSubActors)
            {
                return;
            }

            var slot = new GenericPoolSlot
            {
                PosX = x,
                PosY = Fix.Zero,
                PosZ = z,
                TypeId = kind,
                Param0 = maxLife,
                Flags = FlagActive,
                Reserved1 = (ushort)(maxLife > 65535 ? 65535 : maxLife),
            };
            MatchStateAccess.SetBossSub(ref state, index, slot);
        }

        private static void ArmBuckController(ref MatchState state)
        {
            var slot = new GenericPoolSlot
            {
                TypeId = KindBuck,
                Param0 = 0,
                Flags = 0,
            };
            MatchStateAccess.SetBossSub(ref state, IndexBuck, slot);
        }

        private static void Clear(ref MatchState state)
        {
            for (int i = 0; i < PoolCaps.BossSubActors; i++)
            {
                MatchStateAccess.SetBossSub(ref state, i, default);
            }
        }

        private static long AbsRaw(long v) => v < 0 ? -v : v;
    }
}
