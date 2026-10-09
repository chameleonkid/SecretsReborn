using System;
using SecretsReborn;

public static class CoopProtocolChecks
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public static void Main()
    {
        var hello = new CoopHello { characterToken = Guid.NewGuid().ToString("N"), scene = "forest" };
        Check(CoopProtocol.ValidHello(hello, "forest"), "valid handshake");
        Check(!CoopProtocol.ValidHello(hello, "cave"), "different scene");
        hello.protocol = 1; Check(!CoopProtocol.ValidHello(hello, "forest"), "version mismatch");
        hello.protocol = 17; hello.characterToken = "solo-player"; Check(!CoopProtocol.ValidHello(hello, "forest"), "host identity spoof");
        var input = new CoopCommand { sequence = 1, action = CoopAction.Input, x = 1, y = 1 };
        Check(CoopProtocol.Valid(input), "diagonal input");
        input.x = float.NaN; Check(!CoopProtocol.Valid(input), "NaN");
        input.x = float.PositiveInfinity; Check(!CoopProtocol.Valid(input), "infinity");
        input.x = 20; Check(!CoopProtocol.Valid(input), "oversized movement");
        input.x = 0; input.sequence = 0; Check(!CoopProtocol.Valid(input), "missing sequence");
        Check(CoopProtocol.Valid(new CoopCommand { sequence = 1, action = CoopAction.Equip, from = 39, to = 14 }), "lamp equip");
        Check(!CoopProtocol.Valid(new CoopCommand { sequence = 1, action = CoopAction.Equip, from = 40, to = 14 }), "invalid bag slot");
        Check(!CoopProtocol.Valid(new CoopCommand { sequence = 1, action = CoopAction.Equip, from = 0, to = 15 }), "invalid equipment slot");
        Check(CoopProtocol.Valid(new CoopCommand { sequence = 1, action = CoopAction.Unequip, from = 14, to = -1 }), "automatic bag destination");
        Check(!CoopProtocol.Valid(new CoopCommand { sequence = 1, action = CoopAction.Pickup }), "missing target");
        Check(!CoopProtocol.Valid(new CoopCommand { sequence = 1, action = (CoopAction)999 }), "unknown action");
        Check(!CoopProtocol.Valid(new CoopCommand { sequence = 1, areaEpoch = -1 }), "invalid area generation");
        Check(!CoopProtocol.Valid(new CoopCommand { sequence = 1, action = CoopAction.Attack, areaEpoch = 0 }, 2), "stale attack after return to same scene");
        Check(CoopProtocol.Valid(new CoopCommand { sequence = 1, action = CoopAction.Input, areaEpoch = 2 }, 2), "current area input");
        var loadReady = new CoopAreaMessage { epoch = 4, scene = "forest", load = true, entrance = "" };
        var loadExpected = new CoopAreaMessage { epoch = 4, scene = "forest", load = true };
        Check(CoopProtocol.MatchesAreaReady(loadReady, loadExpected), "JSON empty entrance for load acknowledgment");
        loadReady.epoch = 3; Check(!CoopProtocol.MatchesAreaReady(loadReady, loadExpected), "stale load acknowledgment");
        loadReady.epoch = 4; loadReady.scene = "cave"; Check(!CoopProtocol.MatchesAreaReady(loadReady, loadExpected), "wrong load scene");
        var character = new InventoryState().Capture("guest");
        character.bag[0] = new InventoryStack { itemId = "", count = 0 };
        character.bag[1] = new InventoryStack { itemId = "armor", count = 1 };
        character.equipment[0] = "";
        var data = new SaveGameData { characters = new[] { character } };
        CoopProtocol.RestoreWireEmptySlots(data);
        var inventory = InventoryState.Restore(character);
        Check(inventory.GetSlot(0) == null && inventory.GetEquipment(EquipmentSlot.Head) == null, "wire empty slots");
        Check(inventory.GetSlot(1).itemId == "armor", "wire occupied slot retained");
        character.bag[0] = new InventoryStack { itemId = "armor", count = 0 };
        CoopProtocol.RestoreWireEmptySlots(data);
        bool rejected = false; try { InventoryState.Restore(character); } catch (ArgumentException) { rejected = true; }
        Check(rejected, "malformed occupied slot must still be rejected");
        Console.WriteLine("PASS: scene/protocol handshake, bounded motion, identity token, inventory slot and action validation.");
    }
}
