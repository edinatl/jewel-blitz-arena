using System;
using System.Collections.Generic;

namespace JewelBlitzArena.Data
{
    /// <summary>
    /// Loaded content tables. Sim/Meta/Tests consume this — never ScriptableObjects.
    /// </summary>
    public sealed class ContentCatalog
    {
        private readonly Dictionary<int, FighterKitRecord> _fighters;
        private readonly Dictionary<int, StageRecord> _stages;
        private readonly Dictionary<int, ItemRecord> _items;

        public ContentCatalog(FighterKitRecord[] fighters, StageRecord[] stages, ItemRecord[] items)
            : this(fighters, stages, items, ComputeHash(fighters, stages, items))
        {
        }

        public ContentCatalog(
            FighterKitRecord[] fighters,
            StageRecord[] stages,
            ItemRecord[] items,
            ulong contentHash)
        {
            Fighters = fighters ?? Array.Empty<FighterKitRecord>();
            Stages = stages ?? Array.Empty<StageRecord>();
            Items = items ?? Array.Empty<ItemRecord>();
            ContentHash = contentHash;

            _fighters = new Dictionary<int, FighterKitRecord>(Fighters.Length);
            foreach (FighterKitRecord f in Fighters)
            {
                _fighters[f.FighterId] = f;
            }

            _stages = new Dictionary<int, StageRecord>(Stages.Length);
            foreach (StageRecord s in Stages)
            {
                _stages[s.StageId] = s;
            }

            _items = new Dictionary<int, ItemRecord>(Items.Length);
            foreach (ItemRecord i in Items)
            {
                _items[i.ItemId] = i;
            }
        }

        public FighterKitRecord[] Fighters { get; }
        public StageRecord[] Stages { get; }
        public ItemRecord[] Items { get; }

        /// <summary>Stable content hash for lobby handshake / CI golden checks.</summary>
        public ulong ContentHash { get; }

        public uint SchemaVersion => ContentSchema.Version;

        public bool TryGetFighter(int fighterId, out FighterKitRecord kit) =>
            _fighters.TryGetValue(fighterId, out kit!);

        public bool TryGetStage(int stageId, out StageRecord stage) =>
            _stages.TryGetValue(stageId, out stage!);

        public bool TryGetItem(int itemId, out ItemRecord item) =>
            _items.TryGetValue(itemId, out item!);

        public IReadOnlyList<ItemRecord> AlwaysEligibleItems()
        {
            var list = new List<ItemRecord>();
            foreach (ItemRecord item in Items)
            {
                if (item.CrateEligibility == CrateEligibility.Always)
                {
                    list.Add(item);
                }
            }

            return list;
        }

        public static ulong ComputeHash(
            FighterKitRecord[] fighters,
            StageRecord[] stages,
            ItemRecord[] items)
        {
            byte[] payload = ContentBlobWriter.WritePayload(fighters, stages, items);
            return ContentXxHash64.Hash(payload);
        }
    }
}
