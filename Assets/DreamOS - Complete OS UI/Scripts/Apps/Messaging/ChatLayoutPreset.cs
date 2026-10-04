using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Michsky.DreamOS
{
    public class ChatLayoutPreset : MonoBehaviour
    {
        // Resources
        [SerializeField] private Animator animator;
        public Transform messageParent;
        public Image individualImage;
        public TextMeshProUGUI nameText;
        [HideInInspector] public MessagingManager manager;
        [HideInInspector] public string personName;
        [HideInInspector] public Sprite personPicture;

        // Helpers
        float cachedAnimatorLength = 1;

        void Awake()
        {
            cachedAnimatorLength = DreamOSInternalTools.GetAnimatorClipLength(animator, "ChatLayout_In") + 0.1f;
        }

        public void Show()
        {
            gameObject.SetActive(true);

            individualImage.sprite = personPicture;
            nameText.text = personName;

            animator.enabled = true;
            animator.Play("In");

            StopCoroutine("DisableAnimator");
            StopCoroutine("DisableObject");
            StartCoroutine("DisableAnimator");
        }

        public void Hide()
        {
            animator.enabled = true;
            animator.Play("Out");

            StopCoroutine("DisableAnimator");
            StopCoroutine("DisableObject");
            StartCoroutine("DisableObject");
        }

        IEnumerator DisableObject()
        {
            yield return new WaitForSeconds(cachedAnimatorLength);
            gameObject.SetActive(false);
        }

        IEnumerator DisableAnimator()
        {
            yield return new WaitForSeconds(cachedAnimatorLength);
            animator.enabled = false;
        }
    }
}