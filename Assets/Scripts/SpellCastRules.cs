using System;
namespace SecretsReborn
{
    public static class SpellCastRules
    {
        // Stable target-ID order: remainder goes to the first targets; never multiply the budget.
        public static int Share(int budget,int count,int index) => budget<=0 || count<=0 || index<0 || index>=count ? 0 : budget/count+(index<budget%count ? 1 : 0);
        public static bool ValidNumber(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value>0;
    }
}
