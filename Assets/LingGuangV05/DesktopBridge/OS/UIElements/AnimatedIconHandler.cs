using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// Drives a small state-machine icon (hamburger, chevron, ...) on the desktop (2026-10-19). Replaces DreamOS's
    /// AnimatedIconHandler: same GUID, serialized fields and public API.
    ///
    /// The animator needs "Start", "In" and "Out" states; <see cref="PlayType"/> says what triggers the toggle (click,
    /// hover, a linked button's onClick, or only code/UnityEvents calling <see cref="Animate"/>). The animator is parked
    /// (disabled) once the clip has had time to finish, so idle icons cost nothing.
    /// </summary>
    public class AnimatedIconHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Resources")]
        [SerializeField] private Animator iconAnimator;
        [SerializeField] private ButtonManager targetButton;

        [Header("Settings")]
        [SerializeField] private PlayType playType;
        [SerializeField] private string defaultState = "HamburgerMenu_In";
        [SerializeField] [Range(0, 1)] private float crossFade = 0;

        // Helpers
        bool isIn;
        float disableAfter = 1;
        Coroutine disableRoutine;

        public enum PlayType
        {
            Click,
            Hover,
            Button,
            Other
        }

        void Awake()
        {
            if (iconAnimator == null) { iconAnimator = gameObject.GetComponent<Animator>(); }
            if (playType == PlayType.Button && targetButton != null) { targetButton.onClick.AddListener(Animate); }

            disableAfter = DreamOSInternalTools.GetAnimatorClipLength(iconAnimator, defaultState) + 0.1f;
        }

        void OnEnable()
        {
            RestartDisableTimer();
        }

        public void PlayStart()
        {
            isIn = false;
            Play("Start");
        }

        public void PlayIn()
        {
            isIn = true;
            if (!gameObject.activeInHierarchy) { return; }

            Play("In");
        }

        public void PlayOut()
        {
            isIn = false;
            if (!gameObject.activeInHierarchy) { return; }

            Play("Out");
        }

        public void Animate()
        {
            if (isIn) { PlayOut(); }
            else { PlayIn(); }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (playType == PlayType.Click) { Animate(); }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (playType == PlayType.Hover) { PlayIn(); }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (playType == PlayType.Hover) { PlayOut(); }
        }

        void Play(string state)
        {
            iconAnimator.enabled = true;
            iconAnimator.CrossFade(state, crossFade);
            RestartDisableTimer();
        }

        void RestartDisableTimer()
        {
            if (disableRoutine != null) { StopCoroutine(disableRoutine); }
            disableRoutine = StartCoroutine(DisableAnimator());
        }

        IEnumerator DisableAnimator()
        {
            yield return new WaitForSeconds(disableAfter);
            iconAnimator.enabled = false;
        }
    }
}
