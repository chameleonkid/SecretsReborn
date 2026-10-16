using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SecretsReborn.Editor
{
    [InitializeOnLoad] public static class MainMenuSceneSetup
    {
        static MainMenuSceneSetup() => EditorApplication.update+=Requested;
        static void Requested()
        {
            const string request="Temp/PlaceMainMenuInScene.request";
            if(!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            // Hot reload cannot apply new serialized field layouts; use a real reload before saving the scene.
            var homeField=typeof(MainMenu).GetField("home",BindingFlags.Instance|BindingFlags.NonPublic);
            if(!homeField.IsDefined(typeof(SerializeField),true)
                || typeof(MenuGraphicButton).GetField("selectedScale",BindingFlags.Instance|BindingFlags.NonPublic)==null)
            {
                UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation(UnityEditor.Compilation.RequestScriptCompilationOptions.CleanBuildCache);
                EditorUtility.RequestScriptReload(); return;
            }
            File.Delete(request);
            try { Place(); }
            catch(Exception error) { File.WriteAllText("Temp/MainMenuSceneReport.txt","FAIL: "+error); Debug.LogException(error); }
        }
        [MenuItem("SecretsReborn/Frontend/Place main menu in scene")]
        public static void Place()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play first.");
            for(int i=0;i<SceneManager.sceneCount;i++) if(SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Open scene has unsaved changes; save it first.");
            var scene=SceneManager.GetSceneByPath(NetworkCoop.MenuScene); bool opened=!scene.isLoaded;
            if(opened) scene=EditorSceneManager.OpenScene(NetworkCoop.MenuScene,OpenSceneMode.Additive);
            try
            {
                var roots=scene.GetRootGameObjects();
                var menu=roots.SelectMany(x=>x.GetComponentsInChildren<MainMenu>(true)).Single();
                var home=roots.SelectMany(x=>x.GetComponentsInChildren<MainMenuHomeView>(true)).SingleOrDefault();
                bool changed=false;
                if(home==null)
                {
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/MenuUI/QuietHome/HomeCanvas.prefab");
                    if(prefab==null) throw new InvalidOperationException("Prepare quiet main menu first.");
                    home=((GameObject)PrefabUtility.InstantiatePrefab(prefab,scene)).GetComponent<MainMenuHomeView>();
                    // Initial navigation lives in the scene and can be changed in the Inspector afterwards.
                    for(int i=0;i<home.Buttons.Length;i++)
                    {
                        var button=home.Buttons[i].Button;
                        if(button.navigation.mode!=Navigation.Mode.Automatic) continue;
                        button.navigation=new Navigation {mode=Navigation.Mode.Explicit,
                            selectOnUp=home.Buttons[(i+home.Buttons.Length-1)%home.Buttons.Length].Button,
                            selectOnDown=home.Buttons[(i+1)%home.Buttons.Length].Button};
                        PrefabUtility.RecordPrefabInstancePropertyModifications(button);
                    }
                    changed=true;
                }
                if(menu.Home!=home) { menu.ConfigureHome(home); EditorUtility.SetDirty(menu); changed=true; }
                var input=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<EventSystem>(true)).SingleOrDefault();
                if(input==null)
                {
                    var obj=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
                    SceneManager.MoveGameObjectToScene(obj,scene); input=obj.GetComponent<EventSystem>();
                    input.firstSelectedGameObject=home.Buttons[0].gameObject;
                    var module=obj.GetComponent<InputSystemUIInputModule>();
                    if(module.actionsAsset==null || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(module.actionsAsset)))
                        throw new InvalidOperationException("UI input module needs persistent InputActions; use Reset in Inspector.");
                    changed=true;
                }
                if(changed) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
                if(!opened) { Selection.activeGameObject=home.gameObject; EditorGUIUtility.PingObject(home.gameObject); }
                File.WriteAllText("Temp/MainMenuSceneReport.txt","PASS: MainMenuHomeCanvas and EventSystem are authored in MainMenu.unity; serialized Home reference assigned. Existing layouts preserved. No runtime UI creation required.");
            }
            finally { if(opened) EditorSceneManager.CloseScene(scene,true); }
        }
    }
}
