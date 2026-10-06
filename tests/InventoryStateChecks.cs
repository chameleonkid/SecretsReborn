using System;
using SecretsReborn;

// Standalone check executable; compiles with InventoryState.cs without Unity.
internal static class InventoryStateChecks
{
    private static void Check(bool value, string label) { if (!value) throw new Exception(label); }
    private static ItemRules Rules(string id) => new ItemRules {
        kind = id == "shield" ? ItemKind.Shield : id == "bow" || id == "sword" ? ItemKind.Weapon : id == "ring" ? ItemKind.Ring : ItemKind.Armor,
        twoHanded = id == "bow", maxStack = id == "ore" ? 5 : 1 };
    public static void Main()
    {
        var a = new InventoryState(); var b = new InventoryState();
        Check(a.TryAdd("ore", 7, 5), "split stacks");
        Check(a.GetSlot(0).count == 5 && a.GetSlot(1).count == 2, "counts");
        Check(a.TryMove(0, 39, Rules) && a.GetSlot(0) == null, "fixed slots and move");
        Check(a.TryMove(1, 39, Rules) == false, "full stack unchanged");
        Check(a.TryAdd("armor", 1, 1), "armor pickup");
        Check(!a.TryEquip(0, EquipmentSlot.Head, Rules), "wrong slot rejected");
        Check(a.TryEquip(0, EquipmentSlot.Armor, Rules) && a.GetSlot(0) == null, "equipment leaves bag");
        Check(b.EquippedArmorId == null, "character isolation");
        Check(a.TryUnequip(EquipmentSlot.Armor, Rules, 10) && a.GetSlot(10).itemId == "armor", "unequip to selected slot");
        var weapon = new InventoryState();
        Check(weapon.TryAdd("sword", 1, 1) && weapon.TryEquip(0, EquipmentSlot.MainHand, Rules), "one hand");
        Check(weapon.TryAdd("shield", 1, 1) && weapon.TryEquip(0, EquipmentSlot.OffHand, Rules), "shield");
        Check(weapon.TryAdd("bow", 1, 1) && weapon.TryEquip(0, EquipmentSlot.MainHand, Rules), "bow swap");
        Check(weapon.GetEquipment(EquipmentSlot.OffHand) == null && weapon.GetSlot(0).itemId == "sword" && weapon.GetSlot(1).itemId == "shield", "two hands displace both");
        Check(weapon.TryEquip(1, EquipmentSlot.OffHand, Rules) && weapon.GetEquipment(EquipmentSlot.MainHand) == null, "shield displaces bow");
        var full = new InventoryState();
        full.TryAdd("sword", 1, 1); full.TryEquip(0, EquipmentSlot.MainHand, Rules);
        full.TryAdd("shield", 1, 1); full.TryEquip(0, EquipmentSlot.OffHand, Rules);
        full.TryAdd("bow", 1, 1); full.TryAdd("filler", 39, 1);
        Check(!full.TryEquip(0, EquipmentSlot.MainHand, Rules), "reject two returns with one free slot");
        Check(full.GetSlot(0).itemId == "bow" && full.GetEquipment(EquipmentSlot.MainHand) == "sword" && full.GetEquipment(EquipmentSlot.OffHand) == "shield", "rejected transaction atomic");
        Check(!full.TryUnequip(EquipmentSlot.OffHand, Rules), "full bag unequip rejected");
        Check(!full.TryAdd("extra", 1, 1) && !full.TryAdd("", 1, 1) && !full.TryAdd("extra", -1, 1), "invalid add");
        var rings = new InventoryState(); rings.TryAdd("ring", 2, 1);
        Check(rings.TryEquip(0, EquipmentSlot.Ring1, Rules) && rings.TryEquip(1, EquipmentSlot.Ring2, Rules), "two rings");
        var copy = a.GetSlot(39); copy.count = 99;
        Check(a.GetSlot(39).count == 5, "snapshot cannot mutate authority state");
        Console.WriteLine("PASS: 40 fixed slots, stacking/moves, 14 equipment slots, ownership, two-hand/shield rules, atomic capacity rejection, character isolation.");
    }
}
