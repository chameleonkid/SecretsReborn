using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsReborn
{
    public sealed partial class NetworkCoop
    {
        public bool ChangingArea { get; private set; }
        private int areaEpoch;
        private CoopAreaMessage pendingArea;
        private bool areaLocalReady;
        private string areaLocalId;
        private readonly Dictionary<ulong, string> areaRoster = new Dictionary<ulong, string>();
        private readonly HashSet<ulong> areaWaiting = new HashSet<ulong>();
        private readonly Dictionary<string, bool> areaLamps = new Dictionary<string, bool>();

        internal bool BeginArea(AreaPortal portal)
        {
            if (!Running || !manager.IsServer || ChangingArea || portal == null) return false;
            try { GameSession.ValidateScene(portal.TargetScenePath); }
            catch (Exception error) { status = error.Message; return false; }
            return StartVote(portal.TargetScenePath, portal.TargetEntranceId, null);
        }
        internal bool BeginLoad(WorldSessionState restored, string scene, bool retry = false)
        {
            if (!Running || !manager.IsServer || ChangingArea || VotePending || restored == null) return false;
            GameSession.ValidateScene(scene);
            if (GameSession.Instance.World.Multiplayer) restored.PreserveCharacterSlots(GameSession.Instance.World.CharacterSlots);
            return StartVote(scene, null, restored, retry);
        }
        private void StartAreaTransfer(string scene, string entrance, WorldSessionState restored)
        {
            if (restored != null && GameSession.Instance.World.Multiplayer)
                restored.PreserveCharacterSlots(GameSession.Instance.World.CharacterSlots);
            areaRoster.Clear(); areaWaiting.Clear(); areaLamps.Clear();
            foreach (var pair in owners)
            {
                areaRoster[pair.Key] = pair.Value.CharacterId;
                areaLamps[pair.Value.CharacterId] = pair.Value.GetComponent<PlayerLantern>().IsLit;
                var p = pair.Value.transform.position;
                GameSession.Instance.World.SetPosition(pair.Value.CharacterId, pair.Value.gameObject.scene.path, p.x, p.y, p.z);
                if (pair.Key != NetworkManager.ServerClientId) areaWaiting.Add(pair.Key);
            }
            pendingArea = new CoopAreaMessage { epoch = ++areaEpoch, scene = scene, entrance = entrance, load = restored != null };
            LockArea();
            if (restored != null) GameSession.Instance.ReplaceNetworkWorld(restored, !vote.retry);
            foreach (var client in areaWaiting) Send("sr.area", client, JsonUtility.ToJson(pendingArea));
            StartCoroutine(LoadArea());
        }

        private void LockArea()
        {
            ChangingArea = true; areaLocalReady = false;
            areaLocalId = local != null ? local.CharacterId : original != null ? original.CharacterId : lobbyLocalCharacter;
            reviveIntent = null; panel = false;
            GameSession.Instance.BeginNetworkArea();
            foreach (var body in FindObjectsByType<Rigidbody2D>(FindObjectsSortMode.None)) body.linearVelocity = Vector2.zero;
            foreach (var input in inputs.Values)
            { input.motion = Vector2.zero; input.revive = null; input.menu = false; input.channelStarted = false; input.interrupted = false; }
            status = "Gruppe lädt das nächste Gebiet …";
        }

        private void ReceiveArea(ulong sender, FastBufferReader reader)
        {
            if (manager.IsServer || sender != NetworkManager.ServerClientId || ChangingArea || reader.Length > 2048) return;
            try
            {
                reader.ReadValueSafe(out string json);
                var message = JsonUtility.FromJson<CoopAreaMessage>(json);
                if (message == null || message.epoch != areaEpoch + 1 || !message.load && string.IsNullOrWhiteSpace(message.entrance)) return;
                GameSession.ValidateScene(message.scene);
                LobbyActive = false; VotePending = false; pendingArea = message; areaEpoch = message.epoch; LockArea(); StartCoroutine(LoadArea());
            }
            catch (Exception error) { status = "Gebietswechsel fehlgeschlagen: " + error.Message; StopAndReload(); }
        }

        private IEnumerator LoadArea()
        {
            AsyncOperation load;
            try { load = GameSession.OpenScene(pendingArea.scene); }
            catch (Exception error) { Debug.LogError(error); StopAndReload(); yield break; }
            yield return load;
            yield return null;
            if (!areaLocalReady) { Debug.LogError("Koop-Eintrittspunkt oder Spielfigur fehlt."); StopAndReload(); yield break; }
            if (!manager.IsServer) Send("sr.ready", NetworkManager.ServerClientId, JsonUtility.ToJson(pendingArea));
            double until = Time.realtimeSinceStartupAsDouble + (manager.IsServer ? 30 : 60);
            while (ChangingArea && (!manager.IsServer || areaWaiting.Count > 0) && Time.realtimeSinceStartupAsDouble < until) yield return null;
            if (!ChangingArea || quitting) yield break;
            if (!manager.IsServer) { status = "Host bestätigt den Gebietswechsel nicht."; StopAndReload(); yield break; }
            // A stalled client must not freeze the remaining party indefinitely.
            foreach (var client in new List<ulong>(areaWaiting)) manager.DisconnectClient(client);
            areaWaiting.Clear();
            foreach (var pair in new List<KeyValuePair<ulong, CharacterInventory>>(owners))
                if (pair.Key != NetworkManager.ServerClientId && !manager.ConnectedClients.ContainsKey(pair.Key)) Disconnected(pair.Key);
            GameSession.Instance.World.SetSavedScene(pendingArea.scene);
            Broadcast(); FinishArea(); nextSnapshot = 0;
        }

        private void ReceiveAreaReady(ulong sender, FastBufferReader reader)
        {
            if (!manager.IsServer || !ChangingArea || !areaWaiting.Contains(sender) || reader.Length > 2048) return;
            try
            {
                reader.ReadValueSafe(out string json);
                var ready = JsonUtility.FromJson<CoopAreaMessage>(json);
                if (CoopProtocol.MatchesAreaReady(ready, pendingArea))
                    areaWaiting.Remove(sender);
            }
            catch (Exception error) { Debug.LogWarning("Ungültige Ladebestätigung: " + error.Message); }
        }

        private void AreaLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!ChangingArea || scene.path != pendingArea.scene || quitting) return;
            try
            {
                AreaEntrance entrance = null;
                foreach (var candidate in FindObjectsByType<AreaEntrance>(FindObjectsSortMode.None))
                    if (candidate.EntranceId == pendingArea.entrance)
                    { if (entrance != null) throw new Exception("Doppelter Eintrittspunkt"); entrance = candidate; }
                if (!pendingArea.load && entrance == null) throw new Exception("Eintrittspunkt fehlt: " + pendingArea.entrance);
                var authored = FindObjectsByType<CharacterInventory>(FindObjectsSortMode.None);
                if (authored.Length != 1) throw new Exception("Zielgebiet benötigt genau eine Basis-Spielfigur.");
                original = authored[0]; local = original;
                mirrors.Clear(); motion.Clear(); progress.Clear(); blockedRevives.Clear(); swings.Clear();
                if (manager.IsServer)
                {
                    owners.Clear();
                    original.ConfigureNetwork(areaRoster[NetworkManager.ServerClientId], true, true);
                    GameSession.Warp(original, LoadedPosition(original.CharacterId, pendingArea.load ? original.transform.position : entrance.transform.position));
                    owners[NetworkManager.ServerClientId] = original;
                    foreach (var pair in areaRoster)
                    {
                        if (pair.Key == NetworkManager.ServerClientId) continue;
                        // The loaded host position is the party's common starting point.
                        // Historical guest coordinates must not split the active party.
                        owners[pair.Key] = CreateCharacter(pair.Value, true, false, SafeSpawn(pair.Value, false));
                    }
                    foreach (var actor in owners.Values)
                    {
                        actor.GetComponent<PlayerLantern>().SetLit(!pendingArea.load && areaLamps.TryGetValue(actor.CharacterId, out bool lit) && lit);
                        GameSession.Instance.RegisterSpawn(actor);
                    }
                }
                else
                {
                    original.ConfigureNetwork(areaLocalId, false, true);
                    original.GetComponent<Rigidbody2D>().simulated = false;
                    if (!pendingArea.load) GameSession.Warp(original, entrance.transform.position);
                    foreach (var enemy in FindObjectsByType<TreeMeleeEnemy>(FindObjectsInactive.Include, FindObjectsSortMode.None)) enemy.SetReplica(true);
                }
                if (Camera.main != null)
                {
                    Camera.main.GetComponent<CameraFollow>().Target = local.transform;
                    GameSession.Warp(local, local.transform.position);
                }
                Physics2D.SyncTransforms(); areaLocalReady = true;
            }
            catch (Exception error) { Debug.LogError("Koop-Gebietswechsel: " + error.Message); }
        }

        private void FinishArea()
        {
            ChangingArea = false; GameSession.Instance.EndNetworkArea();
            status = "Gruppe hat das Gebiet betreten.";
            if (manager.IsServer) AnnounceActiveJoins();
        }
        private Vector3 LoadedPosition(string id, Vector3 fallback)
        {
            var p = pendingArea.load ? GameSession.Instance.World.Position(id) : null;
            return p != null && p.scenePath == pendingArea.scene ? new Vector3(p.x, p.y, p.z) : fallback;
        }
    }
}
