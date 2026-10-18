using System.Collections;
using System.Collections.Generic;
using Michsky.DreamOS;
using TMPro;
using UnityEngine;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>
    /// The desktop Notepad (2026-10-19). Replaces DreamOS's NotepadManager: it keeps that script's GUID and serialized
    /// field names, so the Main scene's notes, list, viewer and the "new note" button carry over unchanged.
    ///
    /// A list on the left holds one button per note; clicking one shows it in the viewer on the right, where the title
    /// and text can be edited. Mail and browser attachments open read-only through <see cref="OpenCustomNote"/>.
    /// </summary>
    public class NoteApp : MonoBehaviour
    {
        [System.Serializable]
        public class NoteItem
        {
            public string noteID;
            public string noteTitle = "Title";
            [TextArea(3, 6)] public string noteContent = "Content";
            [HideInInspector] public bool isCustom;
            [HideInInspector] public bool isRemoved;
            [HideInInspector] public NoteListEntry preset;

            [Header("Localization")]
            public string titleKey;
            public string contentKey;
        }

        public List<NoteItem> noteItems = new List<NoteItem>();

        [SerializeField] Transform noteLibraryParent;
        [SerializeField] GameObject noteLibraryPreset;
        public WindowManager windowManager;
        [SerializeField] Animator viewerAnimator;
        [SerializeField] TMP_InputField viewerTitle;
        [SerializeField] TMP_InputField viewerContent;
        [SerializeField] ButtonManager deleteButton;

        [Tooltip("Show the first note whenever the window opens.")]
        public bool openNoteOnEnable = true;
        [Tooltip("Built-in notes take their title and text from the window's LocalizedObject keys.")]
        public bool useLocalization = true;

        NoteListEntry shown;
        bool readOnly;
        float viewerAnimLength = .5f;
        Coroutine viewerAnim;

        void Start() => Initialize();

        void OnEnable()
        {
            if (!openNoteOnEnable) return;
            int first = noteItems.FindIndex(n => !n.isRemoved);
            if (first >= 0) OpenNote(first);
        }

        public void Initialize()
        {
            if (viewerAnimator != null)
                viewerAnimLength = DreamOSInternalTools.GetAnimatorClipLength(viewerAnimator, "NoteViewer_In") + .1f;

            foreach (Transform child in noteLibraryParent) Destroy(child.gameObject);

            var loc = GetComponent<LocalizedObject>();
            bool localize = useLocalization && loc != null && loc.CheckLocalizationStatus();
            for (int i = 0; i < noteItems.Count; i++)
            {
                var note = noteItems[i];
                if (note.isRemoved) continue;
                if (localize && !note.isCustom && !string.IsNullOrEmpty(note.titleKey))
                {
                    note.noteTitle = loc.GetKeyOutput(note.titleKey);
                    note.noteContent = loc.GetKeyOutput(note.contentKey);
                }
                var entry = AddEntry(note, i);
                if (shown == null) shown = entry;
            }

            viewerTitle.onEndEdit.RemoveAllListeners();
            viewerContent.onEndEdit.RemoveAllListeners();
            viewerTitle.onEndEdit.AddListener(_ => UpdateNote(shown.noteIndex));
            viewerContent.onEndEdit.AddListener(_ => UpdateNote(shown.noteIndex));
        }

        NoteListEntry AddEntry(NoteItem note, int index)
        {
            var go = AppSpawn.Row(noteLibraryPreset, noteLibraryParent, note.noteTitle);
            var entry = go.GetComponent<NoteListEntry>();
            entry.manager = this;
            entry.noteIndex = index;
            entry.noteID = note.noteID;
            entry.isCustom = note.isCustom;
            entry.titleText.text = note.noteTitle;
            note.preset = entry;
            go.GetComponent<ButtonManager>().onClick.AddListener(() => OpenNote(entry.noteIndex));
            return entry;
        }

        public void CreateNote(string noteID, string title, string content, bool isCustom = true)
        {
            var note = new NoteItem { noteID = noteID, noteTitle = title, noteContent = content, isCustom = isCustom };
            noteItems.Add(note);
            AddEntry(note, noteItems.Count - 1);
        }

        /// <summary>The list's "+" button.</summary>
        public void CreateEmptyNote()
        {
            CreateNote("UserNote#" + (noteItems.Count - 1), "My new note", null);
            if (gameObject.activeInHierarchy) OpenNote(noteItems.Count - 1);
        }

        public void DeleteNote(int index)
        {
            var note = noteItems[index];
            note.isRemoved = true;
            if (note.preset == null) return;
            Destroy(note.preset.gameObject);
            HideViewer();
        }

        public void DeleteNote(string noteID)
        {
            var note = noteItems.Find(n => n.noteID == noteID);
            if (note != null) DeleteNote(note.preset.noteIndex);
        }

        public void UpdateNote(int index)
        {
            if (readOnly || shown == null) return;
            var note = noteItems[index];
            note.noteTitle = viewerTitle.text;
            note.noteContent = viewerContent.text;
            note.preset.titleText.text = viewerTitle.text;
        }

        public void UpdateNote(string noteID)
        {
            int index = noteItems.FindIndex(n => n.noteID == noteID);
            if (index >= 0) UpdateNote(index);
        }

        public void OpenNote(int index)
        {
            var note = noteItems[index];
            shown = note.preset;
            viewerTitle.text = note.noteTitle;
            viewerContent.text = note.noteContent;
            deleteButton.Interactable(true);
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(() => DeleteNote(shown.noteIndex));
            readOnly = false;
            ShowViewer();
        }

        public void OpenNote(string noteID)
        {
            int index = noteItems.FindIndex(n => n.noteID == noteID);
            if (index >= 0) OpenNote(index);
        }

        /// <summary>Shows a text that is not in the list (a mail or download attachment); it cannot be edited or deleted.</summary>
        public void OpenCustomNote(string title, string note)
        {
            viewerTitle.text = title;
            viewerContent.text = note;
            deleteButton.Interactable(false);
            readOnly = true;
            ShowViewer();
        }

        public void ShowViewer() => PlayViewer("In");

        public void HideViewer() => PlayViewer("Out");

        void PlayViewer(string state)
        {
            viewerAnimator.enabled = true;
            viewerAnimator.Play(state);
            if (viewerAnim != null) StopCoroutine(viewerAnim);
            viewerAnim = StartCoroutine(StopAnimatorLater());
        }

        // The animator is switched off between transitions so it does not hold the viewer's values every frame.
        IEnumerator StopAnimatorLater()
        {
            yield return new WaitForSeconds(viewerAnimLength);
            viewerAnimator.enabled = false;
            viewerAnim = null;
        }
    }
}
