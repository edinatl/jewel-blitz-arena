using JewelBlitzArena.Data;

namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Breakable stage props (crates) on the unused <see cref="MatchState.Crates"/> pool.
    /// One Attack hit opens (combat-and-match.md). Jewel vs item roll uses
    /// <see cref="StageGraph.JewelCrateChancePercent"/>. Boss anatomy is <see cref="BossPartRuntime"/>.
    /// Positions come from authored phase <c>crate_points</c>.
    /// </summary>
    public static class BreakableRuntime
    {
        public const byte FlagActive = 1 << 0;
        public const int KindJewelCrate = 1;

        /// <summary>XZ hit radius for Attack overlap vs crate center.</summary>
        public static readonly Fix CrateHitRadius = Fix.One;

        /// <summary>One-hit crates (Attack opens).</summary>
        public const int OneHitLife = 1;

        public static void InitializeMatch(ref MatchState state, StageGraph graph)
        {
            ArmPhaseCrates(ref state, graph);
        }

        /// <summary>Clear and re-arm crates for the current phase (match init + phase enter).</summary>
        public static void ArmPhaseCrates(ref MatchState state, StageGraph graph)
        {
            Clear(ref state);
            if (!graph.TryGetPhase(state.PhaseIndex, out StagePhaseDef phase))
            {
                return;
            }

            int n = phase.CratePoints.Length;
            if (n > PoolCaps.Crates)
            {
                n = PoolCaps.Crates;
            }

            for (int i = 0; i < n; i++)
            {
                StageCratePointDef pt = phase.CratePoints[i];
                SpawnCrate(ref state, i, pt.X, pt.Z);
            }
        }

        public static int CountActive(ref MatchState state)
        {
            int n = 0;
            for (int i = 0; i < PoolCaps.Crates; i++)
            {
                if ((MatchStateAccess.GetCrate(ref state, i).Flags & FlagActive) != 0)
                {
                    n++;
                }
            }

            return n;
        }

        /// <summary>
        /// After actor ResolveHits: Attack Active frames can open crates in reach.
        /// Sets <see cref="ActorSlot.HitApplied"/> when a crate is struck.
        /// </summary>
        public static void ResolveAttackHits(
            ref MatchState state,
            KitRuntime[] kits,
            StageGraph graph,
            ContentCatalog? catalog,
            ref SimContactBuffer events)
        {
            if (CountActive(ref state) <= 0)
            {
                return;
            }

            int jewelChance = graph.JewelCrateChancePercent;
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
                Fix range = Fix.FromRaw(rangeRaw);
                Fix radiusSum = range + CrateHitRadius;

                for (int c = 0; c < PoolCaps.Crates; c++)
                {
                    GenericPoolSlot crate = MatchStateAccess.GetCrate(ref state, c);
                    if ((crate.Flags & FlagActive) == 0)
                    {
                        continue;
                    }

                    if (!PhysicsWorld.OverlapsXZ(
                            attacker.PosX, attacker.PosZ, crate.PosX, crate.PosZ, radiusSum))
                    {
                        continue;
                    }

                    int damage = attacker.MoveDamage;
                    if (damage < 1)
                    {
                        damage = 1;
                    }

                    crate.Param0 -= damage;
                    attacker.HitApplied = 1;

                    if (crate.Param0 <= 0)
                    {
                        crate.Flags = 0;
                        crate.Param0 = 0;
                        MatchStateAccess.SetCrate(ref state, c, crate);
                        OnBroken(ref state, c, crate.PosX, crate.PosZ, jewelChance, catalog, ref events);
                    }
                    else
                    {
                        MatchStateAccess.SetCrate(ref state, c, crate);
                    }

                    break;
                }
            }
        }

        private static void OnBroken(
            ref MatchState state,
            int crateIndex,
            Fix x,
            Fix z,
            int jewelChancePercent,
            ContentCatalog? catalog,
            ref SimContactBuffer events)
        {
            events.Add(SimEventKind.CrateBroken, (byte)crateIndex, 0);
            if ((state.HouseRulesBits & MatchRules.ExpeditionGleams) != 0)
            {
                GleamBudgetRuntime.OnContainerBroken(ref state, x, z);
            }

            var pos = new FixVec3(x, Fix.Zero, z);
            int roll = state.GameplayRng.NextInt(100);
            if (roll < jewelChancePercent)
            {
                if (JewelRuntime.TrySpawnOnField(ref state, pos))
                {
                    return;
                }
            }

            int itemId = CrateSpawner.RollAlwaysEligible(ref state, catalog);
            if (itemId >= 0)
            {
                CrateSpawner.TrySpawnLooseItem(ref state, itemId, pos);
            }
        }

        private static void SpawnCrate(ref MatchState state, int index, Fix x, Fix z)
        {
            if (index < 0 || index >= PoolCaps.Crates)
            {
                return;
            }

            var slot = new GenericPoolSlot
            {
                PosX = x,
                PosY = Fix.Zero,
                PosZ = z,
                TypeId = KindJewelCrate,
                Param0 = OneHitLife,
                Flags = FlagActive,
            };
            MatchStateAccess.SetCrate(ref state, index, slot);
        }

        private static void Clear(ref MatchState state)
        {
            for (int i = 0; i < PoolCaps.Crates; i++)
            {
                MatchStateAccess.SetCrate(ref state, i, default);
            }
        }
    }
}
