using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SecretsReborn
{
    // Local presentation only. The host still resolves every cast from its own state.
    [DefaultExecutionOrder(-100)]
    public sealed class SpellRingMenu : MonoBehaviour
    {
        private CharacterInventory actor;
        private SpellCaster caster;
        private SpellRingView view;
        private string openedWorldId;
        private readonly List<string> labels=new List<string>();
        private readonly List<Sprite> icons=new List<Sprite>();
        private readonly List<SpellDefinition> spells=new List<SpellDefinition>();
        private readonly List<SpellElement> elements=new List<SpellElement>();
        private List<Component> targets=new List<Component>();
        private SpellDefinition chosen;
        private SpellElement element;
        private string targetId;
        private bool all,book;
        private int stage,selected;
        private float nextMove;
        private int held;
        private string feedback;
        private int closedFrame=-1;
        public bool IsOpen { get; private set; }
        public static bool Selecting(CharacterInventory actor) => actor!=null && (actor.GetComponent<SpellRingMenu>()?.IsOpen==true || NetworkCoop.Active?.SpellSelectionOpen(actor)==true);
        public static bool BlocksInput(CharacterInventory actor) => Selecting(actor) || actor?.GetComponent<SpellRingMenu>()?.closedFrame==Time.frameCount;
        private void Awake() { actor=GetComponent<CharacterInventory>(); caster=GetComponent<SpellCaster>(); }
        private bool Available => actor.LocalInput && !GameSession.Instance.Busy && !SaveBook.IsOpen
            && NetworkCoop.Active?.ChangingArea!=true && NetworkCoop.Active?.VotePending!=true && NetworkCoop.Active?.LobbyActive!=true
            && !GameSession.Instance.IsReceivingReward(actor) && !GameSession.Instance.World.CharacterVitals(actor.CharacterId).IsDown
            && actor.GetComponent<InventoryInteraction>()?.IsOpen!=true && !caster.IsCasting
            && actor.GetComponent<PlayerMelee>()?.IsSwinging!=true;
        public bool Open()
        {
            if (!Available) return false;
            if (view==null)
            {
                var prefab=Resources.Load<GameObject>("Magic/UI/SpellIconRingCanvas");
                if (prefab==null) { Debug.LogWarning("SpellRingCanvas fehlt. SecretsReborn/UI/Prepare spell ring canvas ausführen."); return false; }
                view=Instantiate(prefab).GetComponent<SpellRingView>(); view.Bind(this);
            }
            IsOpen=true; openedWorldId=GameSession.Instance.World.WorldId; stage=0; selected=0; book=false; feedback=null;
            GameSession.Instance.CancelRevive(actor); NetworkCoop.Request(actor,CoopAction.SpellSelection,from:1);
            view.gameObject.SetActive(true); Refresh(); return true;
        }
        public void Close()
        {
            if (IsOpen) { closedFrame=Time.frameCount; NetworkCoop.Request(actor,CoopAction.SpellSelection,from:0); }
            IsOpen=false; caster?.PreviewTargets(null); if (view!=null) view.gameObject.SetActive(false);
        }
        public void Back()
        {
            feedback=null;
            if (book) { book=false; stage=0; }
            else if (stage==0) { Close(); return; }
            else stage--;
            selected=0; Refresh();
        }
        public void ToggleBook() { book=!book; stage=0; selected=0; Refresh(); }
        public void Page(int direction)
        { if (labels.Count==0) return; selected=book ? SpellRingLayout.Page(selected,labels.Count,direction) : (selected+direction+labels.Count)%labels.Count; Refresh(); }
        public void Choose(int index) { selected=index; Confirm(); }
        public void Confirm()
        {
            RefreshChoices(); if (labels.Count==0) return;
            if (book) { chosen=spells[selected]; element=chosen.Element; book=false; stage=2; }
            else if (stage==0) { element=elements[selected]; stage=1; }
            else if (stage==1) { chosen=spells[selected]; stage=2; }
            else if (stage==2)
            {
                all=selected==targets.Count; targetId=all ? null : SpellCaster.Id(targets[selected]); stage=3;
            }
            else
            {
                var rank=chosen.Rank(GameSession.Instance.World.CharacterSpells(actor.CharacterId).Rank(chosen.SpellId));
                var valid=caster.TargetsFor(chosen);
                if (rank==null || caster.Cooldown(chosen.SpellId)>0 || GameSession.Instance.World.CharacterVitals(actor.CharacterId).Mana<rank.manaCost)
                { feedback="Nicht bereit: Mana oder Cooldown prüfen."; Refresh(); return; }
                if (valid.Count==0 || !all && !valid.Exists(x=>SpellCaster.Id(x)==targetId))
                { feedback="Das gewählte Ziel ist nicht mehr verfügbar."; Refresh(); return; }
                var id=chosen.SpellId; var target=targetId; bool every=all;
                Close(); caster.TryCast(id,target,every); return;
            }
            selected=0; Refresh();
        }
        private void RefreshChoices()
        {
            labels.Clear(); icons.Clear(); spells.Clear(); elements.Clear();
            var learned=GameSession.Instance.World.CharacterSpells(actor.CharacterId);
            var catalog=caster.Catalog.Where(x=>x!=null && x.Rank(learned.Rank(x.SpellId))!=null).OrderBy(x=>x.Element).ThenBy(x=>x.SpellId,StringComparer.Ordinal).ToArray();
            if (book || stage==1)
            {
                foreach (var definition in catalog) if (book || definition.Element==element)
                { spells.Add(definition); labels.Add(definition.DisplayName+"\nRang "+learned.Rank(definition.SpellId)); icons.Add(definition.Icon); }
            }
            else if (stage==0)
            {
                foreach (var group in catalog.Select(x=>x.Element).Distinct()) { elements.Add(group); labels.Add(ElementName(group)); icons.Add(Resources.Load<Sprite>("Magic/UI/Icons/"+group.ToString().ToLowerInvariant())); }
            }
            else if (stage==2)
            {
                targets=caster.TargetsFor(chosen);
                foreach (var target in targets) labels.Add(TargetName(target));
                if (targets.Count>0) labels.Add("Alle\n"+targets.Count+" Ziele");
            }
            else labels.Add("Wirken\nBestätigen");
            selected=Mathf.Clamp(selected,0,Mathf.Max(0,labels.Count-1));
        }
        private string TargetName(Component target)
        {
            if (target is CharacterInventory ally)
            { foreach (var slot in GameSession.Instance.World.CharacterSlots) if (slot.id==ally.CharacterId) return slot.name; return "Verbündeter"; }
            return target.gameObject.name;
        }
        private void Refresh()
        {
            if (!IsOpen || view==null) return;
            RefreshChoices();
            string title=book ? "ZAUBERBUCH" : stage==0 ? "ELEMENT" : stage==1 ? ElementName(element) : stage==2 ? "ZIEL WÄHLEN" : "ZAUBER BESTÄTIGEN";
            if(!book && stage>=2 && chosen!=null) title=chosen.DisplayName+" · "+title;
            var preview=new List<Component>(); string details=labels.Count==0 ? stage==2 ? "Kein gültiges Ziel in Reichweite." : "Noch keine Zauber erlernt." : "";
            SpellDefinition shown=book || stage==1 ? spells.Count>0 ? spells[selected] : null : stage>=2 ? chosen : null;
            if (shown!=null)
            {
                int level=GameSession.Instance.World.CharacterSpells(actor.CharacterId).Rank(shown.SpellId); var rank=shown.Rank(level);
                if(rank==null) { feedback="Dieser Rang ist nicht verfügbar."; return; }
                details="Rang "+level+" · Mana "+rank.manaCost+" · Wirkzeit "+rank.castTime.ToString("0.0")+" s · Cooldown "+rank.cooldown.ToString("0.0")+" s · Reichweite "+rank.range;
                details+="\nGesamtwirkung: "+(shown.Targets==SpellTargetKind.LivingAlly ? (rank.totalBudget*.5f).ToString("0.#")+" Herzen" : rank.totalBudget+" Schaden")+" · Cooldown verbleibend "+caster.Cooldown(shown.SpellId).ToString("0.0")+" s";
                if (stage>=2 && !book)
                {
                    targets=caster.TargetsFor(chosen);
                    bool every=stage==2 ? selected==targets.Count : all;
                    foreach (var candidate in targets) if (every || SpellCaster.Id(candidate)==(stage==2 && selected<targets.Count ? SpellCaster.Id(targets[selected]) : targetId)) preview.Add(candidate);
                    if (preview.Count==0) details+="\nKein gültiges Ziel in Reichweite.";
                    else for (int i=0;i<preview.Count;i++)
                    {
                        int share=SpellCastRules.Share(rank.totalBudget,preview.Count,i);
                        details+=(i==0 ? "\n" : " · ")+TargetName(preview[i])+": "+(chosen.Targets==SpellTargetKind.LivingAlly ? (share*.5f).ToString("0.#")+" Herzen" : share+" Schaden");
                    }
                }
            }
            if (!string.IsNullOrEmpty(feedback)) details=feedback+"\n"+details;
            string name=labels.Count>0 ? labels[selected].Replace("\n"," · ") : "Keine Auswahl";
            if (stage>=2 && !book) name=stage==2 ? name : all ? "Alle Ziele" : targets.Find(x=>SpellCaster.Id(x)==targetId) is Component target ? TargetName(target) : "Ziel nicht verfügbar";
            var vitals=GameSession.Instance.World.CharacterVitals(actor.CharacterId);
            caster.PreviewTargets(preview); view.RenderIcons(title+" · "+name+"                        MP "+vitals.Mana+" / "+vitals.MaxMana,labels,icons,selected,details,actor.transform,book,!book && stage>=2);
        }
        public static string ElementName(SpellElement value)
        { switch(value) { case SpellElement.Fire:return "Feuer"; case SpellElement.Ice:return "Eis"; case SpellElement.Light:return "Licht"; case SpellElement.Shadow:return "Schatten"; case SpellElement.Water:return "Wasser"; default:return "Blitz"; } }
        private void Update()
        {
            if (IsOpen && (!Available || openedWorldId!=GameSession.Instance.World.WorldId)) { Close(); return; }
            if (!actor.LocalInput || !Application.isFocused) return;
            var key=Keyboard.current; var pad=Gamepad.current;
            if (key?.mKey.wasPressedThisFrame==true || pad?.selectButton.wasPressedThisFrame==true)
            { if (IsOpen) Close(); else Open(); return; }
            if (!IsOpen) return;
            if (!book && stage==2 && (key?.tKey.wasPressedThisFrame==true || pad?.buttonWest.wasPressedThisFrame==true))
            { selected=selected==targets.Count ? 0 : targets.Count; Refresh(); return; }
            if (!book && stage==2 && Mouse.current?.leftButton.wasPressedThisFrame==true && Camera.main!=null
                && UnityEngine.EventSystems.EventSystem.current?.IsPointerOverGameObject()!=true)
            {
                var point=Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                int nearest=-1; float distance=1.5f;
                for(int i=0;i<targets.Count;i++) { float d=Vector2.Distance(point,targets[i].transform.position); if(d<distance) { nearest=i; distance=d; } }
                if (nearest>=0) { selected=nearest; Refresh(); return; }
            }
            if (key?.escapeKey.wasPressedThisFrame==true || pad?.buttonEast.wasPressedThisFrame==true) { Back(); return; }
            if (key?.tabKey.wasPressedThisFrame==true || pad?.buttonNorth.wasPressedThisFrame==true) { ToggleBook(); return; }
            if (pad?.leftShoulder.wasPressedThisFrame==true) Page(-1);
            if (pad?.rightShoulder.wasPressedThisFrame==true) Page(1);
            int direction=0;
            if (key?.rightArrowKey.wasPressedThisFrame==true || key?.downArrowKey.wasPressedThisFrame==true) direction=1;
            if (key?.leftArrowKey.wasPressedThisFrame==true || key?.upArrowKey.wasPressedThisFrame==true) direction=-1;
            if (pad!=null)
            {
                var axis=pad.dpad.ReadValue(); if (axis.sqrMagnitude<.1f) axis=pad.leftStick.ReadValue();
                int move=axis.magnitude<.55f ? 0 : Mathf.Abs(axis.x)>Mathf.Abs(axis.y) ? axis.x>0 ? 1 : -1 : axis.y<0 ? 1 : -1;
                if (move!=0 && (move!=held || Time.unscaledTime>=nextMove)) { direction=move; nextMove=Time.unscaledTime+(move!=held ? .35f : .14f); }
                held=move;
            }
            if (direction!=0 && labels.Count>0) selected=(selected+direction+labels.Count)%labels.Count;
            if (key?.enterKey.wasPressedThisFrame==true || pad?.buttonSouth.wasPressedThisFrame==true) { Confirm(); return; }
            Refresh();
        }
        private void OnDisable() => Close();
        private void OnDestroy() { Close(); if (view!=null) Destroy(view.gameObject); }
    }
}
