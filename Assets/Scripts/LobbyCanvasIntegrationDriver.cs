using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace SecretsReborn
{
    // Explicit, development-only two-process test. Its world is never saved.
    public sealed class LobbyCanvasIntegrationDriver : MonoBehaviour
    {
        bool host,finished;
        string report,folder;
        double deadline;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            var args=Environment.GetCommandLineArgs();
            if(!Debug.isDebugBuild || Array.IndexOf(args,"--lobby-ui-report")<0 || FindFirstObjectByType<LobbyCanvasIntegrationDriver>()!=null) return;
            var obj=new GameObject("Lobby Canvas integration check"); DontDestroyOnLoad(obj); obj.AddComponent<LobbyCanvasIntegrationDriver>();
        }
        void Awake()
        {
            var args=Environment.GetCommandLineArgs(); host=Array.IndexOf(args,"--lobby-ui-host")>=0;
            report=args[Array.IndexOf(args,"--lobby-ui-report")+1]; folder=Path.GetDirectoryName(report); Directory.CreateDirectory(folder);
            deadline=Time.realtimeSinceStartupAsDouble+100; Application.runInBackground=true;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        }
        void Update() { if(!finished && Time.realtimeSinceStartupAsDouble>deadline) Finish("FAIL: lobby UI timeout, "+NetworkCoop.Active?.Status); }
        void Finish(string result) { if(finished) return; finished=true; File.WriteAllText(report,result); Debug.Log(result); Application.Quit(result.StartsWith("PASS") ? 0 : 1); }
        IEnumerator Press(LobbyAction action,int index=-1)
        {
            var button=LobbyCanvasView.Active.GetComponentsInChildren<LobbyMenuAction>().FirstOrDefault(x=>x.Action==action && (index<0 || x.CardIndex==index) && x.GetComponent<Button>().IsInteractable());
            if(button==null) { Finish("FAIL: missing/interactable lobby action "+action+" "+index); yield break; }
            button.GetComponent<Button>().onClick.Invoke(); yield return new WaitForSecondsRealtime(.5f);
        }
        void Capture(string name) => MainMenuIntegrationDriver.CaptureTo(FindFirstObjectByType<MainMenuHomeView>(),Path.Combine(folder,(host ? "Host" : "Client")+name+".png"));
        IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(.7f);
            var net=NetworkCoop.Active;
            if(host)
            {
                var world=new WorldSessionState("canvas-lobby-"+Guid.NewGuid().ToString("N")); world.EnableMultiplayer();
                world.CreateWorldCharacter("Gründer"); world.CreateWorldCharacter("Reserve");
                if(!net.OpenLobby(true,null,"Host UI",world)) { Finish("FAIL: host lobby start"); yield break; }
                File.WriteAllText(Path.Combine(folder,"host-started"),"ready");
            }
            else
            {
                while(!File.Exists(Path.Combine(folder,"host-started"))) yield return null;
                if(!net.OpenLobby(false,"127.0.0.1","Client UI",resume:false)) { Finish("FAIL: client lobby start"); yield break; }
            }
            while(!net.LobbyConnectedLocally || net.Lobby.players.Length!=2) yield return null;
            yield return new WaitForSecondsRealtime(.3f);
            var view=LobbyCanvasView.Active;
            if(view==null || !view.Visible || !MainMenuIntegrationDriver.ControlsFit(view.gameObject) || view.GetComponentsInChildren<ScrollRect>(true).Length!=0)
            { Finish("FAIL: native lobby missing, overlapping or scrollable"); yield break; }
            Capture("Lobby");
            if(!host) while(!File.Exists(Path.Combine(folder,"host-created"))) yield return null;
            if(host)
            {
                var action=view.GetComponentsInChildren<LobbyMenuAction>().First(x=>x.Action==LobbyAction.CreateOpen && x.GetComponent<Button>().IsInteractable());
                var pad=InputSystem.AddDevice<Gamepad>();
                var home=FindFirstObjectByType<MainMenuHomeView>(); home.UsePointer();
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.East)); yield return new WaitForSecondsRealtime(.1f);
                InputSystem.QueueStateEvent(pad,new GamepadState()); yield return new WaitForSecondsRealtime(.25f);
                if(!net.LobbyActive || home.PointerMode || EventSystem.current.currentSelectedGameObject?.GetComponent<LobbyMenuAction>()?.Action!=LobbyAction.Leave)
                { Finish("FAIL: controller back focus or unexpected lobby exit"); yield break; }
                EventSystem.current.SetSelectedGameObject(action.gameObject);
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South)); yield return new WaitForSecondsRealtime(.1f);
                InputSystem.QueueStateEvent(pad,new GamepadState()); yield return new WaitForSecondsRealtime(.5f); InputSystem.RemoveDevice(pad);
            }
            else { yield return Press(LobbyAction.CreateOpen); if(finished) yield break; }
            if(!net.LobbyCreating || !MainMenuIntegrationDriver.ControlsFit(view.gameObject)) { Finish("FAIL: inline lobby creator"); yield break; }
            var input=view.CurrentPage.GetComponentInChildren<InputField>(); input.text=host ? "Hostfigur" : "Clientfigur";
            var fields=view.CurrentPage.GetComponentsInChildren<MenuChoiceField>(); fields[2].Next(); fields[3].Next();
            Capture("Creator");
            if(host)
            {
                yield return Press(LobbyAction.Cancel); if(net.LobbyCreating || net.Lobby.characters.Length!=2) { Finish("FAIL: creator cancel"); yield break; }
                yield return Press(LobbyAction.CreateOpen); input=view.CurrentPage.GetComponentInChildren<InputField>(); input.text="Hostfigur";
            }
            yield return Press(LobbyAction.CreateConfirm); if(finished) yield break;
            while(string.IsNullOrEmpty(net.LobbySelection)) yield return null;
            string own=net.LobbySelection;
            if(host)
            {
                yield return Press(LobbyAction.Release); yield return Press(LobbyAction.Select,0);
                if(net.LobbySelection!=net.Lobby.founder) { Finish("FAIL: select founder"); yield break; }
                net.RequestLobbyDelete(net.Lobby.founder);
                if(net.LobbyPendingDeletion!=null) { Finish("FAIL: founder delete guard"); yield break; }
                File.WriteAllText(Path.Combine(folder,"host-created"),"ready");
                while(net.Lobby.characters.Length!=4) yield return null;
                Capture("FourCharacters");
                yield return Press(LobbyAction.Delete,1); if(net.LobbyPendingDeletion==null) { Finish("FAIL: delete confirmation missing"); yield break; }
                Capture("DeleteConfirmation"); yield return Press(LobbyAction.Cancel);
                if(net.Lobby.characters.Length!=4) { Finish("FAIL: cancel deleted figure"); yield break; }
                yield return Press(LobbyAction.Delete,1); yield return Press(LobbyAction.DeleteConfirm);
                if(net.Lobby.characters.Length!=3 || net.Lobby.characters.All(c=>c.id!=net.Lobby.founder)) { Finish("FAIL: delete non-founder"); yield break; }
                yield return Press(LobbyAction.Ready);
                File.WriteAllText(Path.Combine(folder,"host-ready"),"ready");
                while(!net.LobbyCanStart) yield return null;
                Capture("BothReady"); yield return Press(LobbyAction.Start);
            }
            else
            {
                while(!File.Exists(Path.Combine(folder,"host-ready"))) yield return null;
                net.ChooseLobbyCharacter(net.Lobby.founder); yield return new WaitForSecondsRealtime(.7f);
                if(net.LobbySelection!=own) { Finish("FAIL: client stole selected figure"); yield break; }
                yield return Press(LobbyAction.Ready);
                while(!net.LobbyReady) yield return null;
                Capture("Ready");
            }
            while(SceneManager.GetActiveScene().path!=NetworkCoop.FirstScene || net.LobbyActive || net.ChangingArea) yield return null;
            yield return new WaitForSecondsRealtime(.7f);
            Finish("PASS: "+(host ? "host" : "client")+" native lobby, four character cards without scrolling/overlap, creator and cancel, network selection/ownership, founder protection, delete/cancel/confirm, ready and shared adventure start.");
        }
    }
}
