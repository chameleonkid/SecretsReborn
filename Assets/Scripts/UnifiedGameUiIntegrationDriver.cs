using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SecretsReborn
{
    // Opt-in development check; creates an unsaved host world and isolated test paths.
    public sealed class UnifiedGameUiIntegrationDriver : MonoBehaviour
    {
        bool host,finished; string report,folder; double deadline;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            var args=Environment.GetCommandLineArgs();
            if(!Debug.isDebugBuild || Array.IndexOf(args,"--game-ui-role")<0 || FindFirstObjectByType<UnifiedGameUiIntegrationDriver>()!=null) return;
            var obj=new GameObject("Unified game UI integration check"); DontDestroyOnLoad(obj); obj.AddComponent<UnifiedGameUiIntegrationDriver>();
        }
        void Awake()
        {
            var args=Environment.GetCommandLineArgs(); host=args[Array.IndexOf(args,"--game-ui-role")+1]=="host";
            report=args[Array.IndexOf(args,"--game-ui-report")+1]; folder=Path.GetDirectoryName(report); Directory.CreateDirectory(folder);
            SaveGameStore.TestSlotRoot=Path.Combine(folder,host ? "HostSlots" : "ClientSlots"); Directory.CreateDirectory(SaveGameStore.TestSlotRoot);
            deadline=Time.realtimeSinceStartupAsDouble+110; Application.runInBackground=true;
        }
        void Update() { if(!finished && Time.realtimeSinceStartupAsDouble>deadline) Finish("FAIL: unified UI timeout: "+NetworkCoop.Active?.Status); }
        void Finish(string result) { if(finished) return; finished=true; File.WriteAllText(report,result); Debug.Log(result); Application.Quit(result.StartsWith("PASS") ? 0 : 1); }
        string Signal(string name) => Path.Combine(folder,name);
        static bool SurfaceValid(GameObject obj) => obj.GetComponent<Image>() is Image image && image.sprite==null && image.color.a>.5f && image.color.a<.95f && obj.GetComponentInChildren<QuietMenuFrameGraphic>(true)!=null;
        static bool InventorySurfaceValid(GameObject obj) => obj.GetComponent<Image>() is Image image && image.sprite!=null && image.color.a==1;
        void Capture(Component component,string name)
        {
            var canvas=component.GetComponent<Canvas>(); var mode=canvas.renderMode; var originalCamera=canvas.worldCamera; float distance=canvas.planeDistance;
            var obj=new GameObject("Game UI verification camera"); var camera=obj.AddComponent<Camera>();
            var worldObject=new GameObject("World verification camera"); var worldCamera=worldObject.AddComponent<Camera>();
            var worldTarget=new RenderTexture(Screen.width,Screen.height,24);
            GameObject backdrop=null;
            var target=new RenderTexture(Screen.width,Screen.height,24); var pixels=new Texture2D(Screen.width,Screen.height,TextureFormat.RGBA32,false); var previous=RenderTexture.active;
            try
            {
                if(Camera.main!=null) { worldCamera.CopyFrom(Camera.main); worldCamera.transform.SetPositionAndRotation(Camera.main.transform.position,Camera.main.transform.rotation); }
                worldCamera.enabled=false; worldCamera.cullingMask=~0; worldCamera.targetTexture=worldTarget; worldCamera.Render();
                backdrop=new GameObject("Verification world backdrop",typeof(RectTransform),typeof(RawImage)); backdrop.transform.SetParent(canvas.transform,false); backdrop.transform.SetAsFirstSibling();
                var backRect=(RectTransform)backdrop.transform; backRect.anchorMin=Vector2.zero; backRect.anchorMax=Vector2.one; backRect.offsetMin=backRect.offsetMax=Vector2.zero;
                var worldImage=backdrop.GetComponent<RawImage>(); worldImage.texture=worldTarget; worldImage.raycastTarget=false;
                // Render the actual overlay in a second pass; world sprite sorting must never cover UI text.
                camera.enabled=false; camera.transform.position=new Vector3(0,0,-10000); camera.orthographic=true; camera.nearClipPlane=.1f; camera.farClipPlane=10;
                camera.clearFlags=CameraClearFlags.Depth; camera.targetTexture=target;
                canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=1;
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active=target; pixels.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0); pixels.Apply();
                File.WriteAllBytes(Path.Combine(folder,(host ? "Host" : "Client")+name+".png"),pixels.EncodeToPNG());
                if(component is InventoryCanvasView)
                {
                    var selected=component.GetComponentsInChildren<InventoryCanvasSlot>().Select(s=>s.transform.Find("Selection")).Where(t=>t.gameObject.activeInHierarchy).ToArray();
                    if(selected.Length!=1) throw new InvalidOperationException("Expected exactly one visible selected slot: "+name);
                    var frame=selected[0].GetComponentInChildren<QuietMenuFrameGraphic>();
                    if(frame==null || frame.GetComponent<CanvasRenderer>()==null) throw new InvalidOperationException("Selected slot has no rendered border: "+name);
                    var corners=new Vector3[4]; ((RectTransform)selected[0]).GetWorldCorners(corners);
                    Vector2 min=RectTransformUtility.WorldToScreenPoint(camera,corners[0]),max=RectTransformUtility.WorldToScreenPoint(camera,corners[2]);
                    int goldPixels=0;
                    for(int y=Mathf.Max(0,Mathf.FloorToInt(min.y));y<Mathf.Min(pixels.height,Mathf.CeilToInt(max.y));y++)
                        for(int x=Mathf.Max(0,Mathf.FloorToInt(min.x));x<Mathf.Min(pixels.width,Mathf.CeilToInt(max.x));x++)
                        { Color pixel=pixels.GetPixel(x,y); if(pixel.r>.85f && pixel.g>.65f && pixel.b<.45f) goldPixels++; }
                    File.AppendAllText(Path.Combine(folder,(host ? "Host" : "Client")+"SelectionPixels.txt"),name+": index="+selected[0].parent.GetComponent<InventoryCanvasSlot>().Index+", gold pixels="+goldPixels+"\n");
                    if(goldPixels<60) throw new InvalidOperationException("Selected gold rim is not visible in rendered pixels: "+name);
                }
            }
            finally
            { RenderTexture.active=previous; canvas.renderMode=mode; canvas.worldCamera=originalCamera; canvas.planeDistance=distance; camera.targetTexture=null; worldCamera.targetTexture=null; if(backdrop!=null) { backdrop.SetActive(false); Destroy(backdrop); } Destroy(obj); Destroy(worldObject); Destroy(pixels); target.Release(); Destroy(target); worldTarget.Release(); Destroy(worldTarget); Canvas.ForceUpdateCanvases(); }
        }
        IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(.7f); var net=NetworkCoop.Active; net.TestDriverActive=true; net.TestVoteAuto=false;
            if(!net.OpenLobby(host,"127.0.0.1",host ? "Host UI" : "Client UI",resume:false)) { Finish("FAIL: UI test lobby"); yield break; }
            if(host)
            {
                var world=GameSession.Instance.World; string founder=world.CreateWorldCharacter("Host"),guest=world.CreateWorldCharacter("Client"); net.ChooseLobbyCharacter(founder,ready:true);
                while(net.Lobby.players.Length!=2 || !net.Lobby.players[1].ready) yield return null;
                if(!net.StartLobbyAdventure()) { Finish("FAIL: UI adventure start"); yield break; }
                while(net.ChangingArea || net.ConnectedCount!=2) yield return null;
                foreach(var enemy in FindObjectsByType<TreeMeleeEnemy>(FindObjectsSortMode.None)) enemy.gameObject.SetActive(false);
                var container=FindFirstObjectByType<SharedStashContainer>();
                foreach(var actor in FindObjectsByType<CharacterInventory>(FindObjectsSortMode.None))
                {
                    GameSession.Warp(actor,container.transform.position+Vector3.right*.6f);
                    actor.TryReceive(actor.Find("foundation-warrior-armor"),1); actor.TryReceive(actor.Find("warm-lamp"),1); actor.TryReceive(actor.Find("gold"),25);
                    for(int i=0;i<3;i++) world.CharacterSpells(actor.CharacterId).Learn("fireball",3);
                }
                File.WriteAllText(Signal("fixture-ready"),"ready");
            }
            else
            {
                while(net.Lobby.characters.Length!=2) yield return null;
                net.ChooseLobbyCharacter(net.Lobby.characters[1].id,ready:true);
                while(net.LocalCharacter==null || net.ChangingArea || net.LobbyActive) yield return null;
                while(!File.Exists(Signal("fixture-ready"))) yield return null;
            }
            var local=net.LocalCharacter; var interaction=local.GetComponent<InventoryInteraction>();
            while(local.State.Count("foundation-warrior-armor")!=1 || GameSession.Instance.World.CharacterSpells(local.CharacterId).Rank("fireball")!=3) yield return null;
            interaction.OpenInventory(); yield return new WaitForSecondsRealtime(.35f);
            var view=FindFirstObjectByType<InventoryCanvasView>(); view.Refresh();
            var panel=view.transform.GetComponentsInChildren<RectTransform>(true).First(x=>x.name=="InventoryPanel").gameObject;
            if(!view.InventoryVisible || !InventorySurfaceValid(panel))
            { Finish("FAIL: illustrated opaque inventory"); yield break; }
            int armor=Enumerable.Range(0,40).First(i=>interaction.DisplayedItem(i)?.ItemId=="foundation-warrior-armor"); interaction.UISelect(armor); view.Refresh(); Capture(view,"Inventory");
            interaction.UISelect(40+(int)EquipmentSlot.Head); view.Refresh(); Capture(view,"EquipmentSelection");
            interaction.UISelect(39); view.Refresh(); Capture(view,"LastBagSelection");
            interaction.UISelect(armor); view.Refresh();
            if(!host)
            {
                interaction.UIActivate(armor);
                while(local.State.GetEquipment(EquipmentSlot.Armor)!="foundation-warrior-armor") yield return null;
                view.Refresh(); Capture(view,"Equipment"); interaction.UIActivate(40+(int)EquipmentSlot.Armor);
                while(local.State.Count("foundation-warrior-armor")!=1) yield return null;
            }
            interaction.CloseMenu(); var stash=FindFirstObjectByType<SharedStashContainer>();
            while(!stash.CanUse(local)) yield return null;
            if(!interaction.OpenStash(stash)) { Finish("FAIL: open styled stash"); yield break; }
            yield return new WaitForSecondsRealtime(.3f); view.Refresh();
            var stashPanel=view.transform.GetComponentsInChildren<RectTransform>(true).First(x=>x.name=="SharedStashPanel").gameObject;
            if(!view.StashVisible || !InventorySurfaceValid(stashPanel)) { Finish("FAIL: illustrated opaque stash"); yield break; }
            if(stashPanel.GetComponentsInChildren<InventoryCanvasSlot>().Length!=80) { Finish("FAIL: stash requires 40 visible slots on each side"); yield break; }
            Capture(view,"Stash");
            interaction.UISelect(79); view.Refresh(); Capture(view,"StashRightSelection");
            interaction.UISelect(0); view.Refresh();
            if(!host)
            {
                interaction.UIActivate(local.State.FirstSlot("foundation-warrior-armor"));
                while(GameSession.Instance.World.SharedStash.Count("foundation-warrior-armor")!=1) yield return null;
                int index=40+GameSession.Instance.World.SharedStash.FirstSlot("foundation-warrior-armor"); interaction.UISelect(index); view.Refresh(); Capture(view,"StashDeposit"); interaction.UIActivate(index);
                while(local.State.Count("foundation-warrior-armor")!=1 || GameSession.Instance.World.SharedStash.Count("foundation-warrior-armor")!=0) yield return null;
            }
            interaction.CloseMenu(); var ring=local.GetComponent<SpellRingMenu>();
            if(!ring.Open()) { Finish("FAIL: open styled spell ring"); yield break; } ring.Confirm(); yield return new WaitForSecondsRealtime(.3f);
            var spell=FindFirstObjectByType<SpellRingView>();
            if(!SurfaceValid(spell.transform.Find("InfoStrip").gameObject) || !spell.GetComponentsInChildren<Text>().Any(x=>x.text.Contains("Burn")))
            { Finish("FAIL: styled spell description or Burn details"); yield break; }
            Capture(spell,"SpellDescription"); ring.ToggleBook(); yield return new WaitForSecondsRealtime(.25f); Capture(spell,"SpellBook"); ring.Close();
            File.WriteAllText(Signal(host ? "host-ui-ready" : "client-ui-ready"),"ready");
            while(!File.Exists(Signal("host-ui-ready")) || !File.Exists(Signal("client-ui-ready"))) yield return null;
            if(host)
            {
                var world=GameSession.Instance.World; var restore=WorldSessionState.Restore(world.Capture()); world.DiscoverChestItem("interface-ui:cancel-marker");
                if(!net.BeginLoad(restore,NetworkCoop.FirstScene)) { Finish("FAIL: first vote start"); yield break; }
                while(net.VotePlayers.Count(p=>p.accepted)!=1) yield return null;
                yield return new WaitForSecondsRealtime(.4f);
                if(!net.VotePending || net.ChangingArea || net.VoteView.PlayerCount!=2 || net.VoteView.AcceptedCount!=1 || !net.VotePlayers.First(x=>x.accepted).name.StartsWith("Client UI"))
                { Finish("FAIL: host consensus checkmarks or early load"); yield break; }
                Capture(net.VoteView,"VoteOneAccepted"); net.AnswerVote(false);
                if(net.VotePending || net.VoteView.Visible || !world.IsChestItemDiscovered("interface-ui:cancel-marker")) { Finish("FAIL: rejection/cancel changed world"); yield break; }
                File.WriteAllText(Signal("vote-cancelled"),"ready");
                while(!File.Exists(Signal("client-saw-cancel"))) yield return null;
                if(!net.BeginLoad(restore,NetworkCoop.FirstScene)) { Finish("FAIL: second vote start"); yield break; }
                while(net.VotePlayers.Count(p=>p.accepted)!=1) yield return null;
                yield return new WaitForSecondsRealtime(.25f); net.VoteView.Accept();
            }
            else
            {
                while(!net.VotePending) yield return null; yield return new WaitForSecondsRealtime(.2f);
                if(net.VoteView.PlayerCount!=2 || net.VoteView.AcceptedCount!=0) { Finish("FAIL: initial client roster"); yield break; }
                net.VoteView.Accept();
                while(net.VotePlayers.Count(p=>p.accepted)!=1) yield return null;
                yield return new WaitForSecondsRealtime(.2f);
                if(!net.VoteAnswered || net.VoteView.AcceptedCount!=1 || net.ChangingArea) { Finish("FAIL: confirmed client checkmark"); yield break; }
                Capture(net.VoteView,"VoteOneAccepted");
                while(net.VotePending) yield return null;
                while(!File.Exists(Signal("vote-cancelled"))) yield return null;
                if(net.VoteView.Visible) { Finish("FAIL: client vote cancellation"); yield break; }
                File.WriteAllText(Signal("client-saw-cancel"),"ready");
                while(!net.VotePending) yield return null; yield return new WaitForSecondsRealtime(.2f);
                if(net.VoteView.AcceptedCount!=0) { Finish("FAIL: stale accepted mark in new vote"); yield break; }
                net.VoteView.Accept();
            }
            while(net.VotePending || net.ChangingArea || net.LocalCharacter==null) yield return null;
            if(GameSession.Instance.World.IsChestItemDiscovered("interface-ui:cancel-marker")) { Finish("FAIL: unanimous load not restored"); yield break; }
            File.WriteAllText(Signal(host ? "host-loaded" : "client-loaded"),"ready");
            while(!File.Exists(Signal("host-loaded")) || !File.Exists(Signal("client-loaded"))) yield return null;
            Finish("PASS: illustrated opaque inventory/stash, rendered gold selection in bag/equipment/both stash halves, equipment and stash transfers, Burn description, host-confirmed consent, cancel/reset/unanimous load.");
        }
    }
}
