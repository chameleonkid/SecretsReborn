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
        Check(vitals.Health == 100 && vitals.Mana == 50, "starting vitals");
        Check(!vitals.Damage(0) && !vitals.Damage(-1) && !vitals.SpendMana(-1), "invalid amounts rejected");
        Check(vitals.Damage(25) && vitals.Health == 75, "damage");
        Check(vitals.SpendMana(20) && vitals.Mana == 30 && !vitals.SpendMana(31) && vitals.Mana == 30, "mana cost atomic");
        Check(ReferenceEquals(vitals, world.CharacterVitals("player-a")), "scene rebind retains vitals");
        Check(world.CharacterVitals("player-b").Health == 100 && new WorldSessionState("world-b").CharacterVitals("player-a").Mana == 50, "vitals character/world separation");
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
        var snapshot = world.Capture(); var restored = WorldSessionState.Restore(snapshot);
        Check(restored.CharacterVitals("player-a").Health == 75 && restored.CharacterVitals("player-a").Mana == 30, "vitals roundtrip");
        snapshot.characters[0].vitals.health = 1;
        Check(vitals.Health == 75 && restored.CharacterVitals("player-a").Health == 75, "vitals snapshot isolation");
        var oldVitals = world.Capture(); oldVitals.version = 3;
        foreach (var c in oldVitals.characters) c.vitals = null;
        Check(WorldSessionState.Restore(oldVitals).CharacterVitals("player-a").Health == 100, "legacy vitals migration");
        var corrupt = world.Capture(); corrupt.characters[0].vitals.health = 101; bool badVitals = false;
        try { WorldSessionState.Restore(corrupt); } catch (ArgumentException) { badVitals = true; }
        Check(badVitals, "invalid vitals rejected");
        corrupt = world.Capture(); corrupt.characters[0].vitals = null; badVitals = false;
        try { WorldSessionState.Restore(corrupt); } catch (ArgumentException) { badVitals = true; }
        Check(badVitals, "missing version 4 vitals rejected");
        Check(vitals.Damage(int.MaxValue) && vitals.IsDown && !vitals.SpendMana(1) && !vitals.Damage(1), "zero HP boundary");
        Check(vitals.Heal(int.MaxValue) && vitals.Health == 100 && !vitals.Heal(1), "heal clamps without overflow");
        Check(vitals.RestoreMana(int.MaxValue) && vitals.Mana == 50 && !vitals.RestoreMana(1), "mana restores without overflow");
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
        Console.WriteLine("PASS: HP/mana boundaries, costs, snapshot isolation, v3 migration and invalid v4 rejection; scene rebinding, world/character isolation, inventory/equipment, pickups and puzzle state.");
    }
}
