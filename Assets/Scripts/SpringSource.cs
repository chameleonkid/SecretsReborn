using UnityEngine;

namespace SecretsReborn
{
    public sealed class SpringSource : MonoBehaviour
    {
        [SerializeField] private GameObject water;
        public void Configure(GameObject surface) => water = surface;
        public void Restore() => water.SetActive(true);
        public void SetRestored(bool value) => water.SetActive(value);
    }
}
