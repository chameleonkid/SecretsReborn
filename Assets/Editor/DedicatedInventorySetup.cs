using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SecretsReborn.Editor
{
    public static class DedicatedInventorySetup
    {
        const string Folder="Assets/Resources/InventoryUI/SlateGold/";
        [MenuItem("SecretsReborn/UI/Apply illustrated inventory and stash")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play first.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("An open scene has unsaved changes; preserve them before authoring UI.");
            Sprite inventory=Artwork("Inventory-v1.png"),stash=Artwork("Stash-v1.png");
            Edit("Assets/Resources/InventoryUI/InventorySlot.prefab",root=>Slot(root.GetComponent<InventoryCanvasSlot>()));
            Edit("Assets/Resources/InventoryUI/InventoryCanvas.prefab",root=>
            {
                foreach(var slot in root.GetComponentsInChildren<InventoryCanvasSlot>(true)) Slot(slot);
                var inv=Find(root,"InventoryPanel"); var store=Find(root,"SharedStashPanel");
                Backplate(inv,inventory); Backplate(store,stash);
                foreach(var panel in new[]{inv,store})
                {
                    foreach(var label in panel.GetComponentsInChildren<Text>(true)) label.color=new Color(.90f,.85f,.72f);
                    foreach(var button in panel.GetComponentsInChildren<Button>(true))
                    {
                        var image=button.GetComponent<Image>(); image.color=new Color(.075f,.066f,.05f,1);
                        Frame(button.transform,.8f,2,new Color(.65f,.51f,.27f));
                    }
                    foreach(string name in new[]{"Quiet border","Item details surface","Equipment divider","Stash divider"})
                        if(panel.Find(name)!=null) panel.Find(name).gameObject.SetActive(false);
                }
                Place(inv,"EquipmentTitle",60,22,410,34); Place(inv,"InventoryTitle",540,22,460,34);
                Place(inv,"BagTitle",540,72,305,26); Place(inv,"Gold",852,72,145,26);
                Place(inv,"CharacterStats",145,75,276,74);
                // Keep the original index mapping: ten bag columns and two equipment columns.
                foreach(var slot in inv.GetComponentsInChildren<InventoryCanvasSlot>(true))
                {
                    int index=slot.Index;
                    if(index<40) Layout((RectTransform)slot.transform,new Rect(540+index%10*46,108+index/10*48,42,44));
                }
                var left=new[]{EquipmentSlot.Head,EquipmentSlot.Shoulders,EquipmentSlot.Armor,EquipmentSlot.Hands,EquipmentSlot.Waist,EquipmentSlot.Legs,EquipmentSlot.Feet};
                var right=new[]{EquipmentSlot.Ring1,EquipmentSlot.Ring2,EquipmentSlot.Amulet,EquipmentSlot.Seal,EquipmentSlot.Cloak,EquipmentSlot.MainHand,EquipmentSlot.OffHand};
                var equipment=inv.GetComponentsInChildren<InventoryCanvasSlot>(true);
                for(int row=0;row<7;row++)
                {
                    Layout((RectTransform)equipment.First(s=>s.Index==40+(int)left[row]).transform,new Rect(53,76+row*60,45,45));
                    Layout((RectTransform)equipment.First(s=>s.Index==40+(int)right[row]).transform,new Rect(432,76+row*60,45,45));
                }
                Layout((RectTransform)equipment.First(s=>s.Index==54).transform,new Rect(239,444,45,45));
                Place(inv,"QuickAccessTitle",155,416,215,22);
                Place(inv,"Controls",540,306,458,32);
                Place(inv,"ItemDetails",545,370,450,136);
                Place(inv,"Status",48,493,425,22);
                Place(inv,"Action",540,542,228,28); Place(inv,"Close",824,542,173,28);
                foreach(var label in inv.GetComponentsInChildren<Text>(true).Where(t=>t.name=="Controls" || t.name=="Status")) label.fontSize=11;
                Place(store,"BagTitle",60,22,438,34); Place(store,"StashTitle",541,22,456,34);
                Place(store,"Info",42,72,955,25);
                foreach(var slot in store.GetComponentsInChildren<InventoryCanvasSlot>(true))
                {
                    slot.gameObject.SetActive(true);
                    int index=slot.Index%40; int half=slot.Index/40;
                    Layout((RectTransform)slot.transform,new Rect(43+half*497+index%10*46,112+index/10*47,42,43));
                }
                Place(store,"Controls",43,342,953,28);
                Place(store,"ItemDetails",54,400,932,110);
                Place(store,"Status",340,543,460,24);
                Place(store,"Transfer",43,542,270,28); Place(store,"Close",825,542,172,28);
                foreach(var label in store.GetComponentsInChildren<Text>(true).Where(t=>t.name=="Controls" || t.name=="Status")) label.fontSize=11;
            });
            // Existing prefabs predate the RequireComponent fix. Repair their authored graphics too.
            foreach(string path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Resources"}).Select(AssetDatabase.GUIDToAssetPath))
            {
                var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(asset.GetComponentsInChildren<Graphic>(true).Any(g=>g.GetComponent<CanvasRenderer>()==null)) Edit(path,root=>Repair(root));
            }
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
            {
                var scene=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i); int fixedCount=0;
                foreach(var root in scene.GetRootGameObjects()) fixedCount+=Repair(root);
                if(fixedCount>0) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
            }
            AssetDatabase.SaveAssets();
            System.IO.File.WriteAllText("Temp/DedicatedInventorySetup.txt","PASS: opaque slate/gold artwork, authored slots, gold selection and all custom graphic CanvasRenderers repaired.");
        }
        static Sprite Artwork(string file)
        {
            string path=Folder+file; AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null) throw new InvalidOperationException("Missing UI artwork: "+path);
            importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
            importer.alphaIsTransparency=false; importer.mipmapEnabled=false; importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.maxTextureSize=2048; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path) ?? throw new InvalidOperationException("Artwork must import as a Sprite: "+path);
        }
        static void Edit(string path,Action<GameObject> change)
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try { change(root); PrefabUtility.SaveAsPrefabAsset(root,path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        static int Repair(GameObject root)
        {
            int count=0;
            foreach(var graphic in root.GetComponentsInChildren<Graphic>(true))
                if(graphic.GetComponent<CanvasRenderer>()==null) { graphic.gameObject.AddComponent<CanvasRenderer>(); graphic.SetAllDirty(); count++; }
            return count;
        }
        static RectTransform Find(GameObject root,string name) => root.GetComponentsInChildren<RectTransform>(true).First(t=>t.name==name);
        static void Backplate(RectTransform panel,Sprite art)
        {
            var image=panel.GetComponent<Image>(); image.sprite=art; image.type=Image.Type.Simple; image.color=Color.white; image.raycastTarget=true;
        }
        static void Slot(InventoryCanvasSlot slot)
        {
            var image=slot.GetComponent<Image>(); image.sprite=null; image.type=Image.Type.Simple; image.color=new Color(.026f,.024f,.021f,1); image.raycastTarget=true;
            Frame(slot.transform,.8f,1.6f,new Color(.43f,.37f,.26f));
            var quality=slot.transform.Find("Quality").GetComponent<Image>(); quality.enabled=false;
            var rarity=Frame(quality.transform,1.2f,1,new Color(.7f,.7f,.7f));
            var selected=slot.transform.Find("Selection").GetComponent<Image>(); selected.enabled=true; selected.sprite=null; selected.type=Image.Type.Simple; selected.fillCenter=true;
            selected.color=new Color(1,.73f,.22f,.085f); selected.raycastTarget=false;
            Frame(selected.transform,2.7f,1.7f,new Color(1,.83f,.28f,1));
            var fields=new SerializedObject(slot); fields.FindProperty("qualityBorder").objectReferenceValue=rarity; fields.ApplyModifiedPropertiesWithoutUndo();
            // Stretch every overlay to its own slot. Bag and equipment rectangles differ in size.
            foreach(string name in new[]{"Quality","Selection"})
            {
                var rect=(RectTransform)slot.transform.Find(name); rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one;
                rect.offsetMin=Vector2.one*(name=="Quality" ? 4 : 0); rect.offsetMax=-rect.offsetMin;
            }
            var ghost=slot.transform.Find("EmptyGlyph").GetComponent<Image>(); ghost.color=new Color(.58f,.53f,.42f,.42f);
            quality.gameObject.SetActive(false); selected.gameObject.SetActive(false);
            Repair(slot.gameObject);
        }
        static QuietMenuFrameGraphic Frame(Transform parent,float width,float inset,Color color)
        {
            var obj=parent.Find("Quiet border")?.gameObject;
            if(obj==null) { obj=new GameObject("Quiet border",typeof(RectTransform)); obj.transform.SetParent(parent,false); }
            var rect=(RectTransform)obj.transform; rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero;
            if(obj.GetComponent<CanvasRenderer>()==null) obj.AddComponent<CanvasRenderer>();
            var graphic=obj.GetComponent<QuietMenuFrameGraphic>() ?? obj.AddComponent<QuietMenuFrameGraphic>(); graphic.raycastTarget=false; graphic.color=color;
            var fields=new SerializedObject(graphic); fields.FindProperty("lineWidth").floatValue=width; fields.FindProperty("inset").floatValue=inset; fields.FindProperty("ornaments").boolValue=false; fields.ApplyModifiedPropertiesWithoutUndo();
            return graphic;
        }
        static void Place(Transform parent,string name,float x,float y,float w,float h)
        {
            var child=parent.Find(name) as RectTransform; if(child!=null) Layout(child,new Rect(x,y,w,h));
        }
        static void Layout(RectTransform rect,Rect area)
        { rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1); rect.anchoredPosition=new Vector2(area.x,-area.y); rect.sizeDelta=area.size; }
    }
}
