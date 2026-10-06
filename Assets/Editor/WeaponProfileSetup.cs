using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SecretsReborn.Editor
{
    [InitializeOnLoad]
    public static class WeaponProfileSetup
    {
        static WeaponProfileSetup() { EditorApplication.update += Requested; }
        private static void Requested()
        {
            const string request = "Temp/SetupWeaponProfiles.request";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.scene.isDirty) return;
            File.Delete(request);
            try { Setup(); } catch (Exception error) { File.WriteAllText("Temp/WeaponProfileReport.txt", "FAIL: " + error); Debug.LogException(error); }
        }
        [MenuItem("SecretsReborn/Combat/Set up item weapon profiles")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Play zuerst beenden.");
            const string root = "Assets/World/Combat/Weapons";
            Directory.CreateDirectory(root); AssetDatabase.Refresh();
            var frames = AssetDatabase.LoadAllAssetsAtPath("Assets/World/Combat/Art/PlayerSword.png").OfType<Sprite>().OrderBy(s => s.name).ToArray();
            if (frames.Length != 128 || frames[68].rect.height != 49) throw new Exception("Korrigierten Schwert-Import zuerst abschließen.");
            var normal = Profile(root + "/TrainingSword.asset", frames, Color.white);
            var red = Profile(root + "/RedTrainingSword.asset", frames, new Color(1, .32f, .27f));
            var normalItem = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/World/Sanctuary/Items/training-sword.asset");
            normalItem.SetWeapon(normal); EditorUtility.SetDirty(normalItem);
            const string redPath = "Assets/World/Sanctuary/Items/red-training-sword.asset";
            var redItem = AssetDatabase.LoadAssetAtPath<ItemDefinition>(redPath);
            if (redItem == null)
            {
                redItem = ScriptableObject.CreateInstance<ItemDefinition>(); redItem.Configure("red-training-sword", "Rötliches Übungsschwert", 1, null);
                redItem.SetEquipment(ItemKind.Weapon, false, normalItem.Icon); redItem.SetIconContent(normalItem.IconContent);
                redItem.SetWeapon(red); AssetDatabase.CreateAsset(redItem, redPath);
            }
            const string playerPath = "Assets/World/Sanctuary/Prefabs/Player.prefab";
            var player = PrefabUtility.LoadPrefabContents(playerPath);
            try
            {
                var serialized = new SerializedObject(player.GetComponent<CharacterInventory>()); var catalog = serialized.FindProperty("catalog");
                bool present = false;
                for (int i = 0; i < catalog.arraySize; i++) if (catalog.GetArrayElementAtIndex(i).objectReferenceValue == redItem) present = true;
                if (!present) { int index = catalog.arraySize; catalog.InsertArrayElementAtIndex(index); catalog.GetArrayElementAtIndex(index).objectReferenceValue = redItem; serialized.ApplyModifiedPropertiesWithoutUndo(); }
                PrefabUtility.SaveAsPrefabAsset(player, playerPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(player); }
            AssetDatabase.SaveAssets();
            if (normalItem.Weapon != normal || redItem.Weapon != red || red.Tint == normal.Tint || red.Frame(68) != normal.Frame(68)
                || normal.HitDelay >= normal.Duration || normal.Cooldown < normal.Duration) throw new Exception("Weapon profile validation failed.");
            File.WriteAllText("Temp/WeaponProfileReport.txt", "PASS: normal/red sword items reference independent weapon profiles; same corrected frames, different tint; timing validated; red item added to player catalog.");
        }
        private static WeaponDefinition Profile(string path, Sprite[] frames, Color tint)
        {
            var profile = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);
            if (profile != null) return profile;
            profile = ScriptableObject.CreateInstance<WeaponDefinition>(); profile.Configure(MeleeWeaponType.Sword, frames, tint);
            AssetDatabase.CreateAsset(profile, path); return profile;
        }
    }
}
