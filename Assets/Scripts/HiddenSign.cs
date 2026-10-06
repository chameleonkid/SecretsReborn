using UnityEngine;

namespace SecretsReborn
{
    public sealed class HiddenSign : MonoBehaviour
    {
        [SerializeField] private PlayerLantern lantern;
        private SpriteRenderer[] glyph;

        private void Awake()
        {
            glyph = GetComponentsInChildren<SpriteRenderer>(true);
            SetVisible(false);
        }

        private void Start()
        {
            if (lantern == null) lantern = FindFirstObjectByType<PlayerLantern>();
        }

        public void Initialize(PlayerLantern source)
        {
            lantern = source;
            glyph = GetComponentsInChildren<SpriteRenderer>(true);
            SetVisible(false);
        }

        private void LateUpdate()
        {
            bool visible = lantern != null && lantern.isActiveAndEnabled && lantern.IsLit
                && ((Vector2)(transform.position - lantern.transform.position)).sqrMagnitude
                    <= lantern.RevealRadius * lantern.RevealRadius;
            SetVisible(visible);
        }

        private void SetVisible(bool visible)
        {
            if (glyph == null) return;
            foreach (var renderer in glyph) renderer.enabled = visible;
        }

        private void OnDisable() => SetVisible(false);
    }
}
