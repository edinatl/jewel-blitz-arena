using System;

namespace JewelBlitzArena.Meta
{
    /// <summary>Tilly's Bench / Tinker — menu stub only in S6.</summary>
    public sealed class TinkerModeStub
    {
        public ModeId Mode => ModeId.TillysBench;

        public string MenuLabel => "Tilly's Bench (stub)";

        public void Open(PlayerProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            profile.SetFlag(ProfileFlags.TouchedTinkerStub);
        }

        public bool CanCraft => false;
    }
}
