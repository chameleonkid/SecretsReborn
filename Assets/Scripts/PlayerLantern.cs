using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace SecretsReborn
{
    [RequireComponent(typeof(CharacterInventory))]
    public sealed class PlayerLantern : MonoBehaviour
    {
        public bool IsLit { get; private set; }
        public bool CanRevealRunes => IsLit && EquippedLamp != null && EquippedLamp.RevealsRunes;
        public float RevealRadius => EquippedLamp != null ? EquippedLamp.RevealRadius : 0;
        public LampDefinition EquippedLamp => inventory != null && inventory.State != null
            ? inventory.Find(inventory.State.GetEquipment(EquipmentSlot.Lamp))?.Lamp : null;
        [SerializeField] private Light2D lampLight;
        [SerializeField] private SpriteRenderer indicator;
        public SpriteRenderer Indicator { get => indicator; set => indicator = value; }
        private CharacterInventory inventory;
        private InventoryInteraction interaction;
        private InputAction toggle;
        private bool localInput = true;
        public void SetLocalInput(bool value) => localInput = value;
        private void Awake()
        {
            inventory = GetComponent<CharacterInventory>();
            interaction = GetComponent<InventoryInteraction>();
            if (lampLight == null)
            {
                var child = new GameObject("Lamp light"); child.transform.SetParent(transform, false);
                lampLight = child.AddComponent<Light2D>();
            }
            lampLight.lightType = Light2D.LightType.Point;
            lampLight.pointLightInnerAngle = lampLight.pointLightOuterAngle = 360;
            lampLight.enabled = false;
            toggle = new InputAction("Toggle lantern", InputActionType.Button);
            toggle.AddBinding("<Keyboard>/l"); toggle.AddBinding("<Gamepad>/buttonNorth");
        }
        private void OnEnable() { toggle.Enable(); inventory.Changed += RefreshLight; }
        private void Update()
        {
            if (GameSession.Instance.World.CharacterVitals(inventory.CharacterId).IsDown) { SetLit(false); return; }
            if (localInput && Application.isFocused && !SpellRingMenu.BlocksInput(inventory) && !GameSession.Instance.Busy && !GameSession.Instance.RewardPresentationActive && !SaveBook.IsOpen && (interaction == null || !interaction.IsOpen)
                && toggle.WasPressedThisFrame())
            { if (!NetworkCoop.Request(inventory, CoopAction.Lamp)) SetLit(!IsLit); }
        }
        public void SetLit(bool value)
        { IsLit = value && EquippedLamp != null; RefreshLight(); }
        private void RefreshLight()
        {
            var profile = EquippedLamp;
            if (profile == null) IsLit = false;
            if (indicator != null) indicator.enabled = false;
            if (lampLight == null) return;
            lampLight.enabled = IsLit;
            if (profile == null) return;
            lampLight.color = profile.LightColor;
            lampLight.intensity = profile.Intensity;
            lampLight.pointLightOuterRadius = profile.LightRadius;
            lampLight.pointLightInnerRadius = profile.LightRadius * .2f;
            lampLight.falloffIntensity = .5f;
        }
        private void OnDisable()
        { toggle.Disable(); inventory.Changed -= RefreshLight; SetLit(false); }
        private void OnDestroy() => toggle.Dispose();
    }
}
