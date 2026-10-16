using JewelBlitzArena.Data;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class BossPartRuntimeTests
{
    private const ulong Seed = 0x4852565F414E4154UL; // HRV_ANAT

    [Test]
    public void Harvester_arms_six_boss_parts()
    {
        var sim = ResetHarvester(Seed);
        MatchState snap = sim.Snapshot();
        Assert.That(BossPartRuntime.CountArmed(ref snap), Is.EqualTo(6));
        Assert.That(BossPartRuntime.IsHeadExposed(ref snap), Is.False);
        Assert.That(MatchStateAccess.GetBossSub(ref snap, BossPartRuntime.IndexBuck).TypeId,
            Is.EqualTo(0), "Buck controller arms lazily on first mount");
        Assert.That(MatchStateAccess.GetBossSub(ref snap, BossPartRuntime.IndexCutters).TypeId,
            Is.EqualTo(0), "Cutters controller arms lazily on first broken leg");
    }

    [Test]
    public void Head_not_hittable_until_two_legs_broken()
    {
        var sim = ResetHarvester(Seed ^ 1UL);
        MatchState snap = sim.Snapshot();

        // Break two legs by zeroing Life through forced Active Strong on each.
        BreakPartAt(sim, ref snap, partIndex: 0);
        snap = sim.Snapshot();
        Assert.That(BossPartRuntime.CountBrokenLegs(ref snap), Is.EqualTo(1));
        Assert.That(BossPartRuntime.IsHeadExposed(ref snap), Is.False);

        BreakPartAt(sim, ref snap, partIndex: 1);
        snap = sim.Snapshot();
        Assert.That(BossPartRuntime.CountBrokenLegs(ref snap), Is.EqualTo(2));
        Assert.That(BossPartRuntime.IsHeadExposed(ref snap), Is.True);
    }

    [Test]
    public void Broken_leg_repairs_after_30s()
    {
        var sim = ResetHarvester(Seed ^ 2UL);
        MatchState snap = sim.Snapshot();
        BreakPartAt(sim, ref snap, partIndex: 0);
        snap = sim.Snapshot();

        GenericPoolSlot leg = MatchStateAccess.GetBossSub(ref snap, 0);
        Assert.That((leg.Flags & BossPartRuntime.FlagRepairing) != 0, Is.True);
        // Break frame already ticked repair once.
        Assert.That(leg.Param0, Is.LessThanOrEqualTo(BossPartRuntime.LegRepairFrames));
        Assert.That(leg.Param0, Is.GreaterThan(BossPartRuntime.LegRepairFrames - 10));

        // Fast-forward repair (Param0 countdown) without needing full 1800 Steps of input.
        leg.Param0 = 2;
        MatchStateAccess.SetBossSub(ref snap, 0, leg);
        sim.Restore(snap);
        sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);

        MatchState after = sim.Snapshot();
        GenericPoolSlot repaired = MatchStateAccess.GetBossSub(ref after, 0);
        Assert.That((repaired.Flags & BossPartRuntime.FlagActive) != 0, Is.True);
        Assert.That((repaired.Flags & BossPartRuntime.FlagBroken) == 0, Is.True);
        Assert.That(repaired.Param0, Is.EqualTo(BossPartRuntime.LegMaxLife));
    }

    [Test]
    public void Tail_not_hittable_from_ground_without_mount()
    {
        var sim = ResetHarvester(Seed ^ 3UL);
        MatchState snap = sim.Snapshot();
        Assert.That(BossPartRuntime.IsHeadExposed(ref snap), Is.False);

        int before = MatchStateAccess.GetBossSub(ref snap, BossPartRuntime.IndexTail).Param0;
        StrikePart(sim, ref snap, partIndex: BossPartRuntime.IndexTail, damage: 90);
        MatchState after = sim.Snapshot();
        int life = MatchStateAccess.GetBossSub(ref after, BossPartRuntime.IndexTail).Param0;
        Assert.That(life, Is.EqualTo(before), "unmounted ground strikes must not damage the tail");
    }

    [Test]
    public void Action_near_broken_leg_mounts_and_can_damage_tail()
    {
        var sim = ResetHarvester(Seed ^ 4UL);
        MatchState snap = sim.Snapshot();
        BreakPartAt(sim, ref snap, partIndex: 0);
        snap = sim.Snapshot();

        MountAtBrokenLeg(sim, ref snap, legIndex: 0);
        snap = sim.Snapshot();
        Assert.That(MatchStateAccess.Actor(ref snap, 0).IsMounted, Is.True);

        int before = MatchStateAccess.GetBossSub(ref snap, BossPartRuntime.IndexTail).Param0;
        StrikePart(sim, ref snap, partIndex: BossPartRuntime.IndexTail, damage: 90, keepMounted: true);
        MatchState after = sim.Snapshot();
        Assert.That(MatchStateAccess.GetBossSub(ref after, BossPartRuntime.IndexTail).Param0,
            Is.LessThan(before));
        Assert.That(MatchStateAccess.Actor(ref after, 0).IsMounted, Is.True);
    }

    [Test]
    public void Buck_throws_non_hanging_mounted_fighter()
    {
        var sim = ResetHarvester(Seed ^ 5UL);
        MatchState snap = sim.Snapshot();
        BreakPartAt(sim, ref snap, partIndex: 0);
        snap = sim.Snapshot();
        MountAtBrokenLeg(sim, ref snap, legIndex: 0);
        snap = sim.Snapshot();

        int lifeBefore = MatchStateAccess.Actor(ref snap, 0).Life;
        // Force Buck fire on the next tick.
        GenericPoolSlot buck = MatchStateAccess.GetBossSub(ref snap, BossPartRuntime.IndexBuck);
        buck.Param0 = 1;
        MatchStateAccess.SetBossSub(ref snap, BossPartRuntime.IndexBuck, buck);
        sim.Restore(snap);
        sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);

        MatchState after = sim.Snapshot();
        ref ActorSlot a0 = ref MatchStateAccess.Actor(ref after, 0);
        Assert.That(a0.IsMounted, Is.False);
        Assert.That(a0.Life, Is.EqualTo(lifeBefore - BossPartRuntime.BuckDamage));
        Assert.That(a0.KnockdownFrames, Is.GreaterThan(0));
    }

    [Test]
    public void Buck_hanging_on_Action_survives()
    {
        var sim = ResetHarvester(Seed ^ 6UL);
        MatchState snap = sim.Snapshot();
        BreakPartAt(sim, ref snap, partIndex: 0);
        snap = sim.Snapshot();
        MountAtBrokenLeg(sim, ref snap, legIndex: 0);
        snap = sim.Snapshot();

        int lifeBefore = MatchStateAccess.Actor(ref snap, 0).Life;
        GenericPoolSlot buck = MatchStateAccess.GetBossSub(ref snap, BossPartRuntime.IndexBuck);
        buck.Param0 = 1;
        MatchStateAccess.SetBossSub(ref snap, BossPartRuntime.IndexBuck, buck);
        sim.Restore(snap);

        var hang = InputFrame.Neutral;
        hang.Action = true;
        sim.Step(hang, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);

        MatchState after = sim.Snapshot();
        ref ActorSlot a0 = ref MatchStateAccess.Actor(ref after, 0);
        Assert.That(a0.IsMounted, Is.True);
        Assert.That(a0.Life, Is.EqualTo(lifeBefore));
    }

    [Test]
    public void Leg_repair_dismounts()
    {
        var sim = ResetHarvester(Seed ^ 7UL);
        MatchState snap = sim.Snapshot();
        BreakPartAt(sim, ref snap, partIndex: 0);
        snap = sim.Snapshot();
        MountAtBrokenLeg(sim, ref snap, legIndex: 0);
        snap = sim.Snapshot();
        Assert.That(MatchStateAccess.Actor(ref snap, 0).IsMounted, Is.True);

        GenericPoolSlot leg = MatchStateAccess.GetBossSub(ref snap, 0);
        leg.Param0 = 1;
        MatchStateAccess.SetBossSub(ref snap, 0, leg);
        sim.Restore(snap);
        sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);

        MatchState after = sim.Snapshot();
        Assert.That(BossPartRuntime.CountBrokenLegs(ref after), Is.EqualTo(0));
        Assert.That(MatchStateAccess.Actor(ref after, 0).IsMounted, Is.False);
    }

    private static Simulation ResetHarvester(ulong seed)
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var config = MatchConfig.CreateDefault(seed, actorCount: 2);
        unsafe
        {
            config.StageId = SampleContentFactory.HarvesterTerracesStageId;
            config.FighterIds[0] = SampleContentFactory.RookFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);
        return sim;
    }

    private static void MountAtBrokenLeg(Simulation sim, ref MatchState snap, int legIndex)
    {
        GenericPoolSlot leg = MatchStateAccess.GetBossSub(ref snap, legIndex);
        {
            ref ActorSlot a0 = ref MatchStateAccess.Actor(ref snap, 0);
            a0.PosX = leg.PosX;
            a0.PosZ = leg.PosZ;
            a0.PosY = Fix.Zero;
            a0.IsMounted = false;
            a0.Action = (byte)ActorAction.Neutral;
            a0.ActionPhase = (byte)ActionPhase.None;
            a0.ActionTimer = 0;
            a0.HitApplied = 0;

            ref ActorSlot a1 = ref MatchStateAccess.Actor(ref snap, 1);
            a1.PosX = Fix.FromInt(40);
            a1.PosZ = Fix.FromInt(40);
        }

        sim.Restore(snap);
        var mount = InputFrame.Neutral;
        mount.Action = true;
        sim.Step(mount, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        snap = sim.Snapshot();
    }

    private static void BreakPartAt(Simulation sim, ref MatchState snap, int partIndex)
    {
        // Deal enough Strong damage over several frames to clear LegMaxLife (500).
        int remaining = MatchStateAccess.GetBossSub(ref snap, partIndex).Param0;
        while (remaining > 0)
        {
            int chunk = remaining > 90 ? 90 : remaining;
            StrikePart(sim, ref snap, partIndex, chunk);
            snap = sim.Snapshot();
            GenericPoolSlot part = MatchStateAccess.GetBossSub(ref snap, partIndex);
            if ((part.Flags & BossPartRuntime.FlagBroken) != 0)
            {
                return;
            }

            remaining = part.Param0;
        }
    }

    private static void StrikePart(
        Simulation sim,
        ref MatchState snap,
        int partIndex,
        int damage,
        bool keepMounted = false)
    {
        GenericPoolSlot part = MatchStateAccess.GetBossSub(ref snap, partIndex);
        {
            ref ActorSlot a0 = ref MatchStateAccess.Actor(ref snap, 0);
            a0.PosX = part.PosX;
            a0.PosZ = part.PosZ;
            if (!keepMounted)
            {
                a0.PosY = Fix.Zero;
                a0.IsMounted = false;
            }

            a0.Action = (byte)ActorAction.Strong;
            a0.ActionPhase = (byte)ActionPhase.Active;
            // Strong stub total 31; Active when elapsed in [10, 13) → ActionTimer 20 → elapsed 12.
            a0.ActionTimer = 20;
            a0.HitApplied = 0;
            a0.MoveDamage = damage;

            // Keep the other fighter far so ResolveHits does not consume HitApplied.
            ref ActorSlot a1 = ref MatchStateAccess.Actor(ref snap, 1);
            a1.PosX = Fix.FromInt(40);
            a1.PosZ = Fix.FromInt(40);
        }

        sim.Restore(snap);
        sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        snap = sim.Snapshot();
    }
}
