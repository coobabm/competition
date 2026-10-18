using System.Collections;
using System.Collections.Generic;
using Michsky.DreamOS;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Video;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>
    /// The desktop video player (2026-10-19), shown in the story as 暴风影音 (see OriginCurtain). Replaces DreamOS's
    /// VideoPlayerManager: same GUID, serialized fields and public methods, so the Main scene's library, "Now Playing"
    /// panel, controls, mini player and their button events carry over unchanged.
    ///
    /// The library lists one button per <see cref="videoItems"/> entry. Opening one plays it on the "Now Playing" panel;
    /// while the pointer is still there the controls fade out after <see cref="hideControlsIn"/> seconds and come back
    /// when it moves. The texts, slider and buttons on the panel are <see cref="VideoAppPart"/>s.
    /// </summary>
    public class VideoApp : MonoBehaviour
    {
        public enum VideoType { Clip, URL }

        [System.Serializable]
        public class VideoItem
        {
            public string title = "Video Title";
            public string description = "Video Description";
            public Sprite cover;
            public VideoType type = VideoType.Clip;
            public VideoClip clip;
            public string URL;
            [HideInInspector] public VideoListEntry preset;
        }

        public List<VideoItem> videoItems = new List<VideoItem>();

        public VideoPlayer videoPlayer;
        public AudioSource audioSource;
        [SerializeField] Transform videoParent;
        [SerializeField] GameObject videoPreset;
        [SerializeField] WindowPanelManager panelManager;
        [SerializeField] Animator videoControls;
        [SerializeField] Animator miniPlayer;

        [Range(1, 15)] public float hideControlsIn = 2.5f;
        [Range(1, 60)] public float seekTime = 10;
        [SerializeField] string videoPanelName = "Now Playing";

        /// <summary>Parts refreshed on play/pause/open; filled by the parts themselves.</summary>
        public List<VideoAppPart> dataToBeUpdated = new List<VideoAppPart>();
        /// <summary>The playing library entry, or -1 for a video opened from outside the library.</summary>
        [HideInInspector] public int currentClipIndex;
        [HideInInspector] public int secondsPassed;
        [HideInInspector] public int minutesPassed;
        [HideInInspector] public int totalSeconds;
        [HideInInspector] public int totalMinutes;
        [HideInInspector] public bool loop;
        /// <summary>Title of a video opened from outside the library (mail attachment, download).</summary>
        [HideInInspector] public string tempVideoTitle;
        [HideInInspector] public bool isDone;
        [HideInInspector] public bool isMiniPlayerEnabled;

        bool watchPointer;
        bool controlsShown;
        float miniPlayerAnimLength = .5f;
        float controlsAnimLength = .5f;
        Vector3 lastPointer;
        Coroutine controlsFade, controlsAnimOff, miniAnimOff, miniOff;

        public double time => videoPlayer.time;
        public ulong duration => (ulong)(videoPlayer.frameCount / videoPlayer.frameRate);
        public double nTime => time / duration;

        /// <summary>The library entry being played, or null for a video from outside the library.</summary>
        public VideoItem Current =>
            currentClipIndex >= 0 && currentClipIndex < videoItems.Count ? videoItems[currentClipIndex] : null;

        void Awake() => Initialize();

        // Pause() does nothing once the window is inactive, so closing the window leaves the video running (as before).
        void OnDisable() => Pause();

        public void Initialize()
        {
            videoPlayer.SetTargetAudioSource(0, audioSource);
            if (miniPlayer != null)
                miniPlayerAnimLength = DreamOSInternalTools.GetAnimatorClipLength(miniPlayer, "MiniPlayer_In") + .1f;
            if (videoControls != null)
                controlsAnimLength = DreamOSInternalTools.GetAnimatorClipLength(videoControls, "VideoControls_In") + .1f;

            foreach (Transform child in videoParent) Destroy(child.gameObject);

            for (int i = 0; i < videoItems.Count; i++)
            {
                var item = videoItems[i];
                bool clip = item.type == VideoType.Clip;
                var entry = AddEntry(item.cover, item.title, item.description, item.type,
                    clip ? FormatLength(item.clip.length) : "URL");
                entry.videoIndex = i;
                if (!clip) entry.videoURL = item.URL;
                item.preset = entry;
                entry.GetComponent<ButtonManager>().onClick.AddListener(() =>
                {
                    if (entry.type == VideoType.Clip) OpenVideo(entry.videoIndex);
                    else OpenVideo(entry.videoURL, entry.videoIndex);
                });
            }

            if (miniPlayer != null) miniPlayer.gameObject.SetActive(false);
        }

        static string FormatLength(double seconds)
        {
            int s = (int)seconds;
            return (s / 60 % 60) + ":" + (s % 60).ToString("D2");
        }

        VideoListEntry AddEntry(Sprite cover, string title, string description, VideoType type, string length)
        {
            var go = AppSpawn.Row(videoPreset, videoParent, title);
            var entry = go.GetComponent<VideoListEntry>();
            entry.manager = this;
            entry.type = type;
            entry.coverImage.sprite = cover;
            entry.titleText.text = title;
            entry.descriptionText.text = description;
            entry.durationText.text = length;
            return entry;
        }

        void Update()
        {
            if (videoPlayer.isPrepared)
            {
                int total = (int)duration, passed = (int)time;
                totalMinutes = total / 60;
                totalSeconds = total - totalMinutes * 60;
                minutesPassed = passed / 60;
                secondsPassed = passed - minutesPassed * 60;
            }

            if (!watchPointer || Mouse.current == null) return;
            Vector2 p = Mouse.current.position.ReadValue();
            var pointer = new Vector3(p.x, p.y, 0);
            if (pointer == lastPointer && controlsShown) HideVideoControls(hideControlsIn);
            else if (pointer != lastPointer && !controlsShown) ShowVideoControls();
            lastPointer = pointer;
        }

        public void OpenVideo(int index)
        {
            Begin(index, null, VideoSource.VideoClip, videoItems[index].clip, null);
        }

        public void OpenVideo(string url, int index = -1)
        {
            Begin(index, null, VideoSource.Url, null, url);
        }

        public void OpenVideo(VideoClip clip, string title)
        {
            Begin(-1, title, VideoSource.VideoClip, clip, null);
        }

        void Begin(int index, string title, VideoSource source, VideoClip clip, string url)
        {
            currentClipIndex = index;
            tempVideoTitle = title;
            videoPlayer.Stop();
            videoPlayer.source = source;
            if (source == VideoSource.VideoClip) videoPlayer.clip = clip;
            else videoPlayer.url = url;
            videoPlayer.time = 0;
            videoPlayer.Play();
            panelManager.OpenPanel(videoPanelName);
            HideMiniPlayer();
            UpdateDataItems();
        }

        /// <summary>Adds a library entry that plays a file or URL (mods and the story's origin video).</summary>
        public void CreateVideo(Sprite cover, string title, string desc, string url)
        {
            var entry = AddEntry(cover, title, desc, VideoType.URL, "MOD");
            entry.videoURL = url;
            entry.GetComponent<ButtonManager>().onClick.AddListener(() => OpenVideo(entry.videoURL));
        }

        public void Play()
        {
            videoPlayer.Play();
            UpdateDataItems();
        }

        public void Pause()
        {
            if (!gameObject.activeInHierarchy) return;
            videoPlayer.Pause();
            UpdateDataItems();
            Halt(ref controlsFade);
        }

        public void SeekForward() => videoPlayer.time += seekTime;

        public void SeekBackward() => videoPlayer.time -= seekTime;

        public void IncreasePlaybackSpeed() => ChangeSpeed(1);

        public void DecreasePlaybackSpeed() => ChangeSpeed(-1);

        void ChangeSpeed(float delta)
        {
            if (!videoPlayer.canSetPlaybackSpeed) return;
            videoPlayer.playbackSpeed = Mathf.Clamp(videoPlayer.playbackSpeed + delta, 0, 10);
        }

        public void ShowVideoControls()
        {
            Halt(ref controlsFade);
            Restart(ref controlsAnimOff, ControlsAnimatorOff());
            Cursor.visible = true;
            controlsShown = true;
            videoControls.enabled = true;
            videoControls.CrossFade("In", .15f);
        }

        public void HideVideoControls(float delay = .1f)
        {
            Restart(ref controlsFade, FadeControls(delay));
            controlsShown = false;
        }

        /// <summary>Hooked to the window's minimise/close: a playing video carries on in the mini player.</summary>
        public void ShowMiniPlayer()
        {
            if (miniPlayer == null) return;
            if (!videoPlayer.isPlaying) { HideMiniPlayer(); return; }
            Halt(ref miniOff);
            Restart(ref miniAnimOff, MiniAnimatorOff());
            miniPlayer.gameObject.SetActive(true);
            isMiniPlayerEnabled = true;
            miniPlayer.enabled = true;
            miniPlayer.Play("In");
        }

        public void HideMiniPlayer()
        {
            if (miniPlayer == null || !isMiniPlayerEnabled) return;
            Halt(ref miniAnimOff);
            Restart(ref miniOff, MiniPlayerOff());
            isMiniPlayerEnabled = false;
            miniPlayer.enabled = true;
            miniPlayer.Play("Out");
        }

        /// <summary>The "Now Playing" panel's pointer enter/exit: watch the pointer to hide or show the controls.</summary>
        public void UpdateControlInput(bool value)
        {
            watchPointer = value;
            if (!value) HideVideoControls();
        }

        public void UpdateDataItems()
        {
            for (int i = 0; i < dataToBeUpdated.Count; i++)
            {
                var part = dataToBeUpdated[i];
                if (part == null) { dataToBeUpdated.RemoveAt(i); continue; }
                if (!part.alwaysUpdate && part.gameObject.activeInHierarchy) part.UpdateItem();
            }
        }

        void Restart(ref Coroutine slot, IEnumerator routine)
        {
            Halt(ref slot);
            slot = StartCoroutine(routine);
        }

        void Halt(ref Coroutine slot)
        {
            if (slot != null) StopCoroutine(slot);
            slot = null;
        }

        IEnumerator FadeControls(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (watchPointer) Cursor.visible = false;
            videoControls.enabled = true;
            videoControls.CrossFade("Out", .15f);
            Restart(ref controlsAnimOff, ControlsAnimatorOff());
            controlsFade = null;
        }

        // The animators are switched off between transitions so they do not hold the UI's values every frame.
        IEnumerator ControlsAnimatorOff()
        {
            yield return new WaitForSeconds(controlsAnimLength);
            videoControls.enabled = false;
            controlsAnimOff = null;
        }

        IEnumerator MiniAnimatorOff()
        {
            yield return new WaitForSeconds(miniPlayerAnimLength);
            miniPlayer.enabled = false;
            miniAnimOff = null;
        }

        IEnumerator MiniPlayerOff()
        {
            yield return new WaitForSeconds(miniPlayerAnimLength);
            miniPlayer.gameObject.SetActive(false);
            miniOff = null;
        }
    }
}
