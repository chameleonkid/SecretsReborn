using System;
using SecretsReborn;
internal static class SpellCastChecks
{
    public static void Main()
    {
        for (int budget=1;budget<30;budget++) for (int count=1;count<8;count++)
        {
            int total=0,min=int.MaxValue,max=0;
            for (int i=0;i<count;i++) { int share=SpellCastRules.Share(budget,count,i); total+=share; min=Math.Min(min,share); max=Math.Max(max,share); }
            if (total!=budget || max-min>1) throw new Exception("Distribution inflated budget");
            for (int projectiles=1;projectiles<=8;projectiles++)
            {
                int delivered=0;
                for (int target=0;target<count;target++)
                    for (int projectile=0;projectile<projectiles;projectile++)
                        delivered+=SpellCastRules.Share(SpellCastRules.Share(budget,count,target),projectiles,projectile);
                if (delivered!=budget) throw new Exception("Projectiles inflated target shares");
            }
        }
        if (SpellCastRules.Share(0,4,0)!=0 || SpellCastRules.Share(5,0,0)!=0 || SpellCastRules.Share(5,2,2)!=0 || SpellCastRules.ValidNumber(float.NaN)) throw new Exception("Invalid input");
        Console.WriteLine("PASS: stable integer distribution preserves total budget, fair remainder and invalid input.");
    }
}
