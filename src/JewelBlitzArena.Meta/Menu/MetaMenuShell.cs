using System.Collections.Generic;
using JewelBlitzArena.Data;

namespace JewelBlitzArena.Meta
{
    /// <summary>Skeletal main-menu entries. Presentation binds labels; Meta owns availability.</summary>
    public sealed class MetaMenuShell
    {
        private readonly TinkerModeStub _tinker = new TinkerModeStub();

        public IReadOnlyList<MetaMenuEntry> BuildEntries(PlayerProfile profile)
        {
            return new[]
            {
                new MetaMenuEntry(ModeId.Custom, "Custom", available: true),
                new MetaMenuEntry(ModeId.Sparring, "Sparring", available: true),
                new MetaMenuEntry(ModeId.Duel, "Duel", available: false, reason: "M2"),
                new MetaMenuEntry(ModeId.Tournament, "Tournament", available: false, reason: "M2"),
                new MetaMenuEntry(ModeId.Expedition, "Expedition", available: true),
                new MetaMenuEntry(ModeId.TillysBench, _tinker.MenuLabel, available: true),
                new MetaMenuEntry(ModeId.Online, "Online", available: false, reason: "S7"),
            };
        }

        public void OpenTinker(PlayerProfile profile) => _tinker.Open(profile);

        /// <summary>Custom stage list including Extra Arena box (locked rows visible).</summary>
        public IReadOnlyList<ExtraArenaRuntime.StageCursorEntry> BuildCustomStageCursor(
            PlayerProfile profile,
            ContentCatalog? catalog = null) =>
            ExtraArenaRuntime.BuildCustomStageCursor(profile, catalog);

        public int ExtraArenaSlotsUnlocked(PlayerProfile profile) =>
            UnlockLadderRuntime.ExtraArenaSlotCount(profile);

        /// <summary>Tilly's Bench rooms (Buy/Sell/Tinker/Talk/Ledger/Fitting/Wheel).</summary>
        public IReadOnlyList<BenchRoomsRuntime.BenchRoomEntry> BuildBenchRooms(
            PlayerProfile profile,
            ulong visitSeed = 0UL) =>
            BenchRoomsRuntime.BuildRooms(profile, visitSeed);

        public int OpenTalk(PlayerProfile profile, ulong visitSeed) =>
            BenchRoomsRuntime.OpenTalk(profile, visitSeed);

        public bool TryEquipDiadem(PlayerProfile profile) =>
            BenchRoomsRuntime.TryEquipDiadem(profile);

        /// <summary>Buy room stock via <see cref="ShopRuntime.BuildStock"/>.</summary>
        public IReadOnlyList<ShopRuntime.StockEntry> OpenBuy(
            PlayerProfile profile,
            ContentCatalog catalog) =>
            BenchRoomsRuntime.OpenBuy(profile, catalog);

        public bool TryBuy(PlayerProfile profile, ContentCatalog catalog, int itemId) =>
            BenchRoomsRuntime.TryBuy(profile, catalog, itemId);

        /// <summary>Sell room list via <see cref="ShopRuntime.BuildSellList"/>.</summary>
        public IReadOnlyList<ShopRuntime.SellEntry> OpenSell(
            PlayerProfile profile,
            ContentCatalog catalog) =>
            BenchRoomsRuntime.OpenSell(profile, catalog);

        public bool TrySell(PlayerProfile profile, ContentCatalog catalog, int itemId) =>
            BenchRoomsRuntime.TrySell(profile, catalog, itemId);
    }

    public readonly struct MetaMenuEntry
    {
        public MetaMenuEntry(ModeId mode, string label, bool available, string? reason = null)
        {
            Mode = mode;
            Label = label;
            Available = available;
            UnavailableReason = reason;
        }

        public ModeId Mode { get; }
        public string Label { get; }
        public bool Available { get; }
        public string? UnavailableReason { get; }
    }
}
