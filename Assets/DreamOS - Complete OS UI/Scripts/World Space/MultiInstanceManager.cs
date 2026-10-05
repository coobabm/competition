using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Michsky.DreamOS
{
    public class MultiInstanceManager : MonoBehaviour
    {
        // Resources
        public Camera playerCamera;

        // Instance List
        public List<InstanceItem> instances = new List<InstanceItem>();

        [System.Serializable]
        public class InstanceItem
        {
            // Core Resoucres
            public WorldSpaceManager worldSpaceManager;
            public Canvas instanceCanvas;
            public UserManager userManager;

            // Editor Only
#if UNITY_EDITOR
            [HideInInspector] public bool isExpanded;
#endif
        }

        void Awake()
        {
            for (int i = 0; i < instances.Count; i++)
            {
                instances[i].userManager.disableUserCreating = true;
                Destroy(instances[i].instanceCanvas.GetComponentInChildren<EventSystem>().gameObject);
            }

            // EventSystem baseES = new GameObject().AddComponent<EventSystem>();
            // baseES.gameObject.AddComponent<InputSystemUIInputModule>();
            // baseES.gameObject.name = "[DreamOS Event System]";
        }

        public void AutoWizard(int instanceIndex)
        {
            // We need to find the resources to initialize the instance
            AutoFindResources(instanceIndex);

            // Destroy all speech recognition comps as it's meant for single-instance purposes
#if UNITY_EDITOR
#if UNITY_2023_2_OR_NEWER
            SpeechRecognition[] srComps = FindObjectsByType<SpeechRecognition>(FindObjectsSortMode.None);
#else
            SpeechRecognition[] srComps = FindObjectsOfType<SpeechRecognition>();
#endif

            foreach (SpeechRecognition tempComp in srComps)
            {
                tempComp.gameObject.SetActive(false);
                DestroyImmediate(tempComp);
            }

            Undo.RecordObject(this, "Destroyed DreamOS speech recognition");
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
        }

        void AutoFindResources(int index)
        {
            instances[index].userManager = instances[index].instanceCanvas.GetComponentInChildren<UserManager>();
            instances[index].userManager.disableUserCreating = true;

            // Set camera
            if (playerCamera == null) { playerCamera = Camera.main; }
            for (int i = 0; i < instances.Count; i++)
            {
                if (playerCamera != null) { instances[i].worldSpaceManager.mainCamera = playerCamera; }
                else { Debug.LogWarning("<b>[DreamOS]</b> No main camera found, player camera is missing.", this); break; }
            }

#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
                return;

            EditorUtility.SetDirty(this);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
        }
    }
}