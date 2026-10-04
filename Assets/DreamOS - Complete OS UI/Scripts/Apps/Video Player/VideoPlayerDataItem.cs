using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Michsky.DreamOS
{
    [DisallowMultipleComponent]
    [AddComponentMenu("DreamOS/Apps/Video Player/Video Player Data Item")]
    public class VideoPlayerDataItem : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Resources")]
        [SerializeField] private VideoPlayerManager playerManager;

        [Header("Settings")]
        public bool alwaysUpdate = false;
        [SerializeField] private ObjectType objectType;

        // Helpers
        TextMeshProUGUI textObj;
        Image imageObj;
        Slider sliderObj;
        ButtonManager btnObj;
        AnimatedIconHandler animHandler;
        bool enableSliderUpdate = true;
        DreamOSDataManager.DataCategory dataCat = DreamOSDataManager.DataCategory.Apps;

        public enum ObjectType
        {
            Title,
            Description,
            Cover,
            CurrentTime,
            Duration,
            VideoSlider,
            PlayButton,
            PauseButton,
            SeekForward,
            SeekBackward,
            Loop,
            VolumeSlider
        }

        void Start()
        {
            Initalize();
        }

        void OnEnable()
        {
            UpdateItem();
        }

        void Update()
        {
            if (!alwaysUpdate)
                return;

            UpdateItem();
        }

        public void Initalize()
        {
            // Try to find it if not assigned already
            if (playerManager == null)
            {
                try
                {
                    foreach (VideoPlayerManager vpm in Resources.FindObjectsOfTypeAll(typeof(VideoPlayerManager)) as VideoPlayerManager[])
                    {
                        if (vpm.gameObject.scene.name != null)
                        {
                            playerManager = vpm;
                            break;
                        }
                    }
                }

                catch { Debug.Log("<b>[Video Player Data Item]</b> Player Manager is missing.", this); return; }
            }

            // Don't go further if music player manager is not assigned
            if (playerManager == null)
                return;

            // Get and change value depending on the object type
            if (objectType == ObjectType.Title || objectType == ObjectType.Description || objectType == ObjectType.CurrentTime || objectType == ObjectType.Duration) { textObj = gameObject.GetComponent<TextMeshProUGUI>(); }
            else if (objectType == ObjectType.Cover) { imageObj = gameObject.GetComponent<Image>(); }
            else if (objectType == ObjectType.VideoSlider) { sliderObj = gameObject.GetComponent<Slider>(); }
            else if (objectType == ObjectType.PlayButton || objectType == ObjectType.PauseButton) { InitializePlayAndPauseButton(); }
            else if (objectType == ObjectType.SeekForward) { InitializeSeekForwardButton(); }
            else if (objectType == ObjectType.SeekBackward) { InitializeSeekBackwardButton(); }
            else if (objectType == ObjectType.Loop) { InitializeLoopButton(); }
            else if (objectType == ObjectType.VolumeSlider) { InitializeVolumeSlider(); }

            // Add this component to the to be updated list
            playerManager.dataToBeUpdated.Add(this);

            // Manually update the item
            UpdateItem();
        }

        public void UpdateItem()
        {
            if (playerManager == null)
                return;

            // Get and change value depending on the object type
            if (objectType == ObjectType.Title && textObj != null) { CheckForTitle(); }
            else if (objectType == ObjectType.Cover && imageObj != null) { imageObj.sprite = playerManager.videoItems[playerManager.currentClipIndex].cover; }
            else if (objectType == ObjectType.CurrentTime && textObj != null) { textObj.text = string.Format("{0:00}:{1:00}", playerManager.minutesPassed, playerManager.secondsPassed); }
            else if (objectType == ObjectType.Duration && textObj != null) { textObj.text = string.Format("{0:00}:{1:00}", playerManager.totalMinutes, playerManager.totalSeconds); }
            else if (objectType == ObjectType.VideoSlider && sliderObj != null) { MoveVideoSlider(); }
            else if ((objectType == ObjectType.PlayButton || objectType == ObjectType.PauseButton) && animHandler != null)
            {
                if (playerManager.videoPlayer.isPlaying) { animHandler.PlayOut(); }
                else { animHandler.PlayIn(); }
            }
        }

        void CheckForTitle()
        {
            if (string.IsNullOrEmpty(playerManager.tempVideoTitle)) { textObj.text = playerManager.videoItems[playerManager.currentClipIndex].title; }
            else { textObj.text = playerManager.tempVideoTitle; }
        }

        void MoveVideoSlider()
        {
            if (enableSliderUpdate)
            {
                sliderObj.maxValue = (float)playerManager.videoPlayer.length;
                sliderObj.value = (float)playerManager.videoPlayer.time;
            }
            else if (!enableSliderUpdate && sliderObj.value < playerManager.duration) { playerManager.videoPlayer.time = sliderObj.value; }
        }

        void InitializeVolumeSlider()
        {
            sliderObj = gameObject.GetComponent<Slider>();
            sliderObj.onValueChanged.AddListener(SetVolume);
            if (playerManager.audioSource != null) { playerManager.audioSource.volume = sliderObj.value; }
        }

        void InitializeLoopButton()
        {
            btnObj = gameObject.GetComponent<ButtonManager>();
            animHandler = gameObject.GetComponent<AnimatedIconHandler>();
            btnObj.onClick.AddListener(delegate
            {
                if (playerManager.videoPlayer.isLooping)
                {
                    DreamOSDataManager.WriteBooleanData(dataCat, "VideoPlayer_Loop", false);
                    playerManager.videoPlayer.isLooping = false;
                    animHandler.PlayOut();
                }

                else
                {
                    DreamOSDataManager.WriteBooleanData(dataCat, "VideoPlayer_Loop", true);
                    playerManager.videoPlayer.isLooping = true;
                    animHandler.PlayIn();
                }
            });

            if (!DreamOSDataManager.ContainsJsonKey(dataCat, "VideoPlayer_Loop") && playerManager.loop) { DreamOSDataManager.WriteBooleanData(dataCat, "VideoPlayer_Loop", true); }
            else if (!DreamOSDataManager.ContainsJsonKey(dataCat, "VideoPlayer_Loop") && !playerManager.loop) { DreamOSDataManager.WriteBooleanData(dataCat, "VideoPlayer_Loop", false); }
            else if (DreamOSDataManager.ReadBooleanData(dataCat, "VideoPlayer_Loop")) { playerManager.loop = true; }
            else if (!DreamOSDataManager.ReadBooleanData(dataCat, "VideoPlayer_Loop")) { playerManager.loop = false; }

            playerManager.videoPlayer.isLooping = playerManager.loop;

            if (playerManager.videoPlayer.isLooping) { animHandler.PlayIn(); }
            else { animHandler.PlayOut(); }
        }

        void InitializeSeekBackwardButton()
        {
            btnObj = gameObject.GetComponent<ButtonManager>();
            btnObj.onClick.AddListener(delegate { playerManager.SeekBackward(); });
        }

        void InitializeSeekForwardButton()
        {
            btnObj = gameObject.GetComponent<ButtonManager>();
            btnObj.onClick.AddListener(delegate { playerManager.SeekForward(); });
        }

        void InitializePlayAndPauseButton()
        {
            btnObj = gameObject.GetComponent<ButtonManager>();
            animHandler = gameObject.GetComponent<AnimatedIconHandler>();
            btnObj.onClick.AddListener(delegate
            {
                if (playerManager.videoPlayer.isPlaying) { playerManager.Pause(); }
                else { playerManager.Play(); }
            });
        }

        public void SetVolume(float volume)
        {
            playerManager.audioSource.volume = sliderObj.value;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (objectType == ObjectType.VideoSlider)
                enableSliderUpdate = false;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (objectType == ObjectType.VideoSlider)
                enableSliderUpdate = true;
        }
    }
}