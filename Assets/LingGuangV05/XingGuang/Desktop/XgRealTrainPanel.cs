using System.Collections.Generic;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

using LingGuangV05.Core;
using LingGuangV05.Desktop.Story;
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
    /// The same panel is the 显微镜 (它在学_可视化借鉴设计 §2.1, after TensorFlow Playground): opened any time with
    /// <see cref="OpenMicroscope(XingGuangView)"/>, it runs a fresh ~8-second real training on 「这是 0 吗？」 and shows the
    /// trained and new pictures with ✓/×, each hidden unit's 64 weights as an 8×8 heat map redrawn every pass (green +,
    /// red −), the links (width ∝ |w|, colour = sign, a flash when one moves fast) and two error lines: trained pictures
    /// and held-out ones.
    /// </summary>
    public sealed class XgRealTrainPanel : MonoBehaviour
    {
        const float W = 604, H = 268, LingerSeconds = 9, FadeSeconds = .35f;
        const int Tiles = 6, Units = 4;
        /// <summary>The microscope's panel, its run length and how long it stays after the run.</summary>
        /// <summary>Gap under the panel: clear of the tray toasts that pop up over the window's bottom-right corner.</summary>
        public const float Bottom = 210;
        const float MicroW = 720, MicroH = 320, MicroSeconds = 8, MicroLinger = 20, MicroCycle = .8f;

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
        // The microscope.
        RectTransform storyRoot, microRoot;
        readonly XgTinyPixels[] microTiles = new XgTinyPixels[Tiles];
        readonly TMP_Text[] microMarks = new TMP_Text[Tiles];
        XgMicroscopeGraphic graph;
        XgTinyCurve microCurve;
        TMP_Text microInputsLabel, microGraphLabel, microCurveLabel, microStatus, microOutput;
        int microscopeRuns, shownPicture = -1;
        float shownAt;

        public static XgRealTrainPanel Install(XingGuangView view, RectTransform root, TMP_FontAsset font)
        {
            var p = view.gameObject.GetComponent<XgRealTrainPanel>() ?? view.gameObject.AddComponent<XgRealTrainPanel>();
            p.view = view;
            p.ui = new XgUi(font, view.Controller != null ? view.Controller.Window : null);
            p.Build(root);
            // The correction animation (它在学 §2.4) shares this corner and waits for this panel.
            XgGradFlowPanel.Install(view, root, font, p);
            return p;
        }

        /// <summary>A real run is on screen (or fading out).</summary>
        public bool Showing => panel != null && panel.gameObject.activeSelf;

        /// <summary>The microscope is on screen.</summary>
        public bool MicroscopeShowing => Showing && run != null && run.lesson == XgTinyLesson.Microscope && closeAt < 0;

        /// <summary>Queues a lesson; it starts when 灵光.exe is visible and no insight card is up.</summary>
        public void Show(XgTinyLesson lesson) { if (!pending.Contains(lesson)) pending.Enqueue(lesson); }

        /// <summary>
        /// The 显微镜: a fresh short real run (~8 s) the player can open any time. Already open: it starts again from new
        /// random weights. A story run on screen goes first; the microscope follows it.
        /// </summary>
        public void OpenMicroscope()
        {
            if (MicroscopeShowing) { Begin(XgTinyLesson.Microscope); return; }
            Show(XgTinyLesson.Microscope);
        }

        /// <summary>Opens the microscope on 灵光.exe's panel. False when the panel is not installed.</summary>
        public static bool OpenMicroscope(XingGuangView view)
        {
            var p = view != null ? view.GetComponent<XgRealTrainPanel>() : null;
            if (p == null) return false;
            p.OpenMicroscope();
            return true;
        }

        static bool ReduceMotion => PlayerPrefs.GetInt(StoryDesktopPresenter.ReduceMotionPref, 0) == 1;

        void Build(RectTransform root)
        {
            panel = Rect("Real Training", root, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-W - 16, Bottom), new Vector2(-16, Bottom + H));
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

            storyRoot = Rect("Story", inner, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            // Inputs: 8×8 pictures with the net's current answer.
            var left = Rect("Inputs", storyRoot, new Vector2(0, 0), new Vector2(0, 1), new Vector2(10, 58), new Vector2(150, -38));
            inputsLabel = ui.Text(Rect("Label", left, new Vector2(0, 1), Vector2.one, new Vector2(0, -16), Vector2.zero), "", 11, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            for (int i = 0; i < Tiles; i++)
            {
                int col = i % 2, row = i / 2;
                var t = Rect("Picture" + i, left, new Vector2(0, 1), new Vector2(0, 1), new Vector2(col * 70, -20 - (row + 1) * 54), new Vector2(col * 70 + 46, -20 - row * 54 - 8));
                tiles[i] = t.gameObject.AddComponent<XgTinyPixels>();
                marks[i] = ui.Text(Rect("Answer", left, new Vector2(0, 1), new Vector2(0, 1), new Vector2(col * 70 + 48, -20 - (row + 1) * 54), new Vector2(col * 70 + 70, -20 - row * 54 - 8)), "", 12, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            }
            UiTip.Add(left, "输入：8×8 的小图，每格一个数（白 0，黑 1）。旁边是它现在的回答：✓ 对，× 错。", "Inputs: 8×8 pictures, one number per square (white 0, black 1). Next to each is its current answer: ✓ right, × wrong.");
            Panel(left, new Color(1, 1, 1, .01f));

            // The error curve.
            var mid = Rect("Curve", storyRoot, new Vector2(0, 0), new Vector2(0, 1), new Vector2(160, 78), new Vector2(424, -38));
            Panel(mid, new Color32(6, 11, 16, 255));
            curveLabel = ui.Text(Rect("Label", mid, new Vector2(0, 1), Vector2.one, new Vector2(6, -16), new Vector2(-6, 0)), "", 11, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            curve = Rect("Line", mid, Vector2.zero, Vector2.one, new Vector2(6, 6), new Vector2(-6, -18)).gameObject.AddComponent<XgTinyCurve>();
            curve.raycastTarget = false;
            UiTip.Add(mid, "误差：每张图「它的回答」和「正确答案」差多少，平方后取平均。0 就是全对。\n这条线是真的：每一点都是刚刚在你的 CPU 上算出来的。",
                "Error: how far each answer is from the right one, squared and averaged. 0 means all right.\nThis line is real: every point was just computed on your CPU.");
            status = ui.Text(Rect("Status", storyRoot, new Vector2(0, 0), new Vector2(0, 0), new Vector2(160, 58), new Vector2(424, 76)), "", 11, XgDark.Ink, TextAlignmentOptions.MidlineLeft);

            // Weights: what each unit looks for, as an 8×8 grid.
            var right = Rect("Weights", storyRoot, new Vector2(0, 0), new Vector2(0, 1), new Vector2(434, 58), new Vector2(592, -38));
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
            BuildMicroscope(inner);
            panel.gameObject.SetActive(false);
        }

        /// <summary>The microscope's layout: trained and new pictures, the net (heat maps and links), the two error lines.</summary>
        void BuildMicroscope(RectTransform inner)
        {
            microRoot = Rect("Microscope", inner, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var left = Rect("Pictures", microRoot, new Vector2(0, 0), new Vector2(0, 1), new Vector2(10, 58), new Vector2(138, -38));
            microInputsLabel = ui.Text(Rect("Label", left, new Vector2(0, 1), Vector2.one, new Vector2(0, -16), Vector2.zero), "", 11, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            for (int i = 0; i < Tiles; i++)
            {
                int col = i / 3, row = i % 3; // column 0: trained pictures, column 1: new ones
                var t = Rect("Picture" + i, left, new Vector2(0, 1), new Vector2(0, 1), new Vector2(col * 66, -20 - row * 54 - 44), new Vector2(col * 66 + 44, -20 - row * 54));
                microTiles[i] = t.gameObject.AddComponent<XgTinyPixels>();
                microTiles[i].Dark = true;
                microMarks[i] = ui.Text(Rect("Answer", left, new Vector2(0, 1), new Vector2(0, 1), new Vector2(col * 66 + 46, -20 - row * 54 - 44), new Vector2(col * 66 + 66, -20 - row * 54)), "", 13, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            }
            UiTip.Add(left, "左列是它练过的图，右列是它从没见过的图。旁边是它现在的回答：✓ 对，× 错。练过的全对不稀奇，新图也对才算学会。",
                "Left: pictures it trains on. Right: pictures it has never seen. Next to each is its current answer: ✓ right, × wrong. Getting trained ones right is easy; new ones right means it learned.");
            Panel(left, new Color(1, 1, 1, .01f));

            var mid = Rect("Net", microRoot, new Vector2(0, 0), new Vector2(0, 1), new Vector2(146, 58), new Vector2(474, -38));
            microGraphLabel = ui.Text(Rect("Label", mid, new Vector2(0, 1), Vector2.one, new Vector2(0, -16), Vector2.zero), "", 11, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            var area = Rect("Graph", mid, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -18));
            graph = area.gameObject.AddComponent<XgMicroscopeGraphic>();
            graph.raycastTarget = false;
            microOutput = ui.Text(Rect("Output", area, new Vector2(XgMicroscopeGraphic.OutX, .5f), new Vector2(XgMicroscopeGraphic.OutX, .5f), new Vector2(-34, -52), new Vector2(34, -20)), "", 12, XgDark.Ink, TextAlignmentOptions.Center);
            UiTip.Add(mid, "中间四张小图是四个隐藏单元的权重：每个单元给 64 个格子各一个数。绿 = 这里有墨就更像「是 0」，红 = 更像「不是」，越亮越重。每训练一轮重画一次。\n连线：越粗权重越大，绿正红负；变化快的线会闪一下。左边是现在送进去的那张图，右边的圆是它的回答。",
                "The four small maps are the four hidden units' weights: one number per square. Green = ink here says \"a 0\", red = says \"not\"; brighter weighs more. Redrawn every pass.\nLinks: thicker = bigger weight, green +, red −; a link that changes fast flashes. Left is the picture going in now; the circle on the right is its answer.");
            Panel(mid, new Color(1, 1, 1, .01f));

            var chart = Rect("Errors", microRoot, new Vector2(0, 0), new Vector2(0, 1), new Vector2(484, 78), new Vector2(708, -38));
            Panel(chart, new Color32(6, 11, 16, 255));
            microCurveLabel = ui.Text(Rect("Label", chart, new Vector2(0, 1), Vector2.one, new Vector2(6, -16), new Vector2(-6, 0)), "", 11, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            microCurve = Rect("Lines", chart, Vector2.zero, Vector2.one, new Vector2(6, 6), new Vector2(-6, -18)).gameObject.AddComponent<XgTinyCurve>();
            microCurve.raycastTarget = false;
            microCurve.GridColor = XgDark.Hairline;
            UiTip.Add(chart, "两条误差线，都是刚在你的 CPU 上算出来的。\n实线：练过的图。虚线：没见过的新图。两条一起往下走，就是真学会了；实线往下、虚线不动，就是在背答案。",
                "Two error lines, both just computed on your CPU.\nSolid: pictures it trains on. Dashed: new pictures it never sees. Both falling together means it really learned; solid falling while dashed stays means it is memorising.");
            microStatus = ui.Text(Rect("Status", microRoot, new Vector2(0, 0), new Vector2(0, 0), new Vector2(484, 58), new Vector2(708, 76)), "", 11, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            microStatus.enableAutoSizing = true; microStatus.fontSizeMin = 9; microStatus.fontSizeMax = 11;
            microRoot.gameObject.SetActive(false);
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
            bool micro = lesson == XgTinyLesson.Microscope;
            // Each look through the microscope starts from new random weights, so no two runs are quite the same.
            run = micro ? new XgTinyRun(lesson, XgTinyRun.Seed + 31 * ++microscopeRuns) : new XgTinyRun(lesson);
            budget = 0; doneAt = -1; closeAt = -1;
            float w = micro ? MicroW : W, h = micro ? MicroH : H;
            panel.offsetMin = new Vector2(-w - 16, Bottom); panel.offsetMax = new Vector2(-16, Bottom + h);
            storyRoot.gameObject.SetActive(!micro); microRoot.gameObject.SetActive(micro);
            if (micro) { BeginMicroscope(); return; }
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
        static float Seconds(XgTinyLesson lesson) => lesson == XgTinyLesson.Microscope ? MicroSeconds : lesson == XgTinyLesson.XorMlp ? 5.5f : 4.5f;

        void BeginMicroscope()
        {
            microCurve.Set(run.loss, null, Points(run), .3f);
            microCurve.SetHeld(run.testLoss);
            for (int i = 0; i < Tiles; i++)
            {
                var s = Picture(i);
                microTiles[i].gameObject.SetActive(s != null); microMarks[i].gameObject.SetActive(s != null);
                if (s != null) { microTiles[i].Values = s.pixels; microTiles[i].Highlight = false; }
            }
            graph.Begin(run.net);
            shownPicture = -1; shownAt = -1;
            panel.gameObject.SetActive(true);
            panel.SetAsLastSibling();
            group.alpha = 0;
            view.Juice.Play(XgJuice.Sfx.Id.Unlock, 1.1f, .5f);
            RefreshMicroscope(0);
        }

        /// <summary>Tile i: the first three trained pictures, then the first three held-out ones (a 0 and two others each).</summary>
        XgTinySample Picture(int i)
        {
            var set = i < 3 ? run.train : run.test;
            int k = i % 3;
            return k < set.Count ? set[k] : null;
        }

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
            if (run.lesson == XgTinyLesson.Microscope) { UpdateMicroscope(now, dt); return; }
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

        void UpdateMicroscope(float now, float dt)
        {
            if (!run.Done)
            {
                budget += dt * run.epochs / MicroSeconds;
                int n = Mathf.FloorToInt(budget);
                if (n > 0) { budget -= n; run.Step(n); microCurve.Changed(); }
                if (run.Done) { doneAt = now; view.Juice.Play(XgJuice.Sfx.Id.Fanfare, 1, .35f); }
            }
            else if (doneAt >= 0 && now - doneAt > MicroLinger) { closeAt = now; return; }
            RefreshMicroscope(dt);
        }

        /// <summary>Every frame of the microscope: answers, heat maps and links, the picture going in, the two lines.</summary>
        void RefreshMicroscope(float dt)
        {
            if (run == null) return;
            var net = run.net;
            title.text = T("显微镜 · 看它学", "Microscope · watch it learn");
            skip.Set(run.Done ? T("关闭", "Close") : T("跳过", "Skip"), true);
            microInputsLabel.text = T("练过的 · 新图", "Trained · new");
            microGraphLabel.text = T("隐藏层：4 个单元在看哪里", "Hidden layer: where 4 units look");
            microCurveLabel.text = T("误差  <color=#E8900C>— 练过的</color>  <color=#5DA8E8>┄ 新图</color>", "Error  <color=#E8900C>— trained</color>  <color=#5DA8E8>┄ new</color>");
            for (int i = 0; i < Tiles; i++)
            {
                var s = Picture(i);
                if (s == null) continue;
                bool yes = net.Predict(s.pixels), right = yes == s.label;
                microMarks[i].text = right ? "<color=#5DCAA5>✓</color>" : "<color=#E24B4A>×</color>";
            }
            // The picture going in: one tile after another, so the links and the answer change with it.
            float now = Time.unscaledTime;
            if (shownPicture < 0 || now - shownAt >= MicroCycle)
            {
                if (shownPicture >= 0) microTiles[shownPicture].Highlight = false;
                do shownPicture = (shownPicture + 1) % Tiles; while (Picture(shownPicture) == null);
                microTiles[shownPicture].Highlight = true;
                shownAt = now;
            }
            var shown = Picture(shownPicture);
            double p = net.Forward(shown.pixels);
            graph.Show(shown.pixels, p, dt, !ReduceMotion);
            bool sayYes = p > .5;
            microOutput.text = (sayYes ? T("是 0", "a 0") : T("不是", "not")) + " " + N(p * 100, "0") + "%"
                + ((sayYes == shown.label) ? " <color=#5DCAA5>✓</color>" : " <color=#E24B4A>×</color>");

            float trained = run.loss.Count > 0 ? run.loss[run.loss.Count - 1] : 0, held = run.testLoss.Count > 0 ? run.testLoss[run.testLoss.Count - 1] : 0;
            microStatus.text = T("第 " + run.Epoch + "/" + run.epochs + " 轮 · 练过 " + N(trained, "0.000") + " · 新图 " + N(held, "0.000"),
                "Pass " + run.Epoch + "/" + run.epochs + " · trained " + N(trained, "0.000") + " · new " + N(held, "0.000"));
            caption.text = MicroCaption(trained, held);
            footer.text = T("真训练：" + run.Parameters + " 个参数，" + run.train.Count + " 张练、" + run.test.Count + " 张留作新图，算了 " + N(run.Milliseconds, "0.0") + " 毫秒。每次打开都从新的随机权重开始。",
                "Real training: " + run.Parameters + " parameters, " + run.train.Count + " pictures to learn from and " + run.test.Count + " kept back, " + N(run.Milliseconds, "0.0") + " ms of computing. Every look starts from new random weights.");
        }

        string MicroCaption(float trained, float held)
        {
            if (!run.Done)
                return run.Epoch < run.epochs / 4
                    ? T("它刚开始学「这是 0 吗？」：权重还是随机的，四张小图一片花。", "It has just started on \"is this a 0?\": the weights are still random, the four maps are noise.")
                    : T("看中间的小图：有的单元慢慢亮出一圈，那是「0 的外圈」。", "Watch the small maps: some units slowly light up a ring. That is the outline of a 0.");
            double trainErr = run.TrainError, testErr = run.TestError;
            if (testErr - trainErr > .15 || held > trained * 2.5f + .03f)
                return T("两条线分开了：练过的图它越来越熟，新图却没跟上。这就是在背答案。", "The two lines came apart: it knows the trained pictures better and better, the new ones did not follow. That is memorising.");
            return T("练过的和新的两条线一起往下走：新图也答对 " + N((1 - testErr) * 100, "0") + "%。它学到的是「0 的样子」，不是背答案。",
                "Trained and new fell together: " + N((1 - testErr) * 100, "0") + "% right on new pictures too. It learned what a 0 looks like, not the answers.");
        }

        void Refresh()
        {
            if (run == null) return;
            if (run.lesson == XgTinyLesson.Microscope) { RefreshMicroscope(0); return; }
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
                marks[i].text = (yes ? T("是", "yes") : T("否", "no")) + (right ? " <color=#5DCAA5>✓</color>" : " <color=#E24B4A>×</color>");
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

    /// <summary>
    /// An 8×8 grid of values: ink on paper (0–1), or signed weights in blue and red scaled to the largest. <see cref="Dark"/>
    /// draws it the scanner way instead (the microscope): bright ink on the dark page, weights in green and red.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgTinyPixels : MaskableGraphic
    {
        static readonly Color Paper = new Color32(250, 248, 240, 255), Ink = new Color32(30, 38, 56, 255);
        static readonly Color Plus = new Color32(59, 91, 219, 255), Minus = new Color32(214, 48, 49, 255), Frame = new Color32(213, 222, 234, 255);
        double[] values;
        bool highlight;
        public bool Signed, Dark;
        public double[] Values { set { values = value; SetVerticesDirty(); } }
        public bool Highlight { set { if (highlight == value) return; highlight = value; SetVerticesDirty(); } }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            float side = Mathf.Min(r.width, r.height);
            if (side < 8) return;
            var o = new Vector2(r.center.x - side / 2, r.center.y - side / 2);
            Color paper = Dark ? XgDark.Page : Paper, ink = Dark ? XgDark.Ink : Ink, plus = Dark ? XgDark.Good : Plus, minus = Dark ? XgDark.Bad : Minus;
            XgDraw.Box(vh, o - Vector2.one * (highlight ? 3 : 1), o + Vector2.one * (side + (highlight ? 3 : 1)), highlight ? XgDark.Gold : Dark ? XgDark.Line : Frame);
            XgDraw.Box(vh, o, o + Vector2.one * side, paper);
            if (values == null) return;
            int n = XgTinyData.Side;
            float cell = side / n;
            double max = 1e-9;
            if (Signed) foreach (var v in values) max = System.Math.Max(max, System.Math.Abs(v));
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    double v = values[y * n + x];
                    Color c = Signed ? Color.Lerp(paper, v >= 0 ? plus : minus, (float)(System.Math.Abs(v) / max)) : Color.Lerp(paper, ink, Mathf.Clamp01((float)v));
                    var min = new Vector2(o.x + x * cell, o.y + (n - 1 - y) * cell);
                    XgDraw.Box(vh, min, min + Vector2.one * cell, c);
                }
        }
    }

    /// <summary>
    /// The live error curve, with an optional grey reference run underneath. Both span the full width. The microscope adds
    /// a dashed blue line for the held-out pictures (<see cref="SetHeld"/>) on the same scale.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgTinyCurve : MaskableGraphic
    {
        static readonly Color Grid = new Color32(214, 222, 235, 255), Reference = new Color32(154, 163, 181, 255), Live = new Color32(232, 144, 12, 255);
        static readonly Color Held = new Color32(93, 168, 232, 255);
        List<float> live, reference, held;
        int capacity = 2;
        float top = .3f;
        public Color GridColor = Grid;

        /// <param name="ceiling">Lowest top of the scale (a perceptron's error can climb to .75 later).</param>
        public void Set(List<float> liveLoss, List<float> referenceLoss, int points, float ceiling = .3f)
        {
            live = liveLoss; reference = referenceLoss; held = null; capacity = Mathf.Max(2, points);
            top = ceiling;
            if (live != null) foreach (var v in live) top = Mathf.Max(top, v);
            if (reference != null) foreach (var v in reference) top = Mathf.Max(top, v);
            top = Mathf.Min(1, top * 1.1f);
            SetVerticesDirty();
        }

        /// <summary>The held-out line (dashed), sharing the live line's width; call after <see cref="Set"/>.</summary>
        public void SetHeld(List<float> heldLoss)
        {
            held = heldLoss;
            if (held != null) foreach (var v in held) top = Mathf.Min(1, Mathf.Max(top, v * 1.1f));
            SetVerticesDirty();
        }

        public void Changed() { SetVerticesDirty(); }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            if (r.width < 4 || r.height < 4) return;
            for (int i = 0; i <= 4; i++) { float y = r.yMin + r.height * i / 4; XgDraw.Seg(vh, new Vector2(r.xMin, y), new Vector2(r.xMax, y), 1, GridColor); }
            if (reference != null) Poly(vh, r, reference, reference.Count, 2, Reference);
            if (held != null) Poly(vh, r, held, capacity, 2f, Held, true);
            if (live != null) Poly(vh, r, live, capacity, 2.5f, Live);
        }

        void Poly(VertexHelper vh, Rect r, List<float> data, int span, float width, Color color, bool dashed = false)
        {
            if (data.Count < 2) return;
            float step = r.width / Mathf.Max(1, span - 1);
            // Long runs are thinned so the mesh stays small (at most ~300 segments).
            int stride = Mathf.Max(1, data.Count / 300);
            Vector2 last = P(r, 0, step, data[0]);
            float walked = 0;
            for (int i = stride; i < data.Count; i += stride) { var p = P(r, i, step, data[i]); Piece(vh, last, p, width, color, dashed, ref walked); last = p; }
            var end = P(r, data.Count - 1, step, data[data.Count - 1]);
            if (end != last) Piece(vh, last, end, width, color, dashed, ref walked);
        }

        /// <summary>One piece of a line; a dashed line keeps its 5-on / 4-off rhythm across pieces.</summary>
        static void Piece(VertexHelper vh, Vector2 a, Vector2 b, float width, Color color, bool dashed, ref float walked)
        {
            if (!dashed) { XgDraw.Seg(vh, a, b, width, color); return; }
            const float On = 5, Period = 9;
            float len = Vector2.Distance(a, b);
            if (len < 1e-3f) return;
            for (float s = 0; s < len;)
            {
                float phase = (walked + s) % Period;
                float stop = Mathf.Min(len, s + (phase < On ? On - phase : Period - phase));
                if (phase < On) XgDraw.Seg(vh, Vector2.Lerp(a, b, s / len), Vector2.Lerp(a, b, stop / len), width, color);
                s = stop;
            }
            walked += len;
        }

        Vector2 P(Rect r, int i, float step, float v) => new Vector2(r.xMin + step * i, r.yMin + r.height * Mathf.Clamp01(v / top));
    }

    /// <summary>
    /// The microscope's net, drawn Playground's way: the picture going in on the left, a fan of 64 links from its squares
    /// to each hidden unit, each unit's 64 weights as an 8×8 heat map (green +, red −, scaled to that unit's largest),
    /// the unit's links to the answer, and the answer as a glowing disc. Link width follows |w| and colour its sign; a link
    /// whose weight moved fast since the last frame flashes white and fades. Glow is vertex alpha, nothing else.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgMicroscopeGraphic : MaskableGraphic
    {
        /// <summary>Where the answer disc sits, as a share of the width (the panel places its label there).</summary>
        public const float OutX = .88f;
        const float UnitX = .5f;
        XgTinyNet net;
        double[] pixels;
        double answer;
        float[] last1, last2, flash1, flash2, act;

        public void Begin(XgTinyNet n)
        {
            net = n; pixels = null; answer = .5;
            last1 = new float[n.w1.Length]; last2 = new float[n.w2.Length];
            flash1 = new float[n.w1.Length]; flash2 = new float[n.w2.Length];
            act = new float[n.Units];
            for (int i = 0; i < last1.Length; i++) last1[i] = (float)n.w1[i];
            for (int i = 0; i < last2.Length; i++) last2[i] = (float)n.w2[i];
            SetVerticesDirty();
        }

        /// <summary>The picture going in and the answer to it; links that moved fast since the last call flash (unless <paramref name="motion"/> is off).</summary>
        public void Show(double[] picture, double p, float dt, bool motion)
        {
            if (net == null) return;
            pixels = picture; answer = p;
            int inputs = net.inputs;
            for (int j = 0; j < net.Units; j++)
            {
                double max = 1e-6, z = net.b1.Length > j ? net.b1[j] : 0;
                for (int i = 0; i < inputs; i++) { double w = net.w1[j * inputs + i]; max = System.Math.Max(max, System.Math.Abs(w)); z += w * picture[i]; }
                act[j] = (float)(1 / (1 + System.Math.Exp(-z)));
                for (int i = 0; i < inputs; i++)
                {
                    int k = j * inputs + i;
                    float now = (float)net.w1[k];
                    // Moved more than 4% of the unit's largest weight in one frame: flash.
                    if (motion && Mathf.Abs(now - last1[k]) > .04f * max) flash1[k] = 1;
                    else flash1[k] = Mathf.Max(0, flash1[k] - dt * 3);
                    last1[k] = now;
                }
            }
            double max2 = 1e-6;
            foreach (var w in net.w2) max2 = System.Math.Max(max2, System.Math.Abs(w));
            for (int j = 0; j < net.w2.Length; j++)
            {
                float now = (float)net.w2[j];
                if (motion && Mathf.Abs(now - last2[j]) > .04f * max2) flash2[j] = 1;
                else flash2[j] = Mathf.Max(0, flash2[j] - dt * 3);
                last2[j] = now;
            }
            SetVerticesDirty();
        }

        static Color Alpha(Color c, float a) { c.a = a; return c; }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            if (net == null || r.width < 40 || r.height < 40) return;
            int units = net.Units, n = XgTinyData.Side, inputs = net.inputs;
            float inSide = Mathf.Min(64, r.height * .36f), inCell = inSide / n;
            var inMin = new Vector2(r.xMin + 2, r.center.y - inSide / 2);
            float pitch = Mathf.Min(54, r.height / units), uSide = pitch - 8;
            float ux = r.xMin + r.width * UnitX - uSide / 2;
            float top = r.center.y + pitch * units / 2 - 4;
            var outC = new Vector2(r.xMin + r.width * OutX, r.center.y + 10);
            const float outR = 13;

            // Links from every square of the picture to every unit (drawn first, behind everything).
            for (int j = 0; j < units; j++)
            {
                var uMin = new Vector2(ux, top - j * pitch - uSide);
                var to = new Vector2(uMin.x - 2, uMin.y + uSide / 2);
                double max = 1e-6;
                for (int i = 0; i < inputs; i++) max = System.Math.Max(max, System.Math.Abs(net.w1[j * inputs + i]));
                for (int i = 0; i < inputs; i++)
                {
                    double w = net.w1[j * inputs + i];
                    float m = (float)(System.Math.Abs(w) / max);
                    float lit = pixels != null ? Mathf.Lerp(.35f, 1, (float)pixels[i]) : .6f;
                    float f = flash1[j * inputs + i];
                    if (m < .12f && f <= 0) continue; // the faintest links only add haze
                    var from = new Vector2(inMin.x + (i % n + .5f) * inCell, inMin.y + (n - 1 - i / n + .5f) * inCell);
                    var c = Color.Lerp(w >= 0 ? XgDark.Good : XgDark.Bad, Color.white, f * .7f);
                    XgSoftDraw.Seg(vh, from, to, .4f + 1.6f * m, .8f, Alpha(c, (.05f + .4f * m * m) * lit + .5f * f));
                }
            }
            // Units to the answer.
            double max2 = 1e-6;
            foreach (var w in net.w2) max2 = System.Math.Max(max2, System.Math.Abs(w));
            for (int j = 0; j < units && j < net.w2.Length; j++)
            {
                var from = new Vector2(ux + uSide + 2, top - j * pitch - uSide / 2);
                float m = (float)(System.Math.Abs(net.w2[j]) / max2), f = flash2[j];
                var c = Color.Lerp(net.w2[j] >= 0 ? XgDark.Good : XgDark.Bad, Color.white, f * .7f);
                XgSoftDraw.Seg(vh, from, outC - new Vector2(outR, 0), 1 + 5 * m, 2, Alpha(c, .25f + .5f * m * Mathf.Lerp(.5f, 1, act[j]) + .3f * f));
            }

            // The picture going in.
            XgDraw.Box(vh, inMin - Vector2.one, inMin + Vector2.one * (inSide + 1), XgDark.Line);
            XgDraw.Box(vh, inMin, inMin + Vector2.one * inSide, XgDark.Page);
            if (pixels != null)
                for (int i = 0; i < inputs; i++)
                {
                    var min = new Vector2(inMin.x + (i % n) * inCell, inMin.y + (n - 1 - i / n) * inCell);
                    XgDraw.Box(vh, min, min + Vector2.one * inCell, Color.Lerp(XgDark.Page, XgDark.Ink, Mathf.Clamp01((float)pixels[i])));
                }

            // Heat maps: what each unit looks for, glowing with how much this picture lights it.
            for (int j = 0; j < units; j++)
            {
                var uMin = new Vector2(ux, top - j * pitch - uSide);
                var c = uMin + Vector2.one * (uSide / 2);
                XgSoftDraw.Disc(vh, c, uSide * .5f, uSide * .35f, Alpha(XgDark.Good, .18f * act[j]));
                XgDraw.Box(vh, uMin - Vector2.one, uMin + Vector2.one * (uSide + 1), XgDark.Line);
                double max = 1e-6;
                for (int i = 0; i < inputs; i++) max = System.Math.Max(max, System.Math.Abs(net.w1[j * inputs + i]));
                float cell = uSide / n;
                for (int i = 0; i < inputs; i++)
                {
                    double w = net.w1[j * inputs + i];
                    var min = new Vector2(uMin.x + (i % n) * cell, uMin.y + (n - 1 - i / n) * cell);
                    XgDraw.Box(vh, min, min + Vector2.one * cell, Color.Lerp(XgDark.Page, w >= 0 ? XgDark.Good : XgDark.Bad, (float)(System.Math.Abs(w) / max)));
                }
            }

            // The answer: green when it says "a 0", red when not; brighter the surer.
            float sure = Mathf.Abs((float)answer - .5f) * 2;
            var col = answer > .5 ? XgDark.Good : XgDark.Bad;
            XgSoftDraw.Disc(vh, outC, outR + 2, 10 + 8 * sure, Alpha(col, .15f + .35f * sure));
            XgDraw.Disc(vh, outC, outR, XgDark.Page);
            XgDraw.Disc(vh, outC, outR - 2, Alpha(col, .25f + .75f * sure));
        }
    }
}
