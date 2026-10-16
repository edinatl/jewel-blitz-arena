using JewelBlitzArena.Data;

namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Boss timeout kill moves (stages.md §3.1 / §3.2): Harvester Blade Sweep (150 s),
    /// Pressworks Flood (180 s from phase B). Unavoidable — eliminates living fighters.
    /// </summary>
    public static class BossTimeoutRuntime
    {
        public const int HarvesterTimeoutFrames = 150 * 60;
        public const int PressworksTimeoutFrames = 180 * 60;

        public const int HazardBladeSweep = 70;
        public const int HazardFlood = 71;

        /// <summary>Remaining frames until timeout (0 if N/A or already fired).</summary>
        public static int Remaining(in MatchState state)
        {
            if ((state.StageFlags & StageFlags.BossTimeoutFired) != 0)
            {
                return 0;
            }

            int limit = LimitFrames(in state);
            if (limit <= 0)
            {
                return 0;
            }

            int left = limit - state.PhaseElapsedFrames;
            return left < 0 ? 0 : left;
        }

        public static void Tick(ref MatchState state, ref SimContactBuffer events)
        {
            if (state.MatchOver || (state.StageFlags & StageFlags.BossTimeoutFired) != 0)
            {
                return;
            }

            int limit = LimitFrames(in state);
            if (limit <= 0)
            {
                return;
            }

            // Fire on the frame PhaseElapsed reaches the limit (after StageRuntime increments).
            if (state.PhaseElapsedFrames < limit)
            {
                return;
            }

            state.StageFlags |= StageFlags.BossTimeoutFired;
            byte hazardId = state.StageId == SampleContentFactory.PressworksStageId
                ? (byte)HazardFlood
                : (byte)HazardBladeSweep;

            for (int a = 0; a < state.ActorCount; a++)
            {
                ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, a);
                if (!actor.IsAlive || actor.IsEliminated)
                {
                    continue;
                }

                events.Add(SimEventKind.HazardHitActor, (byte)a, hazardId);
                CombatSystem.ForceKnockOut(ref state, a);
            }
        }

        private static int LimitFrames(in MatchState state)
        {
            if (state.StageId == SampleContentFactory.HarvesterTerracesStageId)
            {
                // Single open Terrace hunt phase.
                return HarvesterTimeoutFrames;
            }

            if (state.StageId == SampleContentFactory.PressworksStageId
                && state.PhaseIndex == 1)
            {
                // Counted from The Press (phase B).
                return PressworksTimeoutFrames;
            }

            return 0;
        }
    }
}
