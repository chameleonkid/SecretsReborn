namespace SecretsReborn
{
    public static class SaveGameStore
    {
        public const string DefaultPath = "SecretsReborn/world.es3";
        public const string MultiplayerPath = "SecretsReborn/multiplayer.es3";
        public static string MultiplayerSlotPath(int slot)
        {
            if (slot < 0 || slot > 2) throw new System.ArgumentOutOfRangeException(nameof(slot));
            // Slot 1 keeps the existing filename, so previously saved worlds remain available.
            return slot == 0 ? MultiplayerPath : "SecretsReborn/multiplayer-slot-" + (slot + 1) + ".es3";
        }
        private const string Key = "world-session";
        internal static string TestSlotRoot;
        public static string SlotPath(int slot)
        {
            if (slot < 0 || slot > 2) throw new System.ArgumentOutOfRangeException(nameof(slot));
            if (TestSlotRoot != null) return System.IO.Path.Combine(TestSlotRoot, "slot-" + slot + ".es3");
            if (GameSession.Existing != null && GameSession.Existing.World.Multiplayer) return MultiplayerSlotPath(slot);
            return "SecretsReborn/slot-" + (slot + 1) + ".es3";
        }
        private static ES3Settings Settings(string path) => new ES3Settings(path, ES3.Location.File);
        public static void Save(WorldSessionState world, string path = DefaultPath)
        {
            string temporary = path + ".pending";
            ES3.Save(Key, world.Capture(), Settings(temporary));
            // Validate the serialized snapshot before replacing the last working save.
            WorldSessionState.Restore(ES3.Load<SaveGameData>(Key, Settings(temporary)));
            if (Exists(path)) ES3.CreateBackup(Settings(path));
            ES3.CopyFile(Settings(temporary), Settings(path));
            ES3.DeleteFile(Settings(temporary));
        }
        public static WorldSessionState Load(string path = DefaultPath) => WorldSessionState.Restore(ES3.Load<SaveGameData>(Key, Settings(path)));
        public static bool Exists(string path = DefaultPath) => ES3.FileExists(Settings(path));
        public static SaveGameData ReadSummary(int slot)
        {
            return ReadSummary(SlotPath(slot));
        }
        public static SaveGameData ReadSummary(string path)
        {
            if (!Exists(path)) return null;
            var data = ES3.Load<SaveGameData>(Key, Settings(path));
            WorldSessionState.Restore(data);
            return data;
        }
    }
}
