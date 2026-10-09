using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace SecretsReborn
{
    public sealed class SharedBookIntegrationDriver : MonoBehaviour
    {
        private bool host, finished;
        private string report;
        private float deadline;
        private void Awake()
        {
            var args = Environment.GetCommandLineArgs(); host = Array.IndexOf(args, "--book-test-host") >= 0;
            int i = Array.IndexOf(args, "--book-test-report"); report = i < 0 ? null : args[i + 1];
            deadline = Time.unscaledTime + 80;
            NetworkCoop.Active.TestDriverActive = true;
            SaveGameStore.TestSlotRoot = Path.GetFullPath("Temp/BookTest-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(SaveGameStore.TestSlotRoot);
        }
        private void Update() { if (!finished && Time.unscaledTime > deadline) Finish("FAIL: shared book timeout: " + NetworkCoop.Active.Status); }
        private void Finish(string result)
        {
            if (finished) return; finished = true;
            if (host) for (int i = 0; i < 3; i++) if (SaveGameStore.Exists(SaveGameStore.SlotPath(i))) ES3.DeleteFile(SaveGameStore.SlotPath(i));
            Debug.Log(result); if (report != null) File.WriteAllText(report, result); Application.Quit(result.StartsWith("PASS") ? 0 : 1);
        }
        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(1);
            var net = NetworkCoop.Active;
            if (host) GameSession.Instance.World.EnableMultiplayer();
            if (!(host ? net.StartHost() : net.StartClient("127.0.0.1"))) { Finish("FAIL: transport start"); yield break; }
            if (host)
            {
                foreach (var enemy in FindObjectsByType<TreeMeleeEnemy>(FindObjectsSortMode.None)) enemy.enabled = false;
                while (net.ConnectedCount < 2) yield return null;
                var book = FindFirstObjectByType<SaveBook>(); CharacterInventory guest = null;
                foreach (var actor in FindObjectsByType<CharacterInventory>(FindObjectsSortMode.None)) if (actor != net.LocalCharacter) guest = actor;
                GameSession.Warp(guest, book.transform.position); GameSession.Warp(net.LocalCharacter, book.transform.position + Vector3.right * .8f);
                while (!SaveBook.IsOpen) yield return null;
                if (net.CanControlBook) { Finish("FAIL: observer owns book"); yield break; }
                while (!SaveGameStore.Exists(SaveGameStore.SlotPath(1))) yield return null;
                net.BookCommand("select", book, 2);
                if (book.SelectedSlot != 1 || SaveGameStore.SlotPath(0) == SaveGameStore.SlotPath(1)) { Finish("FAIL: observer control or slot paths"); yield break; }
                while (!SaveGameStore.Exists(SaveGameStore.SlotPath(0)) || !SaveGameStore.Exists(SaveGameStore.SlotPath(2))) yield return null;
                if (SaveGameStore.Load(SaveGameStore.SlotPath(1)).SavedHostCharacterId != net.LocalCharacter.CharacterId)
                { Finish("FAIL: save anchored to opener rather than host"); yield break; }
                var metadata = SaveGameStore.ReadSummary(1);
                if (metadata.savedParticipants.Length != 2 || !DateTimeOffset.TryParse(metadata.savedAtUtc, out _))
                { Finish("FAIL: persisted participant names/timestamp"); yield break; }
                GameSession.Instance.World.DiscoverChestItem("book-test:rollback");
                while (!net.VotePending && !net.ChangingArea) yield return null;
                while (net.VotePending || net.ChangingArea) yield return null;
                if (SaveBook.IsOpen || GameSession.Instance.World.IsChestItemDiscovered("book-test:rollback")) { Finish("FAIL: client load not applied"); yield break; }
                var nextBook = FindFirstObjectByType<SaveBook>();
                foreach (var actor in FindObjectsByType<CharacterInventory>(FindObjectsSortMode.None))
                    if (actor != net.LocalCharacter) GameSession.Warp(actor, nextBook.transform.position);
                while (!SaveBook.IsOpen) yield return null;
                while (net.ConnectedCount > 1) yield return null;
                if (SaveBook.IsOpen) { Finish("FAIL: book remained locked after owner disconnect"); yield break; }
                Finish("PASS: client opens shared book; observers cannot control; three host-side slots; host anchor; unanimous client-triggered load; owner disconnect releases book.");
            }
            else
            {
                while (net.LocalCharacter == null) yield return null;
                var book = FindFirstObjectByType<SaveBook>();
                while (!book.CanOpenNetwork(net.LocalCharacter)) yield return null;
                net.BookCommand("open", book);
                while (!SaveBook.IsOpen || !net.CanControlBook) yield return null;
                foreach (int slot in new[] { 1, 0, 2 })
                {
                    net.BookCommand("select", book, slot); while (book.SelectedSlot != slot) yield return null;
                    yield return new WaitForSecondsRealtime(.15f);
                    net.BookCommand("execute", book);
                    while (book.BookMessage != "Slot " + (slot + 1) + " gespeichert.") yield return null;
                }
                yield return new WaitForSecondsRealtime(.5f);
                var summary = book.CaptureNetwork().summaries[1];
                if (summary.savedParticipants?.Length != 2 || !DateTimeOffset.TryParse(summary.savedAtUtc, out _)
                    || SaveGameStore.Exists(SaveGameStore.SlotPath(1)))
                { Finish("FAIL: client summary metadata or client wrote save file"); yield break; }
                net.BookCommand("select", book, 1, true);
                yield return new WaitForSecondsRealtime(.3f); net.BookCommand("execute", book);
                while (!net.VotePending && !net.ChangingArea) yield return null;
                while (net.VotePending || net.ChangingArea) yield return null;
                book = FindFirstObjectByType<SaveBook>();
                while (!book.CanOpenNetwork(net.LocalCharacter)) yield return null;
                net.BookCommand("open", book);
                while (!SaveBook.IsOpen) yield return null;
                yield return new WaitForSecondsRealtime(.3f);
                Finish("PASS: guest book control and replicated selection/results; all three slots saved by host; guest-triggered shared load; disconnect during book.");
            }
        }
    }
}
