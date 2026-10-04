using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

namespace Michsky.DreamOS
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        // Static Instance
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

        // Helpers
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

            mixer.SetFloat("Master", Mathf.Log10(DreamOSDataManager.ReadFloatData(dataCat, masterSlider.saveKey)) * 20);
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

            var clip = UIManagerAsset.mouseStrokes[Random.Range(0, UIManagerAsset.mouseStrokes.Count)];
            if (clip != null) { audioSource.PlayOneShot(clip, Mathf.Clamp01(mouseStrokeVolume)); }
        }

        public void EnableStrokes(bool value)
        {
            if (value == true) { UIManagerAsset.enableKeystrokes = true; }
            else { UIManagerAsset.enableKeystrokes = false; }
        }

        public void SetMasterVolume(float volume)
        {
            if (mixer == null || masterSlider == null)
                return;

            mixer.SetFloat("Master", Mathf.Log10(volume) * 20);

            if (taskbarIndicator != null)
            {
                if (masterSlider.mainSlider.value <= muteValue) { taskbarIndicator.sprite = volumeMuted; }
                else if (masterSlider.mainSlider.value > 0.5f) { taskbarIndicator.sprite = volumeHigh; }
                else if (masterSlider.mainSlider.value < 0.5f) { taskbarIndicator.sprite = volumeLow; }
            }

            if (mixerIndicator != null)
            {
                if (masterSlider.mainSlider.value <= muteValue) { mixerIndicator.sprite = volumeMuted; }
                else if (masterSlider.mainSlider.value > 0.5f) { mixerIndicator.sprite = volumeHigh; }
                else if (masterSlider.mainSlider.value < 0.5f) { mixerIndicator.sprite = volumeLow; }
            }

            if (masterSlider.mainSlider.value > muteValue) { isMuted = false; }
            else { isMuted = true; }
        }

        public void Mute()
        {
            if (isMuted) { masterSlider.mainSlider.value = preMute; }
            else { preMute = masterSlider.mainSlider.value; masterSlider.mainSlider.value = muteValue; }

            SetMasterVolume(masterSlider.mainSlider.value);
        }
    }
}