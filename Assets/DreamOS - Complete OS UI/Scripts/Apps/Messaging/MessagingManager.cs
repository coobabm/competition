using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

namespace Michsky.DreamOS
{
    public class MessagingManager : MonoBehaviour
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
        [SerializeField] private PhotoGalleryManager photoGalleryManager;
        public MessageStoring messageStoring;

        // List
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

        // Helpers
        bool sentSoundHelper = false;
        string latestDynamicMessage;
        string latestSTMessage;
        string tempInputMessage;
        float cachedStorytellerPanelLength = 0.5f;
        [HideInInspector] public bool allowInputSubmit;
        [HideInInspector] public ChatLayoutPreset selectedLayout;
        [HideInInspector] public int currentLayout;
        [HideInInspector] public int dynamicMessageIndex = 0;
        [HideInInspector] public int storyTellerIndex = 0;
        [HideInInspector] public int stItemIndex = 0;
        [HideInInspector] public int stIndexHelper = 0;
        [HideInInspector] public bool isStoryTellerOpen; // isStoryTellerWaiting
        [HideInInspector] public string latestPerson;
        [HideInInspector] public UnityEvent externalEvents = new UnityEvent();

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

        public class StorytellerReplyEvent
        {
            public string replyID;
            public UnityEvent onReplySelect = new UnityEvent();
        }

        public enum Status { Offline, Online }

        void Awake()
        {
#if UNITY_2023_2_OR_NEWER
            if (photoGalleryManager == null && FindObjectsByType<PhotoGalleryManager>(FindObjectsSortMode.None).Length > 0) { photoGalleryManager = FindObjectsByType<PhotoGalleryManager>(FindObjectsSortMode.None)[0]; }
#else
            if (photoGalleryManager == null && FindObjectsOfType(typeof(PhotoGalleryManager)).Length > 0) { photoGalleryManager = (PhotoGalleryManager)FindObjectsOfType(typeof(PhotoGalleryManager))[0]; }  
#endif
            if (storyTellerAnimator != null) { cachedStorytellerPanelLength = DreamOSInternalTools.GetAnimatorClipLength(storyTellerAnimator, "StoryTeller_In") + 0.1f; }

            Initialize();

            if (messageStoring != null && saveMessageHistory) { messageStoring.ReadMessageData(); }
        }

        void OnEnable()
        {
            if (isStoryTellerOpen && stIndexHelper == currentLayout && storyTellerAnimator != null) { ShowStorytellerPanel(); }
            else if (!isStoryTellerOpen && storyTellerAnimator != null) { SetStorytellerPanelDefault(); }
            if (chatList[currentLayout].isVisible && selectedLayout != null) { selectedLayout.Show(); }
        }

        void Update()
        {
            if (string.IsNullOrEmpty(messageInput.text) || EventSystem.current.currentSelectedGameObject != messageInput.gameObject) { return; }
            else if (!messageInput.isFocused) { messageInput.ActivateInputField(); }

            if (allowInputSubmit && Keyboard.current.enterKey.wasPressedThisFrame)
            {
                CreateCustomMessageFromInput(null, true);
            }
        }

