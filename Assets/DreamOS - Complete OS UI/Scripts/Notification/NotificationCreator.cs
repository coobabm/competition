using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Michsky.DreamOS
{
    [AddComponentMenu("DreamOS/Notification/Notification Creator")]
    public class NotificationCreator : MonoBehaviour
    {
        // Content
        public Sprite icon;
        public string title;
        [TextArea] public string description;
        [TextArea(2, 4)] public string popupDescription;
        public List<NotificationManager.ButtonItem> notificationButtons = new List<NotificationManager.ButtonItem>();

        // Settings
        [SerializeField] private bool enableSound = true;
        [SerializeField] private bool createOnEnable = false;
        public NotificationType notificationType;

        public enum NotificationType { Default, OnlyStandard, OnlyPopup }

        void OnEnable()
        {
            if (createOnEnable)
            {
                CreateNotification();
            }
        }

        public void CreateNotification()
        {
            if (notificationType == NotificationType.Default) { NotificationManager.instance.CreateNotificationWithButtons(icon, title, description, notificationButtons, enableSound, true); }
            else if (notificationType == NotificationType.OnlyStandard) { NotificationManager.instance.CreateNotificationWithButtons(icon, title, description, notificationButtons, enableSound, false); }
            else if (notificationType == NotificationType.OnlyPopup) { NotificationManager.instance.CreatePopupNotification(icon, title, description, enableSound); }
        }

        public void CreateButton(string title, Sprite icon, UnityEvent onClick)
        {
            NotificationManager.ButtonItem bitem = new NotificationManager.ButtonItem();
            bitem.buttonText = title;
            bitem.buttonIcon = icon;
            bitem.onClick.AddListener(delegate { onClick = new UnityEvent(); });
            notificationButtons.Add(bitem);
        }
    }
}