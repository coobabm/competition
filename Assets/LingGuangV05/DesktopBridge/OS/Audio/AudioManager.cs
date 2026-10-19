using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// The desktop's sound hub: the shared UI <see cref="AudioSource"/> (<see cref="instance"/>), the master volume
    /// slider wired to the mixer's "Master" parameter, the volume icons in the taskbar and mixer, and the mouse and
    /// keyboard keystroke sounds. Replaces DreamOS's AudioManager (2026-10-19): same GUID, serialized fields and
    /// public API.
    ///
    /// The master volume is saved in the System data category under the slider's save key and restored on Awake.
    ///
    /// The mouse stroke is held until the release and replaced by an action's own sound when the click opens,
    /// closes or switches something (2026-10-19, see <see cref="DesktopSfx"/>).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager instance;

        // Resources
        public UIManager UIManagerAsset;
        public AudioSource audioSource;
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private SliderManager masterSlider;
        [SerializeField] private Image taskbarIndicator;
        [SerializeField] private Image mixerIndicator;

        // Settings
        public Sprite volumeMuted;
        public Sprite volumeLow;
        public Sprite volumeHigh;
        [Range(0f, 1f)] public float mouseStrokeVolume = 1f;

        const string MasterParameter = "Master";

        float muteValue = 0.0001f;
        float preMute = 1;
        bool isMuted;
        DreamOSDataManager.DataCategory dataCat = DreamOSDataManager.DataCategory.System;

        void Awake()
        {
            instance = this;

            if (audioSource == null) { audioSource = GetComponent<AudioSource>(); }
            if (mixer == null || masterSlider == null) { return; }
            if (!DreamOSDataManager.ContainsJsonKey(dataCat, masterSlider.saveKey)) { DreamOSDataManager.WriteFloatData(dataCat, masterSlider.saveKey, masterSlider.mainSlider.value); }

            mixer.SetFloat(MasterParameter, VolumeToDecibels(DreamOSDataManager.ReadFloatData(dataCat, masterSlider.saveKey)));
            masterSlider.mainSlider.onValueChanged.AddListener(SetMasterVolume);
            masterSlider.mainSlider.onValueChanged.Invoke(masterSlider.mainSlider.value);
        }

        void Update()
        {
            if (UIManagerAsset == null || audioSource == null || !UIManagerAsset.enableKeystrokes || Time.timeScale == 0)
                return;

            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            if (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame)) { PlayMouseStroke(); }
            else if (keyboard != null && keyboard.anyKey.wasPressedThisFrame) { PlayKeyboardStroke(); }
        }

        public void PlayKeyboardStroke()
        {
            if (UIManagerAsset == null || audioSource == null || !UIManagerAsset.enableKeyboardKeystroke || UIManagerAsset.keyboardStrokes == null || UIManagerAsset.keyboardStrokes.Count == 0)
                return;

            var clip = UIManagerAsset.keyboardStrokes[Random.Range(0, UIManagerAsset.keyboardStrokes.Count)];
            if (clip != null) { audioSource.PlayOneShot(clip); }
        }

        public void PlayMouseStroke()
        {
            if (UIManagerAsset == null || audioSource == null || !UIManagerAsset.enableMouseKeystroke || UIManagerAsset.mouseStrokes == null || UIManagerAsset.mouseStrokes.Count == 0)
                return;

            // Held until the release so an action sound can take its place (one sound per click)
            var clip = UIManagerAsset.mouseStrokes[Random.Range(0, UIManagerAsset.mouseStrokes.Count)];
            if (clip != null) { DesktopSfx.HoldClick(clip, mouseStrokeVolume); }
        }

        public void EnableStrokes(bool value)
        {
            UIManagerAsset.enableKeystrokes = value;
        }

        public void SetMasterVolume(float volume)
        {
            if (mixer == null || masterSlider == null)
                return;

            mixer.SetFloat(MasterParameter, VolumeToDecibels(volume));

            float sliderValue = masterSlider.mainSlider.value;
            UpdateVolumeIcon(taskbarIndicator, sliderValue);
            UpdateVolumeIcon(mixerIndicator, sliderValue);

            isMuted = sliderValue <= muteValue;
        }

        public void Mute()
        {
            if (isMuted) { masterSlider.mainSlider.value = preMute; }
            else { preMute = masterSlider.mainSlider.value; masterSlider.mainSlider.value = muteValue; }

            SetMasterVolume(masterSlider.mainSlider.value);
        }

        static float VolumeToDecibels(float volume)
        {
            return Mathf.Log10(volume) * 20;
        }

        // Exactly 0.5 keeps whatever icon was showing.
        void UpdateVolumeIcon(Image indicator, float sliderValue)
        {
            if (indicator == null) { return; }

            if (sliderValue <= muteValue) { indicator.sprite = volumeMuted; }
            else if (sliderValue > 0.5f) { indicator.sprite = volumeHigh; }
            else if (sliderValue < 0.5f) { indicator.sprite = volumeLow; }
        }
    }
}
