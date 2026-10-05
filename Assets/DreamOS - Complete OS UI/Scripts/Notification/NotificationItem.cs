using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Michsky.DreamOS
{
    public class NotificationItem : MonoBehaviour
    {
        [Header("Resources")]
        [SerializeField] private Animator animator;
        public Transform buttonParent;
        public Image iconObject;
        public TextMeshProUGUI titleObject;
        public TextMeshProUGUI descriptionObject;

        [Header("Settings")]
        [SerializeField] private string stateID = "StandardNotification_In";

        // Helpers
        NotificationItemParent nip;
        float cachedAnimatorLength = 0.5f;
        bool closeInProgress = false;
        bool waitingToBeEnabled = false;

        void Awake()
        {
            // Cache the anim state length
            if (animator != null) { cachedAnimatorLength = DreamOSInternalTools.GetAnimatorClipLength(animator, stateID) + 0.01f; }
        }

        void OnEnable()
        {
            if (waitingToBeEnabled)
            {
                waitingToBeEnabled = false;
                Open();
            }
        }

        void OnDisable()
        {
            // Destroy object in case if the parent gets disabled during the process
            if (closeInProgress) { Destroy(gameObject); }
        }

        public void Open()
        {
            if (nip == null) { nip = gameObject.GetComponentInParent<NotificationItemParent>(); }
            if (!gameObject.activeInHierarchy) { waitingToBeEnabled = true; return; }

            StopCoroutine("DisableAnimator");
            StopCoroutine("DestroyObject");
            StartCoroutine("DisableAnimator");

            closeInProgress = false;
            animator.enabled = true;
            animator.Play("In");

            nip.UpdateState();
        }

        public void OpenPopup(float duration)
        {
            StopCoroutine("DisableAnimator");
            StopCoroutine("WaitForPopupDuration");

            StartCoroutine("DisableAnimator");
            StartCoroutine("WaitForPopupDuration", duration);

            animator.enabled = true;
            animator.Play("In");
        }

        public void Close()
        {
            StopCoroutine("DisableAnimator");
            StopCoroutine("DestroyObject");
            StartCoroutine("DestroyObject");

            closeInProgress = true;
            animator.enabled = true;
            animator.Play("Out");
        }

        IEnumerator WaitForPopupDuration(float time)
        {
            yield return new WaitForSeconds(time);
            Close();
        }

        IEnumerator DisableAnimator()
        {
            yield return new WaitForSeconds(cachedAnimatorLength);
            animator.enabled = false;
        }

        IEnumerator DestroyObject()
        {
            yield return new WaitForSeconds(cachedAnimatorLength);

            // Set parent as null since we need to update the item parent based on child count
            transform.SetParent(null);

            // Check for item parent
            if (nip != null) { nip.UpdateState(); }

            // Destroy the object
            Destroy(gameObject);
        }
    }
}