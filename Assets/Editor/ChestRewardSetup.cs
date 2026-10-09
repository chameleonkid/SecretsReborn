using System;
using System.IO;
using UnityEditor;

namespace SecretsReborn.Editor
{
    [InitializeOnLoad]
    public static class ChestRewardSetup
    {
        static ChestRewardSetup() => EditorApplication.update += Requested;
        private static void Requested()
        {
            const string request = "Temp/SetupChestRewardsV1.request";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            File.Delete(request);
            try { Setup(); } catch (Exception error) { File.WriteAllText("Temp/ChestRewardSetupReport.txt", "FAIL: " + error); }
        }
        [MenuItem("SecretsReborn/World/Normalize single item chest rewards")]
        public static void Setup()
        {
            int changed = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:LootTable", new[] { "Assets/World" }))
            {
                var table = AssetDatabase.LoadAssetAtPath<LootTable>(AssetDatabase.GUIDToAssetPath(guid));
                if (table.Source != LootSourceKind.Chest) continue;
                var entries = table.Entries;
                if (entries.Length == 0 || entries[0]?.item == null) throw new Exception("Truhe ohne Gegenstand: " + table.name);
                if (entries.Length == 1 && entries[0].count == 1) continue;
                table.Configure(LootSourceKind.Chest, new[] { new LootEntry { item = entries[0].item, count = 1 } });
                EditorUtility.SetDirty(table); changed++;
            }
            AssetDatabase.SaveAssets();
            File.WriteAllText("Temp/ChestRewardSetupReport.txt", "PASS: " + changed + " chest tables normalized to their first single item; scenes unchanged.");
        }
    }
}
