using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SecretsReborn.Editor
{
    [InitializeOnLoad]
    public static class SaveGameIntegrationCheck
    {
        private const string Request = "Temp/CheckSaveGame.request";
        static SaveGameIntegrationCheck() { EditorApplication.update += Requested; }
        private static void Requested()
        {
            if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            File.Delete(Request);
            try { Check(); } catch (Exception error) { Debug.LogException(error); File.WriteAllText("Temp/SaveGameCheckReport.txt", "FAIL: " + error); }
        }
        [MenuItem("SecretsReborn/Session/Verify Easy Save roundtrip")]
        public static void Check()
        {
            string path = Path.GetFullPath("Temp/SaveGameCheck-" + Guid.NewGuid().ToString("N") + ".es3");
            try
            {
                var world = new WorldSessionState("test-world");
                var character = world.CharacterInventory("test-player");
                character.TryAdd("test-armor", 1, 1);
                character.TryEquip(0, EquipmentSlot.Armor, id => new ItemRules { kind = ItemKind.Armor, maxStack = 1 });
                character.TryAdd("test-ring", 2, 1);
                character.TryAdd("test-lamp", 1, 1);
                character.TryEquip(2, EquipmentSlot.Lamp, id => new ItemRules { kind = ItemKind.Lamp, maxStack = 1 });
                character.TryAddGold(125);
                character.TryAdd("test-health-potion", 3, 10);
                character.BindPotion(0, "test-health-potion", id => new ItemRules { potionKind = 1, maxStack = 10 });
                world.TryCollect("test-pickup", () => true);
                world.TryCollect("chest:test-chest", () => true);
                world.TryCollect("loot:test-oak:0", () => true);
                world.DefeatEnemy("test-oak");
                world.SharedStash.TryAdd("test-stash-armor", 1, 1);
                world.Puzzle("test-puzzle").Enter(0);
                world.SetPosition("test-player", "Assets/Scenes/Waldheiligtum-Editable.unity", 2.5f, -4, 0);
                world.SetSavedScene("Assets/Scenes/Waldheiligtum-Editable.unity");
                world.AdvancePlayTime(3723.5);
                world.CharacterVitals("test-player").Damage(1);
                world.CharacterVitals("test-player").SpendMana(20);
                SaveGameStore.Save(world, path);
                var loaded = SaveGameStore.Load(path);
                if (loaded.WorldId != "test-world" || loaded.CharacterInventory("test-player").EquippedArmorId != "test-armor"
                    || loaded.CharacterInventory("test-player").GetEquipment(EquipmentSlot.Lamp) != "test-lamp"
                    || loaded.CharacterInventory("test-player").Gold != 125
                    || loaded.CharacterInventory("test-player").PotionItem(0) != "test-health-potion"
                    || loaded.CharacterInventory("test-player").Count("test-health-potion") != 3
                    || loaded.CharacterInventory("test-player").GetSlot(1).itemId != "test-ring"
                    || !loaded.IsCollected("test-pickup") || !loaded.IsCollected("chest:test-chest") || !loaded.IsCollected("loot:test-oak:0")
                    || !loaded.IsEnemyDefeated("test-oak") || loaded.Puzzle("test-puzzle").Progress != 1
                    || loaded.SharedStash.GetSlot(0)?.itemId != "test-stash-armor"
                    || loaded.Position("test-player").scenePath != "Assets/Scenes/Waldheiligtum-Editable.unity"
                    || loaded.Position("test-player").x != 2.5f || loaded.Position("test-player").y != -4
                    || loaded.PlayTimeSeconds != 3723.5 || loaded.SavedScenePath != "Assets/Scenes/Waldheiligtum-Editable.unity"
                    || loaded.CharacterVitals("test-player").Health != 5 || loaded.CharacterVitals("test-player").Mana != 30)
                    throw new Exception("Roundtrip state mismatch.");
                world.Puzzle("test-puzzle").Enter(1); world.CharacterVitals("test-player").Damage(1); SaveGameStore.Save(world, path);
                if (!SaveGameStore.Exists(path + ".bac") || SaveGameStore.Load(path + ".bac").Puzzle("test-puzzle").Progress != 1
                    || SaveGameStore.Load(path + ".bac").CharacterVitals("test-player").Health != 5)
                    throw new Exception("Backup mismatch.");
                var invalid = world.Capture(); invalid.version = 999;
                bool rejected = false; try { WorldSessionState.Restore(invalid); } catch (ArgumentException) { rejected = true; }
                if (!rejected) throw new Exception("Unknown version accepted.");
                var legacy = world.Capture(); legacy.version = 3;
                foreach (var entry in legacy.characters) entry.vitals = null;
                ES3.Save("world-session", legacy, new ES3Settings(path + ".legacy", ES3.Location.File));
                if (SaveGameStore.Load(path + ".legacy").CharacterVitals("test-player").Health != 6)
                    throw new Exception("Legacy vitals migration mismatch.");
                legacy.version = 4;
                foreach (var entry in legacy.characters) entry.vitals = new VitalsSaveData { health = 75, maxHealth = 100, mana = 30, maxMana = 50 };
                ES3.Save("world-session", legacy, new ES3Settings(path + ".legacy", ES3.Location.File));
                var oldVitals = SaveGameStore.Load(path + ".legacy").CharacterVitals("test-player");
                if (oldVitals.Health != 5 || oldVitals.HeartContainers != 3 || oldVitals.Mana != 30) throw new Exception("V4 heart migration mismatch.");
                legacy = world.Capture(); legacy.version = 6;
                foreach (var entry in legacy.characters) Array.Resize(ref entry.equipment, 14);
                ES3.Save("world-session", legacy, new ES3Settings(path + ".legacy", ES3.Location.File));
                var migratedSlots = SaveGameStore.Load(path + ".legacy").CharacterInventory("test-player");
                if (migratedSlots.GetEquipment(EquipmentSlot.Lamp) != null || migratedSlots.EquippedArmorId != "test-armor")
                    throw new Exception("V6 equipment migration mismatch.");
                world.CharacterVitals("test-player").AddHeartContainer(); SaveGameStore.Save(world, path);
                if (SaveGameStore.Load(path).CharacterVitals("test-player").HeartContainers != 4) throw new Exception("Heart container roundtrip mismatch.");
                File.WriteAllText("Temp/SaveGameCheckReport.txt", "PASS: v15 shared stash, gold/potion references, lamp slot, hearts/mana and heart containers roundtrip; v3/v4/v6 migration, backup, position, inventory and puzzles. Separate test save.");
                Debug.Log("PASS: Herzen, Easy-Save-Roundtrip, Migration und Backup geprüft.");
            }
            finally
            {
                foreach (string file in new[] { path, path + ".bac", path + ".pending", path + ".legacy" }) if (File.Exists(file)) File.Delete(file);
            }
        }
    }
}
