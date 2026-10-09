using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsReborn.Editor
{
    [InitializeOnLoad]
    public static class EquipmentFoundationSetup
    {
        private const string Root = "Assets/World/Equipment";
        private const string Request = "Temp/SetupEquipmentFoundation.request";
        static EquipmentFoundationSetup() => EditorApplication.update += Requested;
        private static void Requested()
        {
            if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            for (int i = 0; i < SceneManager.sceneCount; i++) if (SceneManager.GetSceneAt(i).isDirty) return;
            if (PrefabStageUtility.GetCurrentPrefabStage() != null) return;
            File.Delete(Request);
            try { Setup(); }
            catch (Exception error) { File.WriteAllText("Temp/EquipmentFoundationReport.txt", "FAIL: " + error); Debug.LogException(error); }
        }
        [MenuItem("SecretsReborn/Equipment/Prepare equipment foundation")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play zuerst beenden.");
            for (int i = 0; i < SceneManager.sceneCount; i++) if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Offene Szenen zuerst speichern.");
            foreach (string folder in new[] { "Items", "Tables", "Prefabs", "Art" }) Directory.CreateDirectory(Root + "/" + folder);
            AssetDatabase.Refresh();
            var importer = (TextureImporter)AssetImporter.GetAtPath(Root + "/Art/ManaPotion.png");
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false; importer.spritePixelsPerUnit = 16;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings); settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            var mana = Asset<ItemDefinition>("Items/mana-potion.asset");
            mana.Configure("mana-potion", "Manatrank", 10, null); mana.SetPurpose(ItemPurpose.ManaPotion, 20);
            mana.SetEquipment(ItemKind.None, false, AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/ManaPotion.png")); mana.SetValues(4, 16); EditorUtility.SetDirty(mana);
            string[] looks = { "warrior-armor", "wizard-robes", "paladin-armor" };
            string[] names = { "Wächterrüstung", "Waldmagierrobe", "Quellenhüterrüstung" };
            int[] armor = { 60, 10, 100 };
            var gear = new ItemDefinition[3];
            for (int i = 0; i < gear.Length; i++)
            {
                var look = AssetDatabase.LoadAssetAtPath<ClothingAppearance>("Assets/Resources/CharacterLooks/female-3-outfits-rpc-female-" + looks[i] + ".asset");
                if (look == null) throw new Exception("RetroPixel-Aussehen fehlt: " + looks[i]);
                gear[i] = Asset<ItemDefinition>("Items/" + looks[i] + ".asset");
                gear[i].Configure("foundation-" + looks[i], names[i], 1, look); gear[i].SetEquipment(ItemKind.Armor, false, look.Frame(0));
                gear[i].SetValues(20 + i * 15, 80 + i * 60, armor[i]);
                var fields = new SerializedObject(gear[i]); fields.FindProperty("quality").enumValueIndex = i == 2 ? 2 : 1; fields.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(gear[i]);
                var table = Asset<LootTable>("Tables/" + looks[i] + ".asset"); table.Configure(LootSourceKind.Chest, new[] { new LootEntry { item = gear[i], count = 1 } }); EditorUtility.SetDirty(table);
                WithPrefab("Assets/World/Loot/Prefabs/TreasureChest.prefab", "Prefabs/Chest-" + looks[i] + ".prefab", obj => obj.GetComponent<TreasureChest>().Configure("foundation-" + looks[i], table));
            }
            // Retain every existing catalog entry and give old items sensible starter prices.
            var player = PrefabUtility.LoadPrefabContents("Assets/World/Sanctuary/Prefabs/Player.prefab");
            try
            {
                var fields = new SerializedObject(player.GetComponent<CharacterInventory>()); var catalog = fields.FindProperty("catalog");
                var items = Enumerable.Range(0, catalog.arraySize).Select(i => catalog.GetArrayElementAtIndex(i).objectReferenceValue as ItemDefinition).Where(x => x != null).Concat(gear).Append(mana).Distinct().ToArray();
                foreach (var item in items)
                {
                    if (item.SellValue == 0 && item.Purpose != ItemPurpose.Gold) { item.SetValues(item.Purpose == ItemPurpose.Equipment ? 10 : 2, item.Purpose == ItemPurpose.Equipment ? 40 : 8, item.ArmorValue); EditorUtility.SetDirty(item); }
                }
                catalog.arraySize = items.Length; for (int i = 0; i < items.Length; i++) catalog.GetArrayElementAtIndex(i).objectReferenceValue = items[i]; fields.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(player, "Assets/World/Sanctuary/Prefabs/Player.prefab");
            }
            finally { PrefabUtility.UnloadPrefabContents(player); }
            WithPrefab("Assets/World/Loot/Prefabs/health-potion.prefab", "Prefabs/ManaPotions.prefab", obj =>
            {
                obj.GetComponent<WorldItem>().Configure("foundation-mana", mana, 3); var renderer = obj.GetComponentInChildren<SpriteRenderer>(); renderer.sprite = mana.Icon; renderer.color = Color.white;
            });
            WithPrefab("Assets/World/Loot/Prefabs/health-potion.prefab", "Prefabs/HealthPotions.prefab", obj => obj.GetComponent<WorldItem>().Configure("foundation-health", AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/World/Loot/Items/health-potion.asset"), 3));
            WithPrefab("Assets/World/Loot/Prefabs/gold.prefab", "Prefabs/Gold.prefab", obj => obj.GetComponent<WorldItem>().Configure("foundation-gold", AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/World/Loot/Items/gold.asset"), 25));
            WithPrefab("Assets/World/Enemies/Prefabs/OakMeleeEnemy.prefab", "Prefabs/SturdyOak.prefab", obj =>
            {
                var fields = new SerializedObject(obj.GetComponent<TreeMeleeEnemy>()); fields.FindProperty("maxHealth").intValue = 30; fields.FindProperty("enemyId").stringValue = "foundation-sturdy-oak"; fields.ApplyModifiedPropertiesWithoutUndo();
            });
            AssetDatabase.SaveAssets(); CoopSetup.Setup();
            AddTestGroup();
            SaveGameIntegrationCheck.Check();
            File.WriteAllText("Temp/EquipmentFoundationReport.txt", "PASS: 3 RetroPixel armor items/chest prefabs, mana/health/gold pickups, 30-HP oak, preserved catalog, incremental EquipmentTests group (existing terrain untouched), native Easy Save check.");
        }
        private static void AddTestGroup()
        {
            const string path = "Assets/Scenes/Waldheiligtum-Editable.unity";
            var scene = SceneManager.GetSceneByPath(path); bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var world = scene.GetRootGameObjects().FirstOrDefault(x => x.name == "World");
                if (world == null || world.transform.Find("EquipmentTests") != null) return;
                var player = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<CharacterInventory>(true)).FirstOrDefault();
                var origin = player != null ? player.transform.position : Vector3.zero;
                var group = new GameObject("EquipmentTests"); SceneManager.MoveGameObjectToScene(group, scene); group.transform.SetParent(world.transform, false);
                string[] prefabs = { "Chest-warrior-armor", "Chest-wizard-robes", "Chest-paladin-armor", "HealthPotions", "ManaPotions", "Gold", "SturdyOak" };
                for (int i = 0; i < prefabs.Length; i++)
                {
                    var obj = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/" + prefabs[i] + ".prefab"), scene);
                    obj.transform.SetParent(group.transform, true);
                    obj.transform.position = origin + new Vector3(3 + (i % 3) * 2, -2 - (i / 3) * 2, 0);
                }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        private static T Asset<T>(string relative) where T : ScriptableObject
        {
            string path = Root + "/" + relative; var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); } return asset;
        }
        private static void WithPrefab(string source, string relative, Action<GameObject> configure)
        {
            var obj = PrefabUtility.LoadPrefabContents(source);
            try { configure(obj); PrefabUtility.SaveAsPrefabAsset(obj, Root + "/" + relative); }
            finally { PrefabUtility.UnloadPrefabContents(obj); }
        }
    }
}
