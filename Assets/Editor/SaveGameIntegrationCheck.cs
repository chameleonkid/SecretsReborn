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
                world.TryCollect("test-pickup", () => true);
                world.Puzzle("test-puzzle").Enter(0);
                world.SetPosition("test-player", "Assets/Scenes/Waldheiligtum-Editable.unity", 2.5f, -4, 0);
                world.SetSavedScene("Assets/Scenes/Waldheiligtum-Editable.unity");
                world.AdvancePlayTime(3723.5);
                world.CharacterVitals("test-player").Damage(25);
                world.CharacterVitals("test-player").SpendMana(20);
                SaveGameStore.Save(world, path);
                var loaded = SaveGameStore.Load(path);
                if (loaded.WorldId != "test-world" || loaded.CharacterInventory("test-player").EquippedArmorId != "test-armor"
                    || loaded.CharacterInventory("test-player").GetSlot(1).itemId != "test-ring"
                    || !loaded.IsCollected("test-pickup") || loaded.Puzzle("test-puzzle").Progress != 1
                    || loaded.Position("test-player").scenePath != "Assets/Scenes/Waldheiligtum-Editable.unity"
                    || loaded.Position("test-player").x != 2.5f || loaded.Position("test-player").y != -4
                    || loaded.PlayTimeSeconds != 3723.5 || loaded.SavedScenePath != "Assets/Scenes/Waldheiligtum-Editable.unity"
                    || loaded.CharacterVitals("test-player").Health != 75 || loaded.CharacterVitals("test-player").Mana != 30)
                    throw new Exception("Roundtrip state mismatch.");
                world.Puzzle("test-puzzle").Enter(1); world.CharacterVitals("test-player").Damage(25); SaveGameStore.Save(world, path);
                if (!SaveGameStore.Exists(path + ".bac") || SaveGameStore.Load(path + ".bac").Puzzle("test-puzzle").Progress != 1
                    || SaveGameStore.Load(path + ".bac").CharacterVitals("test-player").Health != 75)
                    throw new Exception("Backup mismatch.");
                var invalid = world.Capture(); invalid.version = 999;
                bool rejected = false; try { WorldSessionState.Restore(invalid); } catch (ArgumentException) { rejected = true; }
                if (!rejected) throw new Exception("Unknown version accepted.");
                var legacy = world.Capture(); legacy.version = 3;
                foreach (var entry in legacy.characters) entry.vitals = null;
                ES3.Save("world-session", legacy, new ES3Settings(path + ".legacy", ES3.Location.File));
                if (SaveGameStore.Load(path + ".legacy").CharacterVitals("test-player").Health != 100)
                    throw new Exception("Legacy vitals migration mismatch.");
                File.WriteAllText("Temp/SaveGameCheckReport.txt", "PASS: Easy-Save-Roundtrip einschließlich HP/Mana, Spielzeit, Szenenübersicht und Position, Inventar und Rätsel; Backup, v3-Migration und Versionsprüfung bestanden. Separater Teststand.");
                Debug.Log("PASS: Easy Save roundtrip und Backup geprüft.");
            }
            finally
            {
                foreach (string file in new[] { path, path + ".bac", path + ".pending", path + ".legacy" }) if (File.Exists(file)) File.Delete(file);
            }
        }
    }
}
