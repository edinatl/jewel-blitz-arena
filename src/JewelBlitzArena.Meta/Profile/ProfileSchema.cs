namespace JewelBlitzArena.Meta
{
    /// <summary>Profile schema versions. Migrations live in <see cref="ProfileMigrator"/>.</summary>
    public static class ProfileSchema
    {
        /// <summary>Empty / pre-Meta placeholder (no file or legacy stub).</summary>
        public const int Version0 = 0;

        /// <summary>Skeletal profile (Ledger + flags + carats).</summary>
        public const int Version1 = 1;

        /// <summary>Adds BestExpeditionDifficulty for unlock ladder (modes.md).</summary>
        public const int Version2 = 2;

        /// <summary>Adds Chits + FacetWheelWidthLevel for Facet Wheel greybox.</summary>
        public const int Version3 = 3;

        /// <summary>Adds PlaytimeSeconds + CaratsSpentLifetime for Citrine gifts.</summary>
        public const int Version4 = 4;

        /// <summary>Adds ProfileSalt + TinkerCounter for Tinker craft seeding.</summary>
        public const int Version5 = 5;

        public const int Current = Version5;
    }
}
