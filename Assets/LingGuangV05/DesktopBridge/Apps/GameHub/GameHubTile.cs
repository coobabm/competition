using LingGuangV05.Desktop.OS;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>A game's tile in the game center library (replaces DreamOS's GameHubLibraryItem; same GUID and fields).</summary>
    public class GameHubTile : MonoBehaviour
    {
        [SerializeField] Image iconObject;
        [SerializeField] Image bannerObject;
        public ButtonManager playButton;
        [HideInInspector] public int gameIndex;

        public void SetIcon(Sprite icon) => iconObject.sprite = icon;

        public void SetBanner(Sprite banner) => bannerObject.sprite = banner;
    }
}
