using JewelBlitzArena.Data;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Meta
{
    /// <summary>
    /// Expedition: gleam budget, chest/time, and difficulty table (modes.md §5).
    /// </summary>
    public class ExpeditionModeOrchestrator : IModeOrchestrator
    {
        public ModeId Mode => ModeId.Expedition;

        public bool WritesProgression => true;

        public MatchConfig BuildConfig(PlayerProfile profile, ModeRequest request)
        {
            int difficulty = ExpeditionChestRuntime.ClampDifficultyForProfile(
                request.Difficulty, profile.BestExpeditionDifficulty);
            int ladder = request.LadderStageIndex;
            if (ladder < 0 || ladder > 4)
            {
                ladder = ExpeditionDifficultyRuntime.InferLadderSlot(request.StageId);
            }

            int actors = ExpeditionDifficultyRuntime.ActorCountFor(
                difficulty, request.StageId, ladder);
            actors = CustomModeOrchestrator.ClampActors(actors);

            MatchConfig config = MatchConfig.CreateDefault(request.Seed, actors);
            config.StageId = request.StageId;
            config.HouseRulesBits = HouseRulesBits.ToSimBits(request.HouseRules)
                | MatchRules.ExpeditionGleams;
            if (request.HouseRules.JewelCap > 0)
            {
                config.JewelCap = request.HouseRules.JewelCap;
            }

            config.GleamPickupScalePermille =
                ExpeditionDifficultyRuntime.GleamScalePermille(difficulty);
            config.BossLifeScalePermille =
                ExpeditionDifficultyRuntime.BossLifeScalePermille(difficulty, actors);

            // Boss ally (diff ≥3): player + CPU share a team so Rally works; TeamMode≠0.
            if (ExpeditionDifficultyRuntime.IsBossStage(request.StageId)
                && ExpeditionDifficultyRuntime.AllyPresent(difficulty)
                && actors >= 2)
            {
                config.TeamMode = 1;
                unsafe
                {
                    config.Teams[0] = 0;
                    config.Teams[1] = 0;
                }
            }

            CustomModeOrchestrator.ApplyFighterIds(ref config, request.FighterIds, actors);
            for (int i = 0; i < actors; i++)
            {
                config.SetCpuLevel(i, difficulty);
            }

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

            bool cleared = ExpeditionChestRuntime.PlayerClearedStage(in result);
            int difficulty = ExpeditionChestRuntime.ClampDifficulty(request.Difficulty);
            int chest = ExpeditionChestRuntime.ResolveChestCarats(
                request.StageId,
                difficulty,
                result.FramesPlayed,
                request.Seed,
                cleared);
            if (chest > 0)
            {
                sum += chest;
            }

            profile.Carats = sum > int.MaxValue ? int.MaxValue : (int)sum;

            if (cleared && difficulty > profile.BestExpeditionDifficulty)
            {
                profile.BestExpeditionDifficulty = difficulty;
            }
        }
    }
}
