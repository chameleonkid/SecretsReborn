using UnityEngine;

namespace SecretsReborn
{
    [CreateAssetMenu(menuName = "SecretsReborn/Lamp profile")]
    public sealed class LampDefinition : ScriptableObject
    {
        [SerializeField] private Color lightColor = new Color(1, .82f, .48f);
        [SerializeField, Min(.1f)] private float lightRadius = 3.5f;
        [SerializeField, Range(1, 10)] private int brightness = 8;
        [SerializeField] private bool revealsRunes;
        [SerializeField, Min(.1f)] private float revealRadius = 3;
        public Color LightColor => lightColor;
        public float LightRadius => Mathf.Max(.1f, lightRadius);
        public int Brightness => Mathf.Clamp(brightness, 1, 10);
        public float Intensity => Brightness / 10f;
        public bool RevealsRunes => revealsRunes;
        public float RevealRadius => Mathf.Min(LightRadius, Mathf.Max(.1f, revealRadius));
        public void Configure(Color color, bool magical)
        { lightColor = color; revealsRunes = magical; }
    }
}
