using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Michsky.DreamOS
{
    public class WallpaperManager : MonoBehaviour
    {
        // Resources
        public WallpaperLibrary wallpaperLibrary;
        public GameObject wallpaperItem;

        // Settings
        public int wallpaperIndex;
        public bool saveSelected = true;

        // Helpers
        List<WallpaperObject> cachedObjects = new List<WallpaperObject>();
        DreamOSDataManager.DataCategory dataCat = DreamOSDataManager.DataCategory.User;

        void Awake()
        {
            // if (userManager == null) { userManager = (UserManager)FindObjectsOfType(typeof(UserManager))[0]; }
            // if (userManager != null) { saveKey = userManager.machineID; }

            GetWallpaperData();
        }

        public void GetWallpaperData()
        {
            if (saveSelected && DreamOSDataManager.ContainsJsonKey(dataCat, "CurrentWallpaper")) { wallpaperIndex = DreamOSDataManager.ReadIntData(dataCat, "CurrentWallpaper"); }
            else if (saveSelected && !DreamOSDataManager.ContainsJsonKey(dataCat, "CurrentWallpaper")) { DreamOSDataManager.WriteIntData(dataCat, "CurrentWallpaper", wallpaperIndex); }
        }

        public void InitializeWallpapers(Transform wallpaperParent)
        {
            // Check for wallpaper item preset
            if (wallpaperItem == null) { return; }

            // Destroy all cached objects
            foreach (Transform child in wallpaperParent) { Destroy(child.gameObject); }

            // Create wallpaper preset for each item
            for (int i = 0; i < wallpaperLibrary.wallpapers.Count; ++i)
            {
                int tempIndex = i;

                GameObject go = Instantiate(wallpaperItem, new Vector3(0, 0, 0), Quaternion.identity);
                go.transform.SetParent(wallpaperParent, false);
                go.name = wallpaperLibrary.wallpapers[i].wallpaperID;

                Image prevImage = go.transform.Find("Image Parent/Image").GetComponent<Image>();
                prevImage.sprite = wallpaperLibrary.wallpapers[i].wallpaperSprite;

                ButtonManager wpButton = go.GetComponent<ButtonManager>();
                wpButton.onClick.AddListener(delegate { SetWallpaper(tempIndex, true); });
            }
        }

        public void SetWallpaper(int index, bool updateCachedObjects = false)
        {
            // Set new wallpaper index
            wallpaperIndex = index;

            // Save selected wallpaper if requested
            if (saveSelected) { DreamOSDataManager.WriteIntData(dataCat, "CurrentWallpaper", wallpaperIndex); }

            // Update each cached objects if requested
            if (updateCachedObjects == true)
            {
                foreach (WallpaperObject wo in cachedObjects)
                {
                    wo.UpdateWallpaper();
                }
            }
        }

        public Sprite GetWallpaper(int index)
        {
            return wallpaperLibrary.wallpapers[index].wallpaperSprite;
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

        public void AddCachedObject(WallpaperObject woInstance, bool updateAfterAdding = false)
        {
            // Add the new cached object
            cachedObjects.Add(woInstance);

            // Update the cached object wallpaper after adding
            if (updateAfterAdding) { woInstance.UpdateWallpaper(); }
        }
    }
}