using LingGuangV05.Desktop.XingGuang;
using UnityEngine;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// Plays the live origin film trial full screen on its own overlay canvas, outside the story flow (the editor menu
    /// Tools / LingGuang / Origin live – preview opens it). Esc ends it; it removes itself when the film is over.
    /// </summary>
    public sealed class OriginLivePreview : MonoBehaviour
    {
        static OriginLivePreview instance;
        OriginLive live;

        public static void Open()
        {
            if (instance != null) return;
            var go = new GameObject("Origin Live Preview", typeof(Canvas));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = short.MaxValue;
            var preview = go.AddComponent<OriginLivePreview>();
            instance = preview;
            preview.live = OriginLive.Create((RectTransform)go.transform);
            var lab = FindAnyObjectByType<XingGuangController>();
            preview.live.Bind(lab != null ? lab.Sim : null);
            preview.live.Begin();
        }

        void Update()
        {
            if (live == null || live.Ended) { instance = null; Destroy(gameObject); }
        }

        void OnDestroy() { if (instance == this) instance = null; }
    }
}
