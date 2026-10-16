using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace SecretsReborn
{
    // Explicit development-only UI check. Save operations use an isolated report folder.
    public sealed class MainMenuIntegrationDriver : MonoBehaviour
    {
        private string report,folder;
        private bool finished;
        private double deadline;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if(!Debug.isDebugBuild || Array.IndexOf(Environment.GetCommandLineArgs(),"--main-menu-report")<0) return;
            new GameObject("Main menu integration check").AddComponent<MainMenuIntegrationDriver>();
        }
        private void Awake()
        {
            var args=Environment.GetCommandLineArgs(); report=args[Array.IndexOf(args,"--main-menu-report")+1]; folder=Path.GetDirectoryName(report);
            Directory.CreateDirectory(folder); deadline=Time.realtimeSinceStartupAsDouble+45;
            SaveGameStore.TestSlotRoot=Path.Combine(folder,"TestSlots"); Directory.CreateDirectory(SaveGameStore.TestSlotRoot);
            Application.runInBackground=true; InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        }
        private void Update() { if(!finished && Time.realtimeSinceStartupAsDouble>deadline) Finish("FAIL: menu input timeout"); }
        private void Finish(string result) { if(finished) return; finished=true; File.WriteAllText(report,result); Debug.Log(result); Application.Quit(result.StartsWith("PASS") ? 0 : 1); }
        private IEnumerator Pad(Gamepad pad,GamepadButton button)
        {
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(button)); yield return new WaitForSecondsRealtime(.09f);
            InputSystem.QueueStateEvent(pad,new GamepadState()); yield return new WaitForSecondsRealtime(.12f);
        }
        private IEnumerator KeyPress(Keyboard keyboard,Key key)
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(key)); yield return new WaitForSecondsRealtime(.09f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return new WaitForSecondsRealtime(.12f);
        }
        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(1);
            var menu=FindFirstObjectByType<MainMenu>(); var view=FindFirstObjectByType<MainMenuHomeView>();
            if(menu==null || view==null || !menu.IsHome || view.Buttons.Length!=4) { Finish("FAIL: graphical home missing"); yield break; }
            if(menu.Home!=view || view.name.EndsWith("(Clone)")
                || FindObjectsByType<MainMenuHomeView>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length!=1
                || FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length!=1)
            { Finish("FAIL: scene-authored menu reference or duplicate UI/input"); yield break; }
            foreach(var button in view.Buttons) if(button.GetComponent<Image>().sprite==null || button.GetComponentsInChildren<Text>().Length!=0)
            { Finish("FAIL: button not a baked graphical label"); yield break; }
            var frame=view.GetComponentInChildren<QuietMenuFrameGraphic>();
            var inlay=frame.transform.parent.Find("Plain translucent inlay").GetComponent<Image>();
            if(inlay.sprite!=null || inlay.color.a<.5f || inlay.color.a>.9f) { Finish("FAIL: inlay is not plain/translucent"); yield break; }
            yield return new WaitForSecondsRealtime(.4f);
            if(EventSystem.current.currentSelectedGameObject!=view.Buttons[0].gameObject || view.Buttons[0].transform.localScale.x<1.04f)
            { Finish("FAIL: initial selection/scale"); yield break; }
            Capture(view,"Home.png");
            var pad=InputSystem.AddDevice<Gamepad>(); var keyboard=Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
            yield return Pad(pad,GamepadButton.DpadDown); yield return Pad(pad,GamepadButton.DpadDown);
            if(EventSystem.current.currentSelectedGameObject!=view.Buttons[2].gameObject) { Finish("FAIL: controller navigation"); yield break; }
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));
            yield return new WaitForSecondsRealtime(.12f); InputSystem.QueueStateEvent(pad,new GamepadState());
            if(!view.Busy || !menu.IsHome || view.Buttons[2].transform.localScale.x<1.08f) { Finish("FAIL: controller press pulse or immediate activation"); yield break; }
            Capture(view,"ButtonPulse.png");
            yield return new WaitForSecondsRealtime(.35f);
            if(menu.IsHome || view.HomeContentVisible || menu.Screen!=MainMenu.Page.Options) { Finish("FAIL: options activation"); yield break; }
            yield return Pad(pad,GamepadButton.East); yield return new WaitForSecondsRealtime(.2f);
            if(!menu.IsHome || !view.gameObject.activeSelf) { Finish("FAIL: controller back"); yield break; }
            yield return KeyPress(keyboard,Key.W);
            if(EventSystem.current.currentSelectedGameObject!=view.Buttons[1].gameObject) { Finish("FAIL: keyboard navigation"); yield break; }
            yield return KeyPress(keyboard,Key.Enter); yield return new WaitForSecondsRealtime(.3f);
            if(menu.IsHome) { Finish("FAIL: keyboard activation"); yield break; }
            yield return KeyPress(keyboard,Key.Escape); yield return new WaitForSecondsRealtime(.2f);
            if(!menu.IsHome) { Finish("FAIL: keyboard back"); yield break; }
            var pointer=new PointerEventData(EventSystem.current) { position=RectTransformUtility.WorldToScreenPoint(null,view.Buttons[0].transform.position),button=PointerEventData.InputButton.Left };
            var hits=new List<RaycastResult>(); view.GetComponent<GraphicRaycaster>().Raycast(pointer,hits);
            if(hits.Count==0 || hits[0].gameObject!=view.Buttons[0].gameObject) { Finish("FAIL: graphic mouse hit area"); yield break; }
            ExecuteEvents.Execute(view.Buttons[0].gameObject,pointer,ExecuteEvents.pointerEnterHandler); yield return new WaitForSecondsRealtime(.45f);
            if(view.Buttons[0].transform.localScale.x<1.04f || view.Buttons[1].transform.localScale.x>1.01f) { Finish("FAIL: hover grow / previous shrink"); yield break; }
            yield return KeyPress(keyboard,Key.DownArrow);
            if(EventSystem.current.currentSelectedGameObject!=view.Buttons[1].gameObject) { Finish("FAIL: mouse-to-keyboard focus switch"); yield break; }
            ExecuteEvents.Execute(view.Buttons[0].gameObject,pointer,ExecuteEvents.pointerMoveHandler); yield return new WaitForSecondsRealtime(.3f);
            if(EventSystem.current.currentSelectedGameObject!=view.Buttons[0].gameObject || view.Buttons[0].transform.localScale.x<1.04f)
            { Finish("FAIL: keyboard-to-mouse focus switch"); yield break; }
            ExecuteEvents.Execute(view.Buttons[0].gameObject,pointer,ExecuteEvents.pointerExitHandler); yield return new WaitForSecondsRealtime(.45f);
            if(view.Buttons[0].transform.localScale.x>1.01f) { Finish("FAIL: hover exit shrink"); yield break; }
            ExecuteEvents.Execute(view.Buttons[0].gameObject,pointer,ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(view.Buttons[0].gameObject,pointer,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(view.Buttons[0].gameObject,pointer,ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(view.Buttons[0].gameObject,pointer,ExecuteEvents.pointerClickHandler);
            yield return new WaitForSecondsRealtime(.4f);
            if(menu.IsHome) { Finish("FAIL: mouse activation"); yield break; }
            InputSystem.RemoveDevice(pad);
            if(menu.Pages==null || menu.Pages.GetComponentsInChildren<MainMenuPageView>(true).Length!=10) { Finish("FAIL: authored submenu pages missing"); yield break; }
            foreach(var kind in new[]{MainMenu.Page.Solo,MainMenu.Page.Multiplayer,MainMenu.Page.Host,MainMenu.Page.Join,MainMenu.Page.SoloCreate,MainMenu.Page.SoloLoad,MainMenu.Page.HostLoad,MainMenu.Page.Options})
            {
                menu.ChangePage(kind); yield return new WaitForSecondsRealtime(.2f); Canvas.ForceUpdateCanvases();
                if(menu.Pages.Shown!=kind || view.HomeContentVisible) { Finish("FAIL: submenu visibility "+kind); yield break; }
                foreach(var homeButton in view.Buttons) if(homeButton.gameObject.activeInHierarchy) { Finish("FAIL: home button overlaps submenu"); yield break; }
                if(!ControlsFit(view.gameObject)) { Finish("FAIL: controls require scrolling or overlap "+kind); yield break; }
                Capture(view,kind+".png");
            }
            menu.ChangePage(MainMenu.Page.SoloCreate); yield return new WaitForSecondsRealtime(.2f);
            var drops=menu.Pages.GetComponentsInChildren<MenuChoiceField>();
            if(drops.Length!=5) { Finish("FAIL: creator fields"); yield break; }
            drops[2].Next(); yield return null;
            if(menu.Creator.Value(2)!=1) { Finish("FAIL: creator field binding"); yield break; }
            EventSystem.current.SetSelectedGameObject(drops[2].gameObject);
            yield return KeyPress(keyboard,Key.RightArrow);
            if(menu.Creator.Value(2)!=2) { Finish("FAIL: creator keyboard choice"); yield break; }
            Capture(view,"CreatorChoices.png");
            yield return new WaitForSecondsRealtime(.3f);
            var create=Array.Find(menu.Pages.GetComponentsInChildren<MainMenuPageAction>(),x=>x.Action==FrontendAction.CreateSolo);
            EventSystem.current.SetSelectedGameObject(create.gameObject); yield return new WaitForSecondsRealtime(.3f);
            if(view.GetComponentsInChildren<ScrollRect>(true).Length!=0 || !ControlsFit(view.gameObject)) { Finish("FAIL: creator still scrolls"); yield break; }
            Capture(view,"CreatorComplete.png");
            menu.ChangePage(MainMenu.Page.SoloLoad); yield return new WaitForSecondsRealtime(.2f);
            string slot=menu.SlotFile(0); SaveGameStore.Save(new WorldSessionState("menu-ui-roundtrip"),slot);
            menu.ChangePage(MainMenu.Page.SoloLoad); yield return new WaitForSecondsRealtime(.2f);
            if(!menu.CanLoadSlot(0) || menu.CanLoadSlot(1)) { Finish("FAIL: slot availability"); yield break; }
            menu.AskDelete(0); yield return new WaitForSecondsRealtime(.2f); Capture(view,"DeleteConfirmation.png");
            if(menu.Screen!=MainMenu.Page.ConfirmDelete) { Finish("FAIL: delete confirmation"); yield break; }
            menu.CancelDelete(); if(!SaveGameStore.Exists(slot)) { Finish("FAIL: cancel deleted slot"); yield break; }
            menu.AskDelete(0); menu.ConfirmDelete(); menu.OpenTrash(); yield return new WaitForSecondsRealtime(.2f);
            if(menu.TrashCount!=1 || !menu.CanRestore(0)) { Finish("FAIL: trash view"); yield break; }
            Capture(view,"Trash.png"); menu.RestoreTrash(0); yield return new WaitForSecondsRealtime(.2f);
            if(menu.TrashCount!=0 || SaveGameStore.Load(slot).WorldId!="menu-ui-roundtrip") { Finish("FAIL: UI save roundtrip"); yield break; }
            menu.Back(); menu.ChangePage(MainMenu.Page.Join); yield return new WaitForSecondsRealtime(.2f);
            menu.Address="invalid-ip"; menu.Connect(); yield return new WaitForSecondsRealtime(.2f);
            if(menu.Message==null || !menu.Available) { Finish("FAIL: invalid IP validation"); yield break; }
            menu.ChangePage(MainMenu.Page.Home); yield return new WaitForSecondsRealtime(.3f); Canvas.ForceUpdateCanvases();
            var mesh=frame.canvasRenderer.GetMesh(); var vertices=mesh.vertices;
            if(vertices.Length<16) { Finish("FAIL: missing frame geometry"); yield break; }
            var bounds=new Vector3[4]; frame.rectTransform.GetWorldCorners(bounds);
            if(bounds[0].x<0 || bounds[0].y<0 || bounds[2].x>Screen.width || bounds[2].y>Screen.height) { Finish("FAIL: menu outside viewport"); yield break; }
            Finish("PASS "+Screen.width+"x"+Screen.height+": authored home and ten subpages, quiet geometry frame, no scroll containers, all controls fit without overlap, keyboard/controller/mouse and pulse, inline creator choices, isolated save deletion/cancel/trash/restore, invalid IP validation.");
        }
        public static bool ControlsFit(GameObject root)
        {
            var items=root.GetComponentsInChildren<Selectable>(); var bounds=new List<Rect>();
            foreach(var item in items)
            {
                if(!item.isActiveAndEnabled || item.navigation.mode==Navigation.Mode.None) continue;
                var corners=new Vector3[4]; ((RectTransform)item.transform).GetWorldCorners(corners);
                // Ignore the deliberate focus/pulse scale when checking authored hit areas.
                Vector3 center=(corners[0]+corners[2])*.5f; Vector3 size=(corners[2]-corners[0])/Mathf.Max(.001f,item.transform.localScale.x);
                var rect=new Rect(center.x-size.x*.5f,center.y-size.y*.5f,size.x,size.y);
                if(rect.xMin<0 || rect.yMin<0 || rect.xMax>Screen.width+1 || rect.yMax>Screen.height+1) { Debug.LogError("Control outside screen: "+item.name); return false; }
                foreach(var other in bounds) if(rect.Overlaps(other)) { Debug.LogError("Overlapping control: "+item.name); return false; }
                bounds.Add(rect);
            }
            return true;
        }
        private void Capture(MainMenuHomeView view,string name)
            => CaptureTo(view,Path.Combine(folder,name));
        public static void CaptureTo(MainMenuHomeView view,string path)
        {
            var canvas=view.GetComponent<Canvas>(); var previousMode=canvas.renderMode; var previousCamera=canvas.worldCamera; float previousDistance=canvas.planeDistance;
            var obj=new GameObject("Menu verification camera"); var camera=obj.AddComponent<Camera>();
            var target=new RenderTexture(Screen.width,Screen.height,24); var pixels=new Texture2D(Screen.width,Screen.height,TextureFormat.RGBA32,false); var previous=RenderTexture.active;
            try
            {
                camera.enabled=false; camera.transform.position=new Vector3(0,0,-10000); camera.orthographic=true; camera.nearClipPlane=.1f; camera.farClipPlane=10;
                camera.targetTexture=target; canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=1;
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0); pixels.Apply(); File.WriteAllBytes(path,pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active=previous; canvas.renderMode=previousMode; canvas.worldCamera=previousCamera; canvas.planeDistance=previousDistance;
                camera.targetTexture=null; Destroy(obj); Destroy(pixels); target.Release(); Destroy(target); Canvas.ForceUpdateCanvases();
            }
        }
    }
}
