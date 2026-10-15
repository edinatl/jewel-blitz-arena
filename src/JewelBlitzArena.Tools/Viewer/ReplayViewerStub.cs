using JewelBlitzArena.Sim;
using JewelBlitzArena.Tools.Replay;

namespace JewelBlitzArena.Tools.Viewer;

/// <summary>
/// Headless replay scrubber stub (M1 will add hitboxes / desync compare UI).
/// Prints selected frames' hash and actor positions.
/// </summary>
public static class ReplayViewerStub
{
    public sealed class FrameDump
    {
        public int Frame { get; init; }
        public ulong Hash { get; init; }
        public required string[] ActorLines { get; init; }
    }

    public static IReadOnlyList<FrameDump> Scrub(ReplayRecording recording, int everyNFrames = 30)
    {
        if (everyNFrames < 1)
        {
            everyNFrames = 1;
        }

        MatchConfig config = recording.ToMatchConfig();
        var sim = new Simulation();
        sim.Reset(config);

        var dumps = new List<FrameDump> { Dump(sim) };

        for (int f = 0; f < recording.Frames.Length; f++)
        {
            ReplayRecording.FrameInputs frame = recording.Frames[f];
            sim.Step(
                new InputFrame { Bits = frame.P0 },
                new InputFrame { Bits = frame.P1 },
                new InputFrame { Bits = frame.P2 },
                new InputFrame { Bits = frame.P3 });

            if (((f + 1) % everyNFrames) == 0 || f + 1 == recording.Frames.Length)
            {
                dumps.Add(Dump(sim));
            }
        }

        return dumps;
    }

    private static FrameDump Dump(Simulation sim)
    {
        var lines = new List<string>();
        MatchState state = sim.Snapshot();
        for (int i = 0; i < state.ActorCount; i++)
        {
            ActorSlot actor = i switch
            {
                0 => state.Actor0,
                1 => state.Actor1,
                2 => state.Actor2,
                _ => state.Actor3,
            };
            lines.Add(
                $"actor{i} life={actor.Life} x={actor.PosX.Raw} z={actor.PosZ.Raw} alive={actor.IsAlive}");
        }

        return new FrameDump
        {
            Frame = sim.Frame,
            Hash = sim.Hash(),
            ActorLines = lines.ToArray(),
        };
    }
}
