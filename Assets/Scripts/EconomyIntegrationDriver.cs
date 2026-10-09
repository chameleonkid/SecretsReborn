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
                if (bag.Gold != 25 || world.CharacterInventory(founder).Gold != 0 || bag.Count("health-potion") != 2 || bag.Count("mana-potion") != 2 || vitals.Health != 6 || vitals.Mana != 30)
                { Finish("FAIL: host economy isolation/consumption"); yield break; }
                string path = Path.Combine(folder, "EconomyIntegration.es3");
                SaveGameStore.Save(world, path); var loaded = SaveGameStore.Load(path); ES3.DeleteFile(path);
                if (loaded.CharacterSpells(guest).Rank("fireball")!=3) { Finish("FAIL: learned spell save"); yield break; }
                if (loaded.CharacterInventory(guest).Gold != 25 || loaded.CharacterInventory(guest).PotionItem(1) != "mana-potion" || loaded.CharacterVitals(guest).Mana != 30)
                { Finish("FAIL: real economy save roundtrip"); yield break; }
                if (loaded.CharacterInventory(founder).Count("foundation-warrior-armor") != 1 || loaded.CharacterInventory(guest).Count("foundation-warrior-armor") != 0 || loaded.SharedStash.Count("foundation-warrior-armor") != 0)
                { Finish("FAIL: item duplicated during stash trade or save"); yield break; }
                world.CharacterSpells(guest).Learn("heal",3); world.CharacterVitals(guest).Damage(2); world.CharacterVitals(founder).Damage(2);
                world.CharacterVitals(guest).RestoreMana(50);
                var dummy=new GameObject("Spell test enemy",typeof(Rigidbody2D),typeof(SpriteRenderer)); dummy.transform.position=actor.transform.position+Vector3.right*4;
                var spellEnemy=dummy.AddComponent<TreeMeleeEnemy>(); spellEnemy.Configure("spell-test-enemy",null); dummy.GetComponent<Rigidbody2D>().gravityScale=0;
                var barrier=new GameObject("Spell test wall",typeof(BoxCollider2D)); barrier.transform.position=actor.transform.position+Vector3.right*2;
                barrier.transform.localScale=new Vector3(.5f,3,1); Physics2D.SyncTransforms();
                File.WriteAllText(Path.Combine(folder,"SpellReady.signal"),"ready");
                while (!File.Exists(Path.Combine(folder,"SpellDone.signal"))) yield return null;
                if (spellEnemy.Alive || world.CharacterVitals(guest).Health!=5 || world.CharacterVitals(founder).Health!=5 || world.CharacterVitals(guest).Mana!=20)
                { Finish("FAIL: host spell damage/heal/budget/mana"); yield break; }
                var hostCaster=actor.GetComponent<SpellCaster>();
                if (hostCaster.Cooldown("heal")<=0 || hostCaster.TryCast("heal",null,true) || world.CharacterVitals(guest).Mana!=20)
                { Finish("FAIL: spell cooldown or mana charged on rejected cast"); yield break; }
                Destroy(dummy); Destroy(barrier);
                Finish("PASS: economy, stash and save; client fireball through wall, shared heal, fixed cast position, host mana/cooldown and replicated casting.");
            }
            else
            {
                while (net.Lobby.characters.Length != 2) yield return null;
                net.ChooseLobbyCharacter(net.Lobby.characters[1].id, ready: true);
                while (net.LocalCharacter == null || net.LobbyActive || net.ChangingArea || !File.Exists(Path.Combine(folder, "EconomyReady.signal"))) yield return null;
                var actor = net.LocalCharacter;
                while (GameSession.Instance.World.CharacterSpells(actor.CharacterId).Rank("fireball")!=3) yield return null;
                while (actor.State.Gold != 25 || actor.State.Count("mana-potion") != 3 || actor.State.PotionItem(0) != "health-potion") yield return null;
                actor.TryUsePotion(0);
                while (actor.State.Count("health-potion") != 2 || actor.PotionCooldownRemaining <= 0) yield return null;
                actor.TryUsePotion(1); yield return new WaitForSecondsRealtime(.35f);
                if (actor.State.Count("mana-potion") != 3) { Finish("FAIL: shared cooldown bypass"); yield break; }
                while (actor.PotionCooldownRemaining > 0) yield return null;
                actor.TryUsePotion(1);
                while (actor.State.Count("mana-potion") != 2) yield return null;
                while (actor.PotionCooldownRemaining > 0) yield return null;
                // Health is full: using a potion must preserve the stack.
                actor.TryUsePotion(0); yield return new WaitForSecondsRealtime(.4f);
                if (actor.State.Count("health-potion") != 2 || actor.PotionCooldownRemaining > 0) { Finish("FAIL: full-health potion consumed"); yield break; }
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
                        string id=type+"-potion-"+size; int shortcut=type=="health" ? 55 : 56;
                        while (actor.State.Count(id)!=2) yield return null;
                        interaction.UIDrop(actor.State.FirstSlot(id),shortcut);
                        while (actor.State.PotionItem(shortcut-55)!=id) yield return null;
                        if (actor.State.Count(id)!=2) { Finish("FAIL: potion shortcut removed backpack stack"); yield break; }
                    }
                }
                interaction.UIDrop(actor.State.FirstSlot("mana-potion-large"),55); yield return new WaitForSecondsRealtime(.2f);
                if (actor.State.PotionItem(0)!="health-potion-large") { Finish("FAIL: wrong potion kind accepted"); yield break; }
                interaction.UIDrop(actor.State.FirstSlot("health-potion"),55); interaction.UIDrop(actor.State.FirstSlot("mana-potion"),56);
                while (actor.State.PotionItem(0)!="health-potion" || actor.State.PotionItem(1)!="mana-potion") yield return null;
                CaptureCanvas(canvas,"InventoryCanvas.png");
                interaction.UIDrop(actor.State.FirstSlot("foundation-warrior-armor"),40+(int)EquipmentSlot.Armor);
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
                ring.Page(0); ring.Page(1); yield return new WaitForSecondsRealtime(.4f);
                var top=ringView.transform.Find("Ring/Icon-1").GetComponent<RectTransform>().anchoredPosition;
                if(Mathf.Abs(top.x)>1 || Mathf.Abs(top.y-74)>1) { Finish("FAIL: rotating selected icon missed fixed top marker"); yield break; }
                ring.Page(-1); yield return new WaitForSecondsRealtime(.4f);
                var ringPosition=actor.transform.position; net.TestMotion=Vector2.right;
                yield return new WaitForSecondsRealtime(.3f);
                if (!ring.IsOpen || Vector2.Distance(actor.transform.position,ringPosition)>.1f) { Finish("FAIL: selection movement lock"); yield break; }
                net.TestMotion=Vector2.zero;
                CaptureCanvas(FindFirstObjectByType<SpellRingView>(),"SpellElementRing.png");
                ring.ToggleBook(); CaptureCanvas(FindFirstObjectByType<SpellRingView>(),"SpellBook.png"); ring.Back();
                ring.Confirm(); ring.Confirm(); // Fire -> Fireball -> explicit target choice
                if (FindFirstObjectByType<SpellRingView>().RingVisible) { Finish("FAIL: icon ring remained visible during target choice"); yield break; }
                CaptureCanvas(FindFirstObjectByType<SpellRingView>(),"SpellTargetRing.png");
                ring.Back(); ring.Back(); ring.Back(); // returns through spell/element then closes
                if (ring.IsOpen) { Finish("FAIL: ring cancel"); yield break; }
                yield return new WaitForSecondsRealtime(.1f);
                var caster=actor.GetComponent<SpellCaster>(); caster.TryCast("fireball","enemy:spell-test-enemy",false);
                while (!caster.IsCasting) yield return null;
                var castPosition=actor.transform.position; caster.TryCast("fireball","enemy:spell-test-enemy",false);
                net.TestMotion=Vector2.right;
                yield return new WaitForSecondsRealtime(.2f);
                if (Vector2.Distance(actor.transform.position,castPosition)>.1f) { Finish("FAIL: casting movement lock"); yield break; }
                net.TestMotion=Vector2.zero;
                while (caster.IsCasting || GameSession.Instance.World.CharacterVitals(actor.CharacterId).Mana!=30) yield return null;
                if (!ring.Open()) { Finish("FAIL: heal ring open"); yield break; }
                ring.Choose(1); ring.Confirm(); // Light -> Heal
                ring.Choose(2); // All (two allies)
                CaptureCanvas(FindFirstObjectByType<SpellRingView>(),"SpellConfirmRing.png");
                ring.Confirm();
                while (!caster.IsCasting) yield return null;
                while (caster.IsCasting || GameSession.Instance.World.CharacterVitals(actor.CharacterId).Mana!=20 || GameSession.Instance.World.CharacterVitals(actor.CharacterId).Health!=5) yield return null;
                File.WriteAllText(Path.Combine(folder,"SpellDone.signal"),"done");
                Finish("PASS: UI/economy regression and client-authority spell requests, replicated casting, blocked movement and shared heal.");
            }
        }
    }
}
