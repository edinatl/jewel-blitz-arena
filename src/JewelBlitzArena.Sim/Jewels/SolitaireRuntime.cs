namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Solitaire use stub (items-and-economy.md §6): grants one bonus jewel on the spot.
    /// Presentation/Meta decide when the pocketed item is activated; Sim only applies the jewel.
    /// Bonus jewels still count toward <see cref="MatchRules.BlitzJewelCount"/> and the jewel cap.
    /// </summary>
    public static class SolitaireRuntime
    {
        /// <summary>
        /// Grant one owned jewel to <paramref name="actorIndex"/> if under Blitz count and cap.
        /// Emits <see cref="SimEventKind.JewelReadyToBlitz"/> when the grant reaches Blitz threshold.
        /// </summary>
        public static bool TryUse(
            ref MatchState state,
            int actorIndex,
            ref SimContactBuffer contacts)
        {
            if (actorIndex < 0 || actorIndex >= state.ActorCount)
            {
                return false;
            }

            ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, actorIndex);
            if (!actor.IsAlive || actor.IsEliminated || actor.IsBlitzed || actor.DrossFrames > 0)
            {
                return false;
            }

            if (actor.JewelsHeld >= MatchRules.BlitzJewelCount)
            {
                return false;
            }

            if (!JewelRuntime.TryGiveOwnedJewel(ref state, actorIndex))
            {
                return false;
            }

            contacts.Add(SimEventKind.ActorJewelPickup, (byte)actorIndex, 0);
            if (actor.JewelsHeld >= MatchRules.BlitzJewelCount)
            {
                contacts.Add(SimEventKind.JewelReadyToBlitz, (byte)actorIndex, 0);
            }

            return true;
        }

        /// <summary>Convenience when the caller has no contact buffer.</summary>
        public static bool TryUse(ref MatchState state, int actorIndex)
        {
            var contacts = default(SimContactBuffer);
            return TryUse(ref state, actorIndex, ref contacts);
        }
    }
}
