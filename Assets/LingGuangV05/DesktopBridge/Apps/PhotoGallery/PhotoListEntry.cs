using LingGuangV05.Desktop.OS;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>One thumbnail in the photo gallery grid (replaces DreamOS's PhotoGalleryPreset; same GUID and fields).</summary>
    public class PhotoListEntry : MonoBehaviour
    {
        public ButtonManager presetButton;
        public Image photoImage;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI descriptionText;
        public AspectRatioFitter aspectRatioFitter;
        [HideInInspector] public PhotoApp manager;
        [HideInInspector] public int photoIndex;
        [HideInInspector] public string photoTitle;
        [HideInInspector] public bool isCustom;
    }
}
