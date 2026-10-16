using JewelBlitzArena.Data;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Meta
{
    /// <summary>
    /// Post-stage Expedition chest + time adjustment (items-and-economy.md §7).
    /// Greybox: chest base is ~55% of the stage design row scaled by difficulty
    /// full-clear target / 8000, stacked on top of in-match gleam carats (Sim budgets
    /// stay unchanged for corpus stability).
    /// </summary>
    public static class ExpeditionChestRuntime
    {
        public const int DiffMin = 1;
        public const int DiffMax = 8;
        public const int DiffDefault = 4;
        public const int FullClearDiff8 = 8000;
        public const int ChestSharePercent = 55;
        public const int PurseValue = 2000;
        public const int PurseDifficultyMin = 5;
        public const int PurseChancePercent = 20;

        /// <summary>FFA par proxy (mid of balance.md §1 65–80 s).</summary>
        public const int ParFfaSeconds = 72;

        /// <summary>Harvester par proxy (mid of balance.md §8 70–90 s).</summary>
        public const int ParHarvesterSeconds = 80;

        /// <summary>Pressworks par proxy (mid of balance.md §8 90–110 s).</summary>
        public const int ParPressworksSeconds = 100;

        public static int ClampDifficulty(int difficulty)
        {
            if (difficulty < DiffMin)
            {
                return DiffMin;
            }

            if (difficulty > DiffMax)
            {
                return DiffMax;
            }

            return difficulty;
        }

        /// <summary>Full-clear carat targets before time adjustment (modes.md / economy §7).</summary>
        public static int FullClearTarget(int difficulty)
        {
            switch (ClampDifficulty(difficulty))
            {
                case 1: return 2000;
                case 2: return 2600;
                case 3: return 3300;
                case 4: return 4000;
                case 5: return 5000;
                case 6: return 6000;
                case 7: return 7000;
                default: return 8000;
            }
        }

        public static int StageDesignTotal(int stageId) =>
            GleamBudgetRuntime.BudgetForStage(stageId);

        public static int ParFrames(int stageId)
        {
            int seconds = ParFfaSeconds;
            if (stageId == SampleContentFactory.HarvesterTerracesStageId)
            {
                seconds = ParHarvesterSeconds;
            }
            else if (stageId == SampleContentFactory.PressworksStageId)
            {
                seconds = ParPressworksSeconds;
            }

            return seconds * 60;
        }

        /// <summary>
        /// Chest base carats before time mul / purse. 55% of stage design × (target/8000).
        /// </summary>
        public static int ChestBaseCarats(int stageId, int difficulty)
        {
            int stage = StageDesignTotal(stageId);
            int target = FullClearTarget(difficulty);
            long chest = ((long)stage * ChestSharePercent) / 100L;
            chest = (chest * target) / FullClearDiff8;
            if (chest < 0)
            {
                return 0;
            }

            if (chest > int.MaxValue)
            {
                return int.MaxValue;
            }

            return (int)chest;
        }

        /// <summary>
        /// Time multiplier in permille. Piecewise linear: 0.75×par→1200, par→1000, 1.5×par→800.
        /// Clamped outside that band.
        /// </summary>
        public static int TimeMulPermille(int framesPlayed, int parFrames)
        {
            if (parFrames <= 0)
            {
                return 1000;
            }

            long ratio = ((long)framesPlayed * 1000L) / parFrames;
            if (ratio < 750)
            {
                ratio = 750;
            }
            else if (ratio > 1500)
            {
                ratio = 1500;
            }

            if (ratio <= 1000)
            {
                // [750,1000] → [1200,1000]
                return (int)(1200 + ((ratio - 750) * (1000 - 1200)) / (1000 - 750));
            }

            // [1000,1500] → [1000,800]
            return (int)(1000 + ((ratio - 1000) * (800 - 1000)) / (1500 - 1000));
        }

        public static int ApplyTimeMul(int chestBase, int framesPlayed, int stageId)
        {
            int mul = TimeMulPermille(framesPlayed, ParFrames(stageId));
            long scaled = ((long)chestBase * mul) / 1000L;
            if (scaled < 0)
            {
                return 0;
            }

            if (scaled > int.MaxValue)
            {
                return int.MaxValue;
            }

            return (int)scaled;
        }

        /// <summary>
        /// Deterministic purse roll at difficulty ≥5 (20%). Seeded from match seed + stage + frames.
        /// </summary>
        public static bool RollPurse(ulong seed, int stageId, int framesPlayed, int difficulty)
        {
            if (ClampDifficulty(difficulty) < PurseDifficultyMin)
            {
                return false;
            }

            ulong x = seed ^ ((ulong)(uint)stageId << 32) ^ (ulong)(uint)framesPlayed;
            x ^= x >> 33;
            x *= 0xff51afd7ed558ccdUL;
            x ^= x >> 33;
            return (int)(x % 100UL) < PurseChancePercent;
        }

        public static int ResolveChestCarats(
            int stageId,
            int difficulty,
            int framesPlayed,
            ulong seed,
            bool stageCleared)
        {
            if (!stageCleared)
            {
                return 0;
            }

            int diff = ClampDifficulty(difficulty);
            if (RollPurse(seed, stageId, framesPlayed, diff))
            {
                return ApplyTimeMul(PurseValue, framesPlayed, stageId);
            }

            int chest = ChestBaseCarats(stageId, diff);
            return ApplyTimeMul(chest, framesPlayed, stageId);
        }

        public static bool PlayerClearedStage(in MatchResult result)
        {
            if (result.ActorCount <= 0)
            {
                return false;
            }

            unsafe
            {
                return result.Placements[0] == 1;
            }
        }
    }
}
