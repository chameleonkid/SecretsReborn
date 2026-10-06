using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SecretsReborn.Editor
{
    [InitializeOnLoad]
    public static class CoopSetup
    {
        static CoopSetup() => EditorApplication.update += Requested;
        private static void Requested()
        {
            const string request = "Temp/SetupLocalCoopV1.request";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            File.Delete(request);
            try { Setup(); } catch (Exception error) { File.WriteAllText("Temp/CoopSetupReport.txt", "FAIL: " + error); Debug.LogException(error); }
        }
        [MenuItem("SecretsReborn/Coop/Prepare local test")]
        public static void Setup()
        {
            Directory.CreateDirectory("Assets/Resources/Coop");
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/World/Sanctuary/Prefabs/Player.prefab");
                var player = (GameObject)PrefabUtility.InstantiatePrefab(source, scene);
                PrefabUtility.SaveAsPrefabAsset(player, "Assets/Resources/Coop/Player.prefab");
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
            AssetDatabase.SaveAssets();
            File.WriteAllText("Temp/CoopSetupReport.txt", "PASS: Standalone cooperative character prefab generated through Unity; authored scene and original Player prefab unchanged.");
        }
        [MenuItem("SecretsReborn/Coop/Build local Windows test player")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play zuerst beenden.");
            Setup(); Directory.CreateDirectory("Builds/LocalCoop");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Waldheiligtum-Editable.unity", "Assets/Scenes/Raetselhoehle-Editable.unity" },
                locationPathName = "Builds/LocalCoop/SecretsReborn.exe", target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            File.WriteAllText("Temp/CoopBuildReport.txt", report.summary.result + ": " + report.summary.totalErrors + " errors, " + report.summary.totalWarnings + " warnings");
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Koop-Testbuild fehlgeschlagen.");
        }
        [InitializeOnLoadMethod]
        private static void BuildRequest() => EditorApplication.update += () =>
        {
            const string request = "Temp/BuildLocalCoopV1.request";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            File.Delete(request);
            try { Build(); } catch (Exception error) { File.WriteAllText("Temp/CoopBuildReport.txt", "FAIL: " + error); Debug.LogException(error); }
        };
    }
}
