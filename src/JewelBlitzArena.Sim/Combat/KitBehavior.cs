namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Named kit exceptions that cannot be expressed as pure move-table data
    /// (<c>docs/tech/architecture.md</c> §2.5). S1 registers the enum; behaviors fill in later.
    /// </summary>
    public enum KitBehavior : byte
    {
        None = 0,
        VanishStrong = 1,
        CommandGrabCarry = 2,
        RiposteStrong = 3,
        ExtendedDodge = 4,
        TripleJumpGlide = 5,
        DoubleHold = 6,
        SwapPartners = 7,
        InvoiceRepeatable = 8,
    }

    public enum ActorAction : byte
    {
        Neutral = 0,
        Attack = 1,
        Strong = 2,
        Flare = 3,
        SurgeA = 4,
        SurgeB = 5,
        Dodge = 6,
        Throw = 7,
        Knockdown = 8,
        Transform = 9,
        Revert = 10,
        Getup = 11,
    }

    public enum ActionPhase : byte
    {
        None = 0,
        Startup = 1,
        Active = 2,
        Recovery = 3,
    }
}
