using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

using LingGuangV05.Desktop.Story;
namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 纠错的瞬间 (它在学_可视化借鉴设计 §2.4, after micrograd): when the player corrects the AI's wrong guess while teaching
    /// (XgSim counts it in teachingCorrections), a small corner panel in 灵光.exe plays ~1.2 s on the microscope's net:
    /// a red dot at the output 「错 −1」, red flowing back along the links with each hidden unit's real share of the blame
    /// (「该怪它 0.31」), then the links' widths shifting with the step. The numbers come from one real backward pass of
    /// XgTinyNet (XgTinyLook.Correction). It never blocks: it waits for the real-training panel and the insight card,
    /// a click anywhere on it closes it, it closes itself a few seconds later, and reduced motion shows the end at once.
    /// The sheet's ✓/× is untouched; the corrected card simply becomes training data as before.
    /// </summary>
    public sealed class XgGradFlowPanel : MonoBehaviour
    {
        const float W = 400, H = 240, Hold = 3.2f, FadeSeconds = .3f, StaleSeconds = 30;
        const int Units = 4;

        XingGuangView view;
        XgRealTrainPanel real;
        XgUi ui;
        RectTransform panel;
        CanvasGroup group;
        XgGradFlowGraphic graph;
        TMP_Text title, caption, footer, wrong, answer;
        readonly TMP_Text[] blame = new TMP_Text[Units];
        XgBtn skip;
        XgSim bound;
        int seen;
        XgTaughtCard pending;
        float pendingAt, t = -1, closeAt = -1;
        XgGradLesson lesson;

        public static XgGradFlowPanel Install(XingGuangView view, RectTransform root, TMP_FontAsset font, XgRealTrainPanel real)
        {
            var p = view.gameObject.GetComponent<XgGradFlowPanel>() ?? view.gameObject.AddComponent<XgGradFlowPanel>();
            p.view = view; p.real = real;
            p.ui = new XgUi(font, view.Controller != null ? view.Controller.Window : null, true);
            p.Build(root);
            return p;
        }

        public bool Showing => panel != null && panel.gameObject.activeSelf;

        static bool ReduceMotion => PlayerPrefs.GetInt(StoryDesktopPresenter.ReduceMotionPref, 0) == 1;

        void Build(RectTransform root)
        {
            panel = Rect("Correction Flow", root, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-W - 16, XgRealTrainPanel.Bottom), new Vector2(-16, XgRealTrainPanel.Bottom + H));
            var bg = Panel(panel, XgDark.Bad);
            // The whole panel is the skip button.
            var b = panel.gameObject.AddComponent<Button>();
            b.targetGraphic = bg; b.transition = Selectable.Transition.None;
            b.onClick.AddListener(Skip);
            var inner = Rect("Inner", panel, Vector2.zero, Vector2.one, new Vector2(1, 1), new Vector2(-1, -1));
            Panel(inner, XgDark.Card).raycastTarget = false;
            group = panel.gameObject.AddComponent<CanvasGroup>();
            var head = Rect("Header", inner, new Vector2(0, 1), Vector2.one, new Vector2(0, -26), Vector2.zero);
            Panel(head, XgDark.Hud).raycastTarget = false;
            title = ui.Text(Rect("Title", head, Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-70, 0)), "", 13, Color.white, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold;
            skip = ui.Button(head, "", Skip, 12);
            skip.rt.gameObject.name = "SkipCorrectionFlow";
            PlaceTopRight(skip, 64, 3, 58, 20);

            var area = TopLeft("Net", inner, 12, 32, XgGradFlowGraphic.Width, XgGradFlowGraphic.Height);
            graph = area.gameObject.AddComponent<XgGradFlowGraphic>();
            graph.raycastTarget = false;
            for (int j = 0; j < Units; j++)
            {
                blame[j] = ui.Text(TopLeft("Blame" + j, area, XgGradFlowGraphic.UnitX + XgGradFlowGraphic.UnitSide + 6, XgGradFlowGraphic.UnitY(j) - 1, 110, 14), "", 10, XgDark.Bad, TextAlignmentOptions.MidlineLeft);
                blame[j].textWrappingMode = TextWrappingModes.NoWrap;
            }
            wrong = ui.Text(TopLeft("Wrong", area, XgGradFlowGraphic.OutX - 40, XgGradFlowGraphic.OutY - 42, 80, 20), "", 15, XgDark.Bad, TextAlignmentOptions.Center);
            wrong.fontStyle = FontStyles.Bold;
            answer = ui.Text(TopLeft("Answer", area, XgGradFlowGraphic.OutX - 46, XgGradFlowGraphic.OutY + 18, 92, 30), "", 10, XgDark.Muted, TextAlignmentOptions.Top);
            caption = ui.Text(Rect("Caption", inner, Vector2.zero, new Vector2(1, 0), new Vector2(10, 20), new Vector2(-10, 54)), "", 12, XgDark.Ink, TextAlignmentOptions.TopLeft);
            caption.enableAutoSizing = true; caption.fontSizeMin = 9; caption.fontSizeMax = 12;
            footer = ui.Text(Rect("Footer", inner, Vector2.zero, new Vector2(1, 0), new Vector2(10, 3), new Vector2(-10, 19)), "", 10, XgDark.Dim, TextAlignmentOptions.MidlineLeft);
            footer.enableAutoSizing = true; footer.fontSizeMin = 8; footer.fontSizeMax = 10;
            UiTip.Add(panel, "你纠正它的那一下：误差从输出往回传，每个隐藏单元分到它该担的一份，每条线按这份挪一点。点一下关掉。",
                "The moment you corrected it: the error flows back from the output, each hidden unit takes its share, and every link moves by its share. Click to close.");
            panel.gameObject.SetActive(false);
        }

        void Skip() { if (Showing && closeAt < 0) closeAt = Time.unscaledTime; }

        bool InsightCardUp { get { var c = view.GetComponent<XgHoloCard>(); return c != null && c.Showing; } }

        void Update()
        {
            if (view == null) return;
            var sim = view.Sim;
            if (sim != bound)
            {
                // A new or loaded game: only corrections from now on.
                bound = sim; pending = null;
                seen = sim != null && sim.S != null ? sim.S.teachingCorrections : 0;
            }
            if (bound == null || bound.S == null) return;
            int now = bound.S.teachingCorrections;
            if (now != seen)
            {
                var list = bound.S.recentCorrections;
                if (now > seen && list != null && list.Count > 0) { pending = list[list.Count - 1]; pendingAt = Time.unscaledTime; }
                seen = now;
            }
            if (!Showing)
            {
                if (pending == null) return;
                if (Time.unscaledTime - pendingAt > StaleSeconds) { pending = null; return; }
                if (view.Visible && (real == null || !real.Showing) && !InsightCardUp && !(view.Teach?.DeskGameVisible ?? false)) { Begin(pending); pending = null; }
                return;
            }
            if (!view.Visible) return; // paused while the window is hidden
            if (view.Teach?.DeskGameVisible ?? false) { panel.gameObject.SetActive(false); lesson = null; return; }
            float clock = Time.unscaledTime;
            if (real != null && real.Showing && closeAt < 0) closeAt = clock; // a real run takes the corner
            if (closeAt >= 0)
            {
                float k = Mathf.Clamp01((clock - closeAt) / FadeSeconds);
                group.alpha = 1 - k;
                if (k >= 1) { panel.gameObject.SetActive(false); lesson = null; }
                return;
            }
            group.alpha = Mathf.Min(1, group.alpha + Time.unscaledDeltaTime / FadeSeconds);
            t += Time.unscaledDeltaTime;
            Show();
            if (t > XgGradFlowGraphic.Seconds + Hold || pending != null && t > XgGradFlowGraphic.Seconds + .6f) closeAt = clock;
        }

        void Begin(XgTaughtCard card)
        {
            lesson = XgTinyLook.Correction(card.seed, card.truth);
            closeAt = -1;
            bool reduced = ReduceMotion || view.Juice != null && view.Juice.Reduced;
            // Reduced motion: no flow, the end state at once.
            t = reduced ? XgGradFlowGraphic.Seconds : 0;
            graph.Set(lesson);
            string q = card.question ?? "";
            if (q.Length > 24) q = q.Substring(0, 24) + "…";
            title.text = T("纠错 · 误差往回传", "Correction · the error flows back");
            skip.Set(T("关闭", "Close"), true);
            caption.text = T("你纠正了它：「" + q + "」。红色从输出往回流，每个隐藏单元上的数是它该担的那份错；每条线按自己那份挪一点。",
                "You corrected it: \"" + q + "\". Red flows back from the output; the number at each hidden unit is its share of the error, and every link moves by its own share.");
            footer.text = T("显微镜那张小网上一次真的反向传播（" + lesson.before.ParameterCount + " 个参数），左边是这道题的 8×8 替身。",
                "One real backward pass on the microscope's small net (" + lesson.before.ParameterCount + " parameters); on the left, an 8×8 stand-in for the question.");
            double p = lesson.grad.output;
            answer.text = T("它答 ", "It said ") + (p > .5 ? T("是 0", "a 0") : T("不是 0", "not a 0")) + " " + N(p * 100, "0") + "%"
                + "\n" + T("应该是 ", "should be ") + (lesson.sample.label ? T("是", "yes") : T("不是", "no"));
            panel.gameObject.SetActive(true);
            panel.SetAsLastSibling();
            group.alpha = 0;
            view.Juice?.Play(XgJuice.Sfx.Id.Swoosh, .8f, .35f);
            Show();
        }

        void Show()
        {
            if (lesson == null) return;
            graph.Clock = t;
            wrong.text = T("错 −1", "wrong −1");
            wrong.alpha = Mathf.Clamp01(t / .15f);
            // The hidden units' blame appears as the red reaches them.
            float k = Mathf.Clamp01((t - XgGradFlowGraphic.ReachHidden + .05f) / .15f);
            for (int j = 0; j < Units; j++)
            {
                bool on = j < lesson.grad.blame.Length;
                blame[j].gameObject.SetActive(on);
                if (!on) continue;
                blame[j].text = T("该怪它 ", "blame ") + N(System.Math.Abs(lesson.grad.blame[j]), "0.00");
                blame[j].alpha = k * Mathf.Lerp(.55f, 1, graph.Share(j));
            }
            answer.alpha = Mathf.Clamp01(t / .3f);
        }
    }

    /// <summary>
    /// The microscope's net in the moment of a correction: the stand-in picture, 64 links to each of the four hidden
    /// units (8×8 weight heat maps), their links to the answer, and the error flowing back in red. Width follows |w|
    /// (green +, red −) and moves from the weights before the step to the weights after it; red follows |gradient|.
    /// Fixed layout measured from the top-left so the panel can place its labels.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgGradFlowGraphic : MaskableGraphic
    {
        public const float Width = 376, Height = 150, Seconds = 1.2f;
        public const float InX = 4, InY = 43, InCell = 8, UnitX = 170, UnitSide = 30, OutX = 336, OutY = 75, OutR = 12;
        /// <summary>When the red reaches the output dot, the hidden units, the input, and when the widths finish moving.</summary>
        public const float ReachOut = .2f, ReachHidden = .5f, ReachInput = .85f;
        public static float UnitY(int j) => 4 + j * 37.5f;

        XgGradLesson lesson;
        float t;
        double g1Max, g2Max, wMax, w2Max, blameMax;
        readonly double[] inputBlame = new double[XgTinyData.Pixels];
        double inputMax;

        public float Clock { set { t = value; SetVerticesDirty(); } }

        public void Set(XgGradLesson l)
        {
            lesson = l;
            var g = l.grad;
            g1Max = g2Max = wMax = w2Max = blameMax = inputMax = 1e-9;
            foreach (var v in g.w1) g1Max = System.Math.Max(g1Max, System.Math.Abs(v));
            foreach (var v in g.w2) g2Max = System.Math.Max(g2Max, System.Math.Abs(v));
            foreach (var v in g.blame) blameMax = System.Math.Max(blameMax, System.Math.Abs(v));
            for (int i = 0; i < l.before.w1.Length; i++) wMax = System.Math.Max(wMax, System.Math.Max(System.Math.Abs(l.before.w1[i]), System.Math.Abs(l.after.w1[i])));
            for (int j = 0; j < l.before.w2.Length; j++) w2Max = System.Math.Max(w2Max, System.Math.Max(System.Math.Abs(l.before.w2[j]), System.Math.Abs(l.after.w2[j])));
            // How much each pixel is blamed: the error that flows back into it (d loss / d pixel).
            int inputs = l.before.inputs;
            for (int i = 0; i < inputs; i++)
            {
                double s = 0;
                for (int j = 0; j < l.before.hidden; j++) s += g.blame[j] * l.before.w1[j * inputs + i];
                inputBlame[i] = System.Math.Abs(s);
                inputMax = System.Math.Max(inputMax, inputBlame[i]);
            }
            SetVerticesDirty();
        }

        /// <summary>Unit j's share of the blame, 0–1 of the largest.</summary>
        public float Share(int j) => lesson == null || j >= lesson.grad.blame.Length ? 0 : (float)(System.Math.Abs(lesson.grad.blame[j]) / blameMax);

        Rect area;
        Vector2 P(float x, float y) => new Vector2(area.xMin + x, area.yMax - y);
        void Box(VertexHelper vh, float x, float y, float w, float h, Color c) => XgDraw.Box(vh, P(x, y + h), P(x + w, y), c);
        static Color Alpha(Color c, float a) { c.a = a; return c; }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            area = GetPixelAdjustedRect();
            if (lesson == null) return;
            var before = lesson.before; var after = lesson.after; var g = lesson.grad;
            int n = XgTinyData.Side, inputs = before.inputs, units = before.Units;
            float toHidden = Mathf.Clamp01((t - ReachOut) / (ReachHidden - ReachOut));
            float toInput = Mathf.Clamp01((t - ReachHidden) / (ReachInput - ReachHidden));
            float move = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - ReachInput) / (Seconds - ReachInput)));
            // After the flow the red stays, fainter, so the end state still shows where the blame went.
            float red = t <= Seconds ? 1 : Mathf.Lerp(1, .45f, Mathf.Clamp01((t - Seconds) / 1.2f));
            var outC = P(OutX, OutY);
            var pix = lesson.sample.pixels;

            for (int j = 0; j < units; j++)
            {
                var to = P(UnitX - 2, UnitY(j) + UnitSide / 2);
                for (int i = 0; i < inputs; i++)
                {
                    int k = j * inputs + i;
                    double w = before.w1[k] + (after.w1[k] - before.w1[k]) * move;
                    float m = (float)(System.Math.Abs(w) / wMax), q = (float)(System.Math.Abs(g.w1[k]) / g1Max);
                    var from = P(InX + (i % n + .5f) * InCell, InY + (i / n + .5f) * InCell);
                    float lit = Mathf.Lerp(.35f, 1, (float)pix[i]);
                    if (m >= .12f) XgSoftDraw.Seg(vh, from, to, .4f + 1.6f * m, .8f, Alpha(w >= 0 ? XgDark.Good : XgDark.Bad, (.05f + .35f * m * m) * lit));
                    // The red runs backwards: from the unit toward the pixel.
                    if (toInput > 0 && q > .08f) XgSoftDraw.Seg(vh, to, Vector2.Lerp(to, from, toInput), .6f + 1.8f * q, 1, Alpha(XgDark.Bad, .7f * q * red));
                }
            }
            for (int j = 0; j < units && j < before.w2.Length; j++)
            {
                var from = P(UnitX + UnitSide + 2, UnitY(j) + UnitSide / 2);
                var end = outC - new Vector2(OutR, 0);
                double w = before.w2[j] + (after.w2[j] - before.w2[j]) * move;
                float m = (float)(System.Math.Abs(w) / w2Max), q = (float)(System.Math.Abs(g.w2[j]) / g2Max);
                XgSoftDraw.Seg(vh, from, end, 1 + 5 * m, 2, Alpha(w >= 0 ? XgDark.Good : XgDark.Bad, .25f + .45f * m));
                if (toHidden > 0) XgSoftDraw.Seg(vh, end, Vector2.Lerp(end, from, toHidden), 1.5f + 3 * q, 2, Alpha(XgDark.Bad, (.4f + .5f * q) * red));
            }

            // The picture, and the pixels the error reaches (red where it is blamed most).
            float side = n * InCell;
            Box(vh, InX - 1, InY - 1, side + 2, side + 2, XgDark.Line);
            for (int i = 0; i < inputs; i++)
            {
                float x = InX + (i % n) * InCell, y = InY + (i / n) * InCell;
                Box(vh, x, y, InCell, InCell, Color.Lerp(XgDark.Page, XgDark.Ink, Mathf.Clamp01((float)pix[i])));
                float b = (float)(inputBlame[i] / inputMax) * Mathf.Clamp01((t - ReachInput + .1f) / .2f) * red;
                if (b > .05f) Box(vh, x, y, InCell, InCell, Alpha(XgDark.Bad, .55f * b));
            }

            // Hidden units: weight heat maps (moving with the step), ringed red by their share of the blame.
            float cell = UnitSide / n;
            for (int j = 0; j < units; j++)
            {
                float y0 = UnitY(j);
                float share = (float)(System.Math.Abs(g.blame[j]) / blameMax) * Mathf.Clamp01((t - ReachHidden + .05f) / .15f) * red;
                if (share > 0) XgSoftDraw.Disc(vh, P(UnitX + UnitSide / 2, y0 + UnitSide / 2), UnitSide * .55f, 6, Alpha(XgDark.Bad, .45f * share));
                Box(vh, UnitX - 1, y0 - 1, UnitSide + 2, UnitSide + 2, share > .02f ? Color.Lerp(XgDark.Line, XgDark.Bad, share) : XgDark.Line);
                double max = 1e-9;
                for (int i = 0; i < inputs; i++) max = System.Math.Max(max, System.Math.Abs(before.w1[j * inputs + i]));
                for (int i = 0; i < inputs; i++)
                {
                    int k = j * inputs + i;
                    double w = before.w1[k] + (after.w1[k] - before.w1[k]) * move;
                    Box(vh, UnitX + (i % n) * cell, y0 + (i / n) * cell, cell, cell, Color.Lerp(XgDark.Page, w >= 0 ? XgDark.Good : XgDark.Bad, Mathf.Clamp01((float)(System.Math.Abs(w) / max))));
                }
            }

            // The answer as the microscope draws it, with the red dot of the error on it.
            double p = g.output;
            float sure = Mathf.Abs((float)p - .5f) * 2;
            var col = p > .5 ? XgDark.Good : XgDark.Bad;
            XgDraw.Disc(vh, outC, OutR, XgDark.Page);
            XgDraw.Disc(vh, outC, OutR - 2, Alpha(col, .2f + .5f * sure));
            float dot = Mathf.Clamp01(t / ReachOut);
            if (dot > 0)
            {
                float pulse = t < Seconds ? 1 + .25f * Mathf.Sin(t * 18) * (1 - t / Seconds) : 1;
                XgSoftDraw.Disc(vh, outC, 7 * dot * pulse, 6, Alpha(XgDark.Bad, .9f));
                XgDraw.Ring(vh, outC, OutR + 2, 2, Alpha(XgDark.Bad, .8f * dot));
            }
        }
    }
}
