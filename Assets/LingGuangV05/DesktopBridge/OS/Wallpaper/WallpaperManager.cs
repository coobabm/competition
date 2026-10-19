using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// Owns the selected desktop wallpaper: builds the picker buttons from a <see cref="WallpaperLibrary"/>, remembers
    /// the choice and refreshes every <see cref="WallpaperObject"/> that registered with it.
    /// Replaces DreamOS's WallpaperManager (2026-10-19): same GUID, serialized fields and public API.
    ///
    /// The choice is saved in DreamOS's User data as "CurrentWallpaper" (an index into the library).
    /// </summary>
    public class WallpaperManager : MonoBehaviour
    {
        // Resources
        public WallpaperLibrary wallpaperLibrary;
        public GameObject wallpaperItem;

        // Settings
        public int wallpaperIndex;
        public bool saveSelected = true;

        const DreamOSDataManager.DataCategory dataCat = DreamOSDataManager.DataCategory.User;
        readonly List<WallpaperObject> cachedObjects = new List<WallpaperObject>();

        void Awake()
        {
            GetWallpaperData();
        }

        /// <summary>Loads the saved wallpaper index, or saves the inspector's one the first time.</summary>
        public void GetWallpaperData()
        {
            if (!saveSelected)
                return;

            if (DreamOSDataManager.ContainsJsonKey(dataCat, "CurrentWallpaper")) { wallpaperIndex = DreamOSDataManager.ReadIntData(dataCat, "CurrentWallpaper"); }
            else { DreamOSDataManager.WriteIntData(dataCat, "CurrentWallpaper", wallpaperIndex); }
        }

        /// <summary>Fills <paramref name="wallpaperParent"/> with one picker button per wallpaper in the library.</summary>
        public void InitializeWallpapers(Transform wallpaperParent)
        {
            if (wallpaperItem == null) { return; }

            foreach (Transform child in wallpaperParent) { Destroy(child.gameObject); }

            for (int i = 0; i < wallpaperLibrary.wallpapers.Count; ++i)
            {
                int tempIndex = i;

                GameObject go = Instantiate(wallpaperItem, new Vector3(0, 0, 0), Quaternion.identity);
                go.transform.SetParent(wallpaperParent, false);
                go.name = wallpaperLibrary.wallpapers[i].wallpaperID;

                Image preview = go.transform.Find("Image Parent/Image").GetComponent<Image>();
                preview.sprite = wallpaperLibrary.wallpapers[i].wallpaperSprite;

                ButtonManager button = go.GetComponent<ButtonManager>();
                button.onClick.AddListener(delegate { SetWallpaper(tempIndex, true); });
            }
        }

        public void SetWallpaper(int index, bool updateCachedObjects = false)
        {
            if (wallpaperLibrary == null || wallpaperLibrary.wallpapers.Count == 0) return;
            wallpaperIndex = Mathf.Clamp(index, 0, wallpaperLibrary.wallpapers.Count - 1);

            if (saveSelected) { DreamOSDataManager.WriteIntData(dataCat, "CurrentWallpaper", wallpaperIndex); }

            if (updateCachedObjects)
            {
                foreach (WallpaperObject wallpaperObject in cachedObjects)
                {
                    if (wallpaperObject != null) wallpaperObject.UpdateWallpaper(this);
                }
            }
        }

        /// <summary>The sprite at <paramref name="index"/> (clamped into range), or null when the library is empty.</summary>
        public Sprite GetWallpaper(int index)
        {
            if (wallpaperLibrary == null || wallpaperLibrary.wallpapers.Count == 0) return null;
            return wallpaperLibrary.wallpapers[Mathf.Clamp(index, 0, wallpaperLibrary.wallpapers.Count - 1)].wallpaperSprite;
        }

        public void AddWallpaperToLibrary(Sprite wallpaperSprite, string wallpaperName)
        {
            if (wallpaperLibrary == null)
            {
                Debug.LogError("<b>[Wallpaper Manager]</b> Cannot add the wallpaper due to missing library.");
                return;
            }

            WallpaperLibrary.WallpaperItem item = new WallpaperLibrary.WallpaperItem();
            item.wallpaperSprite = wallpaperSprite;
            item.wallpaperID = wallpaperName;
            wallpaperLibrary.wallpapers.Add(item);
        }

        /// <summary>Registers a wallpaper image so <see cref="SetWallpaper"/> can refresh it.</summary>
        public void AddCachedObject(WallpaperObject woInstance, bool updateAfterAdding = false)
        {
            if (woInstance == null) return;
            if (!cachedObjects.Contains(woInstance)) cachedObjects.Add(woInstance);

            if (updateAfterAdding) { woInstance.UpdateWallpaper(this); }
        }
    }
}
