using UnityEngine;
using UnityEngine.EventSystems;

namespace Michsky.DreamOS
{
    [DisallowMultipleComponent]
    [AddComponentMenu("DreamOS/Audio/UI Element Sound")]
    public class UIElementSound : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
    {
        [Header("Resources")]
        public AudioManager audioManager;
        public AudioSource audioSource;

        [Header("Custom SFX")]
        public AudioClip hoverSFX;
        public AudioClip clickSFX;

        [Header("Settings")]
        public bool enableHoverSound = true;
        public bool enableClickSound = true;

        void OnEnable()
        {
            if (audioManager != null && audioSource == null)
            {
                audioSource = audioManager.audioSource; 
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (enableHoverSound)
            {
                if (hoverSFX == null) { audioSource.PlayOneShot(audioManager.UIManagerAsset.hoverSound); }
                else { audioSource.PlayOneShot(hoverSFX); }
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (enableClickSound)
            {
                if (clickSFX == null) { audioSource.PlayOneShot(audioManager.UIManagerAsset.clickSound); }
                else { audioSource.PlayOneShot(clickSFX); }
            }
        }
    }
}