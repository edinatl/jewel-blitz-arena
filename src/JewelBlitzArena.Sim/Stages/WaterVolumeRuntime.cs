using JewelBlitzArena.Data;

namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Persistent phase water regions (stages.md §2.3 / §2.4). Not pulse hazards.
    /// Walk speed −20% while inside; <see cref="IsActorInWater"/> for Seam Charge / HUD later.
    /// </summary>
    public static class WaterVolumeRuntime
    {
        /// <summary>Walk multiplier while wading (design: −20%).</summary>
        public const int WalkSpeedPermille = 800;

        public static bool IsActorInWater(ref MatchState state, int actorIndex)
        {
            if (actorIndex < 0 || actorIndex >= state.ActorCount)
            {
                return false;
            }

            if (!TryGetActiveVolume(in state, out Fix cx, out Fix cz, out Fix radius))
            {
                return false;
            }

            ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, actorIndex);
            if (!actor.IsAlive || actor.IsEliminated)
            {
                return false;
            }

            return PhysicsWorld.OverlapsXZ(actor.PosX, actor.PosZ, cx, cz, radius);
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

        public static bool TryGetActiveVolume(
            in MatchState state, out Fix centerX, out Fix centerZ, out Fix radius)
        {
            centerX = Fix.Zero;
            centerZ = Fix.Zero;
            radius = Fix.Zero;

            if (state.StageId == SampleContentFactory.SunkenStrongroomStageId)
            {
                // Phase B Vault door + Phase C Hull breach — risen water.
                if (state.PhaseIndex == 1 || state.PhaseIndex == 2)
                {
                    centerX = Fix.Zero;
                    centerZ = Fix.Zero;
                    radius = Fix.FromInt(8);
                    return true;
                }

                return false;
            }

            if (state.StageId == SampleContentFactory.RimeCutworksStageId)
            {
                // Phase B Flooding — flooded floor is a water volume.
                if (state.PhaseIndex == 1)
                {
                    centerX = Fix.Zero;
                    centerZ = Fix.Zero;
                    radius = Fix.FromInt(8);
                    return true;
                }

                return false;
            }

            return false;
        }
    }
}
