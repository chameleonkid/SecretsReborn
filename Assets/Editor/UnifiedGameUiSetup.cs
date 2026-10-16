using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace SecretsReborn.Editor
{
    public static class UnifiedGameUiSetup
    {
        const string InventoryPath="Assets/Resources/InventoryUI/InventoryCanvas.prefab",SlotPath="Assets/Resources/InventoryUI/InventorySlot.prefab",SpellPath="Assets/Resources/Magic/UI/SpellIconRingCanvas.prefab";
        [MenuItem("SecretsReborn/UI/Apply unified game UI")]
        public static void Setup()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play first.");
            StylePrefab(SlotPath,root=>StyleSlot(root.GetComponent<InventoryCanvasSlot>()));
            StylePrefab(InventoryPath,StyleInventory);
            StylePrefab(SpellPath,StyleSpells);
            VotePrefab(); DedicatedInventorySetup.Apply(); AssetDatabase.SaveAssets();
            File.WriteAllText("Temp/UnifiedGameUiSetup.txt","PASS: illustrated inventory/stash, visible slot selection, spell descriptions and native vote canvas with four participant rows and green consent marks.");
        }
        static void StylePrefab(string path,Action<GameObject> change)
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try { change(root); PrefabUtility.SaveAsPrefabAsset(root,path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        static QuietMenuFrameGraphic Border(GameObject root,float width=1,float corner=12,Color? color=null)
        {
            var child=root.transform.Find("Quiet border");
            if(child==null) child=Stretch("Quiet border",root.transform).transform;
            var border=child.GetComponent<QuietMenuFrameGraphic>() ?? child.gameObject.AddComponent<QuietMenuFrameGraphic>();
            border.raycastTarget=false; border.color=color ?? GameUiStyle.Gold;
            var fields=new SerializedObject(border); fields.FindProperty("lineWidth").floatValue=width; fields.FindProperty("cornerLength").floatValue=corner; fields.FindProperty("ornaments").boolValue=corner>7; fields.ApplyModifiedPropertiesWithoutUndo();
            return border;
        }
        static void Surface(GameObject obj,Color color,bool input=false)
        {
            var image=obj.GetComponent<Image>() ?? obj.AddComponent<Image>(); image.sprite=null; image.type=Image.Type.Simple; image.color=color; image.raycastTarget=input;
        }
        static void StyleSlot(InventoryCanvasSlot slot)
        {
            Surface(slot.gameObject,GameUiStyle.Slot,true); var baseBorder=Border(slot.gameObject,.65f,5,new Color(.38f,.44f,.35f,.65f));
            if(!PrefabUtility.IsPartOfPrefabInstance(baseBorder)) baseBorder.transform.SetAsFirstSibling();
            var quality=slot.transform.Find("Quality").GetComponent<Image>(); quality.enabled=false;
            var rarity=Border(quality.gameObject,.8f,5,Color.white);
            var selected=slot.transform.Find("Selection").GetComponent<Image>(); selected.enabled=false; Border(selected.gameObject,2.1f,7,new Color(1,.83f,.36f));
            var source=new SerializedObject(slot); source.FindProperty("qualityBorder").objectReferenceValue=rarity; source.ApplyModifiedPropertiesWithoutUndo();
            var ghost=slot.transform.Find("EmptyGlyph").GetComponent<Image>(); ghost.color=new Color(.58f,.63f,.59f,.55f);
            var count=slot.GetComponentInChildren<Text>(true); count.color=GameUiStyle.Text;
            quality.gameObject.SetActive(false); selected.gameObject.SetActive(false);
        }
        static void StyleInventory(GameObject root)
        {
            foreach(var slot in root.GetComponentsInChildren<InventoryCanvasSlot>(true)) StyleSlot(slot);
            var inv=root.GetComponentsInChildren<RectTransform>(true).First(x=>x.name=="InventoryPanel");
            var stash=root.GetComponentsInChildren<RectTransform>(true).First(x=>x.name=="SharedStashPanel");
            foreach(var panel in new[]{inv,stash})
            {
                Surface(panel.gameObject,GameUiStyle.Surface,true); Border(panel.gameObject,1.3f,22).transform.SetAsFirstSibling();
                foreach(var label in panel.GetComponentsInChildren<Text>(true)) label.color=GameUiStyle.Text;
                foreach(var button in panel.GetComponentsInChildren<Button>(true))
                {
                    Surface(button.gameObject,new Color(.07f,.12f,.085f,.9f),true); Border(button.gameObject,.8f,7);
                    button.transition=Selectable.Transition.None;
                    if(button.GetComponent<QuietActionButton>()==null) button.gameObject.AddComponent<QuietActionButton>();
                }
                var parent=panel.parent;
                if(parent.name!="Inventory safe area" && parent.name!="Stash safe area")
                {
                    var fit=Stretch(panel==inv ? "Inventory safe area" : "Stash safe area",root.transform);
                    panel.SetParent(fit.transform,false); fit.AddComponent<MenuViewportFit>().Configure((RectTransform)panel);
                }
            }
            var bag=inv.Find("BagTitle").GetComponent<Text>(); Layout(bag.rectTransform,new Rect(520,70,310,25));
            var gold=inv.Find("Gold").GetComponent<Text>(); Layout(gold.rectTransform,new Rect(850,70,150,25)); gold.fontSize=15; gold.alignment=TextAnchor.MiddleRight;
            Layout(inv.Find("Controls") as RectTransform,new Rect(520,326,480,45)); inv.Find("Controls").GetComponent<Text>().color=GameUiStyle.Muted;
            Layout(inv.Find("ItemDetails") as RectTransform,new Rect(520,398,480,124));
            CardBehind(inv,"Item details surface",new Rect(506,386,508,148));
            var lamp=inv.Find("QuickAccessTitle").GetComponent<Text>(); lamp.text="LATERNE"; lamp.alignment=TextAnchor.MiddleCenter; Layout(lamp.rectTransform,new Rect(155,406,245,22));
            Line(inv,"Equipment divider",new Rect(492,72,1,458));
            Layout(stash.Find("Controls") as RectTransform,new Rect(40,326,950,42)); stash.Find("Controls").GetComponent<Text>().color=GameUiStyle.Muted;
            Layout(stash.Find("ItemDetails") as RectTransform,new Rect(56,392,928,116)); CardBehind(stash,"Item details surface",new Rect(40,380,960,144));
            Layout(stash.Find("Status") as RectTransform,new Rect(40,526,770,20));
            Line(stash,"Stash divider",new Rect(518,120,1,185));
        }
        static void StyleSpells(GameObject root)
        {
            foreach(string name in new[]{"InfoStrip","SpellBook","Back"})
            { var obj=root.transform.Find(name).gameObject; Surface(obj,GameUiStyle.Surface,name=="Back"); Border(obj,1,name=="Back" ? 7 : 16).transform.SetAsFirstSibling(); }
            var header=(RectTransform)root.transform.Find("InfoStrip"); header.sizeDelta=new Vector2(1000,128);
            Layout(header.Find("Title") as RectTransform,new Rect(24,14,952,28));
            Layout(header.Find("Details") as RectTransform,new Rect(24,46,952,72));
            foreach(var text in root.GetComponentsInChildren<Text>(true)) text.color=GameUiStyle.Text;
            root.transform.Find("InfoStrip/Title").GetComponent<Text>().fontSize=18;
            var fields=new SerializedObject(root.GetComponent<SpellRingView>()); fields.FindProperty("infoBackgroundAlpha").floatValue=.84f; fields.FindProperty("bookBackgroundAlpha").floatValue=.84f; fields.ApplyModifiedPropertiesWithoutUndo();
            var back=root.transform.Find("Back").gameObject; if(back.GetComponent<QuietActionButton>()==null) back.AddComponent<QuietActionButton>();
        }
        static void CardBehind(Transform parent,string name,Rect rect)
        {
            var obj=parent.Find(name)?.gameObject ?? Node(name,parent); Layout((RectTransform)obj.transform,rect); Surface(obj,new Color(.008f,.02f,.015f,.3f)); Border(obj,.6f,7,new Color(.5f,.51f,.35f,.45f)); obj.transform.SetSiblingIndex(1);
        }
        static void Line(Transform parent,string name,Rect rect)
        { var obj=parent.Find(name)?.gameObject ?? Node(name,parent); Layout((RectTransform)obj.transform,rect); Surface(obj,new Color(.78f,.65f,.34f,.22f)); }
        static GameObject Node(string name,Transform parent)
        { var obj=new GameObject(name,typeof(RectTransform)); obj.transform.SetParent(parent,false); return obj; }
        static GameObject Stretch(string name,Transform parent)
        { var obj=Node(name,parent); var rect=(RectTransform)obj.transform; rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero; return obj; }
        static void Layout(RectTransform rect,Rect area)
        { rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1); rect.anchoredPosition=new Vector2(area.x,-area.y); rect.sizeDelta=area.size; }
        static Text Label(string name,Transform parent,string value,Rect area,int size=16)
        {
            var obj=Node(name,parent); Layout((RectTransform)obj.transform,area); var text=obj.AddComponent<Text>();
            text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize=size; text.color=GameUiStyle.Text; text.text=value; text.raycastTarget=false; text.alignment=TextAnchor.MiddleLeft; return text;
        }
        static void Bottom(RectTransform rect,float x,float y,float width,float height)
        { rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,0); rect.anchoredPosition=new Vector2(x,y); rect.sizeDelta=new Vector2(width,height); }
        static Button VoteButton(Transform parent,string name,string art,float x,UnityEngine.Events.UnityAction action)
        {
            var obj=Node(name,parent); Bottom((RectTransform)obj.transform,x,65,290,64);
            var image=obj.AddComponent<Image>(); image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/MenuUI/QuietHome/Sprites/"+art+".asset"); image.preserveAspect=true;
            var button=obj.AddComponent<Button>(); button.targetGraphic=image; button.transition=Selectable.Transition.None; button.navigation=new Navigation {mode=Navigation.Mode.None}; obj.AddComponent<QuietActionButton>(); UnityEventTools.AddPersistentListener(button.onClick,action); return button;
        }
        static void VotePrefab()
        {
            const string path="Assets/Resources/Interface/GroupVoteCanvas.prefab";
            if(AssetDatabase.LoadAssetAtPath<GameObject>(path)!=null)
            {
                StylePrefab(path,root=> { foreach(var name in root.GetComponentsInChildren<Text>(true).Where(x=>x.name=="Player name")) { name.resizeTextForBestFit=true; name.resizeTextMinSize=12; name.resizeTextMaxSize=18; } });
                return;
            }
            Directory.CreateDirectory("Assets/Resources/Interface"); AssetDatabase.Refresh();
            var root=new GameObject("GroupVoteCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            try
            {
                root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay; root.GetComponent<Canvas>().sortingOrder=250;
                var scale=root.GetComponent<CanvasScaler>(); scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scale.referenceResolution=new Vector2(1280,800); scale.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
                var view=root.AddComponent<GroupVoteCanvasView>(); var shade=Stretch("Dim world",root.transform); Surface(shade,new Color(0,0,0,.27f),true);
                var fit=Stretch("Safe area",root.transform); var panel=Node("Group consent",fit.transform); var rect=(RectTransform)panel.transform; rect.sizeDelta=new Vector2(800,582); Surface(panel,GameUiStyle.Surface,true); Border(panel,1.3f,22); fit.AddComponent<MenuViewportFit>().Configure(rect);
                var title=Label("Heading",panel.transform,"SPIELSTAND GEMEINSAM LADEN?",new Rect(35,28,730,40),24); title.alignment=TextAnchor.MiddleCenter;
                var destination=Label("Destination",panel.transform,"Alle verbundenen Spieler müssen zustimmen.",new Rect(48,83,704,56),16); destination.alignment=TextAnchor.MiddleCenter;
                var rows=new GameObject[4]; var names=new Text[4]; var states=new Text[4]; var marks=new ConsentMarkGraphic[4];
                for(int i=0;i<4;i++)
                {
                    rows[i]=Node("Player "+(i+1),panel.transform); Layout((RectTransform)rows[i].transform,new Rect(48,145+i*58,704,50)); Surface(rows[i],new Color(.10f,.15f,.10f,.48f));
                    var mark=Node("Consent mark",rows[i].transform); Layout((RectTransform)mark.transform,new Rect(14,8,34,34)); marks[i]=mark.AddComponent<ConsentMarkGraphic>(); marks[i].raycastTarget=false;
                    names[i]=Label("Player name",rows[i].transform,"Spieler",new Rect(62,4,360,42),18);
                    names[i].resizeTextForBestFit=true; names[i].resizeTextMinSize=12; names[i].resizeTextMaxSize=18;
                    states[i]=Label("Answer",rows[i].transform,"Wartet auf Antwort",new Rect(430,4,250,42),15); states[i].alignment=TextAnchor.MiddleRight;
                }
                var progress=Label("Progress",panel.transform,"0 / 4 Zustimmungen",new Rect(0,0,700,30),16); Bottom(progress.rectTransform,0,146,700,30); progress.alignment=TextAnchor.MiddleCenter;
                var hint=Label("Controls",panel.transform,"Enter / A: zustimmen · Esc / B: abbrechen",new Rect(0,0,720,32),14); Bottom(hint.rectTransform,0,20,720,32); hint.alignment=TextAnchor.MiddleCenter; hint.color=GameUiStyle.Muted;
                var yes=VoteButton(panel.transform,"Accept","Confirm",-170,view.Accept); var no=VoteButton(panel.transform,"Cancel","Cancel",170,view.Cancel);
                view.Configure(rect,title,destination,progress,hint,rows,names,states,marks,yes,no);
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
