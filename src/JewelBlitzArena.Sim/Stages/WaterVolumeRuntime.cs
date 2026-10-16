namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Persistent phase water regions from authored <c>water_volumes</c> (stages.md §2.3 / §2.4).
    /// Not pulse hazards. Walk speed −20% while inside; <see cref="IsActorInWater"/> for Seam Charge / HUD later.
    /// </summary>
    public static class WaterVolumeRuntime
    {
        /// <summary>Walk multiplier while wading (design: −20%).</summary>
        public const int WalkSpeedPermille = 800;

        public static bool IsActorInWater(ref MatchState state, StageGraph graph, int actorIndex)
        {
            if (actorIndex < 0 || actorIndex >= state.ActorCount)
            {
                return false;
            }

            if (!graph.TryGetPhase(state.PhaseIndex, out StagePhaseDef phase)
                || phase.WaterVolumes.Length == 0)
            {
                return false;
            }

            ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, actorIndex);
            if (!actor.IsAlive || actor.IsEliminated)
            {
                return false;
            }

            for (int i = 0; i < phase.WaterVolumes.Length; i++)
            {
                StageWaterVolumeDef vol = phase.WaterVolumes[i];
                if (PhysicsWorld.OverlapsXZ(
                        actor.PosX, actor.PosZ, vol.CenterX, vol.CenterZ, vol.Radius))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Scale a move step for wading (−20%). No-op when step is zero.</summary>
        public static Fix ApplyWalkSlow(Fix step)
        {
            if (step.Raw == 0)
            {
                return step;
            }

            return new Fix((step.Raw * WalkSpeedPermille) / 1000);
        }

        /// <summary>First authored water volume for the active phase (HUD / presentation).</summary>
        public static bool TryGetActiveVolume(
            in MatchState state, StageGraph graph, out Fix centerX, out Fix centerZ, out Fix radius)
        {
            centerX = Fix.Zero;
            centerZ = Fix.Zero;
            radius = Fix.Zero;

            if (!graph.TryGetPhase(state.PhaseIndex, out StagePhaseDef phase)
                || phase.WaterVolumes.Length == 0)
            {
                return false;
            }

            StageWaterVolumeDef vol = phase.WaterVolumes[0];
            centerX = vol.CenterX;
            centerZ = vol.CenterZ;
            radius = vol.Radius;
            return true;
        }
    }
}
