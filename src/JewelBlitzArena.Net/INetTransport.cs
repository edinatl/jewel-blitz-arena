namespace JewelBlitzArena.Net
{
    /// <summary>Minimal peer input transport. Steam Datagram later; LAN/loopback for scaffold.</summary>
    public interface INetTransport
    {
        void Send(InputFramePacket packet);

        /// <summary>Drain received packets into <paramref name="destination"/>; returns count written.</summary>
        int Poll(InputFramePacket[] destination);
    }
}
