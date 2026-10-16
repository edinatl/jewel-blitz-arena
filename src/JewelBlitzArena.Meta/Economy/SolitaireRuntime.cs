using JewelBlitzArena.Data;

namespace JewelBlitzArena.Meta
{
    /// <summary>
    /// Meta side of Solitaire (items-and-economy.md §6): pocket eligibility for match use.
    /// Sim applies the bonus jewel via <c>JewelBlitzArena.Sim.SolitaireRuntime</c>.
    /// </summary>
    public static class SolitaireRuntime
    {
        public const int ItemId = SampleContentFactory.SolitaireItemId;

        /// <summary>True when Solitaire is owned and flagged in Pocket Items.</summary>
        public static bool IsPocketed(PlayerProfile profile) =>
            profile != null
            && profile.Ledger.Owns(ItemId)
            && profile.Pockets.Contains(ItemId);

        /// <summary>
        /// Pocket Solitaire when owned (Ledger bind). Returns false if not owned / full / already pocketed.
        /// </summary>
        public static bool TryPocket(PlayerProfile profile, ContentCatalog catalog) =>
            LedgerRuntime.TryPocket(profile, catalog, ItemId);

        public static bool TryUnpocket(PlayerProfile profile) =>
            LedgerRuntime.TryUnpocket(profile, ItemId);
    }
}
