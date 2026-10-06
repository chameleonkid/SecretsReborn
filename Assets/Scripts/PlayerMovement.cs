using UnityEngine;
using UnityEngine.InputSystem;

namespace SecretsReborn
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField, Min(0)] private float speed = 4f;
        private Rigidbody2D body;
        private PlayerMelee melee;
        private CharacterInventory actor;
        private InputAction move;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            melee = GetComponent<PlayerMelee>();
            actor = GetComponent<CharacterInventory>();
            move = new InputAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick");
            move.AddBinding("<Gamepad>/dpad");
        }

        private void OnEnable() => move.Enable();
        private void FixedUpdate() => body.linearVelocity =
            Application.isFocused && !GameSession.Instance.Busy
                && (actor == null || !GameSession.Instance.World.CharacterVitals(actor.CharacterId).IsDown) && (melee == null || !melee.IsSwinging)
                ? Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f) * speed : Vector2.zero;
        private void OnDisable()
        {
            move.Disable();
            if (body != null) body.linearVelocity = Vector2.zero;
        }
        private void OnDestroy() => move.Dispose();
    }
}
