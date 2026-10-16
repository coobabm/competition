using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// The spider's speech bubble (剧情修订 2026-10-16 §二): when it says something, a rounded bubble with a little tail
    /// pops up over it (scale overshoot), follows it while it walks, and fades out after a few seconds. Nothing is shown
    /// while it is silent. Lives on the resident spider's desktop layer; <see cref="XgResidentSpider.Speak"/> drives it.
    /// </summary>
    public sealed class XgSpiderBubble
    {
        const float MaxTextWidth = 220, PadX = 12, PadY = 7, Tail = 9, Corner = 9, Gap = 6;
        const float PopSeconds = .24f, FadeIn = .1f, FadeOut = .35f;
        static readonly Color Back = new Color32(255, 253, 246, 245);
        static readonly Color Rim = new Color32(42, 36, 72, 235);
        static readonly Color Ink = new Color32(40, 34, 70, 255);

        RectTransform root;
        XgCrawlerLayer shape;
        TMP_Text text;
        CanvasGroup group;
        Vector2 size;
        float age = -1, life, tailX;
        bool below;

        /// <summary>A line is on screen (popping, holding or fading).</summary>
        public bool Showing => age >= 0;
        /// <summary>The line on screen (tests and QA).</summary>
        public string Line => text != null ? text.text : "";

        public void Build(RectTransform layer, TMP_FontAsset font)
        {
            if (root != null || layer == null) return;
            var go = new GameObject("Spider Bubble", typeof(RectTransform));
            go.layer = layer.gameObject.layer;
            root = (RectTransform)go.transform;
            root.SetParent(layer, false);
            root.anchorMin = root.anchorMax = new Vector2(.5f, .5f);
            root.pivot = new Vector2(.5f, 0);
            group = go.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false; group.interactable = false;
            var shapeGo = new GameObject("Shape", typeof(RectTransform));
            shapeGo.layer = go.layer;
            var shapeRt = (RectTransform)shapeGo.transform;
            shapeRt.SetParent(root, false);
            shapeRt.anchorMin = Vector2.zero; shapeRt.anchorMax = Vector2.one; shapeRt.offsetMin = shapeRt.offsetMax = Vector2.zero;
            shapeGo.AddComponent<CanvasRenderer>();
            shape = shapeGo.AddComponent<XgCrawlerLayer>();
            shape.raycastTarget = false;
            shape.color = Color.white;
            shape.draw = Draw;
            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.layer = go.layer;
            var textRt = (RectTransform)textGo.transform;
            textRt.SetParent(root, false);
            textRt.anchorMin = Vector2.zero; textRt.anchorMax = Vector2.one;
            text = textGo.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.fontSize = 15; text.color = Ink; text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false; text.richText = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            go.SetActive(false);
        }

        /// <summary>Pops the bubble up with <paramref name="line"/> for <paramref name="seconds"/>.</summary>
        public void Say(string line, float seconds)
        {
            if (root == null || string.IsNullOrEmpty(line)) return;
            bool fresh = age < 0 || age > PopSeconds;
            text.text = line;
            var pref = text.GetPreferredValues(line, MaxTextWidth, 0);
            size = new Vector2(Mathf.Min(MaxTextWidth, pref.x) + PadX * 2, pref.y + PadY * 2);
            life = Mathf.Max(1.5f, seconds);
            // A new line while one is still up replaces it without a second pop.
            age = fresh ? 0 : Mathf.Min(age, PopSeconds);
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
        }

        public void Hide()
        {
            age = -1;
            if (root != null) root.gameObject.SetActive(false);
        }

        /// <summary>Follows the spider at <paramref name="at"/> (layer coordinates) and animates; call every frame.</summary>
        public void Tick(float dt, Vector2 at, float spiderSize, Rect screen)
        {
            if (root == null || age < 0) return;
            age += dt;
            if (age >= life) { Hide(); return; }
            // Above the spider, or below it near the top edge; kept inside the screen, the tail still pointing at it.
            float lift = spiderSize * .55f + Gap;
            float total = size.y + Tail;
            below = at.y + lift + total > screen.yMax - 4;
            float x = Mathf.Clamp(at.x, screen.xMin + size.x / 2 + 4, screen.xMax - size.x / 2 - 4);
            float y = below ? at.y - lift - total : at.y + lift;
            root.sizeDelta = new Vector2(size.x, total);
            root.anchoredPosition = new Vector2(x, y);
            tailX = Mathf.Clamp(at.x - x, -size.x / 2 + Corner + Tail, size.x / 2 - Corner - Tail);
            var textRt = text.rectTransform;
            textRt.offsetMin = new Vector2(PadX, (below ? 0 : Tail) + PadY);
            textRt.offsetMax = new Vector2(-PadX, -(below ? Tail : 0) - PadY);
            // Pop: a quick overshoot, then hold; fade at the end.
            float p = Mathf.Clamp01(age / PopSeconds);
            float back = 1.70158f;
            float pop = 1 + (back + 1) * Mathf.Pow(p - 1, 3) + back * Mathf.Pow(p - 1, 2);
            float s = Mathf.LerpUnclamped(.55f, 1, pop);
            root.localScale = new Vector3(s, s, 1);
            group.alpha = Mathf.Min(Mathf.Clamp01(age / FadeIn), Mathf.Clamp01((life - age) / FadeOut));
            shape.Redraw();
        }

        void Draw(VertexHelper vh)
        {
            var r = shape.rectTransform.rect;
            float bodyMin = below ? r.yMax - Tail - size.y : r.yMin + Tail, bodyMax = bodyMin + size.y;
            DrawBody(vh, new Vector2(r.xMin, bodyMin), new Vector2(r.xMax, bodyMax), 1.6f);
        }

        void DrawBody(VertexHelper vh, Vector2 min, Vector2 max, float rim)
        {
            // Rim first (a slightly bigger body and tail), then the paper on top.
            Rounded(vh, min - Vector2.one * rim, max + Vector2.one * rim, Corner + rim, Rim);
            TailShape(vh, min, max, rim, Rim);
            Rounded(vh, min, max, Corner, Back);
            TailShape(vh, min, max, 0, Back);
        }

        void TailShape(VertexHelper vh, Vector2 min, Vector2 max, float grow, Color color)
        {
            float cx = (min.x + max.x) / 2 + tailX;
            float baseY = below ? max.y - 1 : min.y + 1;
            float tipY = below ? max.y + Tail + grow : min.y - Tail - grow;
            float w = Tail * .75f + grow;
            XgDraw.Poly(vh, new[] { new Vector2(cx - w, baseY), new Vector2(cx + w, baseY), new Vector2(cx + (below ? 2 : -2), tipY) }, color);
        }

        static void Rounded(VertexHelper vh, Vector2 min, Vector2 max, float radius, Color color)
        {
            radius = Mathf.Min(radius, (max.x - min.x) / 2, (max.y - min.y) / 2);
            XgDraw.Box(vh, new Vector2(min.x + radius, min.y), new Vector2(max.x - radius, max.y), color);
            XgDraw.Box(vh, new Vector2(min.x, min.y + radius), new Vector2(min.x + radius, max.y - radius), color);
            XgDraw.Box(vh, new Vector2(max.x - radius, min.y + radius), new Vector2(max.x, max.y - radius), color);
            const float Q = Mathf.PI / 2;
            XgDraw.Pie(vh, new Vector2(max.x - radius, max.y - radius), radius, 0, Q, color, 8);
            XgDraw.Pie(vh, new Vector2(min.x + radius, max.y - radius), radius, Q, 2 * Q, color, 8);
            XgDraw.Pie(vh, new Vector2(min.x + radius, min.y + radius), radius, 2 * Q, 3 * Q, color, 8);
            XgDraw.Pie(vh, new Vector2(max.x - radius, min.y + radius), radius, 3 * Q, 4 * Q, color, 8);
        }
    }
}
