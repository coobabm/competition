using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Michsky.DreamOS
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [AddComponentMenu("DreamOS/Apps/App Element")]
    public class AppElement : MonoBehaviour
    {
        // Resources
        public AppLibrary appLibrary;
        public List<AppElement> siblings = new List<AppElement>();

        // Settings
        public string appID;
        public ElementType elementType;
        public IconSize iconSize;
        public bool useGradient = true;

        // Helpers
        public int tempAppIndex;
        bool useLocalization = true;
        UIGradient imageGradient;
        Image imageObject;
        TextMeshProUGUI textObject;

        public enum ElementType { None, Icon, Title, Gradient }
        public enum IconSize { Small, Medium, Big }

        void Awake()
        {
            try
            {
                if (appLibrary == null) { appLibrary = Resources.Load<AppLibrary>("Apps/App Library"); }
                if (elementType == ElementType.Icon && imageObject == null)
                {
                    imageObject = gameObject.GetComponent<Image>();
                    if (useGradient) { imageGradient = gameObject.GetComponent<UIGradient>(); }
                }
                else if (elementType == ElementType.Gradient && imageGradient == null) { imageGradient = gameObject.GetComponent<UIGradient>(); }
                else if (elementType == ElementType.Title && textObject == null) { textObject = gameObject.GetComponent<TextMeshProUGUI>(); }

                UpdateLibrary();
                UpdateElement();
            }

            catch { Debug.LogWarning("<b>[App Element]</b> 'App Library' is missing.", this); }
        }

        void Update()
        {
            if (appLibrary != null && appLibrary.alwaysUpdate) { UpdateElement(); }
            if (Application.isPlaying && appLibrary.optimizeUpdates) { this.enabled = false; }
        }

        public void UpdateLibrary()
        {
            for (int i = 0; i < appLibrary.apps.Count; i++)
            {
                if (appID == appLibrary.apps[i].appTitle) 
                { 
                    tempAppIndex = i; 
                    break; 
                }
            }

            this.enabled = true;
        }

        public void UpdateElement()
        {
            if (tempAppIndex >= appLibrary.apps.Count || appLibrary.apps[tempAppIndex].appTitle != appID)
                return;

            if (elementType == ElementType.Icon && imageObject != null)
            {
                if (iconSize == IconSize.Small) { imageObject.sprite = appLibrary.apps[tempAppIndex].appIconSmall; }
                else if (iconSize == IconSize.Medium) { imageObject.sprite = appLibrary.apps[tempAppIndex].appIconMedium; }
                else if (iconSize == IconSize.Big) { imageObject.sprite = appLibrary.apps[tempAppIndex].appIconBig; }

                if (useGradient && imageGradient != null)
                {
                    imageGradient.color1 = appLibrary.apps[tempAppIndex].gradientLeft;
                    imageGradient.color2 = appLibrary.apps[tempAppIndex].gradientRight;
                    imageGradient.enabled = false;
                    imageGradient.enabled = true;
                }
            }

            else if (elementType == ElementType.Gradient && imageGradient != null)
            {
                imageGradient.color1 = appLibrary.apps[tempAppIndex].gradientLeft;
                imageGradient.color2 = appLibrary.apps[tempAppIndex].gradientRight;
                imageGradient.enabled = false;
                imageGradient.enabled = true;
            }

            else if (elementType == ElementType.Title && textObject != null && !Application.isPlaying)
            {
                textObject.text = appLibrary.apps[tempAppIndex].appTitle;
            }

            else if (elementType == ElementType.Title && textObject != null && useLocalization && Application.isPlaying)
            {
                LocalizedObject tempLoc = gameObject.GetComponent<LocalizedObject>();

                if (tempLoc == null || !tempLoc.CheckLocalizationStatus()) 
                {
                    useLocalization = false;
                    textObject.text = appLibrary.apps[tempAppIndex].appTitle;
                }

                else if (tempLoc != null)
                {
                    tempLoc.localizationKey = appLibrary.apps[tempAppIndex].localizationKey;
                    tempLoc.onLanguageChanged.AddListener(delegate { textObject.text = tempLoc.GetKeyOutput(tempLoc.localizationKey); });
                    tempLoc.InitializeItem();
                    tempLoc.UpdateItem();
                }
            }

            else if (elementType == ElementType.Title && textObject != null && !useLocalization)
            {
                textObject.text = appLibrary.apps[tempAppIndex].appTitle;
            }

            // Check for connected siblings
            if (Application.isPlaying && siblings.Count > 0)
            {
                foreach (AppElement ae in siblings)
                {
                    if (ae == null)
                        continue;

                    ae.appID = appID;
                    ae.UpdateLibrary();
                    ae.UpdateElement();
                }
            }
        }
    }
}