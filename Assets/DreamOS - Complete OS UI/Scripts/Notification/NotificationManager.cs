using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using System.Collections.Generic;

namespace Michsky.DreamOS
{
    public class NotificationManager : MonoBehaviour
    {
        // Static Instance
        public static NotificationManager instance;

        // Resources
        [SerializeField] private Transform notificationParent;
        [SerializeField] private Transform popupNotificationParent;
        [SerializeField] private GameObject notificationButton;

        // Desktop
        public GameObject popupNotification;
        public GameObject standardNotification;

        [HideInInspector] public Image popupIcon;
        [HideInInspector] public TextMeshProUGUI popupTitle;
        [HideInInspector] public TextMeshProUGUI popupDescription;

        // Settings
        [Range(1f, 10)] public float popupDuration = 2.5f;

        // Standard Preset Variables
        [HideInInspector] public Image standardIcon;
        [HideInInspector] public Image standardHeader;
        [HideInInspector] public TextMeshProUGUI standardTitle;
        [HideInInspector] public TextMeshProUGUI standardDescription;

        // Helpers
        List<ButtonItem> ntfButtons = new List<ButtonItem>();

        [System.Serializable]
        public class ButtonItem
        {
            public string buttonText = "Button";
            public Sprite buttonIcon;
            public UnityEvent onClick = new UnityEvent();
        }

        void Awake()
        {
            instance = this;

            foreach (Transform child in notificationParent) { Destroy(child.gameObject); }
            foreach (Transform child in popupNotificationParent) { Destroy(child.gameObject); }
        }

        public void CreateNotification(Sprite icon, string title, string description, bool createPopup = true, bool enableSound = true)
        {
            // Spawn standard notification to the requested parent
            GameObject go = Instantiate(standardNotification, new Vector3(0, 0, 0), Quaternion.identity);
            go.transform.SetParent(notificationParent);

            // Get the notification item
            NotificationItem item = go.GetComponent<NotificationItem>();
            item.iconObject.sprite = icon;
            item.titleObject.text = title;
            item.descriptionObject.text = description;

            // Count requested buttons
            for (int i = 0; i < ntfButtons.Count; ++i)
            {
                // Spawn requested buttons to their parent
                GameObject bgo = Instantiate(notificationButton, new Vector3(0, 0, 0), Quaternion.identity);
                bgo.transform.SetParent(item.buttonParent, false);
                int index = i;

                // Get the button manager
                ButtonManager nButton = bgo.GetComponent<ButtonManager>();
                nButton.buttonText = ntfButtons[i].buttonText;

                if (ntfButtons[i].buttonIcon == null) { nButton.enableIcon = false; }
                else
                {
                    nButton.enableIcon = true;
                    nButton.buttonIcon = ntfButtons[i].buttonIcon;
                }

                nButton.UpdateUI();
                nButton.onClick.AddListener(delegate 
                {
                    ntfButtons[index].onClick.Invoke();
                    item.Close(); 
                });
            }

            // Play sound if enabled
            if (enableSound && AudioManager.instance != null) { AudioManager.instance.audioSource.PlayOneShot(AudioManager.instance.UIManagerAsset.notificationSound); }

            // Create popup notification if enabled
            if (createPopup) { CreatePopupNotification(icon, title, description, false, null); }

            // Open the notification
            item.Open();
        }

        public void CreateNotificationWithButtons(Sprite icon, string title, string description, List<ButtonItem> buttons, bool enableSound = true, bool createPopup = true)
        {
            ntfButtons = buttons;
            CreateNotification(icon, title, description, createPopup, enableSound);
        }

        public void CreatePopupNotification(Sprite icon, string title, string description, bool enableSound = true, AudioClip customSFX = null)
        {
            // Spawn popup notification to the requested parent
            GameObject go = Instantiate(popupNotification, new Vector3(0, 0, 0), Quaternion.identity);
            go.transform.SetParent(popupNotificationParent, false);

            // Get the notification item
            NotificationItem item = go.GetComponent<NotificationItem>();
            item.iconObject.sprite = icon;
            item.titleObject.text = title;
            item.descriptionObject.text = description;

            // Play sound if enabled
            if (enableSound && customSFX != null && AudioManager.instance != null) { AudioManager.instance.audioSource.PlayOneShot(customSFX); }
            else if (enableSound && AudioManager.instance != null) { AudioManager.instance.audioSource.PlayOneShot(AudioManager.instance.UIManagerAsset.notificationSound); }

            // Open the notification
            item.OpenPopup(popupDuration);
        }

        public static void CreateNotification(Sprite icon, string title, string description, bool createPopup = true, bool enableSound = true, NotificationManager manager = null)
        {
            if (manager == null)
            {
                try
                {
                    foreach (NotificationManager nm in Resources.FindObjectsOfTypeAll(typeof(NotificationManager)) as NotificationManager[])
                    {
                        if (nm.gameObject.scene.name != null)
                        {
                            manager = nm;
                        }
                    }
                }

                catch { Debug.Log("<b>[Notification Creating]</b> Notification Manager is missing."); return; }
            }

            manager.CreateNotification(icon, title, description, createPopup, enableSound);
        }

        public static void CreatePopupNotification(Sprite icon, string title, string description, bool enableSound = true, AudioClip customSFX = null, NotificationManager manager = null)
        {
            if (manager == null)
            {
                try
                {
                    foreach (NotificationManager nm in Resources.FindObjectsOfTypeAll(typeof(NotificationManager)) as NotificationManager[])
                    {
                        if (nm.gameObject.scene.name != null)
                        {
                            manager = nm;
                        }
                    }
                }

                catch { Debug.Log("<b>[Notification Creating]</b> Notification Manager is missing."); return; }
            }

            manager.CreatePopupNotification(icon, title, description, enableSound, customSFX);
        }
    }
}