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
    public static class AreaTransitionSetup
    {
        public const string Cave = "Assets/Scenes/Raetselhoehle-Editable.unity";
        private const string Root = "Assets/World/Cave";
        private const string Request = "Temp/SetupAreaTransitions.request";
        static AreaTransitionSetup() { EditorApplication.update += Requested; }
        private static void Requested()
        {
            if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty || string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path)) return;
            File.Delete(Request);
            try { Setup(); }
            catch (Exception error) { File.WriteAllText("Temp/AreaTransitionReport.txt", error.ToString()); Debug.LogException(error); }
        }
        [MenuItem("SecretsReborn/World/Set up cave and area transitions")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play zuerst beenden.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Offene Szenen zuerst speichern.");
            Directory.CreateDirectory(Root + "/Art"); Directory.CreateDirectory(Root + "/Tiles"); Directory.CreateDirectory(Root + "/Prefabs");
            AssetDatabase.Refresh();
            string art = Root + "/Art/CaveTiles.png";
            if (!File.Exists(art))
            {
                File.Copy("Assets/Art/Proposals/cave.png", art); AssetDatabase.ImportAsset(art);
                var rects = Enumerable.Range(0, 3).Select(i => Slice("Floor" + i, 832 + 32 * i, 608)).ToList();
                for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++) rects.Add(Slice("Wall" + x + y, 800 + x * 32, 1024 + y * 32));
                SanctuaryAuthoring.Slice(art, rects.ToArray());
            }
            var sprites = AssetDatabase.LoadAllAssetsAtPath(art).OfType<Sprite>().ToArray();
            var floors = Enumerable.Range(0, 3).Select(i => Tile("Floor" + i, sprites, false)).ToArray();
            var walls = new Tile[3, 3];
            for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++) walls[x, y] = Tile("Wall" + x + y, sprites, true);
            var previous = SceneManager.GetActiveScene();
            if (!File.Exists(Cave))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                try
                {
                    SceneManager.SetActiveScene(scene);
                    var world = new GameObject("World");
                    var grid = Child("Grid", world.transform); grid.AddComponent<Grid>();
                    string[] names = { "Ground", "GroundDetails", "Paths", "GroundOverlay", "Terrain", "TerrainDetails", "Foreground" };
                    int[] orders = { -10, -9, -8, -7, 0, 1, 1000 };
                    var maps = new Tilemap[names.Length];
                    for (int i = 0; i < names.Length; i++)
                    {
                        var obj = Child(names[i], grid.transform); maps[i] = obj.AddComponent<Tilemap>();
                        var renderer = obj.AddComponent<TilemapRenderer>(); renderer.sortingOrder = orders[i];
                        renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/World/Sanctuary/Art/SpriteUnlit.mat");
                        if (i == 4) obj.AddComponent<TilemapCollider2D>();
                    }
                    for (int y = -5; y <= 5; y++) for (int x = -7; x <= 7; x++)
                        maps[0].SetTile(new Vector3Int(x, y, 0), floors[((x + 7) / 3 + (y + 5) / 3) % 3]);
                    for (int y = 0; y < 3; y++) for (int x = -8; x <= 8; x++)
                        maps[4].SetTile(new Vector3Int(x, 6 + y, 0), walls[(x + 9) % 3, y]);
                    for (int y = -6; y <= 5; y++)
                        foreach (int x in new[] { -8, 8 }) maps[4].SetTile(new Vector3Int(x, y, 0), walls[1, 0]);
                    for (int x = -7; x <= 7; x++) if (Math.Abs(x) > 1) maps[4].SetTile(new Vector3Int(x, -6, 0), walls[1, 0]);
                    var props = Child("Props", world.transform); Child("Items", world.transform);
                    var transitions = Child("Transitions", world.transform);
                    Entrance(transitions.transform, "From sanctuary", "cave-entry", new Vector3(0, -2, 0));
                    Portal(transitions.transform, "Return to sanctuary", SanctuaryAuthoring.ScenePath, "sanctuary-return", new Vector3(0, -5, 0));
                    var desk = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/World/Sanctuary/Prefabs/SaveBookDesk.prefab"), props.transform);
                    desk.transform.position = new Vector3(3, 0, 0);
                    var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/World/Sanctuary/Prefabs/Player.prefab"));
                    player.transform.position = new Vector3(0, -2, 0);
                    var cameraObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(CameraFollow)); cameraObj.tag = "MainCamera";
                    cameraObj.transform.position = new Vector3(0, -2, -10);
                    var camera = cameraObj.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 5;
                    camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.035f, .03f, .025f);
                    cameraObj.GetComponent<CameraFollow>().Target = player.transform;
                    if (!EditorSceneManager.SaveScene(scene, Cave)) throw new IOException("Höhle konnte nicht gespeichert werden.");
                }
                finally { EditorSceneManager.CloseScene(scene, true); }
            }
            var sanctuary = SceneManager.GetSceneByPath(SanctuaryAuthoring.ScenePath);
            bool wasOpen = sanctuary.IsValid() && sanctuary.isLoaded;
            if (!wasOpen) sanctuary = EditorSceneManager.OpenScene(SanctuaryAuthoring.ScenePath, OpenSceneMode.Additive);
            try
            {
                var world = sanctuary.GetRootGameObjects().Single(obj => obj.name == "World");
                var transitions = world.transform.Find("Transitions");
                if (transitions == null) transitions = Child("Transitions", world.transform).transform;
                Entrance(transitions, "From cave", "sanctuary-return", new Vector3(.5f, 9, 0));
                Portal(transitions, "Enter cave", Cave, "cave-entry", new Vector3(.5f, 13, 0), "forest-sanctuary-source");
                EditorSceneManager.MarkSceneDirty(sanctuary); EditorSceneManager.SaveScene(sanctuary);
            }
            finally { if (!wasOpen) EditorSceneManager.CloseScene(sanctuary, true); if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous); }
            var builds = EditorBuildSettings.scenes.ToList();
            foreach (string path in new[] { SanctuaryAuthoring.ScenePath, Cave })
            { var existing = builds.FindIndex(s => s.path == path); if (existing >= 0) builds[existing] = new EditorBuildSettingsScene(path, true); else builds.Add(new EditorBuildSettingsScene(path, true)); }
            EditorBuildSettings.scenes = builds.ToArray(); AssetDatabase.SaveAssets();
            var check = EditorSceneManager.OpenScene(Cave, OpenSceneMode.Additive);
            try
            {
                var roots = check.GetRootGameObjects();
                if (roots.SelectMany(o => o.GetComponentsInChildren<Tilemap>()).Count() != 7 || roots.SelectMany(o => o.GetComponentsInChildren<AreaEntrance>()).Count() != 1
                    || roots.SelectMany(o => o.GetComponentsInChildren<AreaPortal>()).Count() != 1 || roots.SelectMany(o => o.GetComponentsInChildren<CharacterInventory>()).Count() != 1)
                    throw new Exception("Höhlenstruktur ungültig.");
            }
            finally { EditorSceneManager.CloseScene(check, true); }
            File.WriteAllText("Temp/AreaTransitionReport.txt", "PASS: native Höhlenszene gespeichert und wieder geöffnet; sieben Tilemaps, Spieler, Eintrittspunkt, Portal; Waldübergang ergänzt und beide Szenen im Build. Vorhandene Wald-Tiles nicht verändert.");
        }
        private static SpriteRect Slice(string name, int x, int y) => new SpriteRect { name = name, rect = new Rect(x, y, 32, 32), alignment = SpriteAlignment.Center, pivot = Vector2.one * .5f, spriteID = GUID.Generate() };
        private static Tile Tile(string name, Sprite[] sprites, bool solid)
        {
            string path = Root + "/Tiles/" + name + ".asset"; var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile != null) return tile;
            tile = ScriptableObject.CreateInstance<Tile>(); tile.sprite = sprites.Single(s => s.name == name);
            tile.colliderType = solid ? UnityEngine.Tilemaps.Tile.ColliderType.Grid : UnityEngine.Tilemaps.Tile.ColliderType.None;
            AssetDatabase.CreateAsset(tile, path); return tile;
        }
        private static GameObject Child(string name, Transform parent) { var obj = new GameObject(name); obj.transform.SetParent(parent, false); return obj; }
        private static void Entrance(Transform parent, string name, string id, Vector3 position)
        {
            if (parent.GetComponentsInChildren<AreaEntrance>().Any(e => e.EntranceId == id)) return;
            var obj = Child(name, parent); obj.transform.position = position; obj.AddComponent<AreaEntrance>().Configure(id);
        }
        private static void Portal(Transform parent, string name, string target, string entrance, Vector3 position, string puzzle = null)
        {
            if (parent.GetComponentsInChildren<AreaPortal>().Any(p => p.TargetScenePath == target)) return;
            string prefabPath = Root + "/Prefabs/" + name.Replace(" ", "") + ".prefab";
            var template = new GameObject(name); template.AddComponent<BoxCollider2D>().isTrigger = true;
            template.GetComponent<BoxCollider2D>().size = new Vector2(2.5f, 1);
            template.AddComponent<AreaPortal>().Configure(target, entrance, puzzle);
            var prefab = PrefabUtility.SaveAsPrefabAsset(template, prefabPath); UnityEngine.Object.DestroyImmediate(template);
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent); obj.transform.position = position;
        }
    }
}
