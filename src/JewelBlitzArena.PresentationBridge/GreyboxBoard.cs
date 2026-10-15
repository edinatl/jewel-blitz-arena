using System.Text;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.PresentationBridge
{
    /// <summary>
    /// Headless ASCII top-down view of actors/jewels/items for greybox smoke without Unity.
    /// </summary>
    public static class GreyboxBoard
    {
        public static string Render(ref MatchState state, int width = 41, int height = 21)
        {
            char[,] grid = new char[height, width];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    grid[y, x] = '.';
                }
            }

            float half = FixDisplay.ToFloat(PhysicsConstants.ArenaHalfExtent);
            float span = half * 2f;
            if (span < 0.001f)
            {
                span = 1f;
            }

            void Plot(float worldX, float worldZ, char glyph)
            {
                int col = (int)(((worldX + half) / span) * (width - 1));
                int row = (int)(((half - worldZ) / span) * (height - 1));
                if (col < 0 || col >= width || row < 0 || row >= height)
                {
                    return;
                }

                grid[row, col] = glyph;
            }

            for (int i = 0; i < PoolCaps.Jewels; i++)
            {
                JewelSlot jewel = MatchStateAccess.GetJewel(ref state, i);
                if (!jewel.IsOnField)
                {
                    continue;
                }

                Plot(FixDisplay.ToFloat(jewel.PosX), FixDisplay.ToFloat(jewel.PosZ), '*');
            }

            for (int i = 0; i < PoolCaps.LooseItems; i++)
            {
                LooseItemSlot item = MatchStateAccess.GetLooseItem(ref state, i);
                if (!item.IsActive)
                {
                    continue;
                }

                if (!item.IsOnField && !item.InFlight)
                {
                    continue;
                }

                Plot(FixDisplay.ToFloat(item.PosX), FixDisplay.ToFloat(item.PosZ), 'o');
            }

            char[] actorGlyphs = { 'A', 'B', 'C', 'D' };
            for (int i = 0; i < state.ActorCount; i++)
            {
                ActorSlot actor = MatchStateAccess.Actor(ref state, i);
                if (!actor.IsAlive)
                {
                    continue;
                }

                Plot(FixDisplay.ToFloat(actor.PosX), FixDisplay.ToFloat(actor.PosZ), actorGlyphs[i]);
            }

            var sb = new StringBuilder(width * (height + 1) + 128);
            sb.Append("frame=").Append(state.Frame)
                .Append(" jewels=").Append(state.ActiveJewelCount)
                .Append(" items=").Append(state.ActiveLooseItemCount)
                .AppendLine();

            for (int i = 0; i < state.ActorCount; i++)
            {
                ActorSlot actor = MatchStateAccess.Actor(ref state, i);
                sb.Append(actorGlyphs[i]).Append(": life=").Append(actor.Life)
                    .Append(" jewels=").Append(actor.JewelsHeld)
                    .Append(" luster=").Append(actor.Luster)
                    .Append(actor.IsBlitzed ? " BLITZ" : string.Empty)
                    .AppendLine();
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    sb.Append(grid[y, x]);
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
