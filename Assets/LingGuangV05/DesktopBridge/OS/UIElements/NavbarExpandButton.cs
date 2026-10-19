using UnityEngine;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// Sets a navigation-drawer toggle icon to match whether the drawer was left open (2026-10-19). Replaces DreamOS's
    /// NavbarExpandButton: same GUID, serialized fields and public API.
    ///
    /// On enable it reads "&lt;appName&gt;_NavDrawer" from the saved data category: true plays the icon's "In" state,
    /// false its "Out" state, and no saved value its "Start" state.
    /// </summary>
    public class NavbarExpandButton : MonoBehaviour
    {
        [Header("Resources")]
        public AnimatedIconHandler animatedIcon;

        [Header("Settings")]
        public string appName = "App Name";

        public DreamOSDataManager.DataCategory dataCategory = DreamOSDataManager.DataCategory.Apps;

        void OnEnable()
        {
            if (animatedIcon == null) { return; }

            string dataKey = appName + "_NavDrawer";

            if (!DreamOSDataManager.ContainsJsonKey(dataCategory, dataKey)) { animatedIcon.PlayStart(); }
            else if (DreamOSDataManager.ReadBooleanData(dataCategory, dataKey)) { animatedIcon.PlayIn(); }
            else { animatedIcon.PlayOut(); }
        }
    }
}
