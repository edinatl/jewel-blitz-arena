using System.Collections.Generic;
using System.Text;
using JewelBlitzArena.Data;
using JewelBlitzArena.Meta;

namespace JewelBlitzArena.PresentationBridge
{
    /// <summary>
    /// Editor-free ASCII for Tilly's Bench rooms (modes.md §6).
    /// Presentation-only; Buy/Sell bind to ShopRuntime stock/list APIs.
    /// </summary>
    public static class BenchRoomAscii
    {
        private static readonly string[] BandLabels =
        {
            "Tier 1-2",
            "Tier 3-4",
            "Tier 5",
            "Tier 6-7",
            "Materials",
            "Carats",
            "Cosmetic",
            "Nothing",
        };

        private static readonly CosmeticSocket[] FittingSockets =
        {
            CosmeticSocket.Head,
            CosmeticSocket.Arm,
            CosmeticSocket.Waist,
            CosmeticSocket.Leg,
            CosmeticSocket.Back,
        };

        public static string RenderRooms(IReadOnlyList<BenchRoomsRuntime.BenchRoomEntry> rooms)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Tilly's Bench ===");
            if (rooms == null)
            {
                return sb.ToString();
            }

            for (int i = 0; i < rooms.Count; i++)
            {
                BenchRoomsRuntime.BenchRoomEntry r = rooms[i];
                string avail = r.Available ? "" : " [locked]";
                string badge = string.IsNullOrEmpty(r.Badge) ? "" : $"  << {r.Badge}";
                sb.AppendLine($"  {i + 1}. {r.Label}{avail}{badge}");
            }

            return sb.ToString();
        }

        public static string RenderBuy(
            IReadOnlyList<ShopRuntime.StockEntry> stock,
            ContentCatalog? catalog,
            PlayerProfile profile)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Buy ===");
            sb.AppendLine($"Carats: {(profile != null ? profile.Carats : 0)}");
            if (stock == null || stock.Count == 0)
            {
                sb.AppendLine("(no stock)");
                return sb.ToString();
            }

            for (int i = 0; i < stock.Count; i++)
            {
                ShopRuntime.StockEntry e = stock[i];
                string name = $"#{e.ItemId}";
                if (catalog != null && catalog.TryGetItem(e.ItemId, out ItemRecord item))
                {
                    name = item.Name;
                }

                string neu = e.IsNewToProfile ? " NEW" : "";
                sb.AppendLine($"  {i + 1,2}. {name,-20} {e.BuyPrice,5}c{neu}");
            }

