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
            // StageRuntime.EnterPhase already called OnPhaseEnter; re-arm phase 0 from data.
            ArmFromPhase(ref state, graph, phaseIndex: 0);
        }

        public static void OnPhaseEnter(ref MatchState state, StageGraph graph, int phaseIndex)
        {
            ArmFromPhase(ref state, graph, phaseIndex);
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
                    OnBroken(ref state, hit.TypeId, hit.Reserved0, hit.PosX, hit.PosZ);
                }
                else
                {
                    MatchStateAccess.SetStageCreature(ref state, best, hit);
                }
            }
        }

        private static void OnBroken(ref MatchState state, int kind, int dropItemId, Fix x, Fix z)
        {
            int itemId = dropItemId > 0 ? dropItemId : ItemIdForKind(kind);
            if (itemId < 0)
            {
                return;
            }

            CrateSpawner.TrySpawnLooseItem(
                ref state, itemId, new FixVec3(x, Fix.Zero, z), stageOnly: true);
        }

        private static int ItemIdForKind(int kind) => kind switch
        {
            KindMortar => MatchRules.StageMortarItemId,
            KindFrostStaff => MatchRules.StageFrostStaffItemId,
            KindMallet => MatchRules.StageMalletItemId,
            KindNet => MatchRules.StageNetItemId,
            KindFlame => MatchRules.StageFlameItemId,
            _ => -1,
        };

        private static void ArmFromPhase(ref MatchState state, StageGraph graph, int phaseIndex)
        {
            Clear(ref state);
            if (!graph.TryGetPhase(phaseIndex, out StagePhaseDef phase))
            {
                return;
            }

            int count = 0;
            for (int i = 0; i < phase.Creatures.Length && count < PoolCaps.StageCreatures; i++)
            {
                StageCreatureDef c = phase.Creatures[i];
                if (c.KindId < 1)
                {
                    continue;
                }

                int life = c.MaxLife > 0 ? c.MaxLife : GuardMaxLife;
                int dropId = c.DropItemId > 0 ? c.DropItemId : ItemIdForKind(c.KindId);
                Arm(ref state, count, c.KindId, c.X, c.Z, life, dropId);
                count++;
            }
        }

        private static void Arm(
            ref MatchState state, int index, int kind, Fix x, Fix z, int maxLife, int dropItemId)
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
                Param0 = maxLife,
                Flags = FlagActive,
                // Drop item id in Reserved0 (stage weapons are small ints).
                Reserved0 = (byte)(dropItemId > 255 ? 255 : (dropItemId < 0 ? 0 : dropItemId)),
                Reserved1 = (ushort)(maxLife > 65535 ? 65535 : maxLife),
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
