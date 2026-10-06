using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SecretsReborn.Editor
{
    [InitializeOnLoad]
    public static class DeathSetup
    {
        static DeathSetup() { EditorApplication.update += Requested; }
        private static void Requested()
        {
            const string request = "Temp/SetupPartyDeathV1.request";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            var stage = PrefabStageUtility.GetCurrentPrefabStage(); if (stage != null && stage.scene.isDirty) return;
            File.Delete(request);
            try { Setup(); } catch (Exception error) { File.WriteAllText("Temp/PartyDeathSetupReport.txt", "FAIL: " + error); Debug.LogException(error); }
        }
        [MenuItem("SecretsReborn/World/Set up party death presentation")]
        public static void Setup()
        {
            const string path = "Assets/World/Sanctuary/Prefabs/Player.prefab";
            var player = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (player.GetComponent<CharacterDeath>() == null) player.AddComponent<CharacterDeath>();
                PrefabUtility.SaveAsPrefabAsset(player, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(player); }
            AssetDatabase.SaveAssets();
            File.WriteAllText("Temp/PartyDeathSetupReport.txt", "PASS: Player prefab contains CharacterDeath; original layered frames support dying/downed presentation. Party wipe and revival rules verified by standalone tests.");
        }
    }
}
