using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Meta
{
    /// <summary>
    /// Expedition greybox: enables gleam budget drops and ingests carats into the profile.
    /// Full difficulty table / chest / time adjustment remain M2.
    /// </summary>
    public class ExpeditionModeOrchestrator : IModeOrchestrator
    {
        public ModeId Mode => ModeId.Expedition;

        public bool WritesProgression => true;

        public MatchConfig BuildConfig(PlayerProfile profile, ModeRequest request)
        {
            int actors = CustomModeOrchestrator.ClampActors(request.ActorCount);
            MatchConfig config = MatchConfig.CreateDefault(request.Seed, actors);
            config.StageId = request.StageId;
            config.HouseRulesBits = HouseRulesBits.ToSimBits(request.HouseRules)
                | MatchRules.ExpeditionGleams;
            if (request.HouseRules.JewelCap > 0)
            {
                config.JewelCap = request.HouseRules.JewelCap;
            }

            CustomModeOrchestrator.ApplyFighterIds(ref config, request.FighterIds, actors);
            profile.SetFlag(ProfileFlags.TouchedExpeditionStub);
            return config;
        }

        public void ApplyResult(PlayerProfile profile, in MatchResult result, ModeRequest request)
        {
            if (result.CaratsEarned > 0)
            {
                long sum = (long)profile.Carats + result.CaratsEarned;
                profile.Carats = sum > int.MaxValue ? int.MaxValue : (int)sum;
            }

            _ = request;
        }
    }
}
