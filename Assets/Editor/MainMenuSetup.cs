using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsReborn.Editor
{
    [InitializeOnLoad] public static class MainMenuSetup
    {
        static MainMenuSetup() => EditorApplication.update += Requested;
        private static void Requested()
        {
            const string request = "Temp/SetupMainMenu.request";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            File.Delete(request);
            try { Setup(); File.WriteAllText("Temp/MainMenuSetupReport.txt", "PASS: MainMenu scene created with native Unity APIs. Adventure scenes untouched."); }
            catch (Exception e) { File.WriteAllText("Temp/MainMenuSetupReport.txt", "FAIL: " + e); Debug.LogException(e); }
        }
        [MenuItem("SecretsReborn/Coop/Prepare main menu and lobby")]
        public static void Setup()
        {
            if (!File.Exists(NetworkCoop.MenuScene))
            {
                var previous = SceneManager.GetActiveScene();
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                try
                {
                    var root = new GameObject("Main Menu"); root.AddComponent<MainMenu>();
                    var camera = new GameObject("Menu Camera").AddComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor;
                    camera.backgroundColor = new Color(.035f, .065f, .05f);
                    EditorSceneManager.SaveScene(scene, NetworkCoop.MenuScene);
                }
                finally { EditorSceneManager.CloseScene(scene, true); SceneManager.SetActiveScene(previous); }
            }
            EditorBuildSettings.scenes = new[] {
                new EditorBuildSettingsScene(NetworkCoop.MenuScene, true),
                new EditorBuildSettingsScene(NetworkCoop.FirstScene, true),
                new EditorBuildSettingsScene("Assets/Scenes/Raetselhoehle-Editable.unity", true) };
            AssetDatabase.SaveAssets();
        }
    }
}
