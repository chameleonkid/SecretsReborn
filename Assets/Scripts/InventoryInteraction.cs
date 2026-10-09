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
        private SharedStashContainer stash;
        public bool IsOpen => open;
        public void SetLocalInput(bool value) { localInput = value; if (!value) SetOpen(false); }
        private string message;
        private int selected;

        private Vector2Int heldDirection;
        private float nextNavigation;
        private bool controllerSelection;
        private const int PotionStart = 40 + InventoryState.EquipmentCapacity;



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

        private static readonly string[] Labels = { "Helm / Hut", "Schultern", "Armor", "Gürtel", "Hände", "Beine", "Stiefel", "Ring 1", "Ring 2", "Amulett", "Seal", "Cloak", "Haupthand", "Nebenhand", "Lampe" };


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
            if (SpellRingMenu.BlocksInput(inventory)) { GameSession.Instance.CancelRevive(inventory); return; }
            if (!localInput) return;
            if (GetComponent<SpellCaster>()?.IsCasting==true) return;
            if (stash != null && (!stash.CanUse(inventory) || !stash.isActiveAndEnabled)) SetOpen(false);
            if (!Application.isFocused || SaveBook.IsOpen || GameSession.Instance.Busy || GameSession.Instance.RewardPresentationActive)
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
                if (key?.digit1Key.wasPressedThisFrame == true || pad?.leftShoulder.wasPressedThisFrame == true) inventory.TryUsePotion(0);
                if (key?.digit2Key.wasPressedThisFrame == true || pad?.rightShoulder.wasPressedThisFrame == true) inventory.TryUsePotion(1);
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
                    SharedStashContainer nearestStash = null; float stashDistance = float.PositiveInfinity;
                    foreach (var candidate in FindObjectsByType<SharedStashContainer>(FindObjectsSortMode.None))
                        if (candidate.CanUse(inventory) && (candidate.transform.position-transform.position).sqrMagnitude < stashDistance)
                        { nearestStash = candidate; stashDistance = (candidate.transform.position-transform.position).sqrMagnitude; }
                    if (nearestStash != null && (item == null || stashDistance < (item.transform.position-transform.position).sqrMagnitude)
                        && (chest == null || stashDistance < (chest.transform.position-transform.position).sqrMagnitude))
                    { stash = nearestStash; selected = 0; SetOpen(true); }
                    else if (item != null) message = item.TryCollect(inventory) ? "Aufgehoben: " + item.Label : "Aufheben nicht möglich (kein Platz oder zu weit entfernt).";
                    else
                    {
                        if (chest != null) message = chest.TryOpen(inventory) ? "Truhe geöffnet. Beute erhalten." : "Truhe bleibt geschlossen: Platz im Inventar prüfen.";
                    }
                }
                return;
            }
            GameSession.Instance.CancelRevive(inventory);
            if ((key != null && key.tabKey.wasPressedThisFrame) || (pad != null && (pad.rightShoulder.wasPressedThisFrame || pad.leftShoulder.wasPressedThisFrame)))
            { selected = stash != null ? selected < 40 ? 40 : 0 : selected < 40 ? 40 : selected < PotionStart ? PotionStart : 0; controllerSelection = true; }
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
                selected = stash != null ? (selected / 40) * 40 + Mathf.Clamp((selected % 40) / 10 + dy,0,3) * 10 + Mathf.Clamp(selected % 10 + dx,0,9)
                    : Navigate(selected, dx, dy); controllerSelection = true;
            }
            if (key != null && key.enterKey.wasPressedThisFrame || pad != null && pad.buttonSouth.wasPressedThisFrame) Activate(selected);
            if (pad != null && pad.buttonEast.wasPressedThisFrame) SetOpen(false);
            if (key?.qKey.wasPressedThisFrame == true || pad?.buttonWest.wasPressedThisFrame == true)
            {
                if (stash != null) Activate(selected);
                else if (selected >= PotionStart) inventory.TryBindPotion(selected-PotionStart,-1);
                else if (selected < 40 && Item(selected)?.Rules.potionKind > 0) inventory.TryBindPotion(Item(selected).Rules.potionKind-1,selected);
                else if (selected >= 40) Activate(selected);
            }
        }
        private static int Navigate(int index, int dx, int dy)
            => InventoryNavigation.Navigate(index, dx, dy);
        private void Activate(int slot)
        {
            if (stash != null) { TransferStash(slot); return; }
            bool result = false;
            if (slot >= PotionStart) result = inventory.TryUsePotion(slot-PotionStart);
            else if (slot >= 40) result = inventory.TryUnequip((EquipmentSlot)(slot - 40));
            else { var target = inventory.PreferredSlot(slot); result = target.HasValue ? inventory.TryEquip(slot, target.Value) : inventory.TryUseItem(slot); }
            message = result ? "Gegenstand angelegt, abgelegt oder benutzt." : "Nicht möglich: Slot ungeeignet, Tasche voll oder Effekt nicht benötigt.";
        }
        private void SetOpen(bool value)
        {
            if (open == value) return;
            open = value; heldDirection = Vector2Int.zero; nextNavigation = 0;
            if (!value) { if (stash != null) selected = 0; stash = null; }
            if (movement == null) return;
            if (open) { movementWasEnabled = movement.enabled; movement.enabled = false; }
            else movement.enabled = movementWasEnabled;
        }
        private void OnDisable() { SetOpen(false); if (inventory != null) GameSession.Existing?.CancelRevive(inventory); }
        private ItemDefinition Item(int index) => inventory.Find(index < 40 ? inventory.State.GetSlot(index)?.itemId
            : index >= PotionStart ? inventory.State.PotionItem(index-PotionStart) : inventory.State.GetEquipment((EquipmentSlot)(index - 40)));
        private static string SlotLabel(int index) => index >= PotionStart ? index == PotionStart ? "HP-Schnellslot" : "Mana-Schnellslot"
            : index >= 40 ? Labels[index-40] : "Rucksack";
        private InventoryStack StashStack(int index) => (index < 40 ? inventory.State : GameSession.Instance.World.SharedStash).GetSlot(index % 40);
        private void TransferStash(int source, int destination = -1)
        {
            var stack = StashStack(source); var item = inventory.Find(stack?.itemId);
            if (item == null) return;
            bool deposit = source < 40; var target = deposit ? GameSession.Instance.World.SharedStash : inventory.State;
            if (destination < 0)
            {
                for (int i = 0; i < 40; i++)
                {
                    var current = target.GetSlot(i);
                    if (current != null && current.itemId == stack.itemId && (long)current.count + stack.count <= item.MaxStack) { destination = i; break; }
                }
                if (destination < 0) for (int i = 0; i < 40; i++) if (target.GetSlot(i) == null) { destination = i; break; }
            }
            if (destination < 0) { message = "Kein Platz für den ganzen Stapel."; return; }
            bool result = stash.TryTransfer(inventory, deposit, source % 40, destination % 40, stack.itemId, stack.count);
            message = result ? NetworkCoop.IsReplica ? "Transfer angefragt." : "Gegenstand verschoben." : "Transfer nicht möglich. Auswahl und Platz prüfen.";
        }
        private InventoryCanvasView view;
        internal bool LocalInputEnabled => localInput;
        internal bool InStash => stash != null;
        internal int SelectedIndex => selected;
        internal bool ControllerSelection => controllerSelection;
        internal CharacterInventory Inventory => inventory;
        internal CharacterAppearance Appearance => appearance;
        internal string StatusMessage => message;
        internal bool PresentationBlocked => SaveBook.IsOpen || GameSession.Instance.Busy || GameSession.Instance.RewardPresentationActive;
        internal ItemDefinition DisplayedItem(int index) => InStash ? inventory.Find(StashStack(index)?.itemId) : Item(index);
        internal int DisplayedCount(int index) => InStash ? StashStack(index)?.count ?? 0 : index < 40 ? inventory.State.GetSlot(index)?.count ?? 0 : index >= PotionStart ? inventory.State.Count(Item(index)?.ItemId) : 1;
        internal string SlotTitle(int index) => InStash ? index < 40 ? "Rucksack" : "Gemeinsames Lager" : SlotLabel(index);
        internal CharacterInventory ReviveTarget => NearestDowned();
        internal bool ReviveInterrupted => reviveInterrupted;
        private bool ValidUISlot(int index) => index >= 0 && index < (InStash ? 80 : PotionStart+2);
        private bool CanOperateUI => localInput && open && !PresentationBlocked && !GameSession.Instance.World.CharacterVitals(inventory.CharacterId).IsDown;
        public bool OpenStash(SharedStashContainer container)
        {
            if (!localInput || container == null || !container.CanUse(inventory) || PresentationBlocked || GameSession.Instance.World.CharacterVitals(inventory.CharacterId).IsDown) return false;
            stash = container; selected = 0; SetOpen(true); return true;
        }
        public void OpenInventory() { if (localInput && !PresentationBlocked && !GameSession.Instance.World.CharacterVitals(inventory.CharacterId).IsDown) { SetOpen(false); selected = 0; SetOpen(true); } }
        public void CloseMenu() => SetOpen(false);
        internal void UISelect(int index) { if (CanOperateUI && ValidUISlot(index)) { selected = index; controllerSelection = false; } }
        internal void UIActivate(int index) { if (CanOperateUI && ValidUISlot(index)) { selected = index; controllerSelection = false; Activate(index); } }
        internal void UIDrop(int from, int to)
        {
            if (!CanOperateUI || !ValidUISlot(from) || !ValidUISlot(to) || from == to) return;
            if (InStash) { if (from/40 != to/40) TransferStash(from,to); return; }
            bool result = to >= PotionStart && from < 40 ? inventory.TryBindPotion(to-PotionStart,from)
                : from >= PotionStart ? to < 40 && inventory.TryBindPotion(from-PotionStart,-1)
                : from < 40 ? to < 40 ? inventory.TryMove(from,to) : inventory.TryEquip(from,(EquipmentSlot)(to-40))
                : to < 40 && inventory.TryUnequip((EquipmentSlot)(from-40),to);
            message = result ? "Transfer angefragt oder Gegenstand verschoben." : "Wechsel nicht möglich: Slot ungeeignet oder kein Platz.";
        }
        private void LateUpdate()
        {
            if (!localInput) return;
            if (view == null)
            {
                var prefab = Resources.Load<GameObject>("InventoryUI/InventoryCanvas");
                if (prefab == null) return;
                view = Instantiate(prefab).GetComponent<InventoryCanvasView>(); view.Bind(this);
            }
            view.Refresh();
        }
        private void OnDestroy() { if (view != null) Destroy(view.gameObject); }
    }
}
