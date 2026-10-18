using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>
    /// A voice-message bubble (moved from DreamOS 2026-10-19; same GUID and fields): play/stop on the shared audio
    /// source, a bar that follows playback over a randomly picked waveform, and (optionally) resuming where it stopped.
    /// </summary>
    public class AudioMessage : MonoBehaviour
    {
        [Header("Resources")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button stopButton;
        [SerializeField] private Slider durationSlider;
        [SerializeField] private Image durationBackground;
        [SerializeField] private Image durationForeground;
        public TextMeshProUGUI timeText;

        [Header("Settings")]
        [SerializeField] private bool rememberPosition = true;
        [SerializeField] private List<Sprite> durationRandomizer = new List<Sprite>();

        [HideInInspector] public AudioSource aSource;
        [HideInInspector] public AudioClip aClip;

        void Start()
        {
            enabled = false;
            ShowPlaying(false);
            durationBackground.sprite = durationRandomizer[Random.Range(0, durationRandomizer.Count)];
            durationForeground.sprite = durationBackground.sprite;
            durationSlider.value = 0;
        }

        void Update()
        {
            // Something else took over the audio source.
            if (aSource.clip.name != aClip.name)
            {
                enabled = false;
                durationSlider.value = 0;
                ShowPlaying(false);
                return;
            }

            durationSlider.value = aSource.time;
            if (durationSlider.value >= aClip.length)
            {
                StopAudio();
                durationSlider.value = 0;
            }
        }

        public void PlayAudio()
        {
            enabled = true;
            durationSlider.maxValue = aClip.length;

            aSource.clip = aClip;
            if (rememberPosition && durationSlider.value < aClip.length) aSource.time = Mathf.Min(durationSlider.value, aSource.clip.length - 0.01f);
            aSource.Play();
            ShowPlaying(true);
        }

        public void StopAudio()
        {
            enabled = false;
            ShowPlaying(false);

            if (rememberPosition) aSource.Pause();
            else
            {
                durationSlider.value = 0;
                aSource.Stop();
            }
        }

        void ShowPlaying(bool playing)
        {
            playButton.gameObject.SetActive(!playing);
            stopButton.gameObject.SetActive(playing);
        }
    }
}
