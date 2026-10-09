using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SecretsReborn
{
    [Serializable] public sealed class SharedBookState
    {
        public long revision;
        public bool open, load;
        public int epoch, selected, confirm = -1;
        public ulong owner;
        public string book, character, message;
        public SaveGameData[] summaries;
        public bool[] occupied;
        public string[] errors;
    }
    [Serializable] public sealed class SharedBookCommand
    { public int epoch, selected; public string action, book; public bool load; public long revision; }

    public sealed partial class NetworkCoop
    {
        private SaveBook sharedBook;
        private ulong bookOwner;
        private long bookRevision, receivedBookRevision;
        private float nextBook;
        public bool SharedBookActive => sharedBook != null && SaveBook.IsOpen;
        public bool CanControlBook => SharedBookActive && bookOwner == manager.LocalClientId;
        public string BookOperatorName
        {
            get
            {
                foreach (var player in lobbyView.players) if (player.client == bookOwner) return player.name;
                return bookOwner == 0 ? "Host" : "Mitspieler";
            }
        }
        public void BookCommand(string action, SaveBook book, int selected = 0, bool load = false)
        {
            if (!Running || LobbyActive || ChangingArea || VotePending || book == null) return;
            var command = new SharedBookCommand { action = action, book = book.BookId, selected = selected, load = load,
                epoch = areaEpoch, revision = manager.IsServer ? bookRevision : receivedBookRevision };
            if (manager.IsServer) ApplyBookCommand(0, command);
            else Send("sr.book-command", 0, JsonUtility.ToJson(command));
        }
        private void ReceiveBookCommand(ulong sender, FastBufferReader reader)
        {
            if (!manager.IsServer || reader.Length > 2048) return;
            try { reader.ReadValueSafe(out string json); ApplyBookCommand(sender, JsonUtility.FromJson<SharedBookCommand>(json)); }
            catch (Exception e) { Debug.LogWarning("Buchanfrage verworfen: " + e.Message); }
        }
        private void ApplyBookCommand(ulong sender, SharedBookCommand command)
        {
            if (command == null || command.epoch != areaEpoch || VotePending || ChangingArea || LobbyActive
                || !owners.TryGetValue(sender, out var actor) || actor == null) return;
            if (command.action == "open")
            {
                if (sharedBook != null || GameSession.Instance.Busy || GameSession.Instance.HasAnyReward
                    || GameSession.Instance.IsGameOver || actor.GetComponent<InventoryInteraction>()?.IsOpen == true) return;
                foreach (var book in FindObjectsByType<SaveBook>(FindObjectsSortMode.None))
                    if (book.BookId == command.book && book.CanOpenNetwork(actor))
                    {
                        sharedBook = book; bookOwner = sender; ++bookRevision;
                        book.OpenNetwork(actor); BroadcastBook(); return;
                    }
                return;
            }
            if (sharedBook == null || sender != bookOwner || command.book != sharedBook.BookId || command.revision != bookRevision) return;
            if (command.action == "close") { CloseSharedBook(); return; }
            if (!sharedBook.CanOpenNetwork(actor)) { CloseSharedBook(); return; }
            if (command.action == "select" && command.selected >= 0 && command.selected < 3)
                sharedBook.SelectNetwork(command.selected, command.load);
            else if (command.action == "execute") sharedBook.ExecuteNetwork();
            if (sharedBook != null) { ++bookRevision; BroadcastBook(); }
        }
        private void BroadcastBook()
        {
            var state = sharedBook != null ? sharedBook.CaptureNetwork() : new SharedBookState();
            state.owner = bookOwner; state.epoch = areaEpoch; state.revision = bookRevision;
            string json = JsonUtility.ToJson(state);
            foreach (var id in owners.Keys) if (id != 0) Send("sr.book", id, json);
        }
        private void ReceiveBook(ulong sender, FastBufferReader reader)
        {
            if (manager.IsServer || sender != 0 || reader.Length > 16000) return;
            try
            {
                reader.ReadValueSafe(out string json); var state = JsonUtility.FromJson<SharedBookState>(json);
                if (state == null || state.epoch != areaEpoch || state.revision < receivedBookRevision || VotePending || ChangingArea) return;
                receivedBookRevision = state.revision; bookOwner = state.owner;
                if (!state.open) { if (sharedBook != null) sharedBook.CloseNetwork(); sharedBook = null; return; }
                if (state.selected < 0 || state.selected > 2 || state.summaries?.Length != 3 || state.occupied?.Length != 3 || state.errors?.Length != 3) return;
                foreach (var book in FindObjectsByType<SaveBook>(FindObjectsSortMode.None)) if (book.BookId == state.book)
                { sharedBook = book; book.ApplyNetwork(state); break; }
            }
            catch (Exception e) { Debug.LogWarning("Buchzustand verworfen: " + e.Message); }
        }
        internal void CloseSharedBook()
        {
            if (sharedBook != null) sharedBook.CloseNetwork(); sharedBook = null;
            if (manager.IsServer) { ++bookRevision; BroadcastBook(); }
        }
        private void UpdateSharedBook()
        {
            if (!manager.IsServer || sharedBook == null) return;
            if (!owners.TryGetValue(bookOwner, out var actor) || !sharedBook.CanOpenNetwork(actor)) { CloseSharedBook(); return; }
            if (Time.unscaledTime >= nextBook) { nextBook = Time.unscaledTime + .1f; BroadcastBook(); }
        }
    }
}
