using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Michsky.DreamOS
{
    public class PhotoGalleryPreset : MonoBehaviour
    {
        public ButtonManager presetButton;
        public Image photoImage;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI descriptionText;
        public AspectRatioFitter aspectRatioFitter;
        [HideInInspector] public PhotoGalleryManager manager;
        [HideInInspector] public int photoIndex;
        [HideInInspector] public string photoTitle;
        [HideInInspector] public bool isCustom;
    }
}