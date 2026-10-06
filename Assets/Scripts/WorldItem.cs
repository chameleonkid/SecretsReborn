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
        public string WorldItemId => worldItemId;
        public string Label => item != null ? item.DisplayName : "Item";
        public void Configure(string id, ItemDefinition definition) { worldItemId = id; item = definition; }
        public bool TryCollect(CharacterInventory character)
        {
            if (collected || character == null || !character.HasStateAuthority || !isActiveAndEnabled
                || Vector2.Distance(character.transform.position, transform.position) > pickupDistance) return false;
            collected = true;
            if (!character.TryReceive(item, count)) { collected = false; return false; }
            gameObject.SetActive(false);
            return true;
        }
    }
}
