using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// Spawns notifications on the desktop: standard cards that stay in the notification center (optionally with
    /// action buttons) and popup toasts that dismiss themselves. Every app raises them through the static
    /// <c>CreateNotification</c> / <c>CreatePopupNotification</c> helpers, which find the scene's manager.
    /// Replaces DreamOS's NotificationManager (2026-10-19): same GUID, serialized fields and public API.
    /// </summary>
    public class NotificationManager : MonoBehaviour
    {
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

        // The buttons given to the last CreateNotificationWithButtons call. They are kept, so a later plain
        // CreateNotification gets the same buttons again (long-standing behaviour).
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
            GameObject go = Instantiate(standardNotification, new Vector3(0, 0, 0), Quaternion.identity);
            go.transform.SetParent(notificationParent);

            NotificationItem item = go.GetComponent<NotificationItem>();
            item.iconObject.sprite = icon;
            item.titleObject.text = title;
            item.descriptionObject.text = description;

            for (int i = 0; i < ntfButtons.Count; ++i) { AddActionButton(item, i); }

            if (enableSound && AudioManager.instance != null) { AudioManager.instance.audioSource.PlayOneShot(AudioManager.instance.UIManagerAsset.notificationSound); }

            // The toast has its own sound handling, so the card above is the one that plays it
            if (createPopup) { CreatePopupNotification(icon, title, description, false, null); }

            item.Open();
        }

        public void CreateNotificationWithButtons(Sprite icon, string title, string description, List<ButtonItem> buttons, bool enableSound = true, bool createPopup = true)
        {
            ntfButtons = buttons;
            CreateNotification(icon, title, description, createPopup, enableSound);
        }

        public void CreatePopupNotification(Sprite icon, string title, string description, bool enableSound = true, AudioClip customSFX = null)
        {
            GameObject go = Instantiate(popupNotification, new Vector3(0, 0, 0), Quaternion.identity);
            go.transform.SetParent(popupNotificationParent, false);

            NotificationItem item = go.GetComponent<NotificationItem>();
            item.iconObject.sprite = icon;
            item.titleObject.text = title;
            item.descriptionObject.text = description;

            if (enableSound && customSFX != null && AudioManager.instance != null) { AudioManager.instance.audioSource.PlayOneShot(customSFX); }
            else if (enableSound && AudioManager.instance != null) { AudioManager.instance.audioSource.PlayOneShot(AudioManager.instance.UIManagerAsset.notificationSound); }

            item.OpenPopup(popupDuration);
        }

        public static void CreateNotification(Sprite icon, string title, string description, bool createPopup = true, bool enableSound = true, NotificationManager manager = null)
        {
            if (manager == null) { manager = FindSceneManager(); }
            if (manager == null) { Debug.Log("<b>[Notification Creating]</b> Notification Manager is missing."); return; }

            manager.CreateNotification(icon, title, description, createPopup, enableSound);
        }

        public static void CreatePopupNotification(Sprite icon, string title, string description, bool enableSound = true, AudioClip customSFX = null, NotificationManager manager = null)
        {
            if (manager == null) { manager = FindSceneManager(); }
            if (manager == null) { Debug.Log("<b>[Notification Creating]</b> Notification Manager is missing."); return; }

            manager.CreatePopupNotification(icon, title, description, enableSound, customSFX);
        }

        /// <summary>The last manager that lives in a scene (prefab assets and the like are skipped), or null.</summary>
        static NotificationManager FindSceneManager()
        {
            NotificationManager found = null;

            try
            {
                foreach (NotificationManager candidate in Resources.FindObjectsOfTypeAll(typeof(NotificationManager)) as NotificationManager[])
                {
                    if (candidate.gameObject.scene.name != null) { found = candidate; }
                }
            }
            catch { return null; }

            return found;
        }

        void AddActionButton(NotificationItem item, int buttonIndex)
        {
            GameObject buttonObject = Instantiate(notificationButton, new Vector3(0, 0, 0), Quaternion.identity);
            buttonObject.transform.SetParent(item.buttonParent, false);

            ButtonItem definition = ntfButtons[buttonIndex];
            ButtonManager button = buttonObject.GetComponent<ButtonManager>();
            button.buttonText = definition.buttonText;

            if (definition.buttonIcon == null) { button.enableIcon = false; }
            else
            {
                button.enableIcon = true;
                button.buttonIcon = definition.buttonIcon;
            }

            button.UpdateUI();

            // Reads the list at click time, like the original: the caller may keep editing it
            button.onClick.AddListener(delegate
            {
                ntfButtons[buttonIndex].onClick.Invoke();
                item.Close();
            });
        }
    }
}
