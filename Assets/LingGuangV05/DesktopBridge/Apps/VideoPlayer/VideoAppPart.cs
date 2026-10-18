using Michsky.DreamOS;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>
    /// One text, image, slider or button on the video player's "Now Playing" panel or mini player (replaces DreamOS's
    /// VideoPlayerDataItem; same GUID and fields). <see cref="objectType"/> says which: texts and the cover follow the
    /// playing video, buttons drive the player, and the loop setting is remembered between sessions.
    /// </summary>
    [DisallowMultipleComponent]
    public class VideoAppPart : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        // The order is serialized: do not reorder.
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

        const string LoopKey = "VideoPlayer_Loop";
        const DreamOSDataManager.DataCategory Category = DreamOSDataManager.DataCategory.Apps;

        [SerializeField] VideoApp playerManager;
        [Tooltip("Refresh every frame instead of only when the video changes (the time text and slider).")]
        public bool alwaysUpdate;
        [SerializeField] ObjectType objectType;

        TextMeshProUGUI text;
        Image image;
        Slider slider;
        AnimatedIconHandler icon;
        // False while the time slider is held, so the drag seeks instead of the video moving the handle.
        bool sliderFollows = true;

        void Start() => Initalize();

        void OnEnable() => UpdateItem();

        void Update()
        {
            if (alwaysUpdate) UpdateItem();
        }

        public void Initalize()
        {
            if (playerManager == null)
                foreach (var app in Resources.FindObjectsOfTypeAll<VideoApp>())
                    if (app.gameObject.scene.name != null) { playerManager = app; break; }
            if (playerManager == null) return;

            switch (objectType)
            {
                case ObjectType.Title:
                case ObjectType.Description:
                case ObjectType.CurrentTime:
                case ObjectType.Duration:
                    text = GetComponent<TextMeshProUGUI>();
                    break;
                case ObjectType.Cover:
                    image = GetComponent<Image>();
                    break;
                case ObjectType.VideoSlider:
                    slider = GetComponent<Slider>();
                    break;
                case ObjectType.PlayButton:
                case ObjectType.PauseButton:
                    icon = GetComponent<AnimatedIconHandler>();
                    OnClick(() => { if (playerManager.videoPlayer.isPlaying) playerManager.Pause(); else playerManager.Play(); });
                    break;
                case ObjectType.SeekForward:
                    OnClick(playerManager.SeekForward);
                    break;
                case ObjectType.SeekBackward:
                    OnClick(playerManager.SeekBackward);
                    break;
                case ObjectType.Loop:
                    SetUpLoop();
                    break;
                case ObjectType.VolumeSlider:
                    slider = GetComponent<Slider>();
                    slider.onValueChanged.AddListener(SetVolume);
                    if (playerManager.audioSource != null) playerManager.audioSource.volume = slider.value;
                    break;
            }

            playerManager.dataToBeUpdated.Add(this);
            UpdateItem();
        }

        void OnClick(UnityEngine.Events.UnityAction action) => GetComponent<ButtonManager>().onClick.AddListener(action);

        void SetUpLoop()
        {
            icon = GetComponent<AnimatedIconHandler>();
            var player = playerManager.videoPlayer;
            OnClick(() =>
            {
                bool on = !player.isLooping;
                DreamOSDataManager.WriteBooleanData(Category, LoopKey, on);
                player.isLooping = on;
                if (on) icon.PlayIn(); else icon.PlayOut();
            });

            if (!DreamOSDataManager.ContainsJsonKey(Category, LoopKey)) DreamOSDataManager.WriteBooleanData(Category, LoopKey, playerManager.loop);
            else playerManager.loop = DreamOSDataManager.ReadBooleanData(Category, LoopKey);
            player.isLooping = playerManager.loop;
            if (player.isLooping) icon.PlayIn(); else icon.PlayOut();
        }

        public void UpdateItem()
        {
            if (playerManager == null) return;
            switch (objectType)
            {
                case ObjectType.Title when text != null:
                    // A video from outside the library shows its own title; the library's otherwise.
                    if (!string.IsNullOrEmpty(playerManager.tempVideoTitle)) text.text = playerManager.tempVideoTitle;
                    else if (playerManager.Current != null) text.text = playerManager.Current.title;
                    break;
                case ObjectType.Cover when image != null:
                    if (playerManager.Current != null) image.sprite = playerManager.Current.cover;
                    break;
                case ObjectType.CurrentTime when text != null:
                    text.text = string.Format("{0:00}:{1:00}", playerManager.minutesPassed, playerManager.secondsPassed);
                    break;
                case ObjectType.Duration when text != null:
                    text.text = string.Format("{0:00}:{1:00}", playerManager.totalMinutes, playerManager.totalSeconds);
                    break;
                case ObjectType.VideoSlider when slider != null:
                    MoveSlider();
                    break;
                case ObjectType.PlayButton when icon != null:
                case ObjectType.PauseButton when icon != null:
                    if (playerManager.videoPlayer.isPlaying) icon.PlayOut(); else icon.PlayIn();
                    break;
            }
        }

        void MoveSlider()
        {
            var player = playerManager.videoPlayer;
            if (sliderFollows)
            {
                slider.maxValue = (float)player.length;
                slider.value = (float)player.time;
            }
            else if (slider.value < playerManager.duration) player.time = slider.value;
        }

        public void SetVolume(float volume) => playerManager.audioSource.volume = slider.value;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (objectType == ObjectType.VideoSlider) sliderFollows = false;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (objectType == ObjectType.VideoSlider) sliderFollows = true;
        }
    }
}
