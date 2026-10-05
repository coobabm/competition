#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Michsky.DreamOS
{
    public class CreateMenu : Editor
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

        static void UpdateCustomEditorPath()
        {
            string darkPath = AssetDatabase.GetAssetPath(Resources.Load("DreamOS-EditorDark"));
            string lightPath = AssetDatabase.GetAssetPath(Resources.Load("DreamOS-EditorLight"));

            EditorPrefs.SetString("DreamOS.CustomEditorDark", darkPath);
            EditorPrefs.SetString("DreamOS.CustomEditorLight", lightPath);
        }
        #endregion

        #region Object Creating
        static void CreateObject(string resourcePath)
        {
            try
            {
                GetObjectPath();
                UpdateCustomEditorPath();

                GameObject clone = Instantiate(AssetDatabase.LoadAssetAtPath(objectPath + resourcePath + ".prefab", typeof(GameObject)), Vector3.zero, Quaternion.identity) as GameObject;

                try
                {
                    if (Selection.activeGameObject == null)
                    {
#if UNITY_2023_2_OR_NEWER
                        var canvas = FindObjectsByType<Canvas>(FindObjectsSortMode.None)[0];
#else
                        var canvas = (Canvas)GameObject.FindObjectsOfType(typeof(Canvas))[0];
#endif
                        clone.transform.SetParent(canvas.transform, false);
                    }

                    else { clone.transform.SetParent(Selection.activeGameObject.transform, false); }

                    clone.name = clone.name.Replace("(Clone)", "").Trim();
                    MakeSceneDirty(clone, clone.name);
                }

                catch
                {
                    CreateCanvas();
#if UNITY_2023_2_OR_NEWER
                    var canvas = FindObjectsByType<Canvas>(FindObjectsSortMode.None)[0];
#else
                    var canvas = (Canvas)GameObject.FindObjectsOfType(typeof(Canvas))[0];
#endif
                    clone.transform.SetParent(canvas.transform, false);
                    clone.name = clone.name.Replace("(Clone)", "").Trim();
                    MakeSceneDirty(clone, clone.name);
                }

                Selection.activeObject = clone;
            }

            catch { SelectErrorDialog(); }
        }

        [MenuItem("GameObject/DreamOS/Canvas", false, 4)]
        static void CreateCanvas()
        {
            try
            {
                GetObjectPath();
                UpdateCustomEditorPath();

                GameObject clone = Instantiate(AssetDatabase.LoadAssetAtPath(objectPath + "UI Elements/Other/Canvas.prefab", typeof(GameObject)), Vector3.zero, Quaternion.identity) as GameObject;
                clone.name = clone.name.Replace("(Clone)", "").Trim();
                Selection.activeObject = clone;

                MakeSceneDirty(clone, clone.name);
            }

            catch { SelectErrorDialog(); }
        }
        #endregion

        #region Button
        [MenuItem("GameObject/DreamOS/Button/Default Button", false, 8)]
        static void DefaultButton()
        {
            CreateObject("UI Elements/Button/Default Button");
        }

        [MenuItem("GameObject/DreamOS/Button/Desktop Button", false, 8)]
        static void DesktopButton()
        {
            CreateObject("UI Elements/Button/Desktop Button");
        }

        [MenuItem("GameObject/DreamOS/Button/Navbar Button", false, 8)]
        static void NavbarButton()
        {
            CreateObject("UI Elements/Button/Navbar Button");
        }

        [MenuItem("GameObject/DreamOS/Button/Taskbar Button", false, 8)]
        static void TaskbarButton()
        {
            CreateObject("UI Elements/Button/Taskbar Button");
        }
        #endregion

        #region Input Field
        [MenuItem("GameObject/DreamOS/Input Field/Input Field (Standard)", false, 8)]
        static void StandardInputField()
        {
            CreateObject("UI Elements/Input Field/Input Field");
        }

        [MenuItem("GameObject/DreamOS/Input Field/Input Field (Alternative)", false, 8)]
        static void AltInputField()
        {
            CreateObject("UI Elements/Input Field/Input Field Alt");
        }
        #endregion

        #region Modal Window
        [MenuItem("GameObject/DreamOS/Modal Window/Standard", false, 8)]
        static void ModalWindow()
        {
            CreateObject("UI Elements/Modal Window/Standard Modal Window");
        }
        #endregion

        #region Scrollbar
        [MenuItem("GameObject/DreamOS/Scrollbar/Standard", false, 8)]
        static void Scrollbar()
        {
            CreateObject("UI Elements/Scrollbar/Scrollbar Vertical");
        }
        #endregion

        #region Selectors
        [MenuItem("GameObject/DreamOS/Selectors/Horizontal Selector", false, 8)]
        static void HorizontalSelector()
        {
            CreateObject("UI Elements/Selectors/Horizontal Selector");
        }

        [MenuItem("GameObject/DreamOS/Selectors/Vertical Selector", false, 8)]
        static void VerticalSelector()
        {
            CreateObject("UI Elements/Selectors/Vertical Selector");
        }
        #endregion

        #region Slider
        [MenuItem("GameObject/DreamOS/Slider/Standard", false, 8)]
        static void Slider()
        {
            CreateObject("UI Elements/Slider/Slider");
        }
        #endregion

        #region Spinners
        [MenuItem("GameObject/DreamOS/Spinners/Default Spinner", false, 8)]
        static void LoaderMaterial()
        {
            CreateObject("UI Elements/Spinner/Default Spinner");
        }
        #endregion

        #region Switch
        [MenuItem("GameObject/DreamOS/Switch/Standard", false, 8)]
        static void Switch()
        {
            CreateObject("UI Elements/Switch/Switch");
        }
        #endregion

        #region UIM
        [MenuItem("GameObject/DreamOS/UI Manager/Image", false, 8)]
        static void UIMImage()
        {
            CreateObject("UI Elements/UIM/Image");
        }

        [MenuItem("GameObject/DreamOS/UI Manager/Text (TMP)", false, 8)]
        static void UIMText()
        {
            CreateObject("UI Elements/UIM/Text (TMP)");
        }
        #endregion
    }
}
#endif