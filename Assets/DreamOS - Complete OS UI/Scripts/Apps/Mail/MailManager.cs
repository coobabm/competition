using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Michsky.DreamOS
{
    public class MailManager : MonoBehaviour
    {
        // Resources
        [SerializeField] private Transform mailViewer;
        [SerializeField] private Transform inboxParent;
        [SerializeField] private Transform sentParent;
        [SerializeField] private Transform junkParent;
        [SerializeField] private PopupPanelManager attachmentPanel;
        [SerializeField] private Transform attachmentParent;
        [SerializeField] private GameObject itemTemplate;
        [SerializeField] private GameObject mailTemplate;
        [SerializeField] private GameObject attachmentItem;
        [SerializeField] private MusicPlayerManager musicManager;
        [SerializeField] private NotepadManager noteManager;
        [SerializeField] private PhotoGalleryManager pictureManager;
        [SerializeField] private VideoPlayerManager videoManager;

        // Settings
        [SerializeField] private bool useLocalization = true;
        public string fromPrefix = "<";
        public string fromSuffix = ">";

        // Content
        public List<MailAsset> mailList = new List<MailAsset>();

        // Helpers
        float cachedTemplateLength = 0.5f;
        MailPreset currentMailPreset;
        MailItemPreset currentItemPreset;

        [System.Serializable]
        public class MailAsset
        {
            public string itemTitle = "Mail Title";
            public MailItem mailAsset;
        }

        void Awake()
        {
            InitializeMails();

            if (mailTemplate != null) { cachedTemplateLength = DreamOSInternalTools.GetAnimatorClipLength(mailTemplate.GetComponent<Animator>(), "MailTemplate_In") + 0.1f; }
#if UNITY_2023_2_OR_NEWER
            if (musicManager == null && FindObjectsByType<MusicPlayerManager>(FindObjectsSortMode.None).Length > 0) { musicManager = FindObjectsByType<MusicPlayerManager>(FindObjectsSortMode.None)[0]; }
            if (noteManager == null && FindObjectsByType<NotepadManager>(FindObjectsSortMode.None).Length > 0) { noteManager = FindObjectsByType<NotepadManager>(FindObjectsSortMode.None)[0]; }
            if (pictureManager == null && FindObjectsByType<PhotoGalleryManager>(FindObjectsSortMode.None).Length > 0) { pictureManager = FindObjectsByType<PhotoGalleryManager>(FindObjectsSortMode.None)[0]; }
            if (videoManager == null && FindObjectsByType<VideoPlayerManager>(FindObjectsSortMode.None).Length > 0) { videoManager = FindObjectsByType<VideoPlayerManager>(FindObjectsSortMode.None)[0]; }
#else
            if (musicManager == null && FindObjectsOfType(typeof(MusicPlayerManager)).Length > 0) { musicManager = (MusicPlayerManager)FindObjectsOfType(typeof(MusicPlayerManager))[0]; }
            if (noteManager == null && FindObjectsOfType(typeof(NotepadManager)).Length > 0) { noteManager = (NotepadManager)FindObjectsOfType(typeof(NotepadManager))[0]; }
            if (pictureManager == null && FindObjectsOfType(typeof(PhotoGalleryManager)).Length > 0) { pictureManager = (PhotoGalleryManager)FindObjectsOfType(typeof(PhotoGalleryManager))[0]; }
            if (videoManager == null && FindObjectsOfType(typeof(VideoPlayerManager)).Length > 0) { videoManager = (VideoPlayerManager)FindObjectsOfType(typeof(VideoPlayerManager))[0]; }
#endif
        }

        public void InitializeMails()
        {
            foreach (Transform child in mailViewer) { Destroy(child.gameObject); }
            foreach (Transform child in inboxParent) { Destroy(child.gameObject); }
            foreach (Transform child in sentParent) { Destroy(child.gameObject); }
            foreach (Transform child in junkParent) { Destroy(child.gameObject); }
            foreach (Transform child in attachmentParent) { Destroy(child.gameObject); }
            for (int i = 0; i < mailList.Count; ++i)
            {
                // Create items
                GameObject itemObj = Instantiate(itemTemplate, new Vector3(0, 0, 0), Quaternion.identity);
                itemObj.gameObject.name = mailList[i].itemTitle;

                if (mailList[i].mailAsset.mailFolder == MailItem.MailFolder.Inbox) { itemObj.transform.SetParent(inboxParent, false); }
                else if (mailList[i].mailAsset.mailFolder == MailItem.MailFolder.Sent) { itemObj.transform.SetParent(sentParent, false); }
                else if (mailList[i].mailAsset.mailFolder == MailItem.MailFolder.Junk) { itemObj.transform.SetParent(junkParent, false); }

                MailItemPreset mip = itemObj.GetComponent<MailItemPreset>();

                if (mailList[i].mailAsset.contactImage == null) { mip.letterText.text = mailList[i].mailAsset.fromName.Substring(0, 1); }
                else
                {
                    mip.coverImage.sprite = mailList[i].mailAsset.contactImage;
                    mip.coverImage.gameObject.SetActive(true);
                }

                mip.mailItem = mailList[i].mailAsset;

                LocalizedObject tempLoc = mip.subjectText.gameObject.GetComponent<LocalizedObject>();

                if (!useLocalization || string.IsNullOrEmpty(mip.mailItem.subjectKey) || tempLoc == null || !tempLoc.CheckLocalizationStatus()) { mip.subjectText.text = mailList[i].mailAsset.subject; }
                else if (tempLoc != null)
                {
                    tempLoc.localizationKey = mip.mailItem.subjectKey;
                    tempLoc.onLanguageChanged.AddListener(delegate { mip.subjectText.text = tempLoc.GetKeyOutput(tempLoc.localizationKey); });
                    tempLoc.InitializeItem();
                    tempLoc.UpdateItem();
                }

                mip.nameText.text = mailList[i].mailAsset.fromName;
                mip.timeText.text = mailList[i].mailAsset.time;
                mip.dateText.text = mailList[i].mailAsset.date;

                // Register item events
                ButtonManager itemBtn = itemObj.GetComponent<ButtonManager>();
                itemBtn.onClick.AddListener(delegate { ApplyToTemplate(mip); });
            }

            if (attachmentPanel != null) { attachmentPanel.gameObject.SetActive(false); attachmentPanel.InstantMinimized(); }
        }

        void ApplyToTemplate(MailItemPreset mip)
        {
            if (currentItemPreset == mip) { return; }
            if (currentMailPreset != null) { Destroy(currentMailPreset.gameObject); }

            foreach (Transform child in attachmentParent) { Destroy(child.gameObject); }

            GameObject viewerObj = Instantiate(mailTemplate, new Vector3(0, 0, 0), Quaternion.identity);
            viewerObj.gameObject.name = mip.mailItem.name;
            viewerObj.transform.SetParent(mailViewer, false);

            currentMailPreset = viewerObj.GetComponent<MailPreset>();
            currentItemPreset = mip;

            if (mip.mailItem.contactImage == null) { currentMailPreset.letterText.text = mip.mailItem.fromName.Substring(0, 1); }
            else
            {
                currentMailPreset.coverImage.sprite = mip.mailItem.contactImage;
                currentMailPreset.coverImage.gameObject.SetActive(true);
            }

            LocalizedObject tempLoc = currentMailPreset.subjectText.gameObject.GetComponent<LocalizedObject>();

            if (!useLocalization || string.IsNullOrEmpty(mip.mailItem.subjectKey) || tempLoc == null || !tempLoc.CheckLocalizationStatus()) { currentMailPreset.subjectText.text = mip.mailItem.subject; }
            else if (tempLoc != null)
            {
                tempLoc.localizationKey = mip.mailItem.subjectKey;
                tempLoc.onLanguageChanged.AddListener(delegate { mip.subjectText.text = tempLoc.GetKeyOutput(tempLoc.localizationKey); });
                tempLoc.InitializeItem();
                tempLoc.UpdateItem();
            }

            currentMailPreset.nameText.text = mip.mailItem.fromName;
            currentMailPreset.fromText.text = fromPrefix + mip.mailItem.from + fromSuffix;
            currentMailPreset.timeText.text = mip.mailItem.time;
            currentMailPreset.dateText.text = mip.mailItem.date;

            LayoutRebuilder.ForceRebuildLayoutImmediate(currentMailPreset.fromText.transform.parent.GetComponent<RectTransform>());
            LayoutRebuilder.ForceRebuildLayoutImmediate(currentMailPreset.dateText.transform.parent.GetComponent<RectTransform>());

            if (!mip.mailItem.useCustomContent)
            {
                currentMailPreset.contentList.SetActive(true);

                LocalizedObject tempConLoc = currentMailPreset.contentText.gameObject.GetComponent<LocalizedObject>();

                if (!useLocalization || string.IsNullOrEmpty(mip.mailItem.contentKey) || tempConLoc == null || !tempLoc.CheckLocalizationStatus()) { currentMailPreset.contentText.text = mip.mailItem.mailContent; }
                else if (tempConLoc != null)
                {
                    tempConLoc.localizationKey = mip.mailItem.contentKey;
                    tempConLoc.onLanguageChanged.AddListener(delegate { currentMailPreset.contentText.text = tempConLoc.GetKeyOutput(mip.mailItem.contentKey); });
                    tempConLoc.InitializeItem();
                    tempConLoc.UpdateItem();
                }

                LayoutRebuilder.ForceRebuildLayoutImmediate(currentMailPreset.contentList.GetComponent<RectTransform>());
            }

            else
            {
                currentMailPreset.contentList.SetActive(false);
                GameObject customContent = Instantiate(mip.mailItem.customContentPrefab, new Vector3(0, 0, 0), Quaternion.identity);
                customContent.gameObject.name = mip.mailItem.name;
                customContent.transform.SetParent(currentMailPreset.customParent, false);
            }

            for (int i = 0; i < mip.mailItem.attachments.Count; ++i)
            {
                int index = i;

                GameObject ataObject = Instantiate(attachmentItem, new Vector3(0, 0, 0), Quaternion.identity);
                ataObject.gameObject.name = mip.mailItem.attachments[i].attachmentTitle;
                ataObject.transform.SetParent(attachmentParent, false);

                ButtonManager ataButton = ataObject.GetComponent<ButtonManager>();
                ataButton.buttonText = mip.mailItem.attachments[i].attachmentTitle;
                ataButton.UpdateUI();

                MailAttachmentPreset map = ataButton.gameObject.GetComponent<MailAttachmentPreset>();

                if (mip.mailItem.attachments[index].attachmentType == MailItem.Attachment.Music)
                {
                    map.musicIcon.SetActive(true);
                    ataButton.onClick.AddListener(delegate 
                    {
                        musicManager.gameObject.GetComponent<WindowManager>().OpenWindow();
                        musicManager.PlayCustomClip(mip.mailItem.attachments[index].musicAttachment, musicManager.libraryPlaylist.coverImage, mip.mailItem.attachments[index].attachmentTitle, mip.mailItem.fromName);
                    });
                }

                else if (mip.mailItem.attachments[index].attachmentType == MailItem.Attachment.Note)
                {
                    map.noteIcon.SetActive(true);
                    ataButton.onClick.AddListener(delegate
                    {
                        noteManager.gameObject.GetComponent<WindowManager>().OpenWindow();
                        noteManager.OpenCustomNote(mip.mailItem.attachments[index].attachmentTitle, mip.mailItem.attachments[index].noteAttachment);
                    });
                }

                else if (mip.mailItem.attachments[index].attachmentType == MailItem.Attachment.Picture)
                {
                    map.pictureIcon.SetActive(true);
                    ataButton.onClick.AddListener(delegate
                    {
                        pictureManager.gameObject.GetComponent<WindowManager>().OpenWindow();
                        pictureManager.OpenPhoto(mip.mailItem.attachments[index].pictureAttachment, mip.mailItem.attachments[index].attachmentTitle, mip.mailItem.fromName);
                    });
                }

                else if (mip.mailItem.attachments[index].attachmentType == MailItem.Attachment.Video)
                {
                    map.videoIcon.SetActive(true);
                    ataButton.onClick.AddListener(delegate
                    {
                        videoManager.gameObject.GetComponent<WindowManager>().OpenWindow();
                        videoManager.OpenVideo(mip.mailItem.attachments[index].videoAttachment, mip.mailItem.attachments[index].attachmentTitle);
                    });
                }
            }

            if (attachmentPanel != null && mip.mailItem.attachments.Count == 0) { attachmentPanel.gameObject.SetActive(false); attachmentPanel.InstantMinimized(); }
            else if (attachmentPanel != null) { attachmentPanel.OpenPanel(); }

            StopCoroutine("DestroyViewerAnimator");
            StartCoroutine("DestroyViewerAnimator");
        }

        IEnumerator DestroyViewerAnimator()
        {
            yield return new WaitForSeconds(cachedTemplateLength);
            currentMailPreset.animator.enabled = false;
        }
    }
}