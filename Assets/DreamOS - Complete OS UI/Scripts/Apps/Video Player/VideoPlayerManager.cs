using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.InputSystem;

namespace Michsky.DreamOS
{
    public class VideoPlayerManager : MonoBehaviour
    {
        // Content
        public List<VideoItem> videoItems = new List<VideoItem>();

        // Resources
        public VideoPlayer videoPlayer;
        public AudioSource audioSource;
        [SerializeField] private Transform videoParent;
        [SerializeField] private GameObject videoPreset;
        [SerializeField] private WindowPanelManager panelManager;
        [SerializeField] private Animator videoControls;
        [SerializeField] private Animator miniPlayer;

        // Settings
        [Range(1, 15)] public float hideControlsIn = 2.5f;
        [Range(1, 60)] public float seekTime = 10;
        [SerializeField] private string videoPanelName = "Now Playing";

        // Helpers
        public List<VideoPlayerDataItem> dataToBeUpdated = new List<VideoPlayerDataItem>();
        [HideInInspector] public int currentClipIndex;
        [HideInInspector] public int secondsPassed;
        [HideInInspector] public int minutesPassed;
        [HideInInspector] public int totalSeconds;
        [HideInInspector] public int totalMinutes;
        [HideInInspector] public bool loop;
        [HideInInspector] public string tempVideoTitle;
        [HideInInspector] public bool isDone;
        [HideInInspector] public bool isMiniPlayerEnabled;
        bool updateControlInput = false;
        bool isControlsVisible = false;
        float cachedMiniPlayerLength = 0.5f;
        float cachedVideoControlsLength = 0.5f;
        Vector3 lastMousePos;

        [System.Serializable]
        public class VideoItem
        {
            public string title = "Video Title";
            public string description = "Video Description";
            public Sprite cover;
            public VideoType type = VideoType.Clip;
            public VideoClip clip;
            public string URL;
            [HideInInspector] public VideoPreset preset;
        }

        public enum VideoType { Clip, URL }

        public double time { get { return videoPlayer.time; } }
        public ulong duration { get { return (ulong)(videoPlayer.frameCount / videoPlayer.frameRate); } }
        public double nTime { get { return time / duration; } }

        void Awake()
        {
            Initialize();
        }

        void OnDisable()
        {
            Pause();
        }

        public void Initialize()
        {
            // Set target audio source
            videoPlayer.SetTargetAudioSource(0, audioSource);

            // Cache the anim state length
            if (miniPlayer != null) { cachedMiniPlayerLength = DreamOSInternalTools.GetAnimatorClipLength(miniPlayer, "MiniPlayer_In") + 0.1f; }
            if (videoControls != null) { cachedVideoControlsLength = DreamOSInternalTools.GetAnimatorClipLength(videoControls, "VideoControls_In") + 0.1f; }

            // Delete every note in library parent
            foreach (Transform child in videoParent) { Destroy(child.gameObject); }

            // Instantiate the entire note library as buttons
            for (int i = 0; i < videoItems.Count; ++i)
            {
                // Spawn video preset
                GameObject go = Instantiate(videoPreset, new Vector3(0, 0, 0), Quaternion.identity);
                go.transform.SetParent(videoParent, false);
                go.gameObject.name = videoItems[i].title;

                // Get and set the preset component
                VideoPreset preset = go.GetComponent<VideoPreset>();
                preset.videoIndex = i;
                preset.manager = this;
                preset.type = videoItems[i].type;
                preset.coverImage.sprite = videoItems[i].cover;
                preset.titleText.text = videoItems[i].title;
                preset.descriptionText.text = videoItems[i].description;
                if (preset.type == VideoType.Clip) { preset.durationText.text = (((int)videoItems[i].clip.length / 60) % 60) + ":" + ((int)videoItems[i].clip.length % 60).ToString("D2"); }
                else { preset.videoURL = videoItems[i].URL; preset.durationText.text = "URL"; }
                videoItems[i].preset = preset;

                // Add button events
                ButtonManager itemButton = go.GetComponent<ButtonManager>();
                itemButton.onClick.AddListener(delegate
                {
                    if (preset.type == VideoType.Clip) { OpenVideo(preset.videoIndex); }
                    else if (preset.type == VideoType.URL) { OpenVideo(preset.videoURL, preset.videoIndex); }
                });
            }

            // Disable mini player
            if (miniPlayer != null) { miniPlayer.gameObject.SetActive(false); }
        }

        void Update()
        {
            if (videoPlayer.isPrepared)
            {
                totalMinutes = (int)duration / 60;
                totalSeconds = (int)duration - totalMinutes * 60;
                minutesPassed = (int)time / 60;
                secondsPassed = (int)time - minutesPassed * 60;
            }

            if (!updateControlInput) { return; }
            else if (lastMousePos == new Vector3(Mouse.current.position.ReadValue().x, Mouse.current.position.ReadValue().y, 0) && isControlsVisible) { HideVideoControls(hideControlsIn); }
            else if (lastMousePos != new Vector3(Mouse.current.position.ReadValue().x, Mouse.current.position.ReadValue().y, 0) && !isControlsVisible) { ShowVideoControls(); }

            lastMousePos = new Vector3(Mouse.current.position.ReadValue().x, Mouse.current.position.ReadValue().y, 0);
        }

        public void OpenVideo(int index)
        {
            currentClipIndex = index;
            tempVideoTitle = null;

            videoPlayer.Stop();
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = videoItems[index].clip;
            videoPlayer.time = 0;
            videoPlayer.Play();

            panelManager.OpenPanel(videoPanelName);

            HideMiniPlayer();
            UpdateDataItems();
        }

