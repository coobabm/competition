using LingGuangV05.Desktop.OS;
using UnityEngine;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>
    /// One browser tab button (moved from DreamOS 2026-10-19; same GUID and fields): page icon and title, a spinner
    /// in place of the icon while loading, and an underline while it is the front tab.
    /// </summary>
    public class WebBrowserTabItem : MonoBehaviour
    {
        public ButtonManager mainButton;
        public ButtonManager closeButton;
        public GameObject iconObject;
        public Spinner spinnerObject;
        public GameObject indicatorObject;
        [HideInInspector] public WebBrowserApp manager;
        [HideInInspector] public string guid;

        void Awake() => DisableSpinner();

        public void EnableSpinner()
        {
            spinnerObject.gameObject.SetActive(true);
            iconObject.SetActive(false);
        }

        public void DisableSpinner()
        {
            spinnerObject.gameObject.SetActive(false);
            iconObject.SetActive(true);
        }

        public void SetData(Sprite icon, string text)
        {
            mainButton.buttonIcon = icon;
            mainButton.buttonText = text;
            mainButton.UpdateUI();
        }

        public void SetIndicator(bool value) => indicatorObject.SetActive(value);
    }
}
