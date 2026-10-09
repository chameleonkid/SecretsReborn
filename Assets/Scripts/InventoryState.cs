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
        public bool currency;
        public int currencyUnits;
        // 0 = none, 1 = health, 2 = mana; quick slots never contain stacks.
        public int potionKind;
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
        public const int MaximumGold = 99999999;
        public int Gold { get; private set; }
        private string[] potionItems = new string[2];
        public bool NeedsEconomyMigration { get; private set; } = true;
        public double PotionReadyAt { get; set; }
        public string PotionItem(int slot) => slot >= 0 && slot < 2 ? potionItems[slot] : null;
        public int Count(string id)
        {
            long count = 0;
            if (id != null) foreach (var stack in bag) if (stack != null && stack.itemId == id) count += stack.count;
            return (int)Math.Min(int.MaxValue, count);
        }
        public int FirstSlot(string id) => id == null ? -1 : Array.FindIndex(bag, s => s != null && s.itemId == id);
        public bool BindPotion(int slot, string id, Func<string, ItemRules> rules)
        {
            if (slot < 0 || slot > 1 || id != null && (FirstSlot(id) < 0 || rules(id).potionKind != slot + 1)) return false;
            potionItems[slot] = id; return true;
        }
        public bool TryAddGold(long amount)
        {
            if (amount <= 0 || amount > MaximumGold - Gold) return false;
            Gold += (int)amount; return true;
        }
        public bool MigrateCurrency(string id, int units)
        {
            long amount = (long)Count(id) * units;
            if (amount == 0 || !TryAddGold(amount)) return false;
            for (int i = 0; i < bag.Length; i++) if (bag[i]?.itemId == id) bag[i] = null;
            return true;
        }
        public void CompleteEconomyMigration() => NeedsEconomyMigration = false;
        public InventoryStack GetSlot(int index) => index >= 0 && index < Capacity && bag[index] != null
            ? new InventoryStack { itemId = bag[index].itemId, count = bag[index].count } : null;
        public string GetEquipment(EquipmentSlot slot) => (int)slot >= 0 && (int)slot < equipment.Length ? equipment[(int)slot] : null;
        public string EquippedArmorId => GetEquipment(EquipmentSlot.Armor);
        public CharacterSaveData Capture(string characterId) => new CharacterSaveData
        { characterId = characterId, bag = Clone(bag), equipment = (string[])equipment.Clone(), gold = Gold, potionItems = (string[])potionItems.Clone() };
        public static InventoryState Restore(CharacterSaveData data, bool allowLegacyEquipment = false, bool legacyEconomy = false)
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
            if (!legacyEconomy && (data.gold < 0 || data.gold > MaximumGold || data.potionItems == null || data.potionItems.Length != 2))
                throw new ArgumentException("Invalid gold or potion shortcuts.");
            if (!legacyEconomy) foreach (var id in data.potionItems)
                if (id != null && string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Invalid potion shortcut.");
            return new InventoryState { bag = Clone(data.bag), equipment = restoredEquipment,
                Gold = legacyEconomy ? 0 : data.gold, potionItems = legacyEconomy ? new string[2] : (string[])data.potionItems.Clone(),
                NeedsEconomyMigration = legacyEconomy };
        }
        public bool TryAdd(string id, int count, int maxStack, int capacity = Capacity)
        {
            var next = Clone(bag);
            if (!Add(next, id, count, maxStack, Math.Min(capacity, Capacity))) return false;
            bag = next; return true;
        }
        // Two containers commit together. A stale request cannot take a new item
        // which has since occupied the same slot. Whole stacks only, no swapping.
        public bool TryTransferTo(InventoryState destination, int from, int to, string expectedItem, int expectedCount, Func<string, ItemRules> rules)
        {
            if (destination == null || destination == this || !Valid(from) || !Valid(to) || rules == null) return false;
            var source = bag[from];
            if (source == null || source.itemId != expectedItem || source.count != expectedCount) return false;
            var item = rules(source.itemId); var target = destination.bag[to];
            if (item.currency || item.maxStack < source.count || target != null && (target.itemId != source.itemId || (long)target.count + source.count > item.maxStack)) return false;
            var nextSource = Clone(bag); var nextTarget = Clone(destination.bag);
            nextTarget[to] = new InventoryStack { itemId = source.itemId, count = source.count + (target?.count ?? 0) }; nextSource[from] = null;
            bag = nextSource; destination.bag = nextTarget; return true;
        }
        public bool TryAddBatch(InventoryStack[] rewards, Func<string, ItemRules> rules)
        {
            if (rewards == null || rewards.Length == 0 || rules == null) return false;
            var next = Clone(bag);
            long nextGold = Gold;
            foreach (var reward in rewards)
            {
                if (reward == null || string.IsNullOrWhiteSpace(reward.itemId) || reward.count <= 0) return false;
                var item = rules(reward.itemId);
                if (item.currency)
                {
                    if (item.currencyUnits <= 0) return false;
                    nextGold += (long)reward.count * item.currencyUnits;
                    if (nextGold > MaximumGold) return false;
                }
                else if (!Add(next, reward.itemId, reward.count, item.maxStack, Capacity)) return false;
            }
            bag = next; Gold = (int)nextGold; return true;
        }
        public bool TryConsume(int index)
        {
            if (!Valid(index) || bag[index] == null) return false;
            bag[index].count--; if (bag[index].count == 0) bag[index] = null;
            return true;
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
