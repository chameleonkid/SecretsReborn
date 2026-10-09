using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SecretsReborn
{
    [Serializable] public sealed class JoinAnnouncement { public long sequence; public string player; }
    public sealed partial class NetworkCoop
    {
        private readonly HashSet<ulong> announcedPlayers = new HashSet<ulong>();
        private readonly Queue<string> joinAnnouncements = new Queue<string>();
        private long announcementSequence, receivedAnnouncement;
        private string visibleAnnouncement;
        private float announcementUntil;
        internal string LatestJoinAnnouncement { get; private set; }
        private void AnnounceActiveJoins()
        {
            if (!manager.IsServer) return;
            foreach (var pair in owners)
            {
                if (pair.Key == 0 || !announcedPlayers.Add(pair.Key)) continue;
                string name = lobbyPlayers.TryGetValue(pair.Key,out var player) ? player.name : "Spieler";
                var notice = new JoinAnnouncement { sequence = ++announcementSequence, player = CleanName(name,"Spieler") };
                ShowJoinAnnouncement(notice.player);
                foreach (var id in owners.Keys) if (id != 0) Send("sr.join-notice",id,JsonUtility.ToJson(notice));
            }
        }
        private void ReceiveJoinAnnouncement(ulong sender, FastBufferReader reader)
        {
            if (manager.IsServer || sender != 0 || reader.Length > 1024) return;
            var notice = ReadJoin<JoinAnnouncement>(reader);
            if (notice == null || notice.sequence <= receivedAnnouncement || string.IsNullOrWhiteSpace(notice.player) || notice.player.Length > 24) return;
            receivedAnnouncement = notice.sequence; ShowJoinAnnouncement(notice.player);
        }
        private void ShowJoinAnnouncement(string name)
        {
            LatestJoinAnnouncement = name + " ist beigetreten.";
            if (joinAnnouncements.Count < 4) joinAnnouncements.Enqueue(LatestJoinAnnouncement);
        }
        private void DrawJoinAnnouncements()
        {
            if (Time.unscaledTime >= announcementUntil)
            {
                visibleAnnouncement = joinAnnouncements.Count > 0 ? joinAnnouncements.Dequeue() : null;
                announcementUntil = visibleAnnouncement != null ? Time.unscaledTime + 5 : 0;
            }
            if (visibleAnnouncement == null) return;
            float width = Mathf.Min(Screen.width-20,520);
            var rect = new Rect((Screen.width-width)/2,20,width,55);
            ForestInventorySkin.Panel(rect);
            var style = MenuArt.Label(20,TextAnchor.MiddleCenter); style.richText = false;
            GUI.Label(rect,visibleAnnouncement,style);
        }
    }
}
