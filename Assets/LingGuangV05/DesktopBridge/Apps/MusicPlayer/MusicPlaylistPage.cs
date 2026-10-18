using System.Collections.Generic;
using Michsky.DreamOS;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>The page that lists one playlist's tracks (replaces DreamOS's PlaylistPanel; same GUID and fields).</summary>
    public class MusicPlaylistPage : MonoBehaviour
    {
        [HideInInspector] public string panelID;
        [HideInInspector] public List<MusicTrackRow> tracks = new List<MusicTrackRow>();

        public ButtonManager playAllButton;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI countText;
        public Image coverImage;
        public Image bannerImage;
        public Transform contentParent;
    }
}
