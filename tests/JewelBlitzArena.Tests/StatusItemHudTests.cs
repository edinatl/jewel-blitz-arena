using JewelBlitzArena.Data;
using JewelBlitzArena.PresentationBridge;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class StatusItemHudTests
{
    [Test]
    public void Status_item_ids_match_MatchRules()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        Assert.That(SampleContentFactory.AdhesiveSprayItemId, Is.EqualTo(MatchRules.AdhesiveSprayItemId));
        Assert.That(SampleContentFactory.IceJarItemId, Is.EqualTo(MatchRules.IceJarItemId));
        Assert.That(catalog.TryGetItem(MatchRules.AdhesiveSprayItemId, out ItemRecord adhesive), Is.True);
        Assert.That(adhesive.Name, Is.EqualTo("Adhesive Spray"));
        Assert.That(catalog.TryGetItem(MatchRules.IceJarItemId, out ItemRecord ice), Is.True);
        Assert.That(ice.Name, Is.EqualTo("Ice Jar"));
    }

    [Test]
    public void Adhesive_Spray_thrown_applies_Slow()
    {
        MatchConfig config = MatchConfig.CreateDefault(0x4144485331UL, actorCount: 2);
        var sim = new Simulation();
        sim.Reset(config);

        int slot = sim.SpawnLooseItem(MatchRules.AdhesiveSprayItemId, Fix.FromInt(2), Fix.Zero, Fix.Zero);
        Assert.That(slot, Is.GreaterThanOrEqualTo(0));

        MatchState snap = sim.Snapshot();
        snap.Actor0.PosX = Fix.FromInt(-4);
        snap.Actor0.PosZ = Fix.Zero;
        snap.Actor1.PosX = Fix.FromInt(2);
        snap.Actor1.PosZ = Fix.Zero;
        LooseItemSlot item = MatchStateAccess.GetLooseItem(ref snap, slot);
        item.HolderSlot = LooseItemSlot.NoHolder;
        item.InFlight = true;
        item.FlightFrames = 8; // past thrower grace window
        item.PosX = Fix.FromInt(2);
        item.PosZ = Fix.Zero;
        MatchStateAccess.SetLooseItem(ref snap, slot, item);
        sim.Restore(snap);

        sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        // Same Step applies Slow then ticks timers once.
        Assert.That(sim.Snapshot().Actor1.SlowFrames, Is.EqualTo(MatchRules.AdhesiveSlowFrames - 1));
    }

    [Test]
    public void Ice_Jar_impact_freezes_nearby_actors()
    {
        MatchConfig config = MatchConfig.CreateDefault(0x4943454A31UL, actorCount: 3);
        var sim = new Simulation();
        sim.Reset(config);

        MatchState snap = sim.Snapshot();
        snap.Actor0.PosX = Fix.Zero;
        snap.Actor0.PosZ = Fix.Zero;
        snap.Actor1.PosX = Fix.FromInt(1);
        snap.Actor1.PosZ = Fix.Zero;
        snap.Actor2.PosX = Fix.FromInt(1);
        snap.Actor2.PosZ = Fix.FromInt(1);
        StatusRuntime.ApplyFreezeInRadius(
            ref snap,
            Fix.FromInt(1),
            Fix.Zero,
            MatchRules.IceJarFreezeRange,
            MatchRules.FreezeDurationFrames,
            excludeActor: 0);
        Assert.That(snap.Actor1.FreezeFrames, Is.EqualTo(MatchRules.FreezeDurationFrames));
        Assert.That(snap.Actor2.FreezeFrames, Is.EqualTo(MatchRules.FreezeDurationFrames));
        Assert.That(snap.Actor0.FreezeFrames, Is.EqualTo(0));
    }

    [Test]
    public void MatchStatusHud_formats_active_and_immunity()
    {
        var actor = new ActorSlot
        {
            SlowFrames = 40,
            FreezeImmuneFrames = 10,
            DizzyImmuneFrames = 5,
        };
        MatchStatusHud hud = MatchStatusHud.FromActor(in actor);
        Assert.That(hud.HasActiveStatus, Is.True);
        Assert.That(hud.HasImmunityHalo, Is.True);
        Assert.That(hud.FormatActive(), Does.Contain("SLW 40"));
        Assert.That(hud.FormatImmunity(), Is.EqualTo("IMM DZY FRZ"));
    }

    [Test]
    public void EdgeBubble_carries_immunity_halo()
    {
        var volume = new CameraVolume(0f, 8f, 0f, 4f);
        var state = new MatchState { ActorCount = 1 };
        state.Actor0.IsAlive = true;
        state.Actor0.PosX = Fix.FromInt(-20);
        state.Actor0.PosZ = Fix.Zero;
        state.Actor0.HeldItemSlot = ActorSlot.NoHeldItem;
        state.Actor0.HeldItemSlotLeft = ActorSlot.NoHeldItem;
        state.Actor0.PinImmuneFrames = 50;

        CameraFrame frame = StageCameraVolumes.FrameActors(ref state, in volume, padding: 1f);
        Assert.That(frame.FitFailed, Is.True);
        EdgeBubble b = frame.GetEdgeBubble(0);
        Assert.That(b.HasImmunityHalo, Is.True);
    }
}
