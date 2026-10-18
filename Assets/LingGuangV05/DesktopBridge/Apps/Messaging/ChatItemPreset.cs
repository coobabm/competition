using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>
    /// One contact row of the chat app (moved from DreamOS 2026-10-19; same GUID and fields): picture, name, latest
    /// line and time, online dot and unread badge.
    /// </summary>
    public class ChatItemPreset : MonoBehaviour
    {
        public Image coverImage;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI latestMessage;
        public TextMeshProUGUI timeText;
        public GameObject onlineIndicator;
        public GameObject offlineIndicator;
        [SerializeField] private GameObject notificationBadge;

        public void EnableNotificationBadge(bool value)
        {
            if (notificationBadge != null) notificationBadge.SetActive(value);
        }

        public void UpdateLatestMessage(string newText, string time)
        {
            latestMessage.text = newText;
            timeText.text = time;
        }

        public void ChangeStatus(MessagingApp.Status status)
        {
            bool online = status == MessagingApp.Status.Online;
            onlineIndicator.SetActive(online);
            offlineIndicator.SetActive(!online);
        }
    }
}
