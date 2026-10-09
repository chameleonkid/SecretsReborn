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
        [SerializeField] private SpriteRenderer eyes;
        [SerializeField] private SpriteRenderer hair;
        [SerializeField] private Sprite[] eyesFrames;
        [SerializeField] private Sprite[] hairFrames;
        private SpriteRenderer headLayer, feetLayer;
        private ClothingAppearance headAppearance, feetAppearance;
        private Color originalHair, originalEyes;
        private Material originalEyeMaterial, eyeTintMaterial;
        private ClothingAppearance profileBody, profileHair, profileEyes;
        public bool MaleBody { get; private set; }
        public void ApplyProfile(WorldCharacterSlot profile)
        {
            profileBody = CharacterLookLibrary.Find(profile?.bodyStyle); profileHair = CharacterLookLibrary.Find(profile?.hairStyle);
            profileEyes = CharacterLookLibrary.Find(profile?.eyeStyle); MaleBody = CharacterCustomization.IsMale(profile?.bodyStyle);
            if (hair != null) hair.color = profile != null && profile.hairColor >= 0 ? CharacterPalette.Hair[profile.hairColor] : originalHair;
            if (eyes != null)
            {
                if (profileEyes == null && profile != null && profile.eyeColor >= 0)
                {
                    if (eyeTintMaterial == null) { var shader = Resources.Load<Shader>("MenuUI/EyeTint"); if (shader != null) eyeTintMaterial = new Material(shader); }
                    if (eyeTintMaterial != null) { eyeTintMaterial.SetColor("_Tint",CharacterPalette.Eyes[profile.eyeColor]); eyes.sharedMaterial = eyeTintMaterial; eyes.color = Color.white; }
                }
                else { eyes.sharedMaterial = originalEyeMaterial; eyes.color = profileEyes != null ? Color.white : originalEyes; }
            }
        }
        public float NameHeight => body != null && body.sprite != null ? body.bounds.max.y - transform.position.y : 1.5f;
        public Material FrontPreviewMaterial(int layer) => layer == 2 && eyes != null && eyeTintMaterial != null && eyes.sharedMaterial == eyeTintMaterial ? eyeTintMaterial : null;
        public void EquipAccessories(ClothingAppearance head, ClothingAppearance feet)
        { headAppearance = head; feetAppearance = feet; }
        // UI reads a fixed down-facing idle pose; gameplay facing and animation stay independent.
        public Sprite FrontPreviewLayer(int layer, out Color tint)
        {
            tint = Color.white;
            switch (layer)
            {
                case 0: return profileBody != null ? profileBody.Frame(0) : bodyFrames != null && bodyFrames.Length > 0 ? bodyFrames[0] : null;
                case 1: tint = equippedClothing != null ? equippedClothing.Tint : Color.white;
                    return equippedClothing != null ? equippedClothing.Frame(0, MaleBody) : null;
                case 2: tint = eyes != null ? eyes.color : Color.white;
                    return profileEyes != null ? profileEyes.Frame(0) : eyesFrames != null && eyesFrames.Length > 0 ? eyesFrames[0] : null;
                case 3: tint = hair != null ? hair.color : Color.white;
                    return profileHair != null ? profileHair.Frame(0) : hairFrames != null && hairFrames.Length > 0 ? hairFrames[0] : null;
                case 4: tint = feetAppearance != null ? feetAppearance.Tint : Color.white;
                    return feetAppearance != null ? feetAppearance.Frame(0, MaleBody) : null;
                case 5: tint = headAppearance != null ? headAppearance.Tint : Color.white;
                    return headAppearance != null ? headAppearance.Frame(0, MaleBody) : null;
                default: return null;
            }
        }
        [SerializeField, Min(1)] private float framesPerSecond = 8;
        private Rigidbody2D movement;
        private CharacterDeath death;
        private int facing;
        private float attackStarted = -10, attackUntil;
        private int attackFacing;
        public void PresentAttack(float duration) { attackStarted = Time.time; attackUntil = Time.time + duration; attackFacing = facing; }
        public Vector2 FacingDirection => facing == 1 ? Vector2.right : facing == 2 ? Vector2.left : facing == 3 ? Vector2.up : Vector2.down;
        private float elapsed;
        private bool externalMotion;
        private Vector2 presentedMotion;

        public void Configure(SpriteRenderer bodyRenderer, SpriteRenderer clothingRenderer,
            Sprite[] frames, ClothingAppearance outfit)
        {
            body = bodyRenderer; clothing = clothingRenderer; bodyFrames = frames; equippedClothing = outfit;
        }

        public void Equip(ClothingAppearance outfit) => equippedClothing = outfit;
        public void ConfigureDetails(SpriteRenderer eyeRenderer, Sprite[] eyeSprites,
            SpriteRenderer hairRenderer, Sprite[] hairSprites)
        {
            eyes = eyeRenderer; eyesFrames = eyeSprites;
            hair = hairRenderer; hairFrames = hairSprites;
        }
        // A future replicated character state can drive presentation without local input.
        public void SetPresentedMotion(Vector2 motion) { externalMotion = true; presentedMotion = motion; }

        private void Awake()
        {
            originalHair = hair != null ? hair.color : Color.white; originalEyes = eyes != null ? eyes.color : Color.white;
            originalEyeMaterial = eyes != null ? eyes.sharedMaterial : null;
            movement = GetComponent<Rigidbody2D>();
            death = GetComponent<CharacterDeath>();
            headLayer = CreateLayer("Equipment head"); feetLayer = CreateLayer("Equipment feet");
        }
        private SpriteRenderer CreateLayer(string name)
        {
            var obj = new GameObject(name, typeof(SpriteRenderer));
            obj.transform.SetParent(transform, false);
            var renderer = obj.GetComponent<SpriteRenderer>();
            if (body != null) renderer.sharedMaterial = body.sharedMaterial;
            return renderer;
        }
        private void OnDestroy() { if (eyeTintMaterial != null) Destroy(eyeTintMaterial); }

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
            if (Time.time < attackUntil && bodyFrames.Length >= 128)
                frame = (attackFacing + 4) * 16 + 3 + Mathf.Min(3, Mathf.FloorToInt((Time.time - attackStarted) / (attackUntil - attackStarted) * 4));
            if (death != null && death.IsDown && bodyFrames.Length >= 128)
            { frame = death.AnimationFrame(facing); attackUntil = 0; }
            body.sprite = profileBody != null ? profileBody.Frame(frame) : bodyFrames[frame];
            clothing.sprite = equippedClothing != null ? equippedClothing.Frame(frame, MaleBody) : null;
            clothing.enabled = clothing.sprite != null;
            clothing.color = equippedClothing != null ? equippedClothing.Tint : Color.white;
            clothing.sortingLayerID = body.sortingLayerID;
            clothing.sortingOrder = body.sortingOrder + 1;
            PresentLayer(eyes, eyesFrames, frame, 2, profileEyes);
            PresentLayer(hair, hairFrames, frame, 3, profileHair);
            PresentEquipment(headLayer, headAppearance, frame, 5);
            PresentEquipment(feetLayer, feetAppearance, frame, 4);
        }
        private void PresentEquipment(SpriteRenderer renderer, ClothingAppearance appearance, int frame, int offset)
        {
            renderer.sprite = appearance != null ? appearance.Frame(frame, MaleBody) : null;
            renderer.enabled = renderer.sprite != null;
            renderer.color = appearance != null ? appearance.Tint : Color.white;
            renderer.sortingLayerID = body.sortingLayerID;
            renderer.sortingOrder = body.sortingOrder + offset;
        }

        private void PresentLayer(SpriteRenderer renderer, Sprite[] frames, int frame, int offset, ClothingAppearance look)
        {
            if (renderer == null) return;
            renderer.sprite = look != null ? look.Frame(frame) : frames != null && frame < frames.Length ? frames[frame] : null;
            renderer.enabled = renderer.sprite != null;
            renderer.sortingLayerID = body.sortingLayerID;
            renderer.sortingOrder = body.sortingOrder + offset;
        }
    }
}
