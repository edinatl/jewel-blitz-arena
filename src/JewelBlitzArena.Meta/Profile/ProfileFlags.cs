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
    }
}
