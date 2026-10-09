using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace SecretsReborn
{
    // Independent of GameSession so it can verify complete lobby teardown/recreation.
    public sealed class MenuRetryIntegrationDriver : MonoBehaviour
    {
        private string report;
        private bool finished;
        private float deadline;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Debug.isDebugBuild || Array.IndexOf(Environment.GetCommandLineArgs(), "--menu-retry-test") < 0) return;
            var obj = new GameObject("Menu retry integration check"); DontDestroyOnLoad(obj); obj.AddComponent<MenuRetryIntegrationDriver>();
        }
        private void Awake()
        {
            var args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "--menu-retry-report");
            report = index < 0 ? null : args[index + 1]; deadline = Time.unscaledTime + 45;
        }
        private void Update() { if (!finished && Time.unscaledTime > deadline) Finish("FAIL: main-menu recreation timeout"); }
        private void Finish(string result)
        { if (finished) return; finished = true; Debug.Log(result); if (report != null) File.WriteAllText(report, result); Application.Quit(result.StartsWith("PASS") ? 0 : 1); }
        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(.5f);
            SaveGameStore.TestSlotRoot = Path.GetFullPath("Temp/TrashTest-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(SaveGameStore.TestSlotRoot);
            var world = new WorldSessionState("trash-test"); world.EnableMultiplayer(); world.CreateWorldCharacter("Gründer");
            string path = SaveGameStore.SlotPath(0);
            SaveGameStore.Save(world, path); SaveGameStore.Save(world, path);
            string entry = SaveGameTrash.MoveToTrash(path);
            if (SaveGameStore.Exists(path) || SaveGameTrash.List().Length != 1 || SaveGameTrash.Summary(entry).worldId != world.WorldId)
            { Finish("FAIL: trash did not preserve world"); yield break; }
            SaveGameStore.Save(new WorldSessionState("occupied-slot"), path);
            bool blocked = false; try { SaveGameTrash.Restore(entry); } catch (IOException) { blocked = true; }
            if (!blocked || SaveGameStore.Load(path).WorldId != "occupied-slot") { Finish("FAIL: restore overwrote occupied slot"); yield break; }
            SaveGameTrash.MoveToTrash(path); SaveGameTrash.Restore(entry);
            if (SaveGameStore.Load(path).FounderCharacterId != world.FounderCharacterId) { Finish("FAIL: restored founder lost"); yield break; }
            foreach (bool host in new[] { true, true, false, false, true })
            {
                var net = NetworkCoop.Active;
                if (net == null || !net.OpenLobby(host, "127.0.0.1", "Retry-Test")) { Finish("FAIL: repeated host/join start"); yield break; }
                yield return new WaitForSecondsRealtime(.3f);
                net.StopAndReload();
                while (NetworkCoop.Active == null || NetworkCoop.Active == net || SceneManager.GetActiveScene().path != NetworkCoop.MenuScene) yield return null;
                yield return new WaitForSecondsRealtime(.2f);
                if (NetworkCoop.Running || GameSession.Existing == null) { Finish("FAIL: session/transport not rebuilt"); yield break; }
            }
            Finish("PASS: reversible slot deletion, backup and founder roundtrip, occupied restore protection; repeated host/join cancellation.");
        }
    }
}
