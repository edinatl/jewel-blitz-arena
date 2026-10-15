using JewelBlitzArena.Sim;

namespace JewelBlitzArena.PresentationBridge
{
    /// <summary>
    /// Presentation-only left/right held-item HUD hints for Clank dual-hold
    /// (and single-hold kits that only fill the right hand). Not hashed.
    /// </summary>
    public readonly struct MatchHoldHud
    {
        public int RightItemId { get; }
        public int LeftItemId { get; }
        public bool HasRight { get; }
        public bool HasLeft { get; }
        public bool HasAny => HasRight || HasLeft;

        public MatchHoldHud(int rightItemId, bool hasRight, int leftItemId, bool hasLeft)
        {
            RightItemId = rightItemId;
            HasRight = hasRight;
            LeftItemId = leftItemId;
            HasLeft = hasLeft;
        }

        public static MatchHoldHud Empty => new MatchHoldHud(-1, false, -1, false);

        public static MatchHoldHud FromActor(ref MatchState state, in ActorSlot actor)
        {
            int rightId = -1;
            bool hasRight = false;
            if (actor.HeldItemSlot != ActorSlot.NoHeldItem
                && actor.HeldItemSlot < PoolCaps.LooseItems)
            {
                LooseItemSlot loose = MatchStateAccess.GetLooseItem(ref state, actor.HeldItemSlot);
                rightId = loose.ItemId;
                hasRight = true;
            }

            int leftId = -1;
            bool hasLeft = false;
            if (actor.HeldItemSlotLeft != ActorSlot.NoHeldItem
                && actor.HeldItemSlotLeft < PoolCaps.LooseItems)
            {
                LooseItemSlot loose = MatchStateAccess.GetLooseItem(ref state, actor.HeldItemSlotLeft);
                leftId = loose.ItemId;
                hasLeft = true;
            }

            return new MatchHoldHud(rightId, hasRight, leftId, hasLeft);
        }

        /// <summary>Compact HUD label: empty, <c>R:id</c>, <c>L:id</c>, or <c>R:id L:id</c>.</summary>
        public string FormatHands()
        {
            if (!HasAny)
            {
                return string.Empty;
            }

            if (HasRight && HasLeft)
            {
                return $"R:{RightItemId} L:{LeftItemId}";
            }

            if (HasRight)
            {
                return $"R:{RightItemId}";
            }

            return $"L:{LeftItemId}";
        }
    }
}
