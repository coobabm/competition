#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.UI.Reach
{
    public class InitReachUI
    {
        [InitializeOnLoad]
        public class InitOnLoad
        {
            static InitOnLoad()
            {
                if (!EditorPrefs.HasKey("ReachUI.HasCustomEditorData"))
                {
                    string darkPath = AssetDatabase.GetAssetPath(Resources.Load("ReachEditor-Dark"));
                    string lightPath = AssetDatabase.GetAssetPath(Resources.Load("ReachEditor-Light"));

                    EditorPrefs.SetString("ReachUI.CustomEditorDark", darkPath);
                    EditorPrefs.SetString("ReachUI.CustomEditorLight", lightPath);
                    EditorPrefs.SetInt("ReachUI.HasCustomEditorData", 1);
                }
            }
        }
    }
}
#endif