using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace SecretsReborn.Editor
{
    [InitializeOnLoad]
    public static class SanctuaryAuthoring
    {
        public const string ScenePath = "Assets/Scenes/Waldheiligtum-Editable.unity";
        private const string Request = "Temp/BuildEditableSanctuary.request";
        private const string Root = "Assets/World/Sanctuary";
        private static Material material;
        private static Sprite white;
        private static Sprite stone;

        static SanctuaryAuthoring()
        {
            if (File.Exists(Request)) EditorApplication.update += BuildRequested;
        }

        private static void BuildRequested()
        {
            if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode
                || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            // A separate untitled scene cannot be created while another one is open.
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path)) return;
            EditorApplication.update -= BuildRequested;
            File.Delete(Request);
            try { Build(); }
            catch (Exception error) { Debug.LogException(error); }
        }

        [MenuItem("SecretsReborn/World/Create editable sanctuary")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Play zuerst beenden.");
            if (File.Exists(ScenePath))
            {
                Debug.Log("Die editierbare Szene existiert bereits und wird nicht überschrieben: " + ScenePath);
                return;
            }
            Directory.CreateDirectory(Root + "/Art");
            Directory.CreateDirectory(Root + "/Tiles");
            Directory.CreateDirectory(Root + "/Prefabs");
            AssetDatabase.Refresh();
            var terrainPath = Root + "/Art/Terrain.png";
            CopyOnce("Assets/Resources/SanctuaryArt/terrain.png", terrainPath);
            var waterPath = Root + "/Art/Water.png";
            CopyOnce("Assets/Resources/SanctuaryArt/water.png", waterPath);
            var treePath = Root + "/Art/Tree.png";
            CopyOnce("Assets/Resources/SanctuaryArt/tree.png", treePath);
            ImportTree(treePath);
            Slice(terrainPath, new[] {
                Rect("Grass", 160, 480, 32, 32), Rect("Stone", 128, 1312, 32, 32) });
            Slice(waterPath, new[] { Rect("SpringWater", 16, 192, 16, 16) });
            var grass = GetSprite(terrainPath, "Grass");
            stone = GetSprite(terrainPath, "Stone");
            var water = GetSprite(waterPath, "SpringWater");
            white = CreateWhiteSprite();
            material = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Art/SpriteUnlit.mat");
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
                AssetDatabase.CreateAsset(material, Root + "/Art/SpriteUnlit.mat");
            }
            var groundTile = TileAsset("Grass", grass, Color.white, false);
            var pathTile = TileAsset("Path", white, new Color(0.48f, 0.43f, 0.30f), false);
            var stoneTile = TileAsset("Stone", stone, Color.white, true);

            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var world = Group("World", null);
                var grid = Group("Grid", world).gameObject.AddComponent<Grid>();
                var ground = Map("Ground", grid.transform, -10, false);
                var paths = Map("Paths", grid.transform, -9, false);
                var terrain = Map("Terrain", grid.transform, 0, true);
                Paint(ground, groundTile, -13, -10, 26, 20);
                Paint(paths, pathTile, -1, -9, 2, 10);
                Paint(paths, pathTile, -1, 10, 3, 4);
                Paint(terrain, stoneTile, -14, -10, 1, 21);
                Paint(terrain, stoneTile, 13, -10, 1, 21);
                Paint(terrain, stoneTile, -14, -11, 28, 1);
                Paint(terrain, stoneTile, -14, 10, 13, 1);
                Paint(terrain, stoneTile, 2, 10, 12, 1);
                Paint(terrain, stoneTile, -2, 11, 1, 3);
                Paint(terrain, stoneTile, 2, 11, 1, 3);
                Paint(terrain, stoneTile, -2, 14, 5, 1);
                var props = Group("Props", world);
                var sanctuary = Group("Sanctuary", world);
                var puzzle = Group("Puzzle", world);

                var tree = Visual("Tree", AssetDatabase.LoadAssetAtPath<Sprite>(treePath), Vector2.one, Color.white, 100);
                tree.GetComponent<SpriteRenderer>().drawMode = SpriteDrawMode.Simple;
                tree.transform.localScale = Vector3.one;
                var trunk = tree.AddComponent<BoxCollider2D>();
                trunk.size = new Vector2(0.65f, 0.65f);
                trunk.offset = Vector2.zero;
                tree.AddComponent<SpriteDepth>();
                var treePrefab = SavePrefab(tree, "Tree");
                foreach (var pos in new[] { new Vector2(-5, 0), new Vector2(5, -2),
                    new Vector2(-4, 5), new Vector2(5, 5), new Vector2(-7, -5) })
                    Place(treePrefab, props, pos);

                var source = Visual("Spring", stone, new Vector2(3, 2), Color.white, 0);
                source.AddComponent<BoxCollider2D>().size = new Vector2(3, 2);
                var basin = Visual("Dry basin", white, new Vector2(2, 1), new Color(0.22f, 0.21f, 0.18f), 1);
                basin.transform.SetParent(source.transform, false);
                var surface = Visual("Water", water, new Vector2(2, 1), Color.white, 2);
                surface.transform.SetParent(source.transform, false);
                surface.SetActive(false);
                source.AddComponent<SpringSource>().Configure(surface);
                var spring = Place(SavePrefab(source, "Spring"), sanctuary, new Vector2(0, 2)).GetComponent<SpringSource>();

                var gateObject = Visual("Sealed passage", stone, new Vector2(3, 1), Color.white, 0);
                gateObject.AddComponent<BoxCollider2D>().size = new Vector2(3, 1);
                var gate = Place(SavePrefab(gateObject, "SealedPassage"), sanctuary, new Vector2(0.5f, 10.5f));

                var playerObject = Visual("Player", white, new Vector2(0.7f, 0.9f), new Color(0.95f, 0.73f, 0.25f), 100);
                playerObject.AddComponent<BoxCollider2D>().size = new Vector2(0.7f, 0.9f);
                var body = playerObject.AddComponent<Rigidbody2D>();
                body.gravityScale = 0;
                body.constraints = RigidbodyConstraints2D.FreezeRotation;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                playerObject.AddComponent<PlayerMovement>();
                playerObject.AddComponent<SpriteDepth>();
                var lantern = playerObject.AddComponent<PlayerLantern>();
                var indicator = Visual("Lantern glow", white, new Vector2(0.25f, 0.25f), new Color(1, 0.95f, 0.55f), 101);
                indicator.transform.SetParent(playerObject.transform, false);
                indicator.transform.localPosition = new Vector3(0.5f, 0, 0);
                indicator.AddComponent<SpriteDepth>();
                lantern.Indicator = indicator.GetComponent<SpriteRenderer>();
                lantern.Indicator.enabled = false;
                var player = Place(SavePrefab(playerObject, "Player"), null, new Vector2(0, -6));
                lantern = player.GetComponent<PlayerLantern>();

                var circleObject = new GameObject("Rune circle");
                for (int r = 0; r < 12; r++)
                {
                    float angle = r * Mathf.PI * 2 / 12;
                    var rune = Visual("Rune", white, new Vector2(0.12f, 0.3f), new Color(0.65f, 0.65f, 0.8f), 1);
                    rune.transform.SetParent(circleObject.transform, false);
                    rune.transform.localPosition = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                    rune.transform.localRotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
                }
                circleObject.AddComponent<RuneCircle>();
                circleObject.AddComponent<HiddenSign>();
                var circlePrefab = SavePrefab(circleObject, "RuneCircle");
                var arrow = new GameObject("Lantern arrow");
                foreach (var stroke in new[] { new Vector3(0, 0, 0), new Vector3(-0.14f, 0.24f, -45), new Vector3(0.14f, 0.24f, 45) })
                {
                    var part = Visual("Glyph stroke", white, new Vector2(0.09f, 0.48f), new Color(0.55f, 0.95f, 1), 1);
                    part.transform.SetParent(arrow.transform, false);
                    part.transform.localPosition = new Vector3(stroke.x, stroke.y, 0);
                    part.transform.localRotation = Quaternion.Euler(0, 0, stroke.z);
                }
                arrow.AddComponent<HiddenSign>();
                var arrowPrefab = SavePrefab(arrow, "LanternArrow");
                var points = new[] { new Vector2(0, -3), new Vector2(4, 0), new Vector2(3, 6), new Vector2(-2, 7) };
                var circles = new RuneCircle[4];
                Arrow(arrowPrefab, puzzle, new Vector2(0, -5), points[0]);
                for (int i = 0; i < 4; i++)
                {
                    circles[i] = Place(circlePrefab, puzzle, points[i]).GetComponent<RuneCircle>();
                    circles[i].name = "Rune circle " + (i + 1);
                    var dest = i < 3 ? points[i + 1] : new Vector2(0.5f, 10);
                    int steps = Mathf.CeilToInt(Vector2.Distance(points[i], dest) / 1.8f);
                    for (int step = 0; step < steps; step++)
                        Arrow(arrowPrefab, puzzle, Vector2.Lerp(points[i], dest, (float)step / steps), dest);
                }
                puzzle.gameObject.AddComponent<SanctuaryPuzzle>().Configure(lantern, circles, spring, gate);

                var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(CameraFollow));
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(0, -6, -10);
                var camera = cameraObject.GetComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 5;
                camera.backgroundColor = new Color(0.12f, 0.18f, 0.13f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                cameraObject.GetComponent<CameraFollow>().Target = player.transform;
                Validate(scene);
                AssetDatabase.SaveAssets();
                if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Szene konnte nicht gespeichert werden.");
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
            // Verify that the saved scene can be reopened with persistent references.
            var saved = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try { Validate(saved); }
            finally { EditorSceneManager.CloseScene(saved, true); }
            File.WriteAllText("Temp/EditableSanctuaryReport.txt", "PASS: Szene gespeichert und erneut geladen; Grid, Tilemaps, Prefabs und vier Kreise geprüft.");
            Debug.Log("Editierbares Waldheiligtum erstellt und geprüft: " + ScenePath);
        }

        private static SpriteRect Rect(string name, int x, int y, int width, int height) => new SpriteRect {
            name = name, rect = new Rect(x, y, width, height), alignment = SpriteAlignment.Center,
            pivot = Vector2.one * 0.5f, spriteID = GUID.Generate() };

        internal static void Slice(string path, SpriteRect[] rects)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            provider.SetSpriteRects(rects);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(
                rects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
        }

        private static void ImportTree(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32;
            importer.spritePivot = new Vector2(0.39816698f, 0.11654253f);
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static Sprite CreateWhiteSprite()
        {
            string path = Root + "/Art/Placeholder.png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(32, 32);
                texture.SetPixels(Enumerable.Repeat(Color.white, 1024).ToArray());
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
            }
            Slice(path, new[] { Rect("Placeholder", 0, 0, 32, 32) });
            return GetSprite(path, "Placeholder");
        }

        private static void CopyOnce(string source, string destination)
        {
            if (!File.Exists(destination)) File.Copy(source, destination);
            AssetDatabase.ImportAsset(destination);
        }

        private static Sprite GetSprite(string path, string name) =>
            AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Single(sprite => sprite.name == name);

        private static Tile TileAsset(string name, Sprite sprite, Color color, bool solid)
        {
            string path = Root + "/Tiles/" + name + ".asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile != null) return tile;
            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.color = color;
            tile.colliderType = solid ? Tile.ColliderType.Grid : Tile.ColliderType.None;
            AssetDatabase.CreateAsset(tile, path);
            return tile;
        }

        private static Transform Group(string name, Transform parent)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        private static Tilemap Map(string name, Transform parent, int order, bool solid)
        {
            var group = Group(name, parent);
            var map = group.gameObject.AddComponent<Tilemap>();
            var renderer = group.gameObject.AddComponent<TilemapRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = order;
            if (solid) group.gameObject.AddComponent<TilemapCollider2D>();
            return map;
        }

        private static void Paint(Tilemap map, Tile tile, int x, int y, int width, int height)
        {
            for (int row = y; row < y + height; row++)
                for (int col = x; col < x + width; col++) map.SetTile(new Vector3Int(col, row, 0), tile);
        }

        private static GameObject Visual(string name, Sprite sprite, Vector2 size, Color color, int order)
        {
            var obj = new GameObject(name);
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = size;
            renderer.color = color;
            renderer.sortingOrder = order;
            return obj;
        }

        private static GameObject SavePrefab(GameObject obj, string name)
        {
            string path = Root + "/Prefabs/" + name + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
            Object.DestroyImmediate(obj);
            if (prefab == null) throw new IOException("Prefab konnte nicht gespeichert werden: " + path);
            return prefab;
        }

        private static GameObject Place(GameObject prefab, Transform parent, Vector2 position)
        {
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            return obj;
        }

        private static void Arrow(GameObject prefab, Transform parent, Vector2 position, Vector2 destination)
        {
            var arrow = Place(prefab, parent, position);
            var direction = destination - position;
            arrow.transform.localRotation = Quaternion.Euler(0, 0,
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90);
        }

        private static void Validate(Scene scene)
        {
            var objects = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            if (!objects.Any(obj => obj.name == "World") || objects.Count(obj => obj.GetComponent<Tilemap>() != null) != 3
                || objects.Count(obj => obj.GetComponent<RuneCircle>() != null) != 4
                || objects.Any(obj => obj.GetComponent<ForestSanctuaryPrototype>() != null)
                || objects.Any(obj => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(obj.gameObject) > 0))
                throw new InvalidOperationException("Szenenstruktur oder Komponenten unvollständig.");
            foreach (var obj in objects.Where(obj => obj.GetComponent<SpriteRenderer>() != null))
                if (obj.GetComponent<SpriteRenderer>().sprite == null) throw new InvalidOperationException("Sprite fehlt: " + obj.name);
            foreach (var map in objects.Select(obj => obj.GetComponent<Tilemap>()).Where(map => map != null))
                if (!map.ContainsTile(AssetDatabase.LoadAssetAtPath<Tile>(Root + "/Tiles/" +
                    (map.name == "Ground" ? "Grass" : map.name == "Paths" ? "Path" : "Stone") + ".asset")))
                    throw new InvalidOperationException("Tilemap ist leer: " + map.name);
            foreach (var name in new[] { "Tree", "Spring", "SealedPassage", "Player", "RuneCircle", "LanternArrow" })
                if (AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/" + name + ".prefab") == null)
                    throw new InvalidOperationException("Prefab fehlt: " + name);
            var puzzle = objects.Select(obj => obj.GetComponent<SanctuaryPuzzle>()).Single(component => component != null);
            var references = new SerializedObject(puzzle);
            foreach (var field in new[] { "lantern", "spring", "gate" })
                if (references.FindProperty(field).objectReferenceValue == null)
                    throw new InvalidOperationException("Szenenreferenz fehlt: " + field);
            var circles = references.FindProperty("circles");
            if (circles.arraySize != 4) throw new InvalidOperationException("Kreisreihenfolge unvollständig.");
            for (int i = 0; i < 4; i++)
                if (circles.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    throw new InvalidOperationException("Kreisreferenz fehlt: " + i);
            foreach (var obj in objects.Where(obj => obj.GetComponent<RuneCircle>() != null
                || obj.GetComponent<SpringSource>() != null || obj.GetComponent<PlayerMovement>() != null))
                if (!PrefabUtility.IsPartOfPrefabInstance(obj.gameObject))
                    throw new InvalidOperationException("Prefab-Verknüpfung fehlt: " + obj.name);
        }
    }
}
