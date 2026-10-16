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

            // Stage hazards (H idle / ! telegraph) and kinematics (+) under patches/actors.
            var hazards = new StageHazardHudEntry[MatchStageHazardHud.MaxHazards];
            int hazardCount = MatchStageHazardHud.CollectHazards(ref state, hazards);
            for (int i = 0; i < hazardCount; i++)
            {
                Plot(hazards[i].X, hazards[i].Z, hazards[i].BoardGlyph);
            }

            var kinematics = new StageKinematicHudEntry[MatchStageHazardHud.MaxKinematics];
            int kinematicCount = MatchStageHazardHud.CollectKinematics(ref state, kinematics);
            for (int i = 0; i < kinematicCount; i++)
            {
                Plot(kinematics[i].X, kinematics[i].Z, kinematics[i].BoardGlyph);
            }

            // Vein Guards (G active / x broken) under patches/actors.
            var creatures = new StageCreatureHudEntry[MatchStageCreatureHud.MaxCreatures];
            int creatureCount = MatchStageCreatureHud.Collect(ref state, creatures);
            for (int i = 0; i < creatureCount; i++)
            {
                Plot(creatures[i].X, creatures[i].Z, creatures[i].BoardGlyph);
            }

            // World patches under actors/items (Ice ~, Smoke #, Slow =).
            var patches = new WorldPatchHudEntry[MatchWorldPatchHud.MaxEntries];
            int patchCount = MatchWorldPatchHud.Collect(ref state, patches);
            for (int i = 0; i < patchCount; i++)
            {
                Plot(patches[i].X, patches[i].Z, patches[i].BoardGlyph);
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
                .Append(" patches=").Append(patchCount)
                .Append(" hazards=").Append(hazardCount)
                .Append(" kin=").Append(kinematicCount)
                .Append(" creatures=").Append(creatureCount);
            if (state.MatchOver)
            {
                sb.Append(" MATCH_OVER");
            }

            sb.AppendLine();

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
