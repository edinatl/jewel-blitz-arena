using JewelBlitzArena.Data;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Meta
{
    /// <summary>
    /// Expedition difficulty table (modes.md §5) + balance.md §8 participant factor.
    /// Pure Meta helpers — Sim only sees the resulting MatchConfig scalars.
    /// </summary>
    public static class ExpeditionDifficultyRuntime
    {
        /// <summary>
        /// Ladder slot: 0=FFA1, 1=FFA2, 2=Harvester, 3=FFA4, 4=Pressworks.
        /// Used for opponent counts at difficulty 5–6.
        /// </summary>
        public static int InferLadderSlot(int stageId)
        {
            if (stageId == SampleContentFactory.HarvesterTerracesStageId)
            {
                return 2;
            }

            if (stageId == SampleContentFactory.PressworksStageId)
            {
                return 4;
            }

            // Story FFAs — treat as early (0) unless caller sets LadderStageIndex.
            return 0;
        }

        public static bool IsBossStage(int stageId) =>
            stageId == SampleContentFactory.HarvesterTerracesStageId
            || stageId == SampleContentFactory.PressworksStageId;

        /// <summary>Opponents per FFA (not including the player).</summary>
        public static int OpponentsForFfa(int difficulty, int ladderSlot)
        {
            int d = ExpeditionChestRuntime.ClampDifficulty(difficulty);
            if (d <= 2)
            {
                return 1;
            }

            if (d <= 4)
            {
                return 2;
            }

            if (d <= 6)
            {
                // Stage 1 (slot 0): 2; stages 2 and 4 (slots 1 and 3): 3.
                if (ladderSlot == 1 || ladderSlot == 3)
                {
                    return 3;
                }

                return 2;
            }

            return 3;
        }

        /// <summary>Total actors for the match (player + opponents, or player + ally on boss).</summary>
        public static int ActorCountFor(int difficulty, int stageId, int ladderSlot)
        {
            if (IsBossStage(stageId))
            {
                return AllyPresent(difficulty) ? 2 : 1;
            }

            return 1 + OpponentsForFfa(difficulty, ladderSlot);
        }

        public static bool AllyPresent(int difficulty) =>
            ExpeditionChestRuntime.ClampDifficulty(difficulty) >= 3;

        /// <summary>Gleam pickup scale as permille (1000 = 1.0).</summary>
        public static int GleamScalePermille(int difficulty)
        {
            switch (ExpeditionChestRuntime.ClampDifficulty(difficulty))
            {
                case 1:
                case 2:
                    return 500;
                case 3:
                case 4:
                    return 700;
                case 5:
                case 6:
                    return 850;
                default:
                    return 1000;
            }
        }

        /// <summary>Boss difficulty_scale as permille (modes.md §5).</summary>
        public static int BossDifficultyScalePermille(int difficulty)
        {
            switch (ExpeditionChestRuntime.ClampDifficulty(difficulty))
            {
                case 1:
                case 2:
                    return 600;
                case 3:
                case 4:
                    return 800;
                case 5:
                case 6:
                    return 1000;
                default:
                    return 1200;
            }
        }

        /// <summary>balance.md §8 participant_factor as permille.</summary>
        public static int ParticipantFactorPermille(int fighterCount)
        {
            if (fighterCount <= 1)
            {
                return 600;
            }

            if (fighterCount == 2)
            {
                return 1000;
            }

            if (fighterCount == 3)
            {
                return 1300;
            }

            return 1550;
        }

        /// <summary>Combined boss Life scale permille = difficulty_scale × participant_factor.</summary>
        public static int BossLifeScalePermille(int difficulty, int fighterCount)
        {
            long scale = (long)BossDifficultyScalePermille(difficulty)
                * ParticipantFactorPermille(fighterCount);
            return (int)(scale / 1000L);
        }

        public static int ScaleLife(int baseLife, int scalePermille)
        {
            if (baseLife <= 0)
            {
                return 0;
            }

            if (scalePermille <= 0)
            {
                return 1;
            }

            long scaled = ((long)baseLife * scalePermille) / 1000L;
            if (scaled < 1)
            {
                return 1;
            }

            if (scaled > int.MaxValue)
            {
                return int.MaxValue;
            }

            return (int)scaled;
        }
    }
}
