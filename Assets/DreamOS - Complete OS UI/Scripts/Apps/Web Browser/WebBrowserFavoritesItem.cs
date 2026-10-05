using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Michsky.DreamOS
{
    public class WebBrowserFavoritesItem : MonoBehaviour
    {
        public Image iconObject;
        public TextMeshProUGUI titleObject;
        public TextMeshProUGUI urlObject;
        public ButtonManager button;

        // Helpers
        [HideInInspector] public WebBrowserManager manager;
        [HideInInspector] public string url;

        public void SetFavorite(bool value)
        {
            manager.SetFavoriteState(value, url);
        }
    }
}