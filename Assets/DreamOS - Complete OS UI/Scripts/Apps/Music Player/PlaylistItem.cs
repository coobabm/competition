using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Michsky.DreamOS
{
    public class PlaylistItem : MonoBehaviour
    {
        [HideInInspector] public string playlistID;
        public ButtonManager playlistButton;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI countText;
        public Image coverImage;
    }
}