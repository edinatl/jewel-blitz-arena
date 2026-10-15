namespace JewelBlitzArena.Data
{
    /// <summary>Budgets from <c>docs/design/balance.md</c> used at export validation.</summary>
    public static class BalanceBudgets
    {
        public static int MaxLifeForRating(int lifeRating)
        {
            switch (lifeRating)
            {
                case 1: return 850;
                case 2: return 925;
                case 3: return 1000;
                case 4: return 1075;
                case 5: return 1150;
                default: return -1;
            }
        }

        public static bool TryGetDamageBand(MoveClass moveClass, out int min, out int max)
        {
            switch (moveClass)
            {
                case MoveClass.Attack:
                    min = 30; max = 40; return true;
                case MoveClass.Strong:
                    min = 80; max = 100; return true;
                case MoveClass.AirAttack:
                    min = 40; max = 50; return true;
                case MoveClass.AirStrong:
                    min = 70; max = 90; return true;
                case MoveClass.DashAttack:
                    min = 60; max = 80; return true;
                case MoveClass.Throw:
                    min = 100; max = 100; return true;
                case MoveClass.GetupAttack:
                    min = 40; max = 40; return true;
                case MoveClass.Flare:
                    // Flare budgets are fighter-specific; S5 only checks non-negative.
                    min = 0; max = 400; return true;
                case MoveClass.Surge:
                    min = 0; max = 330; return true;
                case MoveClass.UtilityStrong:
                    min = 0; max = 0; return true;
                default:
                    min = 0; max = 0; return false;
            }
        }

        public static bool IsValidItemRarity(int rarity) => rarity >= 1 && rarity <= 7;

        /// <summary>Item damage-per-use soft ceiling by rarity (balance.md §6 — lifetime band proxy).</summary>
        public static int MaxItemDamagePerUse(int rarity)
        {
            switch (rarity)
            {
                case 1: return 40;
                case 2: return 55;
                case 3: return 70;
                case 4: return 90;
                case 5: return 110;
                case 6: return 140;
                case 7: return 180;
                default: return 0;
            }
        }
    }
}
