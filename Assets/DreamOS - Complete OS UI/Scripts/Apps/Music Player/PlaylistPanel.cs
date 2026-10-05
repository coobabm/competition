using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Michsky.DreamOS
{
    public class PlaylistPanel : MonoBehaviour
    {
        [HideInInspector] public string panelID;
        [HideInInspector] public List<PlaylistTrack> tracks = new List<PlaylistTrack>();

        public ButtonManager playAllButton;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI countText;
        public Image coverImage;
        public Image bannerImage;
        public Transform contentParent;
    }
}