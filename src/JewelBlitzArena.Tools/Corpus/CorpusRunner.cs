using JewelBlitzArena.Sim;
using JewelBlitzArena.Tools.Replay;

namespace JewelBlitzArena.Tools.Corpus;

public sealed class CorpusRunResult
{
    public required string Path { get; init; }
    public required string Name { get; init; }
    public bool Passed { get; init; }
    public ulong ActualHash { get; init; }
    public ulong ExpectedHash { get; init; }
    public int Frames { get; init; }
    public string? Error { get; init; }
}

/// <summary>Replays every <c>*.jbr.json</c> under a corpus root and compares terminal hashes.</summary>
public static class CorpusRunner
{
    public const string FileSuffix = ".jbr.json";

    public static IReadOnlyList<string> Discover(string corpusRoot)
    {
        if (!Directory.Exists(corpusRoot))
        {
            throw new DirectoryNotFoundException($"Corpus root not found: {corpusRoot}");
        }

        return Directory
            .EnumerateFiles(corpusRoot, "*" + FileSuffix, SearchOption.AllDirectories)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();
    }

    public static CorpusRunResult RunFile(string path)
    {
        try
        {
            ReplayRecording recording = ReplayIo.Load(path);
            ulong expected = ReplayRecording.ParseHashHex(recording.ExpectedHashHex);
            ulong actual = Replay(recording);
            return new CorpusRunResult
            {
                Path = path,
                Name = recording.Name,
                Passed = actual == expected,
                ActualHash = actual,
                ExpectedHash = expected,
                Frames = recording.Frames.Length,
            };
        }
        catch (Exception ex)
        {
            return new CorpusRunResult
            {
                Path = path,
                Name = Path.GetFileName(path),
                Passed = false,
                ActualHash = 0,
                ExpectedHash = 0,
                Frames = 0,
                Error = ex.Message,
            };
        }
    }

    public static IReadOnlyList<CorpusRunResult> RunAll(string corpusRoot)
    {
        var results = new List<CorpusRunResult>();
        foreach (string path in Discover(corpusRoot))
        {
            results.Add(RunFile(path));
        }

        return results;
    }

    public static ulong Replay(ReplayRecording recording)
    {
        MatchConfig config = recording.ToMatchConfig();
        var sim = new Simulation();
        sim.Reset(config);

        ReplayRecording.CpuHandoff[] handoffs = recording.CpuHandoffs ?? Array.Empty<ReplayRecording.CpuHandoff>();

        for (int f = 0; f < recording.Frames.Length; f++)
        {
            ApplyHandoffsAtFrame(sim, handoffs, f);
            ReplayRecording.FrameInputs frame = recording.Frames[f];
            sim.Step(
                new InputFrame { Bits = frame.P0 },
                new InputFrame { Bits = frame.P1 },
                new InputFrame { Bits = frame.P2 },
                new InputFrame { Bits = frame.P3 });
        }

        return sim.Hash();
    }

    private static void ApplyHandoffsAtFrame(Simulation sim, ReplayRecording.CpuHandoff[] handoffs, int frameIndex)
    {
        for (int i = 0; i < handoffs.Length; i++)
        {
            ReplayRecording.CpuHandoff h = handoffs[i];
            if (h.Frame == frameIndex)
            {
                sim.SetCpuControlled(h.PlayerIndex, controlled: true);
            }
        }
    }

    /// <summary>Build a recording from a procedural log and stamp the live hash (for corpus authoring).</summary>
    public static ReplayRecording Capture(
        string name,
        ulong seed,
        int actorCount,
        int stageId,
        InputFrame[] p0Log,
        Func<int, InputFrame, InputFrame>? p1ForFrame = null,
        Func<int, InputFrame, InputFrame>? p2ForFrame = null,
        Func<int, InputFrame, InputFrame>? p3ForFrame = null,
        ReplayRecording.CpuHandoff[]? cpuHandoffs = null,
        string? notes = null)
    {
        var frames = new ReplayRecording.FrameInputs[p0Log.Length];
        for (int f = 0; f < p0Log.Length; f++)
        {
            InputFrame p0 = p0Log[f];
            InputFrame p1 = p1ForFrame?.Invoke(f, p0)
                ?? (actorCount > 1
                    ? InputFrame.FromStick((p0.Direction + 16) & 31, p0.Magnitude)
                    : InputFrame.Neutral);
            InputFrame p2 = p2ForFrame?.Invoke(f, p0)
                ?? (actorCount > 2
                    ? InputFrame.FromStick((p0.Direction + 8) & 31, 1)
                    : InputFrame.Neutral);
            InputFrame p3 = p3ForFrame?.Invoke(f, p0)
                ?? (actorCount > 3
                    ? InputFrame.FromStick((p0.Direction + 24) & 31, 2)
                    : InputFrame.Neutral);

            frames[f] = new ReplayRecording.FrameInputs
            {
                P0 = p0.Bits,
                P1 = p1.Bits,
                P2 = p2.Bits,
                P3 = p3.Bits,
            };
        }

        var recording = new ReplayRecording
        {
            FormatVersion = ReplayRecording.CurrentFormatVersion,
            Name = name,
            Notes = notes,
            Seed = seed,
            StageId = stageId,
            ActorCount = actorCount,
            FighterIds = Enumerable.Range(0, PoolCaps.Actors).ToArray(),
            Frames = frames,
            CpuHandoffs = cpuHandoffs ?? Array.Empty<ReplayRecording.CpuHandoff>(),
            ExpectedHashHex = "0",
        };

        ulong hash = Replay(recording);
        recording.ExpectedHashHex = ReplayRecording.FormatHashHex(hash);
        return recording;
    }
}
