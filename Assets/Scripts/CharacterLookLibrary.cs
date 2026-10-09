using System.Collections.Generic;
using UnityEngine;
namespace SecretsReborn
{
    public static class CharacterLookLibrary
    {
        private static readonly Dictionary<string, ClothingAppearance> cache = new Dictionary<string, ClothingAppearance>();
        public static ClothingAppearance Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (!cache.TryGetValue(id, out var asset) || asset == null)
            { asset = Resources.Load<ClothingAppearance>("CharacterLooks/" + id); cache[id] = asset; }
            return asset;
        }
    }
}
