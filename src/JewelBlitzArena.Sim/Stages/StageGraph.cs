using JewelBlitzArena.Data;

namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Compiled phase graph for one match. Immutable after <see cref="Simulation.Reset"/>;
    /// not part of the hashed snapshot (runtime counters live on <see cref="MatchState"/>).
    /// </summary>
    public sealed class StageGraph
    {
        public static readonly StageGraph Empty = new StageGraph(
            stageId: 0,
            fallIsKo: false,
            jewelCapOverride: 0,
            jewelCrateChancePercent: 8,
            phases: System.Array.Empty<StagePhaseDef>());

        public StageGraph(
            int stageId,
            bool fallIsKo,
            int jewelCapOverride,
            StagePhaseDef[] phases,
            int jewelCrateChancePercent = 8)
        {
            StageId = stageId;
            FallIsKo = fallIsKo;
            JewelCapOverride = jewelCapOverride;
            JewelCrateChancePercent = jewelCrateChancePercent < 0
                ? 0
                : (jewelCrateChancePercent > 100 ? 100 : jewelCrateChancePercent);
            Phases = phases ?? System.Array.Empty<StagePhaseDef>();
        }

        public int StageId { get; }
        public bool FallIsKo { get; }
        public int JewelCapOverride { get; }
        /// <summary>Percent chance a broken crate drops a jewel (combat-and-match.md). Not hashed.</summary>
        public int JewelCrateChancePercent { get; }
        public StagePhaseDef[] Phases { get; }

        public bool TryGetPhase(int index, out StagePhaseDef phase)
        {
            if (index < 0 || index >= Phases.Length)
            {
                phase = default;
                return false;
            }

            phase = Phases[index];
            return true;
        }

        public static StageGraph FromRecord(StageRecord record)
        {
            if (record == null || record.Phases == null || record.Phases.Length == 0)
            {
                return Empty;
            }

            var phases = new StagePhaseDef[record.Phases.Length];
            for (int i = 0; i < record.Phases.Length; i++)
            {
                StagePhaseRecord src = record.Phases[i];
                var hazards = new StageHazardDef[src.Hazards.Length];
                for (int h = 0; h < src.Hazards.Length; h++)
                {
                    StageHazardRecord hr = src.Hazards[h];
                    hazards[h] = new StageHazardDef(
                        hr.HazardId,
                        (StageHazardTrigger)(byte)hr.Trigger,
                        hr.PeriodFrames,
                        hr.TelegraphFrames,
                        hr.Damage,
                        new Fix(hr.CenterXRaw),
                        new Fix(hr.CenterYRaw),
                        new Fix(hr.CenterZRaw),
                        new Fix(hr.RadiusRaw));
                }

                var kinematics = new StageKinematicDef[src.Kinematics.Length];
                for (int k = 0; k < src.Kinematics.Length; k++)
                {
                    StageKinematicRecord kr = src.Kinematics[k];
                    kinematics[k] = new StageKinematicDef(
                        kr.TrackId,
                        kr.PeriodFrames,
                        new Fix(kr.AmplitudeRaw),
                        new Fix(kr.OriginXRaw),
                        new Fix(kr.OriginYRaw),
                        new Fix(kr.OriginZRaw));
                }

                StageCratePointRecord[] srcCrates =
                    src.CratePoints ?? System.Array.Empty<StageCratePointRecord>();
                var cratePoints = new StageCratePointDef[srcCrates.Length];
                for (int c = 0; c < srcCrates.Length; c++)
                {
                    StageCratePointRecord cr = srcCrates[c];
                    cratePoints[c] = new StageCratePointDef(new Fix(cr.XRaw), new Fix(cr.ZRaw));
                }

                StageWaterVolumeRecord[] srcWaters =
                    src.WaterVolumes ?? System.Array.Empty<StageWaterVolumeRecord>();
                var waterVolumes = new StageWaterVolumeDef[srcWaters.Length];
                for (int w = 0; w < srcWaters.Length; w++)
                {
                    StageWaterVolumeRecord wr = srcWaters[w];
                    waterVolumes[w] = new StageWaterVolumeDef(
                        new Fix(wr.CenterXRaw),
                        new Fix(wr.CenterZRaw),
                        new Fix(wr.RadiusRaw));
                }

                phases[i] = new StagePhaseDef(
                    src.DurationFrames,
                    src.TransitionFrames,
                    src.GeometrySetId,
                    src.BlitzSuppressed,
                    (StageCarryItems)(byte)src.CarryItems,
                    (StageCarryJewels)(byte)src.CarryJewels,
                    src.CarryJewelsDropPermille,
                    hazards,
                    kinematics,
                    cratePoints,
                    waterVolumes);
            }

            return new StageGraph(
                record.StageId,
                record.FallIsKo,
                record.JewelCapOverride,
                phases,
                record.JewelCrateChancePercent);
        }
    }

    public readonly struct StagePhaseDef
    {
        public StagePhaseDef(
            int durationFrames,
            int transitionFrames,
            int geometrySetId,
            bool blitzSuppressed,
            StageCarryItems carryItems,
            StageCarryJewels carryJewels,
            int carryJewelsDropPermille,
            StageHazardDef[] hazards,
            StageKinematicDef[] kinematics,
            StageCratePointDef[] cratePoints,
            StageWaterVolumeDef[] waterVolumes)
        {
            DurationFrames = durationFrames;
            TransitionFrames = transitionFrames;
            GeometrySetId = geometrySetId;
            BlitzSuppressed = blitzSuppressed;
            CarryItems = carryItems;
            CarryJewels = carryJewels;
            CarryJewelsDropPermille = carryJewelsDropPermille;
            Hazards = hazards ?? System.Array.Empty<StageHazardDef>();
            Kinematics = kinematics ?? System.Array.Empty<StageKinematicDef>();
            CratePoints = cratePoints ?? System.Array.Empty<StageCratePointDef>();
            WaterVolumes = waterVolumes ?? System.Array.Empty<StageWaterVolumeDef>();
        }

        public int DurationFrames { get; }
        public int TransitionFrames { get; }
        public int GeometrySetId { get; }
        public bool BlitzSuppressed { get; }
        public StageCarryItems CarryItems { get; }
        public StageCarryJewels CarryJewels { get; }
        public int CarryJewelsDropPermille { get; }
        public StageHazardDef[] Hazards { get; }
        public StageKinematicDef[] Kinematics { get; }
        public StageCratePointDef[] CratePoints { get; }
        public StageWaterVolumeDef[] WaterVolumes { get; }
    }

    public readonly struct StageCratePointDef
    {
        public StageCratePointDef(Fix x, Fix z)
        {
            X = x;
            Z = z;
        }

        public Fix X { get; }
        public Fix Z { get; }
    }

    public readonly struct StageWaterVolumeDef
    {
        public StageWaterVolumeDef(Fix centerX, Fix centerZ, Fix radius)
        {
            CenterX = centerX;
            CenterZ = centerZ;
            Radius = radius;
        }

        public Fix CenterX { get; }
        public Fix CenterZ { get; }
        public Fix Radius { get; }
    }

    public readonly struct StageHazardDef
    {
        public StageHazardDef(
            int hazardId,
            StageHazardTrigger trigger,
            int periodFrames,
            int telegraphFrames,
            int damage,
            Fix centerX,
            Fix centerY,
            Fix centerZ,
            Fix radius)
        {
            HazardId = hazardId;
            Trigger = trigger;
            PeriodFrames = periodFrames < 1 ? 1 : periodFrames;
            TelegraphFrames = telegraphFrames < 0 ? 0 : telegraphFrames;
            Damage = damage;
            CenterX = centerX;
            CenterY = centerY;
            CenterZ = centerZ;
            Radius = radius;
        }

        public int HazardId { get; }
        public StageHazardTrigger Trigger { get; }
        public int PeriodFrames { get; }
        public int TelegraphFrames { get; }
        public int Damage { get; }
        public Fix CenterX { get; }
        public Fix CenterY { get; }
        public Fix CenterZ { get; }
        public Fix Radius { get; }
    }

    public readonly struct StageKinematicDef
    {
        public StageKinematicDef(
            int trackId,
            int periodFrames,
            Fix amplitude,
            Fix originX,
            Fix originY,
            Fix originZ)
        {
            TrackId = trackId;
            PeriodFrames = periodFrames < 1 ? 1 : periodFrames;
            Amplitude = amplitude;
            OriginX = originX;
            OriginY = originY;
            OriginZ = originZ;
        }

        public int TrackId { get; }
        public int PeriodFrames { get; }
        public Fix Amplitude { get; }
        public Fix OriginX { get; }
        public Fix OriginY { get; }
        public Fix OriginZ { get; }
    }

    public enum StageCarryItems : byte
    {
        All = 0,
        HeldOnly = 1,
        None = 2,
    }

    public enum StageCarryJewels : byte
    {
        All = 0,
        DropChance = 1,
        None = 2,
    }

    public enum StageHazardTrigger : byte
    {
        PhaseEnter = 0,
        Timer = 1,
        Proximity = 2,
    }
}
