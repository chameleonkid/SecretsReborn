using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace SecretsReborn
{
    // Explicit development-build opt-in. Uses isolated test saves and credentials.
    public sealed class EconomyIntegrationDriver : MonoBehaviour
    {
        private bool host, finished;
        private string report, folder;
        private double deadline;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var args = Environment.GetCommandLineArgs();
            if (!Debug.isDebugBuild || Array.IndexOf(args, "--economy-role") < 0) return;
            var obj = new GameObject("Economy integration check"); DontDestroyOnLoad(obj); obj.AddComponent<EconomyIntegrationDriver>();
        }
        private void Awake()
        {
            var args = Environment.GetCommandLineArgs(); int role = Array.IndexOf(args, "--economy-role"), file = Array.IndexOf(args, "--economy-report");
            host = args[role + 1] == "host"; report = args[file + 1]; folder = Path.GetDirectoryName(report); deadline = Time.realtimeSinceStartupAsDouble + 100;
        }
        private void Update() { if (!finished && Time.realtimeSinceStartupAsDouble > deadline) Finish("FAIL: economy integration timeout: " + NetworkCoop.Active?.Status); }
        private void Finish(string result) { if (finished) return; finished = true; File.WriteAllText(report, result); Debug.Log(result); Application.Quit(result.StartsWith("PASS") ? 0 : 1); }
        private bool CheckWeaponDotDetails(CharacterInventory actor)
        {
            var item=actor.Find("training-sword"); var original=item.Weapon;
            var profile=ScriptableObject.CreateInstance<WeaponDefinition>();
            var interaction=actor.GetComponent<InventoryInteraction>();
            try
            {
                JsonUtility.FromJsonOverwrite("{\"onHitDots\":[{\"kind\":0,\"chancePercent\":100,\"duration\":3,\"tickInterval\":1,\"damagePerTick\":3},{\"kind\":1,\"chancePercent\":25,\"duration\":4,\"tickInterval\":0.5,\"damagePerTick\":2},{\"kind\":2,\"chancePercent\":50,\"duration\":5,\"tickInterval\":1,\"damagePerTick\":4}]}",profile);
                item.SetWeapon(profile); interaction.OpenInventory();
                int slot=-1;
                for(int i=0;i<40;i++) if(interaction.DisplayedItem(i)==item) slot=i;
                if(slot<0) { Finish("FAIL: weapon UI fixture missing"); return false; }
                interaction.UISelect(slot); var canvas=FindFirstObjectByType<InventoryCanvasView>(); canvas.Refresh(); Canvas.ForceUpdateCanvases();
                UnityEngine.UI.Text detail=null;
                foreach(var text in canvas.GetComponentsInChildren<UnityEngine.UI.Text>()) if(text.name=="ItemDetails" && text.text.Contains("Burn:")) detail=text;
                if(detail==null || !detail.text.Contains("Bleed: 4 Schaden/s") || !detail.text.Contains("Poison: 2 Schaden alle 0,5 s für 4 s · 25 % Chance")
                    || detail.cachedTextGenerator.characterCountVisible<detail.text.Length-1)
                { Finish("FAIL: weapon DoT details missing or clipped"); return false; }
                CaptureCanvas(canvas,"WeaponDotDetails.png"); return true;
            }
            finally { item.SetWeapon(original); interaction.CloseMenu(); Destroy(profile); }
        }
        private void CaptureCanvas(Component view, string name)
        {
            // Hidden/headless players have no backbuffer. Render the actual canvas offscreen.
            var canvas = view.GetComponent<Canvas>(); var previousMode = canvas.renderMode;
            var previousCamera = canvas.worldCamera; float previousDistance = canvas.planeDistance;
            var cameraObject = new GameObject("Canvas verification camera"); var camera = cameraObject.AddComponent<Camera>();
            var target = new RenderTexture(1040,600,24); var pixels = new Texture2D(1040,600,TextureFormat.RGBA32,false);
            var previousTarget = RenderTexture.active;
            try
            {
                camera.enabled = false; camera.transform.position = new Vector3(0,0,-10000);
                camera.orthographic = true; camera.nearClipPlane = .1f; camera.farClipPlane = 10;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.04f,.06f,.04f);
                camera.targetTexture = target; canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0,0,1040,600),0,0); pixels.Apply(); File.WriteAllBytes(Path.Combine(folder,name),pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previousTarget; canvas.renderMode = previousMode; canvas.worldCamera = previousCamera; canvas.planeDistance = previousDistance;
                camera.targetTexture = null; Destroy(cameraObject); Destroy(pixels); target.Release(); Destroy(target); Canvas.ForceUpdateCanvases();
            }
        }
        private void CaptureWorld(Vector3 center, string name)
        {
            var obj=new GameObject("Spell verification camera"); var camera=obj.AddComponent<Camera>();
            var target=new RenderTexture(1040,600,24); var pixels=new Texture2D(1040,600,TextureFormat.RGBA32,false);
            var previous=RenderTexture.active;
            try
            {
                camera.enabled=false; camera.transform.position=center+new Vector3(2,0,-10);
                camera.orthographic=true; camera.orthographicSize=3; camera.targetTexture=target;
                camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.04f,.06f,.04f);
                camera.Render(); RenderTexture.active=target; pixels.ReadPixels(new Rect(0,0,1040,600),0,0); pixels.Apply();
                File.WriteAllBytes(Path.Combine(folder,name),pixels.EncodeToPNG());
            }
            finally { RenderTexture.active=previous; camera.targetTexture=null; Destroy(obj); Destroy(pixels); target.Release(); Destroy(target); }
        }
        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(1);
            var net = NetworkCoop.Active;
            if (!net.OpenLobby(host, "127.0.0.1", host ? "Economy host" : "Economy guest", resume: false)) { Finish("FAIL: economy connection"); yield break; }
            if (host)
            {
                net.TestDriverActive = true; var world = GameSession.Instance.World;
                string founder = world.CreateWorldCharacter("Host"), guest = world.CreateWorldCharacter("Guest"); net.ChooseLobbyCharacter(founder, ready: true);
                while (net.Lobby.players.Length != 2 || !net.Lobby.players[1].ready) yield return null;
                if (!net.StartLobbyAdventure()) { Finish("FAIL: economy adventure start"); yield break; }
                while (net.ChangingArea || net.ConnectedCount != 2) yield return null;
                foreach (var enemy in FindObjectsByType<TreeMeleeEnemy>(FindObjectsSortMode.None)) enemy.gameObject.SetActive(false);
                CharacterInventory actor = null;
                while (actor == null) { foreach (var candidate in FindObjectsByType<CharacterInventory>(FindObjectsSortMode.None)) if (candidate.CharacterId == guest) actor = candidate; yield return null; }
                world.CharacterVitals(guest).Damage(2); world.CharacterVitals(guest).SpendMana(40);
                if (!actor.TryReceive(actor.Find("gold"), 25) || !actor.TryReceive(actor.Find("health-potion"), 3) || !actor.TryReceive(actor.Find("mana-potion"), 3)) { Finish("FAIL: economy catalog or receive"); yield break; }
                var container = FindFirstObjectByType<SharedStashContainer>();
                if (container == null || !actor.TryReceive(actor.Find("foundation-warrior-armor"), 1)) { Finish("FAIL: stash fixture missing"); yield break; }
                actor.transform.position = container.transform.position + Vector3.right;
                var lootGroup=FindFirstObjectByType<EnemyLoot>(); TreeMeleeEnemy lootEnemy=null;
                foreach (var candidate in FindObjectsByType<TreeMeleeEnemy>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                    if (candidate.EnemyId==lootGroup?.EnemyId) lootEnemy=candidate;
                if (lootEnemy==null) { Finish("FAIL: enemy loot fixture missing"); yield break; }
                var lootItems=lootGroup.GetComponentsInChildren<WorldItem>(true);
                var originalLootPositions=Array.ConvertAll(lootItems,item=>item.transform.position);
                var deathPosition=lootEnemy.SpawnPosition+Vector3.right*2;
                lootEnemy.RefreshSession(); lootEnemy.transform.position=deathPosition;
                lootEnemy.ReceiveHostHit(lootEnemy.MaxHealth,Vector2.zero);
                for (int index=0;index<lootItems.Length;index++)
                    if (!lootItems[index].gameObject.activeInHierarchy || Vector3.Distance(lootItems[index].transform.position,originalLootPositions[index]+Vector3.right*2)>.001f)
                    { Finish("FAIL: loot stayed at spawn instead of death position"); yield break; }
                lootGroup.RefreshSession();
                if (lootItems.Length==0 || Vector3.Distance(lootItems[0].transform.position,originalLootPositions[0]+Vector3.right*2)>.001f)
                { Finish("FAIL: loot drifted on refresh"); yield break; }
                for (int rank=0;rank<3;rank++) world.CharacterSpells(guest).Learn("fireball",3);
                foreach (string type in new[] { "health", "mana" }) foreach (string size in new[] { "small", "medium", "large" })
                    if (!actor.TryReceive(actor.Find(type+"-potion-"+size),2)) { Finish("FAIL: potion variant catalog"); yield break; }
                File.WriteAllText(Path.Combine(folder, "EconomyReady.signal"), "ready");
                while (!File.Exists(Path.Combine(folder, "StashDeposited.signal"))) yield return null;
                var hostActor = net.LocalCharacter; hostActor.transform.position = container.transform.position + Vector3.down;
                if (!container.TryTransfer(hostActor, false, 0, 0, "foundation-warrior-armor", 1)) { Finish("FAIL: host cannot withdraw client deposit"); yield break; }
                File.WriteAllText(Path.Combine(folder, "StashWithdrawn.signal"), "ready");
                while (!File.Exists(Path.Combine(folder, "EconomyClientDone.signal"))) yield return null;
                var bag = world.CharacterInventory(guest); var vitals = world.CharacterVitals(guest);
                if (bag.Gold != 25 || world.CharacterInventory(founder).Gold != 0 || bag.Count("health-potion") != 2 || bag.Count("mana-potion") != 2 || vitals.Health != 6 || vitals.Mana != 20)
                { Finish("FAIL: host economy isolation/consumption"); yield break; }
                string path = Path.Combine(folder, "EconomyIntegration.es3");
                SaveGameStore.Save(world, path); var loaded = SaveGameStore.Load(path); ES3.DeleteFile(path);
                var savedDeath=loaded.DefeatPosition(lootEnemy.EnemyId);
                if (savedDeath==null || Vector3.Distance(new Vector3(savedDeath.x,savedDeath.y,savedDeath.z),deathPosition)>.001f)
                { Finish("FAIL: loot death position lost in real save roundtrip"); yield break; }
                if (loaded.CharacterSpells(guest).Rank("fireball")!=3) { Finish("FAIL: learned spell save"); yield break; }
                if (loaded.CharacterInventory(guest).Gold != 25 || loaded.CharacterInventory(guest).PotionItem(1) != "mana-potion" || loaded.CharacterVitals(guest).Mana != 20)
                { Finish("FAIL: real economy save roundtrip"); yield break; }
                if (loaded.CharacterInventory(founder).Count("foundation-warrior-armor") != 1 || loaded.CharacterInventory(guest).Count("foundation-warrior-armor") != 0 || loaded.SharedStash.Count("foundation-warrior-armor") != 0)
                { Finish("FAIL: item duplicated during stash trade or save"); yield break; }
                world.CharacterSpells(guest).Learn("heal",3); world.CharacterVitals(guest).Damage(2); world.CharacterVitals(founder).Damage(2);
                world.CharacterVitals(guest).RestoreMana(50);
                var spellFixture=new GameObject("Spell test enemy",typeof(Rigidbody2D),typeof(SpriteRenderer)); spellFixture.transform.position=actor.transform.position+Vector3.right*4;
                var spellEnemy=spellFixture.AddComponent<TreeMeleeEnemy>(); spellEnemy.Configure("spell-test-enemy",null); spellFixture.GetComponent<Rigidbody2D>().gravityScale=0;
                var barrier=new GameObject("Spell test wall",typeof(BoxCollider2D)); barrier.transform.position=actor.transform.position+Vector3.right*2;
                barrier.transform.localScale=new Vector3(.5f,3,1); Physics2D.SyncTransforms();
                File.WriteAllText(Path.Combine(folder,"SpellReady.signal"),"ready");
                var flightCaster=actor.GetComponent<SpellCaster>();
                while (!flightCaster.IsCasting) yield return null;
                while (flightCaster.IsCasting) yield return null;
                var launched=flightCaster.Capture().effects;
                if (!spellEnemy.Alive || spellEnemy.Health!=spellEnemy.MaxHealth || launched==null || Array.FindAll(launched,x=>x.kind=="fireball").Length!=2)
                { Finish("FAIL: rank-3 projectiles missing or damage applied before arrival"); yield break; }
                while (!File.Exists(Path.Combine(folder,"SpellDone.signal"))) yield return null;
                if (spellEnemy.Alive || world.CharacterVitals(guest).Health!=5 || world.CharacterVitals(founder).Health!=5 || world.CharacterVitals(guest).Mana!=20)
                { Finish("FAIL: host spell damage/heal/budget/mana"); yield break; }
                var hostCaster=actor.GetComponent<SpellCaster>();
                if (hostCaster.Cooldown("heal")<=0 || hostCaster.TryCast("heal",null,true) || world.CharacterVitals(guest).Mana!=20)
                { Finish("FAIL: spell cooldown or mana charged on rejected cast"); yield break; }
                Destroy(spellFixture); Destroy(barrier);
                var critArmor=actor.Find("foundation-warrior-armor"); critArmor.SetCritChanceBonus(7);
                var critSword=actor.Find("training-sword");
                if (critSword?.Weapon==null || critSword.Weapon.CritChancePercent!=5) { Finish("FAIL: default weapon crit chance"); yield break; }
                critSword.SetCritChanceBonus(3);
                if (!actor.TryReceive(critArmor,1) || !actor.TryReceive(critSword,1)
                    || !actor.State.TryEquip(actor.State.FirstSlot(critArmor.ItemId),EquipmentSlot.Armor,id=>actor.Find(id).Rules)
                    || actor.Stats.CritChancePercent!=7
                    || !actor.State.TryEquip(actor.State.FirstSlot(critSword.ItemId),EquipmentSlot.MainHand,id=>actor.Find(id).Rules)
                    || actor.Stats.CritChancePercent!=15
                    || !actor.State.TryUnequip(EquipmentSlot.MainHand,id=>actor.Find(id).Rules))
                { Finish("FAIL: weapon/item/armor crit aggregation or bag item bonus"); yield break; }
                critArmor.SetCritChanceBonus(100);
                if (actor.Stats.CritChancePercent!=100) { Finish("FAIL: equipped crit chance missing"); yield break; }
                var critObject=new GameObject("Critical hit test enemy",typeof(Rigidbody2D),typeof(SpriteRenderer));
                critObject.transform.position=actor.transform.position+(Vector3)actor.GetComponent<PlayerMelee>().Facing;
                var critEnemy=critObject.AddComponent<TreeMeleeEnemy>(); critEnemy.Configure("critical-hit-test",null);
                critObject.GetComponent<Rigidbody2D>().simulated=false;
                File.WriteAllText(Path.Combine(folder,"CritReady.signal"),"ready");
                while (!File.Exists(Path.Combine(folder,"CriticalSeen.signal"))) yield return null;
                if (critEnemy.Health!=4) { Finish("FAIL: guaranteed unarmed critical hit was not doubled"); yield break; }
                critArmor.SetCritChanceBonus(0);
                if (actor.Stats.CritChancePercent!=0) { Finish("FAIL: crit chance not recalculated"); yield break; }
                File.WriteAllText(Path.Combine(folder,"NormalReady.signal"),"ready");
                while (!File.Exists(Path.Combine(folder,"NormalSeen.signal"))) yield return null;
                if (critEnemy.Health!=3) { Finish("FAIL: zero-chance hit critted"); yield break; }
                int beforeHealth=world.CharacterVitals(guest).Health;
                if (!GameSession.Instance.ApplyDamage(actor,2) || world.CharacterVitals(guest).Health!=beforeHealth-2)
                { Finish("FAIL: player damage or mitigation changed"); yield break; }
                File.WriteAllText(Path.Combine(folder,"PlayerDamageReady.signal"),"ready");
                while (!File.Exists(Path.Combine(folder,"PlayerDamageSeen.signal"))) yield return null;
                Destroy(critObject);
                while (hostCaster.Cooldown("fireball")>0) yield return null;
                critArmor.SetCritChanceBonus(100);
                var critFireObject=new GameObject("Critical fireball test enemy",typeof(Rigidbody2D),typeof(SpriteRenderer));
                critFireObject.transform.position=actor.transform.position+Vector3.right*4;
                var critFireEnemy=critFireObject.AddComponent<TreeMeleeEnemy>(); critFireEnemy.Configure("critical-fireball-test",null);
                critFireObject.GetComponent<Rigidbody2D>().simulated=false;
                File.WriteAllText(Path.Combine(folder,"CriticalSpellReady.signal"),"ready");
                if (!hostCaster.TryCast("fireball","enemy:critical-fireball-test",false)) { Finish("FAIL: critical spell rejected"); yield break; }
                while (!File.Exists(Path.Combine(folder,"CritDone.signal"))) yield return null;
                if (critFireEnemy.Alive || world.CharacterVitals(guest).Mana!=0) { Finish("FAIL: critical spell damage or mana"); yield break; }
                Destroy(critFireObject);
                critArmor.SetCritChanceBonus(0);
                TreeMeleeEnemy dummy=null;
                foreach(var candidate in FindObjectsByType<TreeMeleeEnemy>(FindObjectsInactive.Include,FindObjectsSortMode.None)) if(candidate.IsTrainingDummy) dummy=candidate;
                if(dummy==null || dummy.MaxHealth!=1000000 || dummy.CanContact || dummy.GetComponent<EnemyLoot>()!=null) { Finish("FAIL: training dummy fixture"); yield break; }
                dummy.RefreshSession(); var dummyPosition=dummy.transform.position;
                actor.transform.position=dummyPosition+Vector3.left*4;
                world.CharacterVitals(guest).RestoreMana(20);
                while(hostCaster.Cooldown("fireball")>0) yield return null;
                for(int level=1;level<=3;level++)
                {
                    var dots=hostCaster.Find("fireball").Rank(level).onHitDots;
                    if(dots==null || dots.Length!=1 || dots[0].kind!=DotKind.Burn || dots[0].damagePerTick!=level)
                    { Finish("FAIL: rank-specific Fireball DoT config"); yield break; }
                    var rankState=new DotState(); int rankDamage=0;
                    rankState.Apply(dots[0],0); rankState.Advance(3,(kind,amount)=>rankDamage+=amount);
                    if(rankDamage!=3*level) { Finish("FAIL: rank-specific Burn total"); yield break; }
                }
                File.WriteAllText(Path.Combine(folder,"TrainingReady.signal"),dummy.EnemyId);
                while(!actor.ForceCritsForTesting) yield return null;
                if(actor.Stats.CritChancePercent!=0 || actor.EffectiveCritChancePercent!=100) { Finish("FAIL: test crit override altered equipment"); yield break; }
                while(dummy.Health==1000000) yield return null;
                double firstArrival=Time.realtimeSinceStartupAsDouble;
                if(dummy.Health!=999988) { Finish("FAIL: projectiles arrived together or wrong first damage: "+dummy.Health); yield break; }
                while(dummy.Health==999988) yield return null;
                double arrivalGap=Time.realtimeSinceStartupAsDouble-firstArrival;
                if(dummy.Health!=999976 || arrivalGap<.04 || Vector3.Distance(dummy.transform.position,dummyPosition)>.001f || dummy.CanContact)
                { Finish("FAIL: training damage, stagger or stationary dummy: "+dummy.Health+", gap "+arrivalGap); yield break; }
                CaptureWorld(dummyPosition-Vector3.right*2,"TrainingDummy.png");
                while(!File.Exists(Path.Combine(folder,"TrainingDone.signal")) || actor.ForceCritsForTesting) yield return null;
                while(dummy.GetComponent<DotReceiver>().ActiveMask!=0) yield return null;
                if(dummy.Health!=999967) { Finish("FAIL: rank-3 Fireball burn expected 3 noncritical ticks of 3 damage: "+dummy.Health); yield break; }
                File.WriteAllText(Path.Combine(folder,"BurnExpired.signal"),"ready");
                while(!File.Exists(Path.Combine(folder,"BurnExpiredSeen.signal"))) yield return null;
                for(int kind=0;kind<3;kind++) if(!GameSession.Instance.ApplyDot(dummy,new DotApplication {kind=(DotKind)kind,duration=2,tickInterval=.5f,damagePerTick=1}))
                { Finish("FAIL: concurrent DoT application"); yield break; }
                File.WriteAllText(Path.Combine(folder,"AllDotsReady.signal"),"ready");
                yield return new WaitForSeconds(.2f);
                CaptureWorld(dummyPosition-Vector3.right*2,"AllDotsHost.png");
                while(dummy.GetComponent<DotReceiver>().ActiveMask!=0) yield return null;
                if(dummy.Health!=999955) { Finish("FAIL: independent DoT damage or duplicate stacks: "+dummy.Health); yield break; }
                while(!File.Exists(Path.Combine(folder,"AllDotsSeen.signal"))) yield return null;
                GameSession.Instance.ApplyDot(dummy,new DotApplication()); dummy.RefreshSession();
                if(dummy.Health!=1000000 || dummy.GetComponent<DotReceiver>().ActiveMask!=0) { Finish("FAIL: dummy session refresh retains DoT"); yield break; }
                // Same target component also supports armored players. DoT uses the existing mitigation path.
                int playerBefore=world.CharacterVitals(guest).Health;
                if(!GameSession.Instance.ApplyDot(actor,new DotApplication {kind=DotKind.Poison,duration=.5f,tickInterval=.5f,damagePerTick=2}))
                { Finish("FAIL: player DoT application"); yield break; }
                while(actor.GetComponent<DotReceiver>().ActiveMask!=0) yield return null;
                if(world.CharacterVitals(guest).Health!=playerBefore-2) { Finish("FAIL: player DoT mitigation"); yield break; }
                var lethalObject=new GameObject("DoT death test",typeof(Rigidbody2D),typeof(SpriteRenderer));
                var lethal=lethalObject.AddComponent<TreeMeleeEnemy>(); lethal.Configure("dot-death-test",null);
                yield return null;
                GameSession.Instance.ApplyDot(lethal,new DotApplication {kind=DotKind.Bleed,duration=1,tickInterval=.1f,damagePerTick=6});
                while(lethal.Alive) yield return null;
                if(lethal.GetComponent<DotReceiver>().ActiveMask!=0 || !world.IsEnemyDefeated(lethal.EnemyId)) { Finish("FAIL: DoT kill/cleanup"); yield break; }
                Destroy(lethalObject);
                File.WriteAllText(Path.Combine(folder,"DotsDone.signal"),"done");
                while(!File.Exists(Path.Combine(folder,"DotsClientDone.signal"))) yield return null;
                Finish("PASS: rank-specific Burn 1/2/3 damage per tick, rank-3 Fireball 9 DoT damage; stagger "+arrivalGap.ToString("0.000")+"s; concurrent DoTs, player mitigation, kill and session cleanup.");
            }
            else
            {
                while (net.Lobby.characters.Length != 2) yield return null;
                net.ChooseLobbyCharacter(net.Lobby.characters[1].id, ready: true);
                while (net.LocalCharacter == null || net.LobbyActive || net.ChangingArea || !File.Exists(Path.Combine(folder, "EconomyReady.signal"))) yield return null;
                var actor = net.LocalCharacter;
                while (GameSession.Instance.World.CharacterSpells(actor.CharacterId).Rank("fireball")!=3) yield return null;
                var replicatedLoot=FindFirstObjectByType<EnemyLoot>();
                while (GameSession.Instance.World.DefeatPosition(replicatedLoot.EnemyId)==null) yield return null;
                TreeMeleeEnemy replicatedEnemy=null;
                foreach (var candidate in FindObjectsByType<TreeMeleeEnemy>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                    if (candidate.EnemyId==replicatedLoot.EnemyId) replicatedEnemy=candidate;
                var replicatedDeath=GameSession.Instance.World.DefeatPosition(replicatedLoot.EnemyId);
                if (replicatedEnemy==null || Vector3.Distance(new Vector3(replicatedDeath.x,replicatedDeath.y,replicatedDeath.z),replicatedEnemy.SpawnPosition+Vector3.right*2)>.001f)
                { Finish("FAIL: client loot death position mismatch"); yield break; }
                var rootBefore=replicatedLoot.transform.position; replicatedLoot.RefreshSession();
                if (Vector3.Distance(replicatedLoot.transform.position,rootBefore)>.001f)
                { Finish("FAIL: client loot position refresh drift"); yield break; }
                while (actor.State.Gold != 25 || actor.State.Count("mana-potion") != 3 || actor.State.PotionItem(0) != "health-potion") yield return null;
                var consumableRing=actor.GetComponent<SpellRingMenu>(); consumableRing.Open(); consumableRing.Confirm();
                if(FindFirstObjectByType<SpellRingView>().SelectedIconAvailable) { Finish("FAIL: insufficient-mana spell not greyed out"); yield break; }
                consumableRing.Confirm();
                if(actor.GetComponent<SpellCaster>().IsCasting || !consumableRing.IsOpen) { Finish("FAIL: insufficient-mana spell confirmed"); yield break; }
                CaptureCanvas(FindFirstObjectByType<SpellRingView>(),"InsufficientManaRing.png");
                consumableRing.SwitchCategory();
                if (FindFirstObjectByType<SpellRingView>().IconCount != 6 || actor.RingItemCount("health-potion-small") != 5 || actor.RingItemCount("mana-potion-small") != 5)
                { Finish("FAIL: legacy potions not combined into three sizes"); yield break; }
                var info=FindFirstObjectByType<SpellRingView>().transform.Find("InfoStrip");
                var infoRect=info.GetComponent<RectTransform>();
                float reserved=CharacterVitalsHud.OccupiedScreenHeight(GameSession.Instance.World.CharacterVitals(actor.CharacterId).HeartContainers)/info.GetComponentInParent<Canvas>().scaleFactor;
                if(-infoRect.anchoredPosition.y<reserved || info.GetComponent<UnityEngine.UI.Image>().color.a>=1
                    || info.Find("Title").GetComponent<UnityEngine.UI.Text>().text.Contains("MP ")
                    || info.Find("Details").GetComponent<UnityEngine.UI.Text>().text.Contains("Trank-Cooldown"))
                { Finish("FAIL: ring panel overlaps HUD, is opaque or duplicates mana/cooldown"); yield break; }
                CaptureCanvas(FindFirstObjectByType<SpellRingView>(),"ConsumableRing.png");
                consumableRing.Confirm();
                while (actor.State.Count("health-potion") != 2) yield return null;
                if(actor.PotionCooldownRemaining!=0) { Finish("FAIL: potion cooldown remains"); yield break; }
                actor.TryUseRingItem("mana-potion");
                while (actor.State.Count("mana-potion") != 2) yield return null;
                if(actor.PotionCooldownRemaining!=0) { Finish("FAIL: consecutive mana potion blocked by cooldown"); yield break; }
                // Health is full: using a potion must preserve the stack.
                actor.TryUseRingItem("health-potion"); yield return new WaitForSecondsRealtime(.4f);
                if (actor.State.Count("health-potion") != 2 || actor.PotionCooldownRemaining > 0) { Finish("FAIL: full-health potion consumed"); yield break; }
                if(!FindFirstObjectByType<SpellRingView>().SelectedIconAvailable) { Finish("FAIL: potion faded at full health"); yield break; }
                CaptureCanvas(FindFirstObjectByType<SpellRingView>(),"FullHealthConsumableRing.png");
                consumableRing.Close();
                var container = FindFirstObjectByType<SharedStashContainer>();
                while (actor.State.Count("foundation-warrior-armor") != 1 || !container.CanUse(actor)) yield return null;
                var interaction = actor.GetComponent<InventoryInteraction>(); interaction.OpenInventory();
                yield return null; yield return null;
                var canvas = FindFirstObjectByType<InventoryCanvasView>();
                if (canvas == null || !canvas.InventoryVisible) { Finish("FAIL: editable inventory canvas missing"); yield break; }
                foreach (string size in new[] { "small", "medium", "large" })
                {
                    foreach (string type in new[] { "health", "mana" })
                    {
                        string id=type+"-potion-"+size;
                        while (actor.State.Count(id)!=2) yield return null;
                    }
                }
                foreach(var slot in canvas.GetComponentsInChildren<InventoryCanvasSlot>()) if(slot.Index>=55) { Finish("FAIL: retired quick slot remains visible"); yield break; }
                interaction.UIDrop(actor.State.FirstSlot("mana-potion-large"),55); yield return new WaitForSecondsRealtime(.2f);
                if(actor.State.PotionItem(0)!="health-potion") { Finish("FAIL: hidden quick slot accepts drops"); yield break; }
                CaptureCanvas(canvas,"InventoryCanvas.png");
                int armorSlot=-1;
                for (int index=0;index<40;index++)
                {
                    var shown=interaction.DisplayedItem(index);
                    if (shown!=null && shown.IsConsumable) { Finish("FAIL: potion visible in regular inventory"); yield break; }
                    if (shown?.ItemId=="foundation-warrior-armor") armorSlot=index;
                }
                if (armorSlot<0) { Finish("FAIL: armor not visible in inventory"); yield break; }
                interaction.UIDrop(armorSlot,40+(int)EquipmentSlot.Armor);
                while (actor.State.GetEquipment(EquipmentSlot.Armor) != "foundation-warrior-armor") yield return null;
                while (GameSession.Instance.World.CharacterVitals(actor.CharacterId).MaxHealth!=8) yield return null;
                if (actor.Stats.Armor!=60 || GameSession.Instance.World.CharacterVitals(actor.CharacterId).BaseMaxHealth!=6 || GameSession.Instance.World.CharacterVitals(actor.CharacterId).Health!=6)
                { Finish("FAIL: replicated equipment stats or equip healing exploit"); yield break; }
                yield return null; yield return null;
                CaptureCanvas(canvas,"EquippedStatsCanvas.png");
                interaction.UIActivate(40+(int)EquipmentSlot.Armor);
                while (actor.State.Count("foundation-warrior-armor") != 1) yield return null;
                while (GameSession.Instance.World.CharacterVitals(actor.CharacterId).MaxHealth!=6) yield return null;
                if (!interaction.OpenStash(container)) { Finish("FAIL: stash UI open"); yield break; }
                yield return null; yield return null;
                if (!canvas.StashVisible || canvas.InventoryVisible) { Finish("FAIL: canvas panel switch"); yield break; }
                CaptureCanvas(canvas,"SharedStashCanvas.png");
                interaction.UIDrop(actor.State.FirstSlot("foundation-warrior-armor"),40);
                while (actor.State.Count("foundation-warrior-armor") != 0 || GameSession.Instance.World.SharedStash.Count("foundation-warrior-armor") != 1) yield return null;
                File.WriteAllText(Path.Combine(folder, "StashDeposited.signal"), "ready");
                while (!File.Exists(Path.Combine(folder, "StashWithdrawn.signal")) || GameSession.Instance.World.SharedStash.Count("foundation-warrior-armor") != 0) yield return null;
                container.TryTransfer(actor, false, 0, 2, "foundation-warrior-armor", 1); yield return new WaitForSecondsRealtime(.4f);
                if (actor.State.Count("foundation-warrior-armor") != 0) { Finish("FAIL: stale stash withdrawal duplicated armor"); yield break; }
                interaction.CloseMenu(); yield return null; yield return null;
                if (canvas.StashVisible || interaction.SelectedIndex >= 57) { Finish("FAIL: canvas close/navigation reset"); yield break; }
                File.WriteAllText(Path.Combine(folder, "EconomyClientDone.signal"), "done");
                while (!File.Exists(Path.Combine(folder,"SpellReady.signal")) || GameSession.Instance.World.CharacterSpells(actor.CharacterId).Rank("heal")!=1) yield return null;
                // Runtime-created test enemy has no authored client counterpart. Create its visual mirror.
                var spellMirror=new GameObject("Spell test enemy",typeof(Rigidbody2D),typeof(SpriteRenderer));
                spellMirror.transform.position=actor.transform.position+Vector3.right*4;
                var mirrorEnemy=spellMirror.AddComponent<TreeMeleeEnemy>(); mirrorEnemy.Configure("spell-test-enemy",null); mirrorEnemy.SetReplica(true);
                var ring=actor.GetComponent<SpellRingMenu>();
                if (!ring.Open()) { Finish("FAIL: ring open"); yield break; }
                if (FindFirstObjectByType<SpellRingView>().IconCount!=2 || !FindFirstObjectByType<SpellRingView>().RingVisible)
                { Finish("FAIL: learned element choices missing"); yield break; }
                var ringView=FindFirstObjectByType<SpellRingView>();
                var manyLabels=new string[12]; var manyIcons=new Sprite[12];
                for(int i=0;i<12;i++) { manyLabels[i]="Test "+i; manyIcons[i]=actor.GetComponent<SpellCaster>().Find("fireball").Icon; }
                ringView.RenderIcons("Layout test",manyLabels,manyIcons,11,"",actor.transform,false,false);
                if(ringView.IconCount!=12 || !ringView.RingVisible) { Finish("FAIL: ring capped at eight entries"); yield break; }
                ring.Page(0); float beforeRight=ringView.Rotation; ring.Page(1); yield return new WaitForSecondsRealtime(.4f);
                if(ringView.Rotation>=beforeRight) { Finish("FAIL: right input did not rotate clockwise"); yield break; }
                var top=ringView.transform.Find("Ring/Icon-1").GetComponent<RectTransform>().anchoredPosition;
                if(Mathf.Abs(top.x)>1 || Mathf.Abs(top.y-74)>1) { Finish("FAIL: rotating selected icon missed fixed top marker"); yield break; }
                float beforeLeft=ringView.Rotation; ring.Page(-1); yield return new WaitForSecondsRealtime(.4f);
                if(ringView.Rotation<=beforeLeft) { Finish("FAIL: left input did not rotate anticlockwise"); yield break; }
                var ringPosition=actor.transform.position; net.TestMotion=Vector2.right;
                yield return new WaitForSecondsRealtime(.3f);
                if (!ring.IsOpen || Vector2.Distance(actor.transform.position,ringPosition)>.1f) { Finish("FAIL: selection movement lock"); yield break; }
                net.TestMotion=Vector2.zero;
                CaptureCanvas(FindFirstObjectByType<SpellRingView>(),"SpellElementRing.png");
                ring.ToggleBook(); CaptureCanvas(FindFirstObjectByType<SpellRingView>(),"SpellBook.png"); ring.Back();
                ring.Confirm(); ring.Confirm(); // Fire -> Fireball -> explicit target choice
                if (FindFirstObjectByType<SpellRingView>().RingVisible) { Finish("FAIL: icon ring remained visible during target choice"); yield break; }
                UnityEngine.UI.Text spellDetails=null;
                foreach(var text in ringView.GetComponentsInChildren<UnityEngine.UI.Text>()) if(text.name=="Details") spellDetails=text;
                Canvas.ForceUpdateCanvases();
                if(spellDetails==null || !spellDetails.text.Contains("Burn: 3 Schaden/s für 3 s · 100 % Chance") || spellDetails.preferredHeight>spellDetails.rectTransform.rect.height+1)
                { Finish("FAIL: current-rank spell DoT details missing or clipped"); yield break; }
                CaptureCanvas(FindFirstObjectByType<SpellRingView>(),"SpellTargetRing.png");
                ring.Back(); ring.Back(); ring.Back(); // returns through spell/element then closes
                if (ring.IsOpen) { Finish("FAIL: ring cancel"); yield break; }
                yield return new WaitForSecondsRealtime(.1f);
                var caster=actor.GetComponent<SpellCaster>();
                ring.Open(); ring.Confirm(); ring.Confirm(); ring.Confirm();
                if(ring.IsOpen) { Finish("FAIL: single target required extra confirmation"); yield break; }
                while (!caster.IsCasting) yield return null;
                var castPosition=actor.transform.position; caster.TryCast("fireball","enemy:spell-test-enemy",false);
                net.TestMotion=Vector2.right;
                yield return new WaitForSecondsRealtime(.2f);
                if (Vector2.Distance(actor.transform.position,castPosition)>.1f) { Finish("FAIL: casting movement lock"); yield break; }
                net.TestMotion=Vector2.zero;
                while (caster.IsCasting || GameSession.Instance.World.CharacterVitals(actor.CharacterId).Mana!=30) yield return null;
                yield return new WaitForSecondsRealtime(.18f);
                int replicatedProjectiles=0;
                Vector3 firstProjectile=Vector3.zero, secondProjectile=Vector3.zero;
                foreach (var sprite in actor.GetComponentsInChildren<SpriteRenderer>())
                    if (sprite.gameObject.name=="Spell fireball" && sprite.enabled && sprite.sprite!=null)
                    { if (replicatedProjectiles++==0) firstProjectile=sprite.transform.position; else secondProjectile=sprite.transform.position; }
                if (replicatedProjectiles!=2) { Finish("FAIL: client did not render two replicated fireball sprites"); yield break; }
                if (Vector3.Distance(firstProjectile,secondProjectile)<.5f) { Finish("FAIL: fireballs did not fan apart before homing"); yield break; }
                CaptureWorld(actor.transform.position,"ReplicatedFireballs.png");
                if (!ring.Open()) { Finish("FAIL: heal ring open"); yield break; }
                ring.Choose(1); ring.Confirm(); // Light -> Heal
                CaptureCanvas(FindFirstObjectByType<SpellRingView>(),"SpellHealTargetRing.png");
                ring.Choose(2); // All (two allies), starts casting immediately.
                if(ring.IsOpen) { Finish("FAIL: group target required extra confirmation"); yield break; }
                while (!caster.IsCasting) yield return null;
                while (caster.IsCasting || GameSession.Instance.World.CharacterVitals(actor.CharacterId).Mana!=20 || GameSession.Instance.World.CharacterVitals(actor.CharacterId).Health!=5) yield return null;
                File.WriteAllText(Path.Combine(folder,"SpellDone.signal"),"done");
                while (!File.Exists(Path.Combine(folder,"CritReady.signal"))) yield return null;
                GameSession.Instance.RequestMeleeAttack(actor);
                UnityEngine.UI.Text criticalNumber=null;
                while (criticalNumber==null)
                {
                    foreach (var text in FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None))
                        if (text.gameObject.name=="Critical damage number" && text.text=="2") criticalNumber=text;
                    yield return null;
                }
                if (criticalNumber.fontSize!=40 || criticalNumber.color.r<.9f || criticalNumber.color.b>.3f)
                { Finish("FAIL: critical number not larger and gold-orange"); yield break; }
                yield return null; CaptureCanvas(FindFirstObjectByType<DamageNumbersView>(),"CriticalDamageNumber.png");
                File.WriteAllText(Path.Combine(folder,"CriticalSeen.signal"),"seen");
                while (!File.Exists(Path.Combine(folder,"NormalReady.signal"))) yield return null;
                yield return new WaitForSecondsRealtime(.5f); GameSession.Instance.RequestMeleeAttack(actor);
                UnityEngine.UI.Text normalNumber=null;
                while (normalNumber==null)
                {
                    foreach (var text in FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None))
                        if (text.gameObject.name=="Damage number" && text.text=="1") normalNumber=text;
                    yield return null;
                }
                if (normalNumber.fontSize!=26 || normalNumber.color.r<.9f || normalNumber.color.g<.85f || normalNumber.color.b>.3f) { Finish("FAIL: normal number not yellow"); yield break; }
                CaptureCanvas(FindFirstObjectByType<DamageNumbersView>(),"YellowDamageNumber.png");
                File.WriteAllText(Path.Combine(folder,"NormalSeen.signal"),"seen");
                while (!File.Exists(Path.Combine(folder,"PlayerDamageReady.signal"))) yield return null;
                bool playerNumber=false;
                while (!playerNumber)
                {
                    foreach (var text in FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None))
                        if (text.gameObject.name=="Damage number" && text.text=="2") playerNumber=true;
                    yield return null;
                }
                File.WriteAllText(Path.Combine(folder,"PlayerDamageSeen.signal"),"seen");
                while (!File.Exists(Path.Combine(folder,"CriticalSpellReady.signal"))) yield return null;
                bool criticalSpellNumber=false;
                while (!criticalSpellNumber)
                {
                    foreach (var text in FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None))
                        if (text.gameObject.name=="Critical damage number" && text.text=="12" && text.fontSize==40) criticalSpellNumber=true;
                    yield return null;
                }
                CaptureCanvas(FindFirstObjectByType<DamageNumbersView>(),"CriticalFireballNumber.png");
                File.WriteAllText(Path.Combine(folder,"CritDone.signal"),"done");
                while(!File.Exists(Path.Combine(folder,"TrainingReady.signal"))) yield return null;
                TreeMeleeEnemy dummy=null;
                while(dummy==null || !dummy.Alive)
                {
                    foreach(var candidate in FindObjectsByType<TreeMeleeEnemy>(FindObjectsInactive.Include,FindObjectsSortMode.None)) if(candidate.IsTrainingDummy) dummy=candidate;
                    yield return null;
                }
                yield return new WaitForSecondsRealtime(.5f);
                if(!CheckWeaponDotDetails(actor)) yield break;
                yield return new WaitForSecondsRealtime(.2f);
                if(!GameSession.Instance.SetCritTestMode(actor,true)) { Finish("FAIL: client crit toggle request"); yield break; }
                while(!actor.ForceCritsForTesting) yield return null;
                if(actor.Stats.CritChancePercent!=0 || actor.EffectiveCritChancePercent!=100 || !actor.GetComponent<SpellCaster>().TryCast("fireball",SpellCaster.Id(dummy),false))
                { Finish("FAIL: client training crit cast"); yield break; }
                while(dummy.Health!=999976) yield return null;
                yield return null;
                var label=dummy.GetComponentInChildren<TextMesh>();
                if(label==null || !label.text.Contains("999.976") || dummy.CanContact) { Finish("FAIL: replicated dummy HP label"); yield break; }
                if(dummy.GetComponent<DotReceiver>().ActiveMask!=1 || GameSession.Instance.ApplyDot(dummy,new DotApplication())) { Finish("FAIL: burn replication or client DoT authority"); yield break; }
                if(!GameSession.Instance.SetCritTestMode(actor,false)) { Finish("FAIL: client crit toggle off"); yield break; }
                while(actor.ForceCritsForTesting) yield return null;
                if(actor.EffectiveCritChancePercent!=0) { Finish("FAIL: crit test off did not restore equipment chance"); yield break; }
                File.WriteAllText(Path.Combine(folder,"TrainingDone.signal"),"done");
                while(!File.Exists(Path.Combine(folder,"BurnExpired.signal")) || dummy.Health!=999967 || dummy.GetComponent<DotReceiver>().ActiveMask!=0) yield return null;
                File.WriteAllText(Path.Combine(folder,"BurnExpiredSeen.signal"),"seen");
                while(!File.Exists(Path.Combine(folder,"AllDotsReady.signal")) || dummy.GetComponent<DotReceiver>().ActiveMask!=7) yield return null;
                yield return null;
                if(dummy.GetComponent<DotEffectsView>().VisibleMask!=7) { Finish("FAIL: concurrent DoT visuals"); yield break; }
                CaptureWorld(dummy.transform.position-Vector3.right*2,"AllDotsClient.png");
                while(dummy.Health!=999955 || dummy.GetComponent<DotReceiver>().ActiveMask!=0) yield return null;
                File.WriteAllText(Path.Combine(folder,"AllDotsSeen.signal"),"seen");
                while(!File.Exists(Path.Combine(folder,"DotsDone.signal"))) yield return null;
                File.WriteAllText(Path.Combine(folder,"DotsClientDone.signal"),"done");
                Finish("PASS: yellow normal numbers, crit toggle, Burn expiration, independent concurrent DoT visuals/HP replication; client cannot apply damage or DoTs.");
            }
        }
    }
}
