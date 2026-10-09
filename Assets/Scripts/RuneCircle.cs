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
        internal int Order => index;
        private bool inside;
        private readonly System.Collections.Generic.HashSet<string> occupants = new System.Collections.Generic.HashSet<string>();
        private SpriteRenderer[] runes;
        [SerializeField, Min(0.1f)] private float activationRadius = 0.85f;

        public void Initialize(Transform target, int order, Action<int> onEntered)
        {
            player = target;
            lantern = target != null ? target.GetComponent<PlayerLantern>() : null;
            inside = false;
            occupants.Clear();
            index = order;
            entered = onEntered;
            runes = GetComponentsInChildren<SpriteRenderer>(true);
        }

        private void FixedUpdate()
        {
            if (NetworkCoop.IsReplica) return;
            if (GameSession.Instance.Busy) return;
            if (NetworkCoop.Running)
            {
                var present = new System.Collections.Generic.HashSet<string>();
                bool newlyEntered = false;
                foreach (var member in FindObjectsByType<CharacterInventory>(FindObjectsSortMode.None))
                {
                    var light = member.GetComponent<PlayerLantern>();
                    float squared = ((Vector2)(member.transform.position - transform.position)).sqrMagnitude;
                    if (!GameSession.Instance.CanFight(member) || light == null || !light.CanRevealRunes
                        || squared > activationRadius * activationRadius || squared > light.RevealRadius * light.RevealRadius) continue;
                    present.Add(member.CharacterId);
                    if (!occupants.Contains(member.CharacterId)) newlyEntered = true;
                }
                occupants.Clear(); foreach (var id in present) occupants.Add(id);
                if (newlyEntered) entered?.Invoke(index);
                return;
            }
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
