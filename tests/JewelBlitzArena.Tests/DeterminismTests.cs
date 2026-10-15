using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class DeterminismTests
{
    private const ulong Seed = 0x4A42415F53305F31UL; // "JBA_S0_1"

    [Test]
    public void Same_input_log_produces_identical_hash_across_two_sims()
    {
        InputFrame[] log = BuildSampleLog(frames: 180);

        ulong hashA = RunToHash(Seed, actorCount: 2, log);
        ulong hashB = RunToHash(Seed, actorCount: 2, log);

        Assert.That(hashA, Is.EqualTo(hashB));
        Assert.That(hashA, Is.Not.EqualTo(0UL));
    }

    [Test]
    public void Snapshot_restore_preserves_hash()
    {
        var config = MatchConfig.CreateDefault(Seed, actorCount: 2);
        var sim = new Simulation();
        sim.Reset(config);

        for (int f = 0; f < 30; f++)
        {
            sim.Step(
                InputFrame.FromStick(direction: f % 32, magnitude: 2),
                InputFrame.FromStick(direction: (f * 3) % 32, magnitude: 1),
                InputFrame.Neutral,
                InputFrame.Neutral);
        }

        MatchState snap = sim.Snapshot();
        ulong before = sim.Hash();

        // Mutate, then restore.
        sim.Step(InputFrame.FromStick(0, 3), InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        Assert.That(sim.Hash(), Is.Not.EqualTo(before));

        sim.Restore(snap);
        Assert.That(sim.Hash(), Is.EqualTo(before));
        Assert.That(sim.Frame, Is.EqualTo(30));
    }

    [Test]
    public void Different_inputs_change_hash()
    {
        InputFrame[] logA = BuildSampleLog(frames: 60);
        InputFrame[] logB = (InputFrame[])logA.Clone();
        logB[20] = InputFrame.FromStick(direction: 8, magnitude: 3);

        ulong hashA = RunToHash(Seed, 2, logA);
        ulong hashB = RunToHash(Seed, 2, logB);

        Assert.That(hashA, Is.Not.EqualTo(hashB));
    }

    [Test]
    public void Different_seeds_change_hash()
    {
        InputFrame[] log = BuildSampleLog(frames: 60);
        ulong hashA = RunToHash(Seed, 2, log);
        ulong hashB = RunToHash(Seed ^ 1UL, 2, log);
        Assert.That(hashA, Is.Not.EqualTo(hashB));
    }

    [Test]
    public void Replay_from_config_plus_log_is_hash_stable()
    {
        InputFrame[] log = BuildSampleLog(frames: 240);
        ulong expected = RunToHash(Seed, 4, log);

        for (int i = 0; i < 5; i++)
        {
            Assert.That(RunToHash(Seed, 4, log), Is.EqualTo(expected));
        }
    }

    private static ulong RunToHash(ulong seed, int actorCount, InputFrame[] perFrameP0)
    {
        // perFrameP0 holds player-0 inputs; other players get derived/neutral inputs.
        var config = MatchConfig.CreateDefault(seed, actorCount);
        var sim = new Simulation();
        sim.Reset(config);

        for (int f = 0; f < perFrameP0.Length; f++)
        {
            InputFrame p0 = perFrameP0[f];
            InputFrame p1 = actorCount > 1
                ? InputFrame.FromStick((p0.Direction + 16) & 31, p0.Magnitude)
                : InputFrame.Neutral;
            InputFrame p2 = actorCount > 2
                ? InputFrame.FromStick((p0.Direction + 8) & 31, magnitude: 1)
                : InputFrame.Neutral;
            InputFrame p3 = actorCount > 3
                ? InputFrame.FromStick((p0.Direction + 24) & 31, magnitude: 2)
                : InputFrame.Neutral;
            sim.Step(p0, p1, p2, p3);
        }

        return sim.Hash();
    }

    private static InputFrame[] BuildSampleLog(int frames)
    {
        var log = new InputFrame[frames];
        for (int f = 0; f < frames; f++)
        {
            int mag = 1 + (f % 3);
            log[f] = InputFrame.FromStick(direction: (f * 5) % 32, magnitude: mag);
            if ((f % 17) == 0)
            {
                log[f].Jump = true;
            }

            if ((f % 23) == 0)
            {
                log[f].Attack = true;
            }
        }

        return log;
    }
}
