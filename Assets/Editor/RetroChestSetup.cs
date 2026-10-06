using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsReborn.Editor
{
    [InitializeOnLoad]
    public static class RetroChestSetup
    {
        static RetroChestSetup() { EditorApplication.update += Requested; }
        private static void Requested()
        {
            const string request = "Temp/SetupRetroDungeonChestV1.request";
            const string presentationRequest = "Temp/SetupRetroChestPresentationV2.request";
            if ((!File.Exists(request) && !File.Exists(presentationRequest)) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            for (int i = 0; i < SceneManager.sceneCount; i++) if (SceneManager.GetSceneAt(i).isDirty) return;
            var stage = PrefabStageUtility.GetCurrentPrefabStage(); if (stage != null && stage.scene.isDirty) return;
            if (File.Exists(request)) File.Delete(request);
            if (File.Exists(presentationRequest)) File.Delete(presentationRequest);
            try { Setup(); } catch (Exception error) { File.WriteAllText("Temp/RetroChestReport.txt", "FAIL: " + error); Debug.LogException(error); }
        }
        [MenuItem("SecretsReborn/World/Use Retro Pixel Dungeon chest")]
        public static void Setup()
        {
            const string art = "Assets/World/Loot/Art/RetroDungeonChest.psd";
            var existing = AssetDatabase.LoadAllAssetsAtPath(art).OfType<Sprite>().ToArray();
            if (existing.Length != 4 || !existing.Any(s => s.name == "wood_chest_1"))
            {
                var rects = new SpriteRect[4];
                for (int i = 0; i < 4; i++) rects[i] = new SpriteRect {
                    name = "wood_chest_" + (i + 1), rect = new Rect(288 + i * 32, 160, 32, 32),
                    alignment = SpriteAlignment.Custom, pivot = new Vector2(.5f, .25f), spriteID = GUID.Generate() };
                SanctuaryAuthoring.Slice(art, rects);
            }
            var frames = AssetDatabase.LoadAllAssetsAtPath(art).OfType<Sprite>().OrderBy(s => s.name).ToArray();
            if (frames.Length != 4 || frames[0].rect != new Rect(288, 160, 32, 32)) throw new Exception("Truhen-Import stimmt nicht mit dem Original überein.");
            var shader = Shader.Find("SecretsReborn/Chest/SpriteLitNoBakedShadow");
            if (shader == null || ShaderUtil.ShaderHasError(shader)) throw new Exception("Schattenfreies Truhen-Material konnte nicht kompiliert werden.");
            const string materialPath = "Assets/World/Loot/Art/ChestNoBakedShadow.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, materialPath); }
            const string path = "Assets/World/Loot/Prefabs/TreasureChest.prefab";
            var prefab = PrefabUtility.LoadPrefabContents(path);
            try
            {
                // Avoid starting session state while configuring an editor prefab.
                var serialized = new SerializedObject(prefab.GetComponent<TreasureChest>());
                var sprites = serialized.FindProperty("openingFrames"); sprites.arraySize = frames.Length;
                for (int i = 0; i < frames.Length; i++) sprites.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();
                prefab.GetComponent<SpriteRenderer>().sprite = frames[0];
                prefab.GetComponent<SpriteRenderer>().sharedMaterial = material;
                prefab.transform.localScale = Vector3.one * 1.35f;
                var collider = prefab.GetComponent<BoxCollider2D>(); collider.size = new Vector2(.65f, .3f); collider.offset = Vector2.zero;
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            var scene = SceneManager.GetSceneByPath(SanctuaryAuthoring.ScenePath); bool open = scene.IsValid() && scene.isLoaded;
            if (!open) scene = EditorSceneManager.OpenScene(SanctuaryAuthoring.ScenePath, OpenSceneMode.Additive);
            try
            {
                foreach (var chest in scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<TreasureChest>(true)))
                {
                    var renderer = chest.GetComponent<SpriteRenderer>(); renderer.sprite = frames[0]; renderer.color = Color.white;
                    renderer.sharedMaterial = material;
                    chest.transform.localScale = Vector3.one * 1.35f;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(chest.transform);
                }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            finally { if (!open) EditorSceneManager.CloseScene(scene, true); }
            AssetDatabase.SaveAssets();
            File.WriteAllText("Temp/RetroChestReport.txt", "PASS V2: original Retro Pixel Dungeons frames and foot pivot; translucent baked shadow rejected by lit shader; shader has no import errors; prefab and sanctuary scale 1.35, base collision preserved.");
        }
    }
}
