using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SecretsReborn
{
    public sealed partial class NetworkCoop
    {
        public bool VotePending { get; private set; }
        private CoopVote vote;
        private string voteEntrance;
        private WorldSessionState voteRestore;
        private readonly HashSet<ulong> voteAccepted = new HashSet<ulong>();
        private long voteSequence, lastVote;
        private double voteDeadline;
        private bool voteAnswered;
        private GroupVoteCanvasView voteView;
        public IReadOnlyList<CoopVotePlayer> VotePlayers => vote?.players ?? Array.Empty<CoopVotePlayer>();
        public bool VoteAnswered => voteAnswered;
        public string VoteHeading => vote?.retry==true ? "GEMEINSAM ERNEUT VERSUCHEN?" : vote?.load==true ? "SPIELSTAND GEMEINSAM LADEN?" : "GEBIET GEMEINSAM WECHSELN?";
        public string VoteDestination => vote==null ? "" : System.IO.Path.GetFileNameWithoutExtension(vote.scene).Replace("-Editable","")+(vote.initialCheckpoint ? " · Start-Checkpoint" : "");
        public int VoteSecondsRemaining => Mathf.Max(0,(int)Math.Ceiling(voteDeadline-(manager.IsServer ? 0 : 30)-Time.realtimeSinceStartupAsDouble));
        public GroupVoteCanvasView VoteView => voteView;
        internal bool TestVoteAuto = true;

        private bool StartVote(string scene, string entrance, WorldSessionState restored, bool retry = false)
        {
            if (VotePending || ChangingArea || GameSession.Instance.HasAnyReward) return false;
            vote = new CoopVote { id = ++voteSequence, scene = scene, load = restored != null, retry = retry,
                initialCheckpoint = retry && !GameSession.Instance.HasSavedCheckpoint };
            vote.players=owners.OrderBy(x=>x.Key).Select(x=>new CoopVotePlayer {client=x.Key,name=VotePlayerName(x.Key,x.Value)}).ToArray();
            voteEntrance = entrance; voteRestore = restored; voteAccepted.Clear();
            ShowVote();
            foreach (var client in owners.Keys)
                if (client != NetworkManager.ServerClientId) Send("sr.vote", client, JsonUtility.ToJson(vote));
            return true;
        }
        private void ShowVote()
        {
            CloseSharedBook();
            VotePending = true; voteAnswered = false; lastVote = vote.id;
            voteDeadline = Time.realtimeSinceStartupAsDouble + (manager.IsServer ? 30 : 60);
            GameSession.Instance.BeginNetworkArea(); reviveIntent = null; panel = false;
            foreach (var body in FindObjectsByType<Rigidbody2D>(FindObjectsSortMode.None)) body.linearVelocity = Vector2.zero;
            if(voteView==null)
            {
                var prefab=Resources.Load<GameObject>("Interface/GroupVoteCanvas");
                if(prefab!=null) { voteView=Instantiate(prefab,transform).GetComponent<GroupVoteCanvasView>(); voteView.Bind(this); }
            }
            if(voteView!=null) voteView.Show();
        }
        private string VotePlayerName(ulong client,CharacterInventory actor)
        {
            string character=null;
            foreach(var c in GameSession.Instance.World.CharacterSlots) if(c.id==actor.CharacterId) character=c.name;
            string name=lobbyPlayers.TryGetValue(client,out var player) ? player.name : character ?? "Spieler "+client;
            if(!string.IsNullOrEmpty(character) && !string.Equals(name,character,StringComparison.OrdinalIgnoreCase)) name+=" · "+character;
            return name+(client==0 ? " (Host)" : "");
        }
        private static bool ValidVotePlayers(CoopVotePlayer[] players)
        {
            if(players==null || players.Length<1 || players.Length>CoopProtocol.MaximumPlayers) return false;
            var ids=new HashSet<ulong>();
            foreach(var p in players) if(p==null || string.IsNullOrWhiteSpace(p.name) || p.name.Length>64 || !ids.Add(p.client)) return false;
            return true;
        }
        private void ReceiveVote(ulong sender, FastBufferReader reader)
        {
            if (manager.IsServer || sender != NetworkManager.ServerClientId || reader.Length > 2048) return;
            try
            {
                reader.ReadValueSafe(out string json);
                var message = JsonUtility.FromJson<CoopVote>(json);
                if (message == null) return;
                if (message.cancel)
                { if (VotePending && message.id == vote.id) EndVote(); return; }
                if(message.progress)
                {
                    if(VotePending && message.id==vote.id && ValidVotePlayers(message.players)) vote.players=message.players;
                    return;
                }
                if (message.id <= lastVote || VotePending || ChangingArea || string.IsNullOrEmpty(message.scene)) return;
                if(!ValidVotePlayers(message.players)) return;
                vote = message; ShowVote();
            }
            catch (Exception error) { Debug.LogWarning("Abstimmung verworfen: " + error.Message); }
        }
        internal void AnswerVote(bool yes)
        {
            if (!VotePending || voteAnswered) return;
            voteAnswered = true;
            if (manager.IsServer) CountVote(NetworkManager.ServerClientId, yes);
            else Send("sr.vote-answer", NetworkManager.ServerClientId, JsonUtility.ToJson(new CoopVote { id = vote.id, answer = yes }));
        }
        private void ReceiveVoteAnswer(ulong sender, FastBufferReader reader)
        {
            if (!manager.IsServer || !VotePending || !owners.ContainsKey(sender) || reader.Length > 2048) return;
            try
            {
                reader.ReadValueSafe(out string json);
                var answer = JsonUtility.FromJson<CoopVote>(json);
                if (answer != null && answer.id == vote.id && !voteAccepted.Contains(sender)) CountVote(sender, answer.answer);
            }
            catch (Exception error) { Debug.LogWarning("Abstimmungsantwort verworfen: " + error.Message); }
        }
        private void CountVote(ulong sender, bool yes)
        {
            if (!yes) { CancelVote(); return; }
            voteAccepted.Add(sender);
            foreach(var player in vote.players) player.accepted=voteAccepted.Contains(player.client);
            var progress=new CoopVote { id=vote.id, progress=true, players=vote.players };
            foreach(var client in owners.Keys) if(client!=NetworkManager.ServerClientId) Send("sr.vote",client,JsonUtility.ToJson(progress));
            if (voteAccepted.Count != owners.Count) return;
            VotePending = false;
            if(voteView!=null) voteView.Hide();
            StartAreaTransfer(vote.scene, voteEntrance, voteRestore);
            voteRestore = null;
        }
        private void CancelVote()
        {
            vote.cancel = true;
            foreach (var client in owners.Keys)
                if (client != NetworkManager.ServerClientId) Send("sr.vote", client, JsonUtility.ToJson(vote));
            EndVote();
        }
        private void EndVote()
        {
            VotePending = false; voteRestore = null; GameSession.Instance.EndNetworkArea();
            if(voteView!=null) voteView.Hide();
            status = "Abgebrochen: Die Gruppe hat nicht vollständig zugestimmt.";
        }
        private void UpdateVote()
        {
            if (Time.realtimeSinceStartupAsDouble > voteDeadline)
            { if (manager.IsServer) CancelVote(); else StopAndReload(); return; }
            if (TestDriverActive) { if (TestVoteAuto) AnswerVote(true); return; }
            if (!Application.isFocused) return;
            var key = Keyboard.current; var pad = Gamepad.current;
            if (key?.escapeKey.wasPressedThisFrame == true || pad?.buttonEast.wasPressedThisFrame == true) AnswerVote(false);
            else if (key?.enterKey.wasPressedThisFrame == true || pad?.buttonSouth.wasPressedThisFrame == true) AnswerVote(true);
        }
        private void DrawVote()
        {
            if(voteView!=null && voteView.Visible) return;
            var rect = new Rect(Screen.width / 2f - 240, Screen.height / 2f - 100, 480, 200);
            GUI.Box(rect, vote.retry ? "GEMEINSAM ERNEUT VERSUCHEN?" : vote.load ? "SPIELSTAND GEMEINSAM LADEN?" : "GEBIET GEMEINSAM WECHSELN?");
            GUI.Label(new Rect(rect.x + 20, rect.y + 42, 440, 65),
                System.IO.Path.GetFileNameWithoutExtension(vote.scene) + (vote.initialCheckpoint ? " · Start-Checkpoint" : "")
                + "\nAlle verbundenen Spieler müssen zustimmen.");
            if (voteAnswered) GUI.Label(new Rect(rect.x + 20, rect.y + 115, 440, 35), "Bestätigt – warte auf die Gruppe …");
            else
            {
                if (GUI.Button(new Rect(rect.x + 20, rect.y + 120, 210, 40), "Ja (Enter / A)")) AnswerVote(true);
                if (GUI.Button(new Rect(rect.x + 250, rect.y + 120, 210, 40), "Nein (Esc / B)")) AnswerVote(false);
            }
        }
    }
}
