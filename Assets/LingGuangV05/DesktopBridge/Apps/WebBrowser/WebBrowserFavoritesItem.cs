using LingGuangV05.Desktop.OS;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>One row of the browser's favorites list (moved from DreamOS 2026-10-19; same GUID and fields).</summary>
    public class WebBrowserFavoritesItem : MonoBehaviour
    {
        public Image iconObject;
        public TextMeshProUGUI titleObject;
        public TextMeshProUGUI urlObject;
        public ButtonManager button;

        [HideInInspector] public WebBrowserApp manager;
        [HideInInspector] public string url;

        /// <summary>Wired to the row's star toggle.</summary>
        public void SetFavorite(bool value) => manager.SetFavoriteState(value, url);
    }
}
