using UnityEngine;

namespace SecretsReborn
{
    // Temporary geometry for the movement prototype, created only in this scene at play time.
    public sealed class ForestSanctuaryPrototype : MonoBehaviour
    {
        private Texture2D texture;
        private Sprite sprite;
        private Material material;
        private Sprite grassArt;
        private Sprite stoneArt;
        private Sprite waterArt;
        private Sprite treeArt;
        private PlayerLantern lantern;
        private readonly RuneSequence sequence = new RuneSequence();
        private readonly RuneCircle[] circles = new RuneCircle[4];
        private SpriteRenderer basin;
        private GameObject gate;
        private string puzzleMessage = "Finde die vier Runenkreise in der richtigen Reihenfolge.";

        private void Awake()
        {
            texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f,
                1, 0, SpriteMeshType.FullRect);
            material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            treeArt = Resources.Load<Sprite>("SanctuaryArt/tree");
            var terrain = Resources.Load<Texture2D>("SanctuaryArt/terrain");
            var water = Resources.Load<Texture2D>("SanctuaryArt/water");
            if (terrain != null)
            {
                grassArt = Sprite.Create(terrain, new Rect(160, 480, 32, 32), Vector2.one * 0.5f,
                    32, 0, SpriteMeshType.FullRect);
                stoneArt = Sprite.Create(terrain, new Rect(128, 1312, 32, 32), Vector2.one * 0.5f,
                    32, 0, SpriteMeshType.FullRect);
            }
            if (water != null)
                waterArt = Sprite.Create(water, new Rect(16, 192, 16, 16), Vector2.one * 0.5f,
                    32, 0, SpriteMeshType.FullRect);

            Block("Forest floor", Vector2.zero, new Vector2(26, 20), new Color(0.17f, 0.29f, 0.19f), false, -10);
            Block("Path", new Vector2(0, -4), new Vector2(2, 10), new Color(0.48f, 0.43f, 0.30f), false, -9);
            var stone = new Color(0.38f, 0.40f, 0.36f);
            Block("Dry spring", new Vector2(0, 2), new Vector2(3, 2), stone, true);
            basin = Block("Dry basin", new Vector2(0, 2), new Vector2(2, 1), new Color(0.22f, 0.21f, 0.18f), false, 1).GetComponent<SpriteRenderer>();
            Block("West boundary", new Vector2(-13, 0), new Vector2(1, 21), stone, true);
            Block("East boundary", new Vector2(13, 0), new Vector2(1, 21), stone, true);
            Block("North west boundary", new Vector2(-7.5f, 10), new Vector2(12, 1), stone, true);
            Block("North east boundary", new Vector2(7.5f, 10), new Vector2(12, 1), stone, true);
            Block("New path", new Vector2(0, 12), new Vector2(3, 5), new Color(0.48f, 0.43f, 0.30f), false, -9);
            gate = Block("Sealed passage", new Vector2(0, 10), new Vector2(3, 1), stone, true);
            Block("Path west edge", new Vector2(-2, 12), new Vector2(1, 4), stone, true);
            Block("Path east edge", new Vector2(2, 12), new Vector2(1, 4), stone, true);
            Block("Prototype end", new Vector2(0, 14), new Vector2(5, 1), stone, true);
            Block("South boundary", new Vector2(0, -10), new Vector2(27, 1), stone, true);
            foreach (var position in new[] { new Vector2(-5, 0), new Vector2(5, -2),
                new Vector2(-4, 5), new Vector2(5, 5), new Vector2(-7, -5) })
            {
                var tree = Block("Tree", position, new Vector2(1.5f, 1.5f), new Color(0.08f, 0.20f, 0.11f), true);
                if (treeArt == null) continue;
                var treeRenderer = tree.GetComponent<SpriteRenderer>();
                treeRenderer.drawMode = SpriteDrawMode.Simple;
                treeRenderer.sprite = treeArt;
                treeRenderer.color = Color.white;
                tree.GetComponent<BoxCollider2D>().size = new Vector2(0.65f, 0.65f);
                tree.AddComponent<SpriteDepth>();
            }

            var player = Block("Player", new Vector2(0, -6), new Vector2(0.7f, 0.9f),
                new Color(0.95f, 0.73f, 0.25f), true, 2);
            var body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            player.AddComponent<PlayerMovement>();
            player.AddComponent<SpriteDepth>();
            lantern = player.AddComponent<PlayerLantern>();
            var lanternMarker = Block("Lantern glow", Vector2.zero, new Vector2(0.25f, 0.25f),
                new Color(1f, 0.95f, 0.55f), false, 3);
            lanternMarker.transform.SetParent(player.transform, false);
            lanternMarker.transform.localPosition = new Vector3(0.5f, 0, 0);
            lantern.Indicator = lanternMarker.GetComponent<SpriteRenderer>();
            lantern.Indicator.enabled = false;

