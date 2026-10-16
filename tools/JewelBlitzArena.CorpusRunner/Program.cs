using JewelBlitzArena.Data;
using JewelBlitzArena.Sim;
using JewelBlitzArena.Tools.Corpus;
using JewelBlitzArena.Tools.Replay;

static int Usage()
{
    Console.Error.WriteLine(
        """
        JewelBlitzArena.CorpusRunner

          run --corpus <dir>              Replay corpus; exit 1 on any hash mismatch
          record --corpus <dir>           (Re)write the S8 seed fixtures with live hashes
          verify-one <file.jbr.json>      Replay a single recording
        """);
    return 2;
}

if (args.Length == 0)
{
    return Usage();
}

string command = args[0];
return command switch
{
    "run" => RunCorpus(args),
    "record" => RecordFixtures(args),
    "verify-one" => VerifyOne(args),
    _ => Usage(),
};

static int RunCorpus(string[] args)
{
    string corpus = RequireOption(args, "--corpus") ?? "corpus";
    IReadOnlyList<CorpusRunResult> results = CorpusRunner.RunAll(corpus);
    if (results.Count == 0)
    {
        Console.Error.WriteLine($"No '{CorpusRunner.FileSuffix}' files under {corpus}");
        return 1;
    }

    int failed = 0;
    foreach (CorpusRunResult r in results)
    {
        if (r.Error is not null)
        {
            Console.WriteLine($"FAIL  {r.Name}  error={r.Error}");
            failed++;
            continue;
        }

        if (r.Passed)
        {
            Console.WriteLine(
                $"PASS  {r.Name}  frames={r.Frames}  hash={ReplayRecording.FormatHashHex(r.ActualHash)}");
        }
        else
        {
            Console.WriteLine(
                $"FAIL  {r.Name}  frames={r.Frames}  " +
                $"expected={ReplayRecording.FormatHashHex(r.ExpectedHash)}  " +
                $"actual={ReplayRecording.FormatHashHex(r.ActualHash)}");
            failed++;
        }
    }

    Console.WriteLine($"Corpus: {results.Count - failed}/{results.Count} passed");
    return failed == 0 ? 0 : 1;
}

static int VerifyOne(string[] args)
{
    if (args.Length < 2)
    {
        return Usage();
    }

    CorpusRunResult r = CorpusRunner.RunFile(args[1]);
    if (r.Error is not null)
    {
        Console.Error.WriteLine(r.Error);
        return 1;
    }

    Console.WriteLine(
        $"{(r.Passed ? "PASS" : "FAIL")} {r.Name} " +
        $"expected={ReplayRecording.FormatHashHex(r.ExpectedHash)} " +
        $"actual={ReplayRecording.FormatHashHex(r.ActualHash)}");
    return r.Passed ? 0 : 1;
}

