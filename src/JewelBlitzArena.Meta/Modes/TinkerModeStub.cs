using System;
using JewelBlitzArena.Data;

namespace JewelBlitzArena.Meta
{
    /// <summary>Tilly's Bench / Tinker — greybox craft entry (Chits earn on failure).</summary>
    public sealed class TinkerModeStub
    {
        public ModeId Mode => ModeId.TillysBench;

        public string MenuLabel => "Tilly's Bench";

        public void Open(PlayerProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            TinkerRuntime.EnsureSalt(profile);
            profile.SetFlag(ProfileFlags.TouchedTinkerStub);
        }

        public bool CanCraft => true;

        public TinkerAttemptResult Craft(
            PlayerProfile profile,
            int inputA,
            int inputB,
            int card = 0)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            return TinkerRuntime.TryCraft(profile, inputA, inputB, card);
        }

        public TinkerAttemptResult Craft(
            PlayerProfile profile,
            ContentCatalog catalog,
            int inputA,
            int inputB,
            int cardItemId = 0)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            return TinkerRuntime.TryCraft(profile, catalog, inputA, inputB, cardItemId);
        }
    }
}
