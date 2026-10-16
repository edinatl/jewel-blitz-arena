using System;
using System.Collections.Generic;
using JewelBlitzArena.Data;

namespace JewelBlitzArena.Meta
{
    /// <summary>Tilly's Bench room ids (modes.md §6 / items-and-economy.md §6).</summary>
    public enum BenchRoom : byte
    {
        Buy = 0,
        Sell = 1,
        Tinker = 2,
        Talk = 3,
        Ledger = 4,
        FittingRoom = 5,
        FacetWheel = 6,
    }

    /// <summary>
    /// Bench room list + Talk/Fitting Room greybox actions.
    /// Talk surfaces "Citrine is at the bench" from <see cref="CitrineGiftsRuntime"/>.
    /// Fitting Room equips owned cosmetics (Diadem stub).
    /// </summary>
    public static class BenchRoomsRuntime
    {
        public readonly struct BenchRoomEntry
        {
            public BenchRoomEntry(
                BenchRoom room,
                string label,
                bool available,
                string? badge = null)
            {
                Room = room;
                Label = label;
                Available = available;
                Badge = badge;
            }

            public BenchRoom Room { get; }
            public string Label { get; }
            public bool Available { get; }
            /// <summary>Optional UI badge (e.g. "Citrine is at the bench").</summary>
            public string? Badge { get; }
        }

        public static IReadOnlyList<BenchRoomEntry> BuildRooms(
            PlayerProfile profile,
            ulong visitSeed)
        {
            bool citrineHere = CitrineGiftsRuntime.IsAtBench(profile, visitSeed);
            string? talkBadge = citrineHere ? "Citrine is at the bench" : null;

            return new[]
            {
                new BenchRoomEntry(BenchRoom.Buy, "Buy", available: true),
                new BenchRoomEntry(BenchRoom.Sell, "Sell", available: true),
                new BenchRoomEntry(BenchRoom.Tinker, "Tinker", available: true),
                new BenchRoomEntry(BenchRoom.Talk, "Talk", available: true, badge: talkBadge),
                new BenchRoomEntry(BenchRoom.Ledger, "Ledger", available: true),
                new BenchRoomEntry(BenchRoom.FittingRoom, "Fitting Room", available: true),
                new BenchRoomEntry(BenchRoom.FacetWheel, "Facet Wheel", available: true),
            };
        }

        /// <summary>Talk: attempt Citrine gift grant for this visit.</summary>
        public static int OpenTalk(PlayerProfile profile, ulong visitSeed)
        {
            if (profile == null)
            {
                return 0;
            }

            profile.SetFlag(ProfileFlags.TouchedTalkRoom);
            return CitrineGiftsRuntime.TryGrantGift(profile, visitSeed);
        }

        /// <summary>
        /// Buy room: stock list from <see cref="ShopRuntime.BuildStock"/> (profile ShopStockSeed).
        /// </summary>
        public static IReadOnlyList<ShopRuntime.StockEntry> OpenBuy(
            PlayerProfile profile,
            ContentCatalog catalog)
        {
            if (profile == null || catalog == null)
            {
                return Array.Empty<ShopRuntime.StockEntry>();
            }

            profile.SetFlag(ProfileFlags.TouchedBuyRoom);
            return ShopRuntime.BuildStock(catalog, profile);
        }

        /// <summary>Buy room: spend carats for a stocked item id.</summary>
        public static bool TryBuy(
            PlayerProfile profile,
            ContentCatalog catalog,
            int itemId) =>
            profile != null
            && catalog != null
            && ShopRuntime.TryBuy(profile, catalog, itemId);

        /// <summary>
        /// Sell room: owned items with sell price &gt; 0 via <see cref="ShopRuntime.BuildSellList"/>.
        /// </summary>
        public static IReadOnlyList<ShopRuntime.SellEntry> OpenSell(
            PlayerProfile profile,
            ContentCatalog catalog)
        {
            if (profile == null || catalog == null)
            {
                return Array.Empty<ShopRuntime.SellEntry>();
            }

            profile.SetFlag(ProfileFlags.TouchedSellRoom);
            return ShopRuntime.BuildSellList(catalog, profile);
        }

        /// <summary>Sell room: pay catalog sell price for an owned item id.</summary>
        public static bool TrySell(
            PlayerProfile profile,
            ContentCatalog catalog,
            int itemId) =>
            profile != null
            && catalog != null
            && ShopRuntime.TrySell(profile, catalog, itemId);

        /// <summary>Ledger room: owned catalog entries + pocket flags.</summary>
        public static IReadOnlyList<LedgerRuntime.LedgerEntry> OpenLedger(
            PlayerProfile profile,
            ContentCatalog catalog) =>
            LedgerRuntime.OpenLedger(profile, catalog);

        public static bool TryPocket(
            PlayerProfile profile,
            ContentCatalog catalog,
            int itemId) =>
            LedgerRuntime.TryPocket(profile, catalog, itemId);

        public static bool TryUnpocket(PlayerProfile profile, int itemId) =>
            LedgerRuntime.TryUnpocket(profile, itemId);

        /// <summary>
        /// Fitting Room: equip an owned catalog cosmetic into its socket (replaces prior).
        /// No Sim hitbox change.
        /// </summary>
        public static bool TryEquipCosmetic(
            PlayerProfile profile,
            ContentCatalog catalog,
            int itemId)
        {
            if (profile == null || catalog == null
                || !catalog.TryGetItem(itemId, out ItemRecord item)
                || item.Category != ItemCategory.Cosmetic
                || item.CosmeticSocket == CosmeticSocket.None)
            {
                return false;
            }

            profile.SetFlag(ProfileFlags.TouchedFittingRoom);
            if (!profile.Ledger.Owns(itemId))
            {
                return false;
            }

            profile.Fitting.Set(item.CosmeticSocket, itemId);
            SyncDiademFlag(profile);
            return true;
        }

        public static bool TryUnequipSocket(PlayerProfile profile, CosmeticSocket socket)
        {
            if (profile == null || socket == CosmeticSocket.None)
            {
                return false;
            }

            if (profile.Fitting.Get(socket) == 0)
            {
                return false;
            }

            profile.Fitting.Clear(socket);
            SyncDiademFlag(profile);
            return true;
        }

        public static int GetEquipped(PlayerProfile profile, CosmeticSocket socket) =>
            profile == null ? 0 : profile.Fitting.Get(socket);

        /// <summary>
        /// Fitting Room: equip Regent's Diadem when owned (compat wrapper → Head socket).
        /// </summary>
        public static bool TryEquipDiadem(PlayerProfile profile)
        {
            if (profile == null)
            {
                return false;
            }

            profile.SetFlag(ProfileFlags.TouchedFittingRoom);
            if (!profile.Ledger.Owns(CitrineGiftsRuntime.DiademItemId))
            {
                return false;
            }

            profile.Fitting.Set(CosmeticSocket.Head, CitrineGiftsRuntime.DiademItemId);
            SyncDiademFlag(profile);
            return true;
        }

        public static bool TryUnequipDiadem(PlayerProfile profile) =>
            TryUnequipSocket(profile, CosmeticSocket.Head);

        public static bool IsDiademEquipped(PlayerProfile profile) =>
            profile != null
            && profile.Fitting.Get(CosmeticSocket.Head) == CitrineGiftsRuntime.DiademItemId;

        private static void SyncDiademFlag(PlayerProfile profile)
        {
            bool on = profile.Fitting.Get(CosmeticSocket.Head) == CitrineGiftsRuntime.DiademItemId;
            profile.SetFlag(ProfileFlags.EquippedDiadem, on);
        }
    }
}