        public void Initialize()
        {
            createdLayoutPresets.Clear();

            foreach (Transform child in chatParent) { Destroy(child.gameObject); }
            foreach (Transform child in chatViewer) { Destroy(child.gameObject); }
            for (int i = 0; i < chatList.Count; ++i)
            {
                // Create chat layout
                GameObject layoutObj = Instantiate(chatLayout, new Vector3(0, 0, 0), Quaternion.identity);
                layoutObj.transform.SetParent(chatViewer, false);
                layoutObj.gameObject.name = chatList[i].chatTitle;

                ChatLayoutPreset layoutPreset = layoutObj.GetComponent<ChatLayoutPreset>();
                layoutPreset.manager = this;
                layoutPreset.personPicture = chatList[i].individualPicture;
                layoutPreset.personName = chatList[i].individualName + " " + chatList[i].individualSurname;
                createdLayoutPresets.Add(layoutPreset);

                // Create beginning indicator
                if (beginningIndicator != null)
                {
                    GameObject indicator = Instantiate(beginningIndicator, new Vector3(0, 0, 0), Quaternion.identity);
                    indicator.transform.SetParent(layoutPreset.messageParent, false);

                    // TextMeshProUGUI indicatorMessage = indicator.GetComponentInChildren<TextMeshProUGUI>();
                    // indicatorMessage.text = indicatorMessage.text + " <b>" + chatList[i].individualName + "</b>";
                }

                // Check and create message presets
                for (int x = 0; x < chatList[i].chatAsset.messageList.Count; ++x)
                {
                    if (chatList[i].chatAsset.messageList[x].objectType == MessagingChat.ObjectType.Message)
                    {
                        GameObject objToCreate = null;

                        if (chatList[i].chatAsset.messageList[x].messageAuthor == MessagingChat.MessageAuthor.Individual) { objToCreate = textMessageRecieved; }
                        else if (chatList[i].chatAsset.messageList[x].messageAuthor == MessagingChat.MessageAuthor.Self) { objToCreate = textMessageSent; }

                        GameObject msgObj = Instantiate(objToCreate, new Vector3(0, 0, 0), Quaternion.identity);
                        msgObj.transform.SetParent(layoutPreset.messageParent, false);

                        ChatMessagePreset messagePreset = msgObj.GetComponent<ChatMessagePreset>();
                        messagePreset.timeText.text = chatList[i].chatAsset.messageList[x].sentTime;

                        LocalizedObject tempLoc = messagePreset.contentText.gameObject.GetComponent<LocalizedObject>();

                        if (!useLocalization || string.IsNullOrEmpty(chatList[i].chatAsset.messageList[x].messageKey) || tempLoc == null || !tempLoc.CheckLocalizationStatus()) { messagePreset.contentText.text = chatList[i].chatAsset.messageList[x].messageContent; }
                        else if (tempLoc != null)
                        {
                            tempLoc.localizationKey = chatList[i].chatAsset.messageList[x].messageKey;
                            tempLoc.onLanguageChanged.AddListener(delegate { messagePreset.contentText.text = tempLoc.GetKeyOutput(tempLoc.localizationKey); });
                            tempLoc.InitializeItem();
                            tempLoc.UpdateItem();
                        }
                    }

                    else if (chatList[i].chatAsset.messageList[x].objectType == MessagingChat.ObjectType.AudioMessage)
                    {
                        GameObject objToCreate = null;

                        if (chatList[i].chatAsset.messageList[x].messageAuthor == MessagingChat.MessageAuthor.Individual) { objToCreate = audioMessageRecieved; }
                        else if (chatList[i].chatAsset.messageList[x].messageAuthor == MessagingChat.MessageAuthor.Self) { objToCreate = audioMessageSent; }

                        GameObject msgObj = Instantiate(objToCreate, new Vector3(0, 0, 0), Quaternion.identity);
                        msgObj.transform.SetParent(layoutPreset.messageParent, false);

                        AudioMessage audioMessage = msgObj.GetComponent<AudioMessage>();
                        audioMessage.aSource = AudioManager.instance.audioSource;
                        audioMessage.aClip = chatList[i].chatAsset.messageList[x].audioMessage;
                        audioMessage.timeText.text = chatList[i].chatAsset.messageList[x].sentTime;
                    }

                    else if (chatList[i].chatAsset.messageList[x].objectType == MessagingChat.ObjectType.ImageMessage)
                    {
                        GameObject objToCreate = null;

                        if (chatList[i].chatAsset.messageList[x].messageAuthor == MessagingChat.MessageAuthor.Individual) { objToCreate = imageMessageRecieved; }
                        else if (chatList[i].chatAsset.messageList[x].messageAuthor == MessagingChat.MessageAuthor.Self) { objToCreate = imageMessageSent; }

                        GameObject msgObj = Instantiate(objToCreate, new Vector3(0, 0, 0), Quaternion.identity);
                        msgObj.transform.SetParent(layoutPreset.messageParent, false);

                        ImageMessage imgMessage = msgObj.GetComponent<ImageMessage>();
                        imgMessage.title = chatList[i].chatAsset.messageList[x].messageContent;
                        imgMessage.description = chatList[i].individualName + " " + chatList[i].individualSurname;
                        imgMessage.spriteVar = chatList[i].chatAsset.messageList[x].imageMessage;
                        imgMessage.imageObject.sprite = imgMessage.spriteVar;
                        imgMessage.timeText.text = chatList[i].chatAsset.messageList[x].sentTime;
                        if (photoGalleryManager != null) { imgMessage.pgm = photoGalleryManager; }
                    }

                    else if (chatList[i].chatAsset.messageList[x].objectType == MessagingChat.ObjectType.Date)
                    {
                        GameObject dateObj = Instantiate(messageDate, new Vector3(0, 0, 0), Quaternion.identity);
                        dateObj.transform.SetParent(layoutPreset.messageParent, false);

                        ChatMessagePreset messagePreset = dateObj.GetComponent<ChatMessagePreset>();
                        messagePreset.contentText.text = chatList[i].chatAsset.messageList[x].messageContent;
                    }
                }

                // Create chat item button
                GameObject msgButton = Instantiate(chatItem, new Vector3(0, 0, 0), Quaternion.identity);
                msgButton.transform.SetParent(chatParent, false);
                msgButton.gameObject.name = chatList[i].chatTitle;

                // Set chat item button parameters
                ChatItemPreset itemPreset = msgButton.GetComponent<ChatItemPreset>();
                itemPreset.coverImage.sprite = chatList[i].individualPicture;
                itemPreset.nameText.text = chatList[i].individualName + " " + chatList[i].individualSurname;
                itemPreset.timeText.text = chatList[i].chatAsset.messageList[chatList[i].chatAsset.messageList.Count - 1].sentTime;

                LocalizedObject tempChatLoc = itemPreset.latestMessage.gameObject.GetComponent<LocalizedObject>();

                if (!useLocalization || string.IsNullOrEmpty(chatList[i].chatAsset.messageList[chatList[i].chatAsset.messageList.Count - 1].messageKey) || tempChatLoc == null || !tempChatLoc.CheckLocalizationStatus()) { itemPreset.latestMessage.text = chatList[i].chatAsset.messageList[chatList[i].chatAsset.messageList.Count - 1].messageContent; }
                else if (tempChatLoc != null)
                {
                    tempChatLoc.localizationKey = chatList[i].chatAsset.messageList[chatList[i].chatAsset.messageList.Count - 1].messageKey;
                    tempChatLoc.onLanguageChanged.AddListener(delegate { itemPreset.latestMessage.text = tempChatLoc.GetKeyOutput(tempChatLoc.localizationKey); });
                    tempChatLoc.InitializeItem();
                    tempChatLoc.UpdateItem();
                }

                // Set default status state
                if (chatList[i].defaultStatus == Status.Offline) { itemPreset.ChangeStatus(Status.Offline); }
                else if (chatList[i].defaultStatus == Status.Online) { itemPreset.ChangeStatus(Status.Online); }

                // Add button events
                ButtonManager itemButton = msgButton.GetComponent<ButtonManager>();
                itemButton.onClick.AddListener(delegate
                {
                    if (selectedLayout != null && selectedLayout.name == itemButton.gameObject.name) { return; }
                    if (selectedLayout != null && selectedLayout.gameObject.activeInHierarchy) { selectedLayout.Hide(); }

                    int indexHelper = 0;
                    for (int s = 0; s < createdLayoutPresets.Count; s++)
                    {
                        if (createdLayoutPresets[s].name == itemButton.gameObject.name)
                        {
                            selectedLayout = createdLayoutPresets[s];
                            indexHelper = s;
                            currentLayout = s;
                            break;
                        }
                    }

                    selectedLayout.gameObject.SetActive(true);

                    ChatLayoutPreset slPreset = selectedLayout.GetComponent<ChatLayoutPreset>();
                    slPreset.Show();

                    if (isStoryTellerOpen && stIndexHelper != indexHelper && storyTellerAnimator != null) { HideStorytellerPanel(); }
                    else if (isStoryTellerOpen && stIndexHelper == indexHelper && storyTellerAnimator != null) { ShowStorytellerPanel(); }

                    itemPreset.EnableNotificationBadge(false);
                    latestPerson = itemPreset.nameText.text;
                });

                layoutObj.SetActive(false);

                if (!chatList[i].isVisible) { msgButton.SetActive(false); }
                if (i == 0) { selectedLayout = layoutPreset; }
            }
        }

