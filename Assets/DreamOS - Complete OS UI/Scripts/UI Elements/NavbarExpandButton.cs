using UnityEngine;

namespace Michsky.DreamOS
{
    public class NavbarExpandButton : MonoBehaviour
    {
        [Header("Resources")]
        public AnimatedIconHandler animatedIcon;

        [Header("Settings")]
        public string appName = "App Name";

        // Helpers
        public DreamOSDataManager.DataCategory dataCategory = DreamOSDataManager.DataCategory.Apps;

        void OnEnable()
        {
            if (animatedIcon == null)
                return;

            string dataKey = appName + "_NavDrawer";

            if (DreamOSDataManager.ContainsJsonKey(dataCategory, dataKey) && DreamOSDataManager.ReadBooleanData(dataCategory, dataKey)) { animatedIcon.PlayIn(); }
            else if (DreamOSDataManager.ContainsJsonKey(dataCategory, dataKey) && !DreamOSDataManager.ReadBooleanData(dataCategory, dataKey)) { animatedIcon.PlayOut(); }
            else { animatedIcon.PlayStart(); }
        }
    }
}