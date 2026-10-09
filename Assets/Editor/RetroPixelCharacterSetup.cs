using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace SecretsReborn.Editor
{
    [InitializeOnLoad] public static class RetroPixelCharacterSetup
    {
        static RetroPixelCharacterSetup() => EditorApplication.update += Requested;
        private static void Requested()
        {
            const string request = "Temp/ImportRetroPixelCharacters.request";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            File.Delete(request);
            try { Import(); } catch (Exception e) { File.WriteAllText("Temp/RetroPixelCharactersReport.txt", "FAIL: " + e); Debug.LogException(e); }
        }
        [MenuItem("SecretsReborn/Assets/Import RetroPixel character layers")]
        public static void Import()
        {
            const string art = "Assets/Art/RetroPixelCharacters", looks = "Assets/Resources/CharacterLooks", prefabs = "Assets/World/CharacterLooks/Prefabs";
            Directory.CreateDirectory(looks); Directory.CreateDirectory(prefabs); AssetDatabase.Refresh();
            var material = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/World/Sanctuary/Prefabs/Player.prefab").GetComponent<SpriteRenderer>().sharedMaterial;
            int count = 0;
            foreach (string file in Directory.GetFiles(art, "*.png").OrderBy(p => p))
            {
                string path = file.Replace('\\','/'), id = Path.GetFileNameWithoutExtension(file);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture.width != 512 || texture.height != 256) throw new Exception("Unexpected character sheet layout: " + path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                var factories = new SpriteDataProviderFactories(); factories.Init();
                var provider = factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
                var existing = provider.GetSpriteRects();
                var rects = new SpriteRect[128];
                for (int i = 0; i < 128; i++)
                {
                    var cell = new Rect(i % 16 * 32, 224 - i / 16 * 32, 32, 32);
                    // Preserve existing sprite identity by its cell, including automatic tight slices.
                    var previous = existing.FirstOrDefault(r => cell.Contains(r.rect.center));
                    rects[i] = new SpriteRect { name = "Frame_" + i.ToString("D3"), rect = cell,
                        alignment = SpriteAlignment.BottomCenter, pivot = new Vector2(.5f,0),
                        spriteID = previous != null ? previous.spriteID : GUID.Generate() };
                }
                SanctuaryAuthoring.Slice(path, rects,24);
                var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
                if (sprites.Length != 128 || sprites.Where((s,i) => s.name != "Frame_" + i.ToString("D3") || s.rect != rects[i].rect
                    || s.pivot != new Vector2(16,0)).Any() || importer.filterMode != FilterMode.Point
                    || importer.textureCompression != TextureImporterCompression.Uncompressed || importer.mipmapEnabled)
                    throw new Exception("Invalid pixel-art import or frame layout: " + id);
                string assetPath = looks + "/" + id + ".asset";
                var look = AssetDatabase.LoadAssetAtPath<ClothingAppearance>(assetPath);
                if (look == null) { look = ScriptableObject.CreateInstance<ClothingAppearance>(); AssetDatabase.CreateAsset(look,assetPath); }
                look.Configure(id, sprites); EditorUtility.SetDirty(look);
                string prefabPath = prefabs + "/" + id + ".prefab";
                if (!File.Exists(prefabPath))
                {
                    var layer = new GameObject(id, typeof(SpriteRenderer));
                    try { var renderer = layer.GetComponent<SpriteRenderer>(); renderer.sprite = sprites[0]; renderer.sharedMaterial = material;
                        PrefabUtility.SaveAsPrefabAsset(layer, prefabPath); }
                    finally { UnityEngine.Object.DestroyImmediate(layer); }
                }
                else
                {
                    var layer = PrefabUtility.LoadPrefabContents(prefabPath);
                    try { layer.GetComponent<SpriteRenderer>().sprite = sprites[0]; PrefabUtility.SaveAsPrefabAsset(layer,prefabPath); }
                    finally { PrefabUtility.UnloadPrefabContents(layer); }
                }
                count++;
            }
            // Paired outfit assets keep one equipment item usable for both body types.
            foreach (string assetPath in Directory.GetFiles(looks,"female-3-outfits*.asset"))
            {
                var female = AssetDatabase.LoadAssetAtPath<ClothingAppearance>(assetPath.Replace('\\','/'));
                string maleId = female.AppearanceId.Replace("female", "male");
                female.SetMaleVariant(AssetDatabase.LoadAssetAtPath<ClothingAppearance>(looks + "/" + maleId + ".asset")); EditorUtility.SetDirty(female);
            }
            var maleRanger = AssetDatabase.LoadAssetAtPath<ClothingAppearance>(looks + "/male-3-outfits-rpc-male-ranger-clothes.asset");
            foreach (string existing in new[] { "Assets/World/Sanctuary/Art/RangerAppearance.asset", "Assets/World/Sanctuary/Items/TrainingArmorAppearance.asset" })
            { var look = AssetDatabase.LoadAssetAtPath<ClothingAppearance>(existing); look.SetMaleVariant(maleRanger); EditorUtility.SetDirty(look); }
            AssetDatabase.SaveAssets();
            File.WriteAllText("Temp/RetroPixelCharactersReport.txt", "PASS: " + count + " RetroPixel sheets, each 128 synchronized frames at 24 PPU; appearance assets and visual layer prefabs; paired equipment appearances. Authored scenes unchanged.");
        }
    }
}
