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
    /// 能力考 on the training page (训练页_答卷诊断能力考_设计.md §三): the chapter button sits the exam, and the answers
    /// play one by one — the question, what it types, ✓ or ×. Six right passes and 「继续」 turns the chapter (the
    /// emergence moment follows); a fail lists why each wrong one was wrong and waits a few rounds. The rules are in
    /// XgSim.AbilityExam.cs; every answer is settled before the first one shows.
    /// </summary>
    public sealed partial class XgTrainPage
    {
        const float ExamRowSeconds = 1.05f, ExamTypeStart = .3f, ExamTypeSeconds = .45f, ExamMarkAt = .85f;

        RectTransform examLayer, examCard, examResultArea;
        TMP_Text examKicker, examTitle, examScore, examResult, examSkip;
        XgBtn examGo;
        readonly List<ExamRow> examRows = new List<ExamRow>();
        XgExamSheet examShown;
        float examT = -1;
        int examMarked;
        bool examFinished;

        sealed class ExamRow
        {
            public RectTransform rt, markRt;
            public Image bg;
            public TMP_Text number, question, answer, mark;
        }

        void BuildExam()
        {
            examLayer = Rect("ExamLayer", (RectTransform)root.parent.parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Panel(examLayer, new Color(0, 0, 0, .7f));
            // A click on the shade skips to the end of the answers.
            examLayer.gameObject.AddComponent<Button>().onClick.AddListener(SkipExam);
            examCard = ui.Card(examLayer, "AbilityExam", new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-460, -275), new Vector2(460, 275));
            examCard.gameObject.AddComponent<Button>().onClick.AddListener(SkipExam);
            examKicker = ui.Text(Strip("Kicker", examCard, 12, 18, 24, 24), "", 13, XgDark.Money, TextAlignmentOptions.MidlineLeft);
            examTitle = ui.Text(Strip("Title", examCard, 30, 32, 24, 220), "", 22, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            examTitle.fontStyle = FontStyles.Bold;
            examScore = ui.Text(Rect("Score", examCard, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-230, -62), new Vector2(-24, -14)), "", 16, XgDark.Ink, TextAlignmentOptions.MidlineRight);
            examScore.textWrappingMode = TextWrappingModes.NoWrap;
            for (int i = 0; i < XgSim.AbilityExamQuestions; i++)
            {
                var row = new ExamRow();
                row.rt = Strip("Q" + (i + 1), examCard, 72 + i * 47, 43, 24, 24);
                row.bg = Panel(row.rt, XgDark.Panel); row.bg.raycastTarget = false;
                row.number = ui.Text(Rect("No", row.rt, Vector2.zero, new Vector2(0, 1), new Vector2(8, 0), new Vector2(30, 0)), (i + 1) + ".", 13, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
                row.question = ui.Text(Rect("Question", row.rt, Vector2.zero, new Vector2(.56f, 1), new Vector2(34, 2), new Vector2(-6, -2)), "", 13, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
                row.question.enableAutoSizing = true; row.question.fontSizeMin = 9; row.question.fontSizeMax = 13;
                row.question.overflowMode = TextOverflowModes.Truncate;
                row.answer = ui.Text(Rect("Answer", row.rt, new Vector2(.56f, 0), Vector2.one, new Vector2(4, 2), new Vector2(-42, -2)), "", 13, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
                row.answer.enableAutoSizing = true; row.answer.fontSizeMin = 9; row.answer.fontSizeMax = 13;
                row.answer.overflowMode = TextOverflowModes.Truncate;
                row.markRt = Rect("Mark", row.rt, new Vector2(1, 0), Vector2.one, new Vector2(-38, 0), new Vector2(-6, 0));
                row.mark = ui.Text(row.markRt, "", 22, XgDark.Muted, TextAlignmentOptions.Center);
                row.mark.fontStyle = FontStyles.Bold;
                examRows.Add(row);
            }
            examResultArea = Rect("Result", examCard, Vector2.zero, new Vector2(1, 0), new Vector2(24, 12), new Vector2(-24, 64));
            examResult = ui.Text(Rect("Text", examResultArea, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-220, 0)), "", 15, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            examResult.enableAutoSizing = true; examResult.fontSizeMin = 11; examResult.fontSizeMax = 15;
            examGo = ui.Button(examResultArea, "", ExamDone, 16);
            examGo.rt.gameObject.name = "ExamContinue";
            examGo.rt.anchorMin = new Vector2(1, 0); examGo.rt.anchorMax = new Vector2(1, 1);
            examGo.rt.offsetMin = new Vector2(-200, 8); examGo.rt.offsetMax = new Vector2(0, -8);
            examGo.label.fontStyle = FontStyles.Bold;
            examSkip = ui.Text(Rect("Skip", examCard, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-260, 64), new Vector2(-24, 84)), T("点一下跳过", "Click to skip"), 11, XgDark.Dim, TextAlignmentOptions.MidlineRight);
            examLayer.gameObject.SetActive(false);
        }

        /// <summary>The chapter button: sit the exam, or claim a pass that waits.</summary>
        void ChapterButton()
        {
            if (Sim.ExamPassPending) { if (Sim.ClaimExamPass()) { Fx.Play(XgJuice.Sfx.Id.Unlock); view.Refresh(true); } return; }
            var sheet = Sim.TakeExam();
            if (sheet == null) { Fx.Play(XgJuice.Sfx.Id.Thud); return; }
            ShowExam(sheet);
        }

        /// <summary>The chapter button's label and state, under the two bars.</summary>
        void RefreshChapterButton()
        {
            string name = XgSim.AbilityName(Sim.NextAbility, Sim.English);
            if (Sim.CardLoanHolds && (Sim.ExamReady || Sim.ExamPassPending))
                confirmStage.Set(Sim.S.clStep == 0 ? T("老周在 YY 上找你……", "Lao Zhou is looking for you on YY…") : T("先回老周：借不借卡", "Answer Lao Zhou first: lend the card or not"), false);
            else if (Sim.PhoneSortHolds && (Sim.ExamReady || Sim.ExamPassPending))
                confirmStage.Set(T("先帮阿杰分完那 " + XgSim.PhoneTotal.ToString("#,0") + " 张图", "Sort Ajie's " + XgSim.PhoneTotal.ToString("#,0") + " pictures first"), false);
            else if (Sim.YnNightHolds && (Sim.ExamReady || Sim.ExamPassPending))
                // 是否夜 (XgSim.YesNoNight.cs) first: the chapter waits for it.
                confirmStage.Set(Sim.YnNightActive ? T("它在 YY 里答题……", "It is answering questions on YY…") : T("先去 YY 考考它：它还只会是和否", "Test it on YY first: it only knows yes and no"), false);
            else if (Sim.ExamPassPending)
                confirmStage.Set(T("能力考过了 · 继续 →", "Exam passed · continue →"), true, XgDark.Accent, Color.white);
            else if (Sim.ExamReady)
                confirmStage.Set(T("参加能力考 ·「" + name + "」", "Sit the exam · " + name), true, new Color32(74, 52, 12, 255), XgDark.Money);
            else if (Sim.ExamRoundsToRetry > 0 && Sim.ExamParamsReady >= XgSim.ExamOpenShare && Sim.ExamSamplesReady >= XgSim.ExamOpenShare)
                confirmStage.Set(T("没考过：再练 " + Sim.ExamRoundsToRetry + " 轮可以重考", "Failed: train " + Sim.ExamRoundsToRetry + " more rounds to sit it again"), false);
            else
                confirmStage.Set(T("还考不了「" + name + "」：接着练，接着喂数据", "Not ready for " + name + ": keep training, keep feeding it data"), false);
        }

        void ShowExam(XgExamSheet sheet)
        {
            examShown = sheet;
            examT = 0; examMarked = 0; examFinished = false;
            examLayer.gameObject.SetActive(true);
            examLayer.SetAsLastSibling();
            view.Juice.BringToFront();
            examKicker.text = T("能力考 · 第 " + sheet.attempt + " 次", "Ability exam · attempt " + sheet.attempt);
            examTitle.text = T("第 " + sheet.ability + " 项能力：「" + XgSim.AbilityName(sheet.ability, false) + "」", "Ability " + sheet.ability + ": " + XgSim.AbilityName(sheet.ability, true));
            examResult.text = "";
            examGo.Show(false);
            examSkip.gameObject.SetActive(true);
            for (int i = 0; i < examRows.Count; i++) examRows[i].rt.gameObject.SetActive(false);
            Fx.Play(XgJuice.Sfx.Id.Whoosh, .9f, .6f);
            UpdateExamScore();
        }

        void SkipExam() { if (examShown != null && examT >= 0 && !examFinished) examT = ExamEnd; }

        float ExamEnd => examShown == null ? 0 : (examShown.items.Count - 1) * ExamRowSeconds + ExamMarkAt + .05f;

        void UpdateExamScore()
        {
            int right = 0;
            for (int i = 0; i < examMarked && i < examShown.items.Count; i++) if (examShown.items[i].right) right++;
            examScore.text = T("答对 ", "Right ") + Hex(XgDark.Good) + "<b>" + right + "</b></color> / " + examShown.items.Count
                + Hex(XgDark.Muted) + T("   及格 ", "   pass ") + XgSim.AbilityExamPass + "</color>";
        }

        /// <summary>Plays the answers one by one (called from <see cref="Tick"/>).</summary>
        void TickExam(float dt)
        {
            if (examShown == null || examT < 0 || !examLayer.gameObject.activeSelf) return;
            examT += dt;
            bool done = examFinished;
            for (int i = 0; i < examShown.items.Count && i < examRows.Count; i++)
            {
                var item = examShown.items[i]; var row = examRows[i];
                float t = examT - i * ExamRowSeconds;
                bool on = t >= 0;
                if (row.rt.gameObject.activeSelf != on) row.rt.gameObject.SetActive(on);
                if (!on) continue;
                row.question.text = item.Question(Sim.English);
                string said = item.Said(Sim.English);
                float k = Mathf.Clamp01((t - ExamTypeStart) / ExamTypeSeconds);
                int n = Mathf.RoundToInt(said.Length * k);
                bool marked = t >= ExamMarkAt;
                Color c = !marked ? XgDark.Ink : item.right ? XgDark.Good : XgDark.Bad;
                row.answer.text = Hex(XgDark.Muted) + T("它答 ", "It says ") + "</color>" + Hex(c) + said.Substring(0, n) + "</color>"
                    + (marked && !item.right && done ? "\n<size=80%>" + Hex(XgDark.Muted) + T(item.reasonZh, item.reasonEn) + "</color></size>" : "");
                row.mark.text = marked ? (item.right ? "✓" : "×") : "";
                row.mark.color = c;
                row.bg.color = !marked ? XgDark.Panel : item.right ? (Color)new Color32(13, 32, 30, 255) : new Color32(38, 18, 22, 255);
                if (marked && i >= examMarked)
                {
                    examMarked = i + 1;
                    UpdateExamScore();
                    // Only the marks reached in time ring (a skip lands them all at once, silently).
                    if (t - ExamMarkAt < .2f && !done)
                    {
                        Fx.Play(item.right ? XgJuice.Sfx.Id.Ding : XgJuice.Sfx.Id.Thud, item.right ? 1 + .04f * i : .8f, .6f);
                        Fx.Knock(row.markRt, .3f, default, .25f, true);
                    }
                }
            }
            if (!examFinished && examT >= ExamEnd) FinishExamShow();
        }

        void FinishExamShow()
        {
            examFinished = true;
            Sim.RevealExam();
            examMarked = examShown.items.Count;
            UpdateExamScore();
            examSkip.gameObject.SetActive(false);
            examGo.Show(true);
            if (examShown.passed)
            {
                examResult.text = Hex(XgDark.Gold) + "<b>" + T("过了！", "Passed!") + "</b></color> " + T("答对 " + examShown.rightCount + " 道。", examShown.rightCount + " right.")
                    + Hex(XgDark.Muted) + T("  它学会「" + XgSim.AbilityName(examShown.ability, false) + "」了。", "  It has learned " + XgSim.AbilityName(examShown.ability, true) + ".") + "</color>";
                examGo.Set(T("继续 →", "Continue →"), true, XgDark.Accent, Color.white);
                Fx.Play(XgJuice.Sfx.Id.Fanfare, 1, .8f);
                Fx.Burst(Fx.At(examScore.rectTransform), 40, XgDark.Gold, XgJuice.Shape.Star, 420, 300, 1.1f);
            }
            else
            {
                examResult.text = Hex(XgDark.Bad) + "<b>" + T("没过。", "Not yet.") + "</b></color> "
                    + T("答对 " + examShown.rightCount + " 道，要 " + XgSim.AbilityExamPass + " 道。错题下面写着差在哪；再练 " + XgSim.ExamRetryRounds + " 轮可以重考，不扣钱。",
                        examShown.rightCount + " right, " + XgSim.AbilityExamPass + " needed. Under each wrong one is what it lacks; train " + XgSim.ExamRetryRounds + " more rounds to sit it again, at no cost.");
                examGo.Set(T("回去练", "Back to training"), true, XgDark.Button, XgDark.Ink);
                Fx.Play(XgJuice.Sfx.Id.Thud, .7f, .6f);
            }
        }

        /// <summary>「继续」 after a pass turns the chapter (the emergence moment takes over); after a fail, back to training.</summary>
        void ExamDone()
        {
            bool passed = examShown != null && examShown.passed;
            examLayer.gameObject.SetActive(false);
            examShown = null; examT = -1;
            if (passed && Sim.ClaimExamPass()) Fx.Play(XgJuice.Sfx.Id.Unlock);
            view.Refresh(true);
        }

        public bool ExamOpen => examLayer != null && examLayer.gameObject.activeSelf;
    }
}
