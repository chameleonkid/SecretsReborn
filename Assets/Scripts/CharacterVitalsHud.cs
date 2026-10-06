using UnityEngine;

namespace SecretsReborn
{
    public sealed class CharacterVitalsHud : MonoBehaviour
    {
        private GUIStyle label;
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
                float scale = Mathf.Clamp(Mathf.Min(Screen.width / 960f, Screen.height / 540f), .6f, 1.5f);
                GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale); GUI.depth = -10;
                float top = Screen.height / scale - 120;
                var panel = new Rect(16, top, 264, 104);
                ForestInventorySkin.Leather(panel); ForestInventorySkin.Button(panel);
                Bar(new Rect(34, top + 18, 228, 29), state.Health, state.MaxHealth, new Color(.63f, .12f, .16f), "HP");
                Bar(new Rect(34, top + 56, 228, 29), state.Mana, state.MaxMana, new Color(.13f, .36f, .68f), "Mana");
                if (state.IsDown) GUI.Label(new Rect(16, top - 24, 264, 24), "Kampfunfähig", label);
            }
            finally { GUI.matrix = oldMatrix; GUI.color = oldColor; GUI.depth = oldDepth; }
        }
        private void Bar(Rect rect, int current, int maximum, Color color, string title)
        {
            GUI.color = new Color(.045f, .055f, .05f); GUI.DrawTexture(rect, Texture2D.whiteTexture);
            var fill = new Rect(rect.x + 3, rect.y + 3, (rect.width - 6) * (maximum > 0 ? (float)current / maximum : 0), rect.height - 6);
            GUI.color = color; GUI.DrawTexture(fill, Texture2D.whiteTexture);
            GUI.color = Color.white; GUI.Label(rect, title + "  " + current + " / " + maximum, label);
        }
    }
}
