namespace JewelBlitzArena.Meta
{
    /// <summary>
    /// Expedition clear-bonus item (items-and-economy.md §7) — greybox stub pool.
    /// One special item per clear: rarity 6 ≥70% on diff 8 (time can add up to +15), else rarity 5.
    /// Prefers never-owned ids; falls back to owned only when the pool is exhausted.
    /// </summary>
    public static class ExpeditionClearBonusRuntime
    {
        public const int Diff8Rarity6BasePercent = 70;
        public const int Diff8Rarity6TimeBonusMax = 15;

        /// <summary>Greybox rarity-5 clear-bonus stubs.</summary>
        public static readonly int[] Rarity5Pool = { 920501, 920502, 920503, 920504, 920505 };

        /// <summary>Greybox rarity-6 clear-bonus stubs.</summary>
        public static readonly int[] Rarity6Pool = { 920601, 920602, 920603 };

        /// <summary>
        /// Rarity-6 chance on difficulty 8. Faster than par adds up to
        /// <see cref="Diff8Rarity6TimeBonusMax"/> points. Other difficulties return 0 (always r5).
        /// </summary>
        public static int Rarity6ChancePercent(int difficulty, int framesPlayed, int parFrames)
        {
            if (ExpeditionChestRuntime.ClampDifficulty(difficulty) < 8)
            {
                return 0;
            }

            int chance = Diff8Rarity6BasePercent;
            if (parFrames > 0 && framesPlayed < parFrames)
            {
                // Linear: 0.75×par → +15, par → +0.
                long ratio = ((long)framesPlayed * 1000L) / parFrames;
                if (ratio < 750)
                {
                    ratio = 750;
                }

                if (ratio > 1000)
                {
                    ratio = 1000;
                }

                int bonus = (int)(((1000 - ratio) * Diff8Rarity6TimeBonusMax) / (1000 - 750));
                chance += bonus;
            }

            if (chance > Diff8Rarity6BasePercent + Diff8Rarity6TimeBonusMax)
            {
                chance = Diff8Rarity6BasePercent + Diff8Rarity6TimeBonusMax;
            }

            return chance;
        }

        public static bool RollRarity6(
            ulong seed,
            int stageId,
            int difficulty,
            int framesPlayed,
            int parFrames)
        {
            int chance = Rarity6ChancePercent(difficulty, framesPlayed, parFrames);
            if (chance <= 0)
            {
                return false;
            }

            ulong x = Mix(seed ^ 0x434C424F4E5553UL ^ ((ulong)(uint)stageId << 11) ^ (ulong)(uint)framesPlayed);
            return (int)(x % 100UL) < chance;
        }

        /// <summary>
        /// Pick and grant a clear-bonus item. Returns granted item id, or 0 if none.
        /// </summary>
        public static int TryGrant(
            PlayerProfile profile,
            ulong seed,
            int stageId,
            int difficulty,
            int framesPlayed)
        {
            if (profile == null)
            {
                return 0;
            }

            int par = ExpeditionChestRuntime.ParFrames(stageId);
            bool rarity6 = RollRarity6(seed, stageId, difficulty, framesPlayed, par);
            int[] pool = rarity6 ? Rarity6Pool : Rarity5Pool;
            int picked = PickPreferUnowned(profile, pool, seed, stageId);
            if (picked == 0)
            {
                // Exhausted preferred rarity — try the other pool before giving up.
                int[] fallback = rarity6 ? Rarity5Pool : Rarity6Pool;
                picked = PickPreferUnowned(profile, fallback, seed ^ 0xFBUL, stageId);
            }

            if (picked == 0)
            {
                return 0;
            }

            profile.Ledger.Grant(picked);
            return picked;
        }

        private static int PickPreferUnowned(
            PlayerProfile profile,
            int[] pool,
            ulong seed,
            int stageId)
        {
            if (pool == null || pool.Length == 0)
            {
                return 0;
            }

            int unownedCount = 0;
            for (int i = 0; i < pool.Length; i++)
            {
                if (!profile.Ledger.Owns(pool[i]))
                {
                    unownedCount++;
                }
            }

            if (unownedCount > 0)
            {
                ulong x = Mix(seed ^ 0x554E4F574EUL ^ ((ulong)(uint)stageId << 7));
                int pick = (int)(x % (ulong)(uint)unownedCount);
                for (int i = 0; i < pool.Length; i++)
                {
                    if (profile.Ledger.Owns(pool[i]))
                    {
                        continue;
                    }

                    if (pick == 0)
                    {
                        return pool[i];
                    }

                    pick--;
                }
            }

            // All owned — still grant a deterministic pick so the bonus is never empty mid-pool.
            ulong y = Mix(seed ^ 0x4F574E4544UL ^ ((ulong)(uint)stageId << 5));
            return pool[(int)(y % (ulong)(uint)pool.Length)];
        }

        private static ulong Mix(ulong x)
        {
            x ^= x >> 30;
            x *= 0xBF58476D1CE4E5B9UL;
            x ^= x >> 27;
            x *= 0x94D049BB133111EBUL;
            x ^= x >> 31;
            return x;
        }
    }
}
