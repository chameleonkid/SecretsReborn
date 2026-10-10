using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SecretsReborn.Editor
{
    [InitializeOnLoad]
    public static class ConsumableRingSetup
    {
        static ConsumableRingSetup()=>EditorApplication.update+=Requested;
        private static void Requested()
        {
            const string request="Temp/SetupConsumableRing.request";
            if(!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            File.Delete(request);
            try { Setup(); } catch(Exception e) { File.WriteAllText("Temp/ConsumableRingReport.txt","FAIL: "+e); Debug.LogException(e); }
        }
        [MenuItem("SecretsReborn/UI/Prepare consumable ring")]
        public static void Setup()
        {
            const string path="Assets/Resources/InventoryUI/InventoryCanvas.prefab";
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach(var slot in prefab.GetComponentsInChildren<InventoryCanvasSlot>(true)) if(slot.Index>=55) slot.gameObject.SetActive(false);
            foreach(var text in prefab.GetComponentsInChildren<Text>(true))
            {
                if(text.name=="HealthShortcutLabel" || text.name=="ManaShortcutLabel") text.gameObject.SetActive(false);
                if(text.text.Contains("Trank zuweisen")) text.text="Stick / Pfeile: wählen · LB/RB/Tab: Bereich · A: benutzen\nX / Q: Equipment ablegen · B / Esc: schließen";
            }
            var fields=new SerializedObject(prefab.GetComponent<InventoryCanvasView>());
            foreach(string name in new[] {"hpIcon","manaIcon","hpCount","manaCount","cooldown"})
                if(fields.FindProperty(name).objectReferenceValue is Component component) component.gameObject.SetActive(false);
            PrefabUtility.SavePrefabAsset(prefab); AssetDatabase.SaveAssets();
            File.WriteAllText("Temp/ConsumableRingReport.txt","PASS: quick slots and potion HUD removed from active inventory; equipment, stash and revive UI retained.");
        }
    }
}
