using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SecretsReborn.Editor
{
    [InitializeOnLoad] public static class QuietMainMenuSetup
    {
        const string Folder="Assets/Resources/MenuUI/QuietHome";
        static QuietMainMenuSetup() => EditorApplication.update+=Requested;
        static void Requested()
        {
            const string request="Temp/SetupQuietMainMenu.request";
            if(!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            File.Delete(request);
            try { Setup(); File.WriteAllText("Temp/QuietMainMenuReport.txt","PASS: calm backdrop, alpha-trimmed graphical buttons, plain translucent inlay and original frame. Editable HomeCanvas prefab ready."); }
            catch(Exception error) { File.WriteAllText("Temp/QuietMainMenuReport.txt","FAIL: "+error); Debug.LogException(error); }
        }
        [MenuItem("SecretsReborn/Frontend/Prepare quiet main menu")]
        public static void Setup()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play first.");
            for(int i=0;i<SceneManager.sceneCount;i++) if(SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Open scene has unsaved changes.");
            Directory.CreateDirectory(Folder+"/Sprites"); AssetDatabase.Refresh();
            Import("Backdrop",false);
            string[] names={"Singleplayer","Multiplayer","Optionen","Exit"};
            var sprites=new Sprite[names.Length];
            for(int i=0;i<names.Length;i++) sprites[i]=CreateSprite(names[i]);
            string prefabPath=Folder+"/HomeCanvas.prefab";
            if(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath)!=null) return;
            var root=new GameObject("MainMenuHomeCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(MainMenuHomeView));
            try
            {
                var canvas=root.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=100;
                var scaler=root.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution=new Vector2(1040,600); scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
                var backdrop=Node("Quiet forest lake",root,Vector2.zero,Vector2.zero);
                var background=backdrop.AddComponent<RawImage>(); background.texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Backdrop.png"); background.raycastTarget=false;
                var fit=backdrop.AddComponent<AspectRatioFitter>(); fit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent; fit.aspectRatio=background.texture.width/(float)background.texture.height;
                var panel=Node("Menu frame",root,Vector2.zero,new Vector2(540,530));
                var inlay=Node("Plain translucent inlay",panel,Vector2.zero,new Vector2(500,490)).AddComponent<Image>();
                inlay.color=new Color(.025f,.045f,.038f,.78f); inlay.raycastTarget=false;
                var frame=Node("Original forest frame",panel,Vector2.zero,new Vector2(540,530)).AddComponent<Image>();
                frame.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/InventoryUI/Sprites/Panel.asset");
                frame.type=Image.Type.Sliced; frame.fillCenter=false; frame.pixelsPerUnitMultiplier=frame.sprite.border.x/27; frame.raycastTarget=false;
                var title=Label("Game title",panel,new Vector2(0,184),new Vector2(460,60),32);
                title.text="SECRETS REBORN"; title.alignment=TextAnchor.MiddleCenter;
                var buttons=new MenuGraphicButton[4];
                for(int i=0;i<buttons.Length;i++)
                {
                    var obj=Node(names[i],panel,new Vector2(0,100-i*86),new Vector2(390,72));
                    var graphic=obj.AddComponent<Image>(); graphic.sprite=sprites[i]; graphic.preserveAspect=true;
                    var button=obj.AddComponent<Button>(); button.targetGraphic=graphic; button.transition=Selectable.Transition.None;
                    buttons[i]=obj.AddComponent<MenuGraphicButton>();
                }
                var status=Label("Connection message",panel,new Vector2(0,-220),new Vector2(450,38),13); status.alignment=TextAnchor.MiddleCenter;
                root.GetComponent<MainMenuHomeView>().Configure(buttons,status);
                PrefabUtility.SaveAsPrefabAsset(root,prefabPath); AssetDatabase.SaveAssets();
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        static void Import(string name,bool readable)
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(Folder+"/"+name+".png");
            if(importer==null) throw new InvalidOperationException("Missing image "+name);
            importer.textureType=TextureImporterType.Default; importer.mipmapEnabled=false; importer.isReadable=readable;
            importer.filterMode=FilterMode.Bilinear; importer.wrapMode=TextureWrapMode.Clamp;
            importer.textureCompression=TextureImporterCompression.Uncompressed; importer.maxTextureSize=2048;
            importer.alphaIsTransparency=name!="Backdrop"; importer.SaveAndReimport();
        }
        static Sprite CreateSprite(string name)
        {
            string asset=Folder+"/Sprites/"+name+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<Sprite>(asset); if(existing!=null) return existing;
            Import(name,true);
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/"+name+".png"); var pixels=texture.GetPixels32();
            int left=texture.width,right=-1,bottom=texture.height,top=-1;
            for(int y=0;y<texture.height;y++) for(int x=0;x<texture.width;x++) if(pixels[y*texture.width+x].a>32)
            { left=Mathf.Min(left,x); right=Mathf.Max(right,x); bottom=Mathf.Min(bottom,y); top=Mathf.Max(top,y); }
            if(right<left) throw new InvalidOperationException("Empty button alpha: "+name);
            left=Mathf.Max(0,left-4); bottom=Mathf.Max(0,bottom-4); right=Mathf.Min(texture.width-1,right+4); top=Mathf.Min(texture.height-1,top+4);
            var bounds=new Rect(left,bottom,right-left+1,top-bottom+1);
            if(bounds.width/bounds.height<3) throw new InvalidOperationException("Button is not isolated on transparent alpha: "+name);
            Import(name,false); texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/"+name+".png");
            var sprite=Sprite.Create(texture,bounds,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect); sprite.name=name;
            AssetDatabase.CreateAsset(sprite,asset); return sprite;
        }
        static GameObject Node(string name,GameObject parent,Vector2 position,Vector2 size)
        {
            var obj=new GameObject(name,typeof(RectTransform)); obj.transform.SetParent(parent.transform,false);
            var rect=(RectTransform)obj.transform; rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f); rect.anchoredPosition=position; rect.sizeDelta=size; return obj;
        }
        static Text Label(string name,GameObject parent,Vector2 position,Vector2 size,int fontSize)
        {
            var text=Node(name,parent,position,size).AddComponent<Text>(); text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize=fontSize; text.color=new Color(.96f,.9f,.73f); text.raycastTarget=false; return text;
        }
    }
}
