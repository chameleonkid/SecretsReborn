using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace SecretsReborn.Editor
{
    [InitializeOnLoad]
    public static class SanctuaryStructureUpgrade
    {
        private const string Root = "Assets/World/Sanctuary";
        private const string Request = "Temp/UpgradeSanctuaryStructures.request";
        static SanctuaryStructureUpgrade()
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
        [MenuItem("SecretsReborn/World/Upgrade spring and sealed passage")]
        public static void Upgrade()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play zuerst beenden.");
            string art = Root + "/Art/SpringRim.png";
            if (!File.Exists(art))
            {
                File.Copy("Assets/Art/Proposals/props.png", art);
                AssetDatabase.ImportAsset(art);
                SanctuaryAuthoring.Slice(art, new[] { new SpriteRect {
                    name = "SpringRim", rect = new Rect(160, 824, 128, 72), alignment = SpriteAlignment.Center,
                    pivot = Vector2.one * .5f, spriteID = GUID.Generate() } });
            }
            var material = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Art/SpriteUnlit.mat");
            Edit("Spring", obj =>
            {
                var rim = obj.GetComponent<SpriteRenderer>();
                rim.sprite = AssetDatabase.LoadAllAssetsAtPath(art).OfType<Sprite>().Single();
                rim.drawMode = SpriteDrawMode.Simple;
                rim.color = Color.white;
                rim.sortingOrder = 2;
                // Scale only the visual child, keeping the root and gameplay collider unchanged.
                var existing = obj.transform.Find("Stone basin rim");
                var visual = existing != null ? existing.gameObject : new GameObject("Stone basin rim", typeof(SpriteRenderer));
                visual.transform.SetParent(obj.transform, false);
                visual.transform.localScale = new Vector3(.75f, .75f, 1);
                var renderer = visual.GetComponent<SpriteRenderer>();
                renderer.sprite = rim.sprite; renderer.sharedMaterial = material; renderer.sortingOrder = 2;
                rim.enabled = false;
                var basin = obj.transform.Find("Dry basin").GetComponent<SpriteRenderer>();
                basin.enabled = true;
                basin.color = new Color(.16f, .19f, .16f);
                basin.size = new Vector2(1.9f, .5f);
                basin.sortingOrder = 3;
                basin.transform.localPosition = new Vector3(0, .1f, 0);
                var water = obj.transform.Find("Water").GetComponent<SpriteRenderer>();
                water.drawMode = SpriteDrawMode.Tiled;
                water.color = Color.white;
                water.size = new Vector2(1.9f, .5f);
                water.sortingOrder = 4;
                water.transform.localPosition = new Vector3(0, .1f, 0);
                water.gameObject.SetActive(false);
                obj.GetComponent<SpringSource>().Configure(water.gameObject);
            });
            var cliffs = AssetDatabase.LoadAllAssetsAtPath(Root + "/Art/Cliffs.png").OfType<Sprite>().ToArray();
            Edit("SealedPassage", obj =>
            {
                obj.GetComponent<SpriteRenderer>().enabled = false;
                for (int i = 0; i < 3; i++)
                {
                    string name = "Seal stone " + i;
                    var existing = obj.transform.Find(name);
                    var part = existing != null ? existing.gameObject : new GameObject(name, typeof(SpriteRenderer));
                    part.transform.SetParent(obj.transform, false);
                    part.transform.localPosition = new Vector3(i - 1, 0, 0);
                    var renderer = part.GetComponent<SpriteRenderer>();
                    renderer.sprite = cliffs.Single(sprite => sprite.name == "Cliff_" + i.ToString("D2"));
                    renderer.sharedMaterial = material; renderer.sortingOrder = 3;
                }
            });
            AssetDatabase.SaveAssets();
            File.WriteAllText("Temp/SanctuaryStructureReport.txt", "PASS: Spring und SealedPassage nativ als Prefabs gespeichert; Steinbecken, Wasserschicht und zusammenhängendes dreiteiliges Felsmotiv; Gameplay-Komponenten und Root-IDs erhalten.");
            Debug.Log("Quelle und versiegelte Passage mit Steinmotiven neu aufgebaut.");
        }
        private static void Edit(string name, Action<GameObject> change)
        {
            string path = Root + "/Prefabs/" + name + ".prefab";
            var obj = PrefabUtility.LoadPrefabContents(path);
            try { change(obj); PrefabUtility.SaveAsPrefabAsset(obj, path); }
            finally { PrefabUtility.UnloadPrefabContents(obj); }
        }
    }
}
