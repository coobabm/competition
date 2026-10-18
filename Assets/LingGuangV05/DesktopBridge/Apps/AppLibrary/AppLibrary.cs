using System.Collections.Generic;
using UnityEngine;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>
    /// The desktop's list of apps: title, localization key, icons and gradient per app (replaces DreamOS's AppLibrary;
    /// same GUID and fields, so "Resources/Apps/App Library" and the Aero copies load unchanged).
    /// </summary>
    [CreateAssetMenu(fileName = "New App Library", menuName = "LingGuang/App Library")]
    public class AppLibrary : ScriptableObject
    {
        [System.Serializable]
        public class AppItem
        {
            public string appTitle = "App Name";
            public string localizationKey;
            public Texture2D appIconPreview;
            public Sprite appIconBig;
            public Sprite appIconMedium;
            public Sprite appIconSmall;
            public Color gradientLeft = new Color32(215, 215, 215, 255);
            public Color gradientRight = new Color32(255, 255, 255, 255);
        }

        [Tooltip("AppElements refresh every frame (editor previews).")]
        public bool alwaysUpdate = false;
        [Tooltip("In Play, AppElements switch themselves off after refreshing until asked again.")]
        public bool optimizeUpdates = true;
        public List<AppItem> apps = new List<AppItem>();
    }
}
