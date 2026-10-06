using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsReborn.Editor
{
    public static class SessionSceneCheck
    {
        [MenuItem("SecretsReborn/Session/Reload active scene in Play")]
        public static void Reload()
        {
            if (!EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode && !Application.isPlaying) return;
            var scene = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(scene.path)) { Debug.LogError("Für diesen Test eine gespeicherte Szene öffnen."); return; }
            // Reload the runtime scene from disk without changing its saved authoring contents.
            var expected = GameSession.Instance.World;
            EditorSceneManager.LoadSceneInPlayMode(scene.path, new LoadSceneParameters(LoadSceneMode.Single));
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlaying) return;
                if (!ReferenceEquals(expected, GameSession.Instance.World)) Debug.LogError("Szenenwechsel hat den Sitzungszustand ersetzt.");
                else Debug.Log("PASS: Host-Sitzung nach Szenen-Neuladen erhalten. Inventar, Ausrüstung, Pickup und Runen im Spiel prüfen.");
            };
        }
        [MenuItem("SecretsReborn/Session/Reload active scene in Play", true)]
        private static bool CanReload() => EditorApplication.isPlaying && !EditorApplication.isCompiling;
    }
}
