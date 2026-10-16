using JewelBlitzArena.Data;

namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Harvester anatomy on <see cref="MatchState.BossSubActors"/> (stages.md §3.1).
    /// Four legs @ 500 (repair 30 s), head @ 1200 (exposed after two legs broken),
    /// tail @ 1000. Cutters / mount / Buck deferred.
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

        public const int LegMaxLife = 500;
        public const int HeadMaxLife = 1200;
        public const int TailMaxLife = 1000;
        public const int LegRepairFrames = 30 * 60; // 30 s
        public const int LegsForHeadExpose = 2;

        public static readonly Fix PartHitRadius = Fix.FromInt(2);

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

        public static int CountArmed(ref MatchState state)
        {
            int n = 0;
            for (int i = 0; i < PoolCaps.BossSubActors; i++)
            {
                if ((MatchStateAccess.GetBossSub(ref state, i).Flags & FlagActive) != 0)
                {
                    n++;
                }
            }

            return n;
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
                if (MatchStateAccess.GetBossSub(ref state, i).TypeId != 0)
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

                int best = -1;
                long bestDistSq = long.MaxValue;
                for (int p = 0; p < PoolCaps.BossSubActors; p++)
                {
                    GenericPoolSlot part = MatchStateAccess.GetBossSub(ref state, p);
                    if (!IsHittable(in part, headExposed))
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

        /// <summary>Leg repair countdown; head/tail stay broken once destroyed.</summary>
        public static void Tick(ref MatchState state)
        {
            for (int i = 0; i < PoolCaps.BossSubActors; i++)
            {
                GenericPoolSlot part = MatchStateAccess.GetBossSub(ref state, i);
                if (part.TypeId == 0)
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

        public static bool IsLeg(int typeId) =>
            typeId == KindLegFL || typeId == KindLegFR
            || typeId == KindLegBL || typeId == KindLegBR;

        private static bool IsHittable(in GenericPoolSlot part, bool headExposed)
        {
            if (part.TypeId == 0)
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

        private static void Clear(ref MatchState state)
        {
            for (int i = 0; i < PoolCaps.BossSubActors; i++)
            {
                MatchStateAccess.SetBossSub(ref state, i, default);
            }
        }
    }
}
