using System;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace SecretsReborn.Editor
{
    [InitializeOnLoad]
    public static class SpellRingSetup
    {
        private const string Path="Assets/Resources/Magic/UI/SpellRingCanvas.prefab";
        private static Font font;
        private static Sprite panel,button,highlight;
        static SpellRingSetup()=>EditorApplication.update+=Requested;
        private static void Requested()
        {
            const string request="Temp/SetupSpellRing.request";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            File.Delete(request); try { Setup(); } catch(Exception error) { File.WriteAllText("Temp/SpellRingReport.txt","FAIL: "+error); Debug.LogException(error); }
        }
        [MenuItem("SecretsReborn/UI/Prepare spell ring canvas")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play zuerst beenden.");
            Directory.CreateDirectory("Assets/Resources/Magic/UI"); AssetDatabase.Refresh();
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Path)==null)
            {
                font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                panel=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/InventoryUI/Sprites/Panel.asset");
                button=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/InventoryUI/Sprites/Button.asset");
                highlight=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/InventoryUI/Sprites/Selected.asset");
                if (panel==null || button==null || highlight==null) throw new Exception("Inventory frame sprites missing.");
                var root=new GameObject("SpellRingCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
                try
                {
                    var canvas=root.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=120; canvas.pixelPerfect=true;
                    var scaler=root.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1040,600); scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
                    var view=root.AddComponent<SpellRingView>(); var window=Rect("Window",root,new Rect(0,0,760,550));
                    var rt=window.GetComponent<RectTransform>(); rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(.5f,.5f); rt.anchoredPosition=Vector2.zero;
                    Frame(window,panel,20);
                    var title=Label("Title",window,new Rect(24,24,712,38),"MAGIE",24); title.alignment=TextAnchor.MiddleCenter;
                    Label("RingHint",window,new Rect(30,68,400,24),"Element → Zauber → Ziel → Bestätigen",14);
                    var choices=new Button[8]; var labels=new Text[8]; var highlights=new Image[8];
                    for (int i=0;i<8;i++)
                    {
                        float angle=i*Mathf.PI/4-Mathf.PI/2;
                        var obj=Rect("Choice-"+i,window,new Rect(230+Mathf.Cos(angle)*150-54,285+Mathf.Sin(angle)*150-30,108,60));
                        var image=Frame(obj,button,8); choices[i]=obj.AddComponent<Button>(); choices[i].targetGraphic=image; choices[i].navigation=new Navigation { mode=Navigation.Mode.None };
                        labels[i]=Label("Label",obj,new Rect(8,8,92,44),"",14); labels[i].alignment=TextAnchor.MiddleCenter;
                        var glow=Rect("Selection",obj,new Rect(0,0,108,60)); highlights[i]=Frame(glow,highlight,8); highlights[i].fillCenter=false; highlights[i].raycastTarget=false;
                    }
                    var icon=Rect("SpellIcon",window,new Rect(196,251,68,68)).AddComponent<Image>(); icon.raycastTarget=false; icon.preserveAspect=true; icon.enabled=false;
                    var details=Label("Details",window,new Rect(438,112,288,322),"",16);
                    var page=Label("Page",window,new Rect(120,455,220,24),"",13); page.alignment=TextAnchor.MiddleCenter;
                    Button(window,"Previous",new Rect(32,447,80,32),"◀",view.Previous);
                    Button(window,"Next",new Rect(350,447,80,32),"▶",view.Next);
                    Button(window,"Book",new Rect(438,447,288,32),"Zauberbuch (Y / Tab)",view.Book);
                    Button(window,"Back",new Rect(32,490,220,36),"Zurück (B / Esc)",view.Back);
                    Label("Controls",window,new Rect(270,490,458,38),"Stick / Pfeile: wählen · A / Enter: bestätigen\nLB / RB: Seite · M / View: schließen",13);
                    Ref(view,"title",title); Ref(view,"details",details); Ref(view,"page",page); Ref(view,"spellIcon",icon);
                    Refs(view,"choices",choices); Refs(view,"labels",labels); Refs(view,"highlights",highlights);
                    PrefabUtility.SaveAsPrefabAsset(root,Path);
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Path); var fields=new SerializedObject(source.GetComponent<SpellRingView>());
            // Move only the original starter-page label clear of the lower ring field.
            // An authored different position remains untouched on subsequent setup runs.
            var pageRect=source.transform.Find("Window/Page")?.GetComponent<RectTransform>();
            if (pageRect!=null && Mathf.Approximately(pageRect.anchoredPosition.y,-455))
            { pageRect.anchoredPosition=new Vector2(pageRect.anchoredPosition.x,-472); pageRect.sizeDelta=new Vector2(pageRect.sizeDelta.x,16); PrefabUtility.SavePrefabAsset(source); }
            foreach (string name in new[] { "choices","labels","highlights" })
            { var values=fields.FindProperty(name); if (values.arraySize!=8) throw new Exception("Ring must have eight editable choices."); for(int i=0;i<8;i++) if (values.GetArrayElementAtIndex(i).objectReferenceValue==null) throw new Exception("Missing ring reference."); }
            AssetDatabase.SaveAssets(); File.WriteAllText("Temp/SpellRingReport.txt","PASS: editable eight-choice spell ring canvas, frames, selection and persistent button references. Existing authored prefab retained.");
        }
        private static GameObject Rect(string name,GameObject parent,Rect rect)
        { var obj=new GameObject(name,typeof(RectTransform)); obj.transform.SetParent(parent.transform,false); var rt=obj.GetComponent<RectTransform>(); rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,1); rt.anchoredPosition=new Vector2(rect.x,-rect.y); rt.sizeDelta=rect.size; return obj; }
        private static Image Frame(GameObject obj,Sprite sprite,float edge)
        { var image=obj.AddComponent<Image>(); image.sprite=sprite; image.type=Image.Type.Tiled; image.pixelsPerUnitMultiplier=sprite.border.x/edge; return image; }
        private static Text Label(string name,GameObject parent,Rect rect,string value,int size)
        { var text=Rect(name,parent,rect).AddComponent<Text>(); text.font=font; text.fontSize=size; text.text=value; text.color=new Color(.94f,.86f,.65f); text.raycastTarget=false; return text; }
        private static void Button(GameObject parent,string name,Rect rect,string value,UnityEngine.Events.UnityAction action)
        { var obj=Rect(name,parent,rect); var image=Frame(obj,button,8); var btn=obj.AddComponent<Button>(); btn.targetGraphic=image; btn.navigation=new Navigation { mode=Navigation.Mode.None }; UnityEventTools.AddPersistentListener(btn.onClick,action); Label("Label",obj,new Rect(3,2,rect.width-6,rect.height-4),value,13).alignment=TextAnchor.MiddleCenter; }
        private static void Ref(UnityEngine.Object owner,string name,UnityEngine.Object value)
        { var fields=new SerializedObject(owner); fields.FindProperty(name).objectReferenceValue=value; fields.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Refs<T>(UnityEngine.Object owner,string name,T[] values) where T:UnityEngine.Object
        { var fields=new SerializedObject(owner); var list=fields.FindProperty(name); list.arraySize=values.Length; for(int i=0;i<values.Length;i++) list.GetArrayElementAtIndex(i).objectReferenceValue=values[i]; fields.ApplyModifiedPropertiesWithoutUndo(); }
    }
}
