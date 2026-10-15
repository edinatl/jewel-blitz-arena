using JewelBlitzArena.Data;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class InvoiceClankSurgeTests
{
    [Test]
    public void Surge_luster_cost_invoice_A_is_180_others_420()
    {
        Assert.That(
            MatchRules.SurgeLusterCostFor(KitBehavior.InvoiceRepeatable, surgeA: true),
            Is.EqualTo(MatchRules.InvoiceSurgeLusterCost));
        Assert.That(
            MatchRules.SurgeLusterCostFor(KitBehavior.InvoiceRepeatable, surgeA: false),
            Is.EqualTo(MatchRules.SurgeLusterCost));
        Assert.That(
            MatchRules.SurgeLusterCostFor(KitBehavior.None, surgeA: true),
            Is.EqualTo(MatchRules.SurgeLusterCost));
        Assert.That(
            MatchRules.SurgeLusterCostFor(KitBehavior.DoubleHold, surgeA: true),
            Is.EqualTo(MatchRules.SurgeLusterCost));
    }

    [Test]
    public void Surge_variant_index_maps_stick_cardinals()
    {
        Assert.That(MatchRules.SurgeVariantIndex(InputFrame.Neutral), Is.EqualTo(0));
        Assert.That(MatchRules.SurgeVariantIndex(InputFrame.FromStick(0, 2)), Is.EqualTo(1));
        Assert.That(MatchRules.SurgeVariantIndex(InputFrame.FromStick(8, 2)), Is.EqualTo(2));
        Assert.That(MatchRules.SurgeVariantIndex(InputFrame.FromStick(24, 2)), Is.EqualTo(3));
        Assert.That(MatchRules.SurgeVariantIndex(InputFrame.FromStick(7, 2)), Is.EqualTo(2));
        Assert.That(MatchRules.SurgeVariantIndex(InputFrame.FromStick(25, 2)), Is.EqualTo(3));
    }

    [Test]
    public void Invoice_SurgeA_charges_180_and_Closing_Time_charges_420()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        MatchConfig config = MatchConfig.CreateDefault(0x494E56313830UL, actorCount: 2);
        unsafe
        {
            config.FighterIds[0] = SampleContentFactory.TillyFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);
        ForceBlitzReady(sim, slot: 0);

        int before = sim.Snapshot().Actor0.Luster;
        var surgeA = InputFrame.Neutral;
        surgeA.SurgeA = true;
        sim.Step(surgeA, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        // Same Step also drains 1 Luster under the house timer.
        int afterA = sim.Snapshot().Actor0.Luster;
        Assert.That(
            before - afterA,
            Is.EqualTo(MatchRules.InvoiceSurgeLusterCost + MatchRules.LusterDrainPerFrame));
        Assert.That(sim.Snapshot().Actor0.Action, Is.EqualTo((byte)ActorAction.SurgeA));
        Assert.That(sim.Snapshot().Actor0.MoveDamage, Is.EqualTo(135)); // 3×45 authored

        // Wait out Invoice frames, then Closing Time (Surge B) at full standard cost.
        DrainAction(sim, slot: 0);
        MatchState snap = sim.Snapshot();
        snap.Actor0.Luster = MatchRules.LusterMax;
        snap.Actor0.IsBlitzed = true;
        snap.Actor0.TransformFrames = 0;
        sim.Restore(snap);

        before = sim.Snapshot().Actor0.Luster;
        var surgeB = InputFrame.Neutral;
        surgeB.SurgeB = true;
        sim.Step(surgeB, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        Assert.That(
            before - sim.Snapshot().Actor0.Luster,
            Is.EqualTo(MatchRules.SurgeLusterCost + MatchRules.LusterDrainPerFrame));
    }

    [Test]
    public void Clank_kit_loads_eight_surge_variants()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        Assert.That(catalog.TryGetFighter(SampleContentFactory.ClankFighterId, out FighterKitRecord rec), Is.True);
        int surgeCount = 0;
        foreach (MoveRecord m in rec.Moves)
        {
            if (m.Class == MoveClass.Surge)
            {
                surgeCount++;
            }
        }

        Assert.That(surgeCount, Is.EqualTo(8));

        KitRuntime kit = KitRuntime.FromRecord(rec);
        Assert.That(kit.HasSurgeVariants, Is.True);
        Assert.That(kit.SurgeAVariants[0]!.Name, Is.EqualTo("Toolbox Mortar"));
        Assert.That(kit.SurgeAVariants[1]!.Damage, Is.EqualTo(280));
        Assert.That(kit.SurgeAVariants[2]!.Damage, Is.EqualTo(320));
        Assert.That(kit.SurgeAVariants[3]!.Damage, Is.EqualTo(70));
        Assert.That(kit.SurgeBVariants[0]!.Damage, Is.EqualTo(0));
        Assert.That(kit.SurgeBVariants[1]!.Name, Is.EqualTo("Spare Flame"));
        Assert.That(kit.SurgeBVariants[3]!.Damage, Is.EqualTo(280));

        Assert.That(kit.ResolveSurgeA(InputFrame.Neutral)!.Name, Is.EqualTo("Toolbox Mortar"));
        Assert.That(kit.ResolveSurgeA(InputFrame.FromStick(0, 2))!.Name, Is.EqualTo("Toolbox Ram"));
        Assert.That(kit.ResolveSurgeA(InputFrame.FromStick(8, 2))!.Name, Is.EqualTo("Toolbox Drop Weight"));
        Assert.That(kit.ResolveSurgeB(InputFrame.FromStick(24, 2))!.Name, Is.EqualTo("Spare Repair"));
    }

    [Test]
    public void Clank_SurgeA_direction_selects_variant_damage()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        MatchConfig config = MatchConfig.CreateDefault(0x434C4E4B31UL, actorCount: 2);
        unsafe
        {
            config.FighterIds[0] = SampleContentFactory.ClankFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);
        ForceBlitzReady(sim, slot: 0);

        // Up stick → Drop Weight (320).
        var upSurge = InputFrame.FromStick(8, 2);
        upSurge.SurgeA = true;
        sim.Step(upSurge, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        Assert.That(sim.Snapshot().Actor0.Action, Is.EqualTo((byte)ActorAction.SurgeA));
        Assert.That(sim.Snapshot().Actor0.MoveDamage, Is.EqualTo(320));
        Assert.That(
            MatchRules.LusterMax - sim.Snapshot().Actor0.Luster,
            Is.EqualTo(MatchRules.SurgeLusterCost + MatchRules.LusterDrainPerFrame));
    }

    [Test]
    public void Bulwark_command_grab_startup_is_twelve()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        Assert.That(catalog.TryGetFighter(SampleContentFactory.BulwarkFighterId, out FighterKitRecord rec), Is.True);
        KitRuntime kit = KitRuntime.FromRecord(rec);
        Assert.That(kit.Strong!.StartupFrames, Is.EqualTo(12));
        Assert.That(kit.Strong.Name, Is.EqualTo("Command Grab"));
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

    private static void DrainAction(Simulation sim, int slot)
    {
        for (int i = 0; i < 80; i++)
        {
            MatchState snap = sim.Snapshot();
            if (MatchStateAccess.Actor(ref snap, slot).Action == (byte)ActorAction.Neutral)
            {
                return;
            }

            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }
    }
}
