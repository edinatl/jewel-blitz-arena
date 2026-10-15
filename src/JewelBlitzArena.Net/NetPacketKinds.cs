namespace JewelBlitzArena.Net
{
    /// <summary>Discriminates payload kinds in <see cref="InputFramePacket.Reserved"/>.</summary>
    public static class NetPacketKinds
    {
        public const byte Input = 0;

        /// <summary>
        /// CPU handoff proposal (modes §7). <see cref="InputFramePacket.Frame"/> = agree frame,
        /// <see cref="InputFramePacket.PlayerIndex"/> = target slot, <see cref="InputFramePacket.Bits"/> = proposer.
        /// </summary>
        public const byte CpuHandoffPropose = 0xC1;
    }
}
