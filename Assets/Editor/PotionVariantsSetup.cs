using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SecretsReborn.Editor
{
    [InitializeOnLoad]
    public static class PotionVariantsSetup
    {
        private const string Root = "Assets/World/Equipment";
        static PotionVariantsSetup() => EditorApplication.update += Requested;
        private static void Requested()
        {
            const string request = "Temp/SetupPotionVariants.request";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (PrefabStageUtility.GetCurrentPrefabStage() != null) return;
            for (int i=0;i<SceneManager.sceneCount;i++) if (SceneManager.GetSceneAt(i).isDirty) return;
            File.Delete(request);
            try { Setup(); } catch (Exception e) { File.WriteAllText("Temp/PotionVariantsReport.txt","FAIL: " + e); Debug.LogException(e); }
        }
        [MenuItem("SecretsReborn/Equipment/Prepare potion variants")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play zuerst beenden.");
            for (int i=0;i<SceneManager.sceneCount;i++) if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Offene Szenen zuerst speichern.");
            InventoryCanvasSetup.Setup(); MoveShortcuts();
            string[] sizes = { "small", "medium", "large" }, labels = { "Kleiner", "Mittlerer", "Großer" };
            int[] health = { 2,6,12 }, mana = { 20,40,80 };
            var items = new ItemDefinition[6];
            for (int kind=0;kind<2;kind++) for (int size=0;size<3;size++)
            {
                string id = (kind==0 ? "health" : "mana") + "-potion-" + sizes[size];
                var template = AssetDatabase.LoadAssetAtPath<ItemDefinition>(kind==0 ? "Assets/World/Loot/Items/health-potion.asset" : Root+"/Items/mana-potion.asset");
                string path=Root+"/Items/"+id+".asset"; var item=AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
                if (item==null)
                {
                    item=ScriptableObject.CreateInstance<ItemDefinition>(); item.Configure(id,labels[size]+(kind==0 ? " Heiltrank" : " Manatrank"),10,null);
                    item.SetPurpose(kind==0 ? ItemPurpose.HealthPotion : ItemPurpose.ManaPotion,kind==0 ? health[size] : mana[size]);
                    item.SetEquipment(ItemKind.None,false,template.Icon); item.SetIconContent(template.IconContent);
                    item.SetValues((kind==0 ? 2 : 4)*(size+1), (kind==0 ? 8 : 16)*(size+1)); AssetDatabase.CreateAsset(item,path);
                }
                items[kind*3+size]=item;
                string prefabPath=Root+"/Prefabs/"+id+".prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath)==null)
                {
                    var obj=PrefabUtility.LoadPrefabContents("Assets/World/Loot/Prefabs/health-potion.prefab");
                    try
                    {
                        obj.name=id; obj.GetComponent<WorldItem>().Configure("test-"+id,item,3);
                        var renderer=obj.GetComponentInChildren<SpriteRenderer>(); renderer.sprite=item.Icon; renderer.color=Color.white;
                        renderer.transform.localScale=Vector3.one*((.55f+.15f*size)/item.Icon.bounds.size.y);
                        PrefabUtility.SaveAsPrefabAsset(obj,prefabPath);
                    }
                    finally { PrefabUtility.UnloadPrefabContents(obj); }
                }
            }
            var player=PrefabUtility.LoadPrefabContents("Assets/World/Sanctuary/Prefabs/Player.prefab");
            try
            {
                var fields=new SerializedObject(player.GetComponent<CharacterInventory>()); var catalog=fields.FindProperty("catalog");
                var all=Enumerable.Range(0,catalog.arraySize).Select(i=>catalog.GetArrayElementAtIndex(i).objectReferenceValue as ItemDefinition).Where(x=>x!=null).Concat(items).Distinct().ToArray();
                catalog.arraySize=all.Length; for (int i=0;i<all.Length;i++) catalog.GetArrayElementAtIndex(i).objectReferenceValue=all[i]; fields.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(player,"Assets/World/Sanctuary/Prefabs/Player.prefab");
            }
            finally { PrefabUtility.UnloadPrefabContents(player); }
            AddPickups(items); AssetDatabase.SaveAssets(); CoopSetup.Setup();
            File.WriteAllText("Temp/PotionVariantsReport.txt","PASS: six potion variants, preserved catalog, native pickup prefabs and incremental PotionTests group; shortcuts moved to equipment side. Existing items and terrain retained.");
        }
        private static void MoveShortcuts()
        {
            const string path="Assets/Resources/InventoryUI/InventoryCanvas.prefab"; var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var panel=root.transform.Find("InventoryPanel");
                if (panel.Find("QuickAccessTitle")!=null) return;
                Place(panel.Find("Slot-55"),155,385,45,45); Place(panel.Find("Slot-56"),300,385,45,45);
                Place(panel.Find("HealthShortcutLabel"),207,385,77,45); Place(panel.Find("ManaShortcutLabel"),352,385,63,45);
                var title=UnityEngine.Object.Instantiate(panel.Find("HealthShortcutLabel").gameObject,panel); title.name="QuickAccessTitle";
                Place(title.transform,155,355,245,24); title.GetComponent<Text>().text="SCHNELLZUGRIFF";
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        private static void Place(Transform obj,float x,float y,float width,float height)
        { var rect=(RectTransform)obj; rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1); rect.anchoredPosition=new Vector2(x,-y); rect.sizeDelta=new Vector2(width,height); }
        private static void AddPickups(ItemDefinition[] items)
        {
            const string path="Assets/Scenes/Waldheiligtum-Editable.unity";
            var scene=SceneManager.GetSceneByPath(path); bool opened=!scene.isLoaded;
            if (opened) scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var world=scene.GetRootGameObjects().First(x=>x.name=="World"); if (world.transform.Find("PotionTests")!=null) return;
                var player=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<CharacterInventory>(true)).FirstOrDefault();
                var origin=player!=null ? player.transform.position : Vector3.zero;
                var group=new GameObject("PotionTests"); SceneManager.MoveGameObjectToScene(group,scene); group.transform.SetParent(world.transform,false);
                for (int i=0;i<items.Length;i++)
                {
                    var obj=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/"+items[i].ItemId+".prefab"),scene);
                    obj.transform.SetParent(group.transform,true); obj.transform.position=origin+new Vector3(-5+(i%3)*1.5f,-2-(i/3)*2,0);
                }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene,true); }
        }
    }
}