        public void CreateMessageFromInput()
        {
            CreateCustomMessageFromInput(null, true);
            messageInput.text = "";
        }

        void CreateCustomMessageFromInput(ChatLayoutPreset parent, bool isSelf)
        {
            if (parent == null) 
            { 
                parent = selectedLayout; 
            }

            if (string.IsNullOrEmpty(messageInput.text) == true || messageInput.text == " ")
            {
                messageInput.text = "";
                return;
            }

            if (selectedLayout != null)
            {
                GameObject msgObj = Instantiate(textMessageSent, new Vector3(0, 0, 0), Quaternion.identity);
                msgObj.transform.SetParent(parent.messageParent, false);

                ChatMessagePreset messagePreset = msgObj.GetComponent<ChatMessagePreset>();
                messagePreset.contentText.text = messageInput.text;
                messagePreset.timeText.text = GetTimeData();

                if (saveMessageHistory && messageStoring != null && isSelf) { messageStoring.ApplyMessageData(parent.name, "standard", "self", messageInput.text, messagePreset.timeText.text); }
                else if (saveMessageHistory && messageStoring != null && !isSelf) { messageStoring.ApplyMessageData(parent.name, "standard", "individual", messageInput.text, messagePreset.timeText.text); }

                LayoutRebuilder.ForceRebuildLayoutImmediate(msgObj.GetComponentInParent<RectTransform>());
                LayoutRebuilder.ForceRebuildLayoutImmediate(msgObj.GetComponent<RectTransform>());
            }

            if (currentLayout <= chatList.Count && chatList[currentLayout].chatAsset.useDynamicMessages && selectedLayout != null && messageInput.text.Length >= 1)
            {
                latestDynamicMessage = messageInput.text.Replace("\n", "");
                CreateDynamicMessage(currentLayout, true);
            }

            if (currentLayout <= chatList.Count && debugStoryTeller && selectedLayout != null && messageInput.text.Length >= 1 && chatList[currentLayout].chatAsset.useStoryTeller)
            {
                latestSTMessage = messageInput.text.Replace("\n", "");
                CreateStoryTeller(chatList[currentLayout].chatTitle, latestSTMessage);
            }

            if (AudioManager.instance != null && !sentSoundHelper) 
            {
                AudioManager.instance.audioSource.PlayOneShot(sentMessageSFX);
            }

            if (isSelf) { UpdateChatItem(parent.name, messageInput.text, false); }
            else { UpdateChatItem(parent.name, messageInput.text, true); }

            externalEvents.Invoke();
            messageInput.text = tempInputMessage;
            sentSoundHelper = false;
        }

        public void CreateMessage(ChatLayoutPreset parent, string msgContent)
        {
            if (selectedLayout == null) { selectedLayout = parent; }

            tempInputMessage = messageInput.text;
            messageInput.text = msgContent;
            CreateCustomMessageFromInput(parent, true);
        }

        public void CreateMessage(int layoutIndex, string msgContent)
        {
            ChatLayoutPreset parent = chatViewer.Find(chatList[layoutIndex].chatTitle).GetComponent<ChatLayoutPreset>();
            CreateMessage(parent, msgContent);
        }

        public void CreateIndividualMessage(ChatLayoutPreset parent, string msgContent, string locKey = null)
        {
            if (selectedLayout == null) { selectedLayout = parent; }
            sentSoundHelper = true;

            GameObject tempMsgObj = textMessageSent;
            textMessageSent = textMessageRecieved;
            tempInputMessage = messageInput.text;   
    
            LocalizedObject tempLoc = gameObject.GetComponent<LocalizedObject>();

            if (!useLocalization || string.IsNullOrEmpty(locKey) || tempLoc == null || !tempLoc.CheckLocalizationStatus()) { messageInput.text = msgContent; }
            else if (tempLoc != null) { messageInput.text = tempLoc.GetKeyOutput(locKey); }

            CreateCustomMessageFromInput(parent, false);
          
            textMessageSent = tempMsgObj;

            if (useNotifications && !parent.gameObject.activeInHierarchy || useNotifications && selectedLayout.name != parent.name)
            {
                for (int i = 0; i < chatList.Count; i++)
                {
                    if (parent.name == chatList[i].chatTitle && latestPerson != chatList[i].individualName)
                    {
                        CreatePopupNotification(notificationIcon, chatList[i].individualName + " " + chatList[i].individualSurname, msgContent);
                        break;
                    }
                }
            }

            else if (AudioManager.instance != null) { AudioManager.instance.audioSource.PlayOneShot(receivedMessageSFX); }
        }

