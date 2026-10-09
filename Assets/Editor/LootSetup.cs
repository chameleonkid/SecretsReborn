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
    public static class LootSetup
    {
        private const string Root = "Assets/World/Loot";
        static LootSetup() { EditorApplication.update += Requested; }
        private static void Requested()
        {
            const string request = "Temp/SetupChestLootV1.request";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            for (int i = 0; i < SceneManager.sceneCount; i++) if (SceneManager.GetSceneAt(i).isDirty) return;
            var stage = PrefabStageUtility.GetCurrentPrefabStage(); if (stage != null && stage.scene.isDirty) return;
            File.Delete(request);
            try { Setup(); } catch (Exception error) { File.WriteAllText("Temp/LootSetupReport.txt", "FAIL: " + error); Debug.LogException(error); }
        }
        private static Sprite Art(string name)
        {
            string path = Root + "/Art/" + name + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32; importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        private static ItemDefinition Item(string id, string label, Sprite sprite, ItemPurpose purpose, int stack, int amount)
        {
            string path = Root + "/Items/" + id + ".asset";
            var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemDefinition>(); item.Configure(id, label, stack, null);
                item.SetEquipment(ItemKind.None, false, sprite); item.SetPurpose(purpose, amount); AssetDatabase.CreateAsset(item, path);
            }
            return item;
        }
        private static LootTable Table(string name, LootSourceKind kind, params LootEntry[] entries)
        {
            string path = Root + "/Tables/" + name + ".asset";
            var table = AssetDatabase.LoadAssetAtPath<LootTable>(path);
            if (table == null) { table = ScriptableObject.CreateInstance<LootTable>(); table.Configure(kind, entries); AssetDatabase.CreateAsset(table, path); }
            if (!table.IsValid) throw new Exception("Ungültige Beutetabelle: " + name);
            return table;
        }
        private static GameObject Pickup(ItemDefinition item, Material material)
        {
            string path = Root + "/Prefabs/" + item.ItemId + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (prefab != null) return prefab;
            var obj = new GameObject(item.DisplayName, typeof(CircleCollider2D), typeof(WorldItem));
            try
            {
                obj.GetComponent<CircleCollider2D>().isTrigger = true; obj.GetComponent<CircleCollider2D>().radius = .8f;
                var visual = new GameObject("Visual", typeof(SpriteRenderer)); visual.transform.SetParent(obj.transform, false);
                var renderer = visual.GetComponent<SpriteRenderer>(); renderer.sprite = item.Icon; renderer.sharedMaterial = material; renderer.sortingOrder = 20;
                visual.transform.localScale = Vector3.one * (.4f / Mathf.Max(item.Icon.bounds.size.x, item.Icon.bounds.size.y));
                obj.GetComponent<WorldItem>().Configure("", item);
                return PrefabUtility.SaveAsPrefabAsset(obj, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(obj); }
        }
        [MenuItem("SecretsReborn/World/Set up chests and enemy loot")]
        public static void Setup()
        {
            foreach (var folder in new[] { "Items", "Tables", "Prefabs" }) Directory.CreateDirectory(Root + "/" + folder);
            AssetDatabase.Refresh();
            var gold = Item("gold", "Gold", Art("Gold"), ItemPurpose.Gold, 999, 1);
            var potion = Item("health-potion", "Heiltrank", Art("Potion"), ItemPurpose.HealthPotion, 10, 2);
            var arrows = Item("arrows", "Pfeile", Art("Arrow"), ItemPurpose.Arrows, 99, 1);
            var armor = AssetDatabase.LoadAssetAtPath<ItemDefinition>(AssetDatabase.FindAssets("t:ItemDefinition", new[] { "Assets/World/Sanctuary/Items" })
                .Select(AssetDatabase.GUIDToAssetPath).First(p => AssetDatabase.LoadAssetAtPath<ItemDefinition>(p).Rules.kind == ItemKind.Armor));
            var armorSettings = new SerializedObject(armor);
            if (armorSettings.FindProperty("armorValue").intValue == 0) { armorSettings.FindProperty("armorValue").intValue = 25; armorSettings.ApplyModifiedPropertiesWithoutUndo(); }
            var sword = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/World/Sanctuary/Items/red-training-sword.asset");
            var normal = Table("NormalEnemy", LootSourceKind.NormalEnemy, new LootEntry { item = gold, count = 5 }, new LootEntry { item = potion }, new LootEntry { item = arrows, count = 5 });
            var chestTable = Table("SanctuaryChest", LootSourceKind.Chest, new LootEntry { item = armor });
            Table("BossEquipment", LootSourceKind.Boss, new LootEntry { item = armor }, new LootEntry { item = sword });
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/World/Sanctuary/Lamps/SpriteLit.mat");
            var pickups = new[] { Pickup(gold, material), Pickup(potion, material), Pickup(arrows, material) };
            const string playerPath = "Assets/World/Sanctuary/Prefabs/Player.prefab";
            var player = PrefabUtility.LoadPrefabContents(playerPath);
            try
            {
                var serialized = new SerializedObject(player.GetComponent<CharacterInventory>()); var catalog = serialized.FindProperty("catalog");
                foreach (var item in new[] { gold, potion, arrows })
                    if (!Enumerable.Range(0, catalog.arraySize).Any(i => catalog.GetArrayElementAtIndex(i).objectReferenceValue == item))
                    { int i = catalog.arraySize; catalog.InsertArrayElementAtIndex(i); catalog.GetArrayElementAtIndex(i).objectReferenceValue = item; }
                serialized.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.SaveAsPrefabAsset(player, playerPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(player); }
            string chestPath = Root + "/Prefabs/TreasureChest.prefab";
            var chestPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(chestPath);
            if (chestPrefab == null)
            {
                var obj = new GameObject("Treasure chest", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(TreasureChest), typeof(SpriteDepth));
                try
                {
                    var renderer = obj.GetComponent<SpriteRenderer>(); renderer.sprite = Art("Chest"); renderer.sharedMaterial = material;
                    obj.transform.localScale = Vector3.one * (.8f / renderer.sprite.bounds.size.x);
                    obj.GetComponent<BoxCollider2D>().size = renderer.sprite.bounds.size * .8f;
                    obj.GetComponent<TreasureChest>().Configure("", chestTable);
                    chestPrefab = PrefabUtility.SaveAsPrefabAsset(obj, chestPath);
                }
                finally { UnityEngine.Object.DestroyImmediate(obj); }
            }
            var scene = SceneManager.GetSceneByPath(SanctuaryAuthoring.ScenePath); bool wasOpen = scene.IsValid() && scene.isLoaded;
            if (!wasOpen) scene = EditorSceneManager.OpenScene(SanctuaryAuthoring.ScenePath, OpenSceneMode.Additive);
            try
            {
                var world = scene.GetRootGameObjects().First(o => o.name == "World");
                var actor = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<CharacterInventory>(true)).First();
                if (!world.GetComponentsInChildren<TreasureChest>(true).Any())
                {
                    var chest = (GameObject)PrefabUtility.InstantiatePrefab(chestPrefab, world.transform);
                    chest.transform.position = actor.transform.position + new Vector3(2, 2);
                    chest.GetComponent<TreasureChest>().Configure("sanctuary-chest-01", chestTable);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(chest.GetComponent<TreasureChest>());
                }
                if (!world.GetComponentsInChildren<EnemyLoot>(true).Any())
                {
                    var enemy = world.GetComponentsInChildren<TreeMeleeEnemy>(true).First();
                    var group = new GameObject("Oak rewards", typeof(EnemyLoot)); group.transform.SetParent(world.transform, false);
                    group.GetComponent<EnemyLoot>().Configure(enemy.EnemyId, normal);
                    var entries = normal.Entries;
                    for (int i = 0; i < entries.Length; i++)
                    {
                        var pickup = (GameObject)PrefabUtility.InstantiatePrefab(pickups[i], group.transform);
                        pickup.transform.position = enemy.transform.position + new Vector3((i - 1) * .55f, -.8f);
                        pickup.GetComponent<WorldItem>().Configure("loot:" + enemy.EnemyId + ":" + i, entries[i].item, entries[i].count);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(pickup.GetComponent<WorldItem>());
                        pickup.SetActive(false);
                    }
                }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            finally { if (!wasOpen) EditorSceneManager.CloseScene(scene, true); }
            AssetDatabase.SaveAssets();
            SaveGameIntegrationCheck.Check();
            File.WriteAllText("Temp/LootSetupReport.txt", "PASS: chest, normal-enemy/boss loot tables, utility item catalog and pickup prefabs imported; authored sanctuary extended; Easy Save roundtrip passed.");
        }
    }
}
