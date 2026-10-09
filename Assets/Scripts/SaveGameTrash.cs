using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace SecretsReborn
{
    [Serializable] public sealed class TrashedSave
    { public string id, originalPath, deletedAtUtc; }
    public static class SaveGameTrash
    {
        private static string Root => SaveGameStore.TestSlotRoot != null ? Path.Combine(SaveGameStore.TestSlotRoot, "trash")
            : new ES3Settings("SecretsReborn/trash", ES3.Location.File).FullPath;
        private static void CheckMenu()
        {
            if (NetworkCoop.Running || SceneManager.GetActiveScene().path != NetworkCoop.MenuScene)
                throw new InvalidOperationException("Spielstände nur im Hauptmenü verwalten.");
        }
        private static bool ValidSlot(string path)
        {
            for (int i = 0; i < 3; i++)
                if (path == SaveGameStore.MultiplayerSlotPath(i) || path == "SecretsReborn/slot-" + (i + 1) + ".es3"
                    || SaveGameStore.TestSlotRoot != null && path == SaveGameStore.SlotPath(i)) return true;
            return false;
        }
        private static string Folder(string id)
        { if (!Guid.TryParseExact(id, "N", out _)) throw new ArgumentException("Ungültiger Papierkorbeintrag."); return Path.Combine(Root, id); }
        public static string MoveToTrash(string path)
        {
            CheckMenu(); if (!ValidSlot(path)) throw new ArgumentException("Kein Spielstand-Slot.");
            string source = new ES3Settings(path, ES3.Location.File).FullPath;
            if (!File.Exists(source)) throw new FileNotFoundException("Dieser Slot ist leer.");
            var entry = new TrashedSave { id = Guid.NewGuid().ToString("N"), originalPath = path, deletedAtUtc = DateTime.UtcNow.ToString("O") };
            string folder = Folder(entry.id); Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "entry.json"), JsonUtility.ToJson(entry));
            if (File.Exists(source + ".bac")) File.Copy(source + ".bac", Path.Combine(folder, "slot.es3.bac"));
            File.Move(source, Path.Combine(folder, "slot.es3"));
            // Keep the backup with the recoverable slot, rather than at its now empty path.
            if (File.Exists(source + ".bac")) File.Delete(source + ".bac");
            return entry.id;
        }
        public static TrashedSave[] List()
        {
            CheckMenu(); var entries = new List<TrashedSave>();
            if (!Directory.Exists(Root)) return entries.ToArray();
            foreach (string folder in Directory.GetDirectories(Root))
                try
                {
                    var entry = JsonUtility.FromJson<TrashedSave>(File.ReadAllText(Path.Combine(folder, "entry.json")));
                    if (entry != null && Folder(entry.id) == folder && ValidSlot(entry.originalPath) && File.Exists(Path.Combine(folder, "slot.es3"))) entries.Add(entry);
                }
                catch (Exception) { /* Ignore incomplete entries; never remove their files. */ }
            entries.Sort((a, b) => string.CompareOrdinal(b.deletedAtUtc, a.deletedAtUtc)); return entries.ToArray();
        }
        public static SaveGameData Summary(string id) => SaveGameStore.ReadSummary(Path.Combine(Folder(id), "slot.es3"));
        public static void Restore(string id)
        {
            CheckMenu(); string folder = Folder(id);
            var entry = JsonUtility.FromJson<TrashedSave>(File.ReadAllText(Path.Combine(folder, "entry.json")));
            if (entry == null || entry.id != id || !ValidSlot(entry.originalPath)) throw new ArgumentException("Ungültiger Papierkorbeintrag.");
            string target = new ES3Settings(entry.originalPath, ES3.Location.File).FullPath;
            if (File.Exists(target)) throw new IOException("Der ursprüngliche Slot ist belegt. Zuerst diesen Slot freigeben.");
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            if (File.Exists(Path.Combine(folder, "slot.es3.bac")) && !File.Exists(target + ".bac"))
                File.Copy(Path.Combine(folder, "slot.es3.bac"), target + ".bac");
            File.Move(Path.Combine(folder, "slot.es3"), target);
        }
    }
}
