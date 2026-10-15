using System.Collections.Generic;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 「它在学」 on its YY conversation (它在学_可视化借鉴设计 §2.2, after Transformer Explainer), drawn over YY without
    /// touching YY's own layout:
    /// <list type="bullet">
    /// <item>While it types a reply (YY's 「对方正在输入…」, timed by <see cref="XgYyTalk"/>), a small dark strip at the
    /// right of the tool row, just above the input box, flashes the 3–5 candidates for each next character with
    /// probability bars; the one it typed is green. Early stages are scattered, stages 5–6 sure (XgSim.Candidates.cs).
    /// It plays inside the reply's own pause, never delays the reply, and covers no message.</item>
    /// <item>From stage 5 (「它记住你说过的话」), hovering a word of one of its replies draws purple lines back to the words
    /// of your earlier lines on screen that it attended to; thicker = more attention.</item>
    /// </list>
    /// Reduced motion: the strip steps slower and without the flash.
    /// </summary>
    public sealed class XgYyLens : MonoBehaviour
    {
        const string Id = YYChatHub.LingGuangId;
        const int MaxCells = 5, MaxLinks = 4, EarlierLines = 6;
        const float CellW = 50, CellGap = 4, StripPad = 4, Hold = .6f, Fade = .3f;
        static readonly Color Purple = new Color32(180, 130, 255, 255);

        static XgYyLens instance;

        XingGuangController controller;
        YYChatView shownIn;
        RectTransform tools, messages, viewport, content;
        // B: the candidate strip.
        RectTransform strip;
        CanvasGroup stripGroup;
        XgCandidateStrip bars;
        readonly TMP_Text[] glyphs = new TMP_Text[MaxCells], percents = new TMP_Text[MaxCells];
        List<XgCandidateStep> steps;
        float startAt, perStep, endAt = -1;
        int shownStep = -1;
        // C: the attention lines.
        XgAttentionOverlay overlay;
        readonly TMP_Text[] weights = new TMP_Text[MaxLinks];
        readonly List<TMP_Text> bubbles = new List<TMP_Text>();
        int bubbleCount = -1;
        float bubblesAt;
        string hoverKey = "";
        readonly List<XgAttentionLink> links = new List<XgAttentionLink>();
        readonly List<TMP_Text> linkTexts = new List<TMP_Text>();
        TMP_Text hoverText;
        XgWordSpan hoverWord;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        internal static void Attach()
        {
            var runtime = FindAnyObjectByType<ChapterOneRuntime>();
            if (runtime == null || runtime.GetComponent<XgYyLens>() != null) return;
            runtime.gameObject.AddComponent<XgYyLens>();
        }

        void Awake() { instance = this; }
        void OnDestroy() { if (instance == this) instance = null; }

        static bool ReduceMotion => PlayerPrefs.GetInt(StoryDesktopPresenter.ReduceMotionPref, 0) == 1;

        /// <summary>
        /// A reply is on its way and will show at <paramref name="deliverAt"/> (unscaled time): play its candidates in the
        /// time left, at least ~0.9 s, a step every 0.05–0.16 s. Called by <see cref="XgYyTalk"/>.
        /// </summary>
        public static void Typing(XgSim sim, string reply, float deliverAt)
        {
            if (instance == null || sim == null || string.IsNullOrEmpty(reply)) return;
            var list = sim.ReplyCandidates(reply);
            if (list.Count == 0) return;
            float now = Time.unscaledTime;
            float window = Mathf.Max(deliverAt - now, .9f);
            float per = Mathf.Clamp(window / list.Count, .05f, .16f);
            if (ReduceMotion) per = Mathf.Max(per, .3f);
            // Never longer than ~2.5 s: a long reply shows its first characters only.
            int fit = Mathf.Max(1, Mathf.FloorToInt(2.5f / per));
            if (list.Count > fit) list.RemoveRange(fit, list.Count - fit);
            instance.steps = list; instance.startAt = now; instance.perStep = per; instance.shownStep = -1;
            instance.endAt = now + per * list.Count + Hold;
        }

        // ───────────── frame ─────────────

        void Update()
        {
            if (controller == null) controller = GetComponent<XingGuangController>();
            var sim = controller != null ? controller.Sim : null;
            var hub = YYChatHub.Instance;
            var view = hub != null ? hub.View : null;
            bool on = sim != null && view != null && hub.S != null && view.IsOpen && hub.S.selected == Id;
            if (on && !Ensure(view)) on = false;
            if (strip != null) UpdateStrip(on);
            if (overlay != null) UpdateAttention(on && sim.AttentionOpen, sim, hub);
        }

        /// <summary>Finds YY's tool row and message area (by the names YYChatView gives them) and builds the strip and the overlay once.</summary>
        bool Ensure(YYChatView view)
        {
            if (shownIn == view && strip != null && overlay != null) return true;
            var root = view.transform.Find("YY2016/Conversation");
            tools = root != null ? root.Find("Tools") as RectTransform : null;
            messages = root != null ? root.Find("Messages") as RectTransform : null;
            viewport = messages != null ? messages.Find("Viewport") as RectTransform : null;
            content = viewport != null ? viewport.Find("Content") as RectTransform : null;
            if (tools == null || content == null) return false;
            shownIn = view;
            TMP_FontAsset font = null;
            foreach (var t in view.GetComponentsInChildren<TMP_Text>(true)) { font = t.font; break; }
            BuildStrip(font);
            BuildOverlay(font);
            return true;
        }

        static TMP_Text Label(RectTransform parent, string name, TMP_FontAsset font, float size, TextAlignmentOptions align)
        {
            var t = XgUi.Rect(name, parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero).gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.fontSize = size; t.alignment = align; t.raycastTarget = false; t.richText = false;
            t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }

        // ───────────── B: next-character candidates ─────────────

        void BuildStrip(TMP_FontAsset font)
        {
            if (strip != null) Destroy(strip.gameObject);
            strip = XgUi.Rect("LingGuangCandidates", tools, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-8, 3), new Vector2(-8, -3));
            stripGroup = strip.gameObject.AddComponent<CanvasGroup>();
            stripGroup.blocksRaycasts = false; stripGroup.interactable = false;
            bars = strip.gameObject.AddComponent<XgCandidateStrip>();
            bars.raycastTarget = false;
            for (int i = 0; i < MaxCells; i++)
            {
                glyphs[i] = Label(strip, "Glyph" + i, font, 14, TextAlignmentOptions.Center);
                percents[i] = Label(strip, "P" + i, font, 10, TextAlignmentOptions.MidlineLeft);
            }
            strip.gameObject.SetActive(false);
        }

        void UpdateStrip(bool on)
        {
            float now = Time.unscaledTime;
            // Off its conversation, or done: the strip goes, and a reply not watched is not replayed later.
            if (!on || steps != null && now >= endAt + Fade) steps = null;
            if (steps == null) { if (strip.gameObject.activeSelf) strip.gameObject.SetActive(false); return; }
            int step = Mathf.Min(steps.Count - 1, Mathf.FloorToInt((now - startAt) / perStep));
            if (!strip.gameObject.activeSelf) { strip.gameObject.SetActive(true); strip.SetAsLastSibling(); }
            stripGroup.alpha = now > endAt ? 1 - (now - endAt) / Fade : 1;
            if (step == shownStep) { bars.Tick(Time.unscaledDeltaTime); return; }
            shownStep = step;
            var options = steps[step].options;
            int cells = Mathf.Min(MaxCells, options.Count);
            // The strip sits right of YY's tool buttons; it narrows its cells rather than cover them.
            float room = Mathf.Max(120, tools.rect.width - 390);
            float cellW = Mathf.Clamp((room - StripPad * 2) / cells - CellGap, 34, CellW);
            float width = StripPad * 2 + cells * (cellW + CellGap) - CellGap;
            strip.offsetMin = new Vector2(-8 - width, 3);
            var ps = new float[cells]; var chosen = new bool[cells];
            for (int i = 0; i < MaxCells; i++)
            {
                bool shown = i < cells;
                glyphs[i].gameObject.SetActive(shown); percents[i].gameObject.SetActive(shown);
                if (!shown) continue;
                var o = options[i];
                ps[i] = (float)o.p; chosen[i] = o.chosen;
                float x = StripPad + i * (cellW + CellGap);
                Place(glyphs[i].rectTransform, x, 0, 16, 1);
                Place(percents[i].rectTransform, x + 17, .45f, cellW - 17, 1);
                glyphs[i].text = o.token;
                glyphs[i].color = o.chosen ? XgDark.Good : XgDark.Ink;
                glyphs[i].fontStyle = o.chosen ? FontStyles.Bold : FontStyles.Normal;
                percents[i].text = N(o.p * 100) + "%";
                percents[i].color = o.chosen ? XgDark.Good : XgDark.Muted;
            }
            bars.Set(ps, chosen, cellW, CellGap, StripPad, !ReduceMotion);
        }

        static string N(double v) => v.ToString(v < 9.95 ? "0.0" : "0", System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>Places a label in the strip: x and width in pixels, y as a share of the height (from yMin to yMax).</summary>
        static void Place(RectTransform rt, float x, float yMin, float w, float yMax)
        {
            rt.anchorMin = new Vector2(0, yMin); rt.anchorMax = new Vector2(0, yMax);
            rt.offsetMin = new Vector2(x, 0); rt.offsetMax = new Vector2(x + w, 0);
        }

        // ───────────── C: attention lines ─────────────

        void BuildOverlay(TMP_FontAsset font)
        {
            if (overlay != null) Destroy(overlay.gameObject);
            var rt = XgUi.Rect("LingGuangAttention", messages, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            overlay = rt.gameObject.AddComponent<XgAttentionOverlay>();
            overlay.raycastTarget = false;
            for (int i = 0; i < MaxLinks; i++)
            {
                weights[i] = Label(rt, "Weight" + i, font, 10, TextAlignmentOptions.BottomLeft);
                weights[i].color = new Color32(124, 82, 214, 255);
                weights[i].gameObject.SetActive(false);
            }
            bubbleCount = -1;
        }

        void UpdateAttention(bool on, XgSim sim, YYChatHub hub)
        {
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (!on || mouse == null) { Clear(); return; }
            if (overlay.transform.GetSiblingIndex() != messages.childCount - 1) overlay.transform.SetAsLastSibling();
            var canvas = overlay.canvas != null ? overlay.canvas.rootCanvas : null;
            var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector2 pointer = mouse.position.ReadValue();
            if (!RectTransformUtility.RectangleContainsScreenPoint(viewport, pointer, cam)) { Clear(); return; }
            Collect();
            TMP_Text hovered = null;
            foreach (var t in bubbles) if (t != null && RectTransformUtility.RectangleContainsScreenPoint(t.rectTransform, pointer, cam)) { hovered = t; break; }
            if (hovered == null) { Clear(); return; }
            int ch = TMP_TextUtilities.FindIntersectingCharacter(hovered, pointer, cam, true);
            if (ch < 0) { Clear(); return; }
            string key = hovered.GetInstanceID() + ":" + ch + ":" + bubbleCount + ":" + sim.S.stage;
            if (key != hoverKey) { hoverKey = key; Resolve(sim, hub, hovered, ch); }
            Draw();
        }

        /// <summary>The message bubbles' texts, in order (YYChatView names each bubble's text "Text"); re-read when the list changes.</summary>
        void Collect()
        {
            if (content.childCount == bubbleCount && Time.unscaledTime - bubblesAt < 1) return;
            bubbleCount = content.childCount; bubblesAt = Time.unscaledTime;
            bubbles.Clear();
            foreach (Transform child in content)
                if (child.name == "Text") { var t = child.GetComponent<TMP_Text>(); if (t != null) bubbles.Add(t); }
        }

        static string Norm(string s) => (s ?? "").Replace("​", "").Trim();

        /// <summary>Which reply and word is under the pointer, which of your earlier lines are on screen, and where it looked.</summary>
        void Resolve(XgSim sim, YYChatHub hub, TMP_Text hovered, int ch)
        {
            links.Clear(); linkTexts.Clear(); hoverText = null;
            var conv = hub.Conversation(Id);
            if (conv == null) return;
            string parsed = Norm(hovered.GetParsedText());
            int reply = -1;
            for (int i = conv.messages.Count - 1; i >= 0; i--)
            {
                var m = conv.messages[i];
                if (m.from == Id && m.kind == YYKind.Text && Norm(m.text) == parsed) { reply = i; break; }
            }
            if (reply < 0) return;
            int hoveredAt = bubbles.IndexOf(hovered);
            // Your lines before it (newest first while walking), each matched to the nearest bubble above it that is on screen.
            var texts = new List<string>(); var shown = new List<TMP_Text>();
            int searchFrom = hoveredAt - 1;
            for (int i = reply - 1; i >= 0 && texts.Count < EarlierLines; i--)
            {
                var m = conv.messages[i];
                if (m.from != YYChatHub.Me || m.kind != YYKind.Text || !string.IsNullOrEmpty(m.sticker)) continue;
                string want = Norm(m.text);
                for (int b = searchFrom; b >= 0; b--)
                {
                    if (Norm(bubbles[b].GetParsedText()) != want) continue;
                    searchFrom = b - 1;
                    if (OnScreen(bubbles[b])) { texts.Insert(0, Norm(bubbles[b].GetParsedText())); shown.Insert(0, bubbles[b]); }
                    break;
                }
            }
            if (texts.Count == 0) return;
            var words = sim.ReplyWords(parsed, texts);
            // TMP's character index is the parsed text's index for plain text (its replies carry no markup).
            foreach (var w in words)
            {
                if (!w.Contains(ch)) continue;
                hoverWord = w;
                hoverText = hovered;
                foreach (var l in sim.ReplyAttention(parsed.Substring(w.start, w.length), texts)) { links.Add(l); linkTexts.Add(shown[l.message]); }
                break;
            }
        }

        bool OnScreen(TMP_Text t)
        {
            var a = new Vector3[4]; var b = new Vector3[4];
            t.rectTransform.GetWorldCorners(a); viewport.GetWorldCorners(b);
            return a[1].y > b[0].y && a[0].y < b[1].y;
        }

        void Draw()
        {
            if (hoverText == null) { overlay.Set(null); HideWeights(); return; }
            var from = Box(hoverText, hoverWord.start, hoverWord.length);
            var lines = new List<XgAttentionOverlay.Line>();
            for (int i = 0; i < MaxLinks; i++)
            {
                bool on = i < links.Count && linkTexts[i] != null;
                weights[i].gameObject.SetActive(on);
                if (!on) continue;
                var l = links[i];
                var to = Box(linkTexts[i], l.start, l.length);
                lines.Add(new XgAttentionOverlay.Line { to = to, weight = (float)l.weight, strength = Mathf.Clamp01((float)l.score), remembered = l.remembered });
                var rt = weights[i].rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
                rt.offsetMin = new Vector2(to.xMax + 2, to.yMax - 4); rt.offsetMax = new Vector2(to.xMax + 60, to.yMax + 10);
                weights[i].text = Mathf.RoundToInt((float)l.weight * 100) + "%" + (l.remembered ? GameText.T(" 记得", " noted") : "");
            }
            overlay.Set(lines, from);
        }

        /// <summary>A span's box in the overlay's space (its first line only, if it wraps).</summary>
        Rect Box(TMP_Text t, int start, int length)
        {
            var info = t.textInfo;
            if (info == null || info.characterCount == 0) return new Rect();
            int first = Mathf.Clamp(start, 0, info.characterCount - 1), last = Mathf.Clamp(start + length - 1, first, info.characterCount - 1);
            int line = info.characterInfo[first].lineNumber;
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
            for (int i = first; i <= last; i++)
            {
                var c = info.characterInfo[i];
                if (c.lineNumber != line || !c.isVisible) continue;
                var lineInfo = info.lineInfo[line];
                min = Vector2.Min(min, new Vector2(c.bottomLeft.x, lineInfo.descender));
                max = Vector2.Max(max, new Vector2(c.topRight.x, lineInfo.ascender));
            }
            if (min.x > max.x) return new Rect();
            var to = (RectTransform)overlay.transform;
            Vector2 a = to.InverseTransformPoint(t.transform.TransformPoint(min)), b = to.InverseTransformPoint(t.transform.TransformPoint(max));
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        void HideWeights() { foreach (var w in weights) if (w != null && w.gameObject.activeSelf) w.gameObject.SetActive(false); }

        void Clear()
        {
            if (hoverKey.Length == 0 && hoverText == null) return;
            hoverKey = ""; hoverText = null; links.Clear(); linkTexts.Clear();
            if (overlay != null) overlay.Set(null);
            HideWeights();
        }
    }

    /// <summary>
    /// The candidate strip's drawing: a dark scanner chip and, per candidate, a thin probability bar; the typed one in
    /// green with a soft glow that flashes as the step lands. Bars ease to their new length (unless motion is off).
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgCandidateStrip : MaskableGraphic
    {
        float[] target = new float[0], shown = new float[0];
        bool[] chosen = new bool[0];
        float cellW = 50, gap = 4, pad = 4, flash;
        bool motion = true;

        public void Set(float[] p, bool[] isChosen, float cellWidth, float cellGap, float padding, bool animate)
        {
            var old = shown;
            target = p; chosen = isChosen; cellW = cellWidth; gap = cellGap; pad = padding; motion = animate;
            shown = new float[p.Length];
            for (int i = 0; i < p.Length; i++) shown[i] = motion && i < old.Length ? old[i] : p[i];
            flash = motion ? 1 : 0;
            SetVerticesDirty();
        }

        public void Tick(float dt)
        {
            bool moving = flash > 0;
            flash = Mathf.Max(0, flash - dt * 5);
            for (int i = 0; i < shown.Length; i++)
            {
                float next = Mathf.MoveTowards(shown[i], target[i], dt * 6);
                if (next != shown[i]) { shown[i] = next; moving = true; }
            }
            if (moving) SetVerticesDirty();
        }

        static Color A(Color c, float a) { c.a = a; return c; }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            if (r.width < 10) return;
            XgDraw.Box(vh, r.min, r.max, A(XgDark.Line, .9f));
            XgDraw.Box(vh, r.min + Vector2.one, r.max - Vector2.one, A(XgDark.Card, .96f));
            float barY = r.yMin + 5, barH = 4;
            for (int i = 0; i < shown.Length; i++)
            {
                float x = r.xMin + pad + i * (cellW + gap);
                var trackMin = new Vector2(x + 17, barY);
                float trackW = cellW - 19;
                if (chosen[i]) XgSoftDraw.Disc(vh, new Vector2(x + 8, r.center.y), 9, 6 + 4 * flash, A(XgDark.Good, .16f + .3f * flash));
                XgDraw.Box(vh, trackMin, trackMin + new Vector2(trackW, barH), XgDark.Track);
                var col = chosen[i] ? XgDark.Good : XgDark.Muted;
                XgDraw.Box(vh, trackMin, trackMin + new Vector2(trackW * Mathf.Clamp01(shown[i]), barH), col);
            }
        }
    }

    /// <summary>
    /// The attention lines over YY's messages: the hovered word gets a soft purple box, each word it attended to a box
    /// as strong as the match, and a curve between them as thick as its share of attention (a glow under a core, all
    /// vertex alpha). A remembered word (a memory-book note holds it) gets a second ring of glow.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgAttentionOverlay : MaskableGraphic
    {
        public struct Line { public Rect to; public float weight, strength; public bool remembered; }

        static readonly Color Glow = new Color32(180, 130, 255, 255), Core = new Color32(124, 82, 214, 255);
        List<Line> lines;
        Rect from;

        public void Set(List<Line> list, Rect word = default)
        {
            if (list == null && lines == null) return;
            lines = list; from = word;
            SetVerticesDirty();
        }

        static Color A(Color c, float a) { c.a = a; return c; }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (lines == null || lines.Count == 0 || from.width <= 0) return;
            var start = new Vector2(from.center.x, from.yMax + 1);
            foreach (var l in lines)
            {
                if (l.to.width <= 0) continue;
                var end = new Vector2(l.to.center.x, l.to.yMin - 1);
                float lift = Mathf.Max(18, Mathf.Abs(end.y - start.y) * .45f);
                var c1 = start + new Vector2(0, lift); var c2 = end - new Vector2(0, lift);
                float core = 1 + 5 * l.weight, a = .35f + .6f * l.strength;
                var prev = start;
                const int N = 20;
                for (int i = 1; i <= N; i++)
                {
                    var p = XgSoftDraw.Bezier(start, c1, c2, end, i / (float)N);
                    XgSoftDraw.Seg(vh, prev, p, core + 3, 3, A(Glow, .22f * a));
                    XgDraw.Seg(vh, prev, p, core, A(Core, a));
                    prev = p;
                }
                Mark(vh, l.to, .18f + .3f * l.strength);
                if (l.remembered) XgSoftDraw.Disc(vh, new Vector2(l.to.xMin - 4, l.to.center.y), 3, 4, A(Glow, .9f));
            }
            Mark(vh, from, .35f);
        }

        static void Mark(VertexHelper vh, Rect r, float a)
        {
            XgDraw.Box(vh, r.min - new Vector2(2, 1), r.max + new Vector2(2, 1), A(Glow, a));
            XgDraw.Box(vh, new Vector2(r.xMin - 2, r.yMin - 2), new Vector2(r.xMax + 2, r.yMin - 1), A(Core, .9f));
        }
    }
}
