using UnityEngine;
using UnityEngine.InputSystem;

namespace SecretsReborn
{
    [DefaultExecutionOrder(-50)]
    public sealed class SaveBook : MonoBehaviour
    {
        [SerializeField] private float interactionDistance = 2;
        private CharacterInventory actor;
        private PlayerMovement movement;
        private bool open, movementEnabled, loadMode;
        private int selected, confirmSlot = -1;
        private string message;
        private Texture2D pages;
        private readonly SaveGameData[] summaries = new SaveGameData[3];
        private readonly bool[] occupied = new bool[3];
        private readonly string[] slotErrors = new string[3];
        private GUIStyle heading, ink, smallInk, centered;
        public static bool IsOpen { get; private set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => IsOpen = false;
        public bool CanUse(CharacterInventory character) => open && actor == character && character != null
            && character.HasStateAuthority && isActiveAndEnabled
            && Vector2.Distance(transform.position, character.transform.position) <= interactionDistance;
        private void OnTriggerEnter2D(Collider2D other)
        {
            var character = other.GetComponentInParent<CharacterInventory>();
            if (character != null && character.HasStateAuthority) actor = character;
        }
        private void OnTriggerExit2D(Collider2D other)
        {
            if (actor != null && other.GetComponentInParent<CharacterInventory>() == actor) { Close(); actor = null; }
        }
        private void Update()
        {
            if (!Application.isFocused || GameSession.Instance.Busy) return;
            var key = Keyboard.current; var pad = Gamepad.current;
            if (!open)
            {
                if (actor == null || actor.GetComponent<InventoryInteraction>()?.IsOpen == true || IsOpen) return;
                if (Vector2.Distance(transform.position, actor.transform.position) > interactionDistance) return;
                if (key != null && key.eKey.wasPressedThisFrame || pad != null && pad.buttonSouth.wasPressedThisFrame)
                {
                    open = IsOpen = true; confirmSlot = -1; message = null; RefreshSlots();
                    movement = actor.GetComponent<PlayerMovement>();
                    if (movement != null) { movementEnabled = movement.enabled; movement.enabled = false; }
                }
                return;
            }
            if (key != null && key.escapeKey.wasPressedThisFrame || pad != null && pad.buttonEast.wasPressedThisFrame) { Close(); return; }
            int direction = 0;
            if (key != null && key.upArrowKey.wasPressedThisFrame || pad != null && pad.dpad.up.wasPressedThisFrame) direction = -1;
            if (key != null && key.downArrowKey.wasPressedThisFrame || pad != null && pad.dpad.down.wasPressedThisFrame) direction = 1;
            if (direction != 0) { selected = (selected + direction + 3) % 3; confirmSlot = -1; }
            if (key != null && key.tabKey.wasPressedThisFrame || pad != null && (pad.leftShoulder.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame))
            { loadMode = !loadMode; confirmSlot = -1; }
            if (key != null && key.enterKey.wasPressedThisFrame || pad != null && pad.buttonSouth.wasPressedThisFrame) Execute(selected);
        }
        private void Execute(int slot)
        {
            selected = slot;
            if (!CanUse(actor)) { Close(); return; }
            if (!loadMode && SaveGameStore.Exists(SaveGameStore.SlotPath(slot)) && confirmSlot != slot)
            { confirmSlot = slot; message = "Belegten Slot überschreiben? Nochmals bestätigen."; return; }
            if (loadMode) GameSession.Instance.LoadAt(this, actor, slot);
            else GameSession.Instance.SaveAt(this, actor, slot);
            message = GameSession.Instance.Status; confirmSlot = -1;
            RefreshSlots();
        }
        public void Close()
        {
            if (!open) return;
            open = IsOpen = false;
            if (movement != null) movement.enabled = movementEnabled;
        }
        private void OnDisable() => Close();
        private void RefreshSlots()
        {
            for (int i = 0; i < 3; i++)
            {
                summaries[i] = null; slotErrors[i] = null;
                try { occupied[i] = SaveGameStore.Exists(SaveGameStore.SlotPath(i)); summaries[i] = SaveGameStore.ReadSummary(i); }
                catch (System.Exception) { occupied[i] = true; slotErrors[i] = "Nicht lesbar"; }
            }
        }
        private static string SceneLabel(SaveGameData data)
        {
            string path = data.savedScenePath;
            if (string.IsNullOrEmpty(path) && data.characters != null)
                foreach (var character in data.characters) if (character.hasPosition) { path = character.scenePath; break; }
            return string.IsNullOrEmpty(path) ? "Gebiet unbekannt" : System.IO.Path.GetFileNameWithoutExtension(path);
        }
        private static string TimeLabel(double seconds)
        {
            long total = (long)System.Math.Max(0, seconds);
            return (total / 3600).ToString("00") + ":" + (total / 60 % 60).ToString("00") + ":" + (total % 60).ToString("00");
        }
        private void Styles()
        {
            if (heading != null) return;
            heading = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, wordWrap = true };
            heading.normal.textColor = new Color(.22f, .15f, .08f);
            ink = new GUIStyle(heading) { fontSize = 18, fontStyle = FontStyle.Normal };
            smallInk = new GUIStyle(ink) { fontSize = 14 };
            centered = new GUIStyle(ink) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        }
        private static void Fill(Rect rect, Color color)
        {
            var old = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = old;
        }
        private static void Frame(Rect rect, Color color, float size)
        {
            Fill(new Rect(rect.x, rect.y, rect.width, size), color);
            Fill(new Rect(rect.x, rect.yMax - size, rect.width, size), color);
            Fill(new Rect(rect.x, rect.y, size, rect.height), color);
            Fill(new Rect(rect.xMax - size, rect.y, size, rect.height), color);
        }
        private bool PaperButton(Rect rect, string label, bool active = false)
        {
            Fill(rect, active ? new Color(.77f, .67f, .41f) : new Color(.84f, .79f, .63f));
            Frame(rect, new Color(.42f, .32f, .16f), active ? 3 : 1);
            GUI.Label(rect, label, centered);
            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }
        private void OnGUI()
        {
            if (!open)
            {
                if (actor != null && !IsOpen) GUI.Box(new Rect(Screen.width / 2 - 170, Screen.height - 125, 340, 35), "E / A: Speicherbuch öffnen");
                return;
            }
            Styles();
            if (pages == null) pages = Resources.Load<Texture2D>("InventoryUI/ForestSaveBook");
            Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0, 0, 0, .55f));
            var old = GUI.matrix;
            float scale = Mathf.Min((Screen.width - 24) / 900f, (Screen.height - 24) / 630f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 900 * scale) / 2, (Screen.height - 630 * scale) / 2), Quaternion.identity, Vector3.one * scale);
            // Keep the original square book aspect ratio, with all controls within the pages.
            if (pages != null) GUI.DrawTexture(new Rect(0, 0, 900, 600), pages, ScaleMode.ScaleToFit);



            GUI.Label(new Rect(115, 75, 285, 48), "CHRONIK", heading);
            GUI.Label(new Rect(115, 125, 285, 65), System.IO.Path.GetFileNameWithoutExtension(actor.gameObject.scene.path), ink);
            GUI.Label(new Rect(115, 190, 285, 45), "Spielzeit  " + TimeLabel(GameSession.Instance.World.PlayTimeSeconds), ink);
            var appearance = actor.GetComponent<CharacterAppearance>();
            if (appearance != null)
                for (int layer = 0; layer < 6; layer++)
                {
                    var sprite = appearance.FrontPreviewLayer(layer, out var tint);
                    if (sprite == null) continue;
                    var previous = GUI.color; GUI.color = tint;
                    var uv = sprite.textureRect;
                    GUI.DrawTextureWithTexCoords(new Rect(190, 245, 150, 150), sprite.texture,
                        new Rect(uv.x / sprite.texture.width, uv.y / sprite.texture.height, uv.width / sprite.texture.width, uv.height / sprite.texture.height));
                    GUI.color = previous;
                }
            GUI.Label(new Rect(115, 420, 285, 65), "Nur am Buch wird deine\nReise festgehalten.", ink);
            GUI.Label(new Rect(115, 495, 285, 50), "LB/RB / Tab: Modus\nD-Pad / Pfeile: Speicherplatz", smallInk);
            if (PaperButton(new Rect(490, 80, 135, 38), "Speichern", !loadMode)) { loadMode = false; confirmSlot = -1; }
            if (PaperButton(new Rect(630, 80, 135, 38), "Laden", loadMode)) { loadMode = true; confirmSlot = -1; }
            for (int i = 0; i < 3; i++)
            {
                var rect = new Rect(490, 140 + i * 95, 275, 85);
                Fill(rect, selected == i ? new Color(.65f, .45f, .16f, .23f) : new Color(.65f, .45f, .16f, .08f));
                Frame(rect, selected == i ? new Color(.54f, .36f, .08f) : new Color(.63f, .56f, .4f), selected == i ? 3 : 1);
                GUI.Label(new Rect(rect.x + 12, rect.y + 7, 250, 25), "SPEICHERPLATZ " + (i + 1), centered);
                var data = summaries[i];
                string details = slotErrors[i] ?? (data == null ? "Noch keine Reise gespeichert" : SceneLabel(data) + "\n" + (data.version < 3 ? "Spielzeit unbekannt" : "Spielzeit  " + TimeLabel(data.playTimeSeconds)));
                GUI.Label(new Rect(rect.x + 12, rect.y + 36, 251, 48), details, smallInk);
                if (GUI.Button(rect, GUIContent.none, GUIStyle.none)) { selected = i; confirmSlot = -1; message = null; }
            }
            string action = confirmSlot == selected ? "Überschreiben bestätigen" : loadMode ? "Reise laden (A)" : "Reise speichern (A)";
            bool enabled = GUI.enabled; GUI.enabled = !loadMode || occupied[selected];
            if (PaperButton(new Rect(490, 435, 275, 38), action, true)) Execute(selected);
            GUI.enabled = enabled;
            GUI.Label(new Rect(490, 485, 275, 60), message ?? "Enter / A: Bestätigen", smallInk);
            if (PaperButton(new Rect(490, 570, 275, 35), "Schließen (Esc / B)")) Close();
            GUI.matrix = old;
        }
    }
}

