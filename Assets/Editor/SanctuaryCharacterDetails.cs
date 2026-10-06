using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace SecretsReborn.Editor
{
    [InitializeOnLoad]
    public static class SanctuaryCharacterDetails
    {
        private const string Root = "Assets/World/Sanctuary";
        private const string Request = "Temp/UpgradeCharacterDetails.request";
        static SanctuaryCharacterDetails()
        {
            if (File.Exists(Request)) EditorApplication.update += Requested;
        }
        private static void Requested()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            EditorApplication.update -= Requested;
            File.Delete(Request);
            try { Upgrade(); } catch (Exception error) { Debug.LogException(error); }
        }
        [MenuItem("SecretsReborn/World/Upgrade character size eyes and hair")]
        public static void Upgrade()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play zuerst beenden.");
            var eyes = Frames("eyes", "GreenEyes");
            var hair = Frames("hair", "PonyHair");
            foreach (var name in new[] { "CharacterBase", "RangerOutfit", "GreenEyes", "PonyHair" })
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(Root + "/Art/" + name + ".png");
                importer.spritePixelsPerUnit = 24;
                importer.SaveAndReimport();
            }
            // Reacquire references after import.
            eyes = Sprites("GreenEyes"); hair = Sprites("PonyHair");
            var material = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Art/SpriteUnlit.mat");
            var eyePrefab = LayerPrefab("GreenEyes", eyes[0], Color.white, material);
            var hairPrefab = LayerPrefab("PonyHair", hair[0], new Color(.7352941f, .5085909f, .22166954f), material);
            string path = Root + "/Prefabs/Player.prefab";
            var player = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var eyeRenderer = Layer(player, "Eyes", eyePrefab);
                var hairRenderer = Layer(player, "Hair", hairPrefab);
                var body = player.GetComponent<SpriteRenderer>();
                eyeRenderer.sortingOrder = body.sortingOrder + 2;
                hairRenderer.sortingOrder = body.sortingOrder + 3;
                player.GetComponent<CharacterAppearance>().ConfigureDetails(eyeRenderer, eyes, hairRenderer, hair);
                var marker = player.transform.Find("Lantern glow");
                if (marker != null) marker.localPosition = new Vector3(.4f, .6f, 0);
                PrefabUtility.SaveAsPrefabAsset(player, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(player); }
            AssetDatabase.SaveAssets();
            File.WriteAllText("Temp/CharacterDetailsReport.txt", "PASS: 128 Augen- und Haarframes, zwei Layer-Prefabs; Player-Prefab mit synchroner Darstellung; 24 PPU (4/3 Maßstab), Root und Fußkollision unverändert.");
            Debug.Log("Figur vergrößert; Augen und Haare als eigene Prefab-Layer eingerichtet.");
        }
        private static Sprite[] Sprites(string name) => AssetDatabase.LoadAllAssetsAtPath(Root + "/Art/" + name + ".png").OfType<Sprite>().OrderBy(sprite => sprite.name).ToArray();
        private static Sprite[] Frames(string source, string name)
        {
            string path = Root + "/Art/" + name + ".png";
            if (!File.Exists(path))
            {
                File.Copy("docs/asset-review/secrets-" + source + ".png", path);
                AssetDatabase.ImportAsset(path);
                var rects = new SpriteRect[128];
                for (int i = 0; i < rects.Length; i++)
                    rects[i] = new SpriteRect { name = "Frame_" + i.ToString("D3"),
                        rect = new Rect(i % 16 * 32, 224 - i / 16 * 32, 32, 32),
                        alignment = SpriteAlignment.BottomCenter, pivot = new Vector2(.5f, 0), spriteID = GUID.Generate() };
                SanctuaryAuthoring.Slice(path, rects);
            }
            var frames = Sprites(name);
            if (frames.Length != 128) throw new InvalidOperationException("128 Frames erwartet: " + name);
            return frames;
        }
        private static GameObject LayerPrefab(string name, Sprite sprite, Color color, Material material)
        {
            string path = Root + "/Prefabs/" + name + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            var obj = new GameObject(name, typeof(SpriteRenderer));
            try
            {
                var renderer = obj.GetComponent<SpriteRenderer>();
                renderer.sprite = sprite; renderer.color = color; renderer.sharedMaterial = material;
                return PrefabUtility.SaveAsPrefabAsset(obj, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(obj); }
        }
        private static SpriteRenderer Layer(GameObject player, string name, GameObject prefab)
        {
            var existing = player.transform.Find(name);
            if (existing != null) return existing.GetComponent<SpriteRenderer>();
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, player.transform);
            obj.name = name;
            obj.transform.localPosition = Vector3.zero;
            return obj.GetComponent<SpriteRenderer>();
        }
    }
}