            var points = new[] { new Vector2(0, -3), new Vector2(4, 0),
                new Vector2(3, 6), new Vector2(-2, 7) };
            CreateSign(new Vector2(0, -5), points[0]);
            for (int i = 0; i < points.Length; i++)
            {
                var circle = new GameObject("Rune circle " + (i + 1));
                circle.transform.SetParent(transform, false);
                circle.transform.localPosition = points[i];
                for (int r = 0; r < 12; r++)
                {
                    float angle = r * Mathf.PI * 2 / 12;
                    var rune = Block("Rune", Vector2.zero, new Vector2(0.12f, 0.3f),
                        new Color(0.65f, 0.65f, 0.8f), false, 1);
                    rune.transform.SetParent(circle.transform, false);
                    rune.transform.localPosition = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                    rune.transform.localRotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
                }
                circles[i] = circle.AddComponent<RuneCircle>();
                circles[i].Initialize(player.transform, i, EnterCircle);
                circle.AddComponent<HiddenSign>().Initialize(lantern);
                var destination = i < 3 ? points[i + 1] : new Vector2(0, 10);
                CreateSign(points[i], destination);
                // Keep arrow spacing below the lantern radius to form a discoverable trail.
                int steps = Mathf.CeilToInt(Vector2.Distance(points[i], destination) / 1.8f);
                for (int step = 1; step < steps; step++)
                    CreateSign(Vector2.Lerp(points[i], destination, (float)step / steps), destination);
            }

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
            Sprite art = label == "Forest floor" ? grassArt :
                (solid && label != "Player" && label != "Tree" ? stoneArt : null);
            if (art != null)
            {
                renderer.sprite = art;
                renderer.drawMode = SpriteDrawMode.Tiled;
                renderer.size = size;
                renderer.color = Color.white;
            }
            if (solid) block.AddComponent<BoxCollider2D>().size = size;
            return block;
        }

        private void EnterCircle(int index)
        {
            if (sequence.IsComplete) return;
            bool correct = sequence.Enter(index);
            for (int i = 0; i < circles.Length; i++) circles[i].SetActivated(i < sequence.Progress);
            puzzleMessage = correct ? "Runenkreis aktiviert: " + sequence.Progress + "/4"
                : "Falsche Reihenfolge. Zurueck zum ersten Kreis!";
            if (!sequence.IsComplete) return;
            basin.color = new Color(0.2f, 0.65f, 1f);
            if (waterArt != null)
            {
                basin.sprite = waterArt;
                basin.drawMode = SpriteDrawMode.Tiled;
                basin.size = new Vector2(2, 1);
                basin.color = Color.white;
            }
            gate.SetActive(false);
            puzzleMessage = "Die Quelle fliesst wieder. Der Weg im Norden ist offen!";
        }

        private void CreateSign(Vector2 position, Vector2 destination)
        {
            var sign = new GameObject("Hidden sign");
            sign.transform.SetParent(transform, false);
            sign.transform.localPosition = position;
            var direction = destination - position;
            sign.transform.localRotation = Quaternion.Euler(0, 0,
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90);
            // A small arrow pointing north, made from three non-colliding placeholder strokes.
            foreach (var stroke in new[] { new Vector3(0, 0, 0),
                new Vector3(-0.14f, 0.24f, -45), new Vector3(0.14f, 0.24f, 45) })
            {
                var part = Block("Glyph stroke", Vector2.zero, new Vector2(0.09f, 0.48f),
                    new Color(0.55f, 0.95f, 1f), false, 1);
                part.transform.SetParent(sign.transform, false);
                part.transform.localPosition = new Vector3(stroke.x, stroke.y, 0);
                part.transform.localRotation = Quaternion.Euler(0, 0, stroke.z);
            }
            sign.AddComponent<HiddenSign>().Initialize(lantern);
        }

        private void OnGUI()
        {
            string status = lantern != null && lantern.IsLit ? "AN" : "AUS";
            GUI.Box(new Rect(12, 12, 460, 110),
                "Waldheiligtum\nWASD / Pfeiltasten / Gamepad: Bewegen\nL / obere Gamepad-Taste: Laterne "
                + status + "\nMit der Laterne nahe Zeichen entdecken.\n" + puzzleMessage);
        }

        private void OnDestroy()
        {
            Destroy(material);
            Destroy(sprite);
            Destroy(texture);
            Destroy(grassArt);
            Destroy(stoneArt);
            Destroy(waterArt);
        }
    }
}
