using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Emergence.Editor
{
    public static class EmergenceSetup
    {
        public const string ScenePath="Assets/Emergence/Scenes/Emergence.unity";
        [MenuItem("Tools/Emergence/Open Game Scene")]
        public static void OpenGame()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before opening the game scene.");
            if(File.Exists(ScenePath)){EditorSceneManager.OpenScene(ScenePath);return;}
            CreateGame();
        }
        [MenuItem("Tools/Emergence/Create Game Scene")]
        public static void CreateGame()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before creating the game scene.");
            var active=SceneManager.GetActiveScene();
            if(active.isDirty && !string.IsNullOrEmpty(active.path))EditorSceneManager.SaveScene(active);
            if(active.isDirty)throw new InvalidOperationException("Save the untitled scene first.");
            Directory.CreateDirectory("Assets/Emergence/Scenes");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var cameraGO=new GameObject("Emergence · Camera",typeof(Camera),typeof(AudioListener));
            var cam=cameraGO.GetComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.031f,.082f,.114f);cam.orthographic=true;cam.orthographicSize=5;cameraGO.transform.position=new Vector3(0,0,-10);cameraGO.tag="MainCamera";
            new GameObject("Emergence · Game",typeof(EmergenceApp));
            EditorSceneManager.SaveScene(scene,ScenePath);
            var existing=EditorBuildSettings.scenes;var scenes=new System.Collections.Generic.List<EditorBuildSettingsScene>{new EditorBuildSettingsScene(ScenePath,true)};
            foreach(var entry in existing)if(entry.path!=ScenePath)scenes.Add(entry);
            EditorBuildSettings.scenes=scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("Emergence scene created: "+ScenePath);
        }
    }
}
