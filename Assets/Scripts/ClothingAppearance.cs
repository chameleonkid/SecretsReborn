using UnityEngine;

namespace SecretsReborn
{
    [CreateAssetMenu(menuName = "SecretsReborn/Clothing Appearance")]
    public sealed class ClothingAppearance : ScriptableObject
    {
        [SerializeField] private string appearanceId;
        [SerializeField] private Sprite[] frames;
        [SerializeField] private Color tint = Color.white;
        [SerializeField] private ClothingAppearance maleVariant;
        public Color Tint => tint;
        public void SetTint(Color color) => tint = color;
        public string AppearanceId => appearanceId;
        public Sprite Frame(int index) => frames != null && index >= 0 && index < frames.Length ? frames[index] : null;
        public Sprite Frame(int index, bool male) => male && maleVariant != null ? maleVariant.Frame(index) : Frame(index);
        public void SetMaleVariant(ClothingAppearance value) => maleVariant = value;
        public void Configure(string id, Sprite[] sprites) { appearanceId = id; frames = sprites; }
    }
}
