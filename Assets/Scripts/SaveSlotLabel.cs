using System;
using System.Globalization;
using System.IO;
namespace SecretsReborn
{
    public static class SaveSlotLabel
    {
        public static string Details(SaveGameData data)
        {
            if (data == null) return "Noch keine Reise gespeichert";
            string place = string.IsNullOrEmpty(data.savedScenePath) ? "Ort unbekannt" : Path.GetFileNameWithoutExtension(data.savedScenePath).Replace("-Editable", "");
            long seconds = (long)Math.Max(0, data.playTimeSeconds);
            string duration = data.version < 3 ? "Spielzeit unbekannt" : "Spielzeit " + (seconds / 3600).ToString("00") + ":" + (seconds / 60 % 60).ToString("00") + ":" + (seconds % 60).ToString("00");
            string date = DateTimeOffset.TryParse(data.savedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var saved)
                ? saved.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss") : "Speicherzeit unbekannt";
            string participants = data.savedParticipants != null && data.savedParticipants.Length > 0
                ? string.Join(", ", Array.ConvertAll(data.savedParticipants, s => s.name)) : "Teilnehmer unbekannt (alter Spielstand)";
            return place + " · " + duration + "\n" + date + "\n" + participants;
        }
    }
}
