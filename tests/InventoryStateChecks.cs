using System;
using SecretsReborn;

// Standalone check executable; compiles with InventoryState.cs without Unity.
internal static class InventoryStateChecks
{
    private static void Check(bool value, string label) { if (!value) throw new Exception(label); }
    private static ItemRules Rules(string id) => new ItemRules {
        kind = id == "lamp" ? ItemKind.Lamp : id == "shield" ? ItemKind.Shield : id == "bow" || id == "sword" ? ItemKind.Weapon : id == "ring" ? ItemKind.Ring : ItemKind.Armor,
        twoHanded = id == "bow", maxStack = id == "ore" ? 5 : 1 };
    public static void Main()
    {
        var a = new InventoryState(); var b = new InventoryState();
        int lamp = 40 + (int)EquipmentSlot.Lamp, feet = 40 + (int)EquipmentSlot.Feet, offHand = 40 + (int)EquipmentSlot.OffHand;
        Check(InventoryNavigation.Navigate(feet, 1, 0) == lamp && InventoryNavigation.Navigate(offHand, -1, 0) == lamp, "lamp reachable horizontally from both columns");
        Check(InventoryNavigation.Navigate(feet, 0, 1) == lamp && InventoryNavigation.Navigate(offHand, 0, 1) == lamp, "lamp reachable downward");
        Check(InventoryNavigation.Navigate(lamp, -1, 0) == feet && InventoryNavigation.Navigate(lamp, 1, 0) == offHand
            && InventoryNavigation.Navigate(lamp, 0, -1) == feet, "lamp selection exits in every supported direction");
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
        Check(weapon.TryAdd("lamp", 1, 1), "lamp pickup");
        int lampIndex = -1;
        for (int i = 0; i < InventoryState.Capacity; i++) if (weapon.GetSlot(i)?.itemId == "lamp") lampIndex = i;
        Check(!weapon.TryEquip(lampIndex, EquipmentSlot.OffHand, Rules), "lamp rejects shield slot");
        Check(weapon.TryEquip(lampIndex, EquipmentSlot.Lamp, Rules), "dedicated lamp slot");
        int bowIndex = -1;
        for (int i = 0; i < InventoryState.Capacity; i++) if (weapon.GetSlot(i)?.itemId == "bow") bowIndex = i;
        Check(weapon.TryEquip(bowIndex, EquipmentSlot.MainHand, Rules) && weapon.GetEquipment(EquipmentSlot.Lamp) == "lamp", "two handed weapon preserves lamp");
        Check(InventoryState.Restore(weapon.Capture("player")).GetEquipment(EquipmentSlot.Lamp) == "lamp", "lamp roundtrip");
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
        Console.WriteLine("PASS: 40 fixed slots, 15 equipment slots, lamp roundtrip and two-hand independence, stacking/moves, atomic capacity rejection, character isolation.");
    }
}
