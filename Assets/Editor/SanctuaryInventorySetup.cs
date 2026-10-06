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
    public static class SanctuaryInventorySetup
    {
        private const string Root = "Assets/World/Sanctuary";
        private const string Request = "Temp/SetupSanctuaryInventory.request";
        static SanctuaryInventorySetup()
        {
            if (File.Exists(Request)) EditorApplication.update += Requested;
        }
        private static void Requested()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            var scene = SceneManager.GetSceneByPath(SanctuaryAuthoring.ScenePath);
            if (scene.IsValid() && scene.isDirty) return;
            EditorApplication.update -= Requested;
            File.Delete(Request);
            try { Setup(); } catch (Exception error) { Debug.LogException(error); }
        }
        [MenuItem("SecretsReborn/World/Set up inventory prototype")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play zuerst beenden.");
            var scene = SceneManager.GetSceneByPath(SanctuaryAuthoring.ScenePath);
            bool open = scene.IsValid() && scene.isLoaded;
            if (open && scene.isDirty) throw new InvalidOperationException("Szenenänderungen zuerst speichern.");
            string folder = Root + "/Items";
            Directory.CreateDirectory(folder); AssetDatabase.Refresh();
            var fallback = AssetDatabase.LoadAssetAtPath<ClothingAppearance>(Root + "/Art/RangerAppearance.asset");
            var armor = AssetDatabase.LoadAssetAtPath<ClothingAppearance>(folder + "/TrainingArmorAppearance.asset");
            if (armor == null)
            {
                armor = ScriptableObject.CreateInstance<ClothingAppearance>();
                armor.Configure("training-armor-blue", AssetDatabase.LoadAllAssetsAtPath(Root + "/Art/RangerOutfit.png").OfType<Sprite>().OrderBy(sprite => sprite.name).ToArray());
                armor.SetTint(new Color(.45f, .65f, 1));
                AssetDatabase.CreateAsset(armor, folder + "/TrainingArmorAppearance.asset");
            }
            var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(folder + "/TrainingArmor.asset");
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemDefinition>();
                item.Configure("training-armor", "Blaue Übungsrüstung", 1, armor);
                AssetDatabase.CreateAsset(item, folder + "/TrainingArmor.asset");
            }
            string playerPath = Root + "/Prefabs/Player.prefab";
            var player = PrefabUtility.LoadPrefabContents(playerPath);
            try
            {
                var inventory = player.GetComponent<CharacterInventory>();
                if (inventory == null) inventory = player.AddComponent<CharacterInventory>();
                inventory.Configure(new[] { item }, fallback);
                if (player.GetComponent<InventoryInteraction>() == null) player.AddComponent<InventoryInteraction>();
                PrefabUtility.SaveAsPrefabAsset(player, playerPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(player); }
            string pickupPath = Root + "/Prefabs/TrainingArmorPickup.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pickupPath);
            if (prefab == null)
            {
                var obj = new GameObject("Training armor pickup", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(WorldItem), typeof(SpriteDepth));
                try
                {
                    var renderer = obj.GetComponent<SpriteRenderer>();
                    renderer.sprite = armor.Frame(0); renderer.color = armor.Tint;
                    renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Art/SpriteUnlit.mat");
                    var trigger = obj.GetComponent<CircleCollider2D>();
                    trigger.isTrigger = true; trigger.radius = 1.4f;
                    obj.GetComponent<WorldItem>().Configure("sanctuary-training-armor", item);
                    prefab = PrefabUtility.SaveAsPrefabAsset(obj, pickupPath);
                }
                finally { UnityEngine.Object.DestroyImmediate(obj); }
            }
            var previous = SceneManager.GetActiveScene();
            if (!open) scene = EditorSceneManager.OpenScene(SanctuaryAuthoring.ScenePath, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var world = scene.GetRootGameObjects().Single(obj => obj.name == "World");
                var parent = world.transform.Find("Items");
                if (parent == null) { parent = new GameObject("Items").transform; parent.SetParent(world.transform, false); }
                if (parent.GetComponentInChildren<WorldItem>(true) == null)
                {
                    var pickup = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                    var actor = scene.GetRootGameObjects().Single(obj => obj.GetComponent<PlayerMovement>() != null);
                    pickup.transform.position = actor.transform.position + Vector3.right * 2;
                }
                var character = scene.GetRootGameObjects().Single(obj => obj.GetComponent<CharacterInventory>() != null);
                if (character.GetComponent<InventoryInteraction>() == null) throw new InvalidOperationException("Inventareingabe fehlt.");
                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Szene konnte nicht gespeichert werden.");
                File.WriteAllText("Temp/SanctuaryInventoryReport.txt", "PASS: Item- und Appearance-Assets, Player-Inventar und Eingabe, Pickup-Prefab und Szeneninstanz gespeichert; Karten-Tiles unverändert.");
                Debug.Log("Inventar-Prototyp eingerichtet: E aufheben, I Inventar, Rüstung anlegen/ablegen.");
            }
            finally
            {
                if (!open) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }
    }
}
