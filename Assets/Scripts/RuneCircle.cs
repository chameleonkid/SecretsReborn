using System;
using UnityEngine;

namespace SecretsReborn
{
    public sealed class RuneCircle : MonoBehaviour
    {
        private Transform player;
        private PlayerLantern lantern;
        private Action<int> entered;
        private int index;
        private bool inside;
        private SpriteRenderer[] runes;
        [SerializeField, Min(0.1f)] private float activationRadius = 0.85f;

        public void Initialize(Transform target, int order, Action<int> onEntered)
        {
            player = target;
            lantern = target != null ? target.GetComponent<PlayerLantern>() : null;
            inside = false;
            index = order;
            entered = onEntered;
            runes = GetComponentsInChildren<SpriteRenderer>(true);
        }

        private void FixedUpdate()
        {
            if (NetworkCoop.IsReplica) return;
            if (player == null) return;
            float distance = ((Vector2)(player.position - transform.position)).sqrMagnitude;
            var actor = player.GetComponent<CharacterInventory>();
            bool nowInside = actor != null && !GameSession.Instance.World.CharacterVitals(actor.CharacterId).IsDown && lantern != null && lantern.CanRevealRunes
                && distance <= lantern.RevealRadius * lantern.RevealRadius && distance <= activationRadius * activationRadius;
            if (nowInside && !inside) entered?.Invoke(index);
            inside = nowInside;
        }

        public void SetActivated(bool active)
        {
            foreach (var rune in runes)
                rune.color = active ? new Color(0.4f, 1f, 0.55f) : new Color(0.65f, 0.65f, 0.8f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, activationRadius);
        }
    }
}
