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
    public void Tail_is_hittable_without_leg_breaks()
    {
        var sim = ResetHarvester(Seed ^ 3UL);
        MatchState snap = sim.Snapshot();
        Assert.That(BossPartRuntime.IsHeadExposed(ref snap), Is.False);

        // Damage tail once; should accept hit (Param0 drops).
        int before = MatchStateAccess.GetBossSub(ref snap, 5).Param0;
        StrikePart(sim, ref snap, partIndex: 5, damage: 90);
        MatchState after = sim.Snapshot();
        int life = MatchStateAccess.GetBossSub(ref after, 5).Param0;
        Assert.That(life, Is.LessThan(before));
        Assert.That((MatchStateAccess.GetBossSub(ref after, 5).Flags & BossPartRuntime.FlagActive) != 0, Is.True);
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

    private static void StrikePart(Simulation sim, ref MatchState snap, int partIndex, int damage)
    {
        GenericPoolSlot part = MatchStateAccess.GetBossSub(ref snap, partIndex);
        {
            ref ActorSlot a0 = ref MatchStateAccess.Actor(ref snap, 0);
            a0.PosX = part.PosX;
            a0.PosZ = part.PosZ;
            a0.PosY = Fix.Zero;
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
