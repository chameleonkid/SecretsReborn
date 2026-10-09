using System.IO;
using UnityEditor;
using UnityEngine;
namespace SecretsReborn.Editor
{
    [InitializeOnLoad] public static class MenuArtworkSetup
    {
        static MenuArtworkSetup() => EditorApplication.update += Requested;
        private static void Requested()
        {
            const string request = "Temp/SetupMenuArtwork.request";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            File.Delete(request);
            var importer = AssetImporter.GetAtPath("Assets/Resources/MenuUI/ForestMenuBackdrop.png") as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Default; importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048; importer.SaveAndReimport();
            File.WriteAllText("Temp/MenuArtworkReport.txt","PASS: generated backdrop imported with point filtering, no mipmaps, no texture compression.");
        }
    }
}
