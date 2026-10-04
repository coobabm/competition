using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace Michsky.DreamOS
{
    public class PhotoGalleryManager : MonoBehaviour
    {
        // Content
        public List<PhotoItem> photoItems = new List<PhotoItem>();
        PhotoGalleryPreset currentPreset;

        // Resources
        [SerializeField] private GameObject pictureLibraryPreset;
        [SerializeField] private Transform pictureLibraryParent;
        [SerializeField] private Image imageViewer;
        [SerializeField] private TextMeshProUGUI viewerTitle;
        [SerializeField] private TextMeshProUGUI viewerDescription;
        [SerializeField] private ButtonManager nextButton;
        [SerializeField] private ButtonManager previousButton;
        [SerializeField] private WindowPanelManager panelManager;

        // Settings
        public bool allowArrowNavigation = true;
        public string viewerPanelName = "Viewer";

        // Helpers
        bool bypassArrowKeys = false;

        [System.Serializable]
        public class PhotoItem
        {
            public string title = "Title";
            public string description = "Description";
            public Sprite photo;
            [HideInInspector] public bool isCustom = false;
            [HideInInspector] public PhotoGalleryPreset preset;
        }

        void Awake()
        {
            Initialize();
        }

        void Update()
        {
            if (!allowArrowNavigation || panelManager.panels[panelManager.currentPanelIndex].panelName != viewerPanelName || currentPreset == null) { return; }
            if (Keyboard.current.leftArrowKey.wasPressedThisFrame && currentPreset.photoIndex > 0) { PrevAction(); }
            else if (Keyboard.current.rightArrowKey.wasPressedThisFrame && currentPreset.photoIndex < photoItems.Count - 1) { NextAction(); }
        }

        public void Initialize()
        {
            // Destroy each object in picture library parent before creating the new ones
            foreach (Transform child in pictureLibraryParent) { Destroy(child.gameObject); }

            // Instantiate the entire picture library as buttons
            for (int i = 0; i < photoItems.Count; ++i)
            {
                // Spawn picture button
                GameObject go = Instantiate(pictureLibraryPreset, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
                go.transform.SetParent(pictureLibraryParent, false);
                go.gameObject.name = photoItems[i].title;

                // Get and set the preset component
                PhotoGalleryPreset preset = go.GetComponent<PhotoGalleryPreset>();
                preset.photoIndex = i;
                preset.manager = this;
                preset.photoTitle = photoItems[i].title;
                preset.titleText.text = photoItems[i].title;
                preset.descriptionText.text = photoItems[i].description;
                preset.photoImage.sprite = photoItems[i].photo;
                photoItems[i].preset = preset;

                // Fit picture to the box depending on its width and height
                if (preset.photoImage.sprite.texture.height > preset.photoImage.sprite.texture.width) { preset.aspectRatioFitter.aspectRatio = 0.5f; }
                else { preset.aspectRatioFitter.aspectRatio = 1.8f; }

                // Add button events
                ButtonManager itemButton = go.GetComponent<ButtonManager>();
                itemButton.onClick.AddListener(delegate { OpenPhoto(preset.photoIndex); });
            }

            // Set button events 
            if (nextButton != null && previousButton != null)
            {
                nextButton.onClick.RemoveAllListeners();
                previousButton.onClick.RemoveAllListeners();

                nextButton.onClick.AddListener(NextAction);
                previousButton.onClick.AddListener(PrevAction);
            }
        }

        public void OpenPhoto(int index)
        {
            // Set the current preset
            currentPreset = photoItems[index].preset;

            // Update viewer
            imageViewer.sprite = photoItems[index].photo;
            viewerTitle.text = photoItems[index].title;
            viewerDescription.text = photoItems[index].description;

            // Enable arrow keys as we're opening up a custom photo
            bypassArrowKeys = false;

            // Check for arrow buttons
            CheckForButtonStates();

            // Open the viewver panel
            panelManager.OpenPanel(viewerPanelName);
        }

        public void OpenPhoto(string photoTitle)
        {
            for (int i = 0; i < photoItems.Count; i++)
            {
                if (photoItems[i].title == photoTitle)
                {
                    OpenPhoto(i);
                    break;
                }
            }
        }

        public void OpenPhoto(Sprite photo, string title, string description)
        {
            // Update viewer
            imageViewer.sprite = photo;
            viewerTitle.text = title;
            viewerDescription.text = description;

            // Open the viewver panel
            panelManager.OpenPanel(viewerPanelName);

            // Bypass arrow keys as we're opening up a custom photo
            bypassArrowKeys = true;

            // Check for arrow buttons
            CheckForButtonStates();
        }

        public void DeletePhoto(int index)
        {
            Destroy(photoItems[index].preset.gameObject);
            photoItems.RemoveAt(index);
            panelManager.OpenFirstPanel();
        }

        public void DeletePhoto(string photoTitle)
        {
            for (int i = 0; i < photoItems.Count; i++)
            {
                if (photoItems[i].title == photoTitle)
                {
                    DeletePhoto(i);
                    break;
                }
            }
        }

        public void CreatePhoto(Sprite photo, string title, string description)
        {
            // Spawn picture button
            GameObject go = Instantiate(pictureLibraryPreset, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
            go.transform.SetParent(pictureLibraryParent, false);
            go.gameObject.name = title;

            // Get and set the preset component
            PhotoGalleryPreset preset = go.GetComponent<PhotoGalleryPreset>();
            preset.manager = this;
            preset.photoTitle = title;
            preset.titleText.text = title;
            preset.descriptionText.text = description;
            preset.photoImage.sprite = photo;

            // Fit picture to the box depending on its width and height
            if (preset.photoImage.sprite.texture.height > preset.photoImage.sprite.texture.width) { preset.aspectRatioFitter.aspectRatio = 0.5f; }
            else { preset.aspectRatioFitter.aspectRatio = 1.8f; }

            // Add button events
            ButtonManager itemButton = go.GetComponent<ButtonManager>();
            itemButton.onClick.AddListener(delegate { OpenPhoto(photo, title, description); });
        }

        public void CheckForButtonStates()
        {
            if (nextButton == null || previousButton == null) { return; }
            else if (bypassArrowKeys) { nextButton.gameObject.SetActive(false); previousButton.gameObject.SetActive(false); return; }
            else if (!bypassArrowKeys) { nextButton.gameObject.SetActive(false); previousButton.gameObject.SetActive(false); }

            if (photoItems.Count == 1)
            {
                nextButton.gameObject.SetActive(false);
                previousButton.gameObject.SetActive(false);
                return;
            }

            if (currentPreset.photoIndex == 0) { previousButton.gameObject.SetActive(false); }
            else { previousButton.gameObject.SetActive(true); }

            if (currentPreset.photoIndex == photoItems.Count - 1) { nextButton.gameObject.SetActive(false); }
            else { nextButton.gameObject.SetActive(true); }
        }

        void NextAction() { pictureLibraryParent.GetChild(currentPreset.photoIndex + 1).GetComponent<ButtonManager>().onClick.Invoke(); }
        void PrevAction() { pictureLibraryParent.GetChild(currentPreset.photoIndex - 1).GetComponent<ButtonManager>().onClick.Invoke(); }
    }
}