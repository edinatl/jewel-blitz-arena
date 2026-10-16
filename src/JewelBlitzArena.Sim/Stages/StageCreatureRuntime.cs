using JewelBlitzArena.Data;

namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Stage creatures on <see cref="MatchState.StageCreatures"/> (stages.md §3.2 Vein Guards).
    /// Pressworks Sorting: five guards @ 300 Life; gate opens at 4 destroyed or 90 s.
    /// </summary>
    public static class StageCreatureRuntime
    {
        public const byte FlagActive = 1 << 0;
        public const byte FlagBroken = 1 << 1;

        public const int KindMortar = 1;
        public const int KindFrostStaff = 2;
        public const int KindMallet = 3;
        public const int KindNet = 4;
        public const int KindFlame = 5;

        public const int GuardMaxLife = 300;
        public const int GuardsForGate = 4;
        public const int GuardCount = 5;

        public static readonly Fix HitRadius = Fix.FromInt(2);

        public static void InitializeMatch(ref MatchState state, StageGraph graph)
        {
            Clear(ref state);
            if (graph.StageId != SampleContentFactory.PressworksStageId)
            {
                return;
            }

            // Phase 0 Sorting is entered from StageRuntime.InitializeMatch — arm there too.
            ArmVeinGuards(ref state);
        }

        public static void OnPhaseEnter(ref MatchState state, StageGraph graph, int phaseIndex)
        {
            if (graph.StageId != SampleContentFactory.PressworksStageId)
            {
                Clear(ref state);
                return;
            }

            if (phaseIndex == 0)
            {
                ArmVeinGuards(ref state);
            }
            else
            {
                // Sorting over — clear remaining guards (gate already opened).
                Clear(ref state);
            }
        }

        public static int CountActive(ref MatchState state)
        {
            int n = 0;
            for (int i = 0; i < PoolCaps.StageCreatures; i++)
            {
                if ((MatchStateAccess.GetStageCreature(ref state, i).Flags & FlagActive) != 0)
                {
                    n++;
                }
            }

            return n;
        }

        public static int CountDestroyed(ref MatchState state)
        {
            int n = 0;
            for (int i = 0; i < PoolCaps.StageCreatures; i++)
            {
                GenericPoolSlot c = MatchStateAccess.GetStageCreature(ref state, i);
                if (c.TypeId == 0)
                {
                    continue;
                }

                if ((c.Flags & FlagBroken) != 0)
                {
                    n++;
                }
            }

            return n;
        }

        public static bool SortingGateReady(ref MatchState state, StageGraph graph)
        {
            if (graph.StageId != SampleContentFactory.PressworksStageId)
            {
                return false;
            }

            if (state.PhaseIndex != 0)
            {
                return false;
            }

            return CountDestroyed(ref state) >= GuardsForGate;
        }

        public static void ResolveAttackHits(
            ref MatchState state,
            KitRuntime[] kits,
            ref SimContactBuffer events)
        {
            if (CountActive(ref state) <= 0)
            {
                return;
            }

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
                Fix radiusSum = Fix.FromRaw(rangeRaw) + HitRadius;
                long rangeLimitSq = radiusSum.Raw * radiusSum.Raw;

                int best = -1;
                long bestDistSq = long.MaxValue;
                for (int c = 0; c < PoolCaps.StageCreatures; c++)
                {
                    GenericPoolSlot creature = MatchStateAccess.GetStageCreature(ref state, c);
                    if ((creature.Flags & FlagActive) == 0)
                    {
                        continue;
                    }

                    long dx = attacker.PosX.Raw - creature.PosX.Raw;
                    long dz = attacker.PosZ.Raw - creature.PosZ.Raw;
                    long distSq = (dx * dx) + (dz * dz);
                    if (distSq > rangeLimitSq)
                    {
                        continue;
                    }

                    if (distSq < bestDistSq || (distSq == bestDistSq && c < best))
                    {
                        bestDistSq = distSq;
                        best = c;
                    }
                }

                if (best < 0)
                {
                    continue;
                }

                GenericPoolSlot hit = MatchStateAccess.GetStageCreature(ref state, best);
                int damage = attacker.MoveDamage;
                if (damage < 1)
                {
                    damage = 1;
                }

                hit.Param0 -= damage;
                attacker.HitApplied = 1;
                events.Add(SimEventKind.StageCreatureHit, (byte)best, (byte)atk);

                if (hit.Param0 <= 0)
                {
                    hit.Param0 = 0;
                    hit.Flags = FlagBroken;
                    MatchStateAccess.SetStageCreature(ref state, best, hit);
                    events.Add(SimEventKind.StageCreatureBroken, (byte)best, (byte)hit.TypeId);
                }
                else
                {
                    MatchStateAccess.SetStageCreature(ref state, best, hit);
                }
            }
        }

        private static void ArmVeinGuards(ref MatchState state)
        {
            Clear(ref state);
            // Spread along the sorting hall (Ban-safe Fix ints).
            Arm(ref state, 0, KindMortar, Fix.FromInt(-6), Fix.FromInt(3));
            Arm(ref state, 1, KindFrostStaff, Fix.FromInt(-3), Fix.FromInt(-3));
            Arm(ref state, 2, KindMallet, Fix.FromInt(0), Fix.FromInt(4));
            Arm(ref state, 3, KindNet, Fix.FromInt(3), Fix.FromInt(-2));
            Arm(ref state, 4, KindFlame, Fix.FromInt(6), Fix.FromInt(2));
        }

        private static void Arm(ref MatchState state, int index, int kind, Fix x, Fix z)
        {
            if (index < 0 || index >= PoolCaps.StageCreatures)
            {
                return;
            }

            var slot = new GenericPoolSlot
            {
                PosX = x,
                PosY = Fix.Zero,
                PosZ = z,
                TypeId = kind,
                Param0 = GuardMaxLife,
                Flags = FlagActive,
                Reserved1 = (ushort)GuardMaxLife,
            };
            MatchStateAccess.SetStageCreature(ref state, index, slot);
        }

        private static void Clear(ref MatchState state)
        {
            for (int i = 0; i < PoolCaps.StageCreatures; i++)
            {
                MatchStateAccess.SetStageCreature(ref state, i, default);
            }
        }
    }
}
