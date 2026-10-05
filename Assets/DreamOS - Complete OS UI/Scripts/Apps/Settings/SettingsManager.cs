using UnityEngine;
using UnityEngine.UI;

namespace Michsky.DreamOS
{
    public class SettingsManager : MonoBehaviour
    {
        // Resources
        [SerializeField] private UIManager UIManagerAsset;
        [SerializeField] private Transform accentColorList;
        [SerializeField] private Transform accentReversedColorList;
        [SerializeField] private ItemDragContainer desktopDragger;
        [SerializeField] private UserManager userManager;
        [SerializeField] private SpeechRecognition speechRecognition;
        [SerializeField] private ModalWindowManager profilePictureWindow;
        [SerializeField] private ModalWindowManager resetPasswordWindow;
        [SerializeField] private ModalWindowManager wipeUserDataWindow;

        // Settings
        public Sprite defaultWallpaper;

        // Debug
        Toggle toggleHelper;

        public void SnapDesktopItems(bool value)
        {
            if (desktopDragger == null)
                return;

            if (value == true) { desktopDragger.SnappedDragMode(); }
            else { desktopDragger.FreeDragMode(); }
        }

        public void SaveDesktopOrder(bool value)
        {
            if (desktopDragger == null)
                return;

            if (value == true) 
            {
                for (int i = 0; i < desktopDragger.transform.childCount; ++i)
                {
                    ItemDragger tempDragger = desktopDragger.transform.GetChild(i).GetComponent<ItemDragger>();
                    tempDragger.rememberPosition = true;
                    if (tempDragger.dragContainer.dragMode == ItemDragContainer.DragMode.Free) { tempDragger.UpdateObject(false); }
                }
            }

            else 
            {
                for (int i = 0; i < desktopDragger.transform.childCount; ++i)
                {
                    ItemDragger tempDragger = desktopDragger.transform.GetChild(i).GetComponent<ItemDragger>();
                    tempDragger.rememberPosition = false;
                }
            }
        }

        public void UseShortTimeFormat(bool value)
        {
            if (DateAndTimeManager.instance == null)
                return;

            DateAndTimeManager.instance.ShortTimeFormat(value);
        }

        public void SpeechRecognition(bool value)
        {
            if (speechRecognition == null)
                return;

#if UNITY_STANDALONE_WIN || UNITY_WSA
            speechRecognition.EnableSpeechRecognition(value);
#endif
        }

        public void AdjustProfilePicture()
        {
            if (profilePictureWindow == null)
                return;

            profilePictureWindow.OpenWindow();
        }

        public void ResetPassword()
        {
            if (resetPasswordWindow == null)
                return;

            resetPasswordWindow.OpenWindow();
        }

        public void WipeUserData()
        {
            if (wipeUserDataWindow == null)
                return;

            wipeUserDataWindow.OpenWindow();
            wipeUserDataWindow.onConfirm.RemoveAllListeners();
            wipeUserDataWindow.onConfirm.AddListener(delegate { userManager.WipeUserData(); wipeUserDataWindow.CloseWindow(); });
        }

        public void CheckForToggles()
        {
            // Invoke color toggle depending on the data
            foreach (Transform obj in accentColorList)
            {
                if (obj.name == PlayerPrefs.GetString("CustomTheme" + "AccentColor"))
                {
                    toggleHelper = obj.GetComponent<Toggle>();
                    toggleHelper.isOn = true;
                    toggleHelper.onValueChanged.Invoke(true);
                }
            }

            foreach (Transform obj in accentReversedColorList)
            {
                if (obj.name == PlayerPrefs.GetString("CustomTheme" + "AccentRevColor"))
                {
                    toggleHelper = obj.GetComponent<Toggle>();
                    toggleHelper.isOn = true;
                    toggleHelper.onValueChanged.Invoke(true);
                }
            }
        }

        public void SelectSystemTheme()
        {
            UIManagerAsset.selectedTheme = UIManager.SelectedTheme.Default;
        }

        public void SelectCustomTheme()
        {
            UIManagerAsset.selectedTheme = UIManager.SelectedTheme.Custom;
        }

        public void ChangeAccentColor(string colorCode)
        {
            // Change color depending on the color code
            Color colorHelper;
            ColorUtility.TryParseHtmlString("#" + colorCode, out colorHelper);
            UIManagerAsset.highlightedColorCustom = new Color(colorHelper.r, colorHelper.g, colorHelper.b, UIManagerAsset.highlightedColorCustom.a);
        }

        public void ChangeAccentReversedColor(string colorCodeReversed)
        {
            // Change color depending on the color code
            Color colorHelper;
            ColorUtility.TryParseHtmlString("#" + colorCodeReversed, out colorHelper);
            UIManagerAsset.highlightedColorSecondaryCustom = new Color(colorHelper.r, colorHelper.g, colorHelper.b, UIManagerAsset.highlightedColorSecondaryCustom.a);
        }
    }
}