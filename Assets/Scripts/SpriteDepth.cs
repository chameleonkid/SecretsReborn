using UnityEngine;

namespace SecretsReborn
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SpriteDepth : MonoBehaviour
    {
        private SpriteRenderer image;
        private void Awake() => image = GetComponent<SpriteRenderer>();
        // Keep depth-sorted actors/props above terrain (0/1) and below Foreground (1000).
        // A bounded curve preserves Y ordering without crossing the ground orders at Y=10.
        private void LateUpdate() => image.sortingOrder =
            500 - Mathf.RoundToInt(Mathf.Atan(transform.position.y * 0.04f) * 250);
    }
}
