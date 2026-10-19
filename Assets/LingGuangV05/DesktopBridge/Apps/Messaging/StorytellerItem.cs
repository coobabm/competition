using LingGuangV05.Desktop.OS;
using UnityEngine;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>
    /// One reply choice in the storyteller panel (moved from DreamOS 2026-10-19; same GUID). Clicking it sends the
    /// reply as the player, closes the panel, starts the contact's answer and fires any reply event with its ID.
    /// </summary>
    [RequireComponent(typeof(ButtonManager))]
    public class StorytellerItem : MonoBehaviour
    {
        [HideInInspector] public int itemIndex;
        [HideInInspector] public int layoutIndex;
        [HideInInspector] public ChatLayoutPreset layout;
        [HideInInspector] public MessagingApp msgManager;
        [HideInInspector] public DynamicMessageHandler handler;
        /// <summary>The reply's translated text, when it has one.</summary>
        [HideInInspector] public string replyLocKey;

        void Start() => GetComponent<ButtonManager>().onClick.AddListener(Pick);

        void Pick()
        {
            var reply = msgManager.chatList[layoutIndex].chatAsset.storyTeller[msgManager.storyTellerIndex].replies[itemIndex];

            msgManager.HideStorytellerPanel();
            msgManager.CreateMessage(layout, string.IsNullOrEmpty(replyLocKey) ? reply.replyContent : replyLocKey);
            msgManager.stItemIndex = itemIndex;
            msgManager.isStoryTellerOpen = false;

            if (!string.IsNullOrEmpty(reply.replyFeedback))
                handler.StartCoroutine(handler.HandleStoryTellerLatency(reply.feedbackLatency, layoutIndex, itemIndex));

            var replyEvent = msgManager.storytellerReplyEvents.Find(e => e.replyID == gameObject.name);
            replyEvent?.onReplySelect.Invoke();
        }
    }
}
