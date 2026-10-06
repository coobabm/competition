using System;
using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;
using InnerVoice = LingGuangV05.Desktop.Story.InnerVoice;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 皮层拓扑: 灵光's one brain as a side view, each region drawn wired by its architecture and animated
    /// (<see cref="XgCortexGraphic"/>), with the wiring of every region on the right and the GPU load under the brain.
    /// The first look at it is a shock to the protagonist, said once.
    /// </summary>
    public sealed partial class XgBoardPage
    {
        enum Mode { Concepts, Cortex, Atlas }

        /// <summary>The cells a region shows at most (the deepest glyph, VGG, has 49).</summary>
        public const int CortexCells = 60;

        Mode mode = Mode.Cortex;
        RectTransform conceptView, cortexView;
        XgBtn conceptTab, cortexTab, atlasTab;
        XgCortexGraphic cortex;
        TMP_Text gpuLabel, gpuValue, detail, legend;
        RectTransform gpuFill;
        Image gpuFillImage;
        readonly XgBtn[] regionRows = new XgBtn[4];
        readonly TMP_Text[] regionStatus = new TMP_Text[4];
        int selectedRegion = -1;
        float animTimer, gpuLoad;
        bool cortexInitialised;

        void BuildModeTabs(RectTransform card)
        {
            conceptTab = ModeTab(card, 312, Mode.Concepts);
            cortexTab = ModeTab(card, 212, Mode.Cortex);
            atlasTab = ModeTab(card, 112, Mode.Atlas);
            UiTip.Add(conceptTab.rt, () => Lang.T("概念板：每个区记住的概念，一格一个。绿是「是」，红是「否」，越深越确定。"));
            UiTip.Add(cortexTab.rt, () => Lang.T("皮层拓扑：它的脑子从侧面看。每个区画成它现在的接法，正在训练的区会亮。"));
            UiTip.Add(atlasTab.rt, () => Lang.T("接法图鉴：解锁过的每一种结构，都是给某个区接线的一种办法。"));
        }

        XgBtn ModeTab(RectTransform card, float fromRight, Mode m)
        {
            var b = ui.Button(card, "", () => SetMode(m), 14);
            PlaceTopRight(b, fromRight, 13, 96, 30);
            return b;
        }

        void SetMode(Mode m)
        {
            if (mode == m) return;
            mode = m;
            Fx.Play(XgJuice.Sfx.Id.Click);
            shownCards = -1;
            Refresh();
            if (m == Mode.Cortex) VoiceCortex();
        }

        void RefreshModeTabs()
        {
            conceptTab.Set(Lang.T("概念板"), true, mode == Mode.Concepts ? XgPalette.Accent : XgPalette.Button, mode == Mode.Concepts ? Color.white : XgPalette.Ink);
            cortexTab.Set(Lang.T("皮层拓扑"), true, mode == Mode.Cortex ? XgPalette.Accent : XgPalette.Button, mode == Mode.Cortex ? Color.white : XgPalette.Ink);
            atlasTab.Set(Lang.T("接法图鉴"), true, mode == Mode.Atlas ? XgPalette.Accent : XgPalette.Button, mode == Mode.Atlas ? Color.white : XgPalette.Ink);
            Show(conceptView, mode == Mode.Concepts);
            Show(cortexView, mode == Mode.Cortex);
            Show(topologyView, mode == Mode.Atlas);
        }

        static void Show(RectTransform rt, bool on) { if (rt != null && rt.gameObject.activeSelf != on) rt.gameObject.SetActive(on); }

        // ---------------------------------------------------------------- build

        void BuildCortex(RectTransform card)
        {
            cortexView = Rect("Cortex", card, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var map = Rect("Map", cortexView, Vector2.zero, new Vector2(.72f, 1), new Vector2(12, 12), new Vector2(-6, -52));
            Panel(map, new Color32(250, 251, 254, 255)).raycastTarget = false;
            var brain = Rect("Brain", map, Vector2.zero, Vector2.one, new Vector2(6, 46), new Vector2(-6, -4));
            cortex = brain.gameObject.AddComponent<XgCortexGraphic>();
            cortex.raycastTarget = false;
            cortex.Font = ui.font;
            cortex.Captions = CortexCaptions();
            cortex.Tokens = new[] { "这", "本", "书", "我", "看", "过" };
            cortex.Source = new[] { "那", "只", "猫", "坐", "在", "垫" };

            var gpu = Rect("Gpu", map, Vector2.zero, new Vector2(1, 0), new Vector2(14, 10), new Vector2(-14, 42));
            gpuLabel = ui.Text(Rect("Label", gpu, new Vector2(0, 1), Vector2.one, new Vector2(0, -18), Vector2.zero), "", 13, XgPalette.Muted, TextAlignmentOptions.MidlineLeft);
            gpuValue = ui.Text(Rect("Value", gpu, new Vector2(0, 1), Vector2.one, new Vector2(0, -18), Vector2.zero), "", 13, XgPalette.Accent, TextAlignmentOptions.MidlineRight);
            var bar = Rect("Bar", gpu, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 14));
            gpuFill = Bar(bar, "Fill", XgPalette.Button, XgPalette.Accent);
            gpuFillImage = gpuFill.GetComponent<Image>();
            UiTip.Add(gpu, () => Lang.T("显卡有多忙。训练一轮时，正在练的那个区整片亮起来，显卡就被它占满一截；到了第六阶段整颗脑子一起亮，一台机器扛不住。"));

            var side = Rect("Side", cortexView, new Vector2(.72f, 0), Vector2.one, new Vector2(6, 12), new Vector2(-12, -52));
            Panel(side, XgPalette.Page).raycastTarget = false;
            ui.Text(Strip("Title", side, 8, 22, 10, 10), Lang.T("各区接线"), 14, XgPalette.Muted, TextAlignmentOptions.MidlineLeft);
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                var row = ui.Button(side, "", () => { selectedRegion = index; Fx.Play(XgJuice.Sfx.Id.Click); RefreshCortex(); }, 13);
                row.rt.anchorMin = new Vector2(0, 1); row.rt.anchorMax = new Vector2(1, 1);
                row.rt.offsetMin = new Vector2(8, -36 - (i + 1) * 56 + 4); row.rt.offsetMax = new Vector2(-8, -36 - i * 56);
                row.label.alignment = TextAlignmentOptions.MidlineLeft;
                row.label.textWrappingMode = TextWrappingModes.Normal;
                row.label.enableAutoSizing = true; row.label.fontSizeMin = 9; row.label.fontSizeMax = 13;
                row.label.rectTransform.offsetMin = new Vector2(10, 2); row.label.rectTransform.offsetMax = new Vector2(-58, -2);
                regionStatus[i] = ui.Text(Rect("Status", row.rt, new Vector2(1, 0), Vector2.one, new Vector2(-60, 0), new Vector2(-8, 0)), "", 12, XgPalette.Good, TextAlignmentOptions.MidlineRight);
                regionRows[i] = row;
            }
            detail = ui.Text(Rect("Detail", side, Vector2.zero, Vector2.one, new Vector2(12, 96), new Vector2(-12, -36 - 4 * 56 - 10)), "", 13, XgPalette.Ink, TextAlignmentOptions.TopLeft);
            detail.enableAutoSizing = true; detail.fontSizeMin = 9; detail.fontSizeMax = 13;
            legend = ui.Text(Rect("Legend", side, Vector2.zero, new Vector2(1, 0), new Vector2(12, 8), new Vector2(-12, 90)), "", 11, XgPalette.Muted, TextAlignmentOptions.BottomLeft);
            legend.enableAutoSizing = true; legend.fontSizeMin = 8; legend.fontSizeMax = 11.5f;
            cortexView.gameObject.SetActive(false);
        }

        static Dictionary<string, string> CortexCaptions() => new Dictionary<string, string>
        {
            { "", Lang.T("尚未接线 · 还没在练这个区") },
            { "perceptron", Lang.T("没有中间层 · 只能划一条直线") },
            { "mlp", Lang.T("层层全连接 · 挪一格就不认得了") },
            { "lenet", Lang.T("同一个窗口滑过整张图") },
            { "alexnet", Lang.T("同样的窗口 · 叠得更深") },
            { "vgg", Lang.T("全是 3×3 小窗口 · 一层叠一层") },
            { "googlenet", Lang.T("大小窗口并排着扫") },
            { "resnet", Lang.T("… × 152 层") },
            { "resnet.skip", Lang.T("恒等旁路：信号原样穿过") },
            { "rnn", Lang.T("越早的字，回环越淡") },
            { "lstm", Lang.T("门：写入 · 遗忘 · 输出") },
            { "gru", Lang.T("两道门：更新 · 重置") },
            { "seq2seq.knot", Lang.T("定长瓶颈") },
            { "attention", Lang.T("线越粗 = 越相关") },
            { "textcnn", Lang.T("整句同时算 · 只看得见窗口那么宽") },
            { "transformer", Lang.T("三种颜色 = 三组注意力头") },
            { "caption", Lang.T("边看边说") },
            { "tone", Lang.T("对话里的赞 / 踩直接拉动它") },
        };

        // ---------------------------------------------------------------- refresh

        void RefreshCortex()
        {
            var board = Sim.Board;
            bool one = OneBrain();
            cortex.Reduced = Sim.S.reduceFx;
            cortex.UnifiedTitle = Lang.T("全皮层统一拓扑 · 同步点亮");
            cortex.SetUnified(one, cortexInitialised);
            cortex.UnifiedCells.Clear();
            header.text = Lang.T("大脑 · 皮层拓扑") + "  <size=13><color=#68748C>" + Lang.T("一颗皮层，四个区。LeNet、LSTM、Transformer 不是别的 AI，是给某个区换的连接拓扑。") + "</color></size>";
            if (selectedRegion < 0) selectedRegion = RegionIndex(Sim.RegionOfTrack(Sim.SelectedTrack));
            int perRegion = (XgCortexGraphic.UnifiedCellCount + 3) / 4;
            for (int i = 0; i < cortex.Regions.Length; i++)
            {
                var r = cortex.Regions[i];
                var concepts = new List<XgConcept>(board.Concepts(r.id));
                concepts.Sort((a, b) => (Math.Abs(b.w) * b.s).CompareTo(Math.Abs(a.w) * a.s));
                string arch = one ? "transformer" : Wiring(r.id, concepts.Count);
                cortex.SetArch(i, arch, cortexInitialised);
                r.training = Sim.RegionTraining(r.id) || one && Sim.S.pretrainRunning;
                r.empty = concepts.Count == 0;
                r.cells.Clear();
                for (int c = 0; c < Math.Min(CortexCells, concepts.Count); c++) r.cells.Add(LookOf(concepts[c]));
                for (int c = 0; c < perRegion && cortex.UnifiedCells.Count < XgCortexGraphic.UnifiedCellCount; c++)
                    cortex.UnifiedCells.Add(c < r.cells.Count ? r.cells[c] : new XgCellLook(XgCellKind.Dim, 0));
                if (r.id == XgSim.ToneRegion) for (int a = 0; a < 3; a++) r.axes[a] = (float)(Sim.ActualAxis(a) / 100);
                cortex.RegionNames[i] = XgSim.RegionName(r.id, Lang.English);
                r.chip = XgSim.RegionName(r.id, Lang.English) + " · " + XgSim.RegionLikeness(r.id, Lang.English);
                r.chipTopo = Lang.T("拓扑 ") + TopologyName(arch);
                r.chipChange = cortex.Rewriting(i) && r.oldArch.Length > 0 && !one ? TopoOf(r.oldArch) + " → " + TopoOf(arch) : "";
                RefreshRegionRow(i, r, arch, concepts.Count, one);
            }
            cortex.ToneLow[0] = Lang.T("冷静"); cortex.ToneLow[1] = Lang.T("正经"); cortex.ToneLow[2] = Lang.T("顺从");
            cortex.ToneHigh[0] = Lang.T("热情"); cortex.ToneHigh[1] = Lang.T("皮"); cortex.ToneHigh[2] = Lang.T("有主见");
            RefreshDetail(one);
            legend.text = "<color=#2F9E44>●</color> " + Lang.T("学成「是」") + "   <color=#D63031>●</color> " + Lang.T("学成「否」")
                + "\n<color=#8A63D2>●</color> " + Lang.T("叠加态：两种都可能") + "   <color=#FFBE28>●</color> " + Lang.T("种子：还没学")
                + "\n" + Lang.T("颜色越深越确定。连线是谁能连到谁，金点是正在训练的信号。");
            cortexInitialised = true;
            cortex.Animate();
        }

        static int RegionIndex(string region)
        {
            switch (region) { case "logic": return 0; case "tone": return 1; case "sequence": return 2; default: return 3; }
        }

        /// <summary>How a region is wired: its architecture (kept when no run trains it), the tone region's own wiring, or "".</summary>
        string Wiring(string region, int concepts)
        {
            if (region == XgSim.ToneRegion) return concepts > 0 ? "tone" : "";
            var a = Sim.RegionWiringOrLast(region);
            return a != null ? a.id : "";
        }

        static XgCellLook LookOf(XgConcept c)
        {
            float strength = Mathf.Clamp01((float)(Math.Abs(c.w) / 2));
            if (c.seed) return new XgCellLook(XgCellKind.Seed, strength);
            if (c.alt.Length > 0) return new XgCellLook(XgCellKind.Superposed, strength);
            return new XgCellLook(c.w >= 0 ? XgCellKind.Yes : XgCellKind.No, strength);
        }

        static string TopoOf(string arch)
        {
            if (arch == "tone") return Lang.T("赞 / 踩直接拉动");
            var a = XgCatalog.Arch(arch);
            return a != null ? Lang.T(a.topo) : Lang.T("未接线");
        }

        static string TopologyName(string arch)
        {
            if (arch.Length == 0) return Lang.T("—— 未接线");
            if (arch == "tone") return Lang.T("—— 对话里的赞 / 踩直接拉动");
            var a = XgCatalog.Arch(arch);
            return a == null ? arch : T(a.name, a.nameEn) + "【" + Lang.T(a.topo) + "】";
        }

        void RefreshRegionRow(int i, XgCortexGraphic.Region r, string arch, int concepts, bool one)
        {
            var row = regionRows[i];
            bool on = selectedRegion == i;
            string name = "<b>" + XgSim.RegionName(r.id, Lang.English) + "</b>  <size=11><color=#68748C>" + XgSim.RegionLikeness(r.id, Lang.English) + "</color></size>";
            string wire = arch.Length == 0 ? "<color=#9AA3B5>" + Lang.T("未接线") + "</color>" : "<color=#3B5BDB>" + TopologyName(arch).TrimStart('—', ' ') + "</color>";
            row.Set(name + "\n<size=11>" + wire + "</size>", true, on ? XgPalette.AccentSoft : Color.white, XgPalette.Ink);
            XgRun trained = null;
            foreach (var run in Sim.Runs) if (XgSim.RegionOf(run.dataset) == r.id) trained = run;
            string status;
            if (r.training) status = "<color=#E86E14>" + Lang.T("训练中") + "</color>";
            else if (one) status = "<color=#2F9E44>" + Lang.T("同步") + "</color>";
            else if (trained != null && trained.epoch > 0) status = "<color=#2F9E44>" + XgSim.Pct(trained.valAcc) + "</color>";
            else if (arch.Length > 0) status = "<color=#9AA3B5>" + Lang.T("没在练") + "</color>";
            else status = "<color=#9AA3B5>—</color>";
            if (cortex.Rewriting(i) && r.oldArch.Length > 0 && !one) status = "<color=#D63031>" + Lang.T("拓扑重写") + "</color>";
            regionStatus[i].text = status;
        }

        void RefreshDetail(bool one)
        {
            int i = Mathf.Clamp(selectedRegion, 0, 3);
            var r = cortex.Regions[i];
            string head = "<b>" + XgSim.RegionName(r.id, Lang.English) + "</b>  <color=#68748C>" + XgSim.RegionLikeness(r.id, Lang.English) + "</color>\n";
            string body;
            if (r.arch.Length == 0)
                body = r.id == XgSim.ToneRegion ? Lang.T("还没和它聊过天。对话里的每一次赞和踩，会直接写进这个区。")
                     : Lang.T("这个区还没接线。去训练页选一个练这个区的数据集，再给它挑一种结构。");
            else if (r.arch == "tone")
                body = Lang.T("语气区不靠结构：对话里每一次赞和踩，直接把「冷静—热情」「正经—皮」「顺从—有主见」三根轴往一边拉。");
            else
            {
                var a = XgCatalog.Arch(r.arch);
                body = "<color=#3B5BDB>" + TopologyName(r.arch) + "</color>\n" + T(a.wire, a.wireEn) + "\n<size=11><color=#68748C>" + T(a.note, a.noteEn) + "</color></size>";
                if (one) body += "\n\n<color=#9A6A00>" + Lang.T("四个区的边界不再是墙：同一种接法，整颗脑子同一拍亮起来。") + "</color>";
            }
            string remembered = r.empty ? Lang.T("这个区现在是空的。") : Lang.T("这个区记着的概念，最牢的几个画在图上。");
            detail.text = head + body + "\n\n<size=11><color=#68748C>" + remembered + "</color></size>";
        }

        // ---------------------------------------------------------------- tick

        void TickCortex(float dt)
        {
            if (cortex == null || Sim == null) return;
            animTimer -= dt;
            if (animTimer <= 0)
            {
                animTimer = Sim.S.reduceFx ? .1f : 0;
                cortex.Animate();
            }
            cortex.SyncLabels();
            TickGpu(dt);
        }

        void TickGpu(float dt)
        {
            bool pre = Sim.S.pretrainRunning;
            string region = null; XgRun busy = null;
            foreach (var run in Sim.Runs) if (run.epochActive) { busy = run; region = XgSim.RegionName(XgSim.RegionOf(run.dataset), Lang.English); }
            float t = Time.unscaledTime;
            float target = .06f;
            if (pre) target = 1;
            else if (busy != null)
            {
                double vram = Math.Max(1, Sim.Vram(Host));
                target = Mathf.Clamp(.55f + .35f * (float)(XgSim.VramNeedMB(busy) / vram), .5f, .97f) + (Sim.S.reduceFx ? 0 : .04f * Mathf.Sin(t * 7) + .02f * Mathf.Sin(t * 13));
            }
            gpuLoad = Mathf.Lerp(gpuLoad, Mathf.Clamp01(target), Mathf.Clamp01(dt * 4));
            SetBar(gpuFill, gpuLoad);
            Color col = pre ? XgPalette.Bad : busy != null ? XgPalette.Accent : XgPalette.Muted;
            if (pre && !Sim.S.reduceFx) col = Color.Lerp(XgPalette.Bad, new Color32(255, 120, 120, 255), .5f + .5f * Mathf.Sin(t * 6));
            gpuFillImage.color = col;
            string label = pre ? Lang.T("GPU 负载 · 预训练中 · 机房租金 ¥") + XgSim.DatacenterRent + Lang.T("/秒")
                         : busy != null ? Lang.T("GPU 负载 · 训练中 · ") + region
                         : Lang.T("GPU 负载 · 空闲");
            if (gpuLabel.text != label) gpuLabel.text = label;
            gpuValue.text = N(gpuLoad * 100, "0") + "%";
            gpuValue.color = pre ? XgPalette.Bad : busy != null ? XgPalette.Accent : XgPalette.Muted;
        }

        // ---------------------------------------------------------------- the first look

        /// <summary>The first time the protagonist sees the map: the brain is not a diagram, it is lit.</summary>
        void VoiceCortex()
        {
            if (Sim == null || Sim.S.cortexSeen) return;
            Sim.S.cortexSeen = true;
            Say("……这是什么？", 1.6f);
            Say(OneBrain() ? "整颗脑子连成了一片。所有的线，同一拍亮起来。"
                           : "一张脑部扫描。看图的在后脑，读字的在耳朵上面，想事的在额头后面——它们真的在亮。", 3.4f);
            Say("我在训练页随手点的那些结构……是在给一颗脑子重新接线？", 3f);
            Say("它不是在跑一个程序。它在长一颗皮层。", 2.8f);
            Say("……谁写的软件，会给自己留一整颗脑子的位置。", 3.2f);
        }

        static void Say(string zh, float seconds) => InnerVoice.Say(zh, Lang.En(zh), seconds);
    }
}
