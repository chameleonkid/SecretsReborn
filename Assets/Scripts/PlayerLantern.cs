using UnityEngine;
using UnityEngine.InputSystem;

namespace SecretsReborn
{
    public sealed class PlayerLantern : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float revealRadius = 3f;
        public bool IsLit { get; private set; }
        public float RevealRadius => revealRadius;
        [SerializeField] private SpriteRenderer indicator;
        public SpriteRenderer Indicator { get => indicator; set => indicator = value; }
        private InputAction toggle;

        private void Awake()
        {
            toggle = new InputAction("Toggle lantern", InputActionType.Button);
            toggle.AddBinding("<Keyboard>/l");
            toggle.AddBinding("<Gamepad>/buttonNorth");
        }

        private void OnEnable() => toggle.Enable();

        private void Update()
        {
            if (Application.isFocused && toggle.WasPressedThisFrame()) IsLit = !IsLit;
            if (Indicator != null) Indicator.enabled = IsLit;
        }

        private void OnDisable()
        {
            toggle.Disable();
            IsLit = false;
            if (Indicator != null) Indicator.enabled = false;
        }

        private void OnDestroy() => toggle.Dispose();
    }
}
