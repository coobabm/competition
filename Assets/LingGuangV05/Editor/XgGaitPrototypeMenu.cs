using LingGuangV05.Desktop.XingGuang;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace LingGuangV05.EditorTools
{
    /// <summary>
    /// Builds the prototype scene for the crawler learning to walk (<see cref="XgGaitPrototype"/>): a camera, an
    /// EventSystem and the prototype with the CJK font. The scene is made beside whatever is open (additively), saved
    /// over <see cref="ScenePath"/>, and closed again, so the scenes you have open are left as they were.
    /// </summary>
    public static class XgGaitPrototypeMenu
    {
        public const string ScenePath = "Assets/LingGuangV05/XingGuang/Prototype/GaitPrototype.unity";
        const string FontPath = "Assets/LingGuangV05/Localization/Fonts/Desktop CJK.asset";

        [MenuItem("LingGuang/Prototypes/Spider learns to walk")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder("Assets/LingGuangV05/XingGuang/Prototype"))
                AssetDatabase.CreateFolder("Assets/LingGuangV05/XingGuang", "Prototype");

            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cam.tag = "MainCamera";
                var c = cam.GetComponent<Camera>();
                c.clearFlags = CameraClearFlags.SolidColor;
                c.backgroundColor = XgDark.Page;
                c.orthographic = true;
                cam.transform.position = new Vector3(0, 0, -10);

                var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

                var go = new GameObject("Gait Prototype");
                var proto = go.AddComponent<XgGaitPrototype>();
                var so = new SerializedObject(proto);
                so.FindProperty("font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
                so.ApplyModifiedPropertiesWithoutUndo();

                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
            var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            EditorGUIUtility.PingObject(asset);
            Debug.Log("[灵光] Spider gait prototype scene saved: " + ScenePath + " (open it and press Play).");
        }
    }
}
