using UnityEngine;

namespace SecretsReborn
{
    public sealed class CharacterVitalsHud : MonoBehaviour
    {
        private GUIStyle label;
        private Texture2D[] hearts;
        private static readonly string[] HeartPixels = {
            "..###.###..", ".#########.", "###########", "###########",
            ".#########.", "..#######..", "...#####...", "....###....", ".....#....." };
        private void OnGUI()
        {
            if (SaveBook.IsOpen || GameSession.Instance.Busy) return;
            var camera = Camera.main;
            var follow = camera != null ? camera.GetComponent<CameraFollow>() : null;
            var actor = follow != null && follow.Target != null ? follow.Target.GetComponent<CharacterInventory>() : null;
            if (actor == null || actor.GetComponent<InventoryInteraction>()?.IsOpen == true) return;
            var state = GameSession.Instance.World.CharacterVitals(actor.CharacterId);
            if (label == null) label = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14, fontStyle = FontStyle.Bold };
            var oldMatrix = GUI.matrix; var oldColor = GUI.color; int oldDepth = GUI.depth;
            try
            {
                // Integer scaling keeps pixel-art edges aligned to screen pixels.
                float scale = Mathf.Clamp(Mathf.Floor(Mathf.Min(Screen.width / 960f, Screen.height / 540f)), 1, 2);
                GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale); GUI.depth = -10;
                int rows = (state.HeartContainers + 9) / 10;
                const float top = 18;
                for (int i = 0; i < state.HeartContainers; i++)
                    Heart(new Vector2(18 + i % 10 * 26, top + i / 10 * 26), state.HeartFill(i));
                Bar(new Rect(18, top + rows * 26 + 6, 200, 20), state.Mana, state.MaxMana, new Color(.2f, .65f, .35f), "Mana");
                if (state.IsDown) GUI.Label(new Rect(16, top + rows * 26 + 30, 264, 24), "Kampfunfähig", label);
            }
            finally { GUI.matrix = oldMatrix; GUI.color = oldColor; GUI.depth = oldDepth; }
        }
        private static bool HeartPixel(int x, int y) => x >= 0 && x < 11 && y >= 0 && y < HeartPixels.Length && HeartPixels[y][x] == '#';
        private void Heart(Vector2 position, int fill)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (hearts == null)
            {
                hearts = new Texture2D[3];
                for (int i = 0; i < hearts.Length; i++) hearts[i] = CreateHeart(i);
            }
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(position.x, position.y, 24, 20), hearts[fill]);
        }
        private static Texture2D CreateHeart(int fill)
        {
            // One complete texture per state prevents seams between separate GUI quads.
            var texture = new Texture2D(12, 10, TextureFormat.RGBA32, false) {
                name = "HUD heart " + fill, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixels(new Color[120]);
            for (int y = 0; y < HeartPixels.Length; y++) for (int x = 0; x < 11; x++)
                if (HeartPixel(x, y))
                    texture.SetPixel(x + 1, 8 - y, new Color(.04f, .055f, .05f, .9f));
            for (int y = 0; y < HeartPixels.Length; y++) for (int x = 0; x < 11; x++)
                if (HeartPixel(x, y))
                {
                    bool edge = !HeartPixel(x - 1, y) || !HeartPixel(x + 1, y) || !HeartPixel(x, y - 1) || !HeartPixel(x, y + 1);
                    bool filled = fill == 2 || fill == 1 && x <= 5;
                    var color = edge ? new Color(.88f, .84f, .67f) : !filled ? new Color(.16f, .19f, .18f)
                        : y <= 2 ? new Color(1, .48f, .48f) : y >= 5 ? new Color(.61f, .06f, .15f) : new Color(.91f, .16f, .25f);
                    texture.SetPixel(x, 9 - y, color);
                }
            texture.Apply(false, true); return texture;
        }
        private void OnDestroy()
        {
            if (hearts != null) foreach (var texture in hearts) if (texture != null) Destroy(texture);
        }
        private void Bar(Rect rect, int current, int maximum, Color color, string title)
        {
            GUI.color = new Color(.88f, .84f, .67f); GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = new Color(.045f, .055f, .05f); GUI.DrawTexture(new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4), Texture2D.whiteTexture);
            var fill = new Rect(rect.x + 3, rect.y + 3, (rect.width - 6) * (maximum > 0 ? (float)current / maximum : 0), rect.height - 6);
            GUI.color = color; GUI.DrawTexture(fill, Texture2D.whiteTexture);
            GUI.color = Color.white; GUI.Label(rect, title + "  " + current + " / " + maximum, label);
        }
    }
}
