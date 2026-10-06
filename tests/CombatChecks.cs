using System;
using SecretsReborn;

internal static class CombatChecks
{
    private static void Check(bool value, string label) { if (!value) throw new Exception(label); }
    public static void Main()
    {
        var cooldown = new CombatCooldown();
        Check(CombatRules.LogFacingRow(0, -1) == 0 && CombatRules.LogFacingRow(0, 1) == 1
            && CombatRules.LogFacingRow(1, 0) == 2 && CombatRules.LogFacingRow(-1, 0) == 3, "original log direction clips");
        Check(cooldown.TryUse(0, .45), "first attack");
        Check(!cooldown.TryUse(.2, .45) && !cooldown.TryUse(.449, .45), "attack spam blocked");
        Check(cooldown.TryUse(.45, .45), "exact expiry accepted");
        Check(!cooldown.TryUse(double.NaN, .45) && !cooldown.TryUse(2, 0), "invalid timers rejected");
        Check(cooldown.TryUse(1, .8) && !cooldown.TryUse(1.5, .8), "hurt invulnerability window");
        Check(CombatRules.InArc(1, 0, 1, 0, 1.6f), "front hit");
        Check(!CombatRules.InArc(-1, 0, 1, 0, 1.6f), "behind rejected");
        Check(!CombatRules.InArc(0, 1, 1, 0, 1.6f), "outside cone rejected");
        Check(!CombatRules.InArc(2, 0, 1, 0, 1.6f), "outside range rejected");
        Check(CombatRules.InArc(0, 0, 0, -1, 1.6f), "overlapping target");
        Check(!CombatRules.InArc(float.NaN, 0, 1, 0, 1.6f), "invalid geometry rejected");
        Console.WriteLine("PASS: attack cooldown, invulnerability, frontal cone, rear/range rejection and invalid input.");
    }
}
