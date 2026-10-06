using UnityEngine;

namespace SecretsReborn
{
    // Authored drop positions with stable IDs reconstruct uncollected rewards after a load.
    public sealed class EnemyLoot : MonoBehaviour
    {
        [SerializeField] private string enemyId;
        [SerializeField] private LootTable lootTable;
        public void Configure(string id, LootTable table) { enemyId = id; lootTable = table; }
        private void Start() => RefreshSession();
        public void RefreshSession()
        {
            bool defeated = lootTable != null && lootTable.IsValid && lootTable.Source != LootSourceKind.Chest
                && GameSession.Instance.World.IsEnemyDefeated(enemyId);
            foreach (var item in GetComponentsInChildren<WorldItem>(true))
                item.gameObject.SetActive(defeated && !GameSession.Instance.World.IsCollected(item.WorldItemId));
        }
    }
}
