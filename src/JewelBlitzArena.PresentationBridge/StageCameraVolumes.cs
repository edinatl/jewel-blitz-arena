using JewelBlitzArena.Data;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.PresentationBridge
{
    /// <summary>
    /// Presentation-only camera volume from the stage catalog (architecture §6).
    /// Sim stores <see cref="MatchState.PhaseIndex"/>; volumes never enter the hash.
    /// </summary>
    public readonly struct CameraVolume
    {
        public float CenterX { get; }
        public float CenterY { get; }
        public float CenterZ { get; }
        public float Radius { get; }

        public CameraVolume(float centerX, float centerY, float centerZ, float radius)
        {
            CenterX = centerX;
            CenterY = centerY;
            CenterZ = centerZ;
            Radius = radius > 0.01f ? radius : 12f;
        }

        public static CameraVolume Default => new CameraVolume(0f, 8f, 0f, 12f);
    }

    public readonly struct CameraFrame
    {
        public float LookX { get; }
        public float LookY { get; }
        public float LookZ { get; }
        public float OrthoSize { get; }
        public int PhaseIndex { get; }
        public float VolumeRadius { get; }

        public CameraFrame(float lookX, float lookY, float lookZ, float orthoSize, int phaseIndex, float volumeRadius)
        {
            LookX = lookX;
            LookY = lookY;
            LookZ = lookZ;
            OrthoSize = orthoSize;
            PhaseIndex = phaseIndex;
            VolumeRadius = volumeRadius;
        }
    }

    public static class StageCameraVolumes
    {
        public static CameraVolume Resolve(ContentCatalog? catalog, in MatchState state)
        {
            if (catalog != null
                && catalog.TryGetStage(state.StageId, out StageRecord stage)
                && stage.Phases != null
                && state.PhaseIndex >= 0
                && state.PhaseIndex < stage.Phases.Length)
            {
                StagePhaseRecord phase = stage.Phases[state.PhaseIndex];
                return new CameraVolume(
                    FixDisplay.ToFloat(new Fix(phase.CameraCenterXRaw)),
                    FixDisplay.ToFloat(new Fix(phase.CameraCenterYRaw)),
                    FixDisplay.ToFloat(new Fix(phase.CameraCenterZRaw)),
                    FixDisplay.ToFloat(new Fix(phase.CameraRadiusRaw)));
            }

            return CameraVolume.Default;
        }

        /// <summary>
        /// Bounding-sphere framing clamped to the phase camera volume (architecture §6 steps 1–3).
        /// </summary>
        public static CameraFrame FrameActors(ref MatchState state, in CameraVolume volume, float padding = 3f)
        {
            float sumX = 0f;
            float sumZ = 0f;
            int alive = 0;
            for (int i = 0; i < state.ActorCount; i++)
            {
                ActorSlot actor = MatchStateAccess.Actor(ref state, i);
                if (!actor.IsAlive)
                {
                    continue;
                }

                sumX += FixDisplay.ToFloat(actor.PosX);
                sumZ += FixDisplay.ToFloat(actor.PosZ);
                alive++;
            }

            float lookX = alive > 0 ? sumX / alive : volume.CenterX;
            float lookZ = alive > 0 ? sumZ / alive : volume.CenterZ;

            float dx = lookX - volume.CenterX;
            float dz = lookZ - volume.CenterZ;
            float distSq = (dx * dx) + (dz * dz);
            float maxR = volume.Radius;
            if (distSq > maxR * maxR && distSq > 0.0001f)
            {
                float dist = (float)System.Math.Sqrt(distSq);
                float s = maxR / dist;
                lookX = volume.CenterX + (dx * s);
                lookZ = volume.CenterZ + (dz * s);
            }

            float maxDist = 0f;
            for (int i = 0; i < state.ActorCount; i++)
            {
                ActorSlot actor = MatchStateAccess.Actor(ref state, i);
                if (!actor.IsAlive)
                {
                    continue;
                }

                float ax = FixDisplay.ToFloat(actor.PosX) - lookX;
                float az = FixDisplay.ToFloat(actor.PosZ) - lookZ;
                float d = (float)System.Math.Sqrt((ax * ax) + (az * az));
                if (d > maxDist)
                {
                    maxDist = d;
                }
            }

            float ortho = maxDist + padding;
            float maxOrtho = volume.Radius + padding;
            if (ortho > maxOrtho)
            {
                ortho = maxOrtho;
            }

            if (ortho < 4f)
            {
                ortho = 4f;
            }

            return new CameraFrame(lookX, volume.CenterY, lookZ, ortho, state.PhaseIndex, volume.Radius);
        }

        public static CameraFrame FrameFromCatalog(ContentCatalog? catalog, ref MatchState state, float padding = 3f)
        {
            CameraVolume volume = Resolve(catalog, in state);
            return FrameActors(ref state, in volume, padding);
        }
    }
}
