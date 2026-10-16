using System.Collections.Generic;
using System.Text;
using JewelBlitzArena.Data;
using JewelBlitzArena.Meta;

namespace JewelBlitzArena.PresentationBridge
{
    /// <summary>
    /// Editor-free ASCII for Tilly's Bench Buy + Facet Wheel rooms (modes.md §6).
    /// Presentation-only; stock comes from <see cref="ShopRuntime.BuildStock"/>.
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

        public static string RenderFacetWheel(PlayerProfile profile)
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

            return sb.ToString();
        }
    }
}
