using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// A single notification card, either a standard one in the notification center or a popup toast.
    /// Replaces DreamOS's NotificationItem (2026-10-19): same GUID, serialized fields and public API.
    ///
    /// The card plays its "In" and "Out" animator states; the animator is switched off once a state has finished so
    /// it does not tick for the lifetime of the card. Popups close themselves after a duration; closing destroys the
    /// card when the "Out" state ends.
    /// </summary>
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

        NotificationItemParent nip;
        float cachedAnimatorLength = 0.5f;
        bool closeInProgress = false;
        bool waitingToBeEnabled = false;

        Coroutine disableAnimatorRoutine;
        Coroutine destroyRoutine;
        Coroutine popupRoutine;

        void Awake()
        {
            if (animator != null) { cachedAnimatorLength = DreamOSInternalTools.GetAnimatorClipLength(animator, stateID) + 0.01f; }
        }

        void OnEnable()
        {
            // Open() was requested while the card was inactive: run it now
            if (waitingToBeEnabled)
            {
                waitingToBeEnabled = false;
                Open();
            }
        }

        void OnDisable()
        {
            // The destroy timer dies with a disabled object, so finish the close here
            if (closeInProgress) { Destroy(gameObject); }
        }

        public void Open()
        {
            if (nip == null) { nip = gameObject.GetComponentInParent<NotificationItemParent>(); }
            if (!gameObject.activeInHierarchy) { waitingToBeEnabled = true; return; }

            Stop(ref disableAnimatorRoutine);
            Stop(ref destroyRoutine);
            disableAnimatorRoutine = StartCoroutine(DisableAnimator());

            closeInProgress = false;
            animator.enabled = true;
            animator.Play("In");

            nip.UpdateState();
        }

        public void OpenPopup(float duration)
        {
            Stop(ref disableAnimatorRoutine);
            Stop(ref popupRoutine);

            disableAnimatorRoutine = StartCoroutine(DisableAnimator());
            popupRoutine = StartCoroutine(WaitForPopupDuration(duration));

            animator.enabled = true;
            animator.Play("In");
        }

        public void Close()
        {
            Stop(ref disableAnimatorRoutine);
            Stop(ref destroyRoutine);
            destroyRoutine = StartCoroutine(DestroyObject());

            closeInProgress = true;
            animator.enabled = true;
            animator.Play("Out");
        }

        void Stop(ref Coroutine routine)
        {
            if (routine != null) { StopCoroutine(routine); }
            routine = null;
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

            // Leave the parent first so its child count is right when it refreshes its empty / clear panels
            transform.SetParent(null);
            if (nip != null) { nip.UpdateState(); }

            Destroy(gameObject);
        }
    }
}