        public void CreateIndividualMessage(int layoutIndex, string msgContent, string locKey = null)
        {
            ChatLayoutPreset parent = chatViewer.Find(chatList[layoutIndex].chatTitle).GetComponent<ChatLayoutPreset>();
            CreateIndividualMessage(parent, msgContent, locKey);
        }

        public void CreateExternalMessage(Transform parent, string msgContent, string msgAuthor)
        {
            GameObject msgObj = Instantiate(textMessageRecieved, new Vector3(0, 0, 0), Quaternion.identity);
            msgObj.transform.SetParent(parent, false);

            ChatMessagePreset messagePreset = msgObj.GetComponent<ChatMessagePreset>();
            messagePreset.contentText.text = msgContent;
            messagePreset.timeText.text = GetTimeData();

            if (useNotifications && !parent.gameObject.activeInHierarchy || useNotifications && latestPerson != msgAuthor) { CreatePopupNotification(notificationIcon, msgAuthor, msgContent); }
            else if (AudioManager.instance != null) { AudioManager.instance.audioSource.PlayOneShot(receivedMessageSFX); }
        }

        public void CreateStoredMessage(string msgID, string message, string time, bool isSelf)
        {
            int tempIndex = 0;

            for (int i = 0; i < chatList.Count; i++)
            {
                if (chatList[i].chatTitle == msgID)
                {
                    tempIndex = i;
                    break;
                }
            }

            ChatLayoutPreset parent = chatViewer.Find(chatList[tempIndex].chatTitle).GetComponent<ChatLayoutPreset>();
            GameObject msgObj;

            if (isSelf == true)
            {
                msgObj = Instantiate(textMessageSent, new Vector3(0, 0, 0), Quaternion.identity);
                UpdateChatItem(chatList[tempIndex].chatTitle, message, false, time);
            }

            else
            {
                msgObj = Instantiate(textMessageRecieved, new Vector3(0, 0, 0), Quaternion.identity);
                UpdateChatItem(chatList[tempIndex].chatTitle, message, true);
            }

            msgObj.transform.SetParent(parent.messageParent, false);

            ChatMessagePreset messagePreset = msgObj.GetComponent<ChatMessagePreset>();
            messagePreset.contentText.text = message;

            LayoutRebuilder.ForceRebuildLayoutImmediate(msgObj.GetComponent<RectTransform>());
        }