            return sb.ToString();
        }

        public static string RenderSell(
            IReadOnlyList<ShopRuntime.SellEntry> list,
            ContentCatalog? catalog,
            PlayerProfile profile)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Sell ===");
            sb.AppendLine($"Carats: {(profile != null ? profile.Carats : 0)}");
            if (list == null || list.Count == 0)
            {
                sb.AppendLine("(nothing to sell)");
                return sb.ToString();
            }

            for (int i = 0; i < list.Count; i++)
            {
                ShopRuntime.SellEntry e = list[i];
                string name = $"#{e.ItemId}";
                if (catalog != null && catalog.TryGetItem(e.ItemId, out ItemRecord item))
                {
                    name = item.Name;
                }

                sb.AppendLine($"  {i + 1,2}. {name,-20} {e.SellPrice,5}c");
            }

            return sb.ToString();
        }

        public static string RenderFitting(PlayerProfile profile, ContentCatalog? catalog)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Fitting Room ===");
            for (int i = 0; i < FittingSockets.Length; i++)
            {
                CosmeticSocket socket = FittingSockets[i];
                int equipped = profile != null ? profile.Fitting.Get(socket) : 0;
                string label = equipped == 0 ? "(empty)" : $"#{equipped}";
                if (equipped != 0 && catalog != null && catalog.TryGetItem(equipped, out ItemRecord item))
                {
                    label = item.Name;
                }

                sb.AppendLine($"  {socket,-5} {label}");
            }

            sb.AppendLine("-- owned cosmetics --");
            if (profile == null || catalog == null)
            {
                sb.AppendLine("(none)");
                return sb.ToString();
            }

            int listed = 0;
            int[] owned = profile.Ledger.ToArray();
            for (int i = 0; i < owned.Length; i++)
            {
                if (!catalog.TryGetItem(owned[i], out ItemRecord cos)
                    || cos.Category != ItemCategory.Cosmetic)
                {
                    continue;
                }

                bool on = profile.Fitting.Get(cos.CosmeticSocket) == cos.ItemId;
                sb.AppendLine($"  {cos.Name,-20} {cos.CosmeticSocket,-5}{(on ? " *" : "")}");
                listed++;
            }

            if (listed == 0)
            {
                sb.AppendLine("(none)");
            }

            return sb.ToString();
        }

        public static string RenderLedger(
            IReadOnlyList<LedgerRuntime.LedgerEntry> entries,
            PlayerProfile profile)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Ledger ===");
            int pocketed = profile != null ? profile.Pockets.Count : 0;
            sb.AppendLine($"Owned: {(entries != null ? entries.Count : 0)}  Pocketed: {pocketed}/{PocketLoadout.MaxSlots}");
            if (entries == null || entries.Count == 0)
            {
                sb.AppendLine("(empty)");
                return sb.ToString();
            }

            for (int i = 0; i < entries.Count; i++)
            {
                LedgerRuntime.LedgerEntry e = entries[i];
                string pocket = e.IsPocketed ? " P" : (e.Pocketable ? " ." : "  ");
                sb.AppendLine($"  {i + 1,2}.{pocket} {e.Name,-20} {e.Category}");
            }

            return sb.ToString();
        }

        public static string RenderTalk(
            PlayerProfile profile,
            ulong visitSeed,
            int lastGiftItemId = 0,
            ContentCatalog? catalog = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Talk ===");
            bool atBench = profile != null && CitrineGiftsRuntime.IsAtBench(profile, visitSeed);
            int chance = profile != null ? CitrineGiftsRuntime.AppearanceChancePermille(profile) : 0;
            sb.AppendLine(atBench
                ? "Citrine is at the bench."
                : "Citrine is away.");
            sb.AppendLine($"Appear chance: {chance}‰");
            bool sol = profile != null && CitrineGiftsRuntime.HasReceivedSolitaire(profile);
            bool dia = profile != null && CitrineGiftsRuntime.HasReceivedDiadem(profile);
            sb.AppendLine($"Gifts: Solitaire={(sol ? "received" : "pending")}  Diadem={(dia ? "received" : "pending")}");
            if (lastGiftItemId != 0)
            {
                string name = $"#{lastGiftItemId}";
                if (catalog != null && catalog.TryGetItem(lastGiftItemId, out ItemRecord gift))
                {
                    name = gift.Name;
                }

                sb.AppendLine($"Last gift: {name}");
            }

            return sb.ToString();
        }

        public static string RenderTinker(
            PlayerProfile profile,
            ContentCatalog? catalog,
            TinkerAttemptResult? lastCraft = null,
            int inputA = 0,
            int inputB = 0)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Tinker ===");
            int chits = profile != null ? profile.Chits : 0;
            int counter = profile != null ? profile.TinkerCounter : 0;
            bool salted = profile != null && profile.ProfileSalt != 0UL;
            sb.AppendLine($"Chits: {chits}  Counter: {counter}  Salt: {(salted ? "set" : "none")}");
            if (inputA != 0 || inputB != 0)
            {
                sb.AppendLine($"Inputs: {ItemLabel(catalog, inputA)} + {ItemLabel(catalog, inputB)}");
            }

            if (lastCraft.HasValue)
            {
                TinkerAttemptResult r = lastCraft.Value;
                sb.Append("Last craft: ");
                sb.Append(r.Outcome);
                if (r.TargetTier > 0)
                {
                    sb.Append("  tier=");
                    sb.Append(r.TargetTier);
                }

                if (r.ResultItemId != 0)
                {
                    sb.Append(" → ");
                    sb.Append(ItemLabel(catalog, r.ResultItemId));
                }

                if (r.ChitsEarned > 0)
                {
                    sb.Append("  +");
                    sb.Append(r.ChitsEarned);
                    sb.Append(" chit");
                }

                if (r.ScrapGranted)
                {
                    sb.Append("  +Scrap");
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static string ItemLabel(ContentCatalog? catalog, int itemId)
        {
            if (itemId == 0)
            {
                return "(none)";
            }

            if (catalog != null && catalog.TryGetItem(itemId, out ItemRecord item))
            {
                return item.Name;
            }

            return $"#{itemId}";
        }

        public static string RenderFacetWheel(PlayerProfile profile) =>
            RenderFacetWheel(profile, lastGrant: null);

        public static string RenderFacetWheel(
            PlayerProfile profile,
            FacetWheelRuntime.SpinGrant? lastGrant)
        {
            var sb = new StringBuilder();
            int width = profile != null ? profile.FacetWheelWidthLevel : 0;
            int chits = profile != null ? profile.Chits : 0;
            sb.AppendLine("=== Facet Wheel ===");
            sb.AppendLine($"Chits: {chits}  (spin={FacetWheelRuntime.SpinCostChits})  width={width}/{FacetWheelRuntime.MaxWidthLevel}");

            var weights = new int[FacetWheelRuntime.BandCount];
            FacetWheelRuntime.BandWeightsPermille(width, weights);
            for (int i = 0; i < FacetWheelRuntime.BandCount; i++)
            {
                int w = weights[i];
                int bars = w / 20;
                if (bars < 0)
                {
                    bars = 0;
                }

                if (bars > 40)
                {
                    bars = 40;
                }

                sb.Append("  ");
                sb.Append(BandLabels[i].PadRight(10));
                sb.Append(' ');
                sb.Append(new string('#', bars));
                sb.Append(' ');
                sb.Append(w);
                sb.AppendLine("‰");
            }

            if (lastGrant.HasValue)
            {
                FacetWheelRuntime.SpinGrant g = lastGrant.Value;
                sb.Append("Last spin: ");
                sb.Append(BandLabels[(int)g.Band]);
                if (g.GrantedItemId != 0)
                {
                    sb.Append(" → item #");
                    sb.Append(g.GrantedItemId);
                }

                if (g.CaratsReward > 0)
                {
                    sb.Append(" → ");
                    sb.Append(g.CaratsReward);
                    sb.Append('c');
                }

                if (g.GrantedItemId == 0 && g.CaratsReward == 0)
                {
                    sb.Append(" → (nothing)");
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
