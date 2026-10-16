using JewelBlitzArena.Sim;

namespace JewelBlitzArena.PresentationBridge
{
    /// <summary>
    /// Presentation-only view of lasting Ice/Smoke/Adhesive floor patches.
    /// Not hashed — reads <see cref="WorldPatchRuntime"/> slots.
    /// </summary>
    public readonly struct WorldPatchHudEntry
    {
        public int Kind { get; }
        public float X { get; }
        public float Z { get; }
        public float Radius { get; }
        public int RemainingFrames { get; }

        public WorldPatchHudEntry(int kind, float x, float z, float radius, int remainingFrames)
        {
            Kind = kind;
            X = x;
            Z = z;
            Radius = radius;
            RemainingFrames = remainingFrames;
        }

        public string KindTag =>
            Kind == WorldPatchRuntime.KindIce ? "ICE"
            : Kind == WorldPatchRuntime.KindSmoke ? "SMK"
            : Kind == WorldPatchRuntime.KindSlow ? "SLW"
            : "???";

        public char BoardGlyph =>
            Kind == WorldPatchRuntime.KindIce ? '~'
            : Kind == WorldPatchRuntime.KindSmoke ? '#'
            : Kind == WorldPatchRuntime.KindSlow ? '='
            : '?';
    }

    public static class MatchWorldPatchHud
    {
        public const int MaxEntries = PoolCaps.GleamPickups;

        /// <summary>Collect active patches in deterministic slot order. Returns count written.</summary>
        public static int Collect(ref MatchState state, WorldPatchHudEntry[] into)
        {
            if (into == null || into.Length == 0)
            {
                return 0;
            }

            int n = 0;
            for (int i = 0; i < PoolCaps.GleamPickups && n < into.Length; i++)
            {
                GenericPoolSlot patch = MatchStateAccess.GetWorldPatch(ref state, i);
                if ((patch.Flags & WorldPatchRuntime.FlagActive) == 0)
                {
                    continue;
                }

                into[n++] = new WorldPatchHudEntry(
                    patch.TypeId,
                    FixDisplay.ToFloat(patch.PosX),
                    FixDisplay.ToFloat(patch.PosZ),
                    FixDisplay.ToFloat(patch.PosY),
                    patch.Param0);
            }

            return n;
        }

        /// <summary>Compact HUD line, e.g. <c>patches=2 ICE@1.0,0.0t120 SMK@2.0,0.0t200</c>.</summary>
        public static string FormatSummary(ref MatchState state, int maxList = 4)
        {
            var buf = new WorldPatchHudEntry[MaxEntries];
            int count = Collect(ref state, buf);
            if (count == 0)
            {
                return string.Empty;
            }

            string s = "patches=" + count;
            int list = count < maxList ? count : maxList;
            for (int i = 0; i < list; i++)
            {
                WorldPatchHudEntry e = buf[i];
                s += " " + e.KindTag + "@"
                    + e.X.ToString("0.#") + "," + e.Z.ToString("0.#")
                    + "t" + e.RemainingFrames;
            }

            if (count > maxList)
            {
                s += " +" + (count - maxList);
            }

            return s;
        }
    }
}
