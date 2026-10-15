using JewelBlitzArena.Data;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class FacingHealSurgeTests
{
    [Test]
    public void Surge_variant_forward_is_facing_relative()
    {
        // Facing −X: stick −X is forward; stick +X is still forward variant (no back).
        Assert.That(MatchRules.SurgeVariantIndex(InputFrame.FromStick(16, 2), facing: 1), Is.EqualTo(1));
        Assert.That(MatchRules.SurgeVariantIndex(InputFrame.FromStick(0, 2), facing: 1), Is.EqualTo(1));
        Assert.That(MatchRules.SurgeVariantIndex(InputFrame.FromStick(0, 2), facing: 0), Is.EqualTo(1));
        // Up still wins when closer to +Z than to facing axis.
        Assert.That(MatchRules.SurgeVariantIndex(InputFrame.FromStick(8, 2), facing: 1), Is.EqualTo(2));
        Assert.That(MatchRules.SurgeVariantIndex(InputFrame.FromStick(24, 2), facing: 0), Is.EqualTo(3));
    }

    [Test]
    public void Clank_resolves_forward_ram_using_facing()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        Assert.That(catalog.TryGetFighter(SampleContentFactory.ClankFighterId, out FighterKitRecord rec), Is.True);
        KitRuntime kit = KitRuntime.FromRecord(rec);

        Assert.That(kit.ResolveSurgeA(InputFrame.FromStick(16, 2), facing: 1)!.Name, Is.EqualTo("Toolbox Ram"));
        Assert.That(kit.ResolveSurgeA(InputFrame.FromStick(0, 2), facing: 0)!.Name, Is.EqualTo("Toolbox Ram"));
        Assert.That(kit.ResolveSurgeA(InputFrame.FromStick(8, 2), facing: 1)!.Name, Is.EqualTo("Toolbox Drop Weight"));
    }

    [Test]
    public void Spare_Repair_heals_self_and_does_not_set_MoveDamage()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        MatchConfig config = MatchConfig.CreateDefault(0x4845414C31UL, actorCount: 2);
        unsafe
        {
            config.FighterIds[0] = SampleContentFactory.ClankFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);
        ForceBlitzReady(sim, 0);

        MatchState snap = sim.Snapshot();
        snap.Actor0.Life = 500;
        snap.Actor0.Facing = 0;
        sim.Restore(snap);

        // Down + Surge B → Spare Repair (heal 280).
        var repair = InputFrame.FromStick(24, 2);
        repair.SurgeB = true;
        sim.Step(repair, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);

        MatchState after = sim.Snapshot();
        Assert.That(after.Actor0.Action, Is.EqualTo((byte)ActorAction.SurgeB));
        Assert.That(after.Actor0.MoveDamage, Is.EqualTo(0));
        Assert.That(after.Actor0.Life, Is.EqualTo(780)); // 500 + 280
    }

    [Test]
    public void Spare_Repair_clamps_to_MaxLife()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        MatchConfig config = MatchConfig.CreateDefault(0x4845414C32UL, actorCount: 2);
        unsafe
        {
            config.FighterIds[0] = SampleContentFactory.ClankFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);
        ForceBlitzReady(sim, 0);

        MatchState snap = sim.Snapshot();
        int max = snap.Actor0.MaxLife;
        snap.Actor0.Life = max - 10;
        sim.Restore(snap);

        var repair = InputFrame.FromStick(24, 2);
        repair.SurgeB = true;
        sim.Step(repair, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        Assert.That(sim.Snapshot().Actor0.Life, Is.EqualTo(max));
    }

    [Test]
    public void Spare_Magnet_strips_held_item_in_range()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        MatchConfig config = MatchConfig.CreateDefault(0x4D41474E31UL, actorCount: 2);
        unsafe
        {
            config.FighterIds[0] = SampleContentFactory.ClankFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);

        // Place foe in range with a held item.
        int itemSlot = sim.SpawnLooseItem(1, Fix.FromInt(2), Fix.Zero, Fix.Zero);
        Assert.That(itemSlot, Is.GreaterThanOrEqualTo(0));

        MatchState snap = sim.Snapshot();
        snap.Actor0.PosX = Fix.Zero;
        snap.Actor0.PosZ = Fix.Zero;
        snap.Actor1.PosX = Fix.FromInt(2);
        snap.Actor1.PosZ = Fix.Zero;
        LooseItemSlot item = MatchStateAccess.GetLooseItem(ref snap, itemSlot);
        item.HolderSlot = 1;
        item.InFlight = false;
        MatchStateAccess.SetLooseItem(ref snap, itemSlot, item);
        snap.Actor1.HeldItemSlot = (byte)itemSlot;
        sim.Restore(snap);

        ForceBlitzReady(sim, 0);

        // Neutral Surge B → Spare Magnet.
        var magnet = InputFrame.Neutral;
        magnet.SurgeB = true;
        sim.Step(magnet, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);

        MatchState after = sim.Snapshot();
        Assert.That(after.Actor1.HasEmptyHands, Is.True);
        LooseItemSlot dropped = MatchStateAccess.GetLooseItem(ref after, itemSlot);
        Assert.That(dropped.HolderSlot, Is.EqualTo(LooseItemSlot.NoHolder));
        Assert.That(dropped.IsActive, Is.True);
        Assert.That(after.Actor0.MoveDamage, Is.EqualTo(0));
    }

    [Test]
    public void Smoke_Vault_is_utility_with_zero_MoveDamage()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        Assert.That(catalog.TryGetFighter(SampleContentFactory.SableFighterId, out FighterKitRecord rec), Is.True);
        KitRuntime kit = KitRuntime.FromRecord(rec);
        Assert.That(kit.SurgeB!.Effect, Is.EqualTo(MoveEffect.Utility));

        MatchConfig config = MatchConfig.CreateDefault(0x534D4B5631UL, actorCount: 2);
        unsafe
        {
            config.FighterIds[0] = SampleContentFactory.SableFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);
        ForceBlitzReady(sim, 0);

        var surgeB = InputFrame.Neutral;
        surgeB.SurgeB = true;
        sim.Step(surgeB, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        Assert.That(sim.Snapshot().Actor0.MoveDamage, Is.EqualTo(0));
        Assert.That(sim.Snapshot().Actor0.Action, Is.EqualTo((byte)ActorAction.SurgeB));
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
