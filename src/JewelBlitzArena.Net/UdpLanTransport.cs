using System;
using System.Net;
using System.Net.Sockets;

namespace JewelBlitzArena.Net
{
    /// <summary>
    /// Minimal LAN UDP transport for 2P greybox. Sends 8-byte <see cref="InputFramePacket"/>
    /// datagrams; non-blocking poll. Steam Datagram can replace this later behind <see cref="INetTransport"/>.
    /// </summary>
    public sealed class UdpLanTransport : INetTransport, IDisposable
    {
        public const int PacketBytes = 8;

        private readonly UdpClient _udp;
        private readonly IPEndPoint _remote;
        private bool _disposed;

        public UdpLanTransport(int localPort, string remoteHost, int remotePort)
        {
            _udp = new UdpClient(localPort);
            _udp.Client.Blocking = false;
            _remote = new IPEndPoint(IPAddress.Parse(remoteHost), remotePort);
        }

        /// <summary>Localhost pair helper: peer 0 uses basePort, peer 1 uses basePort+1.</summary>
        public static (UdpLanTransport peer0, UdpLanTransport peer1) CreateLocalhostPair(int basePort = 41000)
        {
            var p0 = new UdpLanTransport(basePort, "127.0.0.1", basePort + 1);
            var p1 = new UdpLanTransport(basePort + 1, "127.0.0.1", basePort);
            return (p0, p1);
        }

        public void Send(InputFramePacket packet)
        {
            byte[] buf = Serialize(packet);
            _ = _udp.Send(buf, buf.Length, _remote);
        }

        public int Poll(InputFramePacket[] destination)
        {
            if (destination == null || destination.Length == 0)
            {
                return 0;
            }

            int written = 0;
            while (written < destination.Length)
            {
                if (_udp.Available <= 0)
                {
                    break;
                }

                IPEndPoint? remote = null;
                byte[] data;
                try
                {
                    data = _udp.Receive(ref remote);
                }
                catch (SocketException)
                {
                    break;
                }

                if (!TryDeserialize(data, out InputFramePacket packet))
                {
                    continue;
                }

                destination[written++] = packet;
            }

            return written;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _udp.Dispose();
        }

        public static byte[] Serialize(InputFramePacket packet)
        {
            byte[] buf = new byte[PacketBytes];
            WriteInt32LE(buf, 0, packet.Frame);
            buf[4] = packet.PlayerIndex;
            buf[5] = packet.Reserved;
            buf[6] = (byte)(packet.Bits & 0xFF);
            buf[7] = (byte)((packet.Bits >> 8) & 0xFF);
            return buf;
        }

        public static bool TryDeserialize(byte[] data, out InputFramePacket packet)
        {
            packet = default;
            if (data == null || data.Length < PacketBytes)
            {
                return false;
            }

            packet = new InputFramePacket
            {
                Frame = ReadInt32LE(data, 0),
                PlayerIndex = data[4],
                Reserved = data[5],
                Bits = (ushort)(data[6] | (data[7] << 8)),
            };
            return true;
        }

        private static void WriteInt32LE(byte[] buf, int offset, int value)
        {
            unchecked
            {
                buf[offset] = (byte)value;
                buf[offset + 1] = (byte)(value >> 8);
                buf[offset + 2] = (byte)(value >> 16);
                buf[offset + 3] = (byte)(value >> 24);
            }
        }

        private static int ReadInt32LE(byte[] buf, int offset) =>
            buf[offset]
            | (buf[offset + 1] << 8)
            | (buf[offset + 2] << 16)
            | (buf[offset + 3] << 24);
    }
}
