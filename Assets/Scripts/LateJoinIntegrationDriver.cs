using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace SecretsReborn
{
    // Opt-in three-process test: two players start, a third creates a host-owned
    // character in the active world. Saves and connection keys are isolated.
    public sealed class LateJoinIntegrationDriver : MonoBehaviour
    {
        private string role, report, folder;
        private bool finished;
        private double deadline;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var args = Environment.GetCommandLineArgs();
            if (!Debug.isDebugBuild || Array.IndexOf(args,"--late-join-role") < 0) return;
            var obj = new GameObject("Late join integration check"); DontDestroyOnLoad(obj); obj.AddComponent<LateJoinIntegrationDriver>();
        }
        private void Awake()
        {
            var args = Environment.GetCommandLineArgs(); role = args[Array.IndexOf(args,"--late-join-role")+1];
            report = args[Array.IndexOf(args,"--late-join-report")+1]; folder = Path.GetDirectoryName(report);
            deadline = Time.realtimeSinceStartupAsDouble + 90;
        }
        private void Update() { if (!finished && Time.realtimeSinceStartupAsDouble > deadline) Finish("FAIL: late join timeout " + role + " " + NetworkCoop.Active?.Status); }
        private void Finish(string result) { if (finished) return; finished = true; File.WriteAllText(report,result); Debug.Log(result); Application.Quit(result.StartsWith("PASS") ? 0 : 1); }
        private bool Exists(string name) => File.Exists(Path.Combine(folder,name));
        private void Signal(string name) { var path = Path.Combine(folder,name); File.WriteAllText(path+".writing","ready"); File.Move(path+".writing",path); }
        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(1);
            if (role == "third") while (!Exists("Third.request")) yield return null;
            var net = NetworkCoop.Active;
            if (!net.OpenLobby(role == "host","127.0.0.1",role == "third" ? "Später Gast" : role))
            { Finish("FAIL: connection " + role); yield break; }
            if (role == "host")
            {
                net.TestDriverActive = true;
                var world = GameSession.Instance.World;
                var host = world.CreateWorldCharacter("Hostfigur"); world.CreateWorldCharacter("Erster Gast");
                net.ChooseLobbyCharacter(host,ready:true);
                while (net.Lobby.players.Length != 2 || !net.Lobby.players[1].ready) yield return null;
                if (!net.StartLobbyAdventure()) { Finish("FAIL: initial start"); yield break; }
                while (net.ChangingArea) yield return null;
                foreach (var enemy in FindObjectsByType<TreeMeleeEnemy>(FindObjectsSortMode.None)) enemy.gameObject.SetActive(false);
                Signal("Third.request");
                while (net.ConnectedCount != 3 || !Exists("First.ready") || !Exists("Third.ready")) yield return null;
                if (File.ReadAllText(Path.Combine(folder,"First.profile")) == File.ReadAllText(Path.Combine(folder,"Third.profile")))
                { Finish("FAIL: concurrent clients share local credential profile"); yield break; }
                if (world.CharacterSlots.Length != 3 || net.LatestJoinAnnouncement != "Später Gast ist beigetreten.") { Finish("FAIL: host roster or announcement"); yield break; }
                var save = Path.Combine(folder,"LateJoin.es3"); SaveGameStore.Save(world,save); var restored = SaveGameStore.Load(save); ES3.DeleteFile(save);
                var added = restored.CharacterSlots[2];
                if (added.name != "Neue Figur" || added.bodyStyle != CharacterCustomization.Body(true,3) || added.hairStyle != CharacterCustomization.Hair(true,12))
                { Finish("FAIL: created appearance not persisted"); yield break; }
                Signal("Done.request");
                while (net.ConnectedCount != 1) yield return null;
                Finish("PASS: two-player start, third joins running world, host validates and saves creator appearance; announcement reaches all three.");
            }
            else if (role == "first")
            {
                while (net.Lobby.characters.Length != 2) yield return null;
                net.ChooseLobbyCharacter(net.Lobby.characters[1].id,ready:true);
                while (net.LatestJoinAnnouncement != "Später Gast ist beigetreten." || GameSession.Instance.World.CharacterSlots.Length != 3) yield return null;
                File.WriteAllText(Path.Combine(folder,"First.profile"),LocalClientProfile.KeySuffix);
                Signal("First.ready"); while (!Exists("Done.request")) yield return null;
                Finish("PASS: existing guest receives third player and global join announcement.");
            }
            else
            {
                while (net.RejoinSelection == null || net.RejoinSelection.waiting) yield return null;
                if (net.RejoinSelection.characters.Length != 2 || net.RejoinSelection.unavailable.Length != 2) { Finish("FAIL: expected two occupied figures"); yield break; }
                net.CreateRejoinCharacter(new WorldCharacterSlot { name = "Invalid", bodyStyle = "not-a-body" });
                yield return new WaitForSecondsRealtime(.7f);
                if (net.LocalCharacter != null || net.RejoinSelection.characters.Length != 2) { Finish("FAIL: invalid appearance accepted"); yield break; }
                var profile = new WorldCharacterSlot { name = "Neue Figur", hairColor = 4, eyeColor = -1,
                    bodyStyle = CharacterCustomization.Body(true,3), hairStyle = CharacterCustomization.Hair(true,12), eyeStyle = CharacterCustomization.Eyes(true,4) };
                net.CreateRejoinCharacter(profile); net.CreateRejoinCharacter(profile);
                while (net.LocalCharacter == null || net.JoiningSession || net.ChangingArea || net.LatestJoinAnnouncement != "Später Gast ist beigetreten.") yield return null;
                var world = GameSession.Instance.World;
                if (world.CharacterSlots.Length != 3 || world.CharacterProfile(net.LocalCharacter.CharacterId)?.name != "Neue Figur") { Finish("FAIL: duplicate create or identity"); yield break; }
                string identity = net.LocalCharacter.CharacterId;
                File.WriteAllText(Path.Combine(folder,"Third.profile"),LocalClientProfile.KeySuffix);
                var previous = net; net.StopAndReload();
                while (NetworkCoop.Active == null || NetworkCoop.Active == previous || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != NetworkCoop.MenuScene) yield return null;
                net = NetworkCoop.Active;
                if (!net.OpenLobby(false,"127.0.0.1","Später Gast")) { Finish("FAIL: third reconnect start"); yield break; }
                while (net.LocalCharacter == null || net.JoiningSession || net.LobbyActive || net.ChangingArea) yield return null;
                if (net.LocalCharacter.CharacterId != identity) { Finish("FAIL: third client lost own reconnect profile"); yield break; }
                Signal("Third.ready"); while (!Exists("Done.request")) yield return null;
                Finish("PASS: concurrent normal join creates figure; own reconnect retained; invalid appearance/duplicate create rejected; notification received.");
            }
        }
    }
}
