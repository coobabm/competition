using System.Collections;
using System.Collections.Generic;
using Michsky.DreamOS;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>
    /// The desktop web browser (2026-10-19). Replaces DreamOS's WebBrowserManager: same GUID, serialized fields and
    /// public methods, so Main's browser window, the 灵光 download page, the casino sites and the install flow bind
    /// unchanged.
    ///
    /// Pages come from a <see cref="WebBrowserLibrary"/>: typing a URL (with or without "www.") shows that page's
    /// prefab after a load time of page size ÷ network speed; unknown URLs time out into the not-found page, and with
    /// the network down every URL shows the no-connection page. Each tab keeps its own back/forward history. Favorites
    /// and downloads are kept in DreamOS's Network data ("&lt;url&gt;_IsFavorite", "&lt;file&gt;_DownloadState": 0 none,
    /// 1 downloading, 2 done); a finished download opens in the music, notepad, photo or video app by its type.
    /// </summary>
    public class WebBrowserApp : MonoBehaviour
    {
        // Resources
        public NetworkManager networkManager;
        public WebBrowserLibrary webLibrary;
        [SerializeField] private GameObject tabPreset;
        [SerializeField] private Transform tabParent;
        [SerializeField] private Transform pageViewer;
        [SerializeField] private ButtonManager newTabButton;
        [SerializeField] private ButtonManager backButton;
        [SerializeField] private ButtonManager forwardButton;
        [SerializeField] private TMP_InputField urlField;
        [SerializeField] private ButtonManager favoriteButton;
        [SerializeField] private AnimatedIconHandler favoriteAnimator;
        [SerializeField] private GameObject favoritePreset;
        [SerializeField] private Transform favoritesParent;
        [SerializeField] private GameObject downloadPreset;
        [SerializeField] private Transform downloadsParent;
        [SerializeField] private PopupPanelManager downloadsPanel;
        [SerializeField] private MusicApp musicPlayerApp;
        [SerializeField] private VideoApp videoPlayerApp;
        [SerializeField] private NoteApp notepadApp;
        [SerializeField] private PhotoApp photoGalleryApp;

        // Settings
        [SerializeField] private bool rememberTabsOnLaunch = false;
        [SerializeField] private bool openDownloadsPanel = true;
        [SerializeField] private bool useLocalization = true;
        [SerializeField] [Range(1, 10)] private int maxTabLimit = 4;
        [SerializeField] [Range(1, 15)] private float timeoutDuration = 4;
        [SerializeField] [Range(0.1f, 100)] private float defaultNetworkSpeed = 50;

        // State
        public List<TabItem> currentTabs = new List<TabItem>();
        public List<WebBrowserFavoritesItem> favoritePages = new List<WebBrowserFavoritesItem>();
        public List<ActiveCoroutine> activeCoroutines = new List<ActiveCoroutine>();
        public List<WebBrowserDownloadItem> activeDownloads = new List<WebBrowserDownloadItem>();

        const DreamOSDataManager.DataCategory dataCat = DreamOSDataManager.DataCategory.Network;
        bool hasDynamicNetwork;
        bool isUrlFieldActive;
        int currentTabCount;
        string currentTabGuid;
        LocalizedObject localizedObject;

        [System.Serializable]
        public class TabItem
        {
            public string guid;
            public int pageIndex;
            public string pageUrl;
            public GameObject tabPage;
            public WebBrowserTabItem item;
            public List<WebBrowserLibrary.WebPage> tabHistory = new List<WebBrowserLibrary.WebPage>();
        }

        /// <summary>A page load in flight, keyed by the tab that asked for it.</summary>
        [System.Serializable]
        public class ActiveCoroutine
        {
            public string targetGuid;
            public Coroutine coroutine;
        }

        TabItem CurrentTab => currentTabs[GetTabIndex(currentTabGuid)];

        void Awake()
        {
            if (networkManager == null) networkManager = FindObjectsByType<NetworkManager>(FindObjectsSortMode.None)[0];
            if (backButton != null) backButton.onClick.AddListener(GoBack);
            if (forwardButton != null) forwardButton.onClick.AddListener(GoForward);
            if (newTabButton != null) newTabButton.onClick.AddListener(() => CreateNewTab());
            if (favoriteButton != null) favoriteButton.onClick.AddListener(SetFavoriteState);
            if (localizedObject == null) localizedObject = GetComponent<LocalizedObject>();
            // The prefab's sample tabs go; only the "+" button stays.
            foreach (Transform child in tabParent) if (child != newTabButton.transform) Destroy(child.gameObject);

            Initialize();
        }

        void Start()
        {
            ListFavorites();
            ListDownloads();
        }

        void OnEnable() => urlField.text = CurrentTab.pageUrl;

        void OnDisable()
        {
            if (!rememberTabsOnLaunch) CloseAllTabs();
        }

        void Update()
        {
            if (isUrlFieldActive && Keyboard.current.enterKey.wasPressedThisFrame)
            {
                // Toggling interactable drops the caret so Enter doesn't keep editing.
                urlField.interactable = false;
                urlField.interactable = true;
                ActivateURLField(false);
                OpenPage(urlField.text);
            }

            for (int i = 0; i < activeDownloads.Count; i++)
                if (activeDownloads[i] != null) activeDownloads[i].ProcessItem();
        }

        public void Initialize()
        {
            hasDynamicNetwork = networkManager != null;
            backButton.Interactable(false);
            forwardButton.Interactable(false);
            CreateNewTab();
        }

        // ---------- Tabs ----------

        public void CreateNewTab(string customUrl = null)
        {
            if (currentTabCount == maxTabLimit) return;

            currentTabCount++;
            currentTabGuid = DreamOSInternalTools.GenerateUniqueGuid();
            string guid = currentTabGuid;

            var tab = AppSpawn.Row(tabPreset, tabParent, guid).GetComponent<WebBrowserTabItem>();
            tab.manager = this;
            tab.guid = guid;
            tab.mainButton.onClick.AddListener(() => SwitchToTab(guid));
            tab.closeButton.onClick.AddListener(() => CloseTab(guid));
            currentTabs.Add(new TabItem { guid = guid, item = tab });

            if (string.IsNullOrEmpty(customUrl)) OpenHomePage();
            else OpenPage(customUrl);

            if (newTabButton != null)
            {
                if (currentTabCount == maxTabLimit) newTabButton.Interactable(false);
                newTabButton.transform.SetAsLastSibling();
            }
            SwitchToTab(guid);
        }

        public void CloseTab(string guid)
        {
            int tabIndex = GetTabIndex(guid);

            // Loads started from the tab in front stop (DreamOS keys this on the current tab, not the closed one).
            for (int i = 0; i < activeCoroutines.Count; i++)
            {
                if (activeCoroutines[i].targetGuid != currentTabGuid) continue;
                StopCoroutine(activeCoroutines[i].coroutine);
                activeCoroutines.RemoveAt(i);
            }

            Destroy(currentTabs[tabIndex].tabPage);
            Destroy(currentTabs[tabIndex].item.gameObject);
            currentTabs.RemoveAt(tabIndex);
            currentTabCount--;

            if (currentTabCount == 0) CreateNewTab();
            else SwitchToTab(currentTabs[currentTabs.Count - 1].guid);

            if (newTabButton != null && currentTabCount < maxTabLimit) newTabButton.Interactable(true);
            UpdateButtonStates();
        }

        public void SwitchToTab(string guid)
        {
            int tabIndex = GetTabIndex(guid);
            currentTabGuid = guid;

            for (int i = 0; i < currentTabs.Count; i++)
            {
                if (currentTabs[i].tabPage == null) continue;
                bool front = currentTabs[i].guid == guid;
                currentTabs[i].item.SetIndicator(front);
                currentTabs[i].tabPage.SetActive(front);
                if (front) tabIndex = i;
            }

            GetFavoriteState(currentTabs[tabIndex].pageUrl);
            UpdateButtonStates();
            urlField.text = currentTabs[tabIndex].pageUrl;
        }

        /// <summary>
        /// Closes tabs front to back over a shrinking list, as DreamOS did: the browser comes back with one tab (a new
        /// home tab when only one was open, otherwise the last one).
        /// </summary>
        public void CloseAllTabs()
        {
            for (int i = 0; i < currentTabs.Count; i++) CloseTab(currentTabs[i].guid);
        }

        // ---------- Pages ----------

        public void OpenHomePage()
        {
            var tab = CurrentTab;
            var home = webLibrary.homePage;
            tab.item.SetData(home.pageIcon, PageTitle(home));

            if (tab.tabPage != null) Destroy(tab.tabPage);
            tab.tabPage = SpawnPage(home);
            tab.pageUrl = home.pageURL;

            // pageUrl was just set to home, so this only adds the first history entry.
            if (tab.tabHistory.Count == 0 || tab.pageUrl != home.pageURL) AddHistory(tab, CopyOf(home));

            urlField.text = home.pageURL;
        }

        public void OpenNotFoundPage(string tabGuid = null, bool isEnabled = true) => OpenErrorPage(webLibrary.notFoundPage, tabGuid, isEnabled);

        public void OpenNoConnectionPage(string tabGuid = null, bool isEnabled = true) => OpenErrorPage(webLibrary.noConnectionPage, tabGuid, isEnabled);

        void OpenErrorPage(WebBrowserLibrary.WebPage page, string tabGuid, bool isEnabled)
        {
            if (string.IsNullOrEmpty(tabGuid)) tabGuid = currentTabGuid;
            var tab = currentTabs[GetTabIndex(tabGuid)];
            tab.item.SetData(page.pageIcon, PageTitle(page));
            tab.tabPage = SpawnPage(page);
            if (!isEnabled) tab.tabPage.SetActive(false);
        }

        public void OpenPage(string targetUrl, bool addToHistory = true)
        {
            // A new load replaces the front tab's pending one.
            for (int i = 0; i < activeCoroutines.Count; i++)
            {
                if (activeCoroutines[i].targetGuid != currentTabGuid) continue;
                StopCoroutine(activeCoroutines[i].coroutine);
                activeCoroutines.RemoveAt(i);
                break;
            }

            activeCoroutines.Add(new ActiveCoroutine
            {
                coroutine = StartCoroutine(LoadPage(targetUrl, addToHistory)),
                targetGuid = currentTabGuid
            });
        }

        IEnumerator LoadPage(string targetUrl, bool addToHistory)
        {
            int tabIndex = GetTabIndex(currentTabGuid);
            var tab = currentTabs[tabIndex];
            urlField.text = targetUrl;
            string typed = urlField.text;

            int urlIndex = webLibrary.webPages.FindIndex(p => typed.ToLower() == p.pageURL || typed.ToLower() == "www." + p.pageURL);
            bool offline = hasDynamicNetwork && !networkManager.isConnected;

            float loadDuration = 0;
            if (offline || typed == webLibrary.homePage.pageURL) loadDuration = 0;
            else if (urlIndex == -1) loadDuration = timeoutDuration;
            else loadDuration = webLibrary.webPages[urlIndex].pageSize / (hasDynamicNetwork
                ? networkManager.networkItems[networkManager.currentNetworkIndex].networkSpeed
                : defaultNetworkSpeed);

            tab.item.EnableSpinner();

            if (addToHistory)
            {
                if (urlIndex != -1) AddHistory(tab, CopyOf(webLibrary.webPages[urlIndex]));
                else AddHistory(tab, new WebBrowserLibrary.WebPage
                {
                    pageURL = typed,
                    pageTitle = webLibrary.notFoundPage.pageTitle,
                    pageIcon = webLibrary.notFoundPage.pageIcon
                });
            }

            GameObject previousPage = tab.tabPage;
            GameObject newPage = null;
            bool createHome = false, createNoConnection = false, createNotFound = false;

            if (targetUrl == webLibrary.homePage.pageURL) { OpenHomePage(); createHome = true; tab.pageUrl = webLibrary.homePage.pageURL; }
            else if (offline) { createNoConnection = true; tab.pageUrl = typed; }
            else if (urlIndex == -1) { createNotFound = true; tab.pageUrl = typed; }
            else
            {
                // Built hidden now, shown when the load time is up.
                newPage = SpawnPage(webLibrary.webPages[urlIndex]);
                newPage.SetActive(false);
                tab.pageUrl = webLibrary.webPages[urlIndex].pageURL;
            }

            GetFavoriteState(tab.pageUrl);
            UpdateButtonStates();

            yield return new WaitForSeconds(loadDuration);

            if (urlIndex != -1) tab.item.SetData(webLibrary.webPages[urlIndex].pageIcon, PageTitle(webLibrary.webPages[urlIndex]));
            tab.item.DisableSpinner();

            if (previousPage != null) Destroy(previousPage);
            if (!createHome) tab.tabPage = newPage;

            bool front = currentTabGuid == tab.guid;
            if (front)
            {
                if (tab.tabPage != null) tab.tabPage.SetActive(true);
                urlField.text = urlIndex != -1 ? tab.pageUrl : tab.tabHistory[tab.pageIndex].pageURL;
            }
            // Error pages are made now; a tab that is no longer in front gets them hidden.
            if (createNoConnection) OpenNoConnectionPage(tab.guid, front);
            else if (createNotFound) OpenNotFoundPage(tab.guid, front);

            int done = activeCoroutines.FindIndex(c => c.targetGuid == tab.guid);
            if (done >= 0) activeCoroutines.RemoveAt(done);
        }

        public void GoBack()
        {
            var tab = CurrentTab;
            if (tab.tabHistory.Count == 0 || tab.pageIndex == 0) return;
            tab.pageIndex--;
            OpenPage(tab.tabHistory[tab.pageIndex].pageURL, false);
        }

        public void GoForward()
        {
            var tab = CurrentTab;
            if (tab.tabHistory.Count == 0 || tab.pageIndex >= tab.tabHistory.Count - 1) return;
            tab.pageIndex++;
            OpenPage(tab.tabHistory[tab.pageIndex].pageURL, false);
        }

        public void Refresh() => OpenPage(CurrentTab.pageUrl, false);

        public void UpdateButtonStates()
        {
            if (backButton == null || forwardButton == null) return;

            var tab = CurrentTab;
            bool hasHistory = tab.tabHistory.Count > 1;
            backButton.Interactable(hasHistory && tab.pageIndex != 0);
            forwardButton.Interactable(hasHistory && tab.pageIndex != tab.tabHistory.Count - 1);
        }

        public void ActivateURLField(bool value) => isUrlFieldActive = value;

        // ---------- Downloads ----------

        public void DownloadFile(string fileName)
        {
            int index = webLibrary.dlFiles.FindIndex(f => f.fileName == fileName);
            if (index >= 0)
            {
                string key = fileName + "_DownloadState";
                if (DreamOSDataManager.ContainsJsonKey(dataCat, key) && DreamOSDataManager.ReadIntData(dataCat, key) != 0)
                {
                    // Already downloading or downloaded: just show the list.
                    if (openDownloadsPanel && downloadsPanel != null) downloadsPanel.OpenPanel();
                    return;
                }

                DreamOSDataManager.WriteIntData(dataCat, key, 1);
                var row = SpawnDownload(index);
                row.ProcessDownload();
                activeDownloads.Add(row);
            }

            if (openDownloadsPanel && downloadsPanel != null) downloadsPanel.OpenPanel();
        }

        public void DeleteDownloadedFile(string fileName)
        {
            if (!webLibrary.dlFiles.Exists(f => f.fileName == fileName)) return;
            DreamOSDataManager.WriteIntData(dataCat, fileName + "_DownloadState", 0);
            foreach (Transform row in downloadsParent) if (row.gameObject.name == fileName) Destroy(row.gameObject);
        }

        /// <summary>Rebuilds the downloads list from saved states: unfinished ones carry on, finished ones are ready.</summary>
        public void ListDownloads()
        {
            foreach (Transform child in downloadsParent) Destroy(child.gameObject);
            for (int i = 0; i < webLibrary.dlFiles.Count; i++)
            {
                string key = webLibrary.dlFiles[i].fileName + "_DownloadState";
                if (!DreamOSDataManager.ContainsJsonKey(dataCat, key) || DreamOSDataManager.ReadIntData(dataCat, key) == 0) continue;

                var row = SpawnDownload(i);
                int state = DreamOSDataManager.ReadIntData(dataCat, key);
                if (state == 1) { activeDownloads.Add(row); row.ProcessDownload(); }
                else if (state == 2) row.ProcessComplete();
            }
        }

        WebBrowserDownloadItem SpawnDownload(int index)
        {
            var file = webLibrary.dlFiles[index];
            var row = AppSpawn.Row(downloadPreset, downloadsParent, file.fileName).GetComponent<WebBrowserDownloadItem>();
            row.RegisterIdentityRow(); // Inactive presets/parents do not invoke Awake yet.
            row.manager = this;
            row.fileIcon = file.fileIcon;
            row.fileName = file.fileName;
            row.fileSize = file.fileSize;

            // Clicking a finished download opens it in its app.
            switch (file.fileType)
            {
                case WebBrowserLibrary.FileType.Music when musicPlayerApp != null:
                    row.buttonObject.onClick.AddListener(() =>
                    {
                        musicPlayerApp.GetComponent<WindowManager>().OpenWindow();
                        musicPlayerApp.PlayCustomClip(file.musicReference, row.fileIcon, row.fileName, "Downloads");
                    });
                    break;
                case WebBrowserLibrary.FileType.Note when notepadApp != null:
                    row.buttonObject.onClick.AddListener(() =>
                    {
                        notepadApp.GetComponent<WindowManager>().OpenWindow();
                        notepadApp.OpenCustomNote(row.fileName, file.noteReference);
                    });
                    break;
                case WebBrowserLibrary.FileType.Photo when photoGalleryApp != null:
                    row.buttonObject.onClick.AddListener(() =>
                    {
                        photoGalleryApp.GetComponent<WindowManager>().OpenWindow();
                        photoGalleryApp.OpenPhoto(file.photoReference, row.fileName, "Downloads");
                    });
                    break;
                case WebBrowserLibrary.FileType.Video when videoPlayerApp != null:
                    row.buttonObject.onClick.AddListener(() =>
                    {
                        videoPlayerApp.GetComponent<WindowManager>().OpenWindow();
                        videoPlayerApp.OpenVideo(file.videoReference, row.fileName);
                    });
                    break;
            }
            return row;
        }

        // ---------- Favorites ----------

        public void GetFavoriteState(string url)
        {
            bool favorite = DreamOSDataManager.ContainsJsonKey(dataCat, url + "_IsFavorite")
                && DreamOSDataManager.ReadBooleanData(dataCat, url + "_IsFavorite");
            if (favorite) favoriteAnimator.PlayIn();
            else favoriteAnimator.PlayOut();
        }

        /// <summary>The star button: toggles the front tab's page (home and error pages can't be starred).</summary>
        public void SetFavoriteState()
        {
            string url = CurrentTab.pageUrl;
            if (IsBuiltInPage(url)) return;

            bool favorite = DreamOSDataManager.ContainsJsonKey(dataCat, url + "_IsFavorite")
                && DreamOSDataManager.ReadBooleanData(dataCat, url + "_IsFavorite");
            SetFavoriteState(!favorite, url);
        }

        public void SetFavoriteState(bool value, string url, bool writeData = true)
        {
            var page = GetWebPage(url);
            if (page == null) return;

            if (value)
            {
                var row = AppSpawn.Row(favoritePreset, favoritesParent, url).GetComponent<WebBrowserFavoritesItem>();
                row.manager = this;
                row.url = url;
                row.iconObject.sprite = page.pageIcon;
                row.titleObject.text = page.pageTitle;
                row.urlObject.text = page.pageURL;
                row.button.onClick.AddListener(() => OpenPage(row.url));
                favoritePages.Add(row);
                favoriteAnimator.PlayIn();
            }
            else
            {
                int i = favoritePages.FindIndex(f => f.url == url);
                if (i >= 0)
                {
                    Destroy(favoritePages[i].gameObject);
                    favoritePages.RemoveAt(i);
                }
                favoriteAnimator.PlayOut();
            }

            if (writeData) DreamOSDataManager.WriteBooleanData(dataCat, url + "_IsFavorite", value);
        }

        public void ListFavorites()
        {
            foreach (Transform child in favoritesParent) Destroy(child.gameObject);
            foreach (var page in webLibrary.webPages)
            {
                string key = page.pageURL + "_IsFavorite";
                if (DreamOSDataManager.ContainsJsonKey(dataCat, key))
                    SetFavoriteState(DreamOSDataManager.ReadBooleanData(dataCat, key), page.pageURL, false);
            }
        }

        // ---------- Helpers ----------

        bool IsBuiltInPage(string url) =>
            url == webLibrary.homePage.pageURL || url == webLibrary.noConnectionPage.pageURL || url == webLibrary.notFoundPage.pageURL;

        string PageTitle(WebBrowserLibrary.WebPage page)
        {
            if (useLocalization && !string.IsNullOrEmpty(page.titleKey) && localizedObject != null && localizedObject.CheckLocalizationStatus())
                return localizedObject.GetKeyOutput(page.titleKey);
            return page.pageTitle;
        }

        GameObject SpawnPage(WebBrowserLibrary.WebPage page) => AppSpawn.Row(page.pageContent, pageViewer, page.pageURL);

        static WebBrowserLibrary.WebPage CopyOf(WebBrowserLibrary.WebPage page) => new WebBrowserLibrary.WebPage
        {
            pageURL = page.pageURL,
            pageTitle = page.pageTitle,
            pageIcon = page.pageIcon,
            pageSize = page.pageSize,
            pageContent = page.pageContent
        };

        static void AddHistory(TabItem tab, WebBrowserLibrary.WebPage entry)
        {
            tab.tabHistory.Add(entry);
            tab.pageIndex = tab.tabHistory.Count - 1;
        }

        int GetTabIndex(string guid) => currentTabs.FindIndex(t => t.guid == guid);

        WebBrowserLibrary.WebPage GetWebPage(string url) => webLibrary.webPages.Find(p => p.pageURL == url);
    }
}
