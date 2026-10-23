using System.Collections;
using System.Collections.Generic;
using LingGuangV05.Desktop.OS;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>
    /// The desktop chat app that YY sits on (2026-10-19). Replaces DreamOS's MessagingManager: same GUID, serialized
    /// fields, public methods and state, so YY's shell, the 老周 chat adapter and the story presenter bind unchanged.
    ///
    /// Each <see cref="ChatItem"/> gets a contact row and a conversation (<see cref="ChatLayoutPreset"/>) built from
    /// its <see cref="MessagingChat"/> history. Typed messages go out through <see cref="CreateMessageFromInput"/>;
    /// other code posts messages for either side with the Create… methods, which also move the contact to the top,
    /// show its latest line and badge it when unread. A chat can answer typed lines (dynamic messages, with a
    /// typing bubble) and run a storyteller: a scripted line followed by reply choices in a slide-up panel.
    ///
    /// Sending works by putting the text into <see cref="messageInput"/> and submitting it (DreamOS's design, kept:
    /// listeners on <see cref="externalEvents"/> read the input field while it still holds the sent line).
    /// </summary>
    public class MessagingApp : MonoBehaviour
    {
        // Resources
        [SerializeField] private Transform chatParent;
        public Transform chatViewer;
        public GameObject chatLayout;
        [SerializeField] private GameObject chatItem;
        [SerializeField] private GameObject textMessageSent;
        [SerializeField] private GameObject textMessageRecieved;
        [SerializeField] private GameObject imageMessageSent;
        [SerializeField] private GameObject imageMessageRecieved;
        [SerializeField] private GameObject audioMessageSent;
        [SerializeField] private GameObject audioMessageRecieved;
        public GameObject chatMessageTimer;
        [SerializeField] private GameObject messageDate;
        public GameObject beginningIndicator;
        public TMP_InputField messageInput;
        public Animator storyTellerAnimator;
        [SerializeField] private Transform storyTellerList;
        public GameObject storyTellerObject;
        [SerializeField] private PhotoApp photoGalleryManager;
        public MessageStoring messageStoring;

        // Lists
        public List<ChatItem> chatList = new List<ChatItem>();
        public List<ChatLayoutPreset> createdLayoutPresets = new List<ChatLayoutPreset>();
        public List<StorytellerReplyEvent> storytellerReplyEvents = new List<StorytellerReplyEvent>();

        // Settings
        public AudioClip sentMessageSFX;
        public AudioClip receivedMessageSFX;
        public Sprite notificationIcon;
        public bool debugStoryTeller = true;
        public bool useNotifications = true;
        public bool useLocalization = true;
        public bool dynamicSorting = true;
        public bool saveMessageHistory = false;
        [SerializeField] [TextArea(2, 3)] private string audioMessageNotification = "Sent an audio message";
        [SerializeField] [TextArea(2, 3)] private string imageMessageNotification = "Sent an image";

        // State
        [HideInInspector] public bool allowInputSubmit;
        [HideInInspector] public ChatLayoutPreset selectedLayout;
        [HideInInspector] public int currentLayout;
        [HideInInspector] public int dynamicMessageIndex = 0;
        [HideInInspector] public int storyTellerIndex = 0;
        [HideInInspector] public int stItemIndex = 0;
        /// <summary>The chat whose storyteller choices are up.</summary>
        [HideInInspector] public int stIndexHelper = 0;
        /// <summary>Storyteller choices are waiting for the player.</summary>
        [HideInInspector] public bool isStoryTellerOpen;
        /// <summary>Full name of the contact last opened.</summary>
        [HideInInspector] public string latestPerson;
        /// <summary>Runs after every message sent through the input field (typed or posted by code).</summary>
        [HideInInspector] public UnityEvent externalEvents = new UnityEvent();

        bool muteSentSound;
        string latestDynamicMessage;
        string latestSTMessage;
        string savedInput;
        float storytellerPanelLength = 0.5f;
        Coroutine storytellerAnimatorOff;

        [System.Serializable]
        public class ChatItem
        {
            public string chatTitle = "Chat Title";
            public string individualName = "Name";
            public string individualSurname = "Surname";
            public Sprite individualPicture;
            public MessagingChat chatAsset;
            public Status defaultStatus = Status.Offline;
            [Tooltip("Sets the visibility of chat item button.")]
            public bool isVisible = true;
        }

        /// <summary>Something to run when the storyteller reply with this ID is picked.</summary>
        public class StorytellerReplyEvent
        {
            public string replyID;
            public UnityEvent onReplySelect = new UnityEvent();
        }

        public enum Status { Offline, Online }

        void Awake()
        {
            if (photoGalleryManager == null) photoGalleryManager = FindAnyObjectByType<PhotoApp>();
            if (storyTellerAnimator != null)
                storytellerPanelLength = DreamOSInternalTools.GetAnimatorClipLength(storyTellerAnimator, "StoryTeller_In") + 0.1f;

            Initialize();

            if (messageStoring != null && saveMessageHistory) messageStoring.ReadMessageData();
        }

        void OnEnable()
        {
            if (storyTellerAnimator != null)
            {
                if (isStoryTellerOpen && stIndexHelper == currentLayout) ShowStorytellerPanel();
                else if (!isStoryTellerOpen) SetStorytellerPanelDefault();
            }
            if (chatList[currentLayout].isVisible && selectedLayout != null) selectedLayout.Show();
        }

        void Update()
        {
            // Only while the input field is selected and has text; keeps the caret in it.
            if (string.IsNullOrEmpty(messageInput.text) || EventSystem.current.currentSelectedGameObject != messageInput.gameObject) return;
            if (!messageInput.isFocused) messageInput.ActivateInputField();

            if (allowInputSubmit && Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame) SubmitInput(null, true);
        }

        /// <summary>Builds every contact row and conversation from <see cref="chatList"/>; the first chat is selected.</summary>
        public void Initialize()
        {
            createdLayoutPresets.Clear();
            foreach (Transform child in chatParent) Destroy(child.gameObject);
            foreach (Transform child in chatViewer) Destroy(child.gameObject);

            for (int i = 0; i < chatList.Count; ++i)
            {
                ChatItem chat = chatList[i];
                List<MessagingChat.ChatMessage> history = chat.chatAsset.messageList;

                GameObject layoutObj = Place(chatLayout, chatViewer);
                layoutObj.name = chat.chatTitle;
                var layout = layoutObj.GetComponent<ChatLayoutPreset>();
                layout.manager = this;
                layout.personPicture = chat.individualPicture;
                layout.personName = FullName(chat);
                createdLayoutPresets.Add(layout);

                if (beginningIndicator != null) Place(beginningIndicator, layout.messageParent);
                foreach (var entry in history) AddHistoryEntry(chat, layout, entry);

                // Contact row
                GameObject rowObj = Place(chatItem, chatParent);
                rowObj.name = chat.chatTitle;
                var row = rowObj.GetComponent<ChatItemPreset>();
                var last = history[history.Count - 1];
                row.coverImage.sprite = chat.individualPicture;
                row.nameText.text = FullName(chat);
                row.timeText.text = last.sentTime;
                ShowText(row.latestMessage, last.messageKey, last.messageContent);
                row.ChangeStatus(chat.defaultStatus);

                var button = rowObj.GetComponent<ButtonManager>();
                button.onClick.AddListener(() => OpenChat(button.gameObject.name, row));

                layoutObj.SetActive(false);
                if (!chat.isVisible) rowObj.SetActive(false);
                if (i == 0) selectedLayout = layout;
            }
        }

        void AddHistoryEntry(ChatItem chat, ChatLayoutPreset layout, MessagingChat.ChatMessage entry)
        {
            bool theirs = entry.messageAuthor == MessagingChat.MessageAuthor.Individual;
            switch (entry.objectType)
            {
                case MessagingChat.ObjectType.Message:
                {
                    var message = Place(theirs ? textMessageRecieved : textMessageSent, layout.messageParent).GetComponent<ChatMessagePreset>();
                    message.timeText.text = entry.sentTime;
                    ShowText(message.contentText, entry.messageKey, entry.messageContent);
                    break;
                }
                case MessagingChat.ObjectType.AudioMessage:
                {
                    var audio = Place(theirs ? audioMessageRecieved : audioMessageSent, layout.messageParent).GetComponent<AudioMessage>();
                    audio.aSource = AudioManager.instance.audioSource;
                    audio.aClip = entry.audioMessage;
                    audio.timeText.text = entry.sentTime;
                    break;
                }
                case MessagingChat.ObjectType.ImageMessage:
                {
                    var image = Place(theirs ? imageMessageRecieved : imageMessageSent, layout.messageParent).GetComponent<ImageMessage>();
                    image.title = entry.messageContent;
                    image.description = FullName(chat);
                    image.spriteVar = entry.imageMessage;
                    image.imageObject.sprite = image.spriteVar;
                    image.timeText.text = entry.sentTime;
                    if (photoGalleryManager != null) image.pgm = photoGalleryManager;
                    break;
                }
                case MessagingChat.ObjectType.Date:
                    Place(messageDate, layout.messageParent).GetComponent<ChatMessagePreset>().contentText.text = entry.messageContent;
                    break;
            }
        }

        /// <summary>A contact row was clicked: show its conversation and bring its storyteller panel along.</summary>
        void OpenChat(string chatTitle, ChatItemPreset row)
        {
            if (selectedLayout != null && selectedLayout.name == chatTitle) return;
            if (selectedLayout != null && selectedLayout.gameObject.activeInHierarchy) selectedLayout.Hide();

            int index = 0;
            for (int s = 0; s < createdLayoutPresets.Count; s++)
            {
                if (createdLayoutPresets[s].name != chatTitle) continue;
                selectedLayout = createdLayoutPresets[s];
                index = s;
                currentLayout = s;
                break;
            }

            selectedLayout.gameObject.SetActive(true);
            selectedLayout.Show();

            if (isStoryTellerOpen && storyTellerAnimator != null)
            {
                if (stIndexHelper != index) HideStorytellerPanel();
                else ShowStorytellerPanel();
            }

            row.EnableNotificationBadge(false);
            latestPerson = row.nameText.text;
        }

        // ---------- Sending through the input field ----------

        /// <summary>The send button: sends what the player typed and clears the field.</summary>
        public void CreateMessageFromInput()
        {
            SubmitInput(null, true);
            messageInput.text = "";
        }

        /// <summary>
        /// Posts the input field's text into <paramref name="parent"/> (the selected chat when null) as one of ours or,
        /// with the received preset swapped in by the caller, as theirs; then lets the chat answer and puts back
        /// whatever the field held before.
        /// </summary>
        void SubmitInput(ChatLayoutPreset parent, bool isSelf)
        {
            if (parent == null) parent = selectedLayout;

            if (string.IsNullOrEmpty(messageInput.text) || messageInput.text == " ")
            {
                messageInput.text = "";
                return;
            }

            string text = messageInput.text;
            if (selectedLayout != null)
            {
                GameObject msgObj = Place(textMessageSent, parent.messageParent);
                var message = msgObj.GetComponent<ChatMessagePreset>();
                message.contentText.text = text;
                message.timeText.text = GetTimeData();

                if (saveMessageHistory && messageStoring != null)
                    messageStoring.ApplyMessageData(parent.name, "standard", isSelf ? "self" : "individual", text, message.timeText.text);

                LayoutRebuilder.ForceRebuildLayoutImmediate(msgObj.GetComponentInParent<RectTransform>());
                LayoutRebuilder.ForceRebuildLayoutImmediate(msgObj.GetComponent<RectTransform>());
            }

            if (selectedLayout != null && currentLayout < chatList.Count)
            {
                MessagingChat chat = chatList[currentLayout].chatAsset;
                if (chat.useDynamicMessages)
                {
                    latestDynamicMessage = text.Replace("\n", "");
                    CreateDynamicMessage(currentLayout, true);
                }
                if (debugStoryTeller && chat.useStoryTeller)
                {
                    latestSTMessage = text.Replace("\n", "");
                    CreateStoryTeller(chatList[currentLayout].chatTitle, latestSTMessage);
                }
            }

            if (!muteSentSound) PlaySound(sentMessageSFX);
            UpdateChatItem(parent.name, messageInput.text, !isSelf);

            externalEvents.Invoke();
            messageInput.text = savedInput;
            muteSentSound = false;
        }

        /// <summary>Sends <paramref name="msgContent"/> as the player, as if typed.</summary>
        public void CreateMessage(ChatLayoutPreset parent, string msgContent)
        {
            if (selectedLayout == null) selectedLayout = parent;
            savedInput = messageInput.text;
            messageInput.text = msgContent;
            SubmitInput(parent, true);
        }

        public void CreateMessage(int layoutIndex, string msgContent) => CreateMessage(LayoutAt(layoutIndex), msgContent);

        /// <summary>The contact says <paramref name="msgContent"/> (localized by <paramref name="locKey"/> when set).</summary>
        public void CreateIndividualMessage(ChatLayoutPreset parent, string msgContent, string locKey = null)
        {
            if (selectedLayout == null) selectedLayout = parent;
            muteSentSound = true;

            GameObject sentPreset = textMessageSent;
            textMessageSent = textMessageRecieved;
            savedInput = messageInput.text;

            var loc = GetComponent<LocalizedObject>();
            messageInput.text = CanLocalize(loc, locKey) ? loc.GetKeyOutput(locKey) : msgContent;
            SubmitInput(parent, false);
            textMessageSent = sentPreset;

            if (useNotifications && (!parent.gameObject.activeInHierarchy || selectedLayout.name != parent.name))
            {
                foreach (var chat in chatList)
                {
                    if (parent.name == chat.chatTitle && latestPerson != chat.individualName)
                    {
                        CreatePopupNotification(notificationIcon, FullName(chat), msgContent);
                        break;
                    }
                }
            }
            else PlaySound(receivedMessageSFX);
        }

        public void CreateIndividualMessage(int layoutIndex, string msgContent, string locKey = null) =>
            CreateIndividualMessage(LayoutAt(layoutIndex), msgContent, locKey);

        // ---------- Posting messages directly ----------

        /// <summary>A received bubble in any message list (not a chat of this app).</summary>
        public void CreateExternalMessage(Transform parent, string msgContent, string msgAuthor)
        {
            var message = Place(textMessageRecieved, parent).GetComponent<ChatMessagePreset>();
            message.contentText.text = msgContent;
            message.timeText.text = GetTimeData();

            if (useNotifications && (!parent.gameObject.activeInHierarchy || latestPerson != msgAuthor)) CreatePopupNotification(notificationIcon, msgAuthor, msgContent);
            else PlaySound(receivedMessageSFX);
        }

        /// <summary>A line read back from <see cref="MessageStoring"/>.</summary>
        public void CreateStoredMessage(string msgID, string message, string time, bool isSelf)
        {
            int index = Mathf.Max(0, chatList.FindIndex(c => c.chatTitle == msgID));
            string title = chatList[index].chatTitle;
            ChatLayoutPreset parent = LayoutAt(index);

            GameObject msgObj = Instantiate(isSelf ? textMessageSent : textMessageRecieved, Vector3.zero, Quaternion.identity);
            if (isSelf) UpdateChatItem(title, message, false, time);
            else UpdateChatItem(title, message, true);
            msgObj.transform.SetParent(parent.messageParent, false);

            msgObj.GetComponent<ChatMessagePreset>().contentText.text = message;
            LayoutRebuilder.ForceRebuildLayoutImmediate(msgObj.GetComponent<RectTransform>());
        }

        /// <summary>One of our messages with a given time, without going through the input field.</summary>
        public void CreateCustomMessage(ChatLayoutPreset parent, string message, string time, string locKey = null)
        {
            GameObject msgObj = Place(textMessageSent, parent.messageParent);
            var preset = msgObj.GetComponent<ChatMessagePreset>();
            preset.timeText.text = time;

            var loc = preset.contentText.GetComponent<LocalizedObject>();
            if (CanLocalize(loc, locKey))
            {
                BindLocalized(preset.contentText, loc, locKey);
                message = preset.contentText.text;
            }
            else preset.contentText.text = message;

            LayoutRebuilder.ForceRebuildLayoutImmediate(msgObj.GetComponent<RectTransform>());
            PlaySound(sentMessageSFX);
            if (saveMessageHistory && messageStoring != null) messageStoring.ApplyMessageData(parent.name, "standard", "self", message, time);
            UpdateChatItem(parent.name, message, false);
        }

        public void CreateCustomMessage(int layoutIndex, string message, string time, string locKey = null) =>
            CreateCustomMessage(LayoutAt(layoutIndex), message, time, locKey);

        /// <summary>The contact's message with a given time; notifies when that chat isn't in front.</summary>
        public void CreateCustomIndividualMessage(ChatLayoutPreset parent, string message, string time, string locKey = null)
        {
            GameObject msgObj = Place(textMessageRecieved, parent.messageParent);
            var preset = msgObj.GetComponent<ChatMessagePreset>();
            preset.timeText.text = time;

            if (!useLocalization || string.IsNullOrEmpty(locKey)) preset.contentText.text = message;
            else
            {
                // Unlike the other senders this doesn't check the language is ready (as in DreamOS).
                var loc = preset.contentText.GetComponent<LocalizedObject>();
                if (loc != null)
                {
                    BindLocalized(preset.contentText, loc, locKey);
                    message = preset.contentText.text;
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(msgObj.GetComponent<RectTransform>());
            NotifyOrPing(parent, message);
            if (saveMessageHistory && messageStoring != null) messageStoring.ApplyMessageData(parent.name, "standard", "individual", message, time);
            UpdateChatItem(parent.name, message, true);
        }

        public void CreateCustomIndividualMessage(int layoutIndex, string message, string time, string locKey = null) =>
            CreateCustomIndividualMessage(LayoutAt(layoutIndex), message, time, locKey);

        public void CreateDate(ChatLayoutPreset parent, string date)
        {
            GameObject dateObj = Place(messageDate, parent.messageParent);
            dateObj.GetComponent<ChatMessagePreset>().contentText.text = date;
            LayoutRebuilder.ForceRebuildLayoutImmediate(dateObj.GetComponent<RectTransform>());
        }

        public void CreateDate(int layoutIndex, string date) => CreateDate(LayoutAt(layoutIndex), date);

        public void CreateImageMessage(ChatLayoutPreset parent, Sprite sprite, string title, string description, string time = null)
        {
            PlaceImage(imageMessageSent, parent, sprite, title, description, time);
            PlaySound(sentMessageSFX);
            UpdateChatItem(parent.name, title, false);
        }

        public void CreateImageMessage(int layoutIndex, Sprite sprite, string title, string description, string time = "") =>
            CreateImageMessage(LayoutAt(layoutIndex), sprite, title, description, time);

        public void CreateIndividualImageMessage(ChatLayoutPreset parent, Sprite sprite, string title, string description, string time = null)
        {
            PlaceImage(imageMessageRecieved, parent, sprite, title, description, time);
            NotifyOrPing(parent, imageMessageNotification);
            UpdateChatItem(parent.name, title, true);
        }

        public void CreateIndividualImageMessage(int layoutIndex, Sprite sprite, string title, string description, string time = null) =>
            CreateIndividualImageMessage(LayoutAt(layoutIndex), sprite, title, description, time);

        public void CreateAudioMessage(ChatLayoutPreset parent, AudioClip audio, string time = null)
        {
            PlaceAudio(audioMessageSent, parent, audio, time);
            PlaySound(sentMessageSFX);
            UpdateChatItem(parent.name, audio.name, false);
        }

        public void CreateAudioMessage(int layoutIndex, AudioClip audio, string time = null) =>
            CreateAudioMessage(LayoutAt(layoutIndex), audio, time);

        public void CreateIndividualAudioMessage(ChatLayoutPreset parent, AudioClip audio, string time = null)
        {
            PlaceAudio(audioMessageRecieved, parent, audio, time);
            NotifyOrPing(parent, audioMessageNotification);
            UpdateChatItem(parent.name, audio.name, true);
        }

        public void CreateIndividualAudioMessage(int layoutIndex, AudioClip audio, string time = null) =>
            CreateIndividualAudioMessage(LayoutAt(layoutIndex), audio, time);

        void PlaceImage(GameObject preset, ChatLayoutPreset parent, Sprite sprite, string title, string description, string time)
        {
            GameObject msgObj = Place(preset, parent.messageParent);
            var image = msgObj.GetComponent<ImageMessage>();
            image.title = title;
            image.description = description;
            image.spriteVar = sprite;
            image.imageObject.sprite = sprite;
            if (photoGalleryManager != null) image.pgm = photoGalleryManager;
            image.timeText.text = string.IsNullOrEmpty(time) ? GetTimeData() : time;
            LayoutRebuilder.ForceRebuildLayoutImmediate(msgObj.GetComponent<RectTransform>());
        }

        void PlaceAudio(GameObject preset, ChatLayoutPreset parent, AudioClip clip, string time)
        {
            GameObject msgObj = Place(preset, parent.messageParent);
            var audio = msgObj.GetComponent<AudioMessage>();
            audio.aSource = AudioManager.instance.audioSource;
            audio.aClip = clip;
            audio.timeText.text = string.IsNullOrEmpty(time) ? GetTimeData() : time;
            LayoutRebuilder.ForceRebuildLayoutImmediate(msgObj.GetComponent<RectTransform>());
        }

        // ---------- Dynamic replies and the storyteller ----------

        /// <summary>
        /// Answers the line just sent if the chat has a dynamic message for it: either starts its storyteller, or
        /// (waiting) shows a typing bubble and answers after the reply latency and timer.
        /// </summary>
        public void CreateDynamicMessage(int layoutIndex, bool waitingForTimer = true)
        {
            ChatItem chatItem = chatList[layoutIndex];
            MessagingChat chat = chatItem.chatAsset;
            if (chat.dynamicMessages.Count == 0) return;

            for (int i = 0; i < chat.dynamicMessages.Count; i++)
            {
                if (latestDynamicMessage != chat.dynamicMessages[i].messageContent) continue;
                if (!chat.dynamicMessages[i].enableReply) return;
                dynamicMessageIndex = i;
                break;
            }

            var dynamic = chat.dynamicMessages[dynamicMessageIndex];
            if (dynamic.messageContent != latestDynamicMessage) return;

            if (!string.IsNullOrEmpty(dynamic.runStoryteller) && chat.useDynamicMessages)
            {
                if (!chat.storyTeller.Exists(s => s.itemID == dynamic.runStoryteller))
                    Debug.Log("Couldn't find any Storyteller item with the following ID: " + dynamic.runStoryteller);
                else CreateStoryTeller(chatItem.chatTitle, dynamic.runStoryteller);
            }
            else if (!waitingForTimer && chat.useDynamicMessages)
            {
                ChatLayoutPreset layout = chatViewer.Find(chatItem.chatTitle)?.GetComponent<ChatLayoutPreset>();
                if (layout == null) layout = selectedLayout;
                GameObject msgObj = Place(textMessageRecieved, layout.messageParent);

                var message = msgObj.GetComponent<ChatMessagePreset>();
                message.timeText.text = GetTimeData();
                ShowText(message.contentText, dynamic.replyKey, dynamic.replyContent);

                if (saveMessageHistory && messageStoring != null)
                    messageStoring.ApplyMessageData(layout.gameObject.name, "standard", "individual", message.contentText.text, message.timeText.text);

                LayoutRebuilder.ForceRebuildLayoutImmediate(msgObj.GetComponentInParent<RectTransform>());
                LayoutRebuilder.ForceRebuildLayoutImmediate(msgObj.GetComponent<RectTransform>());

                if (useNotifications && (!layout.gameObject.activeInHierarchy || selectedLayout.name != layout.gameObject.name))
                    CreatePopupNotification(notificationIcon, FullName(chatItem), dynamic.replyContent);
                else PlaySound(receivedMessageSFX);

                UpdateChatItem(chatItem.chatTitle, dynamic.replyContent, true);
            }
            else if (waitingForTimer)
            {
                allowInputSubmit = false;
                var handler = new GameObject("[Temp Dynamic Message Handler]").AddComponent<DynamicMessageHandler>();
                handler.manager = this;
                handler.StartCoroutine(handler.HandleDynamicMessage(dynamic.replyLatency, layoutIndex));
            }
        }

        /// <summary>
        /// Starts storyteller item <paramref name="storyTellerID"/> of a chat: its line is posted after a typing
        /// bubble, and its replies become buttons in the storyteller panel.
        /// </summary>
        public void CreateStoryTeller(string chatTitle, string storyTellerID)
        {
            if (storyTellerAnimator == null || storyTellerList == null) return;

            int layoutIndex = chatList.FindIndex(c => c.chatTitle == chatTitle);
            if (layoutIndex == -1) return;
            MessagingChat chat = chatList[layoutIndex].chatAsset;

            int itemIndex = chat.storyTeller.FindIndex(s => s.itemID == storyTellerID);
            if (itemIndex == -1) return;
            storyTellerIndex = itemIndex;
            stIndexHelper = layoutIndex;
            var item = chat.storyTeller[itemIndex];

            foreach (Transform child in storyTellerList) Destroy(child.gameObject);

            var handler = new GameObject("[Temp Storyteller Handler] " + item.itemID).AddComponent<DynamicMessageHandler>();
            handler.manager = this;
            if (!string.IsNullOrEmpty(item.messageContent))
                handler.StartCoroutine(handler.HandleStoryTeller(item.messageLatency, layoutIndex, item.messageAuthor == MessagingChat.MessageAuthor.Individual));

            // A localized reply's text carries over to later unlocalized replies (as in DreamOS).
            string replyLocKey = null;
            for (int i = 0; i < item.replies.Count; i++)
            {
                var reply = item.replies[i];
                GameObject choice = Place(storyTellerObject, storyTellerList);

                var brief = choice.GetComponentInChildren<TextMeshProUGUI>();
                var loc = brief.GetComponent<LocalizedObject>();
                if (CanLocalize(loc, reply.briefKey))
                {
                    BindLocalized(brief, loc, reply.briefKey);
                    replyLocKey = loc.GetKeyOutput(reply.contentKey);
                }
                else brief.text = reply.replyBrief;

                var sti = choice.GetComponent<StorytellerItem>();
                sti.layout = LayoutAt(layoutIndex);
                sti.layoutIndex = layoutIndex;
                sti.itemIndex = i;
                sti.msgManager = this;
                sti.handler = handler;
                sti.name = reply.replyID;
                sti.replyLocKey = replyLocKey;
            }
        }

        public void EnableDynamicMessageReply(string messageID) => SetDynamicReply(currentLayout, messageID, true);
        public void EnableDynamicMessageReply(int layoutIndex, string messageID) => SetDynamicReply(layoutIndex, messageID, true);
        public void DisableDynamicMessageReply(string messageID) => SetDynamicReply(currentLayout, messageID, false);
        public void DisableDynamicMessageReply(int layoutIndex, string messageID) => SetDynamicReply(layoutIndex, messageID, false);

        void SetDynamicReply(int layoutIndex, string messageID, bool value)
        {
            var dynamic = chatList[layoutIndex].chatAsset.dynamicMessages.Find(d => d.messageID == messageID);
            if (dynamic != null) dynamic.enableReply = value;
        }

        public void ShowStorytellerPanel()
        {
            isStoryTellerOpen = true;
            PlayStorytellerPanel("In");
        }

        public void HideStorytellerPanel() => PlayStorytellerPanel("Out");

        void SetStorytellerPanelDefault() => PlayStorytellerPanel("Start");

        void PlayStorytellerPanel(string state)
        {
            storyTellerAnimator.enabled = true;
            storyTellerAnimator.Play(state);
            if (storytellerAnimatorOff != null) StopCoroutine(storytellerAnimatorOff);
            storytellerAnimatorOff = StartCoroutine(DisableStorytellerAnimator());
        }

        IEnumerator DisableStorytellerAnimator()
        {
            yield return new WaitForSeconds(storytellerPanelLength);
            storyTellerAnimator.enabled = false;
            storytellerAnimatorOff = null;
        }

        // ---------- Contacts ----------

        public void EnableChat(string chatTitle) => chatParent.Find(chatTitle).gameObject.SetActive(true);

        /// <summary>Shows a chat's latest line on its contact row, moves the row to the top and badges it when unread.</summary>
        public void UpdateChatItem(string chatTitle, string newMessage, bool useUnreadBadge, string time = null)
        {
            var row = chatParent.Find(chatTitle).GetComponent<ChatItemPreset>();
            if (row == null) return;
            if (dynamicSorting) row.transform.SetAsFirstSibling();

            row.UpdateLatestMessage(newMessage, string.IsNullOrEmpty(time) ? GetTimeData() : time);

            if (useUnreadBadge && selectedLayout != null && (!selectedLayout.gameObject.activeInHierarchy || chatTitle != selectedLayout.name))
                row.EnableNotificationBadge(true);
        }

        public void ChangeStatus(Status status, string chatTitle) => chatParent.Find(chatTitle).GetComponent<ChatItemPreset>().ChangeStatus(status);

        public void AllowInputSubmit(bool value) => allowInputSubmit = value;

        public int GetChatLayoutIndexFromTitle(string chatTitle) => Mathf.Max(0, chatList.FindIndex(c => c.chatTitle == chatTitle));

        public string GetChatLayoutTitleFromIndex(int index) => chatList[index].chatTitle;

        /// <summary>The desktop clock as "h:mm" (with AM/PM in 12-hour mode); null without a clock.</summary>
        public string GetTimeData()
        {
            var clock = DateAndTimeManager.instance;
            if (clock == null) return null;

            string time = clock.currentHour + ":" + (clock.currentMinute.ToString().Length == 1 ? "0" : "") + clock.currentMinute;
            if (clock.useShortTimeFormat) time += clock.isAm ? " AM" : " PM";
            return time;
        }

        // ---------- Helpers ----------

        static GameObject Place(GameObject preset, Transform parent)
        {
            var go = Instantiate(preset, Vector3.zero, Quaternion.identity);
            go.transform.SetParent(parent, false);
            return go;
        }

        ChatLayoutPreset LayoutAt(int index) => chatViewer.Find(chatList[index].chatTitle).GetComponent<ChatLayoutPreset>();

        static string FullName(ChatItem chat) => chat.individualName + " " + chat.individualSurname;

        bool CanLocalize(LocalizedObject loc, string key) =>
            useLocalization && !string.IsNullOrEmpty(key) && loc != null && loc.CheckLocalizationStatus();

        /// <summary>Shows <paramref name="key"/>'s translation, or <paramref name="fallback"/> when it can't be localized.</summary>
        void ShowText(TextMeshProUGUI text, string key, string fallback)
        {
            var loc = text.GetComponent<LocalizedObject>();
            if (CanLocalize(loc, key)) BindLocalized(text, loc, key);
            else text.text = fallback;
        }

        static void BindLocalized(TextMeshProUGUI text, LocalizedObject loc, string key)
        {
            loc.localizationKey = key;
            loc.onLanguageChanged.AddListener(_ => text.text = loc.GetKeyOutput(loc.localizationKey));
            loc.InitializeItem();
            loc.UpdateItem();
        }

        /// <summary>A received message pops a notification unless its chat is open in front; then it just pings.</summary>
        void NotifyOrPing(ChatLayoutPreset parent, string description)
        {
            if (useNotifications && (!parent.gameObject.activeInHierarchy || selectedLayout.name != parent.name))
            {
                var chat = chatList.Find(c => c.chatTitle == parent.name);
                if (chat != null) CreatePopupNotification(notificationIcon, FullName(chat), description);
            }
            else PlaySound(receivedMessageSFX);
        }

        static void PlaySound(AudioClip clip)
        {
            if (AudioManager.instance != null) AudioManager.instance.audioSource.PlayOneShot(clip);
        }

        void CreatePopupNotification(Sprite icon, string title, string description)
        {
            if (NotificationManager.instance == null) return;
            NotificationManager.instance.CreatePopupNotification(icon, title, description, true, receivedMessageSFX);
        }
    }
}
