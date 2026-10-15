using System;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Meta
{
    /// <summary>M2 Expedition shell — not implemented in S6.</summary>
    public sealed class ExpeditionModeStub : IModeOrchestrator
    {
        public ModeId Mode => ModeId.Expedition;

        public bool WritesProgression => true;

        public MatchConfig BuildConfig(PlayerProfile profile, ModeRequest request)
        {
            profile.SetFlag(ProfileFlags.TouchedExpeditionStub);
            throw new NotImplementedException("Expedition is M2 — S6 only stubs the menu entry.");
        }

        public void ApplyResult(PlayerProfile profile, in MatchResult result, ModeRequest request) =>
            throw new NotImplementedException("Expedition is M2 — S6 only stubs the menu entry.");
    }
}
