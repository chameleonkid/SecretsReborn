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
        public bool LocalInput { get; private set; } = true;
        public void ConfigureNetwork(string id, bool authority, bool input)
        {
            characterId = id; hasStateAuthority = authority; LocalInput = input;
            GetComponent<InventoryInteraction>()?.SetLocalInput(input);
            GetComponent<PlayerMelee>()?.SetLocalInput(input);
            GetComponent<PlayerLantern>()?.SetLocalInput(input);
            RefreshSession();
        }
        public event Action Changed;
        internal void NotifyInventoryChanged() => Changed?.Invoke();
        public void Configure(ItemDefinition[] definitions, ClothingAppearance fallback)
        { catalog = definitions; baseClothing = fallback; }
        private void Awake() => State = NetworkCoop.Running && NetworkCoop.Active.ChangingArea
            ? new InventoryState() : GameSession.Instance.World.CharacterInventory(characterId);
        public void RefreshSession()
        { State = GameSession.Instance.World.CharacterInventory(characterId); MigrateEconomy(); SyncEquipmentVitals(); GetComponent<CharacterDeath>()?.RefreshState(); ApplyAppearance(); Changed?.Invoke(); }
        private void MigrateEconomy()
        {
            if (!hasStateAuthority || catalog == null) return;
            foreach (var item in catalog) if (item != null)
            {
                if (item.Purpose == ItemPurpose.Gold) State.MigrateCurrency(item.ItemId,item.UseAmount);
                if (State.NeedsEconomyMigration) AutoBindPotion(item);
            }
            State.CompleteEconomyMigration();
        }
        private void AutoBindPotion(ItemDefinition item)
        {
            int slot = item.Rules.potionKind - 1;
            if (slot >= 0 && State.PotionItem(slot) == null) State.BindPotion(slot,item.ItemId,Rules);
        }
        private void Start() { MigrateEconomy(); ApplyAppearance(); GameSession.Instance.RegisterSpawn(this); }
        public ItemDefinition Find(string id) => catalog == null ? null : Array.Find(catalog, item => item != null && item.ItemId == id);
        public bool TryReceive(ItemDefinition item, int count)
        {
            if (!hasStateAuthority || item == null || Find(item.ItemId) != item) return false;
            if (count <= 0) return false;
            if (item.Purpose == ItemPurpose.Gold ? !State.TryAddGold((long)count * item.UseAmount) : !State.TryAdd(item.ItemId, count, item.MaxStack)) return false;
            AutoBindPotion(item);
            Changed?.Invoke(); return true;
        }
        private ItemRules Rules(string id) => Find(id)?.Rules ?? default;
        public bool TryReceiveBatch(InventoryStack[] rewards)
        {
            if (!hasStateAuthority || !State.TryAddBatch(rewards, Rules)) return false;
            foreach (var reward in rewards) if (Find(reward.itemId) is ItemDefinition item) AutoBindPotion(item);
            Changed?.Invoke(); return true;
        }
        public bool TryUseItem(int index)
        {
            if (NetworkCoop.Request(this, CoopAction.UseItem, index)) return true;
            if (!hasStateAuthority || SaveBook.IsOpen || !GameSession.Instance.CanChangeVitals(this) || GameSession.Instance.World.CharacterVitals(characterId).IsDown || PotionCooldownRemaining > 0) return false;
            var item = Find(State.GetSlot(index)?.itemId); if (item == null) return false;
            bool applied = item.Purpose == ItemPurpose.HealthPotion ? GameSession.Instance.ApplyHealing(this, item.UseAmount)
                : item.Purpose == ItemPurpose.ManaPotion && GameSession.Instance.RestoreMana(this, item.UseAmount);
            if (!applied) return false;
            State.TryConsume(index); State.PotionReadyAt = Time.timeAsDouble + item.UseCooldown; Changed?.Invoke(); return true;
        }
        private double replicaPotionReadyAt;
        public float PotionCooldownRemaining => (float)Math.Max(0, (hasStateAuthority ? State.PotionReadyAt : replicaPotionReadyAt) - Time.timeAsDouble);
        internal void SetReplicaPotionCooldown(float remaining) => replicaPotionReadyAt = Time.timeAsDouble + Mathf.Clamp(remaining,0,30);
        public bool TryBindPotion(int slot, int bagIndex)
        {
            if (NetworkCoop.Request(this,CoopAction.BindPotion,bagIndex,slot)) return true;
            if (!hasStateAuthority || GameSession.Instance.Busy || slot < 0 || slot > 1 || bagIndex < -1 || bagIndex >= InventoryState.Capacity) return false;
            string id = bagIndex == -1 ? null : State.GetSlot(bagIndex)?.itemId;
            if (bagIndex >= 0 && id == null || !State.BindPotion(slot,id,Rules)) return false;
            Changed?.Invoke(); return true;
        }
        public bool TryUsePotion(int slot)
        {
            if (slot < 0 || slot > 1) return false;
            if (NetworkCoop.Request(this,CoopAction.UsePotion,slot)) return true;
            var id = State.PotionItem(slot);
            if (id == null || Find(id)?.Rules.potionKind != slot + 1) return false;
            return TryUseItem(State.FirstSlot(id));
        }
        public CharacterStats Stats
        {
            get
            {
                var stats=new CharacterStats { WeaponDamage=1,AttackCooldown=.45f };
                if (State==null) return stats;
                foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
                {
                    var item=Find(State.GetEquipment(slot)); if (item==null) continue;
                    stats.AddEquipment(item.ArmorValue,item.BonusHalfHearts,item.BonusMana);
                    if (slot==EquipmentSlot.MainHand && item.Weapon!=null) { stats.WeaponDamage=item.Weapon.Damage; stats.AttackCooldown=item.Weapon.Cooldown; }
                }
                return stats;
            }
        }
        public int TotalArmor => Stats.Armor;
        private void SyncEquipmentVitals()
        {
            if (!hasStateAuthority) return;
            var stats=Stats; GameSession.Instance.World.CharacterVitals(characterId).SetEquipmentBonuses(stats.BonusHalfHearts,stats.BonusMana);
        }
        public bool TryMove(int from, int to)
        {
            if (NetworkCoop.Request(this, CoopAction.MoveItem, from, to)) return true;
            if (!hasStateAuthority || GameSession.Instance.Busy || !State.TryMove(from, to, Rules)) return false;
            Changed?.Invoke(); return true;
        }
        public bool TryEquip(int from, EquipmentSlot target)
        {
            if (NetworkCoop.Request(this, CoopAction.Equip, from, (int)target)) return true;
            if (!hasStateAuthority || GameSession.Instance.Busy || !State.TryEquip(from, target, Rules)) return false;
            ApplyAppearance(); Changed?.Invoke(); return true;
        }
        public bool TryUnequip(EquipmentSlot slot, int destination = -1)
        {
            if (NetworkCoop.Request(this, CoopAction.Unequip, (int)slot, destination)) return true;
            if (!hasStateAuthority || GameSession.Instance.Busy || !State.TryUnequip(slot, Rules, destination)) return false;
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
            SyncEquipmentVitals();
            var appearance = GetComponent<CharacterAppearance>();
            appearance.ApplyProfile(GameSession.Instance.World.CharacterProfile(characterId));
            appearance.Equip(Find(State.EquippedArmorId)?.ArmorAppearance ?? baseClothing);
            appearance.EquipAccessories(Find(State.GetEquipment(EquipmentSlot.Head))?.ArmorAppearance,
                Find(State.GetEquipment(EquipmentSlot.Feet))?.ArmorAppearance);
        }
    }
}
