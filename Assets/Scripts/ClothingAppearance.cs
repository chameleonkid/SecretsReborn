using UnityEngine;

namespace SecretsReborn
{
    [CreateAssetMenu(menuName = "SecretsReborn/Clothing Appearance")]
    public sealed class ClothingAppearance : ScriptableObject
    {
        [SerializeField] private string appearanceId;
        [SerializeField] private Sprite[] frames;
        [SerializeField] private Color tint = Color.white;
        public Color Tint => tint;
        public void SetTint(Color color) => tint = color;
        public string AppearanceId => appearanceId;
        public Sprite Frame(int index) => frames != null && index >= 0 && index < frames.Length ? frames[index] : null;
        public void Configure(string id, Sprite[] sprites) { appearanceId = id; frames = sprites; }
    }
}
