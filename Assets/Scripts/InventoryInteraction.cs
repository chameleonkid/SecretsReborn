using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SecretsReborn
{
    [RequireComponent(typeof(CharacterInventory))]
    public sealed class InventoryInteraction : MonoBehaviour
    {
        [SerializeField] private bool localInput = true;
        private readonly HashSet<WorldItem> nearby = new HashSet<WorldItem>();
        private CharacterInventory inventory;
        private PlayerMovement movement;
        private CharacterAppearance appearance;
        private bool open, movementWasEnabled;
        public bool IsOpen => open;
        public void SetLocalInput(bool value) { localInput = value; if (!value) SetOpen(false); }
        private string message;
        private int selected, dragSource = -1;
        private Vector2 dragStart;
        private Vector2Int heldDirection;
        private float nextNavigation;
        private bool controllerSelection;
        private GUIStyle titleStyle, textStyle, smallStyle;
        private Rect pressedButton;
        private bool buttonPressed;
        private bool reviveInterrupted;
        private CharacterInventory NearestDowned()
        {
            CharacterInventory best = null; float distance = float.PositiveInfinity;
            foreach (var candidate in FindObjectsByType<CharacterInventory>(FindObjectsSortMode.None))
            {
                if (!GameSession.Instance.CanRevive(inventory, candidate)) continue;
                float squared = (candidate.transform.position - transform.position).sqrMagnitude;
                if (squared < distance) { best = candidate; distance = squared; }
            }
            return best;
        }
        private Material grayscaleIconMaterial;
        private static readonly string[] Labels = { "Helm / Hut", "Schultern", "Armor", "Gürtel", "Hände", "Beine", "Stiefel", "Ring 1", "Ring 2", "Amulett", "Seal", "Cloak", "Haupthand", "Nebenhand", "Lampe" };
        private static readonly EquipmentSlot[] Left = { EquipmentSlot.Head, EquipmentSlot.Shoulders, EquipmentSlot.Armor, EquipmentSlot.Hands, EquipmentSlot.Waist, EquipmentSlot.Legs, EquipmentSlot.Feet };
        private static readonly EquipmentSlot[] Right = { EquipmentSlot.Ring1, EquipmentSlot.Ring2, EquipmentSlot.Amulet, EquipmentSlot.Seal, EquipmentSlot.Cloak, EquipmentSlot.MainHand, EquipmentSlot.OffHand };
        private void Awake() { inventory = GetComponent<CharacterInventory>(); movement = GetComponent<PlayerMovement>(); appearance = GetComponent<CharacterAppearance>(); }
        private void OnTriggerEnter2D(Collider2D other)
        { var item = other.GetComponentInParent<WorldItem>(); if (item != null) nearby.Add(item); }
        private void OnTriggerExit2D(Collider2D other)
        { var item = other.GetComponentInParent<WorldItem>(); if (item != null) nearby.Remove(item); }
        private WorldItem Nearest()
        {
            WorldItem best = null; float distance = float.PositiveInfinity;
            // Client mirrors do not simulate colliders; their hint still uses the
            // authoritative pickup range rather than relying on local triggers.
            IEnumerable<WorldItem> candidates = NetworkCoop.IsReplica
                ? FindObjectsByType<WorldItem>(FindObjectsSortMode.None) : nearby;
            foreach (var item in candidates)
            {
                if (item == null || !item.CanUse(inventory)) continue;
                float candidate = ((Vector2)(item.transform.position - transform.position)).sqrMagnitude;
                if (candidate < distance) { best = item; distance = candidate; }
            }
            return best;
        }
        private void InteractionTarget(out WorldItem item, out TreasureChest chest)
        {
            item = Nearest(); chest = NearestChest();
            if (item == null || chest == null) return;
            // Compare usable targets together; an overlapping pickup trigger must not block a chest.
            float itemDistance = ((Vector2)(item.transform.position - transform.position)).sqrMagnitude;
            float chestDistance = ((Vector2)(chest.transform.position - transform.position)).sqrMagnitude;
            if (chestDistance <= itemDistance) item = null;
            else chest = null;
        }
        private TreasureChest NearestChest()
        {
            TreasureChest best = null; float distance = float.PositiveInfinity;
            foreach (var chest in FindObjectsByType<TreasureChest>(FindObjectsSortMode.None))
            {
                if (chest.Opened || !chest.CanUse(inventory)) continue;
                float candidate = (chest.transform.position - transform.position).sqrMagnitude;
                if (candidate < distance) { best = chest; distance = candidate; }
            }
            return best;
        }
        private void Update()
        {
            if (!localInput) return;
            if (!Application.isFocused || SaveBook.IsOpen || GameSession.Instance.RewardPresentationActive)
            { GameSession.Instance.CancelRevive(inventory); return; }
            if (GameSession.Instance.World.CharacterVitals(inventory.CharacterId).IsDown)
            { GameSession.Instance.CancelRevive(inventory); SetOpen(false); return; }
            var key = Keyboard.current; var pad = Gamepad.current;
            bool interactionHeld = key != null && key.eKey.isPressed || pad != null && pad.buttonSouth.isPressed;
            if (!interactionHeld) { reviveInterrupted = false; GameSession.Instance.CancelRevive(inventory); }
            if ((key != null && (key.iKey.wasPressedThisFrame || (open && key.escapeKey.wasPressedThisFrame)))
                || (pad != null && pad.startButton.wasPressedThisFrame)) SetOpen(!open);
            if (!open)
            {
                var downed = NearestDowned();
                if (interactionHeld && downed != null)
                {
                    bool pressed = key != null && key.eKey.wasPressedThisFrame || pad != null && pad.buttonSouth.wasPressedThisFrame;
                    if (!pressed && !GameSession.Instance.IsReviving(inventory)) reviveInterrupted = true;
                    if (!reviveInterrupted && !GameSession.Instance.HoldRevive(inventory, downed)) reviveInterrupted = true;
                    return;
                }
                GameSession.Instance.CancelRevive(inventory);
                if ((key != null && key.eKey.wasPressedThisFrame) || (pad != null && pad.buttonSouth.wasPressedThisFrame))
                {
                    InteractionTarget(out var item, out var chest);
                    if (item != null) message = item.TryCollect(inventory) ? "Aufgehoben: " + item.Label : "Aufheben nicht möglich (kein Platz oder zu weit entfernt).";
                    else
                    {
                        if (chest != null) message = chest.TryOpen(inventory) ? "Truhe geöffnet. Beute erhalten." : "Truhe bleibt geschlossen: Platz im Inventar prüfen.";
                    }
                }
                return;
            }
            GameSession.Instance.CancelRevive(inventory);
            if ((key != null && key.tabKey.wasPressedThisFrame) || (pad != null && (pad.rightShoulder.wasPressedThisFrame || pad.leftShoulder.wasPressedThisFrame)))
            { selected = selected < 40 ? 40 : 0; controllerSelection = true; }
            int dx = 0, dy = 0;
            if (key != null && key.leftArrowKey.wasPressedThisFrame || pad != null && pad.dpad.left.wasPressedThisFrame) dx = -1;
            if (key != null && key.rightArrowKey.wasPressedThisFrame || pad != null && pad.dpad.right.wasPressedThisFrame) dx = 1;
            if (key != null && key.upArrowKey.wasPressedThisFrame || pad != null && pad.dpad.up.wasPressedThisFrame) dy = -1;
            if (key != null && key.downArrowKey.wasPressedThisFrame || pad != null && pad.dpad.down.wasPressedThisFrame) dy = 1;
            if (pad != null)
            {
                Vector2 axis = pad.dpad.ReadValue();
                if (axis.sqrMagnitude < .1f) axis = pad.leftStick.ReadValue();
                var direction = axis.magnitude < .55f ? Vector2Int.zero : Mathf.Abs(axis.x) > Mathf.Abs(axis.y)
                    ? new Vector2Int(axis.x > 0 ? 1 : -1, 0) : new Vector2Int(0, axis.y > 0 ? -1 : 1);
                if (direction != Vector2Int.zero && (direction != heldDirection || Time.unscaledTime >= nextNavigation))
                {
                    dx = direction.x; dy = direction.y;
                    nextNavigation = Time.unscaledTime + (direction != heldDirection ? .35f : .12f);
                }
                heldDirection = direction;
            }
            if (dx != 0 || dy != 0)
            {
                selected = Navigate(selected, dx, dy); controllerSelection = true;
            }
            if (key != null && key.enterKey.wasPressedThisFrame || pad != null && pad.buttonSouth.wasPressedThisFrame) Activate(selected);
            if (pad != null && pad.buttonEast.wasPressedThisFrame) SetOpen(false);
            if (pad != null && pad.buttonWest.wasPressedThisFrame && selected >= 40) Activate(selected);
        }
        private static int Navigate(int index, int dx, int dy)
            => InventoryNavigation.Navigate(index, dx, dy);
        private void Activate(int slot)
        {
            bool result = false;
            if (slot >= 40) result = inventory.TryUnequip((EquipmentSlot)(slot - 40));
            else { var target = inventory.PreferredSlot(slot); result = target.HasValue ? inventory.TryEquip(slot, target.Value) : inventory.TryUseItem(slot); }
            message = result ? "Gegenstand angelegt, abgelegt oder benutzt." : "Nicht möglich: Slot ungeeignet, Tasche voll oder Effekt nicht benötigt.";
        }
        private void SetOpen(bool value)
        {
            if (open == value) return;
            open = value; dragSource = -1; buttonPressed = false; heldDirection = Vector2Int.zero; nextNavigation = 0;
            if (movement == null) return;
            if (open) { movementWasEnabled = movement.enabled; movement.enabled = false; }
            else movement.enabled = movementWasEnabled;
        }
        private void OnDisable() { SetOpen(false); if (inventory != null) GameSession.Existing?.CancelRevive(inventory); }
        private ItemDefinition Item(int index) => inventory.Find(index < 40 ? inventory.State.GetSlot(index)?.itemId : inventory.State.GetEquipment((EquipmentSlot)(index - 40)));
        private void OnGUI()
        {
            if (!localInput) return;
            if (GameSession.Instance.RewardPresentationActive) return;
            if (!open)
            {
                var downed = NearestDowned();
                if (downed != null)
                {
                    float progress = GameSession.Instance.ReviveProgress(inventory);
                    GUI.Box(new Rect(12, Screen.height - 85, 420, 70), "E / A halten: Wiederbeleben (3 Sekunden)\n" + (reviveInterrupted ? "Unterbrochen – Taste loslassen und erneut halten." : "Mitspieler: " + downed.CharacterId));
                    GUI.Box(new Rect(24, Screen.height - 35, 396 * progress, 12), "");
                    return;
                }
                InteractionTarget(out var item, out var chest);
                GUI.Box(new Rect(12, Screen.height - 65, 570, 55), "I / Start: Inventar     E / A: Aufheben / Truhe\n" + (item != null ? item.Label : chest != null ? chest.Label : message ?? ""));
                return;
            }
            float scale = Mathf.Min(Screen.width / 1040f, Screen.height / 600f);
            var previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1040 * scale) / 2, (Screen.height - 600 * scale) / 2), Quaternion.identity, Vector3.one * scale);
            Styles();
            ForestInventorySkin.Panel(new Rect(0, 0, 1040, 600));
            ForestInventorySkin.Leather(new Rect(20, 60, 475, 435));
            ForestInventorySkin.Leather(new Rect(505, 60, 515, 435));
            GUI.Label(new Rect(40, 30, 450, 30), "AUSRÜSTUNG", titleStyle);
            GUI.Label(new Rect(520, 30, 480, 30), "INVENTAR", titleStyle);
            GUI.Label(new Rect(520, 68, 480, 30), "REISEGEPÄCK   /   40 Plätze", textStyle);
            var rects = new Rect[40 + InventoryState.EquipmentCapacity];
            for (int i = 0; i < 40; i++) rects[i] = new Rect(520 + i % 10 * 49, 105 + i / 10 * 49, 45, 45);
            for (int row = 0; row < 7; row++)
            {
                rects[40 + (int)Left[row]] = new Rect(40, 65 + row * 61, 54, 54);
                rects[40 + (int)Right[row]] = new Rect(420, 65 + row * 61, 54, 54);
            }
            Preview(new Rect(155, 100, 210, 320));
            rects[40 + (int)EquipmentSlot.Lamp] = new Rect(233, 429, 54, 54);
            int hover = -1;
            for (int i = 0; i < rects.Length; i++)
            {
                var rect = rects[i]; var item = Item(i);
                ForestInventorySkin.Slot(rect, i == selected);
                if (item == null && i >= 40)
                {
                    var old = GUI.color; GUI.color = new Color(.48f, .48f, .48f, 1);
                    if (i == 40 + (int)EquipmentSlot.Lamp && inventory.Find("warm-lamp")?.Icon != null)
                        DrawSprite(new Rect(rect.x + 8, rect.y + 8, rect.width - 16, rect.height - 16), inventory.Find("warm-lamp").Icon, GUI.color, null, GrayscaleIconMaterial());
                    else ForestInventorySkin.Glyph(new Rect(rect.x + 8, rect.y + 8, rect.width - 16, rect.height - 16), i - 40);
                    GUI.color = old;
                }
                if (rect.Contains(Event.current.mousePosition)) Border(rect, new Color(.6f, .7f, .7f), 2);
                if (item != null)
                {
                    Border(new Rect(rect.x + 6, rect.y + 6, rect.width - 12, rect.height - 12), item.QualityColor, 2);
                    if (item.Icon != null) DrawSprite(new Rect(rect.x + 9, rect.y + 9, rect.width - 18, rect.height - 18), item.Icon, item.IconTint, item.IconContent);
                    else GUI.Label(rect, item.DisplayName.Substring(0, Mathf.Min(6, item.DisplayName.Length)), smallStyle);
                    var stack = i < 40 ? inventory.State.GetSlot(i) : null;
                    if (stack != null && stack.count > 1) GUI.Label(new Rect(rect.x + 3, rect.yMax - 20, rect.width, 20), stack.count.ToString());
                }
                else if (i == 53 && inventory.Find(inventory.State.GetEquipment(EquipmentSlot.MainHand))?.Rules.twoHanded == true)
                    GUI.Label(rect, "2H");
                if (rect.Contains(Event.current.mousePosition)) hover = i;
            }
            GUI.Label(new Rect(520, 320, 470, 65), "Stick / D-Pad / Pfeile: auswählen · LB/RB / Tab: Bereich\nA / Enter / Rechtsklick: anlegen oder ablegen\nZiehen: verschieben · B / Esc: schließen", smallStyle);
            if (Event.current.type == EventType.MouseMove || Event.current.type == EventType.MouseDown) controllerSelection = false;
            int inspectedIndex = !controllerSelection && hover >= 0 ? hover : selected;
            var inspected = Item(inspectedIndex);
            ForestInventorySkin.Leather(new Rect(520, 390, 475, 110));
            GUI.Label(new Rect(535, 398, 445, 100), inspected != null
                ? inspected.DisplayName + "\n" + inspected.QualityLabel + " · " + (inspectedIndex >= 40 ? Labels[inspectedIndex - 40] : "Taschenplatz " + (inspectedIndex + 1)) + (inspected.Rules.twoHanded ? " · Zweihand" : "")
                    + "\n" + inspected.Description
                : inspectedIndex >= 40 ? Labels[inspectedIndex - 40] + "\nNicht belegt" : "Taschenplatz " + (inspectedIndex + 1) + "\nLeer", textStyle);
            GUI.Label(new Rect(35, 510, 950, 30), message ?? "Goldener Rahmen: ausgewählter Slot", smallStyle);
            if (ArtButton(new Rect(520, 545, 225, 30), selected >= 40 ? "Ablegen → Tasche (A / X)" : "Anlegen (A / Enter)", Item(selected) != null)) Activate(selected);
            if (ArtButton(new Rect(820, 545, 175, 30), "Schließen (B / Esc)", true)) SetOpen(false);
            var evt = Event.current;
            if (evt.type == EventType.MouseDown && hover >= 0)
            {
                selected = hover;
                if (evt.button == 1) Activate(hover);
                else if (evt.button == 0) { dragSource = hover; dragStart = evt.mousePosition; }
                evt.Use();
            }
            if (evt.type == EventType.MouseUp && evt.button == 0 && dragSource >= 0)
            {
                if (hover >= 0 && hover != dragSource && Vector2.Distance(dragStart, evt.mousePosition) > 5)
                {
                    bool result = dragSource < 40
                        ? hover < 40 ? inventory.TryMove(dragSource, hover) : inventory.TryEquip(dragSource, (EquipmentSlot)(hover - 40))
                        : hover < 40 && inventory.TryUnequip((EquipmentSlot)(dragSource - 40), hover);
                    message = result ? "Gegenstand verschoben." : "Wechsel nicht möglich: Slot ungeeignet oder kein Platz.";
                }
                dragSource = -1; evt.Use();
            }
            if (dragSource >= 0 && Item(dragSource)?.Icon != null && Vector2.Distance(dragStart, evt.mousePosition) > 5)
                DrawSprite(new Rect(evt.mousePosition.x - 20, evt.mousePosition.y - 20, 40, 40), Item(dragSource).Icon, Item(dragSource).IconTint, Item(dragSource).IconContent);
            GUI.matrix = previous;
        }
        private void Styles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 21, fontStyle = FontStyle.Bold };
            titleStyle.normal.textColor = new Color(.94f, .79f, .48f);
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
            textStyle.normal.textColor = new Color(.9f, .94f, .93f);
            smallStyle = new GUIStyle(textStyle) { fontSize = 12 };
            smallStyle.normal.textColor = new Color(.68f, .77f, .78f);
        }
        private bool ArtButton(Rect rect, string label, bool enabled)
        {
            // Draw art independently of IMGUI's disabled/hover button backgrounds.
            var previous = GUI.color;
            bool hover = rect.Contains(Event.current.mousePosition);
            GUI.color = enabled ? hover ? new Color(1, .94f, .75f) : Color.white : new Color(.65f, .65f, .65f);
            ForestInventorySkin.Button(rect);
            GUI.color = previous;
            var style = new GUIStyle(textStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 13, fontStyle = FontStyle.Bold };
            style.normal.textColor = enabled ? new Color(.95f, .85f, .6f) : new Color(.65f, .65f, .58f);
            GUI.Label(rect, label, style);
            var evt = Event.current;
            if (enabled && hover && evt.type == EventType.MouseDown && evt.button == 0)
            { pressedButton = rect; buttonPressed = true; evt.Use(); }
            if (buttonPressed && pressedButton == rect && evt.type == EventType.MouseUp && evt.button == 0)
            { buttonPressed = false; evt.Use(); return enabled && hover; }
            return false;
        }
        private static void Fill(Rect rect, Color color)
        {
            var previous = GUI.color; GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = previous;
        }
        private static void Border(Rect rect, Color color, float width)
        {
            Fill(new Rect(rect.x, rect.y, rect.width, width), color);
            Fill(new Rect(rect.x, rect.yMax - width, rect.width, width), color);
            Fill(new Rect(rect.x, rect.y, width, rect.height), color);
            Fill(new Rect(rect.xMax - width, rect.y, width, rect.height), color);
        }
        private void Preview(Rect rect)
        {
            if (appearance == null) return;
            for (int layer = 0; layer < 6; layer++)
            {
                var sprite = appearance.FrontPreviewLayer(layer, out var tint);
                DrawSprite(rect, sprite, tint);
            }
        }
        private Material GrayscaleIconMaterial()
        {
            if (grayscaleIconMaterial == null)
            {
                var shader = Resources.Load<Shader>("InventoryUI/GrayscaleIcon");
                if (shader != null) grayscaleIconMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }
            return grayscaleIconMaterial;
        }
        private void OnDestroy()
        { if (grayscaleIconMaterial != null) Destroy(grayscaleIconMaterial); }
        private static void DrawSprite(Rect area, Sprite sprite, Color tint, Rect? content = null, Material material = null)
        {
            if (sprite == null) return;
            var region = content ?? new Rect(0, 0, 1, 1);
            var full = sprite.textureRect;
            var uv = new Rect(full.x + region.x * full.width, full.y + region.y * full.height, full.width * region.width, full.height * region.height);
            float ratio = uv.width / uv.height;
            float width = Mathf.Min(area.width, area.height * ratio), height = width / ratio;
            var rect = new Rect(area.center.x - width / 2, area.center.y - height / 2, width, height);
            var old = GUI.color; GUI.color = tint;
            var textureCoordinates = new Rect(uv.x / sprite.texture.width, uv.y / sprite.texture.height, uv.width / sprite.texture.width, uv.height / sprite.texture.height);
            if (material == null) GUI.DrawTextureWithTexCoords(rect, sprite.texture, textureCoordinates);
            else if (Event.current.type == EventType.Repaint)
                Graphics.DrawTexture(rect, sprite.texture, textureCoordinates, 0, 0, 0, 0, tint, material);
            GUI.color = old;
        }
    }
}
