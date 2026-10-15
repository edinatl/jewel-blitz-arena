using System.Diagnostics;
using JewelBlitzArena.Net;
using JewelBlitzArena.PresentationBridge;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.LanSmoke;

/// <summary>
/// UDP LAN smoke. Default: single-process dual <see cref="UdpLanTransport"/> lockstep
/// (proves the socket path without Unity). Optional <c>--two-process</c> launches peer roles.
/// </summary>
internal static class Program
{
    private const ulong Seed = 0x4C414E5F534D4BUL; // LAN_SMK

    private static int Main(string[] args)
    {
        int role = -1;
        int frames = 120;
        int basePort = 41200;
        int inputDelay = SessionConfig.DefaultInputDelay;
        bool twoProcess = false;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] is "--role" && i + 1 < args.Length)
            {
                role = int.Parse(args[++i]);
            }
            else if (args[i] is "--frames" && i + 1 < args.Length)
            {
                frames = int.Parse(args[++i]);
            }
            else if (args[i] is "--port" && i + 1 < args.Length)
            {
                basePort = int.Parse(args[++i]);
            }
            else if (args[i] is "--input-delay" && i + 1 < args.Length)
            {
                inputDelay = int.Parse(args[++i]);
            }
            else if (args[i] is "--two-process")
            {
                twoProcess = true;
            }
            else if (args[i] is "--help" or "-h")
            {
                Console.WriteLine("""
                    JewelBlitzArena.LanSmoke — UDP greybox smoke
                      (default) single-process dual UdpLanTransport lockstep
                      --two-process     parent launches --role 0 and --role 1
                      --role 0|1        peer mode (used by parent)
                      --frames N --port BASE --input-delay N
                    """);
                return 0;
            }
        }

        if (role >= 0)
        {
            return RunPeer(role, frames, basePort, inputDelay);
        }

        if (twoProcess)
        {
            return RunTwoProcessParent(frames, basePort, inputDelay);
        }

        return RunInProcessUdp(frames, basePort, inputDelay);
    }

    private static int RunInProcessUdp(int frames, int basePort, int inputDelay)
    {
        (UdpLanTransport t0, UdpLanTransport t1) = UdpLanTransport.CreateLocalhostPair(basePort);
        try
        {
            var s0 = new RollbackSession(SessionConfig.Lan2P(0, inputDelay), t0);
            var s1 = new RollbackSession(SessionConfig.Lan2P(1, inputDelay), t1);
            MatchConfig config = MatchConfig.CreateDefault(Seed, actorCount: 2);
            s0.Reset(config);
            s1.Reset(config);

            int stalled = 0;
            int rollbacks = 0;
            for (int f = 0; f < frames; f++)
            {
                int dir = (f / 8) % 2 == 0 ? 1 : -1;
                InputFrame i0 = InputQuantizer.FromCardinal(dir, 0, attack: (f % 17) == 0);
                InputFrame i1 = InputQuantizer.FromCardinal(-dir, 0, attack: (f % 19) == 0);
                bool a0 = s0.Advance(i0);
                bool a1 = s1.Advance(i1);
                if (!a0 || !a1)
                {
                    stalled++;
                }
            }

            rollbacks = s0.RollbackCount + s1.RollbackCount;
            ulong h0 = s0.Sim.Hash();
            ulong h1 = s1.Sim.Hash();
            bool match = h0 == h1;
            Console.WriteLine(
                $"udp-lockstep frames={frames} port={basePort} inputDelay={inputDelay} " +
                $"rollbacks={rollbacks} stalledTicks={stalled} " +
                $"hash0=0x{h0:X16} hash1=0x{h1:X16} match={match} " +
                $"desync={s0.DesyncFlagged || s1.DesyncFlagged}");

            MatchState board = s0.Sim.Snapshot();
            Console.Write(GreyboxBoard.Render(ref board));

            if (!match || s0.DesyncFlagged || s1.DesyncFlagged)
            {
                Console.Error.WriteLine("DESYNC");
                return 2;
            }

            Console.WriteLine("lan ok");
            return 0;
        }
        finally
        {
            t0.Dispose();
            t1.Dispose();
        }
    }

    private static int RunTwoProcessParent(int frames, int basePort, int inputDelay)
    {
        string dll = typeof(Program).Assembly.Location;
        ProcessStartInfo Make(int role)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            psi.ArgumentList.Add(dll);
            psi.ArgumentList.Add("--role");
            psi.ArgumentList.Add(role.ToString());
            psi.ArgumentList.Add("--frames");
            psi.ArgumentList.Add(frames.ToString());
            psi.ArgumentList.Add("--port");
            psi.ArgumentList.Add(basePort.ToString());
            psi.ArgumentList.Add("--input-delay");
            psi.ArgumentList.Add(inputDelay.ToString());
            return psi;
        }

        using Process p0 = Process.Start(Make(0)) ?? throw new InvalidOperationException("peer0");
        using Process p1 = Process.Start(Make(1)) ?? throw new InvalidOperationException("peer1");
        string out0 = p0.StandardOutput.ReadToEnd();
        string err0 = p0.StandardError.ReadToEnd();
        string out1 = p1.StandardOutput.ReadToEnd();
        string err1 = p1.StandardError.ReadToEnd();
        _ = p0.WaitForExit(60_000);
        _ = p1.WaitForExit(60_000);

        Console.WriteLine("--- peer0 ---");
        Console.Write(out0);
        if (err0.Length > 0)
        {
            Console.Error.Write(err0);
        }

        Console.WriteLine("--- peer1 ---");
        Console.Write(out1);
        if (err1.Length > 0)
        {
            Console.Error.Write(err1);
        }

        // Two-process is best-effort (independent clocks); parent only checks both exited 0.
        if (p0.ExitCode != 0 || p1.ExitCode != 0)
        {
            Console.Error.WriteLine($"peer exit codes {p0.ExitCode}/{p1.ExitCode}");
            return 2;
        }

        Console.WriteLine("two-process peers exited 0 (hashes may differ without lockstep clock)");
        return 0;
    }

    private static int RunPeer(int role, int frames, int basePort, int inputDelay)
    {
        if (role is not (0 or 1))
        {
            return 1;
        }

        int localPort = basePort + role;
        int remotePort = basePort + (1 - role);
        using var transport = new UdpLanTransport(localPort, "127.0.0.1", remotePort);
        var session = new RollbackSession(SessionConfig.Lan2P(role, inputDelay), transport);
        session.Reset(MatchConfig.CreateDefault(Seed, actorCount: 2));
        Thread.Sleep(80);

        int advanced = 0;
        for (int f = 0; f < frames; f++)
        {
            int dir = (f / 8) % 2 == 0 ? 1 : -1;
            if (role == 1)
            {
                dir = -dir;
            }

            InputFrame input = InputQuantizer.FromCardinal(dir, 0, attack: (f % 17) == 0);
            if (session.Advance(input))
            {
                advanced++;
            }

            Thread.Sleep(2);
        }

        Console.WriteLine(
            $"role={role} advanced={advanced}/{frames} rollbacks={session.RollbackCount} " +
            $"stall={session.IsStalling}/{session.StallFrames} hash=0x{session.Sim.Hash():X16}");
        return session.DesyncFlagged ? 2 : 0;
    }
}
