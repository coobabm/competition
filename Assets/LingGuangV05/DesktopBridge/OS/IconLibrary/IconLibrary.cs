using System.Collections.Generic;
using UnityEngine;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// The desktop's icon set: one entry per named icon with its 32/64/128/256 px sprites, looked up by title by
    /// <see cref="IconManager"/> (default asset: Resources/Icons/Icon Library). Replaces DreamOS's IconLibrary
    /// (2026-10-19): same GUID, serialized fields and public API.
    /// </summary>
    [CreateAssetMenu(fileName = "New Icon Library", menuName = "LingGuang/OS/New Icon Library")]
    public class IconLibrary : ScriptableObject
    {
        // Settings
        public bool alwaysUpdate = false;
        public bool optimizeUpdates = true;
        public Texture2D searchIcon;

        // Content
        public List<IconItem> icons = new List<IconItem>();

        [System.Serializable]
        public class IconItem
        {
            public string iconTitle = "Icon";
            public Texture2D iconPreview;
            public Sprite iconSprite32;
            public Sprite iconSprite64;
            public Sprite iconSprite128;
            public Sprite iconSprite256;
        }
    }
}
