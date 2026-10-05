using UnityEngine;
using UnityEngine.UI;

namespace Michsky.DreamOS
{
    [RequireComponent(typeof(Image))]
    public class WallpaperObject : MonoBehaviour
    {
        // Resources
        [SerializeField] private WallpaperManager wallpaperManager;
        [SerializeField] private Image targetImage;

        void Awake()
        {
            // Get the target image
            if (targetImage == null) { targetImage = GetComponent<Image>(); }

            // Get the manager
#if UNITY_2023_2_OR_NEWER
            if (wallpaperManager == null && FindObjectsByType<WallpaperManager>(FindObjectsSortMode.None).Length > 0) { wallpaperManager = FindObjectsByType<WallpaperManager>(FindObjectsSortMode.None)[0]; }
#else
            if (wallpaperManager == null && FindObjectsOfType(typeof(WallpaperManager)).Length > 0) { wallpaperManager = (WallpaperManager)FindObjectsOfType(typeof(WallpaperManager))[0]; }
#endif
        }

        void Start()
        {
            // Check for wallpaper manager
            if (wallpaperManager == null) { Debug.LogWarning("<b>[Wallpaper Object]</b> Cannot update the wallpaper because 'Wallpaper Manager' is missing."); }
            else { wallpaperManager.AddCachedObject(this, true); }
        }

        public void UpdateWallpaper()
        {
            // Check for wallpaper manager and return if it's missing
            if (wallpaperManager == null)
                return;

            // Set the wallpaper to targetImage
            targetImage.sprite = wallpaperManager.GetWallpaper(wallpaperManager.wallpaperIndex);
        }

        public void UpdateWallpaper(WallpaperManager manager)
        {
            // Set the wallpaper to targetImage
            targetImage.sprite = manager.GetWallpaper(wallpaperManager.wallpaperIndex);
        }
    }
}