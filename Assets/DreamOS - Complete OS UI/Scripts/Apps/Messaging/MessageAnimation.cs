using System.Collections;
using UnityEngine;

namespace Michsky.DreamOS
{
    public class MessageAnimation : MonoBehaviour
    {
        public Animator animator;
        float destroyAfter = 0.5f;

        void Start()
        {
            destroyAfter = DreamOSInternalTools.GetAnimatorClipLength(animator, "MessageTyping_Start") + 0.1f;
            StartCoroutine(DestroyComponents());
        }

        IEnumerator DestroyComponents()
        {
            yield return new WaitForSeconds(destroyAfter);
            Destroy(animator);
            Destroy(this);
        }
    }
}