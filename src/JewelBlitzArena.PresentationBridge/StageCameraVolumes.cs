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

        public static CameraVolume Lerp(in CameraVolume a, in CameraVolume b, float t)
        {
            if (t <= 0f)
            {
                return a;
            }

            if (t >= 1f)
            {
                return b;
            }

            return new CameraVolume(
                a.CenterX + ((b.CenterX - a.CenterX) * t),
                a.CenterY + ((b.CenterY - a.CenterY) * t),
                a.CenterZ + ((b.CenterZ - a.CenterZ) * t),
                a.Radius + ((b.Radius - a.Radius) * t));
        }
    }

    /// <summary>
    /// Off-screen actor inset when the volume fit fails (combat-and-match §12).
    /// <see cref="EdgeU"/>/<see cref="EdgeV"/> are direction on the frame rim (−1..1).
    /// Pips / held-item id are presentation HUD hints (not hashed).
    /// </summary>
    public readonly struct EdgeBubble
    {
        public int ActorIndex { get; }
        public float EdgeU { get; }
        public float EdgeV { get; }
        public float WorldX { get; }
        public float WorldZ { get; }
        public int JewelsHeld { get; }
        public int HeldItemId { get; }
        public bool HasHeldItem { get; }

        public EdgeBubble(
            int actorIndex,
            float edgeU,
            float edgeV,
            float worldX,
            float worldZ,
            int jewelsHeld = 0,
            int heldItemId = -1,
            bool hasHeldItem = false)
        {
            ActorIndex = actorIndex;
            EdgeU = edgeU;
            EdgeV = edgeV;
            WorldX = worldX;
            WorldZ = worldZ;
            JewelsHeld = jewelsHeld;
            HeldItemId = heldItemId;
            HasHeldItem = hasHeldItem;
        }
    }

    public readonly struct CameraFrame
    {
        public float LookX { get; }
        public float LookY { get; }
        public float LookZ { get; }
        public float OrthoSize { get; }
        public int PhaseIndex { get; }
        public float VolumeRadius { get; }

        /// <summary>True when actors do not fit the phase max volume — edge bubbles apply.</summary>
        public bool FitFailed { get; }

        /// <summary>True while lerping previous→current volume during a phase transition.</summary>
        public bool InScriptedPath { get; }

        /// <summary>0 at transition start, 1 at arrival.</summary>
        public float PathT { get; }

        public int EdgeBubbleCount { get; }
        public EdgeBubble Edge0 { get; }
        public EdgeBubble Edge1 { get; }
        public EdgeBubble Edge2 { get; }
        public EdgeBubble Edge3 { get; }

        public CameraFrame(
            float lookX,
            float lookY,
            float lookZ,
            float orthoSize,
            int phaseIndex,
            float volumeRadius,
            bool fitFailed = false,
            bool inScriptedPath = false,
            float pathT = 0f,
            int edgeBubbleCount = 0,
            EdgeBubble edge0 = default,
            EdgeBubble edge1 = default,
            EdgeBubble edge2 = default,
            EdgeBubble edge3 = default)
        {
            LookX = lookX;
            LookY = lookY;
            LookZ = lookZ;
            OrthoSize = orthoSize;
            PhaseIndex = phaseIndex;
            VolumeRadius = volumeRadius;
            FitFailed = fitFailed;
            InScriptedPath = inScriptedPath;
            PathT = pathT;
            EdgeBubbleCount = edgeBubbleCount;
            Edge0 = edge0;
            Edge1 = edge1;
            Edge2 = edge2;
            Edge3 = edge3;
        }

        public EdgeBubble GetEdgeBubble(int index)
        {
            switch (index)
            {
                case 0: return Edge0;
                case 1: return Edge1;
                case 2: return Edge2;
                case 3: return Edge3;
                default: return default;
            }
        }
    }

    public static class StageCameraVolumes
    {
        public static CameraVolume ResolvePhaseVolume(ContentCatalog? catalog, int stageId, int phaseIndex)
        {
            if (catalog != null
                && catalog.TryGetStage(stageId, out StageRecord stage)
                && stage.Phases != null
                && phaseIndex >= 0
                && phaseIndex < stage.Phases.Length)
            {
                StagePhaseRecord phase = stage.Phases[phaseIndex];
                return new CameraVolume(
                    FixDisplay.ToFloat(new Fix(phase.CameraCenterXRaw)),
                    FixDisplay.ToFloat(new Fix(phase.CameraCenterYRaw)),
                    FixDisplay.ToFloat(new Fix(phase.CameraCenterZRaw)),
                    FixDisplay.ToFloat(new Fix(phase.CameraRadiusRaw)));
            }

            return CameraVolume.Default;
        }

        /// <summary>
        /// Resolve the active volume, lerping previous→current during
        /// <see cref="StageFlags.InTransition"/> (scripted path without schema v3).
        /// </summary>
        public static CameraVolume Resolve(ContentCatalog? catalog, in MatchState state)
        {
            ResolvePath(catalog, in state, out CameraVolume volume, out _, out _);
            return volume;
        }

        public static void ResolvePath(
            ContentCatalog? catalog,
            in MatchState state,
            out CameraVolume volume,
            out bool inScriptedPath,
            out float pathT)
        {
            CameraVolume current = ResolvePhaseVolume(catalog, state.StageId, state.PhaseIndex);
            inScriptedPath = false;
            pathT = 1f;

            if ((state.StageFlags & StageFlags.InTransition) == 0
                || state.PhaseIndex <= 0
                || catalog == null
                || !catalog.TryGetStage(state.StageId, out StageRecord stage)
                || stage.Phases == null
                || state.PhaseIndex >= stage.Phases.Length)
            {
                volume = current;
                return;
            }

            StagePhaseRecord phase = stage.Phases[state.PhaseIndex];
            int total = ScaleTransitionFrames(phase.TransitionFrames, state.PhaseClockPermille);
            if (total <= 0)
            {
                volume = current;
                return;
            }

            CameraVolume previous = ResolvePhaseVolume(catalog, state.StageId, state.PhaseIndex - 1);
            int remaining = state.TransitionRemainingFrames;
            if (remaining < 0)
            {
                remaining = 0;
            }

            if (remaining > total)
            {
                remaining = total;
            }

            pathT = 1f - (remaining / (float)total);
            volume = CameraVolume.Lerp(in previous, in current, pathT);
            inScriptedPath = true;
        }

        /// <summary>
        /// Bounding-sphere framing clamped to the phase camera volume (architecture §6 steps 1–3).
        /// On fit failure, pulls to max ortho and assigns edge bubbles (combat-and-match §12).
        /// </summary>
        public static CameraFrame FrameActors(ref MatchState state, in CameraVolume volume, float padding = 3f)
        {
            return FrameActors(
                ref state,
                in volume,
                padding,
                inScriptedPath: false,
                pathT: 0f);
        }

        public static CameraFrame FrameActors(
            ref MatchState state,
            in CameraVolume volume,
            float padding,
            bool inScriptedPath,
            float pathT)
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

            float desiredOrtho = maxDist + padding;
            float maxOrtho = volume.Radius + padding;
            bool fitFailed = desiredOrtho > maxOrtho + 0.001f;
            float ortho = fitFailed ? maxOrtho : desiredOrtho;

            if (ortho < 4f)
            {
                ortho = 4f;
            }

            int bubbleCount = 0;
            EdgeBubble e0 = default;
            EdgeBubble e1 = default;
            EdgeBubble e2 = default;
            EdgeBubble e3 = default;

            if (fitFailed)
            {
                CollectEdgeBubbles(
                    ref state,
                    lookX,
                    lookZ,
                    ortho,
                    ref bubbleCount,
                    ref e0,
                    ref e1,
                    ref e2,
                    ref e3);
            }

            return new CameraFrame(
                lookX,
                volume.CenterY,
                lookZ,
                ortho,
                state.PhaseIndex,
                volume.Radius,
                fitFailed,
                inScriptedPath,
                pathT,
                bubbleCount,
                e0,
                e1,
                e2,
                e3);
        }

        public static CameraFrame FrameFromCatalog(ContentCatalog? catalog, ref MatchState state, float padding = 3f)
        {
            ResolvePath(catalog, in state, out CameraVolume volume, out bool scripted, out float pathT);
            return FrameActors(ref state, in volume, padding, scripted, pathT);
        }

        private static void CollectEdgeBubbles(
            ref MatchState state,
            float lookX,
            float lookZ,
            float ortho,
            ref int bubbleCount,
            ref EdgeBubble e0,
            ref EdgeBubble e1,
            ref EdgeBubble e2,
            ref EdgeBubble e3)
        {
            float rim = ortho > 0.01f ? ortho : 0.01f;
            for (int i = 0; i < state.ActorCount && bubbleCount < 4; i++)
            {
                ActorSlot actor = MatchStateAccess.Actor(ref state, i);
                if (!actor.IsAlive)
                {
                    continue;
                }

                float ax = FixDisplay.ToFloat(actor.PosX);
                float az = FixDisplay.ToFloat(actor.PosZ);
                float ox = ax - lookX;
                float oz = az - lookZ;
                float d = (float)System.Math.Sqrt((ox * ox) + (oz * oz));
                if (d <= rim + 0.05f)
                {
                    continue;
                }

                float inv = d > 0.0001f ? 1f / d : 0f;
                float edgeU = ox * inv;
                float edgeV = oz * inv;
                bool hasItem = actor.HeldItemSlot != ActorSlot.NoHeldItem;
                int itemId = -1;
                if (hasItem)
                {
                    LooseItemSlot loose = MatchStateAccess.GetLooseItem(ref state, actor.HeldItemSlot);
                    itemId = loose.ItemId;
                }

                var bubble = new EdgeBubble(
                    i,
                    edgeU,
                    edgeV,
                    ax,
                    az,
                    jewelsHeld: actor.JewelsHeld,
                    heldItemId: itemId,
                    hasHeldItem: hasItem);
                switch (bubbleCount)
                {
                    case 0: e0 = bubble; break;
                    case 1: e1 = bubble; break;
                    case 2: e2 = bubble; break;
                    case 3: e3 = bubble; break;
                }

                bubbleCount++;
            }
        }

        private static int ScaleTransitionFrames(int authoredFrames, int permille)
        {
            if (authoredFrames <= 0)
            {
                return 0;
            }

            int p = permille <= 0 ? 1000 : permille;
            long scaled = ((long)authoredFrames * p) / 1000L;
            if (scaled < 1)
            {
                return 1;
            }

            if (scaled > int.MaxValue)
            {
                return int.MaxValue;
            }

            return (int)scaled;
        }
    }
}
