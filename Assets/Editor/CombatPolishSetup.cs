using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace SecretsReborn.Editor
{
    [InitializeOnLoad]
    public static class CombatPolishSetup
    {
        static CombatPolishSetup() { EditorApplication.update += Requested; }
        private static void Requested()
        {
            const string request = "Temp/CombatOriginalFrames.request";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.scene.isDirty) return;
            File.Delete(request);
            try { Setup(); } catch (Exception error) { File.WriteAllText("Temp/CombatPolishReport.txt", "FAIL: " + error); Debug.LogException(error); }
        }
        [MenuItem("SecretsReborn/Combat/Import synchronized player sword")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Play zuerst beenden.");
            const string path = "Assets/World/Combat/Art/PlayerSword.png";
            AssetDatabase.ImportAsset(path);
            ImportOriginalSwordFrames(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.spritePixelsPerUnit = 24; importer.SaveAndReimport();
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
            if (sprites.Length != 128 || sprites[68].rect.height != 49) throw new Exception("Original extended sword rectangles were not imported.");
            Directory.CreateDirectory("Assets/World/Combat/Prefabs"); AssetDatabase.Refresh();
            const string visualPath = "Assets/World/Combat/Prefabs/PlayerSwordVisual.prefab";
            var visual = AssetDatabase.LoadAssetAtPath<GameObject>(visualPath);
            if (visual == null)
            {
                var obj = new GameObject("Player sword visual", typeof(SpriteRenderer));
                try
                {
                    obj.GetComponent<SpriteRenderer>().sprite = sprites[67];
                    obj.GetComponent<SpriteRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/World/Sanctuary/Art/SpriteUnlit.mat");
                    visual = PrefabUtility.SaveAsPrefabAsset(obj, visualPath);
                }
                finally { UnityEngine.Object.DestroyImmediate(obj); }
            }
            RepairOakFrames();
            const string playerPath = "Assets/World/Sanctuary/Prefabs/Player.prefab";
            var player = PrefabUtility.LoadPrefabContents(playerPath);
            try { player.GetComponent<PlayerMelee>().ConfigureSword(sprites); player.GetComponent<PlayerMelee>().ConfigureSwordPrefab(visual); PrefabUtility.SaveAsPrefabAsset(player, playerPath); }
            finally { PrefabUtility.UnloadPrefabContents(player); }
            var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/World/Sanctuary/Items/training-sword.asset");
            item.SetEquipment(ItemKind.Weapon, false, sprites[66]); EditorUtility.SetDirty(item); AssetDatabase.SaveAssets();
            File.WriteAllText("Temp/CombatPolishReport.txt", "PASS: Original variable-size sword rectangles restored with stable GUIDs and player foot anchors; sword visual prefab assigned; oak direction rows match original LogWalk clips.");
        }
        private static void ImportOriginalSwordFrames(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var factories = new SpriteDataProviderFactories(); factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
            var existing = provider.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);
            string text = File.ReadAllText("docs/asset-review/player-sword.import.txt");
            const string pattern = @"name: rpc female sword_(\d+)\s+rect:\s+serializedVersion: \d+\s+x: ([\d.]+)\s+y: ([\d.]+)\s+width: ([\d.]+)\s+height: ([\d.]+)\s+alignment: \d+\s+pivot: \{x: ([\d.eE+-]+), y: ([\d.eE+-]+)\}";
            var matches = Regex.Matches(text, pattern);
            if (matches.Count != 128) throw new Exception("Original sword metadata must contain 128 frames.");
            var rects = new SpriteRect[128];
            foreach (Match match in matches)
            {
                int index = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                float[] values = Enumerable.Range(2, 6).Select(i => float.Parse(match.Groups[i].Value, CultureInfo.InvariantCulture)).ToArray();
                string name = "Sword" + index.ToString("D3");
                rects[index] = new SpriteRect { name = name, rect = new Rect(values[0], values[1], values[2], values[3]), alignment = SpriteAlignment.Custom,
                    // Match the exact player cell anchor, including oversized weapon frames.
                    pivot = new Vector2((index % 16 * 32 + 16 - values[0]) / values[2],
                        (224 - index / 16 * 32 - values[1]) / values[3]),
                    spriteID = existing.TryGetValue(name, out var id) ? id : GUID.Generate() };
            }
            SanctuaryAuthoring.Slice(path, rects);
        }
        private static void RepairOakFrames()
        {
            const string path = "Assets/World/Enemies/Art/OakEnemy.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var factories = new SpriteDataProviderFactories(); factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
            var rects = provider.GetSpriteRects();
            foreach (var frame in rects)
            {
                // Anchor each trimmed rect to the same point in its original 32px cell.
                float x = Mathf.Floor(frame.rect.center.x / 32) * 32 + 16;
                float y = Mathf.Floor(frame.rect.center.y / 32) * 32 + 8;
                frame.alignment = SpriteAlignment.Custom;
                frame.pivot = new Vector2((x - frame.rect.x) / frame.rect.width, (y - frame.rect.y) / frame.rect.height);
            }
            provider.SetSpriteRects(rects); provider.Apply(); importer.SaveAndReimport();
            var ordered = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
                .OrderByDescending(s => Mathf.Floor(s.rect.center.y / 32))
                .ThenBy(s => Mathf.Floor(s.rect.center.x / 32)).ToArray();
            if (ordered.Length != 24) throw new Exception("Expected 24 oak frames.");
            const string prefabPath = "Assets/World/Enemies/Prefabs/OakMeleeEnemy.prefab";
            var enemy = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var component = enemy.GetComponent<TreeMeleeEnemy>(); component.Configure(component.EnemyId, ordered);
                enemy.GetComponent<SpriteRenderer>().sprite = ordered[0];
                PrefabUtility.SaveAsPrefabAsset(enemy, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(enemy); }
        }
    }
}
