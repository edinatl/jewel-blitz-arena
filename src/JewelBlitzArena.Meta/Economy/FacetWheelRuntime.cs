using System.Collections.Generic;
using JewelBlitzArena.Data;

namespace JewelBlitzArena.Meta
{
    /// <summary>Facet Wheel bands (items-and-economy.md §5).</summary>
    public enum FacetWheelBand : byte
    {
        Tier12 = 0,
        Tier34 = 1,
        Tier5 = 2,
        Tier67 = 3,
        Materials = 4,
        Carats = 5,
        Cosmetic = 6,
        Nothing = 7,
    }

    /// <summary>
    /// Tilly's Facet Wheel greybox: 3 Chits per spin; width level widens high tiers.
    /// Deterministic from seed — never a mystery box (weights are inspectable).
    /// </summary>
    public static class FacetWheelRuntime
    {
        public const int SpinCostChits = 3;
        public const int BandCount = 8;
        public const int MaxWidthLevel = 5;
        public const int CaratsRewardMin = 500;
        public const int CaratsRewardMax = 3000;

        public readonly struct SpinGrant
        {
            public SpinGrant(FacetWheelBand band, int caratsReward, int grantedItemId)
            {
                Band = band;
                CaratsReward = caratsReward;
                GrantedItemId = grantedItemId;
            }

            public FacetWheelBand Band { get; }
            public int CaratsReward { get; }
            /// <summary>Catalog item granted (0 = none / carats-only / nothing).</summary>
            public int GrantedItemId { get; }
        }

        /// <summary>Cumulative carat cost to reach each width level (economy §5).</summary>
        public static readonly int[] WidthLevelCumulativeCost =
        {
            0, 2000, 6000, 15000, 30000, 60000,
        };

        /// <summary>
        /// Default band weights (permille). Width level shifts weight from Nothing/Tier12
        /// into Tier67/Cosmetic.
        /// </summary>
        public static void BandWeightsPermille(int widthLevel, int[] into)
        {
            if (into == null || into.Length < BandCount)
            {
                return;
            }

            int level = widthLevel;
            if (level < 0)
            {
                level = 0;
            }

            if (level > MaxWidthLevel)
            {
                level = MaxWidthLevel;
            }

            // Base favors low tiers + nothing.
            into[(int)FacetWheelBand.Tier12] = 280;
            into[(int)FacetWheelBand.Tier34] = 180;
            into[(int)FacetWheelBand.Tier5] = 80;
            into[(int)FacetWheelBand.Tier67] = 40;
            into[(int)FacetWheelBand.Materials] = 100;
            into[(int)FacetWheelBand.Carats] = 100;
            into[(int)FacetWheelBand.Cosmetic] = 40;
            into[(int)FacetWheelBand.Nothing] = 180;

            // Each width level: −20 Nothing, −10 Tier12 → +15 Tier67, +15 Cosmetic.
            int shift = level * 10;
            into[(int)FacetWheelBand.Nothing] -= shift * 2;
            into[(int)FacetWheelBand.Tier12] -= shift;
            into[(int)FacetWheelBand.Tier67] += (shift * 3) / 2;
            into[(int)FacetWheelBand.Cosmetic] += (shift * 3) / 2;

            int sum = 0;
            for (int i = 0; i < BandCount; i++)
            {
                if (into[i] < 0)
                {
                    into[i] = 0;
                }

                sum += into[i];
            }

            if (sum != 1000 && sum > 0)
            {
                // Nudge Nothing so weights sum to 1000.
                into[(int)FacetWheelBand.Nothing] += 1000 - sum;
                if (into[(int)FacetWheelBand.Nothing] < 0)
                {
                    into[(int)FacetWheelBand.Nothing] = 0;
                }
            }
        }

        public static FacetWheelBand Spin(ulong seed, int widthLevel, out int caratsReward)
        {
            var weights = new int[BandCount];
            BandWeightsPermille(widthLevel, weights);
            ulong x = Mix(seed);
            int roll = (int)(x % 1000UL);
            int acc = 0;
            for (int i = 0; i < BandCount; i++)
            {
                acc += weights[i];
                if (roll < acc)
                {
                    caratsReward = i == (int)FacetWheelBand.Carats
                        ? CaratsFromSeed(x)
                        : 0;
                    return (FacetWheelBand)i;
                }
            }

            caratsReward = 0;
            return FacetWheelBand.Nothing;
        }

        /// <summary>Spend Chits and apply carat rewards (no item grants).</summary>
        public static bool TrySpin(
            PlayerProfile profile, ulong seed, out FacetWheelBand band, out int caratsReward)
        {
            bool ok = TrySpin(profile, catalog: null, seed, out SpinGrant grant);
            band = grant.Band;
            caratsReward = grant.CaratsReward;
            return ok;
        }

