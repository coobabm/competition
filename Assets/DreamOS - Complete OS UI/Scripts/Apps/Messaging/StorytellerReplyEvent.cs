using UnityEngine;
using UnityEngine.Events;

namespace Michsky.DreamOS
{
    [AddComponentMenu("DreamOS/Apps/Messaging/Storyteller Reply Event")]
    public class StorytellerReplyEvent : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private MessagingManager messagingManager;
        [SerializeField] private string replyID;

        [Header("Events")]
        public UnityEvent onReplySelect = new UnityEvent();

        void Start()
        {
            if (messagingManager == null)
            {
                Debug.LogError("<b>[Storyteller Reply Event]</b> Messaging Manager is missing.", this);
                return;
            }

            MessagingManager.StorytellerReplyEvent item = new MessagingManager.StorytellerReplyEvent();
            item.replyID = replyID;
            item.onReplySelect.AddListener(() => onReplySelect.Invoke());
            messagingManager.storytellerReplyEvents.Add(item);
        }
    }
}