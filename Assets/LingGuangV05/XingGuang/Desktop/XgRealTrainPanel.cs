using System.Collections.Generic;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

using LingGuangV05.Core;
namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// The one real training in the game (design v1.1 §11.8.1, §13): a small live panel in the corner of 灵光.exe
    /// that trains a real perceptron or MLP (XgTinyRun) on 8×8 pictures while the player watches the error curve,
    /// the input pictures with the net's answers, and each unit's 64 weights as a tiny grid.
    /// Three moments: when stage one ends, a perceptron that cannot learn XOR and then two layers with an S-curve that
    /// can; and the stage-two emergence (it learns "is it a 0?" and knows the 0 of 0.txt).
    /// It never blocks: it waits behind the insight card, never takes the whole window, closes by itself shortly
    /// after the run ends, and 「跳过」 closes it at once.
    /// </summary>
    public sealed class XgRealTrainPanel : MonoBehaviour
    {
        const float W = 604, H = 268, LingerSeconds = 9, FadeSeconds = .35f;
        const int Tiles = 6, Units = 4;

        XingGuangView view;
        XgUi ui;
        XgSim bound;
        RectTransform panel;
        CanvasGroup group;
        TMP_Text title, status, caption, footer, curveLabel, weightsLabel, inputsLabel;
        XgBtn skip;
        readonly XgTinyPixels[] tiles = new XgTinyPixels[Tiles];
        readonly TMP_Text[] marks = new TMP_Text[Tiles];
        readonly XgTinyPixels[] weights = new XgTinyPixels[Units];
        XgTinyCurve curve;
        readonly Queue<XgTinyLesson> pending = new Queue<XgTinyLesson>();
        XgTinyRun run;
        float budget, doneAt = -1, closeAt = -1;

        public static XgRealTrainPanel Install(XingGuangView view, RectTransform root, TMP_FontAsset font)
        {
            var p = view.gameObject.GetComponent<XgRealTrainPanel>() ?? view.gameObject.AddComponent<XgRealTrainPanel>();
            p.view = view;
            p.ui = new XgUi(font, view.Controller != null ? view.Controller.Window : null);
            p.Build(root);
            return p;
        }

        /// <summary>A real run is on screen (or fading out).</summary>
        public bool Showing => panel != null && panel.gameObject.activeSelf;

        /// <summary>Queues a lesson; it starts when 灵光.exe is visible and no insight card is up.</summary>
        public void Show(XgTinyLesson lesson) { if (!pending.Contains(lesson)) pending.Enqueue(lesson); }

        void Build(RectTransform root)
        {
            panel = Rect("Real Training", root, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-W - 16, 60), new Vector2(-16, 60 + H));
            Panel(panel, XgDark.Line);
            var inner = Rect("Inner", panel, Vector2.zero, Vector2.one, new Vector2(1, 1), new Vector2(-1, -1));
            Panel(inner, XgDark.Card);
            group = panel.gameObject.AddComponent<CanvasGroup>();

            var head = Rect("Header", inner, new Vector2(0, 1), Vector2.one, new Vector2(0, -32), Vector2.zero);
            Panel(head, XgDark.Hud).raycastTarget = false;
            title = ui.Text(Rect("Title", head, Vector2.zero, Vector2.one, new Vector2(12, 0), new Vector2(-96, 0)), "", 15, Color.white, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold;
            skip = ui.Button(head, "", Skip, 13);
            skip.rt.gameObject.name = "SkipRealTraining";
            PlaceTopRight(skip, 88, 4, 80, 24);
            UiTip.Add(skip.rt, "关掉这段演示。不影响任何进度。", "Close this demo. It changes no progress.");

            // Inputs: 8×8 pictures with the net's current answer.
            var left = Rect("Inputs", inner, new Vector2(0, 0), new Vector2(0, 1), new Vector2(10, 58), new Vector2(150, -38));
            inputsLabel = ui.Text(Rect("Label", left, new Vector2(0, 1), Vector2.one, new Vector2(0, -16), Vector2.zero), "", 11, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            for (int i = 0; i < Tiles; i++)
            {
                int col = i % 2, row = i / 2;
                var t = Rect("Picture" + i, left, new Vector2(0, 1), new Vector2(0, 1), new Vector2(col * 70, -20 - (row + 1) * 54), new Vector2(col * 70 + 46, -20 - row * 54 - 8));
                tiles[i] = t.gameObject.AddComponent<XgTinyPixels>();
                marks[i] = ui.Text(Rect("Answer", left, new Vector2(0, 1), new Vector2(0, 1), new Vector2(col * 70 + 48, -20 - (row + 1) * 54), new Vector2(col * 70 + 70, -20 - row * 54 - 8)), "", 12, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            }
            UiTip.Add(left, "输入：8×8 的小图，每格一个数（白 0，黑 1）。旁边是它现在的回答：✓ 对，✗ 错。", "Inputs: 8×8 pictures, one number per square (white 0, black 1). Next to each is its current answer: ✓ right, ✗ wrong.");
            Panel(left, new Color(1, 1, 1, .01f));

            // The error curve.
            var mid = Rect("Curve", inner, new Vector2(0, 0), new Vector2(0, 1), new Vector2(160, 78), new Vector2(424, -38));
            Panel(mid, new Color32(6, 11, 16, 255));
            curveLabel = ui.Text(Rect("Label", mid, new Vector2(0, 1), Vector2.one, new Vector2(6, -16), new Vector2(-6, 0)), "", 11, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            curve = Rect("Line", mid, Vector2.zero, Vector2.one, new Vector2(6, 6), new Vector2(-6, -18)).gameObject.AddComponent<XgTinyCurve>();
            curve.raycastTarget = false;
            UiTip.Add(mid, "误差：每张图「它的回答」和「正确答案」差多少，平方后取平均。0 就是全对。\n这条线是真的：每一点都是刚刚在你的 CPU 上算出来的。",
                "Error: how far each answer is from the right one, squared and averaged. 0 means all right.\nThis line is real: every point was just computed on your CPU.");
            status = ui.Text(Rect("Status", inner, new Vector2(0, 0), new Vector2(0, 0), new Vector2(160, 58), new Vector2(424, 76)), "", 11, XgDark.Ink, TextAlignmentOptions.MidlineLeft);

            // Weights: what each unit looks for, as an 8×8 grid.
            var right = Rect("Weights", inner, new Vector2(0, 0), new Vector2(0, 1), new Vector2(434, 58), new Vector2(592, -38));
            weightsLabel = ui.Text(Rect("Label", right, new Vector2(0, 1), Vector2.one, new Vector2(0, -16), Vector2.zero), "", 11, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            for (int i = 0; i < Units; i++)
            {
                int col = i % 2, row = i / 2;
                var t = Rect("Unit" + i, right, new Vector2(0, 1), new Vector2(0, 1), new Vector2(col * 80, -20 - (row + 1) * 80), new Vector2(col * 80 + 74, -20 - row * 80 - 6));
                weights[i] = t.gameObject.AddComponent<XgTinyPixels>();
                weights[i].Signed = true;
            }
            UiTip.Add(right, "权重：每个单元给 64 个格子各一个分量。蓝 = 这里有墨就更像「是」，红 = 更像「否」，越深越重。",
                "Weights: each unit gives each of the 64 squares a share. Blue = ink here says \"yes\", red = says \"no\"; darker weighs more.");
            Panel(right, new Color(1, 1, 1, .01f));

            caption = ui.Text(Rect("Caption", inner, Vector2.zero, new Vector2(1, 0), new Vector2(12, 24), new Vector2(-12, 56)), "", 13, XgDark.Ink, TextAlignmentOptions.TopLeft);
            caption.enableAutoSizing = true; caption.fontSizeMin = 10; caption.fontSizeMax = 13;
            footer = ui.Text(Rect("Footer", inner, Vector2.zero, new Vector2(1, 0), new Vector2(12, 4), new Vector2(-12, 22)), "", 11, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            footer.enableAutoSizing = true; footer.fontSizeMin = 9; footer.fontSizeMax = 11;
            panel.gameObject.SetActive(false);
        }

        void OnDestroy() { Unbind(); }

        void Unbind()
        {
            if (bound == null) return;
            bound.StageAdvanced -= OnStageAdvanced; bound.Emerged -= OnEmerged;
            bound = null;
        }

        void OnStageAdvanced(int from) { if (from == 1) { Show(XgTinyLesson.XorPerceptron); Show(XgTinyLesson.XorMlp); } }
        void OnEmerged(int stage, string line) { if (stage == 2) Show(XgTinyLesson.Zero); }

        void Skip() { if (Showing && closeAt < 0) closeAt = Time.unscaledTime; }

        bool InsightCardUp { get { var c = view.GetComponent<XgHoloCard>(); return c != null && c.Showing; } }

        void Begin(XgTinyLesson lesson)
        {
            run = new XgTinyRun(lesson);
            budget = 0; doneAt = -1; closeAt = -1;
            curve.Set(run.loss, lesson == XgTinyLesson.XorMlp ? new XgTinyRun(XgTinyLesson.XorPerceptron).Finish().loss : null, Points(run), run.net.IsPerceptron ? .75f : .3f);
            bool xor = lesson != XgTinyLesson.Zero;
            for (int i = 0; i < Tiles; i++)
            {
                bool on = xor ? i < run.train.Count : i < run.test.Count;
                tiles[i].gameObject.SetActive(on); marks[i].gameObject.SetActive(on);
                if (on) { tiles[i].Values = (xor ? run.train : run.test)[i].pixels; tiles[i].Highlight = false; }
            }
            for (int i = 0; i < Units; i++) weights[i].gameObject.SetActive(i < run.net.Units);
            if (run.net.Units == 1) Place(weights[0].rectTransform, 0, 0, 154, 154);
            else for (int i = 0; i < Units; i++) Place(weights[i].rectTransform, (i % 2) * 80, (i / 2) * 80, 74, 74);
            panel.gameObject.SetActive(true);
            panel.SetAsLastSibling();
            group.alpha = 0;
            view.Juice.Play(XgJuice.Sfx.Id.Unlock, 1.1f, .6f);
            Refresh();
        }

        static void Place(RectTransform rt, float x, float y, float w, float h)
        { rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.offsetMin = new Vector2(x, -20 - y - h); rt.offsetMax = new Vector2(x + w, -20 - y); }

        static int Points(XgTinyRun r) => 1 + r.epochs * (r.net.IsPerceptron ? r.train.Count : 1);

        /// <summary>Seconds a lesson's animation lasts (the training itself takes milliseconds).</summary>
        static float Seconds(XgTinyLesson lesson) => lesson == XgTinyLesson.XorMlp ? 5.5f : 4.5f;

        void Update()
        {
            if (view == null) return;
            var sim = view.Sim;
            if (sim != bound)
            {
                Unbind();
                bound = sim;
                if (bound != null) { bound.StageAdvanced += OnStageAdvanced; bound.Emerged += OnEmerged; }
            }
            if (!Showing)
            {
                if (pending.Count > 0 && view.Visible && !InsightCardUp) Begin(pending.Dequeue());
                return;
            }
            if (!view.Visible) return; // paused while the window is hidden
            float now = Time.unscaledTime, dt = Time.unscaledDeltaTime;
            if (closeAt >= 0)
            {
                float k = Mathf.Clamp01((now - closeAt) / FadeSeconds);
                group.alpha = 1 - k;
                if (k >= 1) { panel.gameObject.SetActive(false); run = null; }
                return;
            }
            group.alpha = Mathf.Min(1, group.alpha + dt / FadeSeconds);
            if (!run.Done)
            {
                budget += dt * run.epochs / Seconds(run.lesson);
                int n = Mathf.FloorToInt(budget);
                if (n > 0) { budget -= n; run.Step(n); curve.Changed(); }
                if (run.Done) { doneAt = now; view.Juice.Play(run.lesson == XgTinyLesson.XorPerceptron ? XgJuice.Sfx.Id.Thud : XgJuice.Sfx.Id.Fanfare, 1, .5f); }
                Refresh();
            }
            else if (doneAt >= 0 && now - doneAt > LingerSeconds) closeAt = now;
        }

        void Refresh()
        {
            if (run == null) return;
            var lesson = run.lesson;
            bool xor = lesson != XgTinyLesson.Zero;
            title.text = lesson == XgTinyLesson.XorPerceptron ? Lang.T("真训练 · 异或 · 单层感知机")
                : lesson == XgTinyLesson.XorMlp ? Lang.T("真训练 · 异或 · 两层 + S 形")
                : Lang.T("真训练 · 认出「0」");
            skip.Set(run.Done ? Lang.T("关闭") : Lang.T("跳过"), true);
            inputsLabel.text = xor ? Lang.T("输入（4 张图）") : Lang.T("没训练过的图");
            weightsLabel.text = run.net.IsPerceptron ? Lang.T("权重（1 个单元）") : T("隐藏层权重（" + run.net.Units + " 个单元）", "Hidden weights (" + run.net.Units + " units)");
            curveLabel.text = Lang.T("误差") + (lesson == XgTinyLesson.XorMlp ? "  <color=#58798A>" + Lang.T("灰：单层感知机") + "</color>  <color=#E8900C>" + Lang.T("金：两层") + "</color>" : "");

            var set = xor ? run.train : run.test;
            for (int i = 0; i < Tiles && i < set.Count; i++)
            {
                var s = set[i];
                bool yes = run.net.Predict(s.pixels), right = yes == s.label;
                marks[i].text = (yes ? T("是", "yes") : T("否", "no")) + (right ? " <color=#5DCAA5>✓</color>" : " <color=#E24B4A>✗</color>");
            }
            for (int i = 0; i < run.net.Units && i < Units; i++) weights[i].Values = run.net.UnitWeights(i);

            float loss = run.loss.Count > 0 ? run.loss[run.loss.Count - 1] : 0;
            status.text = T("第 ", "Pass ") + run.Epoch + "/" + run.epochs + T(" 轮", "") + Lang.T(" · 误差 ") + N(loss, "0.000")
                + Lang.T(" · 答错 ") + N(run.TrainError * 100, "0") + "%";
            caption.text = Caption();
            footer.text = T("只有这一段是真训练：" + run.Parameters + " 个参数，算了 " + N(run.Milliseconds, "0.0") + " 毫秒。后面的阶段太大，游戏里改用模拟。",
                "Only this part is real training: " + run.Parameters + " parameters, " + N(run.Milliseconds, "0.0") + " ms of computing. Later stages are too big, so the game simulates them.");
        }

        string Caption()
        {
            switch (run.lesson)
            {
                case XgTinyLesson.XorPerceptron:
                    return run.Done
                        ? Lang.T("误差在 25% 到 75% 之间来回跳，就是降不到 0：单层只能画一条直线，而异或的两个「是」在对角上，一条线分不开。")
                        : Lang.T("一个单层感知机正在学这 4 张图：左边亮、右边亮，只亮一边才算「是」。");
                case XgTinyLesson.XorMlp:
                    return run.Done
                        ? T("同样 4 张图，加一层、换成有坡度的 S 形：误差一路掉到 " + N(run.loss[run.loss.Count - 1], "0.00") + "，4 张全对。这就是反向传播。",
                            "The same 4 pictures with a second layer and a sloped S-curve: the error falls to " + N(run.loss[run.loss.Count - 1], "0.00") + " and all 4 are right. That is backpropagation.")
                        : Lang.T("两层网络在学同一道异或。误差从下层一路传回来，每个单元学一条线，合起来就分开了。");
                default:
                    if (!run.Done) return T("它在学「这是 0 吗？」：" + run.train.Count + " 张手写的 8×8 数字，一半是 0。", "It is learning \"is this a 0?\": " + run.train.Count + " hand-drawn 8×8 digits, half of them zeros.");
                    var zero = XgTinyData.TheZero();
                    tiles[0].Values = zero.pixels; tiles[0].Highlight = true;
                    double p = run.net.Forward(zero.pixels);
                    marks[0].text = p > .5 ? "<color=#5DCAA5>「0」</color>" : T("否", "no");
                    return p > .5
                        ? T("最后一张它从没见过：字迹和那个删掉的 0.txt 一样。它答：「0」（" + N(p * 100, "0") + "%）。没见过的 " + run.test.Count + " 张里答对 " + N((1 - run.TestError) * 100, "0") + "%。",
                            "The last picture is one it never saw, in the hand of the deleted 0.txt. It answers \"0\" (" + N(p * 100, "0") + "%). Right on " + N((1 - run.TestError) * 100, "0") + "% of " + run.test.Count + " unseen pictures.")
                        : T("那张 0.txt 的 0，它还没认出来（" + N(p * 100, "0") + "%）。", "It does not know the 0 of 0.txt yet (" + N(p * 100, "0") + "%).");
            }
        }
    }

    /// <summary>An 8×8 grid of values: ink on paper (0–1), or signed weights in blue and red scaled to the largest.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgTinyPixels : MaskableGraphic
    {
        static readonly Color Paper = new Color32(250, 248, 240, 255), Ink = new Color32(30, 38, 56, 255);
        static readonly Color Plus = new Color32(59, 91, 219, 255), Minus = new Color32(214, 48, 49, 255), Frame = new Color32(213, 222, 234, 255);
        double[] values;
        bool highlight;
        public bool Signed;
        public double[] Values { set { values = value; SetVerticesDirty(); } }
        public bool Highlight { set { if (highlight == value) return; highlight = value; SetVerticesDirty(); } }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            float side = Mathf.Min(r.width, r.height);
            if (side < 8) return;
            var o = new Vector2(r.center.x - side / 2, r.center.y - side / 2);
            XgDraw.Box(vh, o - Vector2.one * (highlight ? 3 : 1), o + Vector2.one * (side + (highlight ? 3 : 1)), highlight ? XgDark.Gold : Frame);
            XgDraw.Box(vh, o, o + Vector2.one * side, Paper);
            if (values == null) return;
            int n = XgTinyData.Side;
            float cell = side / n;
            double max = 1e-9;
            if (Signed) foreach (var v in values) max = System.Math.Max(max, System.Math.Abs(v));
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    double v = values[y * n + x];
                    Color c = Signed ? Color.Lerp(Paper, v >= 0 ? Plus : Minus, (float)(System.Math.Abs(v) / max)) : Color.Lerp(Paper, Ink, Mathf.Clamp01((float)v));
                    var min = new Vector2(o.x + x * cell, o.y + (n - 1 - y) * cell);
                    XgDraw.Box(vh, min, min + Vector2.one * cell, c);
                }
        }
    }

    /// <summary>The live error curve, with an optional grey reference run underneath. Both span the full width.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgTinyCurve : MaskableGraphic
    {
        static readonly Color Grid = new Color32(214, 222, 235, 255), Reference = new Color32(154, 163, 181, 255), Live = new Color32(232, 144, 12, 255);
        List<float> live, reference;
        int capacity = 2;
        float top = .3f;

        /// <param name="ceiling">Lowest top of the scale (a perceptron's error can climb to .75 later).</param>
        public void Set(List<float> liveLoss, List<float> referenceLoss, int points, float ceiling = .3f)
        {
            live = liveLoss; reference = referenceLoss; capacity = Mathf.Max(2, points);
            top = ceiling;
            if (live != null) foreach (var v in live) top = Mathf.Max(top, v);
            if (reference != null) foreach (var v in reference) top = Mathf.Max(top, v);
            top = Mathf.Min(1, top * 1.1f);
            SetVerticesDirty();
        }

        public void Changed() { SetVerticesDirty(); }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            if (r.width < 4 || r.height < 4) return;
            for (int i = 0; i <= 4; i++) { float y = r.yMin + r.height * i / 4; XgDraw.Seg(vh, new Vector2(r.xMin, y), new Vector2(r.xMax, y), 1, Grid); }
            if (reference != null) Poly(vh, r, reference, reference.Count, 2, Reference);
            if (live != null) Poly(vh, r, live, capacity, 2.5f, Live);
        }

        void Poly(VertexHelper vh, Rect r, List<float> data, int span, float width, Color color)
        {
            if (data.Count < 2) return;
            float step = r.width / Mathf.Max(1, span - 1);
            // Long runs are thinned so the mesh stays small (at most ~300 segments).
            int stride = Mathf.Max(1, data.Count / 300);
            Vector2 last = P(r, 0, step, data[0]);
            for (int i = stride; i < data.Count; i += stride) { var p = P(r, i, step, data[i]); XgDraw.Seg(vh, last, p, width, color); last = p; }
            var end = P(r, data.Count - 1, step, data[data.Count - 1]);
            if (end != last) XgDraw.Seg(vh, last, end, width, color);
        }

        Vector2 P(Rect r, int i, float step, float v) => new Vector2(r.xMin + step * i, r.yMin + r.height * Mathf.Clamp01(v / top));
    }
}
