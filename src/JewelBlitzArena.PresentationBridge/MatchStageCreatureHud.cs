using JewelBlitzArena.Sim;

namespace JewelBlitzArena.PresentationBridge
{
    /// <summary>
    /// Presentation-only view of armed stage creatures (Vein Guards).
    /// Not hashed — reads <see cref="MatchState"/> StageCreatures pool.
    /// </summary>
    public readonly struct StageCreatureHudEntry
    {
        public int KindId { get; }
        public float X { get; }
        public float Z { get; }
        public int Life { get; }
        public bool Broken { get; }

        public StageCreatureHudEntry(int kindId, float x, float z, int life, bool broken)
        {
            KindId = kindId;
            X = x;
            Z = z;
            Life = life;
            Broken = broken;
        }

        /// <summary>Active guard 'G'; broken husk 'x'.</summary>
        public char BoardGlyph => Broken ? 'x' : 'G';

        public string Tag => Broken ? "CRT" + KindId + "X" : "CRT" + KindId;
    }

    public static class MatchStageCreatureHud
    {
        public const int MaxCreatures = PoolCaps.StageCreatures;

        /// <summary>Collect armed (active or broken) stage creatures. Returns count written.</summary>
        public static int Collect(ref MatchState state, StageCreatureHudEntry[] into)
        {
            if (into == null || into.Length == 0)
            {
                return 0;
            }

            int n = 0;
            for (int i = 0; i < PoolCaps.StageCreatures && n < into.Length; i++)
            {
                GenericPoolSlot slot = MatchStateAccess.GetStageCreature(ref state, i);
                if (slot.TypeId == 0)
                {
                    continue;
                }

                bool active = (slot.Flags & StageCreatureRuntime.FlagActive) != 0;
                bool broken = (slot.Flags & StageCreatureRuntime.FlagBroken) != 0;
                if (!active && !broken)
                {
                    continue;
                }

                into[n++] = new StageCreatureHudEntry(
                    slot.TypeId,
                    FixDisplay.ToFloat(slot.PosX),
                    FixDisplay.ToFloat(slot.PosZ),
                    slot.Param0,
                    broken);
            }

            return n;
        }

        public static string FormatSummary(ref MatchState state, int maxList = 5)
        {
            var buf = new StageCreatureHudEntry[MaxCreatures];
            int count = Collect(ref state, buf);
            if (count == 0)
            {
                return "creatures=0";
            }

            var sb = new System.Text.StringBuilder(64);
            sb.Append("creatures=").Append(count);
            int listed = count < maxList ? count : maxList;
            for (int i = 0; i < listed; i++)
            {
                StageCreatureHudEntry e = buf[i];
                sb.Append(' ').Append(e.Tag)
                    .Append('@').Append(e.X.ToString("0.#"))
                    .Append(',').Append(e.Z.ToString("0.#"));
            }

            if (count > maxList)
            {
                sb.Append(" …");
            }

            return sb.ToString();
        }
    }
}
