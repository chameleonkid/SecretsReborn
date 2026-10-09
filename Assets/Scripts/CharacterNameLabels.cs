using UnityEngine;
namespace SecretsReborn
{
    public sealed class CharacterNameLabels : MonoBehaviour
    {
        private GUIStyle label;
        private void OnGUI()
        {
            var camera = Camera.main;
            if (camera == null || SaveBook.IsOpen || GameSession.Instance.Busy) return;
            if (label == null) label = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14, fontStyle = FontStyle.Bold };
            foreach (var actor in FindObjectsByType<CharacterInventory>(FindObjectsSortMode.None))
            {
                string name = GameSession.Instance.World.CharacterProfile(actor.CharacterId)?.name ?? "Abenteurer";
                var screen = camera.WorldToScreenPoint(actor.transform.position + Vector3.up * (actor.GetComponent<CharacterAppearance>().NameHeight + .2f));
                if (screen.z < 0 || screen.x < 0 || screen.x > Screen.width || screen.y < 0 || screen.y > Screen.height) continue;
                var rect = new Rect(screen.x - 120, Screen.height - screen.y - 25, 240, 24);
                label.normal.textColor = Color.black; GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), name, label);
                label.normal.textColor = new Color(1,.92f,.72f); GUI.Label(rect, name, label);
            }
        }
    }
}
