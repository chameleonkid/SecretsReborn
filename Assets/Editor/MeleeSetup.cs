using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SecretsReborn.Editor
{
    [InitializeOnLoad]
    public static class MeleeSetup
    {
        private const string Request = "Temp/SetupMelee.request";
        private const string ScaleRequest = "Temp/FixOakScale.request";
        private const string Prefab = "Assets/World/Enemies/Prefabs/OakMeleeEnemy.prefab";
        static MeleeSetup() { EditorApplication.update += Requested; }
        private static void Requested()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (File.Exists(ScaleRequest))
            {
                File.Delete(ScaleRequest);
                try { FixScale(); } catch (Exception error) { Debug.LogException(error); File.WriteAllText("Temp/OakScaleReport.txt", "FAIL: " + error); }
            }
            if (!File.Exists(Request)) return;
            for (int i = 0; i < SceneManager.sceneCount; i++) if (SceneManager.GetSceneAt(i).isDirty) return;
            File.Delete(Request);
            try { Setup(); } catch (Exception error) { File.WriteAllText("Temp/MeleeSetupReport.txt", "FAIL: " + error); Debug.LogException(error); }
        }
        [MenuItem("SecretsReborn/Combat/Fix oak sprite scale")]
        public static void FixScale()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Play zuerst beenden.");
            const string path = "Assets/World/Enemies/Art/OakEnemy.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) throw new Exception("Eichen-Spritesheet fehlt.");
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            if (sprites.Length == 0 || sprites.Any(s => Mathf.Abs(s.pixelsPerUnit - 16) > .001f)) throw new Exception("Sprite-Maßstab nicht übernommen.");
            File.WriteAllText("Temp/OakScaleReport.txt", "PASS: Oak sprites imported at 16 PPU, Point, Full Rect; existing slices and prefab scale preserved.");
        }
        [MenuItem("SecretsReborn/Combat/Set up oak melee enemy")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Play zuerst beenden.");
            for (int i = 0; i < SceneManager.sceneCount; i++) if (SceneManager.GetSceneAt(i).isDirty) throw new Exception("Szenen zuerst speichern.");
            Directory.CreateDirectory("Assets/World/Enemies/Prefabs"); AssetDatabase.Refresh();
            const string art = "Assets/World/Enemies/Art/OakEnemy.png";
            if (AssetDatabase.LoadAllAssetsAtPath(art).OfType<Sprite>().Count() != 24)
            {
                var rects = new SpriteRect[24];
                for (int i = 0; i < 24; i++) rects[i] = new SpriteRect { name = "Oak" + i.ToString("D2"), rect = new Rect(i % 6 * 32, 96 - i / 6 * 32, 32, 32),
                    alignment = SpriteAlignment.Custom, pivot = new Vector2(.5f, .25f), spriteID = GUID.Generate() };
                SanctuaryAuthoring.Slice(art, rects);
                var importer = (TextureImporter)AssetImporter.GetAtPath(art); importer.spritePixelsPerUnit = 16; importer.SaveAndReimport();
            }
            FixScale();
            var sprites = AssetDatabase.LoadAllAssetsAtPath(art).OfType<Sprite>()
                .OrderByDescending(s => Mathf.Floor(s.rect.center.y / 32)).ThenBy(s => Mathf.Floor(s.rect.center.x / 32)).ToArray();
            const string playerPath = "Assets/World/Sanctuary/Prefabs/Player.prefab";
            var player = PrefabUtility.LoadPrefabContents(playerPath);
            try { if (player.GetComponent<PlayerMelee>() == null) player.AddComponent<PlayerMelee>(); PrefabUtility.SaveAsPrefabAsset(player, playerPath); }
            finally { PrefabUtility.UnloadPrefabContents(player); }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
            if (prefab == null)
            {
                var obj = new GameObject("Oak melee enemy", typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SpriteDepth), typeof(TreeMeleeEnemy));
                try
                {
                    obj.GetComponent<SpriteRenderer>().sprite = sprites[0];
                    obj.GetComponent<SpriteRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/World/Sanctuary/Art/SpriteUnlit.mat");
                    var body = obj.GetComponent<Rigidbody2D>(); body.gravityScale = 0; body.constraints = RigidbodyConstraints2D.FreezeRotation;
                    body.mass = 2; body.interpolation = RigidbodyInterpolation2D.Interpolate;
                    body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                    obj.GetComponent<BoxCollider2D>().size = new Vector2(.8f, .6f);
                    obj.GetComponent<TreeMeleeEnemy>().Configure("", sprites);
                    prefab = PrefabUtility.SaveAsPrefabAsset(obj, Prefab);
                }
                finally { UnityEngine.Object.DestroyImmediate(obj); }
            }
            var scene = SceneManager.GetSceneByPath(SanctuaryAuthoring.ScenePath);
            bool open = scene.IsValid() && scene.isLoaded;
            var previous = SceneManager.GetActiveScene();
            if (!open) scene = EditorSceneManager.OpenScene(SanctuaryAuthoring.ScenePath, OpenSceneMode.Additive);
            try
            {
                var world = scene.GetRootGameObjects().Single(o => o.name == "World");
                if (!world.GetComponentsInChildren<TreeMeleeEnemy>(true).Any(e => e.EnemyId == "sanctuary-oak-01"))
                {
                    var parent = world.transform.Find("Enemies");
                    if (parent == null) { var group = new GameObject("Enemies"); group.transform.SetParent(world.transform, false); parent = group.transform; }
                    var ground = world.transform.Find("Grid/Ground")?.GetComponent<Tilemap>();
                    var candidates = new[] { new Vector3(-6, 2), new Vector3(-5, 3), new Vector3(6, 2), new Vector3(5, 3), new Vector3(-4, 1) };
                    Physics2D.SyncTransforms();
                    var position = candidates.FirstOrDefault(p => ground != null && ground.HasTile(ground.WorldToCell(p))
                        && !Physics2D.OverlapBoxAll(p, new Vector2(1, .8f), 0).Any(c => c != null && !c.isTrigger && c.gameObject.scene == scene));
                    if (position == Vector3.zero) throw new Exception("Kein freier Gegnerplatz gefunden. Einen Platz wählen und Setup erneut ausführen.");
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                    instance.transform.position = position;
                    instance.GetComponent<TreeMeleeEnemy>().SetIdentity("sanctuary-oak-01");
                    PrefabUtility.RecordPrefabInstancePropertyModifications(instance.GetComponent<TreeMeleeEnemy>());
                }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
                foreach (var actor in scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<CharacterInventory>()))
                    if (actor.GetComponent<PlayerMelee>() == null) throw new Exception("Spieler hat keinen Nahkampf-Component.");
                File.WriteAllText("Temp/MeleeSetupReport.txt", "PASS: Original log_oak art (24 frames, 16 PPU), new oak melee prefab, player melee component and forest enemy saved. Existing terrain preserved.");
            }
            finally { if (!open) EditorSceneManager.CloseScene(scene, true); if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous); }
        }
    }
}
