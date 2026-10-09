using System;
using SecretsReborn;

internal static class SharedStashChecks
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static ItemRules Rules(string id) => new ItemRules { maxStack = id == "potion" ? 10 : 1, currency = id == "gold" };
    public static void Main()
    {
        var world = new WorldSessionState("stash-world"); var a = world.CharacterInventory("a"); var b = world.CharacterInventory("b"); var stash = world.SharedStash;
        a.TryAdd("armor",1,1);
        Check(a.TryTransferTo(stash,0,0,"armor",1,Rules) && a.GetSlot(0) == null,"deposit commits both containers");
        Check(stash.TryTransferTo(b,0,0,"armor",1,Rules) && !stash.TryTransferTo(a,0,0,"armor",1,Rules),"concurrent withdrawal has one winner");
        stash.TryAdd("other-armor",1,1);
        Check(!stash.TryTransferTo(a,0,0,"armor",1,Rules) && stash.GetSlot(0).itemId == "other-armor","stale request cannot take replacement item");
        a.TryAdd("potion",6,10); stash.TryAdd("potion",5,10);
        Check(!a.TryTransferTo(stash,0,1,"potion",6,Rules) && a.Count("potion") == 6 && stash.Count("potion") == 5,"overflow rejects atomically");
        Check(!a.TryTransferTo(stash,0,2,"potion",5,Rules),"stale count rejected");
        Check(a.TryTransferTo(stash,0,2,"potion",6,Rules) && stash.Count("potion") == 11,"whole stack deposit");
        var full = new InventoryState(); full.TryAdd("armor",40,1);
        Check(!stash.TryTransferTo(full,0,0,"other-armor",1,Rules) && stash.GetSlot(0).itemId == "other-armor","full destination preserves source");
        var saved = world.Capture(); var loaded = WorldSessionState.Restore(saved);
        Check(loaded.SharedStash.Count("potion") == 11 && loaded.CharacterInventory("b").Count("armor") == 1,"world and personal state roundtrip");
        saved.sharedStash[0].count = 9; Check(loaded.SharedStash.GetSlot(0).count == 1,"save copy isolation");
        var legacy = world.Capture(); legacy.version = 14; legacy.sharedStash = null;
        Check(WorldSessionState.Restore(legacy).SharedStash.GetSlot(0) == null,"legacy world starts with empty stash");
        var wire = world.Capture(); wire.sharedStash[3] = new InventoryStack { itemId = "",count = 0 }; CoopProtocol.RestoreWireEmptySlots(wire);
        Check(WorldSessionState.Restore(wire).SharedStash.GetSlot(3) == null,"wire empty stash slots canonicalized");
        var command = new CoopCommand { sequence=1,action=CoopAction.StashWithdraw,from=0,to=1,target="stash",expectedItem="armor",expectedCount=1 };
        Check(CoopProtocol.Valid(command),"valid transfer command"); command.expectedCount=0; Check(!CoopProtocol.Valid(command),"missing count rejected");
        Console.WriteLine("PASS: atomic stash transfers, competing withdrawal, stale item/count rejection, capacity, save migration/copy isolation and network validation.");
    }
}
