using System;

namespace SecretsReborn
{
    public enum EquipmentSlot { Head, Shoulders, Armor, Waist, Hands, Legs, Feet, Ring1, Ring2, Amulet, Seal, Cloak, MainHand, OffHand, Lamp }
    public enum ItemKind { None, Head, Shoulders, Armor, Waist, Hands, Legs, Feet, Ring, Amulet, Seal, Cloak, Weapon, Shield, Lamp }
    public struct ItemRules
    {
        public ItemKind kind;
        public bool twoHanded;
        public int maxStack;
        public bool Fits(EquipmentSlot slot)
        {
            switch (kind)
            {
                case ItemKind.Head: return slot == EquipmentSlot.Head;
                case ItemKind.Shoulders: return slot == EquipmentSlot.Shoulders;
                case ItemKind.Armor: return slot == EquipmentSlot.Armor;
                case ItemKind.Waist: return slot == EquipmentSlot.Waist;
                case ItemKind.Hands: return slot == EquipmentSlot.Hands;
                case ItemKind.Legs: return slot == EquipmentSlot.Legs;
                case ItemKind.Feet: return slot == EquipmentSlot.Feet;
                case ItemKind.Ring: return slot == EquipmentSlot.Ring1 || slot == EquipmentSlot.Ring2;
                case ItemKind.Amulet: return slot == EquipmentSlot.Amulet;
                case ItemKind.Seal: return slot == EquipmentSlot.Seal;
                case ItemKind.Cloak: return slot == EquipmentSlot.Cloak;
                case ItemKind.Weapon: return slot == EquipmentSlot.MainHand;
                case ItemKind.Shield: return slot == EquipmentSlot.OffHand;
                case ItemKind.Lamp: return slot == EquipmentSlot.Lamp;
                default: return false;
            }
        }
    }
    [Serializable]
    public sealed class InventoryStack
    {
        public string itemId;
        public int count;
    }
    [Serializable]
    public sealed class InventoryState
    {
        public const int Capacity = 40;
        private InventoryStack[] bag = new InventoryStack[Capacity];
        public const int EquipmentCapacity = 15;
        private string[] equipment = new string[EquipmentCapacity];
        public InventoryStack GetSlot(int index) => index >= 0 && index < Capacity && bag[index] != null
            ? new InventoryStack { itemId = bag[index].itemId, count = bag[index].count } : null;
        public string GetEquipment(EquipmentSlot slot) => (int)slot >= 0 && (int)slot < equipment.Length ? equipment[(int)slot] : null;
        public string EquippedArmorId => GetEquipment(EquipmentSlot.Armor);
        public CharacterSaveData Capture(string characterId) => new CharacterSaveData
        { characterId = characterId, bag = Clone(bag), equipment = (string[])equipment.Clone() };
        public static InventoryState Restore(CharacterSaveData data, bool allowLegacyEquipment = false)
        {
            if (data == null || data.bag == null || data.bag.Length != Capacity || data.equipment == null
                || data.equipment.Length != EquipmentCapacity && !(allowLegacyEquipment && data.equipment.Length == 14))
                throw new ArgumentException("Invalid inventory dimensions.");
            foreach (var stack in data.bag)
                if (stack != null && (string.IsNullOrWhiteSpace(stack.itemId) || stack.count <= 0)) throw new ArgumentException("Invalid item stack.");
            foreach (var id in data.equipment)
                if (id != null && string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Invalid equipment ID.");
            var restoredEquipment = new string[EquipmentCapacity];
            Array.Copy(data.equipment, restoredEquipment, data.equipment.Length);
            return new InventoryState { bag = Clone(data.bag), equipment = restoredEquipment };
        }
        public bool TryAdd(string id, int count, int maxStack, int capacity = Capacity)
        {
            var next = Clone(bag);
            if (!Add(next, id, count, maxStack, Math.Min(capacity, Capacity))) return false;
            bag = next; return true;
        }
        public bool TryMove(int from, int to, Func<string, ItemRules> rules)
        {
            if (!Valid(from) || !Valid(to) || from == to || bag[from] == null) return false;
            var next = Clone(bag);
            if (next[to] != null && next[to].itemId == next[from].itemId)
            {
                int amount = Math.Min(next[from].count, Math.Max(0, rules(next[from].itemId).maxStack - next[to].count));
                if (amount == 0) return false;
                next[to].count += amount; next[from].count -= amount;
                if (next[from].count == 0) next[from] = null;
            }
            else { var temp = next[to]; next[to] = next[from]; next[from] = temp; }
            bag = next; return true;
        }
        public bool TryEquip(int from, EquipmentSlot target, Func<string, ItemRules> rules)
        {
            if (!Valid(from) || bag[from] == null) return false;
            string id = bag[from].itemId;
            var rule = rules(id);
            if (!rule.Fits(target)) return false;
            var next = Clone(bag); var worn = (string[])equipment.Clone();
            next[from].count--; if (next[from].count == 0) next[from] = null;
            bool Return(EquipmentSlot slot)
            {
                string old = worn[(int)slot];
                if (old == null) return true;
                if (!Add(next, old, 1, rules(old).maxStack, Capacity)) return false;
                worn[(int)slot] = null; return true;
            }
            if (!Return(target)) return false;
            if (target == EquipmentSlot.MainHand && rule.twoHanded && !Return(EquipmentSlot.OffHand)) return false;
            if (target == EquipmentSlot.OffHand && worn[(int)EquipmentSlot.MainHand] != null
                && rules(worn[(int)EquipmentSlot.MainHand]).twoHanded && !Return(EquipmentSlot.MainHand)) return false;
            worn[(int)target] = id; bag = next; equipment = worn; return true;
        }
        public bool TryUnequip(EquipmentSlot slot, Func<string, ItemRules> rules, int destination = -1)
        {
            string id = GetEquipment(slot);
            if (id == null) return false;
            var next = Clone(bag);
            if (destination >= 0)
            {
                if (!Valid(destination) || next[destination] != null) return false;
                next[destination] = new InventoryStack { itemId = id, count = 1 };
            }
            else if (!Add(next, id, 1, rules(id).maxStack, Capacity)) return false;
            bag = next; equipment[(int)slot] = null; return true;
        }
        private static bool Valid(int index) => index >= 0 && index < Capacity;
        private static InventoryStack[] Clone(InventoryStack[] source)
        {
            var result = new InventoryStack[source.Length];
            for (int i = 0; i < source.Length; i++) if (source[i] != null)
                result[i] = new InventoryStack { itemId = source[i].itemId, count = source[i].count };
            return result;
        }
        private static bool Add(InventoryStack[] target, string id, int count, int max, int capacity)
        {
            if (string.IsNullOrWhiteSpace(id) || count <= 0 || max <= 0 || capacity <= 0) return false;
            long space = 0;
            for (int i = 0; i < capacity; i++) space += target[i] == null ? max : target[i].itemId == id ? Math.Max(0, max - target[i].count) : 0;
            if (space < count) return false;
            for (int i = 0; i < capacity && count > 0; i++)
            {
                var stack = target[i];
                if (stack == null || stack.itemId != id) continue;
                int add = Math.Min(count, Math.Max(0, max - stack.count)); stack.count += add; count -= add;
            }
            for (int i = 0; i < capacity && count > 0; i++) if (target[i] == null)
            { int add = Math.Min(count, max); target[i] = new InventoryStack { itemId = id, count = add }; count -= add; }
            return true;
        }
    }
}
