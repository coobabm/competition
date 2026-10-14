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
    /// 卡住时让玩家诊断 (训练页_答卷诊断能力考_设计.md §二): when a run plateaus, 「它卡住了 · 看看为什么」 opens the
    /// evidence (practice cards against new cards, the kinds of mistakes, a check of the labels) and the four causes.
    /// A wrong pick greys out with why not; the right one says what fixes it and offers the way there. The rules are
    /// in XgSim.Diagnosis.cs.
    /// </summary>
    public sealed partial class XgTrainPage
    {
        const int KindRows = 4;

        XgBtn diagnose, diagClose, diagFix, diagDone;
        readonly XgBtn[] diagCauses = new XgBtn[4];
        RectTransform diagLayer, diagCard, trainedFill, newFill;
        readonly List<RectTransform> kindFills = new List<RectTransform>();
        readonly List<TMP_Text> kindLabels = new List<TMP_Text>();
        TMP_Text diagTitle, trainedText, newText, kindsTitle, labelsText, examplesText, diagAsk, diagResult;
        XgDiagnosisResult diagSolved;
        XgTrack diagTrack;

        void BuildDiagnosis()
        {
            // The way in: under the round's progress, lit while a plateau waits to be worked out.
            diagnose = ui.Button(trainRow, "", OpenDiagnosis, 15);
            diagnose.rt.gameObject.name = "DiagnoseButton";
            diagnose.rt.anchorMin = diagnose.rt.anchorMax = Vector2.zero;
            diagnose.rt.offsetMin = new Vector2(252, 6); diagnose.rt.offsetMax = new Vector2(252 + 280, 38);
            diagnose.label.fontStyle = FontStyles.Bold;
            diagnose.Show(false);
            UiTip.Add(diagnose.rt, () => T("再练也涨不动了。看看证据，自己判断是哪里出了问题：第一次就选对有奖励。", "Training no longer helps. Read the evidence and decide what is wrong: the right cause at the first pick earns a reward."));

            diagLayer = Rect("DiagnosisLayer", (RectTransform)root.parent.parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Panel(diagLayer, new Color(0, 0, 0, .55f));
            diagCard = ui.Card(diagLayer, "Diagnosis", new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-470, -255), new Vector2(470, 255));
            diagTitle = ui.Text(Strip("Title", diagCard, 14, 30, 22, 130), "", 20, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            diagTitle.fontStyle = FontStyles.Bold;
            diagClose = ui.Button(diagCard, T("先不管", "Not now"), CloseDiagnosis, 13);
            diagClose.rt.anchorMin = diagClose.rt.anchorMax = Vector2.one;
            diagClose.rt.offsetMin = new Vector2(-112, -44); diagClose.rt.offsetMax = new Vector2(-18, -14);

            // Left: the evidence.
            var left = Rect("Evidence", diagCard, Vector2.zero, new Vector2(.56f, 1), new Vector2(22, 18), new Vector2(-8, -56));
            var head = ui.Text(Strip("PracticeHead", left, 0, 20, 0, 0), T("练过的题 · 没见过的题（各抽 " + XgSim.DiagnosisSampleCards + " 道）", "Practice cards · new cards (" + XgSim.DiagnosisSampleCards + " of each)"), 13, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            head.textWrappingMode = TextWrappingModes.NoWrap;
            trainedFill = EvidenceRow(left, 24, T("练过的", "Practice"), out trainedText);
            newFill = EvidenceRow(left, 50, T("没见过的", "New"), out newText);
            kindsTitle = ui.Text(Strip("KindsHead", left, 88, 20, 0, 0), "", 13, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            kindsTitle.textWrappingMode = TextWrappingModes.NoWrap;
            for (int i = 0; i < KindRows; i++)
            {
                var row = Strip("Kind" + (i + 1), left, 112 + i * 28, 24, 0, 0);
                var label = ui.Text(Rect("Text", row, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-150, 0)), "", 13, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
                label.enableAutoSizing = true; label.fontSizeMin = 10; label.fontSizeMax = 13;
                label.overflowMode = TextOverflowModes.Truncate;
                var bar = Rect("Bar", row, new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-142, -6), new Vector2(0, 6));
                kindFills.Add(Bar(bar, "Fill", XgDark.Track, XgDark.Bad));
                kindLabels.Add(label);
            }
            labelsText = ui.Text(Strip("Labels", left, 232, 40, 0, 0), "", 13, XgDark.Ink, TextAlignmentOptions.TopLeft);
            examplesText = ui.Text(Rect("Examples", left, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 150)), "", 12, XgDark.Muted, TextAlignmentOptions.TopLeft);
            examplesText.enableAutoSizing = true; examplesText.fontSizeMin = 10; examplesText.fontSizeMax = 12;

            // Right: the four causes, the answer, the way out.
            var right = Rect("Causes", diagCard, new Vector2(.58f, 0), Vector2.one, new Vector2(8, 18), new Vector2(-22, -56));
            diagAsk = ui.Text(Strip("Ask", right, 0, 20, 0, 0), T("你觉得是什么原因？", "What do you think is wrong?"), 14, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            for (int i = 0; i < XgSim.DiagnosisCauses.Length; i++)
            {
                var cause = XgSim.DiagnosisCauses[i];
                var b = ui.Button(right, "", () => Pick(cause), 16);
                b.rt.gameObject.name = "Cause" + cause;
                b.rt.anchorMin = new Vector2(0, 1); b.rt.anchorMax = new Vector2(1, 1);
                b.rt.offsetMin = new Vector2(0, -66 - i * 46); b.rt.offsetMax = new Vector2(0, -28 - i * 46);
                diagCauses[i] = b;
            }
            diagResult = ui.Text(Rect("Result", right, Vector2.zero, Vector2.one, new Vector2(0, 46), new Vector2(0, -222)), "", 14, XgDark.Ink, TextAlignmentOptions.TopLeft);
            diagResult.enableAutoSizing = true; diagResult.fontSizeMin = 11; diagResult.fontSizeMax = 14;
            diagFix = ui.Button(right, "", null, 14);
            diagFix.rt.anchorMin = Vector2.zero; diagFix.rt.anchorMax = new Vector2(.62f, 0);
            diagFix.rt.offsetMin = new Vector2(0, 4); diagFix.rt.offsetMax = new Vector2(-6, 38);
            diagDone = ui.Button(right, T("知道了", "Got it"), CloseDiagnosis, 14);
            diagDone.rt.anchorMin = new Vector2(.62f, 0); diagDone.rt.anchorMax = new Vector2(1, 0);
            diagDone.rt.offsetMin = new Vector2(0, 4); diagDone.rt.offsetMax = new Vector2(0, 38);
            diagLayer.gameObject.SetActive(false);
        }

        RectTransform EvidenceRow(RectTransform parent, float y, string name, out TMP_Text count)
        {
            var row = Strip("Row" + name, parent, y, 22, 0, 0);
            var label = ui.Text(Rect("Name", row, Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(70, 0)), name, 13, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            var bar = Rect("Bar", row, Vector2.zero, Vector2.one, new Vector2(76, 5), new Vector2(-96, -5));
            var fill = Bar(bar, "Fill", XgDark.Track, XgDark.Good);
            count = ui.Text(Rect("Count", row, new Vector2(1, 0), Vector2.one, new Vector2(-90, 0), Vector2.zero), "", 13, XgDark.Ink, TextAlignmentOptions.MidlineRight);
            count.textWrappingMode = TextWrappingModes.NoWrap;
            return fill;
        }

        void RefreshDiagnoseButton(XgRun run)
        {
            if (diagnose == null) return;
            bool due = Sim.DiagnosisDue(run) != XgLimit.None;
            diagnose.Show(due);
            if (due) diagnose.Set(T("它卡住了 · 看看为什么 →", "It's stuck · see why →"), true, new Color32(74, 52, 12, 255), XgDark.Money);
        }

        void OpenDiagnosis()
        {
            var run = Sim.Run(Track);
            if (Sim.DiagnosisDue(run) == XgLimit.None) { Fx.Play(XgJuice.Sfx.Id.Thud); return; }
            diagTrack = Track;
            diagSolved = null;
            diagResult.text = "";
            Fx.Play(XgJuice.Sfx.Id.Whoosh, 1, .6f);
            diagLayer.gameObject.SetActive(true);
            diagLayer.SetAsLastSibling();
            Fx.Knock(diagCard, .06f);
            ShowEvidence(run);
            ShowCauses(run);
        }

        void CloseDiagnosis()
        {
            if (diagLayer != null) diagLayer.gameObject.SetActive(false);
            diagSolved = null;
            view.Refresh(true);
        }

        public bool DiagnosisOpen => diagLayer != null && diagLayer.gameObject.activeSelf;

        void ShowEvidence(XgRun run)
        {
            var d = XgCatalog.Dataset(run.dataset);
            diagTitle.text = T("它卡住了：「" + d.name + "」再练也涨不动。为什么？", "It's stuck: " + d.nameEn + " no longer improves. Why?");
            var ev = Sim.DiagnosisEvidence(run);
            SetBar(trainedFill, ev.trainedRight / (float)ev.trainedTotal);
            SetBar(newFill, ev.newRight / (float)ev.newTotal);
            trainedText.text = T("对 " + ev.trainedRight + " / " + ev.trainedTotal, ev.trainedRight + " / " + ev.trainedTotal + " right");
            newText.text = T("对 " + ev.newRight + " / " + ev.newTotal, ev.newRight + " / " + ev.newTotal + " right");
            kindsTitle.text = T("它答错的 " + ev.wrongTotal + " 道题，按错法分：", "The " + ev.wrongTotal + " cards it got wrong, by kind of mistake:");
            // The top kinds; with more kinds than rows, the last row adds up the rest (「其他」).
            bool more = ev.kinds.Count > KindRows;
            int shown = more ? KindRows - 1 : ev.kinds.Count, rest = 0;
            for (int i = shown; i < ev.kinds.Count; i++) rest += ev.kinds[i].count;
            for (int i = 0; i < KindRows; i++)
            {
                bool other = more && i == KindRows - 1;
                bool has = i < shown || other;
                kindLabels[i].transform.parent.gameObject.SetActive(has);
                if (!has) continue;
                string text = other ? T("其他 " + (ev.kinds.Count - shown) + " 种", (ev.kinds.Count - shown) + " other kinds") : T(ev.kinds[i].text, ev.kinds[i].textEn);
                int count = other ? rest : ev.kinds[i].count;
                kindLabels[i].text = text + "  " + Hex(XgDark.Muted) + count + "</color>";
                SetBar(kindFills[i], ev.wrongTotal == 0 ? 0 : count / (float)ev.wrongTotal);
            }
            labelsText.text = T("核对它“答错”的 " + ev.checkedLabels + " 道练习题的标签：", "Checked the labels of " + ev.checkedLabels + " practice cards it \"got wrong\": ")
                + (ev.labelWrong > 0 ? Hex(XgDark.Money) + T(ev.labelWrong + " 道是标签写错了", ev.labelWrong + " were labelled wrong") + "</color>"
                                     : Hex(XgDark.Good) + T("标签都对", "the labels are right") + "</color>");
            var ex = Sim.English ? ev.examplesEn : ev.examples;
            examplesText.text = ex.Count == 0 ? "" : T("错题举例：", "For example:") + "\n· " + string.Join("\n· ", ex);
        }

        void ShowCauses(XgRun run)
        {
            var wrong = Sim.DiagnosisWrongPicks(run);
            for (int i = 0; i < diagCauses.Length; i++)
            {
                var cause = XgSim.DiagnosisCauses[i];
                bool isWrong = wrong.Contains(cause), isRight = diagSolved != null && diagSolved.actual == cause;
                string name = XgSim.CauseName(cause, Sim.English);
                if (isRight) diagCauses[i].Set("✓ " + name, false, XgDark.AccentSoft, XgDark.Good);
                else if (isWrong) diagCauses[i].Set("× " + name, false, XgDark.Disabled, XgDark.Dim);
                else diagCauses[i].Set(name, diagSolved == null, XgDark.Button, diagSolved == null ? XgDark.Ink : XgDark.Dim);
            }
            diagAsk.text = diagSolved != null ? T("原因找到了", "Found it")
                : wrong.Count > 0 ? T("再看看证据，换一个？", "Look at the evidence again. Another one?") : T("你觉得是什么原因？", "What do you think is wrong?");
            diagFix.Show(diagSolved != null);
            diagDone.Show(diagSolved != null);
            if (diagSolved == null) return;
            switch (diagSolved.actual)
            {
                case XgLimit.Params: SetFix(T("去科技加宽、加深 →", "Width and layers in Tech →"), () => { CloseDiagnosis(); view.ShowTab("tree"); }); break;
                case XgLimit.Structure: SetFix(T("去道具看结构 →", "Structures in Items →"), () => { CloseDiagnosis(); view.ShowTab("items"); }); break;
                case XgLimit.Data: SetFix(T("去标注台多标 →", "Label more →"), () => { CloseDiagnosis(); view.Controller.OpenCrowd("label"); }); break;
                default: SetFix(T("清洗噪声", "Clean the noise"), () => { CloseDiagnosis(); CleanCurrentNoise(); }); break;
            }
        }

        void SetFix(string text, UnityEngine.Events.UnityAction act)
        {
            diagFix.Set(text, true, XgDark.Accent, Color.white);
            diagFix.button.onClick.RemoveAllListeners();
            diagFix.button.onClick.AddListener(act);
        }

        void Pick(XgLimit cause)
        {
            var run = Sim.Run(diagTrack);
            var r = Sim.Diagnose(run, cause, Host);
            if (r == null) { CloseDiagnosis(); return; }
            int i = System.Array.IndexOf(XgSim.DiagnosisCauses, cause);
            if (!r.correct)
            {
                Fx.Play(XgJuice.Sfx.Id.Thud, .8f, .6f);
                Fx.Knock(diagCauses[i].rt, .06f, new Vector2(10, 0));
                diagResult.text = Hex(XgDark.Bad) + T(r.line, r.lineEn) + "</color>";
                ShowCauses(run);
                return;
            }
            diagSolved = r;
            Fx.Play(r.firstTry ? XgJuice.Sfx.Id.Fanfare : XgJuice.Sfx.Id.Unlock, 1, .7f);
            Fx.Burst(Fx.At(diagCauses[i].rt), r.firstTry ? 28 : 12, r.firstTry ? XgDark.Gold : XgDark.Good, XgJuice.Shape.Star, 300, 300, .8f);
            diagResult.text = Hex(XgDark.Good) + T(r.line, r.lineEn) + "</color>"
                + (r.reward.Length > 0 ? "\n\n" + Hex(XgDark.Gold) + T(r.reward, r.rewardEn) + "</color>" : "");
            ShowCauses(run);
            view.Refresh(true);
        }
    }
}
