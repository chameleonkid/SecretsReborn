using System;
using System.Collections.Generic;

namespace SecretsReborn
{
    // Runtime host-world state. No scene objects, input, sprites or singleton character inventory.
    public sealed class WorldSessionState
    {
        public InventoryState SharedStash { get; private set; } = new InventoryState();
        public string WorldId { get; }
        public string FounderCharacterId { get; private set; }
        private string savedAtUtc;
        private WorldCharacterSlot[] savedParticipants = Array.Empty<WorldCharacterSlot>();
        public void MarkSaved(IEnumerable<string> activeIds)
        {
            var participants = new List<WorldCharacterSlot>();
            foreach (var id in activeIds)
            {
                var slot = slots.Find(s => s.id == id);
                participants.Add(new WorldCharacterSlot { id = id, name = slot != null ? slot.name : Multiplayer ? "Charakter " + (participants.Count + 1) : "Abenteurer" });
            }
            savedAtUtc = DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
            savedParticipants = participants.ToArray();
        }
        public bool Multiplayer { get; private set; }
        private readonly List<WorldCharacterSlot> slots = new List<WorldCharacterSlot>();
        public WorldCharacterSlot[] CharacterSlots => slots.ConvertAll(s => s.Copy()).ToArray();
        public WorldCharacterSlot CharacterProfile(string id) => slots.Find(s => s.id == id)?.Copy();
        public void CreateSoloProfile(string name, int hairColor, int eyeColor, string bodyStyle = null, string hairStyle = null, string eyeStyle = null)
        {
            if (Multiplayer || slots.Count != 0 || string.IsNullOrWhiteSpace(name) || name.Trim().Length > 24
                || hairColor < 0 || hairColor > 5 || eyeColor < -1 || eyeColor > 3
                || !CharacterCustomization.Valid(bodyStyle, hairStyle, eyeStyle)) throw new ArgumentException("Ungültiger Einzelspielercharakter.");
            slots.Add(new WorldCharacterSlot { id = "solo-player", name = name.Trim(), hairColor = hairColor, eyeColor = eyeColor,
                bodyStyle = bodyStyle, hairStyle = hairStyle, eyeStyle = eyeStyle });
            CharacterInventory("solo-player"); CharacterVitals("solo-player");
        }
        public void EnableMultiplayer()
        {
            Multiplayer = true;
            if (slots.Count == 0) foreach (var id in characters.Keys)
            { if (slots.Count == 4) break; slots.Add(new WorldCharacterSlot { id = id, name = "Charakter " + (slots.Count + 1) }); }
            AssignLegacyFounder();
        }
        private void AssignLegacyFounder()
        {
            if (!string.IsNullOrEmpty(FounderCharacterId) || slots.Count == 0) return;
            FounderCharacterId = slots.Exists(s => s.id == SavedHostCharacterId) ? SavedHostCharacterId : slots[0].id;
        }
        public bool DeleteWorldCharacter(string id)
        {
            if (!Multiplayer || string.IsNullOrEmpty(id) || id == FounderCharacterId || !slots.Exists(s => s.id == id)) return false;
            if (SavedHostCharacterId == id)
            {
                var anchor = Position(id);
                if (anchor != null) SetPosition(FounderCharacterId, anchor.scenePath, anchor.x, anchor.y, anchor.z);
                SavedHostCharacterId = FounderCharacterId;
            }
            slots.RemoveAll(s => s.id == id); characters.Remove(id); vitals.Remove(id); positions.Remove(id); spellBooks.Remove(id);
            return true;
        }
        public string CreateWorldCharacter(string name, int hairColor = -1, int eyeColor = -1, string bodyStyle = null, string hairStyle = null, string eyeStyle = null)
        {
            if (!Multiplayer || slots.Count >= 4 || string.IsNullOrWhiteSpace(name) || name.Trim().Length > 24)
                throw new ArgumentException("Kein freier Charakterplatz oder ungültiger Name.");
            if (hairColor < -1 || hairColor > 5 || eyeColor < -1 || eyeColor > 3 || !CharacterCustomization.Valid(bodyStyle, hairStyle, eyeStyle)) throw new ArgumentException("Ungültige Charakteroptik.");
            string id = "world-" + Guid.NewGuid().ToString("N");
            slots.Add(new WorldCharacterSlot { id = id, name = name.Trim(), hairColor = hairColor, eyeColor = eyeColor,
                bodyStyle = bodyStyle, hairStyle = hairStyle, eyeStyle = eyeStyle });
            if (string.IsNullOrEmpty(FounderCharacterId)) FounderCharacterId = id;
            CharacterInventory(id); CharacterVitals(id); return id;
        }
        public double PlayTimeSeconds { get; private set; }
        public string SavedScenePath { get; private set; }
        public string SavedHostCharacterId { get; private set; }
        public void SetSavedHost(string id) => SavedHostCharacterId = id;
        public void PreserveCharacterSlots(WorldCharacterSlot[] roster)
        {
            EnableMultiplayer();
            foreach (var slot in roster)
            {
                if (slots.Exists(s => s.id == slot.id)) continue;
                if (slots.Count >= 4) throw new ArgumentException("Charakterplätze des Spielstands widersprechen der aktiven Gruppe.");
                slots.Add(slot.Copy());
                CharacterInventory(slot.id); CharacterVitals(slot.id);
            }
        }
        public void AdvancePlayTime(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) throw new ArgumentException("Invalid playtime.");
            PlayTimeSeconds += seconds;
        }
        public void SetSavedScene(string path) => SavedScenePath = path;
        private readonly Dictionary<string, InventoryState> characters = new Dictionary<string, InventoryState>();
        private readonly Dictionary<string, CharacterVitalsState> vitals = new Dictionary<string, CharacterVitalsState>();
        private readonly Dictionary<string,SpellBookState> spellBooks=new Dictionary<string,SpellBookState>();
        public SpellBookState CharacterSpells(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Missing character ID.");
            if (!spellBooks.TryGetValue(id,out var book)) { book=new SpellBookState(); spellBooks.Add(id,book); }
            return book;
        }
        public CharacterVitalsState CharacterVitals(string characterId)
        {
            CharacterInventory(characterId);
            if (!vitals.TryGetValue(characterId, out var state))
            { state = new CharacterVitalsState(); vitals.Add(characterId, state); }
            return state;
        }
        private readonly Dictionary<string, RuneSequence> puzzles = new Dictionary<string, RuneSequence>();
        private readonly HashSet<string> collectedItems = new HashSet<string>();
        private readonly HashSet<string> defeatedEnemies = new HashSet<string>();
        private readonly HashSet<string> discoveredChestItems = new HashSet<string>();
        public bool IsChestItemDiscovered(string itemId) => discoveredChestItems.Contains(itemId);
        public bool DiscoverChestItem(string itemId) => !string.IsNullOrWhiteSpace(itemId) && discoveredChestItems.Add(itemId);
        public bool IsEnemyDefeated(string id) => defeatedEnemies.Contains(id);
        public bool DefeatEnemy(string id) => !string.IsNullOrWhiteSpace(id) && defeatedEnemies.Add(id);
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
            var data = new SaveGameData { worldId = WorldId, multiplayer = Multiplayer, characterSlots = CharacterSlots, playTimeSeconds = PlayTimeSeconds, savedScenePath = SavedScenePath, savedHostCharacterId = SavedHostCharacterId,
                founderCharacterId = FounderCharacterId, sharedStash = SharedStash.Capture("shared-stash").bag,
                savedAtUtc = savedAtUtc, savedParticipants = Array.ConvertAll(savedParticipants, s => new WorldCharacterSlot { id = s.id, name = s.name }),
                characters = new CharacterSaveData[characters.Count],
                puzzles = new PuzzleSaveData[puzzles.Count], collectedItems = new string[collectedItems.Count], defeatedEnemies = new string[defeatedEnemies.Count],
                discoveredChestItems = new string[discoveredChestItems.Count] };
            int i = 0; foreach (var pair in characters)
            {
                var character = pair.Value.Capture(pair.Key);
                character.vitals = CharacterVitals(pair.Key).Capture();
                character.spells=CharacterSpells(pair.Key).Capture();
                var position = Position(pair.Key);
                if (position != null) { character.hasPosition = true; character.scenePath = position.scenePath; character.x = position.x; character.y = position.y; character.z = position.z; }
                data.characters[i++] = character;
            }
            i = 0; foreach (var pair in puzzles) data.puzzles[i++] = new PuzzleSaveData { puzzleId = pair.Key, progress = pair.Value.Progress };
            collectedItems.CopyTo(data.collectedItems); defeatedEnemies.CopyTo(data.defeatedEnemies); discoveredChestItems.CopyTo(data.discoveredChestItems); return data;
        }
        public static WorldSessionState Restore(SaveGameData data)
        {
            if (data == null || data.version < 1 || data.version > 17 || data.characters == null || data.puzzles == null || data.collectedItems == null || data.version >= 6 && data.defeatedEnemies == null
                || data.version >= 8 && data.discoveredChestItems == null)
                throw new ArgumentException("Unsupported or incomplete savegame.");
            var world = new WorldSessionState(data.worldId);
            if (data.version >= 15) world.SharedStash = InventoryState.Restore(new CharacterSaveData { bag = data.sharedStash, equipment = new string[InventoryState.EquipmentCapacity] });
            if (data.version >= 3) world.AdvancePlayTime(data.playTimeSeconds);
            world.SavedScenePath = data.savedScenePath;
            world.SavedHostCharacterId = data.version >= 9 ? data.savedHostCharacterId : null;
            if (data.version >= 10)
            {
                if (data.savedParticipants == null || data.savedParticipants.Length > 4) throw new ArgumentException("Invalid saved participants.");
                if (!string.IsNullOrEmpty(data.savedAtUtc) && !DateTimeOffset.TryParse(data.savedAtUtc, out _)) throw new ArgumentException("Invalid save timestamp.");
                var participantIds = new HashSet<string>();
                foreach (var p in data.savedParticipants)
                    if (p == null || string.IsNullOrWhiteSpace(p.id) || string.IsNullOrWhiteSpace(p.name) || p.name.Length > 24 || !participantIds.Add(p.id)) throw new ArgumentException("Invalid saved participant.");
                world.savedAtUtc = data.savedAtUtc;
                world.savedParticipants = Array.ConvertAll(data.savedParticipants, s => new WorldCharacterSlot { id = s.id, name = s.name });
            }
            foreach (var character in data.characters)
            {
                if (character == null || string.IsNullOrWhiteSpace(character.characterId)) throw new ArgumentException("Invalid character ID.");
                world.characters.Add(character.characterId, InventoryState.Restore(character, data.version < 7, data.version < 14));
                world.spellBooks.Add(character.characterId,data.version>=17 ? SpellBookState.Restore(character.spells) : new SpellBookState());
                world.vitals.Add(character.characterId, data.version >= 5 ? CharacterVitalsState.Restore(character.vitals)
                    : data.version == 4 ? CharacterVitalsState.RestoreLegacy(character.vitals) : new CharacterVitalsState());
                if (data.version >= 2 && character.hasPosition) world.SetPosition(character.characterId, character.scenePath, character.x, character.y, character.z);
            }
            if (data.version >= 9)
            {
                if (data.characterSlots == null || data.characterSlots.Length > 4) throw new ArgumentException("Invalid character slots.");
                world.Multiplayer = data.multiplayer;
                var ids = new HashSet<string>();
                foreach (var slot in data.characterSlots)
                {
                    if (slot == null || !world.characters.ContainsKey(slot.id ?? "") || !ids.Add(slot.id)
                        || string.IsNullOrWhiteSpace(slot.name) || slot.name.Length > 24) throw new ArgumentException("Invalid character slot.");
                    if (data.version >= 12 && (slot.hairColor < -1 || slot.hairColor > 5 || slot.eyeColor < -1 || slot.eyeColor > 3)) throw new ArgumentException("Invalid character appearance.");
                    if (data.version >= 13 && !CharacterCustomization.Valid(slot.bodyStyle, slot.hairStyle, slot.eyeStyle)) throw new ArgumentException("Invalid character layer IDs.");
                    var migratedSlot = data.version >= 12 ? slot.Copy() : new WorldCharacterSlot { id = slot.id, name = slot.name };
                    if (data.version < 13) migratedSlot.bodyStyle = migratedSlot.hairStyle = migratedSlot.eyeStyle = null;
                    world.slots.Add(migratedSlot);
                }
            }
            if (data.version >= 11)
            {
                world.FounderCharacterId = data.founderCharacterId;
                if (world.Multiplayer && world.slots.Count > 0 && !world.slots.Exists(s => s.id == world.FounderCharacterId))
                    throw new ArgumentException("Invalid founder character.");
            }
            else if (world.Multiplayer) world.AssignLegacyFounder();
            foreach (var puzzle in data.puzzles)
            {
                if (puzzle == null || string.IsNullOrWhiteSpace(puzzle.puzzleId) || puzzle.progress < 0 || puzzle.progress > 4)
                    throw new ArgumentException("Invalid puzzle.");
                var sequence = new RuneSequence(); for (int step = 0; step < puzzle.progress; step++) sequence.Enter(step);
                world.puzzles.Add(puzzle.puzzleId, sequence);
            }
            foreach (var id in data.collectedItems)
                if (string.IsNullOrWhiteSpace(id) || !world.collectedItems.Add(id)) throw new ArgumentException("Invalid collected item ID.");
            if (data.version >= 6) foreach (var id in data.defeatedEnemies)
                if (string.IsNullOrWhiteSpace(id) || !world.defeatedEnemies.Add(id)) throw new ArgumentException("Invalid defeated enemy ID.");
            if (data.version >= 8) foreach (var id in data.discoveredChestItems)
                if (string.IsNullOrWhiteSpace(id) || !world.discoveredChestItems.Add(id)) throw new ArgumentException("Invalid discovered chest item ID.");
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
