using UnityEngine;

namespace SecretsReborn
{
    public enum CharacterLifePhase { Alive, Dying, Downed }
    public sealed class CharacterDeath : MonoBehaviour
    {
        public const float AnimationSeconds = 1;
        private CharacterInventory actor;
        private Rigidbody2D body;
        private Collider2D[] colliders;
        private bool[] enabledBeforeDeath;
        private bool presentedDown;
        private float started;
        public bool IsDown => actor != null && GameSession.Instance.World.CharacterVitals(actor.CharacterId).IsDown;
        public CharacterLifePhase Phase => !IsDown ? CharacterLifePhase.Alive
            : !presentedDown || Time.unscaledTime - started < AnimationSeconds ? CharacterLifePhase.Dying : CharacterLifePhase.Downed;
        private void Awake()
        { actor = GetComponent<CharacterInventory>(); body = GetComponent<Rigidbody2D>(); colliders = GetComponents<Collider2D>(); }
        private void Update() => RefreshState();
        public void RefreshState()
        {
            if (IsDown && !presentedDown)
            {
                presentedDown = true; started = Time.unscaledTime;
                if (body != null) body.linearVelocity = Vector2.zero;
                enabledBeforeDeath = new bool[colliders.Length];
                for (int i = 0; i < colliders.Length; i++)
                { enabledBeforeDeath[i] = colliders[i].enabled; if (!colliders[i].isTrigger) colliders[i].enabled = false; }
                GetComponent<PlayerLantern>()?.SetLit(false);
            }
            else if (!IsDown && presentedDown) RestoreColliders();
        }
        public int AnimationFrame(int facing)
        {
            float elapsed = presentedDown ? Time.unscaledTime - started : 0;
            // Original Take Damage/Hurt rows, followed by Sleep (57) and Dead (58).
            if (elapsed < .15f) return (facing + 4) * 16 + 13;
            if (elapsed < .3f) return (facing + 4) * 16 + 14;
            if (elapsed < .5f) return (facing + 4) * 16 + 15;
            return elapsed < .75f ? 57 : 58;
        }
        private void RestoreColliders()
        {
            if (enabledBeforeDeath != null)
                for (int i = 0; i < colliders.Length; i++) if (colliders[i] != null) colliders[i].enabled = enabledBeforeDeath[i];
            presentedDown = false; enabledBeforeDeath = null;
        }
        private void OnDisable() => RestoreColliders();
    }
}
