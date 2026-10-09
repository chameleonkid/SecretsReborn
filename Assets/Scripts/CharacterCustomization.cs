using System;
namespace SecretsReborn
{
    // Stable asset IDs shared by validation, saves, networking and the creator.
    public static class CharacterCustomization
    {
        public static readonly string[] HairStyles = { "pony", "alpaca", "bat", "beetle", "bumblebee", "ferret", "hedgehog", "hyena", "monkey", "orca", "panda", "rhino", "shark" };
        public static readonly string[] EyeStyles = { "green", "brown", "dark-blue", "grey", "purple", "red", "yellow" };
        public static readonly string[] EyeNames = { "Grün", "Braun", "Blau", "Grau", "Violett", "Rot", "Gold" };
        private static readonly string[] Skins = { "", "-pale", "-brown", "-dark", "-elf", "-elf-dark", "-elf-blue", "-white" };
        public static readonly string[] SkinNames = { "Standard", "Hell", "Braun", "Dunkel", "Elf", "Elf dunkel", "Elf blau", "Sehr hell" };
        public static string Family(bool male) => male ? "male" : "female";
        public static string Body(bool male, int skin) => Family(male) + "-1-base-rpc-" + Family(male) + "-base" + (male && skin == 7 ? "-tan" : Skins[skin]);
        public static string Hair(bool male, int style) => Family(male) + "-5-hairstyles-rpc-" + Family(male) + "-" + HairStyles[style];
        public static string Eyes(bool male, int style) => Family(male) + "-2-eye-colors-rpc-" + Family(male) + "-" + EyeStyles[style];
        public static bool IsMale(string body) => body != null && body.StartsWith("male-", StringComparison.Ordinal);
        public static bool Valid(string body, string hair, string eyes)
        {
            if (string.IsNullOrEmpty(body)) return string.IsNullOrEmpty(hair) && string.IsNullOrEmpty(eyes);
            bool male = IsMale(body), validBody = false, validHair = false, validEyes = false;
            for (int i = 0; i < 8; i++) validBody |= body == Body(male, i);
            for (int i = 0; i < HairStyles.Length; i++) validHair |= hair == Hair(male, i);
            for (int i = 0; i < EyeStyles.Length; i++) validEyes |= eyes == Eyes(male, i);
            return validBody && validHair && validEyes;
        }
    }
}
