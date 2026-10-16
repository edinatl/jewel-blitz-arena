using JewelBlitzArena.Data;
using JewelBlitzArena.Meta;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class ExpeditionDifficultyTests
{
    [Test]
    public void Table_vectors_match_modes_md()
    {
        Assert.That(ExpeditionDifficultyRuntime.OpponentsForFfa(1, 0), Is.EqualTo(1));
        Assert.That(ExpeditionDifficultyRuntime.OpponentsForFfa(4, 0), Is.EqualTo(2));
        Assert.That(ExpeditionDifficultyRuntime.OpponentsForFfa(5, 0), Is.EqualTo(2));
        Assert.That(ExpeditionDifficultyRuntime.OpponentsForFfa(5, 1), Is.EqualTo(3));
        Assert.That(ExpeditionDifficultyRuntime.OpponentsForFfa(8, 0), Is.EqualTo(3));

        Assert.That(ExpeditionDifficultyRuntime.GleamScalePermille(1), Is.EqualTo(500));
        Assert.That(ExpeditionDifficultyRuntime.GleamScalePermille(8), Is.EqualTo(1000));
        Assert.That(ExpeditionDifficultyRuntime.BossDifficultyScalePermille(2), Is.EqualTo(600));
        Assert.That(ExpeditionDifficultyRuntime.BossDifficultyScalePermille(8), Is.EqualTo(1200));
        Assert.That(ExpeditionDifficultyRuntime.AllyPresent(2), Is.False);
        Assert.That(ExpeditionDifficultyRuntime.AllyPresent(3), Is.True);
    }

    [Test]
    public void Boss_life_scale_solo_diff1_is_point_three_six()
    {
        // 0.6 difficulty × 0.6 participant = 0.36 → 6000 × 0.36 = 2160 (balance.md §8).
        int scale = ExpeditionDifficultyRuntime.BossLifeScalePermille(1, fighterCount: 1);
        Assert.That(scale, Is.EqualTo(360));
        Assert.That(ExpeditionDifficultyRuntime.ScaleLife(6000, scale), Is.EqualTo(2160));
    }

    [Test]
    public void BuildConfig_sets_actors_scales_and_ally_team()
    {
        var orch = new ExpeditionModeOrchestrator();
        var profile = PlayerProfile.CreateNew();

        MatchConfig ffa = orch.BuildConfig(profile, new ModeRequest
        {
            Mode = ModeId.Expedition,
            Difficulty = 1,
            StageId = SampleContentFactory.CitrineBazaarStageId,
            LadderStageIndex = 0,
        });
        Assert.That(ffa.ActorCount, Is.EqualTo(2)); // player + 1 opponent
        Assert.That(ffa.GleamPickupScalePermille, Is.EqualTo(500));
        // Boss scale still computed from actor count (unused on FFA): 0.6 × 1.0 = 0.6
        Assert.That(ffa.BossLifeScalePermille, Is.EqualTo(600));

        MatchConfig harvesterSolo = orch.BuildConfig(profile, new ModeRequest
        {
            Mode = ModeId.Expedition,
            Difficulty = 2,
            StageId = SampleContentFactory.HarvesterTerracesStageId,
        });
        Assert.That(harvesterSolo.ActorCount, Is.EqualTo(1));
        Assert.That(harvesterSolo.TeamMode, Is.EqualTo(0));
        Assert.That(harvesterSolo.BossLifeScalePermille, Is.EqualTo(360));

        MatchConfig harvesterAlly = orch.BuildConfig(profile, new ModeRequest
        {
            Mode = ModeId.Expedition,
            Difficulty = 4,
            StageId = SampleContentFactory.HarvesterTerracesStageId,
        });
        Assert.That(harvesterAlly.ActorCount, Is.EqualTo(2));
        Assert.That(harvesterAlly.TeamMode, Is.EqualTo(1));
        unsafe
        {
            Assert.That(harvesterAlly.Teams[0], Is.EqualTo(harvesterAlly.Teams[1]));
        }

        Assert.That(harvesterAlly.BossLifeScalePermille, Is.EqualTo(800)); // 0.8 × 1.0
        Assert.That(harvesterAlly.GetCpuLevel(1), Is.EqualTo(4));
    }

    [Test]
    public void Sim_applies_boss_life_scale_on_Harvester()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var config = MatchConfig.CreateDefault(0x444946465F3154UL, actorCount: 1);
        unsafe
        {
            config.StageId = SampleContentFactory.HarvesterTerracesStageId;
            config.FighterIds[0] = SampleContentFactory.RookFighterId;
        }

        config.BossLifeScalePermille = 360;
        var sim = new Simulation();
        sim.Reset(config, catalog);
        MatchState snap = sim.Snapshot();
        Assert.That(
            MatchStateAccess.GetBossSub(ref snap, 0).Param0,
            Is.EqualTo(ExpeditionDifficultyRuntime.ScaleLife(BossPartRuntime.LegMaxLife, 360)));
    }

    [Test]
    public void Default_config_keeps_unscaled_corpus_path()
    {
        var config = MatchConfig.CreateDefault(1UL, actorCount: 2);
        Assert.That(config.GleamPickupScalePermille, Is.EqualTo(1000));
        Assert.That(config.BossLifeScalePermille, Is.EqualTo(1000));
    }
}