static int RecordFixtures(string[] args)
{
    string corpus = RequireOption(args, "--corpus") ?? "corpus";
    Directory.CreateDirectory(corpus);

    Write(corpus, CorpusRunner.Capture(
        name: "neutral-2p-180",
        seed: 0x4A42415F53305F31UL,
        actorCount: 2,
        stageId: 0,
        p0Log: BuildWalkLog(180),
        notes: "S0-style two-actor walk for 3 seconds (flat stageId=0; S5 kits via catalog)."));

    Write(corpus, CorpusRunner.Capture(
        name: "ffa-4p-240",
        seed: 0x4A42415F53305F32UL,
        actorCount: 4,
        stageId: 0,
        p0Log: BuildWalkLog(240),
        notes: "Four-actor free-for-all input surface, 4 seconds."));

    Write(corpus, CorpusRunner.Capture(
        name: "buttons-2p-120",
        seed: 0x4A42415F53305F33UL,
        actorCount: 2,
        stageId: SampleContentFactory.CitrineBazaarStageId,
        p0Log: BuildButtonLog(120),
        notes: "Stick + Jump/Attack on Citrine Bazaar (stage graph + hazards via S5 catalog)."));

    Write(corpus, CorpusRunner.Capture(
        name: "idle-2p-60",
        seed: 0x4A42415F53305F34UL,
        actorCount: 2,
        stageId: 0,
        p0Log: Enumerable.Repeat(InputFrame.Neutral, 60).ToArray(),
        notes: "Neutral inputs still advance RNG and frame counter."));

    // CPU replace: P1 human inputs for 30f, then Sim CPU brain owns slot 1 (hash-stable).
    Write(corpus, CorpusRunner.Capture(
        name: "cpu-replace-2p-120",
        seed: 0x4A42415F43505531UL, // JBA_CPU1
        actorCount: 2,
        stageId: 0,
        p0Log: BuildWalkLog(120),
        p1ForFrame: (f, _) => f < 30
            ? InputFrame.FromStick(16, 2)
            : InputFrame.Neutral,
        cpuHandoffs: new[]
        {
            new ReplayRecording.CpuHandoff { Frame = 30, PlayerIndex = 1 },
        },
        notes: "S8: CPU handoff at frame 30; Sim brain drives P1 thereafter (disconnect replace)."));

    // Status world patches: Ice / Smoke / Adhesive jars + walk through them.
    Write(corpus, CorpusRunner.Capture(
        name: "status-patches-2p-120",
        seed: 0x4A42415F53545031UL, // JBA_STP1
        actorCount: 2,
        stageId: 0,
        p0Log: BuildWalkLog(120),
        itemSpawns: new[]
        {
            new ReplayRecording.ItemSpawn
            {
                Frame = 0,
                ItemId = MatchRules.IceJarItemId,
                X = 1,
                Y = 0,
                Z = 0,
                SpawnWorldPatch = true,
            },
            new ReplayRecording.ItemSpawn
            {
                Frame = 0,
                ItemId = MatchRules.SmokeJarItemId,
                X = 2,
                Y = 0,
                Z = 0,
                SpawnWorldPatch = true,
            },
            new ReplayRecording.ItemSpawn
            {
                Frame = 10,
                ItemId = MatchRules.AdhesiveSprayItemId,
                X = 0,
                Y = 0,
                Z = 1,
                SpawnWorldPatch = true,
            },
        },
        notes: "S8: Ice/Smoke/Adhesive world patches via item-spawn handoffs (format v3)."));

    // Fulgur Cloister phase advance: 10% Phase Clock → Courtyard 240f + Landslide 24f → Library.
    Write(corpus, CorpusRunner.Capture(
        name: "fulgur-library-2p-300",
        seed: 0x4A42415F46554731UL, // JBA_FUG1
        actorCount: 2,
        stageId: SampleContentFactory.FulgurCloisterStageId,
        p0Log: BuildWalkLog(300),
        phaseClockPermille: 100,
        notes: "S8: Fulgur Cloister via S5 catalog; PhaseClock 10% reaches Library with crush hazard + bookcase kin."));

    // Sunken Strongroom: 10% clock → Vault floor 150f + door 150f + transitions → Hull breach by ~324f.
    Write(corpus, CorpusRunner.Capture(
        name: "sunken-breach-2p-360",
        seed: 0x4A42415F53554E31UL, // JBA_SUN1
        actorCount: 2,
        stageId: SampleContentFactory.SunkenStrongroomStageId,
        p0Log: BuildWalkLog(360),
        phaseClockPermille: 100,
        notes: "S8: Sunken Strongroom; PhaseClock 10% → Hull breach; water volumes (−20% walk) on door/breach."));

    // Rime Cutworks: 10% clock → Dry yard + Flooding → Crane yard by ~324f.
    Write(corpus, CorpusRunner.Capture(
        name: "rime-crane-2p-360",
        seed: 0x4A42415F52494D31UL, // JBA_RIM1
        actorCount: 2,
        stageId: SampleContentFactory.RimeCutworksStageId,
        p0Log: BuildWalkLog(360),
        phaseClockPermille: 100,
        notes: "S8: Rime Cutworks; PhaseClock 10% → Crane yard; flooded-floor water (−20% walk) on phase B."));

    // The Orrery: 10% clock → Gear 150f + Infestation 180f + Planet transition 12f → Planet disc by ~342f.
    Write(corpus, CorpusRunner.Capture(
        name: "orrery-disc-2p-360",
        seed: 0x4A42415F4F525231UL, // JBA_ORR1
        actorCount: 2,
        stageId: SampleContentFactory.OrreryStageId,
        p0Log: BuildWalkLog(360),
        phaseClockPermille: 100,
        notes: "S8: The Orrery via S5 catalog; PhaseClock 10% reaches Planet disc (arm gap + gust + rotating arm)."));

    // Harvester open terrace (jewel cap 6) — boss AI v1 (footfall↔patrol, intake chase).
    Write(corpus, CorpusRunner.Capture(
        name: "harvester-2p-180",
        seed: 0x4A42415F48525631UL, // JBA_HRV1
        actorCount: 2,
        stageId: SampleContentFactory.HarvesterTerracesStageId,
        p0Log: BuildWalkLog(180),
        notes: "S8: Harvester Terraces anatomy + crates (legs/head/tail; footfall/intake/stomp; 4 crates)."));

    // Pressworks: 10% clock → Sorting 540f (90 s) + transition 12f → The Press (Live Hands chase).
    Write(corpus, CorpusRunner.Capture(
        name: "pressworks-press-2p-180",
        seed: 0x4A42415F50525331UL, // JBA_PRS1
        actorCount: 2,
        stageId: SampleContentFactory.PressworksStageId,
        p0Log: BuildWalkLog(560),
        phaseClockPermille: 100,
        notes: "S8: Pressworks Vein Guards + 90 s Sorting gate; PhaseClock 10% clears Sorting; Live Hands on The Press."));

    Console.WriteLine($"Recorded fixtures into {Path.GetFullPath(corpus)}");
    return 0;
}

static void Write(string corpus, ReplayRecording recording)
{
    string path = Path.Combine(corpus, recording.Name + CorpusRunner.FileSuffix);
    ReplayIo.Save(path, recording);
    Console.WriteLine($"wrote {path} hash={recording.ExpectedHashHex}");
}

static InputFrame[] BuildWalkLog(int frames)
{
    var log = new InputFrame[frames];
    for (int f = 0; f < frames; f++)
    {
        int mag = 1 + (f % 3);
        log[f] = InputFrame.FromStick((f * 5) % 32, mag);
    }

    return log;
}

static InputFrame[] BuildButtonLog(int frames)
{
    var log = new InputFrame[frames];
    for (int f = 0; f < frames; f++)
    {
        var frame = InputFrame.FromStick((f * 3) % 32, 2);
        if ((f % 11) == 0)
        {
            frame.Jump = true;
        }

        if ((f % 13) == 0)
        {
            frame.Attack = true;
        }

        if ((f % 17) == 0)
        {
            frame.Action = true;
        }

        log[f] = frame;
    }

    return log;
}

static string? RequireOption(string[] args, string name)
{
    for (int i = 0; i < args.Length - 1; i++)
    {
        if (args[i] == name)
        {
            return args[i + 1];
        }
    }

    return null;
}
