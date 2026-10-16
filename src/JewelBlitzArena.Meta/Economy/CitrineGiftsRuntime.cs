using JewelBlitzArena.Data;

namespace JewelBlitzArena.Meta
{
    /// <summary>
    /// Captain Citrine Talk gifts (items-and-economy.md §6) — greybox stub.
    /// Appears after unlock; chance rises with playtime and lifetime carats spent.
    /// Two gifts, each once: Solitaire and Regent's Diadem.
    /// </summary>
    public static class CitrineGiftsRuntime
    {
        /// <summary>Solitaire — rarity 7, never, pocketable (stub id).</summary>
        public const int SolitaireItemId = 910001;

        /// <summary>Regent's Diadem — rarity 7 head cosmetic (S5 catalog id).</summary>
        public const int DiademItemId = SampleContentFactory.RegentsDiademItemId;

        public const int DiademSellCarats = 100_000;

        /// <summary>Base appearance chance (permille) once unlocked.</summary>
        public const int BaseAppearPermille = 80;

        /// <summary>+1‰ per 60 s of playtime, capped.</summary>
        public const int PlaytimePermillePerMinute = 1;
        public const int PlaytimeBonusCapPermille = 200;

        /// <summary>+1‰ per 500 carats spent, capped.</summary>
        public const int SpendPermillePer500Carats = 1;
        public const int SpendBonusCapPermille = 200;

        public static bool HasReceivedSolitaire(PlayerProfile profile) =>
            profile != null && profile.HasFlag(ProfileFlags.ReceivedSolitaire);

        public static bool HasReceivedDiadem(PlayerProfile profile) =>
            profile != null && profile.HasFlag(ProfileFlags.ReceivedDiadem);

        public static bool HasPendingGifts(PlayerProfile profile) =>
            profile != null
            && profile.HasFlag(ProfileFlags.UnlockedCaptainCitrine)
            && (!HasReceivedSolitaire(profile) || !HasReceivedDiadem(profile));

        /// <summary>
        /// Appearance chance in permille (0–1000). Shown as "Citrine is at the bench".
        /// </summary>
        public static int AppearanceChancePermille(PlayerProfile profile)
        {
            if (profile == null || !profile.HasFlag(ProfileFlags.UnlockedCaptainCitrine))
            {
                return 0;
            }

            if (!HasPendingGifts(profile))
            {
                return 0;
            }

            int chance = BaseAppearPermille;

            int playBonus = profile.PlaytimeSeconds / 60 * PlaytimePermillePerMinute;
            if (playBonus > PlaytimeBonusCapPermille)
            {
                playBonus = PlaytimeBonusCapPermille;
            }

            int spendBonus = (profile.CaratsSpentLifetime / 500) * SpendPermillePer500Carats;
            if (spendBonus > SpendBonusCapPermille)
            {
                spendBonus = SpendBonusCapPermille;
            }

            chance += playBonus + spendBonus;
            if (chance > 1000)
            {
                chance = 1000;
            }

            return chance;
        }

        /// <summary>Deterministic "Citrine is at the bench" roll for this visit seed.</summary>
        public static bool IsAtBench(PlayerProfile profile, ulong visitSeed)
        {
            int chance = AppearanceChancePermille(profile);
            if (chance <= 0)
            {
                return false;
            }

            ulong x = Mix(visitSeed ^ 0x43495452494E45UL ^ (ulong)(uint)profile.PlaytimeSeconds);
            return (int)(x % 1000UL) < chance;
        }

        /// <summary>
        /// If Citrine is at the bench, grant the next pending gift (Solitaire then Diadem).
        /// Returns granted item id, or 0.
        /// </summary>
        public static int TryGrantGift(PlayerProfile profile, ulong visitSeed)
        {
            if (profile == null || !IsAtBench(profile, visitSeed))
            {
                return 0;
            }

            if (!HasReceivedSolitaire(profile))
            {
                profile.Ledger.Grant(SolitaireItemId);
                profile.SetFlag(ProfileFlags.ReceivedSolitaire);
                return SolitaireItemId;
            }

            if (!HasReceivedDiadem(profile))
            {
                profile.Ledger.Grant(DiademItemId);
                profile.SetFlag(ProfileFlags.ReceivedDiadem);
                return DiademItemId;
            }

            return 0;
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
