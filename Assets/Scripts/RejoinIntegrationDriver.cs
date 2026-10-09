using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace SecretsReborn
{
    // Survives client session teardown. All saves and handoff markers are test-only.
    public sealed class RejoinIntegrationDriver : MonoBehaviour
    {
        private bool host, finished;
        private string report, folder;
        private double deadline;
        private string stage;
        private float nextDiagnostics;
        private void Mark(string value) { stage = value; File.AppendAllText(report+".trace",value+"\n"); }
        private void Signal(string name,string value)
        {
            string path = Path.Combine(folder,name), temporary = path + ".writing";
            File.WriteAllText(temporary,value); File.Move(temporary,path);
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var args = Environment.GetCommandLineArgs();
            if (!Debug.isDebugBuild || Array.IndexOf(args,"--rejoin-host") < 0 && Array.IndexOf(args,"--rejoin-client") < 0) return;
            var obj = new GameObject("Reconnect integration check"); DontDestroyOnLoad(obj); obj.AddComponent<RejoinIntegrationDriver>();
        }
        private void Awake()
        {
            var args = Environment.GetCommandLineArgs(); host = Array.IndexOf(args,"--rejoin-host") >= 0;
            int i = Array.IndexOf(args,"--rejoin-report"); report = args[i+1]; folder = Path.GetDirectoryName(report);
            deadline = Time.realtimeSinceStartupAsDouble + 120;
        }
        private void Update()
        {
            if (!finished && Time.unscaledTime >= nextDiagnostics)
            {
                nextDiagnostics = Time.unscaledTime + 5;
                var selection = NetworkCoop.Active?.RejoinSelection;
                if (selection != null) File.AppendAllText(report+".trace","selection: "+JsonUtility.ToJson(selection)+"\n");
                foreach (var manager in FindObjectsByType<Unity.Netcode.NetworkManager>(FindObjectsSortMode.None))
                    File.AppendAllText(report+".trace","transport: listening="+manager.IsListening+" server="+manager.IsServer+" clients="+manager.ConnectedClientsIds.Count+" singleton="+(manager == Unity.Netcode.NetworkManager.Singleton)+" time="+manager.NetworkTimeSystem?.LocalTime+" endpoint="+manager.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().ConnectionData.Address+" status="+NetworkCoop.Active?.Status+"\n");
            }
            if (!finished && Time.realtimeSinceStartupAsDouble > deadline) Finish("FAIL: rejoin timeout at " + stage + ": " + NetworkCoop.Active?.Status);
        }
        private void Finish(string result) { if (finished) return; finished = true; File.WriteAllText(report,result); Debug.Log(result); Application.Quit(result.StartsWith("PASS") ? 0 : 1); }
        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(1);
            var net = NetworkCoop.Active;
            Mark("initial lobby");
            if (!net.OpenLobby(host,"127.0.0.1",host ? "Host" : "Gast",resume:false)) { Finish("FAIL: initial connection"); yield break; }
            if (host) net.TestDriverActive = true;
            if (host)
            {
                var world = GameSession.Instance.World;
                string founder = world.CreateWorldCharacter("Hostfigur"), guest = world.CreateWorldCharacter("Gastfigur");
                world.CreateWorldCharacter("Freie Figur"); net.ChooseLobbyCharacter(founder,ready:true);
                while (net.Lobby.players.Length != 2 || !net.Lobby.players[1].ready) yield return null;
                Mark("initial adventure");
                if (!net.StartLobbyAdventure()) { Finish("FAIL: start"); yield break; }
                while (net.ChangingArea) yield return null;
                foreach (var enemy in FindObjectsByType<TreeMeleeEnemy>(FindObjectsSortMode.None)) enemy.gameObject.SetActive(false);
                world.CharacterVitals(guest).Damage(5); world.CharacterVitals(guest).SpendMana(17);
                world.CharacterInventory(guest).TryAdd("rejoin-test-item",7,99);
                Signal("RejoinDisconnect.request","first");
                Mark("waiting initial disconnect");
                while (net.ConnectedCount != 1) yield return null;
                if (world.CharacterProfile(guest) == null || world.CharacterVitals(guest).Health != 1) { Finish("FAIL: disconnect erased state"); yield break; }
                string save = Path.Combine(folder,"RejoinWorld-"+Guid.NewGuid().ToString("N")+".es3");
                SaveGameStore.Save(world,save); var loaded = SaveGameStore.Load(save); ES3.DeleteFile(save);
                if (loaded.CharacterVitals(guest).Health != 1 || loaded.CharacterInventory(guest).GetSlot(0)?.count != 7)
                { Finish("FAIL: absent character not saved"); yield break; }
                if (!net.BeginLoad(loaded,"Assets/Scenes/Raetselhoehle-Editable.unity")) { Finish("FAIL: host area change while guest absent"); yield break; }
                Mark("host area change");
                while (net.VotePending || net.ChangingArea) yield return null;
                world = GameSession.Instance.World;
                foreach (var enemy in FindObjectsByType<TreeMeleeEnemy>(FindObjectsSortMode.None)) enemy.gameObject.SetActive(false);
                // Hold a host operation: the reconnect must queue without spawning or freezing the party itself.
                GameSession.Instance.BeginNetworkArea(); Signal("RejoinConnect.request","resume");
                Mark("waiting queued reconnect");
                yield return new WaitForSecondsRealtime(3);
                if (net.ConnectedCount != 1) { Finish("FAIL: joined during host operation"); yield break; }
                GameSession.Instance.EndNetworkArea();
                Mark("waiting resumed actor");
                while (net.ConnectedCount != 2) yield return null;
                if (world.CharacterVitals(guest).Health != 1 || world.CharacterVitals(guest).Mana != 33) { Finish("FAIL: reconnect healed character"); yield break; }
                world.CharacterVitals(guest).Damage(1);
                Signal("RejoinDisconnect.request","dead");
                Mark("waiting dead disconnect");
                while (net.ConnectedCount != 1) yield return null;
                Signal("RejoinConnect.request","dead-resume");
                Mark("waiting dead reconnect");
                while (net.ConnectedCount != 2) yield return null;
                if (!world.CharacterVitals(guest).IsDown) { Finish("FAIL: reconnect revived character"); yield break; }
                Signal("RejoinDisconnect.request","fresh");
                while (net.ConnectedCount != 1) yield return null;
                Signal("RejoinConnect.request","new-key");
                // Client cancels its provisional fresh attempt, resumes its old
                // figure, then leaves again to select a different free figure.
                while (net.ConnectedCount != 2) yield return null;
                Signal("CancelResumeSeen.request","ready");
                while (net.ConnectedCount != 1) yield return null;
                Signal("SecondFreshConnect.request","ready");
                while (net.ConnectedCount != 2) yield return null;
                Signal("FinalJoinSeen.request","ready");
                while (net.ConnectedCount != 1) yield return null;
                Finish("PASS: running-session reconnect queued during host operation, absent character saved, state/death preserved; disconnect frees figure immediately; reconnect requires confirmation; occupied figures rejected.");
            }
            else
            {
                while (net.Lobby.characters.Length != 3) yield return null;
                Mark("client selected initial figure");
                string guest = net.Lobby.characters[1].id; net.ChooseLobbyCharacter(guest,ready:true);
                while (net.LobbyActive || net.ChangingArea || net.LocalCharacter == null) yield return null;
                for (int round = 0; round < 2; round++)
                {
                    Mark("client waiting disconnect " + round);
                    string stop = Path.Combine(folder,"RejoinDisconnect.request"), connect = Path.Combine(folder,"RejoinConnect.request");
                    while (!File.Exists(stop)) yield return null; File.Delete(stop);
                    var previous = net; net.StopAndReload();
                    Mark("client waiting rebuilt menu " + round);
                    float checkAt = 0;
                    while (NetworkCoop.Active == null || NetworkCoop.Active == previous || SceneManager.GetActiveScene().path != NetworkCoop.MenuScene)
                    {
                        if (Time.unscaledTime >= checkAt) { checkAt = Time.unscaledTime + 2;
                            File.AppendAllText(report+".trace","menu wait: activeNull="+(NetworkCoop.Active==null)+" old="+(NetworkCoop.Active==previous)+" scene="+SceneManager.GetActiveScene().path+" expected="+NetworkCoop.MenuScene+"\n"); }
                        yield return null;
                    }
                    while (!File.Exists(connect)) yield return null; File.Delete(connect);
                    yield return new WaitForSecondsRealtime(.5f);
                    Mark("client reconnecting " + round);
                    net = NetworkCoop.Active; if (!net.OpenLobby(false,"127.0.0.1","Gast")) { Finish("FAIL: reconnect request"); yield break; }
                    while (!net.CanChooseRejoin) yield return null;
                    yield return new WaitForSecondsRealtime(.7f);
                    if (net.LocalCharacter != null || Array.IndexOf(net.RejoinSelection.unavailable,guest) >= 0)
                    { Finish("FAIL: automatic assignment or disconnected figure still blocked"); yield break; }
                    net.ChooseRejoinCharacter(guest);
                    while (net.LocalCharacter == null || net.JoiningSession || net.LobbyActive || net.ChangingArea) yield return null;
                    Mark("client resumed " + round);
                    if (net.LocalCharacter.CharacterId != guest || GameSession.Instance.World.CharacterInventory(guest).GetSlot(0)?.count != 7)
                    { Finish("FAIL: selected identity or inventory lost"); yield break; }
                    var hostPose = GameSession.Instance.World.Position(GameSession.Instance.World.FounderCharacterId);
                    if (SceneManager.GetActiveScene().path != "Assets/Scenes/Raetselhoehle-Editable.unity" || hostPose == null
                        || Vector2.Distance(net.LocalCharacter.transform.position,new Vector2(hostPose.x,hostPose.y)) > 3)
                    { Finish("FAIL: reconnect did not follow host into current area"); yield break; }
                    if (round == 1 && !GameSession.Instance.World.CharacterVitals(guest).IsDown) { Finish("FAIL: dead client revived"); yield break; }
                }
                while (!File.Exists(Path.Combine(folder,"RejoinDisconnect.request"))) yield return null;
                File.Delete(Path.Combine(folder,"RejoinDisconnect.request"));
                var last = net; net.StopAndReload();
                while (NetworkCoop.Active == null || NetworkCoop.Active == last || SceneManager.GetActiveScene().path != NetworkCoop.MenuScene) yield return null;
                while (!File.Exists(Path.Combine(folder,"RejoinConnect.request"))) yield return null;
                File.Delete(Path.Combine(folder,"RejoinConnect.request")); net = NetworkCoop.Active;
                if (!net.OpenLobby(false,"127.0.0.1","Neuer Gast",resume:false)) { Finish("FAIL: fresh connection"); yield break; }
                while (!net.CanChooseRejoin) yield return null;
                string free = net.RejoinSelection.characters[2].id;
                if (Array.IndexOf(net.RejoinSelection.unavailable,guest) >= 0) { Finish("FAIL: disconnected figure not immediately free"); yield break; }
                net.ChooseRejoinCharacter(net.RejoinSelection.characters[0].id); yield return new WaitForSecondsRealtime(.7f);
                if (net.LocalCharacter != null) { Finish("FAIL: occupied host figure stolen"); yield break; }
                last = net; net.StopAndReload();
                while (NetworkCoop.Active == null || NetworkCoop.Active == last || SceneManager.GetActiveScene().path != NetworkCoop.MenuScene) yield return null;
                net = NetworkCoop.Active;
                if (!net.OpenLobby(false,"127.0.0.1","Gast")) { Finish("FAIL: normal join after fresh cancellation"); yield break; }
                while (!net.CanChooseRejoin) yield return null;
                    yield return new WaitForSecondsRealtime(.7f);
                    if (net.LocalCharacter != null || Array.IndexOf(net.RejoinSelection.unavailable,guest) >= 0)
                    { Finish("FAIL: automatic assignment or disconnected figure still blocked"); yield break; }
                    net.ChooseRejoinCharacter(guest);
                    while (net.LocalCharacter == null || net.JoiningSession || net.LobbyActive || net.ChangingArea) yield return null;
                if (net.LocalCharacter.CharacterId != guest) { Finish("FAIL: cancelled fresh attempt overwrote saved identity"); yield break; }
                Mark("cancelled fresh attempt preserved original reconnect identity");
                while (!File.Exists(Path.Combine(folder,"CancelResumeSeen.request"))) yield return null;
                last = net; net.StopAndReload();
                while (NetworkCoop.Active == null || NetworkCoop.Active == last || SceneManager.GetActiveScene().path != NetworkCoop.MenuScene) yield return null;
                while (!File.Exists(Path.Combine(folder,"SecondFreshConnect.request"))) yield return null;
                net = NetworkCoop.Active;
                if (!net.OpenLobby(false,"127.0.0.1","Neuer Gast",resume:false)) { Finish("FAIL: second fresh connection"); yield break; }
                while (!net.CanChooseRejoin) yield return null;
                Mark("confirming other free figure " + free + " offer=" + JsonUtility.ToJson(net.RejoinSelection));
                net.ChooseRejoinCharacter(free);
                while (net.LocalCharacter == null || net.JoiningSession || net.ChangingArea) yield return null;
                if (net.LocalCharacter.CharacterId != free)
                { Finish("FAIL: free figure binding"); yield break; }
                while (!File.Exists(Path.Combine(folder,"FinalJoinSeen.request"))) yield return null;
                Finish("PASS: explicit reconnect retains inventory/death; repeat selection works; occupied host rejected and free figure selectable.");
            }
        }
    }
}
