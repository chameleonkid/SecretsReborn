using UnityEngine;

namespace SecretsReborn
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SpriteDepth : MonoBehaviour
    {
        private SpriteRenderer image;
        private void Awake() => image = GetComponent<SpriteRenderer>();
        private void LateUpdate() => image.sortingOrder = 100 - Mathf.RoundToInt(transform.position.y * 10);
    }
}
