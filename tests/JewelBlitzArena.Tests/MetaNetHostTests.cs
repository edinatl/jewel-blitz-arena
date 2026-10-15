using JewelBlitzArena.Meta;
using JewelBlitzArena.Net;
using JewelBlitzArena.PresentationBridge;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class MetaNetHostTests
{
    private const ulong Seed = 0x4D4554414E4554UL; // METANET

    [Test]
    public void MetaGreyboxFlow_boot_custom_stamps_token()
    {
        string path = MetaGreyboxFlow.DefaultTempProfilePath("jba-host-meta-");
        try
        {
            var flow = new MetaGreyboxFlow(path);
            var request = new ModeRequest
            {
                Mode = ModeId.Custom,
                Seed = Seed,
                ActorCount = 2,
            };
            MetaGreyboxRunResult result = flow.RunScripted(60, request, walk: true);
            Assert.That(result.OwnsGreyboxToken, Is.True);
            Assert.That(result.ProfileFlags & ProfileFlags.CompletedCustomGreybox, Is.Not.EqualTo(0));
            Assert.That(result.Hash, Is.Not.EqualTo(0UL));
            Assert.That(File.Exists(path), Is.True);
        }
        finally
        {
            string? dir = Path.GetDirectoryName(path);
            if (dir != null && Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }

    [Test]
    public void LoopbackGreyboxRunner_stays_in_sync()
    {
        var runner = new LoopbackGreyboxRunner(inputDelay: 2, deliveryDelayFrames: 3);
        runner.Reset(Seed);
        for (int f = 0; f < 90; f++)
        {
            // Flip stick so delayed delivery misses repeat-last prediction.
            int dir = (f / 8) % 2 == 0 ? 1 : -1;
            InputFrame i0 = InputQuantizer.FromCardinal(dir, 0, attack: (f % 17) == 0);
            InputFrame i1 = InputQuantizer.FromCardinal(-dir, 0, attack: (f % 19) == 0);
            runner.Tick(i0, i1);
        }

        Assert.That(runner.StatesMatch(), Is.True);
        Assert.That(runner.HashesAgree(), Is.True);
        Assert.That(runner.DesyncFlagged, Is.False);
        Assert.That(runner.RollbackCount, Is.GreaterThan(0));
        Assert.That(runner.Current.Frame, Is.EqualTo(90));
    }

    [Test]
    public void UdpLanTransport_localhost_pair_exchanges_packets()
    {
        (UdpLanTransport p0, UdpLanTransport p1) = UdpLanTransport.CreateLocalhostPair(42100);
        try
        {
            InputFrame frame = InputQuantizer.FromCardinal(1, 0, attack: true);
            InputFramePacket sent = InputFramePacket.From(7, 0, frame);
            p0.Send(sent);

            // Give the datagram a moment on loopback.
            var buf = new InputFramePacket[8];
            int got = 0;
            for (int attempt = 0; attempt < 50 && got == 0; attempt++)
            {
                Thread.Sleep(2);
                got = p1.Poll(buf);
            }

            Assert.That(got, Is.EqualTo(1));
            Assert.That(buf[0].Frame, Is.EqualTo(7));
            Assert.That(buf[0].PlayerIndex, Is.EqualTo(0));
            Assert.That(buf[0].Bits, Is.EqualTo(frame.Bits));
        }
        finally
        {
            p0.Dispose();
            p1.Dispose();
        }
    }

    [Test]
    public void UdpLanTransport_serialize_round_trip()
    {
        InputFramePacket src = InputFramePacket.From(99, 1, InputQuantizer.FromAxes(0f, 1f, jump: true));
        byte[] bytes = UdpLanTransport.Serialize(src);
        Assert.That(bytes.Length, Is.EqualTo(UdpLanTransport.PacketBytes));
        Assert.That(UdpLanTransport.TryDeserialize(bytes, out InputFramePacket dst), Is.True);
        Assert.That(dst.Frame, Is.EqualTo(99));
        Assert.That(dst.PlayerIndex, Is.EqualTo(1));
        Assert.That(dst.Bits, Is.EqualTo(src.Bits));
    }
}
