using UnityEngine;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>
    /// Lets buttons inside a web page prefab drive the browser that shows it (moved from DreamOS 2026-10-19; same GUID
    /// and methods, so the pages' UnityEvents bind unchanged).
    /// </summary>
    [DisallowMultipleComponent]
    public class WebBrowserEventSender : MonoBehaviour
    {
        WebBrowserApp browser;

        void Start() => browser = GetComponentInParent<WebBrowserApp>();

        public void GoBack() => browser.GoBack();
        public void GoForward() => browser.GoForward();
        public void Refresh() => browser.Refresh();
        public void OpenHome() => browser.OpenHomePage();
        public void OpenPage(string pageUrl) => browser.OpenPage(pageUrl);
        public void DownloadFile(string fileName) => browser.DownloadFile(fileName);
    }
}
