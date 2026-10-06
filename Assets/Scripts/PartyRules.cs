using System.Collections.Generic;

namespace SecretsReborn
{
    public static class PartyRules
    {
        public static bool AllDown(IEnumerable<CharacterVitalsState> members)
        {
            if (members == null) return false;
            bool any = false;
            foreach (var member in members)
            {
                if (member == null || !member.IsDown) return false;
                any = true;
            }
            return any;
        }
    }
}
