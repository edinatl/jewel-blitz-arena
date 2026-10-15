using System.Runtime.InteropServices;

namespace JewelBlitzArena.Sim
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ActorSlot
    {
        public Fix PosX;
        public Fix PosY;
        public Fix PosZ;
        public Fix VelX;
        public Fix VelY;
        public Fix VelZ;

        public int Life;
        public int MaxLife;
        public int Luster;
        public int FighterId;

        public int StunFrames;
        public int KnockdownFrames;
        public int InvulnFrames;
        public int ActionTimer;
        public int MoveDamage;
        public int ComboChain;
        public int ComboDamage;
        public int ComboResetTimer;
        public int DodgeCooldown;
        public int DodgeArmed;
        public int DrossFrames;
        public int TransformFrames;
        public int RevertFrames;
        public int BodyPropFrames;
        public int AttackStringStep;
        public int AttackStringTimer;
        public int KosDealt;
        public int JewelsCollectedTotal;
        public int LastHitFrame;

        public byte JewelsHeld;
        public byte Team;
        public byte Flags;
        public byte Action;
        public byte ActionPhase;
        public byte KitBehavior;
        public byte ReachRating;
        public byte HitApplied;
        public byte Facing; // 0 = +X, 1 = -X
        public byte HeldItemSlot; // 255 = empty (S2)

        /// <summary>Blinded timer (balance.md §4); was Reserved1 — same layout stride.</summary>
        public ushort BlindFrames;

        public const byte FlagAlive = 1 << 0;
        public const byte FlagBlitzed = 1 << 1;
        public const byte FlagEliminated = 1 << 2;
        public const byte NoHeldItem = 255;

        public static int SizeOf
        {
            get
            {
                unsafe
                {
                    return sizeof(ActorSlot);
                }
            }
        }

        public bool IsAlive
        {
            get => (Flags & FlagAlive) != 0;
            set => SetFlag(FlagAlive, value);
        }

        public bool IsBlitzed
        {
            get => (Flags & FlagBlitzed) != 0;
            set => SetFlag(FlagBlitzed, value);
        }

        public bool IsEliminated
        {
            get => (Flags & FlagEliminated) != 0;
            set => SetFlag(FlagEliminated, value);
        }

        public bool HasEmptyHands => HeldItemSlot == NoHeldItem;

        public bool CanAct =>
            IsAlive
            && !IsEliminated
            && StunFrames <= 0
            && KnockdownFrames <= 0
            && TransformFrames <= 0
            && RevertFrames <= 0
            && BodyPropFrames <= 0;

        private void SetFlag(byte flag, bool value)
        {
            if (value)
            {
                Flags = (byte)(Flags | flag);
            }
            else
            {
                Flags = (byte)(Flags & ~flag);
            }
        }
    }
}
