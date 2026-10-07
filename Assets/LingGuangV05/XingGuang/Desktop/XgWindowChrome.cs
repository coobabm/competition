using HongmengOS.Aero2010;
using Michsky.DreamOS;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// The dark caption of the 灵光.exe window (lingguang-redesign/index.html): a breathing green dot, 「灵光.exe · 深度学习
    /// 工作站」 and dark ─ □ ✕ buttons whose ✕ turns red under the pointer. It restyles only this window's own copy of the
    /// Aero chrome (frame, caption, fine border, status bar); the objects, the dragger and the button wiring stay, so
    /// dragging, minimise, maximise and close work as before. Other windows keep their Aero bars.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class XgWindowChrome : MonoBehaviour
    {
        static readonly Color CloseHot = new Color32(163, 45, 45, 255);

        TMP_Text title, status;
        RectTransform dot;
        XgDotGraphic dotGraphic;

        /// <summary>Restyles the window once; later calls only return the component.</summary>
        public static XgWindowChrome Apply(WindowManager window)
        {
            if (window == null || window.windowContainer == null) return null;
            var container = window.windowContainer;
            var chrome = container.GetComponent<XgWindowChrome>();
            if (chrome != null) return chrome;
            chrome = container.gameObject.AddComponent<XgWindowChrome>();
            chrome.Restyle(container);
            return chrome;
        }

        void Restyle(Transform container)
        {
            Fill(container.Find("Background"), XgDark.Frame);
            var border = container.Find("Aero Fine Border");
            if (border != null) foreach (var line in border.GetComponentsInChildren<Image>(true)) line.color = XgDark.Line;

            var dragger = container.Find("Dragger") as RectTransform;
            if (dragger != null)
            {
                Fill(dragger, XgDark.Frame);
                var icon = dragger.Find("ChapterOneTitleIcon");
                if (icon != null) icon.gameObject.SetActive(false);
                // Hairline under the caption, like the mockup's title border.
                var rule = Rect("Caption Rule", dragger, Vector2.zero, new Vector2(1, 0), new Vector2(-7, 0), new Vector2(7, 1));
                Panel(rule, XgDark.Hairline).raycastTarget = false;
                dot = Rect("Breathing Dot", dragger, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(10, -5), new Vector2(20, 5));
                dot.gameObject.AddComponent<CanvasRenderer>();
                var disc = dot.gameObject.AddComponent<XgDotGraphic>();
                disc.raycastTarget = false; disc.color = XgDark.Good;
                dotGraphic = disc;
                title = dragger.Find("ChapterOneTitle")?.GetComponent<TMP_Text>();
                if (title != null)
                {
                    var rt = title.rectTransform;
                    rt.offsetMin = new Vector2(28, rt.offsetMin.y);
                    title.fontSize = 14; title.color = XgDark.Muted; title.richText = true;
                    title.alignment = TextAlignmentOptions.MidlineLeft;
                    title.textWrappingMode = TextWrappingModes.NoWrap; title.overflowMode = TextOverflowModes.Ellipsis;
                }
                var captions = dragger.Find("ChapterOneCaptionButtons");
                if (captions != null)
                {
                    Caption(captions.Find("ChapterOneMinimize"), "─", XgDark.Panel3, XgDark.Ink);
                    Caption(captions.Find("ChapterOneMaximize"), "□", XgDark.Panel3, XgDark.Ink);
                    Caption(captions.Find("ChapterOneClose"), "×", CloseHot, Color.white);
                }
            }

            var bar = container.Find("Aero Status Bar");
            if (bar != null)
            {
                Fill(bar, XgDark.Panel);
                status = bar.Find("Status")?.GetComponent<TMP_Text>();
                if (status != null) { status.color = XgDark.Muted; status.fontSize = 12; status.richText = true; }
            }
        }

        static void Fill(Transform t, Color color)
        {
            var image = t != null ? t.GetComponent<Image>() : null;
            // Flat: no sprite and no Aero Glass material (its highlight would show through the dark caption).
            if (image != null) { image.color = color; image.sprite = null; image.material = null; }
        }

        static void Caption(Transform button, string glyph, Color hot, Color hotInk)
        {
            if (button == null) return;
            var face = button.GetComponent<Image>();
            if (face != null) face.color = XgDark.Frame;
            // The Aero feedback keeps eating the caption drags (so they never move the window) but stops tinting.
            var aero = button.GetComponent<AeroCaptionFeedback>();
            if (aero != null) aero.face = null;
            var b = button.GetComponent<Button>();
            if (b != null) b.transition = Selectable.Transition.None;
            var labels = button.GetComponentsInChildren<TMP_Text>(true);
            foreach (var label in labels)
            {
                label.color = XgDark.Muted; label.fontSize = 15;
                if (label.name == "Label") label.text = glyph;
            }
            var hover = button.gameObject.AddComponent<XgCaptionHover>();
            hover.face = face; hover.labels = labels; hover.hot = hot; hover.hotInk = hotInk;
        }

        public void SetTitle(string app, string subtitle)
        {
            if (title != null) title.text = app + " <color=#4E6C7C>· " + subtitle + "</color>";
        }

        public void SetStatus(string text) { if (status != null) status.text = text; }

        void Update()
        {
            if (dotGraphic == null) return;
            // Breathes about every three seconds, like the mockup's avatar; never flashes.
            float k = .5f + .5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2 / 3f);
            dotGraphic.Glow = .35f + .65f * k;
        }
    }

    /// <summary>The caption button's hover face: dark at rest, lit (red for ✕) under the pointer, darker while pressed.</summary>
    public sealed class XgCaptionHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public Image face;
        public TMP_Text[] labels;
        public Color hot, hotInk;
        bool over, down;
        public void OnPointerEnter(PointerEventData e) { over = true; Paint(); }
        public void OnPointerExit(PointerEventData e) { over = false; down = false; Paint(); }
        public void OnPointerDown(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) { down = true; Paint(); } }
        public void OnPointerUp(PointerEventData e) { down = false; Paint(); }
        void OnDisable() { over = down = false; Paint(); }
        void Paint()
        {
            if (face != null) face.color = !over ? XgDark.Frame : down ? hot * .8f : hot;
            if (labels != null) foreach (var l in labels) if (l != null) l.color = over ? hotInk : XgDark.Muted;
        }
    }

    /// <summary>A soft round dot with a halo (the title bar's status light and the avatar orb's core).</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgDotGraphic : MaskableGraphic
    {
        float glow = 1;
        /// <summary>Halo strength 0–1.</summary>
        public float Glow { get => glow; set { if (Mathf.Abs(value - glow) < .01f) return; glow = value; SetVerticesDirty(); } }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            float radius = Mathf.Min(r.width, r.height) * .5f;
            var halo = color; halo.a *= .35f * glow;
            XgSoftDraw.Halo(vh, r.center, radius * 1.6f, halo, 20);
            XgDraw.Disc(vh, r.center, radius * .62f, color, 20);
        }
    }
}
