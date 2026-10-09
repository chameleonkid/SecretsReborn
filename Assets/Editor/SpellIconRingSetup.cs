using System;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace SecretsReborn.Editor
{
    [InitializeOnLoad]
    public static class SpellIconRingSetup
    {
        private const string Path="Assets/Resources/Magic/UI/SpellIconRingCanvas.prefab";
        private static Font font;
        static SpellIconRingSetup()=>EditorApplication.update+=Requested;
        private static void Requested()
        {
            const string request="Temp/SetupSpellIconRing.request";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            File.Delete(request); try { Setup(); } catch(Exception e) { File.WriteAllText("Temp/SpellIconRingReport.txt","FAIL: "+e); Debug.LogException(e); }
        }
        [MenuItem("SecretsReborn/UI/Prepare spell icon ring")]
        public static void Setup()
        {
            AssetDatabase.Refresh();
            foreach(string file in Directory.GetFiles("Assets/Resources/Magic/UI/Icons/Textures","*.png"))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(file.Replace('\\','/'));
                importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
                importer.filterMode=FilterMode.Point; importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled=false; importer.alphaIsTransparency=true; importer.npotScale=TextureImporterNPOTScale.None;
                importer.isReadable=true;
                var settings=new TextureImporterSettings(); importer.ReadTextureSettings(settings); settings.spriteMeshType=SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
                string spritePath="Assets/Resources/Magic/UI/Icons/"+System.IO.Path.GetFileNameWithoutExtension(file)+".asset";
                if(AssetDatabase.LoadAssetAtPath<Sprite>(spritePath)==null)
                {
                    var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(file.Replace('\\','/')); var pixels=texture.GetPixels32();
                    int xMin=texture.width,yMin=texture.height,xMax=-1,yMax=-1;
                    for(int y=0;y<texture.height;y++) for(int x=0;x<texture.width;x++) if(pixels[y*texture.width+x].a>16)
                    { xMin=Mathf.Min(xMin,x); yMin=Mathf.Min(yMin,y); xMax=Mathf.Max(xMax,x); yMax=Mathf.Max(yMax,y); }
                    if(xMax<0) throw new Exception("Empty icon: "+file);
                    var sprite=Sprite.Create(texture,new Rect(xMin,yMin,xMax-xMin+1,yMax-yMin+1),new Vector2(.5f,.5f),32,0,SpriteMeshType.FullRect);
                    sprite.name=System.IO.Path.GetFileNameWithoutExtension(file); AssetDatabase.CreateAsset(sprite,spritePath);
                }
            }
            SetIcon("fireball","fire"); SetIcon("heal","heal");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Path)==null)
            {
                font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                var root=new GameObject("SpellIconRingCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
                try
                {
                    var canvas=root.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=120; canvas.pixelPerfect=true;
                    var scaler=root.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1040,600); scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
                    var view=root.AddComponent<SpellRingView>();
                    var header=Rect("InfoStrip",root,new Vector2(0,-10),new Vector2(1000,106),new Vector2(.5f,1)); Frame(header);
                    var title=Label("Title",header,new Vector2(16,-8),new Vector2(960,28),20);
                    var details=Label("Details",header,new Vector2(16,-39),new Vector2(960,63),14);
                    var hint=Rect("Controls",root,new Vector2(0,10),new Vector2(1000,24),new Vector2(.5f,0));
                    var page=hint.AddComponent<Text>(); Style(page,14); page.alignment=TextAnchor.MiddleCenter;
                    var ring=Rect("Ring",root,Vector2.zero,Vector2.zero,new Vector2(.5f,.5f));
                    var template=Rect("IconTemplate",ring,Vector2.zero,new Vector2(34,34),new Vector2(.5f,.5f));
                    var image=template.AddComponent<Image>(); image.preserveAspect=true;
                    var btn=template.AddComponent<Button>(); btn.targetGraphic=image; btn.navigation=new Navigation {mode=Navigation.Mode.None}; btn.transition=Selectable.Transition.None;
                    template.SetActive(false);
                    var marker=Rect("FixedSelectionMarker",ring,new Vector2(0,74),new Vector2(48,48),new Vector2(.5f,.5f));
                    // Four small brackets, no slot frame or opaque background.
                    foreach(int x in new[] {-1,1}) foreach(int y in new[] {-1,1})
                    {
                        var a=Rect("BracketH",marker,new Vector2(x*20,y*23),new Vector2(10,3),new Vector2(.5f,.5f));
                        var b=Rect("BracketV",marker,new Vector2(x*23,y*20),new Vector2(3,10),new Vector2(.5f,.5f));
                        foreach(var obj in new[] {a,b}) { var corner=obj.AddComponent<Image>(); corner.color=new Color(1,.8f,.3f); corner.raycastTarget=false; }
                    }
                    var book=Rect("SpellBook",root,new Vector2(0,-30),new Vector2(600,400),new Vector2(.5f,.5f)); Frame(book);
                    var bookText=Label("LearnedSpells",book,new Vector2(25,-20),new Vector2(540,350),18);
                    var back=Rect("Back",root,new Vector2(-430,42),new Vector2(140,28),new Vector2(.5f,0)); Frame(back);
                    var backBtn=back.AddComponent<Button>(); backBtn.targetGraphic=back.GetComponent<Image>(); backBtn.navigation=new Navigation {mode=Navigation.Mode.None}; UnityEventTools.AddPersistentListener(backBtn.onClick,view.Back);
                    Label("Label",back,new Vector2(8,-4),new Vector2(124,22),14).text="Zurück (B / Esc)";
                    Ref(view,"title",title); Ref(view,"details",details); Ref(view,"page",page);
                    Ref(view,"ringRoot",ring.GetComponent<RectTransform>()); Ref(view,"iconTemplate",image); Ref(view,"marker",marker.GetComponent<RectTransform>());
                    Ref(view,"bookPanel",book); Ref(view,"bookText",bookText);
                    PrefabUtility.SaveAsPrefabAsset(root,Path);
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Path); var fields=new SerializedObject(prefab.GetComponent<SpellRingView>());
            foreach(string name in new[] {"title","details","page","ringRoot","iconTemplate","marker","bookPanel","bookText"})
                if(fields.FindProperty(name).objectReferenceValue==null) throw new Exception("Missing icon ring reference: "+name);
            AssetDatabase.SaveAssets(); File.WriteAllText("Temp/SpellIconRingReport.txt","PASS: Secrets icons Point/None, spell icon references, editable icon ring prefab. Existing authored prefabs retained.");
        }
        private static void SetIcon(string id,string icon)
        {
            var item=AssetDatabase.LoadAssetAtPath<SpellDefinition>("Assets/Resources/Magic/Spells/"+id+".asset");
            var fields=new SerializedObject(item); var value=fields.FindProperty("icon");
            if(value.objectReferenceValue==null) { value.objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Magic/UI/Icons/"+icon+".asset"); fields.ApplyModifiedPropertiesWithoutUndo(); }
        }
        private static GameObject Rect(string name,GameObject parent,Vector2 position,Vector2 size,Vector2 anchor)
        { var obj=new GameObject(name,typeof(RectTransform)); obj.transform.SetParent(parent.transform,false); var rt=obj.GetComponent<RectTransform>(); rt.anchorMin=rt.anchorMax=anchor; rt.pivot=anchor; rt.anchoredPosition=position; rt.sizeDelta=size; return obj; }
        private static void Frame(GameObject obj)
        { var image=obj.AddComponent<Image>(); image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/InventoryUI/Sprites/Button.asset"); image.type=Image.Type.Tiled; image.pixelsPerUnitMultiplier=image.sprite.border.x/6; }
        private static Text Label(string name,GameObject parent,Vector2 position,Vector2 size,int fontSize)
        { var text=Rect(name,parent,position,size,new Vector2(0,1)).AddComponent<Text>(); Style(text,fontSize); return text; }
        private static void Style(Text text,int size) { text.font=font; text.fontSize=size; text.color=new Color(.94f,.86f,.65f); text.raycastTarget=false; }
        private static void Ref(UnityEngine.Object owner,string name,UnityEngine.Object value)
        { var fields=new SerializedObject(owner); fields.FindProperty(name).objectReferenceValue=value; fields.ApplyModifiedPropertiesWithoutUndo(); }
    }
}
