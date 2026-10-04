#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.DreamOS
{
    [CustomEditor(typeof(AudioManager))]
    public class AudioManagerEditor : Editor
    {
        private AudioManager amTarget;
        private GUISkin customSkin;

        private void OnEnable()
        {
            amTarget = (AudioManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = DreamOSEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = DreamOSEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            var UIManagerAsset = serializedObject.FindProperty("UIManagerAsset");
            var audioSource = serializedObject.FindProperty("audioSource");
            var mixer = serializedObject.FindProperty("mixer");
            var masterSlider = serializedObject.FindProperty("masterSlider");
            var taskbarIndicator = serializedObject.FindProperty("taskbarIndicator");
            var mixerIndicator = serializedObject.FindProperty("mixerIndicator");

            var volumeMuted = serializedObject.FindProperty("volumeMuted");
            var volumeLow = serializedObject.FindProperty("volumeLow");
            var volumeHigh = serializedObject.FindProperty("volumeHigh");

            DreamOSEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
            DreamOSEditorHandler.DrawProperty(UIManagerAsset, customSkin, "UI Manager");
            DreamOSEditorHandler.DrawProperty(audioSource, customSkin, "Audio Source");
            DreamOSEditorHandler.DrawProperty(mixer, customSkin, "Mixer");
            DreamOSEditorHandler.DrawProperty(masterSlider, customSkin, "Master Slider");
            DreamOSEditorHandler.DrawProperty(taskbarIndicator, customSkin, "Taskbar Indicator");
            DreamOSEditorHandler.DrawProperty(mixerIndicator, customSkin, "Mixer Indicator");

            DreamOSEditorHandler.DrawHeader(customSkin, "Header_Settings", 10);
            DreamOSEditorHandler.DrawProperty(volumeMuted, customSkin, "Volume Muted");
            DreamOSEditorHandler.DrawProperty(volumeLow, customSkin, "Volume Low");
            DreamOSEditorHandler.DrawProperty(volumeHigh, customSkin, "Volume High");
            DreamOSEditorHandler.DrawProperty(serializedObject.FindProperty("mouseStrokeVolume"), customSkin, "Mouse Click Volume");

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif