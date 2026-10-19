using System.Collections.Generic;
using LingGuangV05.Desktop.OS;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>
    /// The desktop photo gallery (2026-10-19). Replaces DreamOS's PhotoGalleryManager: same GUID and serialized fields,
    /// so the Main scene's photos, grid and viewer carry over unchanged.
    ///
    /// The grid holds one thumbnail button per photo; clicking one opens the "Viewer" panel, where the arrow buttons or
    /// arrow keys step through the list. Mail, chat and browser images open in the viewer through
    /// <see cref="OpenPhoto(Sprite,string,string)"/> without arrows. In the game the window is hidden by MainSceneEntry.
    /// </summary>
    public class PhotoApp : MonoBehaviour
    {
        [System.Serializable]
        public class PhotoItem
        {
            public string title = "Title";
            public string description = "Description";
            public Sprite photo;
            [HideInInspector] public bool isCustom;
            [HideInInspector] public PhotoListEntry preset;
        }

        public List<PhotoItem> photoItems = new List<PhotoItem>();

        [SerializeField] GameObject pictureLibraryPreset;
        [SerializeField] Transform pictureLibraryParent;
        [SerializeField] Image imageViewer;
        [SerializeField] TextMeshProUGUI viewerTitle;
        [SerializeField] TextMeshProUGUI viewerDescription;
        [SerializeField] ButtonManager nextButton;
        [SerializeField] ButtonManager previousButton;
        [SerializeField] WindowPanelManager panelManager;

        public bool allowArrowNavigation = true;
        public string viewerPanelName = "Viewer";

        PhotoListEntry shown;
        // A photo opened from outside the list has no neighbours to step to.
        bool outsideList;

        void Awake() => Initialize();

        void Update()
        {
            if (!allowArrowNavigation || shown == null) return;
            if (panelManager.panels[panelManager.currentPanelIndex].panelName != viewerPanelName) return;
            var keys = Keyboard.current;
            if (keys == null) return;
            if (keys.leftArrowKey.wasPressedThisFrame && shown.photoIndex > 0) Step(-1);
            else if (keys.rightArrowKey.wasPressedThisFrame && shown.photoIndex < photoItems.Count - 1) Step(1);
        }

        public void Initialize()
        {
            foreach (Transform child in pictureLibraryParent) Destroy(child.gameObject);

            for (int i = 0; i < photoItems.Count; i++)
            {
                var item = photoItems[i];
                var entry = AddEntry(item.photo, item.title, item.description);
                entry.photoIndex = i;
                item.preset = entry;
                entry.GetComponent<ButtonManager>().onClick.AddListener(() => OpenPhoto(entry.photoIndex));
            }

            if (nextButton != null && previousButton != null)
            {
                nextButton.onClick.RemoveAllListeners();
                previousButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(() => Step(1));
                previousButton.onClick.AddListener(() => Step(-1));
            }
        }

        PhotoListEntry AddEntry(Sprite photo, string title, string description)
        {
            var go = AppSpawn.Row(pictureLibraryPreset, pictureLibraryParent, title);
            var entry = go.GetComponent<PhotoListEntry>();
            entry.manager = this;
            entry.photoTitle = title;
            entry.titleText.text = title;
            entry.descriptionText.text = description;
            entry.photoImage.sprite = photo;
            // Portrait photos get a tall thumbnail, everything else a wide one.
            var tex = photo.texture;
            entry.aspectRatioFitter.aspectRatio = tex.height > tex.width ? .5f : 1.8f;
            return entry;
        }

        public void OpenPhoto(int index)
        {
            var item = photoItems[index];
            shown = item.preset;
            ShowInViewer(item.photo, item.title, item.description);
            outsideList = false;
            UpdateArrows();
            panelManager.OpenPanel(viewerPanelName);
        }

        public void OpenPhoto(string photoTitle)
        {
            int index = photoItems.FindIndex(p => p.title == photoTitle);
            if (index >= 0) OpenPhoto(index);
        }

        /// <summary>Shows an image that is not in the list (an attachment or a download).</summary>
        public void OpenPhoto(Sprite photo, string title, string description)
        {
            ShowInViewer(photo, title, description);
            panelManager.OpenPanel(viewerPanelName);
            outsideList = true;
            UpdateArrows();
        }

        void ShowInViewer(Sprite photo, string title, string description)
        {
            imageViewer.sprite = photo;
            viewerTitle.text = title;
            viewerDescription.text = description;
        }

        public void DeletePhoto(int index)
        {
            Destroy(photoItems[index].preset.gameObject);
            photoItems.RemoveAt(index);
            panelManager.OpenFirstPanel();
        }

        public void DeletePhoto(string photoTitle)
        {
            int index = photoItems.FindIndex(p => p.title == photoTitle);
            if (index >= 0) DeletePhoto(index);
        }

        /// <summary>Adds a thumbnail for an image that is not in <see cref="photoItems"/> (used by mods).</summary>
        public void CreatePhoto(Sprite photo, string title, string description)
        {
            var entry = AddEntry(photo, title, description);
            entry.GetComponent<ButtonManager>().onClick.AddListener(() => OpenPhoto(photo, title, description));
        }

        public void CheckForButtonStates() => UpdateArrows();

        void UpdateArrows()
        {
            if (nextButton == null || previousButton == null) return;
            bool arrows = !outsideList && photoItems.Count > 1;
            previousButton.gameObject.SetActive(arrows && shown.photoIndex > 0);
            nextButton.gameObject.SetActive(arrows && shown.photoIndex < photoItems.Count - 1);
        }

        // Steps through the grid's own buttons, so a step behaves exactly like clicking the neighbouring thumbnail.
        void Step(int delta) =>
            pictureLibraryParent.GetChild(shown.photoIndex + delta).GetComponent<ButtonManager>().onClick.Invoke();
    }
}
