using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Michsky.DreamOS
{
    public class NotepadManager : MonoBehaviour
    {
        // Content
        public List<NoteItem> noteItems = new List<NoteItem>();
        NotepadPreset currentPreset;

        // Resources
        [SerializeField] private Transform noteLibraryParent;
        [SerializeField] private GameObject noteLibraryPreset;
        public WindowManager windowManager;
        [SerializeField] private Animator viewerAnimator;
        [SerializeField] private TMP_InputField viewerTitle;
        [SerializeField] private TMP_InputField viewerContent;
        [SerializeField] private ButtonManager deleteButton;
        public NotepadStoring notepadStoring;

        // Settings
        public bool saveCustomNotes = false;
        public bool openNoteOnEnable = true;
        public bool useLocalization = true;

        // Helpers
        bool bypassUpdate = false;
        float cachedTemplateLength = 0.5f;

        [System.Serializable]
        public class NoteItem
        {
            public string noteID;
            public string noteTitle = "Title";
            [TextArea(3, 6)] public string noteContent = "Content";
            [HideInInspector] public bool isCustom = false;
            [HideInInspector] public bool isRemoved = false;
            [HideInInspector] public NotepadPreset preset;

            [Header("Localization")]
            public string titleKey;
            public string contentKey;
        }

        void Start()
        {
            Initialize();
        }

        void OnEnable()
        {
            if (openNoteOnEnable && noteItems.Count > 0)
            {
                for (int i = 0; i < noteItems.Count; i++)
                {
                    if (!noteItems[i].isRemoved)
                    {
                        OpenNote(i);
                        break;
                    }
                }
            }
        }

        public void Initialize()
        {
            // Cache the anim state length
            if (viewerAnimator != null) { cachedTemplateLength = DreamOSInternalTools.GetAnimatorClipLength(viewerAnimator, "NoteViewer_In") + 0.1f; }

            // Delete every note in library parent
            foreach (Transform child in noteLibraryParent) { Destroy(child.gameObject); }

            // Instantiate the entire note library as buttons
            for (int i = 0; i < noteItems.Count; ++i) 
            {
                if (noteItems[i].isRemoved)
                    continue;

                // Spawn note preset
                GameObject go = Instantiate(noteLibraryPreset, new Vector3(0, 0, 0), Quaternion.identity);
                go.transform.SetParent(noteLibraryParent, false);
                go.gameObject.name = noteItems[i].noteTitle;

                // Check for localization
                LocalizedObject tempLoc = gameObject.GetComponent<LocalizedObject>();
                if (!noteItems[i].isCustom && useLocalization && !string.IsNullOrEmpty(noteItems[i].titleKey) && tempLoc != null && tempLoc.CheckLocalizationStatus())
                {
                    noteItems[i].noteTitle = tempLoc.GetKeyOutput(noteItems[i].titleKey);
                    noteItems[i].noteContent = tempLoc.GetKeyOutput(noteItems[i].contentKey);
                }

                // Get and set the preset component
                NotepadPreset preset = go.GetComponent<NotepadPreset>();
                preset.noteID = noteItems[i].noteID;
                preset.noteIndex = i;
                preset.manager = this;
                preset.titleText.text = noteItems[i].noteTitle;
                noteItems[i].preset = preset;

                // Add button events
                ButtonManager itemButton = go.GetComponent<ButtonManager>();
                itemButton.onClick.AddListener(delegate { OpenNote(preset.noteIndex); });

                // Set currentPreset
                if (currentPreset == null) { currentPreset = preset; }
            }

            // Check for saved custom notes
            if (notepadStoring != null) { notepadStoring.ReadNoteData(); }

            // Set TMP events
            viewerTitle.onEndEdit.RemoveAllListeners();
            viewerContent.onEndEdit.RemoveAllListeners();
            viewerTitle.onEndEdit.AddListener(delegate { UpdateNote(currentPreset.noteIndex); });
            viewerContent.onEndEdit.AddListener(delegate { UpdateNote(currentPreset.noteIndex); });
        }

        public void CreateNote(string noteID, string title, string content, bool isCustom = true)
        {
            // Create list entry
            NoteItem tempItem = new NoteItem();
            tempItem.noteID = noteID;
            tempItem.noteTitle = title;
            tempItem.noteContent = content;
            tempItem.isCustom = isCustom;
            noteItems.Add(tempItem);

            // Get the list index
            int listIndex = noteItems.Count - 1;

            // Spawn note preset
            GameObject go = Instantiate(noteLibraryPreset, new Vector3(0, 0, 0), Quaternion.identity);
            go.transform.SetParent(noteLibraryParent, false);
            go.gameObject.name = noteItems[listIndex].noteTitle;

            // Get and set the preset component
            NotepadPreset preset = go.GetComponent<NotepadPreset>();
            preset.noteID = noteID;
            preset.noteIndex = listIndex;
            preset.manager = this;
            preset.titleText.text = title;
            preset.isCustom = isCustom;
            tempItem.preset = preset;

            // Add button events
            ButtonManager itemButton;
            itemButton = go.GetComponent<ButtonManager>();
            itemButton.onClick.AddListener(delegate { OpenNote(preset.noteIndex); });
        }

        public void CreateEmptyNote()
        {
            CreateNote("UserNote#" + (noteItems.Count - 1).ToString(), "My new note", null);
            if (gameObject.activeInHierarchy) { OpenNote(noteItems.Count - 1); }
        }

        public void DeleteNote(int index)
        {
            noteItems[index].isRemoved = true;

            if (noteItems[index].preset == null)
                return;

            Destroy(noteItems[index].preset.gameObject);
            HideViewer();
        }

        public void DeleteNote(string noteID)
        {
            for (int i = 0; i < noteItems.Count; i++)
            {
                if (noteItems[i].noteID == noteID)
                {
                    DeleteNote(noteItems[i].preset.noteIndex);
                    break;
                }
            }
        }

        public void UpdateNote(int index)
        {
            if (bypassUpdate || currentPreset == null)
                return;

            // Update the content
            noteItems[index].noteTitle = viewerTitle.text;
            noteItems[index].noteContent = viewerContent.text;
            noteItems[index].preset.titleText.text = viewerTitle.text;

            // Check for notepad storing and update the data
            if (notepadStoring != null && noteItems[index].isCustom) { notepadStoring.UpdateData(); }
        }

        public void UpdateNote(string noteID)
        {
            for (int i = 0; i < noteItems.Count; i++)
            {
                if (noteItems[i].noteID == noteID)
                {
                    UpdateNote(i);
                    break;
                }
            }
        }

        public void OpenNote(int index)
        {
            // Set the content
            currentPreset = noteItems[index].preset;
            viewerTitle.text = noteItems[index].noteTitle;
            viewerContent.text = noteItems[index].noteContent;

            // Set button parameters
            deleteButton.Interactable(true);
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(delegate { DeleteNote(currentPreset.noteIndex); });

            // Allow updating and show the viewer
            bypassUpdate = false;
            ShowViewer();
        }

        public void OpenNote(string noteID)
        {
            for (int i = 0; i < noteItems.Count; i++)
            {
                if (noteItems[i].noteID == noteID)
                {
                    OpenNote(i);
                    break;
                }
            }
        }

        public void OpenCustomNote(string title, string note)
        {
            // Set the content
            viewerTitle.text = title;
            viewerContent.text = note;

            // Set button parameters
            deleteButton.Interactable(false);

            // Bypass updating and show the viewer
            bypassUpdate = true;
            ShowViewer();
        }

        public void ShowViewer()
        {
            viewerAnimator.enabled = true;
            viewerAnimator.Play("In");

            StopCoroutine("DisableViewerAnimator");
            StartCoroutine("DisableViewerAnimator");
        }

        public void HideViewer()
        {
            viewerAnimator.enabled = true;
            viewerAnimator.Play("Out");

            StopCoroutine("DisableViewerAnimator");
            StartCoroutine("DisableViewerAnimator");
        }

        IEnumerator DisableViewerAnimator()
        {
            yield return new WaitForSeconds(cachedTemplateLength);
            viewerAnimator.enabled = false;
        }
    }
}