using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsReborn
{
    [Serializable] public sealed class LobbyPlayer
    { public ulong client; public string name, character; public bool ready; }
    [Serializable] public sealed class LobbyState
    { public LobbyPlayer[] players; public WorldCharacterSlot[] characters; public string founder; public LobbyPortrait[] portraits; }
    [Serializable] public sealed class LobbyCommand
    { public string name, character, create, bodyStyle, hairStyle, eyeStyle; public bool ready, clear; public int hair = -1, eyes = -1; }

    public sealed partial class NetworkCoop
    {
        public const string MenuScene = "Assets/Scenes/MainMenu.unity";
        public const string FirstScene = "Assets/Scenes/Waldheiligtum-Editable.unity";
        public bool LobbyActive { get; private set; }
        private bool lobbySession;
        private float nextLobby;
        private readonly Dictionary<ulong, LobbyPlayer> lobbyPlayers = new Dictionary<ulong, LobbyPlayer>();
        private LobbyState lobbyView = new LobbyState { players = Array.Empty<LobbyPlayer>(), characters = Array.Empty<WorldCharacterSlot>() };
        private string lobbyCharacterName = "Abenteurer", lobbyLocalCharacter;
        private string deleteCharacter;
        private bool creatorOpen;
        private CharacterCreatorSelection creator = new CharacterCreatorSelection();
        public LobbyState Lobby => lobbyView;

        public bool OpenLobby(bool host, string ip, string playerName, WorldSessionState world = null, bool resume = true)
        {
            if (Running || started || characterPrefab == null || SceneManager.GetActiveScene().path != MenuScene) return false;
            if (!host && !System.Net.IPAddress.TryParse(ip, out _)) { status = "Ungültige IP-Adresse."; return false; }
            LobbyActive = lobbySession = true; Application.runInBackground = true;
            if (host)
            {
                world = world ?? new WorldSessionState(Guid.NewGuid().ToString("N")); world.EnableMultiplayer();
                GameSession.Instance.BeginFrontendWorld(world);
                lobbyPlayers[0] = new LobbyPlayer { client = 0, name = CleanName(playerName, "Spieler 1") };
                transport.SetConnectionData("127.0.0.1", 7777, "0.0.0.0");
                manager.NetworkConfig.ConnectionData = HelloBytes(Guid.NewGuid().ToString("N"));
                started = manager.StartHost();
            }
            else
            {
                transport.SetConnectionData(ip, 7777);
                manager.NetworkConfig.ConnectionData = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(new CoopHello
                { characterToken = Ticket(ip,resume), scene = MenuScene, playerName = CleanName(playerName, "Spieler") }));
                started = manager.StartClient();
            }
            if (!started) { LobbyActive = lobbySession = false; status = "Lobby konnte nicht gestartet werden."; }
            return started;
        }
        private static string CleanName(string name, string fallback)
        { name = (name ?? "").Trim(); return name.Length == 0 ? fallback : name.Substring(0, Math.Min(24, name.Length)); }
        private void LobbyConnected(ulong id)
        {
            if (!manager.IsServer) return;
            if (!lobbyPlayers.ContainsKey(id)) lobbyPlayers[id] = new LobbyPlayer { client = id, name = "Spieler " + (lobbyPlayers.Count + 1) };
            BroadcastLobby();
        }
        private void BroadcastLobby()
        {
            lobbyView = new LobbyState { players = new List<LobbyPlayer>(lobbyPlayers.Values).ToArray(), characters = GameSession.Instance.World.CharacterSlots, founder = GameSession.Instance.World.FounderCharacterId };
            var portraits = new List<LobbyPortrait>();
            foreach (var c in lobbyView.characters)
            {
                var inventory = GameSession.Instance.World.CharacterInventory(c.id);
                portraits.Add(new LobbyPortrait { id = c.id, armor = inventory.GetEquipment(EquipmentSlot.Armor), head = inventory.GetEquipment(EquipmentSlot.Head), feet = inventory.GetEquipment(EquipmentSlot.Feet) });
            }
            lobbyView.portraits = portraits.ToArray();
            string json = JsonUtility.ToJson(lobbyView);
            foreach (var id in lobbyPlayers.Keys) if (id != 0) Send("sr.lobby", id, json);
        }
        private void ReceiveLobby(ulong sender, FastBufferReader reader)
        {
            if (manager.IsServer || sender != 0 || !LobbyActive || reader.Length > 8192) return;
            try { reader.ReadValueSafe(out string json); var state = JsonUtility.FromJson<LobbyState>(json);
                if (state?.players == null || state.characters == null || state.players.Length > 4 || state.characters.Length > 4) return;
                lobbyView = state; foreach (var p in state.players) if (p.client == manager.LocalClientId) lobbyLocalCharacter = p.character;
            } catch (Exception e) { status = e.Message; }
        }
        private void ReceiveLobbyCommand(ulong sender, FastBufferReader reader)
        {
            if (!manager.IsServer || !LobbyActive || !lobbyPlayers.ContainsKey(sender) || reader.Length > 1024) return;
            try { reader.ReadValueSafe(out string json); ApplyLobbyCommand(sender, JsonUtility.FromJson<LobbyCommand>(json)); }
            catch (Exception e) { status = e.Message; }
        }
        private void ApplyLobbyCommand(ulong sender, LobbyCommand command)
        {
            if (command == null || !lobbyPlayers.TryGetValue(sender, out var player)) return;
            if (command.clear)
            { player.character = null; player.ready = false; if (sender == 0) lobbyLocalCharacter = null; BroadcastLobby(); return; }
            if (command.name != null) player.name = CleanName(command.name, player.name);
            if (!string.IsNullOrWhiteSpace(command.create))
            {
                if (!string.IsNullOrEmpty(player.character)) return;
                command.character = GameSession.Instance.World.CreateWorldCharacter(command.create, command.hair, command.eyes, command.bodyStyle, command.hairStyle, command.eyeStyle);
            }
            if (!string.IsNullOrEmpty(command.character))
            {
                bool found = false;
                foreach (var slot in GameSession.Instance.World.CharacterSlots) if (slot.id == command.character) found = true;
                foreach (var other in lobbyPlayers.Values) if (other.client != sender && other.character == command.character) return;
                if (!found) return;
                if (player.character != command.character) player.ready = false;
                player.character = command.character;
            }
            if (!string.IsNullOrEmpty(player.character)) player.ready = command.ready;
            if (sender == 0) lobbyLocalCharacter = player.character;
            BroadcastLobby();
        }
        public void ChooseLobbyCharacter(string id, string create = null, bool ready = false, int hair = -1, int eyes = -1,
            string bodyStyle = null, string hairStyle = null, string eyeStyle = null)
        {
            if (!LobbyActive || !manager.IsConnectedClient) return;
            var command = new LobbyCommand { character = id, create = create, ready = ready, hair = hair, eyes = eyes,
                bodyStyle = bodyStyle, hairStyle = hairStyle, eyeStyle = eyeStyle };
            if (manager.IsServer) ApplyLobbyCommand(0, command);
            else Send("sr.lobby-command", 0, JsonUtility.ToJson(command));
        }
        private void ClearLobbySelection()
        {
            var command = new LobbyCommand { clear = true };
            if (manager.IsServer) ApplyLobbyCommand(0, command);
            else Send("sr.lobby-command", 0, JsonUtility.ToJson(command));
        }
        public bool DeleteLobbyCharacter(string id)
        {
            if (!LobbyActive || !manager.IsServer) return false;
            foreach (var p in lobbyPlayers.Values) if (p.character == id) { status = "Charakterauswahl zuerst freigeben."; return false; }
            if (!GameSession.Instance.World.DeleteWorldCharacter(id)) { status = "Gründerfigur ist geschützt oder Charakter fehlt."; return false; }
            foreach (var p in lobbyPlayers.Values) p.ready = false;
            deleteCharacter = null; status = "Charakter gelöscht. Dauerhaft nach dem nächsten Speichern am Buch.";
            BroadcastLobby(); return true;
        }
        public bool StartLobbyAdventure()
        {
            if (!LobbyActive || !manager.IsServer || lobbyPlayers.Count == 0) return false;
            foreach (var p in lobbyPlayers.Values) if (!p.ready || string.IsNullOrEmpty(p.character)) return false;
            var world = GameSession.Instance.World;
            string scene = string.IsNullOrEmpty(world.SavedScenePath) ? FirstScene : world.SavedScenePath;
            GameSession.ValidateScene(scene);
            areaRoster.Clear(); areaWaiting.Clear(); areaLamps.Clear();
            foreach (var p in lobbyPlayers.Values)
            { areaRoster[p.client] = p.character; if (p.client != 0) { approved[p.client] = p.character; areaWaiting.Add(p.client); inputs[p.client] = new RemoteInput();
                    reservations.Bind(connectionTokens[p.client],world.WorldId,p.character); } }
            lobbyLocalCharacter = areaRoster[0];
            // The common saved host point is independent of which character hosts today.
            var anchor = world.Position(world.SavedHostCharacterId ?? "");
            if (anchor == null) foreach (var c in world.Capture().characters)
                if (c.hasPosition && c.scenePath == scene) { anchor = c; break; }
            if (anchor != null && anchor.scenePath == scene)
                world.SetPosition(lobbyLocalCharacter, scene, anchor.x, anchor.y, anchor.z);
            pendingArea = new CoopAreaMessage { epoch = ++areaEpoch, scene = scene, load = true };
            LobbyActive = false; LockArea();
            foreach (var id in areaWaiting) Send("sr.area", id, JsonUtility.ToJson(pendingArea));
            StartCoroutine(LoadArea()); return true;
        }
        private void DrawLobby()
        {
            MenuArt.Backdrop();
            var oldMatrix = GUI.matrix;
            float scale = Mathf.Min(Screen.width / 1200f, Screen.height / 760f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width-1200*scale)/2,(Screen.height-760*scale)/2),Quaternion.identity,Vector3.one*scale);
            ForestInventorySkin.Panel(new Rect(15,15,1170,730));
            GUI.Label(new Rect(50,45,1100,60), "SECRETS REBORN", MenuArt.Label(40,TextAnchor.MiddleCenter));
            GUI.Label(new Rect(50,105,1100,40), "GEMEINSAM INS ABENTEUER · BIS ZU VIER SPIELER", MenuArt.Label(16,TextAnchor.MiddleCenter));
            if (creatorOpen) { DrawCreator(); GUI.matrix = oldMatrix; return; }
            MenuArt.BlockInput = deleteCharacter != null;
            for (int i = 0; i < 4; i++)
            {
                var card = new Rect(55+i*277,175,260,350);
                ForestInventorySkin.Slot(card, i < lobbyView.characters.Length && lobbyView.characters[i].id == lobbyLocalCharacter);
                ForestInventorySkin.Leather(new Rect(card.x+9,card.y+9,card.width-18,card.height-18));
                if (i >= lobbyView.characters.Length)
                {
                    GUI.Label(new Rect(card.x+20,card.y+70,220,70), "FREIER CHARAKTERPLATZ", MenuArt.Label(19,TextAnchor.MiddleCenter));
                    GUI.Label(new Rect(card.x+20,card.y+175,220,60), "Erstelle eine neue Figur für diese Welt.", MenuArt.Label(15,TextAnchor.MiddleCenter));
                    GUI.enabled = string.IsNullOrEmpty(lobbyLocalCharacter);
                    if (MenuArt.Button(new Rect(card.x+20,card.y+270,220,48), "Charakter erstellen")) { creatorOpen = true; creator = new CharacterCreatorSelection(); }
                    GUI.enabled = true; continue;
                }
                var c = lobbyView.characters[i]; LobbyPlayer owner = null;
                foreach (var p in lobbyView.players) if (p.character == c.id) owner = p;
                LobbyPortrait portrait = null;
                foreach (var p in lobbyView.portraits ?? Array.Empty<LobbyPortrait>()) if (p.id == c.id) portrait = p;
                GUI.Label(new Rect(card.x+15,card.y+12,230,45),c.name,MenuArt.Label(21,TextAnchor.MiddleCenter));
                MenuArt.Portrait(new Rect(card.x+40,card.y+58,180,185),c,portrait);
                string note = owner == null ? "VERFÜGBAR" : owner.name + (owner.ready ? " · BEREIT" : " · WARTET");
                GUI.Label(new Rect(card.x+15,card.y+237,230,35),note,MenuArt.Label(14,TextAnchor.MiddleCenter));
                GUI.enabled = owner == null || owner.client == manager.LocalClientId;
                if (MenuArt.Button(new Rect(card.x+20,card.y+275,220,40),c.id == lobbyLocalCharacter ? "Deine Figur" : "Auswählen")) ChooseLobbyCharacter(c.id);
                GUI.enabled = true;
                if (c.id == lobbyView.founder) GUI.Label(new Rect(card.x+15,card.y+315,230,25),"GRÜNDERFIGUR · GESCHÜTZT",MenuArt.Label(11,TextAnchor.MiddleCenter));
                else if (manager.IsServer)
                {
                    GUI.enabled = owner == null;
                    if (MenuArt.Button(new Rect(card.x+55,card.y+319,150,25),"Figur löschen")) deleteCharacter = c.id;
                    GUI.enabled = true;
                }
            }
            GUI.Label(new Rect(60,535,1080,32),"Verbundene Spieler: " + lobbyView.players.Length + "/4 · Jede Figur gehört dieser Host-Welt.",MenuArt.Label(15,TextAnchor.MiddleCenter));
            GUI.enabled = !string.IsNullOrEmpty(lobbyLocalCharacter);
            bool ready = false; foreach (var p in lobbyView.players) if (p.client == manager.LocalClientId) ready = p.ready;
            if (MenuArt.Button(new Rect(60,585,250,50),ready ? "Nicht mehr bereit" : "Bereit zum Abenteuer")) ChooseLobbyCharacter(lobbyLocalCharacter,ready: !ready);
            if (MenuArt.Button(new Rect(325,585,250,50),"Auswahl freigeben")) ClearLobbySelection();
            GUI.enabled = manager.IsServer;
            foreach (var p in lobbyView.players) if (!p.ready || string.IsNullOrEmpty(p.character)) GUI.enabled = false;
            if (MenuArt.Button(new Rect(590,585,250,50),manager.IsServer ? "Abenteuer starten" : "Host startet die Gruppe")) StartLobbyAdventure();
            GUI.enabled = true;
            if (MenuArt.Button(new Rect(855,585,285,50),"Lobby verlassen")) StopAndReload();
            GUI.Label(new Rect(60,651,1080,55),status,MenuArt.Label(14,TextAnchor.MiddleCenter));
            if (deleteCharacter != null)
            {
                MenuArt.BlockInput = false;
                ForestInventorySkin.Panel(new Rect(290,230,620,275));
                string name = "Figur"; foreach (var c in lobbyView.characters) if (c.id == deleteCharacter) name = c.name;
                GUI.Label(new Rect(335,275,530,105),"„"+name+"“ löschen?\nInventar, Ausrüstung und persönlicher Fortschritt gehen verloren.",MenuArt.Label(18,TextAnchor.MiddleCenter));
                if (MenuArt.Button(new Rect(335,405,250,48),"Löschung bestätigen")) DeleteLobbyCharacter(deleteCharacter);
                if (MenuArt.Button(new Rect(600,405,265,48),"Abbrechen")) deleteCharacter = null;
            }
            GUI.matrix = oldMatrix;
            MenuArt.BlockInput = false;
        }
        private void DrawCreator()
        {
            ForestInventorySkin.Panel(new Rect(250,160,700,500));
            GUI.Label(new Rect(295,195,610,40),"DEINE NEUE FIGUR",MenuArt.Label(28,TextAnchor.MiddleCenter));
            MenuArt.Portrait(new Rect(300,265,230,300),creator.Profile(lobbyCharacterName));
            GUI.Label(new Rect(565,270,320,25),"Charaktername",MenuArt.Label());
            lobbyCharacterName = GUI.TextField(new Rect(565,305,320,38),lobbyCharacterName,24);
            creator.Draw(new Rect(565,350,320,235));
            GUI.enabled = !string.IsNullOrWhiteSpace(lobbyCharacterName) && string.IsNullOrEmpty(lobbyLocalCharacter) && lobbyView.characters.Length < 4;
            if (MenuArt.Button(new Rect(305,600,280,45),"Figur erstellen")) { var p = creator.Profile(lobbyCharacterName);
                ChooseLobbyCharacter(null,lobbyCharacterName,hair: p.hairColor,eyes: p.eyeColor,bodyStyle: p.bodyStyle,hairStyle: p.hairStyle,eyeStyle: p.eyeStyle); creatorOpen = false; }
            GUI.enabled = true;
            if (MenuArt.Button(new Rect(610,600,280,45),"Abbrechen")) creatorOpen = false;
        }
    }
}
