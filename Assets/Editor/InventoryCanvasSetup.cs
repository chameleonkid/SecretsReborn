using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace SecretsReborn.Editor
{
    [InitializeOnLoad]
    public static class InventoryCanvasSetup
    {
        private const string Root = "Assets/Resources/InventoryUI";
        private static Sprite panel, slot, selected, button;
        private static Font font;
        static InventoryCanvasSetup() => EditorApplication.update += Requested;
        private static void Requested()
        {
            const string request = "Temp/SetupInventoryCanvas.request";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            File.Delete(request); try { Setup(); } catch (Exception e) { File.WriteAllText("Temp/InventoryCanvasSetupReport.txt","FAIL: " + e); Debug.LogException(e); }
        }
        [MenuItem("SecretsReborn/UI/Prepare editable inventory canvas")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play zuerst beenden.");
            Directory.CreateDirectory(Root + "/Sprites"); AssetDatabase.Refresh(); font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/ForestInventoryAtlas.png");
            panel = Region(atlas,"Panel",new Rect(0,.52f,.5f,.48f),true);
            slot = Region(atlas,"Slot",new Rect(.5f,.52f,.5f,.48f),true);
            selected = Region(atlas,"Selected",new Rect(0,.036f,.5f,.484f),true);
            button = Region(atlas,"Button",new Rect(.5f,.036f,.5f,.484f),true);
            const string slotPath = Root + "/InventorySlot.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(slotPath) == null)
            {
                var obj = Rect("InventorySlot",null,new Rect(0,0,45,45)); var component = obj.AddComponent<InventoryCanvasSlot>();
                Frame(obj,slot,7,true);
                var quality = MakeImage("Quality",obj,new Rect(3,3,39,39),selected,7); quality.fillCenter = false; quality.type = Image.Type.Sliced;
                var ghost = MakeImage("EmptyGlyph",obj,new Rect(9,9,27,27),null,0); ghost.preserveAspect = true; ghost.color = new Color(.48f,.48f,.48f);
                var icon = Raw("ItemIcon",obj,new Rect(9,9,27,27)); Center(icon.rectTransform);
                var count = Label("StackCount",obj,new Rect(4,28,37,15),"",12); count.alignment = TextAnchor.MiddleRight;
                var highlight = MakeImage("Selection",obj,new Rect(0,0,45,45),selected,7); highlight.fillCenter = false; highlight.type = Image.Type.Sliced;
                Ref(component,"icon",icon); Ref(component,"ghost",ghost); Ref(component,"quality",quality); Ref(component,"selection",highlight); Ref(component,"amount",count);
                PrefabUtility.SaveAsPrefabAsset(obj,slotPath); UnityEngine.Object.DestroyImmediate(obj);
            }
            const string canvasPath = Root + "/InventoryCanvas.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(canvasPath) == null)
            {
                var root = new GameObject("InventoryCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
                var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100; canvas.pixelPerfect = true;
                var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1040,600); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                var view = root.AddComponent<InventoryCanvasView>();
                var inv = Panel("InventoryPanel",root); var stash = Panel("SharedStashPanel",root); var hud = Rect("PotionAndReviveHUD",root,new Rect(0,0,1040,600));
                Stretch(hud.GetComponent<RectTransform>());
                Label("EquipmentTitle",inv,new Rect(40,24,450,36),"AUSRÜSTUNG",23); Label("InventoryTitle",inv,new Rect(520,24,460,36),"INVENTAR",23);
                Label("BagTitle",inv,new Rect(520,70,470,25),"REISEGEPÄCK  /  40 Plätze",15);
                var slots = new InventoryCanvasSlot[57];
                for (int i = 0; i < 40; i++) slots[i] = Slot(inv,i,new Rect(520+i%10*49,105+i/10*49,45,45),null);
                EquipmentSlot[] left = { EquipmentSlot.Head,EquipmentSlot.Shoulders,EquipmentSlot.Armor,EquipmentSlot.Hands,EquipmentSlot.Waist,EquipmentSlot.Legs,EquipmentSlot.Feet };
                EquipmentSlot[] right = { EquipmentSlot.Ring1,EquipmentSlot.Ring2,EquipmentSlot.Amulet,EquipmentSlot.Seal,EquipmentSlot.Cloak,EquipmentSlot.MainHand,EquipmentSlot.OffHand };
                var glyphs = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/EquipmentGlyphs.png");
                for (int row = 0; row < 7; row++) foreach (bool isLeft in new[] { true,false })
                {
                    int equipment = (int)(isLeft ? left[row] : right[row]); var glyph = Region(glyphs,"Glyph-"+equipment,new Rect(equipment%4*.25f,.75f-equipment/4*.25f,.25f,.25f),false);
                    slots[40+equipment] = Slot(inv,40+equipment,new Rect(isLeft ? 40 : 420,65+row*61,45,45),glyph);
                }
                slots[54] = Slot(inv,54,new Rect(233,429,45,45),ItemSprite("warm-lamp"));
                Label("QuickAccessTitle",inv,new Rect(155,355,245,24),"SCHNELLZUGRIFF",14);
                slots[55] = Slot(inv,55,new Rect(155,385,45,45),ItemSprite("health-potion")); slots[56] = Slot(inv,56,new Rect(300,385,45,45),ItemSprite("mana-potion"));
                Label("HealthShortcutLabel",inv,new Rect(207,385,77,45),"HP\n1 / LB",13); Label("ManaShortcutLabel",inv,new Rect(352,385,63,45),"MANA\n2 / RB",13);
                var portraits = new RawImage[6]; for (int i = 0; i < 6; i++) { portraits[i] = Raw("PortraitLayer-"+i,inv,new Rect(155,158,192,192)); }
                Refs(view,"portrait",portraits); Refs(view,"inventorySlots",slots);
                Ref(view,"gold",Label("Gold",inv,new Rect(800,326,200,35),"GOLD",16));
                Label("Controls",inv,new Rect(520,378,480,45),"Stick / Pfeile: wählen · LB/RB / Tab: Bereich · A: benutzen\nQ / X: Trank zuweisen oder Equipment ablegen · B / Esc: schließen",12);
                Ref(view,"inventoryDetails",Label("ItemDetails",inv,new Rect(520,430,480,106),"Gegenstand",13)); Ref(view,"inventoryStatus",Label("Status",inv,new Rect(35,510,450,30),"",12));
                Ref(view,"actionLabel",Button(inv,"Action",new Rect(520,545,225,34),"Anlegen",view.ActivateSelected)); Button(inv,"Close",new Rect(820,545,175,34),"Schließen (B / Esc)",view.Close);
                Label("BagTitle",stash,new Rect(40,30,450,35),"DEIN RUCKSACK",23); Label("StashTitle",stash,new Rect(530,30,450,35),"GEMEINSAMES LAGER",23);
                Label("Info",stash,new Rect(40,78,950,24),"40 gemeinsame Plätze · Die Welt läuft weiter · Gold bleibt persönlich",13);
                var stashSlots = new InventoryCanvasSlot[80]; for (int i = 0; i < 80; i++) stashSlots[i] = Slot(stash,i,new Rect(40+i/40*490+i%10*47,120+(i%40)/10*47,43,43),null); Refs(view,"stashSlots",stashSlots);
                Label("Controls",stash,new Rect(40,320,950,48),"Stick / Pfeile: wählen · LB/RB / Tab: Bereich · A / Enter / Rechtsklick: übertragen\nZiehen: Zielplatz wählen · B / Esc: schließen",13);
                Ref(view,"stashDetails",Label("ItemDetails",stash,new Rect(40,382,950,122),"",14)); Ref(view,"stashStatus",Label("Status",stash,new Rect(40,516,950,28),"",13));
                Ref(view,"stashActionLabel",Button(stash,"Transfer",new Rect(40,550,270,34),"Einlagern",view.ActivateSelected)); Button(stash,"Close",new Rect(825,550,175,34),"Schließen (B / Esc)",view.Close);
                var potionGroup = Rect("PotionShortcuts",hud,new Rect(905,515,125,75)); var pg = potionGroup.GetComponent<RectTransform>(); pg.anchorMin = pg.anchorMax = new Vector2(1,0); pg.pivot = new Vector2(1,0); pg.anchoredPosition = new Vector2(-10,10);
                Ref(view,"hpIcon",Raw("HealthIcon",potionGroup,new Rect(8,4,27,27))); Ref(view,"manaIcon",Raw("ManaIcon",potionGroup,new Rect(70,4,27,27)));
                Ref(view,"hpCount",Label("HealthCount",potionGroup,new Rect(0,34,64,22),"1 / LB",11)); Ref(view,"manaCount",Label("ManaCount",potionGroup,new Rect(64,34,64,22),"2 / RB",11)); Ref(view,"cooldown",Label("Cooldown",potionGroup,new Rect(0,58,124,18),"",12));
                Ref(view,"reviveText",Label("ReviveHint",hud,new Rect(20,500,440,35),"Wiederbeleben",14)); var bar = Rect("ReviveProgress",hud,new Rect(20,540,380,12)); var fill = MakeImage("Fill",bar,new Rect(0,0,380,12),null,0); fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.color = new Color(.8f,.65f,.3f); Ref(view,"reviveProgress",fill);
                var dragIcon = Raw("DragIcon",root,new Rect(0,0,40,40)); dragIcon.gameObject.SetActive(false); Ref(view,"dragIcon",dragIcon);
                Ref(view,"inventoryPanel",inv); Ref(view,"stashPanel",stash); Ref(view,"hud",hud);
                // Visible in prefab mode for layout authoring; runtime chooses the active panel.
                inv.SetActive(true); stash.SetActive(false); PrefabUtility.SaveAsPrefabAsset(root,canvasPath); UnityEngine.Object.DestroyImmediate(root);
            }
            AssetDatabase.SaveAssets(); Validate();
            File.WriteAllText("Temp/InventoryCanvasSetupReport.txt","PASS: editable Canvas and reusable slot prefabs created/validated. Existing prefabs are retained on subsequent setup runs.");
        }
        private static void Validate()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/InventoryCanvas.prefab"); var view = new SerializedObject(source.GetComponent<InventoryCanvasView>());
            if (view.FindProperty("inventorySlots").arraySize != 57 || view.FindProperty("stashSlots").arraySize != 80) throw new Exception("UI slot counts invalid.");
            foreach (var name in new[] { "inventorySlots","stashSlots" }) { var list = view.FindProperty(name); for (int i=0;i<list.arraySize;i++) if (list.GetArrayElementAtIndex(i).objectReferenceValue == null) throw new Exception("Missing UI slot."); }
        }
        private static GameObject Rect(string name,GameObject parent,Rect rect)
        { var obj = new GameObject(name,typeof(RectTransform)); if (parent != null) obj.transform.SetParent(parent.transform,false); Layout(obj.GetComponent<RectTransform>(),rect); return obj; }
        private static void Layout(RectTransform transform,Rect rect) { transform.anchorMin=transform.anchorMax=new Vector2(0,1); transform.pivot=new Vector2(0,1); transform.anchoredPosition=new Vector2(rect.x,-rect.y); transform.sizeDelta=rect.size; }
        private static void Center(RectTransform rect) { rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f); rect.anchoredPosition=Vector2.zero; }
        private static void Stretch(RectTransform rect) { rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero; }
        private static GameObject Panel(string name,GameObject root) { var obj=Rect(name,root,new Rect(0,0,1040,600)); Center(obj.GetComponent<RectTransform>()); Frame(obj,panel,27,true); return obj; }
        private static Image Frame(GameObject obj,Sprite sprite,float edge,bool raycast=false) { var image=obj.AddComponent<Image>(); image.sprite=sprite; image.type=Image.Type.Tiled; image.pixelsPerUnitMultiplier=sprite.border.x/edge; image.raycastTarget=raycast; return image; }
        private static Image MakeImage(string name,GameObject parent,Rect rect,Sprite sprite,float edge) { var obj=Rect(name,parent,rect); var image=obj.AddComponent<Image>(); image.raycastTarget=false; image.sprite=sprite; if (sprite != null && edge>0) { image.type=Image.Type.Sliced; image.pixelsPerUnitMultiplier=sprite.border.x/edge; } return image; }
        private static RawImage Raw(string name,GameObject parent,Rect rect) { var obj=Rect(name,parent,rect); var image=obj.AddComponent<RawImage>(); image.raycastTarget=false; return image; }
        private static Text Label(string name,GameObject parent,Rect rect,string value,int size) { var obj=Rect(name,parent,rect); var text=obj.AddComponent<Text>(); text.font=font; text.fontSize=size; text.text=value; text.color=new Color(.94f,.86f,.65f); text.raycastTarget=false; text.verticalOverflow=VerticalWrapMode.Overflow; return text; }
        private static Text Button(GameObject parent,string name,Rect rect,string value,UnityEngine.Events.UnityAction action) { var obj=Rect(name,parent,rect); var image=Frame(obj,button,9,true); var buttonComponent=obj.AddComponent<Button>(); buttonComponent.targetGraphic=image; buttonComponent.navigation=new Navigation { mode=Navigation.Mode.None }; UnityEventTools.AddPersistentListener(buttonComponent.onClick,action); var text=Label("Label",obj,new Rect(0,0,rect.width,rect.height),value,13); text.alignment=TextAnchor.MiddleCenter; return text; }
        private static InventoryCanvasSlot Slot(GameObject parent,int index,Rect rect,Sprite ghost)
        {
            var obj=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/InventorySlot.prefab")); obj.transform.SetParent(parent.transform,false); obj.name="Slot-"+index; Layout(obj.GetComponent<RectTransform>(),rect);
            var component=obj.GetComponent<InventoryCanvasSlot>(); var fields=new SerializedObject(component); fields.FindProperty("index").intValue=index; fields.ApplyModifiedPropertiesWithoutUndo();
            var image=obj.transform.Find("EmptyGlyph").GetComponent<Image>(); image.sprite=ghost; image.enabled=ghost != null;
            if (ghost != null && index>=54) { var shader=Resources.Load<Shader>("InventoryUI/GrayscaleIcon"); const string path=Root+"/Sprites/GrayscaleIcons.mat"; var material=AssetDatabase.LoadAssetAtPath<Material>(path); if (material==null) { material=new Material(shader); AssetDatabase.CreateAsset(material,path); } image.material=material; }
            foreach (var child in new[] { "Quality","Selection" }) Stretch(obj.transform.Find(child).GetComponent<RectTransform>());
            return component;
        }
        private static Sprite Region(Texture2D texture,string name,Rect uv,bool border)
        { string path=Root+"/Sprites/"+name+".asset"; var existing=AssetDatabase.LoadAssetAtPath<Sprite>(path); if (existing!=null) return existing; var rect=new Rect(uv.x*texture.width,uv.y*texture.height,uv.width*texture.width,uv.height*texture.height); var sprite=Sprite.Create(texture,rect,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,border ? new Vector4(rect.width*.14f,rect.height*.14f,rect.width*.14f,rect.height*.14f) : Vector4.zero); sprite.name=name; AssetDatabase.CreateAsset(sprite,path); return sprite; }
        private static Sprite ItemSprite(string id) { foreach (string guid in AssetDatabase.FindAssets("t:ItemDefinition",new[] { "Assets/World" })) { var item=AssetDatabase.LoadAssetAtPath<ItemDefinition>(AssetDatabase.GUIDToAssetPath(guid)); if (item.ItemId==id) return item.Icon; } return null; }
        private static void Ref(UnityEngine.Object owner,string name,UnityEngine.Object value) { var fields=new SerializedObject(owner); fields.FindProperty(name).objectReferenceValue=value; fields.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Refs<T>(UnityEngine.Object owner,string name,T[] values) where T:UnityEngine.Object { var fields=new SerializedObject(owner); var list=fields.FindProperty(name); list.arraySize=values.Length; for(int i=0;i<values.Length;i++) list.GetArrayElementAtIndex(i).objectReferenceValue=values[i]; fields.ApplyModifiedPropertiesWithoutUndo(); }
    }
}
