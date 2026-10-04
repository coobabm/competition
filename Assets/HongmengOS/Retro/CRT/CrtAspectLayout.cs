using UnityEngine;
using UnityEngine.EventSystems;

namespace HongmengOS.Retro
{
    /// <summary>Keeps the aperture and every surrounding bevel concentric and physically 4:3.</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class CrtAspectLayout : UIBehaviour
    {
        [SerializeField] private RectTransform monitor;
        [SerializeField] private RectTransform viewport;
        [SerializeField] private RectTransform display;
        [SerializeField] private RectTransform pocket;
        [SerializeField] private RectTransform bezel;
        [SerializeField] private RectTransform gasket;
        [SerializeField] private RectTransform glassBed;
        [SerializeField] private RectTransform crown;
        [SerializeField] private RectTransform lowerLip;
        [SerializeField] private Vector4 displayBox = new Vector4(.030f, .128f, .833f, .949f);
        [SerializeField] private float aspect = 4f / 3f;
        private bool applying;

        public RectTransform Viewport => viewport;
        public RectTransform Display => display;
        public float Aspect => aspect;

        public void Configure(RectTransform owner, RectTransform content, RectTransform screen,
            RectTransform outerPocket, RectTransform darkBezel, RectTransform innerGasket,
            RectTransform bed, RectTransform upperHighlight, RectTransform bottomHighlight)
        {
            monitor = owner;
            viewport = content;
            display = screen;
            pocket = outerPocket;
            bezel = darkBezel;
            gasket = innerGasket;
            glassBed = bed;
            crown = upperHighlight;
            lowerLip = bottomHighlight;
            Apply();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Apply();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            Apply();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            aspect = Mathf.Max(0.25f, aspect);
            // Unity can validate while loading; layout is applied by OnEnable or explicit builder call.
        }
#endif

        public void Apply()
        {
            if (applying || monitor == null || viewport == null) return;
            float width = monitor.rect.width;
            float height = monitor.rect.height;
            if (width < 1f || height < 1f) return;
            applying = true;
            try
            {
                float scale = Mathf.Min(width / 1920f, height / 1080f);
                float availableWidth = (displayBox.z - displayBox.x) * width - 104f * scale;
                float availableHeight = (displayBox.w - displayBox.y) * height - 90f * scale;
                float screenHeight = Mathf.Max(1f, Mathf.Min(availableHeight, availableWidth / aspect));
                Vector2 size = new Vector2(screenHeight * aspect, screenHeight);
                Vector2 center = new Vector2((displayBox.x + displayBox.z) * .5f * width,
                    (displayBox.y + displayBox.w) * .5f * height);
                SetRect(viewport, center, size, width, height);
                SetRect(display, center, size, width, height);
                SetRect(glassBed, center, size + Vector2.one * 5f * scale, width, height);
                SetRect(gasket, center, size + Vector2.one * 18f * scale, width, height);
                SetRect(bezel, center, size + Vector2.one * 46f * scale, width, height);
                SetRect(pocket, center, size + new Vector2(104f, 90f) * scale, width, height);
                SetRect(crown, center + Vector2.up * (size.y * .5f + 21f * scale),
                    new Vector2(size.x - 36f * scale, 1.8f * scale), width, height);
                SetRect(lowerLip, center - Vector2.up * (size.y * .5f + 22f * scale),
                    new Vector2(size.x - 30f * scale, 2.4f * scale), width, height);
            }
            finally { applying = false; }
        }

        private static void SetRect(RectTransform rect, Vector2 center, Vector2 size, float width, float height)
        {
            if (rect == null) return;
            rect.anchorMin = new Vector2((center.x - size.x * .5f) / width, (center.y - size.y * .5f) / height);
            rect.anchorMax = new Vector2((center.x + size.x * .5f) / width, (center.y + size.y * .5f) / height);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
