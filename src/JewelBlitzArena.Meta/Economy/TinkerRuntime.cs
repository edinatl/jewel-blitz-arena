using System.Collections.Generic;
using JewelBlitzArena.Data;

namespace JewelBlitzArena.Meta
{
    /// <summary>Tinker craft outcome (items-and-economy.md §4).</summary>
    public enum TinkerOutcome : byte
    {
        Failure = 0,
        Success = 1,
        Double = 2,
    }

    /// <summary>Result of one greybox Tinker attempt.</summary>
    public readonly struct TinkerAttemptResult
    {
        public TinkerAttemptResult(
            TinkerOutcome outcome,
            int resultItemId,
            int chitsEarned,
            bool scrapGranted,
            int counterUsed,
            int targetTier = 0)
        {
            Outcome = outcome;
            ResultItemId = resultItemId;
            ChitsEarned = chitsEarned;
            ScrapGranted = scrapGranted;
            CounterUsed = counterUsed;
            TargetTier = targetTier;
        }

        public TinkerOutcome Outcome { get; }
        public int ResultItemId { get; }
        public int ChitsEarned { get; }
        public bool ScrapGranted { get; }
        public int CounterUsed { get; }
        public int TargetTier { get; }
    }

    /// <summary>
    /// Tilly's Bench Tinker: binary mix → success/double/failure.
    /// With a catalog, success picks from the tag-affinity rarity pool (economy §4).
    /// Failure pays Scrap + Chit. Seeded by hash(profile_salt, tinker_counter, A, B, card).
    /// </summary>
    public static class TinkerRuntime
    {
        public const int ScrapItemId = 900101;
        public const int SuccessPercent = 55;
        public const int DoublePercent = 15;
        public const int FailurePercent = 30;
        public const int DoubleExtraSellCarats = 200;
        public const int ResultItemBaseId = 930000;
        public const int MaxTargetTier = 7;
        public const int MaxTargetTierWithoutBlueprint = 6;

        /// <summary>Ensure a non-zero profile salt for deterministic craft seeds.</summary>
        public static void EnsureSalt(PlayerProfile profile)
        {
            if (profile == null)
            {
                return;
            }

            if (profile.ProfileSalt != 0UL)
            {
                return;
            }

            ulong salt = 0x54494C4C59534C54UL; // TILLYSLT
            string name = profile.DisplayName ?? string.Empty;
            for (int i = 0; i < name.Length; i++)
            {
                salt = Mix(salt ^ ((ulong)(uint)name[i] << (i & 15)));
            }

            profile.ProfileSalt = salt == 0UL ? 1UL : salt;
        }

        public static ulong CraftSeed(ulong profileSalt, int tinkerCounter, int inputA, int inputB, int card)
        {
            ulong x = profileSalt;
            x ^= (ulong)(uint)tinkerCounter * 0x9E3779B97F4A7C15UL;
            x ^= (ulong)(uint)inputA << 1;
            x ^= (ulong)(uint)inputB << 17;
            x ^= (ulong)(uint)card << 33;
            return Mix(x);
        }

        public static TinkerOutcome ResolveOutcome(ulong craftSeed)
        {
            int roll = (int)(craftSeed % 100UL);
            if (roll < SuccessPercent)
            {
                return TinkerOutcome.Success;
            }

            if (roll < SuccessPercent + DoublePercent)
            {
                return TinkerOutcome.Double;
            }

            return TinkerOutcome.Failure;
        }

        public static int ResultItemIdFor(int inputA, int inputB, int card) =>
            ResultItemBaseId + (((inputA * 31) + (inputB * 17) + (card * 13)) & 0xFF);

        public static ItemTag AffinitySet(
            ItemRecord? a,
            ItemRecord? b,
            ItemTag cardTags,
            out bool sharedTag)
        {
            ItemTag set = ItemTag.None;
            if (a != null)
            {
                set |= a.Tags;
            }

            if (b != null)
            {
                set |= b.Tags;
            }

            sharedTag = a != null && b != null && (a.Tags & b.Tags) != ItemTag.None;
            if (cardTags != ItemTag.None)
            {
                if ((set & cardTags) != ItemTag.None)
                {
                    sharedTag = true;
                }

                set |= cardTags;
            }

            return set;
        }

