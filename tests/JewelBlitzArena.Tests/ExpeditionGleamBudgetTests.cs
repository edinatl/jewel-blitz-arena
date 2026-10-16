using JewelBlitzArena.Data;
using JewelBlitzArena.Meta;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class ExpeditionGleamBudgetTests
{
    private const ulong Seed = 0x4558505F474C4D42UL; // EXP_GLMB

    [Test]
    public void Expedition_config_sets_gleam_bit_and_splits_budget()
    {
        var profile = PlayerProfile.CreateNew();
        var orch = new ExpeditionModeOrchestrator();
        var request = new ModeRequest
        {
            Mode = ModeId.Expedition,
            Seed = Seed,
            ActorCount = 2,
            StageId = SampleContentFactory.CitrineBazaarStageId,
        };

        MatchConfig config = orch.BuildConfig(profile, request);
        Assert.That(config.HouseRulesBits & MatchRules.ExpeditionGleams, Is.Not.EqualTo(0));

        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var sim = new Simulation();
        sim.Reset(config, catalog);
        MatchState snap = sim.Snapshot();
        // In-match = 45% of design row 1100 → 495; chest still keys off 1100.
        int total = GleamBudgetRuntime.BudgetForStage(SampleContentFactory.CitrineBazaarStageId);
        Assert.That(total, Is.EqualTo(495));
        Assert.That(
            GleamBudgetRuntime.DesignTotalForStage(SampleContentFactory.CitrineBazaarStageId),
            Is.EqualTo(1100));
        Assert.That(
            snap.GleamBudgetKoRemaining + snap.GleamBudgetKdRemaining + snap.GleamBudgetCrateRemaining,
            Is.EqualTo(total));
        Assert.That(snap.GleamBudgetKoRemaining, Is.EqualTo((total * 50) / 100));
    }

    [Test]
    public void High_difficulty_gated_until_clear_at_5()
    {
        var orch = new ExpeditionModeOrchestrator();
        var profile = PlayerProfile.CreateNew();
        var request = new ModeRequest
        {
            Mode = ModeId.Expedition,
            Seed = Seed ^ 9UL,
            ActorCount = 2,
            StageId = SampleContentFactory.CitrineBazaarStageId,
            Difficulty = 8,
        };

        MatchConfig locked = orch.BuildConfig(profile, request);
        Assert.That(locked.GetCpuLevel(0), Is.EqualTo(6), "fresh profile clamps 7–8 → 6");

        profile.BestExpeditionDifficulty = 5;
        MatchConfig unlocked = orch.BuildConfig(profile, request);
        Assert.That(unlocked.GetCpuLevel(0), Is.EqualTo(8));
    }

    [Test]
    public void Clear_at_5_records_best_expedition_difficulty()
    {
        var orch = new ExpeditionModeOrchestrator();
        var profile = PlayerProfile.CreateNew();
        var request = new ModeRequest
        {
            Mode = ModeId.Expedition,
            Seed = Seed ^ 10UL,
            StageId = SampleContentFactory.CitrineBazaarStageId,
            Difficulty = 5,
        };

        var cleared = new MatchResult
        {
            CaratsEarned = 0,
            ActorCount = 2,
            FramesPlayed = ExpeditionChestRuntime.ParFrames(request.StageId),
        };
        unsafe
        {
            cleared.Placements[0] = 1;
            cleared.Placements[1] = 2;
        }

        orch.ApplyResult(profile, cleared, request);
        Assert.That(profile.BestExpeditionDifficulty, Is.EqualTo(5));
    }

    [Test]
    public void KO_spends_ko_budget_and_spawns_gleams()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var config = MatchConfig.CreateDefault(Seed ^ 1UL, actorCount: 2);
        config.HouseRulesBits = MatchRules.HouseLusterTimer | MatchRules.ExpeditionGleams;
        unsafe
        {
            config.StageId = SampleContentFactory.CitrineBazaarStageId;
            config.FighterIds[0] = SampleContentFactory.RookFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);
        MatchState before = sim.Snapshot();
        int koBefore = before.GleamBudgetKoRemaining;
        Assert.That(koBefore, Is.GreaterThan(0));

        {
            ref ActorSlot victim = ref MatchStateAccess.Actor(ref before, 1);
            victim.Life = 1;
            victim.PosX = Fix.FromInt(5);
            victim.PosZ = Fix.FromInt(5);
            ref ActorSlot atk = ref MatchStateAccess.Actor(ref before, 0);
            atk.PosX = Fix.FromInt(5);
            atk.PosZ = Fix.FromInt(5);
            atk.Action = (byte)ActorAction.Attack;
            atk.ActionPhase = (byte)ActionPhase.Active;
            atk.ActionTimer = 10;
            atk.HitApplied = 0;
            atk.MoveDamage = 50;
        }

        sim.Restore(before);
        sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);

        MatchState after = sim.Snapshot();
        Assert.That(MatchStateAccess.Actor(ref after, 1).IsEliminated, Is.True);
        Assert.That(after.GleamBudgetKoRemaining, Is.LessThan(koBefore));
        Assert.That(GleamRuntime.CountActive(ref after), Is.GreaterThan(0));
    }

    [Test]
    public void Meta_ApplyResult_adds_carats_to_profile()
    {
        var profile = PlayerProfile.CreateNew();
        profile.Carats = 10;
        var orch = new ExpeditionModeOrchestrator();
        var result = new MatchResult { CaratsEarned = 100, ActorCount = 2 };
        orch.ApplyResult(profile, result, new ModeRequest { Mode = ModeId.Expedition });
        Assert.That(profile.Carats, Is.EqualTo(110));
    }

    [Test]
    public void Menu_lists_Expedition_available()
    {
        var menu = new MetaMenuShell();
        var entries = menu.BuildEntries(PlayerProfile.CreateNew());
        bool found = false;
        foreach (MetaMenuEntry e in entries)
        {
            if (e.Mode == ModeId.Expedition)
            {
                Assert.That(e.Available, Is.True);
                found = true;
            }
        }

        Assert.That(found, Is.True);
    }
}
