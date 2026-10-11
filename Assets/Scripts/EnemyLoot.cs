using UnityEngine;

namespace SecretsReborn
{
    // Stable IDs and authored offsets reconstruct uncollected rewards at the saved death position.
    public sealed class EnemyLoot : MonoBehaviour
    {
        [SerializeField] private string enemyId;
        [SerializeField] private LootTable lootTable;
        private Vector3 authoredPosition;
        private TreeMeleeEnemy enemy;
        public string EnemyId => enemyId;
        private void Awake() => authoredPosition=transform.position;
        public void Configure(string id, LootTable table) { enemyId = id; lootTable = table; }
        private void Start() => RefreshSession();
        public void RefreshSession()
        {
            if (enemy==null)
                foreach (var candidate in FindObjectsByType<TreeMeleeEnemy>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                    if (candidate.EnemyId==enemyId && candidate.gameObject.scene==gameObject.scene) { enemy=candidate; break; }
            var position=GameSession.Instance.World.DefeatPosition(enemyId);
            transform.position=authoredPosition;
            if (enemy!=null && position!=null && position.scenePath==gameObject.scene.path)
                transform.position+=new Vector3(position.x,position.y,position.z)-enemy.SpawnPosition;
            bool defeated = lootTable != null && lootTable.IsValid && lootTable.Source != LootSourceKind.Chest
                && GameSession.Instance.World.IsEnemyDefeated(enemyId);
            foreach (var item in GetComponentsInChildren<WorldItem>(true))
                item.gameObject.SetActive(defeated && !GameSession.Instance.World.IsCollected(item.WorldItemId));
        }
    }
}
