using JewelBlitzArena.Data;

namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Expedition in-match carat budget (items-and-economy.md §7):
    /// KO 50% / knockdowns caused 30% / containers 20%. Spawns denomination gleams.
    /// In-match pool is ~45% of the stage design row; chest uses the full design row.
    /// </summary>
    public static class GleamBudgetRuntime
    {
        /// <summary>Full design-row totals (economy §7). Chest keys off these.</summary>
        public const int DesignFfaTotal = 1100;
        public const int DesignHarvesterTotal = 2000;
        public const int DesignPressworksTotal = 2100;

        /// <summary>Legacy aliases — prefer <see cref="DesignTotalForStage"/> / <see cref="BudgetForStage"/>.</summary>
        public const int DefaultFfaBudget = DesignFfaTotal;
        public const int HarvesterBudget = DesignHarvesterTotal;
        public const int PressworksBudget = DesignPressworksTotal;

        /// <summary>In-match share of the design row (chest keeps the complementary 55%).</summary>
        public const int InMatchBudgetPercent = 45;

        /// <summary>Soft per-event spend caps so one KO cannot dump the whole pool.</summary>
        public const int MaxKoDrop = 400;
        public const int MaxKdDrop = 150;
        public const int MaxCrateDrop = 100;

        public static void InitializeMatch(ref MatchState state, StageGraph graph)
        {
            state.CaratsCollected = 0;
            state.GleamBudgetKoRemaining = 0;
            state.GleamBudgetKdRemaining = 0;
            state.GleamBudgetCrateRemaining = 0;

            if ((state.HouseRulesBits & MatchRules.ExpeditionGleams) == 0)
            {
                return;
            }

            int total = BudgetForStage(graph.StageId);
            state.GleamBudgetKoRemaining = (total * 50) / 100;
            state.GleamBudgetKdRemaining = (total * 30) / 100;
            state.GleamBudgetCrateRemaining = total
                - state.GleamBudgetKoRemaining
                - state.GleamBudgetKdRemaining;
        }

        /// <summary>Full economy §7 design row for the stage (chest / Meta).</summary>
        public static int DesignTotalForStage(int stageId)
        {
            if (stageId == SampleContentFactory.HarvesterTerracesStageId)
            {
                return DesignHarvesterTotal;
            }

            if (stageId == SampleContentFactory.PressworksStageId)
            {
                return DesignPressworksTotal;
            }

            return DesignFfaTotal;
        }

        /// <summary>In-match gleam pool (~45% of design row).</summary>
        public static int BudgetForStage(int stageId) =>
            (DesignTotalForStage(stageId) * InMatchBudgetPercent) / 100;

        public static void OnKo(ref MatchState state, Fix x, Fix z)
        {
            Spend(ref state, ref state.GleamBudgetKoRemaining, MaxKoDrop, x, z);
        }

        public static void OnKnockdownCaused(ref MatchState state, Fix x, Fix z)
        {
            Spend(ref state, ref state.GleamBudgetKdRemaining, MaxKdDrop, x, z);
        }

        public static void OnContainerBroken(ref MatchState state, Fix x, Fix z)
        {
            Spend(ref state, ref state.GleamBudgetCrateRemaining, MaxCrateDrop, x, z);
        }

        private static void Spend(ref MatchState state, ref int pool, int maxDrop, Fix x, Fix z)
        {
            if ((state.HouseRulesBits & MatchRules.ExpeditionGleams) == 0 || pool <= 0)
            {
                return;
            }

            int allot = pool < maxDrop ? pool : maxDrop;
            int spent = SpawnDenominations(ref state, allot, x, z);
            pool -= spent;
            if (pool < 0)
            {
                pool = 0;
            }
        }

        /// <summary>Greedy gold → silver → copper until allotment is spent or slots full.</summary>
        public static int SpawnDenominations(ref MatchState state, int allotment, Fix x, Fix z)
        {
            int remaining = allotment;
            int spent = 0;
            while (remaining >= MatchRules.GleamGoldCarats)
            {
                if (GleamRuntime.TrySpawn(ref state, GleamRuntime.KindGold, x, z) < 0)
                {
                    break;
                }

                remaining -= MatchRules.GleamGoldCarats;
                spent += MatchRules.GleamGoldCarats;
            }

            while (remaining >= MatchRules.GleamSilverCarats)
            {
                if (GleamRuntime.TrySpawn(ref state, GleamRuntime.KindSilver, x, z) < 0)
                {
                    break;
                }

                remaining -= MatchRules.GleamSilverCarats;
                spent += MatchRules.GleamSilverCarats;
            }

            while (remaining >= MatchRules.GleamCopperCarats)
            {
                if (GleamRuntime.TrySpawn(ref state, GleamRuntime.KindCopper, x, z) < 0)
                {
                    break;
                }

                remaining -= MatchRules.GleamCopperCarats;
                spent += MatchRules.GleamCopperCarats;
            }

            return spent;
        }
    }
}
