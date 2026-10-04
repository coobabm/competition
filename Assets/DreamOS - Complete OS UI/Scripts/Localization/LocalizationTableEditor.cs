#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
    [CustomEditor(typeof(LocalizationTable))]
    public class LocalizationTableEditor : Editor
    {
        private GUISkin customSkin;
        private LocalizationTable ltTarget;

        private void OnEnable()
        {
            ltTarget = (LocalizationTable)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            if (customSkin == null)
            {
                EditorGUILayout.HelpBox("Editor variables are missing. You can manually fix this by deleting " +
                    "DreamOS > Resources folder and then re-import the package. \n\nIf you're still seeing this " +
                    "dialog even after the re-import, contact me with this ID: " + UIManager.buildID, MessageType.Error);
                return;
            }

            // Tab_Settings Header
            DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 8);
            GUI.enabled = false;

            var tableID = serializedObject.FindProperty("tableID");
            DreamOSEditorHandler.DrawProperty(tableID, customSkin, "Table ID");

            var localizationSettings = serializedObject.FindProperty("localizationSettings");
            DreamOSEditorHandler.DrawProperty(localizationSettings, customSkin, "Settings");

            GUI.enabled = true;

            if (ltTarget.localizationSettings != null && ltTarget.localizationSettings.languages.Count != 0 && GUILayout.Button("Edit Table", customSkin.button))
            {
                for (int i = 0; i < ltTarget.localizationSettings.languages[0].localizationLanguage.tableList.Count; i++)
                {
                    if (ltTarget.localizationSettings.languages[0].localizationLanguage.tableList[i].table == ltTarget)
                        LocalizationTableWindow.ShowWindow(ltTarget.localizationSettings, ltTarget, i);
                }
            }
        }
    }
}
#endif