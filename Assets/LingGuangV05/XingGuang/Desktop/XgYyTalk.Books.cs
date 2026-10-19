using System;
using LingGuangV05.Core;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEngine;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    public sealed partial class XgYyTalk
    {
        bool composingBookNotice;
        string pendingBookToast;
        ChapterOneSim bookNoticeSave;
        float nextBookSaveAttempt;

        bool DeferredBookCompletionLine(XgChatLine line)
        {
            if (line.from == "me" || bound == null) return false;
            foreach (var completion in bound.S.bookCompletions)
                if (!completion.acknowledged && completion.stage < XgSim.BookAskStage
                    && line.text.StartsWith(bound.BookCompletionText(completion), StringComparison.Ordinal)) return true;
            return false;
        }
        void ResetBookNotices()
        {
            pendingBookToast = null; bookNoticeSave = null; composingBookNotice = false; nextBookSaveAttempt = 0;
        }
        void TickBookNotices(YYChatHub hub)
        {
            var runtime = controller.runtime;
            if (runtime?.Sim == null || !ReferenceEquals(hub.runtime?.Sim, runtime.Sim) || !hub.BoundTo(runtime.Sim)
                || !controller.BoundTo(runtime.Sim) || !controller.LifeEventSettingsAvailable || runtime.Sim.InPrologue || !hub.S.lingguangUnlocked) return;
            if (StoryFocus.Active || XgStageCurtain.Busy || controller.View?.SetupShowing == true || controller.View?.Holo?.Showing == true
                || InnerVoice.Busy || AiJoinsYy.Playing || NightPlaying || waiting
                || LoveQuestionCutscene.Playing || controller.View?.Train?.ExamOpen == true
                || controller.View?.Train?.DiagnosisOpen == true || controller.View?.Train?.AssessmentBlocksInput == true) return;
            if (pendingBookToast != null)
            {
                if (!ReferenceEquals(bookNoticeSave, runtime.Sim)) { ResetBookNotices(); return; }
                if (Time.unscaledTime < nextBookSaveAttempt) return;
                nextBookSaveAttempt = Time.unscaledTime + 10;
                if (!PersistBookNotice()) return;
                if (pendingBookToast.Length > 0) hub.NotifyBookCompletion(bookNoticeSave, pendingBookToast);
                ResetBookNotices(); return;
            }
            var completion = bound.PendingBookCompletion;
            if (completion == null) return;
            var book = XgBooks.Get(completion.bookId);
            if (book == null) return;
            // One answer owner at a time. Later completed books stay durable until the existing question is answered.
            if (completion.stage >= XgSim.BookAskStage && bound.BookPuzzlePending != null && bound.BookPuzzleBook?.id != book.id) return;
            if (completion.stage >= XgSim.BookAskStage && bound.S.redMansionBeat == 1 && book.id != XgSim.RedMansionId) return;
            string text = bound.BookCompletionText(completion);
            if (completion.stage < XgSim.BookAskStage)
            {
                // Preserve the recorded completion-stage vocabulary even if a queued notice is delivered later.
                var ch = book.chapters[book.chapters.Length - 1];
                var words = completion.stage <= 2 ? (bound.English && ch.namesEn.Length > 0 ? ch.namesEn : ch.names)
                    : (bound.English && ch.wordsEn.Length > 0 ? ch.wordsEn : ch.words);
                if (words.Length > 0) text += "\n" + (completion.stage <= 2 ? words[0] + "?" : string.Join(" / ", words));
            }
            else if (bound.BookPuzzlePending == null && bound.S.redMansionBeat != 1)
            {
                foreach (var puzzle in XgBooks.Puzzles(book.id))
                {
                    if (puzzle.chapter >= bound.ChaptersRead(book) || bound.PuzzleAsked(puzzle.id)) continue;
                    composingBookNotice = true;
                    try { bound.AskBookPuzzle(puzzle); }
                    finally { composingBookNotice = false; }
                    if (bound.S.bookPuzzle == puzzle.id) text += "\n" + T(puzzle.q, puzzle.qEn);
                    break;
                }
            }
            if (book.id == XgSim.RedMansionId && bound.S.redMansionBeat == 1)
                text += "\n" + T("后面呢？", "What comes next?");
            if (!hub.RecordBookCompletion(runtime.Sim, completion.key, text, out bool added)) return;
            bound.AcknowledgeBookCompletion(completion.key);
            runtime.MarkDirty(); bookNoticeSave = runtime.Sim;
            // A single save transaction contains the YY receipt, acknowledgement and pending question.
            if (PersistBookNotice())
            {
                if (added) hub.NotifyBookCompletion(bookNoticeSave, text);
                bookNoticeSave = null;
            }
            else
            {
                pendingBookToast = added ? text : "";
                nextBookSaveAttempt = Time.unscaledTime + 10;
            }
        }
        bool PersistBookNotice()
            => controller.runtime.TestMode || !controller.runtime.useDiskSave || controller.SaveNow();

        string BookContinuationChoice => T("后四十回是别人续的。", "The last forty chapters are a continuation.");

        bool HeardRedMansion(XgSim sim, string text)
        {
            if (!ReferenceEquals(controller.Sim, sim) || !controller.BoundTo(controller.runtime?.Sim)) return false;
            if (sim.S.redMansionBeat != 1 || text != sim.RedMansionAnswerText && text != BookContinuationChoice) return false;
            Echo(() => sim.AddLine("me", text));
            string reply = sim.AnswerRedMansion();
            if (!string.IsNullOrEmpty(reply)) sim.AddLine("ai", reply);
            controller.runtime.MarkDirty(); controller.SaveNow(); return true;
        }
    }
}
