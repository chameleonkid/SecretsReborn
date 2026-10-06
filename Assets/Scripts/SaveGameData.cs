using System;

namespace SecretsReborn
{
    [Serializable] public sealed class CharacterSaveData
    {
        public string characterId;
        public InventoryStack[] bag;
        public string[] equipment;
        public bool hasPosition;
        public string scenePath;
        public float x, y, z;
        public VitalsSaveData vitals;
    }
    [Serializable] public sealed class PuzzleSaveData { public string puzzleId; public int progress; }
    [Serializable] public sealed class SaveGameData
    {
        public int version = 6;
        public double playTimeSeconds;
        public string savedScenePath;
        public string worldId;
        public CharacterSaveData[] characters;
        public PuzzleSaveData[] puzzles;
        public string[] collectedItems;
        public string[] defeatedEnemies = Array.Empty<string>();
    }
}
