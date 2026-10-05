using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Michsky.DreamOS
{
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
            if (notificationBadge == null)
                return;

            if (value == true) { notificationBadge.SetActive(true); }
            else { notificationBadge.SetActive(false); }
        }

        public void UpdateLatestMessage(string newText, string time)
        {
            latestMessage.text = newText;
            timeText.text = time;
        }

        public void ChangeStatus(MessagingManager.Status status)
        {
            if (status == MessagingManager.Status.Offline) { onlineIndicator.SetActive(false); offlineIndicator.SetActive(true); }
            else if (status == MessagingManager.Status.Online) { onlineIndicator.SetActive(true); offlineIndicator.SetActive(false); }
        }
    }
}