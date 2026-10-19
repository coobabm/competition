using LingGuangV05.Desktop.OS;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>One video in the player's library (replaces DreamOS's VideoPreset; same GUID and fields).</summary>
    public class VideoListEntry : MonoBehaviour
    {
        public ButtonManager presetButton;
        public Image coverImage;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI descriptionText;
        public TextMeshProUGUI durationText;
        public VideoApp.VideoType type;
        [HideInInspector] public VideoApp manager;
        [HideInInspector] public int videoIndex;
        [HideInInspector] public string videoURL;
    }
}
