using System;
using SecretsReborn;
internal static class EnemyLootPositionChecks
{
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Reject(SaveGameData data)
    {
        try { WorldSessionState.Restore(data); } catch (ArgumentException) { return; }
        throw new Exception("Invalid defeat position accepted");
    }
    public static void Main()
    {
        var world=new WorldSessionState("loot-position-test");
        Check(world.DefeatEnemy("oak", "forest", 12, -3, .5f),"first death");
        Check(!world.DefeatEnemy("oak", "forest", 99, 99, 0),"duplicate death");
        var saved=world.Capture(); var loaded=WorldSessionState.Restore(saved);
        var position=loaded.DefeatPosition("oak");
        Check(loaded.IsEnemyDefeated("oak") && position.scenePath=="forest" && position.x==12 && position.y==-3 && position.z==.5f,"roundtrip");
        saved.enemyDefeatPositions[0].x=50; position.x=30;
        Check(world.DefeatPosition("oak").x==12 && loaded.DefeatPosition("oak").x==12,"snapshot and read isolation");
        saved=world.Capture(); saved.version=17; saved.enemyDefeatPositions=null;
        Check(WorldSessionState.Restore(saved).IsEnemyDefeated("oak") && WorldSessionState.Restore(saved).DefeatPosition("oak")==null,"legacy save fallback");
        saved=world.Capture(); saved.enemyDefeatPositions[0].x=float.NaN; Reject(saved);
        saved=world.Capture(); saved.enemyDefeatPositions[0].enemyId="alive-enemy"; Reject(saved);
        saved=world.Capture(); saved.enemyDefeatPositions=new[] { saved.enemyDefeatPositions[0], saved.enemyDefeatPositions[0] }; Reject(saved);
        saved=world.Capture(); saved.enemyDefeatPositions=null; Reject(saved);
        Console.WriteLine("PASS: enemy death position roundtrip, immutable snapshots, duplicate death, legacy migration and invalid save rejection.");
    }
}
