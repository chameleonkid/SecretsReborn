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
    public static class SanctuaryLayerUpgrade
    {
        private const string Root = "Assets/World/Sanctuary";
        private const string Request = "Temp/UpgradeSanctuaryLayers.request";
        static SanctuaryLayerUpgrade()
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
            try { Upgrade(); } catch (Exception error) { Debug.LogException(error); }
        }

        [MenuItem("SecretsReborn/World/Add terrain authoring layers")]
        public static void Upgrade()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play zuerst beenden.");
            var scene = SceneManager.GetSceneByPath(SanctuaryAuthoring.ScenePath);
            bool open = scene.IsValid() && scene.isLoaded;
            if (open && scene.isDirty) throw new InvalidOperationException("Szene zuerst speichern.");
            var previous = SceneManager.GetActiveScene();
            if (!open) scene = EditorSceneManager.OpenScene(SanctuaryAuthoring.ScenePath, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var world = scene.GetRootGameObjects().Single(obj => obj.name == "World");
                var grid = world.transform.Find("Grid");
                var material = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Art/SpriteUnlit.mat");
                string[] names = { "Ground", "GroundDetails", "Paths", "GroundOverlay", "Terrain", "TerrainDetails", "Foreground" };
                int[] orders = { -10, -9, -8, -7, 0, 1, 1000 };
                for (int i = 0; i < names.Length; i++)
                {
                    var child = grid.Find(names[i]);
                    if (child == null)
                    {
                        child = new GameObject(names[i], typeof(Tilemap), typeof(TilemapRenderer)).transform;
                        child.SetParent(grid, false);
                    }
                    child.SetSiblingIndex(i);
                    var renderer = child.GetComponent<TilemapRenderer>();
                    renderer.sharedMaterial = material;
                    renderer.sortingOrder = orders[i];
                    if (names[i] == "Terrain" && child.GetComponent<TilemapCollider2D>() == null)
                        child.gameObject.AddComponent<TilemapCollider2D>();
                }
                var stairs = world.transform.Find("Sanctuary/Exit stairs");
                if (stairs != null)
                {
                    var renderer = stairs.GetComponent<SpriteRenderer>();
                    if (renderer == null || renderer.sprite == null || stairs.localScale != Vector3.one
                        || stairs.localRotation != Quaternion.identity)
                        throw new InvalidOperationException("Treppeninstanz verändert; Aufteilung benötigt eine unskalierte Treppe.");
                    var tiles = StairTiles();
                    var ground = grid.Find("GroundOverlay").GetComponent<Tilemap>();
                    var terrain = grid.Find("Terrain").GetComponent<Tilemap>();
                    var bottom = grid.GetComponent<Grid>().WorldToCell(renderer.bounds.min);
                    for (int row = 0; row < 5; row++)
                        for (int col = 0; col < 4; col++)
                        {
                            var cell = bottom + new Vector3Int(col, row, 0);
                            var map = col == 0 || col == 3 ? terrain : ground;
                            var existing = map.GetTile(cell);
                            if (existing != null && !(map == terrain && existing.name.StartsWith("Cliff_")))
                                throw new InvalidOperationException("Treppenbereich enthält bereits Tiles: " + cell);
                        }
                    for (int row = 0; row < 5; row++)
                        for (int col = 0; col < 4; col++)
                            (col == 0 || col == 3 ? terrain : ground).SetTile(bottom + new Vector3Int(col, row, 0), tiles[row * 4 + col]);
                    UnityEngine.Object.DestroyImmediate(stairs.gameObject);
                }
                var puzzle = world.transform.Find("Puzzle").GetComponent<SanctuaryPuzzle>();
                if (new SerializedObject(puzzle).FindProperty("gate").objectReferenceValue == null)
                    throw new InvalidOperationException("Rätseltor fehlt.");
                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Speichern fehlgeschlagen.");
                File.WriteAllText("Temp/SanctuaryLayerReport.txt", "PASS: sieben Tilemaps, sortierte Ebenen; Stufen auf GroundOverlay ohne Collider, Ränder auf Terrain mit Grid-Kollision; Rätseltor erhalten; Szene gespeichert.");
                Debug.Log("Waldheiligtum: sieben Geländeebenen und geteilte Treppe gespeichert.");
            }
            finally
            {
                if (!open) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        private static Tile[] StairTiles()
        {
            string path = Root + "/Art/StairTiles.png";
            if (!File.Exists(path))
            {
                File.Copy(Root + "/Art/Terrain.png", path);
                AssetDatabase.ImportAsset(path);
                var rects = new SpriteRect[20];
                for (int row = 0; row < 5; row++)
                    for (int col = 0; col < 4; col++)
                    {
                        int index = row * 4 + col;
                        rects[index] = new SpriteRect { name = "Stair_" + index.ToString("D2"),
                            rect = new Rect(96 + col * 32, 928 + row * 32, 32, 32),
                            alignment = SpriteAlignment.Center, pivot = Vector2.one * .5f, spriteID = GUID.Generate() };
                    }
                SanctuaryAuthoring.Slice(path, rects);
            }
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            var tiles = new Tile[20];
            for (int i = 0; i < tiles.Length; i++)
            {
                string name = "Stair_" + i.ToString("D2"), tilePath = Root + "/Tiles/" + name + ".asset";
                tiles[i] = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
                if (tiles[i] != null) continue;
                tiles[i] = ScriptableObject.CreateInstance<Tile>();
                tiles[i].sprite = sprites.Single(sprite => sprite.name == name);
                tiles[i].colliderType = i % 4 == 0 || i % 4 == 3 ? Tile.ColliderType.Grid : Tile.ColliderType.None;
                AssetDatabase.CreateAsset(tiles[i], tilePath);
            }
            return tiles;
        }
    }
}
