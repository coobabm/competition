using System.Collections.Generic;
using UnityEngine;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// The wallpapers the desktop can show: an ID and a sprite each.
    /// Replaces DreamOS's WallpaperLibrary (2026-10-19): same GUID, serialized fields and public API.
    /// </summary>
    [CreateAssetMenu(fileName = "New Wallpaper Library", menuName = "LingGuang/OS/New Wallpaper Library")]
    public class WallpaperLibrary : ScriptableObject
    {
        public List<WallpaperItem> wallpapers = new List<WallpaperItem>();

        [System.Serializable]
        public class WallpaperItem
        {
            public string wallpaperID = "Wallpaper";
            public Sprite wallpaperSprite;
        }
    }
}
