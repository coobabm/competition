using System;
using System.Collections.Generic;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using static LingGuangV05.Desktop.XingGuang.XgUi;

using LingGuangV05.Core;
namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 训练: one big 训练一轮 button (0.6 s per epoch), the learning curve, the assessment card every record earns
    /// (each epoch ends with an exam) with its grade stamp, and the model as read-only information. There are no knobs
    /// (训练不再需要调参数): the lab configures the model from what is owned (XgSim.AutoModel.cs); the page shows it, the
    /// risk that a round goes down, and plainly why training has stopped improving.
    /// </summary>
    public sealed partial class XgTrainPage : XgPage
    {
        public const float AutomaticCardSeconds = 3;
        /// <summary>Every epoch is assessed; only a new grade by hand earns the big card, other records a corner card.</summary>
        public static bool UseAssessmentOverlay(XgAssessment a) => a.hand && a.newGrade >= 1;
        XgBtn[] trackTabs = new XgBtn[2];
        XgBtn summary, train, assess, autoTrain, saveModel, cleanNoise;
        readonly List<XgBtn> dataBtns = new List<XgBtn>();
        readonly List<XgBtn> packBtns = new List<XgBtn>();
        readonly List<float> noValidationCurve = new List<float>();
        readonly List<string> dataIds = new List<string>();
        RectTransform left, right, chartArea, epochBar, epochFill, dataBox, dataViewport, gpuTarget, diagnosticArea;
        RectTransform[] pips;
        XgChartGraphic chart;
        XgStageGraphic diagnostic;
        readonly List<XgStageGraphic> previousDiagnostics = new List<XgStageGraphic>();
        TMP_Text diagnosticTitle, diagnosticNote, gpuLabel, noiseText, noiseHint;
        XgGlow gpuGlow;
        TMP_Text numbers, yTop, yMid, yLow, legend, risk, hint, mode, trainSub, modelTitle, modelInfo, shapeText, techText, statsText, dataTitle;
        XgGlow trainGlow;
        XgHold hold;
        float glitch;
        Vector2 chartRestPosition;
        string shownDataKey = "", shownStructure = "";

        // assessment card
        RectTransform card, stamp, assessmentLayer;
        Image assessmentShade;
        CanvasGroup cardGroup;
        Image cardBg;
        TMP_Text cardTitle, cardScore, cardBest, cardReward, stampText, ribbon;
        XgRingGraphic stampRing;
        XgAssessment shown;
        XgSim assessmentOwner;
        float cardT = -1, cardLife, rollFrom, rollTo, nextTick;
        bool stamped, cardBig;

        XgTrack Track => Sim.SelectedTrack;

        public override void Build(RectTransform area)
        {
            root = Rect("train", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            left = ui.Card(root, "Curve", Vector2.zero, new Vector2(.62f, 1), Vector2.zero, new Vector2(-6, 0));
            for (int i = 0; i < 2; i++)
            {
                var t = (XgTrack)i;
                trackTabs[i] = ui.Button(left, "", () => { Sim.SelectedTrack = t; Fx.Play(XgJuice.Sfx.Id.Click); Refresh(); }, 15);
                PlaceTopLeft(trackTabs[i], 14 + i * 112, 10, 106, 32);
            }
            summary = ui.Button(left, "", () => view.ShowTab("items"), 14);
            summary.rt.anchorMin = new Vector2(0, 1); summary.rt.anchorMax = new Vector2(1, 1);
            summary.rt.offsetMin = new Vector2(244, -42); summary.rt.offsetMax = new Vector2(-14, -10);
            summary.label.alignment = TextAlignmentOptions.MidlineLeft;
            numbers = ui.Text(Strip("Numbers", left, 48, 26, 16, 16), "", 16, XgDark.Ink, TextAlignmentOptions.MidlineLeft);

            chartArea = Rect("ChartArea", left, Vector2.zero, Vector2.one, new Vector2(56, 214), new Vector2(-16, -80));
            Panel(chartArea, new Color32(6, 11, 16, 255));
            chartRestPosition = chartArea.anchoredPosition;
            chart = Rect("Chart", chartArea, Vector2.zero, new Vector2(1, .52f), new Vector2(4, 4), new Vector2(-4, -4)).gameObject.AddComponent<XgChartGraphic>();
            chart.raycastTarget = false;
            chart.Capacity = XgSim.HistoryLength;
            BuildDiagnostics();
            yTop = ui.Text(Rect("YTop", chart.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(-54, -11), new Vector2(-2, 11)), "", 12, XgDark.Muted, TextAlignmentOptions.MidlineRight);
            yMid = ui.Text(Rect("YMid", chart.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(-54, -11), new Vector2(-2, 11)), "", 12, XgDark.Muted, TextAlignmentOptions.MidlineRight);
            yLow = ui.Text(Rect("YLow", chart.rectTransform, Vector2.zero, Vector2.zero, new Vector2(-54, -11), new Vector2(-2, 11)), "", 12, XgDark.Muted, TextAlignmentOptions.MidlineRight);
            legend = ui.Text(Rect("Legend", left, Vector2.zero, new Vector2(1, 0), new Vector2(56, 188), new Vector2(-16, 210)), "", 13, XgDark.Muted, TextAlignmentOptions.MidlineLeft);

            epochBar = Rect("EpochBar", left, Vector2.zero, new Vector2(1, 0), new Vector2(16, 170), new Vector2(-150, 182));
            epochFill = Bar(epochBar, "Fill", XgDark.Track, XgDark.Accent);
            for (int i = 1; i < 16; i++)
            {
                var gap = Rect("EpochSegment" + i, epochBar, new Vector2(i / 16f, 0), new Vector2(i / 16f, 1), new Vector2(-1, 0), new Vector2(1, 0));
                Panel(gap, XgDark.Card).raycastTarget = false;
            }
            pips = new RectTransform[4];
            for (int i = 0; i < 4; i++)
            {
                pips[i] = Rect("Pip" + i, left, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-138 + i * 30, 168), new Vector2(-114 + i * 30, 184));
                Panel(pips[i], XgDark.Disabled).raycastTarget = false;
            }

            var trainRect = Rect("TrainAt", left, Vector2.zero, Vector2.zero, new Vector2(16, 76), new Vector2(316, 156));
            trainGlow = Glow(trainRect, XgDark.Accent, 6);
            train = ui.Button(left, "", null, 28);
            Place(train, trainRect);
            train.label.fontStyle = FontStyles.Bold;
            train.label.rectTransform.offsetMin = new Vector2(6, 18);
            trainSub = ui.Text(Rect("Sub", train.rt, Vector2.zero, new Vector2(1, 0), new Vector2(6, 6), new Vector2(-6, 28)), "", 13, Color.white, TextAlignmentOptions.Center);
            mode = ui.Text(Rect("Mode", train.rt, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-110, -22), new Vector2(-6, -4)), "", 12, new Color(1, 1, 1, .8f), TextAlignmentOptions.TopRight);
            hold = train.rt.gameObject.AddComponent<XgHold>();
            hold.Down = () => Press();
            train.rt.gameObject.AddComponent<XgTrainingSubmit>().Submit = Press;
            assess = ui.Button(left, "", () => DoAssess(), 18);
            PlaceBottomLeft(assess, 326, 76, 150, 80);
            autoTrain = ui.Button(left, "", () => { Sim.SetAutoTrain(Track, !Sim.Run(Track).running); Fx.Play(XgJuice.Sfx.Id.Click); Refresh(); }, 15);
            PlaceBottomLeft(autoTrain, 486, 76, 140, 80);
            risk = ui.Text(Rect("Risk", left, Vector2.zero, new Vector2(1, 0), new Vector2(16, 36), new Vector2(-16, 70)), "", 14, XgDark.Bad, TextAlignmentOptions.MidlineLeft);
            hint = ui.Text(Rect("Hint", left, Vector2.zero, new Vector2(1, 0), new Vector2(16, 6), new Vector2(-16, 36)), "", 14, XgDark.Muted, TextAlignmentOptions.MidlineLeft);

            BuildModel();
            BuildCard();
            for (int i = 0; i < 2; i++) UiTip.Add(trackTabs[i].rt, "两条线练的是灵光同一颗脑子的两个区：看图区（像视觉皮层）和读字区（像语言区）。各练各的，共用显卡和经费。", "The two tracks train two regions of 灵光's one brain: seeing (like the visual cortex) and reading (like the language areas). They train separately and share the GPU and funds.");
            UiTip.Add(train.rt, "训练一轮：喂灵光一批卡，练完在没见过的题上考一次。\n这里的「一轮」是一批，不是把整个数据集过一遍：数据越多，要越多轮才过完一遍（下面写着已过几遍）。\n手动按会叠连击（学得更多）；刷新纪录就记成绩、存检查点、发奖金。", "Train one epoch: feed the model a batch of cards, then an exam on unseen cards.\nAn epoch here is a batch, not a pass over the whole dataset: the more data, the more epochs one pass takes (see the passes below).\nPressing by hand builds combo (it learns more); a new record is scored, saved and paid.");
            UiTip.Add(autoTrain.rt, "自动训练：每隔几秒自己训练一轮（效果是手按的一半，不算连击）。", "Auto-train: runs an epoch every few seconds (half as effective as by hand, no combo).");
            UiTip.Add(summary.rt, () => ArchTip(Sim.Run(Track).arch) + "\n\n" + T("这个区现在自动用的结构。结构是道具：买到更好的，它自己换上。点一下去道具页。", "The structure this region uses now, chosen automatically. Structures are items: buy a better one and it switches by itself. Click for the Items page."));
        }

        void BuildCard()
        {
            // Covers the whole application content, not just the chart. Mini-cards do not intercept empty space.
            assessmentLayer = Rect("AssessmentLayer", (RectTransform)root.parent.parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            assessmentShade = Panel(assessmentLayer, Color.clear);
            assessmentLayer.gameObject.AddComponent<Button>().onClick.AddListener(DismissAssessment);
            assessmentLayer.gameObject.AddComponent<XgAssessmentClock>().Page = this;
            card = Rect("Assessment", assessmentLayer, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-200, -96), new Vector2(200, 96));
            cardBg = Panel(card, new Color(.1f, .12f, .24f, .94f));
            cardGroup = card.gameObject.AddComponent<CanvasGroup>();
            cardBg.raycastTarget = true;
            card.gameObject.AddComponent<Button>().onClick.AddListener(DismissAssessment);
            cardTitle = ui.Text(Rect("Title", card, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -34), new Vector2(-16, -8)), "", 15, new Color(1, 1, 1, .75f), TextAlignmentOptions.MidlineLeft);
            cardScore = ui.Text(Rect("Score", card, new Vector2(0, 0), new Vector2(.62f, 1), new Vector2(16, 50), new Vector2(0, -36)), "", 64, Color.white, TextAlignmentOptions.MidlineLeft);
            cardScore.fontStyle = FontStyles.Bold;
            cardBest = ui.Text(Rect("Best", card, Vector2.zero, new Vector2(.62f, 0), new Vector2(16, 28), new Vector2(0, 52)), "", 14, new Color(1, 1, 1, .7f), TextAlignmentOptions.MidlineLeft);
            cardReward = ui.Text(Rect("Reward", card, Vector2.zero, new Vector2(1, 0), new Vector2(16, 6), new Vector2(-16, 30)), "", 16, XgDark.Gold, TextAlignmentOptions.MidlineLeft);
            stamp = Rect("Stamp", card, new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-150, -62), new Vector2(-26, 62));
            stampRing = stamp.gameObject.AddComponent<XgRingGraphic>(); stampRing.Width = 8; stampRing.raycastTarget = false;
            stampText = ui.Text(Rect("Letter", stamp, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 72, Color.white, TextAlignmentOptions.Center);
            stampText.fontStyle = FontStyles.Bold;
            ribbon = ui.Text(Rect("Ribbon", card, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-180, -32), new Vector2(-12, -8)), "", 16, XgDark.Gold, TextAlignmentOptions.MidlineRight);
            ribbon.fontStyle = FontStyles.Bold;
            assessmentLayer.gameObject.SetActive(false);
        }

        void DismissAssessment() { cardT = -1; assessmentLayer.gameObject.SetActive(false); }

        void BuildDiagnostics()
        {
            diagnosticArea = Rect("StageDiagnostics", chartArea, new Vector2(0, .55f), Vector2.one, new Vector2(5, 0), new Vector2(-5, -3));
            diagnosticTitle = ui.Text(Strip("Title", diagnosticArea, 0, 22, 2, 2), "", 13, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            diagnostic = Rect("Current", diagnosticArea, Vector2.zero, Vector2.one, new Vector2(3, 20), new Vector2(-95, -25)).gameObject.AddComponent<XgStageGraphic>();
            diagnostic.raycastTarget = false;
            for (int i = 0; i < 5; i++)
            {
                var r = Rect("PreviousStage" + (i + 1), diagnosticArea, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-88, 22 + i * 16), new Vector2(-3, -25));
                var g = r.gameObject.AddComponent<XgStageGraphic>(); g.raycastTarget = false; previousDiagnostics.Add(g);
            }
            diagnosticNote = ui.Text(Rect("Caveat", diagnosticArea, Vector2.zero, new Vector2(1, 0), new Vector2(2, 0), new Vector2(-2, 19)), "", 11, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
        }

        void BuildModel()
        {
            right = ui.Card(root, "Model", new Vector2(.62f, 0), Vector2.one, new Vector2(6, 0), Vector2.zero);
            modelTitle = ui.Text(Strip("ModelTitle", right, 8, 28, 14, 128), "", 17, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            saveModel = ui.Button(right, "", () =>
            {
                if (Sim.SaveModel(Track) == null) { Fx.Play(XgJuice.Sfx.Id.Thud); return; }
                Fx.Knock(saveModel.rt, .15f);
                Fx.Burst(Fx.At(saveModel.rt), 10, XgDark.Accent, XgJuice.Shape.Spark, 180);
                view.Refresh(true);
            }, 14);
            PlaceTopRight(saveModel, 124, 8, 110, 28);
            modelTitle.fontStyle = FontStyles.Bold;
            // The model, read only (训练不再需要调参数): what the lab configured from what is owned and what fits.
            modelInfo = ui.Text(Strip("ModelInfo", right, 40, 26, 14, 14), "", 16, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            modelInfo.fontStyle = FontStyles.Bold;
            modelInfo.enableAutoSizing = true; modelInfo.fontSizeMin = 11; modelInfo.fontSizeMax = 16;
            shapeText = ui.Text(Strip("ModelShape", right, 68, 46, 14, 14), "", 13, XgDark.Muted, TextAlignmentOptions.TopLeft);
            shapeText.enableAutoSizing = true; shapeText.fontSizeMin = 10; shapeText.fontSizeMax = 13;
            techText = ui.Text(Strip("ModelTechniques", right, 116, 46, 14, 14), "", 13, XgDark.Muted, TextAlignmentOptions.TopLeft);
            techText.enableAutoSizing = true; techText.fontSizeMin = 10; techText.fontSizeMax = 13;
            NodeLink(modelInfo, () => ShapeNode(XgNodeKind.Width));
            NodeLink(shapeText, () => ShapeNode(XgNodeKind.Depth));
            techText.raycastTarget = true;
            var items = techText.gameObject.AddComponent<Button>();
            items.targetGraphic = techText; items.transition = Selectable.Transition.None;
            items.onClick.AddListener(() => view.ShowTab("items"));
            UiTip.Add(saveModel.rt, "把当前模型存进模型仓库。", "Save the current model to the model library.");
            UiTip.Add(modelInfo, () => T("模型是自动配置的：结构用已经买到的最好的那个，宽度和层数用科技买到的上限里显卡装得下的最大的。\n参数量 = 层数 × 宽度² × 结构系数。括号里拿真实的著名模型比一比大小，只是参考。", "The model configures itself: the best structure you own, and the biggest width and depth the tech tree allows that fit the card.\nParameters = layers × width² × structure factor. The famous model in brackets is only for a sense of scale."));
            UiTip.Add(shapeText, () => T("想要更大的模型：科技里买「宽」「层」，或者在淘货加显卡。买到以后，下一轮自动换上。", "For a bigger model, buy width or layers in the tech tree, or add a card from the shop. It switches over by the next round."));
            UiTip.Add(techText, () => T("买到的技巧全部自动打开。Dropout、数据增强、BatchNorm、预热、梯度裁剪都会让「退步」少一些。", "Every technique you own is on. Dropout, augmentation, BatchNorm, warm-up and clipping all make drops rarer."));
            statsText = ui.Text(Strip("Stats", right, 166, 238, 14, 14), "", 13, XgDark.Muted, TextAlignmentOptions.TopLeft);
            statsText.enableAutoSizing = true; statsText.fontSizeMin = 10; statsText.fontSizeMax = 13;
            UiTip.Add(statsText, () => T("每一轮都往这个模型的上限走一步。上限由参数量和样本量决定：参数和样本够，就一直涨；不够，就停在那里。\n每一轮也有一点机会退步：数据太少（背答案）、数据太脏、刚涨了一大截时更容易。学会的能力不会丢。", "Every round takes the model a step towards its ceiling, which parameters and samples set: with enough of both it keeps rising; without, it stops there.\nEach round may also go down a little, more often when the data is scarce (it memorises), dirty, or right after a big jump. Learned abilities are never lost."));
            noiseText = ui.Text(Strip("DatasetNoise", right, 410, 20, 14, 14), "", 13, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            noiseText.enableAutoSizing = true; noiseText.fontSizeMin = 11; noiseText.fontSizeMax = 13;
            noiseText.textWrappingMode = TextWrappingModes.NoWrap;
            noiseHint = ui.Text(Strip("NoiseCleaningHint", right, 432, 30, 14, 180), "", 11, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            noiseHint.enableAutoSizing = true; noiseHint.fontSizeMin = 9; noiseHint.fontSizeMax = 11;
            noiseHint.textWrappingMode = TextWrappingModes.NoWrap;
            cleanNoise = ui.Button(right, "", CleanCurrentNoise, 12);
            cleanNoise.rt.gameObject.name = "CleanDatasetNoise";
            PlaceTopRight(cleanNoise, 168, 430, 154, 34);
            BuildDataEconomy(right);
            dataTitle = ui.Text(Strip("DataTitle", right, 468, 24, 14, 14), "", 16, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            dataTitle.fontStyle = FontStyles.Bold;
            NodeLink(dataTitle, () => Sim.Run(Track).dataset + ".pack");
            var scroll = Rect("Datasets", right, Vector2.zero, Vector2.one, new Vector2(14, 86), new Vector2(-14, -496));
            var scroller = scroll.gameObject.AddComponent<ScrollRect>();
            dataViewport = Rect("Viewport", scroll, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Panel(dataViewport, new Color(1, 1, 1, .01f)); dataViewport.gameObject.AddComponent<RectMask2D>();
            dataBox = Rect("Content", dataViewport, new Vector2(0, 1), Vector2.one, Vector2.zero, Vector2.zero);
            dataBox.pivot = new Vector2(.5f, 1); dataBox.sizeDelta = Vector2.zero;
            scroller.viewport = dataViewport; scroller.content = dataBox; scroller.horizontal = false; scroller.vertical = true; scroller.scrollSensitivity = 24;
            scroller.movementType = ScrollRect.MovementType.Clamped;
            gpuTarget = Rect("GpuInstallTarget", right, Vector2.zero, new Vector2(1, 0), new Vector2(14, 8), new Vector2(-14, 77));
            Panel(gpuTarget, XgDark.AccentSoft);
            gpuGlow = Glow(gpuTarget, XgDark.Gold, 4);
            gpuLabel = ui.Text(Rect("GpuLabel", gpuTarget, Vector2.zero, Vector2.one, new Vector2(12, 4), new Vector2(-12, -4)), "", 14, XgDark.Accent, TextAlignmentOptions.MidlineLeft);
            gpuTarget.gameObject.AddComponent<XgGpuInstallDrop>().Page = this;
        }

        /// <summary>The main line from the training page: the next ability's two bars, and whether this model would count.</summary>
        string AbilityLine(XgRun run)
        {
            int next = Sim.NextAbility;
            if (next == 0) return T("六项能力都有了。", "All six abilities are there.");
            double need = Sim.ParamsThreshold(next), size = XgSim.ParamsK(run);
            string line = "<color=#C88A00>" + T("下一项能力「", "Next ability: ") + XgSim.AbilityName(next, Sim.English) + T("」", "") + "</color>  "
                + T("参数 ", "parameters ") + XgSim.ParamsText(Sim.TrainedParamsK) + "/" + XgSim.ParamsText(need)
                + T(" · 样本 ", " · samples ") + XgSim.SamplesText(Sim.TrainedSamples) + "/" + XgSim.SamplesText(Sim.SamplesThreshold(next));
            if (Sim.TrainedParamsK + 1e-9 < need)
                line += size + 1e-9 >= need ? T("\n这个模型够大：练到 C 级就算数。", "\nThis model is big enough: it counts once assessed at grade C.")
                    : T("\n这个模型 " + XgSim.ParamsText(size) + "，还不够大：去科技加宽、加深，或在道具买参数更多的结构。", "\nThis model has " + XgSim.ParamsText(size) + ", not enough yet: widen or deepen in the tech tree, or buy a bigger structure on the Items page.");
            return line;
        }

        /// <summary>An architecture is a way of wiring one region of 灵光's brain; its history comes second.</summary>
        string ArchTip(string id)
        {
            var n = XgCatalog.Node(id); var a = XgCatalog.Arch(id);
            if (a == null) return n != null ? T(n.note, n.noteEn) : "";
            string region = a.shared ? Sim.RegionOfTrack(Track) : a.track == XgTrack.Vision ? "vision" : "sequence";
            return "<b>" + T(a.name, a.nameEn) + "</b>  " + a.year + "\n"
                + T("灵光" + XgSim.RegionName(region, false) + "的连接拓扑：", "Topology of 灵光's " + XgSim.RegionName(region, true).ToLowerInvariant() + " region: ") + (a.topo.Length > 0 ? "<b>【" + a.topo + "】</b>" : "") + T(a.wire, a.wireEn)
                + (string.IsNullOrEmpty(a.note) ? "" : "\n<color=#6F95A5>" + Lang.T("历史上：") + T(a.note, a.noteEn) + "</color>");
        }

        /// <summary>The first change of structure: the protagonist works out what a "network" is to 灵光 (once per save).</summary>
        void VoiceWiring()
        {
            if (Sim.S.wiringVoiced) return;
            Sim.S.wiringVoiced = true;
            LingGuangV05.Desktop.Story.InnerVoice.Say("等等……论文里这些网络，不是别的 AI。", "Wait… these networks from the papers aren't other AIs.", 2.4f);
            LingGuangV05.Desktop.Story.InnerVoice.Say("是给它的脑子重写连接拓扑。像人的大脑皮层，看图的、读字的、想事的，各管一块。", "They're ways of wiring its brain. Like a human cortex: seeing, reading, thinking, each has its own patch.", 3.2f);
            LingGuangV05.Desktop.Story.InnerVoice.Say("……难怪机箱风扇一下子狂转。整颗脑子一亮，整张显卡都是它的。", "…No wonder the case fans just roared. When the whole brain lights up, the whole card is its.", 3f);
        }

        void Press()
        {
            if (Sim.Run(Track).epochActive) return;
            if (!Sim.BeginEpoch(Track, Host, true)) { Fx.Knock(train.rt, .04f, new Vector2(10, 0)); Fx.Play(XgJuice.Sfx.Id.Thud); view.Refresh(true); }
        }

        void DoAssess()
        {
            var a = Sim.Assess(Track, Host, true);
            if (a == null) { Fx.Play(XgJuice.Sfx.Id.Thud); return; }
            Fx.Knock(assess.rt, .1f);
        }

        void CleanCurrentNoise()
        {
            // The core rechecks access, power, project state, wallet and remaining noise atomically.
            // No local sample/score/reward mutation: this is simulated dataset cleanup, not GGUF training.
            int removed = Sim.CleanNoise(Sim.Run(Track).dataset, Host, XgSim.NoiseCleanBatchLimit);
            if (removed <= 0) { Fx.Play(XgJuice.Sfx.Id.Thud, 1, .4f); view.Refresh(true); return; }
            Fx.Knock(cleanNoise.rt, .08f);
            Fx.Float(Fx.At(cleanNoise.rt, new Vector2(-40, 30)), Lang.T("清洗噪声 ") + removed + " · −¥" + Money(removed * XgSim.NoiseCleanMoneyPerItem), XgDark.Good, 18, 35, .8f);
            Fx.Play(XgJuice.Sfx.Id.Tick, 1.1f, .5f);
            view.Refresh(true);
        }

        /// <summary>Called on authoritative epoch start, not after already paying/evaluating.</summary>
        public void OnEpochStarted(XgEpoch e)
        {
            if (!view.Visible || view.Tab != "train") return;
            if ((XgTrack)e.track != Track) return;
            if (e.hand)
            {
                Fx.Knock(train.rt, .1f, new Vector2(0, -4));
                Fx.Burst(Fx.At(gpuTarget), 12, new Color32(255, 200, 90, 255), XgJuice.Shape.Spark, 300, 500, .45f);
                Fx.Play(XgJuice.Sfx.Id.Fan, 1 + Mathf.Min(.8f, Sim.S.combo * .02f), .8f);
            }
            else
            {
                Fx.Knock(train.rt, .025f);
                Fx.Play(XgJuice.Sfx.Id.Tick, .8f, .25f);
            }
            Refresh();
        }

        public void OnEpoch(XgEpoch e)
        {
            if (!view.Visible || view.Tab != "train" || (XgTrack)e.track != Track) return;
            Fx.Shockwave(Fx.At(epochBar, new Vector2(epochBar.rect.width * .5f, 0)), XgDark.Accent, e.hand ? 60 : 28, .3f, e.hand ? 5 : 2);
            var d = XgCatalog.Dataset(Sim.Run(Track).dataset);
            double points = d == null ? 0 : e.gain * 1000 / Math.Max(1e-9, d.chanceError - d.floorError);
            if (e.dropped)
            {
                // A drop: the round went backwards a little (训练不再需要调参数). Nothing learned is lost.
                Fx.Float(Fx.At(epochBar, new Vector2(0, 20)), T("退步 " + N(points, "0") + " 分 · " + e.dropReason, "Down " + N(-points, "0") + " points · " + e.dropReasonEn), XgDark.Bad, 16, 30, .8f, 1.4f);
                Fx.Knock(chartArea, .06f, new Vector2(6, 0));
                Fx.Play(XgJuice.Sfx.Id.Thud, .8f, e.hand ? .6f : .25f);
            }
            else if (e.hand) Fx.Float(Fx.At(epochBar, new Vector2(0, 20)), T("第 " + e.epoch + " 轮 · ", "Epoch " + e.epoch + " · ") + (points >= .5 ? "+" + N(points, "0") + T(" 分", " points") : T("涨不动了", "no gain")), XgDark.Accent, 16, 30, .6f, 1.1f);
            chart.PulseLastSegment(e.hand ? 1 : .35f);
            Refresh();
        }

        public void Glitch() { glitch = Sim.LastEpochWasHand ? .3f : .12f; }

        public override void Tick(float dt)
        {
            var run = Sim.Run(Track);
            SetBar(epochFill, run.epochActive ? (float)run.epochProgress : 0);
            if (glitch > 0)
            {
                glitch -= dt;
                numbers.text = Garble();
                numbers.color = XgDark.Bad;
                chartArea.anchoredPosition = chartRestPosition + (Fx.Reduced || !Sim.LastEpochWasHand ? Vector2.zero : new Vector2(UnityEngine.Random.Range(-6f, 6f), 0));
                if (glitch <= 0) { numbers.color = XgDark.Ink; chartArea.anchoredPosition = chartRestPosition; Refresh(); }
            }
            trainGlow.on = Sim.TrainingUnlocked(Track) && !run.epochActive && Sim.S.combo >= 10;
            trainGlow.color = XgDark.Tiers[Mathf.Min(4, XgSim.TierOf(Sim.S.combo) + 1)] * new Color(1, 1, 1, .7f);
        }

        public bool AssessmentBlocksInput => cardT >= 0 && cardBig && assessmentLayer.gameObject.activeSelf;

        static string Garble()
        {
            const string chars = "NaN∞#%@?!01";
            var s = new char[40];
            for (int i = 0; i < s.Length; i++) s[i] = chars[UnityEngine.Random.Range(0, chars.Length)];
            return "loss = " + new string(s);
        }

        // ───────────── assessment card ─────────────

        public void OnAssessed(XgAssessment a)
        {
            if (!view.Visible) return;
            if (view.Tab != "train" || (XgTrack)a.track != Track)
            {
                if (a.record && a.hand == false && a.newGrade >= 0) view.ShowToast(Lang.T("自动评估：") + XgCatalog.Dataset(a.dataset).name + " " + XgCatalog.GradeNames[a.grade] + " " + N(a.score, "0"), 2.5f);
                return;
            }
            if (AssessmentBlocksInput && shown != null && shown.hand && !a.hand) return;
            // Every epoch is assessed; only a record is worth a card.
            if (!a.record) return;
            shown = a;
            assessmentOwner = Sim;
            cardBig = UseAssessmentOverlay(a);
            cardT = 0; stamped = false;
            cardLife = cardBig ? float.PositiveInfinity : AutomaticCardSeconds;
            rollFrom = 0;
            rollTo = (float)a.score;
            nextTick = 0;
            assessmentLayer.gameObject.SetActive(true);
            assessmentLayer.SetAsLastSibling();
            assessmentShade.raycastTarget = cardBig;
            assessmentShade.color = Color.clear;
            cardGroup.alpha = 1;
            card.localScale = Vector3.one * (cardBig ? 1 : .7f);
            card.anchorMin = card.anchorMax = cardBig ? new Vector2(.5f, .5f) : new Vector2(1, 0);
            card.anchoredPosition = cardBig ? Vector2.zero : new Vector2(150, 86);
            var d = XgCatalog.Dataset(a.dataset);
            cardTitle.text = Lang.T("评估 · ") + T(d.name, d.nameEn) + (a.hand ? "" : Lang.T("（自动）"));
            cardBest.text = Lang.T("历史最高 ") + N(a.previousBest, "0") + " · " + XgSim.Pct(a.acc);
            cardReward.text = "";
            ribbon.text = "";
            stampText.text = ""; stampRing.color = Color.clear;
            if (cardBig) Fx.Play(XgJuice.Sfx.Id.Whoosh, 1, a.hand ? .7f : .3f);
        }

        public void TickAssessment(float dt)
        {
            if (cardT < 0 || shown == null) return;
            if (!view.Visible || !ReferenceEquals(Sim, assessmentOwner)) { DismissAssessment(); return; }
            cardT += dt;
            const float rollTime = .8f;
            float k = Mathf.Clamp01(cardT / rollTime);
            float ease = 1 - Mathf.Pow(1 - k, 3);
            cardScore.text = N(Mathf.Lerp(rollFrom, rollTo, ease), "0");
            if (k < 1 && shown.hand && cardT >= nextTick) { nextTick = cardT + .06f; Fx.Play(XgJuice.Sfx.Id.Tick, 1 + ease * .6f, .4f); }
            if (cardBig) assessmentShade.color = new Color(.035f, .045f, .1f, .68f * Mathf.Clamp01(cardT / .15f));
            else card.anchoredPosition = new Vector2(Mathf.Lerp(150, -150, 1 - Mathf.Pow(1 - Mathf.Clamp01(cardT / .25f), 3)), 86);
            if (k >= 1 && !stamped)
            {
                stamped = true;
                Stamp();
            }
            if (stamped)
            {
                float s = Mathf.Clamp01((cardT - rollTime) / .18f);
                stamp.localScale = Vector3.one * Mathf.Lerp(2.2f, 1, 1 - (1 - s) * (1 - s));
                stamp.localRotation = Quaternion.Euler(0, 0, -12);
            }
            float fade = float.IsPositiveInfinity(cardLife) ? 1 : Mathf.Clamp01((cardLife - cardT) / .2f);
            cardGroup.alpha = fade;
            cardBg.color = shown.record ? new Color(.1f, .12f, .24f, .96f) : new Color(.3f, .32f, .38f, .96f);
            if (fade <= 0) DismissAssessment();
        }

        void Stamp()
        {
            var a = shown;
            var color = XgDark.Grades[a.grade];
            stampText.text = XgCatalog.GradeNames[a.grade];
            stampText.color = color; stampRing.color = color;
            double pay = a.reward + a.gradeBonus;
            cardReward.text = a.record ? Lang.T("新纪录 +¥") + Money(pay) + (a.gradeBonus > 0 ? Lang.T("（含评级奖 ¥") + Money(a.gradeBonus) + Lang.T("）") : "")
                : Lang.T("没有进步 +¥0");
            cardReward.color = a.record ? XgDark.Gold : new Color(1, 1, 1, .5f);
            ribbon.text = a.newGrade >= 0 ? Lang.T("首次 ") + XgCatalog.GradeNames[a.newGrade] + "！" : a.record ? Lang.T("新纪录") : "";
            Vector2 at = Fx.At(stamp);
            if (!a.hand)
            {
                Fx.Play(a.record ? XgJuice.Sfx.Id.Coin : XgJuice.Sfx.Id.Tick, 1, .5f);
                if (a.record) Fx.Float(Fx.At(card, new Vector2(0, 50)), "+¥" + Money(pay), XgDark.Money, 18);
                return;
            }
            if (!a.record)
            {
                cardBg.color = new Color(.3f, .32f, .38f, .94f);
                Fx.Play(XgJuice.Sfx.Id.Thud);
                Fx.Float(Fx.At(card, new Vector2(0, 70)), Lang.T("没有进步 +¥0"), new Color(.8f, .82f, .9f), 20);
                return;
            }
            Fx.HitStop(a.grade >= 4 ? 200 : 120);
            Fx.Shake(a.grade >= 4 ? 12 : 6, a.grade >= 4 ? .45f : .3f);
            Fx.Shockwave(at, color, 260, .4f, 12);
            Fx.Play(XgJuice.Sfx.Id.Stamp);
            Fx.Flash(Color.white, .1f, .6f);
            Fx.Burst(at, 40, XgDark.Gold, XgJuice.Shape.Yen, 360);
            Fx.Burst(at, 30, Color.white, XgJuice.Shape.Confetti, 420);
            Fx.Float(Fx.At(card, new Vector2(0, 110)), Lang.T("新纪录 +¥") + Money(pay), XgDark.Gold, 32, 70, 1.1f, 1.5f);
            Fx.Play(XgJuice.Sfx.Id.Fanfare, a.grade >= 4 ? 1.12f : 1, .8f);
            if (a.grade >= 4)
            {
                Fx.Shockwave(at, XgDark.Gold, 420, .6f, 16);
                Fx.Burst(Vector2.zero, 60, XgDark.Gold, XgJuice.Shape.Star, 520, 300, 1.2f);
            }
        }

        // ───────────── refresh ─────────────

        public override void Refresh()
        {
            if (root == null) return;
            var track = Track;
            if (!Sim.TrainingUnlocked(track) && Sim.TrainingUnlocked(track == XgTrack.Vision ? XgTrack.Sequence : XgTrack.Vision))
                Sim.SelectedTrack = track = track == XgTrack.Vision ? XgTrack.Sequence : XgTrack.Vision;
            var run = Sim.Run(track);
            var d = XgCatalog.Dataset(run.dataset);
            var a = XgCatalog.Arch(run.arch);
            string[] names = { Lang.T("视觉"), Lang.T("文字 / 序列") };
            for (int i = 0; i < 2; i++)
            {
                bool on = (int)track == i, open = Sim.TrainingUnlocked((XgTrack)i);
                trackTabs[i].Show(open);
                trackTabs[i].Set(names[i] + (Sim.Runs[i].running ? " ●" : ""), open, on ? XgDark.Accent : XgDark.Button, on ? Color.white : XgDark.Ink);
            }
            string region = XgSim.RegionOf(run.dataset);
            summary.Set(T(XgSim.RegionName(region, false), XgSim.RegionName(region, true)) + T("自动用 ", " runs ") + T(a.name, a.nameEn) + " · " + T(d.name, d.nameEn) + "  <color=#8FB8FF>" + T("道具 →", "Items →") + "</color>", true, Color.clear);
            // The first time the lab rewires a region by itself: the protagonist works out what a network is to it.
            string shapeKey = (int)track + ":" + run.arch;
            if (shownStructure.Length > 0 && shownStructure != shapeKey && shownStructure[0] == shapeKey[0]) VoiceWiring();
            shownStructure = shapeKey;

            double best = Sim.BestAcc(d.id), bestScore = Sim.BestScore(d.id);
            int grade = XgSim.Grade(bestScore);
            if (glitch <= 0)
                numbers.text = T("训练 ", "Train ") + Hex(XgChartGraphic.TrainColor) + XgSim.Pct(run.trainAcc) + "</color>   "
                    + T("验证 ", "Val ") + Hex(XgChartGraphic.ValColor) + "<b>" + XgSim.Pct(run.valAcc) + "</b></color>   "
                    + Lang.T("最佳 ") + Hex(XgDark.Grades[XgSim.Grade(bestScore)]) + "<b>" + (best > 0 ? XgCatalog.GradeNames[XgSim.Grade(bestScore)] + " " + N(bestScore, "0") : "—") + "</b></color>   "
                    + Lang.T("下一评级 ") + (grade >= XgCatalog.GradeNames.Length - 1 ? T("已满级", "maxed") : XgCatalog.GradeNames[grade + 1] + " " + XgCatalog.GradeScore[grade + 1]);
            chart.Capacity = Mathf.Clamp(run.histVal.Count, 24, XgSim.HistoryLength);
            chart.SetData(run.histVal, noValidationCurve, 0, true);
            yTop.text = N(chart.Max, "0.00");
            yMid.text = N((chart.Max + chart.Min) / 2, "0.00");
            yLow.text = N(chart.Min, "0.00");
            legend.text = Hex(XgChartGraphic.TrainColor) + "━ " + T("模拟损失 −log(验证准确率)：往下是进步，往上是退步", "Simulated loss −log(validation accuracy): down is progress, up is a drop") + "</color>";
            RefreshDiagnostics(run);

            // Every epoch is assessed: no countdown pips, no separate assess button.
            for (int i = 0; i < pips.Length; i++) pips[i].gameObject.SetActive(false);

            bool unlocked = Sim.TrainingUnlocked(track);
            string blocker = Sim.StartBlocker(run, Host);
            int level = Sim.AutoTrainLevel;
            train.Set(Lang.T("训练一轮"), unlocked && blocker == null && !run.epochActive, XgDark.Accent, Color.white);
            trainSub.text = unlocked ? T("第 " + run.epoch + " 轮", "epoch " + run.epoch) + (Sim.S.combo > 0 ? Lang.T(" · 连击 ×") + N(Sim.ComboMultiplier, "0.00") : "") : Lang.T("先标够样本");
            string[] modes = { "☛ " + Lang.T("手动"), "", "◷ crontab", "▣ " + Lang.T("守护进程"), "✓ " + Lang.T("自动评估"), "⇒ AutoML" };
            mode.text = modes[Mathf.Clamp(level, 0, 5)];
            assess.Show(false);
            autoTrain.Show(level >= 2);
            if (level >= 2) autoTrain.Set(Lang.T("自动训练\n") + (run.running ? Lang.T("开") : Lang.T("关")), true, run.running ? XgDark.OnFill : (Color?)null, run.running ? XgDark.Good : (Color?)null);

            // The chance that the next round goes down, as a word and a reason (XgSim.AutoModel.cs).
            var dropRisk = Sim.DropRisk(run);
            risk.text = unlocked ? Sim.DropRiskText(run) : "";
            risk.color = dropRisk.level >= 2 ? XgDark.Bad : dropRisk.level == 1 ? XgDark.Gold : XgDark.Muted;
            string waiting = run.epochActive ? Sim.Blocker(run, Host) : null;
            hint.text = blocker != null ? "⚠ " + blocker : waiting != null ? "◷ " + waiting : Hint(run);
            hint.color = blocker != null ? XgDark.Bad : Sim.Plateaued(run) || Sim.ParamsShortForNext(run) ? XgDark.Gold : XgDark.Muted;

            RefreshModel(track, run, a);
        }

        string Hint(XgRun run)
        {
            if (!Sim.TrainingUnlocked(Track)) return T("先去标注台标够 " + XgCatalog.SamplesToTrain + " 条样本。", "Label " + XgCatalog.SamplesToTrain + " samples first.");
            if (run.epoch == 0) return Lang.T("按「训练一轮」。每轮练完都会考一次：刷新纪录才给钱。");
            string plateau = Sim.PlateauText(run, out _);
            if (plateau.Length > 0) return plateau;
            if (run.staleEvals >= XgSim.StaleHintEpochs) return Sim.StaleHint(run);
            return T("参数和样本够，就一直往上涨；连击越高，每轮涨得越多（最多 ×2）。", "With enough parameters and samples it keeps rising; a higher combo gains more per round (up to ×2).");
        }

        /// <summary>The techniques the model trains with: every one owned is on (道具 → 技巧).</summary>
        string Techniques(XgRun run)
        {
            var on = new List<string>();
            void Add(bool owned, string zh, string en) { if (owned) on.Add(T(zh, en)); }
            Add(Sim.Has("relu"), "ReLU", "ReLU");
            Add(Sim.Has("dropout"), "Dropout", "Dropout");
            Add(Sim.Has("augment"), "数据增强", "Augmentation");
            Add(Sim.BatchNormOwned, "BatchNorm", "BatchNorm");
            Add(Sim.WarmupOwned, "预热", "Warm-up");
            Add(Sim.ClipOwned, "梯度裁剪", "Clipping");
            Add(Sim.SkipOwned, "跨层直连", "Skip links");
            Add(Sim.PositionOwned, "位置标记", "Positions");
            Add(run.features, "特征工程", "Feature engineering");
            Add(Sim.Has("lrschedule"), "学习率衰减", "LR schedule");
            if (on.Count == 0)
                return T("技巧：还没有。去道具页买 Dropout、数据增强、BatchNorm、预热：买到就自动开，退步会少一些。",
                    "Techniques: none yet. Buy Dropout, augmentation, BatchNorm or warm-up on the Items page: they switch on by themselves and make drops rarer.");
            return T("技巧（买到就自动开）：", "Techniques (on once bought): ") + string.Join(T(" · ", " · "), on);
        }

        /// <summary>A memory size the way the page shows it: 0.4G, 6G.</summary>
        static string Gigabytes(double mb) => N(mb / 1024, mb < 10240 ? "0.#" : "0") + "G";

        void RefreshModel(XgTrack track, XgRun run, XgArch a)
        {
            modelTitle.text = T("模型（自动配置）· 优化器 ", "Model (configured automatically) · optimizer ") + Sim.OptimizerName;
            saveModel.Show(run.epoch > 0);
            saveModel.Set(Lang.T("存入仓库"), run.epoch > 0 && !run.epochActive, XgDark.AccentSoft, XgDark.Accent);
            double size = XgSim.ParamsK(run);
            // 「模型：ResNet · 12M 参数 · 显存 0.4/6G」
            modelInfo.text = T("模型：", "Model: ") + T(a.name, a.nameEn) + " · " + XgSim.ParamsText(size) + T(" 参数", " parameters")
                + T(" · 显存 ", " · VRAM ") + N(XgSim.VramNeedMB(run) / 1024, "0.0") + "/" + Gigabytes(Sim.Vram(Host));
            int depthCap = Sim.DepthCap(track), widthCap = Sim.WidthCap(track), archDepth = Sim.MaxDepth(run);
            bool fullDepth = run.depth >= Math.Min(depthCap, archDepth), fullWidth = run.width >= widthCap;
            shapeText.text = T("宽 ", "Width ") + XgCatalog.Widths[run.width] + "/" + XgCatalog.Widths[widthCap] + T(" · ", " · ") + run.depth + "/" + Math.Min(depthCap, archDepth) + T(" 层", " layers")
                + " <color=#58798A>(" + XgSim.ParamScale(size, Sim.English) + ")</color>\n"
                + (fullDepth && fullWidth ? T("已是科技上限里最大的一个；要更大，去科技买「宽」「层」。", "The biggest the tech tree allows; for more, buy width or layers in the tech tree.")
                    : T("显卡只装得下这么大；加显卡或接线页腾出显存，就自动长大。", "The card only holds this much; add a card or free memory on the Wiring page and it grows by itself."));
            techText.text = Techniques(run);
            double share = Host.Compute;
            string plateau = Sim.PlateauText(run, out var limit);
            string ceiling = run.epoch > 0 ? T("这个模型的上限 ", "This model's ceiling ") + N(XgSim.Score(run.dataset, Sim.CeilingAcc(run)), "0") + T(" 分", " points")
                + (Sim.Plateaued(run) ? T("（已经到了）", " (reached)") : T("，还差 ", ", ") + N(Sim.HeadroomPoints(run), "0") + T(" 分", " to go")) : "";
            statsText.text = Lang.T("每轮喂 ") + Sim.CardsPerEpoch(run, Math.Max(.5, share), true) + Lang.T(" 张 / 数据池 ") + Sim.PoolSize(run) + Lang.T(" 张（已过 ") + N(Sim.PassesOverData(run), "0.0") + Lang.T(" 遍）")
                + (run.lastScore >= 0 ? Lang.T(" · 最近评估 ") + XgCatalog.GradeNames[XgSim.Grade(run.lastScore)] + " " + N(run.lastScore, "0") : "")
                + (ceiling.Length > 0 ? "\n" + ceiling : "")
                + (plateau.Length > 0 ? "\n<color=#C88A00>" + plateau + "</color>" : "")
                + "\n" + AbilityLine(run);

            // Own mistakes plus outside noise from packs and crowd tasks, and the user-log button (XgTrainPage.Data.cs).
            double noise = Sim.Noise(run.dataset) + Sim.DataNoise(run.dataset);
            RefreshDataEconomy(run);
            bool audit = Sim.Has("label.audit");
            cleanNoise.Show(Sim.Has("label.audit"));
            int cleanable = audit ? Sim.CleanableNoise(run.dataset, Host, XgSim.NoiseCleanBatchLimit) : 0;
            string cleaningState = cleanable > 0 ? Lang.T("本次 ") + cleanable + " · ¥" + Money(cleanable * XgSim.NoiseCleanMoneyPerItem)
                : noise < 1 ? Lang.T("无噪声") : Sim.ProjectActive ? Lang.T("研发占用 GPU")
                : Host.Blocker != null || Host.Compute <= 0 ? Lang.T("无可用算力") : Lang.T("经费不足或暂不可清洗");
            cleanNoise.Set(Lang.T("清洗最多 ") + XgSim.NoiseCleanBatchLimit + T(" 条", " labels") + "\n<size=10>" + cleaningState + "</size>", cleanable > 0, cleanable > 0 ? XgDark.AccentSoft : XgDark.Button, cleanable > 0 ? XgDark.Accent : XgDark.Muted);
            noiseHint.text = audit
                ? "¥" + Money(XgSim.NoiseCleanMoneyPerItem) + " + " + N(XgSim.NoiseCleanGpuSecondsPerItem, "0.0") + Lang.T(" GPU秒 / 条") + "\n" + Lang.T("模拟数据清洗；不训练 GGUF")
                : Lang.T("模拟数据质量统计（非 GGUF 训练）");
            noiseHint.rectTransform.offsetMax = new Vector2(audit ? -180 : -14, noiseHint.rectTransform.offsetMax.y);

            dataTitle.text = Lang.T("数据包 · 拖到 GPU 安装");
            var data = XgCatalog.DatasetsFor(track).FindAll(x => Sim.DatasetAvailable(x.id) || Sim.NodeVisible(XgCatalog.Node(x.id + ".pack")));
            string dkey = string.Join(",", data.ConvertAll(x => x.id)) + track;
            if (dkey != shownDataKey)
            {
                shownDataKey = dkey;
                foreach (var b in dataBtns) UnityEngine.Object.Destroy(b.rt.gameObject);
                foreach (var b in packBtns) UnityEngine.Object.Destroy(b.rt.gameObject);
                dataBtns.Clear(); dataIds.Clear();
                packBtns.Clear();
                for (int i = 0; i < data.Count; i++)
                {
                    string id = data[i].id;
                    var b = ui.Button(dataBox, "", () => { if (Sim.SetDataset(Track, id)) { Fx.Play(XgJuice.Sfx.Id.Swoosh); Fx.Knock(dataBtns[dataIds.IndexOf(id)].rt, .12f); } Refresh(); }, 13);
                    b.rt.anchorMin = new Vector2(0, 1); b.rt.anchorMax = new Vector2(1, 1); b.rt.pivot = new Vector2(.5f, 1);
                    b.rt.offsetMin = new Vector2(0, -i * 36 - 32); b.rt.offsetMax = new Vector2(-98, -i * 36);
                    dataBtns.Add(b); dataIds.Add(id);
                    UiTip.Add(b.rt, () => { var d = XgCatalog.Dataset(id); return d == null ? "" : "<b>" + T(d.name, d.nameEn) + "</b>\n" + (string.IsNullOrEmpty(d.note) ? "" : T(d.note, d.noteEn) + "\n") + Lang.T("指标：") + T(d.metric, d.metricEn) + Lang.T("\n样本：") + Sim.Samples(id).ToString("0") + Lang.T("（不够就去标注台标，或买数据包）") + "\n" + XgDataUi.SourcesSummary(Sim, id); });
                    var install = ui.Button(dataBox, "", () => InstallPack(id), 12);
                    PlaceTopRight(install, 94, i * 36, 92, 32);
                    var drag = install.rt.gameObject.AddComponent<XgDataPackDrag>(); drag.Page = this; drag.Dataset = id;
                    UiTip.Add(install.rt, () => PackTip(id));
                    packBtns.Add(install);
                }
                dataBox.sizeDelta = new Vector2(0, data.Count * 36);
            }
            for (int i = 0; i < dataBtns.Count; i++)
            {
                var x = XgCatalog.Dataset(dataIds[i]);
                bool on = x.id == run.dataset;
                double sc = Sim.BestScore(x.id);
                string grade = Sim.BestAcc(x.id) > 0 ? "  " + Hex(on ? Color.white : XgDark.Grades[XgSim.Grade(sc)]) + XgCatalog.GradeNames[XgSim.Grade(sc)] + " " + N(sc, "0") + "</color>" : "";
                bool enough = Sim.Samples(x.id) >= XgCatalog.SamplesToTrain;
                dataBtns[i].Set(T(x.name, x.nameEn) + grade, Sim.DatasetAvailable(x.id) && (enough || on) && !run.epochActive, on ? XgDark.Accent : XgDark.Button, on ? Color.white : XgDark.Ink);
                var node = XgCatalog.Node(x.id + ".pack");
                // The public pack itself; a junk pack also makes Owns() true but leaves the public pack for sale.
                bool owned = Sim.S.owned.Contains(x.id) || node != null && Sim.Has(node.id);
                bool canBuy = node != null && Sim.Status(node, Host) == XgSim.NodeStatus.Buyable;
                packBtns[i].Show(node != null);
                // Any pack of this dataset still coming down (public, junk or story; XgSim.DataSources.cs).
                var download = XgDataUi.ActiveDownload(Sim, x.id);
                if (download != null)
                {
                    // 摆渡云 at 100KB/s: shows the time left; clicking pays for acceleration.
                    packBtns[i].Show(true);
                    double speed = Sim.AccelerateOfferCost(download.id);
                    bool rich = Host.Money >= speed;
                    packBtns[i].Set(N(download.downloadProgress * 100, "0") + Lang.T("% · 加速 ¥") + Money(speed), rich, rich ? XgDark.AccentSoft : XgDark.Button, rich ? XgDark.Accent : XgDark.Muted);
                }
                else packBtns[i].Set(owned ? Lang.T("已安装") : node != null ? "↓ ¥" + Money(Sim.NodeCost(node)) : "", !owned && canBuy, canBuy ? XgDark.AccentSoft : XgDark.Button, canBuy ? XgDark.Accent : XgDark.Muted);
            }
            // Pack switches of the current dataset below the list; also sizes the scroll content (XgTrainPage.Sources.cs).
            RefreshSources(run, dataBtns.Count);
            double watts = Host is XingGuangHost home ? home.TrainingWatts : 0;
            gpuLabel.text = "▣ GPU  · " + Lang.T("松手安装数据包") + "\n" +
                Lang.T("训练负载 ") + N(watts, "0") + " W  · " + Lang.T("累计 GPU 时间 ") + N(Sim.S.trainedSeconds, "0.0") + " s";
        }

        void RefreshDiagnostics(XgRun run)
        {
            int stage = Sim.StageFor(Track);
            string[] zh = { "", "权重 · 28×28 像素", "MLP · 隐藏层组合", Track == XgTrack.Vision ? "局部结构 · 深度与表现" : "序列 · 记忆衰减", Track == XgTrack.Vision ? "残差 · 信息保留" : "门控 · 长期记忆", "注意力 · 寻找线索", "多头注意力 · 并行" };
            string[] en = { "", "Weights · 28×28 pixels", "MLP · hidden features", Track == XgTrack.Vision ? "Local structure · depth" : "Sequence · memory decay", Track == XgTrack.Vision ? "Residual · preserve information" : "Gates · longer memory", "Attention · find the clue", "Multi-head attention · parallel" };
            diagnosticTitle.text = stage + " · " + T(zh[stage], en[stage]);
            diagnostic.Show(Sim, run, stage);
            diagnosticNote.text = stage >= 5
                ? Lang.T("教学示意；模拟 GPU ") + XgSim.Pct(Sim.GpuUtilization(run))
                : Lang.T("教学示意，不是本地模型的内部权重");
            int count = stage - 1;
            for (int i = 0; i < previousDiagnostics.Count; i++)
            {
                var previous = previousDiagnostics[i];
                previous.gameObject.SetActive(i < count);
                if (i >= count) continue;
                var rt = previous.rectTransform;
                rt.anchorMin = new Vector2(1, i / (float)count); rt.anchorMax = new Vector2(1, (i + 1f) / count);
                rt.offsetMin = new Vector2(-85, 22f / count); rt.offsetMax = new Vector2(-3, -25f / count);
                previous.Show(Sim, run, i + 1);
            }
        }

        public bool CanInstallPack(string dataset)
        {
            var node = XgCatalog.Node(dataset + ".pack");
            return node != null && Sim.NodeVisible(node) && Sim.Status(node, Host) == XgSim.NodeStatus.Buyable;
        }

        public bool InstallPack(string dataset)
        {
            var download = XgDataUi.ActiveDownload(Sim, dataset);
            if (download != null)
            {
                if (Sim.AccelerateOffer(download.id, Host)) { Fx.Play(XgJuice.Sfx.Id.Unlock); view.Refresh(true); return true; }
                Fx.Play(XgJuice.Sfx.Id.Thud); return false;
            }
            if (!CanInstallPack(dataset) || !Sim.BuyNode(dataset + ".pack", Host)) { Fx.Play(XgJuice.Sfx.Id.Thud); return false; }
            if (Sim.Downloading(dataset))
                LingGuangV05.Desktop.Story.PrologueDirector.Desk?.Popup(Lang.T("摆渡云"),
                    T("正在下载《" + XgCatalog.Dataset(dataset).name + "》……非会员限速 100KB/s，预计 " + N(Sim.DownloadLeft(dataset), "0") + " 秒。开通超级会员立享极速下载！",
                      "Downloading " + XgCatalog.Dataset(dataset).nameEn + "… free users are limited to 100KB/s, about " + N(Sim.DownloadLeft(dataset), "0") + " s. Go super member for full speed!"), 8);
            Fx.Knock(gpuTarget, .12f, new Vector2(0, -8));
            Fx.Shockwave(Fx.At(gpuTarget), XgDark.Accent, 150, .35f, 7);
            Fx.Burst(Fx.At(gpuTarget), 20, XgDark.Gold, XgJuice.Shape.Spark, 220);
            Fx.Play(XgJuice.Sfx.Id.Clack, .7f, .7f); Fx.Play(XgJuice.Sfx.Id.Buzz, 1.8f, .25f);
            view.Refresh(true);
            return true;
        }

        public void SetPackDragging(bool dragging) { gpuGlow.on = dragging; }

        void FocusNode(string id)
        {
            view.ShowTab("tree");
            if (!view.Tree.FocusNode(id)) view.Tree.FocusFrontier();
        }

        void NodeLink(TMP_Text label, Func<string> id)
        {
            label.raycastTarget = true;
            var button = label.gameObject.AddComponent<Button>();
            button.targetGraphic = label; button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => FocusNode(id()));
        }

        string ShapeNode(XgNodeKind kind)
        {
            XgNode best = null;
            string lane = Sim.StageFor(Track) < 3 ? "trunk" : Track == XgTrack.Vision ? "vision" : "sequence";
            foreach (var node in XgCatalog.Nodes)
                if (node.kind == kind && node.lane == lane && Sim.NodeVisible(node) && (best == null || !Sim.Has(node.id) && (Sim.Has(best.id) || node.value < best.value))) best = node;
            return best != null ? best.id : Sim.Run(Track).arch;
        }
    }

    public sealed class XgTrainingSubmit : MonoBehaviour, ISubmitHandler
    {
        public Action Submit;
        public void OnSubmit(BaseEventData e) { Submit?.Invoke(); }
    }

    public sealed class XgAssessmentClock : MonoBehaviour
    {
        public XgTrainPage Page;
        void Update() { Page?.TickAssessment(Time.unscaledDeltaTime); }
    }

    public sealed class XgDataPackDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public XgTrainPage Page;
        public string Dataset;
        public bool Dragging { get; private set; }
        RectTransform ghost, surface;
        public void OnBeginDrag(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left || Page == null || !Page.CanInstallPack(Dataset)) return;
            surface = Page.root;
            ghost = Rect("DraggingDataPack", surface, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-36, -24), new Vector2(36, 24));
            Panel(ghost, XgDark.Accent).raycastTarget = false;
            ghost.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
            Dragging = true; Page.SetPackDragging(true); OnDrag(e);
        }
        public void OnDrag(PointerEventData e)
        {
            if (!Dragging) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(surface, e.position, e.pressEventCamera, out var local)) ghost.anchoredPosition = local;
        }
        public void OnEndDrag(PointerEventData e) { Cancel(); }
        void OnDisable() { Cancel(); }
        void Cancel()
        {
            if (ghost != null) Destroy(ghost.gameObject);
            ghost = null; Dragging = false; Page?.SetPackDragging(false);
        }
    }

    public sealed class XgGpuInstallDrop : MonoBehaviour, IDropHandler
    {
        public XgTrainPage Page;
        public void OnDrop(PointerEventData e)
        {
            var drag = e.pointerDrag != null ? e.pointerDrag.GetComponent<XgDataPackDrag>() : null;
            if (drag != null && drag.Dragging && drag.Page == Page) Page.InstallPack(drag.Dataset);
        }
    }
}
