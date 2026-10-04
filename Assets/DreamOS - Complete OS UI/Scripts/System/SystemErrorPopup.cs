using System.Collections;
using UnityEngine;

namespace Michsky.DreamOS
{
    public class SystemErrorPopup : MonoBehaviour
    {
        [Header("Resources")]
        [SerializeField] private Animator animator;

        [Header("Settings")]
        [SerializeField] private bool showOnAwake = false;
        [SerializeField][Range(0, 15)] private float autoHideIn = 0;

        // Helpers
        float cachedStateLength = 0.5f;
        bool isOn;
        bool isLeftover;

        void Awake()
        {
            cachedStateLength = DreamOSInternalTools.GetAnimatorClipLength(animator, "SystemErrorPopup_Show") + 0.1f;
         
            if (showOnAwake) { Show(); }
            else if (!isOn) { gameObject.SetActive(false); }
        }

        void OnEnable()
        {
            if (isLeftover)
            {
                isOn = false;
                isLeftover = false;
                gameObject.SetActive(false);
            }
        }

        void OnDisable()
        {
            if (isOn)
            {
                isLeftover = true;
            }
        }

        public void Show()
        {
            if (isOn)
                return;

            isOn = true;
            gameObject.SetActive(true);

            animator.enabled = true;
            animator.Play("Show");

            StopCoroutine("DisableObject");
            StopCoroutine("DisableAnimator");
            StartCoroutine("DisableAnimator");

            if (autoHideIn > 0)
            {
                StopCoroutine("AutoHideTimer");
                StartCoroutine("AutoHideTimer", autoHideIn);
            }
        }

        public void Hide()
        {
            if (!isOn || !gameObject.activeInHierarchy)
                return;

            isOn = false;
            animator.enabled = true;
            animator.Play("Hide");

            StopCoroutine("DisableAnimator");
            StartCoroutine("DisableAnimator");

            StopCoroutine("DisableObject");
            StartCoroutine("DisableObject");
        }

        IEnumerator AutoHideTimer()
        {
            yield return new WaitForSeconds(autoHideIn);
            Hide();
        }

        IEnumerator DisableAnimator()
        {
            yield return new WaitForSeconds(cachedStateLength);
            animator.enabled = false;
        }

        IEnumerator DisableObject()
        {
            yield return new WaitForSeconds(cachedStateLength);
            gameObject.SetActive(false);
        }
    }
}