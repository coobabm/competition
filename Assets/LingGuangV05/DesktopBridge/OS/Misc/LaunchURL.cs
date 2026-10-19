using UnityEngine;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// Opens a link in the system browser from a UnityEvent (OpenURL is wired to buttons in scenes). Replaces
    /// DreamOS's LaunchURL (2026-10-19): same GUID and public API.
    /// </summary>
    public class LaunchURL : MonoBehaviour
    {
        public void OpenURL(string urlLink)
        {
            Application.OpenURL(urlLink);
        }
    }
}
