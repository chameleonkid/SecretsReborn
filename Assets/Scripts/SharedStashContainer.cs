using UnityEngine;

namespace SecretsReborn
{
    public sealed class SharedStashContainer : MonoBehaviour
    {
        [SerializeField] private string containerId = "sanctuary-shared-stash";
        [SerializeField, Min(.1f)] private float interactionDistance = 2;
        public string ContainerId => containerId;
        public bool CanUse(CharacterInventory actor) => actor != null && isActiveAndEnabled
            && actor.gameObject.scene == gameObject.scene && Vector2.Distance(actor.transform.position, transform.position) <= interactionDistance;
        public bool TryTransfer(CharacterInventory actor, bool deposit, int from, int to, string expectedItem, int expectedCount)
        {
            if (!CanUse(actor)) return false;
            if (NetworkCoop.Request(actor, deposit ? CoopAction.StashDeposit : CoopAction.StashWithdraw, from, to, containerId, expectedItem, expectedCount)) return true;
            if (!actor.HasStateAuthority || SaveBook.IsOpen || !GameSession.Instance.CanChangeVitals(actor)
                || GameSession.Instance.World.CharacterVitals(actor.CharacterId).IsDown) return false;
            var stash = GameSession.Instance.World.SharedStash;
            bool result = deposit ? actor.State.TryTransferTo(stash, from, to, expectedItem, expectedCount, id => actor.Find(id)?.Rules ?? default)
                : stash.TryTransferTo(actor.State, from, to, expectedItem, expectedCount, id => actor.Find(id)?.Rules ?? default);
            if (result) actor.NotifyInventoryChanged(); return result;
        }
    }
}
