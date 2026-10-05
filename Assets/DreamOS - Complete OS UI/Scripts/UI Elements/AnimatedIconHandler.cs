using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Michsky.DreamOS
{
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
            StopCoroutine("DisableAnimator");
            StartCoroutine("DisableAnimator");
        }

        public void PlayStart()
        {
            isIn = false;
            iconAnimator.enabled = true;
            iconAnimator.CrossFade("Start", crossFade);

            StopCoroutine("DisableAnimator");
            StartCoroutine("DisableAnimator");
        }

        public void PlayIn()
        {
            isIn = true;

            if (!gameObject.activeInHierarchy)
                return;

            iconAnimator.enabled = true;
            iconAnimator.CrossFade("In", crossFade);

            StopCoroutine("DisableAnimator");
            StartCoroutine("DisableAnimator");
        }

        public void PlayOut()
        {
            isIn = false;

            if (!gameObject.activeInHierarchy)
                return;

            iconAnimator.enabled = true;
            iconAnimator.CrossFade("Out", crossFade);

            StopCoroutine("DisableAnimator");
            StartCoroutine("DisableAnimator");
        }

        public void Animate()
        {
            if (isIn == true) { PlayOut(); }
            else { PlayIn(); }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (playType == PlayType.Click)
            {
                Animate();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (playType == PlayType.Hover)
            {
                PlayIn();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (playType == PlayType.Hover)
            {
                PlayOut();
            }
        }

        IEnumerator DisableAnimator()
        {
            yield return new WaitForSeconds(disableAfter);
            iconAnimator.enabled = false;
        }
    }
}