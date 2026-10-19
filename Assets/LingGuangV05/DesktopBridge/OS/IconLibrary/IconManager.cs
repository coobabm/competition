using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// Sets an <see cref="Image"/>'s sprite from the <see cref="IconLibrary"/> by icon title and size
    /// (0 = 32 px ... 3 = 256 px). Replaces DreamOS's IconManager (2026-10-19): same GUID, serialized fields and
    /// public API.
    ///
    /// Runs in edit mode too, so authored icons preview in the scene. After the first update it switches itself off
    /// unless the library asks for continuous updates.
    /// </summary>
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [AddComponentMenu("LingGuang/OS/Image/Icon Manager")]
    [RequireComponent(typeof(Image))]
    public class IconManager : MonoBehaviour
    {
        // Resources
        public IconLibrary iconLibrary;

        // Icon Info
        public string selectedIconID;
        public int selectedIconIndex;
        [Range(0, 3)] public int spriteSize;

        [HideInInspector] public Image imageObject;
        [HideInInspector] public string currentSize;
        [HideInInspector] public bool size32;
        [HideInInspector] public bool size64;
        [HideInInspector] public bool size128;
        [HideInInspector] public bool size256;

        void Awake()
        {
            try
            {
                if (iconLibrary == null) { iconLibrary = Resources.Load<IconLibrary>("Icons/Icon Library"); }
                if (imageObject == null) { imageObject = gameObject.GetComponent<Image>(); }

                enabled = true;
                UpdateElement();
            }

            catch { Debug.LogWarning("<b>Icon Library</b> is missing, but it should be assigned.", this); }
        }

        void Update()
        {
            if (iconLibrary != null && iconLibrary.alwaysUpdate == true) { UpdateElement(); }
            if (Application.isPlaying == true && iconLibrary != null && iconLibrary.optimizeUpdates == true) { enabled = false; }
        }

        public void UpdateElement()
        {
            for (int i = 0; i < iconLibrary.icons.Count; i++)
            {
                if (selectedIconID == iconLibrary.icons[i].iconTitle && gameObject.activeInHierarchy == true)
                {
                    imageObject.sprite = GetSprite(iconLibrary.icons[i], spriteSize, imageObject.sprite);
                    break;
                }
            }

            if (iconLibrary.alwaysUpdate == false)
                enabled = false;
        }

        public void UpdateSpriteSize(int spriteIndex, int newSize)
        {
            imageObject.sprite = GetSprite(iconLibrary.icons[spriteIndex], newSize, imageObject.sprite);
        }

        public void ChangeIcon(string newSprite, int preferredSize)
        {
            int selectedSpriteIndex = -1;

            for (int i = 0; i < iconLibrary.icons.Count; i++)
            {
                if (newSprite == iconLibrary.icons[i].iconTitle)
                {
                    selectedSpriteIndex = i;
                    break;
                }
            }

            if (selectedSpriteIndex != -1) { UpdateSpriteSize(selectedSpriteIndex, preferredSize); }
            else { Debug.Log("<b>[Icon Manager]</b> Cannot find an icon named '" + newSprite + "'"); }
        }

        // An out-of-range size leaves the current sprite alone.
        static Sprite GetSprite(IconLibrary.IconItem icon, int size, Sprite current)
        {
            switch (size)
            {
                case 0: return icon.iconSprite32;
                case 1: return icon.iconSprite64;
                case 2: return icon.iconSprite128;
                case 3: return icon.iconSprite256;
                default: return current;
            }
        }
    }
}
