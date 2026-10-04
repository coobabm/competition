using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Michsky.DreamOS
{
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

        // Hidden vars
        [HideInInspector] public AudioSource aSource;
        [HideInInspector] public AudioClip aClip;

        void Start()
        {
            this.enabled = false;

            playButton.gameObject.SetActive(true);
            stopButton.gameObject.SetActive(false);
        
            durationBackground.sprite = durationRandomizer[Random.Range(0, durationRandomizer.Count)];
            durationForeground.sprite = durationBackground.sprite;

            durationSlider.value = 0;
        }

        void Update()
        {
            if (aSource.clip.name != aClip.name)
            {
                this.enabled = false;
                durationSlider.value = 0;

                playButton.gameObject.SetActive(true);
                stopButton.gameObject.SetActive(false);

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
            this.enabled = true;            
            durationSlider.maxValue = aClip.length;

            aSource.clip = aClip; 
            if (rememberPosition && durationSlider.value < aClip.length) { aSource.time = Mathf.Min(durationSlider.value, aSource.clip.length - 0.01f); }
            aSource.Play();

            playButton.gameObject.SetActive(false);
            stopButton.gameObject.SetActive(true);
        }

        public void StopAudio()
        {
            this.enabled = false;

            playButton.gameObject.SetActive(true);
            stopButton.gameObject.SetActive(false);

            if (rememberPosition) { aSource.Pause(); }
            else
            {
                durationSlider.value = 0;
                aSource.Stop();
            }
        }
    }
}