using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsReborn
{
    [Serializable] public sealed class RejoinOffer
    { public string world, message; public WorldCharacterSlot[] characters; public LobbyPortrait[] portraits; public string[] unavailable; public bool waiting; }
    [Serializable] public sealed class RejoinChoice { public string world, character; public bool creating; public WorldCharacterSlot create; }
    [Serializable] public sealed class RejoinStart { public string world, character, scene; public int epoch; public long nonce; }

    public sealed partial class NetworkCoop
    {
        private sealed class WaitingGuest
        { public string token, name, character; public RejoinStart load; public double deadline, lastChoice; }
        private readonly Dictionary<ulong,string> connectionTokens = new Dictionary<ulong,string>();
        private readonly Dictionary<ulong,WaitingGuest> waitingGuests = new Dictionary<ulong,WaitingGuest>();
        private static readonly Dictionary<string,string> processTickets = new Dictionary<string,string>();
        private string attemptedHost, attemptedTicket;
        private RejoinOffer joinOffer;
        private RejoinStart joinLoad;
        private long joinNonce;
        private float nextJoinOffer;
        private static T ReadJoin<T>(FastBufferReader reader) where T : class
        { try { reader.ReadValueSafe(out string json); return JsonUtility.FromJson<T>(json); } catch { return null; } }
        public bool JoiningSession { get; private set; }
        internal RejoinOffer RejoinSelection => joinOffer;
        internal bool CanChooseRejoin => joinOffer != null && JoiningSession && !loadingJoin
            && !joinOffer.waiting && manager.IsConnectedClient;
        private bool loadingJoin;
        private bool joinCreatorOpen;
        private CharacterCreatorSelection joinCreator = new CharacterCreatorSelection();
        private string joinCharacterName = "Abenteurer";
        private static string TicketPreferenceKey(string ip)
        {
            var args = Environment.GetCommandLineArgs();
            int report = Array.IndexOf(args,"--rejoin-report");
            int lateReport = Array.IndexOf(args,"--late-join-report");
            if (Debug.isDebugBuild && Array.IndexOf(args,"--late-join-role") >= 0 && lateReport >= 0 && lateReport + 1 < args.Length)
                return "SecretsReborn.Test.LateJoin." + System.IO.Path.GetDirectoryName(args[lateReport+1]) + "." + ip + LocalClientProfile.KeySuffix;
            if (Debug.isDebugBuild && Array.IndexOf(args,"--rejoin-client") >= 0 && report >= 0 && report + 1 < args.Length)
                return "SecretsReborn.Test.Rejoin." + args[report+1] + "." + ip;
            return "SecretsReborn.Rejoin." + ip + LocalClientProfile.KeySuffix;
        }
        private string Ticket(string ip, bool resume)
        {
            string key = TicketPreferenceKey(ip);
            string token = null;
            if (resume && !processTickets.TryGetValue(ip,out token)) token = PlayerPrefs.GetString(key,"");
            if (!Guid.TryParseExact(token,"N",out _)) token = Guid.NewGuid().ToString("N");
            // A fresh attempt is provisional. Cancelling character selection must
            // preserve the credential of the last successfully played character.
            attemptedHost = ip; attemptedTicket = token; return token;
        }
        private void RememberJoinedTicket()
        {
            if (!lobbySession || manager.IsServer || attemptedTicket == null) return;
            processTickets[attemptedHost] = attemptedTicket;
            PlayerPrefs.SetString(TicketPreferenceKey(attemptedHost),attemptedTicket); PlayerPrefs.Save();
            attemptedTicket = null;
        }
        private bool JoinBlocked => ChangingArea || VotePending || SaveBook.IsOpen || GameSession.Instance.Busy
            || GameSession.Instance.HasAnyReward || GameSession.Instance.IsGameOver || GameSession.Instance.PartyDefeated;
        private bool CharacterUnavailable(string id, ulong client)
        {
            foreach (var actor in owners.Values) if (actor != null && actor.CharacterId == id) return true;
            foreach (var pair in waitingGuests) if (pair.Key != client && pair.Value.character == id) return true;
            return false;
        }
        private void QueueRunningGuest(ulong client)
        {
            // A connection credential identifies a connection, never a character.
            // Every guest must explicitly confirm a currently available figure.
            nextJoinOffer = 0;
        }
        private void UpdateRejoins()
        {
            if (!manager.IsServer || waitingGuests.Count == 0) return;
            double now = Time.realtimeSinceStartupAsDouble;
            foreach (var pair in new List<KeyValuePair<ulong,WaitingGuest>>(waitingGuests))
            {
                var guest = pair.Value;
                if (guest.load != null && now > guest.deadline) { manager.DisconnectClient(pair.Key); continue; }
                if (guest.load != null && (guest.load.epoch != areaEpoch || guest.load.world != GameSession.Instance.World.WorldId)) guest.load = null;
                if (!JoinBlocked && guest.character != null && guest.load == null)
                {
                    if (GameSession.Instance.World.CharacterProfile(guest.character) == null) { guest.character = null; continue; }
                    guest.load = new RejoinStart { nonce = ++joinNonce, world = GameSession.Instance.World.WorldId,
                        character = guest.character, scene = SceneManager.GetActiveScene().path, epoch = areaEpoch };
                    guest.deadline = now + 45;
                    Send("sr.join-start",pair.Key,JsonUtility.ToJson(guest.load));
                }
            }
            if (Time.unscaledTime < nextJoinOffer) return; nextJoinOffer = Time.unscaledTime + .5f;
            foreach (var pair in waitingGuests)
            {
                if (pair.Value.load != null) continue;
                var blocked = new List<string>(); var portraits = new List<LobbyPortrait>();
                foreach (var c in GameSession.Instance.World.CharacterSlots)
                {
                    if (CharacterUnavailable(c.id,pair.Key)) blocked.Add(c.id);
                    var inventory = GameSession.Instance.World.CharacterInventory(c.id);
                    portraits.Add(new LobbyPortrait { id = c.id, armor = inventory.GetEquipment(EquipmentSlot.Armor), head = inventory.GetEquipment(EquipmentSlot.Head), feet = inventory.GetEquipment(EquipmentSlot.Feet) });
                }
                var offer = new RejoinOffer { world = GameSession.Instance.World.WorldId, characters = GameSession.Instance.World.CharacterSlots,
                    portraits = portraits.ToArray(), unavailable = blocked.ToArray(), waiting = JoinBlocked,
                    message = JoinBlocked ? "Beitritt wartet auf den Abschluss des aktuellen Vorgangs oder die Fortsetzung nach Game Over."
                        : GameSession.Instance.World.CharacterSlots.Length < 4
                        ? "Wähle eine freie Figur oder erstelle eine neue. Die Gruppe spielt weiter."
                        : blocked.Count == GameSession.Instance.World.CharacterSlots.Length
                        ? "Keine Figur frei: alle werden gerade gespielt oder für einen bestätigten Beitritt geladen."
                        : "Wähle eine freie gespeicherte Figur. Die Gruppe spielt weiter." };
                Send("sr.join-offer",pair.Key,JsonUtility.ToJson(offer));
            }
        }
        private void ReceiveJoinOffer(ulong sender, FastBufferReader reader)
        {
            if (manager.IsServer || sender != 0 || !lobbySession || local != null || reader.Length > 8192) return;
            var offer = ReadJoin<RejoinOffer>(reader);
            if (offer?.characters == null || offer.characters.Length > 4 || offer.unavailable == null || offer.portraits == null) return;
            joinOffer = offer; JoiningSession = true; LobbyActive = false;
        }
        internal void ChooseRejoinCharacter(string id)
        {
            if (!CanChooseRejoin) return;
            Send("sr.join-choice",0,JsonUtility.ToJson(new RejoinChoice { world = joinOffer.world, character = id }));
        }
        internal void CreateRejoinCharacter(WorldCharacterSlot profile)
        {
            if (!CanChooseRejoin || joinOffer.characters.Length >= 4 || profile == null) return;
            Send("sr.join-choice",0,JsonUtility.ToJson(new RejoinChoice { world = joinOffer.world, creating = true, create = profile }));
        }
        private void ReceiveJoinChoice(ulong sender, FastBufferReader reader)
        {
            if (!manager.IsServer || !waitingGuests.TryGetValue(sender,out var guest) || reader.Length > 1024 || guest.load != null) return;
            double now = Time.realtimeSinceStartupAsDouble; if (now - guest.lastChoice < .3) return; guest.lastChoice = now;
            var choice = ReadJoin<RejoinChoice>(reader);
            if (choice == null || choice.world != GameSession.Instance.World.WorldId) return;
            if (choice.creating)
            {
                if (choice.create == null || JoinBlocked || guest.character != null || GameSession.Instance.World.CharacterSlots.Length >= 4) return;
                var p = choice.create;
                try { choice.character = GameSession.Instance.World.CreateWorldCharacter(p.name,p.hairColor,p.eyeColor,p.bodyStyle,p.hairStyle,p.eyeStyle); }
                catch (ArgumentException) { nextJoinOffer = 0; return; }
            }
            if (GameSession.Instance.World.CharacterProfile(choice.character) == null || CharacterUnavailable(choice.character,sender)) return;
            guest.character = choice.character; nextJoinOffer = 0;
        }
        private void ReceiveJoinStart(ulong sender, FastBufferReader reader)
        {
            if (manager.IsServer || sender != 0 || !lobbySession || local != null && !loadingJoin || reader.Length > 2048) return;
            var start = ReadJoin<RejoinStart>(reader);
            if (start == null || start.nonce <= (joinLoad?.nonce ?? 0) || string.IsNullOrEmpty(start.character) || start.epoch < 0) return;
            GameSession.ValidateScene(start.scene); joinLoad = start;
            if (!loadingJoin) StartCoroutine(LoadJoiningGuest());
        }
        private IEnumerator LoadJoiningGuest()
        {
            loadingJoin = JoiningSession = true; LobbyActive = false;
            while (joinLoad != null && !quitting)
            {
                var loading = joinLoad;
                lobbyLocalCharacter = loading.character; areaEpoch = loading.epoch;
                pendingArea = new CoopAreaMessage { scene = loading.scene, epoch = loading.epoch, load = true };
                LockArea(); yield return GameSession.OpenScene(loading.scene); yield return null;
                if (!areaLocalReady) { status = "Beitrittspunkt fehlt."; StopAndReload(); yield break; }
                if (joinLoad != loading) continue;
                Send("sr.join-ready",0,JsonUtility.ToJson(loading));
                double until = Time.realtimeSinceStartupAsDouble + 60;
                while (ChangingArea && joinLoad == loading && !quitting && Time.realtimeSinceStartupAsDouble < until) yield return null;
                if (joinLoad != loading) continue;
                if (ChangingArea && !quitting) { status = "Beitritt wurde nicht bestätigt."; StopAndReload(); }
                break;
            }
            loadingJoin = JoiningSession = false; joinOffer = null;
        }
        private void ReceiveJoinReady(ulong sender, FastBufferReader reader)
        {
            if (!manager.IsServer || !waitingGuests.TryGetValue(sender,out var guest) || guest.load == null || reader.Length > 2048) return;
            var ready = ReadJoin<RejoinStart>(reader);
            if (ready == null || ready.nonce != guest.load.nonce || ready.character != guest.character || ready.world != guest.load.world || ready.epoch != guest.load.epoch || ready.scene != guest.load.scene) return;
            if (JoinBlocked || ready.epoch != areaEpoch || ready.world != GameSession.Instance.World.WorldId) { guest.load = null; return; }
            var actor = CreateCharacter(guest.character,true,false,SafeSpawn(guest.character,false));
            owners[sender] = actor; approved[sender] = guest.character; inputs[sender] = new RemoteInput { received = Time.unscaledTime };
            waitingGuests.Remove(sender);
            lobbyPlayers[sender] = new LobbyPlayer { client = sender, name = guest.name, character = guest.character, ready = true };
            GameSession.Instance.RegisterSpawn(actor); nextSnapshot = 0; Broadcast();
            AnnounceActiveJoins();
        }
        private void DrawRejoin()
        {
            MenuArt.Backdrop();
            var old = GUI.matrix; float scale = Mathf.Min(Screen.width/1200f,Screen.height/760f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width-1200*scale)/2,(Screen.height-760*scale)/2),Quaternion.identity,Vector3.one*scale);
            ForestInventorySkin.Panel(new Rect(15,15,1170,730));
            if (joinCreatorOpen && !loadingJoin && joinOffer != null)
            {
                ForestInventorySkin.Panel(new Rect(265,190,670,480));
                GUI.Label(new Rect(300,210,600,45),"DEINE NEUE FIGUR",MenuArt.Label(25,TextAnchor.MiddleCenter));
                MenuArt.Portrait(new Rect(300,265,230,300),joinCreator.Profile(joinCharacterName));
                GUI.Label(new Rect(565,270,320,25),"Charaktername",MenuArt.Label());
                joinCharacterName = GUI.TextField(new Rect(565,305,320,38),joinCharacterName,24);
                joinCreator.Draw(new Rect(565,350,320,235));
                GUI.enabled = CanChooseRejoin && joinOffer.characters.Length < 4 && !string.IsNullOrWhiteSpace(joinCharacterName);
                if (MenuArt.Button(new Rect(305,600,280,45),"Erstellen und beitreten")) { CreateRejoinCharacter(joinCreator.Profile(joinCharacterName)); joinCreatorOpen = false; }
                GUI.enabled = true;
                if (MenuArt.Button(new Rect(610,600,280,45),"Zurück zur Auswahl")) joinCreatorOpen = false;
                GUI.matrix = old; return;
            }
            GUI.Label(new Rect(50,60,1100,55),"LAUFENDER SESSION BEITRETEN",MenuArt.Label(30,TextAnchor.MiddleCenter));
            GUI.Label(new Rect(60,125,1080,65),loadingJoin ? "Figur und aktuelles Gebiet werden geladen …" : joinOffer?.message ?? "Verbinde …",MenuArt.Label(18,TextAnchor.MiddleCenter));
            if (!loadingJoin && joinOffer != null)
                for (int i = 0; i < joinOffer.characters.Length; i++)
                {
                    var c = joinOffer.characters[i]; var card = new Rect(55+i*277,215,260,325); ForestInventorySkin.Slot(card,false);
                    GUI.Label(new Rect(card.x+15,card.y+15,230,40),c.name,MenuArt.Label(20,TextAnchor.MiddleCenter));
                    LobbyPortrait gear = null; foreach (var p in joinOffer.portraits) if (p.id == c.id) gear = p;
                    MenuArt.Portrait(new Rect(card.x+35,card.y+65,190,190),c,gear);
                    bool unavailable = Array.IndexOf(joinOffer.unavailable,c.id) >= 0;
                    GUI.enabled = CanChooseRejoin && !unavailable;
                    if (MenuArt.Button(new Rect(card.x+20,card.y+265,220,42),unavailable ? "Belegt" : "Mit Figur beitreten")) ChooseRejoinCharacter(c.id);
                    GUI.enabled = true;
                }
            if (!loadingJoin && joinOffer != null && joinOffer.characters.Length < 4)
            {
                GUI.enabled = CanChooseRejoin;
                if (MenuArt.Button(new Rect(440,550,320,48),"Neue Figur erstellen")) { joinCreatorOpen = true; joinCreator = new CharacterCreatorSelection(); }
                GUI.enabled = true;
            }
            if (MenuArt.Button(new Rect(440,620,320,48),"Beitritt abbrechen")) StopAndReload(); GUI.matrix = old;
        }
    }
}
