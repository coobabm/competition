using UnityEngine;
using UnityEngine.UI;

namespace Michsky.DreamOS
{
    public class GameHubLibraryItem : MonoBehaviour
    {
        [SerializeField] private Image iconObject;
        [SerializeField] private Image bannerObject;
        public ButtonManager playButton;
        [HideInInspector] public int gameIndex;

        public void SetIcon(Sprite icon)
        {
            iconObject.sprite = icon;
        }

        public void SetBanner(Sprite banner)
        {
            bannerObject.sprite = banner;
        }
    }
}