        /// <summary>
        /// Host spin: spend Chits, apply band reward (carats and/or catalog item grant).
        /// Item pick is deterministic from seed; empty pools grant nothing for that band.
        /// Duplicate cosmetics pay sell price instead (capacity 1).
        /// </summary>
        public static bool TrySpin(
            PlayerProfile profile,
            ContentCatalog? catalog,
            ulong seed,
            out SpinGrant grant)
        {
            grant = new SpinGrant(FacetWheelBand.Nothing, 0, 0);
            if (profile == null || profile.Chits < SpinCostChits)
            {
                return false;
            }

            profile.Chits -= SpinCostChits;
            FacetWheelBand band = Spin(seed, profile.FacetWheelWidthLevel, out int bandCarats);
            int grantedItemId = 0;
            int caratsPaid = 0;

            if (band == FacetWheelBand.Carats && bandCarats > 0)
            {
                long sum = (long)profile.Carats + bandCarats;
                profile.Carats = sum > int.MaxValue ? int.MaxValue : (int)sum;
                caratsPaid = bandCarats;
            }
            else if (catalog != null
                && band != FacetWheelBand.Nothing
                && band != FacetWheelBand.Carats)
            {
                ulong x = Mix(seed ^ 0x4752414E54UL);
                int pick = PickGrantItem(catalog, band, x);
                if (pick != 0 && catalog.TryGetItem(pick, out ItemRecord item))
                {
                    if (item.Category == ItemCategory.Cosmetic && profile.Ledger.Owns(pick))
                    {
                        long sum = (long)profile.Carats + item.SellPrice;
                        profile.Carats = sum > int.MaxValue ? int.MaxValue : (int)sum;
                        caratsPaid = item.SellPrice;
                    }
                    else
                    {
                        profile.Ledger.Grant(pick);
                        grantedItemId = pick;
                    }
                }
            }

            profile.SetFlag(ProfileFlags.TouchedFacetWheel);
            grant = new SpinGrant(band, caratsPaid, grantedItemId);
            return true;
        }

        private static int PickGrantItem(
            ContentCatalog catalog,
            FacetWheelBand band,
            ulong x)
        {
            var pool = new List<int>();
            switch (band)
            {
                case FacetWheelBand.Tier12:
                    CollectTier(catalog, 1, 2, pool);
                    break;
                case FacetWheelBand.Tier34:
                    CollectTier(catalog, 3, 4, pool);
                    break;
                case FacetWheelBand.Tier5:
                    CollectTier(catalog, 5, 5, pool);
                    break;
                case FacetWheelBand.Tier67:
                    CollectTier(catalog, 6, 7, pool);
                    break;
                case FacetWheelBand.Materials:
                    foreach (ItemRecord item in catalog.Items)
                    {
                        if (item.Category == ItemCategory.Material)
                        {
                            pool.Add(item.ItemId);
                        }
                    }

                    break;
                case FacetWheelBand.Cosmetic:
                    foreach (ItemRecord item in catalog.Items)
                    {
                        if (item.Category == ItemCategory.Cosmetic)
                        {
                            pool.Add(item.ItemId);
                        }
                    }

                    break;
                default:
                    return 0;
            }

            if (pool.Count == 0)
            {
                return 0;
            }

            pool.Sort();
            int pick = (int)(x % (ulong)(uint)pool.Count);
            return pool[pick];
        }

        private static void CollectTier(ContentCatalog catalog, int rarityMin, int rarityMax, List<int> pool)
        {
            foreach (ItemRecord item in catalog.Items)
            {
                if (item.Rarity < rarityMin || item.Rarity > rarityMax)
                {
                    continue;
                }

                if (item.Category == ItemCategory.StageWeapon
                    || item.Category == ItemCategory.Gift
                    || item.Category == ItemCategory.Material
                    || item.Category == ItemCategory.Card
                    || item.Category == ItemCategory.Cosmetic)
                {
                    continue;
                }

                pool.Add(item.ItemId);
            }
        }

        public static int CostToReachWidthLevel(int level)
        {
            if (level < 0)
            {
                return 0;
            }

            if (level > MaxWidthLevel)
            {
                level = MaxWidthLevel;
            }

            return WidthLevelCumulativeCost[level];
        }

        /// <summary>Spend carats to raise width by one level. Returns false if unaffordable / maxed.</summary>
        public static bool TryWiden(PlayerProfile profile)
        {
            if (profile == null)
            {
                return false;
            }

            int next = profile.FacetWheelWidthLevel + 1;
            if (next > MaxWidthLevel)
            {
                return false;
            }

            int need = WidthLevelCumulativeCost[next] - WidthLevelCumulativeCost[profile.FacetWheelWidthLevel];
            if (profile.Carats < need)
            {
                return false;
            }

            profile.Carats -= need;
            ShopRuntime.RecordCaratsSpent(profile, need);
            profile.FacetWheelWidthLevel = next;
            return true;
        }

        private static int CaratsFromSeed(ulong x)
        {
            int span = CaratsRewardMax - CaratsRewardMin;
            return CaratsRewardMin + (int)((x >> 17) % (ulong)(span + 1));
        }

        private static ulong Mix(ulong x)
        {
            x ^= x >> 33;
            x *= 0xff51afd7ed558ccdUL;
            x ^= x >> 33;
            x *= 0xc4ceb9fe1a85ec53UL;
            x ^= x >> 33;
            return x;
        }
    }
}
