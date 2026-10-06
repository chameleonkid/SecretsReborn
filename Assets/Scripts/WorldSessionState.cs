using System;
using System.Collections.Generic;

namespace SecretsReborn
{
    // Runtime host-world state. No scene objects, input, sprites or singleton character inventory.
    public sealed class WorldSessionState
    {
        public string WorldId { get; }
        public double PlayTimeSeconds { get; private set; }
        public string SavedScenePath { get; private set; }
        public void AdvancePlayTime(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) throw new ArgumentException("Invalid playtime.");
            PlayTimeSeconds += seconds;
        }
        public void SetSavedScene(string path) => SavedScenePath = path;
        private readonly Dictionary<string, InventoryState> characters = new Dictionary<string, InventoryState>();
        private readonly Dictionary<string, CharacterVitalsState> vitals = new Dictionary<string, CharacterVitalsState>();
        public CharacterVitalsState CharacterVitals(string characterId)
        {
            CharacterInventory(characterId);
            if (!vitals.TryGetValue(characterId, out var state))
            { state = new CharacterVitalsState(); vitals.Add(characterId, state); }
            return state;
        }
        private readonly Dictionary<string, RuneSequence> puzzles = new Dictionary<string, RuneSequence>();
        private readonly HashSet<string> collectedItems = new HashSet<string>();
        private readonly Dictionary<string, CharacterSaveData> positions = new Dictionary<string, CharacterSaveData>();
        public void SetPosition(string characterId, string scenePath, float x, float y, float z)
        {
            if (string.IsNullOrWhiteSpace(scenePath) || float.IsNaN(x) || float.IsInfinity(x) || float.IsNaN(y) || float.IsInfinity(y) || float.IsNaN(z) || float.IsInfinity(z))
                throw new ArgumentException("Invalid character position.");
            positions[characterId] = new CharacterSaveData { hasPosition = true, scenePath = scenePath, x = x, y = y, z = z };
        }
        public CharacterSaveData Position(string characterId)
        {
            if (!positions.TryGetValue(characterId, out var value)) return null;
            return new CharacterSaveData { hasPosition = true, scenePath = value.scenePath, x = value.x, y = value.y, z = value.z };
        }
        public WorldSessionState(string worldId)
        {
            if (string.IsNullOrWhiteSpace(worldId)) throw new ArgumentException("World ID required.");
            WorldId = worldId;
        }
        public InventoryState CharacterInventory(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId)) throw new ArgumentException("Character ID required.");
            if (!characters.TryGetValue(characterId, out var state))
            { state = new InventoryState(); characters.Add(characterId, state); }
            return state;
        }
        public RuneSequence Puzzle(string puzzleId)
        {
            if (string.IsNullOrWhiteSpace(puzzleId)) throw new ArgumentException("Puzzle ID required.");
            if (!puzzles.TryGetValue(puzzleId, out var state))
            { state = new RuneSequence(); puzzles.Add(puzzleId, state); }
            return state;
        }
        public bool IsCollected(string worldItemId) => collectedItems.Contains(worldItemId);
        public SaveGameData Capture()
        {
            var data = new SaveGameData { worldId = WorldId, playTimeSeconds = PlayTimeSeconds, savedScenePath = SavedScenePath,
                characters = new CharacterSaveData[characters.Count],
                puzzles = new PuzzleSaveData[puzzles.Count], collectedItems = new string[collectedItems.Count] };
            int i = 0; foreach (var pair in characters)
            {
                var character = pair.Value.Capture(pair.Key);
                character.vitals = CharacterVitals(pair.Key).Capture();
                var position = Position(pair.Key);
                if (position != null) { character.hasPosition = true; character.scenePath = position.scenePath; character.x = position.x; character.y = position.y; character.z = position.z; }
                data.characters[i++] = character;
            }
            i = 0; foreach (var pair in puzzles) data.puzzles[i++] = new PuzzleSaveData { puzzleId = pair.Key, progress = pair.Value.Progress };
            collectedItems.CopyTo(data.collectedItems); return data;
        }
        public static WorldSessionState Restore(SaveGameData data)
        {
            if (data == null || data.version < 1 || data.version > 4 || data.characters == null || data.puzzles == null || data.collectedItems == null)
                throw new ArgumentException("Unsupported or incomplete savegame.");
            var world = new WorldSessionState(data.worldId);
            if (data.version >= 3) world.AdvancePlayTime(data.playTimeSeconds);
            world.SavedScenePath = data.savedScenePath;
            foreach (var character in data.characters)
            {
                if (character == null || string.IsNullOrWhiteSpace(character.characterId)) throw new ArgumentException("Invalid character ID.");
                world.characters.Add(character.characterId, InventoryState.Restore(character));
                world.vitals.Add(character.characterId, data.version >= 4 ? CharacterVitalsState.Restore(character.vitals) : new CharacterVitalsState());
                if (data.version >= 2 && character.hasPosition) world.SetPosition(character.characterId, character.scenePath, character.x, character.y, character.z);
            }
            foreach (var puzzle in data.puzzles)
            {
                if (puzzle == null || string.IsNullOrWhiteSpace(puzzle.puzzleId) || puzzle.progress < 0 || puzzle.progress > 4)
                    throw new ArgumentException("Invalid puzzle.");
                var sequence = new RuneSequence(); for (int step = 0; step < puzzle.progress; step++) sequence.Enter(step);
                world.puzzles.Add(puzzle.puzzleId, sequence);
            }
            foreach (var id in data.collectedItems)
                if (string.IsNullOrWhiteSpace(id) || !world.collectedItems.Add(id)) throw new ArgumentException("Invalid collected item ID.");
            return world;
        }
        public bool TryCollect(string worldItemId, Func<bool> receive)
        {
            if (string.IsNullOrWhiteSpace(worldItemId) || receive == null || !collectedItems.Add(worldItemId)) return false;
            // Claim first to prevent reentrant requests from awarding the same item twice.
            try
            {
                if (receive()) return true;
                collectedItems.Remove(worldItemId); return false;
            }
            catch { collectedItems.Remove(worldItemId); throw; }
        }
    }
}
