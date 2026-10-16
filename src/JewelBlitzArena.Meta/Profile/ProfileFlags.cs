namespace JewelBlitzArena.Meta
{
    /// <summary>Bit flags on <see cref="PlayerProfile"/> — not Sim state.</summary>
    public static class ProfileFlags
    {
        /// <summary>Set when Custom greybox completes once (S6 exit criterion).</summary>
        public const int CompletedCustomGreybox = 1 << 0;

        /// <summary>Reserved: first Expedition stub touch.</summary>
        public const int TouchedExpeditionStub = 1 << 1;

        /// <summary>Reserved: first Tinker stub touch.</summary>
        public const int TouchedTinkerStub = 1 << 2;

        /// <summary>modes.md §8: Captain Citrine at 7 distinct clears.</summary>
        public const int UnlockedCaptainCitrine = 1 << 3;

        /// <summary>modes.md §8: Sound Test at 8 distinct clears.</summary>
        public const int UnlockedSoundTest = 1 << 4;

        /// <summary>modes.md §8: Terraces (no boss) + Extra Arena slot 1 at 9.</summary>
        public const int UnlockedTerracesExtra = 1 << 5;

        /// <summary>modes.md §8: Boss Rush preset at 10.</summary>
        public const int UnlockedBossRushPreset = 1 << 6;

        /// <summary>modes.md §8: Tournament Remix at 12.</summary>
        public const int UnlockedRemixOption = 1 << 7;

        /// <summary>First Facet Wheel spin.</summary>
        public const int TouchedFacetWheel = 1 << 8;
    }
}
