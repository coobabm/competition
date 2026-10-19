using UnityEngine;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// The wallpaper picker grid in Settings: asks the <see cref="WallpaperManager"/> to spawn one button per
    /// wallpaper when it starts. Replaces DreamOS's WallpaperLibraryContainer (2026-10-19): same GUID, serialized
    /// fields and public API.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("LingGuang/OS/Wallpaper/Wallpaper Library Container")]
    public class WallpaperLibraryContainer : MonoBehaviour
    {
        // Resources
        [SerializeField] private WallpaperManager wallpaperManager;

        void Start()
        {
            FetchItems();
        }

        public void FetchItems()
        {
            if (wallpaperManager == null)
            {
#if UNITY_2023_2_OR_NEWER
                WallpaperManager[] found = FindObjectsByType<WallpaperManager>(FindObjectsSortMode.None);
#else
                WallpaperManager[] found = FindObjectsOfType<WallpaperManager>();
#endif
                if (found.Length == 0) { Debug.Log("<b>[Wallpaper Library Container]</b> Wallpaper Manager is missing.", this); return; }
                wallpaperManager = found[0];
            }

            foreach (Transform child in transform) { Destroy(child.gameObject); }

            wallpaperManager.InitializeWallpapers(transform);
        }
    }
}