        public void CreateCustomMessage(ChatLayoutPreset parent, string message, string time, string locKey = null)
        {
            GameObject msgObj = Instantiate(textMessageSent, new Vector3(0, 0, 0), Quaternion.identity);
            msgObj.transform.SetParent(parent.messageParent, false);

            ChatMessagePreset messagePreset = msgObj.GetComponent<ChatMessagePreset>();
            messagePreset.timeText.text = time;

            LocalizedObject tempLoc = messagePreset.contentText.gameObject.GetComponent<LocalizedObject>();

            if (!useLocalization || string.IsNullOrEmpty(locKey) || tempLoc == null || !tempLoc.CheckLocalizationStatus()) { messagePreset.contentText.text = message; }
            else if (tempLoc != null)
            {
                tempLoc.localizationKey = locKey;
                tempLoc.onLanguageChanged.AddListener(delegate { messagePreset.contentText.text = tempLoc.GetKeyOutput(tempLoc.localizationKey); });
                tempLoc.InitializeItem();
                tempLoc.UpdateItem();

                message = messagePreset.contentText.text;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(msgObj.GetComponent<RectTransform>());

            if (AudioManager.instance != null) { AudioManager.instance.audioSource.PlayOneShot(sentMessageSFX); }
            if (saveMessageHistory && messageStoring != null) { messageStoring.ApplyMessageData(parent.name, "standard", "self", message, time); }

            UpdateChatItem(parent.name, message, false);
        }

        public void CreateCustomMessage(int layoutIndex, string message, string time, string locKey = null)
        {
            ChatLayoutPreset parent = chatViewer.Find(chatList[layoutIndex].chatTitle).GetComponent<ChatLayoutPreset>();
            CreateCustomMessage(parent, message, time, locKey);
        }

        public void CreateCustomIndividualMessage(ChatLayoutPreset parent, string message, string time, string locKey = null)
        {
            GameObject msgObj = Instantiate(textMessageRecieved, new Vector3(0, 0, 0), Quaternion.identity);
            msgObj.transform.SetParent(parent.messageParent, false);

            ChatMessagePreset messagePreset = msgObj.GetComponent<ChatMessagePreset>();
            messagePreset.timeText.text = time;

            if (!useLocalization || string.IsNullOrEmpty(locKey)) { messagePreset.contentText.text = message; }
            else
            {
                LocalizedObject tempLoc = messagePreset.contentText.gameObject.GetComponent<LocalizedObject>();

                if (tempLoc != null)
                {
                    tempLoc.localizationKey = locKey;
                    tempLoc.onLanguageChanged.AddListener(delegate { messagePreset.contentText.text = tempLoc.GetKeyOutput(tempLoc.localizationKey); });
                    tempLoc.InitializeItem();
                    tempLoc.UpdateItem();

                    message = messagePreset.contentText.text;
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(msgObj.GetComponent<RectTransform>());

            if (useNotifications && !parent.gameObject.activeInHierarchy || useNotifications && selectedLayout.name != parent.name)
            {
                for (int i = 0; i < chatList.Count; i++)
                {
                    if (parent.name == chatList[i].chatTitle)
                    {
                        CreatePopupNotification(notificationIcon, chatList[i].individualName + " " + chatList[i].individualSurname, message);
                        break;
                    }
                }
            }

            else if (AudioManager.instance != null) { AudioManager.instance.audioSource.PlayOneShot(receivedMessageSFX); }

            // Check for message storing
            if (saveMessageHistory && messageStoring != null) { messageStoring.ApplyMessageData(parent.name, "standard", "individual", message, time); }

            UpdateChatItem(parent.name, message, true);
        }

        public void CreateCustomIndividualMessage(int layoutIndex, string message, string time, string locKey = null)
        {
            ChatLayoutPreset parent = chatViewer.Find(chatList[layoutIndex].chatTitle).GetComponent<ChatLayoutPreset>();
            CreateCustomIndividualMessage(parent, message, time, locKey);
        }

        public void CreateDate(ChatLayoutPreset parent, string date)
        {
            GameObject dateObj = Instantiate(messageDate, new Vector3(0, 0, 0), Quaternion.identity);
            dateObj.transform.SetParent(parent.messageParent, false);

            ChatMessagePreset messagePreset = dateObj.GetComponent<ChatMessagePreset>();
            messagePreset.contentText.text = date;

            LayoutRebuilder.ForceRebuildLayoutImmediate(dateObj.GetComponent<RectTransform>());
        }

        public void CreateDate(int layoutIndex, string date)
        {
            ChatLayoutPreset parent = chatViewer.Find(chatList[layoutIndex].chatTitle).GetComponent<ChatLayoutPreset>();
            CreateDate(parent, date);
        }

        public void CreateImageMessage(ChatLayoutPreset parent, Sprite sprite, string title, string description, string time = null)
        {
            if (parent == null) { }

            GameObject msgObj = Instantiate(imageMessageSent, new Vector3(0, 0, 0), Quaternion.identity);
            msgObj.transform.SetParent(parent.messageParent, false);

            ImageMessage imgMessage = msgObj.GetComponent<ImageMessage>();
            imgMessage.title = title;
            imgMessage.description = description;
            imgMessage.spriteVar = sprite;
            imgMessage.imageObject.sprite = imgMessage.spriteVar;
            if (photoGalleryManager != null) { imgMessage.pgm = photoGalleryManager; }
            if (string.IsNullOrEmpty(time)) { imgMessage.timeText.text = GetTimeData(); }
            else { imgMessage.timeText.text = time; }

            LayoutRebuilder.ForceRebuildLayoutImmediate(msgObj.GetComponent<RectTransform>());

            if (AudioManager.instance != null) { AudioManager.instance.audioSource.PlayOneShot(sentMessageSFX); }
            UpdateChatItem(parent.name, title, false);
        }

        public void CreateImageMessage(int layoutIndex, Sprite sprite, string title, string description, string time = "")
        {
            ChatLayoutPreset parent = chatViewer.Find(chatList[layoutIndex].chatTitle).GetComponent<ChatLayoutPreset>();
            CreateImageMessage(parent, sprite, title, description, time);
        }

        public void CreateIndividualImageMessage(ChatLayoutPreset parent, Sprite sprite, string title, string description, string time = null)
        {
            GameObject msgObj = Instantiate(imageMessageRecieved, new Vector3(0, 0, 0), Quaternion.identity);
            msgObj.transform.SetParent(parent.messageParent, false);

            ImageMessage imgMessage = msgObj.GetComponent<ImageMessage>();
            imgMessage.title = title;
            imgMessage.description = description;
            imgMessage.spriteVar = sprite;
            imgMessage.imageObject.sprite = imgMessage.spriteVar;
            if (photoGalleryManager != null) { imgMessage.pgm = photoGalleryManager; }
            if (string.IsNullOrEmpty(time)) { imgMessage.timeText.text = GetTimeData(); }
            else { imgMessage.timeText.text = time; }

            LayoutRebuilder.ForceRebuildLayoutImmediate(msgObj.GetComponent<RectTransform>());

            if (useNotifications && !parent.gameObject.activeInHierarchy || useNotifications && selectedLayout.name != parent.name)
            {
                for (int i = 0; i < chatList.Count; i++)
                {
                    if (parent.name == chatList[i].chatTitle)
                    {
                        CreatePopupNotification(notificationIcon, chatList[i].individualName + " " + chatList[i].individualSurname, imageMessageNotification);
                        break;
                    }
                }
            }

            else if (AudioManager.instance != null) { AudioManager.instance.audioSource.PlayOneShot(receivedMessageSFX); }

            UpdateChatItem(parent.name, title, true);
        }

        public void CreateIndividualImageMessage(int layoutIndex, Sprite sprite, string title, string description, string time = null)
        {
            ChatLayoutPreset parent = chatViewer.Find(chatList[layoutIndex].chatTitle).GetComponent<ChatLayoutPreset>();
            CreateIndividualImageMessage(parent, sprite, title, description, time);
        }

        public void CreateAudioMessage(ChatLayoutPreset parent, AudioClip audio, string time = null)
        {
            GameObject msgObj = Instantiate(audioMessageSent, new Vector3(0, 0, 0), Quaternion.identity);
            msgObj.transform.SetParent(parent.messageParent, false);

            AudioMessage audioMessage = msgObj.GetComponent<AudioMessage>();
            audioMessage.aSource = AudioManager.instance.audioSource;
            audioMessage.aClip = audio;

            if (string.IsNullOrEmpty(time)) { audioMessage.timeText.text = GetTimeData(); }
            else { audioMessage.timeText.text = time; }

            LayoutRebuilder.ForceRebuildLayoutImmediate(msgObj.GetComponent<RectTransform>());

            if (AudioManager.instance != null) { AudioManager.instance.audioSource.PlayOneShot(sentMessageSFX); }
            UpdateChatItem(parent.name, audio.name, false);
        }

        public void CreateAudioMessage(int layoutIndex, AudioClip audio, string time = null)
        {
            ChatLayoutPreset parent = chatViewer.Find(chatList[layoutIndex].chatTitle).GetComponent<ChatLayoutPreset>();
            CreateAudioMessage(parent, audio, time);
        }

        public void CreateIndividualAudioMessage(ChatLayoutPreset parent, AudioClip audio, string time = null)
        {
            GameObject msgObj = Instantiate(audioMessageRecieved, new Vector3(0, 0, 0), Quaternion.identity);
            msgObj.transform.SetParent(parent.messageParent, false);

            AudioMessage audioMessage = msgObj.GetComponent<AudioMessage>();
            audioMessage.aSource = AudioManager.instance.audioSource;
            audioMessage.aClip = audio;

            if (string.IsNullOrEmpty(time)) { audioMessage.timeText.text = GetTimeData(); }
            else { audioMessage.timeText.text = time; }

            LayoutRebuilder.ForceRebuildLayoutImmediate(msgObj.GetComponent<RectTransform>());

            if (useNotifications && !parent.gameObject.activeInHierarchy || useNotifications && selectedLayout.name != parent.name)
            {
                for (int i = 0; i < chatList.Count; i++)
                {
                    if (parent.name == chatList[i].chatTitle)
                    {
                        CreatePopupNotification(notificationIcon, chatList[i].individualName + " " + chatList[i].individualSurname, audioMessageNotification);
                        break;
                    }
                }
            }

            else if (AudioManager.instance != null) { AudioManager.instance.audioSource.PlayOneShot(receivedMessageSFX); }

            UpdateChatItem(parent.name, audio.name, true);
        }

        public void CreateIndividualAudioMessage(int layoutIndex, AudioClip audio, string time = null)
        {
            ChatLayoutPreset parent = chatViewer.Find(chatList[layoutIndex].chatTitle).GetComponent<ChatLayoutPreset>();
            CreateIndividualAudioMessage(parent, audio, time);
        }

        public void CreateDynamicMessage(int layoutIndex, bool waitingForTimer = true)
        {
            for (int i = 0; i < chatList[layoutIndex].chatAsset.dynamicMessages.Count; i++)
            {
                if (latestDynamicMessage == chatList[layoutIndex].chatAsset.dynamicMessages[i].messageContent)
                {
                    if (!chatList[layoutIndex].chatAsset.dynamicMessages[i].enableReply) { return; }
                    dynamicMessageIndex = i;
                    break;
                }
            }

            if (!string.IsNullOrEmpty(chatList[layoutIndex].chatAsset.dynamicMessages[dynamicMessageIndex].runStoryteller) && chatList[layoutIndex].chatAsset.useDynamicMessages && chatList[layoutIndex].chatAsset.dynamicMessages[dynamicMessageIndex].messageContent == latestDynamicMessage)
            {
                string tempStorytellerID = null;

                for (int i = 0; i < chatList[layoutIndex].chatAsset.storyTeller.Count; i++)
                {
                    if (chatList[layoutIndex].chatAsset.storyTeller[i].itemID == chatList[layoutIndex].chatAsset.dynamicMessages[dynamicMessageIndex].runStoryteller)
                    {
                        tempStorytellerID = chatList[layoutIndex].chatAsset.storyTeller[i].itemID;
                        break;
                    }
                }

                if (tempStorytellerID == null) { Debug.Log("Couldn't find any Storyteller item with the following ID: " + chatList[layoutIndex].chatAsset.dynamicMessages[dynamicMessageIndex].runStoryteller); }
                else { CreateStoryTeller(chatList[layoutIndex].chatTitle, chatList[layoutIndex].chatAsset.dynamicMessages[dynamicMessageIndex].runStoryteller); }
            }

            else if (!waitingForTimer && chatList[layoutIndex].chatAsset.useDynamicMessages && chatList[layoutIndex].chatAsset.dynamicMessages[dynamicMessageIndex].messageContent == latestDynamicMessage)
            {
                GameObject msgObj = Instantiate(textMessageRecieved, new Vector3(0, 0, 0), Quaternion.identity);
                ChatLayoutPreset layout = null;

                try
                {
                    layout = chatViewer.Find(chatList[layoutIndex].chatTitle).GetComponent<ChatLayoutPreset>();
                    msgObj.transform.SetParent(layout.messageParent, false);
                }

                catch { msgObj.transform.SetParent(selectedLayout.messageParent, false); }

                ChatMessagePreset messagePreset = msgObj.GetComponent<ChatMessagePreset>();
                messagePreset.timeText.text = GetTimeData();

                LocalizedObject tempLoc = messagePreset.contentText.gameObject.GetComponent<LocalizedObject>();

                if (!useLocalization || string.IsNullOrEmpty(chatList[layoutIndex].chatAsset.dynamicMessages[dynamicMessageIndex].replyKey) || tempLoc == null || !tempLoc.CheckLocalizationStatus()) { messagePreset.contentText.text = chatList[layoutIndex].chatAsset.dynamicMessages[dynamicMessageIndex].replyContent; }
                else if (tempLoc != null)
                {
                    tempLoc.localizationKey = chatList[layoutIndex].chatAsset.dynamicMessages[dynamicMessageIndex].replyKey;
                    tempLoc.onLanguageChanged.AddListener(delegate { messagePreset.contentText.text = tempLoc.GetKeyOutput(tempLoc.localizationKey); });
                    tempLoc.InitializeItem();
                    tempLoc.UpdateItem();
                }

                // Store the message
                if (saveMessageHistory && messageStoring != null) { messageStoring.ApplyMessageData(layout.gameObject.name, "standard", "individual", messagePreset.contentText.text, messagePreset.timeText.text); }

                LayoutRebuilder.ForceRebuildLayoutImmediate(msgObj.GetComponentInParent<RectTransform>());
                LayoutRebuilder.ForceRebuildLayoutImmediate(msgObj.GetComponent<RectTransform>());

                if (useNotifications && !layout.gameObject.activeInHierarchy || useNotifications && selectedLayout.name != layout.gameObject.name) { CreatePopupNotification(notificationIcon, chatList[layoutIndex].individualName + " " + chatList[layoutIndex].individualSurname, chatList[layoutIndex].chatAsset.dynamicMessages[dynamicMessageIndex].replyContent); }
                else if (AudioManager.instance != null) { AudioManager.instance.audioSource.PlayOneShot(receivedMessageSFX); }

                UpdateChatItem(chatList[layoutIndex].chatTitle, chatList[layoutIndex].chatAsset.dynamicMessages[dynamicMessageIndex].replyContent, true);
            }

            else if (waitingForTimer && chatList[layoutIndex].chatAsset.dynamicMessages[dynamicMessageIndex].messageContent == latestDynamicMessage)
            {
                allowInputSubmit = false;
              
                GameObject tempHandlerObj = new GameObject();
                tempHandlerObj.name = "[Temp Dynamic Message Handler]";
           
                DynamicMessageHandler tempHandler = tempHandlerObj.AddComponent<DynamicMessageHandler>();
                tempHandler.manager = this;
                tempHandler.StartCoroutine(tempHandler.HandleDynamicMessage(chatList[layoutIndex].chatAsset.dynamicMessages[dynamicMessageIndex].replyLatency, layoutIndex));
            }
        }

        public void CreateStoryTeller(string chatTitle, string storyTellerID)
        {
            if (storyTellerAnimator == null || storyTellerList == null)
                return;

            bool catchedID = false;
            int layoutIndex = -1;
            string replyLocKey = null;

            for (int i = 0; i < chatList.Count; i++)
            {
                if (chatTitle == chatList[i].chatTitle)
                {
                    layoutIndex = i;
                    break;
                }
            }

            if (layoutIndex == -1)
                return;

            for (int i = 0; i < chatList[layoutIndex].chatAsset.storyTeller.Count; i++)
            {
                if (storyTellerID == chatList[layoutIndex].chatAsset.storyTeller[i].itemID)
                {
                    storyTellerIndex = i;
                    catchedID = true;
                    break;
                }
            }

            if (catchedID == false)
                return;

            stIndexHelper = layoutIndex;

            foreach (Transform child in storyTellerList)
                Destroy(child.gameObject);

            GameObject tempHandlerObj = new GameObject();
            tempHandlerObj.name = "[Temp Storyteller Handler] " + chatList[layoutIndex].chatAsset.storyTeller[storyTellerIndex].itemID;

            DynamicMessageHandler tempHandler = tempHandlerObj.AddComponent<DynamicMessageHandler>();
            tempHandler.manager = this;

            if (!string.IsNullOrEmpty(chatList[layoutIndex].chatAsset.storyTeller[storyTellerIndex].messageContent) && chatList[layoutIndex].chatAsset.storyTeller[storyTellerIndex].messageAuthor == MessagingChat.MessageAuthor.Self)
            {
                tempHandler.StartCoroutine(tempHandler.HandleStoryTeller(chatList[layoutIndex].chatAsset.storyTeller[storyTellerIndex].messageLatency, layoutIndex, false));
            }

            else if (!string.IsNullOrEmpty(chatList[layoutIndex].chatAsset.storyTeller[storyTellerIndex].messageContent) && chatList[layoutIndex].chatAsset.storyTeller[storyTellerIndex].messageAuthor == MessagingChat.MessageAuthor.Individual)
            {
                tempHandler.StartCoroutine(tempHandler.HandleStoryTeller(chatList[layoutIndex].chatAsset.storyTeller[storyTellerIndex].messageLatency, layoutIndex, true));
            }

            for (int i = 0; i < chatList[layoutIndex].chatAsset.storyTeller[storyTellerIndex].replies.Count; i++)
            {
                GameObject strObj = Instantiate(storyTellerObject, new Vector3(0, 0, 0), Quaternion.identity);
                strObj.transform.SetParent(storyTellerList, false);

                TextMeshProUGUI strBrief = strObj.transform.GetComponentInChildren<TextMeshProUGUI>();
                LocalizedObject tempLoc = strBrief.gameObject.GetComponent<LocalizedObject>();

                if (!useLocalization || string.IsNullOrEmpty(chatList[layoutIndex].chatAsset.storyTeller[storyTellerIndex].replies[i].briefKey) || tempLoc == null || !tempLoc.CheckLocalizationStatus()) { strBrief.text = chatList[layoutIndex].chatAsset.storyTeller[storyTellerIndex].replies[i].replyBrief; }
                else
                {
                    tempLoc.localizationKey = chatList[layoutIndex].chatAsset.storyTeller[storyTellerIndex].replies[i].briefKey;
                    tempLoc.onLanguageChanged.AddListener(delegate { strBrief.text = tempLoc.GetKeyOutput(tempLoc.localizationKey); });
                    tempLoc.InitializeItem();
                    tempLoc.UpdateItem();

                    replyLocKey = tempLoc.GetKeyOutput(chatList[layoutIndex].chatAsset.storyTeller[storyTellerIndex].replies[i].contentKey);
                }

                ChatLayoutPreset layout = chatViewer.Find(chatList[layoutIndex].chatTitle).GetComponent<ChatLayoutPreset>();
                StorytellerItem sti = strObj.GetComponent<StorytellerItem>();

                sti.layout = layout;
                sti.layoutIndex = layoutIndex;
                sti.itemIndex = i;
                sti.msgManager = this;
                sti.handler = tempHandler;
                sti.name = chatList[layoutIndex].chatAsset.storyTeller[storyTellerIndex].replies[i].replyID;
                sti.replyLocKey = replyLocKey;
            }
        }

        public string GetTimeData()
        {
            string tempValue = null;
            
            if (DateAndTimeManager.instance != null && DateAndTimeManager.instance.useShortTimeFormat)
            {
                if (DateAndTimeManager.instance.currentMinute.ToString().Length == 1) { tempValue = DateAndTimeManager.instance.currentHour + ":" + "0" + DateAndTimeManager.instance.currentMinute; }
                else { tempValue = DateAndTimeManager.instance.currentHour + ":" + DateAndTimeManager.instance.currentMinute; }

                if (DateAndTimeManager.instance.isAm) { tempValue = tempValue + " AM"; }
                else { tempValue = tempValue + " PM"; }
            }

            else if (DateAndTimeManager.instance != null && !DateAndTimeManager.instance.useShortTimeFormat)
            {
                if (DateAndTimeManager.instance.currentMinute.ToString().Length == 1) { tempValue = DateAndTimeManager.instance.currentHour + ":" + "0" + DateAndTimeManager.instance.currentMinute; }
                else { tempValue = DateAndTimeManager.instance.currentHour + ":" + DateAndTimeManager.instance.currentMinute; }
            }

            return tempValue;
        }

        public void EnableDynamicMessageReply(string messageID)
        {
            for (int i = 0; i < chatList[currentLayout].chatAsset.dynamicMessages.Count; i++)
            {
                if (messageID == chatList[currentLayout].chatAsset.dynamicMessages[i].messageID)
                {
                    chatList[currentLayout].chatAsset.dynamicMessages[i].enableReply = true;
                    break;
                }
            }
        }

        public void EnableDynamicMessageReply(int layoutIndex, string messageID)
        {
            for (int i = 0; i < chatList[layoutIndex].chatAsset.dynamicMessages.Count; i++)
            {
                if (messageID == chatList[layoutIndex].chatAsset.dynamicMessages[i].messageID)
                {
                    chatList[layoutIndex].chatAsset.dynamicMessages[i].enableReply = true;
                    break;
                }
            }
        }

        public void DisableDynamicMessageReply(string messageID)
        {
            for (int i = 0; i < chatList[currentLayout].chatAsset.dynamicMessages.Count; i++)
            {
                if (messageID == chatList[currentLayout].chatAsset.dynamicMessages[i].messageID)
                {
                    chatList[currentLayout].chatAsset.dynamicMessages[i].enableReply = false;
                    break;
                }
            }
        }

        public void DisableDynamicMessageReply(int layoutIndex, string messageID)
        {
            for (int i = 0; i < chatList[layoutIndex].chatAsset.dynamicMessages.Count; i++)
            {
                if (messageID == chatList[layoutIndex].chatAsset.dynamicMessages[i].messageID)
                {
                    chatList[layoutIndex].chatAsset.dynamicMessages[i].enableReply = false;
                    break;
                }
            }
        }

        public void EnableChat(string chatTitle)
        {
            chatParent.Find(chatTitle).gameObject.SetActive(true);
        }

        public void UpdateChatItem(string chatTitle, string newMessage, bool useUnreadBadge, string time = null)
        {
            ChatItemPreset mcip = chatParent.Find(chatTitle).GetComponent<ChatItemPreset>();

            if (mcip == null) { return; }
            if (dynamicSorting) { mcip.transform.SetAsFirstSibling(); }

            if (string.IsNullOrEmpty(time)) { mcip.UpdateLatestMessage(newMessage, GetTimeData()); }
            else { mcip.UpdateLatestMessage(newMessage, time); }

            if (selectedLayout != null && !selectedLayout.gameObject.activeInHierarchy && useUnreadBadge == true) { mcip.EnableNotificationBadge(true); }
            else if (selectedLayout != null && chatTitle != selectedLayout.name && useUnreadBadge) { mcip.EnableNotificationBadge(true); }         
        }

        public void ChangeStatus(Status status, string chatTitle)
        {
            ChatItemPreset preset = chatParent.Find(chatTitle).GetComponent<ChatItemPreset>();
            preset.ChangeStatus(status);
        }

        public void AllowInputSubmit(bool value)
        {
            allowInputSubmit = value;
        }

        public void ShowStorytellerPanel()
        {
            isStoryTellerOpen = true;

            storyTellerAnimator.enabled = true;
            storyTellerAnimator.Play("In");

            StopCoroutine("DisableStorytellerAnimator");
            StartCoroutine("DisableStorytellerAnimator");
        }

        public void HideStorytellerPanel()
        {
            storyTellerAnimator.enabled = true;
            storyTellerAnimator.Play("Out");

            StopCoroutine("DisableStorytellerAnimator");
            StartCoroutine("DisableStorytellerAnimator");
        }

        void SetStorytellerPanelDefault()
        {
            storyTellerAnimator.enabled = true;
            storyTellerAnimator.Play("Start");

            StopCoroutine("DisableStorytellerAnimator");
            StartCoroutine("DisableStorytellerAnimator");
        }

        void CreatePopupNotification(Sprite icon, string name, string description)
        {
            if (NotificationManager.instance == null)
                return;

            NotificationManager.instance.CreatePopupNotification(icon, name, description, true, receivedMessageSFX);
        }

        IEnumerator DisableStorytellerAnimator()
        {
            yield return new WaitForSeconds(cachedStorytellerPanelLength);
            storyTellerAnimator.enabled = false;
        }

        public int GetChatLayoutIndexFromTitle(string chatTitle)
        {
            int index = 0;

            for (int i = 0; i < chatList.Count; i++)
            {
                if (chatList[i].chatTitle == chatTitle)
                {
                    index = i;
                    break;
                }
            }

            return index;
        }

        public string GetChatLayoutTitleFromIndex(int index)
        {
            return chatList[index].chatTitle;
        }
    }
}