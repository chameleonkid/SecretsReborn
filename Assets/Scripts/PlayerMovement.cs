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
        private bool networkDriven;
        private Vector2 networkMotion;
        private bool rewardImmobilized;
        private RigidbodyConstraints2D constraintsBeforeReward;
        internal void SetRewardImmobilized(bool value)
        {
            if (body == null || rewardImmobilized == value) return;
            rewardImmobilized = value;
            if (value)
            {
                constraintsBeforeReward = body.constraints;
                body.linearVelocity = Vector2.zero; body.angularVelocity = 0;
                body.constraints = RigidbodyConstraints2D.FreezeAll;
            }
            else body.constraints = constraintsBeforeReward;
        }
        public void SetNetworkMotion(Vector2 motion) { networkDriven = true; networkMotion = motion; }
        public Vector2 ReadLocalMotion() => Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1);

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
        private void FixedUpdate()
        {
            SetRewardImmobilized(actor != null && actor.HasStateAuthority && (GameSession.Instance.IsReceivingReward(actor) || actor.GetComponent<SpellCaster>()?.IsCasting==true || SpellRingMenu.Selecting(actor)));
            body.linearVelocity =
            !NetworkCoop.IsReplica && (networkDriven || Application.isFocused) && !GameSession.Instance.Busy && !SaveBook.IsOpen
                && (actor == null || !GameSession.Instance.World.CharacterVitals(actor.CharacterId).IsDown && !GameSession.Instance.IsReceivingReward(actor) && actor.GetComponent<SpellCaster>()?.IsCasting!=true && !SpellRingMenu.Selecting(actor)) && (melee == null || !melee.IsSwinging)
                ? (networkDriven ? networkMotion : ReadLocalMotion()) * speed : Vector2.zero;
        }
        private void OnDisable()
        {
            move.Disable();
            SetRewardImmobilized(false);
            if (body != null) body.linearVelocity = Vector2.zero;
        }
        private void OnDestroy() => move.Dispose();
    }
}
