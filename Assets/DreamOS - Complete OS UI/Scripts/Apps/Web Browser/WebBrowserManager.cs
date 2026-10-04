using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace Michsky.DreamOS
{
    public class WebBrowserManager : MonoBehaviour
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
        [SerializeField] private MusicPlayerManager musicPlayerApp;
        [SerializeField] private VideoPlayerManager videoPlayerApp;
        [SerializeField] private NotepadManager notepadApp;
        [SerializeField] private PhotoGalleryManager photoGalleryApp;

        // Settings
        [SerializeField] private bool rememberTabsOnLaunch = false;
        [SerializeField] private bool openDownloadsPanel = true;
        [SerializeField] private bool useLocalization = true;
        [SerializeField] [Range(1, 10)] private int maxTabLimit = 4;
        [SerializeField] [Range(1, 15)] private float timeoutDuration = 4;
        [SerializeField] [Range(0.1f, 100)] private float defaultNetworkSpeed = 50;

        // Helpers
        bool hasDynamicNetwork;
        bool isUrlFieldActive;
        int currentTabCount;
        string currentTabGuid;
        LocalizedObject localizedObject;
        public List<TabItem> currentTabs = new List<TabItem>();
        public List<WebBrowserFavoritesItem> favoritePages = new List<WebBrowserFavoritesItem>();
        public List<ActiveCoroutine> activeCoroutines = new List<ActiveCoroutine>();
        public List<WebBrowserDownloadItem> activeDownloads = new List<WebBrowserDownloadItem>();
        DreamOSDataManager.DataCategory dataCat = DreamOSDataManager.DataCategory.Network;

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

        [System.Serializable]
        public class ActiveCoroutine
        {
            public string targetGuid;
            public Coroutine coroutine;
        }

        void Awake()
        {
            // Check for components
#if UNITY_2023_2_OR_NEWER
            if (networkManager == null) { networkManager = FindObjectsByType<NetworkManager>(FindObjectsSortMode.None)[0]; }
#else
            if (networkManager == null) { networkManager = (NetworkManager)FindObjectsOfType(typeof(NetworkManager))[0]; }  
#endif
            if (backButton != null) { backButton.onClick.AddListener(delegate { GoBack(); }); }
            if (forwardButton != null) { forwardButton.onClick.AddListener(delegate { GoForward(); }); }
            if (newTabButton != null) { newTabButton.onClick.AddListener(delegate { CreateNewTab(); }); }
            if (favoriteButton != null) { favoriteButton.onClick.AddListener(delegate { SetFavoriteState(); }); }
            if (localizedObject == null) { localizedObject = gameObject.GetComponent<LocalizedObject>(); }
            foreach (Transform child in tabParent) { if (child != newTabButton.transform) { Destroy(child.gameObject); } }

            Initialize();
        }

        void Start()
        {
            ListFavorites();
            ListDownloads();
        }

        void OnEnable()
        {
            int tabIndex = GetTabIndex(currentTabGuid);
            urlField.text = currentTabs[tabIndex].pageUrl;
        }

        void OnDisable()
        {
            if (!rememberTabsOnLaunch) { CloseAllTabs(); }
        }

        void Update()
        {
            if (isUrlFieldActive && Keyboard.current.enterKey.wasPressedThisFrame)
            {
                urlField.interactable = false;
                urlField.interactable = true;
              
                ActivateURLField(false);
                OpenPage(urlField.text);
            }

            if (activeDownloads.Count > 0)
            {
                for (int i = 0; i < activeDownloads.Count; i++)
                {
                    if (activeDownloads[i] == null)
                        continue;

                    activeDownloads[i].ProcessItem();
                }
            }
        }

        public void Initialize()
        {
            if (networkManager == null) { hasDynamicNetwork = false; }
            else { hasDynamicNetwork = true; }

            backButton.Interactable(false);
            forwardButton.Interactable(false);

            CreateNewTab();
        }

        public void CreateNewTab(string customUrl = null)
        {
            if (currentTabCount == maxTabLimit)
                return;

            // Set the parameters
            currentTabCount++;
            currentTabGuid = DreamOSInternalTools.GenerateUniqueGuid();
            string tempTabGuid = currentTabGuid;

            // Create the tab item
            GameObject tempItem = Instantiate(tabPreset, new Vector3(0, 0, 0), Quaternion.identity);
            tempItem.transform.SetParent(tabParent, false);
            tempItem.gameObject.name = tempTabGuid;

            // Get the tab item component
            WebBrowserTabItem tempComp = tempItem.GetComponent<WebBrowserTabItem>();
            tempComp.manager = this;
            tempComp.guid = tempTabGuid;
            tempComp.mainButton.onClick.AddListener(delegate { SwitchToTab(tempTabGuid); });
            tempComp.closeButton.onClick.AddListener(delegate { CloseTab(tempTabGuid); });

            // Add tab item
            TabItem newItem = new TabItem();
            newItem.guid = currentTabGuid;
            newItem.item = tempComp;
            currentTabs.Add(newItem);

            if (string.IsNullOrEmpty(customUrl)) { OpenHomePage(); }
            else { OpenPage(customUrl); }

            // Check for tab limit and disable the button
            if (newTabButton != null && currentTabCount == maxTabLimit) { newTabButton.Interactable(false); }
        
            // Set the tab index
            if (newTabButton != null) { newTabButton.transform.SetAsLastSibling(); }

            // Switch to tab
            SwitchToTab(tempTabGuid);
        }

        public void CloseTab(string guid)
        {
            int tabIndex = GetTabIndex(guid);

            // Stop all necesarry coroutines
            for (int i = 0; i < activeCoroutines.Count; i++)
            {
                if (activeCoroutines[i].targetGuid == currentTabGuid)
                {
                    StopCoroutine(activeCoroutines[i].coroutine);
                    activeCoroutines.RemoveAt(i);
                }
            }

            Destroy(currentTabs[tabIndex].tabPage);
            Destroy(currentTabs[tabIndex].item.gameObject);

            currentTabs.RemoveAt(tabIndex);
            currentTabCount--;

            // Check for tab count and create a new one if nothing exists
            if (currentTabCount == 0) { CreateNewTab(); }
            else { SwitchToTab(currentTabs[currentTabs.Count - 1].guid); }

            // Check for tab limit and disable the button
            if (newTabButton != null && currentTabCount < maxTabLimit) { newTabButton.Interactable(true); }

            // Update button states
            UpdateButtonStates();
        }

        public void SwitchToTab(string guid)
        {
            int tabIndex = GetTabIndex(guid);

            // Set tab index
            currentTabGuid = guid;

            // Disable other tabs
            for (int i = 0; i < currentTabs.Count; i++)
            {
                if (currentTabs[i].tabPage == null) { continue; }
                else if (currentTabGuid == currentTabs[i].guid) { currentTabs[i].item.SetIndicator(true); currentTabs[i].tabPage.SetActive(true); tabIndex = i; }
                else { currentTabs[i].item.SetIndicator(false); currentTabs[i].tabPage.SetActive(false); }
            }

            // Get favorite state
            GetFavoriteState(currentTabs[tabIndex].pageUrl);

            // Update button states
            UpdateButtonStates();

            // Update URL Field
            urlField.text = currentTabs[tabIndex].pageUrl;
        }

        public void CloseAllTabs()
        {
            for (int i = 0; i < currentTabs.Count; i++)
            { 
                CloseTab(currentTabs[i].guid);
            }
        }

        public void OpenHomePage()
        {
            int tabIndex = GetTabIndex(currentTabGuid);

            // Update tab UI
            string tempTitle = webLibrary.homePage.pageTitle;

            // Check for localization
            if (useLocalization && !string.IsNullOrEmpty(webLibrary.homePage.titleKey) && localizedObject != null && localizedObject.CheckLocalizationStatus())
            {
                tempTitle = localizedObject.GetKeyOutput(webLibrary.homePage.titleKey);
            }

            currentTabs[tabIndex].item.SetData(webLibrary.homePage.pageIcon, tempTitle);

            // Destroy the previous page
            if (currentTabs[tabIndex].tabPage != null) { Destroy(currentTabs[tabIndex].tabPage); }

            // Create the tab content
            GameObject tObject = Instantiate(webLibrary.homePage.pageContent, new Vector3(0, 0, 0), Quaternion.identity);
            tObject.name = webLibrary.homePage.pageURL;
            tObject.transform.SetParent(pageViewer, false);

            // Set the current tab page
            currentTabs[tabIndex].tabPage = tObject;
            currentTabs[tabIndex].pageUrl = webLibrary.homePage.pageURL;

            // Add to tab history
            if (currentTabs[tabIndex].tabHistory.Count == 0 || currentTabs[tabIndex].pageUrl != webLibrary.homePage.pageURL)
            {
                WebBrowserLibrary.WebPage tempItem = new WebBrowserLibrary.WebPage();
                tempItem.pageURL = webLibrary.homePage.pageURL;
                tempItem.pageTitle = webLibrary.homePage.pageTitle;
                tempItem.pageIcon = webLibrary.homePage.pageIcon;
                tempItem.pageSize = webLibrary.homePage.pageSize;
                tempItem.pageContent = webLibrary.homePage.pageContent;

                // currentTabs[tabIndex].tabHistory.RemoveRange(currentTabs[tabIndex].pageIndex, currentTabs[tabIndex].tabHistory.Count);
                currentTabs[tabIndex].tabHistory.Add(tempItem);
                currentTabs[tabIndex].pageIndex = currentTabs[tabIndex].tabHistory.Count - 1;
            }

            // Update URL Field
            urlField.text = webLibrary.homePage.pageURL;
        }

        public void OpenNotFoundPage(string tabGuid = null, bool isEnabled = true)
        {
            if (string.IsNullOrEmpty(tabGuid)) { tabGuid = currentTabGuid; }
            int tabIndex = GetTabIndex(tabGuid);

            // Update tab UI
            string tempTitle = webLibrary.notFoundPage.pageTitle;

            // Check for localization
            if (useLocalization && !string.IsNullOrEmpty(webLibrary.notFoundPage.titleKey) && localizedObject != null && localizedObject.CheckLocalizationStatus())
            {
                tempTitle = localizedObject.GetKeyOutput(webLibrary.notFoundPage.titleKey);
            }

            currentTabs[tabIndex].item.SetData(webLibrary.notFoundPage.pageIcon, tempTitle);

            // Create the tab content
            GameObject tObject = Instantiate(webLibrary.notFoundPage.pageContent, new Vector3(0, 0, 0), Quaternion.identity);
            tObject.name = webLibrary.notFoundPage.pageURL;
            tObject.transform.SetParent(pageViewer, false);
            if (!isEnabled) { tObject.SetActive(false); }

            // Set the current tab page
            currentTabs[tabIndex].tabPage = tObject;
        }

        public void OpenNoConnectionPage(string tabGuid = null, bool isEnabled = true)
        {
            if (string.IsNullOrEmpty(tabGuid)) { tabGuid = currentTabGuid; }
            int tabIndex = GetTabIndex(tabGuid);

            // Update tab UI
            string tempTitle = webLibrary.noConnectionPage.pageTitle;

            // Check for localization
            if (useLocalization && !string.IsNullOrEmpty(webLibrary.noConnectionPage.titleKey) && localizedObject != null && localizedObject.CheckLocalizationStatus())
            {
                tempTitle = localizedObject.GetKeyOutput(webLibrary.noConnectionPage.titleKey);
            }

            currentTabs[tabIndex].item.SetData(webLibrary.noConnectionPage.pageIcon, tempTitle);

            // Create the tab content
            GameObject tObject = Instantiate(webLibrary.noConnectionPage.pageContent, new Vector3(0, 0, 0), Quaternion.identity);
            tObject.name = webLibrary.noConnectionPage.pageURL;
            tObject.transform.SetParent(pageViewer, false);
            if (!isEnabled) { tObject.SetActive(false); }

            // Set the current tab page
            currentTabs[tabIndex].tabPage = tObject;
        }

        public void OpenPage(string targetUrl, bool addToHistory = true)
        {
            for (int i = 0; i < activeCoroutines.Count; i++)
            {
                if (activeCoroutines[i].targetGuid == currentTabGuid)
                {
                    StopCoroutine(activeCoroutines[i].coroutine);
                    activeCoroutines.RemoveAt(i);
                    break;
                }
            }

            Coroutine tempCoroutine = StartCoroutine(OpenPageHelper(targetUrl, addToHistory));
            ActiveCoroutine acor = new ActiveCoroutine();
            acor.coroutine = tempCoroutine;
            acor.targetGuid = currentTabGuid;
            activeCoroutines.Add(acor);
        }

        IEnumerator OpenPageHelper(string targetUrl, bool addToHistory)
        {
            int urlIndex = -1;
            int tabIndex = GetTabIndex(currentTabGuid);
           
            float loadDuration = 0;
           
            bool createNoConnection = false;
            bool createNotFound = false;
            bool createHome = false;
            
            GameObject previousPage = null;
            GameObject newPage = null;

            // Update URL Field
            urlField.text = targetUrl;

            // Search the library
            for (int i = 0; i < webLibrary.webPages.Count; i++)
            {
                if (urlField.text.ToLower() == webLibrary.webPages[i].pageURL || urlField.text.ToLower() == "www." + webLibrary.webPages[i].pageURL)
                {
                    urlIndex = i;
                    break;
                }
            }

            if ((hasDynamicNetwork && !networkManager.isConnected) || urlField.text == webLibrary.homePage.pageURL) { loadDuration = 0; }
            else if (urlIndex == -1) { loadDuration = timeoutDuration; }
            else if (hasDynamicNetwork && urlIndex != -1)
            {
                float pageSize = webLibrary.webPages[urlIndex].pageSize;
                loadDuration = (pageSize / networkManager.networkItems[networkManager.currentNetworkIndex].networkSpeed);
            }
            else if (!hasDynamicNetwork && urlIndex != -1)
            {
                float pageSize = webLibrary.webPages[urlIndex].pageSize;
                loadDuration = (pageSize / defaultNetworkSpeed);
            }

            // Update tab spinner
            currentTabs[tabIndex].item.EnableSpinner();

            // Add to tab history
            if (addToHistory && urlIndex != -1)
            {
                WebBrowserLibrary.WebPage tempItem = new WebBrowserLibrary.WebPage();
                tempItem.pageURL = webLibrary.webPages[urlIndex].pageURL;
                tempItem.pageTitle = webLibrary.webPages[urlIndex].pageTitle;
                tempItem.pageIcon = webLibrary.webPages[urlIndex].pageIcon;
                tempItem.pageSize = webLibrary.webPages[urlIndex].pageSize;
                tempItem.pageContent = webLibrary.webPages[urlIndex].pageContent;

                // currentTabs[tabIndex].tabHistory.RemoveRange(currentTabs[tabIndex].pageIndex + 1, currentTabs[tabIndex].tabHistory.Count);
                currentTabs[tabIndex].tabHistory.Add(tempItem);
                currentTabs[tabIndex].pageIndex = currentTabs[tabIndex].tabHistory.Count - 1;
            }

            else if (addToHistory && urlIndex == -1)
            {
                WebBrowserLibrary.WebPage tempItem = new WebBrowserLibrary.WebPage();
                tempItem.pageURL = urlField.text;
                tempItem.pageTitle = webLibrary.notFoundPage.pageTitle;
                tempItem.pageIcon = webLibrary.notFoundPage.pageIcon;

                // currentTabs[tabIndex].tabHistory.RemoveRange(currentTabs[tabIndex].pageIndex + 1, currentTabs[tabIndex].tabHistory.Count);
                currentTabs[tabIndex].tabHistory.Add(tempItem);
                currentTabs[tabIndex].pageIndex = currentTabs[tabIndex].tabHistory.Count - 1;
            }

            // Set the previous page
            if (currentTabs[tabIndex].tabPage != null) { previousPage = currentTabs[tabIndex].tabPage; }

            // Check for conditions and create the page
            if (targetUrl == webLibrary.homePage.pageURL) { OpenHomePage(); createHome = true; currentTabs[tabIndex].pageUrl = webLibrary.homePage.pageURL; }
            else if (hasDynamicNetwork && !networkManager.isConnected) { createNoConnection = true; currentTabs[tabIndex].pageUrl = urlField.text; }
            else if ((hasDynamicNetwork && networkManager.isConnected && urlIndex == -1) || (!hasDynamicNetwork && urlIndex == -1)) { createNotFound = true; currentTabs[tabIndex].pageUrl = urlField.text; }
            else
            {
                // Create the tab content
                GameObject tObject = Instantiate(webLibrary.webPages[urlIndex].pageContent, new Vector3(0, 0, 0), Quaternion.identity);
                tObject.name = webLibrary.webPages[urlIndex].pageURL;
                tObject.transform.SetParent(pageViewer, false);
                tObject.gameObject.SetActive(false);

                // Set the current tab page
                newPage = tObject;
                currentTabs[tabIndex].pageUrl = webLibrary.webPages[urlIndex].pageURL;
            }

            // Get favorite state
            GetFavoriteState(currentTabs[tabIndex].pageUrl);

            // Update button states
            UpdateButtonStates();

            // Wait for load duration
            yield return new WaitForSeconds(loadDuration);

            // Update tab UI
            if (urlIndex != -1) 
            {
                string tempTitle = webLibrary.webPages[urlIndex].pageTitle;

                // Check for localization
                if (useLocalization && !string.IsNullOrEmpty(webLibrary.webPages[urlIndex].titleKey) && localizedObject != null && localizedObject.CheckLocalizationStatus())
                {
                    tempTitle = localizedObject.GetKeyOutput(webLibrary.webPages[urlIndex].titleKey);
                }

                currentTabs[tabIndex].item.SetData(webLibrary.webPages[urlIndex].pageIcon, tempTitle);
            }

            // Update tab spinner
            currentTabs[tabIndex].item.DisableSpinner();

            // Destroy the previous page
            if (previousPage != null) { Destroy(previousPage); }

            // Set the new page
            if (!createHome) { currentTabs[tabIndex].tabPage = newPage; }

            // If the user still on the tab
            if (currentTabGuid == currentTabs[tabIndex].guid) 
            {
                // Enable the current page
                if (currentTabs[tabIndex].tabPage != null) { currentTabs[tabIndex].tabPage.SetActive(true); }

                // Update URL Field
                if (urlIndex != -1) { urlField.text = currentTabs[tabIndex].pageUrl; }
                else if (urlIndex == -1) { urlField.text = currentTabs[tabIndex].tabHistory[currentTabs[tabIndex].pageIndex].pageURL; }

                // Get the stored data before coroutine and create the page
                if (createNoConnection) { OpenNoConnectionPage(currentTabs[tabIndex].guid); }
                else if (createNotFound) { OpenNotFoundPage(currentTabs[tabIndex].guid); }
            }

            else
            {
                // Get the stored data before coroutine and create the page as disabled
                if (createNoConnection) { OpenNoConnectionPage(currentTabs[tabIndex].guid, false); }
                else if (createNotFound) { OpenNotFoundPage(currentTabs[tabIndex].guid, false); }
            }

            // Delete the coroutine entry
            for (int i = 0; i < activeCoroutines.Count; i++)
            {
                if (activeCoroutines[i].targetGuid == currentTabs[tabIndex].guid)
                {
                    activeCoroutines.RemoveAt(i);
                    break;
                }
            }
        }

        public void GoBack()
        {
            int tabIndex = GetTabIndex(currentTabGuid);

            // Check for previous pages
            if (currentTabs[tabIndex].tabHistory.Count > 0)
            {
                currentTabs[tabIndex].pageIndex--;
                OpenPage(currentTabs[tabIndex].tabHistory[currentTabs[tabIndex].pageIndex].pageURL, false);
            }
        }

        public void GoForward()
        {
            int tabIndex = GetTabIndex(currentTabGuid);

            // Check for previous pages
            if (currentTabs[tabIndex].tabHistory.Count > 0 && currentTabs[tabIndex].pageIndex < currentTabs[tabIndex].tabHistory.Count - 1)
            {
                currentTabs[tabIndex].pageIndex++;
                OpenPage(currentTabs[tabIndex].tabHistory[currentTabs[tabIndex].pageIndex].pageURL, false);
            }
        }

        public void Refresh()
        {
            OpenPage(currentTabs[GetTabIndex(currentTabGuid)].pageUrl, false);
        }

        public void DownloadFile(string fileName)
        {
            for (int i = 0; i < webLibrary.dlFiles.Count; i++)
            {
                string shortKey = webLibrary.dlFiles[i].fileName + "_DownloadState";

                if (webLibrary.dlFiles[i].fileName == fileName)
                {
                    if (DreamOSDataManager.ContainsJsonKey(dataCat, shortKey) && DreamOSDataManager.ReadIntData(dataCat, shortKey) != 0)
                    {
                        if (openDownloadsPanel && downloadsPanel != null) { downloadsPanel.OpenPanel(); }
                        return;
                    }

                    int index = i;

                    // Set starting data
                    DreamOSDataManager.WriteIntData(dataCat, fileName + "_DownloadState", 1);

                    // Create the object
                    GameObject dObject = Instantiate(downloadPreset, new Vector3(0, 0, 0), Quaternion.identity);
                    dObject.name = webLibrary.dlFiles[i].fileName;
                    dObject.transform.SetParent(downloadsParent, false);

                    // Get the component
                    WebBrowserDownloadItem dItem = dObject.GetComponent<WebBrowserDownloadItem>();
                    dItem.manager = this;
                    dItem.fileIcon = webLibrary.dlFiles[i].fileIcon;
                    dItem.fileName = webLibrary.dlFiles[i].fileName;
                    dItem.fileSize = webLibrary.dlFiles[i].fileSize;
                    dItem.ProcessDownload();
                    activeDownloads.Add(dItem);

                    // Set events
                    if (webLibrary.dlFiles[i].fileType == WebBrowserLibrary.FileType.Music && musicPlayerApp != null)
                    {
                        dItem.buttonObject.onClick.AddListener(delegate
                        {
                            musicPlayerApp.gameObject.GetComponent<WindowManager>().OpenWindow();
                            musicPlayerApp.PlayCustomClip(webLibrary.dlFiles[index].musicReference, dItem.fileIcon, dItem.fileName, "Downloads");
                        });
                    }

                    else if (webLibrary.dlFiles[i].fileType == WebBrowserLibrary.FileType.Note && notepadApp != null)
                    {
                        dItem.buttonObject.onClick.AddListener(delegate
                        {
                            notepadApp.gameObject.GetComponent<WindowManager>().OpenWindow();
                            notepadApp.OpenCustomNote(dItem.fileName, webLibrary.dlFiles[index].noteReference);
                        });
                    }

                    else if (webLibrary.dlFiles[i].fileType == WebBrowserLibrary.FileType.Photo && photoGalleryApp != null)
                    {
                        dItem.buttonObject.onClick.AddListener(delegate
                        {
                            photoGalleryApp.gameObject.GetComponent<WindowManager>().OpenWindow();
                            photoGalleryApp.OpenPhoto(webLibrary.dlFiles[index].photoReference, dItem.fileName, "Downloads");
                        });
                    }

                    else if (webLibrary.dlFiles[i].fileType == WebBrowserLibrary.FileType.Video && videoPlayerApp != null)
                    {
                        dItem.buttonObject.onClick.AddListener(delegate
                        {
                            videoPlayerApp.gameObject.GetComponent<WindowManager>().OpenWindow();
                            videoPlayerApp.OpenVideo(webLibrary.dlFiles[index].videoReference, dItem.fileName);
                        });
                    }

                    break;
                }
            }

            // Open the downloads panel if available
            if (openDownloadsPanel && downloadsPanel != null) { downloadsPanel.OpenPanel(); }
        }

        public void DeleteDownloadedFile(string fileName)
        {
            for (int i = 0; i < webLibrary.dlFiles.Count; i++)
            {
                if (webLibrary.dlFiles[i].fileName == fileName)
                {
                    DreamOSDataManager.WriteIntData(dataCat, fileName + "_DownloadState", 0);
                    foreach (Transform tempObj in downloadsParent) { if (tempObj.gameObject.name == fileName) { Destroy(tempObj.gameObject); } }
                    break;
                }
            }
        }

        public void UpdateButtonStates()
        {
            if (backButton == null || forwardButton == null)
                return;

            int tabIndex = GetTabIndex(currentTabGuid);

            if (currentTabs[tabIndex].tabHistory.Count > 1 && currentTabs[tabIndex].pageIndex == 0)
            {
                backButton.Interactable(false);
                forwardButton.Interactable(true);
            }

            else if (currentTabs[tabIndex].tabHistory.Count > 1 && currentTabs[tabIndex].pageIndex == currentTabs[tabIndex].tabHistory.Count - 1)
            {
                backButton.Interactable(true);
                forwardButton.Interactable(false);
            }

            else if (currentTabs[tabIndex].tabHistory.Count > 1 && currentTabs[tabIndex].pageIndex != 0 && currentTabs[tabIndex].pageIndex != currentTabs[tabIndex].tabHistory.Count - 1)
            {
                backButton.Interactable(true);
                forwardButton.Interactable(true);
            }

            else
            {
                backButton.Interactable(false);
                forwardButton.Interactable(false);
            }
        }

        public void GetFavoriteState(string url)
        {
            if (!DreamOSDataManager.ContainsJsonKey(dataCat, url + "_IsFavorite")) { favoriteAnimator.PlayOut(); }
            else if (DreamOSDataManager.ReadBooleanData(dataCat, url + "_IsFavorite")) { favoriteAnimator.PlayIn(); }
            else if (url == webLibrary.homePage.pageURL || url == webLibrary.noConnectionPage.pageURL || url == webLibrary.notFoundPage.pageURL) { favoriteAnimator.PlayOut(); }
            else { favoriteAnimator.PlayOut(); }
        }

        public void SetFavoriteState()
        {
            int tabIndex = GetTabIndex(currentTabGuid);
            string url = currentTabs[tabIndex].pageUrl;

            // Return if it's not a custom web page
            if (url == webLibrary.homePage.pageURL || url == webLibrary.noConnectionPage.pageURL || url == webLibrary.notFoundPage.pageURL)
                return;

            // Cache value
            bool value;

            // Get the current data
            if (!DreamOSDataManager.ContainsJsonKey(dataCat, url + "_IsFavorite")) { value = true; }
            else if (!DreamOSDataManager.ReadBooleanData(dataCat, url + "_IsFavorite")) { value = true; }
            else { value = false; }

            // Call the function
            SetFavoriteState(value, url);
        }

        public void SetFavoriteState(bool value, string url, bool writeData = true)
        {
            bool pageCatched = false;
            int tabIndex = GetTabIndex(currentTabGuid);
            WebBrowserLibrary.WebPage targetPage = GetWebPage(url);

            // Search page in web pages
            for (int i = 0; i < webLibrary.webPages.Count; i++)
            {
                if (url == webLibrary.webPages[i].pageURL)
                {
                    pageCatched = true;
                    break;
                }
            }

            if (!pageCatched)
                return;

            if (value == true)
            {
                // Create the object
                GameObject fObject = Instantiate(favoritePreset, new Vector3(0, 0, 0), Quaternion.identity);
                fObject.name = url;
                fObject.transform.SetParent(favoritesParent, false);

                // Get the component
                WebBrowserFavoritesItem fItem = fObject.GetComponent<WebBrowserFavoritesItem>();
                fItem.manager = this;
                fItem.url = url;
                fItem.iconObject.sprite = targetPage.pageIcon;
                fItem.titleObject.text = targetPage.pageTitle;
                fItem.urlObject.text = targetPage.pageURL;
                fItem.button.onClick.AddListener(delegate { OpenPage(fItem.url); });
                favoritePages.Add(fItem);

                // Play the animation
                favoriteAnimator.PlayIn();
            }

            else 
            {
                // Search for the item
                for (int i = 0; i < favoritePages.Count; i++)
                {
                    if (favoritePages[i].url == url)
                    {
                        Destroy(favoritePages[i].gameObject);
                        favoritePages.RemoveAt(i);
                        break;
                    }
                }

                // Play the animation
                favoriteAnimator.PlayOut();
            }

            // Write the data
            if (writeData) { DreamOSDataManager.WriteBooleanData(dataCat, url + "_IsFavorite", value); }
        }

        public void ListFavorites()
        {
            foreach (Transform child in favoritesParent) { Destroy(child.gameObject); }
            for (int i = 0; i < webLibrary.webPages.Count; i++)
            {
                if (DreamOSDataManager.ContainsJsonKey(dataCat, webLibrary.webPages[i].pageURL + "_IsFavorite"))
                {
                    SetFavoriteState(DreamOSDataManager.ReadBooleanData(dataCat, webLibrary.webPages[i].pageURL + "_IsFavorite"), webLibrary.webPages[i].pageURL, false);
                }
            }
        }

        public void ListDownloads()
        {
            foreach (Transform child in downloadsParent) { Destroy(child.gameObject); }
            for (int i = 0; i < webLibrary.dlFiles.Count; i++)
            {
                if (DreamOSDataManager.ContainsJsonKey(dataCat, webLibrary.dlFiles[i].fileName + "_DownloadState") && DreamOSDataManager.ReadIntData(dataCat, webLibrary.dlFiles[i].fileName + "_DownloadState") != 0)
                {
                    int index = i;

                    // Create the object
                    GameObject dObject = Instantiate(downloadPreset, new Vector3(0, 0, 0), Quaternion.identity);
                    dObject.name = webLibrary.dlFiles[i].fileName;
                    dObject.transform.SetParent(downloadsParent, false);

                    // Get the component
                    WebBrowserDownloadItem dItem = dObject.GetComponent<WebBrowserDownloadItem>();
                    dItem.manager = this;
                    dItem.fileIcon = webLibrary.dlFiles[i].fileIcon;
                    dItem.fileName = webLibrary.dlFiles[i].fileName;
                    dItem.fileSize = webLibrary.dlFiles[i].fileSize;

                    // Set events
                    if (webLibrary.dlFiles[i].fileType == WebBrowserLibrary.FileType.Music && musicPlayerApp != null)
                    {
                        dItem.buttonObject.onClick.AddListener(delegate
                        {
                            musicPlayerApp.gameObject.GetComponent<WindowManager>().OpenWindow();
                            musicPlayerApp.PlayCustomClip(webLibrary.dlFiles[index].musicReference, dItem.fileIcon, dItem.fileName, "Downloads");
                        });
                    }

                    else if (webLibrary.dlFiles[i].fileType == WebBrowserLibrary.FileType.Note && notepadApp != null)
                    {
                        dItem.buttonObject.onClick.AddListener(delegate
                        {
                            notepadApp.gameObject.GetComponent<WindowManager>().OpenWindow();
                            notepadApp.OpenCustomNote(dItem.fileName, webLibrary.dlFiles[index].noteReference);
                        });
                    }

                    else if (webLibrary.dlFiles[i].fileType == WebBrowserLibrary.FileType.Photo && photoGalleryApp != null)
                    {
                        dItem.buttonObject.onClick.AddListener(delegate
                        {
                            photoGalleryApp.gameObject.GetComponent<WindowManager>().OpenWindow();
                            photoGalleryApp.OpenPhoto(webLibrary.dlFiles[index].photoReference, dItem.fileName, "Downloads");
                        });
                    }

                    else if (webLibrary.dlFiles[i].fileType == WebBrowserLibrary.FileType.Video && videoPlayerApp != null)
                    {
                        dItem.buttonObject.onClick.AddListener(delegate
                        {
                            videoPlayerApp.gameObject.GetComponent<WindowManager>().OpenWindow();
                            videoPlayerApp.OpenVideo(webLibrary.dlFiles[index].videoReference, dItem.fileName);
                        });
                    }

                    // Check the download state data
                    if (DreamOSDataManager.ReadIntData(dataCat, webLibrary.dlFiles[i].fileName + "_DownloadState") == 1) { activeDownloads.Add(dItem); dItem.ProcessDownload(); }
                    else if (DreamOSDataManager.ReadIntData(dataCat, webLibrary.dlFiles[i].fileName + "_DownloadState") == 2) { dItem.ProcessComplete(); }
                }
            }
        }

        public void ActivateURLField(bool value)
        {
            isUrlFieldActive = value;
        }

        int GetTabIndex(string guid)
        {
            int tabIndex = -1;

            for (int i = 0; i < currentTabs.Count; i++)
            {
                if (currentTabs[i].guid == guid)
                {
                    tabIndex = i;
                    break;
                }
            }

            return tabIndex;
        }

        WebBrowserLibrary.WebPage GetWebPage(string url)
        {
            WebBrowserLibrary.WebPage item = null;

            for (int i = 0; i < webLibrary.webPages.Count; i++)
            {
                if (webLibrary.webPages[i].pageURL == url)
                {
                    item = webLibrary.webPages[i];
                    break;
                }
            }

            return item;
        }
    }
}