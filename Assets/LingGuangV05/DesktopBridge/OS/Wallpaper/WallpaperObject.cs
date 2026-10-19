using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// Put on an Image that should show the current desktop wallpaper. It finds the <see cref="WallpaperManager"/>,
    /// registers with it on Start and gets its sprite refreshed whenever the wallpaper changes.
    /// Replaces DreamOS's WallpaperObject (2026-10-19): same GUID, serialized fields and public API.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class WallpaperObject : MonoBehaviour
    {
        // Resources
        [SerializeField] private WallpaperManager wallpaperManager;
        [SerializeField] private Image targetImage;

        void Awake()
        {
            if (targetImage == null) { targetImage = GetComponent<Image>(); }

            if (wallpaperManager == null)
            {
#if UNITY_2023_2_OR_NEWER
                WallpaperManager[] found = FindObjectsByType<WallpaperManager>(FindObjectsSortMode.None);
#else
                WallpaperManager[] found = FindObjectsOfType<WallpaperManager>();
#endif
                if (found.Length > 0) { wallpaperManager = found[0]; }
            }
        }

        void Start()
        {
            if (wallpaperManager == null) { Debug.LogWarning("<b>[Wallpaper Object]</b> Cannot update the wallpaper because 'Wallpaper Manager' is missing."); }
            else { wallpaperManager.AddCachedObject(this, true); }
        }

        public void UpdateWallpaper()
        {
            if (wallpaperManager == null)
                return;

            targetImage.sprite = wallpaperManager.GetWallpaper(wallpaperManager.wallpaperIndex);
        }

        /// <summary>Called by the manager: adopts it as our manager and shows its current wallpaper.</summary>
        public void UpdateWallpaper(WallpaperManager manager)
        {
            if (manager == null) return;
            wallpaperManager = manager;
            if (targetImage == null) targetImage = GetComponent<Image>();
            targetImage.sprite = manager.GetWallpaper(manager.wallpaperIndex);
        }
    }
}
