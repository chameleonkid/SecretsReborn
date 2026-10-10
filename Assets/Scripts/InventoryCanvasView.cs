using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace SecretsReborn
{
    public sealed class InventoryCanvasView : MonoBehaviour
    {
        [SerializeField] private GameObject inventoryPanel, stashPanel, hud;
        [SerializeField] private InventoryCanvasSlot[] inventorySlots, stashSlots;
        [SerializeField] private Text inventoryDetails, stashDetails, inventoryStatus, stashStatus, gold, actionLabel, stashActionLabel, reviveText, hpCount, manaCount;
        [SerializeField] private RawImage[] portrait;
        [SerializeField] private RawImage hpIcon, manaIcon, dragIcon;
        [SerializeField] private Text cooldown;
        [SerializeField] private Text characterStats;
        [SerializeField] private Image reviveProgress;
        private InventoryInteraction owner;
        internal bool InventoryVisible => inventoryPanel.activeSelf;
        internal bool StashVisible => stashPanel.activeSelf;
        private int hover = -1, drag = -1;
        private string draggedItem;
        private int draggedCount;
        private Vector2 dragSize, portraitSize, hpSize, manaSize;
        private void Awake()
        {
            if (EventSystem.current == null)
            {
                var system = new GameObject("Inventory EventSystem",typeof(EventSystem)); system.transform.SetParent(transform,false);
                var module = system.AddComponent<InputSystemUIInputModule>(); module.AssignDefaultActions(); module.move = module.submit = module.cancel = null;
            }
            dragSize = dragIcon.rectTransform.sizeDelta; portraitSize = portrait[0].rectTransform.sizeDelta;
            hpSize = hpIcon.rectTransform.sizeDelta; manaSize = manaIcon.rectTransform.sizeDelta;
        }
        public void Bind(InventoryInteraction interaction)
        {
            owner = interaction;
            foreach (var slot in inventorySlots) { slot.Bind(this); if(slot.Index>=55) slot.gameObject.SetActive(false); } foreach (var slot in stashSlots) slot.Bind(this);
            foreach(var label in inventoryPanel.GetComponentsInChildren<Text>(true))
            {
                if(label.name=="HealthShortcutLabel" || label.name=="ManaShortcutLabel") label.gameObject.SetActive(false);
                if(label.text.Contains("Trank zuweisen")) label.text="Stick / Pfeile: wählen · LB/RB/Tab: Bereich · A: benutzen\nX / Q: Equipment ablegen · B / Esc: schließen";
            }
        }
        public void Refresh()
        {
            if (owner == null) return;
            bool visible = owner.LocalInputEnabled && !owner.PresentationBlocked;
            inventoryPanel.SetActive(visible && owner.IsOpen && !owner.InStash);
            stashPanel.SetActive(visible && owner.IsOpen && owner.InStash); hud.SetActive(visible && !owner.IsOpen);
            if (!visible || !owner.IsOpen) { EndDrag(); hover = -1; }
            var actor = owner.Inventory;
            if (owner.IsOpen && visible)
            {
                var slots = owner.InStash ? stashSlots : inventorySlots;
                foreach (var slot in slots) slot.Refresh(owner.DisplayedItem(slot.Index),owner.DisplayedCount(slot.Index),slot.Index == owner.SelectedIndex);
                int inspected = !owner.ControllerSelection && hover >= 0 && hover < slots.Length ? hover : owner.SelectedIndex;
                var item = owner.DisplayedItem(inspected);
                string details = item == null ? "Nicht belegt" : item.DisplayName + " · " + item.QualityLabel + (item.Rules.twoHanded ? " · Zweihand" : "") + "\n" + item.Description;
                if (owner.InStash) { stashDetails.text = details; stashStatus.text = owner.StatusMessage ?? "Gemeinsamer Weltspeicher zum Tauschen."; stashActionLabel.text = owner.SelectedIndex < 40 ? "Einlagern (A / Enter)" : "Auslagern (A / Enter)"; }
                else
                {
                    inventoryDetails.text = details; inventoryStatus.text = owner.StatusMessage ?? "Goldener Rahmen: ausgewählter Slot"; gold.text = "GOLD  " + actor.State.Gold;
                    var stats=actor.Stats; var vitals=GameSession.Instance.World.CharacterVitals(actor.CharacterId);
                    if (characterStats!=null) characterStats.text="RÜSTUNG  "+stats.Armor+" · Reduktion "+(100d*stats.Armor/(100d+stats.Armor)).ToString("0.#")+"%\nHERZEN  "+(vitals.MaxHealth/2)+" (Basis "+(vitals.BaseMaxHealth/2)+") · MANA  "+vitals.MaxMana+" (Basis "+vitals.BaseMaxMana+")\nSCHADEN  "+stats.WeaponDamage+" · ANGRIFF  "+stats.AttackCooldown.ToString("0.##")+" s";
                    actionLabel.text = owner.SelectedIndex >= 55 || owner.DisplayedItem(owner.SelectedIndex)?.Rules.potionKind > 0 ? "Benutzen (A / Enter)" : owner.SelectedIndex >= 40 ? "Ablegen (A / Enter)" : "Anlegen (A / Enter)";
                    for (int i = 0; i < portrait.Length; i++)
                    {
                        var sprite = owner.Appearance.FrontPreviewLayer(i,out var tint); portrait[i].gameObject.SetActive(sprite != null);
                        if (sprite != null) Sprite(portrait[i],sprite,tint,new Rect(0,0,1,1),portraitSize);
                        portrait[i].material = owner.Appearance.FrontPreviewMaterial(i);
                    }
                }
            }
            if (hud.activeSelf)
            {
                hpIcon.gameObject.SetActive(false); manaIcon.gameObject.SetActive(false); hpCount.gameObject.SetActive(false); manaCount.gameObject.SetActive(false); cooldown.gameObject.SetActive(false);
                var target = owner.ReviveTarget; reviveText.gameObject.SetActive(target != null); reviveProgress.transform.parent.gameObject.SetActive(target != null);
                if (target != null) { reviveText.text = owner.ReviveInterrupted ? "Unterbrochen – Taste erneut halten" : "E / A halten: Wiederbeleben"; reviveProgress.fillAmount = GameSession.Instance.ReviveProgress(actor); }
            }
        }
        private static void Potion(RawImage image,Text label,CharacterInventory actor,int slot,string key,Vector2 bounds)
        { var item = actor.Find(actor.State.PotionItem(slot)); image.gameObject.SetActive(item?.Icon != null); if (item?.Icon != null) Sprite(image,item.Icon,item.IconTint,item.IconContent,bounds); label.text = key + "  " + actor.State.Count(item?.ItemId); }
        internal static void Sprite(RawImage image,Sprite sprite,Color tint,Rect content,Vector2? bounds = null)
        {
            var rect = sprite.textureRect; var uv = new Rect(rect.x+rect.width*content.x,rect.y+rect.height*content.y,rect.width*content.width,rect.height*content.height);
            image.texture = sprite.texture; image.uvRect = new Rect(uv.x/sprite.texture.width,uv.y/sprite.texture.height,uv.width/sprite.texture.width,uv.height/sprite.texture.height); image.color = tint;
            Vector2 area = bounds ?? new Vector2(27,27); float scale = Mathf.Min(area.x/uv.width,area.y/uv.height); image.rectTransform.sizeDelta = new Vector2(uv.width*scale,uv.height*scale);
        }
        internal void Hover(int index) => hover = index;
        internal void Select(int index) { hover = index; owner.UISelect(index); }
        internal void Activate(int index) { hover = index; owner.UIActivate(index); }
        public void ActivateSelected() => owner?.UIActivate(owner.SelectedIndex);
        public void Close() { EndDrag(); hover = -1; owner?.CloseMenu(); }
        internal void BeginDrag(InventoryCanvasSlot source,Vector2 pointer)
        { var item = owner.DisplayedItem(source.Index); if (item?.Icon == null) return; drag = source.Index; draggedItem = item.ItemId; draggedCount = owner.DisplayedCount(drag); dragIcon.gameObject.SetActive(true); Sprite(dragIcon,item.Icon,item.IconTint,item.IconContent,dragSize); Drag(pointer); }
        internal void Drag(Vector2 pointer) { if (drag >= 0) dragIcon.rectTransform.position = pointer; }
        internal void Drop(int destination) { if (drag >= 0 && owner.DisplayedItem(drag)?.ItemId == draggedItem && owner.DisplayedCount(drag) == draggedCount) owner.UIDrop(drag,destination); EndDrag(); }
        internal void EndDrag() { drag = -1; if (dragIcon != null) dragIcon.gameObject.SetActive(false); }
    }
}
