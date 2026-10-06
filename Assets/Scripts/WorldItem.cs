using UnityEngine;

namespace SecretsReborn
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class WorldItem : MonoBehaviour
    {
        [SerializeField] private string worldItemId;
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(1)] private int count = 1;
        [SerializeField, Min(.1f)] private float pickupDistance = 1.6f;
        private bool collected;
        private bool hiddenByCollection;
        public string WorldItemId => worldItemId;
        public string Label => item != null ? item.DisplayName : "Item";
        private void OnEnable()
        {
            if (GameSession.Instance.World.IsCollected(worldItemId))
            { collected = true; hiddenByCollection = true; gameObject.SetActive(false); }
        }
        public void RefreshSession()
        {
            collected = GameSession.Instance.World.IsCollected(worldItemId);
            if (collected) { hiddenByCollection = true; gameObject.SetActive(false); }
            else if (hiddenByCollection) { hiddenByCollection = false; gameObject.SetActive(true); }
        }
        public void Configure(string id, ItemDefinition definition) { worldItemId = id; item = definition; }
        public void Configure(string id, ItemDefinition definition, int amount) { Configure(id, definition); count = Mathf.Max(1, amount); }
        public bool CanUse(CharacterInventory character) => !collected && character != null
            && character.HasStateAuthority && isActiveAndEnabled && character.gameObject.scene == gameObject.scene
            && Vector2.Distance(character.transform.position, transform.position) <= pickupDistance;
        public bool TryCollect(CharacterInventory character)
        {
            if (!CanUse(character) || !GameSession.Instance.CanFight(character)) return false;
            collected = true;
            if (!GameSession.Instance.World.TryCollect(worldItemId, () => character.TryReceive(item, count)))
            { collected = false; return false; }
            gameObject.SetActive(false);
            hiddenByCollection = true;
            return true;
        }
    }
}
