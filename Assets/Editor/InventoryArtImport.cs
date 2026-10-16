using UnityEditor;
using UnityEngine;

namespace SecretsReborn.Editor
{
    public sealed class InventoryArtImport : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/InventoryUI/")) return;
            var importer = (TextureImporter)assetImporter;
            bool backplate=assetPath.StartsWith("Assets/Resources/InventoryUI/SlateGold/");
            importer.textureType = backplate ? TextureImporterType.Sprite : TextureImporterType.Default;
            if(backplate) importer.spriteImportMode=SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = backplate ? FilterMode.Bilinear : FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.npotScale = TextureImporterNPOTScale.None;
        }
    }
}
