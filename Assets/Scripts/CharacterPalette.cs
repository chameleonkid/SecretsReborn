using UnityEngine;
namespace SecretsReborn
{
    public static class CharacterPalette
    {
        public static readonly Color[] Hair = { new Color(.74f,.51f,.22f), new Color(.95f,.8f,.42f), new Color(.24f,.2f,.18f),
            new Color(.85f,.33f,.16f), new Color(.8f,.82f,.88f), new Color(.55f,.35f,.75f) };
        public static readonly string[] HairNames = { "Braun", "Blond", "Dunkel", "Kupfer", "Silber", "Violett" };
        public static readonly Color[] Eyes = { new Color(.35f,.8f,.35f), new Color(.3f,.6f,1), new Color(1,.65f,.2f), new Color(.7f,.4f,1) };
        public static readonly string[] EyeNames = { "Grün", "Blau", "Bernstein", "Violett" };
    }
}
