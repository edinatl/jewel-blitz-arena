using JewelBlitzArena.Sim;

namespace JewelBlitzArena.PresentationBridge
{
    /// <summary>
    /// Presentation-only view of armed stage hazard volumes and kinematic tracks.
    /// Not hashed — reads <see cref="MatchState"/> hazard/kinematic pools.
    /// </summary>
    public readonly struct StageHazardHudEntry
    {
        public int HazardId { get; }
        public float X { get; }
        public float Z { get; }
        public int CycleFrames { get; }
        public int Damage { get; }
        public bool Telegraphing { get; }

        public StageHazardHudEntry(
            int hazardId, float x, float z, int cycleFrames, int damage, bool telegraphing)
        {
            HazardId = hazardId;
            X = x;
            Z = z;
            CycleFrames = cycleFrames;
            Damage = damage;
            Telegraphing = telegraphing;
        }

        public char BoardGlyph => Telegraphing ? '!' : 'H';

        public string Tag =>
            "HZD" + HazardId + (Telegraphing ? "T" : string.Empty);
    }

    public readonly struct StageKinematicHudEntry
    {
        public int TrackId { get; }
        public float X { get; }
        public float Z { get; }

        public StageKinematicHudEntry(int trackId, float x, float z)
        {
            TrackId = trackId;
            X = x;
            Z = z;
        }

        public char BoardGlyph => '+';
    }

    public static class MatchStageHazardHud
    {
        public const int MaxHazards = PoolCaps.HazardVolumes;
        public const int MaxKinematics = PoolCaps.KinematicBodies;

        /// <summary>Collect active hazard volumes in slot order. Returns count written.</summary>
        public static int CollectHazards(ref MatchState state, StageHazardHudEntry[] into)
        {
            if (into == null || into.Length == 0)
            {
                return 0;
            }

            int n = 0;
            int limit = state.ActiveHazardCount;
            if (limit > PoolCaps.HazardVolumes)
            {
                limit = PoolCaps.HazardVolumes;
            }

            for (int i = 0; i < limit && n < into.Length; i++)
            {
                GenericPoolSlot slot = MatchStateAccess.GetHazard(ref state, i);
                if ((slot.Flags & StageRuntime.HazardFlagActive) == 0)
                {
                    continue;
                }

                into[n++] = new StageHazardHudEntry(
                    slot.TypeId,
                    FixDisplay.ToFloat(slot.PosX),
                    FixDisplay.ToFloat(slot.PosZ),
                    slot.Param0,
                    slot.Reserved1,
                    (slot.Flags & StageRuntime.HazardFlagTelegraph) != 0);
            }

            return n;
        }

        /// <summary>Collect active kinematic tracks in slot order. Returns count written.</summary>
        public static int CollectKinematics(ref MatchState state, StageKinematicHudEntry[] into)
        {
            if (into == null || into.Length == 0)
            {
                return 0;
            }

            int n = 0;
            int limit = state.ActiveKinematicCount;
            if (limit > PoolCaps.KinematicBodies)
            {
                limit = PoolCaps.KinematicBodies;
            }

            for (int i = 0; i < limit && n < into.Length; i++)
            {
                GenericPoolSlot slot = MatchStateAccess.GetKinematic(ref state, i);
                if ((slot.Flags & StageRuntime.KinematicFlagActive) == 0)
                {
                    continue;
                }

                into[n++] = new StageKinematicHudEntry(
                    slot.TypeId,
                    FixDisplay.ToFloat(slot.PosX),
                    FixDisplay.ToFloat(slot.PosZ));
            }

            return n;
        }

        /// <summary>
        /// Compact hazard line, e.g. <c>hazards=1 HZD2T@0.0,0.0 d80 c120</c>.
        /// Empty when no active hazards.
        /// </summary>
        public static string FormatHazardSummary(ref MatchState state, int maxList = 4)
        {
            var buf = new StageHazardHudEntry[MaxHazards];
            int count = CollectHazards(ref state, buf);
            if (count == 0)
            {
                return string.Empty;
            }

            string s = "hazards=" + count;
            int list = count < maxList ? count : maxList;
            for (int i = 0; i < list; i++)
            {
                StageHazardHudEntry e = buf[i];
                s += " " + e.Tag + "@"
                    + e.X.ToString("0.#") + "," + e.Z.ToString("0.#")
                    + " d" + e.Damage
                    + " c" + e.CycleFrames;
            }

            if (count > maxList)
            {
                s += " +" + (count - maxList);
            }

            return s;
        }

        /// <summary>
        /// Phase / transition / counts line, e.g. <c>phase=1 geom=1 kin=1 TRANS</c>.
        /// </summary>
        public static string FormatPhaseSummary(ref MatchState state)
        {
            string s = "phase=" + state.PhaseIndex
                + " geom=" + state.ActiveGeometrySetId
                + " hazards=" + state.ActiveHazardCount
                + " kin=" + state.ActiveKinematicCount;
            if ((state.StageFlags & StageFlags.InTransition) != 0)
            {
                s += " TRANS";
            }

            if ((state.StageFlags & StageFlags.HazardsEnabled) == 0)
            {
                s += " HZD_OFF";
            }

            return s;
        }
    }
}
