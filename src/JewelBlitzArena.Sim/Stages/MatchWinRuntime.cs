namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Shared boss-clear win: living fighters place 1, MatchEndFrame = Frame+1
    /// (Expedition chest keys off actor 0 == 1).
    /// </summary>
    public static class MatchWinRuntime
    {
        public static void TryBossClear(ref MatchState state)
        {
            if (state.MatchEndFrame != 0)
            {
                return;
            }

            for (int i = 0; i < state.ActorCount; i++)
            {
                ref ActorSlot a = ref MatchStateAccess.Actor(ref state, i);
                if (a.IsAlive && !a.IsEliminated)
                {
                    MatchStateAccess.SetPlacement(ref state, i, 1);
                }
            }

            state.MatchEndFrame = state.Frame + 1;
        }
    }
}
