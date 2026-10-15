namespace JewelBlitzArena.Data
{
    public static class ContentSchema
    {
        /// <summary>
        /// v2: stage graph phase transitions, hazards, kinematics, camera volumes.
        /// v3: optional authored <c>scripted_camera</c> path keys per phase enter-transition.
        /// v4: fighter <c>kitBehavior</c> named exception enum (architecture §2.5).
        /// v5: move <c>effect</c> (Heal / Magnet / Utility) for Surge semantics.
        /// v6: move effects Stun / Blind (Bateria, Spare Chaff).
        /// </summary>
        public const uint Version = 6;
        public const string BlobExtension = ".jba";
        public static readonly byte[] Magic = { (byte)'J', (byte)'B', (byte)'A', (byte)'D' };

        public const int MaxNameLength = 48;
        public const int MaxMovesPerKit = 16;
        public const int MaxPhasesPerStage = 8;
        public const int MaxHazardsPerPhase = 8;
        public const int MaxKinematicsPerPhase = 8;
        public const int MaxItems = 256;
        public const int MaxFighters = 32;
        public const int MaxStages = 32;
    }

    public enum CarryItemsMode : byte
    {
        All = 0,
        HeldOnly = 1,
        None = 2,
    }

    public enum CarryJewelsMode : byte
    {
        All = 0,
        DropChance = 1,
        None = 2,
    }

    public enum HazardTriggerKind : byte
    {
        PhaseEnter = 0,
        Timer = 1,
        Proximity = 2,
    }

    public enum ItemCategory : byte
    {
        Firearm = 0,
        Explosive = 1,
        Melee = 2,
        Healing = 3,
        Status = 4,
        Mobility = 5,
        Summon = 6,
        Toy = 7,
        Material = 8,
        Card = 9,
        Cosmetic = 10,
        Gift = 11,
    }

    public enum CrateEligibility : byte
    {
        Always = 0,
        WhenOwned = 1,
        Never = 2,
    }

    public enum MoveClass : byte
    {
        Attack = 0,
        Strong = 1,
        AirAttack = 2,
        AirStrong = 3,
        DashAttack = 4,
        Throw = 5,
        GetupAttack = 6,
        Flare = 7,
        Surge = 8,
        UtilityStrong = 9,
    }

    /// <summary>Optional move side-effect beyond hit damage (cast.md Surge utilities / heals).</summary>
    public enum MoveEffect : byte
    {
        None = 0,
        /// <summary>Restore Life equal to <see cref="MoveRecord.Damage"/>; no offensive hit.</summary>
        Heal = 1,
        /// <summary>No offensive hit; strip held items and pull/catch loose items in range.</summary>
        Magnet = 2,
        /// <summary>No offensive hit (control / blackout stubs).</summary>
        Utility = 3,
        /// <summary>Area Stun (Bateria); keeps <see cref="MoveRecord.Damage"/> as hit damage.</summary>
        Stun = 4,
        /// <summary>Area Blind (Spare Chaff); keeps <see cref="MoveRecord.Damage"/> as hit damage.</summary>
        Blind = 5,
    }
}
