using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Michsky.DreamOS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public class DateAndTimeTaskbar : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Animator animator;
        float cachedStateLength = 0.5f;

        void Awake()
        {
            // Get the animator
            if (animator == null) { animator = GetComponent<Animator>(); }

            // Check if raycasting is available
            if (gameObject.GetComponent<Image>() == null)
            {
                Image raycastImg = gameObject.AddComponent<Image>();
                raycastImg.color = new Color(0, 0, 0, 0);
                raycastImg.raycastTarget = true;
            }

            // Get the animator state length
            cachedStateLength = DreamOSInternalTools.GetAnimatorClipLength(animator, "DateAndTimeTaskbar_In") + 0.1f;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            animator.enabled = true;
            animator.Play("In");

            StopCoroutine("DisableAnimator");
            StartCoroutine("DisableAnimator");
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            animator.enabled = true;
            animator.Play("Out");

            StopCoroutine("DisableAnimator");
            StartCoroutine("DisableAnimator");
        }

        IEnumerator DisableAnimator()
        {
            yield return new WaitForSeconds(cachedStateLength + 0.1f);
            animator.enabled = false;
        }
    }
}