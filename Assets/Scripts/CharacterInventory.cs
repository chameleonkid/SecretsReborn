using System;
using UnityEngine;

namespace SecretsReborn
{
    [RequireComponent(typeof(CharacterAppearance), typeof(CharacterDeath))]
    public sealed class CharacterInventory : MonoBehaviour
    {
        [SerializeField] private string characterId = "solo-player";
        [SerializeField] private ItemDefinition[] catalog;
        [SerializeField] private ClothingAppearance baseClothing;
        // Solo authority adapter. A network session must set this on the host only
        // and validate sender ownership before forwarding requests here.
        [SerializeField] private bool hasStateAuthority = true;
        public string CharacterId => characterId;
        public InventoryState State { get; private set; }
        public bool HasStateAuthority => hasStateAuthority;
        public event Action Changed;
        public void Configure(ItemDefinition[] definitions, ClothingAppearance fallback)
        { catalog = definitions; baseClothing = fallback; }
        private void Awake() => State = GameSession.Instance.World.CharacterInventory(characterId);
        public void RefreshSession()
        { State = GameSession.Instance.World.CharacterInventory(characterId); GetComponent<CharacterDeath>()?.RefreshState(); ApplyAppearance(); Changed?.Invoke(); }
        private void Start() { ApplyAppearance(); GameSession.Instance.RegisterSpawn(this); }
        public ItemDefinition Find(string id) => catalog == null ? null : Array.Find(catalog, item => item != null && item.ItemId == id);
        public bool TryReceive(ItemDefinition item, int count)
        {
            if (!hasStateAuthority || item == null || Find(item.ItemId) != item) return false;
            if (!State.TryAdd(item.ItemId, count, item.MaxStack)) return false;
            Changed?.Invoke(); return true;
        }
        private ItemRules Rules(string id) => Find(id)?.Rules ?? default;
        public bool TryReceiveBatch(InventoryStack[] rewards)
        {
            if (!hasStateAuthority || !State.TryAddBatch(rewards, Rules)) return false;
            Changed?.Invoke(); return true;
        }
        public bool TryUseItem(int index)
        {
            if (!hasStateAuthority || GameSession.Instance.Busy || GameSession.Instance.World.CharacterVitals(characterId).IsDown) return false;
            var item = Find(State.GetSlot(index)?.itemId); if (item == null) return false;
            bool applied = item.Purpose == ItemPurpose.HealthPotion ? GameSession.Instance.ApplyHealing(this, item.UseAmount)
                : item.Purpose == ItemPurpose.ManaPotion && GameSession.Instance.RestoreMana(this, item.UseAmount);
            if (!applied) return false;
            State.TryConsume(index); Changed?.Invoke(); return true;
        }
        public int TotalArmor
        {
            get
            {
                long total = 0;
                foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot))) total += Find(State.GetEquipment(slot))?.ArmorValue ?? 0;
                return (int)Math.Min(total, 1000000);
            }
        }
        public bool TryMove(int from, int to)
        {
            if (!hasStateAuthority || !State.TryMove(from, to, Rules)) return false;
            Changed?.Invoke(); return true;
        }
        public bool TryEquip(int from, EquipmentSlot target)
        {
            if (!hasStateAuthority || !State.TryEquip(from, target, Rules)) return false;
            ApplyAppearance(); Changed?.Invoke(); return true;
        }
        public bool TryUnequip(EquipmentSlot slot, int destination = -1)
        {
            if (!hasStateAuthority || !State.TryUnequip(slot, Rules, destination)) return false;
            ApplyAppearance(); Changed?.Invoke(); return true;
        }
        public EquipmentSlot? PreferredSlot(int index)
        {
            var item = Find(State.GetSlot(index)?.itemId);
            if (item == null) return null;
            if (item.Rules.kind == ItemKind.Ring && State.GetEquipment(EquipmentSlot.Ring1) != null && State.GetEquipment(EquipmentSlot.Ring2) == null)
                return EquipmentSlot.Ring2;
            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot))) if (item.Rules.Fits(slot)) return slot;
            return null;
        }
        private void ApplyAppearance()
        {
            var appearance = GetComponent<CharacterAppearance>();
            appearance.Equip(Find(State.EquippedArmorId)?.ArmorAppearance ?? baseClothing);
            appearance.EquipAccessories(Find(State.GetEquipment(EquipmentSlot.Head))?.ArmorAppearance,
                Find(State.GetEquipment(EquipmentSlot.Feet))?.ArmorAppearance);
        }
    }
}