        public void OpenVideo(string url, int index = -1)
        {
            currentClipIndex = index;
            tempVideoTitle = null;

            videoPlayer.Stop();
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = url;
            videoPlayer.time = 0;
            videoPlayer.Play();

            panelManager.OpenPanel(videoPanelName);

            HideMiniPlayer();
            UpdateDataItems();
        }

        public void OpenVideo(VideoClip clip, string title)
        {
            currentClipIndex = -1;
            tempVideoTitle = title;

            videoPlayer.Stop();
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = clip;
            videoPlayer.time = 0;
            videoPlayer.Play();

            panelManager.OpenPanel(videoPanelName);

            HideMiniPlayer();
            UpdateDataItems();
        }

        public void CreateVideo(Sprite cover, string title, string desc, string url)
        {
            // Spawn video preset
            GameObject go = Instantiate(videoPreset, new Vector3(0, 0, 0), Quaternion.identity);
            go.transform.SetParent(videoParent, false);
            go.gameObject.name = title;

            // Get and set the preset component
            VideoPreset preset = go.GetComponent<VideoPreset>();
            preset.manager = this;
            preset.type = VideoType.URL;
            preset.coverImage.sprite = cover;
            preset.titleText.text = title;
            preset.descriptionText.text = desc;
            preset.videoURL = url;
            preset.durationText.text = "MOD";

            // Add button events
            ButtonManager itemButton = go.GetComponent<ButtonManager>();
            itemButton.onClick.AddListener(delegate { OpenVideo(preset.videoURL); });
        }

        public void Play()
        { 
            videoPlayer.Play();
            UpdateDataItems();
        }

        public void Pause() 
        {
            if (!gameObject.activeInHierarchy)
                return;

            videoPlayer.Pause();
            UpdateDataItems();
            StopCoroutine("FadeOutVideoControls");
        }

        public void SeekForward() 
        {
            videoPlayer.time += seekTime;
        }

        public void SeekBackward() 
        { 
            videoPlayer.time -= seekTime;
        }

        public void IncreasePlaybackSpeed()
        {
            if (!videoPlayer.canSetPlaybackSpeed)
                return;

            videoPlayer.playbackSpeed += 1;
            videoPlayer.playbackSpeed = Mathf.Clamp(videoPlayer.playbackSpeed, 0, 10);
        }

        public void DecreasePlaybackSpeed() 
        {
            if (!videoPlayer.canSetPlaybackSpeed)
                return;

            videoPlayer.playbackSpeed -= 1;
            videoPlayer.playbackSpeed = Mathf.Clamp(videoPlayer.playbackSpeed, 0, 10);
        }

        public void ShowVideoControls()
        {
            StopCoroutine("FadeOutVideoControls");
            StopCoroutine("DisableVideoControlsAnimator");
            StartCoroutine("DisableVideoControlsAnimator");

            Cursor.visible = true;
            isControlsVisible = true;
            videoControls.enabled = true;
            videoControls.CrossFade("In", 0.15f);
        }

        public void HideVideoControls(float time = 0.1f)
        {
            StopCoroutine("FadeOutVideoControls");
            StartCoroutine("FadeOutVideoControls", time);

            isControlsVisible = false;
        }

        public void ShowMiniPlayer()
        {
            if (miniPlayer != null && videoPlayer.isPlaying)
            {
                StopCoroutine("DisableMiniPlayerAnimator");
                StopCoroutine("DisableMiniPlayer");
                StartCoroutine("DisableMiniPlayerAnimator");

                miniPlayer.gameObject.SetActive(true);
                isMiniPlayerEnabled = true;
                miniPlayer.enabled = true;
                miniPlayer.Play("In");
            }

            else if (miniPlayer != null && !videoPlayer.isPlaying) { HideMiniPlayer(); }
        }

        public void HideMiniPlayer()
        {
            if (miniPlayer != null && isMiniPlayerEnabled)
            {
                StopCoroutine("DisableMiniPlayerAnimator");
                StopCoroutine("DisableMiniPlayer");
                StartCoroutine("DisableMiniPlayer");

                isMiniPlayerEnabled = false;
                miniPlayer.enabled = true;
                miniPlayer.Play("Out");
            }
        }

        public void UpdateControlInput(bool value)
        {
            updateControlInput = value;
            if (value == false) { HideVideoControls(); }
        }

        public void UpdateDataItems()
        {
            for (int i = 0; i < dataToBeUpdated.Count; ++i)
            {
                if (dataToBeUpdated[i] == null) { dataToBeUpdated.RemoveAt(i); continue; }
                else if (dataToBeUpdated[i].alwaysUpdate) { continue; }
                else if (dataToBeUpdated[i].gameObject.activeInHierarchy == true) { dataToBeUpdated[i].UpdateItem(); }
            }
        }

        IEnumerator FadeOutVideoControls(float time)
        {
            yield return new WaitForSeconds(time);

            if (updateControlInput) { Cursor.visible = false; }
            videoControls.enabled = true;
            videoControls.CrossFade("Out", 0.15f);

            StopCoroutine("DisableVideoControlsAnimator");
            StartCoroutine("DisableVideoControlsAnimator");
        }

        IEnumerator DisableMiniPlayer()
        {
            yield return new WaitForSeconds(cachedMiniPlayerLength);
            miniPlayer.gameObject.SetActive(false);
        }

        IEnumerator DisableMiniPlayerAnimator()
        {
            yield return new WaitForSeconds(cachedMiniPlayerLength);
            miniPlayer.enabled = false;
        }

        IEnumerator DisableVideoControlsAnimator()
        {
            yield return new WaitForSeconds(cachedVideoControlsLength);
            videoControls.enabled = false;
        }
    }
}