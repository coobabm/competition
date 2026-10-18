using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>
    /// Smooth-scrolls a web page to a point when one of its links asks (moved from DreamOS's samples 2026-10-19; same
    /// GUID and fields). It eases the scrollbar toward the target and switches itself off once within
    /// <see cref="scrollHelper"/> of it.
    /// </summary>
    public class WebsiteScroller : MonoBehaviour
    {
        public Scrollbar scrollbar;
        [Range(0.01f, 0.1f)] public float scrollSmooth = 0.01f;
        [Range(0.005f, 0.1f)] public float scrollHelper = 0.01f;

        float scrollTo;
        float stopAt;
        bool goingUp;

        void Start() => enabled = false;

        void Update()
        {
            if (scrollbar.value != scrollTo) scrollbar.value = Mathf.Lerp(scrollbar.value, scrollTo, scrollSmooth);
            if (goingUp ? scrollbar.value >= stopAt : scrollbar.value <= stopAt) enabled = false;
        }

        public void ScrollTo(float value)
        {
            enabled = true;
            scrollTo = value;
            goingUp = scrollbar.value < scrollTo;
            stopAt = goingUp ? scrollTo - scrollHelper : scrollTo + scrollHelper;
        }
    }
}
