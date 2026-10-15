namespace JewelBlitzArena.Meta
{
    /// <summary>Profile schema versions. Migrations live in <see cref="ProfileMigrator"/>.</summary>
    public static class ProfileSchema
    {
        /// <summary>Empty / pre-Meta placeholder (no file or legacy stub).</summary>
        public const int Version0 = 0;

        /// <summary>Current skeletal profile (Ledger + flags + carats).</summary>
        public const int Version1 = 1;

        public const int Current = Version1;
    }
}
