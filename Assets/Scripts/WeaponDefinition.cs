using UnityEngine;

namespace SecretsReborn
{
    public enum MeleeWeaponType { Sword, Axe, Mace }
    [CreateAssetMenu(menuName = "SecretsReborn/Weapon profile")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        [SerializeField] private MeleeWeaponType weaponType;
        [SerializeField] private Sprite[] frames;
        [SerializeField] private Color tint = Color.white;
        [SerializeField, Min(1)] private int damage = 2;
        [SerializeField, Min(.1f)] private float range = 1.6f;
        [SerializeField, Min(.1f)] private float attackDuration = .35f;
        [SerializeField, Min(.1f)] private float cooldown = .45f;
        [SerializeField, Range(0, 1)] private float hitFraction = .4f;
        [SerializeField, Min(0)] private float knockback = 5;
        [Tooltip("Optional material supporting _EmissionMap and _EmissionColor; Bloom is configured separately.")]
        [SerializeField] private Material visualMaterial = null;
        [SerializeField] private Texture2D emissionMask = null;
        [SerializeField, ColorUsage(false, true)] private Color emissionColor = Color.black;
        public MeleeWeaponType Type => weaponType;
        public Color Tint => tint;
        public int Damage => Mathf.Max(1, damage);
        public float Range => Mathf.Max(.1f, range);
        public float Duration => Mathf.Max(.1f, attackDuration);
        public float Cooldown => Mathf.Max(Duration, cooldown);
        public float HitDelay => Duration * Mathf.Clamp01(hitFraction);
        public float Knockback => Mathf.Max(0, knockback);
        public Material VisualMaterial => visualMaterial;
        public Texture2D EmissionMask => emissionMask;
        public Color EmissionColor => emissionColor;
        public Sprite Frame(int index) => frames != null && index >= 0 && index < frames.Length ? frames[index] : null;
        public void Configure(MeleeWeaponType type, Sprite[] sprites, Color color)
        { weaponType = type; frames = sprites; tint = color; }
    }
}
