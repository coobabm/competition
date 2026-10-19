using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// Re-runs a layout group's layout when it is enabled (or on demand through <see cref="FixLayout"/>), which
    /// nested layout groups and content size fitters need after being activated. Replaces DreamOS's LayoutGroupFix
    /// (2026-10-19): same GUID, serialized fields and public API.
    ///
    /// The fix can run at once or after a short real-time delay, and either forces an immediate rebuild or only
    /// marks the layout dirty. In the editor the layout is always rebuilt immediately on enable.
    /// </summary>
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [AddComponentMenu("LingGuang/OS/Layout/Layout Group Fix")]
    public class LayoutGroupFix : MonoBehaviour
    {
        [SerializeField] private bool fixOnEnable = true;
        [SerializeField] private bool fixWithDelay = true;
        [SerializeField] private RebuildMethod rebuildMethod;

        const float FixDelay = 0.025f;

        public enum RebuildMethod { ForceRebuild, MarkRebuild }

        void OnEnable()
        {
#if UNITY_EDITOR
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
            if (Application.isPlaying == false) { return; }
#endif
            if (fixWithDelay == true) { StartCoroutine(RebuildAfterDelay()); }
            else if (fixOnEnable == true) { Rebuild(); }
        }

        public void FixLayout()
        {
            if (fixWithDelay == false) { Rebuild(); }
            else { StartCoroutine(RebuildAfterDelay()); }
        }

        void Rebuild()
        {
            RectTransform target = GetComponent<RectTransform>();

            if (rebuildMethod == RebuildMethod.ForceRebuild) { LayoutRebuilder.ForceRebuildLayoutImmediate(target); }
            else { LayoutRebuilder.MarkLayoutForRebuild(target); }
        }

        IEnumerator RebuildAfterDelay()
        {
            yield return new WaitForSecondsRealtime(FixDelay);
            Rebuild();
        }
    }
}
