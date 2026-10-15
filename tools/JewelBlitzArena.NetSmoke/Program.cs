using JewelBlitzArena.Net;
using JewelBlitzArena.PresentationBridge;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.NetSmoke;

internal static class Program
{
    private const ulong Seed = 0x53375F4E4554UL; // S7_NET

    private static int Main(string[] args)
    {
        int frames = 180;
        int delay = SessionConfig.DefaultInputDelay;
        int delivery = 3;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] is "--frames" && i + 1 < args.Length)
            {
                frames = int.Parse(args[++i]);
            }
            else if (args[i] is "--input-delay" && i + 1 < args.Length)
            {
                delay = int.Parse(args[++i]);
            }
            else if (args[i] is "--delivery-delay" && i + 1 < args.Length)
            {
                delivery = int.Parse(args[++i]);
            }
            else if (args[i] is "--help" or "-h")
            {
                Console.WriteLine("JewelBlitzArena.NetSmoke — dual loopback rollback smoke");
                Console.WriteLine("  --frames N --input-delay N --delivery-delay N");
                return 0;
            }
        }

        var harness = new DualSessionHarness(inputDelay: delay, deliveryDelayFrames: delivery);
        harness.Reset(Seed);

        for (int f = 0; f < frames; f++)
        {
            int dir = (f / 8) % 2 == 0 ? 1 : -1;
            InputFrame i0 = InputQuantizer.FromCardinal(dir, 0, attack: (f % 17) == 0);
            InputFrame i1 = InputQuantizer.FromCardinal(-dir, 0, action: (f % 19) == 0);
            harness.Tick(i0, i1);
        }

        bool match = harness.StatesMatch();
        bool hashes = harness.HashesAgreeAtLastProbe();
        Console.WriteLine(
            $"frames={frames} inputDelay={delay} deliveryDelay={delivery} " +
            $"rollbacks={harness.Player0.RollbackCount}+{harness.Player1.RollbackCount} " +
            $"misses={harness.Player0.PredictionMisses}+{harness.Player1.PredictionMisses} " +
            $"hash0=0x{harness.Player0.Sim.Hash():X16} hash1=0x{harness.Player1.Sim.Hash():X16} " +
            $"match={match} probeOk={hashes}");

        if (!match || !hashes || harness.Player0.DesyncFlagged || harness.Player1.DesyncFlagged)
        {
            Console.Error.WriteLine("DESYNC");
            return 2;
        }

        if (delivery > 0 && harness.Player0.RollbackCount + harness.Player1.RollbackCount == 0)
        {
            Console.Error.WriteLine("expected rollbacks with delivery delay");
            return 3;
        }

        Console.WriteLine("ok");
        return 0;
    }
}
