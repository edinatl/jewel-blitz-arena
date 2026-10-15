using JewelBlitzArena.Tools.Replay;
using JewelBlitzArena.Tools.Viewer;

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: JewelBlitzArena.ReplayViewer <file.jbr.json> [--every N]");
    return 2;
}

string path = args[0];
int every = 30;
for (int i = 1; i < args.Length - 1; i++)
{
    if (args[i] == "--every" && int.TryParse(args[i + 1], out int n))
    {
        every = n;
    }
}

ReplayRecording recording = ReplayIo.Load(path);
Console.WriteLine($"Replay '{recording.Name}' seed=0x{recording.Seed:x} actors={recording.ActorCount} frames={recording.Frames.Length}");
Console.WriteLine($"Expected terminal hash: {recording.ExpectedHashHex}");
Console.WriteLine();

foreach (var dump in ReplayViewerStub.Scrub(recording, every))
{
    Console.WriteLine($"frame={dump.Frame} hash={ReplayRecording.FormatHashHex(dump.Hash)}");
    foreach (string line in dump.ActorLines)
    {
        Console.WriteLine($"  {line}");
    }
}

ulong actual = JewelBlitzArena.Tools.Corpus.CorpusRunner.Replay(recording);
ulong expected = ReplayRecording.ParseHashHex(recording.ExpectedHashHex);
Console.WriteLine();
Console.WriteLine(actual == expected
    ? $"OK terminal hash matches ({ReplayRecording.FormatHashHex(actual)})"
    : $"MISMATCH expected={ReplayRecording.FormatHashHex(expected)} actual={ReplayRecording.FormatHashHex(actual)}");
return actual == expected ? 0 : 1;
