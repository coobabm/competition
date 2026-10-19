using System.Collections;
using LingGuangV05.Desktop.OS;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>
    /// One conversation of the chat app (moved from DreamOS 2026-10-19; same GUID and fields): the contact's picture
    /// and name on top, messages under <see cref="messageParent"/>. Show/Hide play its slide in/out.
    /// </summary>
    public class ChatLayoutPreset : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        public Transform messageParent;
        public Image individualImage;
        public TextMeshProUGUI nameText;
        [HideInInspector] public MessagingApp manager;
        [HideInInspector] public string personName;
        [HideInInspector] public Sprite personPicture;

        float animationLength = 1;
        Coroutine pending;

        void Awake() => animationLength = DreamOSInternalTools.GetAnimatorClipLength(animator, "ChatLayout_In") + 0.1f;

        public void Show()
        {
            gameObject.SetActive(true);
            individualImage.sprite = personPicture;
            nameText.text = personName;

            // The chat app can refresh a layout while its enclosing chat pane is hidden.
            if (!isActiveAndEnabled) return;
            Play("In", AnimatorOff());
        }

        public void Hide() => Play("Out", SwitchOff());

        void Play(string state, IEnumerator after)
        {
            animator.enabled = true;
            animator.Play(state);
            if (pending != null) StopCoroutine(pending);
            pending = StartCoroutine(after);
        }

        IEnumerator SwitchOff()
        {
            yield return new WaitForSeconds(animationLength);
            pending = null;
            gameObject.SetActive(false);
        }

        IEnumerator AnimatorOff()
        {
            yield return new WaitForSeconds(animationLength);
            pending = null;
            animator.enabled = false;
        }
    }
}
