using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Michsky.DreamOS
{
    [DisallowMultipleComponent]
    [AddComponentMenu("DreamOS/Apps/Music Player/Music Player Data Item")]
    public class MusicPlayerDataItem : MonoBehaviour
    {
        [Header("Resources")]
        [SerializeField] private MusicPlayerManager playerManager;

        [Header("Settings")]
        public bool alwaysUpdate = false;
        [SerializeField] private ObjectType objectType;

        // Helpers
        float colorAnimDuration = 0.25f;
        TextMeshProUGUI textObj;
        Image imageObj;
        Slider sliderObj;
        ButtonManager btnObj;
        AnimatedIconHandler animHandler;
        DreamOSDataManager.DataCategory dataCat = DreamOSDataManager.DataCategory.Apps;

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
                    foreach (MusicPlayerManager mpm in Resources.FindObjectsOfTypeAll(typeof(MusicPlayerManager)) as MusicPlayerManager[])
                    {
                        if (mpm.gameObject.scene.name != null) 
                        { 
                            playerManager = mpm;
                            break;
                        }
                    }
                }

                catch { Debug.Log("<b>[Music Player Data Item]</b> Player Manager is missing.", this); return; }
            }

            // Don't go further if music player manager is not assigned
            if (playerManager == null)
                return;

            // Get and change the value depending on the object type
            if (objectType == ObjectType.Title || objectType == ObjectType.Artist || objectType == ObjectType.Album) { textObj = GetComponent<TextMeshProUGUI>(); }
            else if (objectType == ObjectType.PlayTime || objectType == ObjectType.Duration) { textObj = GetComponent<TextMeshProUGUI>(); }
            else if (objectType == ObjectType.Cover || objectType == ObjectType.AccentColor || objectType == ObjectType.AccentMatchColor) { imageObj = GetComponent<Image>(); }
            else if (objectType == ObjectType.AccentColorTMP || objectType == ObjectType.AccentMatchColorTMP) { textObj = GetComponent<TextMeshProUGUI>(); }
            else if (objectType == ObjectType.PlayButton || objectType == ObjectType.PauseButton) { InitializePlayAndPauseButton(); }
            else if (objectType == ObjectType.NextButton) { InitializeNextButton(); }
            else if (objectType == ObjectType.PrevButton) { InitializePrevButton(); }
            else if (objectType == ObjectType.MusicSlider) { InitializeMusicSlider(); }
            else if (objectType == ObjectType.VolumeSlider) { InitializeVolumeSlider(); }
            else if (objectType == ObjectType.Repeat) { InitializeRepeatButton(); }
            else if (objectType == ObjectType.Shuffle) { InitializeShuffleButton(); }

            // Add this component to the to be updated list
            playerManager.dataToBeUpdated.Add(this);

            // Manually update the item
            UpdateItem();
        }

        public void UpdateItem()
        {
            if (playerManager == null || playerManager.audioSource.clip == null)
                return;

            // Change the value depending on the object type
            if (objectType == ObjectType.Title && textObj != null) { textObj.text = playerManager.GetTrackName(); }
            else if (objectType == ObjectType.Artist && textObj != null) { textObj.text = playerManager.GetArtistName(); }
            else if (objectType == ObjectType.Album && textObj != null) { textObj.text = playerManager.GetAlbumName(); }
            else if (objectType == ObjectType.Cover && imageObj != null) { imageObj.sprite = playerManager.GetCoverArt(); }
            else if (objectType == ObjectType.PlayTime && textObj != null) { textObj.text = playerManager.GetNormalizedPlayTime(); }
            else if (objectType == ObjectType.Duration && textObj != null) { textObj.text = playerManager.GetNormalizedDuration(); }
            else if (objectType == ObjectType.MusicSlider && sliderObj != null) { sliderObj.maxValue = playerManager.audioSource.clip.length; sliderObj.value = playerManager.audioSource.time; }
            else if (objectType == ObjectType.Shuffle && playerManager.shuffle && animHandler != null) { animHandler.PlayIn(); }
            else if (objectType == ObjectType.Shuffle && !playerManager.shuffle && animHandler != null) { animHandler.PlayOut(); }
            else if (objectType == ObjectType.Repeat && playerManager.repeat && animHandler != null) { animHandler.PlayIn(); }
            else if (objectType == ObjectType.Repeat && !playerManager.repeat && animHandler != null) { animHandler.PlayOut(); }
            else if (objectType == ObjectType.AccentColor && imageObj != null) { SetImageColor(playerManager.GetAccentColor(imageObj)); }
            else if (objectType == ObjectType.AccentMatchColor && imageObj != null) { SetImageColor(playerManager.GetAccentMatchColor(imageObj)); }
            else if (objectType == ObjectType.AccentColorTMP && textObj != null) { SetTextColor(playerManager.GetAccentColor(textObj)); }
            else if (objectType == ObjectType.AccentMatchColorTMP && textObj != null) { SetTextColor(playerManager.GetAccentMatchColor(textObj)); }
            else if ((objectType == ObjectType.PlayButton || objectType == ObjectType.PauseButton) && animHandler != null)
            {
                if (playerManager.audioSource.isPlaying) { animHandler.PlayOut(); }
                else { animHandler.PlayIn(); }
            }
        }

        void InitializeMusicSlider()
        {
            sliderObj = gameObject.GetComponent<Slider>();
            sliderObj.onValueChanged.AddListener(delegate
            {
                if (sliderObj.value > playerManager.audioSource.clip.length - 0.01f)
                    return;

                playerManager.audioSource.time = sliderObj.value;
            });
        }

        void InitializeVolumeSlider()
        {
            sliderObj = gameObject.GetComponent<Slider>();
            sliderObj.onValueChanged.AddListener(SetVolume);
            if (playerManager.audioSource != null) { playerManager.audioSource.volume = sliderObj.value; }
        }

        void InitializePlayAndPauseButton()
        {
            btnObj = gameObject.GetComponent<ButtonManager>();
            animHandler = gameObject.GetComponent<AnimatedIconHandler>();
            btnObj.onClick.AddListener(delegate
            {
                if (!playerManager.gameObject.activeInHierarchy) { playerManager.gameObject.GetComponent<WindowManager>().OpenWindow(); playerManager.Play(); }
                else if (playerManager.gameObject.activeInHierarchy && playerManager.audioSource.isPlaying) { playerManager.Pause(); }
                else if (playerManager.gameObject.activeInHierarchy && !playerManager.audioSource.isPlaying) { playerManager.Play(); }
            });
        }

        void InitializeNextButton()
        {
            btnObj = gameObject.GetComponent<ButtonManager>();
            animHandler = gameObject.GetComponent<AnimatedIconHandler>();
            btnObj.onClick.AddListener(delegate
            {
                if (playerManager.gameObject.activeInHierarchy) 
                {
                    playerManager.audioSource.time = 0;
                    playerManager.NextTrack();
                    if (animHandler != null) { animHandler.PlayIn(); }
                }
            });
        }

        void InitializePrevButton()
        {
            btnObj = gameObject.GetComponent<ButtonManager>();
            animHandler = gameObject.GetComponent<AnimatedIconHandler>();
            btnObj.onClick.AddListener(delegate
            {
                if (playerManager.gameObject.activeInHierarchy)
                {
                    playerManager.audioSource.time = 0;
                    playerManager.PrevTrack();
                    if (animHandler != null) { animHandler.PlayIn(); }
                }
            });
        }

        void InitializeRepeatButton()
        {
            btnObj = gameObject.GetComponent<ButtonManager>();
            animHandler = gameObject.GetComponent<AnimatedIconHandler>();
            btnObj.onClick.AddListener(delegate
            {
                if (playerManager.repeat)
                {
                    DreamOSDataManager.WriteBooleanData(dataCat, "MusicPlayer_Repeat", false);
                    playerManager.repeat = false;
                    animHandler.PlayOut();
                }

                else
                {
                    DreamOSDataManager.WriteBooleanData(dataCat, "MusicPlayer_Repeat", true);
                    playerManager.repeat = true;
                    animHandler.PlayIn();
                }
            });

            if (!DreamOSDataManager.ContainsJsonKey(dataCat, "MusicPlayer_Repeat") && playerManager.repeat) { DreamOSDataManager.WriteBooleanData(dataCat, "MusicPlayer_Repeat", true); }
            else if (!DreamOSDataManager.ContainsJsonKey(dataCat, "MusicPlayer_Repeat") && !playerManager.repeat) { DreamOSDataManager.WriteBooleanData(dataCat, "MusicPlayer_Repeat", false); }
            else if (DreamOSDataManager.ReadBooleanData(dataCat, "MusicPlayer_Repeat")) { playerManager.repeat = true; }
            else if (!DreamOSDataManager.ReadBooleanData(dataCat, "MusicPlayer_Repeat")) { playerManager.repeat = false; }
        }

        void InitializeShuffleButton()
        {
            btnObj = gameObject.GetComponent<ButtonManager>();
            animHandler = gameObject.GetComponent<AnimatedIconHandler>();
            btnObj.onClick.AddListener(delegate
            {
                if (playerManager.shuffle)
                {
                    DreamOSDataManager.WriteBooleanData(dataCat, "MusicPlayer_Shuffle", false);
                    playerManager.shuffle = false;
                    animHandler.PlayOut();
                }

                else
                {
                    DreamOSDataManager.WriteBooleanData(dataCat, "MusicPlayer_Shuffle", true);
                    playerManager.shuffle = true;
                    animHandler.PlayIn();
                }
            });

            if (!DreamOSDataManager.ContainsJsonKey(dataCat, "MusicPlayer_Shuffle") && playerManager.shuffle) { DreamOSDataManager.WriteBooleanData(dataCat, "MusicPlayer_Shuffle", true); }
            else if (!DreamOSDataManager.ContainsJsonKey(dataCat, "MusicPlayer_Shuffle") && !playerManager.shuffle) { DreamOSDataManager.WriteBooleanData(dataCat, "MusicPlayer_Shuffle", false); }
            else if (DreamOSDataManager.ReadBooleanData(dataCat, "MusicPlayer_Shuffle")) { playerManager.shuffle = true; }
            else if (!DreamOSDataManager.ReadBooleanData(dataCat, "MusicPlayer_Shuffle")) { playerManager.shuffle = false; }
        }

        public void SetVolume(float volume)
        {
            playerManager.audioSource.volume = sliderObj.value;
        }

        void SetImageColor(Color targetColor)
        {
            if (!imageObj.gameObject.activeInHierarchy) { imageObj.color = targetColor; }
            else 
            {
                StopCoroutine("ChangeImageColor");
                StartCoroutine("ChangeImageColor", targetColor);
            }
        }

        void SetTextColor(Color targetColor)
        {
            if (!textObj.gameObject.activeInHierarchy) { textObj.color = targetColor; }
            else
            {
                StopCoroutine("ChangeTextColor");
                StartCoroutine("ChangeTextColor", targetColor);
            }
        }

        IEnumerator ChangeImageColor(Color targetColor)
        {
            float startTime = Time.time;
            Color baseColor = imageObj.color;

            while (Time.time - startTime < colorAnimDuration)
            {
                float t = (Time.time - startTime) / colorAnimDuration;
                imageObj.color = Color.Lerp(baseColor, targetColor, t);
                yield return new WaitForEndOfFrame();
            }

            imageObj.color = targetColor;
        }

        IEnumerator ChangeTextColor(Color targetColor)
        {
            float startTime = Time.time;
            Color baseColor = textObj.color;

            while (Time.time - startTime < colorAnimDuration)
            {
                float t = (Time.time - startTime) / colorAnimDuration;
                textObj.color = Color.Lerp(baseColor, targetColor, t);
                yield return new WaitForEndOfFrame();
            }

            textObj.color = targetColor;
        }
    }
}