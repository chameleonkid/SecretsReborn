using UnityEngine;

namespace SecretsReborn
{
    // Temporary geometry for the movement prototype, created only in this scene at play time.
    public sealed class ForestSanctuaryPrototype : MonoBehaviour
    {
        private Texture2D texture;
        private Sprite sprite;
        private Material material;

        private void Awake()
        {
            texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1);
            material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));

            Block("Forest floor", Vector2.zero, new Vector2(26, 20), new Color(0.17f, 0.29f, 0.19f), false, -10);
            Block("Path", new Vector2(0, -4), new Vector2(2, 10), new Color(0.48f, 0.43f, 0.30f), false, -9);
            var stone = new Color(0.38f, 0.40f, 0.36f);
            Block("Dry spring", new Vector2(0, 2), new Vector2(3, 2), stone, true);
            Block("Dry basin", new Vector2(0, 2), new Vector2(2, 1), new Color(0.22f, 0.21f, 0.18f), false, 1);
            Block("West boundary", new Vector2(-13, 0), new Vector2(1, 21), stone, true);
            Block("East boundary", new Vector2(13, 0), new Vector2(1, 21), stone, true);
            Block("North boundary", new Vector2(0, 10), new Vector2(27, 1), stone, true);
            Block("South boundary", new Vector2(0, -10), new Vector2(27, 1), stone, true);
            foreach (var position in new[] { new Vector2(-5, 0), new Vector2(5, -2),
                new Vector2(-4, 5), new Vector2(5, 5), new Vector2(-7, -5) })
                Block("Tree placeholder", position, new Vector2(1.5f, 1.5f), new Color(0.08f, 0.20f, 0.11f), true);

            var player = Block("Player", new Vector2(0, -6), new Vector2(0.7f, 0.9f),
                new Color(0.95f, 0.73f, 0.25f), true, 2);
            var body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            player.AddComponent<PlayerMovement>();

            var camera = Camera.main;
            if (camera != null)
            {
                camera.orthographic = true;
                camera.orthographicSize = 5;
                camera.transform.position = new Vector3(0, -6, -10);
                camera.gameObject.AddComponent<CameraFollow>().Target = player.transform;
            }
        }

        private GameObject Block(string label, Vector2 position, Vector2 size, Color color,
            bool solid, int order = 0)
        {
            var block = new GameObject(label);
            block.transform.SetParent(transform, false);
            block.transform.localPosition = position;
            var renderer = block.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = size;
            renderer.color = color;
            renderer.sortingOrder = order;
            if (solid) block.AddComponent<BoxCollider2D>().size = size;
            return block;
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(12, 12, 350, 64), "Waldheiligtum - Bewegungsprototyp\nWASD / Pfeiltasten / Gamepad\nQuelle und Baeume sind feste Hindernisse.");
        }

        private void OnDestroy()
        {
            Destroy(material);
            Destroy(sprite);
            Destroy(texture);
        }
    }
}
