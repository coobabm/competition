using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Michsky.DreamOS
{
    public class WebBrowserDownloadItem : MonoBehaviour
    {
        [Header("Resources")]
        public ButtonManager buttonObject;
        [SerializeField] private Image fileIconObject;
        [SerializeField] private TextMeshProUGUI fileNameObject;
        [SerializeField] private TextMeshProUGUI fileSizeObject;
        [SerializeField] private Slider downloadBar;
        [SerializeField] private TextMeshProUGUI downloadStatus;

        [Header("Settings")]
        [SerializeField] private Sprite notificationIcon;
        [SerializeField] private string notificationDescription = "Download completed";

        // Helpers
        float downloadMultiplier = 0;
        [HideInInspector] public WebBrowserManager manager;
        [HideInInspector] public Sprite fileIcon;
        [HideInInspector] public string fileName;
        [HideInInspector] public float fileSize;
        [HideInInspector] public bool isFinished;
        [HideInInspector] public bool isProcessing;
        DreamOSDataManager.DataCategory dataCat = DreamOSDataManager.DataCategory.Network;

        public void ProcessItem()
        {
            if (isFinished) { this.enabled = false; return; }
            else if (!isProcessing || manager == null || !manager.networkManager.isConnected) { return; }
            else if (manager.networkManager.dynamicNetwork) { downloadMultiplier = manager.networkManager.networkItems[manager.networkManager.currentNetworkIndex].networkSpeed; }
            else if (!manager.networkManager.dynamicNetwork && downloadMultiplier != manager.networkManager.defaultSpeed) { downloadMultiplier = manager.networkManager.defaultSpeed; }

            // Increase the visuals depending on download size
            downloadBar.value += Time.deltaTime * downloadMultiplier;
            downloadStatus.text = string.Format("{0} MB / {1} MB", downloadBar.value.ToString("F1"), downloadBar.maxValue.ToString("F1"));

            // Process complete once the goal is reached
            if (downloadBar.value == fileSize) 
            { 
                ProcessComplete();

                // Delete from active downloads
                for (int i = 0; i < manager.activeDownloads.Count; i++)
                {
                    if (manager.activeDownloads[i].fileName == fileName)
                    {
                        manager.activeDownloads.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        public void ProcessDownload()
        {
            // Set object meta
            fileIconObject.sprite = fileIcon;
            fileNameObject.text = fileName;
            fileSizeObject.text = string.Format("{0} MB", fileSize.ToString());

            // Change the bar value
            downloadBar.value = 0;
            downloadBar.maxValue = fileSize;

            // Set parameters
            buttonObject.Interactable(false);
            isProcessing = true;
            isFinished = false;

            // Set data
            if (DreamOSDataManager.ReadIntData(dataCat, fileName + "_DownloadState") != 1) { DreamOSDataManager.WriteIntData(dataCat, fileName + "_DownloadState", 1); }
        }

        public void ProcessComplete()
        {
            // Set object meta
            fileIconObject.sprite = fileIcon;
            fileNameObject.text = fileName;
            fileSizeObject.text = string.Format("{0} MB", fileSize.ToString());

            // Play notification
            if (isProcessing && NotificationManager.instance != null) { NotificationManager.instance.CreateNotification(notificationIcon, fileName, notificationDescription); }

            // Set parameters
            buttonObject.Interactable(true);
            isProcessing = false;
            isFinished = true;

            // Set data
            if (DreamOSDataManager.ReadIntData(dataCat, fileName + "_DownloadState") != 2) { DreamOSDataManager.WriteIntData(dataCat, fileName + "_DownloadState", 2); }

            // Destroy objects
            Destroy(downloadBar.gameObject);
            Destroy(downloadStatus.gameObject);
        }

        public void DeleteFile()
        {
            manager.DeleteDownloadedFile(fileName);
        }
    }
}