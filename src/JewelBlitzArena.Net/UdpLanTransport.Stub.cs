using System;

namespace JewelBlitzArena.Net
{
    /// <summary>
    /// Placeholder for LAN UDP (and later Steam Datagram) transport.
    /// S7 scaffold proves rollback over <see cref="LoopbackHub"/>; real sockets are a follow-on.
    /// </summary>
    public sealed class UdpLanTransport : INetTransport
    {
        public void Send(InputFramePacket packet) =>
            throw new NotImplementedException("LAN UDP transport is not wired in the S7 scaffold; use LoopbackHub / DualSessionHarness.");

        public int Poll(InputFramePacket[] destination) =>
            throw new NotImplementedException("LAN UDP transport is not wired in the S7 scaffold; use LoopbackHub / DualSessionHarness.");
    }
}
