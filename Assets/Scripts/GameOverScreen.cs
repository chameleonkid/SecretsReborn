using UnityEngine;

namespace SecretsReborn
{
    public sealed class GameOverScreen : MonoBehaviour
    {
        private void OnGUI()
        {
            var session = GetComponent<GameSession>();
            if (session == null || session.Busy || !session.IsGameOver) return;
            int depth = GUI.depth; var color = GUI.color; var matrix = GUI.matrix;
            try
            {
                GUI.depth = -1000;
                GUI.color = new Color(.025f, .035f, .035f, .9f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
                float scale = Mathf.Min(Screen.width / 800f, Screen.height / 540f);
                GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 800 * scale) / 2, (Screen.height - 540 * scale) / 2), Quaternion.identity, Vector3.one * scale);
                ForestInventorySkin.Panel(new Rect(100, 115, 600, 320));
                var heading = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 48, fontStyle = FontStyle.Bold };
                heading.normal.textColor = new Color(.95f, .74f, .44f);
                GUI.Label(new Rect(130, 155, 540, 65), "GAME OVER", heading);
                var text = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 17, wordWrap = true };
                text.normal.textColor = new Color(.88f, .9f, .85f);
                GUI.Label(new Rect(150, 230, 500, 45), "Alle Charaktere sind gefallen.", text);
                if (session.CanRetryCheckpoint)
                {
                    GUI.Label(new Rect(155, 280, 490, 40), session.HasSavedCheckpoint
                        ? "Fortschritt seit dem letzten Speicherstand\nwird beim Neustart zurückgesetzt."
                        : "Noch kein Speicherstand: Rückkehr zum Start-Checkpoint.\nFortschritt seit dem Start wird zurückgesetzt.", text);
                    var button = new Rect(245, 350, 310, 40);
                    ForestInventorySkin.Button(button);
                    GUI.Label(button, NetworkCoop.Running ? "Gemeinsam neu starten (Enter / A)" : "Erneut versuchen (Enter / A)", text);
                    if (GUI.Button(button, GUIContent.none, GUIStyle.none)) session.ReturnToCheckpoint();
                }
                else GUI.Label(new Rect(155, 285, 490, 70), NetworkCoop.IsReplica
                    ? "Der Host kann den gemeinsamen Neustart anfragen.\nDanach müssen alle Spieler zustimmen."
                    : "Kein Rückkehrpunkt verfügbar.", text);
            }
            finally { GUI.depth = depth; GUI.color = color; GUI.matrix = matrix; }
        }
    }
}
