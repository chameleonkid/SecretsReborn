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
        public SpellEffectSnapshot[] effects;
    }
    [Serializable] public sealed class SpellEffectSnapshot
    {
        public int id;
        public string kind;
        public float x, y, angle, age;
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
        private float castCritChance;
        private sealed class Flight
        {
            public Component target;
            public int amount;
            public float range;
            public double expires;
            public WorldSessionState world;
            public string scene;
            public SpellEffectSnapshot visual;
            public Vector2 launch, spreadControl1, spreadControl2, spreadEnd;
            public float spreadDuration;
            public bool critical;
            public float speed;
            public DotApplication[] dots;
        }
        private readonly List<Flight> flights = new List<Flight>();
        private readonly List<SpellEffectSnapshot> effects = new List<SpellEffectSnapshot>();
        private int effectSequence;
        private SpellEffectsView effectView;
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
        private void Awake() { actor=GetComponent<CharacterInventory>(); body=GetComponent<Rigidbody2D>(); catalog=Resources.LoadAll<SpellDefinition>("Magic/Spells"); effectView=gameObject.AddComponent<SpellEffectsView>(); }
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
            runtimeWorld=castWorld;
            castCritChance=actor.EffectiveCritChancePercent;
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
                if (target is TreeMeleeEnemy enemy && enemy.Alive && enemy.HasStateAuthority)
                {
                    bool critical=CombatRules.IsCritical(castCritChance,UnityEngine.Random.value);
                    amount=CombatRules.HitDamage(amount,critical);
                    int count = Mathf.Clamp(rank.visualProjectiles, 1, 8);
                    for (int projectile = 0; projectile < count; projectile++)
                    {
                        int share = SpellCastRules.Share(amount, count, projectile);
                        if (share == 0) continue;
                        var origin = (Vector2)transform.position + Vector2.up * .5f;
                        var destination = (Vector2)enemy.transform.position + Vector2.up * .5f;
                        var forward = (destination-origin).normalized;
                        if (forward.sqrMagnitude<.01f) forward=Vector2.up;
                        var sideways = new Vector2(-forward.y,forward.x);
                        float lane = count>1 ? (projectile/(float)(count-1)*2-1) : 0;
                        float spreadScale = Mathf.Clamp01(Vector2.Distance(origin,destination)/2);
                        var position = origin + sideways * (lane * .12f * spreadScale);
                        float forwardSpread=count>1 ? UnityEngine.Random.Range(.65f,1.05f) : .8f;
                        float sideSpread=count>1 ? UnityEngine.Random.Range(.75f,1.1f) : .9f;
                        var spreadEnd = origin + (forward * forwardSpread + sideways * (lane * sideSpread)) * spreadScale;
                        var approach = (destination-spreadEnd).normalized;
                        var visual = NewEffect("fireball", position);
                        var launchDirection = forward * .15f + sideways * lane;
                        visual.angle = Mathf.Atan2(launchDirection.y,launchDirection.x)*Mathf.Rad2Deg;
                        flights.Add(new Flight { target = target, amount = share, range = rank.range, expires = Time.timeAsDouble + 4,
                            world = castWorld, scene = scene, visual = visual, launch=position,
                            spreadControl1=position+(forward*.15f+sideways*(lane*.75f))*spreadScale,
                            spreadControl2=spreadEnd-approach*(.4f*spreadScale), spreadEnd=spreadEnd,
                            spreadDuration=count>1 ? Mathf.Max(.08f,(.3f+lane*.08f+UnityEngine.Random.Range(-.02f,.02f))*spreadScale) : 0,
                            speed=count>1 ? 8-lane*1.1f+UnityEngine.Random.Range(-.2f,.2f) : 8, critical=critical, dots=rank.onHitDots });
                    }
                }
                else if (target is CharacterInventory ally && GameSession.Instance.ApplyHealing(ally,amount)) NewEffect("heal", ally.transform.position);
            }
            Status="Zauber abgeschlossen."; Cancel();
        }
        public void Cancel() { spell=null; targets.Clear(); Freeze(false); }
        private SpellEffectSnapshot NewEffect(string kind, Vector2 position)
        {
            var effect = new SpellEffectSnapshot { id = ++effectSequence, kind = kind, x = position.x, y = position.y };
            effects.Add(effect); return effect;
        }
        private void ClearEffects() { flights.Clear(); effects.Clear(); effectView?.Clear(); }
        private void UpdateFlights()
        {
            for (int i = flights.Count - 1; i >= 0; i--)
            {
                var flight = flights[i]; var enemy = flight.target as TreeMeleeEnemy;
                if (flight.world != GameSession.Instance.World || flight.scene != gameObject.scene.path || enemy == null || !enemy.Alive
                    || !enemy.HasStateAuthority || !ValidTarget(enemy, flight.range) || Time.timeAsDouble >= flight.expires)
                { effects.Remove(flight.visual); flights.RemoveAt(i); continue; }
                var position = new Vector2(flight.visual.x, flight.visual.y);
                var previousPosition = position;
                var destination = (Vector2)enemy.transform.position + Vector2.up * .5f;
                float previousAge = flight.visual.age;
                flight.visual.age += Time.deltaTime;
                if (previousAge < flight.spreadDuration)
                {
                    // First fan away from the caster on a smooth arc, then home toward the fixed target.
                    float t=Mathf.Clamp01(flight.visual.age/flight.spreadDuration), u=1-t;
                    position=u*u*u*flight.launch+3*u*u*t*flight.spreadControl1+3*u*t*t*flight.spreadControl2+t*t*t*flight.spreadEnd;
                }
                float homingTime=flight.visual.age-Mathf.Max(previousAge,flight.spreadDuration);
                if (homingTime>0) position=Vector2.MoveTowards(position,destination,flight.speed*homingTime);
                var direction = position - previousPosition;
                if (direction.sqrMagnitude<.000001f) direction=destination-position;
                flight.visual.angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                flight.visual.x = position.x; flight.visual.y = position.y;
                if (flight.visual.age<flight.spreadDuration) continue;
                if (Vector2.Distance(position, destination) > .08f) continue;
                if(GameSession.Instance.ApplyEnemyDamage(enemy,flight.amount,Vector2.zero,flight.critical)) GameSession.Instance.ApplyOnHitDots(enemy,flight.dots);
                effects.Remove(flight.visual); flights.RemoveAt(i); NewEffect("hit", destination);
            }
            for (int i = effects.Count - 1; i >= 0; i--)
            {
                var effect = effects[i]; if (effect.kind == "fireball") continue;
                effect.age += Time.deltaTime;
                if (effect.age >= .8f) effects.RemoveAt(i);
            }
        }
        private void Freeze(bool value)
        {
            if (body==null || frozen==value) return;
            frozen=value;
            GetComponent<PlayerMovement>()?.SetRewardImmobilized(value || GameSession.Existing?.IsReceivingReward(actor)==true);
        }
        private void Update()
        {
            if (actor.HasStateAuthority && runtimeWorld!=GameSession.Instance.World) { Cancel(); ClearEffects(); ready.Clear(); runtimeWorld=GameSession.Instance.World; }
            if (actor.HasStateAuthority)
            {
                if (!actor.isActiveAndEnabled || GameSession.Instance.Busy || SaveBook.IsOpen || NetworkCoop.Active?.ChangingArea == true || NetworkCoop.Active?.VotePending == true
                    || GameSession.Instance.World.CharacterVitals(actor.CharacterId).IsDown) ClearEffects();
                else UpdateFlights();
            }
            effectView.Present(IsCasting, ActiveSpell, actor.HasStateAuthority ? effects.ToArray() : replica?.effects);
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
            return new SpellCastSnapshot { spellId=ActiveSpell,casting=IsCasting,remaining=Remaining,sequence=sequence,cooldowns=cooldowns.ToArray(),effects=effects.ToArray() };
        }
        public void ApplyReplica(SpellCastSnapshot state) { replica=state; replicaAt=Time.unscaledTime; }
        private void OnDisable() { Cancel(); ClearEffects(); PreviewTargets(null); if (marker!=null) marker.enabled=false; }
        private void OnDestroy() { if (markerMaterial!=null) Destroy(markerMaterial); }
    }
}
