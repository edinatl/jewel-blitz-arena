using JewelBlitzArena.Data;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class StatusMagnetTests
{
    [Test]
    public void Bateria_stuns_grounded_foe_in_range()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        MatchConfig config = MatchConfig.CreateDefault(0x4254525331UL, actorCount: 2);
        unsafe
        {
            config.FighterIds[0] = SampleContentFactory.TeoFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);
        ForceBlitzReady(sim, 0);

        MatchState snap = sim.Snapshot();
        snap.Actor0.PosX = Fix.Zero;
        snap.Actor0.PosZ = Fix.Zero;
        snap.Actor1.PosX = Fix.FromInt(4);
        snap.Actor1.PosZ = Fix.Zero;
        snap.Actor1.PosY = Fix.Zero;
        sim.Restore(snap);

        var bateria = InputFrame.Neutral;
        bateria.SurgeB = true;
        sim.Step(bateria, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);

        MatchState after = sim.Snapshot();
        Assert.That(after.Actor0.Action, Is.EqualTo((byte)ActorAction.SurgeB));
        Assert.That(after.Actor0.MoveDamage, Is.EqualTo(60));
        // Same Step ticks StunFrames once.
        Assert.That(after.Actor1.StunFrames, Is.EqualTo(MatchRules.BateriaStunFrames - 1));
    }

    [Test]
    public void Bateria_skips_airborne_foe()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        MatchConfig config = MatchConfig.CreateDefault(0x4254525332UL, actorCount: 2);
        unsafe
        {
            config.FighterIds[0] = SampleContentFactory.TeoFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);
        ForceBlitzReady(sim, 0);

        MatchState snap = sim.Snapshot();
        snap.Actor1.PosX = Fix.FromInt(3);
        snap.Actor1.PosY = Fix.FromInt(2);
        sim.Restore(snap);

        var bateria = InputFrame.Neutral;
        bateria.SurgeB = true;
        sim.Step(bateria, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        Assert.That(sim.Snapshot().Actor1.StunFrames, Is.EqualTo(0));
    }

    [Test]
    public void Chaff_blinds_foe_and_blocks_pickup()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        MatchConfig config = MatchConfig.CreateDefault(0x4348464631UL, actorCount: 2);
        unsafe
        {
            config.FighterIds[0] = SampleContentFactory.ClankFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);
        ForceBlitzReady(sim, 0);

        MatchState snap = sim.Snapshot();
        snap.Actor0.PosX = Fix.Zero;
        snap.Actor0.PosZ = Fix.Zero;
        snap.Actor1.PosX = Fix.FromInt(2);
        snap.Actor1.PosZ = Fix.Zero;
        sim.Restore(snap);

        // Up + Surge B → Spare Chaff (Blind).
        var chaff = InputFrame.FromStick(8, 2);
        chaff.SurgeB = true;
        sim.Step(chaff, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);

        MatchState after = sim.Snapshot();
        Assert.That(after.Actor0.MoveDamage, Is.EqualTo(40));
        Assert.That(after.Actor1.BlindFrames, Is.EqualTo(MatchRules.BlindDurationFrames - 1));

        // Spawn item on foe; Action pickup must fail while Blinded.
        int itemSlot = sim.SpawnLooseItem(1, Fix.FromInt(2), Fix.Zero, Fix.Zero);
        Assert.That(itemSlot, Is.GreaterThanOrEqualTo(0));
        var pickup = InputFrame.Neutral;
        pickup.Action = true;
        sim.Step(InputFrame.Neutral, pickup, InputFrame.Neutral, InputFrame.Neutral);
        Assert.That(sim.Snapshot().Actor1.HasEmptyHands, Is.True);
        Assert.That(sim.Snapshot().Actor1.BlindFrames, Is.GreaterThan(0));
    }

    [Test]
    public void Blinded_does_not_auto_face()
    {
        MatchConfig config = MatchConfig.CreateDefault(0x424C4E4431UL, actorCount: 2);
        var sim = new Simulation();
        sim.Reset(config);
        MatchState snap = sim.Snapshot();
        snap.Actor0.Facing = 0;
        snap.Actor0.BlindFrames = 30;
        sim.Restore(snap);

        // Stick −X would normally flip Facing to 1.
        sim.Step(InputFrame.FromStick(16, 2), InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        Assert.That(sim.Snapshot().Actor0.Facing, Is.EqualTo(0));
        Assert.That(sim.Snapshot().Actor0.BlindFrames, Is.EqualTo(29));
    }

    [Test]
    public void Magnet_catches_nearest_into_empty_hands()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        MatchConfig config = MatchConfig.CreateDefault(0x4D41474331UL, actorCount: 2);
        unsafe
        {
            config.FighterIds[0] = SampleContentFactory.ClankFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);

        int near = sim.SpawnLooseItem(1, Fix.FromInt(1), Fix.Zero, Fix.Zero);
        int far = sim.SpawnLooseItem(2, Fix.FromInt(3), Fix.Zero, Fix.Zero);
        Assert.That(near, Is.GreaterThanOrEqualTo(0));
        Assert.That(far, Is.GreaterThanOrEqualTo(0));

        MatchState snap = sim.Snapshot();
        snap.Actor0.PosX = Fix.Zero;
        snap.Actor0.PosZ = Fix.Zero;
        snap.Actor0.HeldItemSlot = ActorSlot.NoHeldItem;
        sim.Restore(snap);
        ForceBlitzReady(sim, 0);

        var magnet = InputFrame.Neutral;
        magnet.SurgeB = true;
        sim.Step(magnet, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);

        MatchState after = sim.Snapshot();
        Assert.That(after.Actor0.HeldItemSlot, Is.EqualTo((byte)near));
        LooseItemSlot held = MatchStateAccess.GetLooseItem(ref after, near);
        Assert.That(held.HolderSlot, Is.EqualTo(0));
        // Far item snapped to pickup ring, still on field.
        LooseItemSlot piled = MatchStateAccess.GetLooseItem(ref after, far);
        Assert.That(piled.HolderSlot, Is.EqualTo(LooseItemSlot.NoHolder));
        Assert.That(piled.IsActive, Is.True);
        Assert.That(System.Math.Abs(piled.PosX.Raw - after.Actor0.PosX.Raw), Is.LessThan(Fix.FromInt(2).Raw));
    }

    [Test]
    public void Authored_Bateria_and_Chaff_compile_effects()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        KitRuntime teo = KitRuntime.FromRecord(catalog.Fighters[
            IndexOf(catalog, SampleContentFactory.TeoFighterId)]);
        Assert.That(teo.SurgeB!.Name, Is.EqualTo("Bateria"));
        Assert.That(teo.SurgeB.Effect, Is.EqualTo(MoveEffect.Stun));

        KitRuntime clank = KitRuntime.FromRecord(catalog.Fighters[
            IndexOf(catalog, SampleContentFactory.ClankFighterId)]);
        Assert.That(clank.SurgeBVariants[2]!.Name, Is.EqualTo("Spare Chaff"));
        Assert.That(clank.SurgeBVariants[2]!.Effect, Is.EqualTo(MoveEffect.Blind));
    }

    private static int IndexOf(ContentCatalog catalog, int fighterId)
    {
        for (int i = 0; i < catalog.Fighters.Length; i++)
        {
            if (catalog.Fighters[i].FighterId == fighterId)
            {
                return i;
            }
        }

        return -1;
    }

    private static void ForceBlitzReady(Simulation sim, int slot)
    {
        MatchState snap = sim.Snapshot();
        ref ActorSlot actor = ref MatchStateAccess.Actor(ref snap, slot);
        actor.IsBlitzed = true;
        actor.Luster = MatchRules.LusterMax;
        actor.TransformFrames = 0;
        actor.InvulnFrames = 0;
        actor.Action = (byte)ActorAction.Neutral;
        actor.ActionTimer = 0;
        actor.ActionPhase = (byte)ActionPhase.None;
        sim.Restore(snap);
    }
}
