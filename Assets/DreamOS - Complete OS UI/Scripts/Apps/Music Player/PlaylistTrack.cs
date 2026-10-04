using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Michsky.DreamOS
{
    public class PlaylistTrack : MonoBehaviour
    {
        [HideInInspector] public MusicPlayerManager manager;
        [HideInInspector] public int itemIndex;
        [HideInInspector] public MusicPlayerPlaylist playlist;
        [HideInInspector] public Sprite cover;
        [HideInInspector] public string title;
        [HideInInspector] public string artist;
        [HideInInspector] public string album;
        [HideInInspector] public PlaylistTrack twinTrack;
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
            if (value == true) { nowPlaying.SetActive(true); }
            else { nowPlaying.SetActive(false); }

            if (twinTrack != null) { twinTrack.SetNowPlayingState(value); }
        }
    }
}