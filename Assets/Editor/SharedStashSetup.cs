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
    public static class SharedStashSetup
    {
        private static string lastWait;
        private static void Wait(string reason)
        { if (lastWait == reason) return; lastWait = reason; File.WriteAllText("Temp/SharedStashSetupReport.txt","WAIT: " + reason); }
        static SharedStashSetup() => EditorApplication.update += Requested;
        private static void Requested()
        {
            const string request = "Temp/SetupSharedStash.request";
            if (!File.Exists(request)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Wait("Play beenden."); return; }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (PrefabStageUtility.GetCurrentPrefabStage() != null) { Wait("Prefab speichern und Prefab-Modus schließen."); return; }
            for (int i = 0; i < SceneManager.sceneCount; i++) if (SceneManager.GetSceneAt(i).isDirty) { Wait("Szene speichern: " + SceneManager.GetSceneAt(i).name); return; }
            File.Delete(request);
            try { Setup(); } catch (Exception e) { File.WriteAllText("Temp/SharedStashSetupReport.txt", "FAIL: " + e); Debug.LogException(e); }
        }
        [MenuItem("SecretsReborn/Equipment/Prepare shared stash and normalize icons")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play zuerst beenden.");
            for (int i = 0; i < SceneManager.sceneCount; i++) if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Szenen zuerst speichern.");
            NormalizeIcons();
            const string prefab = "Assets/World/Equipment/Prefabs/SharedStash.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefab) == null)
            {
                var obj = PrefabUtility.LoadPrefabContents("Assets/World/Loot/Prefabs/TreasureChest.prefab");
                try
                {
                    obj.name = "SharedStash"; UnityEngine.Object.DestroyImmediate(obj.GetComponent<TreasureChest>()); obj.AddComponent<SharedStashContainer>();
                    var label = new GameObject("Lager label",typeof(TextMesh)); label.transform.SetParent(obj.transform,false); label.transform.localPosition = new Vector3(0,.7f,0);
                    var text = label.GetComponent<TextMesh>(); text.text = "Lager"; text.anchor = TextAnchor.MiddleCenter; text.characterSize = .075f; text.fontSize = 32; text.color = new Color(1,.9f,.6f);
                    text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.GetComponent<MeshRenderer>().sharedMaterial = text.font.material; label.GetComponent<MeshRenderer>().sortingOrder = 1100;
                    PrefabUtility.SaveAsPrefabAsset(obj,prefab);
                }
                finally { PrefabUtility.UnloadPrefabContents(obj); }
            }
            const string path = "Assets/Scenes/Waldheiligtum-Editable.unity";
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play wurde während des Setups gestartet. Stoppen und Setup erneut starten.");
            var scene = SceneManager.GetSceneByPath(path); bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects(); var world = roots.FirstOrDefault(x => x.name == "World");
                if (world != null && !roots.SelectMany(x => x.GetComponentsInChildren<SharedStashContainer>(true)).Any())
                {
                    var player = roots.SelectMany(x => x.GetComponentsInChildren<CharacterInventory>(true)).FirstOrDefault();
                    var obj = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefab),scene);
                    obj.transform.SetParent(world.transform,true); obj.transform.position = (player != null ? player.transform.position : Vector3.zero) + new Vector3(-3,-2,0);
                    if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play wurde während der Platzierung gestartet. Stoppen und Setup erneut starten.");
                    EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                }
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene,true); }
            AssetDatabase.SaveAssets(); CoopSetup.Setup(); SaveGameIntegrationCheck.Check();
            File.WriteAllText("Temp/SharedStashSetupReport.txt","PASS: native shared stash prefab/scene placement, normalized visible icon bounds, Easy Save roundtrip. Existing terrain retained.");
        }
        [MenuItem("SecretsReborn/Equipment/Normalize item icon bounds")]
        public static void NormalizeIcons()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:ItemDefinition",new[] { "Assets/World" }))
            {
                var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(AssetDatabase.GUIDToAssetPath(guid)); var sprite = item.Icon;
                if (sprite == null) continue;
                var texture = sprite.texture; var old = RenderTexture.active; var rt = RenderTexture.GetTemporary(texture.width,texture.height,0,RenderTextureFormat.ARGB32); rt.filterMode = FilterMode.Point;
                var copy = new Texture2D(texture.width,texture.height,TextureFormat.RGBA32,false);
                try
                {
                    Graphics.Blit(texture,rt); RenderTexture.active = rt; copy.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0); copy.Apply();
                    var pixels = copy.GetPixels32(); var full = sprite.textureRect; var area = item.IconContent;
                    int left = Mathf.Clamp(Mathf.FloorToInt(full.x + area.x * full.width),0,texture.width-1), bottom = Mathf.Clamp(Mathf.FloorToInt(full.y + area.y * full.height),0,texture.height-1);
                    int right = Mathf.Clamp(Mathf.CeilToInt(full.x + area.xMax * full.width),left+1,texture.width), top = Mathf.Clamp(Mathf.CeilToInt(full.y + area.yMax * full.height),bottom+1,texture.height);
                    int minX = right, minY = top, maxX = left-1, maxY = bottom-1;
                    for (int y = bottom; y < top; y++) for (int x = left; x < right; x++) if (pixels[y * texture.width+x].a >= 32)
                    { minX = Math.Min(minX,x); minY = Math.Min(minY,y); maxX = Math.Max(maxX,x); maxY = Math.Max(maxY,y); }
                    if (maxX >= minX && maxY >= minY)
                    {
                        item.SetIconContent(new Rect((minX-full.x)/full.width,(minY-full.y)/full.height,(maxX-minX+1)/full.width,(maxY-minY+1)/full.height)); EditorUtility.SetDirty(item);
                    }
                }
                finally { RenderTexture.active = old; RenderTexture.ReleaseTemporary(rt); UnityEngine.Object.DestroyImmediate(copy); }
            }
            AssetDatabase.SaveAssets();
        }
    }
}
