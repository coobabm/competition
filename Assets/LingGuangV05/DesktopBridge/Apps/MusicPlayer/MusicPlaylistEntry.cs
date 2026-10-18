using Michsky.DreamOS;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>A playlist button in the music player's sidebar (replaces DreamOS's PlaylistItem; same GUID and fields).</summary>
    public class MusicPlaylistEntry : MonoBehaviour
    {
        [HideInInspector] public string playlistID;
        public ButtonManager playlistButton;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI countText;
        public Image coverImage;
    }
}
