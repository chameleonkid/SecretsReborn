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
    public static class SanctuaryVisualUpgrade
    {
        private const string Root = "Assets/World/Sanctuary";
        private const string Request = "Temp/UpgradeSanctuaryVisuals.request";

        static SanctuaryVisualUpgrade()
        {
            if (File.Exists(Request)) EditorApplication.update += Requested;
        }

        private static void Requested()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            // Do not overwrite an in-memory scene containing unsaved user changes.
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.path == SanctuaryAuthoring.ScenePath && scene.isDirty) return;
            }
            EditorApplication.update -= Requested;
            File.Delete(Request);
            try { Upgrade(); }
            catch (Exception error) { Debug.LogException(error); }
        }

        [MenuItem("SecretsReborn/World/Upgrade character and ground")]
        public static void Upgrade()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play zuerst beenden.");
            var scene = SceneManager.GetSceneByPath(SanctuaryAuthoring.ScenePath);
            bool alreadyOpen = scene.IsValid() && scene.isLoaded;
            if (alreadyOpen && scene.isDirty) throw new InvalidOperationException("Änderungen an der Szene zuerst speichern.");
            var previous = SceneManager.GetActiveScene();
            var bodyFrames = Frames("docs/asset-review/character.png", Root + "/Art/CharacterBase.png");
            var armorFrames = Frames("Assets/Art/Proposals/ranger-outfit.png", Root + "/Art/RangerOutfit.png");
            var outfit = AssetDatabase.LoadAssetAtPath<ClothingAppearance>(Root + "/Art/RangerAppearance.asset");
            if (outfit == null)
            {
                outfit = ScriptableObject.CreateInstance<ClothingAppearance>();
                outfit.Configure("ranger", armorFrames);
                AssetDatabase.CreateAsset(outfit, Root + "/Art/RangerAppearance.asset");
            }
            UpgradePlayer(bodyFrames, outfit);
            var tiles = GrassTiles();
            if (!alreadyOpen) scene = EditorSceneManager.OpenScene(SanctuaryAuthoring.ScenePath, OpenSceneMode.Additive);
            try
            {
                var ground = scene.GetRootGameObjects().SelectMany(obj => obj.GetComponentsInChildren<Tilemap>())
                    .Single(map => map.name == "Ground");
                int changed = VaryGround(ground, tiles, 1729);
                EditorSceneManager.MarkSceneDirty(scene);
                AssetDatabase.SaveAssets();
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Szene konnte nicht gespeichert werden.");
                File.WriteAllText("Temp/SanctuaryVisualReport.txt",
                    "PASS: 128 Körperframes und 128 Kleidungsframes; Player-Prefab gespeichert; " + changed
                    + " vorhandene Gras-Zellen variiert. Seed 1729, 2x2-Motive, keine Kartenform geändert.");
                Debug.Log("Spieler und Boden aktualisiert. " + changed + " Gras-Zellen gespeichert.");
            }
            finally
            {
                if (!alreadyOpen) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        private static Sprite[] Frames(string source, string target)
        {
            if (!File.Exists(target))
            {
                File.Copy(source, target);
                AssetDatabase.ImportAsset(target);
                var rects = new SpriteRect[128];
                for (int i = 0; i < rects.Length; i++) rects[i] = new SpriteRect {
                    name = "Frame_" + i.ToString("D3"), rect = new Rect((i % 16) * 32, 224 - (i / 16) * 32, 32, 32),
                    alignment = SpriteAlignment.BottomCenter, pivot = new Vector2(0.5f, 0), spriteID = GUID.Generate() };
                SanctuaryAuthoring.Slice(target, rects);
            }
            var frames = AssetDatabase.LoadAllAssetsAtPath(target).OfType<Sprite>().OrderBy(sprite => sprite.name).ToArray();
            if (frames.Length != 128) throw new InvalidOperationException("128 Animationsframes erwartet: " + target);
            return frames;
        }

        private static void UpgradePlayer(Sprite[] frames, ClothingAppearance outfit)
        {
            string path = Root + "/Prefabs/Player.prefab";
            var player = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var renderer = player.GetComponent<SpriteRenderer>();
                renderer.drawMode = SpriteDrawMode.Simple;
                renderer.sprite = frames[0];
                renderer.color = Color.white;
                player.transform.localScale = Vector3.one;
                var collider = player.GetComponent<BoxCollider2D>();
                collider.size = new Vector2(0.4f, 0.45f);
                collider.offset = new Vector2(0, 0.3f);
                var clothingTransform = player.transform.Find("Clothing");
                if (clothingTransform == null)
                {
                    clothingTransform = new GameObject("Clothing", typeof(SpriteRenderer)).transform;
                    clothingTransform.SetParent(player.transform, false);
                }
                var clothing = clothingTransform.GetComponent<SpriteRenderer>();
                clothing.sprite = outfit.Frame(0);
                clothing.sharedMaterial = renderer.sharedMaterial;
                clothing.sortingOrder = renderer.sortingOrder + 1;
                var appearance = player.GetComponent<CharacterAppearance>();
                if (appearance == null) appearance = player.AddComponent<CharacterAppearance>();
                appearance.Configure(renderer, clothing, frames, outfit);
                var marker = player.transform.Find("Lantern glow");
                if (marker != null) marker.localPosition = new Vector3(0.3f, 0.45f, 0);
                PrefabUtility.SaveAsPrefabAsset(player, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(player); }
        }

        private static Tile[] GrassTiles()
        {
            string source = Root + "/Art/Terrain.png";
            string target = Root + "/Art/GrassVariants.png";
            if (!File.Exists(target))
            {
                File.Copy(source, target);
                AssetDatabase.ImportAsset(target);
                // Adjacent pieces from the same grass patch: no edges, rocks or mismatched biomes.
                var rects = new SpriteRect[12];
                var origins = new[] { new Vector2Int(128, 448), new Vector2Int(160, 480), new Vector2Int(192, 448) };
                var decoded = new Texture2D(2, 2);
                try
                {
                    decoded.LoadImage(File.ReadAllBytes(source));
                    for (int motif = 0; motif < 3; motif++)
                        for (int quadrant = 0; quadrant < 4; quadrant++)
                        {
                            int x = origins[motif].x + quadrant % 2 * 32;
                            int y = origins[motif].y + quadrant / 2 * 32;
                            if (decoded.GetPixels(x, y, 32, 32).Any(pixel => pixel.a < 0.99f))
                                throw new InvalidOperationException("Gras-Ausschnitt enthält transparente Kanten.");
                            int index = motif * 4 + quadrant;
                            rects[index] = new SpriteRect { name = "GrassPatch_" + index.ToString("D2"),
                                rect = new Rect(x, y, 32, 32), alignment = SpriteAlignment.Center,
                                pivot = Vector2.one * 0.5f, spriteID = GUID.Generate() };
                        }
                }
                finally { Object.DestroyImmediate(decoded); }
                SanctuaryAuthoring.Slice(target, rects);
            }
            var sprites = AssetDatabase.LoadAllAssetsAtPath(target).OfType<Sprite>().OrderBy(sprite => sprite.name).ToArray();
            if (sprites.Length != 12) throw new InvalidOperationException("Zwölf Grasvarianten erwartet.");
            var tiles = new Tile[12];
            for (int i = 0; i < tiles.Length; i++)
            {
                string path = Root + "/Tiles/GrassPatch_" + i.ToString("D2") + ".asset";
                tiles[i] = AssetDatabase.LoadAssetAtPath<Tile>(path);
                if (tiles[i] != null) continue;
                tiles[i] = ScriptableObject.CreateInstance<Tile>();
                tiles[i].sprite = sprites[i];
                tiles[i].colliderType = Tile.ColliderType.None;
                AssetDatabase.CreateAsset(tiles[i], path);
            }
            return tiles;
        }

        private static int VaryGround(Tilemap ground, Tile[] variants, int seed)
        {
            var original = AssetDatabase.LoadAssetAtPath<Tile>(Root + "/Tiles/Grass.asset");
            int count = 0;
            foreach (var cell in ground.cellBounds.allPositionsWithin)
            {
                var existing = ground.GetTile(cell);
                if (existing != original && !variants.Contains(existing)) continue;
                // A deterministic motif per 2x2 block preserves the source's adjoining pattern.
                ground.SetTile(cell, variants[GroundMotifPattern.TileIndex(cell.x, cell.y, seed)]);
                count++;
            }
            return count;
        }
    }
}
