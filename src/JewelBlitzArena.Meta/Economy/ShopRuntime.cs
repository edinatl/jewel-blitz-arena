using System.Collections.Generic;
using JewelBlitzArena.Data;

namespace JewelBlitzArena.Meta
{
    /// <summary>
    /// Tilly's Bench Buy/Sell greybox (items-and-economy.md §6).
    /// Buy spends carats and tracks <see cref="PlayerProfile.CaratsSpentLifetime"/>.
    /// Sell pays catalog sell price; ever-owned flag stays (Ledger ownership retained).
    /// </summary>
    public static class ShopRuntime
    {
        public const int AlwaysStockCount = 10;
        public const int RotatingStockCount = 5;
        public const int RotatingRarityMin = 3;
        public const int RotatingRarityMax = 5;

        public readonly struct StockEntry
        {
            public StockEntry(int itemId, int buyPrice, bool isNewToProfile)
            {
                ItemId = itemId;
                BuyPrice = buyPrice;
                IsNewToProfile = isNewToProfile;
            }

            public int ItemId { get; }
            public int BuyPrice { get; }
            public bool IsNewToProfile { get; }
        }

        public static bool IsSoldByTilly(ItemRecord item) =>
            item != null
            && item.BuyPrice > 0
            && item.Rarity >= 1
            && item.Rarity <= 5
            && item.Category != ItemCategory.StageWeapon
            && item.Category != ItemCategory.Gift
            && item.CrateEligibility != CrateEligibility.Never;

        /// <summary>Build stock from the profile's current <see cref="PlayerProfile.ShopStockSeed"/>.</summary>
        public static IReadOnlyList<StockEntry> BuildStock(
            ContentCatalog catalog,
            PlayerProfile profile) =>
            BuildStock(catalog, profile, profile?.ShopStockSeed ?? 0UL);

        /// <summary>
        /// Reseed rotating stock after an Expedition stage (items-and-economy.md §6).
        /// </summary>
        public static void ReseedAfterExpeditionStage(
            PlayerProfile profile,
            ulong matchSeed,
            int stageId,
            int framesPlayed)
        {
            if (profile == null)
            {
                return;
            }

            ulong x = profile.ShopStockSeed;
            x ^= matchSeed;
            x ^= (ulong)(uint)stageId << 17;
            x ^= (ulong)(uint)framesPlayed;
            profile.ShopStockSeed = Mix(x == 0UL ? 1UL : x);
        }

        /// <summary>
        /// Build shop stock: up to 10 always (lowest item id among sellable) + 5 rotating r3–5.
        /// </summary>
        public static IReadOnlyList<StockEntry> BuildStock(
            ContentCatalog catalog,
            PlayerProfile profile,
            ulong seed)
        {
            var result = new List<StockEntry>();
            if (catalog == null || profile == null)
            {
                return result;
            }

            var always = new List<ItemRecord>();
            var rotatingPool = new List<ItemRecord>();
            foreach (ItemRecord item in catalog.Items)
            {
                if (!IsSoldByTilly(item))
                {
                    continue;
                }

                always.Add(item);
                if (item.Rarity >= RotatingRarityMin && item.Rarity <= RotatingRarityMax)
                {
                    rotatingPool.Add(item);
                }
            }

            always.Sort((a, b) => a.ItemId.CompareTo(b.ItemId));
            int alwaysTake = always.Count < AlwaysStockCount ? always.Count : AlwaysStockCount;
            var taken = new HashSet<int>();
            for (int i = 0; i < alwaysTake; i++)
            {
                ItemRecord item = always[i];
                taken.Add(item.ItemId);
                result.Add(new StockEntry(
                    item.ItemId,
                    item.BuyPrice,
                    isNewToProfile: !profile.Ledger.Owns(item.ItemId)));
            }

            // Deterministic rotate from remaining r3–5 pool.
            var candidates = new List<ItemRecord>();
            for (int i = 0; i < rotatingPool.Count; i++)
            {
                if (!taken.Contains(rotatingPool[i].ItemId))
                {
                    candidates.Add(rotatingPool[i]);
                }
            }

            ulong x = Mix(seed ^ 0x53544F434BUL);
            int rotateTake = candidates.Count < RotatingStockCount
                ? candidates.Count
                : RotatingStockCount;
            for (int n = 0; n < rotateTake; n++)
            {
                int pick = (int)(x % (ulong)(uint)candidates.Count);
                x = Mix(x);
                ItemRecord item = candidates[pick];
                candidates.RemoveAt(pick);
                result.Add(new StockEntry(
                    item.ItemId,
                    item.BuyPrice,
                    isNewToProfile: !profile.Ledger.Owns(item.ItemId)));
            }

            return result;
        }

        public static bool TryBuy(
            PlayerProfile profile,
            ContentCatalog catalog,
            int itemId)
        {
            if (profile == null || catalog == null || !catalog.TryGetItem(itemId, out ItemRecord item))
            {
                return false;
            }

            if (!IsSoldByTilly(item) || profile.Carats < item.BuyPrice)
            {
                return false;
            }

            profile.Carats -= item.BuyPrice;
            long spent = (long)profile.CaratsSpentLifetime + item.BuyPrice;
            profile.CaratsSpentLifetime = spent > int.MaxValue ? int.MaxValue : (int)spent;
            profile.Ledger.Grant(itemId);
            return true;
        }

        /// <summary>
        /// Sell pays catalog sell price. Ever-owned stays (Ledger keeps the id).
        /// Returns false if not owned or sell price is 0.
        /// </summary>
        public static bool TrySell(
            PlayerProfile profile,
            ContentCatalog catalog,
            int itemId)
        {
            if (profile == null || catalog == null || !catalog.TryGetItem(itemId, out ItemRecord item))
            {
                return false;
            }

            if (!profile.Ledger.Owns(itemId) || item.SellPrice <= 0)
            {
                return false;
            }

            long sum = (long)profile.Carats + item.SellPrice;
            profile.Carats = sum > int.MaxValue ? int.MaxValue : (int)sum;
            // Ever-owned: do not Revoke.
            return true;
        }

        public static void RecordCaratsSpent(PlayerProfile profile, int amount)
        {
            if (profile == null || amount <= 0)
            {
                return;
            }

            long spent = (long)profile.CaratsSpentLifetime + amount;
            profile.CaratsSpentLifetime = spent > int.MaxValue ? int.MaxValue : (int)spent;
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
