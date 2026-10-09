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
        private void CaptureCanvas(InventoryCanvasView view, string name)
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
                if (loaded.CharacterInventory(guest).Gold != 25 || loaded.CharacterInventory(guest).PotionItem(1) != "mana-potion" || loaded.CharacterVitals(guest).Mana != 30)
                { Finish("FAIL: real economy save roundtrip"); yield break; }
                if (loaded.CharacterInventory(founder).Count("foundation-warrior-armor") != 1 || loaded.CharacterInventory(guest).Count("foundation-warrior-armor") != 0 || loaded.SharedStash.Count("foundation-warrior-armor") != 0)
                { Finish("FAIL: item duplicated during stash trade or save"); yield break; }
                Finish("PASS: native host/client gold isolation, potion RPC/cooldown, client stash deposit/host withdrawal, stale withdrawal rejected and real Easy Save roundtrip.");
            }
            else
            {
                while (net.Lobby.characters.Length != 2) yield return null;
                net.ChooseLobbyCharacter(net.Lobby.characters[1].id, ready: true);
                while (net.LocalCharacter == null || net.LobbyActive || net.ChangingArea || !File.Exists(Path.Combine(folder, "EconomyReady.signal"))) yield return null;
                var actor = net.LocalCharacter;
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
                Finish("PASS: editable inventory/stash canvas creation and switching, UI equip/unequip/drop through host RPC, replicated currency/potions, stash trade and stale withdrawal protection.");
            }
        }
    }
}
