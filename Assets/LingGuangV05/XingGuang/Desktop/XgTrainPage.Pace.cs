using System;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using static LingGuangV05.Desktop.XingGuang.XgUi;

using LingGuangV05.Core;
namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 进度条仿真 on the training page (XgSim.TrainPace.cs): the current line's round as a console progress line by the
    /// 训练一轮 button — the phase, the samples a second, the wobbly ETA and, in amber, why it stalls — with tick marks
    /// at the phase boundaries and a flash at validation's jump to 100%; under it the other line's round in small when
    /// both train, or a Keras-style summary of the last round. Also the curve card's extras: the 它在背答案 note on the
    /// two curves (XgSim.Holdout.cs) and the microscope button (XgRealTrainPanel).
    /// </summary>
    public sealed partial class XgTrainPage
    {
        /// <summary>The bar's text is rewritten this often (seconds); the bar itself moves every frame.</summary>
        const float PaceTextEvery = .1f;

        XgPaceBarGraphic paceBar, otherBar;
        TMP_Text paceHead, paceEta, paceSub, otherName, otherState, memorizeNote;
        RectTransform otherRow;
        XgBtn microscope;
        float paceTextTimer;
        readonly bool[] paceWasActive = new bool[2];
        readonly int[] paceEpoch = new int[2];

        void BuildPace(RectTransform mid)
        {
            paceHead = ui.Text(Rect("PaceHead", mid, new Vector2(0, 1), Vector2.one, new Vector2(0, -18), new Vector2(-96, 0)), "", 12, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            paceHead.textWrappingMode = TextWrappingModes.NoWrap; paceHead.overflowMode = TextOverflowModes.Ellipsis;
            paceEta = ui.Text(Rect("PaceEta", mid, new Vector2(1, 1), Vector2.one, new Vector2(-96, -18), Vector2.zero), "", 12, XgDark.Muted, TextAlignmentOptions.MidlineRight);
            paceEta.textWrappingMode = TextWrappingModes.NoWrap;
            epochBar = Rect("EpochBar", mid, new Vector2(0, 1), Vector2.one, new Vector2(0, -32), new Vector2(0, -19));
            paceBar = epochBar.gameObject.AddComponent<XgPaceBarGraphic>();
            paceBar.raycastTarget = false;
            paceBar.LoadEnd = (float)XgSim.PaceTrainAt; paceBar.TrainEnd = (float)XgSim.PaceValidateAt;

            // The line under the bar: the other line's round in small, or the last round's summary.
            paceSub = ui.Text(Rect("PaceSub", mid, new Vector2(0, 1), Vector2.one, new Vector2(0, -52), new Vector2(0, -34)), "", 11, XgDark.Dim, TextAlignmentOptions.MidlineLeft);
            paceSub.textWrappingMode = TextWrappingModes.NoWrap; paceSub.overflowMode = TextOverflowModes.Ellipsis;
            otherRow = Rect("OtherLine", mid, new Vector2(0, 1), Vector2.one, new Vector2(0, -52), new Vector2(0, -34));
            otherName = ui.Text(Rect("Name", otherRow, Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(40, 0)), "", 11, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            otherName.textWrappingMode = TextWrappingModes.NoWrap;
            var mini = Rect("Bar", otherRow, new Vector2(0, .5f), new Vector2(.36f, .5f), new Vector2(42, -3), new Vector2(0, 3));
            otherBar = mini.gameObject.AddComponent<XgPaceBarGraphic>();
            otherBar.raycastTarget = false; otherBar.Cell = 3;
            otherBar.LoadEnd = (float)XgSim.PaceTrainAt; otherBar.TrainEnd = (float)XgSim.PaceValidateAt;
            otherState = ui.Text(Rect("State", otherRow, new Vector2(.36f, 0), Vector2.one, new Vector2(8, 0), Vector2.zero), "", 11, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            otherState.textWrappingMode = TextWrappingModes.NoWrap; otherState.overflowMode = TextOverflowModes.Ellipsis;
            otherRow.gameObject.SetActive(false);

            UiTip.Add(epochBar, () => T("这一轮的进度：先加载数据（硬盘一阵快一阵慢），再训练（前向、反向，比较稳），最后在没见过的题上验证一次，然后跳到 100%。\n模型越大、每轮的卡越多越慢，显卡越多越快。显卡太热会降频，两条线一起练要轮流用卡。",
                "This round's progress: loading the data (the disk comes in bursts), training (forward and backward, steadier), then one check on unseen cards and the jump to 100%.\nBigger models and more cards a round are slower; more GPUs are faster. Hot cards clock down, and two lines training at once take turns on the card."));
        }

        void BuildCurveExtras()
        {
            // 它在背答案: at the right end of the two curves, on the gap, while it is wide.
            memorizeNote = ui.Text(Rect("Memorizing", chart.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-96, -9), new Vector2(-6, 9)), "", 11, XgTwoCurveGraphic.GapColor, TextAlignmentOptions.MidlineRight);
            memorizeNote.textWrappingMode = TextWrappingModes.NoWrap; memorizeNote.fontStyle = FontStyles.Bold;
            memorizeNote.gameObject.SetActive(false);
            UiTip.Add(memorizeNote, () => T("练过的题它都会，新题不会：它记住了答案，没学会规律。参数比样本多得多时就会这样。",
                "It knows the cards it practised but not new ones: it memorised the answers instead of the rule. It happens when it has far more parameters than samples."));

            // 显微镜: a short real run on a tiny net (XgRealTrainPanel). The desktop font has no 🔬.
            microscope = ui.Button(curveCard, T("显微镜 · 看它学", "Microscope · watch it learn"), () =>
            {
                if (XgRealTrainPanel.OpenMicroscope(view)) Fx.Play(XgJuice.Sfx.Id.Click);
                else Fx.Play(XgJuice.Sfx.Id.Thud);
            }, 12);
            microscope.rt.gameObject.name = "Microscope";
            microscope.rt.anchorMin = microscope.rt.anchorMax = new Vector2(1, 0);
            microscope.rt.offsetMin = new Vector2(-144, 5); microscope.rt.offsetMax = new Vector2(-12, 27);
            microscope.label.enableAutoSizing = true; microscope.label.fontSizeMin = 9; microscope.label.fontSizeMax = 12;
            microscope.Set(T("显微镜 · 看它学", "Microscope · watch it learn"), true, XgDark.AccentSoft, XgDark.Good);
            microscope.Show(view != null);
            UiTip.Add(microscope.rt, () => T("打开显微镜：一个真的小网络，从随机权重开始练大约 8 秒，看每个神经元慢慢学会看哪里。每次打开都从头练。",
                "Open the microscope: a real tiny network trains for about 8 seconds from random weights, and you watch each neuron learn where to look. Each look starts afresh."));
        }

        /// <summary>The 它在背答案 note sits on the gap at the right end of the curves while it is wide.</summary>
        void RefreshMemorizing(XgRun run)
        {
            if (memorizeNote == null) return;
            bool gap = chart.LastGap(out var middle);
            bool wide = run.epoch > 0 && gap && Sim.Memorizing(run);
            memorizeNote.gameObject.SetActive(wide);
            if (!wide) return;
            memorizeNote.text = T("它在背答案", "memorising");
            var rect = chart.rectTransform.rect;
            float y = Mathf.Clamp(middle.y - rect.yMin, 9, Mathf.Max(9, rect.height - 9));
            memorizeNote.rectTransform.anchoredPosition = new Vector2(-51, y);
        }

        bool ReducedMotion => Fx.Reduced || Sim.S.reduceFx;

        void TickPace(float dt)
        {
            if (paceBar == null) return;
            var track = Track;
            var run = Sim.Run(track);
            var pace = Sim.Pace(run);
            int t = (int)track;
            bool reduced = ReducedMotion;
            paceBar.Reduced = reduced; otherBar.Reduced = reduced;
            // The jump to 100%: the round just landed on this line.
            if (paceWasActive[t] && !run.epochActive && run.epoch != paceEpoch[t]) paceBar.Finish();
            paceWasActive[t] = run.epochActive; paceEpoch[t] = run.epoch;
            bool moving = pace.active && pace.phase != XgPacePhase.Validate && pace.speed > .05;
            if (!paceBar.Flashing) paceBar.Show(pace.active ? (float)pace.progress : 0, pace.active, moving, Stalled(pace));

            var other = track == XgTrack.Vision ? XgTrack.Sequence : XgTrack.Vision;
            var otherRun = Sim.Run(other);
            var otherPace = Sim.Pace(otherRun);
            int o = (int)other;
            bool both = otherPace.active && Sim.TrainingUnlocked(other);
            if (paceWasActive[o] && !otherRun.epochActive && otherRun.epoch != paceEpoch[o]) otherBar.Finish();
            paceWasActive[o] = otherRun.epochActive; paceEpoch[o] = otherRun.epoch;
            bool otherShown = both || otherBar.Flashing;
            if (otherRow.gameObject.activeSelf != otherShown) otherRow.gameObject.SetActive(otherShown);
            if (paceSub.gameObject.activeSelf == otherShown) paceSub.gameObject.SetActive(!otherShown);
            if (both && !otherBar.Flashing)
                otherBar.Show((float)otherPace.progress, true, otherPace.phase != XgPacePhase.Validate && otherPace.speed > .05, Stalled(otherPace));

            paceTextTimer -= dt;
            if (paceTextTimer > 0) return;
            paceTextTimer = PaceTextEvery;
            PaceText(track, run, pace);
            if (otherShown) OtherText(other, otherPace);
        }

        /// <summary>The bar dims when the round stands (nearly) still; sharing the card only slows it, so it dims only when it crawls.</summary>
        static bool Stalled(XgPace p) => p.active && p.stall != XgPaceStall.None && (p.stall != XgPaceStall.SharedCard || p.speed < .5);

        void PaceText(XgTrack track, XgRun run, XgPace p)
        {
            if (!Sim.TrainingUnlocked(track)) { paceHead.text = paceEta.text = paceSub.text = ""; return; }
            if (!p.active)
            {
                // Idle: how long a round of this model takes here, like the console before the first epoch.
                double seconds = Sim.PaceSeconds(run, Host);
                int cards = Sim.CardsPerEpoch(run, Math.Max(.5, Host.Compute), true);
                paceHead.text = Hex(XgDark.Muted) + T("空闲 · 一轮约 " + N(seconds, "0.0") + " 秒，" + cards + " 张卡", "Idle · a round takes about " + N(seconds, "0.0") + " s, " + cards + " cards") + "</color>";
                paceEta.text = "";
                paceSub.text = LastRoundLine(run);
                return;
            }
            string head = "<b>" + p.phaseLabel + "</b>";
            if (p.phase == XgPacePhase.Validate) head += "  " + Hex(XgDark.Muted) + T("在没见过的题上考一次", "a check on unseen cards") + "</color>";
            else head += "  " + Hex(XgDark.Good) + N(SamplesPerSecond(run, p), "#,##0") + "</color>" + Hex(XgDark.Muted) + T(" 样本/秒", " samples/s") + "</color>";
            if (p.stall != XgPaceStall.None && p.stallReason.Length > 0) head += "  " + Hex(XgDark.Hot) + p.stallReason + "</color>";
            paceHead.text = head;
            paceEta.text = Hex(XgDark.Ink) + N(Math.Floor(p.progress * 100), "0") + "%</color>  " + T("还要 ", "ETA ") + Clock(p.eta);
            int fed = Sim.CardsPerEpoch(run, Math.Max(.5, Host.Compute), run.epochHand);
            paceSub.text = T("第 " + (run.epoch + 1) + " 轮 · " + fed + " 张卡 · 标称 " + N(p.nominal, "0.0") + " 秒" + (run.epochHand ? "" : " · 自动"),
                "epoch " + (run.epoch + 1) + " · " + fed + " cards · nominal " + N(p.nominal, "0.0") + " s" + (run.epochHand ? "" : " · auto"));
        }

        void OtherText(XgTrack other, XgPace p)
        {
            otherName.text = other == XgTrack.Vision ? T("视觉", "Vision") : T("文字", "Text");
            if (!p.active) { otherState.text = Hex(XgDark.Good) + T("这一轮练完了", "round done") + "</color>"; return; }
            otherState.text = p.phaseLabel + " " + N(Math.Floor(p.progress * 100), "0") + "% · " + T("还要 ", "ETA ") + Clock(p.eta)
                + (p.stall != XgPaceStall.None && p.stallReason.Length > 0 ? "  " + Hex(XgDark.Hot) + p.stallReason + "</color>" : "");
        }

        /// <summary>Samples a second right now: the round's cards over its planned length, at this moment's speed.</summary>
        double SamplesPerSecond(XgRun run, XgPace p)
        {
            int cards = Sim.CardsPerEpoch(run, Math.Max(.5, Host.Compute), run.epochHand);
            double rate = cards / Math.Max(.01, p.nominal) * Math.Max(0, p.speed);
            return double.IsNaN(rate) || double.IsInfinity(rate) ? 0 : rate;
        }

        /// <summary>The last round as the console printed it: 「上一轮 2.6 秒 · 练过的 91.2% · 新题 87.4%」.</summary>
        string LastRoundLine(XgRun run)
        {
            if (run.epoch <= 0 || run.paceSeed == 0) return "";
            return T("上一轮 " + N(run.paceClock, "0.0") + " 秒 · 练过的 " + XgSim.Pct(Sim.PracticeAccuracy(run)) + " · 新题 " + XgSim.Pct(run.valAcc),
                "last round " + N(run.paceClock, "0.0") + " s · practised " + XgSim.Pct(Sim.PracticeAccuracy(run)) + " · new " + XgSim.Pct(run.valAcc));
        }

        /// <summary>An ETA the way a console prints it: 0:07, 1:12.</summary>
        static string Clock(double seconds)
        {
            int s = (int)Math.Ceiling(Math.Max(0, double.IsNaN(seconds) ? 0 : seconds));
            return s / 60 + ":" + (s % 60).ToString("00");
        }
    }
}
