using JewelBlitzArena.Data;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class WaterVolumeRuntimeTests
{
    private const ulong Seed = 0x5741545F564F4C31UL; // WAT_VOL1

    [Test]
    public void Sunken_vault_door_and_breach_have_water()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var config = MatchConfig.CreateDefault(Seed, actorCount: 2);
        unsafe
        {
            config.StageId = SampleContentFactory.SunkenStrongroomStageId;
            config.PhaseClockPermille = 100;
            config.FighterIds[0] = SampleContentFactory.RookFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);

        StageGraph graph = sim.StageGraph;
        MatchState floor = sim.Snapshot();
        Assert.That(floor.PhaseIndex, Is.EqualTo(0));
        Assert.That(WaterVolumeRuntime.IsActorInWater(ref floor, graph, 0), Is.False);

        // Vault floor 25s @ 10% clock = 150f + transition 12f → Vault door.
        for (int f = 0; f < 150 + 12; f++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        MatchState door = sim.Snapshot();
        Assert.That(door.PhaseIndex, Is.EqualTo(1));
        {
            ref ActorSlot a0 = ref MatchStateAccess.Actor(ref door, 0);
            a0.PosX = Fix.Zero;
            a0.PosZ = Fix.Zero;
        }

        Assert.That(WaterVolumeRuntime.IsActorInWater(ref door, graph, 0), Is.True);

        // Vault door another 150f + 12f transition → Hull breach.
        sim.Restore(door);
        for (int f = 0; f < 150 + 12; f++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        MatchState breach = sim.Snapshot();
        Assert.That(breach.PhaseIndex, Is.EqualTo(2));
        {
            ref ActorSlot a0 = ref MatchStateAccess.Actor(ref breach, 0);
            a0.PosX = Fix.Zero;
            a0.PosZ = Fix.Zero;
        }

        Assert.That(WaterVolumeRuntime.IsActorInWater(ref breach, graph, 0), Is.True);
    }

    [Test]
    public void Rime_flooding_has_water_dry_yard_does_not()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var config = MatchConfig.CreateDefault(Seed ^ 1UL, actorCount: 2);
        unsafe
        {
            config.StageId = SampleContentFactory.RimeCutworksStageId;
            config.PhaseClockPermille = 100;
            config.FighterIds[0] = SampleContentFactory.RookFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);
        StageGraph graph = sim.StageGraph;
        MatchState dry = sim.Snapshot();
        Assert.That(WaterVolumeRuntime.IsActorInWater(ref dry, graph, 0), Is.False);

        for (int f = 0; f < 150 + 12; f++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        MatchState flood = sim.Snapshot();
        Assert.That(flood.PhaseIndex, Is.EqualTo(1));
        {
            ref ActorSlot a0 = ref MatchStateAccess.Actor(ref flood, 0);
            a0.PosX = Fix.Zero;
            a0.PosZ = Fix.Zero;
        }

        Assert.That(WaterVolumeRuntime.IsActorInWater(ref flood, graph, 0), Is.True);
    }

    [Test]
    public void Water_walk_slow_is_twenty_percent()
    {
        Fix full = Fix.FromInt(5);
        Fix slowed = WaterVolumeRuntime.ApplyWalkSlow(full);
        Assert.That(slowed.Raw, Is.EqualTo((full.Raw * 800) / 1000));
    }
}
