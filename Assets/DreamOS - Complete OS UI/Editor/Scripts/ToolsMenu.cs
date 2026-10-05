#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Michsky.DreamOS
{
    public class ToolsMenu : Editor
    {
        static string objectPath;

        #region Methods & Helpers
        static void GetObjectPath()
        {
            objectPath = AssetDatabase.GetAssetPath(Resources.Load("UI Manager/DreamOS UI Manager"));
            objectPath = objectPath.Replace("Resources/UI Manager/DreamOS UI Manager.asset", "").Trim();
            objectPath = objectPath + "Prefabs/";
        }

        static void MakeSceneDirty(GameObject source, string sourceName)
        {
            if (Application.isPlaying == false)
            {
                Undo.RegisterCreatedObjectUndo(source, sourceName);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }

        static void SelectErrorDialog()
        {
            EditorUtility.DisplayDialog("DreamOS", "Cannot create the object due to missing manager file. " +
                    "Make sure you have a valid 'DreamOS UI Manager' file in DreamOS > Resources > UI Manager folder.", "Okay");
        }
        #endregion

        #region Tools Menu
        [MenuItem("Tools/DreamOS/Create Overlay Resources", false, 12)]
        static void CreateOverlayResources()
        {
            try
            {
                GetObjectPath();
                GameObject clone = AssetDatabase.LoadAssetAtPath(objectPath + "Main Sources/DreamOS Canvas.prefab", typeof(GameObject)) as GameObject;
                PrefabUtility.InstantiatePrefab(clone);
                clone.name = clone.name.Replace("(Clone)", "").Trim();
                MakeSceneDirty(clone, clone.name);
            }

            catch { SelectErrorDialog(); }
        }

        [MenuItem("Tools/DreamOS/Create World Space Resources", false, 12)]
        static void CreateWorldSpaceResources()
        {
            try
            {
                GetObjectPath();
                GameObject clone = AssetDatabase.LoadAssetAtPath(objectPath + "Main Sources/World Space Resources.prefab", typeof(GameObject)) as GameObject;
                PrefabUtility.InstantiatePrefab(clone);
                clone.name = clone.name.Replace("(Clone)", "").Trim();
                MakeSceneDirty(clone, clone.name);
            }

            catch { SelectErrorDialog(); }
        }

        [MenuItem("Tools/DreamOS/Create Multi Instance Manager", false, 12)]
        static void CreateMultiInstanceManager()
        {
            GameObject tempObj = new GameObject("Multi Instance Manager");
            tempObj.AddComponent<MultiInstanceManager>();
            Selection.activeObject = tempObj;
            MakeSceneDirty(tempObj, tempObj.name);
        }

        [MenuItem("Tools/DreamOS/Open UI Manager", false, 24)]
        static void OpenUIManager()
        {
            Selection.activeObject = Resources.Load("UI Manager/DreamOS UI Manager");

            if (Selection.activeObject == null)
                Debug.Log("Can't find an asset named 'DreamOS UI Manager'. Make sure you have a valid 'UI Manager' asset in Resources/UI Manager folder. " +
                    "You can create a new UI Manager asset or re-import the pack if you can't see the file.");
        }

        [MenuItem("Tools/DreamOS/Select App Library")]
        static void SelectAppLibrary()
        {
            Selection.activeObject = Resources.Load("Apps/App Library");

            if (Selection.activeObject == null)
                Debug.Log("Can't find an asset named 'App Library'. Make sure you have 'App Library' asset in Resources/Apps folder.");
        }

        [MenuItem("Tools/DreamOS/Select Chat Library")]
        static void SelectChatList()
        {
            Selection.activeObject = Resources.Load("Chats/Example Chat");

            if (Selection.activeObject == null)
                Debug.Log("Can't find an asset named 'Example Chat'. Make sure you have 'Example Chat' asset in Resources/Chats folder.");
        }

        [MenuItem("Tools/DreamOS/Select Mail Library")]
        static void SelectMailLibrary()
        {
            Selection.activeObject = Resources.Load("Mail/Example Mail");
        }

        [MenuItem("Tools/DreamOS/Select Music Library")]
        static void SelectMusicLibrary()
        {
            Selection.activeObject = Resources.Load("Music Player/Music Library");

            if (Selection.activeObject == null)
                Debug.Log("Can't find an asset named 'Music Library'. Make sure you have 'Music Library' asset in Resources/Music Player folder.");
        }

        [MenuItem("Tools/DreamOS/Select Profile Picture Library")]
        static void SelectPPLibrary()
        {
            Selection.activeObject = Resources.Load("Profile Pictures/Profile Picture Library");

            if (Selection.activeObject == null)
                Debug.Log("Can't find an asset named 'Profile Picture Library'. Make sure you have 'Profile Picture Library' asset in Resources/Profile Pictures folder.");
        }

        [MenuItem("Tools/DreamOS/Select Web Library")]
        static void SelectWebLibrary()
        {
            Selection.activeObject = Resources.Load("Web Browser/Web Library");

            if (Selection.activeObject == null)
                Debug.Log("Can't find an asset named 'Web Library'. Make sure you have 'Web Library' asset in Resources/Web Browser folder.");
        }

        [MenuItem("Tools/DreamOS/Select Wallpaper Library")]
        static void SelectWPLibrary()
        {
            Selection.activeObject = Resources.Load("Wallpapers/Wallpaper Library");

            if (Selection.activeObject == null)
                Debug.Log("Can't find an asset named 'Wallpaper Library'. Make sure you have 'Wallpaper Library' asset in Resources/Wallpapers folder.");
        }
        #endregion
    }
}
#endif