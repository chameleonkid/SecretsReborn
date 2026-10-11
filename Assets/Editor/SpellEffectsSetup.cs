using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SecretsReborn.Editor
{
    [InitializeOnLoad]
    public static class SpellEffectsSetup
    {
        static SpellEffectsSetup() => EditorApplication.update += Requested;
        private static void Requested()
        {
            const string request = "Temp/ConfigureSpellEffects.request";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            File.Delete(request);
            try { Configure(); }
            catch (Exception e) { File.WriteAllText("Temp/SpellEffectsReport.txt", "FAIL: " + e); Debug.LogException(e); }
        }
        [MenuItem("SecretsReborn/Magic/Configure spell effect sprites")]
        public static void Configure()
        {
            var files = Directory.GetFiles("Assets/Resources/Magic/Effects", "*.png", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                string path = file.Replace('\\', '/');
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
                if (AssetDatabase.LoadAssetAtPath<Sprite>(path) == null) throw new InvalidOperationException("Missing sprite: " + path);
            }
            if (files.Length != 38) throw new InvalidOperationException("Expected 38 effect frames, got " + files.Length);
            File.WriteAllText("Temp/SpellEffectsReport.txt", "PASS: 38 native sprite imports (2 flying, 11 hit, 25 heal). ");
        }
    }
}
