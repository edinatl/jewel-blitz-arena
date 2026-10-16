using System.Collections.Generic;
using JewelBlitzArena.Data;

namespace JewelBlitzArena.Meta
{
    /// <summary>
    /// Custom Extra Arena box (modes.md §8 / stages.md §4).
    /// Slots unlock at clears 9 / 11 / 13 / 14; slot 4 needs full cast.
    /// Stage graphs live in S5 at ids 101–104; the box lists them after story/boss stages.
    /// </summary>
    public static class ExtraArenaRuntime
    {
        public const int TerracesNoBossStageId = SampleContentFactory.TerracesNoBossStageId;
        public const int BehindCounterStageId = SampleContentFactory.BehindCounterStageId;
        public const int SpirewalkStageId = SampleContentFactory.SpirewalkStageId;
        public const int CascadeStageId = SampleContentFactory.CascadeStageId;

        public static readonly int[] SlotStageIds =
        {
            TerracesNoBossStageId,
            BehindCounterStageId,
            SpirewalkStageId,
            CascadeStageId,
        };

        public static readonly string[] SlotNames =
        {
            "Terraces (no boss)",
            "Behind the Counter",
            "Spirewalk",
            "The Cascade",
        };

        public readonly struct ExtraArenaEntry
        {
            public ExtraArenaEntry(int slot, int stageId, string name, bool unlocked)
            {
                Slot = slot;
                StageId = stageId;
                Name = name;
                Unlocked = unlocked;
            }

            /// <summary>1-based Extra Arena slot.</summary>
            public int Slot { get; }
            public int StageId { get; }
            public string Name { get; }
            public bool Unlocked { get; }
        }

        public readonly struct StageCursorEntry
        {
            public StageCursorEntry(
                int stageId,
                string label,
                bool unlocked,
                bool isExtraArenaBox)
            {
                StageId = stageId;
                Label = label;
                Unlocked = unlocked;
                IsExtraArenaBox = isExtraArenaBox;
            }

            public int StageId { get; }
            public string Label { get; }
            public bool Unlocked { get; }
            public bool IsExtraArenaBox { get; }
        }

        public static bool IsExtraArenaStageId(int stageId)
        {
            for (int i = 0; i < SlotStageIds.Length; i++)
            {
                if (SlotStageIds[i] == stageId)
                {
                    return true;
                }
            }

            return false;
        }

        public static IReadOnlyList<ExtraArenaEntry> BoxEntries(PlayerProfile profile)
        {
            int unlockedSlots = UnlockLadderRuntime.ExtraArenaSlotCount(profile);
            var list = new List<ExtraArenaEntry>(SlotStageIds.Length);
            for (int i = 0; i < SlotStageIds.Length; i++)
            {
                string name = SlotNames[i];
                list.Add(new ExtraArenaEntry(
                    slot: i + 1,
                    stageId: SlotStageIds[i],
                    name: name,
                    unlocked: (i + 1) <= unlockedSlots));
            }

            return list;
        }

        public static bool IsStageUnlocked(PlayerProfile profile, int stageId)
        {
            if (profile == null)
            {
                return false;
            }

            if (!IsExtraArenaStageId(stageId))
            {
                // Story / boss stages always selectable in Custom greybox.
                return true;
            }

            int slots = UnlockLadderRuntime.ExtraArenaSlotCount(profile);
            for (int i = 0; i < SlotStageIds.Length; i++)
            {
                if (SlotStageIds[i] == stageId)
                {
                    return (i + 1) <= slots;
                }
            }

            return false;
        }

        /// <summary>
        /// Custom stage cursor: story + boss from catalog (Extra Arenas excluded), then
        /// Extra Arena box rows (visible locked until unlock — modes.md §4).
        /// </summary>
        public static IReadOnlyList<StageCursorEntry> BuildCustomStageCursor(
            PlayerProfile profile,
            ContentCatalog? catalog)
        {
            var list = new List<StageCursorEntry>();
            if (catalog != null)
            {
                foreach (StageRecord stage in catalog.Stages)
                {
                    if (IsExtraArenaStageId(stage.StageId))
                    {
                        continue;
                    }

                    list.Add(new StageCursorEntry(
                        stage.StageId,
                        stage.Name,
                        unlocked: true,
                        isExtraArenaBox: false));
                }
            }
            else
            {
                list.Add(new StageCursorEntry(
                    SampleContentFactory.CitrineBazaarStageId,
                    "Citrine Bazaar",
                    unlocked: true,
                    isExtraArenaBox: false));
            }

            // Prefer authored names from catalog when present.
            IReadOnlyList<ExtraArenaEntry> box = BoxEntries(profile);
            for (int i = 0; i < box.Count; i++)
            {
                ExtraArenaEntry extra = box[i];
                string label = extra.Name;
                if (catalog != null && catalog.TryGetStage(extra.StageId, out StageRecord authored))
                {
                    label = authored.Name;
                }

                list.Add(new StageCursorEntry(
                    extra.StageId,
                    label,
                    unlocked: extra.Unlocked,
                    isExtraArenaBox: true));
            }

            return list;
        }
    }
}
