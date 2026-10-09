using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SecretsReborn
{
    [Serializable] public sealed class SpellCastSnapshot
    {
        public string spellId;
        public bool casting;
        public float remaining;
        public int sequence;
        public SpellCooldownSnapshot[] cooldowns;
    }
    [Serializable] public sealed class SpellCooldownSnapshot { public string id; public float remaining; }
    [RequireComponent(typeof(CharacterInventory))]
    public sealed class SpellCaster : MonoBehaviour
    {
        private CharacterInventory actor;
        private SpellDefinition[] catalog;
        private readonly Dictionary<string,double> ready=new Dictionary<string,double>();
        private SpellRankDefinition rank;
        private SpellDefinition spell;
        private readonly List<Component> targets=new List<Component>();
        private WorldSessionState castWorld;
        private double finish;
        private string scene;
        private Rigidbody2D body;
        private bool frozen;
        private SpellCastSnapshot replica;
        private int sequence;
        public bool IsCasting => actor!=null && (actor.HasStateAuthority ? spell!=null : replica?.casting==true);
        public string ActiveSpell => actor!=null && actor.HasStateAuthority ? spell?.SpellId : replica?.spellId;
        public float Remaining => actor!=null && actor.HasStateAuthority ? (float)Math.Max(0,finish-Time.timeAsDouble) : replica?.remaining ?? 0;
        public string Status { get; private set; }
        private int targetIndex;
        private bool testSelection;
        private WorldSessionState runtimeWorld;
        private SpellTargetKind selectionKind;
        private LineRenderer marker;
        private Material markerMaterial;
        private readonly List<LineRenderer> previewMarkers=new List<LineRenderer>();
        public SpellDefinition[] Catalog => catalog;
        public List<Component> TargetsFor(SpellDefinition definition)
        { var data=definition?.Rank(GameSession.Instance.World.CharacterSpells(actor.CharacterId).Rank(definition.SpellId)); return data==null ? new List<Component>() : Eligible(definition.Targets,data.range); }
        private void Awake() { actor=GetComponent<CharacterInventory>(); body=GetComponent<Rigidbody2D>(); catalog=Resources.LoadAll<SpellDefinition>("Magic/Spells"); }
        public SpellDefinition Find(string id) => Array.Find(catalog,x=>x!=null && x.SpellId==id);
        public float Cooldown(string id)
        {
            if (!actor.HasStateAuthority && replica?.cooldowns!=null)
                foreach (var entry in replica.cooldowns) if (entry.id==id) return Mathf.Max(0,entry.remaining-(Time.unscaledTime-replicaAt));
            return ready.TryGetValue(id,out var at) ? (float)Math.Max(0,at-Time.timeAsDouble) : 0;
        }
        private float replicaAt;
        private bool Allowed => actor.isActiveAndEnabled && !GameSession.Instance.Busy && !SaveBook.IsOpen
            && NetworkCoop.Active?.ChangingArea!=true && NetworkCoop.Active?.VotePending!=true
            && !GameSession.Instance.IsReceivingReward(actor) && !GameSession.Instance.World.CharacterVitals(actor.CharacterId).IsDown
            && actor.GetComponent<InventoryInteraction>()?.IsOpen!=true && NetworkCoop.Active?.MenuOpen(actor)!=true;
        public bool TryCast(string id,string targetId,bool all)
        {
            if (NetworkCoop.Request(actor,CoopAction.CastSpell,from:all ? 1 : 0,target:targetId,expectedItem:id)) return true;
            if (!actor.HasStateAuthority || !Allowed || IsCasting || GameSession.Instance.IsReviving(actor) || actor.GetComponent<PlayerMelee>()?.IsSwinging==true) return false;
            var definition=Find(id); var level=definition?.Rank(GameSession.Instance.World.CharacterSpells(actor.CharacterId).Rank(id));
            if (level==null || level.totalBudget<=0 || level.manaCost<=0 || !SpellCastRules.ValidNumber(level.castTime) || !SpellCastRules.ValidNumber(level.cooldown) || !SpellCastRules.ValidNumber(level.range) || Cooldown(id)>0) return false;
            var valid=Eligible(definition.Targets,level.range); targets.Clear();
            foreach (var candidate in valid) if (all || Id(candidate)==targetId) targets.Add(candidate);
            if (targets.Count==0 || !GameSession.Instance.TrySpendMana(actor,level.manaCost)) { targets.Clear(); Status="Kein gültiges Ziel oder nicht genug Mana."; return false; }
            spell=definition; rank=level; castWorld=GameSession.Instance.World; scene=gameObject.scene.path;
            finish=Time.timeAsDouble+level.castTime; ready[id]=finish+level.cooldown; sequence++;
            Freeze(true); GameSession.Instance.CancelRevive(actor); Status="Wirkt: "+definition.DisplayName; return true;
        }
        private List<Component> Eligible(SpellTargetKind kind,float range)
        {
            var list=new List<Component>();
            if (kind==SpellTargetKind.Enemy)
            { foreach (var enemy in FindObjectsByType<TreeMeleeEnemy>(FindObjectsSortMode.None)) if (enemy.Alive && ValidTarget(enemy,range)) list.Add(enemy); }
            else
            { foreach (var ally in FindObjectsByType<CharacterInventory>(FindObjectsSortMode.None)) if (!GameSession.Instance.World.CharacterVitals(ally.CharacterId).IsDown && ValidTarget(ally,range)) list.Add(ally); }
            list.Sort((a,b)=>string.CompareOrdinal(Id(a),Id(b))); return list;
        }
        private bool ValidTarget(Component target,float range) => target!=null && target.gameObject.activeInHierarchy && target.gameObject.scene==gameObject.scene
            && Vector2.Distance(transform.position,target.transform.position)<=range;
        public static string Id(Component target) => target is TreeMeleeEnemy enemy ? "enemy:"+enemy.EnemyId : target is CharacterInventory ally ? "ally:"+ally.CharacterId : null;
        private void Resolve()
        {
            for (int i=0;i<targets.Count;i++)
            {
                var target=targets[i]; int amount=SpellCastRules.Share(rank.totalBudget,targets.Count,i);
                // Lost targets consume their fixed share; no retargeting or redistribution.
                if (amount==0 || !ValidTarget(target,rank.range)) continue;
                if (target is TreeMeleeEnemy enemy && enemy.Alive && enemy.HasStateAuthority) enemy.ReceiveHostHit(amount,Vector2.zero);
                else if (target is CharacterInventory ally) GameSession.Instance.ApplyHealing(ally,amount);
            }
            Status="Zauber abgeschlossen."; Cancel();
        }
        public void Cancel() { spell=null; targets.Clear(); Freeze(false); }
        private void Freeze(bool value)
        {
            if (body==null || frozen==value) return;
            frozen=value;
            GetComponent<PlayerMovement>()?.SetRewardImmobilized(value || GameSession.Existing?.IsReceivingReward(actor)==true);
        }
        private void Update()
        {
            if (actor.HasStateAuthority && runtimeWorld!=GameSession.Instance.World) { Cancel(); ready.Clear(); runtimeWorld=GameSession.Instance.World; }
            if (actor.HasStateAuthority && spell!=null)
            {
                if (!Allowed || castWorld!=GameSession.Instance.World || scene!=gameObject.scene.path) { Status="Wirken abgebrochen."; Cancel(); }
                else if (Time.timeAsDouble>=finish) Resolve();
            }
            if (!actor.LocalInput || !Application.isFocused || !Allowed || SpellRingMenu.Selecting(actor)) { if (marker!=null) marker.enabled=false; return; }
            var key=Keyboard.current; if (key==null) return;
            if (key.escapeKey.wasPressedThisFrame && IsCasting) { if (!NetworkCoop.Request(actor,CoopAction.CancelSpell)) Cancel(); }
            // Temporary development-only test access, replaced by the ring UI.
            if (Debug.isDebugBuild && actor.HasStateAuthority && key.f9Key.wasPressedThisFrame)
                foreach (var player in FindObjectsByType<CharacterInventory>(FindObjectsSortMode.None)) if (player.HasStateAuthority)
                    foreach (var definition in catalog) GameSession.Instance.World.CharacterSpells(player.CharacterId).Learn(definition.SpellId,definition.RankCount);
            if (key.f10Key.wasPressedThisFrame) { targetIndex++; testSelection=true; }
            if (key.f7Key.wasPressedThisFrame) { testSelection=true; selectionKind=SpellTargetKind.Enemy; TestCast("fireball",key.leftShiftKey.isPressed); }
            if (key.f8Key.wasPressedThisFrame) { testSelection=true; selectionKind=SpellTargetKind.LivingAlly; TestCast("heal",key.leftShiftKey.isPressed); }
            if (testSelection) ShowTarget();
        }
        private void TestCast(string id,bool all)
        {
            var definition=Find(id); if (definition==null) return;
            var choices=Eligible(definition.Targets,8);
            if (choices.Count>0) TryCast(id,Id(choices[targetIndex%choices.Count]),all);
        }
        private void ShowTarget()
        {
            var choices=Eligible(selectionKind,8); if (choices.Count==0) { if (marker!=null) marker.enabled=false; return; }
            if (marker==null)
            {
                var obj=new GameObject("Spell test target"); obj.transform.SetParent(transform,false); marker=obj.AddComponent<LineRenderer>();
                markerMaterial=new Material(Shader.Find("Sprites/Default")); marker.sharedMaterial=markerMaterial;
                marker.loop=true; marker.positionCount=32; marker.startWidth=marker.endWidth=.04f; marker.sortingOrder=1000;
            }
            marker.enabled=true; marker.startColor=marker.endColor=selectionKind==SpellTargetKind.Enemy ? Color.red : Color.cyan;
            var point=choices[targetIndex%choices.Count].transform.position;
            for (int i=0;i<32;i++) marker.SetPosition(i,point+new Vector3(Mathf.Cos(i*Mathf.PI/16)*.55f,Mathf.Sin(i*Mathf.PI/16)*.55f,0));
        }
        public void PreviewTargets(IReadOnlyList<Component> choices)
        {
            int count=choices?.Count ?? 0;
            while (previewMarkers.Count<count)
            {
                if (markerMaterial==null) markerMaterial=new Material(Shader.Find("Sprites/Default"));
                var obj=new GameObject("Spell target preview"); obj.transform.SetParent(transform,false);
                var line=obj.AddComponent<LineRenderer>(); line.sharedMaterial=markerMaterial; line.loop=true;
                line.positionCount=32; line.startWidth=line.endWidth=.045f; line.sortingOrder=1000; previewMarkers.Add(line);
            }
            for (int n=0;n<previewMarkers.Count;n++)
            {
                var line=previewMarkers[n]; line.enabled=n<count && choices[n]!=null; if (!line.enabled) continue;
                line.startColor=line.endColor=choices[n] is CharacterInventory ? Color.cyan : new Color(1,.45f,.1f);
                var point=choices[n].transform.position;
                for (int i=0;i<32;i++) line.SetPosition(i,point+new Vector3(Mathf.Cos(i*Mathf.PI/16)*.55f,Mathf.Sin(i*Mathf.PI/16)*.55f,0));
            }
        }
        public SpellCastSnapshot Capture()
        {
            var cooldowns=new List<SpellCooldownSnapshot>();
            foreach (var entry in ready) if (entry.Value>Time.timeAsDouble) cooldowns.Add(new SpellCooldownSnapshot { id=entry.Key,remaining=(float)(entry.Value-Time.timeAsDouble) });
            return new SpellCastSnapshot { spellId=ActiveSpell,casting=IsCasting,remaining=Remaining,sequence=sequence,cooldowns=cooldowns.ToArray() };
        }
        public void ApplyReplica(SpellCastSnapshot state) { replica=state; replicaAt=Time.unscaledTime; }
        private void OnDisable() { Cancel(); PreviewTargets(null); if (marker!=null) marker.enabled=false; }
        private void OnDestroy() { if (markerMaterial!=null) Destroy(markerMaterial); }
    }
}
