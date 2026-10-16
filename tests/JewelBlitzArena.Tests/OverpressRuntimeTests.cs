using JewelBlitzArena.Data;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class OverpressRuntimeTests
{
    private const ulong Seed = 0x4F5652505F4C4645UL; // OVRP_LFE

    [Test]
    public void Vitre_body_arms_on_Press_phase_only()
    {
        var sim = ResetPressworks(Seed, phaseClockPermille: 100);
        MatchState sorting = sim.Snapshot();
        Assert.That(sorting.PhaseIndex, Is.EqualTo(0));
        Assert.That(VitreBossRuntime.BodyLife(ref sorting), Is.EqualTo(0));

        AdvanceToPress(sim);
        MatchState press = sim.Snapshot();
        Assert.That(press.PhaseIndex, Is.EqualTo(1));
        Assert.That(VitreBossRuntime.BodyLife(ref press), Is.EqualTo(VitreBossRuntime.VitreMaxLife));
    }

    [Test]
    public void Attack_damages_Vitre_body()
    {
        var sim = ResetPressworks(Seed ^ 1UL, phaseClockPermille: 100);
        AdvanceToPress(sim);
        MatchState snap = sim.Snapshot();
        int before = VitreBossRuntime.BodyLife(ref snap);
        StrikeBody(sim, ref snap, damage: 200);
        MatchState after = sim.Snapshot();
        Assert.That(VitreBossRuntime.BodyLife(ref after), Is.EqualTo(before - 200));
        Assert.That(VitreBossRuntime.IsOverpressArmed(ref after), Is.False);
    }

    [Test]
    public void Overpress_fires_at_30_percent_and_kos_center()
    {
        var sim = ResetPressworks(Seed ^ 2UL, phaseClockPermille: 100);
        AdvanceToPress(sim);
        MatchState snap = sim.Snapshot();

        // Force Life to the Overpress threshold.
        GenericPoolSlot body = MatchStateAccess.GetBossSub(ref snap, VitreBossRuntime.IndexBody);
        body.Param0 = (VitreBossRuntime.VitreMaxLife * VitreBossRuntime.OverpressLifePermille) / 1000;
        MatchStateAccess.SetBossSub(ref snap, VitreBossRuntime.IndexBody, body);
        {
            ref ActorSlot a0 = ref MatchStateAccess.Actor(ref snap, 0);
            a0.PosX = Fix.Zero;
            a0.PosZ = Fix.Zero;
            a0.PosY = Fix.Zero;
            a0.InvulnFrames = 0;
            ref ActorSlot a1 = ref MatchStateAccess.Actor(ref snap, 1);
            a1.PosX = Fix.FromInt(40);
            a1.PosZ = Fix.FromInt(40);
        }

        sim.Restore(snap);
        // Start shrink on next tick.
        sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        MatchState shrinking = sim.Snapshot();
        Assert.That(VitreBossRuntime.TryGetShrinkRadius(ref shrinking, out _), Is.True);

        for (int f = 0; f < VitreBossRuntime.OverpressShrinkFrames; f++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        MatchState after = sim.Snapshot();
        Assert.That(MatchStateAccess.Actor(ref after, 0).IsEliminated, Is.True);
        Assert.That(MatchStateAccess.Actor(ref after, 1).IsEliminated, Is.False);
        Assert.That(VitreBossRuntime.TryGetShrinkRadius(ref after, out _), Is.False);
    }

    [Test]
    public void Overpress_cooldown_blocks_second_arm()
    {
        var sim = ResetPressworks(Seed ^ 3UL, phaseClockPermille: 100);
        AdvanceToPress(sim);
        MatchState snap = sim.Snapshot();

        GenericPoolSlot body = MatchStateAccess.GetBossSub(ref snap, VitreBossRuntime.IndexBody);
        body.Param0 = 1;
        MatchStateAccess.SetBossSub(ref snap, VitreBossRuntime.IndexBody, body);
        {
            ref ActorSlot a0 = ref MatchStateAccess.Actor(ref snap, 0);
            a0.PosX = Fix.FromInt(40);
            a0.PosZ = Fix.FromInt(40);
            ref ActorSlot a1 = ref MatchStateAccess.Actor(ref snap, 1);
            a1.PosX = Fix.FromInt(41);
            a1.PosZ = Fix.FromInt(41);
        }

        sim.Restore(snap);
        sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        for (int f = 0; f < VitreBossRuntime.OverpressShrinkFrames; f++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        MatchState cooled = sim.Snapshot();
        GenericPoolSlot over = MatchStateAccess.GetBossSub(ref cooled, VitreBossRuntime.IndexOverpress);
        Assert.That((over.Flags & VitreBossRuntime.FlagCooldown) != 0, Is.True);
        Assert.That(over.Param0, Is.GreaterThan(0));
        Assert.That(VitreBossRuntime.TryGetShrinkRadius(ref cooled, out _), Is.False);

        // Still ≤30% Life but cooldown active — must not re-enter shrink this frame.
        sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        MatchState still = sim.Snapshot();
        Assert.That(VitreBossRuntime.TryGetShrinkRadius(ref still, out _), Is.False);
    }

    private static Simulation ResetPressworks(ulong seed, int phaseClockPermille)
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var config = MatchConfig.CreateDefault(seed, actorCount: 2);
        unsafe
        {
            config.StageId = SampleContentFactory.PressworksStageId;
            config.PhaseClockPermille = phaseClockPermille;
            config.FighterIds[0] = SampleContentFactory.RookFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);
        return sim;
    }

    private static void AdvanceToPress(Simulation sim)
    {
        for (int f = 0; f < 540 + 12; f++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }
    }

    private static void StrikeBody(Simulation sim, ref MatchState snap, int damage)
    {
        GenericPoolSlot body = MatchStateAccess.GetBossSub(ref snap, VitreBossRuntime.IndexBody);
        {
            ref ActorSlot a0 = ref MatchStateAccess.Actor(ref snap, 0);
            a0.PosX = body.PosX;
            a0.PosZ = body.PosZ;
            a0.PosY = Fix.Zero;
            a0.Action = (byte)ActorAction.Strong;
            a0.ActionPhase = (byte)ActionPhase.Active;
            a0.ActionTimer = 20;
            a0.HitApplied = 0;
            a0.MoveDamage = damage;

            ref ActorSlot a1 = ref MatchStateAccess.Actor(ref snap, 1);
            a1.PosX = Fix.FromInt(40);
            a1.PosZ = Fix.FromInt(40);
        }

        sim.Restore(snap);
        sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        snap = sim.Snapshot();
    }
}
