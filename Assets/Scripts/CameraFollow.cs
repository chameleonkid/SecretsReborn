using UnityEngine;

namespace SecretsReborn
{
    public sealed class CameraFollow : MonoBehaviour
    {
        public Transform Target { get; set; }
        [SerializeField, Min(0.01f)] private float smoothTime = 0.15f;
        private Vector3 velocity;

        private void LateUpdate()
        {
            if (Target == null) return;
            var destination = new Vector3(Target.position.x, Target.position.y, -10f);
            transform.position = Vector3.SmoothDamp(transform.position, destination,
                ref velocity, smoothTime);
        }
    }
}
