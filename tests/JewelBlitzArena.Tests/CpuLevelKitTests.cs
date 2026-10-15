using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class CpuLevelKitTests
{
    [Test]
    public void Cpu_level_clamps_and_scales_think_hold_and_magnitude()
    {
        Assert.That(CpuBrainRuntime.ClampLevel(0), Is.EqualTo(1));
        Assert.That(CpuBrainRuntime.ClampLevel(99), Is.EqualTo(8));
        Assert.That(CpuBrainRuntime.ThinkHoldFrames(1), Is.GreaterThan(CpuBrainRuntime.ThinkHoldFrames(8)));
        Assert.That(CpuBrainRuntime.StickMagnitudeForLevel(1), Is.EqualTo(1));
        Assert.That(CpuBrainRuntime.StickMagnitudeForLevel(4), Is.EqualTo(2));
        Assert.That(CpuBrainRuntime.StickMagnitudeForLevel(8), Is.EqualTo(3));
    }

    [Test]
    public void MatchConfig_cpu_level_feeds_activate()
    {
        MatchConfig config = MatchConfig.CreateDefault(0x4C564C3154UL, actorCount: 2);
        config.SetCpuLevel(1, 7);
        var sim = new Simulation();
        sim.Reset(config);
        sim.SetCpuControlled(1, true);

        Assert.That(sim.IsCpuControlled(1), Is.True);
        MatchState snap = sim.Snapshot();
        Assert.That(CpuBrainRuntime.GetLevel(ref snap, 1), Is.EqualTo(7));
        Assert.That(sim.GetCpuLevel(1), Is.EqualTo(7));
    }

    [Test]
    public void Higher_cpu_level_moves_fuller_than_low_level_same_seed()
    {
        MatchConfig lowCfg = MatchConfig.CreateDefault(0x4C564C324FUL, actorCount: 2);
        lowCfg.SetCpuLevel(1, 1);
        MatchConfig highCfg = MatchConfig.CreateDefault(0x4C564C324FUL, actorCount: 2);
        highCfg.SetCpuLevel(1, 8);

        var low = new Simulation();
        var high = new Simulation();
        low.Reset(lowCfg);
        high.Reset(highCfg);
        low.SetCpuControlled(1, true);
        high.SetCpuControlled(1, true);

        // Park P0; CPU on P1 chases toward origin/P0.
        InputFrame idle = InputFrame.Neutral;
        for (int i = 0; i < 60; i++)
        {
            low.Step(idle, idle, idle, idle);
            high.Step(idle, idle, idle, idle);
        }

        MatchState lowSnap = low.Snapshot();
        MatchState highSnap = high.Snapshot();
        long lowTravel = System.Math.Abs(lowSnap.Actor1.PosX.Raw - Fix.FromInt(1).Raw)
            + System.Math.Abs(lowSnap.Actor1.PosZ.Raw);
        long highTravel = System.Math.Abs(highSnap.Actor1.PosX.Raw - Fix.FromInt(1).Raw)
            + System.Math.Abs(highSnap.Actor1.PosZ.Raw);
        Assert.That(highTravel, Is.GreaterThan(lowTravel));
    }

    [Test]
    public void Cpu_level_brain_is_hash_stable()
    {
        MatchConfig config = MatchConfig.CreateDefault(0x4C564C3348UL, actorCount: 2);
        config.SetCpuLevel(1, 5);
        var a = new Simulation();
        var b = new Simulation();
        a.Reset(config);
        b.Reset(config);
        a.SetCpuControlled(1, true);
        b.SetCpuControlled(1, true);

        InputFrame p0 = InputFrame.FromStick(0, 2);
        for (int i = 0; i < 90; i++)
        {
            a.Step(p0, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
            b.Step(p0, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        Assert.That(a.Hash(), Is.EqualTo(b.Hash()));
    }
}
