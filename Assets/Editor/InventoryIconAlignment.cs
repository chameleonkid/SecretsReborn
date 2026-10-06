using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SecretsReborn.Editor
{
    [InitializeOnLoad]
    public static class InventoryIconAlignment
    {
        private const string Request = "Temp/AlignInventoryIcons.request";
        static InventoryIconAlignment() { if (File.Exists(Request)) EditorApplication.update += Requested; }
        private static void Requested()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            EditorApplication.update -= Requested; File.Delete(Request);
            try { Align(); } catch (Exception error) { Debug.LogException(error); }
        }
        [MenuItem("SecretsReborn/Inventory/Align item icons")]
        public static void Align()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:ItemDefinition"))
            {
                var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (item.Icon == null) continue;
                string path = AssetDatabase.GetAssetPath(item.Icon.texture);
                if (!File.Exists(path)) continue;
                var texture = new Texture2D(2, 2);
                try
                {
                    if (!texture.LoadImage(File.ReadAllBytes(path))) continue;
                    var rect = item.Icon.rect;
                    int minX = (int)rect.width, minY = (int)rect.height, maxX = -1, maxY = -1;
                    for (int y = 0; y < (int)rect.height; y++)
                        for (int x = 0; x < (int)rect.width; x++)
                            if (texture.GetPixel((int)rect.x + x, (int)rect.y + y).a > .1f)
                            { minX = Math.Min(minX, x); minY = Math.Min(minY, y); maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y); }
                    if (maxX < minX) continue;
                    item.SetIconContent(new Rect(minX / rect.width, minY / rect.height, (maxX - minX + 1) / rect.width, (maxY - minY + 1) / rect.height));
                    EditorUtility.SetDirty(item);
                }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
            }
            AssetDatabase.SaveAssets();
            File.WriteAllText("Temp/InventoryIconAlignmentReport.txt", "PASS: Item-Icon-Inhalt anhand Alpha vermessen und zentrierbare UV-Bereiche gespeichert; ursprüngliche Texturen und Figurenframes erhalten.");
        }
    }
}
