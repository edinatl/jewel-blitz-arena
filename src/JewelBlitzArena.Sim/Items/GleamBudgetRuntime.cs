using JewelBlitzArena.Data;

namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Expedition in-match carat budget (items-and-economy.md §7):
    /// KO 50% / knockdowns caused 30% / containers 20%. Spawns denomination gleams.
    /// </summary>
    public static class GleamBudgetRuntime
    {
        public const int DefaultFfaBudget = 1100;
        public const int HarvesterBudget = 2000;
        public const int PressworksBudget = 2100;

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

        public static int BudgetForStage(int stageId)
        {
            if (stageId == SampleContentFactory.HarvesterTerracesStageId)
            {
                return HarvesterBudget;
            }

            if (stageId == SampleContentFactory.PressworksStageId)
            {
                return PressworksBudget;
            }

            return DefaultFfaBudget;
        }

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
