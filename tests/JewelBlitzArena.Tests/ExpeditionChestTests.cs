using JewelBlitzArena.Data;
using JewelBlitzArena.Meta;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class ExpeditionChestTests
{
    [Test]
    public void Time_mul_piecewise_at_band_ends_and_par()
    {
        const int par = 7200; // 120 s @ 60 Hz for easy math
        Assert.That(ExpeditionChestRuntime.TimeMulPermille((par * 3) / 4, par), Is.EqualTo(1200));
        Assert.That(ExpeditionChestRuntime.TimeMulPermille(par, par), Is.EqualTo(1000));
        Assert.That(ExpeditionChestRuntime.TimeMulPermille((par * 3) / 2, par), Is.EqualTo(800));
        Assert.That(ExpeditionChestRuntime.TimeMulPermille(0, par), Is.EqualTo(1200));
        Assert.That(ExpeditionChestRuntime.TimeMulPermille(par * 3, par), Is.EqualTo(800));
    }

    [Test]
    public void Full_clear_targets_and_chest_base_rise_with_difficulty()
    {
        Assert.That(ExpeditionChestRuntime.FullClearTarget(1), Is.EqualTo(2000));
        Assert.That(ExpeditionChestRuntime.FullClearTarget(8), Is.EqualTo(8000));
        Assert.That(ExpeditionChestRuntime.FullClearTarget(0), Is.EqualTo(2000));
        Assert.That(ExpeditionChestRuntime.FullClearTarget(99), Is.EqualTo(8000));

        int d1 = ExpeditionChestRuntime.ChestBaseCarats(
            SampleContentFactory.CitrineBazaarStageId, 1);
        int d8 = ExpeditionChestRuntime.ChestBaseCarats(
            SampleContentFactory.CitrineBazaarStageId, 8);
        Assert.That(d8, Is.GreaterThan(d1));
        // Diff-8 FFA1: 55% of 1100 = 605.
        Assert.That(d8, Is.EqualTo(605));
        Assert.That(
            ExpeditionChestRuntime.ChestBaseCarats(
                SampleContentFactory.HarvesterTerracesStageId, 8),
            Is.EqualTo(1100)); // 55% of 2000
    }

    [Test]
    public void ApplyResult_adds_chest_on_clear_skips_on_fail()
    {
        var orch = new ExpeditionModeOrchestrator();
        var request = new ModeRequest
        {
            Mode = ModeId.Expedition,
            Seed = 0x43484553545F31UL,
            StageId = SampleContentFactory.CitrineBazaarStageId,
            Difficulty = 8,
        };

        var cleared = new MatchResult
        {
            CaratsEarned = 100,
            ActorCount = 2,
            FramesPlayed = ExpeditionChestRuntime.ParFrames(request.StageId),
        };
        unsafe
        {
            cleared.Placements[0] = 1;
            cleared.Placements[1] = 2;
        }

        var profile = PlayerProfile.CreateNew();
        profile.Carats = 0;
        orch.ApplyResult(profile, cleared, request);
        // In-match 100 + chest 605 at par (mul 1.0), unless purse/coffer rolls.
        Assert.That(profile.Carats, Is.GreaterThanOrEqualTo(100 + 605));
        // Purse ≤2000; Coffer jackpot is 8000 (diff-8 under-par path — this clear is at par so no coffer).
        Assert.That(profile.Carats, Is.LessThanOrEqualTo(100 + 2000));

        var failed = cleared;
        unsafe
        {
            failed.Placements[0] = 2;
            failed.Placements[1] = 1;
        }

        profile = PlayerProfile.CreateNew();
        profile.Carats = 0;
        orch.ApplyResult(profile, failed, request);
        Assert.That(profile.Carats, Is.EqualTo(100), "failed stage keeps in-match only");
    }

    [Test]
    public void Fast_clear_boosts_chest_slow_cuts()
    {
        int stage = SampleContentFactory.CitrineBazaarStageId;
        int par = ExpeditionChestRuntime.ParFrames(stage);
        int baseChest = ExpeditionChestRuntime.ChestBaseCarats(stage, 8);
        int fast = ExpeditionChestRuntime.ApplyTimeMul(baseChest, (par * 3) / 4, stage);
        int slow = ExpeditionChestRuntime.ApplyTimeMul(baseChest, (par * 3) / 2, stage);
        Assert.That(fast, Is.EqualTo((baseChest * 1200) / 1000));
        Assert.That(slow, Is.EqualTo((baseChest * 800) / 1000));
        Assert.That(fast, Is.GreaterThan(slow));
    }
}
