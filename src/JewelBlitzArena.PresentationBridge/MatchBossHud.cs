using JewelBlitzArena.Sim;

namespace JewelBlitzArena.PresentationBridge
{
    /// <summary>
    /// Presentation-only view of Harvester boss parts and Vitre body on BossSubActors.
    /// Controllers (Buck / Cutters / Overpress) are omitted from the board.
    /// </summary>
    public readonly struct BossHudEntry
    {
        public int KindId { get; }
        public float X { get; }
        public float Z { get; }
        public int Life { get; }
        public bool Broken { get; }
        public bool Repairing { get; }

        public BossHudEntry(int kindId, float x, float z, int life, bool broken, bool repairing)
        {
            KindId = kindId;
            X = x;
            Z = z;
            Life = life;
            Broken = broken;
            Repairing = repairing;
        }

        /// <summary>
        /// Leg 'L', head 'h', tail 't', Vitre 'V'; repairing leg 'r'; broken 'x'.
        /// </summary>
        public char BoardGlyph
        {
            get
            {
                if (Broken)
                {
                    return 'x';
                }

                if (Repairing)
                {
                    return 'r';
                }

                if (BossPartRuntime.IsLeg(KindId))
                {
                    return 'L';
                }

                if (KindId == BossPartRuntime.KindHead)
                {
                    return 'h';
                }

                if (KindId == BossPartRuntime.KindTail)
                {
                    return 't';
                }

                if (KindId == VitreBossRuntime.KindVitreBody)
                {
                    return 'V';
                }

                return '?';
            }
        }

        public string Tag
        {
            get
            {
                string baseTag = KindId switch
                {
                    BossPartRuntime.KindHead => "HEAD",
                    BossPartRuntime.KindTail => "TAIL",
                    VitreBossRuntime.KindVitreBody => "VITRE",
                    _ => BossPartRuntime.IsLeg(KindId) ? "LEG" + KindId : "BOS" + KindId,
                };
                if (Broken)
                {
                    return baseTag + "X";
                }

                if (Repairing)
                {
                    return baseTag + "R";
                }

                return baseTag;
            }
        }
    }

    public static class MatchBossHud
    {
        public const int MaxParts = PoolCaps.BossSubActors;

        public static int Collect(ref MatchState state, BossHudEntry[] into)
        {
            if (into == null || into.Length == 0)
            {
                return 0;
            }

            int n = 0;
            for (int i = 0; i < PoolCaps.BossSubActors && n < into.Length; i++)
            {
                GenericPoolSlot slot = MatchStateAccess.GetBossSub(ref state, i);
                if (slot.TypeId == 0 || !IsBoardPart(slot.TypeId))
                {
                    continue;
                }

                bool broken = (slot.Flags & BossPartRuntime.FlagBroken) != 0;
                bool repairing = (slot.Flags & BossPartRuntime.FlagRepairing) != 0;
                // Vitre clears FlagActive on death but keeps TypeId — treat Life≤0 as broken.
                if (slot.TypeId == VitreBossRuntime.KindVitreBody && slot.Param0 <= 0)
                {
                    broken = true;
                }

                bool active = (slot.Flags & BossPartRuntime.FlagActive) != 0;
                if (!active && !broken && !repairing)
                {
                    continue;
                }

                into[n++] = new BossHudEntry(
                    slot.TypeId,
                    FixDisplay.ToFloat(slot.PosX),
                    FixDisplay.ToFloat(slot.PosZ),
                    slot.Param0,
                    broken,
                    repairing);
            }

            return n;
        }

        public static string FormatSummary(ref MatchState state, int maxList = 6)
        {
            var buf = new BossHudEntry[MaxParts];
            int count = Collect(ref state, buf);
            if (count == 0)
            {
                return "boss=0";
            }

            var sb = new System.Text.StringBuilder(80);
            sb.Append("boss=").Append(count);
            int listed = count < maxList ? count : maxList;
            for (int i = 0; i < listed; i++)
            {
                BossHudEntry e = buf[i];
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

        private static bool IsBoardPart(int kindId) =>
            BossPartRuntime.IsHarvesterPart(kindId) || kindId == VitreBossRuntime.KindVitreBody;
    }
}
