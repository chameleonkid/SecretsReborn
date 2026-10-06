using UnityEditor;
using UnityEngine;

namespace SecretsReborn.Editor
{
    public static class CharacterVitalsTools
    {
        private static CharacterInventory Actor()
        {
            if (!EditorApplication.isPlaying) return null;
            var follow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
            return follow != null && follow.Target != null ? follow.Target.GetComponent<CharacterInventory>() : null;
        }
        [MenuItem("SecretsReborn/Character/Test/Take half-heart damage")]
        private static void Damage() => GameSession.Instance.ApplyDamage(Actor(), 1);
        [MenuItem("SecretsReborn/Character/Test/Heal one heart")]
        private static void Heal() => GameSession.Instance.ApplyHealing(Actor(), 2);
        [MenuItem("SecretsReborn/Character/Test/Add heart container")]
        private static void Container() => GameSession.Instance.AddHeartContainer(Actor());
        [MenuItem("SecretsReborn/Character/Test/Spend 10 mana")]
        private static void Spend() => GameSession.Instance.TrySpendMana(Actor(), 10);
        [MenuItem("SecretsReborn/Character/Test/Restore 10 mana")]
        private static void Restore() => GameSession.Instance.RestoreMana(Actor(), 10);
        [MenuItem("SecretsReborn/Character/Test/Take half-heart damage", true)]
        [MenuItem("SecretsReborn/Character/Test/Heal one heart", true)]
        [MenuItem("SecretsReborn/Character/Test/Add heart container", true)]
        [MenuItem("SecretsReborn/Character/Test/Spend 10 mana", true)]
        [MenuItem("SecretsReborn/Character/Test/Restore 10 mana", true)]
        private static bool Validate() => Actor() != null;
        [MenuItem("SecretsReborn/Character/Test/Equip training sword")]
        private static void EquipSword()
        { EquipTestWeapon("training-sword"); }
        [MenuItem("SecretsReborn/Character/Test/Equip red training sword")]
        private static void EquipRedSword() => EquipTestWeapon("red-training-sword");
        [MenuItem("SecretsReborn/Character/Test/Equip red training sword", true)]
        private static bool ValidateRedSword() => Actor() != null;
        private static void EquipTestWeapon(string itemId)
        {
            var actor = Actor(); if (actor == null) return;
            var item = actor.Find(itemId);
            if (item == null) { Debug.LogError("Übungsschwert fehlt im Charakterkatalog."); return; }
            if (actor.State.GetEquipment(EquipmentSlot.MainHand) == item.ItemId) return;
            int slot = -1;
            for (int i = 0; i < InventoryState.Capacity; i++) if (actor.State.GetSlot(i)?.itemId == item.ItemId) { slot = i; break; }
            if (slot < 0 && actor.TryReceive(item, 1))
                for (int i = 0; i < InventoryState.Capacity; i++) if (actor.State.GetSlot(i)?.itemId == item.ItemId) { slot = i; break; }
            if (slot < 0 || !actor.TryEquip(slot, EquipmentSlot.MainHand)) Debug.LogWarning("Schwert konnte nicht angelegt werden: Inventarplätze prüfen.");
        }
        [MenuItem("SecretsReborn/Character/Test/Equip training sword", true)]
        private static bool ValidateSword() => Actor() != null;
    }
}
