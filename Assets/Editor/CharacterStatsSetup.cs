using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SecretsReborn.Editor
{
    [InitializeOnLoad]
    public static class CharacterStatsSetup
    {
        static CharacterStatsSetup() => EditorApplication.update += Requested;
        private static void Requested()
        {
            const string request="Temp/SetupCharacterStats.request";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || PrefabStageUtility.GetCurrentPrefabStage()!=null) return;
            File.Delete(request); try { Setup(); } catch (Exception e) { File.WriteAllText("Temp/CharacterStatsReport.txt","FAIL: "+e); Debug.LogException(e); }
        }
        [MenuItem("SecretsReborn/Equipment/Prepare character stats")]
        public static void Setup()
        {
            var canvas=PrefabUtility.LoadPrefabContents("Assets/Resources/InventoryUI/InventoryCanvas.prefab");
            try
            {
                var panel=canvas.transform.Find("InventoryPanel"); var label=panel.Find("CharacterStats");
                if (label==null)
                {
                    var obj=UnityEngine.Object.Instantiate(panel.Find("ItemDetails").gameObject,panel); obj.name="CharacterStats";
                    var rect=(RectTransform)obj.transform; rect.anchoredPosition=new Vector2(155,-65); rect.sizeDelta=new Vector2(260,80);
                    obj.GetComponent<Text>().fontSize=12; label=obj.transform;
                }
                var fields=new SerializedObject(canvas.GetComponent<InventoryCanvasView>()); fields.FindProperty("characterStats").objectReferenceValue=label.GetComponent<Text>(); fields.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(canvas,"Assets/Resources/InventoryUI/InventoryCanvas.prefab");
            }
            finally { PrefabUtility.UnloadPrefabContents(canvas); }
            string[] names={ "warrior-armor","wizard-robes","paladin-armor" }; int[] hearts={1,0,2}, mana={0,30,10};
            for (int i=0;i<3;i++)
            {
                var item=AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/World/Equipment/Items/"+names[i]+".asset");
                item.SetStatBonuses(hearts[i],mana[i]); EditorUtility.SetDirty(item);
            }
            var world=new WorldSessionState("stat-roundtrip");
            world.CharacterInventory("stats");
            world.CharacterVitals("stats").SetEquipmentBonuses(4,30);
            string path=Path.GetFullPath("Temp/StatsRoundtrip.es3");
            try
            {
                SaveGameStore.Save(world,path); var loaded=SaveGameStore.Load(path).CharacterVitals("stats");
                if (loaded.BaseMaxHealth!=6 || loaded.MaxHealth!=10 || loaded.BaseMaxMana!=50 || loaded.MaxMana!=80 || loaded.Health!=6) throw new Exception("Stats file roundtrip mismatch");
                loaded.SetEquipmentBonuses(0,0); if (loaded.MaxHealth!=6 || loaded.MaxMana!=50) throw new Exception("Equipment became permanent");
            }
            finally { if (ES3.FileExists(path)) ES3.DeleteFile(path); if (ES3.FileExists(path+".bac")) ES3.DeleteFile(path+".bac"); }
            AssetDatabase.SaveAssets(); CoopSetup.Setup();
            File.WriteAllText("Temp/CharacterStatsReport.txt","PASS: editable stats panel, test equipment bonuses and real Easy Save base/derived vitals roundtrip.");
        }
    }
}
