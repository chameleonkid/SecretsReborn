using System;
using SecretsReborn;
public static class ReconnectChecks
{
    private static void Check(bool condition,string label) { if (!condition) throw new Exception(label); }
    public static void Main()
    {
        var leases = new ReconnectReservations(); string a = Guid.NewGuid().ToString("N"), b = Guid.NewGuid().ToString("N");
        leases.Bind(a,"world-a","figure");
        Check(leases.ReservedForOther("figure",b,"world-a",1),"active character exclusive");
        leases.Release(a,10);
        Check(leases.Resolve(a,"world-a",20) == "figure" && leases.Resolve(a,"world-b",20) == null,"reconnect ticket world isolation");
        Check(leases.ReservedForOther("figure",b,"world-a",309) && !leases.ReservedForOther("figure",a,"world-a",309),"five minute reservation protects returning owner");
        Check(leases.Resolve(a,"world-a",310) == null && !leases.ReservedForOther("figure",b,"world-a",310),"expired figure becomes available");
        leases.Bind(b,"world-a","figure");
        Check(leases.ReservedForOther("figure",a,"world-a",400),"previous ticket cannot steal active figure");
        bool invalid = false; try { leases.Bind("player-name","world-a","figure"); } catch(ArgumentException) { invalid = true; }
        Check(invalid,"display names cannot be reconnect tickets");
        Console.WriteLine("PASS: active exclusivity, world isolation, five-minute grace, expiry and reconnect token validation.");
    }
}
