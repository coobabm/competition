using System.Collections.Generic;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 「它怎么看的」 (它在学_可视化借鉴设计 §2.3, after CNN Explainer): a picture question on the sheet, opened over the
    /// curve card. A real tiny conv net (XgTinyConvNet) looks at an 8×8 picture standing for the question
    /// (XgTinyLook): the input, the four maps its 3×3 kernels make, the four 2×2 max-pooled maps, and its answer as
    /// two bars. The map that pushed hardest toward the answer is outlined: red when the answer is wrong (the one that
    /// "looked wrong"), green when right. Clicking a map slides its 3×3 window over the input one cell at a time, with
    /// the nine pixels, the nine weights and the sum below. The answer always agrees with the sheet's ✓/×.
    /// Presentation only; closing it changes nothing.
    /// </summary>
    public sealed class XgConvLook : MonoBehaviour
    {
        /// <summary>Seconds per window position while sliding (36 positions).</summary>
        const float SlideStep = .09f;
        const int Maps = XgTinyConvNet.Kernels, Cells = XgTinyConvNet.MapCells;
        /// <summary>The diagram's own size (layers above the slide detail); it is scaled to fit the card and centred.</summary>
        const float StageW = XgConvLayersGraphic.Width, StageH = XgConvLayersGraphic.Height + 8 + XgConvDetailGraphic.Height;
        /// <summary>Room kept clear above (header) and below (caption and footer) the diagram, and its scale limits.</summary>
        const float StageTop = 36, StageBottom = 68, StageSide = 12, MinScale = .6f, MaxScale = 1.6f;

        XgUi ui;
        XgJuice fx;
        TMP_Text title, caption, footer, hint, sum, yesLabel, noLabel, topLabel;
        readonly TMP_Text[] names = new TMP_Text[Maps], patch = new TMP_Text[9], weights = new TMP_Text[9], cells = new TMP_Text[Cells];
        RectTransform detail, inner, stage;
        XgConvLayersGraphic layers;
        XgConvDetailGraphic detailGraph;
        XgSheetItem item;
        XgSheetLook look;
        bool shownRight, shownDirty;
        int selected = -1;
        float slide;

        /// <summary>The overlay over <paramref name="card"/> (hidden until a picture question is clicked).</summary>
        public static XgConvLook Create(RectTransform card, XgUi ui, XgJuice fx)
        {
            var rt = Rect("ConvLook", card, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var look = rt.gameObject.AddComponent<XgConvLook>();
            look.ui = ui; look.fx = fx;
            look.Build(rt);
            rt.gameObject.SetActive(false);
            return look;
        }

        public bool Showing => gameObject.activeSelf;
        public XgSheetItem Item => item;

        void Build(RectTransform rt)
        {
            Panel(rt, XgDark.Line);
            inner = Rect("Inner", rt, Vector2.zero, Vector2.one, new Vector2(1, 1), new Vector2(-1, -1));
            Panel(inner, XgDark.Card);
            var head = Rect("Header", inner, new Vector2(0, 1), Vector2.one, new Vector2(0, -28), Vector2.zero);
            Panel(head, XgDark.Hud).raycastTarget = false;
            title = ui.Text(Rect("Title", head, Vector2.zero, Vector2.one, new Vector2(12, 0), new Vector2(-84, 0)), "", 14, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold;
            title.textWrappingMode = TextWrappingModes.NoWrap; title.overflowMode = TextOverflowModes.Ellipsis;
            var close = ui.Button(head, T("收起", "Close"), Hide, 12);
            close.rt.gameObject.name = "CloseConvLook";
            PlaceTopRight(close, 76, 3, 70, 22);

            // The diagram is drawn at a fixed size on a stage that Fit() scales to the card (the card is much larger than
            // the diagram on a wide window and smaller on a narrow one).
            stage = Rect("Stage", inner, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            stage.sizeDelta = new Vector2(StageW, StageH);

            // The layers: input → 4 maps → 4 pooled maps → the answer.
            var area = TopLeft("Layers", stage, 0, 0, XgConvLayersGraphic.Width, XgConvLayersGraphic.Height);
            layers = area.gameObject.AddComponent<XgConvLayersGraphic>();
            layers.raycastTarget = false;
            Label(area, T("输入 8×8", "Input 8×8"), 0, 22, 152);
            Label(area, T("3×3 卷积 → 4 张边缘图", "3×3 kernels → 4 maps"), XgConvLayersGraphic.MapX - 4, 0, 130);
            Label(area, T("2×2 池化", "2×2 pool"), XgConvLayersGraphic.PoolX - 4, 0, 80);
            Label(area, T("它的回答", "Its answer"), XgConvLayersGraphic.BarX, 0, 180);
            for (int m = 0; m < Maps; m++)
            {
                names[m] = ui.Text(TopLeft("Name" + m, area, XgConvLayersGraphic.MapX + XgConvLayersGraphic.MapSide + 6, XgConvLayersGraphic.MapY(m) + 10, 70, 30), "", 11, XgDark.Muted, TextAlignmentOptions.TopLeft);
                // The map itself is the button: click to slide its window.
                int k = m;
                var hit = TopLeft("Map" + m, area, XgConvLayersGraphic.MapX - 4, XgConvLayersGraphic.MapY(m) - 3, XgConvLayersGraphic.MapSide + 80, XgConvLayersGraphic.MapSide + 6);
                var img = Panel(hit, new Color(1, 1, 1, .001f));
                var b = hit.gameObject.AddComponent<Button>();
                b.targetGraphic = img; b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => Select(k));
                UiTip.Add(hit, "点一下：看这个 3×3 的核在输入上一格一格滑过去。", "Click: watch this 3×3 kernel slide over the input, one cell at a time.");
            }
            yesLabel = ui.Text(TopLeft("Yes", area, XgConvLayersGraphic.BarX, XgConvLayersGraphic.YesY - 18, 186, 16), "", 12, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            noLabel = ui.Text(TopLeft("No", area, XgConvLayersGraphic.BarX, XgConvLayersGraphic.NoY - 18, 186, 16), "", 12, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            topLabel = ui.Text(TopLeft("Top", area, XgConvLayersGraphic.BarX, XgConvLayersGraphic.NoY + 26, 186, 60), "", 11, XgDark.Muted, TextAlignmentOptions.TopLeft);
            UiTip.Add(area, () => T(
                "一张真的小卷积网：4 个 3×3 的核各扫一遍输入，得到 4 张 6×6 的图（绿 = 正，红 = 负，负的会被 ReLU 抹成 0）；每 2×2 取最大，缩成 3×3；最后一层把这 36 个数加权，算出它像哪个数字。\n框出来的那张，是把它的回答推得最远的一张。",
                "A real tiny conv net: 4 kernels of 3×3 each sweep the input into a 6×6 map (green +, red −; ReLU turns negatives to 0); each 2×2 keeps its largest, leaving 3×3; the last layer weighs those 36 numbers into which digit it looks like.\nThe outlined map pushed its answer furthest."));

            // The detail: the window's nine pixels × the kernel's nine weights = one cell of the big map.
            detail = TopLeft("Detail", stage, 0, XgConvLayersGraphic.Height + 8, XgConvDetailGraphic.Width, XgConvDetailGraphic.Height);
            detailGraph = detail.gameObject.AddComponent<XgConvDetailGraphic>();
            detailGraph.raycastTarget = false;
            for (int i = 0; i < 9; i++)
            {
                int r = i / 3, c = i % 3;
                patch[i] = Num(detail, XgConvDetailGraphic.PatchX + c * XgConvDetailGraphic.Big, XgConvDetailGraphic.Top + r * XgConvDetailGraphic.Big, XgConvDetailGraphic.Big, 10);
                weights[i] = Num(detail, XgConvDetailGraphic.KernelX + c * XgConvDetailGraphic.Big, XgConvDetailGraphic.Top + r * XgConvDetailGraphic.Big, XgConvDetailGraphic.Big, 10);
            }
            for (int i = 0; i < Cells; i++)
            {
                int r = i / XgTinyConvNet.MapSide, c = i % XgTinyConvNet.MapSide;
                cells[i] = Num(detail, XgConvDetailGraphic.MapX + c * XgConvDetailGraphic.Small, XgConvDetailGraphic.Top + r * XgConvDetailGraphic.Small, XgConvDetailGraphic.Small, 8);
            }
            ui.Text(TopLeft("Times", detail, XgConvDetailGraphic.PatchX + 3 * XgConvDetailGraphic.Big, XgConvDetailGraphic.Top + 30, XgConvDetailGraphic.KernelX - XgConvDetailGraphic.PatchX - 3 * XgConvDetailGraphic.Big, 30), "×", 16, XgDark.Muted, TextAlignmentOptions.Center);
            Label(detail, T("窗口里的 9 格", "9 pixels in the window"), XgConvDetailGraphic.PatchX, 0, 110);
            Label(detail, T("核的 9 个权重", "the kernel's 9 weights"), XgConvDetailGraphic.KernelX, 0, 110);
            sum = ui.Text(TopLeft("Sum", detail, XgConvDetailGraphic.SumX, XgConvDetailGraphic.Top, XgConvDetailGraphic.MapX - XgConvDetailGraphic.SumX - 6, 96), "", 12, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            float ex = XgConvDetailGraphic.MapX + XgTinyConvNet.MapSide * XgConvDetailGraphic.Small + 14;
            Label(detail, T("这张边缘图", "the map"), XgConvDetailGraphic.MapX, 0, 110);
            ui.Text(TopLeft("Explain", detail, ex, XgConvDetailGraphic.Top, XgConvDetailGraphic.Width - ex, 108),
                T("窗口每往右挪一格，这张图就多一个数：9 个像素和 9 个权重对应相乘，加起来，再加偏置。笔画和核的方向一致的地方最亮。",
                    "Each step of the window adds one number to the map: 9 pixels times 9 weights, added up, plus the bias. It lights up most where the strokes run the kernel's way."),
                11, XgDark.Muted, TextAlignmentOptions.TopLeft);
            hint = ui.Text(TopLeft("Hint", stage, 0, XgConvLayersGraphic.Height + 8, XgConvDetailGraphic.Width, XgConvDetailGraphic.Height), "", 13, XgDark.Muted, TextAlignmentOptions.MidlineLeft);

            caption = ui.Text(Rect("Caption", inner, Vector2.zero, new Vector2(1, 0), new Vector2(12, 22), new Vector2(-12, 60)), "", 13, XgDark.Ink, TextAlignmentOptions.TopLeft);
            caption.enableAutoSizing = true; caption.fontSizeMin = 10; caption.fontSizeMax = 13;
            footer = ui.Text(Rect("Footer", inner, Vector2.zero, new Vector2(1, 0), new Vector2(12, 4), new Vector2(-12, 20)), "", 11, XgDark.Dim, TextAlignmentOptions.MidlineLeft);
            footer.enableAutoSizing = true; footer.fontSizeMin = 9; footer.fontSizeMax = 11;
        }

        void Label(RectTransform parent, string text, float x, float y, float w)
        {
            var t = ui.Text(TopLeft("Label", parent, x, y, w, 16), text, 11, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            t.textWrappingMode = TextWrappingModes.NoWrap;
        }

        TMP_Text Num(RectTransform parent, float x, float y, float side, float size)
        {
            var t = ui.Text(TopLeft("Num", parent, x, y, side, side), "", size, XgDark.Ink, TextAlignmentOptions.Center);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            return t;
        }

        /// <summary>Opens the look for a question (clicking the open one again closes it).</summary>
        public void Toggle(XgSim sim, XgSheetItem it)
        {
            if (Showing && item == it) { Hide(); return; }
            Show(sim, it);
            fx?.Play(XgJuice.Sfx.Id.Click);
        }

        public void Show(XgSim sim, XgSheetItem it)
        {
            var l = XgTinyLook.For(sim, it);
            if (l == null) { Hide(); return; }
            item = it; look = l;
            shownRight = it.right; shownDirty = sim.SheetDirty(it);
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Fit();
            selected = -1;
            var net = XgTinyConvNet.Digits;
            layers.Set(look, net);
            detailGraph.Set(look, net);
            Fill(net);
            Select(-1);
        }

        public void Hide()
        {
            if (gameObject.activeSelf) gameObject.SetActive(false);
            item = null; look = null; selected = -1;
        }

        /// <summary>After the sheet refreshed: closes when the question left, and redraws when its answer flipped.</summary>
        public void Sync(XgSim sim, List<XgSheetItem> sheet)
        {
            if (!Showing) return;
            if (item == null || sheet == null || !sheet.Contains(item)) { Hide(); return; }
            if (item.right != shownRight || sim.SheetDirty(item) != shownDirty) Show(sim, item);
        }

        static string Signed(double v, string f = "0.0") => (v < 0 ? "−" : "") + N(System.Math.Abs(v), f);

        string Name(int m) => T(XgTinyConvNet.KernelNames[m], XgTinyConvNet.KernelNamesEn[m]);

        void Fill(XgTinyConvNet net)
        {
            bool right = look.Right;
            Color said = right ? XgDark.Good : XgDark.Bad;
            title.text = T("它怎么看的 · 这是 " + look.asked + " 吗？", "How it sees · is this a " + look.asked + "?")
                + "  " + Hex(said) + (look.said ? T("它答 是", "it says yes") : T("它答 否", "it says no")) + " " + (right ? "✓" : "×") + "</color>";
            for (int m = 0; m < Maps; m++)
            {
                double max = 0;
                for (int i = 0; i < Cells; i++) max = System.Math.Max(max, look.trace.maps[m * Cells + i]);
                bool isKey = m == look.key;
                names[m].text = (isKey ? Hex(right ? XgDark.Good : XgDark.Bad) : "") + Name(m) + (isKey ? "</color>" : "")
                    + "\n<size=85%>" + Hex(XgDark.Dim) + T("最亮 ", "peak ") + N(max, "0.0") + "</color></size>";
            }
            double p = look.pYes;
            yesLabel.text = (look.said ? Hex(said) : "") + T("是 " + look.asked, "a " + look.asked) + "  " + N(p * 100, "0") + "%" + (look.said ? (right ? " ✓" : " ×") + "</color>" : "");
            noLabel.text = (!look.said ? Hex(said) : "") + T("不是", "not") + "  " + N((1 - p) * 100, "0") + "%" + (!look.said ? (right ? " ✓" : " ×") + "</color>" : "");
            topLabel.text = T("十个数字里它觉得最像 ", "Of all ten digits it looks most like ") + look.top + "（" + N(look.trace.probs[look.top] * 100, "0") + "%）"
                + "\n" + T("图上画的是 ", "The picture shows a ") + look.digit;

            string key = Name(look.key);
            string text;
            if (right)
                text = T("它答对了。把它推向「" + (look.said ? "是 " + look.asked : "不是 " + look.asked) + "」最多的是「" + key + "」那张图（" + Signed(look.push[look.key]) + "）：框成绿色的那张。",
                    "Right. The \"" + key + "\" map pushed hardest toward " + (look.said ? "\"a " + look.asked + "\"" : "\"not a " + look.asked + "\"") + " (" + Signed(look.push[look.key]) + "): outlined in green.");
            else
                text = T("它答错了：把这张 " + look.digit + " 看成了 " + look.seen + "。「" + key + "」那张图看偏了：它亮的地方把答案往 " + look.seen + " 推了 " + Signed(look.push[look.key]) + "（比 " + look.against + " 多），框成红色的那张。",
                    "Wrong: it took this " + look.digit + " for a " + look.seen + ". The \"" + key + "\" map looked the wrong way: where it lights up pushed the answer toward " + look.seen + " by " + Signed(look.push[look.key]) + " over " + look.against + ". Outlined in red.");
            if (look.blendDigit >= 0)
                text += T("这张写得含糊：掺了 " + N(look.blend * 100, "0") + "% 的「" + look.blendDigit + "」。", " This one is sloppy: " + N(look.blend * 100, "0") + "% of a " + look.blendDigit + " is mixed in.");
            caption.text = text;
            footer.text = (look.standIn ? T("原题的图比 8×8 大，这里换一张 8×8 的手写数字，答案和原题一样。", "The real picture is bigger than 8×8; an 8×8 handwritten digit with the same answer stands in. ") : "")
                + T("真卷积网：" + net.ParameterCount + " 个参数，练过 300 张手写数字，在你的 CPU 上算的。", "A real conv net: " + net.ParameterCount + " parameters, trained on 300 handwritten digits, computed on your CPU.");
        }

        void Select(int m)
        {
            selected = m;
            slide = 0;
            bool on = m >= 0;
            detail.gameObject.SetActive(on);
            hint.gameObject.SetActive(!on);
            if (!on)
            {
                hint.text = T("点左边任意一张边缘图：看它的 3×3 窗口怎么在输入上一格一格滑过去，每一格都是 9 个像素和 9 个权重对应相乘再加起来。",
                    "Click any map on the left: watch its 3×3 window slide over the input one cell at a time. Each cell is 9 pixels times 9 weights, added up.");
                layers.Slide(-1, 0);
                return;
            }
            var net = XgTinyConvNet.Digits;
            for (int i = 0; i < 9; i++)
            {
                double w = net.kernels[m * 9 + i];
                weights[i].text = Signed(w, "0.00");
                weights[i].color = XgDark.Ink;
            }
            fx?.Play(XgJuice.Sfx.Id.Tick, 1.2f, .4f);
            // Reduced motion: the whole map at once, the window resting on its last position.
            if (fx != null && fx.Reduced) slide = Cells * SlideStep;
            Show(Shown());
        }

        int Shown() => Mathf.Clamp(Mathf.FloorToInt(slide / SlideStep) + 1, 1, Cells);

        void OnRectTransformDimensionsChange() { if (stage != null) Fit(); }

        /// <summary>Scales the stage to the room between the header and the caption, centred, within the scale limits.</summary>
        void Fit()
        {
            var r = inner.rect;
            float w = r.width - 2 * StageSide, h = r.height - StageTop - StageBottom;
            if (w <= 0 || h <= 0) return;
            float scale = Mathf.Clamp(Mathf.Min(w / StageW, h / StageH), MinScale, MaxScale);
            stage.localScale = new Vector3(scale, scale, 1);
            stage.anchoredPosition = new Vector2(0, (StageBottom - StageTop) / 2);
        }

        void Update()
        {
            if (selected < 0 || look == null) return;
            int before = Shown();
            slide += Time.unscaledDeltaTime;
            int now = Shown();
            if (now != before) { Show(now); if (now == Cells) fx?.Play(XgJuice.Sfx.Id.Tick, 1.5f, .25f); }
        }

        /// <summary>The first <paramref name="count"/> window positions done; the last one is the window's current place.</summary>
        void Show(int count)
        {
            int m = selected, side = XgTinyConvNet.MapSide, n = XgTinyData.Side;
            layers.Slide(m, count);
            detailGraph.Slide(m, count);
            int k = count - 1, r = k / side, c = k % side;
            var x = look.sample.pixels;
            for (int i = 0; i < 9; i++) patch[i].text = N(x[(r + i / 3) * n + c + i % 3], "0.0");
            for (int i = 0; i < Cells; i++)
            {
                double v = look.trace.maps[m * Cells + i];
                cells[i].text = i < count ? Signed(v) : "";
                cells[i].color = i == k ? Color.white : XgDark.Ink;
            }
            double z = look.trace.maps[m * Cells + k], b = XgTinyConvNet.Digits.kernelBias[m];
            sum.text = T("相乘再加 ", "Sum ") + Signed(z - b, "0.00") + "\n" + T("加偏置 ", "plus bias ") + Signed(b, "0.00") + "\n= " + Hex(z >= 0 ? XgDark.Good : XgDark.Bad) + "<b>" + Signed(z, "0.00") + "</b></color>"
                + "\n<size=85%>" + Hex(XgDark.Muted) + (z > 0 ? T("ReLU 后留下", "kept by ReLU") : T("ReLU 后变 0", "ReLU makes it 0")) + "</color></size>";
        }
    }

    /// <summary>
    /// The layered view: the input picture, four signed 6×6 maps, four pooled 3×3 maps, the answer bars, and thin
    /// links between the columns. Fixed layout (<see cref="Width"/> × <see cref="Height"/>, measured from the top-left)
    /// so the overlay can place its labels and click targets on it.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgConvLayersGraphic : MaskableGraphic
    {
        public const float Width = 616, Height = 232;
        public const float InX = 0, InY = 40, InCell = 19, MapX = 200, MapCell = 8, MapSide = MapCell * XgTinyConvNet.MapSide;
        public const float PoolX = 330, PoolCell = 12, PoolSide = PoolCell * XgTinyConvNet.PoolSide, BarX = 430, BarW = 180, BarH = 12, YesY = 86, NoY = 136;
        public static float MapY(int m) => 22 + m * 53;
        XgSheetLook look;
        XgTinyConvNet net;
        int sliding = -1, slid;

        public void Set(XgSheetLook l, XgTinyConvNet n) { look = l; net = n; sliding = -1; SetVerticesDirty(); }
        public void Slide(int map, int count) { sliding = map; slid = count; SetVerticesDirty(); }

        Rect area;
        Vector2 P(float x, float y) => new Vector2(area.xMin + x, area.yMax - y);
        void Box(VertexHelper vh, float x, float y, float w, float h, Color c) => XgDraw.Box(vh, P(x, y + h), P(x + w, y), c);
        void Frame(VertexHelper vh, float x, float y, float w, float h, float t, Color c)
        {
            Box(vh, x - t, y - t, w + 2 * t, t, c); Box(vh, x - t, y + h, w + 2 * t, t, c);
            Box(vh, x - t, y, t, h, c); Box(vh, x + w, y, t, h, c);
        }
        static Color Alpha(Color c, float a) { c.a = a; return c; }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            area = GetPixelAdjustedRect();
            if (look == null || net == null) return;
            var t = look.trace;
            int n = XgTinyData.Side, side = XgTinyConvNet.MapSide, ps = XgTinyConvNet.PoolSide, cells = XgTinyConvNet.MapCells, pcells = XgTinyConvNet.PoolCells;
            Color keyColor = look.Right ? XgDark.Good : XgDark.Bad;
            float inSide = InCell * n;

            // Links first, behind everything.
            for (int m = 0; m < XgTinyConvNet.Kernels; m++)
            {
                float my = MapY(m) + MapSide / 2;
                bool key = m == look.key;
                Color c = key ? Alpha(keyColor, .7f) : XgDark.Hairline;
                XgDraw.Seg(vh, P(InX + inSide + 4, InY + inSide / 2), P(MapX - 4, my), key ? 2 : 1, c);
                XgDraw.Seg(vh, P(MapX + MapSide + 4, my), P(PoolX - 4, MapY(m) + 6 + PoolSide / 2), key ? 2 : 1, c);
                float w = Mathf.Clamp01((float)System.Math.Abs(look.push[m]) / MaxPush());
                Color to = look.push[m] >= 0 ? Alpha(keyColor, .25f + .5f * w) : XgDark.Hairline;
                XgDraw.Seg(vh, P(PoolX + PoolSide + 4, MapY(m) + 6 + PoolSide / 2), P(BarX - 6, (look.said ? YesY : NoY) + BarH / 2), .5f + 2.5f * w, to);
            }

            // The input, with the sliding window.
            Box(vh, InX - 1, InY - 1, inSide + 2, inSide + 2, XgDark.Line);
            for (int i = 0; i < XgTinyData.Pixels; i++)
                Box(vh, InX + (i % n) * InCell, InY + (i / n) * InCell, InCell, InCell, Color.Lerp(XgDark.Page, XgDark.Ink, Mathf.Clamp01((float)look.sample.pixels[i])));
            if (sliding >= 0 && slid > 0)
            {
                int k = slid - 1, r = k / side, c = k % side;
                Frame(vh, InX + c * InCell, InY + r * InCell, 3 * InCell, 3 * InCell, 2, XgDark.Gold);
            }

            // The maps, signed and on one scale so they compare.
            double mapMax = 1e-9, poolMax = 1e-9;
            foreach (var v in t.maps) mapMax = System.Math.Max(mapMax, System.Math.Abs(v));
            foreach (var v in t.pooled) poolMax = System.Math.Max(poolMax, v);
            for (int m = 0; m < XgTinyConvNet.Kernels; m++)
            {
                float y0 = MapY(m);
                bool key = m == look.key, slidingThis = m == sliding;
                if (key) Frame(vh, MapX, y0, MapSide, MapSide, 2, keyColor);
                else Box(vh, MapX - 1, y0 - 1, MapSide + 2, MapSide + 2, slidingThis ? XgDark.Gold : XgDark.Line);
                if (slidingThis && key) Frame(vh, MapX - 2, y0 - 2, MapSide + 4, MapSide + 4, 1, XgDark.Gold);
                for (int i = 0; i < cells; i++)
                {
                    double v = t.maps[m * cells + i];
                    bool hidden = slidingThis && i >= slid;
                    Color c = hidden ? XgDark.Page : Color.Lerp(XgDark.Page, v >= 0 ? XgDark.Good : XgDark.Bad, (float)(System.Math.Abs(v) / mapMax));
                    Box(vh, MapX + (i % side) * MapCell, y0 + (i / side) * MapCell, MapCell, MapCell, c);
                }
                if (slidingThis && slid > 0)
                {
                    int k = slid - 1;
                    Frame(vh, MapX + (k % side) * MapCell, y0 + (k / side) * MapCell, MapCell, MapCell, 1, Color.white);
                }
                // Pooled: what survives ReLU and the 2×2 max.
                float py = y0 + 6;
                Box(vh, PoolX - 1, py - 1, PoolSide + 2, PoolSide + 2, key ? keyColor : XgDark.Line);
                for (int i = 0; i < pcells; i++)
                    Box(vh, PoolX + (i % ps) * PoolCell, py + (i / ps) * PoolCell, PoolCell, PoolCell, Color.Lerp(XgDark.Page, XgDark.Good, (float)(t.pooled[m * pcells + i] / poolMax)));
            }

            // The answer: two bars, the one it said in green (right) or red (wrong).
            Color said = look.Right ? XgDark.Good : XgDark.Bad;
            Bar(vh, YesY, (float)look.pYes, look.said ? said : XgDark.Dim);
            Bar(vh, NoY, 1 - (float)look.pYes, !look.said ? said : XgDark.Dim);
        }

        float MaxPush()
        {
            double max = 1e-6;
            foreach (var v in look.push) max = System.Math.Max(max, System.Math.Abs(v));
            return (float)max;
        }

        void Bar(VertexHelper vh, float y, float k, Color c)
        {
            Box(vh, BarX, y, BarW, BarH, XgDark.Track);
            Box(vh, BarX, y, BarW * Mathf.Clamp01(k), BarH, c);
        }
    }

    /// <summary>
    /// The window in detail: its nine pixels, the kernel's nine weights (green +, red −) and the map growing cell by
    /// cell, all big enough for the numbers the overlay writes on them.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgConvDetailGraphic : MaskableGraphic
    {
        public const float Width = 616, Height = 130, Top = 18, Big = 30, Small = 18;
        public const float PatchX = 0, KernelX = 126, SumX = 226, MapX = 340;
        XgSheetLook look;
        XgTinyConvNet net;
        int map = -1, count;

        public void Set(XgSheetLook l, XgTinyConvNet n) { look = l; net = n; map = -1; SetVerticesDirty(); }
        public void Slide(int m, int c) { map = m; count = c; SetVerticesDirty(); }

        Rect area;
        Vector2 P(float x, float y) => new Vector2(area.xMin + x, area.yMax - y);
        void Box(VertexHelper vh, float x, float y, float w, float h, Color c) => XgDraw.Box(vh, P(x, y + h), P(x + w, y), c);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            area = GetPixelAdjustedRect();
            if (look == null || net == null || map < 0 || count < 1) return;
            int side = XgTinyConvNet.MapSide, n = XgTinyData.Side, cells = XgTinyConvNet.MapCells;
            int k = count - 1, r = k / side, c = k % side;
            double wMax = 1e-9;
            for (int i = 0; i < 9; i++) wMax = System.Math.Max(wMax, System.Math.Abs(net.kernels[map * 9 + i]));
            Box(vh, PatchX - 1, Top - 1, 3 * Big + 2, 3 * Big + 2, XgDark.Gold);
            Box(vh, KernelX - 1, Top - 1, 3 * Big + 2, 3 * Big + 2, XgDark.Line);
            for (int i = 0; i < 9; i++)
            {
                float x = (i % 3) * Big, y = Top + (i / 3) * Big;
                double v = look.sample.pixels[(r + i / 3) * n + c + i % 3], w = net.kernels[map * 9 + i];
                // Dim enough that the numbers on top stay readable.
                Box(vh, PatchX + x + .5f, y + .5f, Big - 1, Big - 1, Color.Lerp(XgDark.Page, XgDark.Dim, Mathf.Clamp01((float)v)));
                Box(vh, KernelX + x + .5f, y + .5f, Big - 1, Big - 1, Color.Lerp(XgDark.Page, w >= 0 ? XgDark.Good : XgDark.Bad, .55f * (float)(System.Math.Abs(w) / wMax)));
            }
            double mMax = 1e-9;
            for (int i = 0; i < cells; i++) mMax = System.Math.Max(mMax, System.Math.Abs(look.trace.maps[map * cells + i]));
            Box(vh, MapX - 1, Top - 1, side * Small + 2, side * Small + 2, XgDark.Line);
            for (int i = 0; i < cells; i++)
            {
                double v = look.trace.maps[map * cells + i];
                Color col = i < count ? Color.Lerp(XgDark.Page, v >= 0 ? XgDark.Good : XgDark.Bad, .6f * (float)(System.Math.Abs(v) / mMax)) : XgDark.Page;
                Box(vh, MapX + (i % side) * Small + .5f, Top + (i / side) * Small + .5f, Small - 1, Small - 1, col);
            }
            float cx = MapX + c * Small, cy = Top + r * Small;
            Box(vh, cx - 1, cy - 1, Small + 2, 1, Color.white); Box(vh, cx - 1, cy + Small, Small + 2, 1, Color.white);
            Box(vh, cx - 1, cy, 1, Small, Color.white); Box(vh, cx + Small, cy, 1, Small, Color.white);
        }
    }
}