        public static int TargetTier(
            ItemRecord? a,
            ItemRecord? b,
            bool sharedTag,
            bool blueprintScrap)
        {
            int rarityA = a?.Rarity ?? 1;
            int rarityB = b?.Rarity ?? 1;
            int baseTier = (rarityA + rarityB + 1) / 2;
            int shift = sharedTag ? 1 : 0;
            if (blueprintScrap)
            {
                shift++;
            }

            int target = baseTier + shift;
            int max = blueprintScrap ? MaxTargetTier : MaxTargetTierWithoutBlueprint;
            if (target < 1)
            {
                target = 1;
            }

            if (target > max)
            {
                target = max;
            }

            return target;
        }

        public static bool IsPoolEligible(ItemRecord item) =>
            item != null
            && item.Category != ItemCategory.Gift
            && item.Category != ItemCategory.Card
            && item.Category != ItemCategory.StageWeapon
            && item.Category != ItemCategory.Cosmetic
            && item.ItemId != ScrapItemId;

        /// <summary>
        /// Build weighted pool of catalog items at <paramref name="rarity"/> intersecting
        /// <paramref name="affinity"/>. Weight = popcount(intersection).
        /// </summary>
        public static void BuildPool(
            ContentCatalog catalog,
            ItemTag affinity,
            int rarity,
            List<int> itemIds,
            List<int> weights)
        {
            itemIds.Clear();
            weights.Clear();
            if (catalog == null || affinity == ItemTag.None)
            {
                return;
            }

            foreach (ItemRecord item in catalog.Items)
            {
                if (!IsPoolEligible(item) || item.Rarity != rarity)
                {
                    continue;
                }

                ItemTag hit = item.Tags & affinity;
                if (hit == ItemTag.None)
                {
                    continue;
                }

                int w = PopCount((ushort)hit);
                if (w <= 0)
                {
                    continue;
                }

                itemIds.Add(item.ItemId);
                weights.Add(w);
            }
        }

        /// <summary>
        /// Pick from affinity pool at target tier; step down if empty. Returns 0 if none.
        /// </summary>
        public static int PickFromAffinityPool(
            ContentCatalog catalog,
            ItemTag affinity,
            int targetTier,
            ulong seed)
        {
            var ids = new List<int>();
            var weights = new List<int>();
            for (int tier = targetTier; tier >= 1; tier--)
            {
                BuildPool(catalog, affinity, tier, ids, weights);
                if (ids.Count == 0)
                {
                    continue;
                }

                int total = 0;
                for (int i = 0; i < weights.Count; i++)
                {
                    total += weights[i];
                }

                int roll = (int)(Mix(seed ^ (ulong)(uint)tier) % (ulong)(uint)total);
                int acc = 0;
                for (int i = 0; i < ids.Count; i++)
                {
                    acc += weights[i];
                    if (roll < acc)
                    {
                        return ids[i];
                    }
                }

                return ids[ids.Count - 1];
            }

            return 0;
        }

