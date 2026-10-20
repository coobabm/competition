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

        // Layout of the page at the normal window size (about 1064 × 617 here): the left column is 642 wide, the right 410.
        const float RightWidth = 410, ColumnGap = 12, TabsHeight = 34, TrainRowHeight = 112 + RateStripHeight + RateStripGap, RowGap = 10;

        XgBtn[] trackTabs = new XgBtn[2];
        XgBtn summary, train, assess, saveModel, cleanNoise, orders;
        readonly List<XgBtn> dataBtns = new List<XgBtn>();
        readonly List<XgBtn> packBtns = new List<XgBtn>();
        readonly List<string> dataIds = new List<string>();
        RectTransform left, right, curveCard, trainRow, chartArea, epochBar, dataBox, dataViewport, gpuTarget, diagnosticArea;
        XgTwoCurveGraphic chart;
        XgStageGraphic diagnostic;
        readonly List<XgStageGraphic> previousDiagnostics = new List<XgStageGraphic>();
        TMP_Text diagnosticTitle, diagnosticNote, gpuLabel, noiseText, noiseHint;
        XgGlow gpuGlow;
        TMP_Text numbers, yTop, yMid, yLow, legend, risk, hint, trainSub, trainBill, dataTitle;
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
            var fileDrop = root.gameObject.AddComponent<XgFilesTrainDrop>();
            fileDrop.CurrentSim = () => Sim;
            fileDrop.TargetVisible = () => view.Visible && view.Tab == "train";
            fileDrop.Notify = message => view.ShowToast(message, 3.5f);
            fileDrop.RefreshView = () => view.Refresh(true);
            var fileDropSurface = root.gameObject.AddComponent<Image>();
            fileDropSurface.color = Color.clear; fileDropSurface.raycastTarget = true;
            // Keep the curve and training controls together; the source roll is always on their left.
            left = Rect("TrainingMain", root, Vector2.zero, Vector2.one, new Vector2(RightWidth + ColumnGap, 0), Vector2.zero);
            right = Rect("SourceSidebar", root, Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(RightWidth, 0));

            // Top: the two tracks, the model in one line (structure · parameters · VRAM), and the save button.
            for (int i = 0; i < 2; i++)
            {
                var t = (XgTrack)i;
                trackTabs[i] = ui.Button(left, "", () => { Sim.SelectedTrack = t; Fx.Play(XgJuice.Sfx.Id.Click); Refresh(); }, 15);
                PlaceTopLeft(trackTabs[i], i * 112, 0, 106, TabsHeight);
            }
            summary = ui.Button(left, "", () => view.ShowTab("items"), 14);
            summary.rt.anchorMin = new Vector2(0, 1); summary.rt.anchorMax = new Vector2(1, 1);
            summary.rt.offsetMin = new Vector2(230, -TabsHeight); summary.rt.offsetMax = new Vector2(-108, 0);
            summary.label.alignment = TextAlignmentOptions.MidlineLeft;
            summary.label.enableAutoSizing = true; summary.label.fontSizeMin = 10; summary.label.fontSizeMax = 14;
            saveModel = ui.Button(left, "", () =>
            {
                var checkpoint = BestRollback();
                if (RollbackBusy || checkpoint == null || !Sim.LoadModel(checkpoint.id)) { Fx.Play(XgJuice.Sfx.Id.Thud); view.Refresh(true); return; }
                view.ShowToast(T("已回到最佳可用检查点：", "Restored best available checkpoint: ") + checkpoint.name, 4);
                Fx.Knock(saveModel.rt, .15f);
                Fx.Burst(Fx.At(saveModel.rt), 10, XgDark.Accent, XgJuice.Shape.Spark, 180);
                view.Refresh(true);
            }, 14);
            saveModel.rt.gameObject.name = "RollbackModel";
            UiTip.Add(saveModel.rt, () => RollbackTip());
            saveModel.rt.anchorMin = saveModel.rt.anchorMax = new Vector2(1, 1);
            saveModel.rt.offsetMin = new Vector2(-100, -TabsHeight); saveModel.rt.offsetMax = Vector2.zero;

            // Bottom up: the train row, then the curve card takes what is left.
            trainRow = Rect("TrainRow", left, Vector2.zero, new Vector2(1, 0), Vector2.zero, Vector2.zero);
            curveCard = ui.Card(left, "Curve", Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -TabsHeight - RowGap));
            LayoutLeft();

            numbers = ui.Text(Strip("Numbers", curveCard, 6, 24, 12, 12), "", 15, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            numbers.enableAutoSizing = true; numbers.fontSizeMin = 10; numbers.fontSizeMax = 15; numbers.textWrappingMode = TextWrappingModes.NoWrap;
            // The sheet (答卷) takes the top of the card; the curve and the stage diagram share the bottom.
            chartArea = Rect("ChartArea", curveCard, Vector2.zero, new Vector2(1, SheetSplit), new Vector2(58, 30), new Vector2(-12, -6));
            Panel(chartArea, new Color32(6, 11, 16, 255));
            chartRestPosition = chartArea.anchoredPosition;
            chart = Rect("Chart", chartArea, new Vector2(0, .37f), Vector2.one, new Vector2(4, 2), new Vector2(-4, -4)).gameObject.AddComponent<XgTwoCurveGraphic>();
            chart.raycastTarget = false;
            chart.MarkDrops = true;
            chart.Capacity = XgSim.HistoryLength;
            BuildDiagnostics();
            BuildSheet();
            yTop = ui.Text(Rect("YTop", chart.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(-54, -11), new Vector2(-2, 11)), "", 12, XgDark.Muted, TextAlignmentOptions.MidlineRight);
            yMid = ui.Text(Rect("YMid", chart.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(-54, -11), new Vector2(-2, 11)), "", 12, XgDark.Muted, TextAlignmentOptions.MidlineRight);
            yLow = ui.Text(Rect("YLow", chart.rectTransform, Vector2.zero, Vector2.zero, new Vector2(-54, -11), new Vector2(-2, 11)), "", 12, XgDark.Muted, TextAlignmentOptions.MidlineRight);
            legend = ui.Text(Rect("Legend", chart.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(8, -20), new Vector2(-8, -2)), "", 11, XgDark.Dim, TextAlignmentOptions.TopLeft);
            legend.textWrappingMode = TextWrappingModes.NoWrap;
            hint = ui.Text(Rect("Hint", curveCard, Vector2.zero, new Vector2(1, 0), new Vector2(12, 4), new Vector2(-150, 28)), "", 13, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            hint.enableAutoSizing = true; hint.fontSizeMin = 10; hint.fontSizeMax = 13;
            BuildCurveExtras();

            // The train row: the big button (with this round's electricity), the round's progress, risk and active effects, auto-train.
            // The 学习率 strip runs along the top of the row (XgTrainPage.Rate.cs); the button and the round sit under it.
            var trainRect = Rect("TrainAt", trainRow, Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(240, -RateStripHeight - RateStripGap));
            trainGlow = Glow(trainRect, XgDark.Accent, 6);
            train = ui.Button(trainRow, "", null, 28);
            Place(train, trainRect);
            train.rt.gameObject.name = "TrainButton";
            train.label.fontStyle = FontStyles.Bold;
            train.label.alignment = TextAlignmentOptions.TopLeft;
            train.label.rectTransform.offsetMin = new Vector2(8, 34); train.label.rectTransform.offsetMax = new Vector2(-104, -8);
            train.label.enableAutoSizing = true; train.label.fontSizeMin = 15; train.label.fontSizeMax = 28;
            trainSub = ui.Text(Rect("Sub", train.Visual, Vector2.zero, new Vector2(1, 0), new Vector2(14, 8), new Vector2(-14, 30)), "", 13, Color.white, TextAlignmentOptions.BottomLeft);
            trainSub.textWrappingMode = TextWrappingModes.NoWrap;
            trainBill = ui.Text(Rect("Bill", train.Visual, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-104, -44), new Vector2(-10, -8)), "", 14, XgDark.Money, TextAlignmentOptions.TopRight);
            trainBill.textWrappingMode = TextWrappingModes.NoWrap;
            hold = train.rt.gameObject.AddComponent<XgHold>();
            hold.Down = () => Press();
            train.rt.gameObject.AddComponent<XgTrainingSubmit>().Submit = Press;
            assess = ui.Button(left, "", () => DoAssess(), 18);
            assess.Show(false);

            var mid = Rect("Mid", trainRow, Vector2.zero, Vector2.one, new Vector2(252, 0), new Vector2(-8, -RateStripHeight - RateStripGap));
            // The round's progress bar, the way a training console draws it (XgTrainPage.Pace.cs).
            BuildPace(mid);
            risk = ui.Text(Rect("Risk", mid, new Vector2(0, 1), Vector2.one, new Vector2(0, -80), new Vector2(0, -52)), "", 13, XgDark.Bad, TextAlignmentOptions.TopLeft);
            risk.enableAutoSizing = true; risk.fontSizeMin = 10; risk.fontSizeMax = 13;

            BuildRates();
            BuildSide();
            BuildCard();
            BuildDiagnosis();
            BuildExam();
            BuildTrainingSeals();
            BuildDataInstallHint();
            for (int i = 0; i < 2; i++)
                UiTip.Add(trackTabs[i].rt, () => T(("两条线练的是灵光同一颗脑子的两个区：看图区（像视觉皮层）和读字区（像语言区）。各练各的，共用显卡和经费。" .Replace("灵光", Sim.AiDisplayName)), ("The two tracks train two regions of 灵光's one brain: seeing (like the visual cortex) and reading (like the language areas). They train separately and share the GPU and funds." .Replace("灵光", Sim.AiDisplayName))));
            UiTip.Add(train.rt, () => T(("训练一轮：喂灵光一批卡，练完在没见过的题上考一次。\n这里的「一轮」是一批，不是把整个数据集过一遍：数据越多，要越多轮才过完一遍（模型的提示里写着已过几遍）。\n刷新纪录就记成绩、存检查点、发奖金。\n右上角的电费是这一轮要付的：按这个月的阶梯电价算，谷电时段内减半。" .Replace("灵光", Sim.AiDisplayName)),
                "Train one epoch: feed the model a batch of cards, then an exam on unseen cards.\nAn epoch here is a batch, not a pass over the whole dataset: the more data, the more epochs one pass takes (the model's note says how many passes so far).\nA new record is scored, saved and paid.\nThe price at the top right is what this round costs: this month's tiered rate, halved under off-peak power."));
            UiTip.Add(summary.rt, () => ModelTip());
            UiTip.Add(risk, () => RiskTip());
            UiTip.Add(numbers, () => NumbersTip());
            UiTip.Add(hint, () => T("训练为什么停下来，或者下一步做什么。", "Why training stopped, or what to do next."));
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
            // The teaching diagram of the stage sits under the curve, inside the same card (it used to take the upper half of the old page).
            diagnosticArea = Rect("StageDiagnostics", chartArea, Vector2.zero, new Vector2(1, .36f), new Vector2(5, 3), new Vector2(-5, 0));
            diagnosticTitle = ui.Text(Strip("Title", diagnosticArea, 0, 20, 2, 95), "", 13, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            diagnosticTitle.textWrappingMode = TextWrappingModes.NoWrap;
            diagnostic = Rect("Current", diagnosticArea, Vector2.zero, Vector2.one, new Vector2(3, 2), new Vector2(-95, -20)).gameObject.AddComponent<XgStageGraphic>();
            diagnostic.raycastTarget = false;
            for (int i = 0; i < 5; i++)
            {
                var r = Rect("PreviousStage" + (i + 1), diagnosticArea, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-88, 2 + i * 16), new Vector2(-3, -20));
                var g = r.gameObject.AddComponent<XgStageGraphic>(); g.raycastTarget = false; previousDiagnostics.Add(g);
            }
            diagnosticNote = ui.Text(Rect("Caveat", diagnosticArea, new Vector2(.5f, 1), Vector2.one, new Vector2(0, -20), new Vector2(-2, 0)), "", 11, XgDark.Muted, TextAlignmentOptions.MidlineRight);
            diagnosticNote.textWrappingMode = TextWrappingModes.NoWrap;
        }

        /// <summary>An architecture is a way of wiring one region of 灵光's brain; its history comes second.</summary>
        string ArchTip(string id)
        {
            var n = XgCatalog.Node(id); var a = XgCatalog.Arch(id);
            if (a == null) return n != null ? T(n.note, n.noteEn) : "";
            string region = a.shared ? Sim.RegionOfTrack(Track) : a.track == XgTrack.Vision ? "vision" : "sequence";
            return "<b>" + T(a.name, a.nameEn) + "</b>  " + XgCatalog.ShownYear(a.year) + "\n"
                + T(Sim.AiDisplayName + XgSim.RegionName(region, false) + "的连接拓扑：", "Topology of " + Sim.AiDisplayName + "'s " + XgSim.RegionName(region, true).ToLowerInvariant() + " region: ") + (a.topo.Length > 0 ? "<b>【" + a.topo + "】</b>" : "") + T(a.wire, a.wireEn)
                + (string.IsNullOrEmpty(a.note) ? "" : "\n<color=#6F95A5>" + Lang.T("历史上：") + T(a.note, a.noteEn) + "</color>");
        }

        /// <summary>The first change of structure: the protagonist works out what a "network" is to 灵光 (once per save).</summary>
        void VoiceWiring()
        {
            if (Sim.S.wiringVoiced) return;
            Sim.S.wiringVoiced = true;
            LingGuangV05.Desktop.Story.InnerVoice.Say("……机箱风扇一下子狂转起来。", "…The case fans just roared.", 2.4f);
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
            Fx.Knock(train.rt, .1f, new Vector2(0, -4));
            Fx.Burst(Fx.At(gpuTarget != null && gpuTarget.gameObject.activeInHierarchy ? gpuTarget : train.rt), 12, new Color32(255, 200, 90, 255), XgJuice.Shape.Spark, 300, 500, .45f);
            Fx.Play(XgJuice.Sfx.Id.Fan, 1, .8f);
            Refresh();
        }

        public void OnEpoch(XgEpoch e)
        {
            if (!view.Visible || view.Tab != "train" || (XgTrack)e.track != Track) return;
            QueueRoundPulse(e); // Only the completion cue may follow a live music accent; results remain immediate.
            var d = XgCatalog.Dataset(Sim.Run(Track).dataset);
            double points = d == null ? 0 : e.gain * 1000 / Math.Max(1e-9, d.chanceError - d.floorError);
            if (e.dropped)
            {
                // A drop: the round went backwards a little (训练不再需要调参数). Nothing learned is lost.
                Fx.Float(Fx.At(epochBar, new Vector2(0, 20)), T("退步 " + N(points, "0") + " 分 · " + e.dropReason, "Down " + N(-points, "0") + " points · " + e.dropReasonEn), XgDark.Bad, 16, 30, .8f, 1.4f);
                Fx.Knock(chartArea, .06f, new Vector2(6, 0));
                Fx.Play(XgJuice.Sfx.Id.Thud, .8f, .6f);
                if (e.overfit) OverfitNotice(points);
            }
            else if (e.rolledBack)
            {
                // 残差贴纸 (a relic): the drop that would have happened does not count.
                Fx.Float(Fx.At(epochBar, new Vector2(0, 20)), T("残差贴纸 · 这次退步不计入", "Residual sticker · the drop does not count"), XgDark.Link, 16, 30, .8f, 1.4f);
                Fx.Play(XgJuice.Sfx.Id.Unlock, 1, .5f);
            }
            chart.PulseLastSegment(1);
            Refresh();
            // The round's points are a corner badge now; what changed is the sheet (答卷).
            SheetRound(e, points);
        }

        public void Glitch() { glitch = Sim.LastEpochWasHand ? .3f : .12f; }

        public override void Tick(float dt)
        {
            TickRhythm();
            TickExam(dt);
            view.Data?.Tick(dt);
            var run = Sim.Run(Track);
            TickPace(dt);
            if (glitch > 0)
            {
                glitch -= dt;
                numbers.text = Garble();
                numbers.color = XgDark.Bad;
                chartArea.anchoredPosition = chartRestPosition + (Fx.Reduced || !Sim.LastEpochWasHand ? Vector2.zero : new Vector2(UnityEngine.Random.Range(-6f, 6f), 0));
                if (glitch <= 0) { numbers.color = XgDark.Ink; chartArea.anchoredPosition = chartRestPosition; Refresh(); }
            }
            trainGlow.on = false; // training has no combo
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
                return;
            }
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
            cardTitle.text = Lang.T("评估 · ") + T(d.name, d.nameEn);
            cardBest.text = Lang.T("历史最高 ") + N(a.previousBest, "0") + " · " + XgSim.Pct(a.acc);
            cardReward.text = "";
            ribbon.text = "";
            stampText.text = ""; stampRing.color = Color.clear;
            if (cardBig) Fx.Play(XgJuice.Sfx.Id.Whoosh, 1, .7f);
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
            orders.Set(Sim.FeatureVisible("contracts") ? T("企业订单 →", "Orders →") : (Sim.S.stage < 4 ? T("第 3 章开放订单", "Orders in chapter 3") : T("C 级解锁订单", "Grade C: orders")), Sim.FeatureVisible("contracts"), XgDark.Panel, XgDark.Muted);
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
                trackTabs[i].Set(names[i] + (Sim.Runs[i].epochActive ? " ●" : ""), open, on ? XgDark.Accent : XgDark.Button, on ? Color.white : XgDark.Ink);
            }
            // The first time the lab rewires a region by itself: the protagonist works out what a network is to it.
            string shapeKey = (int)track + ":" + run.arch;
            if (shownStructure.Length > 0 && shownStructure != shapeKey && shownStructure[0] == shapeKey[0]) VoiceWiring();
            shownStructure = shapeKey;
            RefreshModelLine(track, run, a);

            double best = Sim.BestAcc(d.id), bestScore = Sim.BestScore(d.id);
            int grade = XgSim.Grade(bestScore);
            if (glitch <= 0)
            {
                string ceiling = run.epoch > 0 ? T("上限 ", "Ceiling ") + Hex(XgDark.Muted) + N(XgSim.Score(run.dataset, run.ceiling), "0") + "</color>   " : "";
                numbers.text = "<b>" + T(d.name, d.nameEn) + "</b>   "
                    + T("新题 ", "New ") + Hex(XgTwoCurveGraphic.NewColor) + "<b>" + XgSim.Pct(run.valAcc) + "</b></color>   "
                    + T("练过的 ", "Practised ") + Hex(XgTwoCurveGraphic.PracticeColor) + XgSim.Pct(Sim.PracticeAccuracy(run)) + "</color>   "
                    + Lang.T("最佳 ") + Hex(XgDark.Grades[XgSim.Grade(bestScore)]) + "<b>" + (best > 0 ? XgCatalog.GradeNames[XgSim.Grade(bestScore)] + " " + N(bestScore, "0") : "—") + "</b></color>   "
                    + ceiling
                    + T("第 " + run.epoch + " 轮", "Epoch " + run.epoch);
                numbers.color = XgDark.Ink;
            }
            chart.Capacity = Mathf.Clamp(run.histVal.Count, 24, XgSim.HistoryLength);
            chart.SetCeiling(run.epoch > 0 ? (float)run.ceiling : 0, new Color32(88, 121, 138, 255));
            chart.SetData(Sim.PracticeSeries(run), run.histVal, (float)XgSim.MemorizingGap);
            yTop.text = XgSim.Pct(chart.Max);
            yMid.text = XgSim.Pct((chart.Max + chart.Min) / 2);
            yLow.text = XgSim.Pct(chart.Min);
            legend.text = Hex(XgTwoCurveGraphic.PracticeColor) + "━ " + T("练过的题", "practised") + "</color>   " + Hex(XgTwoCurveGraphic.NewColor) + "╍ " + T("新题（验证）", "new cards (validation)") + "</color>   "
                + Hex(new Color32(88, 121, 138, 255)) + "╌ " + T("上限", "ceiling") + "</color>   " + Hex(XgDark.Bad) + "● " + T("退步", "a drop") + "</color>";
            RefreshMemorizing(run);
            RefreshDiagnostics(run);
            RefreshSheet(run);
            RefreshDiagnoseButton(run);

            bool unlocked = Sim.TrainingUnlocked(track);
            string blocker = Sim.StartBlocker(run, Host);
            train.Set(Lang.T("训练一轮") + (Sim.TrainingQuizBonusAvailable || run.trainingQuizBonusReserved ? " · +5%" : ""), unlocked && blocker == null && !run.epochActive, XgDark.Accent, Color.white);
            trainSub.text = unlocked ? T("第 " + run.epoch + " 轮", "epoch " + run.epoch) : Lang.T("先标够样本");
            RefreshBill(run, unlocked);
            RefreshRates(run, Sim.TrainPart("rate"));
            assess.Show(false);

            // The chance that the next round goes down, and the cards' heat (XgSim.AutoModel.cs, ChapterOneSim.Bills.cs).
            var forecast = Sim.TrainingRiskForecast(run);
            risk.text = unlocked ? RiskLine(run, forecast) : "";
            risk.color = forecast.level >= 2 ? XgDark.Bad : forecast.level == 1 ? XgDark.Money : XgDark.Muted;
            string waiting = run.epochActive ? Sim.Blocker(run, Host) : null;
            hint.text = blocker != null ? "⚠ " + blocker : waiting != null ? "◷ " + waiting : Hint(run);
            hint.color = blocker != null ? XgDark.Bad : Sim.Plateaued(run) || Sim.ParamsShortForNext(run) ? XgDark.Gold : XgDark.Muted;

            RefreshSide(track, run);
        }

        /// <summary>
        /// 「下一轮退步：中 12% · 脏标注 120 条」, and on a second line the warning when the cards are hot. The reason of the
        /// risk is in the tooltip.
        /// </summary>
        string RiskLine(XgRun run, XgRiskForecast forecast)
        {
            string line = Sim.TrainingRiskLine(forecast);
            double dirty = Math.Floor(Sim.Noise(run.dataset) + Sim.DataNoise(run.dataset));
            if (dirty >= 1) line += "  <color=#3A5566>" + T("脏标注 " + N(dirty, "0") + " 条", N(dirty, "0") + " dirty labels") + "</color>";
            if (HeatWarning()) line += "\n<color=#EF9F27>" + T("显卡太热，可能烧卡", "The cards are hot and may burn out") + "</color>";
            // Stays on the page for a while after an overfit drop, whatever toast comes next (XgTrainPage.Overfit.cs).
            if (OverfitNudging) line = "<color=#E24B4A><b>" + T("上一轮过拟合退步了：补数据 / Dropout / 点「回档」", "Last round overfit: more data / Dropout / Restore") + "</b></color>\n" + line;
            return line;
        }

        /// <summary>The cards have trained long enough in a row that every further second may burn one out (and nothing stops it now).</summary>
        bool HeatWarning() => Host is XingGuangHost home && home.WearRiskLive;

        string RiskTip()
        {
            var run = Sim.Run(Track);
            string tip = Sim.TrainingRiskDetail(Sim.TrainingRiskForecast(run));
            if (Host is XingGuangHost home && home.WearRiskLive)
                tip += "\n\n" + T("显卡连着练太久了：每多练一秒都有一点概率烧掉一张卡（送修要花钱，修好前少一张卡）。歇几秒就清零。九州风神让概率减半。",
                    "The cards have trained non-stop for long: every further second may burn one out (a repair costs money, and the card is gone until it is back). A few seconds of rest reset it. The tower cooler halves the chance.");
            return tip;
        }

        string NumbersTip()
        {
            var run = Sim.Run(Track);
            return T("新题：在没见过的题上的准确率（验证，成绩按它算），曲线里的虚线。练过的：在它练过的题上的准确率，曲线里的实线。\n两条线分得越开，它越是在背答案：参数比样本多得多时就会这样，多标数据、买数据包或 Dropout 能让两条线靠拢。\n最佳：这个数据集刷过的最高成绩。\n上限：这个模型现在最多能练到的分数，由参数量、样本量和结构决定；灰色细虚线就是它。\n红点：这一轮退步了。",
                "New: accuracy on unseen cards (validation, which the score uses), the dashed line. Practised: accuracy on the cards it trained on, the solid line.\nThe further apart the lines, the more it memorises: that happens when it has far more parameters than samples; more labels, a data pack or Dropout bring them together.\nBest: the top score reached on this dataset.\nCeiling: the most this model can reach now, set by parameters, samples and structure; the thin grey dashes are it.\nRed dots: rounds that went back.");
        }

        /// <summary>The price of the next round, as the household's bills will take it (this month's tier, cuDNN trims the energy).</summary>
        void RefreshBill(XgRun run, bool unlocked)
        {
            if (!unlocked || !(Host is XingGuangHost home)) { trainBill.text = ""; return; }
            double price = home.RoundPrice(XgSim.DurationFor(run), Sim.TrainPriceFactor, Sim.TrainEnergyFactor);
            trainBill.text = T("电费 ¥", "Power ¥") + N(price, price < 10 ? "0.0" : "0");
        }

        string Hint(XgRun run)
        {
            if (!Sim.TrainingUnlocked(Track)) return T("先去标注台标够 " + XgCatalog.SamplesToTrain + " 条样本。", "Label " + XgCatalog.SamplesToTrain + " samples first.");
            if (run.epoch == 0) return Lang.T("按「训练一轮」。每轮练完都会考一次：刷新纪录才给钱。");
            // The cause is the player's to work out (卡住时让玩家诊断): no fix named before the diagnosis.
            if (Sim.DiagnosisDue(run) != XgLimit.None) return T("它卡住了，再练也涨不动。点「看看为什么」，自己找原因。", "It's stuck and training no longer helps. Press \"see why\" and work out the cause.");
            string plateau = Sim.PlateauText(run, out _);
            if (plateau.Length > 0) return plateau;
            if (run.staleEvals >= XgSim.StaleHintEpochs) return Sim.StaleHint(run);
            if (Sim.Memorizing(run)) return T("两条线分开了：练过的题它都会，新题不会，它在背答案。多标数据、买数据包或用 Dropout，两条线就会靠拢。",
                "The lines have parted: it knows its practice cards but not new ones, so it is memorising. More labels, a data pack or Dropout bring them together.");
            return T("参数和样本够，就一直往上涨。", "With enough parameters and samples it keeps rising.");
        }

        /// <summary>The model in one line (structure · parameters · VRAM) and the save button; the rest of the old model card is in <see cref="ModelTip"/>.</summary>
        void RefreshModelLine(XgTrack track, XgRun run, XgArch a)
        {
            double size = XgSim.ParamsK(run);
            double need = XgSim.VramNeedMB(run), have = Sim.Vram(Host);
            string vram = N(need / 1024, "0.0") + "/" + Gigabytes(have) + (need > have + 1e-6 ? " <color=#E24B4A>" + T("超了", "over") + "</color>" : "");
            summary.Set(T(a.name, a.nameEn) + " · " + XgSim.ParamsText(size) + T(" 参数", " params") + T(" · 显存 ", " · VRAM ") + vram + "  <color=#8FB8FF>→</color>", true, Color.clear);
            var checkpoint = BestRollback();
            saveModel.Show(true);
            bool nudge = OverfitNudging && !RollbackBusy && checkpoint != null;
            saveModel.Set(RollbackBusy ? T("回档 · 忙碌", "Restore · busy") : checkpoint == null ? T("回档 · 无档", "No checkpoint") : nudge ? T("← 回档", "← Restore") : T("回档", "Restore"), !RollbackBusy && checkpoint != null,
                nudge ? Color.Lerp(XgDark.Card, XgDark.Bad, .55f) : XgDark.AccentSoft, nudge ? Color.white : XgDark.Accent);
        }

        /// <summary>
        /// What the old model card said, for the model line's tooltip: the structure, the shape it configured itself to,
        /// the techniques in use, what a round feeds, the ceiling and why training stalls.
        /// </summary>
        string ModelTip()
        {
            var track = Track;
            var run = Sim.Run(track);
            var a = XgCatalog.Arch(run.arch);
            double size = XgSim.ParamsK(run);
            int depthCap = Sim.DepthCap(track), widthCap = Sim.WidthCap(track), archDepth = Sim.MaxDepth(run);
            bool fullDepth = run.depth >= Math.Min(depthCap, archDepth), fullWidth = run.width >= widthCap;
            string region = XgSim.RegionOf(run.dataset);
            var tip = new System.Text.StringBuilder();
            tip.Append(T(XgSim.RegionName(region, false), XgSim.RegionName(region, true))).Append(T("自动用的结构，点一下去道具页。\n", " uses this structure automatically; click for the Items page.\n"));
            tip.Append(ArchTip(run.arch)).Append("\n\n");
            tip.Append(T("模型是自动配置的：结构用已经买到的最好的那个，宽度和层数用科技买到的上限里显卡装得下的最大的。\n参数量 = 层数 × 宽度² × 结构系数。括号里拿真实的著名模型比一比大小，只是参考。",
                "The model configures itself: the best structure you own, and the biggest width and depth the tech tree allows that fit the card.\nParameters = layers × width² × structure factor. The famous model in brackets is only for a sense of scale.")).Append("\n");
            tip.Append(T("宽 ", "Width ")).Append(XgCatalog.Widths[run.width]).Append("/").Append(XgCatalog.Widths[widthCap]).Append(" · ").Append(run.depth).Append("/").Append(Math.Min(depthCap, archDepth)).Append(T(" 层", " layers"))
                .Append(" (").Append(XgSim.ParamScale(size, Sim.English)).Append(")\n");
            tip.Append(fullDepth && fullWidth ? T("已是科技上限里最大的一个；要更大，去科技买「宽」「层」。", "The biggest the tech tree allows; for more, buy width or layers in the tech tree.")
                : T("显卡只装得下这么大；加显卡或接线页腾出显存，就自动长大。", "The card only holds this much; add a card or free memory on the Wiring page and it grows by itself.")).Append("\n\n");
            tip.Append(Techniques(run)).Append("\n");
            tip.Append(T("优化器：", "Optimizer: ")).Append(Sim.OptimizerName).Append("\n\n");
            double share = Host.Compute;
            tip.Append(Lang.T("每轮喂 ")).Append(Sim.CardsPerEpoch(run, Math.Max(.5, share), true)).Append(Lang.T(" 张 / 数据池 ")).Append(Sim.PoolSize(run)).Append(Lang.T(" 张（已过 ")).Append(N(Sim.PassesOverData(run), "0.0")).Append(Lang.T(" 遍）"));
            if (run.lastScore >= 0) tip.Append(Lang.T(" · 最近评估 ")).Append(XgCatalog.GradeNames[XgSim.Grade(run.lastScore)]).Append(" ").Append(N(run.lastScore, "0"));
            if (run.epoch > 0)
                tip.Append("\n").Append(T("这个模型的上限 ", "This model's ceiling ")).Append(N(XgSim.Score(run.dataset, Sim.CeilingAcc(run)), "0")).Append(T(" 分", " points"))
                    .Append(Sim.Plateaued(run) ? T("（已经到了）", " (reached)") : T("，还差 ", ", ") + N(Sim.HeadroomPoints(run), "0") + T(" 分", " to go"));
            string plateau = Sim.DiagnosisDue(run) != XgLimit.None ? T("它卡住了：原因要你自己找（训练按钮旁「看看为什么」）。", "It's stuck: the cause is yours to find (\"see why\" by the train button).") : Sim.PlateauText(run, out _);
            if (plateau.Length > 0) tip.Append("\n<color=#C88A00>").Append(plateau).Append("</color>");
            tip.Append("\n\n").Append(T("每一轮都往这个模型的上限走一步。上限由参数量和样本量决定：参数和样本够，就一直涨；不够，就停在那里。\n每一轮也有一点机会退步：数据太少（背答案）、数据太脏、刚涨了一大截时更容易。学会的能力不会丢。",
                "Every round takes the model a step towards its ceiling, which parameters and samples set: with enough of both it keeps rising; without, it stops there.\nEach round may also go down a little, more often when the data is scarce (it memorises), dirty, or right after a big jump. Learned abilities are never lost."));
            return tip.ToString();
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

        void RefreshDiagnostics(XgRun run)
        {
            if (!Sim.TrainPart("weights")) return;
            int stage = Sim.StageFor(Track);
            string[] zh = { "", "权重 · 28×28 像素", "MLP · 隐藏层组合", Track == XgTrack.Vision ? "局部结构 · 深度与表现" : "序列 · 记忆衰减", Track == XgTrack.Vision ? "残差 · 信息保留" : "门控 · 长期记忆", "注意力 · 寻找线索", "多头注意力 · 并行" };
            string[] en = { "", "Weights · 28×28 pixels", "MLP · hidden features", Track == XgTrack.Vision ? "Local structure · depth" : "Sequence · memory decay", Track == XgTrack.Vision ? "Residual · preserve information" : "Gates · longer memory", "Attention · find the clue", "Multi-head attention · parallel" };
            // The teaching note sits after the title: the thumbnails of the earlier stages take the right edge.
            string note = stage >= 5
                ? Lang.T("教学示意；模拟 GPU ") + XgSim.Pct(Sim.GpuUtilization(run))
                : Lang.T("教学示意，不是本地模型的内部权重");
            diagnosticTitle.text = stage + " · " + T(zh[stage], en[stage]) + "   <size=11><color=#6F95A5>" + note + "</color></size>";
            diagnostic.Show(Sim, run, stage);
            diagnosticNote.text = "";
            int count = stage - 1;
            for (int i = 0; i < previousDiagnostics.Count; i++)
            {
                var previous = previousDiagnostics[i];
                previous.gameObject.SetActive(i < count);
                if (i >= count) continue;
                var rt = previous.rectTransform;
                rt.anchorMin = new Vector2(1, i / (float)count); rt.anchorMax = new Vector2(1, (i + 1f) / count);
                rt.offsetMin = new Vector2(-85, 2f / count); rt.offsetMax = new Vector2(-3, -20f / count);
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
                    T("正在下载《" + XgCatalog.Dataset(dataset).name + "》……走家里的" + XgSim.BroadbandName(Sim.BroadbandTier, false) + "（" + LineSpeed(Sim.LineMBps) + "），预计 " + N(Sim.DownloadLeft(dataset), "0") + " 秒。急用可以开摆渡云超级会员。",
                      "Downloading " + XgCatalog.Dataset(dataset).nameEn + "… over the home " + XgSim.BroadbandName(Sim.BroadbandTier, true) + " (" + LineSpeed(Sim.LineMBps) + "), about " + N(Sim.DownloadLeft(dataset), "0") + " s. Bodu Cloud super membership speeds it up."), 8);
            Fx.Knock(gpuTarget != null ? gpuTarget : train.rt, .12f, new Vector2(0, -8));
            Fx.Shockwave(Fx.At(gpuTarget != null ? gpuTarget : train.rt), XgDark.Accent, 150, .35f, 7);
            Fx.Burst(Fx.At(gpuTarget != null ? gpuTarget : train.rt), 20, XgDark.Gold, XgJuice.Shape.Spark, 220);
            Fx.Play(XgJuice.Sfx.Id.Clack, .7f, .7f); Fx.Play(XgJuice.Sfx.Id.Buzz, 1.8f, .25f);
            view.Refresh(true);
            return true;
        }

        public void SetPackDragging(bool dragging) { if(gpuGlow != null) gpuGlow.on = dragging; }

        /// <summary>The home line's speed as a download dialog shows it: 250KB/s, 1.2MB/s.</summary>
        static string LineSpeed(double mbps) => mbps < 1 ? N(Math.Max(1, mbps * 1024), "0") + "KB/s" : N(mbps, mbps < 10 ? "0.#" : "0") + "MB/s";

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
        public void OnSubmit(BaseEventData e)
        {
            if (!isActiveAndEnabled) return;
            var button = GetComponent<UnityEngine.UI.Selectable>();
            if (button == null || !button.IsActive() || !button.IsInteractable()) return;
            Submit?.Invoke();
        }
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
            if (e.pointerDrag != null && e.pointerDrag.GetComponent<XgFilesRow>() != null)
            {
                GetComponentInParent<XgFilesTrainDrop>()?.OnDrop(e);
                return;
            }
            var drag = e.pointerDrag != null ? e.pointerDrag.GetComponent<XgDataPackDrag>() : null;
            if (drag != null && drag.Dragging && drag.Page == Page) Page.InstallPack(drag.Dataset);
        }
    }
}
