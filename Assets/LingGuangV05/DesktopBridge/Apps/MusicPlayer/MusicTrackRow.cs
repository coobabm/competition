using Michsky.DreamOS;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>
    /// One track row on a playlist page or in the library (replaces DreamOS's PlaylistTrack; same GUID and fields).
    /// A library row and its playlist-page row are twins: both show the "now playing" mark together.
    /// </summary>
    public class MusicTrackRow : MonoBehaviour
    {
        [HideInInspector] public MusicApp manager;
        [HideInInspector] public int itemIndex;
        [HideInInspector] public MusicPlaylist playlist;
        [HideInInspector] public Sprite cover;
        [HideInInspector] public string title;
        [HideInInspector] public string artist;
        [HideInInspector] public string album;
        [HideInInspector] public MusicTrackRow twinTrack;
        [HideInInspector] public Color accentColor;
        [HideInInspector] public Color accentMatchColor;

        public ButtonManager button;
        public Image backgroundImage;
        public Image coverImage;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI artistText;
        public TextMeshProUGUI durationText;
        public GameObject nowPlaying;

        public void SetNowPlayingState(bool value)
        {
            nowPlaying.SetActive(value);
            if (twinTrack != null) twinTrack.SetNowPlayingState(value);
        }
    }
}
