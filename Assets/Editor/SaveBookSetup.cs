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
    public static class SaveBookSetup
    {
        private const string Request = "Temp/SetupSaveBook.request";
        static SaveBookSetup() { if (File.Exists(Request)) EditorApplication.update += Requested; }
        private static void Requested()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            var scene = SceneManager.GetSceneByPath(SanctuaryAuthoring.ScenePath);
            if (scene.IsValid() && scene.isDirty) return;
            EditorApplication.update -= Requested; File.Delete(Request);
            try { Setup(); } catch (Exception error) { Debug.LogException(error); }
        }
        [MenuItem("SecretsReborn/World/Set up save book")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play beenden.");
            const string root = "Assets/World/Sanctuary";
            string path = root + "/Art/SaveDesk.png";
            if (!File.Exists(path))
            {
                File.Copy("docs/asset-review/Desk_type1.png", path); AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite; importer.spritePixelsPerUnit = 24;
                importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            string pages = "Assets/Resources/InventoryUI/SaveBookPages.png";
            if (!File.Exists(pages)) { File.Copy("docs/asset-review/SavePanel.png", pages); AssetDatabase.ImportAsset(pages); }
            string prefabPath = root + "/Prefabs/SaveBookDesk.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                var obj = new GameObject("Save book desk", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(SpriteDepth), typeof(SaveBook));
                try
                {
                    var image = obj.GetComponent<SpriteRenderer>(); image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    image.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(root + "/Art/SpriteUnlit.mat");
                    obj.GetComponent<BoxCollider2D>().size = new Vector2(1, .55f);
                    var range = obj.AddComponent<CircleCollider2D>(); range.isTrigger = true; range.radius = 2;
                    prefab = PrefabUtility.SaveAsPrefabAsset(obj, prefabPath);
                }
                finally { UnityEngine.Object.DestroyImmediate(obj); }
            }
            var scene = SceneManager.GetSceneByPath(SanctuaryAuthoring.ScenePath); bool open = scene.IsValid() && scene.isLoaded;
            if (open && scene.isDirty) throw new InvalidOperationException("Szene zuerst speichern.");
            var previous = SceneManager.GetActiveScene();
            if (!open) scene = EditorSceneManager.OpenScene(SanctuaryAuthoring.ScenePath, OpenSceneMode.Additive);
            try
            {
                var world = scene.GetRootGameObjects().Single(obj => obj.name == "World");
                if (world.GetComponentInChildren<SaveBook>(true) == null)
                {
                    var desk = (GameObject)PrefabUtility.InstantiatePrefab(prefab, world.transform.Find("Props"));
                    desk.transform.position = new Vector3(-2, -6, 0);
                }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
                File.WriteAllText("Temp/SaveBookSetupReport.txt", "PASS: originales Secrets-Schreibtisch/Buch-Art, Prefab und Szeneninstanz gespeichert; drei Buch-Slots, kein globales Autosave.");
            }
            finally
            {
                if (!open) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }
    }
}
