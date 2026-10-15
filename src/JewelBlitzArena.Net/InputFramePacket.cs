using System.Runtime.InteropServices;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Net
{
    /// <summary>
    /// Wire payload for one player's quantized input at a sim frame (2-byte <see cref="InputFrame"/>).
    /// Packets may carry a short history of recent frames for redundancy (scaffold: single frame).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct InputFramePacket
    {
        public int Frame;
        public byte PlayerIndex;
        public byte Reserved;
        public ushort Bits;

        public InputFrame ToInputFrame() => new InputFrame { Bits = Bits };

        public static InputFramePacket From(int frame, byte playerIndex, InputFrame input) =>
            new InputFramePacket
            {
                Frame = frame,
                PlayerIndex = playerIndex,
                Reserved = 0,
                Bits = input.Bits,
            };
    }
}
