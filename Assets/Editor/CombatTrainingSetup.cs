using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace SecretsReborn.Editor
{
    [InitializeOnLoad] public static class CombatTrainingSetup
    {
        const string PrefabPath="Assets/World/Combat/Prefabs/TrainingDummy.prefab";
        static CombatTrainingSetup() => EditorApplication.update+=Requested;
        static void Requested()
        {
            const string request="Temp/SetupCombatTraining.request";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            File.Delete(request);
            try { Setup(); } catch(Exception e) { File.WriteAllText("Temp/CombatTrainingReport.txt","FAIL: "+e); Debug.LogException(e); }
        }
        [MenuItem("SecretsReborn/Combat/Prepare training dummy")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play first.");
            for(int i=0;i<SceneManager.sceneCount;i++) if(SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Open scene has unsaved changes.");
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if(prefab==null)
            {
                var root=new GameObject("TrainingDummy",typeof(Rigidbody2D),typeof(SpriteRenderer),typeof(CircleCollider2D),typeof(TreeMeleeEnemy),typeof(TrainingDummyView));
                try
                {
                    root.GetComponent<TreeMeleeEnemy>().ConfigureTrainingDummy("training-dummy-sanctuary-01");
                    root.GetComponent<SpriteRenderer>().enabled=false;
                    var body=root.GetComponent<Rigidbody2D>(); body.bodyType=RigidbodyType2D.Kinematic; body.gravityScale=0; body.constraints=RigidbodyConstraints2D.FreezeAll;
                    var collider=root.GetComponent<CircleCollider2D>(); collider.radius=.4f; collider.offset=new Vector2(0,.2f);
                    var sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
                    if(sprite==null) throw new InvalidOperationException("Missing built-in shape sprite.");
                    var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/World/Sanctuary/Art/SpriteUnlit.mat");
                    Shape(root,"Foot",sprite,material,new Vector2(0,-.45f),new Vector2(.85f,.15f),new Color(.25f,.12f,.06f));
                    Shape(root,"Post",sprite,material,new Vector2(0,-.1f),new Vector2(.18f,1.2f),new Color(.43f,.25f,.1f));
                    Shape(root,"Crossbar",sprite,material,new Vector2(0,.35f),new Vector2(1.35f,.16f),new Color(.53f,.31f,.12f));
                    Shape(root,"Straw torso",sprite,material,new Vector2(0,.26f),new Vector2(.65f,.7f),new Color(.76f,.59f,.28f));
                    Shape(root,"Target outer",sprite,material,new Vector2(0,.3f),new Vector2(.35f,.35f),new Color(.72f,.17f,.1f));
                    Shape(root,"Target center",sprite,material,new Vector2(0,.3f),new Vector2(.16f,.16f),new Color(1,.89f,.55f));
                    Shape(root,"Straw head",sprite,material,new Vector2(0,.83f),new Vector2(.38f,.38f),new Color(.89f,.72f,.4f));
                    var label=new GameObject("Training HP",typeof(TextMesh)); label.transform.SetParent(root.transform,false); label.transform.localPosition=new Vector3(0,1.35f,0);
                    var text=label.GetComponent<TextMesh>(); text.text="TRAININGSPUPPE\n1.000.000 / 1.000.000 HP"; text.anchor=TextAnchor.MiddleCenter; text.characterSize=.045f; text.fontSize=32; text.color=new Color(1,.95f,.6f);
                    label.GetComponent<MeshRenderer>().sortingOrder=950;
                    root.GetComponent<TrainingDummyView>().Configure(text);
                    prefab=PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            const string scenePath="Assets/Scenes/Waldheiligtum-Editable.unity";
            var scene=SceneManager.GetSceneByPath(scenePath); bool opened=!scene.isLoaded;
            if(opened) scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Additive);
            try
            {
                if (!scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<TreeMeleeEnemy>(true)).Any(x=>x.IsTrainingDummy))
                {
                    var player=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<CharacterInventory>(true)).FirstOrDefault();
                    var world=scene.GetRootGameObjects().First(x=>x.name=="World");
                    var obj=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene); obj.transform.SetParent(world.transform,true);
                    obj.transform.position=(player!=null ? player.transform.position : Vector3.zero)+new Vector3(4,-3,0);
                    EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                }
            }
            finally { if(opened) EditorSceneManager.CloseScene(scene,true); }
            AssetDatabase.SaveAssets(); File.WriteAllText("Temp/CombatTrainingReport.txt","PASS: stationary 1,000,000 HP training dummy prefab and sanctuary instance, no contact damage or loot.");
        }
        static void Shape(GameObject root,string name,Sprite sprite,Material material,Vector2 position,Vector2 size,Color color)
        {
            var obj=new GameObject(name,typeof(SpriteRenderer)); obj.transform.SetParent(root.transform,false);
            obj.transform.localPosition=position; obj.transform.localScale=new Vector3(size.x/sprite.bounds.size.x,size.y/sprite.bounds.size.y,1);
            var renderer=obj.GetComponent<SpriteRenderer>(); renderer.sprite=sprite; renderer.sharedMaterial=material; renderer.color=color;
            // Parts share a common foot anchor for world depth; child order is kept by Z.
            obj.transform.localPosition+=Vector3.forward*(-root.transform.childCount*.001f);
        }
    }
}
