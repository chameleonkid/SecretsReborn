using System;
using UnityEngine;

namespace SecretsReborn
{
    public enum LootSourceKind { NormalEnemy, Boss, Chest }
    [Serializable] public sealed class LootEntry
    {
        public ItemDefinition item;
        [Min(1)] public int count = 1;
    }
    [CreateAssetMenu(menuName = "SecretsReborn/Loot table")]
    public sealed class LootTable : ScriptableObject
    {
        [SerializeField] private LootSourceKind source;
        [SerializeField] private LootEntry[] entries;
        public LootSourceKind Source => source;
        public LootEntry[] Entries => entries == null ? Array.Empty<LootEntry>() : (LootEntry[])entries.Clone();
        public bool IsValid
        {
            get
            {
                if (entries == null || entries.Length == 0) return false;
                foreach (var entry in entries)
                    if (entry == null || entry.item == null || entry.count <= 0
                        || source == LootSourceKind.NormalEnemy && (entry.item.Rules.kind != ItemKind.None || entry.item.Purpose == ItemPurpose.Equipment)) return false;
                return true;
            }
        }
        public InventoryStack[] Rewards()
        {
            if (!IsValid) return null;
            var result = new InventoryStack[entries.Length];
            for (int i = 0; i < entries.Length; i++) result[i] = new InventoryStack { itemId = entries[i].item.ItemId, count = entries[i].count };
            return result;
        }
        public void Configure(LootSourceKind kind, LootEntry[] rewards) { source = kind; entries = rewards; }
    }
}
