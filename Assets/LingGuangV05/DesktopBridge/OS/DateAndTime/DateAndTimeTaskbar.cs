using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// Hover reveal for the taskbar clock (2026-10-19). Replaces DreamOS's DateAndTimeTaskbar: same GUID, serialized
    /// field and behaviour.
    ///
    /// Plays "In" when the pointer enters and "Out" when it leaves, then switches the animator off once the clip has
    /// run. A transparent raycast image is added if the object has none so the hover is detected at all.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public class DateAndTimeTaskbar : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Animator animator;
        float cachedStateLength = 0.5f;

        void Awake()
        {
            if (animator == null) { animator = GetComponent<Animator>(); }

            if (GetComponent<Image>() == null)
            {
                Image raycastImage = gameObject.AddComponent<Image>();
                raycastImage.color = new Color(0, 0, 0, 0);
                raycastImage.raycastTarget = true;
            }

            cachedStateLength = DreamOSInternalTools.GetAnimatorClipLength(animator, "DateAndTimeTaskbar_In") + 0.1f;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            PlayState("In");
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            PlayState("Out");
        }

        void PlayState(string state)
        {
            animator.enabled = true;
            animator.Play(state);

            StopCoroutine(nameof(DisableAnimator));
            StartCoroutine(nameof(DisableAnimator));
        }

        IEnumerator DisableAnimator()
        {
            yield return new WaitForSeconds(cachedStateLength + 0.1f);
            animator.enabled = false;
        }
    }
}
