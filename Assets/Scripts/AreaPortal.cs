using UnityEngine;

namespace SecretsReborn
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class AreaPortal : MonoBehaviour
    {
        [SerializeField] private string targetScenePath;
        [SerializeField] private string targetEntranceId;
        [SerializeField] private string requiredPuzzleId;
        public string TargetScenePath => targetScenePath;
        public string TargetEntranceId => targetEntranceId;
        public bool IsUnlocked => string.IsNullOrEmpty(requiredPuzzleId) || GameSession.Instance.World.Puzzle(requiredPuzzleId).IsComplete;
        public void Configure(string scene, string entrance, string puzzle = null)
        { targetScenePath = scene; targetEntranceId = entrance; requiredPuzzleId = puzzle; }
        private void OnTriggerEnter2D(Collider2D other)
        {
            var character = other.GetComponentInParent<CharacterInventory>();
            if (character != null) GameSession.Instance.RequestAreaChange(this, character);
        }
        public bool CanUse(CharacterInventory actor) => actor != null && actor.HasStateAuthority && isActiveAndEnabled && IsUnlocked
            && actor.gameObject.scene == gameObject.scene && Vector2.Distance(actor.transform.position, transform.position) <= 2.5f;
    }
}
