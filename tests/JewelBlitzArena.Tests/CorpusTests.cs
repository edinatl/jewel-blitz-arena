using JewelBlitzArena.Tools.Corpus;
using JewelBlitzArena.Tools.Replay;

namespace JewelBlitzArena.Tests;

public sealed class CorpusTests
{
    private static string CorpusRoot
    {
        get
        {
            string? dir = TestContext.CurrentContext.TestDirectory;
            while (!string.IsNullOrEmpty(dir))
            {
                if (File.Exists(Path.Combine(dir, "JewelBlitzArena.sln")))
                {
                    return Path.Combine(dir, "corpus");
                }

                dir = Directory.GetParent(dir)?.FullName;
            }

            throw new DirectoryNotFoundException("Could not locate repo root (JewelBlitzArena.sln) from test directory.");
        }
    }

    [Test]
    public void Corpus_directory_contains_seed_fixtures()
    {
        Assert.That(Directory.Exists(CorpusRoot), Is.True, $"Missing corpus at {CorpusRoot}");
        var files = CorpusRunner.Discover(CorpusRoot);
        Assert.That(files, Has.Count.GreaterThanOrEqualTo(4));
    }

    [Test]
    public void Every_corpus_entry_replays_to_its_expected_hash()
    {
        var results = CorpusRunner.RunAll(CorpusRoot);
        Assert.That(results, Is.Not.Empty);
        foreach (var r in results)
        {
            Assert.That(r.Error, Is.Null, $"{r.Name}: {r.Error}");
            Assert.That(
                r.Passed,
                Is.True,
                $"{r.Name}: expected {ReplayRecording.FormatHashHex(r.ExpectedHash)} " +
                $"actual {ReplayRecording.FormatHashHex(r.ActualHash)}");
        }
    }

    [Test]
    public void Tampered_expected_hash_is_detected()
    {
        string path = CorpusRunner.Discover(CorpusRoot).First();
        ReplayRecording recording = ReplayIo.Load(path);
        ulong real = CorpusRunner.Replay(recording);
        recording.ExpectedHashHex = ReplayRecording.FormatHashHex(real ^ 1UL);

        string temp = Path.Combine(Path.GetTempPath(), "jba-tamper-" + Guid.NewGuid().ToString("n") + CorpusRunner.FileSuffix);
        try
        {
            ReplayIo.Save(temp, recording);
            CorpusRunResult result = CorpusRunner.RunFile(temp);
            Assert.That(result.Passed, Is.False);
            Assert.That(result.ActualHash, Is.EqualTo(real));
        }
        finally
        {
            if (File.Exists(temp))
            {
                File.Delete(temp);
            }
        }
    }
}
