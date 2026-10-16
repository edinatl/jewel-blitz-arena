using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Meta
{
    /// <summary>
    /// Expedition greybox: gleam budget drops, post-stage chest + time adjustment.
    /// Full difficulty table (opponents / boss scale / ally) remains deferred.
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
            // Difficulty is Meta-only for chest sizing until opponent/boss scale lands.
            _ = ExpeditionChestRuntime.ClampDifficulty(request.Difficulty);
            profile.SetFlag(ProfileFlags.TouchedExpeditionStub);
            return config;
        }

        public void ApplyResult(PlayerProfile profile, in MatchResult result, ModeRequest request)
        {
            long sum = profile.Carats;
            if (result.CaratsEarned > 0)
            {
                sum += result.CaratsEarned;
            }

            int chest = ExpeditionChestRuntime.ResolveChestCarats(
                request.StageId,
                request.Difficulty,
                result.FramesPlayed,
                request.Seed,
                ExpeditionChestRuntime.PlayerClearedStage(in result));
            if (chest > 0)
            {
                sum += chest;
            }

            profile.Carats = sum > int.MaxValue ? int.MaxValue : (int)sum;
        }
    }
}
