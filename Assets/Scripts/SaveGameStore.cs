namespace SecretsReborn
{
    public static class SaveGameStore
    {
        public const string DefaultPath = "SecretsReborn/world.es3";
        private const string Key = "world-session";
        public static string SlotPath(int slot)
        {
            if (slot < 0 || slot > 2) throw new System.ArgumentOutOfRangeException(nameof(slot));
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
            string path = SlotPath(slot);
            if (!Exists(path)) return null;
            var data = ES3.Load<SaveGameData>(Key, Settings(path));
            WorldSessionState.Restore(data);
            return data;
        }
    }
}
