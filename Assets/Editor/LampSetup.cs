using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace SecretsReborn.Editor
{
    [InitializeOnLoad]
    public static class LampSetup
    {
        static LampSetup() { EditorApplication.update += Requested; }
        private static void Requested()
        {
            const string request = "Temp/SetupEquippedLampsV7.request";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            for (int i = 0; i < SceneManager.sceneCount; i++) if (SceneManager.GetSceneAt(i).isDirty) return;
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.scene.isDirty) return;
            File.Delete(request);
            try { Setup(); } catch (Exception error) { File.WriteAllText("Temp/LampSetupReport.txt", "FAIL: " + error); Debug.LogException(error); }
        }
        [MenuItem("SecretsReborn/World/Set up equipped lamps")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Play zuerst beenden.");
            const string root = "Assets/World/Sanctuary/Lamps";
            Directory.CreateDirectory(root); AssetDatabase.Refresh();
            var icon = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Proposals/lantern.png").OfType<Sprite>().FirstOrDefault();
            if (icon == null) throw new Exception("Lampen-Sprite fehlt.");
            var warm = Item(root, "warm-lamp", "Wanderlaterne", new Color(1, .82f, .48f), false, icon);
            var magic = Item(root, "rune-lamp", "Runenlaterne", new Color(.72f, .35f, 1), true, icon);
            var materialPath = root + "/SpriteLit.mat";
            var lit = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (lit == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
                if (shader == null) throw new Exception("URP-2D-Lit-Shader fehlt.");
                lit = new Material(shader); AssetDatabase.CreateAsset(lit, materialPath);
            }
            const string playerPath = "Assets/World/Sanctuary/Prefabs/Player.prefab";
            var player = PrefabUtility.LoadPrefabContents(playerPath);
            try
            {
                var serialized = new SerializedObject(player.GetComponent<CharacterInventory>());
                var catalog = serialized.FindProperty("catalog");
                foreach (var item in new[] { warm, magic })
                    if (!Enumerable.Range(0, catalog.arraySize).Any(i => catalog.GetArrayElementAtIndex(i).objectReferenceValue == item))
                    { int i = catalog.arraySize; catalog.InsertArrayElementAtIndex(i); catalog.GetArrayElementAtIndex(i).objectReferenceValue = item; }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var lantern = player.GetComponent<PlayerLantern>();
                var child = player.transform.Find("Lamp light");
                if (child == null) { var obj = new GameObject("Lamp light"); obj.transform.SetParent(player.transform, false); child = obj.transform; }
                var light = child.GetComponent<Light2D>() ?? child.gameObject.AddComponent<Light2D>();
                light.lightType = Light2D.LightType.Point; light.pointLightInnerAngle = light.pointLightOuterAngle = 360;
                light.pointLightOuterRadius = 3.5f; light.pointLightInnerRadius = .7f; light.color = warm.Lamp.LightColor; light.intensity = .8f; light.enabled = false;
                var settings = new SerializedObject(lantern); settings.FindProperty("lampLight").objectReferenceValue = light; settings.ApplyModifiedPropertiesWithoutUndo();
                if (lantern.Indicator != null) lantern.Indicator.enabled = false;
                ConvertRenderers(player, lit);
                PrefabUtility.SaveAsPrefabAsset(player, playerPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(player); }
            // Prefab assets also need lit materials after later instantiation.
            foreach (var path in new[] { "Assets/World/Enemies/Prefabs/OakMeleeEnemy.prefab", "Assets/World/Combat/Prefabs/PlayerSwordVisual.prefab" })
            {
                var obj = PrefabUtility.LoadPrefabContents(path);
                try { ConvertRenderers(obj, lit); PrefabUtility.SaveAsPrefabAsset(obj, path); }
                finally { PrefabUtility.UnloadPrefabContents(obj); }
            }
            var warmPrefab = Pickup(root, warm, icon, lit);
            var magicPrefab = Pickup(root, magic, icon, lit);
            foreach (string path in new[] { SanctuaryAuthoring.ScenePath, "Assets/Scenes/Raetselhoehle-Editable.unity" })
            {
                var scene = SceneManager.GetSceneByPath(path);
                bool wasOpen = scene.IsValid() && scene.isLoaded;
                if (!wasOpen) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    foreach (var obj in scene.GetRootGameObjects()) ConvertRenderers(obj, lit);
                    if (!scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Light2D>(true)).Any(l => l.lightType == Light2D.LightType.Global))
                    {
                        var ambient = new GameObject("Ambient light", typeof(Light2D)); SceneManager.MoveGameObjectToScene(ambient, scene);
                        var light = ambient.GetComponent<Light2D>(); light.lightType = Light2D.LightType.Global; light.color = Color.white; light.intensity = .65f;
                    }
                    if (path == SanctuaryAuthoring.ScenePath)
                    {
                        var actor = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<CharacterInventory>(true)).First();
                        var world = scene.GetRootGameObjects().First(o => o.name == "World");
                        var group = world.transform.Find("Items");
                        if (group == null) { var obj = new GameObject("Items"); obj.transform.SetParent(world.transform, false); group = obj.transform; }
                        int index = 0;
                        foreach (var prefab in new[] { warmPrefab, magicPrefab })
                        {
                            var id = "sanctuary-" + (index == 0 ? "warm-lamp" : "rune-lamp");
                            if (!world.GetComponentsInChildren<WorldItem>(true).Any(w => w.WorldItemId == id))
                            {
                                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group);
                                instance.transform.position = actor.transform.position + new Vector3(index == 0 ? -1 : 1, -.6f);
                                instance.GetComponent<WorldItem>().Configure(id, index == 0 ? warm : magic);
                                PrefabUtility.RecordPrefabInstancePropertyModifications(instance.GetComponent<WorldItem>());
                            }
                            index++;
                        }
                    }
                    EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                }
                finally { if (!wasOpen) EditorSceneManager.CloseScene(scene, true); }
            }
            AssetDatabase.SaveAssets();
            if (warm.Lamp.RevealsRunes || !magic.Lamp.RevealsRunes || warm.Lamp.LightColor == magic.Lamp.LightColor
                || !warm.Rules.Fits(EquipmentSlot.Lamp) || warm.Rules.Fits(EquipmentSlot.OffHand)) throw new Exception("Lamp profile validation failed.");
            SaveGameIntegrationCheck.Check();
            File.WriteAllText("Temp/LampSetupReport.txt", "PASS: warm/rune lamp profiles, item catalog, pickup prefabs, player point light and lit scene materials with ambient lighting; authored scene layouts preserved.");
            Debug.Log("PASS: Lampen, Licht und Prefabs eingerichtet.");
        }
        private static ItemDefinition Item(string root, string id, string label, Color color, bool magical, Sprite icon)
        {
            var path = root + "/" + id + "-light.asset";
            var profile = AssetDatabase.LoadAssetAtPath<LampDefinition>(path);
            if (profile == null) { profile = ScriptableObject.CreateInstance<LampDefinition>(); profile.Configure(color, magical); AssetDatabase.CreateAsset(profile, path); }
            path = root + "/" + id + ".asset";
            var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (item == null)
            { item = ScriptableObject.CreateInstance<ItemDefinition>(); item.Configure(id, label, 1, null); item.SetEquipment(ItemKind.Lamp, false, icon); item.SetLamp(profile); AssetDatabase.CreateAsset(item, path); }
            return item;
        }
        private static GameObject Pickup(string root, ItemDefinition item, Sprite icon, Material lit)
        {
            var path = root + "/" + item.ItemId + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (prefab != null) return prefab;
            var obj = new GameObject(item.DisplayName, typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(WorldItem));
            try
            {
                var renderer = obj.GetComponent<SpriteRenderer>(); renderer.sprite = icon; renderer.sharedMaterial = lit;
                renderer.color = item == null ? Color.white : item.Lamp.RevealsRunes ? new Color(.8f, .5f, 1) : Color.white;
                renderer.sortingOrder = 20;
                // Normalize only this pickup, leaving the source art's importer unchanged.
                obj.transform.localScale = Vector3.one * (.55f / Mathf.Max(icon.bounds.size.x, icon.bounds.size.y));
                var collider = obj.GetComponent<CircleCollider2D>(); collider.isTrigger = true;
                collider.radius = Mathf.Max(icon.bounds.size.x, icon.bounds.size.y);
                obj.GetComponent<WorldItem>().Configure("", item);
                return PrefabUtility.SaveAsPrefabAsset(obj, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(obj); }
        }
        private static void ConvertRenderers(GameObject obj, Material lit)
        {
            foreach (var renderer in obj.GetComponentsInChildren<Renderer>(true))
            {
                var old = renderer.sharedMaterial;
                if (!(renderer is SpriteRenderer) && !(renderer is UnityEngine.Tilemaps.TilemapRenderer)) continue;
                if (old != null && old.shader.name != "Universal Render Pipeline/2D/Sprite-Unlit-Default" && old.shader.name != "Sprites/Default") continue;
                renderer.sharedMaterial = lit;
                if (PrefabUtility.IsPartOfPrefabInstance(renderer)) PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
        }
    }
}
