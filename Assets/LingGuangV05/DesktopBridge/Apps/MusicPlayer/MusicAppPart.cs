using System.Collections;
using LingGuangV05.Desktop.OS;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>
    /// One text, image, slider or button that follows or drives the music player (replaces DreamOS's
    /// MusicPlayerDataItem; same GUID and fields). <see cref="objectType"/> says which. Repeat and shuffle are
    /// remembered between sessions; accent parts fade to the playing cover's colour.
    /// </summary>
    [DisallowMultipleComponent]
    public class MusicAppPart : MonoBehaviour
    {
        // The order is serialized: do not reorder.
        public enum ObjectType
        {
            Title,
            Artist,
            Album,
            Cover,
            PlayTime,
            Duration,
            MusicSlider,
            PlayButton,
            PauseButton,
            NextButton,
            PrevButton,
            Repeat,
            Shuffle,
            VolumeSlider,
            AccentColor,
            AccentMatchColor,
            AccentColorTMP,
            AccentMatchColorTMP
        }

        const float ColorFade = .25f;
        const DreamOSDataManager.DataCategory Category = DreamOSDataManager.DataCategory.Apps;

        [SerializeField] MusicApp playerManager;
        [Tooltip("Refresh every frame instead of only when the track changes (the play time and slider).")]
        public bool alwaysUpdate;
        [SerializeField] ObjectType objectType;

        TextMeshProUGUI text;
        Image image;
        Slider slider;
        AnimatedIconHandler icon;
        Coroutine fade;

        public bool IsVolumeSlider => objectType == ObjectType.VolumeSlider;

        void Start() => Initalize();

        void OnEnable() => UpdateItem();

        void Update()
        {
            if (alwaysUpdate) UpdateItem();
        }

        public void Initalize()
        {
            if (playerManager == null)
                foreach (var app in Resources.FindObjectsOfTypeAll<MusicApp>())
                    if (app.gameObject.scene.name != null) { playerManager = app; break; }
            if (playerManager == null) return;

            switch (objectType)
            {
                case ObjectType.Title:
                case ObjectType.Artist:
                case ObjectType.Album:
                case ObjectType.PlayTime:
                case ObjectType.Duration:
                case ObjectType.AccentColorTMP:
                case ObjectType.AccentMatchColorTMP:
                    text = GetComponent<TextMeshProUGUI>();
                    break;
                case ObjectType.Cover:
                case ObjectType.AccentColor:
                case ObjectType.AccentMatchColor:
                    image = GetComponent<Image>();
                    break;
                case ObjectType.PlayButton:
                case ObjectType.PauseButton:
                    icon = GetComponent<AnimatedIconHandler>();
                    OnClick(PlayOrPause);
                    break;
                case ObjectType.NextButton:
                    icon = GetComponent<AnimatedIconHandler>();
                    OnClick(() => Skip(true));
                    break;
                case ObjectType.PrevButton:
                    icon = GetComponent<AnimatedIconHandler>();
                    OnClick(() => Skip(false));
                    break;
                case ObjectType.MusicSlider:
                    slider = GetComponent<Slider>();
                    slider.onValueChanged.AddListener(_ =>
                    {
                        // The very end would stop the clip instead of seeking.
                        if (slider.value > playerManager.audioSource.clip.length - .01f) return;
                        playerManager.audioSource.time = slider.value;
                    });
                    break;
                case ObjectType.VolumeSlider:
                    slider = GetComponent<Slider>();
                    slider.onValueChanged.AddListener(SetVolume);
                    if (playerManager.audioSource != null) playerManager.audioSource.volume = slider.value;
                    break;
                case ObjectType.Repeat:
                    SetUpToggle("MusicPlayer_Repeat", () => playerManager.repeat, v => playerManager.repeat = v);
                    break;
                case ObjectType.Shuffle:
                    SetUpToggle("MusicPlayer_Shuffle", () => playerManager.shuffle, v => playerManager.shuffle = v);
                    break;
            }

            playerManager.dataToBeUpdated.Add(this);
            UpdateItem();
        }

        void OnClick(UnityEngine.Events.UnityAction action) => GetComponent<ButtonManager>().onClick.AddListener(action);

        void PlayOrPause()
        {
            if (!playerManager.gameObject.activeInHierarchy)
            {
                playerManager.GetComponent<WindowManager>().OpenWindow();
                playerManager.Play();
            }
            else if (playerManager.audioSource.isPlaying) playerManager.Pause();
            else playerManager.Play();
        }

        void Skip(bool forward)
        {
            if (!playerManager.gameObject.activeInHierarchy) return;
            playerManager.audioSource.time = 0;
            if (forward) playerManager.NextTrack(); else playerManager.PrevTrack();
            if (icon != null) icon.PlayIn();
        }

        // Repeat / shuffle: a click flips the setting and saves it; on start the saved value wins over the scene's.
        void SetUpToggle(string key, System.Func<bool> get, System.Action<bool> set)
        {
            icon = GetComponent<AnimatedIconHandler>();
            OnClick(() =>
            {
                bool on = !get();
                DreamOSDataManager.WriteBooleanData(Category, key, on);
                set(on);
                if (on) icon.PlayIn(); else icon.PlayOut();
            });
            if (!DreamOSDataManager.ContainsJsonKey(Category, key)) DreamOSDataManager.WriteBooleanData(Category, key, get());
            else set(DreamOSDataManager.ReadBooleanData(Category, key));
        }

        public void UpdateItem()
        {
            if (playerManager == null || playerManager.audioSource.clip == null) return;
            var m = playerManager;
            switch (objectType)
            {
                case ObjectType.Title when text != null: text.text = m.GetTrackName(); break;
                case ObjectType.Artist when text != null: text.text = m.GetArtistName(); break;
                case ObjectType.Album when text != null: text.text = m.GetAlbumName(); break;
                case ObjectType.Cover when image != null: image.sprite = m.GetCoverArt(); break;
                case ObjectType.PlayTime when text != null: text.text = m.GetNormalizedPlayTime(); break;
                case ObjectType.Duration when text != null: text.text = m.GetNormalizedDuration(); break;
                case ObjectType.MusicSlider when slider != null:
                    slider.maxValue = m.audioSource.clip.length;
                    slider.SetValueWithoutNotify(m.audioSource.time);
                    break;
                case ObjectType.Shuffle when icon != null: if (m.shuffle) icon.PlayIn(); else icon.PlayOut(); break;
                case ObjectType.Repeat when icon != null: if (m.repeat) icon.PlayIn(); else icon.PlayOut(); break;
                case ObjectType.AccentColor when image != null: FadeTo(m.GetAccentColor(image)); break;
                case ObjectType.AccentMatchColor when image != null: FadeTo(m.GetAccentMatchColor(image)); break;
                case ObjectType.AccentColorTMP when text != null: FadeTo(m.GetAccentColor(text)); break;
                case ObjectType.AccentMatchColorTMP when text != null: FadeTo(m.GetAccentMatchColor(text)); break;
                case ObjectType.PlayButton when icon != null:
                case ObjectType.PauseButton when icon != null:
                    if (m.audioSource.isPlaying) icon.PlayOut(); else icon.PlayIn();
                    break;
            }
        }

        public void SetVolume(float volume) => playerManager.audioSource.volume = slider.value;

        Graphic Tinted => image != null ? image : (Graphic)text;

        void FadeTo(Color target)
        {
            var graphic = Tinted;
            if (!graphic.gameObject.activeInHierarchy) { graphic.color = target; return; }
            if (fade != null) StopCoroutine(fade);
            fade = StartCoroutine(Fade(graphic, target));
        }

        IEnumerator Fade(Graphic graphic, Color target)
        {
            float start = Time.time;
            Color from = graphic.color;
            while (Time.time - start < ColorFade)
            {
                graphic.color = Color.Lerp(from, target, (Time.time - start) / ColorFade);
                yield return new WaitForEndOfFrame();
            }
            graphic.color = target;
            fade = null;
        }
    }
}
