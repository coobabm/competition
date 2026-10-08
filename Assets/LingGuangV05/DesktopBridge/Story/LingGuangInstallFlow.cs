using HongmengOS.Aero2010;
using LingGuangV05.Runtime;
using Michsky.DreamOS;
using UnityEngine;
using AppNames = LingGuangV05.Core.AppNames;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// 灵光.exe is not on a new desktop: the player downloads it with the native browser (lingguang.cc → 下载).
    /// Until GameState says installed, its desktop icon, taskbar button and start-menu row stay hidden.
    /// When the native download item for <see cref="FileName"/> finishes, the game marks it installed and saves;
    /// the icon then appears and clicking the finished download opens the app.
    /// The DreamOS download record is reconciled to the game save, so a new game always starts undownloaded.
    /// Network: if nothing is connected, the home broadband (network 0) is connected for this session only, without
    /// writing DreamOS user data.
    /// </summary>
    public sealed class LingGuangInstallFlow : MonoBehaviour
    {
        public const string FileName = AppNames.ExeZh;
        public const string AppId = "lingguang";
        public ChapterOneRuntime runtime;
        public ChapterOneDesktopRouter router;

        private WebBrowserManager browser;
        private AeroStartMenu startMenu;
        private WindowManager hiddenWindow;
        private bool reconciled, wiredItem, networkChecked;
        private bool? shown;

        private void Start()
        {
            browser = FindAnyObjectByType<WebBrowserManager>(FindObjectsInactive.Include);
            startMenu = FindAnyObjectByType<AeroStartMenu>(FindObjectsInactive.Include);
        }

        private void LateUpdate()
        {
            if (runtime == null || runtime.Sim == null || router == null) return;
            var sim = runtime.Sim;
            bool installed = sim.AppInstalled || runtime.TestMode;
            if (!networkChecked) EnsureNetwork();
            if (!reconciled) Reconcile(installed);
            Gate(installed);
            if (browser == null) return;
            var item = FindItem();
            if (item == null) { wiredItem = false; return; }
            if (item.isFinished && !sim.AppInstalled)
            {
                sim.InstallApp();
                if (runtime.useDiskSave) runtime.SaveNow();
            }
            if (item.isFinished && !wiredItem && item.buttonObject != null)
            {
                item.buttonObject.onClick.AddListener(OpenApp);
                wiredItem = true;
            }
        }

        private void OpenApp() { if (runtime.Sim.AppInstalled) router.Open(AppId); }

        private WebBrowserDownloadItem FindItem()
        {
            foreach (var d in browser.activeDownloads) if (d != null && d.fileName == FileName) return d;
            foreach (var d in browser.GetComponentsInChildren<WebBrowserDownloadItem>(true)) if (d.fileName == FileName) return d;
            return null;
        }

        /// <summary>DreamOS keeps download state in its own user data; the game save is the authority.</summary>
        private void Reconcile(bool installed)
        {
            if (browser == null) { reconciled = true; return; }
            string downloadKey = FileName + "_DownloadState";
            if (!installed && DreamOSDataManager.ContainsJsonKey(DreamOSDataManager.DataCategory.Network, downloadKey)
                && DreamOSDataManager.ReadIntData(DreamOSDataManager.DataCategory.Network, downloadKey) != 0)
            {
                browser.DeleteDownloadedFile(FileName);
                for (int i = browser.activeDownloads.Count - 1; i >= 0; i--)
                    if (browser.activeDownloads[i] == null || browser.activeDownloads[i].fileName == FileName) browser.activeDownloads.RemoveAt(i);
            }
            reconciled = true;
        }

        private void EnsureNetwork()
        {
            networkChecked = true;
            var network = FindAnyObjectByType<NetworkManager>(FindObjectsInactive.Include);
            if (network == null || network.isConnected || network.networkItems.Count == 0) return;
            network.currentNetworkIndex = 0;
            network.isConnected = true;
            network.UpdateIndicators();
        }

        private void Gate(bool installed)
        {
            var bridge = router.Get(AppId);
            if (bridge == null) return;
            // Not downloaded yet: the app cannot be running either (a window restored from an older session stays shut).
            if (!installed && bridge.Window != null && bridge.Window.isOn) bridge.CloseApp();
            if (shown != installed)
            {
                if (bridge.DesktopShortcut != null)
                {
                    if (installed) PlaceInFreeCell(bridge.DesktopShortcut.transform as RectTransform);
                    bridge.DesktopShortcut.gameObject.SetActive(installed);
                }
                if (bridge.TaskbarShortcut != null) bridge.TaskbarShortcut.gameObject.SetActive(installed);
                SetStartMenuEntry(bridge.Window, installed);
                shown = installed;
            }
            // Start-menu search re-activates rows; keep the hidden row hidden.
            if (!installed && startMenu != null && startMenu.programs != null)
                foreach (var entry in startMenu.programs)
                    if (entry.window == null && entry.row != null && entry.row.activeSelf && hiddenWindow != null && IsOurRow(entry)) entry.row.SetActive(false);
        }

        /// <summary>
        /// The desktop keeps free-drag icon positions in DreamOS data. While 灵光 was hidden its slot went to the next
        /// icon, so on install it would land on top of that icon. If it overlaps, move it to the first empty grid cell
        /// (column by column) and store that position before the dragger reads it.
        /// </summary>
        private static void PlaceInFreeCell(RectTransform icon)
        {
            if (icon == null || icon.parent == null) return;
            var grid = icon.parent.GetComponent<UnityEngine.UI.GridLayoutGroup>();
            if (grid == null) return;
            var others = new System.Collections.Generic.List<Vector2>();
            foreach (Transform child in icon.parent)
                if (child != icon && child.gameObject.activeSelf) others.Add(((RectTransform)child).anchoredPosition);
            bool Taken(Vector2 p) { foreach (var o in others) if ((o - p).sqrMagnitude < 40 * 40) return true; return false; }
            if (!Taken(icon.anchoredPosition)) return;
            float startX = float.MaxValue, startY = float.MinValue;
            foreach (var o in others) { startX = Mathf.Min(startX, o.x); startY = Mathf.Max(startY, o.y); }
            float stepX = grid.cellSize.x + grid.spacing.x, stepY = grid.cellSize.y + grid.spacing.y;
            float height = ((RectTransform)icon.parent).rect.height;
            int rows = Mathf.Max(1, Mathf.FloorToInt((height - grid.padding.top) / stepY));
            for (int col = 0; col < 12; col++)
                for (int row = 0; row < rows; row++)
                {
                    var p = new Vector2(startX + col * stepX, startY - row * stepY);
                    if (Taken(p)) continue;
                    icon.anchoredPosition = p;
                    var dragger = icon.GetComponent<ItemDragger>();
                    if (dragger != null) dragger.UpdatePositionData();
                    return;
                }
        }

        private bool IsOurRow(AeroStartMenu.ProgramEntry entry) { return entry.searchKeywords != null && entry.searchKeywords.Contains("#lingguang-hidden"); }

        /// <summary>Hidden = window detached from the entry (search and Enter skip it) and row inactive.</summary>
        private void SetStartMenuEntry(WindowManager window, bool installed)
        {
            if (startMenu == null || startMenu.programs == null) return;
            for (int i = 0; i < startMenu.programs.Length; i++)
            {
                var entry = startMenu.programs[i];
                if (!installed && window != null && entry.window == window)
                {
                    hiddenWindow = window;
                    entry.window = null;
                    entry.searchKeywords = (entry.searchKeywords ?? "") + " #lingguang-hidden";
                    if (entry.row != null) entry.row.SetActive(false);
                }
                else if (installed && hiddenWindow != null && IsOurRow(entry))
                {
                    entry.window = hiddenWindow;
                    entry.searchKeywords = entry.searchKeywords.Replace(" #lingguang-hidden", "");
                    if (entry.row != null) entry.row.SetActive(true);
                }
                startMenu.programs[i] = entry;
            }
        }
    }
}
