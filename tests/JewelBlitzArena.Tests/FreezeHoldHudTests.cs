using JewelBlitzArena.Data;
using JewelBlitzArena.PresentationBridge;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class FreezeHoldHudTests
{
    [Test]
    public void Petrify_Gas_item_id_matches_MatchRules()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        Assert.That(SampleContentFactory.PetrifyGasItemId, Is.EqualTo(MatchRules.PetrifyGasItemId));
        Assert.That(catalog.TryGetItem(MatchRules.PetrifyGasItemId, out ItemRecord item), Is.True);
        Assert.That(item.Name, Is.EqualTo("Petrify Gas"));
        Assert.That(item.Category, Is.EqualTo(ItemCategory.Status));
    }

    [Test]
    public void ApplyFreeze_blocks_action_and_ticks_down()
    {
        MatchConfig config = MatchConfig.CreateDefault(0x46525A4531UL, actorCount: 2);
        var sim = new Simulation();
        sim.Reset(config);

        MatchState snap = sim.Snapshot();
        StatusRuntime.ApplyFreeze(ref snap, 1, MatchRules.FreezeDurationFrames);
        sim.Restore(snap);

        Assert.That(sim.Snapshot().Actor1.FreezeFrames, Is.EqualTo(MatchRules.FreezeDurationFrames));
        Assert.That(sim.Snapshot().Actor1.CanAct, Is.False);

        sim.Step(InputFrame.Neutral, InputFrame.FromStick(0, 2), InputFrame.Neutral, InputFrame.Neutral);
        MatchState after = sim.Snapshot();
        Assert.That(after.Actor1.FreezeFrames, Is.EqualTo(MatchRules.FreezeDurationFrames - 1));
        Assert.That(after.Actor1.VelX.Raw, Is.EqualTo(0));
        Assert.That(after.Actor1.VelZ.Raw, Is.EqualTo(0));
    }

    [Test]
    public void Freeze_halves_damage_and_100_plus_breaks()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        MatchConfig config = MatchConfig.CreateDefault(0x46524B4231UL, actorCount: 2);
        unsafe
        {
            config.FighterIds[0] = SampleContentFactory.TeoFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);
        KitRuntime kit = KitRuntime.FromRecord(catalog.Fighters[
            IndexOf(catalog, SampleContentFactory.TeoFighterId)]);
        Assert.That(kit.Attack, Is.Not.Null);
        int total = kit.Attack!.StartupFrames + kit.Attack.ActiveFrames + kit.Attack.RecoveryFrames;

        MatchState snap = sim.Snapshot();
        snap.Actor1.FreezeFrames = 60;
        snap.Actor1.Life = 500;
        snap.Actor1.MaxLife = 500;
        snap.Actor0.MoveDamage = 120;
        snap.Actor0.Action = (byte)ActorAction.Attack;
        // TickAction decrements first; choose timer so elapsed lands in Active.
        snap.Actor0.ActionTimer = total - kit.Attack.StartupFrames;
        snap.Actor0.ActionPhase = (byte)ActionPhase.Active;
        snap.Actor0.HitApplied = 0;
        snap.Actor0.PosX = Fix.Zero;
        snap.Actor0.PosZ = Fix.Zero;
        snap.Actor1.PosX = Fix.FromInt(1);
        snap.Actor1.PosZ = Fix.Zero;
        sim.Restore(snap);

        int lifeBefore = sim.Snapshot().Actor1.Life;
        sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        MatchState after = sim.Snapshot();

        // 120 raw → 50% = 60 while frozen; freeze clears because raw ≥ 100.
        Assert.That(after.Actor1.Life, Is.EqualTo(lifeBefore - 60));
        Assert.That(after.Actor1.FreezeFrames, Is.EqualTo(0));
        Assert.That(after.Actor1.FreezeImmuneFrames, Is.EqualTo(MatchRules.FreezeImmuneFrames));
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

    [Test]
    public void Freeze_immunity_blocks_reapply()
    {
        MatchConfig config = MatchConfig.CreateDefault(0x4652494D31UL, actorCount: 2);
        var sim = new Simulation();
        sim.Reset(config);

        MatchState snap = sim.Snapshot();
        snap.Actor1.FreezeImmuneFrames = 30;
        sim.Restore(snap);

        snap = sim.Snapshot();
        StatusRuntime.ApplyFreeze(ref snap, 1, MatchRules.FreezeDurationFrames);
        Assert.That(snap.Actor1.FreezeFrames, Is.EqualTo(0));
    }

    [Test]
    public void MatchHoldHud_reports_left_and_right_for_Clank()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        MatchConfig config = MatchConfig.CreateDefault(0x48414E4453UL, actorCount: 2);
        unsafe
        {
            config.FighterIds[0] = SampleContentFactory.ClankFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);

        int right = sim.SpawnLooseItem(1, Fix.Zero, Fix.Zero, Fix.Zero);
        int left = sim.SpawnLooseItem(2, Fix.Zero, Fix.Zero, Fix.One / 4);
        Assert.That(right, Is.GreaterThanOrEqualTo(0));
        Assert.That(left, Is.GreaterThanOrEqualTo(0));

        MatchState snap = sim.Snapshot();
        LooseItemSlot ir = MatchStateAccess.GetLooseItem(ref snap, right);
        ir.HolderSlot = 0;
        MatchStateAccess.SetLooseItem(ref snap, right, ir);
        LooseItemSlot il = MatchStateAccess.GetLooseItem(ref snap, left);
        il.HolderSlot = 0;
        MatchStateAccess.SetLooseItem(ref snap, left, il);
        snap.Actor0.HeldItemSlot = (byte)right;
        snap.Actor0.HeldItemSlotLeft = (byte)left;
        sim.Restore(snap);

        snap = sim.Snapshot();
        MatchHoldHud hold = MatchHoldHud.FromActor(ref snap, in snap.Actor0);
        Assert.That(hold.HasRight, Is.True);
        Assert.That(hold.HasLeft, Is.True);
        Assert.That(hold.RightItemId, Is.EqualTo(1));
        Assert.That(hold.LeftItemId, Is.EqualTo(2));
        Assert.That(hold.FormatHands(), Is.EqualTo("R:1 L:2"));
    }

    [Test]
    public void EdgeBubble_carries_dual_hand_ids()
    {
        var volume = new CameraVolume(0f, 8f, 0f, 4f);
        var state = new MatchState { ActorCount = 1 };
        state.Actor0.IsAlive = true;
        state.Actor0.PosX = Fix.FromInt(-20);
        state.Actor0.PosZ = Fix.Zero;

        int right = 0;
        int left = 1;
        MatchStateAccess.SetLooseItem(ref state, right, new LooseItemSlot
        {
            ItemId = 7,
            HolderSlot = 0,
            Flags = LooseItemSlot.FlagActive,
        });
        MatchStateAccess.SetLooseItem(ref state, left, new LooseItemSlot
        {
            ItemId = 9,
            HolderSlot = 0,
            Flags = LooseItemSlot.FlagActive,
        });
        state.Actor0.HeldItemSlot = (byte)right;
        state.Actor0.HeldItemSlotLeft = (byte)left;

        CameraFrame frame = StageCameraVolumes.FrameActors(ref state, in volume, padding: 1f);
        Assert.That(frame.FitFailed, Is.True);
        Assert.That(frame.EdgeBubbleCount, Is.EqualTo(1));
        EdgeBubble b = frame.GetEdgeBubble(0);
        Assert.That(b.HasHeldItemRight, Is.True);
        Assert.That(b.HasHeldItemLeft, Is.True);
        Assert.That(b.HeldItemIdRight, Is.EqualTo(7));
        Assert.That(b.HeldItemIdLeft, Is.EqualTo(9));
        Assert.That(b.HeldItemId, Is.EqualTo(7));
    }
}
