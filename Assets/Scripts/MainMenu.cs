using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsReborn
{
    public sealed class MainMenu : MonoBehaviour
    {
        internal static string ConnectionError;
        private string playerName = "Spieler", address = "127.0.0.1", message;
        private enum Page { Home, Solo, SoloCreate, Multiplayer, Host, Join, SoloLoad, HostLoad, Options }; private Page page;
        private string characterName = "Abenteurer";
        private readonly CharacterCreatorSelection creator = new CharacterCreatorSelection();
        private GUIStyle title;
        private readonly SaveGameData[] slotSummaries = new SaveGameData[3];
        private readonly string[] slotErrors = new string[3];
        private string pendingDelete;
        private bool showTrash;
        private TrashedSave[] trash = Array.Empty<TrashedSave>();
        private Vector2 trashScroll;
        private Vector2 slotsScroll;
        private void Start()
        {
            if (!string.IsNullOrEmpty(ConnectionError)) { message = ConnectionError; ConnectionError = null; }
            // Returning from a lobby destroys the previous persistent session.
            // Runtime startup callbacks do not run again on a normal scene reload.
            var session = GameSession.Instance;
            var args = Environment.GetCommandLineArgs();
            // Existing opt-in combat/transport smoke tests still start in the adventure.
            if (Debug.isDebugBuild && (Array.IndexOf(args, "--coop-smoke-host") >= 0 || Array.IndexOf(args, "--coop-smoke-client") >= 0
                || Array.IndexOf(args, "--book-test-host") >= 0 || Array.IndexOf(args, "--book-test-client") >= 0))
                GameSession.OpenScene(NetworkCoop.FirstScene);
        }
        private void OnGUI()
        {
            var net = NetworkCoop.Active;
            if (net == null) return;
            if (net != null && (net.LobbyActive || net.ChangingArea || net.JoiningSession)) return;
            MenuArt.Backdrop();
            ForestInventorySkin.Panel(new Rect(Screen.width/2f-285,35,570,Screen.height-70));
            if (title == null) title = MenuArt.Label(32, TextAnchor.MiddleCenter);
            GUILayout.BeginArea(new Rect(Screen.width / 2f - 260, 60, 520, Screen.height - 110));
            GUILayout.Label("SECRETS REBORN", title); GUILayout.Space(20);
            switch (page)
            {
                case Page.Home:
                    Navigate("Singleplayer", Page.Solo); Navigate("Multiplayer", Page.Multiplayer);
                    Navigate("Optionen", Page.Options);
                    if (MenuArt.Button("Exit")) Application.Quit();
                    break;
                case Page.Solo:
                    Navigate("Start New", Page.SoloCreate);
                    Navigate("Load Game", Page.SoloLoad); Navigate("Back", Page.Home); break;
                case Page.SoloCreate:
                    GUILayout.Label("Deine neue Figur", MenuArt.Label(22, TextAnchor.MiddleCenter));
                    characterName = GUILayout.TextField(characterName, 24);
                    MenuArt.Portrait(GUILayoutUtility.GetRect(200,125), creator.Profile(characterName));
                    creator.Draw();
                    GUI.enabled = !string.IsNullOrWhiteSpace(characterName);
                    if (MenuArt.Button("Figur erstellen und starten")) Attempt(() =>
                    { var p = creator.Profile(characterName); var world = new WorldSessionState(Guid.NewGuid().ToString("N")); world.CreateSoloProfile(characterName,p.hairColor,p.eyeColor,p.bodyStyle,p.hairStyle,p.eyeStyle); return StartSolo(world); });
                    GUI.enabled = true; Navigate("Back",Page.Solo); break;
                case Page.Multiplayer:
                    Navigate("Host Game", Page.Host); Navigate("Join Game", Page.Join); Navigate("Back", Page.Home); break;
                case Page.Host:
                    GUILayout.Label("Spielername"); playerName = GUILayout.TextField(playerName, 24);
                    if (MenuArt.Button("New Game")) Attempt(() => net.OpenLobby(true, null, playerName));
                    Navigate("Load Game", Page.HostLoad); Navigate("Back", Page.Multiplayer); break;
                case Page.Join:
                    GUILayout.Label("Spielername"); playerName = GUILayout.TextField(playerName, 24);
                    GUILayout.Label("Host-IP · UDP 7777"); address = GUILayout.TextField(address, 45);
                    if (MenuArt.Button("Connect")) Attempt(() => net.OpenLobby(false, address, playerName));
                    Navigate("Back", Page.Multiplayer); break;
                case Page.SoloLoad:
                case Page.HostLoad:
                    if (showTrash) { DrawTrash(); break; }
                    slotsScroll = GUILayout.BeginScrollView(slotsScroll, GUILayout.MaxHeight(Mathf.Max(160, Screen.height - 240)));
                    for (int i = 0; i < 3; i++)
                    {
                        int slot = i; string path = page == Page.HostLoad ? SaveGameStore.MultiplayerSlotPath(slot) : "SecretsReborn/slot-" + (slot + 1) + ".es3";
                        GUI.enabled = slotSummaries[i] != null && slotErrors[i] == null;
                        if (MenuArt.Button("Slot " + (slot + 1) + "\n" + (slotErrors[i] ?? SaveSlotLabel.Details(slotSummaries[i])),
                            GUILayout.MinHeight(100))) Attempt(() => page == Page.HostLoad
                            ? net.OpenLobby(true, null, playerName, SaveGameStore.Load(path)) : StartSolo(SaveGameStore.Load(path)));
                        GUI.enabled = SaveGameStore.Exists(path);
                        if (MenuArt.Button("Slot " + (i + 1) + " löschen")) pendingDelete = path;
                    }
                    GUI.enabled = true;
                    if (pendingDelete != null)
                    {
                        int index = page == Page.HostLoad ? Array.FindIndex(new[] { SaveGameStore.MultiplayerSlotPath(0), SaveGameStore.MultiplayerSlotPath(1), SaveGameStore.MultiplayerSlotPath(2) }, p => p == pendingDelete)
                            : Array.FindIndex(new[] { "SecretsReborn/slot-1.es3", "SecretsReborn/slot-2.es3", "SecretsReborn/slot-3.es3" }, p => p == pendingDelete);
                        GUILayout.Label("Slot " + (index + 1) + " in den Papierkorb verschieben?\n" + (index >= 0 ? SaveSlotLabel.Details(slotSummaries[index]) : ""));
                        if (MenuArt.Button("Löschung bestätigen"))
                        { try { SaveGameTrash.MoveToTrash(pendingDelete); pendingDelete = null; RefreshSlots(); message = "In den Papierkorb verschoben."; } catch (Exception e) { message = e.Message; } }
                        if (MenuArt.Button("Abbrechen")) pendingDelete = null;
                    }
                    if (MenuArt.Button("Papierkorb / Wiederherstellen"))
                    { try { trash = SaveGameTrash.List(); showTrash = true; pendingDelete = null; } catch (Exception e) { message = e.Message; } }
                    GUILayout.EndScrollView();
                    Navigate("Back", page == Page.HostLoad ? Page.Host : Page.Solo); break;
                case Page.Options:
                    Navigate("Back", Page.Home); break;
            }
            GUILayout.Space(15); GUILayout.Label(message ?? "");
            GUILayout.EndArea();
        }
        private void Navigate(string label, Page target)
        {
            if (!MenuArt.Button(label)) return;
            page = target; message = null; pendingDelete = null; showTrash = false;
            if (target != Page.HostLoad && target != Page.SoloLoad) return;
            RefreshSlots();
        }
        private void RefreshSlots()
        {
            for (int i = 0; i < 3; i++)
            {
                slotSummaries[i] = null; slotErrors[i] = null;
                try { slotSummaries[i] = SaveGameStore.ReadSummary(page == Page.HostLoad ? SaveGameStore.MultiplayerSlotPath(i) : "SecretsReborn/slot-" + (i + 1) + ".es3"); }
                catch (Exception) { slotErrors[i] = "Spielstand nicht lesbar"; }
            }
        }
        private void DrawTrash()
        {
            GUILayout.Label("Papierkorb · Wiederherstellung nur in einen freien ursprünglichen Slot");
            trashScroll = GUILayout.BeginScrollView(trashScroll, GUILayout.MaxHeight(320));
            foreach (var entry in trash)
            {
                string details;
                try { details = SaveSlotLabel.Details(SaveGameTrash.Summary(entry.id)); } catch (Exception) { details = "Nicht lesbarer Spielstand (Datei bleibt erhalten)."; }
                GUILayout.Label(entry.originalPath + "\n" + details);
                GUI.enabled = !SaveGameStore.Exists(entry.originalPath);
                if (MenuArt.Button("Wiederherstellen"))
                { try { SaveGameTrash.Restore(entry.id); trash = SaveGameTrash.List(); RefreshSlots(); message = "Wiederhergestellt."; } catch (Exception e) { message = e.Message; } }
                GUI.enabled = true;
            }
            GUILayout.EndScrollView();
            if (MenuArt.Button("Back")) { showTrash = false; RefreshSlots(); }
        }
        private void Attempt(Func<bool> action)
        { try { if (!action()) message = NetworkCoop.Active.Status; } catch (Exception e) { message = e.Message; } }
        private bool StartSolo(WorldSessionState world)
        {
            if (world != null && world.Multiplayer) throw new ArgumentException("Multiplayer-Spielstände bitte über die Lobby öffnen.");
            world = world ?? new WorldSessionState(Guid.NewGuid().ToString("N"));
            string scene = string.IsNullOrEmpty(world.SavedScenePath) ? NetworkCoop.FirstScene : world.SavedScenePath;
            GameSession.ValidateScene(scene);
            GameSession.Instance.BeginFrontendWorld(world);
            SceneManager.sceneLoaded += RestoreSolo;
            GameSession.OpenScene(scene); return true;
        }
        private static void RestoreSolo(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= RestoreSolo;
            foreach (var actor in FindObjectsByType<CharacterInventory>(FindObjectsSortMode.None))
            {
                actor.RefreshSession(); var position = GameSession.Instance.World.Position(actor.CharacterId);
                if (position != null && position.scenePath == scene.path) GameSession.Warp(actor, new Vector3(position.x, position.y, position.z));
            }
        }
    }
}
