using UnityEngine;
using UnityEngine.Events;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>
    /// Runs <see cref="onReplySelect"/> when the storyteller reply <see cref="replyID"/> is picked in the chat app
    /// (moved from DreamOS 2026-10-19; same GUID and fields).
    /// </summary>
    public class StorytellerReplyEvent : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private MessagingApp messagingManager;
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

            var item = new MessagingApp.StorytellerReplyEvent { replyID = replyID };
            item.onReplySelect.AddListener(onReplySelect.Invoke);
            messagingManager.storytellerReplyEvents.Add(item);
        }
    }
}
