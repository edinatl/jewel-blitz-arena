using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Meta
{
    /// <summary>
    /// Sparring preset (local training). Always available. No progression writes.
    /// Training flags stay on the request until Sim exposes MatchConfig.training.
    /// </summary>
    public sealed class SparringModeOrchestrator : IModeOrchestrator
    {
        /// <summary>Training stage id stub (Terraces geometry, boss absent) — not the Custom unlock entry.</summary>
        public const int TrainingStageId = 9001;

        public ModeId Mode => ModeId.Sparring;

        public bool WritesProgression => false;

        public MatchConfig BuildConfig(PlayerProfile profile, ModeRequest request)
        {
            int actors = CustomModeOrchestrator.ClampActors(request.ActorCount <= 0 ? 2 : request.ActorCount);
            if (actors > 2)
            {
                actors = 2; // Sparring: 1–2 fighters
            }

            MatchConfig config = MatchConfig.CreateDefault(request.Seed, actors);
            config.StageId = request.StageId != 0 ? request.StageId : TrainingStageId;
            config.HouseRulesBits = HouseRulesBits.ToSimBits(request.HouseRules);
            CustomModeOrchestrator.ApplyFighterIds(ref config, request.FighterIds, actors);
            _ = profile;
            _ = request.SparringJewelRespawn;
            _ = request.SparringItemSpawner;
            return config;
        }

        public void ApplyResult(PlayerProfile profile, in MatchResult result, ModeRequest request)
        {
            // No Ledger / carats.
            _ = profile;
            _ = result;
            _ = request;
        }
    }
}
