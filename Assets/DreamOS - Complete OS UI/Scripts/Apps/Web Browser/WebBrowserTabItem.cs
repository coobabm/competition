using UnityEngine;

namespace Michsky.DreamOS
{
    public class WebBrowserTabItem : MonoBehaviour
    {
        public ButtonManager mainButton;
        public ButtonManager closeButton;
        public GameObject iconObject;
        public Spinner spinnerObject;
        public GameObject indicatorObject;
        [HideInInspector] public WebBrowserManager manager;
        [HideInInspector] public string guid;

        void Awake()
        {
            DisableSpinner();
        }

        public void EnableSpinner()
        {
            spinnerObject.gameObject.SetActive(true);
            iconObject.gameObject.SetActive(false);
        }

        public void DisableSpinner()
        {
            spinnerObject.gameObject.SetActive(false);
            iconObject.gameObject.SetActive(true);
        }

        public void SetData(Sprite icon, string text)
        {
            mainButton.buttonIcon = icon;
            mainButton.buttonText = text;
            mainButton.UpdateUI();
        }

        public void SetIndicator(bool value)
        {
            indicatorObject.SetActive(value);
        }
    }
}
