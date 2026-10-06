using UnityEngine;

namespace SecretsReborn
{
    // Presentation only: equipment authority and inventory remain outside this component.
    [DefaultExecutionOrder(100)]
    public sealed class CharacterAppearance : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private SpriteRenderer clothing;
        [SerializeField] private Sprite[] bodyFrames;
        [SerializeField] private ClothingAppearance equippedClothing;
        [SerializeField, Min(1)] private float framesPerSecond = 8;
        private Rigidbody2D movement;
        private int facing;
        private float elapsed;
        private bool externalMotion;
        private Vector2 presentedMotion;

        public void Configure(SpriteRenderer bodyRenderer, SpriteRenderer clothingRenderer,
            Sprite[] frames, ClothingAppearance outfit)
        {
            body = bodyRenderer; clothing = clothingRenderer; bodyFrames = frames; equippedClothing = outfit;
        }

        public void Equip(ClothingAppearance outfit) => equippedClothing = outfit;
        // A future replicated character state can drive presentation without local input.
        public void SetPresentedMotion(Vector2 motion) { externalMotion = true; presentedMotion = motion; }

        private void Awake() => movement = GetComponent<Rigidbody2D>();

        private void LateUpdate()
        {
            if (body == null || clothing == null || bodyFrames == null || bodyFrames.Length < 64) return;
            Vector2 motion = externalMotion ? presentedMotion : movement != null ? movement.linearVelocity : Vector2.zero;
            bool walking = motion.sqrMagnitude > 0.01f;
            int previousFacing = facing;
            if (walking)
                facing = Mathf.Abs(motion.x) > Mathf.Abs(motion.y)
                    ? (motion.x > 0 ? 1 : 2) : (motion.y > 0 ? 3 : 0);
            if (!walking || previousFacing != facing) elapsed = 0;
            else elapsed += Time.deltaTime;
            int frame = facing * 16 + (walking ? 1 + (Mathf.FloorToInt(elapsed * framesPerSecond) % 6) : 0);
            body.sprite = bodyFrames[frame];
            clothing.sprite = equippedClothing != null ? equippedClothing.Frame(frame) : null;
            clothing.enabled = clothing.sprite != null;
            clothing.sortingLayerID = body.sortingLayerID;
            clothing.sortingOrder = body.sortingOrder + 1;
        }
    }
}
