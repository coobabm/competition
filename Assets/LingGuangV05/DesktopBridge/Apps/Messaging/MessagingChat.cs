using System.Collections.Generic;
using UnityEngine;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>
    /// One chat's script (moved from DreamOS 2026-10-19; same GUID, fields and enum orders, so LaoZhou.asset loads
    /// unchanged): the starting history, the lines it answers (dynamic messages) and its storyteller items.
    /// </summary>
    [CreateAssetMenu(fileName = "New Chat", menuName = "LingGuang/Messaging Chat")]
    public class MessagingChat : ScriptableObject
    {
        public bool saveConversation = false;
        public bool useDynamicMessages = false;
        public bool useStoryTeller = false;

        public List<ChatMessage> messageList = new List<ChatMessage>();
        public List<DynamicMessages> dynamicMessages = new List<DynamicMessages>();
        public List<StoryTeller> storyTeller = new List<StoryTeller>();

        public enum DynamicMessageReplyBehavior { DoNothing, DisableReply }

        [System.Serializable]
        public class ChatMessage
        {
            [TextArea(3, 6)] public string messageContent = "My message";
            public ObjectType objectType;
            public MessageAuthor messageAuthor;
            public string sentTime = "00:00";
            public AudioClip audioMessage;
            public Sprite imageMessage;

            [Header("Localization")]
            public string messageKey;
        }

        /// <summary>When the player sends exactly <see cref="messageContent"/>, the contact answers.</summary>
        [System.Serializable]
        public class DynamicMessages
        {
            public string messageID = "MESSAGE_0";
            [TextArea(3, 6)] public string messageContent = "My message";
            [TextArea(3, 6)] public string replyContent = "Reply message";

            [Header("Settings")]
            [Range(0.1f, 25)] public float replyLatency = 1;
            [Range(0.1f, 25)] public float replyTimer = 1.5f;
            public DynamicMessageReplyBehavior replyBehavior = DynamicMessageReplyBehavior.DoNothing;
            public bool enableReply = true;

            [Header("Storyteller")]
            public string runStoryteller;

            [Header("Localization")]
            public string replyKey;
        }

        /// <summary>A scripted line and the reply choices offered after it.</summary>
        [System.Serializable]
        public class StoryTeller
        {
            public string itemID = "ITEM_0";
            public MessageAuthor messageAuthor;
            [TextArea(3, 6)] public string messageContent = "My message";
            [Range(0, 25)] public float messageLatency = 1;
            [Range(0, 25)] public float messageTimer = 1.5f;
            public List<StoryTellerItem> replies = new List<StoryTellerItem>();

            [Header("Localization")]
            public string messageKey;
        }

        /// <summary>A reply choice: what the button says, what gets sent, the contact's answer and what runs next.</summary>
        [System.Serializable]
        public class StoryTellerItem
        {
            public string replyID;
            [TextArea] public string replyBrief = "Reply brief";
            [TextArea] public string replyContent = "Reply content";
            [TextArea] public string replyFeedback = "Reply feedback";
            [Range(0.1f, 25)] public float feedbackLatency = 1;
            [Range(0.1f, 25)] public float feedbackTimer = 1.5f;
            public string callAfter;

            [Header("Localization")]
            public string briefKey;
            public string contentKey;
            public string feedbackKey;
        }

        public enum MessageAuthor { Self, Individual }
        public enum ObjectType { Message, Date, AudioMessage, ImageMessage }
    }
}
