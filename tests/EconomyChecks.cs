using System;
using SecretsReborn;
public static class EconomyChecks
{
    static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    static ItemRules Rules(string id) => id == "gold" ? new ItemRules { currency = true, currencyUnits = 1 }
        : new ItemRules { maxStack = 10, potionKind = id == "hp" ? 1 : id == "mana" ? 2 : 0 };
    public static void Main()
    {
        var bag = new InventoryState();
        Check(bag.TryAddGold(12) && !bag.TryAddGold(-1) && !bag.TryAddGold(long.MaxValue),"gold bounds");
        Check(bag.TryAdd("hp",3,10) && bag.BindPotion(0,"hp",Rules) && !bag.BindPotion(1,"hp",Rules),"typed shortcut");
        Check(bag.TryMove(0,39,Rules) && bag.Count(bag.PotionItem(0)) == 3,"shortcut follows item, not bag index");
        Check(bag.TryConsume(39) && bag.Count("hp") == 2,"single stack consumed");
        var saved = bag.Capture("player"); var restored = InventoryState.Restore(saved);
        Check(restored.Gold == 12 && restored.PotionItem(0) == "hp" && restored.Count("hp") == 2,"save roundtrip");
        saved.potionItems[0] = "changed"; Check(restored.PotionItem(0) == "hp","copy isolation");
        Check(restored.BindPotion(0,null,Rules) && restored.Count("hp") == 2,"clear reference leaves potion in bag");
        var full = new InventoryState(); Check(full.TryAdd("hp",400,10),"fill bag");
        Check(full.TryAddBatch(new[] { new InventoryStack { itemId="gold",count=5 } },Rules) && full.Gold == 5,"gold with full bag");
        Check(!full.TryAddBatch(new[] { new InventoryStack { itemId="gold",count=7 }, new InventoryStack { itemId="mana",count=1 } },Rules) && full.Gold == 5,"atomic mixed reward");
        var legacy = InventoryState.Restore(full.Capture("player"),false,true);
        Check(legacy.Gold == 0 && legacy.NeedsEconomyMigration,"legacy defaults");
        var oldGold = new InventoryState(); oldGold.TryAdd("gold",20,999);
        // An absent legacy character can be saved in v14 before its catalog is
        // bound. Conversion must still work when it eventually joins again.
        var absent = InventoryState.Restore(oldGold.Capture("absent"));
        Check(absent.MigrateCurrency("gold", 1) && absent.Gold == 20 && absent.Count("gold") == 0,"absent character gold migration survives intermediate save");
        Check(oldGold.MigrateCurrency("gold",1) && oldGold.Gold == 20 && oldGold.Count("gold") == 0
            && !oldGold.MigrateCurrency("gold",1) && oldGold.Gold == 20,"legacy gold conversion once");
        Check(InventoryNavigation.Navigate(30,0,1) == 55 && InventoryNavigation.Navigate(55,1,0) == 56
            && InventoryNavigation.Navigate(56,0,-1) == 35,"controller quickslot navigation");
        Check(!CoopProtocol.Valid(new CoopCommand {sequence=1,action=CoopAction.UsePotion,from=2})
            && !CoopProtocol.Valid(new CoopCommand {sequence=1,action=CoopAction.BindPotion,from=0,to=-1}),"network slot validation");
        Console.WriteLine("PASS: currency bounds, atomic rewards, typed potion references, save/legacy migration, controller navigation and network validation.");
    }
}
