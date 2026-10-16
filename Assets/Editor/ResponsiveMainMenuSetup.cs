using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SecretsReborn.Editor
{
    [InitializeOnLoad] public static class ResponsiveMainMenuSetup
    {
        const string Folder="Assets/Resources/MenuUI/QuietHome";
        static readonly string[] Art={"StartNew","LoadGame","Back","HostGame","JoinGame","NewGame","Connect","CreateSolo","Delete","Trash","Restore","Confirm","Cancel","Next","Previous","Blank"};
        static readonly Color Ink=new Color(.025f,.06f,.045f,.9f),Gold=new Color(.85f,.72f,.4f,.75f);
        static ResponsiveMainMenuSetup() => EditorApplication.update+=Requested;
        static void Requested()
        {
            const string request="Temp/SetupResponsiveMenu.request";
            if(!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if(typeof(MainMenuHomeView).GetField("homeContent",BindingFlags.Instance|BindingFlags.NonPublic)==null
                || typeof(MainMenu).GetField("pages",BindingFlags.Instance|BindingFlags.NonPublic)==null)
            {
                UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation(UnityEditor.Compilation.RequestScriptCompilationOptions.CleanBuildCache);
                EditorUtility.RequestScriptReload(); return;
            }
            File.Delete(request);
            try { Setup(); }
            catch(Exception error) { File.WriteAllText("Temp/ResponsiveMenuReport.txt","FAIL: "+error); Debug.LogException(error); }
        }
        [MenuItem("SecretsReborn/Frontend/Prepare responsive menu pages")]
        public static void Setup()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play first.");
            for(int i=0;i<SceneManager.sceneCount;i++) if(SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Open scene has unsaved changes; save first.");
            ImportAtlas();
            var scene=SceneManager.GetSceneByPath(NetworkCoop.MenuScene); bool opened=!scene.isLoaded;
            if(opened) scene=EditorSceneManager.OpenScene(NetworkCoop.MenuScene,OpenSceneMode.Additive);
            try
            {
                var menu=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<MainMenu>(true)).Single();
                var home=menu.Home;
                bool unpacked=PrefabUtility.IsPartOfPrefabInstance(home);
                if(unpacked) PrefabUtility.UnpackPrefabInstance(home.gameObject,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                if(home.GetComponent<MainMenuPagesView>()!=null)
                {
                    if(unpacked)
                    {
                        var existingPanel=home.transform.Find("Menu frame");
                        var fit=home.GetComponentInChildren<MenuViewportFit>(true);
                        if(existingPanel!=null) existingPanel.SetParent(fit.transform,true);
                        var homeRoot=home.GetComponentInChildren<MainMenuPageView>(true).transform.parent.Find("Home");
                        foreach(var button in home.Buttons) button.transform.SetParent(homeRoot,false);
                        foreach(var outline in home.GetComponentsInChildren<Outline>(true))
                        { var obj=outline.gameObject; UnityEngine.Object.DestroyImmediate(outline); AddFieldBorder(obj); }
                        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
                    }
                    File.WriteAllText("Temp/ResponsiveMenuReport.txt","PASS: responsive scene hierarchy and proportional fit preserved."); return;
                }
                var panel=(RectTransform)home.transform.Find("Menu frame");
                var viewport=Stretch("Safe area and proportional fit",home.transform);
                panel.SetParent(viewport.transform,true); viewport.AddComponent<MenuViewportFit>().Configure(panel);
                var image=panel.Find("Original forest frame").GetComponent<Image>();
                var source=image.sprite; var tint=image.color; float corner=source.border.x/image.pixelsPerUnitMultiplier;
                UnityEngine.Object.DestroyImmediate(image);
                var frame=panel.Find("Original forest frame").gameObject.AddComponent<MenuFrameGraphic>(); frame.Configure(source,corner); frame.color=tint;
                var scaler=home.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution=new Vector2(1040,600); scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
                var pagesRoot=Stretch("Pages",panel);
                var homePage=Stretch("Home",pagesRoot.transform);
                foreach(var button in home.Buttons) button.transform.SetParent(homePage.transform,false);
                home.ConfigureContent(homePage); PrefabUtility.RecordPrefabInstancePropertyModifications(home);
                var view=home.gameObject.AddComponent<MainMenuPagesView>(); menu.ConfigurePages(view); EditorUtility.SetDirty(menu);
                var screens=new List<MainMenuPageView>(); var rows=new List<MainMenuSlotRow>(); var trashRows=new List<MainMenuTrashRow>();
                InputField hero=null,host=null,guest=null,ip=null; Dropdown[] fields=null; MenuPortraitView portrait=null;
                Text deletion=null,trashPage=null,empty=null; Button create=null,previous=null,next=null;
                foreach(MainMenu.Page kind in Enum.GetValues(typeof(MainMenu.Page)))
                {
                    if(kind==MainMenu.Page.Home) continue;
                    string heading=kind==MainMenu.Page.Solo ? "SINGLEPLAYER" : kind==MainMenu.Page.Multiplayer ? "MULTIPLAYER" : kind==MainMenu.Page.SoloCreate ? "DEINE NEUE FIGUR"
                        : kind==MainMenu.Page.Host ? "HOST GAME" : kind==MainMenu.Page.Join ? "JOIN GAME" : kind==MainMenu.Page.Options ? "OPTIONEN"
                        : kind==MainMenu.Page.Trash ? "PAPIERKORB" : kind==MainMenu.Page.ConfirmDelete ? "LÖSCHEN BESTÄTIGEN" : "LOAD GAME";
                    var root=Stretch(kind.ToString(),pagesRoot.transform); root.SetActive(false);
                    Label("Page heading",root.transform,heading,18,new Vector2(0,138),new Vector2(460,28)).alignment=TextAnchor.MiddleCenter;
                    var scroll=Scroll(root.transform); var content=scroll.content;
                    var page=root.AddComponent<MainMenuPageView>(); page.Configure(kind,scroll); screens.Add(page);
                    var back=Button(menu,root.transform,"Back",FrontendAction.Back); Position(back.transform,new Vector2(0,-210),new Vector2(290,52));
                    switch(kind)
                    {
                        case MainMenu.Page.Solo:
                            Button(menu,content,"StartNew",FrontendAction.Navigate,MainMenu.Page.SoloCreate);
                            Button(menu,content,"LoadGame",FrontendAction.Navigate,MainMenu.Page.SoloLoad); break;
                        case MainMenu.Page.Multiplayer:
                            Button(menu,content,"HostGame",FrontendAction.Navigate,MainMenu.Page.Host);
                            Button(menu,content,"JoinGame",FrontendAction.Navigate,MainMenu.Page.Join); break;
                        case MainMenu.Page.Host:
                            host=Input(content,"Spielername",24); Button(menu,content,"NewGame",FrontendAction.HostNew);
                            Button(menu,content,"LoadGame",FrontendAction.Navigate,MainMenu.Page.HostLoad); break;
                        case MainMenu.Page.Join:
                            guest=Input(content,"Spielername",24); ip=Input(content,"Host-IP · UDP 7777",45); Button(menu,content,"Connect",FrontendAction.Connect); break;
                        case MainMenu.Page.SoloCreate:
                            hero=Input(content,"Name",24);
                            var appearance=Horizontal("Appearance",content,260);
                            var preview=Node("Portrait",appearance.transform); Layout(preview,160,260); var previewBox=preview.AddComponent<Image>(); previewBox.color=new Color(.02f,.04f,.03f,.4f); previewBox.raycastTarget=false;
                            var images=new RawImage[6];
                            for(int i=0;i<6;i++) { var layer=Node("Layer "+i,preview.transform).AddComponent<RawImage>(); Position(layer.transform,Vector2.zero,new Vector2(160,160)); layer.raycastTarget=false; images[i]=layer; }
                            portrait=preview.AddComponent<MenuPortraitView>();
                            portrait.Configure(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Coop/Player.prefab").GetComponent<CharacterAppearance>(),images);
                            var controls=Vertical("Look controls",appearance.transform,0); Layout(controls,220,260);
                            fields=new Dropdown[5]; string[] captions={"Körper","Hautton","Frisur","Haarfarbe","Augen"};
                            string[][] choices={new[]{"A","B"},CharacterCustomization.SkinNames,CharacterCustomization.HairStyles,CharacterPalette.HairNames,CharacterCustomization.EyeNames};
                            for(int i=0;i<5;i++) fields[i]=Drop(controls.transform,captions[i],choices[i]);
                            create=Button(menu,content,"CreateSolo",FrontendAction.CreateSolo).Button; break;
                        case MainMenu.Page.SoloLoad:
                        case MainMenu.Page.HostLoad:
                            for(int i=0;i<3;i++)
                            {
                                var row=Vertical("Slot "+(i+1),content,8); Card(row);
                                var text=BodyText(row.transform,"Slot "+(i+1)+"\nLeer");
                                var actions=Horizontal("Actions",row.transform,58);
                                var load=Button(menu,actions.transform,"LoadGame",FrontendAction.LoadSlot,row:i);
                                var remove=Button(menu,actions.transform,"Delete",FrontendAction.AskDelete,row:i);
                                var binding=row.AddComponent<MainMenuSlotRow>(); binding.Configure(i,text,load.Button,remove.Button); rows.Add(binding);
                            }
                            Button(menu,content,"Trash",FrontendAction.OpenTrash); break;
                        case MainMenu.Page.ConfirmDelete:
                            deletion=BodyText(content,"In den Papierkorb verschieben?");
                            Button(menu,content,"Confirm",FrontendAction.ConfirmDelete); Button(menu,content,"Cancel",FrontendAction.CancelDelete); break;
                        case MainMenu.Page.Trash:
                            BodyText(content,"Wiederherstellung nur in einen freien ursprünglichen Slot.");
                            empty=BodyText(content,"Papierkorb leer.");
                            for(int i=0;i<MainMenu.TrashRowsPerPage;i++)
                            {
                                var row=Vertical("Trash entry "+(i+1),content,8); Card(row);
                                var text=BodyText(row.transform,"Spielstand"); var restore=Button(menu,row.transform,"Restore",FrontendAction.RestoreTrash,row:i);
                                var binding=row.AddComponent<MainMenuTrashRow>(); binding.Configure(i,text,restore.Button); trashRows.Add(binding);
                            }
                            trashPage=BodyText(content,"Seite 1 / 1"); trashPage.alignment=TextAnchor.MiddleCenter;
                            var paging=Horizontal("Pagination",content,58);
                            previous=Button(menu,paging.transform,"Previous",FrontendAction.PreviousTrash).Button;
                            next=Button(menu,paging.transform,"Next",FrontendAction.NextTrash).Button; break;
                    }
                }
                view.Configure(screens.ToArray(),hero,host,guest,ip,fields,portrait,rows.ToArray(),trashRows.ToArray(),deletion,trashPage,empty,create,previous,next);
                var status=panel.Find("Connection message").GetComponent<Text>(); status.resizeTextForBestFit=true; status.resizeTextMinSize=10; status.resizeTextMaxSize=13;
                PrefabUtility.RecordPrefabInstancePropertyModifications(status);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
                if(!opened) { Selection.activeGameObject=pagesRoot; EditorGUIUtility.PingObject(pagesRoot); }
                File.WriteAllText("Temp/ResponsiveMenuReport.txt","PASS: all ten submenu screens authored in MainMenu.unity, fixed-aspect ornament frame, safe-area proportional fit, common graphic buttons, forms/portrait/slots/trash/confirmation, scroll focus and persistent actions.");
            }
            finally { if(opened) EditorSceneManager.CloseScene(scene,true); }
        }
        internal static void ImportAtlas(string filename="SubmenuButtons.png",string[] names=null)
        {
            names=names ?? Art;
            string path=Folder+"/"+filename; var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            if(importer==null) { AssetDatabase.Refresh(); importer=(TextureImporter)AssetImporter.GetAtPath(path); }
            importer.textureType=TextureImporterType.Default; importer.mipmapEnabled=false; importer.isReadable=true; importer.alphaIsTransparency=true;
            importer.filterMode=FilterMode.Bilinear; importer.textureCompression=TextureImporterCompression.Uncompressed; importer.maxTextureSize=2048; importer.SaveAndReimport();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path); var pixels=texture.GetPixels32(); int width=texture.width,height=texture.height;
            var bands=new List<Vector2Int>(); int start=-1;
            for(int y=height-1;y>=0;y--)
            {
                int count=0; for(int x=0;x<width;x++) if(pixels[y*width+x].a>80) count++;
                bool occupied=count>width*.2f;
                if(occupied && start<0) start=y;
                if(!occupied && start>=0) { bands.Add(new Vector2Int(Mathf.Max(0,y-4),Mathf.Min(height-1,start+4))); start=-1; }
            }
            if(bands.Count!=8) throw new InvalidOperationException("Expected eight isolated atlas rows, found "+bands.Count);
            var rects=new Rect[16];
            for(int row=0;row<8;row++) for(int column=0;column<2;column++)
            {
                int left=width,right=-1,low=bands[row].x,high=bands[row].y;
                for(int y=low;y<=high;y++) for(int x=column*width/2;x<(column+1)*width/2;x++) if(pixels[y*width+x].a>80) { left=Mathf.Min(left,x); right=Mathf.Max(right,x); }
                if(right<left) throw new InvalidOperationException("Atlas cell empty");
                left=Mathf.Max(column*width/2,left-4); right=Mathf.Min((column+1)*width/2-1,right+4); rects[row*2+column]=new Rect(left,low,right-left+1,high-low+1);
            }
            importer.isReadable=false; importer.SaveAndReimport(); texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            for(int i=0;i<names.Length;i++)
            {
                string asset=Folder+"/Sprites/"+names[i]+".asset"; if(AssetDatabase.LoadAssetAtPath<Sprite>(asset)!=null) continue;
                var sprite=Sprite.Create(texture,rects[i],new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect); sprite.name=names[i]; AssetDatabase.CreateAsset(sprite,asset);
            }
        }
        internal static GameObject Node(string name,Transform parent)
        { var obj=new GameObject(name,typeof(RectTransform)); obj.transform.SetParent(parent,false); return obj; }
        internal static GameObject Stretch(string name,Transform parent)
        { var obj=Node(name,parent); var r=(RectTransform)obj.transform; r.anchorMin=Vector2.zero; r.anchorMax=Vector2.one; r.offsetMin=r.offsetMax=Vector2.zero; return obj; }
        internal static void Position(Transform transform,Vector2 position,Vector2 size)
        { var r=(RectTransform)transform; r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f); r.anchoredPosition=position; r.sizeDelta=size; }
        static void Layout(GameObject obj,float width,float height)
        { var layout=obj.GetComponent<LayoutElement>() ?? obj.AddComponent<LayoutElement>(); if(width>0) { layout.minWidth=width; layout.preferredWidth=width; } if(height>0) { layout.minHeight=height; layout.preferredHeight=height; } }
        static GameObject Vertical(string name,Transform parent,int padding)
        {
            var obj=Node(name,parent); var group=obj.AddComponent<VerticalLayoutGroup>(); group.spacing=8; group.padding=new RectOffset(padding,padding,padding,padding);
            group.childControlWidth=group.childControlHeight=true; group.childForceExpandWidth=true; group.childForceExpandHeight=false; return obj;
        }
        static GameObject Horizontal(string name,Transform parent,float height)
        { var obj=Node(name,parent); var group=obj.AddComponent<HorizontalLayoutGroup>(); group.spacing=12; group.childControlWidth=group.childControlHeight=true; group.childForceExpandWidth=true; group.childForceExpandHeight=true; Layout(obj,0,height); return obj; }
        static void Card(GameObject obj) { var image=obj.AddComponent<Image>(); image.color=new Color(.015f,.04f,.025f,.5f); image.raycastTarget=false; }
        internal static Text Label(string name,Transform parent,string value,int size,Vector2 position,Vector2 dimensions)
        { var obj=Node(name,parent); Position(obj.transform,position,dimensions); var text=obj.AddComponent<Text>(); Style(text,size); text.text=value; return text; }
        internal static void Style(Text text,int size) { text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize=size; text.color=new Color(.96f,.9f,.73f); text.raycastTarget=false; }
        static Text BodyText(Transform parent,string value)
        { var text=Node("Details",parent).AddComponent<Text>(); Style(text,14); text.text=value; text.verticalOverflow=VerticalWrapMode.Overflow; return text; }
        static MenuGraphicButton Button(MainMenu menu,Transform parent,string art,FrontendAction action,MainMenu.Page target=MainMenu.Page.Home,int row=0)
        {
            var obj=Node(art,parent); Layout(obj,0,parent.GetComponent<HorizontalLayoutGroup>()!=null ? 54 : 72); var image=obj.AddComponent<Image>(); image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Folder+"/Sprites/"+art+".asset"); image.preserveAspect=true;
            var button=obj.AddComponent<Button>(); button.targetGraphic=image; button.transition=Selectable.Transition.None;
            var graphic=obj.AddComponent<MenuGraphicButton>(); var binding=obj.AddComponent<MainMenuPageAction>(); binding.Configure(menu,action,target,row);
            UnityEventTools.AddPersistentListener(button.onClick,binding.Click); return graphic;
        }
        static InputField Input(Transform parent,string caption,int limit)
        {
            var row=Vertical(caption,parent,0); BodyText(row.transform,caption);
            var obj=Node("Input",row.transform); Layout(obj,0,40); FieldImage(obj);
            var text=Stretch("Value",obj.transform).AddComponent<Text>(); Style(text,16); text.alignment=TextAnchor.MiddleLeft;
            text.rectTransform.offsetMin=new Vector2(12,3); text.rectTransform.offsetMax=new Vector2(-12,-3);
            var field=obj.AddComponent<InputField>(); field.textComponent=text; field.characterLimit=limit; field.caretColor=new Color(.96f,.9f,.73f); return field;
        }
        static void FieldImage(GameObject obj)
        { obj.AddComponent<Image>(); AddFieldBorder(obj); }
        internal static void AddFieldBorder(GameObject obj)
        {
            obj.GetComponent<Image>().color=Ink;
            for(int edge=0;edge<4;edge++)
            {
                var line=Stretch("Border "+edge,obj.transform); var rect=(RectTransform)line.transform;
                rect.anchorMin=edge==0 ? Vector2.zero : edge==1 ? new Vector2(0,1) : edge==2 ? Vector2.zero : new Vector2(1,0);
                rect.anchorMax=edge==0 ? new Vector2(1,0) : edge==1 ? Vector2.one : edge==2 ? new Vector2(0,1) : Vector2.one;
                rect.sizeDelta=edge<2 ? new Vector2(0,1) : new Vector2(1,0);
                var image=line.AddComponent<Image>(); image.color=Gold; image.raycastTarget=false;
            }
        }
        static Dropdown Drop(Transform parent,string caption,string[] options)
        {
            var obj=Node(caption,parent); Layout(obj,0,42); FieldImage(obj);
            var label=Stretch("Selected",obj.transform).AddComponent<Text>(); Style(label,14); label.alignment=TextAnchor.MiddleLeft;
            label.rectTransform.offsetMin=new Vector2(8,0); label.rectTransform.offsetMax=new Vector2(-22,0);
            var dropdown=obj.AddComponent<Dropdown>(); dropdown.captionText=label;
            var template=Node("Template",obj.transform); var rect=(RectTransform)template.transform; rect.anchorMin=new Vector2(0,0); rect.anchorMax=new Vector2(1,0); rect.pivot=new Vector2(.5f,1); rect.anchoredPosition=Vector2.zero; rect.sizeDelta=new Vector2(0,180); FieldImage(template);
            var viewport=Stretch("Viewport",template.transform); viewport.AddComponent<RectMask2D>();
            var content=Node("Content",viewport.transform); var c=(RectTransform)content.transform; c.anchorMin=new Vector2(0,1); c.anchorMax=new Vector2(1,1); c.pivot=new Vector2(.5f,1); c.sizeDelta=new Vector2(0,30);
            var item=Node("Item",content.transform); var r=(RectTransform)item.transform; r.anchorMin=new Vector2(0,1); r.anchorMax=new Vector2(1,1); r.pivot=new Vector2(.5f,1); r.sizeDelta=new Vector2(0,30);
            var itemImage=item.AddComponent<Image>(); itemImage.color=new Color(.09f,.16f,.11f); var toggle=item.AddComponent<Toggle>(); toggle.targetGraphic=itemImage;
            var check=Node("Selected marker",item.transform).AddComponent<Image>(); check.color=Gold; Position(check.transform,new Vector2(8,0),new Vector2(5,12)); check.rectTransform.anchorMin=check.rectTransform.anchorMax=new Vector2(0,.5f); toggle.graphic=check;
            var itemText=Stretch("Label",item.transform).AddComponent<Text>(); Style(itemText,14); itemText.alignment=TextAnchor.MiddleLeft; itemText.rectTransform.offsetMin=new Vector2(18,0); itemText.rectTransform.offsetMax=new Vector2(-5,0);
            var scroll=template.AddComponent<ScrollRect>(); scroll.viewport=(RectTransform)viewport.transform; scroll.content=c; scroll.horizontal=false; scroll.movementType=ScrollRect.MovementType.Clamped; scroll.scrollSensitivity=20;
            dropdown.template=rect; dropdown.itemText=itemText; dropdown.ClearOptions(); dropdown.AddOptions(options.Select(value=>caption+": "+value).ToList()); template.SetActive(false); return dropdown;
        }
        static ScrollRect Scroll(Transform parent)
        {
            var obj=Stretch("Scroll area",parent); var r=(RectTransform)obj.transform; r.offsetMin=new Vector2(32,85); r.offsetMax=new Vector2(-32,-145);
            var scroll=obj.AddComponent<ScrollRect>(); scroll.horizontal=false; scroll.movementType=ScrollRect.MovementType.Clamped; scroll.scrollSensitivity=30;
            var viewport=Stretch("Viewport",obj.transform); viewport.AddComponent<Image>().color=new Color(0,0,0,.001f); viewport.AddComponent<RectMask2D>(); ((RectTransform)viewport.transform).offsetMax=new Vector2(-12,0);
            var content=Vertical("Content",viewport.transform,16); var c=(RectTransform)content.transform; c.anchorMin=new Vector2(0,1); c.anchorMax=new Vector2(1,1); c.pivot=new Vector2(.5f,1); c.anchoredPosition=Vector2.zero; c.sizeDelta=Vector2.zero;
            var layout=content.GetComponent<VerticalLayoutGroup>(); layout.spacing=14; layout.padding.left=layout.padding.right=34;
            content.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var bar=Stretch("Scrollbar",obj.transform); var b=(RectTransform)bar.transform; b.anchorMin=new Vector2(1,0); b.anchorMax=new Vector2(1,1); b.offsetMin=new Vector2(-7,0); b.offsetMax=Vector2.zero;
            bar.AddComponent<Image>().color=new Color(.02f,.04f,.025f,.4f); var scrollbar=bar.AddComponent<Scrollbar>(); scrollbar.direction=Scrollbar.Direction.BottomToTop; scrollbar.navigation=new Navigation {mode=Navigation.Mode.None};
            var handle=Stretch("Handle",bar.transform).AddComponent<Image>(); handle.color=Gold; scrollbar.handleRect=(RectTransform)handle.transform; scrollbar.targetGraphic=handle;
            scroll.content=c; scroll.viewport=(RectTransform)viewport.transform; scroll.verticalScrollbar=scrollbar; scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
            obj.AddComponent<MenuScrollFocus>(); return scroll;
        }
    }
}
