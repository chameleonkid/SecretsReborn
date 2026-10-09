using UnityEngine;
using UnityEngine.InputSystem;

namespace SecretsReborn
{
    [RequireComponent(typeof(CharacterInventory))]
    public sealed class PlayerMelee : MonoBehaviour
    {
        [SerializeField] private bool localInput = true;
        [SerializeField] private Sprite[] swordFrames;
        [SerializeField] private GameObject swordVisualPrefab;
        private CharacterInventory actor;
        private CharacterAppearance appearance;
        private SpriteRenderer sword;
        private float startedAt = -10;
        private float duration = .35f;
        private WeaponDefinition attackProfile;
        private bool armedSwing;
        public void SetLocalInput(bool value) => localInput = value;
        private Material baseMaterial;
        private MaterialPropertyBlock properties;
        private Vector2 attackFacing;
        public CombatCooldown AttackCooldown { get; } = new CombatCooldown();
        public CombatCooldown HurtCooldown { get; } = new CombatCooldown();
        public Vector2 Facing => appearance != null ? appearance.FacingDirection : Vector2.down;
        public Vector2 AttackFacing => attackFacing;
        public bool IsSwinging => Time.time < startedAt + duration;
        public void ConfigureSword(Sprite[] frames) => swordFrames = frames;
        public void ConfigureSwordPrefab(GameObject prefab) => swordVisualPrefab = prefab;
        private void Awake()
        {
            actor = GetComponent<CharacterInventory>(); appearance = GetComponent<CharacterAppearance>();
            var child = swordVisualPrefab != null ? Instantiate(swordVisualPrefab, transform, false) : new GameObject("Animated sword");
            child.transform.SetParent(transform, false);
            sword = child.GetComponent<SpriteRenderer>() ?? child.AddComponent<SpriteRenderer>();
            sword.sharedMaterial = GetComponent<SpriteRenderer>().sharedMaterial;
            baseMaterial = sword.sharedMaterial; properties = new MaterialPropertyBlock();
            sword.enabled = false;
        }
        private void Update()
        {
            if (!localInput || !Application.isFocused || GameSession.Instance.RewardInputBlocked(actor)) return;
            if (Keyboard.current?.spaceKey.wasPressedThisFrame == true || Keyboard.current?.jKey.wasPressedThisFrame == true
                || Gamepad.current?.buttonWest.wasPressedThisFrame == true) GameSession.Instance.RequestMeleeAttack(actor);
        }
        private void LateUpdate()
        {
            bool visible = IsSwinging && armedSwing && !GameSession.Instance.World.CharacterVitals(actor.CharacterId).IsDown
                && (NetworkCoop.IsReplica || GameSession.Instance.CanFight(actor));
            sword.enabled = visible;
            if (!visible) return;
            int row = attackFacing.x > .5f ? 1 : attackFacing.x < -.5f ? 2 : attackFacing.y > .5f ? 3 : 0;
            int frame = (row + 4) * 16 + 3 + Mathf.Min(3, Mathf.FloorToInt((Time.time - startedAt) / duration * 4));
            sword.sprite = attackProfile != null ? attackProfile.Frame(frame) : swordFrames != null && frame < swordFrames.Length ? swordFrames[frame] : null;
            sword.enabled = sword.sprite != null;
            sword.sortingOrder = GetComponent<SpriteRenderer>().sortingOrder + (row == 3 ? -1 : 6);
        }
        public void PresentSwing(WeaponDefinition profile, bool armed)
        {
            attackProfile = profile; armedSwing = armed; duration = profile != null ? profile.Duration : .35f;
            startedAt = Time.time; attackFacing = Facing;
            sword.color = profile != null ? profile.Tint : Color.white;
            sword.sharedMaterial = profile != null && profile.VisualMaterial != null ? profile.VisualMaterial : baseMaterial;
            properties.Clear();
            if (profile != null && sword.sharedMaterial.HasProperty("_EmissionMap") && sword.sharedMaterial.HasProperty("_EmissionColor"))
            {
                properties.SetTexture("_EmissionMap", profile.EmissionMask != null ? profile.EmissionMask : Texture2D.blackTexture);
                properties.SetColor("_EmissionColor", profile.EmissionColor);
            }
            sword.SetPropertyBlock(properties);
            appearance?.PresentAttack(duration);
        }
    }
}
