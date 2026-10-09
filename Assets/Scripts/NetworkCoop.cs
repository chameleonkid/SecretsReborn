using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SecretsReborn
{
    // Initial LAN adapter. Domain IDs and the host's world state remain independent
    // of transport IDs. NGO carries input requests and server-authored snapshots.
    [DefaultExecutionOrder(-100)]
    public sealed partial class NetworkCoop : MonoBehaviour
    {
        public static NetworkCoop Active { get; private set; }
        public static bool Running => Active != null && Active.manager != null && Active.manager.IsListening;
        public static bool IsReplica => Running && !Active.manager.IsServer;
        private NetworkManager manager;
        private UnityTransport transport;
        private CharacterInventory original, local;
        private GameObject characterPrefab;
        private readonly Dictionary<ulong, CharacterInventory> owners = new Dictionary<ulong, CharacterInventory>();
        private readonly Dictionary<ulong, string> approved = new Dictionary<ulong, string>();
        private readonly Dictionary<string, CharacterInventory> mirrors = new Dictionary<string, CharacterInventory>();
        private readonly Dictionary<ulong, RemoteInput> inputs = new Dictionary<ulong, RemoteInput>();
        private readonly Dictionary<string, int> swings = new Dictionary<string, int>();
        private readonly Dictionary<string, float> progress = new Dictionary<string, float>();
        private readonly Dictionary<string, ReplicaMotion> motion = new Dictionary<string, ReplicaMotion>();
        private readonly HashSet<string> blockedRevives = new HashSet<string>();
        private sealed class RemoteInput
        {
            public Vector2 motion;
            public string revive;
            public bool menu, interrupted, channelStarted;
            public float received, window, actionWindow;
            public int packetCount, actionCount;
            public long sequence;
        }
        private long outgoing, snapshotSequence, receivedSnapshot;
        private float nextInput, nextSnapshot;
        private string reviveIntent, address = "127.0.0.1", status = "Lokaler Koop-Test bereit.";
        private bool panel, started, quitting;
        private bool previousBackground;
        public CharacterInventory LocalCharacter => local;
        public string Status => status;
        internal Vector2 TestMotion;
        internal bool TestDriverActive;
        internal int ConnectedCount => owners.Count;
        internal IEnumerable<string> ActiveCharacterIds
        { get { foreach (var actor in owners.Values) if (actor != null) yield return actor.CharacterId; } }
        internal void TestRequest(CoopCommand command) { if (TestDriverActive && IsReplica) SendCommand(command); }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Active = null;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var session = GameSession.Instance;
        }
        private void Awake()
        {
            if (Active != null && Active != this) { Destroy(this); return; }
            Active = this;
            characterPrefab = Resources.Load<GameObject>("Coop/Player");
            var obj = new GameObject("Local cooperative transport");
            DontDestroyOnLoad(obj);
            transport = obj.AddComponent<UnityTransport>();
            manager = obj.AddComponent<NetworkManager>();
            manager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport, EnableSceneManagement = false,
                ConnectionApproval = true, ProtocolVersion = 14, TickRate = 30,
                ForceSamePrefabs = false, ClientConnectionBufferTimeout = 10
            };
            manager.ConnectionApprovalCallback = Approve;
            manager.OnClientConnectedCallback += Connected;
            manager.OnClientDisconnectCallback += Disconnected;
            manager.OnServerStarted += RegisterMessages;
            manager.OnClientStarted += RegisterMessages;
            SceneManager.sceneLoaded += AreaLoaded;
            previousBackground = Application.runInBackground;
            var args = Environment.GetCommandLineArgs();
            if (Debug.isDebugBuild && (Array.IndexOf(args, "--book-test-host") >= 0 || Array.IndexOf(args, "--book-test-client") >= 0))
                gameObject.AddComponent<SharedBookIntegrationDriver>();
            if (Debug.isDebugBuild && (Array.IndexOf(args, "--lobby-test-host") >= 0 || Array.IndexOf(args, "--lobby-test-client") >= 0))
                gameObject.AddComponent<LobbyIntegrationDriver>();
            if (Debug.isDebugBuild && (Array.IndexOf(args, "--coop-smoke-host") >= 0 || Array.IndexOf(args, "--coop-smoke-client") >= 0))
            { TestDriverActive = true; gameObject.AddComponent<CoopSmokeDriver>(); }
        }
        public bool StartHost()
        {
            if (!Prepare()) return false;
            local = original;
            transport.SetConnectionData("127.0.0.1", 7777, "0.0.0.0");
            manager.NetworkConfig.ConnectionData = HelloBytes("00000000000000000000000000000000");
            started = manager.StartHost(); panel = false;
            status = started ? "Host aktiv · UDP 7777" : "Host konnte nicht gestartet werden.";
            return started;
        }
        public bool StartClient(string hostAddress)
        {
            if (!Prepare()) return false;
            if (!System.Net.IPAddress.TryParse(hostAddress, out _)) { status = "Bitte eine gültige IP-Adresse eingeben."; return false; }
            address = hostAddress;
            var token = PlayerPrefs.GetString("SecretsReborn.Coop.CharacterToken", "");
            if (!Guid.TryParseExact(token, "N", out _))
            { token = Guid.NewGuid().ToString("N"); PlayerPrefs.SetString("SecretsReborn.Coop.CharacterToken", token); PlayerPrefs.Save(); }
            manager.NetworkConfig.ConnectionData = HelloBytes(token);
            original.ConfigureNetwork("coop-" + token, false, false);
            original.GetComponent<Rigidbody2D>().simulated = false;
            foreach (var enemy in FindObjectsByType<TreeMeleeEnemy>(FindObjectsInactive.Include, FindObjectsSortMode.None)) enemy.SetReplica(true);
            transport.SetConnectionData(address, 7777);
            started = manager.StartClient(); panel = false;
            status = started ? "Verbinde mit " + address + " …" : "Verbindung konnte nicht gestartet werden.";
            if (!started) StopAndReload();
            return started;
        }
        private bool Prepare()
        {
            if (Running || started || GameSession.Instance.Busy || SaveBook.IsOpen || GameSession.Instance.RewardPresentationActive) return false;
            original = Camera.main?.GetComponent<CameraFollow>()?.Target?.GetComponent<CharacterInventory>();
            if (original == null || characterPrefab == null) { status = "Spieler oder Koop-Prefab fehlt. Editor-Setup ausführen."; return false; }
            if (FindObjectsByType<CharacterInventory>(FindObjectsSortMode.None).Length != 1)
            { status = "Vor dem Start bitte den lokalen Test-Begleiter entfernen."; return false; }
            if (original.GetComponent<InventoryInteraction>()?.IsOpen == true || GameSession.Instance.World.CharacterVitals(original.CharacterId).IsDown)
            { status = "Inventar schließen; Koop mit lebendem Charakter starten."; return false; }
            Application.runInBackground = true;
            return true;
        }
        private byte[] HelloBytes(string token) => Encoding.UTF8.GetBytes(JsonUtility.ToJson(new CoopHello
        { characterToken = token, scene = SceneManager.GetActiveScene().path }));
        private void Approve(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            response.CreatePlayerObject = false; response.Pending = false;
            if (request.ClientNetworkId == NetworkManager.ServerClientId) { response.Approved = true; return; }
            try
            {
                if (request.Payload == null || request.Payload.Length > 1024) throw new ArgumentException();
                var hello = JsonUtility.FromJson<CoopHello>(Encoding.UTF8.GetString(request.Payload));
                string id = "coop-" + hello.characterToken;
                bool runningJoin = lobbySession && !LobbyActive && hello.scene == MenuScene;
                bool duplicate = connectionTokens.ContainsValue(hello.characterToken);
                response.Approved = (!lobbySession || LobbyActive || runningJoin) && (runningJoin || !ChangingArea && !VotePending) && CoopProtocol.ValidHello(hello, runningJoin ? MenuScene : SceneManager.GetActiveScene().path)
                    && approved.Count < CoopProtocol.MaximumPlayers - 1 && !duplicate;
                response.Reason = response.Approved ? "" : duplicate ? "Diese Spielerkennung ist bereits verbunden."
                    : approved.Count >= CoopProtocol.MaximumPlayers - 1 ? "Die Sitzung ist voll (maximal vier Spieler inklusive Host)."
                    : "Szene oder Netzwerkprotokoll passt nicht zur Host-Sitzung.";
                if (TestDriverActive) Debug.Log("Connection approval: " + response.Approved + " scene=" + hello.scene + " runningJoin=" + runningJoin + " duplicate=" + duplicate + " clients=" + approved.Count);
                if (response.Approved)
                {
                    approved[request.ClientNetworkId] = id; connectionTokens[request.ClientNetworkId] = hello.characterToken;
                    if (runningJoin) waitingGuests[request.ClientNetworkId] = new WaitingGuest { token = hello.characterToken, name = CleanName(hello.playerName,"Spieler") };
                    else if (LobbyActive) lobbyPlayers[request.ClientNetworkId] = new LobbyPlayer { client = request.ClientNetworkId, name = CleanName(hello.playerName, "Spieler " + (lobbyPlayers.Count + 1)) };
                }
            }
            catch { response.Approved = false; response.Reason = "Ungültige Verbindungsanfrage."; }
        }
        private void RegisterMessages()
        {
            manager.CustomMessagingManager.RegisterNamedMessageHandler("sr.join-notice", ReceiveJoinAnnouncement);
            manager.CustomMessagingManager.RegisterNamedMessageHandler("sr.join-offer", ReceiveJoinOffer);
            manager.CustomMessagingManager.RegisterNamedMessageHandler("sr.join-choice", ReceiveJoinChoice);
            manager.CustomMessagingManager.RegisterNamedMessageHandler("sr.join-start", ReceiveJoinStart);
            manager.CustomMessagingManager.RegisterNamedMessageHandler("sr.join-ready", ReceiveJoinReady);
            manager.CustomMessagingManager.RegisterNamedMessageHandler("sr.book", ReceiveBook);
            manager.CustomMessagingManager.RegisterNamedMessageHandler("sr.book-command", ReceiveBookCommand);
            manager.CustomMessagingManager.RegisterNamedMessageHandler("sr.lobby", ReceiveLobby);
            manager.CustomMessagingManager.RegisterNamedMessageHandler("sr.lobby-command", ReceiveLobbyCommand);
            manager.CustomMessagingManager.RegisterNamedMessageHandler("sr.command", ReceiveCommand);
            manager.CustomMessagingManager.RegisterNamedMessageHandler("sr.snapshot", ReceiveSnapshot);
            manager.CustomMessagingManager.RegisterNamedMessageHandler("sr.area", ReceiveArea);
            manager.CustomMessagingManager.RegisterNamedMessageHandler("sr.ready", ReceiveAreaReady);
            manager.CustomMessagingManager.RegisterNamedMessageHandler("sr.vote", ReceiveVote);
            manager.CustomMessagingManager.RegisterNamedMessageHandler("sr.vote-answer", ReceiveVoteAnswer);
        }
        private void Connected(ulong clientId)
        {
            if (manager.IsServer && waitingGuests.ContainsKey(clientId)) { QueueRunningGuest(clientId); return; }
            if (LobbyActive) { LobbyConnected(clientId); return; }
            if (!manager.IsServer) { status = "Mit Host verbunden."; return; }
            if (clientId == NetworkManager.ServerClientId)
            { owners[clientId] = original; original.ConfigureNetwork(original.CharacterId, true, true); }
            else
            {
                if (!approved.TryGetValue(clientId, out var id)) { manager.DisconnectClient(clientId); return; }
                var actor = CreateCharacter(id, true, false, SafeSpawn(id));
                owners[clientId] = actor; inputs[clientId] = new RemoteInput { received = Time.unscaledTime };
            }
            GameSession.Instance.RegisterSpawn(owners[clientId]);
            status = "Host · " + owners.Count + "/4 Spieler"; nextSnapshot = 0;
        }
        private Vector3 SafeSpawn(string id, bool useSaved = true)
        {
            var saved = GameSession.Instance.World.Position(id);
            if (useSaved && saved != null && saved.scenePath == SceneManager.GetActiveScene().path)
            {
                bool blocked = false;
                foreach (var hit in Physics2D.OverlapCircleAll(new Vector2(saved.x, saved.y), .4f)) if (!hit.isTrigger) blocked = true;
                if (!blocked) return new Vector3(saved.x, saved.y, saved.z);
            }
            // A clear floor position near the host, without teleporting through scenery.
            foreach (var offset in new[] { Vector2.right * 1.5f, Vector2.left * 1.5f, Vector2.down * 1.5f, Vector2.up * 1.5f })
            {
                Vector2 candidate = (Vector2)original.transform.position + offset;
                bool blocked = false;
                foreach (var hit in Physics2D.OverlapCircleAll(candidate, .4f)) if (!hit.isTrigger) blocked = true;
                if (!blocked && GameSession.Instance.ClearCombatPath(original.transform.position, candidate)) return candidate;
            }
            return original.transform.position;
        }
        private CharacterInventory CreateCharacter(string id, bool authority, bool localInput, Vector3 position)
        {
            var obj = Instantiate(characterPrefab, position, Quaternion.identity);
            obj.name = "Character " + id;
            var actor = obj.GetComponent<CharacterInventory>(); actor.ConfigureNetwork(id, authority, localInput);
            var body = obj.GetComponent<Rigidbody2D>(); body.simulated = authority;
            obj.GetComponent<PlayerMovement>().SetNetworkMotion(Vector2.zero);
            return actor;
        }
        private void Disconnected(ulong clientId)
        {
            if (quitting) return;
            if (manager.IsServer && connectionTokens.TryGetValue(clientId,out var token))
            { connectionTokens.Remove(clientId); nextJoinOffer = 0; }
            if (manager.IsServer && waitingGuests.Remove(clientId)) { approved.Remove(clientId); nextJoinOffer = 0; return; }
            if (LobbyActive && manager.IsServer && clientId != 0) { lobbyPlayers.Remove(clientId); approved.Remove(clientId); BroadcastLobby(); return; }
            if (manager.IsServer && clientId != NetworkManager.ServerClientId)
            {
                if (bookOwner == clientId && sharedBook != null) CloseSharedBook(); if (VotePending) CancelVote();
                if (owners.TryGetValue(clientId, out var actor))
                {
                    if (actor != null)
                    {
                        var p = actor.transform.position;
                        GameSession.Instance.World.SetPosition(actor.CharacterId, actor.gameObject.scene.path, p.x, p.y, p.z);
                        GameSession.Instance.CancelRevive(actor); Destroy(actor.gameObject);
                    }
                    GameSession.Instance.RemovePartyMember(approved[clientId]);
                    GameSession.Instance.ReleaseChestReward(approved[clientId]);
                    owners.Remove(clientId);
                }
                areaRoster.Remove(clientId); areaWaiting.Remove(clientId);
                approved.Remove(clientId); inputs.Remove(clientId); nextSnapshot = 0;
                lobbyPlayers.Remove(clientId);
                status = "Host · " + owners.Count + "/4 Spieler";
            }
            else if (!manager.IsServer)
            {
                status = "Verbindung beendet: " + manager.DisconnectReason;
                MainMenu.ConnectionError = status;
                if (TestDriverActive) GetComponent<CoopSmokeDriver>()?.Disconnected(); else StopAndReload();
            }
        }
        public bool OwnsLocal(CharacterInventory actor) => actor != null && actor == local;
        public static bool Request(CharacterInventory actor, CoopAction action, int from = 0, int to = 0, string target = null)
        {
            if (!IsReplica || !Active.OwnsLocal(actor)) return false;
            Active.SendCommand(new CoopCommand { action = action, from = from, to = to, target = target }); return true;
        }
        public bool SetReviveIntent(CharacterInventory actor, string target)
        {
            if (!OwnsLocal(actor)) return false;
            if (target != null && blockedRevives.Contains(actor.CharacterId)) { reviveIntent = null; return false; }
            reviveIntent = target; return true;
        }
        public bool HasReviveIntent(CharacterInventory actor) => OwnsLocal(actor) && !string.IsNullOrEmpty(reviveIntent);
        public float ReplicaProgress(CharacterInventory actor) => actor != null && progress.TryGetValue(actor.CharacterId, out var value) ? value : 0;
        public bool MenuOpen(CharacterInventory actor)
        {
            foreach (var owner in owners)
                if (owner.Value == actor && inputs.TryGetValue(owner.Key, out var input)) return input.menu;
            return false;
        }
        public void RecordSwing(CharacterInventory actor)
        { if (Running && manager.IsServer) swings[actor.CharacterId] = swings.TryGetValue(actor.CharacterId, out var count) ? count + 1 : 1; }
        private void SendCommand(CoopCommand command)
        {
            if (ChangingArea || VotePending) return;
            command.sequence = ++outgoing;
            command.areaEpoch = areaEpoch;
            Send("sr.command", NetworkManager.ServerClientId, JsonUtility.ToJson(command));
        }
        private void Send(string name, ulong recipient, string json)
        {
            int bytes = Encoding.UTF8.GetByteCount(json);
            if (bytes > 240000) { status = "Netzwerkzustand überschreitet das Testlimit."; return; }
            using (var writer = new FastBufferWriter(bytes * 2 + 16, Allocator.Temp))
            {
                writer.WriteValueSafe(json);
                manager.CustomMessagingManager.SendNamedMessage(name, recipient, writer, NetworkDelivery.ReliableFragmentedSequenced);
            }
        }
        private void ReceiveCommand(ulong sender, FastBufferReader reader)
        {
            if (!manager.IsServer || sender == NetworkManager.ServerClientId || !owners.TryGetValue(sender, out var actor)
                || !inputs.TryGetValue(sender, out var input) || reader.Length > 2048) return;
            try
            {
                reader.ReadValueSafe(out string json);
                var command = JsonUtility.FromJson<CoopCommand>(json);
                if (ChangingArea || VotePending || !CoopProtocol.Valid(command, areaEpoch) || command.sequence <= input.sequence) return;
                if (Time.unscaledTime - input.window > 1) { input.window = Time.unscaledTime; input.packetCount = 0; }
                if (++input.packetCount > 80) return;
                input.sequence = command.sequence;
                if (command.action == CoopAction.ConfirmReward)
                { GameSession.Instance.ConfirmChestReward(actor, command.target); nextSnapshot = 0; return; }
                if (command.action == CoopAction.Input)
                {
                    input.motion = Vector2.ClampMagnitude(new Vector2(command.x, command.y), 1);
                    input.revive = command.target; input.menu = command.menuOpen; input.received = Time.unscaledTime;
                    return;
                }
                if (Time.unscaledTime - input.actionWindow > 1) { input.actionWindow = Time.unscaledTime; input.actionCount = 0; }
                if (++input.actionCount > 20 || SaveBook.IsOpen || GameSession.Instance.Busy || GameSession.Instance.IsReceivingReward(actor)
                    || GameSession.Instance.World.CharacterVitals(actor.CharacterId).IsDown) return;
                switch (command.action)
                {
                    case CoopAction.MoveItem: actor.TryMove(command.from, command.to); break;
                    case CoopAction.Equip: actor.TryEquip(command.from, (EquipmentSlot)command.to); break;
                    case CoopAction.Unequip: actor.TryUnequip((EquipmentSlot)command.from, command.to); break;
                    case CoopAction.UseItem: actor.TryUseItem(command.from); break;
                    case CoopAction.Attack: GameSession.Instance.RequestMeleeAttack(actor); break;
                    case CoopAction.Lamp:
                        if (GameSession.Instance.CanFight(actor)) actor.GetComponent<PlayerLantern>().SetLit(!actor.GetComponent<PlayerLantern>().IsLit);
                        break;
                    case CoopAction.Pickup:
                        foreach (var item in FindObjectsByType<WorldItem>(FindObjectsSortMode.None)) if (item.WorldItemId == command.target) { item.TryCollect(actor); break; }
                        break;
                    case CoopAction.Chest:
                        foreach (var chest in FindObjectsByType<TreasureChest>(FindObjectsSortMode.None)) if (chest.ChestId == command.target) { chest.TryOpen(actor); break; }
                        break;
                }
                nextSnapshot = 0;
            }
            catch (Exception error) { Debug.LogWarning("Koop-Anfrage verworfen: " + error.Message); }
        }
        private void Update()
        {
            if (Keyboard.current?.f6Key.wasPressedThisFrame == true) panel = !panel;
            if (!Running) return;
            UpdateRejoins();
            if (LobbyActive) { if (manager.IsServer && Time.unscaledTime >= nextLobby) { nextLobby = Time.unscaledTime + .5f; BroadcastLobby(); } return; }
            UpdateSharedBook(); if (VotePending) { UpdateVote(); return; }
            if (ChangingArea) return;
            if (manager.IsServer)
            {
                foreach (var pair in inputs)
                {
                    if (!owners.TryGetValue(pair.Key, out var actor)) continue;
                    var input = pair.Value;
                    bool fresh = Time.unscaledTime - input.received <= .35f;
                    actor.GetComponent<PlayerMovement>().SetNetworkMotion(fresh && !input.menu ? input.motion : Vector2.zero);
                    if (!fresh || input.menu || string.IsNullOrEmpty(input.revive))
                    { input.interrupted = false; input.channelStarted = false; GameSession.Instance.CancelRevive(actor); continue; }
                    if (input.channelStarted && !GameSession.Instance.IsReviving(actor)) input.interrupted = true;
                    if (input.interrupted) continue;
                    CharacterInventory target = null;
                    foreach (var member in owners.Values) if (member.CharacterId == input.revive) { target = member; break; }
                    if (!GameSession.Instance.HoldRevive(actor, target)) input.interrupted = true;
                    else input.channelStarted = true;
                }
                if (Time.unscaledTime >= nextSnapshot) { nextSnapshot = Time.unscaledTime + .1f; Broadcast(); }
            }
            else if (local != null && manager.IsConnectedClient && Time.unscaledTime >= nextInput)
            {
                nextInput = Time.unscaledTime + .05f;
                bool menu = local.GetComponent<InventoryInteraction>().IsOpen || SaveBook.IsOpen || panel;
                var direction = TestDriverActive ? TestMotion : !menu && Application.isFocused ? local.GetComponent<PlayerMovement>().ReadLocalMotion() : Vector2.zero;
                SendCommand(new CoopCommand { action = CoopAction.Input, x = direction.x, y = direction.y, menuOpen = menu,
                    target = !menu && (Application.isFocused || TestDriverActive) ? reviveIntent : null });
            }
        }
        private void Broadcast()
        {
            var session = GameSession.Instance;
            var poses = new List<CoopActorPose>();
            foreach (var actor in owners.Values)
            {
                var p = actor.transform.position; var v = actor.GetComponent<Rigidbody2D>().linearVelocity;
                bool interrupted = false;
                foreach (var owner in owners) if (owner.Value == actor && inputs.TryGetValue(owner.Key, out var input)) interrupted = input.interrupted;
                session.World.SetPosition(actor.CharacterId, actor.gameObject.scene.path, p.x, p.y, p.z);
                poses.Add(new CoopActorPose { id = actor.CharacterId, x = p.x, y = p.y, dx = v.x, dy = v.y,
                    reviveProgress = session.ReviveProgress(actor), lamp = actor.GetComponent<PlayerLantern>().IsLit, reviveInterrupted = interrupted,
                    swing = swings.TryGetValue(actor.CharacterId, out var count) ? count : 0 });
            }
            var enemies = new List<CoopEnemyPose>();
            foreach (var enemy in FindObjectsByType<TreeMeleeEnemy>(FindObjectsInactive.Include, FindObjectsSortMode.None)) enemies.Add(enemy.CaptureReplica());
            var snapshot = new CoopSnapshot { sequence = ++snapshotSequence, areaEpoch = areaEpoch, scene = SceneManager.GetActiveScene().path,
                world = session.World.Capture(), actors = poses.ToArray(), enemies = enemies.ToArray(), gameOver = session.IsGameOver,
                paused = false, rewards = session.CaptureChestRewards() };
            foreach (var pair in owners)
            {
                if (pair.Key == NetworkManager.ServerClientId) continue;
                snapshot.localCharacter = pair.Value.CharacterId;
                Send("sr.snapshot", pair.Key, JsonUtility.ToJson(snapshot));
            }
        }
        private void ReceiveSnapshot(ulong sender, FastBufferReader reader)
        {
            if (manager.IsServer || sender != NetworkManager.ServerClientId || reader.Length > 480000) return;
            try
            {
                reader.ReadValueSafe(out string json);
                var snapshot = JsonUtility.FromJson<CoopSnapshot>(json);
                // A guest may join a running host after earlier area transitions.
                if (!ChangingArea && receivedSnapshot == 0 && snapshot != null && snapshot.areaEpoch >= 0) areaEpoch = snapshot.areaEpoch;
                if (snapshot == null || snapshot.protocol != 14 || snapshot.sequence <= receivedSnapshot || snapshot.actors == null
                    || snapshot.actors.Length > 4 || snapshot.areaEpoch != areaEpoch || snapshot.scene != SceneManager.GetActiveScene().path
                    || ChangingArea && !areaLocalReady) return;
                CoopProtocol.RestoreWireEmptySlots(snapshot.world);
                var restored = WorldSessionState.Restore(snapshot.world);
                receivedSnapshot = snapshot.sequence;
                GameSession.Instance.AcceptReplica(restored, snapshot.gameOver, snapshot.paused);
                GameSession.Instance.AcceptChestRewards(snapshot.rewards);
                var present = new HashSet<string>();
                foreach (var pose in snapshot.actors)
                {
                    present.Add(pose.id);
                    if (!mirrors.TryGetValue(pose.id, out var actor))
                    {
                        actor = pose.id == snapshot.localCharacter ? original : CreateCharacter(pose.id, false, false, new Vector3(pose.x, pose.y));
                        actor.ConfigureNetwork(pose.id, false, pose.id == snapshot.localCharacter); mirrors[pose.id] = actor;
                        swings[pose.id] = pose.swing;
                    }
                    if (pose.id == snapshot.localCharacter) local = actor;
                    if (!motion.TryGetValue(pose.id, out var blend)) { blend = new ReplicaMotion(); motion[pose.id] = blend; }
                    blend.Push(pose.x, pose.y, Time.unscaledTimeAsDouble,
                        snapshot.paused || restored.CharacterVitals(pose.id).IsDown);
                    blend.Sample(Time.unscaledTimeAsDouble, out float displayX, out float displayY);
                    actor.transform.position = new Vector3(displayX, displayY, actor.transform.position.z);
                    actor.GetComponent<CharacterAppearance>().SetPresentedMotion(new Vector2(pose.dx, pose.dy));
                    actor.RefreshSession(); actor.GetComponent<PlayerLantern>().SetLit(pose.lamp);
                    progress[pose.id] = pose.reviveProgress;
                    if (pose.reviveInterrupted) blockedRevives.Add(pose.id); else blockedRevives.Remove(pose.id);
                    if (swings[pose.id] != pose.swing)
                    {
                        swings[pose.id] = pose.swing;
                        var item = actor.Find(actor.State.GetEquipment(EquipmentSlot.MainHand));
                        actor.GetComponent<PlayerMelee>().PresentSwing(item?.Weapon, item != null);
                    }
                }
                var remove = new List<string>();
                foreach (var pair in mirrors) if (!present.Contains(pair.Key)) { Destroy(pair.Value.gameObject); remove.Add(pair.Key); }
                foreach (var id in remove) { mirrors.Remove(id); motion.Remove(id); }
                GameSession.Instance.SetReplicaParty(present);
                if (local != null && local.CharacterId == snapshot.localCharacter && present.Contains(snapshot.localCharacter)) RememberJoinedTicket();
                if (local != null && Camera.main != null) Camera.main.GetComponent<CameraFollow>().Target = local.transform;
                foreach (var item in FindObjectsByType<WorldItem>(FindObjectsInactive.Include, FindObjectsSortMode.None)) item.RefreshSession();
                foreach (var loot in FindObjectsByType<EnemyLoot>(FindObjectsSortMode.None)) loot.RefreshSession();
                foreach (var chest in FindObjectsByType<TreasureChest>(FindObjectsSortMode.None)) chest.RefreshReplica();
                foreach (var enemy in FindObjectsByType<TreeMeleeEnemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    foreach (var pose in snapshot.enemies ?? Array.Empty<CoopEnemyPose>()) if (pose.id == enemy.EnemyId) { enemy.ApplyReplica(pose); break; }
                foreach (var puzzle in FindObjectsByType<SanctuaryPuzzle>(FindObjectsSortMode.None)) puzzle.RefreshSession();
                status = "Client · " + snapshot.actors.Length + "/4 Spieler";
                if (ChangingArea) FinishArea();
            }
            catch (Exception error) { Debug.LogWarning("Koop-Zustand verworfen: " + error.Message); }
        }
        private void LateUpdate()
        {
            if (!IsReplica) return;
            foreach (var pair in mirrors)
            {
                if (pair.Value == null || !motion.TryGetValue(pair.Key, out var blend)) continue;
                blend.Sample(Time.unscaledTimeAsDouble, out float x, out float y);
                var position = pair.Value.transform.position;
                pair.Value.transform.position = new Vector3(x, y, position.z);
            }
        }
        public void StopAndReload()
        {
            if (quitting) return;
            quitting = true; started = false;
            StartCoroutine(LeaveSession());
        }
        private System.Collections.IEnumerator LeaveSession()
        {
            var path = lobbySession ? MenuScene : SceneManager.GetActiveScene().path;
            GameSession.Instance.BeginNetworkArea();
            manager.Shutdown();
            // NGO completes shutdown in its network update. Destroying the transport
            // immediately discards the disconnect packet and leaves the host waiting
            // for a timeout before the same client can return.
            double until = Time.realtimeSinceStartupAsDouble + 6;
            while (manager != null && (manager.IsListening || manager.ShutdownInProgress)
                && Time.realtimeSinceStartupAsDouble < until) yield return null;
            Application.runInBackground = previousBackground;
            foreach (var pair in owners) if (pair.Value != null) GameSession.Instance.RemovePartyMember(pair.Value.CharacterId);
            foreach (var id in areaRoster.Values) GameSession.Instance.RemovePartyMember(id);
            GameSession.Instance.ClearReplicaFlags();
            if (manager != null) Destroy(manager.gameObject);
            Destroy(GameSession.Instance.gameObject);
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadSceneAsync(path);
#endif
        }
        private void OnApplicationQuit() { quitting = true; LocalClientProfile.Release(); }
        private void OnDestroy()
        {
            quitting = true;
            SceneManager.sceneLoaded -= AreaLoaded;
            if (manager != null) { manager.Shutdown(); Destroy(manager.gameObject); }
            Application.runInBackground = previousBackground;
            if (Active == this) Active = null;
        }
        private void OnGUI()
        {
            DrawJoinAnnouncements();
            if (JoiningSession) { DrawRejoin(); return; }
            if (LobbyActive) { DrawLobby(); return; }
            if (SceneManager.GetActiveScene().path == MenuScene && !ChangingArea) return;
            if (VotePending) { DrawVote(); return; }
            if (ChangingArea) GUI.Box(new Rect(Screen.width / 2f - 180, 70, 360, 42), "Gebiet wird gemeinsam geladen …");
            if (!panel)
            {
                if (GUI.Button(new Rect(Screen.width - 155, 12, 140, 28), Running ? "Koop aktiv · F6" : "Koop-Test · F6")) panel = true;
                return;
            }
            var rect = new Rect(Screen.width / 2f - 220, Screen.height / 2f - 135, 440, 270);
            GUI.Box(rect, "LOKALER KOOP-TEST");
            GUI.Label(new Rect(rect.x + 20, rect.y + 40, 400, 45), status);
            if (!Running && !started)
            {
                GUI.Label(new Rect(rect.x + 20, rect.y + 90, 125, 25), "Host-IP:");
                address = GUI.TextField(new Rect(rect.x + 145, rect.y + 90, 270, 25), address, 45);
                if (GUI.Button(new Rect(rect.x + 20, rect.y + 130, 190, 35), "Host starten")) StartHost();
                if (GUI.Button(new Rect(rect.x + 230, rect.y + 130, 190, 35), "Beitreten")) StartClient(address);
                GUI.Label(new Rect(rect.x + 20, rect.y + 180, 400, 40), "Gleiche Szene öffnen. Zwei Fenster auf diesem PC:\nHost im Editor, Client im Test-Build (127.0.0.1).");
            }
            else if (GUI.Button(new Rect(rect.x + 20, rect.y + 130, 400, 35), "Sitzung verlassen → Solo neu starten")) StopAndReload();
            if (GUI.Button(new Rect(rect.x + 145, rect.y + 230, 150, 25), "Schließen")) panel = false;
        }
    }
}
