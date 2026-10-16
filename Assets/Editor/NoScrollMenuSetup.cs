using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using R=SecretsReborn.Editor.ResponsiveMainMenuSetup;

namespace SecretsReborn.Editor
{
    public static class NoScrollMenuSetup
    {
        const string Folder="Assets/Resources/MenuUI/QuietHome/Sprites/";
        static readonly string[] LobbyArt={"LobbySelect","LobbySelected","LobbyCreate","LobbyReady","LobbyNotReady","LobbyRelease","LobbyStart","LobbyLeave","LobbyHostStarts","LobbyWaiting","LobbyProtected","LobbyConnected","LobbyNext","LobbyBack","LobbyConfirm","LobbyCancel"};
        [MenuItem("SecretsReborn/Frontend/Prepare spacious menu and lobby")]
        public static void Setup()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play first.");
            for(int i=0;i<SceneManager.sceneCount;i++) if(SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Unsaved scene changes; save first.");
            var scene=SceneManager.GetSceneByPath(NetworkCoop.MenuScene);
            if(!scene.isLoaded) throw new InvalidOperationException("Open MainMenu first.");
            var menu=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<MainMenu>(true)).Single();
            var home=menu.Home;
            if(home.GetComponent<LobbyCanvasView>()!=null)
            {
                if(home.GetComponentsInChildren<ScrollRect>(true).Length>0)
                {
                    RemoveLayouts(home.gameObject);
                    foreach(var action in home.GetComponentsInChildren<LobbyMenuAction>(true).Where(x=>x.Action==LobbyAction.Delete))
                        R.Position(action.transform,new Vector2(0,-165),new Vector2(145,30));
                    RenamePageContents(home.gameObject);
                    EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                }
                File.WriteAllText("Temp/NoScrollMenuSetup.txt","PASS: spacious layouts preserved; no scroll components remain."); return;
            }
            R.ImportAtlas("LobbyButtons.png",LobbyArt);
            var panel=(RectTransform)home.GetComponentInChildren<MenuFrameGraphic>(true).transform.parent;
            var scaler=home.GetComponent<CanvasScaler>(); scaler.referenceResolution=new Vector2(1280,800); scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            Frame(panel); panel.sizeDelta=new Vector2(640,660);
            Top(panel.Find("Game title"),new Vector2(0,-52),new Vector2(580,44));
            Bottom(panel.Find("Connection message"),new Vector2(0,27),new Vector2(600,28));
            var pages=home.GetComponentsInChildren<MainMenuPageView>(true);
            MenuChoiceField[] choices=null;
            foreach(var page in pages)
            {
                RemoveLayouts(page.gameObject);
                var root=page.transform; var content=root.Find("Scroll area/Viewport/Content");
                StretchExisting(root.Find("Scroll area")); StretchExisting(root.Find("Scroll area/Viewport")); StretchExisting(content);
                var viewportImage=content.parent.GetComponent<Image>(); if(viewportImage!=null) viewportImage.raycastTarget=false;
                var bar=root.Find("Scroll area/Scrollbar"); if(bar!=null) UnityEngine.Object.DestroyImmediate(bar.gameObject);
                Vector2 size=page.Page==MainMenu.Page.SoloCreate ? new Vector2(1040,700)
                    : page.Page==MainMenu.Page.SoloLoad || page.Page==MainMenu.Page.HostLoad ? new Vector2(1120,680)
                    : page.Page==MainMenu.Page.Trash ? new Vector2(1120,720)
                    : page.Page==MainMenu.Page.Host || page.Page==MainMenu.Page.Join ? new Vector2(800,650) : new Vector2(720,620);
                page.ConfigureLayout(size); EditorUtility.SetDirty(page);
                Top(root.Find("Page heading"),new Vector2(0,-100),new Vector2(size.x-80,30));
                var back=Find(root,FrontendAction.Back); Bottom(back.transform,new Vector2(0,62),new Vector2(260,58));
                switch(page.Page)
                {
                    case MainMenu.Page.Solo:
                    case MainMenu.Page.Multiplayer:
                        var links=root.GetComponentsInChildren<MainMenuPageAction>(true).Where(x=>x.Action==FrontendAction.Navigate).ToArray();
                        for(int i=0;i<links.Length;i++) R.Position(links[i].transform,new Vector2(0,55-i*115),new Vector2(420,82)); break;
                    case MainMenu.Page.Host:
                        Input(root.GetComponentInChildren<InputField>(true),new Vector2(0,115),480);
                        R.Position(Find(root,FrontendAction.HostNew).transform,new Vector2(0,-5),new Vector2(420,82));
                        R.Position(Find(root,FrontendAction.Navigate).transform,new Vector2(0,-115),new Vector2(420,82)); break;
                    case MainMenu.Page.Join:
                        var inputs=root.GetComponentsInChildren<InputField>(true); Input(inputs[0],new Vector2(0,120),480); Input(inputs[1],new Vector2(0,10),480);
                        R.Position(Find(root,FrontendAction.Connect).transform,new Vector2(0,-120),new Vector2(420,82)); break;
                    case MainMenu.Page.SoloCreate:
                        choices=Creator(root); break;
                    case MainMenu.Page.SoloLoad:
                    case MainMenu.Page.HostLoad:
                        var rows=root.GetComponentsInChildren<MainMenuSlotRow>(true);
                        for(int i=0;i<rows.Length;i++)
                        {
                            R.Position(rows[i].transform,new Vector2((i-1)*345,0),new Vector2(318,360));
                            var text=rows[i].GetComponentInChildren<Text>(); R.Position(text.transform,new Vector2(0,85),new Vector2(282,155)); text.fontSize=16; text.verticalOverflow=VerticalWrapMode.Truncate;
                            var actions=rows[i].transform.Find("Actions"); R.Position(actions,new Vector2(0,-100),new Vector2(282,150));
                            R.Position(Find(actions,FrontendAction.LoadSlot).transform,new Vector2(0,40),new Vector2(260,62));
                            R.Position(Find(actions,FrontendAction.AskDelete).transform,new Vector2(0,-40),new Vector2(260,62));
                        }
                        Bottom(back.transform,new Vector2(-250,62),new Vector2(260,58));
                        var trash=Find(root,FrontendAction.OpenTrash); trash.transform.SetParent(root,false); Bottom(trash.transform,new Vector2(250,62),new Vector2(300,66)); break;
                    case MainMenu.Page.Trash:
                        var entries=root.GetComponentsInChildren<MainMenuTrashRow>(true);
                        for(int i=0;i<entries.Length;i++)
                        {
                            if(i>=MainMenu.TrashRowsPerPage) { UnityEngine.Object.DestroyImmediate(entries[i].gameObject); continue; }
                            R.Position(entries[i].transform,new Vector2(i%2==0 ? -247 : 247,i<2 ? 100 : -110),new Vector2(460,190));
                            var text=entries[i].GetComponentInChildren<Text>(); R.Position(text.transform,new Vector2(0,30),new Vector2(420,110)); text.fontSize=13; text.verticalOverflow=VerticalWrapMode.Truncate;
                            R.Position(Find(entries[i].transform,FrontendAction.RestoreTrash).transform,new Vector2(0,-60),new Vector2(280,58));
                        }
                        foreach(var text in content.GetComponentsInChildren<Text>(true).Where(x=>x.transform.parent==content))
                        { R.Position(text.transform,new Vector2(0,text.text.StartsWith("Seite") ? -236 : text.text.StartsWith("Papierkorb") ? 0 : 220),new Vector2(900,28)); text.alignment=TextAnchor.MiddleCenter; }
                        var paging=content.Find("Pagination"); R.Position(paging,new Vector2(110,-285),new Vector2(580,60));
                        R.Position(Find(paging,FrontendAction.PreviousTrash).transform,new Vector2(-150,0),new Vector2(250,58));
                        R.Position(Find(paging,FrontendAction.NextTrash).transform,new Vector2(150,0),new Vector2(250,58));
                        Bottom(back.transform,new Vector2(-385,46),new Vector2(240,56)); break;
                    case MainMenu.Page.ConfirmDelete:
                        R.Position(content.GetComponentInChildren<Text>().transform,new Vector2(0,70),new Vector2(600,160));
                        R.Position(Find(root,FrontendAction.ConfirmDelete).transform,new Vector2(0,-50),new Vector2(360,76));
                        R.Position(Find(root,FrontendAction.CancelDelete).transform,new Vector2(0,-150),new Vector2(360,76)); back.gameObject.SetActive(false); break;
                }
            }
            var config=new SerializedObject(menu.Pages); var trashField=config.FindProperty("trash");
            var visibleTrash=home.GetComponentsInChildren<MainMenuTrashRow>(true); trashField.arraySize=visibleTrash.Length;
            for(int i=0;i<visibleTrash.Length;i++) trashField.GetArrayElementAtIndex(i).objectReferenceValue=visibleTrash[i];
            config.ApplyModifiedPropertiesWithoutUndo(); menu.Pages.ConfigureLayout(panel,new Vector2(640,660),choices); EditorUtility.SetDirty(menu.Pages);
            for(int i=0;i<home.Buttons.Length;i++) R.Position(home.Buttons[i].transform,new Vector2(0,145-i*100),new Vector2(430,80));
            BuildLobby(home,pages.Single(x=>x.Page==MainMenu.Page.SoloCreate).gameObject,pages.Single(x=>x.Page==MainMenu.Page.ConfirmDelete).gameObject);
            RenamePageContents(home.gameObject);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            File.WriteAllText("Temp/NoScrollMenuSetup.txt","PASS: all main-menu pages fit without scroll, new quiet gold geometry frame, cycle selectors, three save cards, paged trash, scene-authored lobby/cards/creator/delete confirmation.");
        }
        static void RemoveLayouts(GameObject root)
        {
            foreach(var focus in root.GetComponentsInChildren<MenuScrollFocus>(true)) UnityEngine.Object.DestroyImmediate(focus);
            foreach(var component in root.GetComponentsInChildren<Component>(true))
                if(component is ScrollRect || component is MenuScrollFocus || component is LayoutGroup || component is LayoutElement || component is ContentSizeFitter || component is RectMask2D)
                    UnityEngine.Object.DestroyImmediate(component);
        }
        static void RenamePageContents(GameObject root)
        {
            foreach(var node in root.GetComponentsInChildren<Transform>(true).Where(x=>x.name=="Scroll area"))
            {
                node.name="Page content"; var viewport=node.Find("Viewport"); if(viewport==null) continue;
                var content=viewport.Find("Content"); if(content!=null) content.name="Controls"; viewport.name="Layout";
            }
        }
        static void StretchExisting(Transform transform)
        { var rect=(RectTransform)transform; rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero; rect.localScale=Vector3.one; }
        static void Top(Transform transform,Vector2 position,Vector2 size)
        { R.Position(transform,position,size); var rect=(RectTransform)transform; rect.anchorMin=rect.anchorMax=new Vector2(.5f,1); }
        static void Bottom(Transform transform,Vector2 position,Vector2 size)
        { R.Position(transform,position,size); var rect=(RectTransform)transform; rect.anchorMin=rect.anchorMax=new Vector2(.5f,0); }
        static MainMenuPageAction Find(Transform root,FrontendAction action) => root.GetComponentsInChildren<MainMenuPageAction>(true).First(x=>x.Action==action);
        static Sprite Art(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Folder+name+".asset");
        static void Frame(RectTransform panel)
        {
            var frame=panel.GetComponentInChildren<MenuFrameGraphic>(true);
            GameObject obj=frame!=null ? frame.gameObject : R.Stretch("Quiet gold frame",panel);
            if(frame!=null) UnityEngine.Object.DestroyImmediate(frame);
            obj.name="Quiet gold frame"; StretchExisting(obj.transform);
            var graphic=obj.AddComponent<QuietMenuFrameGraphic>(); graphic.color=new Color(.78f,.65f,.34f,.9f); graphic.raycastTarget=false;
            var inlay=panel.Find("Plain translucent inlay");
            if(inlay==null) inlay=R.Stretch("Plain translucent inlay",panel).transform;
            StretchExisting(inlay); var rect=(RectTransform)inlay; rect.offsetMin=Vector2.one*10; rect.offsetMax=-Vector2.one*10;
            var image=inlay.GetComponent<Image>() ?? inlay.gameObject.AddComponent<Image>(); image.color=new Color(.018f,.038f,.03f,.86f); image.raycastTarget=false;
            inlay.SetAsFirstSibling(); obj.transform.SetSiblingIndex(1);
        }
        static void Input(InputField input,Vector2 position,float width)
        {
            R.Position(input.transform.parent,position,new Vector2(width,80));
            var label=input.transform.parent.GetComponentsInChildren<Text>(true).First(x=>x!=input.textComponent);
            R.Position(label.transform,new Vector2(0,28),new Vector2(width,26)); label.fontSize=14;
            R.Position(input.transform,new Vector2(0,-10),new Vector2(width,44));
        }
        static MenuChoiceField[] Creator(Transform root)
        {
            var content=root.Find("Scroll area/Viewport/Content"); var name=root.GetComponentInChildren<InputField>(true); Input(name,new Vector2(205,190),450);
            var appearance=content.Find("Appearance"); StretchExisting(appearance);
            var portrait=appearance.Find("Portrait"); R.Position(portrait,new Vector2(-285,-5),new Vector2(330,420));
            foreach(var layer in portrait.GetComponentsInChildren<RawImage>()) R.Position(layer.transform,Vector2.zero,new Vector2(320,320));
            var controls=appearance.Find("Look controls"); R.Position(controls,new Vector2(205,-35),new Vector2(450,330));
            var dropdowns=controls.GetComponentsInChildren<Dropdown>(true); var fields=new MenuChoiceField[5];
            string[] captions={"Körper","Hautton","Frisur","Haarfarbe","Augen"};
            string[][] values={new[]{"A","B"},CharacterCustomization.SkinNames,CharacterCustomization.HairStyles,CharacterPalette.HairNames,CharacterCustomization.EyeNames};
            for(int i=0;i<5;i++)
            {
                var obj=dropdowns[i].gameObject; UnityEngine.Object.DestroyImmediate(dropdowns[i]);
                while(obj.transform.childCount>0) UnityEngine.Object.DestroyImmediate(obj.transform.GetChild(0).gameObject);
                R.Position(obj.transform,new Vector2(0,132-i*66),new Vector2(450,54)); R.AddFieldBorder(obj);
                fields[i]=obj.AddComponent<MenuChoiceField>(); fields[i].targetGraphic=obj.GetComponent<Image>();
                var caption=R.Label("Caption",obj.transform,captions[i],13,new Vector2(-157,0),new Vector2(116,48)); caption.alignment=TextAnchor.MiddleLeft;
                var label=R.Label("Value",obj.transform,"",16,new Vector2(32,0),new Vector2(274,48)); label.alignment=TextAnchor.MiddleCenter;
                fields[i].Configure(label,values[i]);
                Arrow(obj.transform,"Previous",-88,fields[i].Previous); Arrow(obj.transform,"Next",205,fields[i].Next);
            }
            var back=Find(root,FrontendAction.Back); Bottom(back.transform,new Vector2(-280,62),new Vector2(270,60));
            var create=Find(root,FrontendAction.CreateSolo); create.transform.SetParent(root,false); Bottom(create.transform,new Vector2(205,62),new Vector2(430,64));
            return fields;
        }
        static void Arrow(Transform parent,string name,float x,UnityEngine.Events.UnityAction action)
        {
            var obj=R.Node(name,parent); R.Position(obj.transform,new Vector2(x,0),new Vector2(30,46));
            var image=obj.AddComponent<Image>(); image.color=new Color(.22f,.27f,.16f,.4f);
            var button=obj.AddComponent<Button>(); button.targetGraphic=image; button.navigation=new Navigation {mode=Navigation.Mode.None};
            UnityEventTools.AddPersistentListener(button.onClick,action);
            var text=R.Label("Arrow",obj.transform,name=="Previous" ? "‹" : "›",28,Vector2.zero,new Vector2(30,46)); text.alignment=TextAnchor.MiddleCenter;
        }
        static Button LobbyButton(Transform parent,string art,LobbyAction action,Vector2 position,Vector2 size,int index=0)
        {
            var obj=R.Node(art,parent); R.Position(obj.transform,position,size); var image=obj.AddComponent<Image>(); image.sprite=Art(art); image.preserveAspect=true;
            var button=obj.AddComponent<Button>(); button.targetGraphic=image; button.transition=Selectable.Transition.None;
            obj.AddComponent<MenuGraphicButton>(); var binding=obj.AddComponent<LobbyMenuAction>(); binding.Configure(action,index); UnityEventTools.AddPersistentListener(button.onClick,binding.Click); return button;
        }
        static Text Text(Transform parent,string name,string value,Vector2 position,Vector2 size,int font=15)
        { var text=R.Label(name,parent,value,font,position,size); text.alignment=TextAnchor.MiddleCenter; return text; }
        static MenuPortraitView Portrait(Transform parent,Vector2 position,Vector2 size)
        {
            var obj=R.Node("Portrait",parent); R.Position(obj.transform,position,size); var layers=new RawImage[6];
            for(int i=0;i<6;i++) { var image=R.Node("Layer "+i,obj.transform).AddComponent<RawImage>(); StretchExisting(image.transform); image.raycastTarget=false; layers[i]=image; }
            var preview=obj.AddComponent<MenuPortraitView>(); preview.Configure(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Coop/Player.prefab").GetComponent<CharacterAppearance>(),layers); return preview;
        }
        static void BuildLobby(MainMenuHomeView home,GameObject soloCreator,GameObject deletePage)
        {
            var fit=R.Stretch("Lobby safe area and proportional fit",home.transform);
            var panel=R.Node("Multiplayer lobby",fit.transform); R.Position(panel.transform,Vector2.zero,new Vector2(1120,720));
            fit.AddComponent<MenuViewportFit>().Configure((RectTransform)panel.transform); Frame((RectTransform)panel.transform);
            var title=Text(panel.transform,"Game title","SECRETS REBORN",Vector2.zero,new Vector2(1000,44),32); Top(title.transform,new Vector2(0,-50),new Vector2(1000,44));
            var heading=Text(panel.transform,"Page heading","GEMEINSAM INS ABENTEUER",Vector2.zero,new Vector2(1000,30),18); Top(heading.transform,new Vector2(0,-98),new Vector2(1000,30));
            var message=Text(panel.transform,"Connection message","",Vector2.zero,new Vector2(1000,30),13); Bottom(message.transform,new Vector2(0,28),new Vector2(1000,30));
            var selection=R.Stretch("Character selection",panel.transform); var cards=new LobbyCharacterCard[4];
            for(int i=0;i<4;i++)
            {
                var card=R.Node("Character "+(i+1),selection.transform); R.Position(card.transform,new Vector2((i-1.5f)*272,25),new Vector2(250,366)); card.AddComponent<Image>().color=new Color(.025f,.06f,.045f,.72f);
                var name=Text(card.transform,"Name","Freier Charakterplatz",new Vector2(0,153),new Vector2(230,40),19);
                var portrait=Portrait(card.transform,new Vector2(0,28),new Vector2(192,192));
                var state=Text(card.transform,"Ownership","",new Vector2(0,-76),new Vector2(230,40),13);
                var note=Text(card.transform,"Founder protection","",new Vector2(0,-155),new Vector2(230,25),11);
                var choose=LobbyButton(card.transform,"LobbySelect",LobbyAction.Select,new Vector2(0,-116),new Vector2(220,54),i);
                var create=LobbyButton(card.transform,"LobbyCreate",LobbyAction.CreateOpen,new Vector2(0,-70),new Vector2(220,54),i);
                var remove=LobbyButton(card.transform,"Delete",LobbyAction.Delete,new Vector2(0,-165),new Vector2(145,30),i);
                cards[i]=card.AddComponent<LobbyCharacterCard>(); cards[i].Configure(i,name,state,note,portrait,choose,create,remove,Art("LobbySelect"),Art("LobbySelected"));
            }
            var roster=Text(selection.transform,"Players","",new Vector2(0,-194),new Vector2(1050,35),14);
            var ready=LobbyButton(selection.transform,"LobbyReady",LobbyAction.Ready,new Vector2(-408,-280),new Vector2(250,60));
            var release=LobbyButton(selection.transform,"LobbyRelease",LobbyAction.Release,new Vector2(-136,-280),new Vector2(250,60));
            var start=LobbyButton(selection.transform,"LobbyStart",LobbyAction.Start,new Vector2(136,-280),new Vector2(250,60));
            var leave=LobbyButton(selection.transform,"LobbyLeave",LobbyAction.Leave,new Vector2(408,-280),new Vector2(250,60));
            var creator=UnityEngine.Object.Instantiate(soloCreator,panel.transform,false); creator.name="Lobby character creator"; StretchExisting(creator.transform); UnityEngine.Object.DestroyImmediate(creator.GetComponent<MainMenuPageView>()); UnityEngine.Object.DestroyImmediate(creator.transform.Find("Page heading").gameObject);
            var createButton=Rebind(creator,FrontendAction.CreateSolo,LobbyAction.CreateConfirm,"LobbyCreate"); Rebind(creator,FrontendAction.Back,LobbyAction.Cancel,"LobbyCancel");
            var confirm=UnityEngine.Object.Instantiate(deletePage,panel.transform,false); confirm.name="Lobby delete confirmation"; StretchExisting(confirm.transform); UnityEngine.Object.DestroyImmediate(confirm.GetComponent<MainMenuPageView>()); UnityEngine.Object.DestroyImmediate(confirm.transform.Find("Page heading").gameObject);
            Rebind(confirm,FrontendAction.ConfirmDelete,LobbyAction.DeleteConfirm,"LobbyConfirm"); Rebind(confirm,FrontendAction.CancelDelete,LobbyAction.Cancel,"LobbyCancel");
            foreach(var old in confirm.GetComponentsInChildren<MainMenuPageAction>(true)) { ClearButton(old.GetComponent<Button>()); UnityEngine.Object.DestroyImmediate(old); }
            var view=home.gameObject.AddComponent<LobbyCanvasView>(); view.Configure(home,panel,selection,creator,confirm,cards,heading,message,roster,confirm.GetComponentInChildren<Text>(true),
                creator.GetComponentInChildren<InputField>(true),creator.GetComponentsInChildren<MenuChoiceField>(true),creator.GetComponentInChildren<MenuPortraitView>(true),ready,release,start,leave,createButton,Art("LobbyReady"),Art("LobbyNotReady"),Art("LobbyStart"),Art("LobbyHostStarts"));
            creator.SetActive(false); confirm.SetActive(false); panel.SetActive(false);
        }
        static void ClearButton(Button button) { for(int i=button.onClick.GetPersistentEventCount()-1;i>=0;i--) UnityEventTools.RemovePersistentListener(button.onClick,i); }
        static Button Rebind(GameObject root,FrontendAction old,LobbyAction action,string art)
        {
            var binding=Find(root.transform,old); var obj=binding.gameObject; var button=obj.GetComponent<Button>(); ClearButton(button); UnityEngine.Object.DestroyImmediate(binding);
            var lobby=obj.AddComponent<LobbyMenuAction>(); lobby.Configure(action); UnityEventTools.AddPersistentListener(button.onClick,lobby.Click); button.image.sprite=Art(art); return button;
        }
    }
}
