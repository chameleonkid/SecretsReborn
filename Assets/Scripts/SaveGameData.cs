using System;

namespace SecretsReborn
{
    [Serializable] public sealed class CharacterSaveData
    {
        public string characterId;
        public InventoryStack[] bag;
        public string[] equipment;
        public int gold;
        public string[] potionItems = new string[2];
        public bool hasPosition;
        public string scenePath;
        public float x, y, z;
        public VitalsSaveData vitals;
    }
    [Serializable] public sealed class PuzzleSaveData { public string puzzleId; public int progress; }
    [Serializable] public sealed class SaveGameData
    {
        public int version = 16;
        public InventoryStack[] sharedStash = new InventoryStack[InventoryState.Capacity];
        public string founderCharacterId;
        public string savedAtUtc;
        public WorldCharacterSlot[] savedParticipants = Array.Empty<WorldCharacterSlot>();
        public bool multiplayer;
        public WorldCharacterSlot[] characterSlots = Array.Empty<WorldCharacterSlot>();
        public double playTimeSeconds;
        public string savedScenePath;
        public string savedHostCharacterId;
        public string worldId;
        public CharacterSaveData[] characters;
        public PuzzleSaveData[] puzzles;
        public string[] collectedItems;
        public string[] defeatedEnemies = Array.Empty<string>();
        public string[] discoveredChestItems = Array.Empty<string>();
    }
}
