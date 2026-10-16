using JewelBlitzArena.Data;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class BreakableRuntimeTests
{
    private const ulong Seed = 0x42524B5F43524154UL; // BRK_CRAT

    [Test]
    public void Harvester_arms_four_terrace_crates()
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
        MatchState snap = sim.Snapshot();
        Assert.That(BreakableRuntime.CountActive(ref snap), Is.EqualTo(4));
    }

    [Test]
    public void Attack_opens_crate_and_spawns_drop()
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
        GenericPoolSlot crate0 = MatchStateAccess.GetCrate(ref snap, 0);
        Assert.That((crate0.Flags & BreakableRuntime.FlagActive) != 0, Is.True);

        int jewelsBefore = JewelRuntime.CountTowardCap(ref snap);
        int itemsBefore = CrateSpawner.CountActiveItems(ref snap);

        // Park attacker on the crate and force Active Attack.
        // Jab stub: startup 4 / active 2 / recovery 8 (total 14). ActionTimer must land
        // elapsed in the Active window after TickAction decrements (elapsed = 14 - timer).
        {
            ref ActorSlot a0 = ref MatchStateAccess.Actor(ref snap, 0);
            a0.PosX = crate0.PosX;
            a0.PosZ = crate0.PosZ;
            a0.PosY = Fix.Zero;
            a0.Action = (byte)ActorAction.Attack;
            a0.ActionPhase = (byte)ActionPhase.Active;
            a0.ActionTimer = 10;
            a0.HitApplied = 0;
            a0.MoveDamage = 35;
        }

        sim.Restore(snap);
        sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);

        MatchState after = sim.Snapshot();
        Assert.That(
            (MatchStateAccess.GetCrate(ref after, 0).Flags & BreakableRuntime.FlagActive) == 0,
            Is.True,
            "crate should be broken");

        int jewelsAfter = JewelRuntime.CountTowardCap(ref after);
        int itemsAfter = CrateSpawner.CountActiveItems(ref after);
        Assert.That(
            jewelsAfter > jewelsBefore || itemsAfter > itemsBefore,
            Is.True,
            "broken crate should drop a jewel or loose item");
    }

    [Test]
    public void Citrine_has_no_breakables_yet()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var config = MatchConfig.CreateDefault(Seed ^ 2UL, actorCount: 2);
        unsafe
        {
            config.StageId = SampleContentFactory.CitrineBazaarStageId;
            config.FighterIds[0] = SampleContentFactory.RookFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);
        MatchState snap = sim.Snapshot();
        Assert.That(BreakableRuntime.CountActive(ref snap), Is.EqualTo(0));
    }
}
