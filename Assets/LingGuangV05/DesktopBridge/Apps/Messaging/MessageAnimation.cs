using System.Collections;
using LingGuangV05.Desktop.OS;
using UnityEngine;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>
    /// Plays a bubble's pop-in once and then removes its Animator, so finished bubbles cost nothing (moved from
    /// DreamOS 2026-10-19; same GUID and fields).
    /// </summary>
    public class MessageAnimation : MonoBehaviour
    {
        public Animator animator;

        IEnumerator Start()
        {
            yield return new WaitForSeconds(DreamOSInternalTools.GetAnimatorClipLength(animator, "MessageTyping_Start") + 0.1f);
            Destroy(animator);
            Destroy(this);
        }
    }
}
