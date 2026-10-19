using LingGuangV05.Desktop.OS;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>
    /// The handlers behind the desktop Settings window's switches and buttons (2026-10-19). Replaces DreamOS's
    /// SettingsManager: same GUID, serialized fields and method names, so every toggle and button event in the Main
    /// scene still lands here.
    /// </summary>
    public class SettingsApp : MonoBehaviour
    {
        [SerializeField] UIManager UIManagerAsset;
        [SerializeField] Transform accentColorList;
        [SerializeField] Transform accentReversedColorList;
        [SerializeField] ItemDragContainer desktopDragger;
        [SerializeField] UserManager userManager;
        [SerializeField] SpeechRecognition speechRecognition;
        [SerializeField] ModalWindowManager profilePictureWindow;
        [SerializeField] ModalWindowManager resetPasswordWindow;
        [SerializeField] ModalWindowManager wipeUserDataWindow;

        public Sprite defaultWallpaper;

        /// <summary>"Snap desktop icons to a grid".</summary>
        public void SnapDesktopItems(bool value)
        {
            if (desktopDragger == null) return;
            if (value) desktopDragger.SnappedDragMode(); else desktopDragger.FreeDragMode();
        }

        /// <summary>"Remember desktop icon positions".</summary>
        public void SaveDesktopOrder(bool value)
        {
            if (desktopDragger == null) return;
            foreach (Transform child in desktopDragger.transform)
            {
                // Icons added at runtime (灵光.exe and the like) are not draggable.
                var dragger = child.GetComponent<ItemDragger>();
                if (dragger == null) continue;
                dragger.rememberPosition = value;
                if (value && dragger.dragContainer.dragMode == ItemDragContainer.DragMode.Free) dragger.UpdateObject(false);
            }
        }

        public void UseShortTimeFormat(bool value)
        {
            if (DateAndTimeManager.instance != null) DateAndTimeManager.instance.ShortTimeFormat(value);
        }

        public void SpeechRecognition(bool value)
        {
            if (speechRecognition == null) return;
#if UNITY_STANDALONE_WIN || UNITY_WSA
            speechRecognition.EnableSpeechRecognition(value);
#endif
        }

        public void AdjustProfilePicture()
        {
            if (profilePictureWindow != null) profilePictureWindow.OpenWindow();
        }

        public void ResetPassword()
        {
            if (resetPasswordWindow != null) resetPasswordWindow.OpenWindow();
        }

        /// <summary>Asks first; confirming wipes the user's saved data.</summary>
        public void WipeUserData()
        {
            if (wipeUserDataWindow == null) return;
            wipeUserDataWindow.OpenWindow();
            wipeUserDataWindow.onConfirm.RemoveAllListeners();
            wipeUserDataWindow.onConfirm.AddListener(() => { userManager.WipeUserData(); wipeUserDataWindow.CloseWindow(); });
        }

        /// <summary>Turns on the accent colour swatches saved by the custom theme.</summary>
        public void CheckForToggles()
        {
            SelectSaved(accentColorList, "CustomThemeAccentColor");
            SelectSaved(accentReversedColorList, "CustomThemeAccentRevColor");
        }

        static void SelectSaved(Transform list, string key)
        {
            string saved = PlayerPrefs.GetString(key);
            foreach (Transform swatch in list)
            {
                if (swatch.name != saved) continue;
                var toggle = swatch.GetComponent<Toggle>();
                toggle.isOn = true;
                toggle.onValueChanged.Invoke(true);
            }
        }

        public void SelectSystemTheme() => UIManagerAsset.selectedTheme = UIManager.SelectedTheme.Default;

        public void SelectCustomTheme() => UIManagerAsset.selectedTheme = UIManager.SelectedTheme.Custom;

        /// <summary>A swatch's hex code (without '#') becomes the custom theme's accent; its alpha stays.</summary>
        public void ChangeAccentColor(string colorCode)
        {
            ColorUtility.TryParseHtmlString("#" + colorCode, out var c);
            var old = UIManagerAsset.highlightedColorCustom;
            UIManagerAsset.highlightedColorCustom = new Color(c.r, c.g, c.b, old.a);
        }

        public void ChangeAccentReversedColor(string colorCodeReversed)
        {
            ColorUtility.TryParseHtmlString("#" + colorCodeReversed, out var c);
            var old = UIManagerAsset.highlightedColorSecondaryCustom;
            UIManagerAsset.highlightedColorSecondaryCustom = new Color(c.r, c.g, c.b, old.a);
        }
    }
}
