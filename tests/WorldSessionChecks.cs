using System;
using SecretsReborn;

internal static class WorldSessionChecks
{
    private static void Check(bool value, string label) { if (!value) throw new Exception(label); }
    public static void Main()
    {
        var world = new WorldSessionState("world-a");
        var character = world.CharacterInventory("player-a");
        var vitals = world.CharacterVitals("player-a");
        Check(vitals.Health == 6 && vitals.Mana == 50, "starting vitals");
        Check(!vitals.Damage(0) && !vitals.Damage(-1) && !vitals.SpendMana(-1), "invalid amounts rejected");
        Check(vitals.Damage(1) && vitals.Health == 5, "damage");
        Check(vitals.SpendMana(20) && vitals.Mana == 30 && !vitals.SpendMana(31) && vitals.Mana == 30, "mana cost atomic");
        Check(ReferenceEquals(vitals, world.CharacterVitals("player-a")), "scene rebind retains vitals");
        Check(world.CharacterVitals("player-b").Health == 6 && new WorldSessionState("world-b").CharacterVitals("player-a").Mana == 50, "vitals character/world separation");
        character.TryAdd("armor", 1, 1);
        Check(character.TryEquip(0, EquipmentSlot.Armor, id => new ItemRules { kind = ItemKind.Armor, maxStack = 1 }), "equip");
        // A new scene component resolves by ID instead of constructing replacement state.
        var rebound = world.CharacterInventory("player-a");
        Check(ReferenceEquals(character, rebound) && rebound.EquippedArmorId == "armor", "scene rebind retains equipment");
        Check(world.CharacterInventory("player-b").EquippedArmorId == null, "character separation");
        Check(new WorldSessionState("world-b").CharacterInventory("player-a").EquippedArmorId == null, "world separation");
        Check(world.TryCollect("pickup-1", () => rebound.TryAdd("ring", 1, 1)), "pickup");
        Check(world.IsCollected("pickup-1") && !world.TryCollect("pickup-1", () => throw new Exception("must not award twice")), "pickup persisted and unique");
        Check(!world.TryCollect("pickup-2", () => false) && !world.IsCollected("pickup-2"), "failed receive releases claim");
        Check(world.TryCollect("pickup-2", () => true), "retry possible");
        Check(world.TryCollect("pickup-3", () => !world.TryCollect("pickup-3", () => true)), "reentrant duplicate blocked");
        var puzzle = world.Puzzle("sanctuary"); puzzle.Enter(0); puzzle.Enter(1);
        Check(world.Puzzle("sanctuary").Progress == 2, "partial puzzle survives rebind");
        world.Puzzle("sanctuary").Enter(2); world.Puzzle("sanctuary").Enter(3);
        Check(world.Puzzle("sanctuary").IsComplete && !world.Puzzle("other-puzzle").IsComplete, "completion scoped");
        Check(!new WorldSessionState("world-a").IsCollected("pickup-1"), "fresh session reset");
        world.SetPosition("player-a", "sanctuary", 2, -3, 0);
        world.AdvancePlayTime(3601.25); world.SetSavedScene("sanctuary");
        Check(character.TryAdd("rune-lamp", 1, 1) && character.TryEquip(1, EquipmentSlot.Lamp,
            id => new ItemRules { kind = ItemKind.Lamp, maxStack = 1 }), "lamp equipped for save");
        var snapshot = world.Capture(); var restored = WorldSessionState.Restore(snapshot);
        Check(restored.CharacterInventory("player-a").GetEquipment(EquipmentSlot.Lamp) == "rune-lamp"
            && restored.CharacterInventory("player-b").GetEquipment(EquipmentSlot.Lamp) == null, "lamp save roundtrip and per-character isolation");
        var oldSlots = world.Capture(); oldSlots.version = 6;
        foreach (var c in oldSlots.characters) Array.Resize(ref c.equipment, 14);
        var upgraded = WorldSessionState.Restore(oldSlots);
        Check(upgraded.CharacterInventory("player-a").GetEquipment(EquipmentSlot.Lamp) == null
            && upgraded.CharacterInventory("player-a").EquippedArmorId == "armor", "v6 upgrade preserves armor and appends empty lamp slot");
        oldSlots.version = 7; bool shortSlotsRejected = false;
        try { WorldSessionState.Restore(oldSlots); } catch (ArgumentException) { shortSlotsRejected = true; }
        Check(shortSlotsRejected, "v7 requires new equipment dimensions");
        Check(world.DefeatEnemy("oak-01") && !world.DefeatEnemy("oak-01"), "enemy defeat unique");
        Check(WorldSessionState.Restore(world.Capture()).IsEnemyDefeated("oak-01"), "defeat survives save and scene rebind");
        Check(!new WorldSessionState("world-b").IsEnemyDefeated("oak-01"), "enemy defeat world isolation");
        var legacyEnemy = world.Capture(); legacyEnemy.version = 5; legacyEnemy.defeatedEnemies = null;
        Check(!WorldSessionState.Restore(legacyEnemy).IsEnemyDefeated("oak-01"), "legacy saves have no defeated enemies");
        var badEnemies = world.Capture(); badEnemies.defeatedEnemies = new[] { "oak-01", "oak-01" }; bool duplicateEnemy = false;
        try { WorldSessionState.Restore(badEnemies); } catch (ArgumentException) { duplicateEnemy = true; }
        Check(duplicateEnemy, "duplicate enemy IDs rejected");
        Check(restored.CharacterVitals("player-a").Health == 5 && restored.CharacterVitals("player-a").Mana == 30, "vitals roundtrip");
        snapshot.characters[0].vitals.health = 1;
        Check(vitals.Health == 5 && restored.CharacterVitals("player-a").Health == 5, "vitals snapshot isolation");
        var oldVitals = world.Capture(); oldVitals.version = 3;
        foreach (var c in oldVitals.characters) c.vitals = null;
        Check(WorldSessionState.Restore(oldVitals).CharacterVitals("player-a").Health == 6, "legacy vitals migration");
        var corrupt = world.Capture(); corrupt.characters[0].vitals.health = 7; bool badVitals = false;
        try { WorldSessionState.Restore(corrupt); } catch (ArgumentException) { badVitals = true; }
        Check(badVitals, "invalid vitals rejected");
        corrupt = world.Capture(); corrupt.characters[0].vitals = null; badVitals = false;
        try { WorldSessionState.Restore(corrupt); } catch (ArgumentException) { badVitals = true; }
        Check(badVitals, "missing version 5 vitals rejected");
        Check(vitals.Damage(int.MaxValue) && vitals.IsDown && !vitals.SpendMana(1) && !vitals.Damage(1), "zero HP boundary");
        Check(vitals.Heal(int.MaxValue) && vitals.Health == 6 && !vitals.Heal(1), "heal clamps without overflow");
        Check(vitals.RestoreMana(int.MaxValue) && vitals.Mana == 50 && !vitals.RestoreMana(1), "mana restores without overflow");
        var hearts = new CharacterVitalsState();
        Check(hearts.HeartContainers == 3 && hearts.HeartFill(0) == 2 && hearts.HeartFill(2) == 2, "three full starting hearts");
        hearts.Damage(3);
        Check(hearts.HeartFill(0) == 2 && hearts.HeartFill(1) == 1 && hearts.HeartFill(2) == 0, "full half and empty hearts");
        Check(hearts.AddHeartContainer() && hearts.MaxHealth == 8 && hearts.Health == 5, "container grows capacity and heals one heart");
        while (hearts.HeartContainers < 20) Check(hearts.AddHeartContainer(), "container growth");
        Check(!hearts.AddHeartContainer() && hearts.MaxHealth == 40, "twenty heart cap");
        var savedHearts = CharacterVitalsState.Restore(hearts.Capture());
        Check(savedHearts.HeartContainers == 20 && savedHearts.Health == hearts.Health, "container capacity roundtrip");
        var legacyHp = world.Capture(); legacyHp.version = 4;
        foreach (var c in legacyHp.characters) c.vitals = new VitalsSaveData { health = 75, maxHealth = 100, mana = 30, maxMana = 50 };
        var migrated = WorldSessionState.Restore(legacyHp).CharacterVitals("player-a");
        Check(migrated.Health == 5 && migrated.MaxHealth == 6 && migrated.Mana == 30, "100 HP migration retains ratio and mana");
        Check(CharacterVitalsState.RestoreLegacy(new VitalsSaveData { health = 1, maxHealth = 100 }).Health == 1, "migration preserves living player");
        Check(CharacterVitalsState.RestoreLegacy(new VitalsSaveData { health = 0, maxHealth = 100 }).IsDown, "migration preserves down state");
        foreach (int invalidMaximum in new[] { 0, 5, 7, 42 })
        {
            bool invalidHearts = false;
            try { CharacterVitalsState.Restore(new VitalsSaveData { maxHealth = invalidMaximum }); } catch (ArgumentException) { invalidHearts = true; }
            Check(invalidHearts, "invalid heart capacity rejected");
        }
        Check(restored.Position("player-a").scenePath == "sanctuary" && restored.Position("player-a").y == -3, "position roundtrip");
        Check(restored.PlayTimeSeconds == 3601.25 && restored.SavedScenePath == "sanctuary", "playtime and scene metadata");
        var legacy = world.Capture(); legacy.version = 2;
        Check(WorldSessionState.Restore(legacy).PlayTimeSeconds == 0, "older save defaults to zero playtime");
        var badTime = world.Capture(); badTime.playTimeSeconds = double.NaN; bool timeRejected = false;
        try { WorldSessionState.Restore(badTime); } catch (ArgumentException) { timeRejected = true; }
        Check(timeRejected, "invalid playtime rejected");
        Check(restored.CharacterInventory("player-a").EquippedArmorId == "armor" && restored.Puzzle("sanctuary").IsComplete
            && restored.IsCollected("pickup-1"), "save snapshot roundtrip");
        snapshot.characters[0].equipment[2] = null;
        Check(world.CharacterInventory("player-a").EquippedArmorId == "armor" && restored.CharacterInventory("player-a").EquippedArmorId == "armor", "snapshot copies state");
        snapshot.version = 999; bool rejected = false;
        try { WorldSessionState.Restore(snapshot); } catch (ArgumentException) { rejected = true; }
        Check(rejected, "unknown schema rejected");
        var invalid = world.Capture(); invalid.characters[0].bag = new InventoryStack[2]; rejected = false;
        try { WorldSessionState.Restore(invalid); } catch (ArgumentException) { rejected = true; }
        Check(rejected, "invalid dimensions rejected");
        Console.WriteLine("PASS: HP/mana boundaries, costs, snapshot isolation, v3 migration and invalid v5 rejection; scene rebinding, world/character isolation, inventory/equipment, pickups and puzzle state.");
    }
}
