using JewelBlitzArena.Data;
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
            else if (a is "--help" or "-h")
            {
                PrintHelp();
                return 0;
            }
        }

        ContentCatalog? catalog = null;
        if (blobPath != null)
        {
            catalog = ContentBlobReader.Read(File.ReadAllBytes(blobPath));
        }
        else
        {
            string defaultBlob = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory, "..", "..", "..", "..", "..", "content", "export", "s5_stub.jba"));
            if (File.Exists(defaultBlob))
            {
                catalog = ContentBlobReader.Read(File.ReadAllBytes(defaultBlob));
            }
        }

        var session = new MatchSession();
        MatchConfig config = MatchConfig.CreateDefault(seed, actors);
        session.Reset(config, catalog);

        for (int f = 0; f < frames; f++)
        {
            InputFrame p0 = InputFrame.Neutral;
            InputFrame p1 = InputFrame.Neutral;
            if (scriptedWalk)
            {
                // Pace toward +X / −X so actors separate on the board.
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

        // Restart parity (same seed, same scripted inputs).
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

        ulong hash2 = verify.Hash();
        if (hash2 != hash)
        {
            Console.Error.WriteLine($"RESTART MISMATCH 0x{hash:X16} vs 0x{hash2:X16}");
            return 2;
        }

        Console.WriteLine("restart hash OK");
        return 0;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            JewelBlitzArena.GreyboxCli — headless presentation bridge smoke

              --frames N     Sim frames to step (default 180)
              --seed HEX     Match seed (default S3_HOST)
              --actors N     Actor count 1–4 (default 2)
              --ascii        Print ASCII board
              --out PATH     Write board dump
              --blob PATH    Optional .jba content blob
              --neutral      All-neutral inputs (no scripted walk)
            """);
    }
}
