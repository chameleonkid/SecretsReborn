using UnityEngine;

namespace SecretsReborn
{
    public sealed class AreaEntrance : MonoBehaviour
    {
        [SerializeField] private string entranceId;
        public string EntranceId => entranceId;
        public void Configure(string id) => entranceId = id;
        private void OnDrawGizmos() { Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, .4f); }
    }
}