        /// <summary>Catalog-aware craft (tag-affinity pool on success).</summary>
        public static TinkerAttemptResult TryCraft(
            PlayerProfile profile,
            ContentCatalog catalog,
            int inputA,
            int inputB,
            int cardItemId = 0)
        {
            if (profile == null)
            {
                return new TinkerAttemptResult(TinkerOutcome.Failure, 0, 0, false, 0);
            }

            EnsureSalt(profile);
            int counterUsed = profile.TinkerCounter;
            if (profile.TinkerCounter < int.MaxValue)
            {
                profile.TinkerCounter++;
            }

            profile.SetFlag(ProfileFlags.TouchedTinkerStub);

            ulong seed = CraftSeed(profile.ProfileSalt, counterUsed, inputA, inputB, cardItemId);
            TinkerOutcome outcome = ResolveOutcome(seed);

            ItemRecord? a = null;
            ItemRecord? b = null;
            ItemRecord? card = null;
            if (catalog != null)
            {
                catalog.TryGetItem(inputA, out a);
                catalog.TryGetItem(inputB, out b);
                if (cardItemId > 0)
                {
                    catalog.TryGetItem(cardItemId, out card);
                }
            }

            ItemTag cardTags = card?.Tags ?? ItemTag.None;
            bool blueprint = card != null
                && card.Category == ItemCategory.Card
                && (card.Name.IndexOf("Blueprint", System.StringComparison.OrdinalIgnoreCase) >= 0);
            ItemTag affinity = AffinitySet(a, b, cardTags, out bool shared);
            int tier = TargetTier(a, b, shared, blueprint);

            if (outcome == TinkerOutcome.Failure)
            {
                // Empty pool at tier 1 forces failure (economy §4) — already on failure path.
                if (profile.Chits < int.MaxValue)
                {
                    profile.Chits++;
                }

                profile.Ledger.Grant(ScrapItemId);
                return new TinkerAttemptResult(
                    outcome, 0, chitsEarned: 1, scrapGranted: true, counterUsed, tier);
            }

            int resultId = 0;
            if (catalog != null && affinity != ItemTag.None)
            {
                resultId = PickFromAffinityPool(catalog, affinity, tier, seed);
            }

            if (resultId == 0)
            {
                // No intersecting pool → failure (economy §4 empty tier-1).
                if (profile.Chits < int.MaxValue)
                {
                    profile.Chits++;
                }

                profile.Ledger.Grant(ScrapItemId);
                return new TinkerAttemptResult(
                    TinkerOutcome.Failure, 0, chitsEarned: 1, scrapGranted: true, counterUsed, tier);
            }

            bool firstGrant = profile.Ledger.Grant(resultId);
            if (outcome == TinkerOutcome.Double)
            {
                int sell = DoubleExtraSellCarats;
                if (catalog != null && catalog.TryGetItem(resultId, out ItemRecord resultItem)
                    && resultItem.SellPrice > 0)
                {
                    sell = resultItem.SellPrice;
                }

                if (!firstGrant || !profile.Ledger.Grant(resultId))
                {
                    long sum = (long)profile.Carats + sell;
                    profile.Carats = sum > int.MaxValue ? int.MaxValue : (int)sum;
                }
            }

            return new TinkerAttemptResult(
                outcome, resultId, chitsEarned: 0, scrapGranted: false, counterUsed, tier);
        }

        /// <summary>
        /// Catalog-free craft (stub result ids). Kept for tests that do not load S5.
        /// </summary>
        public static TinkerAttemptResult TryCraft(
            PlayerProfile profile,
            int inputA,
            int inputB,
            int card = 0)
        {
            if (profile == null)
            {
                return new TinkerAttemptResult(TinkerOutcome.Failure, 0, 0, false, 0);
            }

            EnsureSalt(profile);
            int counterUsed = profile.TinkerCounter;
            if (profile.TinkerCounter < int.MaxValue)
            {
                profile.TinkerCounter++;
            }

            profile.SetFlag(ProfileFlags.TouchedTinkerStub);

            ulong seed = CraftSeed(profile.ProfileSalt, counterUsed, inputA, inputB, card);
            TinkerOutcome outcome = ResolveOutcome(seed);

            if (outcome == TinkerOutcome.Failure)
            {
                if (profile.Chits < int.MaxValue)
                {
                    profile.Chits++;
                }

                profile.Ledger.Grant(ScrapItemId);
                return new TinkerAttemptResult(outcome, 0, chitsEarned: 1, scrapGranted: true, counterUsed);
            }

            int resultId = ResultItemIdFor(inputA, inputB, card);
            bool firstGrant = profile.Ledger.Grant(resultId);
            if (outcome == TinkerOutcome.Double)
            {
                if (!firstGrant || !profile.Ledger.Grant(resultId))
                {
                    long sum = (long)profile.Carats + DoubleExtraSellCarats;
                    profile.Carats = sum > int.MaxValue ? int.MaxValue : (int)sum;
                }
            }

            return new TinkerAttemptResult(
                outcome, resultId, chitsEarned: 0, scrapGranted: false, counterUsed);
        }

        private static int PopCount(ushort value)
        {
            int n = 0;
            while (value != 0)
            {
                value = (ushort)(value & (value - 1));
                n++;
            }

            return n;
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
