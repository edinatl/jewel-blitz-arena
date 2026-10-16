using JewelBlitzArena.Data;
using JewelBlitzArena.Meta;
using JewelBlitzArena.PresentationBridge;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.GreyboxCli;

internal static class Program
{
    private const ulong DefaultSeed = 0x53335F484F5354UL; // S3_HOST

    private static int Main(string[] args)
    {
        int frames = 180;
        ulong seed = DefaultSeed;
        int actors = 2;
        bool ascii = false;
        string? outPath = null;
        string? blobPath = null;
        bool scriptedWalk = true;
        bool useMeta = false;
        bool useLoopback = false;
        int deliveryDelay = 2;
        string? benchRoom = null;

        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];
            if (a is "--frames" && i + 1 < args.Length)
            {
                frames = int.Parse(args[++i]);
            }
            else if (a is "--seed" && i + 1 < args.Length)
            {
                seed = ulong.Parse(args[++i], System.Globalization.NumberStyles.HexNumber);
            }
            else if (a is "--actors" && i + 1 < args.Length)
            {
                actors = int.Parse(args[++i]);
            }
            else if (a is "--ascii")
            {
                ascii = true;
            }
            else if (a is "--out" && i + 1 < args.Length)
            {
                outPath = args[++i];
            }
            else if (a is "--blob" && i + 1 < args.Length)
            {
                blobPath = args[++i];
            }
            else if (a is "--neutral")
            {
                scriptedWalk = false;
            }
            else if (a is "--meta")
            {
                useMeta = true;
            }
            else if (a is "--bench" && i + 1 < args.Length)
            {
                benchRoom = args[++i].Trim().ToLowerInvariant();
            }
            else if (a is "--loopback")
            {
                useLoopback = true;
            }
            else if (a is "--delivery-delay" && i + 1 < args.Length)
            {
                deliveryDelay = int.Parse(args[++i]);
            }
            else if (a is "--help" or "-h")
            {
                PrintHelp();
                return 0;
            }
        }

        ContentCatalog? catalog = LoadCatalog(blobPath);

        if (benchRoom != null)
        {
            return RunBench(benchRoom, seed, outPath, catalog);
        }

        if (useMeta)
        {
            return RunMeta(frames, seed, actors, scriptedWalk, ascii, outPath, catalog);
        }

        if (useLoopback)
        {
            return RunLoopback(frames, seed, scriptedWalk, deliveryDelay, ascii, outPath, catalog);
        }

        return RunOffline(frames, seed, actors, scriptedWalk, ascii, outPath, catalog);
    }

    private static ContentCatalog? LoadCatalog(string? blobPath)
    {
        if (blobPath != null)
        {
            return ContentBlobReader.Read(File.ReadAllBytes(blobPath));
        }

        string defaultBlob = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "content", "export", "s5_stub.jba"));
        if (File.Exists(defaultBlob))
        {
            return ContentBlobReader.Read(File.ReadAllBytes(defaultBlob));
        }

        return null;
    }

    private static int RunBench(
        string room,
        ulong seed,
        string? outPath,
        ContentCatalog? catalog)
    {
        string profilePath = MetaGreyboxFlow.DefaultTempProfilePath("jba-cli-bench-");
        try
        {
            var flow = new MetaGreyboxFlow(profilePath);
            flow.SetCatalog(catalog);
            flow.BootProfile();
            flow.Profile.Carats = 5_000;
            flow.Profile.Chits = FacetWheelRuntime.SpinCostChits * 3;
            flow.Profile.ShopStockSeed = seed == 0UL ? 0x42454E4348UL : seed;
            // Seed a couple owned sellables / cosmetics for Sell + Fitting smoke.
            flow.Profile.Ledger.Grant(1);
            flow.Profile.Ledger.Grant(SampleContentFactory.RegentsDiademItemId);
            flow.Profile.Ledger.Grant(63);

            string render;
            if (room is "buy")
            {
                render = flow.RenderBuyRoom();
            }
            else if (room is "sell")
            {
                render = flow.RenderSellRoom();
            }
            else if (room is "fitting" or "fit")
            {
                flow.TryEquipCosmetic(SampleContentFactory.RegentsDiademItemId);
                render = flow.RenderFittingRoom();
            }
            else if (room is "ledger")
            {
                flow.Profile.Ledger.Grant(SampleContentFactory.SolitaireItemId);
                flow.TryPocketItem(SampleContentFactory.SolitaireItemId);
                render = flow.RenderLedgerRoom();
            }
            else if (room is "wheel" or "facet" or "facetwheel")
            {
                render = flow.RenderFacetWheelRoom(spinSeed: seed == 0UL ? 0x574845454CUL : seed);
            }
            else if (room is "rooms" or "menu")
            {
                render = flow.RenderBenchRooms(visitSeed: seed);
            }
            else
            {
                Console.Error.WriteLine(
                    $"unknown --bench room '{room}' (buy|sell|fitting|ledger|wheel|rooms)");
                return 2;
            }

            Console.WriteLine(render.TrimEnd());
            if (outPath != null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
                File.WriteAllText(outPath, render);
                Console.WriteLine($"wrote {outPath}");
            }

            return 0;
        }
        finally
        {
            string? dir = Path.GetDirectoryName(profilePath);
            if (dir != null && Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }

    private static int RunMeta(
        int frames,
        ulong seed,
        int actors,
        bool walk,
        bool ascii,
        string? outPath,
        ContentCatalog? catalog)
    {
        string profilePath = MetaGreyboxFlow.DefaultTempProfilePath("jba-cli-meta-");
        try
        {
            var flow = new MetaGreyboxFlow(profilePath);
            flow.SetCatalog(catalog);
            var request = new ModeRequest
            {
                Mode = ModeId.Custom,
                Seed = seed,
                ActorCount = actors,
            };
            MetaGreyboxRunResult result = flow.RunScripted(frames, request, walk);
            MatchState board = flow.Session.Current;
            string render = GreyboxBoard.Render(ref board);
            Console.WriteLine(
                $"meta seed=0x{seed:X16} frames={frames} hash=0x{result.Hash:X16} " +
                $"token={result.OwnsGreyboxToken} flags=0x{result.ProfileFlags:X}");
            if (ascii || outPath != null)
            {
                Console.WriteLine(render);
            }

            if (outPath != null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
                File.WriteAllText(
                    outPath,
                    $"meta seed=0x{seed:X16} frames={frames} hash=0x{result.Hash:X16} token={result.OwnsGreyboxToken}\n{render}");
                Console.WriteLine($"wrote {outPath}");
            }

            return result.OwnsGreyboxToken ? 0 : 2;
        }
        finally
        {
            string? dir = Path.GetDirectoryName(profilePath);
            if (dir != null && Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }

    private static int RunLoopback(
        int frames,
        ulong seed,
        bool walk,
        int deliveryDelay,
        bool ascii,
        string? outPath,
        ContentCatalog? catalog)
    {
        var runner = new LoopbackGreyboxRunner(inputDelay: 2, deliveryDelayFrames: deliveryDelay);
        runner.Reset(seed, catalog);
        for (int f = 0; f < frames; f++)
        {
            InputFrame p0 = InputFrame.Neutral;
            InputFrame p1 = InputFrame.Neutral;
            if (walk)
            {
                int dir = (f / 8) % 2 == 0 ? 1 : -1;
                p0 = InputQuantizer.FromCardinal(dir, 0);
                p1 = InputQuantizer.FromCardinal(-dir, 0);
            }

            runner.Tick(p0, p1);
        }

        MatchState board = runner.Current;
        string render = GreyboxBoard.Render(ref board);
        bool match = runner.StatesMatch();
        bool hashes = runner.HashesAgree();
        Console.WriteLine(
            $"loopback seed=0x{seed:X16} frames={frames} hash=0x{runner.Hash:X16} " +
            $"rollbacks={runner.RollbackCount} match={match} probeOk={hashes} desync={runner.DesyncFlagged}");
        if (ascii || outPath != null)
        {
            Console.WriteLine(render);
        }

        if (outPath != null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
            File.WriteAllText(
                outPath,
                $"loopback seed=0x{seed:X16} frames={frames} hash=0x{runner.Hash:X16} rollbacks={runner.RollbackCount}\n{render}");
            Console.WriteLine($"wrote {outPath}");
        }

        if (!match || !hashes || runner.DesyncFlagged)
        {
            return 2;
        }

        if (deliveryDelay > 0 && runner.RollbackCount == 0)
        {
            Console.Error.WriteLine("expected rollbacks with delivery delay");
            return 3;
        }

        Console.WriteLine("restart hash OK");
        return 0;
    }

    private static int RunOffline(
        int frames,
        ulong seed,
        int actors,
        bool scriptedWalk,
        bool ascii,
        string? outPath,
        ContentCatalog? catalog)
    {
        var session = new MatchSession();
        MatchConfig config = MatchConfig.CreateDefault(seed, actors);
        session.Reset(config, catalog);

        for (int f = 0; f < frames; f++)
        {
            InputFrame p0 = InputFrame.Neutral;
            InputFrame p1 = InputFrame.Neutral;
            if (scriptedWalk)
            {
                p0 = InputQuantizer.FromCardinal(1, 0);
                p1 = InputQuantizer.FromCardinal(-1, 0);
                if ((f / 30) % 2 == 1)
                {
                    p0 = InputQuantizer.FromCardinal(0, 1);
                    p1 = InputQuantizer.FromCardinal(0, -1);
                }
            }

            session.Step(p0, p1, InputFrame.Neutral, InputFrame.Neutral);
        }

        ulong hash = session.Hash();
        MatchState board = session.Current;
        string render = GreyboxBoard.Render(ref board);

        Console.WriteLine($"seed=0x{seed:X16} frames={frames} hash=0x{hash:X16}");
        if (ascii || outPath != null)
        {
            Console.WriteLine(render);
        }

        if (outPath != null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
            File.WriteAllText(outPath, $"seed=0x{seed:X16} frames={frames} hash=0x{hash:X16}\n{render}");
            Console.WriteLine($"wrote {outPath}");
        }

        var verify = new MatchSession();
        verify.Reset(config, catalog);
        for (int f = 0; f < frames; f++)
        {
            InputFrame p0 = InputFrame.Neutral;
            InputFrame p1 = InputFrame.Neutral;
            if (scriptedWalk)
            {
                p0 = InputQuantizer.FromCardinal(1, 0);
                p1 = InputQuantizer.FromCardinal(-1, 0);
                if ((f / 30) % 2 == 1)
                {
                    p0 = InputQuantizer.FromCardinal(0, 1);
                    p1 = InputQuantizer.FromCardinal(0, -1);
                }
            }

            verify.Step(p0, p1, InputFrame.Neutral, InputFrame.Neutral);
        }

        if (verify.Hash() != hash)
        {
            Console.Error.WriteLine($"RESTART MISMATCH 0x{hash:X16} vs 0x{verify.Hash():X16}");
            return 2;
        }

        Console.WriteLine("restart hash OK");
        return 0;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            JewelBlitzArena.GreyboxCli — headless presentation bridge smoke

              --frames N          Sim frames to step (default 180)
              --seed HEX          Match seed
              --actors N          Actor count 1–4 (default 2)
              --ascii             Print ASCII board
              --out PATH          Write board dump
              --blob PATH         Optional .jba content blob
              --neutral           All-neutral inputs
              --meta              Boot → Custom → profile stamp path
              --bench buy|sell|fitting|ledger|wheel|rooms
                                  Editor-free Bench room ASCII
              --loopback          DualSessionHarness rollback greybox
              --delivery-delay N  Loopback one-way delay in frames (default 2)
            """);
    }
}
