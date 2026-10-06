using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace SecretsReborn.Editor
{
    [InitializeOnLoad]
    public static class SanctuaryLandscapeUpgrade
    {
        private const string Root = "Assets/World/Sanctuary";
        private const string Request = "Temp/UpgradeSanctuaryLandscape.request";

        static SanctuaryLandscapeUpgrade()
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

        [MenuItem("SecretsReborn/World/Upgrade cliffs trees and stairs")]
        public static void Upgrade()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play zuerst beenden.");
            var scene = SceneManager.GetSceneByPath(SanctuaryAuthoring.ScenePath);
            bool open = scene.IsValid() && scene.isLoaded;
            if (open && scene.isDirty) throw new InvalidOperationException("Szenenänderungen zuerst speichern.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Art/SpriteUnlit.mat");
            var cliffs = CliffTiles();
            var previous = SceneManager.GetActiveScene();
            if (!open) scene = EditorSceneManager.OpenScene(SanctuaryAuthoring.ScenePath, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var world = scene.GetRootGameObjects().Single(obj => obj.name == "World");
                var terrain = world.transform.Find("Grid/Terrain").GetComponent<Tilemap>();
                var oldStone = AssetDatabase.LoadAssetAtPath<Tile>(Root + "/Tiles/Stone.asset");
                foreach (var cell in terrain.cellBounds.allPositionsWithin)
                {
                    var tile = terrain.GetTile(cell);
                    if (tile == oldStone || (tile != null && tile.name.StartsWith("Cliff_"))) terrain.SetTile(cell, null);
                }
                // Horizontal front faces: three source rows, kept in their original vertical order.
                Horizontal(terrain, cliffs, -15, -2, 10);
                Horizontal(terrain, cliffs, 2, 14, 10);
                Horizontal(terrain, cliffs, -15, 14, -13);
                for (int y = -10; y < 10; y++)
                    for (int col = 0; col < 2; col++)
                    {
                        terrain.SetTile(new Vector3Int(-15 + col, y, 0), cliffs[9 + col]);
                        terrain.SetTile(new Vector3Int(13 + col, y, 0), cliffs[11 + col]);
                    }
                Horizontal(terrain, cliffs, -2, 2, 14);

                var trees = new[] {
                    Tree("CanopyTree", "tree-canopy", new Rect(0, 160, 160, 160), material),
                    Tree("TieredTree", "tree-tiered", new Rect(0, 352, 128, 160), material),
                    Tree("TealTree", "tree-crown", new Rect(0, 352, 128, 160), material) };
                var props = world.transform.Find("Props");
                var oldTrees = props.Cast<Transform>().Where(obj => obj.GetComponent<SpriteDepth>() != null
                    && obj.GetComponent<BoxCollider2D>() != null).ToArray();
                for (int i = 0; i < oldTrees.Length; i++)
                {
                    var position = oldTrees[i].localPosition;
                    var rotation = oldTrees[i].localRotation;
                    var replacement = (GameObject)PrefabUtility.InstantiatePrefab(trees[i % trees.Length], props);
                    replacement.transform.localPosition = position;
                    replacement.transform.localRotation = rotation;
                    Object.DestroyImmediate(oldTrees[i].gameObject);
                }

                var stairsPrefab = Stairs(material);
                var sanctuary = world.transform.Find("Sanctuary");
                if (sanctuary.Find("Exit stairs") == null)
                {
                    var stairs = (GameObject)PrefabUtility.InstantiatePrefab(stairsPrefab, sanctuary);
                    stairs.name = "Exit stairs";
                    stairs.transform.localPosition = new Vector3(0.5f, 11, 0);
                }
                // The existing puzzle-controlled gate remains the only blocker across the stair lane.
                var puzzle = world.transform.Find("Puzzle").GetComponent<SanctuaryPuzzle>();
                var gate = new SerializedObject(puzzle).FindProperty("gate").objectReferenceValue as GameObject;
                if (gate == null) throw new InvalidOperationException("Rätseldurchgang fehlt.");
                if (terrain.HasTile(new Vector3Int(0, 10, 0)) || terrain.HasTile(new Vector3Int(0, 12, 0)))
                    throw new InvalidOperationException("Treppe durch Terrain blockiert.");
                if (oldTrees.Length < 3) Debug.LogWarning("Weniger als drei vorhandene Bäume; Varianten bleiben als Prefabs verfügbar.");
                EditorSceneManager.MarkSceneDirty(scene);
                AssetDatabase.SaveAssets();
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Speichern fehlgeschlagen.");
                File.WriteAllText("Temp/SanctuaryLandscapeReport.txt", "PASS: 13 Cliff-Tiles, dreizeilige Frontwände, zweispaltige Seiten; "
                    + oldTrees.Length + " Bäume ersetzt; ExitStairs-Prefab mit seitlichen Kollisionen; Rätseltor referenziert, Treppenmitte frei.");
                Debug.Log("Waldheiligtum: mehrteilige Felswände, Baumvarianten und Ausgangstreppe gespeichert.");
            }
            finally
            {
                if (!open) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        private static SpriteRect SpriteRect(string name, Rect rect, Vector2 pivot) => new SpriteRect {
            name = name, rect = rect, alignment = SpriteAlignment.Custom, pivot = pivot, spriteID = GUID.Generate() };

        private static Tile[] CliffTiles()
        {
            string path = Root + "/Art/Cliffs.png";
            if (!File.Exists(path))
            {
                File.Copy(Root + "/Art/Terrain.png", path);
                AssetDatabase.ImportAsset(path);
                var rects = new SpriteRect[14];
                for (int row = 0; row < 3; row++)
                    for (int col = 0; col < 3; col++)
                        rects[row * 3 + col] = SpriteRect("Cliff_" + (row * 3 + col).ToString("D2"),
                            new Rect(128 + col * 32, 1696 + row * 32, 32, 32), Vector2.one * 0.5f);
                for (int col = 0; col < 2; col++)
                {
                    rects[9 + col] = SpriteRect("Cliff_" + (9 + col).ToString("D2"), new Rect(col * 32, 1824, 32, 32), Vector2.one * 0.5f);
                    rects[11 + col] = SpriteRect("Cliff_" + (11 + col).ToString("D2"), new Rect(320 + col * 32, 1824, 32, 32), Vector2.one * 0.5f);
                }
                rects[13] = SpriteRect("ExitStairs", new Rect(96, 928, 128, 160), new Vector2(0.5f, 0.5f));
                SanctuaryAuthoring.Slice(path, rects);
            }
            var tiles = new Tile[13];
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            for (int i = 0; i < tiles.Length; i++)
            {
                string name = "Cliff_" + i.ToString("D2"), assetPath = Root + "/Tiles/" + name + ".asset";
                tiles[i] = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
                if (tiles[i] != null) continue;
                tiles[i] = ScriptableObject.CreateInstance<Tile>();
                tiles[i].sprite = sprites.Single(sprite => sprite.name == name);
                tiles[i].colliderType = Tile.ColliderType.Grid;
                AssetDatabase.CreateAsset(tiles[i], assetPath);
            }
            return tiles;
        }

        private static void Horizontal(Tilemap map, Tile[] tiles, int first, int last, int bottom)
        {
            for (int x = first; x <= last; x++)
                for (int row = 0; row < 3; row++)
                    map.SetTile(new Vector3Int(x, bottom + row, 0), tiles[row * 3 + ((x - first) % 3)]);
        }

        private static GameObject Tree(string name, string sourceName, Rect frame, Material material)
        {
            string path = Root + "/Prefabs/" + name + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            string artPath = Root + "/Art/" + name + ".png";
            if (!File.Exists(artPath))
            {
                File.Copy("Assets/Art/Proposals/" + sourceName + ".png", artPath);
                AssetDatabase.ImportAsset(artPath);
                SanctuaryAuthoring.Slice(artPath, new[] { SpriteRect(name, frame, new Vector2(0.5f, 0.18f)) });
            }
            var obj = new GameObject(name, typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(SpriteDepth));
            try
            {
                var renderer = obj.GetComponent<SpriteRenderer>();
                renderer.sprite = AssetDatabase.LoadAllAssetsAtPath(artPath).OfType<Sprite>().Single();
                renderer.sharedMaterial = material;
                renderer.drawMode = SpriteDrawMode.Simple;
                obj.transform.localScale = Vector3.one;
                var collider = obj.GetComponent<BoxCollider2D>();
                collider.size = new Vector2(0.7f, 0.7f);
                collider.offset = Vector2.zero;
                return PrefabUtility.SaveAsPrefabAsset(obj, path);
            }
            finally { Object.DestroyImmediate(obj); }
        }

        private static GameObject Stairs(Material material)
        {
            string path = Root + "/Prefabs/ExitStairs.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            var obj = new GameObject("Exit stairs", typeof(SpriteRenderer));
            try
            {
                var renderer = obj.GetComponent<SpriteRenderer>();
                renderer.sprite = AssetDatabase.LoadAllAssetsAtPath(Root + "/Art/Cliffs.png").OfType<Sprite>().Single(sprite => sprite.name == "ExitStairs");
                renderer.sharedMaterial = material;
                renderer.sortingOrder = -5;
                foreach (float x in new[] { -1.8f, 1.8f })
                {
                    var rail = new GameObject("Stone side");
                    rail.transform.SetParent(obj.transform, false);
                    rail.transform.localPosition = new Vector3(x, 0, 0);
                    rail.AddComponent<BoxCollider2D>().size = new Vector2(0.4f, 5f);
                }
                return PrefabUtility.SaveAsPrefabAsset(obj, path);
            }
            finally { Object.DestroyImmediate(obj); }
        }
    }
}
