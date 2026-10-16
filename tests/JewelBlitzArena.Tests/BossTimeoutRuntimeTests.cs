using JewelBlitzArena.Data;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class BossTimeoutRuntimeTests
{
    private const ulong Seed = 0x54494D455F4F5554UL; // TIME_OUT

    [Test]
    public void Harvester_timeout_remaining_counts_down()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var config = MatchConfig.CreateDefault(Seed, actorCount: 2);
        unsafe
        {
            config.StageId = SampleContentFactory.HarvesterTerracesStageId;
            config.FighterIds[0] = SampleContentFactory.RookFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);
        Assert.That(
            StageRuntime.BossTimeoutRemaining(in sim.State),
            Is.EqualTo(BossTimeoutRuntime.HarvesterTimeoutFrames));

        for (int f = 0; f < 60; f++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        Assert.That(
            StageRuntime.BossTimeoutRemaining(in sim.State),
            Is.EqualTo(BossTimeoutRuntime.HarvesterTimeoutFrames - 60));
    }

    [Test]
    public void Harvester_blade_sweep_eliminates_fighters()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var config = MatchConfig.CreateDefault(Seed ^ 1UL, actorCount: 2);
        unsafe
        {
            config.StageId = SampleContentFactory.HarvesterTerracesStageId;
            config.FighterIds[0] = SampleContentFactory.RookFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);

        MatchState snap = sim.Snapshot();
        snap.PhaseElapsedFrames = BossTimeoutRuntime.HarvesterTimeoutFrames - 1;
        sim.Restore(snap);
        sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);

        MatchState after = sim.Snapshot();
        Assert.That((after.StageFlags & StageFlags.BossTimeoutFired) != 0, Is.True);
        Assert.That(MatchStateAccess.Actor(ref after, 0).IsEliminated, Is.True);
        Assert.That(MatchStateAccess.Actor(ref after, 1).IsEliminated, Is.True);
        Assert.That(StageRuntime.BossTimeoutRemaining(in after), Is.EqualTo(0));
    }

    [Test]
    public void Pressworks_timeout_starts_on_Press_phase()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var config = MatchConfig.CreateDefault(Seed ^ 2UL, actorCount: 2);
        unsafe
        {
            config.StageId = SampleContentFactory.PressworksStageId;
            config.PhaseClockPermille = 100;
            config.FighterIds[0] = SampleContentFactory.RookFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);
        Assert.That(StageRuntime.BossTimeoutRemaining(in sim.State), Is.EqualTo(0));

        for (int f = 0; f < 150 + 12; f++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        MatchState press = sim.Snapshot();
        Assert.That(press.PhaseIndex, Is.EqualTo(1));
        Assert.That(
            StageRuntime.BossTimeoutRemaining(in press),
            Is.EqualTo(BossTimeoutRuntime.PressworksTimeoutFrames - press.PhaseElapsedFrames));
    }
}
