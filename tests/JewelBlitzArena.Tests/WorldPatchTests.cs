using JewelBlitzArena.Data;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class WorldPatchTests
{
    [Test]
    public void Smoke_and_Ice_jar_ids_match_MatchRules()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        Assert.That(SampleContentFactory.SmokeJarItemId, Is.EqualTo(MatchRules.SmokeJarItemId));
        Assert.That(SampleContentFactory.IceJarItemId, Is.EqualTo(MatchRules.IceJarItemId));
        Assert.That(catalog.TryGetItem(MatchRules.SmokeJarItemId, out ItemRecord smoke), Is.True);
        Assert.That(smoke.Name, Is.EqualTo("Smoke Jar"));
        Assert.That(catalog.TryGetItem(MatchRules.IceJarItemId, out ItemRecord ice), Is.True);
        Assert.That(ice.Name, Is.EqualTo("Ice Jar"));
    }

    [Test]
    public void Ice_Jar_land_spawns_lasting_freeze_patch()
    {
        MatchConfig config = MatchConfig.CreateDefault(0x4943455031UL, actorCount: 2);
        var sim = new Simulation();
        sim.Reset(config);

        int slot = sim.SpawnLooseItem(MatchRules.IceJarItemId, Fix.FromInt(1), Fix.FromInt(2), Fix.Zero);
        Assert.That(slot, Is.GreaterThanOrEqualTo(0));

        MatchState snap = sim.Snapshot();
        snap.Actor0.PosX = Fix.FromInt(-5);
        snap.Actor0.PosZ = Fix.Zero;
        snap.Actor1.PosX = Fix.FromInt(1);
        snap.Actor1.PosZ = Fix.Zero;
        LooseItemSlot item = MatchStateAccess.GetLooseItem(ref snap, slot);
        item.HolderSlot = LooseItemSlot.NoHolder;
        item.InFlight = true;
        item.FlightFrames = 8;
        item.PosX = Fix.FromInt(1);
        item.PosY = PhysicsConstants.ItemRadius;
        item.PosZ = Fix.Zero;
        item.VelX = Fix.Zero;
        item.VelY = Fix.Zero;
        item.VelZ = Fix.Zero;
        MatchStateAccess.SetLooseItem(ref snap, slot, item);
        sim.Restore(snap);

        sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);

        MatchState after = sim.Snapshot();
        Assert.That(WorldPatchRuntime.CountActive(ref after), Is.EqualTo(1));
        // Patch pulse runs after TickActors, so full pulse remains this frame.
        Assert.That(after.Actor1.FreezeFrames, Is.EqualTo(MatchRules.WorldPatchStatusPulseFrames));

        // Patch persists across steps.
        for (int i = 0; i < 10; i++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        after = sim.Snapshot();
        Assert.That(WorldPatchRuntime.CountActive(ref after), Is.EqualTo(1));
        Assert.That(after.Actor1.FreezeFrames, Is.GreaterThan(0));
    }

    [Test]
    public void Smoke_Jar_patch_blinds_actors_in_radius()
    {
        MatchConfig config = MatchConfig.CreateDefault(0x534D4B5031UL, actorCount: 2);
        var sim = new Simulation();
        sim.Reset(config);

        MatchState snap = sim.Snapshot();
        snap.Actor1.PosX = Fix.Zero;
        snap.Actor1.PosZ = Fix.Zero;
        WorldPatchRuntime.Spawn(
            ref snap,
            WorldPatchRuntime.KindSmoke,
            Fix.Zero,
            Fix.Zero,
            MatchRules.SmokeJarBlindRange,
            MatchRules.SmokePatchLifetimeFrames);
        sim.Restore(snap);

        sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        MatchState after = sim.Snapshot();
        Assert.That(after.Actor1.BlindFrames, Is.EqualTo(MatchRules.WorldPatchStatusPulseFrames));
        Assert.That(WorldPatchRuntime.CountActive(ref after), Is.EqualTo(1));
    }

    [Test]
    public void World_patch_expires_after_lifetime()
    {
        MatchConfig config = MatchConfig.CreateDefault(0x5041545845UL, actorCount: 1);
        var sim = new Simulation();
        sim.Reset(config);

        MatchState snap = sim.Snapshot();
        WorldPatchRuntime.Spawn(
            ref snap,
            WorldPatchRuntime.KindIce,
            Fix.Zero,
            Fix.Zero,
            MatchRules.IceJarFreezeRange,
            lifetimeFrames: 3);
        sim.Restore(snap);

        sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        snap = sim.Snapshot();
        Assert.That(WorldPatchRuntime.CountActive(ref snap), Is.EqualTo(1));
        sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        snap = sim.Snapshot();
        Assert.That(WorldPatchRuntime.CountActive(ref snap), Is.EqualTo(1));
        sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        snap = sim.Snapshot();
        Assert.That(WorldPatchRuntime.CountActive(ref snap), Is.EqualTo(0));
    }

    [Test]
    public void Leaving_ice_patch_lets_freeze_expire()
    {
        MatchConfig config = MatchConfig.CreateDefault(0x4C45415631UL, actorCount: 2);
        var sim = new Simulation();
        sim.Reset(config);

        MatchState snap = sim.Snapshot();
        snap.Actor1.PosX = Fix.Zero;
        snap.Actor1.PosZ = Fix.Zero;
        WorldPatchRuntime.Spawn(
            ref snap,
            WorldPatchRuntime.KindIce,
            Fix.Zero,
            Fix.Zero,
            MatchRules.IceJarFreezeRange,
            MatchRules.IcePatchLifetimeFrames);
        sim.Restore(snap);

        sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        Assert.That(sim.Snapshot().Actor1.FreezeFrames, Is.GreaterThan(0));

        // Move foe out of radius.
        snap = sim.Snapshot();
        snap.Actor1.PosX = Fix.FromInt(20);
        snap.Actor1.FreezeFrames = 5;
        sim.Restore(snap);

        for (int i = 0; i < 8; i++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        snap = sim.Snapshot();
        Assert.That(snap.Actor1.FreezeFrames, Is.EqualTo(0));
        Assert.That(WorldPatchRuntime.CountActive(ref snap), Is.EqualTo(1));
    }
}
