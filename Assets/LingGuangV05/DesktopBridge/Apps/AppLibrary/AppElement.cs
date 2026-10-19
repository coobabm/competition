using System.Collections.Generic;
using LingGuangV05.Desktop.OS;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>
    /// An app's icon, title or gradient anywhere on the desktop (2026-10-19). Replaces DreamOS's AppElement: same
    /// GUID and fields. <see cref="appID"/> names an entry of the <see cref="AppLibrary"/>; the element shows that
    /// entry's icon (small/medium/big), its title (localized when the object has a LocalizedObject) or its gradient.
    /// Siblings follow this element's app.
    /// </summary>
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public class AppElement : MonoBehaviour
    {
        public enum ElementType { None, Icon, Title, Gradient }
        public enum IconSize { Small, Medium, Big }

        public AppLibrary appLibrary;
        public List<AppElement> siblings = new List<AppElement>();

        public string appID;
        public ElementType elementType;
        public IconSize iconSize;
        public bool useGradient = true;

        public int tempAppIndex;
        bool useLocalization = true;
        UIGradient imageGradient;
        Image imageObject;
        TextMeshProUGUI textObject;

        void Awake()
        {
            try
            {
                if (appLibrary == null) appLibrary = Resources.Load<AppLibrary>("Apps/App Library");
                if (elementType == ElementType.Icon && imageObject == null)
                {
                    imageObject = GetComponent<Image>();
                    if (useGradient) imageGradient = GetComponent<UIGradient>();
                }
                else if (elementType == ElementType.Gradient && imageGradient == null) imageGradient = GetComponent<UIGradient>();
                else if (elementType == ElementType.Title && textObject == null) textObject = GetComponent<TextMeshProUGUI>();

                UpdateLibrary();
                UpdateElement();
            }
            catch { Debug.LogWarning("<b>[App Element]</b> 'App Library' is missing.", this); }
        }

        void Update()
        {
            if (appLibrary == null) return;
            if (appLibrary.alwaysUpdate) UpdateElement();
            // With optimizeUpdates the element only refreshes when asked (UpdateLibrary re-enables it).
            if (Application.isPlaying && appLibrary.optimizeUpdates) enabled = false;
        }

        /// <summary>Finds <see cref="appID"/> in the library and re-enables the element.</summary>
        public void UpdateLibrary()
        {
            int index = appLibrary.apps.FindIndex(a => a.appTitle == appID);
            if (index >= 0) tempAppIndex = index;
            enabled = true;
        }

        public void UpdateElement()
        {
            if (tempAppIndex >= appLibrary.apps.Count || appLibrary.apps[tempAppIndex].appTitle != appID) return;
            var app = appLibrary.apps[tempAppIndex];

            if (elementType == ElementType.Icon && imageObject != null)
            {
                imageObject.sprite = iconSize == IconSize.Small ? app.appIconSmall
                    : iconSize == IconSize.Medium ? app.appIconMedium : app.appIconBig;
                if (useGradient && imageGradient != null) Paint(app);
            }
            else if (elementType == ElementType.Gradient && imageGradient != null) Paint(app);
            else if (elementType == ElementType.Title && textObject != null) ShowTitle(app);

            if (!Application.isPlaying) return;
            foreach (var sibling in siblings)
            {
                if (sibling == null) continue;
                sibling.appID = appID;
                sibling.UpdateLibrary();
                sibling.UpdateElement();
            }
        }

        void Paint(AppLibrary.AppItem app)
        {
            imageGradient.color1 = app.gradientLeft;
            imageGradient.color2 = app.gradientRight;
            // Toggling the effect makes the graphic rebuild with the new colours.
            imageGradient.enabled = false;
            imageGradient.enabled = true;
        }

        void ShowTitle(AppLibrary.AppItem app)
        {
            if (!Application.isPlaying || !useLocalization) { textObject.text = app.appTitle; return; }

            var loc = GetComponent<LocalizedObject>();
            if (loc == null || !loc.CheckLocalizationStatus())
            {
                useLocalization = false;
                textObject.text = app.appTitle;
                return;
            }
            loc.localizationKey = app.localizationKey;
            loc.onLanguageChanged.AddListener(_ => textObject.text = loc.GetKeyOutput(loc.localizationKey));
            loc.InitializeItem();
            loc.UpdateItem();
        }
    }
}
