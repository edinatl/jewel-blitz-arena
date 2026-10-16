using JewelBlitzArena.Data;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class ProjectileRuntimeTests
{
    private const ulong Seed = 0x4355545F4752564CUL; // CUT_GRVL

    [Test]
    public void Idle_harvester_has_no_projectiles_or_cutters_controller()
    {
        var sim = ResetHarvester(Seed);
        MatchState snap = sim.Snapshot();
        Assert.That(ProjectileRuntime.CountActive(ref snap), Is.EqualTo(0));
        Assert.That(MatchStateAccess.GetBossSub(ref snap, BossPartRuntime.IndexCutters).TypeId,
            Is.EqualTo(0));
    }

    [Test]
    public void Broken_leg_arms_cutters_and_spawns_gravel()
    {
        var sim = ResetHarvester(Seed ^ 1UL);
        MatchState snap = sim.Snapshot();
        BreakLeg(sim, ref snap, 0);
        snap = sim.Snapshot();

        // Force cadence fire next frame.
        GenericPoolSlot cut = MatchStateAccess.GetBossSub(ref snap, BossPartRuntime.IndexCutters);
        Assert.That(cut.TypeId, Is.EqualTo(BossPartRuntime.KindCutters));
        cut.Param0 = 1;
        MatchStateAccess.SetBossSub(ref snap, BossPartRuntime.IndexCutters, cut);
        sim.Restore(snap);
        sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);

        MatchState after = sim.Snapshot();
        Assert.That(ProjectileRuntime.CountActive(ref after), Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void Gravel_damages_actor_on_overlap()
    {
        var sim = ResetHarvester(Seed ^ 2UL);
        MatchState snap = sim.Snapshot();
        int lifeBefore = MatchStateAccess.Actor(ref snap, 0).Life;

        // Park actor at origin and spawn a bolt on top of them.
        {
            ref ActorSlot a0 = ref MatchStateAccess.Actor(ref snap, 0);
            a0.PosX = Fix.Zero;
            a0.PosZ = Fix.Zero;
            a0.PosY = Fix.Zero;
            a0.InvulnFrames = 0;
            a0.DodgeArmed = 0;
        }

        Assert.That(ProjectileRuntime.TrySpawnGravel(ref snap), Is.True);
        // Zero velocity so integrate stays on the actor this frame.
        ProjectileSlot p = MatchStateAccess.GetProjectile(ref snap, 0);
        p.VelX = Fix.Zero;
        p.VelZ = Fix.Zero;
        MatchStateAccess.SetProjectile(ref snap, 0, p);
        sim.Restore(snap);

        var events = default(SimContactBuffer);
        MatchState live = sim.Snapshot();
        ProjectileRuntime.Tick(ref live, ref events);

        Assert.That(MatchStateAccess.Actor(ref live, 0).Life,
            Is.EqualTo(lifeBefore - ProjectileRuntime.GravelDamage));
        Assert.That(ProjectileRuntime.CountActive(ref live), Is.EqualTo(0));
    }

    [Test]
    public void Cutters_prefer_mounted_target()
    {
        var sim = ResetHarvester(Seed ^ 3UL);
        MatchState snap = sim.Snapshot();
        {
            ref ActorSlot a0 = ref MatchStateAccess.Actor(ref snap, 0);
            a0.PosX = Fix.FromInt(20);
            a0.PosZ = Fix.Zero;
            a0.IsMounted = false;

            ref ActorSlot a1 = ref MatchStateAccess.Actor(ref snap, 1);
            a1.PosX = Fix.FromInt(2);
            a1.PosZ = Fix.Zero;
            a1.IsMounted = true;
        }

        int target = ProjectileRuntime.FindCuttersTarget(ref snap, Fix.Zero, Fix.Zero);
        Assert.That(target, Is.EqualTo(1), "mounted fighter wins over nearer unmounted");
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

    private static void BreakLeg(Simulation sim, ref MatchState snap, int partIndex)
    {
        int remaining = MatchStateAccess.GetBossSub(ref snap, partIndex).Param0;
        while (remaining > 0)
        {
            GenericPoolSlot part = MatchStateAccess.GetBossSub(ref snap, partIndex);
            {
                ref ActorSlot a0 = ref MatchStateAccess.Actor(ref snap, 0);
                a0.PosX = part.PosX;
                a0.PosZ = part.PosZ;
                a0.PosY = Fix.Zero;
                a0.IsMounted = false;
                a0.Action = (byte)ActorAction.Strong;
                a0.ActionPhase = (byte)ActionPhase.Active;
                a0.ActionTimer = 20;
                a0.HitApplied = 0;
                a0.MoveDamage = remaining > 90 ? 90 : remaining;

                ref ActorSlot a1 = ref MatchStateAccess.Actor(ref snap, 1);
                a1.PosX = Fix.FromInt(40);
                a1.PosZ = Fix.FromInt(40);
            }

            sim.Restore(snap);
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
            snap = sim.Snapshot();
            part = MatchStateAccess.GetBossSub(ref snap, partIndex);
            if ((part.Flags & BossPartRuntime.FlagBroken) != 0)
            {
                return;
            }

            remaining = part.Param0;
        }
    }
}
