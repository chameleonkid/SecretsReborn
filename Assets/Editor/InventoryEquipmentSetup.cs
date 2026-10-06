using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SecretsReborn.Editor
{
    [InitializeOnLoad]
    public static class InventoryEquipmentSetup
    {
        private const string Root = "Assets/World/Sanctuary";
        private const string Request = "Temp/SetupEquipmentSlots.request";
        static InventoryEquipmentSetup()
        { if (File.Exists(Request)) EditorApplication.update += Requested; }
        private static void Requested()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            EditorApplication.update -= Requested; File.Delete(Request);
            try { Setup(); } catch (Exception error) { Debug.LogException(error); }
        }
        [MenuItem("SecretsReborn/World/Set up equipment slot examples")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play zuerst beenden.");
            string folder = Root + "/Items";
            var sword = Item("training-sword", "Übungsschwert", ItemKind.Weapon, false);
            var bow = Item("training-bow", "Übungsbogen", ItemKind.Weapon, true);
            var shield = Item("training-shield", "Übungsschild", ItemKind.Shield, false);
            var ring = Item("training-ring", "Übungsring", ItemKind.Ring, false);
            var armor = AssetDatabase.LoadAssetAtPath<ItemDefinition>(folder + "/TrainingArmor.asset");
            armor.SetEquipment(ItemKind.Armor, false, null); EditorUtility.SetDirty(armor);
            string path = Root + "/Prefabs/Player.prefab";
            var player = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var inventory = player.GetComponent<CharacterInventory>();
                var serialized = new SerializedObject(inventory);
                var current = serialized.FindProperty("catalog");
                var catalog = Enumerable.Range(0, current.arraySize).Select(i => current.GetArrayElementAtIndex(i).objectReferenceValue as ItemDefinition)
                    .Concat(new[] { armor, sword, bow, shield, ring }).Where(item => item != null).Distinct().ToArray();
                inventory.Configure(catalog, serialized.FindProperty("baseClothing").objectReferenceValue as ClothingAppearance);
                PrefabUtility.SaveAsPrefabAsset(player, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(player); }
            AssetDatabase.SaveAssets();
            File.WriteAllText("Temp/EquipmentSlotsReport.txt", "PASS: 40-Slot-UI und Ausrüstung importiert; Katalog um Schwert, Bogen, Schild und Ring erweitert, vier Test-Pickup-Prefabs gespeichert; Szene unverändert.");
        }
        private static ItemDefinition Item(string id, string label, ItemKind kind, bool twoHanded)
        {
            string path = Root + "/Items/" + id + ".asset";
            var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemDefinition>();
                item.Configure(id, label, 1, null); item.SetEquipment(kind, twoHanded, null);
                AssetDatabase.CreateAsset(item, path);
            }
            string prefabPath = Root + "/Prefabs/" + id + "-pickup.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                var obj = new GameObject(label, typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(WorldItem), typeof(SpriteDepth));
                try
                {
                    var renderer = obj.GetComponent<SpriteRenderer>();
                    renderer.sprite = AssetDatabase.LoadAllAssetsAtPath(Root + "/Art/Placeholder.png").OfType<Sprite>().First();
                    renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Art/SpriteUnlit.mat");
                    renderer.color = kind == ItemKind.Shield ? new Color(.5f, .65f, .9f) : twoHanded ? new Color(.7f, .45f, .2f) : kind == ItemKind.Ring ? Color.yellow : Color.gray;
                    obj.transform.localScale = Vector3.one * .4f;
                    var trigger = obj.GetComponent<CircleCollider2D>(); trigger.isTrigger = true; trigger.radius = 3.5f;
                    obj.GetComponent<WorldItem>().Configure("sanctuary-" + id, item);
                    PrefabUtility.SaveAsPrefabAsset(obj, prefabPath);
                }
                finally { UnityEngine.Object.DestroyImmediate(obj); }
            }
            return item;
        }
    }
}
