using JewelBlitzArena.Data;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Meta
{
    /// <summary>
    /// Custom greybox / rules sandbox. Design: no economy progression.
    /// S6 exit still stamps one Ledger inventory flag + profile flag after a completed greybox.
    /// </summary>
    public sealed class CustomModeOrchestrator : IModeOrchestrator
    {
        public ModeId Mode => ModeId.Custom;

        /// <summary>Custom writes no carats; S6 greybox stamp is a one-shot inventory flag only.</summary>
        public bool WritesProgression => true;

        public MatchConfig BuildConfig(PlayerProfile profile, ModeRequest request)
        {
            int actors = ClampActors(request.ActorCount);
            MatchConfig config = MatchConfig.CreateDefault(request.Seed, actors);
            int stageId = request.StageId;
            // Locked Extra Arena → fall back to Citrine (box visible but not selectable).
            if (!ExtraArenaRuntime.IsStageUnlocked(profile, stageId))
            {
                stageId = SampleContentFactory.CitrineBazaarStageId;
            }

            config.StageId = stageId;
            config.HouseRulesBits = HouseRulesBits.ToSimBits(request.HouseRules);
            if (request.HouseRules.JewelCap > 0)
            {
                config.JewelCap = request.HouseRules.JewelCap;
            }

            ApplyFighterIds(ref config, request.FighterIds, actors);
            profile.HouseRulesDefaults = request.HouseRules;
            return config;
        }

        public void ApplyResult(PlayerProfile profile, in MatchResult result, ModeRequest request)
        {
            // Design: Custom progression writes = None. S6 exit: stamp one inventory flag once.
            if (!profile.HasFlag(ProfileFlags.CompletedCustomGreybox))
            {
                profile.SetFlag(ProfileFlags.CompletedCustomGreybox);
                profile.Ledger.Grant(LedgerInventory.CustomGreyboxTokenItemId);
            }

            _ = result;
            _ = request;
        }

        internal static int ClampActors(int actorCount) =>
            actorCount < 1 ? 1 : (actorCount > PoolCaps.Actors ? PoolCaps.Actors : actorCount);

        internal static void ApplyFighterIds(ref MatchConfig config, int[]? fighterIds, int actors)
        {
            if (fighterIds == null || fighterIds.Length == 0)
            {
                return;
            }

            unsafe
            {
                for (int i = 0; i < actors && i < fighterIds.Length; i++)
                {
                    config.FighterIds[i] = fighterIds[i];
                }
            }
        }
    }
}
