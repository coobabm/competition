using System;
using System.Collections.Generic;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 训练: one big 训练一轮 button (0.6 s per epoch), the learning curve, the assessment card every record earns
    /// (each epoch ends with an exam) with its grade stamp, and the model's knobs.
    /// </summary>
    public sealed partial class XgTrainPage : XgPage
    {
        public const float AutomaticCardSeconds = 3;
        /// <summary>Every epoch is assessed; only a new grade by hand earns the big card, other records a corner card.</summary>
        public static bool UseAssessmentOverlay(XgAssessment a) => a.hand && a.newGrade >= 1;
        XgBtn[] trackTabs = new XgBtn[2];
        XgBtn summary, train, assess, autoTrain, depthDown, depthUp, widthDown, widthUp, saveModel, cleanNoise;
        /// <summary>Rule knobs (design v1.1 §4.3): activation, gradient clipping, skip connections, position tags, warm-up.</summary>
        XgBtn[] acts; XgBtn[] toggles;
        XgBtn[] lrs;
        readonly List<XgBtn> archBtns = new List<XgBtn>();
        readonly List<XgBtn> dataBtns = new List<XgBtn>();
        readonly List<XgBtn> packBtns = new List<XgBtn>();
        readonly List<float> noValidationCurve = new List<float>();
        readonly List<string> archIds = new List<string>(), dataIds = new List<string>();
        RectTransform left, right, chartArea, epochBar, epochFill, archBox, dataBox, dataViewport, gpuTarget, diagnosticArea;
        RectTransform[] pips;
        XgChartGraphic chart;
        XgStageGraphic diagnostic;
        readonly List<XgStageGraphic> previousDiagnostics = new List<XgStageGraphic>();
        TMP_Text diagnosticTitle, diagnosticNote, gpuLabel, noiseText, noiseHint;
        XgGlow gpuGlow;
        TMP_Text numbers, yTop, yMid, yLow, legend, risk, hint, mode, trainSub, modelTitle, depthText, widthText, lrText, statsText, dataTitle;
        XgGlow trainGlow;
        XgHold hold;
        float glitch;
        Vector2 chartRestPosition;
        string shownArchKey = "", shownDataKey = "";

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

        RectTransform depthConfirmation;
        TMP_Text depthWarning;
        XgBtn depthConfirm, depthCancel;
        XgSim depthOwner;
        XgTrack depthTrack;
        string depthDataset;
        int requestedDepth, previousDepth, depthReleasedFrame;
        bool depthInputReleased;
        float depthReadyAt;
        readonly Dictionary<Selectable, bool> depthBackground = new Dictionary<Selectable, bool>();

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
            summary = ui.Button(left, "", () => FocusNode(Sim.Run(Track).arch), 14);
            summary.rt.anchorMin = new Vector2(0, 1); summary.rt.anchorMax = new Vector2(1, 1);
            summary.rt.offsetMin = new Vector2(244, -42); summary.rt.offsetMax = new Vector2(-14, -10);
            summary.label.alignment = TextAlignmentOptions.MidlineLeft;
            numbers = ui.Text(Strip("Numbers", left, 48, 26, 16, 16), "", 16, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);

            chartArea = Rect("ChartArea", left, Vector2.zero, Vector2.one, new Vector2(56, 214), new Vector2(-16, -80));
            Panel(chartArea, new Color32(250, 251, 254, 255));
            chartRestPosition = chartArea.anchoredPosition;
            chart = Rect("Chart", chartArea, Vector2.zero, new Vector2(1, .52f), new Vector2(4, 4), new Vector2(-4, -4)).gameObject.AddComponent<XgChartGraphic>();
            chart.raycastTarget = false;
            chart.Capacity = XgSim.HistoryLength;
            BuildDiagnostics();
            yTop = ui.Text(Rect("YTop", chart.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(-54, -11), new Vector2(-2, 11)), "", 12, XgPalette.Muted, TextAlignmentOptions.MidlineRight);
            yMid = ui.Text(Rect("YMid", chart.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(-54, -11), new Vector2(-2, 11)), "", 12, XgPalette.Muted, TextAlignmentOptions.MidlineRight);
            yLow = ui.Text(Rect("YLow", chart.rectTransform, Vector2.zero, Vector2.zero, new Vector2(-54, -11), new Vector2(-2, 11)), "", 12, XgPalette.Muted, TextAlignmentOptions.MidlineRight);
            legend = ui.Text(Rect("Legend", left, Vector2.zero, new Vector2(1, 0), new Vector2(56, 188), new Vector2(-16, 210)), "", 13, XgPalette.Muted, TextAlignmentOptions.MidlineLeft);

            epochBar = Rect("EpochBar", left, Vector2.zero, new Vector2(1, 0), new Vector2(16, 170), new Vector2(-150, 182));
            epochFill = Bar(epochBar, "Fill", new Color32(230, 234, 244, 255), XgPalette.Accent);
            for (int i = 1; i < 16; i++)
            {
                var gap = Rect("EpochSegment" + i, epochBar, new Vector2(i / 16f, 0), new Vector2(i / 16f, 1), new Vector2(-1, 0), new Vector2(1, 0));
                Panel(gap, Color.white).raycastTarget = false;
            }
            pips = new RectTransform[4];
            for (int i = 0; i < 4; i++)
            {
                pips[i] = Rect("Pip" + i, left, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-138 + i * 30, 168), new Vector2(-114 + i * 30, 184));
                Panel(pips[i], XgPalette.Disabled).raycastTarget = false;
            }

            var trainRect = Rect("TrainAt", left, Vector2.zero, Vector2.zero, new Vector2(16, 76), new Vector2(316, 156));
            trainGlow = Glow(trainRect, XgPalette.Accent, 6);
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
            risk = ui.Text(Rect("Risk", left, Vector2.zero, new Vector2(1, 0), new Vector2(16, 36), new Vector2(-16, 70)), "", 14, XgPalette.Bad, TextAlignmentOptions.MidlineLeft);
            hint = ui.Text(Rect("Hint", left, Vector2.zero, new Vector2(1, 0), new Vector2(16, 6), new Vector2(-16, 36)), "", 14, XgPalette.Muted, TextAlignmentOptions.MidlineLeft);

            BuildModel();
            BuildCard();
            for (int i = 0; i < 2; i++) UiTip.Add(trackTabs[i].rt, "两条线：视觉（看图）和序列（读文字）。各练各的，共用显卡和经费。", "Two tracks: vision (images) and sequence (text). They train separately and share the GPU and funds.");
            UiTip.Add(train.rt, "训练一轮：喂模型一批卡，练完在没见过的题上考一次。\n这里的「一轮」是一批，不是把整个数据集过一遍：数据越多，要越多轮才过完一遍（下面写着已过几遍）。\n手动按会叠连击（学得更多）；刷新纪录就记成绩、存检查点、发奖金。", "Train one epoch: feed the model a batch of cards, then an exam on unseen cards.\nAn epoch here is a batch, not a pass over the whole dataset: the more data, the more epochs one pass takes (see the passes below).\nPressing by hand builds combo (it learns more); a new record is scored, saved and paid.");
            UiTip.Add(autoTrain.rt, "自动训练：每隔几秒自己训练一轮（效果是手按的一半，不算连击）。", "Auto-train: runs an epoch every few seconds (half as effective as by hand, no combo).");
            UiTip.Add(summary.rt, "当前结构的摘要。点一下在科技里找到它。", "Summary of the current structure. Click to find it in Research.");
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
            cardReward = ui.Text(Rect("Reward", card, Vector2.zero, new Vector2(1, 0), new Vector2(16, 6), new Vector2(-16, 30)), "", 16, XgPalette.Gold, TextAlignmentOptions.MidlineLeft);
            stamp = Rect("Stamp", card, new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-150, -62), new Vector2(-26, 62));
            stampRing = stamp.gameObject.AddComponent<XgRingGraphic>(); stampRing.Width = 8; stampRing.raycastTarget = false;
            stampText = ui.Text(Rect("Letter", stamp, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 72, Color.white, TextAlignmentOptions.Center);
            stampText.fontStyle = FontStyles.Bold;
            ribbon = ui.Text(Rect("Ribbon", card, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-180, -32), new Vector2(-12, -8)), "", 16, XgPalette.Gold, TextAlignmentOptions.MidlineRight);
            ribbon.fontStyle = FontStyles.Bold;
            assessmentLayer.gameObject.SetActive(false);
        }

        void DismissAssessment() { cardT = -1; assessmentLayer.gameObject.SetActive(false); }

        void BuildDiagnostics()
        {
            diagnosticArea = Rect("StageDiagnostics", chartArea, new Vector2(0, .55f), Vector2.one, new Vector2(5, 0), new Vector2(-5, -3));
            diagnosticTitle = ui.Text(Strip("Title", diagnosticArea, 0, 22, 2, 2), "", 13, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
            diagnostic = Rect("Current", diagnosticArea, Vector2.zero, Vector2.one, new Vector2(3, 20), new Vector2(-95, -25)).gameObject.AddComponent<XgStageGraphic>();
            diagnostic.raycastTarget = false;
            for (int i = 0; i < 5; i++)
            {
                var r = Rect("PreviousStage" + (i + 1), diagnosticArea, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-88, 22 + i * 16), new Vector2(-3, -25));
                var g = r.gameObject.AddComponent<XgStageGraphic>(); g.raycastTarget = false; previousDiagnostics.Add(g);
            }
            diagnosticNote = ui.Text(Rect("Caveat", diagnosticArea, Vector2.zero, new Vector2(1, 0), new Vector2(2, 0), new Vector2(-2, 19)), "", 11, XgPalette.Muted, TextAlignmentOptions.MidlineLeft);
            BuildMistakes();
        }

        void BuildModel()
        {
            right = ui.Card(root, "Model", new Vector2(.62f, 0), Vector2.one, new Vector2(6, 0), Vector2.zero);
            modelTitle = ui.Text(Strip("ModelTitle", right, 8, 28, 14, 128), "", 17, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
            saveModel = ui.Button(right, "", () =>
            {
                if (Sim.SaveModel(Track) == null) { Fx.Play(XgJuice.Sfx.Id.Thud); return; }
                Fx.Knock(saveModel.rt, .15f);
                Fx.Burst(Fx.At(saveModel.rt), 10, XgPalette.Accent, XgJuice.Shape.Spark, 180);
                view.Refresh(true);
            }, 14);
            PlaceTopRight(saveModel, 124, 8, 110, 28);
            modelTitle.fontStyle = FontStyles.Bold;
            var archScroll = Strip("Archs", right, 40, 108, 14, 14);
            var archViewport = Rect("Viewport", archScroll, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Panel(archViewport, new Color(1, 1, 1, .01f)); archViewport.gameObject.AddComponent<RectMask2D>();
            archBox = Rect("Content", archViewport, new Vector2(0, 1), Vector2.one, Vector2.zero, Vector2.zero); archBox.pivot = new Vector2(.5f, 1);
            var archScroller = archScroll.gameObject.AddComponent<ScrollRect>(); archScroller.viewport = archViewport; archScroller.content = archBox;
            archScroller.horizontal = false; archScroller.vertical = true; archScroller.scrollSensitivity = 24; archScroller.movementType = ScrollRect.MovementType.Clamped;
            depthText = ui.Text(Strip("Depth", right, 154, 30, 14, 104), "", 15, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
            depthDown = ui.Button(right, "−", () => { RequestDepth(Sim.Run(Track).depth - 1); }, 18);
            depthUp = ui.Button(right, "+", () => { RequestDepth(Sim.Run(Track).depth + 1); }, 18);
            PlaceTopRight(depthDown, 96, 154, 40, 30); PlaceTopRight(depthUp, 52, 154, 40, 30);
            widthText = ui.Text(Strip("Width", right, 188, 30, 14, 104), "", 15, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
            widthDown = ui.Button(right, "−", () => { Sim.SetWidth(Track, Sim.Run(Track).width - 1, Host); Knob(widthDown); }, 18);
            widthUp = ui.Button(right, "+", () => { if (Sim.SetWidth(Track, Sim.Run(Track).width + 1, Host)) Knob(widthUp); else Deny(widthUp); }, 18);
            PlaceTopRight(widthDown, 96, 188, 40, 30); PlaceTopRight(widthUp, 52, 188, 40, 30);
            lrText = ui.Text(Strip("LrText", right, 222, 26, 14, 14), "", 15, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
            NodeLink(modelTitle, () => Sim.Run(Track).arch);
            NodeLink(depthText, () => ShapeNode(XgNodeKind.Depth));
            NodeLink(widthText, () => ShapeNode(XgNodeKind.Width));
            NodeLink(lrText, () => "shared.lr");
            UiTip.Add(saveModel.rt, "把当前模型存进模型仓库。", "Save the current model to the model library.");
            foreach (var b in new[] { depthDown, depthUp }) UiTip.Add(b.rt, DepthTip);
            foreach (var b in new[] { widthDown, widthUp }) UiTip.Add(b.rt, WidthTip);
            lrs = new XgBtn[XgCatalog.LearningRates.Length];
            for (int i = 0; i < lrs.Length; i++)
            {
                int index = i;
                lrs[i] = ui.Button(right, Sim.RateLabel(i), () => { if (Sim.SetLr(Track, index)) Knob(lrs[index]); }, 14);
                lrs[i].rt.anchorMin = new Vector2(i / (float)lrs.Length, 1); lrs[i].rt.anchorMax = new Vector2((i + 1f) / lrs.Length, 1);
                lrs[i].rt.offsetMin = new Vector2(14, -278); lrs[i].rt.offsetMax = new Vector2(-4, -250);
                UiTip.Add(lrs[i].rt, "学习率：每一步改多少。大了学得快但会「炸」（loss 变 NaN）；小了稳但慢。\n炸了就往小调。结构越复杂，越要小。", "Learning rate: how much each step changes. Big learns fast but can blow up (NaN); small is steady but slow.\nIf it blows up, go smaller. Complex structures want small rates.");
            }
            string[] actNames = { "阶跃", "S 形", "ReLU" }, actNamesEn = { "Step", "S-curve", "ReLU" };
            acts = new XgBtn[3];
            for (int i = 0; i < acts.Length; i++)
            {
                int index = i;
                acts[i] = ui.Button(right, T(actNames[i], actNamesEn[i]), () => { if (Sim.SetActivation(Track, index)) Knob(acts[index]); }, 13);
                acts[i].rt.anchorMin = new Vector2(i / 3f, 1); acts[i].rt.anchorMax = new Vector2((i + 1f) / 3, 1);
                acts[i].rt.offsetMin = new Vector2(14, -312); acts[i].rt.offsetMax = new Vector2(-4, -286);
                UiTip.Add(acts[i].rt, "激活函数：每个单元怎么「出声」。\n阶跃没有坡度，误差传不回去，多层网络学不动；S 形和 ReLU 可以。", "Activation: how each unit responds.\nA step has no slope, so errors cannot flow back through layers; S-curve and ReLU can.");
            }
            toggles = new XgBtn[7];
            for (int i = 0; i < toggles.Length; i++)
            {
                int index = i;
                toggles[i] = ui.Button(right, "", () => { if (Toggle(index)) Knob(toggles[index]); }, 11);
                toggles[i].label.enableAutoSizing = true; toggles[i].label.fontSizeMin = 8; toggles[i].label.fontSizeMax = 11;
                toggles[i].rt.anchorMin = new Vector2(i / (float)toggles.Length, 1); toggles[i].rt.anchorMax = new Vector2((i + 1f) / toggles.Length, 1);
                toggles[i].rt.offsetMin = new Vector2(14, -344); toggles[i].rt.offsetMax = new Vector2(-4, -318);
                UiTip.Add(toggles[i].rt, () => ToggleHelp(index));
            }
            statsText = ui.Text(Strip("Stats", right, 348, 58, 14, 14), "", 13, XgPalette.Muted, TextAlignmentOptions.TopLeft);
            statsText.enableAutoSizing = true; statsText.fontSizeMin = 10; statsText.fontSizeMax = 13;
            UiTip.Add(statsText, () => T("参数量 = 层数 × 宽度² × 结构系数。括号里拿真实的著名模型比一比大小，只是参考；\n能不能学会，还要看结构、激活、学习率和数据。",
                "Parameters = layers × width² × architecture factor. The famous model in brackets is only a sense of scale;\nwhether it learns also depends on structure, activation, learning rate and data."));
            noiseText = ui.Text(Strip("DatasetNoise", right, 410, 20, 14, 14), "", 13, XgPalette.Muted, TextAlignmentOptions.MidlineLeft);
            noiseText.enableAutoSizing = true; noiseText.fontSizeMin = 11; noiseText.fontSizeMax = 13;
            noiseText.textWrappingMode = TextWrappingModes.NoWrap;
            noiseHint = ui.Text(Strip("NoiseCleaningHint", right, 432, 30, 14, 180), "", 11, XgPalette.Muted, TextAlignmentOptions.MidlineLeft);
            noiseHint.enableAutoSizing = true; noiseHint.fontSizeMin = 9; noiseHint.fontSizeMax = 11;
            noiseHint.textWrappingMode = TextWrappingModes.NoWrap;
            cleanNoise = ui.Button(right, "", CleanCurrentNoise, 12);
            cleanNoise.rt.gameObject.name = "CleanDatasetNoise";
            PlaceTopRight(cleanNoise, 168, 430, 154, 34);
            BuildDataEconomy(right);
            dataTitle = ui.Text(Strip("DataTitle", right, 468, 24, 14, 14), "", 16, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
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
            Panel(gpuTarget, XgPalette.AccentSoft);
            gpuGlow = Glow(gpuTarget, XgPalette.Gold, 4);
            gpuLabel = ui.Text(Rect("GpuLabel", gpuTarget, Vector2.zero, Vector2.one, new Vector2(12, 4), new Vector2(-12, -4)), "", 14, XgPalette.Accent, TextAlignmentOptions.MidlineLeft);
            gpuTarget.gameObject.AddComponent<XgGpuInstallDrop>().Page = this;
        }

        static string ToggleHelp(int index)
        {
            switch (index)
            {
                case 0: return T("梯度裁剪：每一步改动再大也不超过上限，防止「炸」。循环网络必备。", "Gradient clipping: caps every step so training does not blow up. A must for recurrent nets.");
                case 1: return T("跨层直连：每层留一条捷径，原样转交成了默认，多出来的层不再把票改写。网络超过 20 层时必须开。", "Skip connections: a shortcut around every layer makes passing on unchanged the default, so extra layers stop rewriting the votes. Needed past 20 layers.");
                case 2: return T("位置标记：给每个字一个位置编号。不用循环时，它靠这个知道先后顺序。", "Position tags: number every word's position. Without a loop this is how it knows the order.");
                case 3: return T("预热：换了设置以后，前 60 张卡的学习率从很小慢慢升上去。开头误差最大，这时步子小才不炸。深网络和 Transformer 尤其需要。", "Warm-up: after a change, the rate climbs from almost nothing over the first 60 cards. Early errors are the largest, so small steps keep it from tearing. Deep nets and Transformers need it most.");
                case 6: return T("BatchNorm：每层把数值拉回同一个尺度。能用更大的学习率，20 层左右的普通网络也练得动；再深还是越深越差，要靠跨层直连。", "BatchNorm: every layer brings its numbers back to one scale. Takes larger rates and lets plain nets of about 20 layers train; deeper than that still gets worse without skip connections.");
                case 5: return T("特征工程：人替它做特征——逻辑题把两个条件拼成一个，图片去噪点再居中，句子去掉语气词、按字和两字词读。\n不换结构也能过墙，但人工整理费时间：每轮训练量减半。",
                    "Feature engineering: people make the features — logic pairs two conditions, pictures are denoised and centred, sentences drop fillers and are read as words and word pairs.\nPasses walls without a new structure, but by hand: half the cards per epoch.");
                default: return T("只用注意力：拿掉循环，每个字直接看所有字，可以并行算。", "Attention only: drop the loop; every word looks at every word, all in parallel.");
            }
        }

        void Knob(XgBtn b) { Fx.Knock(b.rt, .15f); Fx.Play(XgJuice.Sfx.Id.Tick, 1.3f); Refresh(); }

        /// <summary>The standing wall: its datasets, targets, and whether this run is set up the golden way yet.</summary>
        /// <summary>Depth buttons: what a layer more or less does to the parameter count, and where the cap comes from.</summary>
        string DepthTip()
        {
            var run = Sim.Run(Track);
            double now = XgSim.ParamsK(run);
            int cap = Sim.DepthCap(Track);
            string up = run.depth < cap ? T("加一层 → ", "One more layer → ") + Params(XgSim.ParamsKWith(run, run.depth + 1, run.width))
                : T("已到上限：科技买「" + (run.depth + 1) + " 层」之类的层数节点", "At the cap: buy a layer node such as \"" + (run.depth + 1) + " layers\" in the tech tree");
            string down = run.depth > 1 ? T("减一层 → ", "One fewer → ") + Params(XgSim.ParamsKWith(run, run.depth - 1, run.width)) : "";
            return T("层数：网络有几层。层多能学更复杂的东西，但更慢、更占显存；太深还会越练越差（要靠跨层直连）。", "Depth: how many layers. More layers learn harder things but are slower and use more VRAM; too deep gets worse without skip connections.")
                + "\n" + T("现在 ", "Now ") + run.depth + T(" 层 · 参数量 ", " layers · ") + Params(now) + T("", " parameters") + T("（参数量跟层数成正比）", " (grows in step with depth)")
                + "\n" + up + (down.Length > 0 ? "　" + down : "");
        }

        /// <summary>Width buttons: wider layers multiply the parameter count, and the cap comes from the 「宽 N」 nodes.</summary>
        string WidthTip()
        {
            var run = Sim.Run(Track);
            double now = XgSim.ParamsK(run);
            int cap = Sim.WidthCap(Track), w = XgCatalog.Widths[run.width];
            string up = run.width < cap ? T("加宽到 ", "Widen to ") + XgCatalog.Widths[run.width + 1] + " → " + Params(XgSim.ParamsKWith(run, run.depth, run.width + 1)) + T("（×4）", " (×4)")
                : run.width + 1 < XgCatalog.Widths.Length ? T("已到上限：科技买「宽 " + XgCatalog.Widths[run.width + 1] + "」", "At the cap: buy \"Width " + XgCatalog.Widths[run.width + 1] + "\" in the tech tree")
                : T("已是最宽", "Already the widest");
            return T("宽度：每层有多少个单元。越宽记得越多，也越占显存。", "Width: units per layer. Wider remembers more and uses more VRAM.")
                + "\n" + T("现在宽 ", "Now width ") + w + T(" · 参数量 ", " · ") + Params(now) + T("", " parameters") + T("（宽度翻倍，参数量 ×4）", " (double the width, ×4 parameters)")
                + "\n" + up;
        }

        string WallLine(XgRun run)
        {
            var wall = Sim.ActiveWall;
            if (wall == null) return Sim.S.stage == 1 ? T("还没撞墙。先把手上的桌练到 C 级。", "No wall yet. Get a desk to grade C first.") : T("还没撞墙。", "No wall yet.");
            var sb = new System.Text.StringBuilder("<color=#D63031>" + T("墙 · ", "Wall · ") + T(wall.name, wall.nameEn) + "</color>  ");
            foreach (var c in wall.checks)
            {
                bool done = Sim.WallCheckPassed(wall, c);
                var d = XgCatalog.Dataset(c.dataset);
                double acc = d != null ? Sim.Board.Accuracy(Sim.TestSet(c.dataset), Sim.Knobs(Sim.Run(d.track))) : 0;
                sb.Append(done ? "✓ " : "○ ").Append(d != null ? T(d.name, d.nameEn) : c.dataset).Append(" ").Append(XgSim.Pct(acc)).Append("/").Append(XgSim.Pct(c.target)).Append("  ");
            }
            return sb.ToString();
        }

        bool Toggle(int index)
        {
            var run = Sim.Run(Track);
            switch (index)
            {
                case 0: return Sim.SetClip(Track, !run.clip);
                case 1: return Sim.SetSkip(Track, !run.skip);
                case 2: return Sim.SetPosition(Track, !run.position);
                case 3: return Sim.SetWarmup(Track, !run.warmup);
                case 5: return Sim.SetFeatures(Track, !run.features);
                case 6: return Sim.SetBatchNorm(Track, run.batchNormOff);
                default: return Sim.SetAttentionOnly(Track, !run.attnOnly);
            }
        }

        void RefreshKnobs(XgRun run)
        {
            bool board = Sim.UseBoard;
            bool anyAct = board && (Sim.ActivationOwned(1) || Sim.ActivationOwned(2));
            for (int i = 0; i < acts.Length; i++)
            {
                acts[i].Show(anyAct && Sim.ActivationOwned(i));
                bool on = Sim.EffectiveActivation(run) == i;
                acts[i].Set(T(new[] { "激活：阶跃", "S 形", "ReLU" }[i], new[] { "Act: step", "S-curve", "ReLU" }[i]), !run.epochActive, on ? XgPalette.Accent : XgPalette.Button, on ? Color.white : XgPalette.Ink);
            }
            bool[] owned = { Sim.ClipOwned, Sim.SkipOwned, Sim.PositionOwned, Sim.WarmupOwned, Sim.AttentionOnlyOwned && run.arch == "attention", Sim.FeaturesOwned, Sim.BatchNormOwned };
            bool[] state = { run.clip, run.skip || run.arch == "resnet" || run.arch == "transformer", run.position, run.warmup, run.attnOnly, run.features, !run.batchNormOff };
            string[] zh = { "梯度裁剪", "跨层直连", "位置标记", "预热", "只用注意力", "特征工程", "BatchNorm" }, en = { "Clip", "Skip", "Positions", "Warm-up", "Attn only", "Features", "BatchNorm" };
            for (int i = 0; i < toggles.Length; i++)
            {
                toggles[i].Show(board && owned[i]);
                toggles[i].Set(T(zh[i], en[i]) + (state[i] ? T(" 开", " on") : T(" 关", " off")), !run.epochActive, state[i] ? XgPalette.AccentSoft : XgPalette.Button, state[i] ? XgPalette.Accent : XgPalette.Muted);
            }
        }
        void Deny(XgBtn b) { Fx.Knock(b.rt, .05f, new Vector2(8, 0)); Fx.Play(XgJuice.Sfx.Id.Thud, 1.4f, .6f); Refresh(); }

        // ───────────── pressing ─────────────

        void RequestDepth(int depth)
        {
            if (depthOwner != null) return;
            var run = Sim.Run(Track);
            if (run.epochActive || run.running || depth < 1 || depth > Sim.DepthCap(Track)) return;
            if (depthConfirmation == null)
            {
                depthConfirmation = Rect("ConfirmDepthReset", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                Panel(depthConfirmation, new Color(0, 0, 0, .65f));
                var box = Rect("Card", depthConfirmation, new Vector2(.12f, .28f), new Vector2(.88f, .72f), Vector2.zero, Vector2.zero);
                Panel(box, XgPalette.Page);
                depthWarning = ui.Text(Rect("Warning", box, new Vector2(0, .3f), Vector2.one, new Vector2(18, 8), new Vector2(-18, -16)), "", 16, XgPalette.Ink, TextAlignmentOptions.TopLeft);
                depthWarning.enableAutoSizing = true; depthWarning.fontSizeMin = 12; depthWarning.fontSizeMax = 16;
                depthCancel = ui.Button(box, "", () => { CancelDepth(); Refresh(); }, 14); PlaceBottomLeft(depthCancel, 18, 12, 130, 36);
                depthConfirm = ui.Button(box, "", ConfirmDepth, 14);
                depthConfirm.rt.anchorMin = depthConfirm.rt.anchorMax = new Vector2(1, 0);
                depthConfirm.rt.offsetMin = new Vector2(-160, 12); depthConfirm.rt.offsetMax = new Vector2(-18, 48);
                depthCancel.button.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = depthConfirm.button, selectOnRight = depthConfirm.button };
                depthConfirm.button.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = depthCancel.button, selectOnRight = depthCancel.button };
                var lifetime = root.gameObject.AddComponent<XgDepthConfirmationLifetime>();
                lifetime.Disabled = CancelDepth; lifetime.Poll = PollDepthConfirmation;
            }
            depthOwner = Sim; depthTrack = Track; depthDataset = run.dataset; previousDepth = run.depth; requestedDepth = depth;
            depthReadyAt = Time.unscaledTime + .5f; depthInputReleased = false;
            depthConfirmation.SetAsLastSibling(); depthConfirmation.gameObject.SetActive(true);
            DisableDepthBackground(); RefreshDepthWarning();
            depthCancel.button.Select();
        }

        void RefreshDepthWarning()
        {
            var run = Sim.Run(depthTrack);
            bool deferred = run.formal != null && run.formal.set;
            bool reset = !deferred || requestedDepth != run.formal.depth;
            string effect = !reset ? T("\n恢复到当前模型的原层数，不会重置学习成果。", "\nRestores the trained depth without resetting learned concepts.")
                : deferred ? T("\n下一轮正式训练将重置该板块的学习成果（保留种子）。", "\nThe next real epoch resets this region's learned concepts (seeds survive).")
                : T("\n确认后将立即重建该板块，学习成果会重置（保留种子）。", "\nConfirming rebuilds this region immediately, resetting learned concepts (seeds survive).");
            depthWarning.text = T("正式设置：层数 ", "Live settings: depth ") + previousDepth + " → " + requestedDepth + effect
                + T("\n取消不改设置。无损比较请去「诊断 → 调整方案」。", "\nCancel changes nothing. For a safe comparison use Diagnosis → Adjust.");
            depthCancel.Set(T("取消", "Cancel"), true); depthConfirm.Set(T("确认修改", "Confirm change"), DepthConfirmationReady(), XgPalette.Bad, Color.white);
        }

        void DisableDepthBackground()
        {
            if (depthOwner == null) return;
            foreach (var selectable in root.GetComponentsInChildren<Selectable>(true))
            {
                if (selectable.transform.IsChildOf(depthConfirmation)) continue;
                if (!depthBackground.ContainsKey(selectable)) depthBackground.Add(selectable, selectable.interactable);
                selectable.interactable = false;
            }
        }

        void PollDepthConfirmation()
        {
            if (depthOwner == null) return;
            if (!view.Visible || !root.gameObject.activeInHierarchy || depthOwner != Sim || depthTrack != Track
                || Sim.Run(Track).dataset != depthDataset || Sim.Run(Track).depth != previousDepth
                || Sim.Run(Track).running || Sim.Run(Track).epochActive
                || Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            { CancelDepth(); if (view.Visible && root.gameObject.activeInHierarchy) Refresh(); return; }
            if (!DepthActivationHeld())
            {
                if (!depthInputReleased) { depthInputReleased = true; depthReleasedFrame = Time.frameCount; }
            }
            else if (!DepthConfirmationReady()) depthInputReleased = false;
            depthConfirm.button.interactable = DepthConfirmationReady();
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected == null || !selected.transform.IsChildOf(depthConfirmation)) depthCancel.button.Select();
        }

        static bool DepthActivationHeld()
        {
            var keyboard = Keyboard.current;
            return keyboard != null && (keyboard.enterKey.isPressed || keyboard.numpadEnterKey.isPressed || keyboard.spaceKey.isPressed)
                || Mouse.current != null && Mouse.current.leftButton.isPressed;
        }

        bool DepthConfirmationReady() => depthInputReleased && Time.frameCount > depthReleasedFrame && Time.unscaledTime >= depthReadyAt;

        void CancelDepth()
        {
            depthOwner = null; depthInputReleased = false;
            foreach (var entry in depthBackground) if (entry.Key != null) entry.Key.interactable = entry.Value;
            depthBackground.Clear();
            if (depthConfirmation != null)
            {
                var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
                if (selected != null && selected.transform.IsChildOf(depthConfirmation)) EventSystem.current.SetSelectedGameObject(null);
                depthConfirmation.gameObject.SetActive(false);
            }
        }

        void ConfirmDepth()
        {
            if (!DepthConfirmationReady()) return;
            if (!view.Visible || !root.gameObject.activeInHierarchy) { CancelDepth(); return; }
            var owner = depthOwner; var track = depthTrack; int depth = requestedDepth;
            bool valid = owner == Sim && Track == track && Sim.Run(track).dataset == depthDataset
                && Sim.Run(track).depth == previousDepth && !Sim.Run(track).epochActive && !Sim.Run(track).running;
            CancelDepth();
            if (valid && Sim.SetDepth(track, depth, Host)) Knob(depthUp); else Deny(depthUp);
            Refresh();
        }

        void Press()
        {
            if (depthOwner != null) return;
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
            Fx.Float(Fx.At(cleanNoise.rt, new Vector2(-40, 30)), T("清洗噪声 ", "Cleaned noise ") + removed + " · −¥" + Money(removed * XgSim.NoiseCleanMoneyPerItem), XgPalette.Good, 18, 35, .8f);
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
            Fx.Shockwave(Fx.At(epochBar, new Vector2(epochBar.rect.width * .5f, 0)), XgPalette.Accent, e.hand ? 60 : 28, .3f, e.hand ? 5 : 2);
            if (e.hand) Fx.Float(Fx.At(epochBar, new Vector2(0, 20)), T("第 " + e.epoch + " 轮 · 模拟 loss ", "Epoch " + e.epoch + " · simulated loss ") + N(-Math.Log(Math.Max(.0001, e.trainAcc)), "0.00"), XgPalette.Accent, 16, 30, .6f, 1.1f);
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
                numbers.color = XgPalette.Bad;
                chartArea.anchoredPosition = chartRestPosition + (Fx.Reduced || !Sim.LastEpochWasHand ? Vector2.zero : new Vector2(UnityEngine.Random.Range(-6f, 6f), 0));
                if (glitch <= 0) { numbers.color = XgPalette.Ink; chartArea.anchoredPosition = chartRestPosition; Refresh(); }
            }
            trainGlow.on = Sim.TrainingUnlocked(Track) && !run.epochActive && Sim.S.combo >= 10;
            trainGlow.color = XgPalette.Tiers[Mathf.Min(4, XgSim.TierOf(Sim.S.combo) + 1)] * new Color(1, 1, 1, .7f);
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
                if (a.record && a.hand == false && a.newGrade >= 0) view.ShowToast(T("自动评估：", "Auto assessment: ") + XgCatalog.Dataset(a.dataset).name + " " + XgCatalog.GradeNames[a.grade] + " " + N(a.score, "0"), 2.5f);
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
            cardTitle.text = T("评估 · ", "Assessment · ") + T(d.name, d.nameEn) + (a.hand ? "" : T("（自动）", " (auto)"));
            cardBest.text = T("历史最高 ", "Best ") + N(a.previousBest, "0") + " · " + XgSim.Pct(a.acc);
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
            var color = XgPalette.Grades[a.grade];
            stampText.text = XgCatalog.GradeNames[a.grade];
            stampText.color = color; stampRing.color = color;
            double pay = a.reward + a.gradeBonus;
            cardReward.text = a.record ? T("新纪录 +¥", "Record +¥") + Money(pay) + (a.gradeBonus > 0 ? T("（含评级奖 ¥", " (grade bonus ¥") + Money(a.gradeBonus) + T("）", ")") : "")
                : T("没有进步 +¥0", "No progress +¥0");
            cardReward.color = a.record ? XgPalette.Gold : new Color(1, 1, 1, .5f);
            ribbon.text = a.newGrade >= 0 ? T("首次 ", "First ") + XgCatalog.GradeNames[a.newGrade] + "！" : a.record ? T("新纪录", "RECORD") : "";
            Vector2 at = Fx.At(stamp);
            if (!a.hand)
            {
                Fx.Play(a.record ? XgJuice.Sfx.Id.Coin : XgJuice.Sfx.Id.Tick, 1, .5f);
                if (a.record) Fx.Float(Fx.At(card, new Vector2(0, 50)), "+¥" + Money(pay), XgPalette.Money, 18);
                return;
            }
            if (!a.record)
            {
                cardBg.color = new Color(.3f, .32f, .38f, .94f);
                Fx.Play(XgJuice.Sfx.Id.Thud);
                Fx.Float(Fx.At(card, new Vector2(0, 70)), T("没有进步 +¥0", "No progress +¥0"), new Color(.8f, .82f, .9f), 20);
                return;
            }
            Fx.HitStop(a.grade >= 4 ? 200 : 120);
            Fx.Shake(a.grade >= 4 ? 12 : 6, a.grade >= 4 ? .45f : .3f);
            Fx.Shockwave(at, color, 260, .4f, 12);
            Fx.Play(XgJuice.Sfx.Id.Stamp);
            Fx.Flash(Color.white, .1f, .6f);
            Fx.Burst(at, 40, XgPalette.Gold, XgJuice.Shape.Yen, 360);
            Fx.Burst(at, 30, Color.white, XgJuice.Shape.Confetti, 420);
            Fx.Float(Fx.At(card, new Vector2(0, 110)), T("新纪录 +¥", "Record +¥") + Money(pay), XgPalette.Gold, 32, 70, 1.1f, 1.5f);
            Fx.Play(XgJuice.Sfx.Id.Fanfare, a.grade >= 4 ? 1.12f : 1, .8f);
            if (a.grade >= 4)
            {
                Fx.Shockwave(at, XgPalette.Gold, 420, .6f, 16);
                Fx.Burst(Vector2.zero, 60, XgPalette.Gold, XgJuice.Shape.Star, 520, 300, 1.2f);
            }
        }

        // ───────────── refresh ─────────────

        public override void Refresh()
        {
            if (depthOwner != null)
            {
                if (depthOwner != Sim || depthTrack != Track || Sim.Run(Track).dataset != depthDataset || Sim.Run(Track).epochActive || Sim.Run(Track).running) CancelDepth();
                else RefreshDepthWarning();
            }
            if (root == null) return;
            var track = Track;
            if (!Sim.TrainingUnlocked(track) && Sim.TrainingUnlocked(track == XgTrack.Vision ? XgTrack.Sequence : XgTrack.Vision))
                Sim.SelectedTrack = track = track == XgTrack.Vision ? XgTrack.Sequence : XgTrack.Vision;
            var run = Sim.Run(track);
            var d = XgCatalog.Dataset(run.dataset);
            var a = XgCatalog.Arch(run.arch);
            string[] names = { T("视觉", "Vision"), T("文字 / 序列", "Text / sequence") };
            for (int i = 0; i < 2; i++)
            {
                bool on = (int)track == i, open = Sim.TrainingUnlocked((XgTrack)i);
                trackTabs[i].Show(open);
                trackTabs[i].Set(names[i] + (Sim.Runs[i].running ? " ●" : ""), open, on ? XgPalette.Accent : XgPalette.Button, on ? Color.white : XgPalette.Ink);
            }
            summary.Set(T(d.name, d.nameEn) + " · " + T(a.name, a.nameEn) + " · " + run.depth + T(" 层 · 宽 ", " layers · width ") + XgCatalog.Widths[run.width] + T(" · 学习率 ", " · rate ") + Sim.RateLabel(run.lr) + "  <color=#3B5BDB>" + T("科技 →", "tree →") + "</color>", true, Color.clear);

            double best = Sim.BestAcc(d.id), bestScore = Sim.BestScore(d.id);
            int grade = XgSim.Grade(bestScore);
            if (glitch <= 0)
                numbers.text = T("训练 ", "Train ") + Hex(XgChartGraphic.TrainColor) + XgSim.Pct(run.trainAcc) + "</color>   "
                    + T("验证 ", "Val ") + Hex(XgChartGraphic.ValColor) + "<b>" + XgSim.Pct(run.valAcc) + "</b></color>   "
                    + T("最佳 ", "Best ") + Hex(XgPalette.Grades[XgSim.Grade(bestScore)]) + "<b>" + (best > 0 ? XgCatalog.GradeNames[XgSim.Grade(bestScore)] + " " + N(bestScore, "0") : "—") + "</b></color>   "
                    + T("下一评级 ", "Next grade ") + (grade >= XgCatalog.GradeNames.Length - 1 ? T("已满级", "maxed") : XgCatalog.GradeNames[grade + 1] + " " + XgCatalog.GradeScore[grade + 1]);
            chart.Capacity = Mathf.Clamp(run.histVal.Count, 24, XgSim.HistoryLength);
            chart.SetData(run.histTrain, noValidationCurve, 0, true);
            yTop.text = N(chart.Max, "0.00");
            yMid.text = N((chart.Max + chart.Min) / 2, "0.00");
            yLow.text = N(chart.Min, "0.00");
            legend.text = Hex(XgChartGraphic.TrainColor) + "━ " + T("模拟损失 −log(训练准确率)", "Simulated loss −log(train accuracy)") + "</color>";
            RefreshDiagnostics(run);

            // Every epoch is assessed: no countdown pips, no separate assess button.
            for (int i = 0; i < pips.Length; i++) pips[i].gameObject.SetActive(false);

            bool unlocked = Sim.TrainingUnlocked(track);
            string blocker = Sim.Blocker(run, Host);
            int level = Sim.AutoTrainLevel;
            train.Set(T("训练一轮", "Train 1 epoch"), unlocked && blocker == null && !run.epochActive, XgPalette.Accent, Color.white);
            trainSub.text = unlocked ? T("第 " + run.epoch + " 轮", "epoch " + run.epoch) + (Sim.S.combo > 0 ? T(" · 连击 ×", " · combo ×") + N(Sim.ComboMultiplier, "0.00") : "") : T("先标够样本", "label first");
            string[] modes = { "☛ " + T("手动", "hand"), "", "◷ crontab", "▣ " + T("守护进程", "daemon"), "✓ " + T("自动评估", "auto-eval"), "⇒ AutoML" };
            mode.text = modes[Mathf.Clamp(level, 0, 5)];
            assess.Show(false);
            autoTrain.Show(level >= 2);
            if (level >= 2) autoTrain.Set(T("自动训练\n", "Auto-train\n") + (run.running ? T("开", "on") : T("关", "off")), true, run.running ? new Color32(226, 246, 230, 255) : (Color?)null, run.running ? XgPalette.Good : (Color?)null);

            double hazard = Sim.UseBoard ? 0 : Sim.Hazard(run);
            var boardKnobs = Sim.Knobs(run);
            if (Sim.UseBoard)
                risk.text = boardKnobs.lr * 1.5 > boardKnobs.TearAt
                    ? T("⚠ 学习率 " + Sim.RateLabel(run.lr) + " 太大：模型的小船说翻就翻（NaN）。调小，或开梯度裁剪", "⚠ Rate " + Sim.RateLabel(run.lr) + " is too high: NaN incoming. Lower it or clip gradients")
                    : boardKnobs.depth > 4 && boardKnobs.G > 0 && boardKnobs.G < .5 ? T("⚠ S 形激活每往下一层只剩四分之一：层数太多，底层学不动", "⚠ An S-curve passes a quarter per layer: too many layers and the bottom stops learning")
                    : boardKnobs.depth > 1 && boardKnobs.G <= 0 ? T("⚠ 阶跃激活没有坡度：误差传不到下面的层", "⚠ A step has no slope: the error never reaches lower layers") : "";
            else risk.text = hazard > 0
                ? T("⚠ 发散风险：每 100 轮约 ", "⚠ Divergence risk: about ") + N(Math.Min(100, (1 - Math.Pow(1 - hazard * XgSim.EpochSeconds, 100)) * 100), "0") + T("%。调小学习率，或研究更稳的优化器", "% per 100 epochs. Lower the rate or research a steadier optimizer")
                : (run.depth > Sim.MaxDepth(run) ? T("⚠ 超过 " + Sim.MaxDepth(run) + " 层：梯度消失，多出来的层在拖后腿", "⚠ Past " + Sim.MaxDepth(run) + " layers gradients vanish; extra layers hurt") : "");
            hint.text = blocker != null ? "⚠ " + blocker : Hint(run, best);
            hint.color = blocker != null ? XgPalette.Bad : XgPalette.Muted;

            RefreshModel(track, run, a);
            DisableDepthBackground();
        }

        string Hint(XgRun run, double best)
        {
            if (!Sim.TrainingUnlocked(Track)) return T("先去标注台标够 " + XgCatalog.SamplesToTrain + " 条样本。", "Label " + XgCatalog.SamplesToTrain + " samples first.");
            if (run.epoch == 0) return T("按「训练一轮」。每轮练完都会考一次：刷新纪录才给钱。", "Press Train. Every epoch ends with an exam; a new record pays.");
            if (run.staleEvals >= XgSim.StaleHintEpochs) return Sim.StaleHint(run);
            if (run.valAcc + .002 < run.trainAcc - .01 && run.valAcc < best - .002)
                return T("验证集在掉、训练集还在涨：过拟合了。最佳检查点已经存好；加数据或买 Dropout。", "Validation falls while training rises: overfitting. The best checkpoint is safe; add data or Dropout.");
            return T("连击越高，每轮学得越多（最多 ×2）。学习率越大越快，但太大会 NaN。", "A higher combo trains more per epoch (up to ×2). Higher rates are faster but may NaN.");
        }

        void RefreshModel(XgTrack track, XgRun run, XgArch a)
        {
            modelTitle.text = T("正式模型 · 优化器 ", "Live model · optimizer ") + Sim.OptimizerName;
            saveModel.Show(run.epoch > 0);
            saveModel.Set(T("存入仓库", "Save model"), run.epoch > 0 && !run.epochActive, XgPalette.AccentSoft, XgPalette.Accent);
            // Architectures owned on this track.
            var archs = new List<XgArch>();
            foreach (var x in XgCatalog.Archs) if (XgSim.ArchitectureFits(x, track) && Sim.Has(x.id)) archs.Add(x);
            string key = string.Join(",", archs.ConvertAll(x => x.id)) + track;
            if (key != shownArchKey)
            {
                shownArchKey = key;
                foreach (var b in archBtns) UnityEngine.Object.Destroy(b.rt.gameObject);
                archBtns.Clear(); archIds.Clear();
                for (int i = 0; i < archs.Count; i++)
                {
                    string id = archs[i].id;
                    var b = ui.Button(archBox, "", () => { if (Sim.SetArch(Track, id)) { Fx.Play(XgJuice.Sfx.Id.Swoosh); Fx.Knock(archBtns[archIds.IndexOf(id)].rt, .12f); } Refresh(); }, 14);
                    b.rt.anchorMin = new Vector2((i % 2) * .5f, 1); b.rt.anchorMax = new Vector2((i % 2 + 1) * .5f, 1);
                    b.rt.offsetMin = new Vector2(2, -(i / 2) * 36 - 32); b.rt.offsetMax = new Vector2(-2, -(i / 2) * 36);
                    archBtns.Add(b); archIds.Add(id);
                    UiTip.Add(b.rt, () => { var n = XgCatalog.Node(id); var a = XgCatalog.Arch(id); return (a != null ? "<b>" + T(a.name, a.nameEn) + "</b>  " + a.year + "\n" : "") + (a != null && !string.IsNullOrEmpty(a.note) ? T(a.note, a.noteEn) : n != null ? T(n.note, n.noteEn) : ""); });
                }
                archBox.sizeDelta = new Vector2(0, Mathf.CeilToInt(archs.Count / 2f) * 36);
            }
            for (int i = 0; i < archBtns.Count; i++)
            {
                var x = XgCatalog.Arch(archIds[i]);
                bool on = x.id == run.arch;
                archBtns[i].Set(T(x.name, x.nameEn) + " <size=11>" + x.year + "</size>", !run.epochActive, on ? XgPalette.Accent : XgPalette.Button, on ? Color.white : XgPalette.Ink);
            }

            int cap = Sim.DepthCap(track), max = Sim.MaxDepth(run);
            depthText.text = T("层数 ", "Layers ") + "<b>" + run.depth + "</b> / " + T("可用 ", "cap ") + cap + (max >= 999 ? T("（残差，无上限）", " (residual)") : T(" · 架构上限 ", " · arch limit ") + max);
            depthText.color = run.depth > max ? XgPalette.Bad : XgPalette.Ink;
            bool shapeUnlocked = Sim.StageFor(track) >= 2;
            depthText.gameObject.SetActive(shapeUnlocked); depthDown.Show(shapeUnlocked); depthUp.Show(shapeUnlocked);
            depthDown.Set("−", run.depth > 1 && !run.epochActive && !run.running);
            depthUp.Set("+", run.depth < cap && !run.epochActive && !run.running);
            int wcap = Sim.WidthCap(track);
            widthText.text = T("宽度 ", "Width ") + "<b>" + XgCatalog.Widths[run.width] + "</b> / " + T("可用 ", "cap ") + XgCatalog.Widths[wcap];
            widthText.gameObject.SetActive(shapeUnlocked); widthDown.Show(shapeUnlocked); widthUp.Show(shapeUnlocked);
            widthDown.Set("−", run.width > 0 && !run.epochActive);
            widthUp.Set("+", run.width < wcap && !run.epochActive);
            bool knob = Sim.HasLrKnob(track), scheduled = run.autoLr && (Sim.Has("lrschedule") || Sim.AutoTrainLevel >= 5);
            lrText.text = T("学习率", "Learning rate") + (knob ? (scheduled ? T("（自动调度中）", " (scheduled)") : "") : T("：" + Sim.RateLabel(XgSim.DefaultLr(track)) + "（科技买「学习率旋钮」才能调）", ": " + Sim.RateLabel(XgSim.DefaultLr(track)) + " (buy the knob in the tree)"));
            RefreshKnobs(run);
            for (int i = 0; i < lrs.Length; i++)
            {
                lrs[i].Show(knob);
                bool on = i == run.lr;
                var probe = new XgRun { track = run.track, arch = run.arch, dataset = run.dataset, depth = run.depth, width = run.width, lr = i };
                bool risky = Sim.UseBoard ? XgSim.RateValues[i] * 1.5 > Sim.Knobs(run).TearAt : Sim.Hazard(probe) > 0;
                lrs[i].Set(Sim.RateLabel(i), !run.epochActive, on ? (risky ? XgPalette.Bad : XgPalette.Accent) : XgPalette.Button, on ? Color.white : risky ? XgPalette.Bad : XgPalette.Ink);
            }
            double share = Host.Compute;
            var knobs = Sim.Knobs(run);
            statsText.text = T("参数量 ", "Parameters ") + "<b>" + Params(XgSim.ParamsK(run)) + "</b> <color=#8A94A8>(" + XgSim.ParamScale(XgSim.ParamsK(run), Sim.English) + ")</color>"
                + T(" · 显存 ", " · VRAM ") + N(XgSim.VramNeedMB(run), "0") + "/" + N(Sim.Vram(Host), "0") + " MB"
                + (Sim.UseBoard
                    ? T(" · 每轮喂 ", " · each epoch feeds ") + Sim.CardsPerEpoch(run, Math.Max(.5, share), true) + T(" 张 / 数据池 ", " of ") + Sim.PoolSize(run) + T(" 张（已过 ", " cards (") + N(Sim.PassesOverData(run), "0.0") + T(" 遍）", " passes so far)")
                      + "\n" + T("格子 ", "Cells ") + Sim.Board.Count(XgSim.RegionOf(run.dataset)) + "/" + knobs.Cells + T(" · 层间系数 g=", " · layer factor g=") + N(knobs.G, "0.00")
                    : T(" · 每轮 ", " · per epoch ") + N(Sim.StepsPerSecond(run, Math.Max(.5, share)) * XgSim.EpochSeconds, "0") + T(" 步", " steps")
                      + "\n" + T("理论上限 ", "Ceiling ") + XgSim.Pct(Sim.PeakAccuracy(run, run.lr)))
                + (run.lastScore >= 0 ? T(" · 最近评估 ", " · last assessment ") + XgCatalog.GradeNames[XgSim.Grade(run.lastScore)] + " " + N(run.lastScore, "0") : "")
                + "\n" + (Sim.UseBoard ? WallLine(run) : T(a.note, a.noteEn));

            // Own mistakes plus outside noise from packs and crowd tasks, and the user-log button (XgTrainPage.Data.cs).
            double noise = Sim.Noise(run.dataset) + Sim.DataNoise(run.dataset);
            RefreshDataEconomy(run);
            bool audit = Sim.Has("label.audit");
            cleanNoise.Show(Sim.Has("label.audit"));
            int cleanable = audit ? Sim.CleanableNoise(run.dataset, Host, XgSim.NoiseCleanBatchLimit) : 0;
            string cleaningState = cleanable > 0 ? T("本次 ", "This batch ") + cleanable + " · ¥" + Money(cleanable * XgSim.NoiseCleanMoneyPerItem)
                : noise < 1 ? T("无噪声", "No noise") : Sim.ProjectActive ? T("研发占用 GPU", "GPU reserved for research")
                : Host.Blocker != null || Host.Compute <= 0 ? T("无可用算力", "No available compute") : T("经费不足或暂不可清洗", "Unavailable or insufficient funds");
            cleanNoise.Set(T("清洗最多 ", "Clean up to ") + XgSim.NoiseCleanBatchLimit + T(" 条", " labels") + "\n<size=10>" + cleaningState + "</size>", cleanable > 0, cleanable > 0 ? XgPalette.AccentSoft : XgPalette.Button, cleanable > 0 ? XgPalette.Accent : XgPalette.Muted);
            noiseHint.text = audit
                ? "¥" + Money(XgSim.NoiseCleanMoneyPerItem) + " + " + N(XgSim.NoiseCleanGpuSecondsPerItem, "0.0") + T(" GPU秒 / 条", " GPU s / label") + "\n" + T("模拟数据清洗；不训练 GGUF", "Simulated data; not GGUF training")
                : T("模拟数据质量统计（非 GGUF 训练）", "Simulated data quality (not GGUF training)");
            noiseHint.rectTransform.offsetMax = new Vector2(audit ? -180 : -14, noiseHint.rectTransform.offsetMax.y);

            dataTitle.text = T("数据包 · 拖到 GPU 安装", "Data packs · drag to GPU");
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
                    UiTip.Add(b.rt, () => { var d = XgCatalog.Dataset(id); return d == null ? "" : "<b>" + T(d.name, d.nameEn) + "</b>\n" + (string.IsNullOrEmpty(d.note) ? "" : T(d.note, d.noteEn) + "\n") + T("指标：", "Metric: ") + T(d.metric, d.metricEn) + T("\n样本：", "\nSamples: ") + Sim.Samples(id).ToString("0") + T("（不够就去标注台标，或买数据包）", " (label more, or buy the data pack)") + "\n" + XgDataUi.SourcesSummary(Sim, id); });
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
                string grade = Sim.BestAcc(x.id) > 0 ? "  " + Hex(on ? Color.white : XgPalette.Grades[XgSim.Grade(sc)]) + XgCatalog.GradeNames[XgSim.Grade(sc)] + " " + N(sc, "0") + "</color>" : "";
                bool enough = Sim.Samples(x.id) >= XgCatalog.SamplesToTrain;
                dataBtns[i].Set(T(x.name, x.nameEn) + grade, Sim.DatasetAvailable(x.id) && (enough || on) && !run.epochActive, on ? XgPalette.Accent : XgPalette.Button, on ? Color.white : XgPalette.Ink);
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
                    packBtns[i].Set(N(download.downloadProgress * 100, "0") + T("% · 加速 ¥", "% · speed ¥") + Money(speed), rich, rich ? XgPalette.AccentSoft : XgPalette.Button, rich ? XgPalette.Accent : XgPalette.Muted);
                }
                else packBtns[i].Set(owned ? T("已安装", "Installed") : node != null ? "↓ ¥" + Money(Sim.NodeCost(node)) : "", !owned && canBuy, canBuy ? XgPalette.AccentSoft : XgPalette.Button, canBuy ? XgPalette.Accent : XgPalette.Muted);
            }
            // Pack switches of the current dataset below the list; also sizes the scroll content (XgTrainPage.Sources.cs).
            RefreshSources(run, dataBtns.Count);
            double watts = Host is XingGuangHost home ? home.TrainingWatts : 0;
            gpuLabel.text = "▣ GPU  · " + T("松手安装数据包", "drop data pack to install") + "\n" +
                T("训练负载 ", "Training load ") + N(watts, "0") + " W  · " + T("累计 GPU 时间 ", "GPU time ") + N(Sim.S.trainedSeconds, "0.0") + " s";
        }

        void RefreshDiagnostics(XgRun run)
        {
            if (RefreshMistakes(run)) return;
            int stage = Sim.StageFor(Track);
            string[] zh = { "", "权重 · 28×28 像素", "MLP · 隐藏层组合", Track == XgTrack.Vision ? "局部结构 · 深度与表现" : "序列 · 记忆衰减", Track == XgTrack.Vision ? "残差 · 信息保留" : "门控 · 长期记忆", "注意力 · 寻找线索", "多头注意力 · 并行" };
            string[] en = { "", "Weights · 28×28 pixels", "MLP · hidden features", Track == XgTrack.Vision ? "Local structure · depth" : "Sequence · memory decay", Track == XgTrack.Vision ? "Residual · preserve information" : "Gates · longer memory", "Attention · find the clue", "Multi-head attention · parallel" };
            diagnosticTitle.text = stage + " · " + T(zh[stage], en[stage]);
            diagnostic.Show(Sim, run, stage);
            diagnosticNote.text = stage >= 5
                ? T("教学示意；模拟 GPU ", "Teaching diagram; simulated GPU ") + XgSim.Pct(Sim.GpuUtilization(run))
                : T("教学示意，不是本地模型的内部权重", "Teaching diagram, not local-model weights");
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
                LingGuangV05.Desktop.Story.PrologueDirector.Desk?.Popup(T("摆渡云", "Bodu Cloud"),
                    T("正在下载《" + XgCatalog.Dataset(dataset).name + "》……非会员限速 100KB/s，预计 " + N(Sim.DownloadLeft(dataset), "0") + " 秒。开通超级会员立享极速下载！",
                      "Downloading " + XgCatalog.Dataset(dataset).nameEn + "… free users are limited to 100KB/s, about " + N(Sim.DownloadLeft(dataset), "0") + " s. Go super member for full speed!"), 8);
            Fx.Knock(gpuTarget, .12f, new Vector2(0, -8));
            Fx.Shockwave(Fx.At(gpuTarget), XgPalette.Accent, 150, .35f, 7);
            Fx.Burst(Fx.At(gpuTarget), 20, XgPalette.Gold, XgJuice.Shape.Spark, 220);
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

    public sealed class XgDepthConfirmationLifetime : MonoBehaviour
    {
        public Action Disabled, Poll;
        void Update() { Poll?.Invoke(); }
        void OnDisable() { Disabled?.Invoke(); }
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
            Panel(ghost, XgPalette.Accent).raycastTarget = false;
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
