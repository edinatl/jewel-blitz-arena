namespace JewelBlitzArena.Meta
{
    /// <summary>Lobby inputs used to build a <see cref="Sim.MatchConfig"/>.</summary>
    public sealed class ModeRequest
    {
        public ModeId Mode { get; set; } = ModeId.Custom;

        public ulong Seed { get; set; } = 0x53365F4D455441UL; // S6_META

        public int ActorCount { get; set; } = 2;

        public int StageId { get; set; }

        /// <summary>Fighter ids per slot; null → sequential defaults.</summary>
        public int[]? FighterIds { get; set; }

        public HouseRulesSettings HouseRules { get; set; } = HouseRulesSettings.CreateDefaults();

        /// <summary>Expedition difficulty 1–8 (default 4). Clamped by the orchestrator.</summary>
        public int Difficulty { get; set; } = ExpeditionChestRuntime.DiffDefault;

        /// <summary>
        /// Expedition ladder slot 0–4 (FFA1, FFA2, Harvester, FFA4, Pressworks).
        /// −1 → infer from StageId (bosses exact; story FFAs default to slot 0).
        /// </summary>
        public int LadderStageIndex { get; set; } = -1;

        /// <summary>Sparring: jewel_mode respawn (Meta flag only until Sim training lands).</summary>
        public bool SparringJewelRespawn { get; set; }

        /// <summary>Sparring: item spawner pause insert enabled (Meta flag only).</summary>
        public bool SparringItemSpawner { get; set; }
    }
}
