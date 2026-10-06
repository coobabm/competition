using System;
using System.Collections.Generic;
using LingGuangV05.Core.Story;

namespace LingGuangV05.Core
{
    /// <summary>
    /// The game's side of the story contract: which facts beats may read and which signals they may wait on.
    /// Hand-written tables, no reflection. Add a name here before using it in story JSON; content lint enforces it.
    /// </summary>
    public sealed class ChapterOneStoryVars : IStoryVars, IStoryVarsWriter
    {
        public static readonly string[] Signals =
        {
            // host lifecycle
            "boot", "state",
            // simulation (ChapterOneSim.Signal)
            "card.resolved", "exam.started", "exam.passed", "exam.failed",
            "gpu.bought", "gpu.sold", "case.bought", "bill.paid", "power.unpaid", "breaker.tripped", "breaker.reset",
            "skill.upgraded", "node.added", "node.removed", "edge.added", "edge.removed",
            "job.enabled", "job.disabled", "day.ended",
            // runtime and desktop
            "name.set", "app.open", "app.installed", "chat.sent", "debug",
            // 灵光 lab milestones (XingGuangController, first time each)
            "lg.trainable", "lg.epoch", "lg.assess", "lg.checkpoint", "lg.gradeA", "lg.gradeS", "lg.auto", "lg.autotrain", "lg.contract", "lg.nan", "lg.combo50",
            "story.delivered", "lg.paidAssessment", "lg.migration", "lg.unsure", "lg.wall", "lg.breakthrough", "lg.stage", "lg.project", "lg.ending", "lg.month", "prologue.done",
            // story-internal
            "chapter.up", "story.next",
        };

        private static readonly string[] Names =
        {
            "chapter", "day", "money", "samples", "steps", "learningPoints", "gameSeconds", "totalEarned", "temperature", "billDue",
            "cardsReviewed", "correctedCards", "gpuCount", "caseCount", "semanticLevel", "trainingLevel",
            "examAttempts", "examActive", "lastExamPassed", "chapterOneComplete",
            "jobEnabled", "breakerTripped", "unpaidPower", "powered",
            "accuracy", "heartbeatsPerSecond", "incomePerSecond", "hasOutputPath", "canStartExam",
            "labCaptcha", "labMeme", "labGo", "labPoems", "labNews", "labTranslate", "labUnsure", "labStage", "labVisionStage", "labSequenceStage", "labProjectActive", "labCanProject", "labExperiment", "labEndingAnswered", "labMigrated", "labStageEpochs", "labChatTurns", "labGarble",
            "hiddenNodes", "edges", "memoryUsed", "memoryRatio", "fanOutCount", "convergeCount", "named", "appInstalled", "prologueDone",
        };

        private readonly Func<ChapterOneSim> sim;
        private readonly Func<string, double?> labFacts;

        /// <param name="simulation">Getter, so a reloaded or reset save is picked up automatically.</param>
        public ChapterOneStoryVars(Func<ChapterOneSim> simulation, Func<string, double?> lab = null) { sim = simulation; labFacts = lab; }

        public IEnumerable<string> KnownNames { get { return Names; } }

        public static bool IsKnownSignal(string signal)
        {
            if (signal == StoryEngine.AnySignal) return true;
            int colon = signal.IndexOf(':');
            string name = colon >= 0 ? signal.Substring(0, colon) : signal;
            return Array.IndexOf(Signals, name) >= 0;
        }

        public bool TryGet(string name, out double value)
        {
            value = 0;
            if (Array.IndexOf(Names, name) >= 0 && name.StartsWith("lab", StringComparison.Ordinal))
            {
                double? fact = labFacts == null ? null : labFacts(name);
                if (fact.HasValue && !double.IsNaN(fact.Value) && !double.IsInfinity(fact.Value)) value = fact.Value;
                return true;
            }
            var s = sim();
            if (s == null) return false;
            var S = s.S;
            switch (name)
            {
                case "chapter": value = S.chapter; return true;
                case "day": value = S.day; return true;
                case "money": value = S.money; return true;
                case "samples": value = S.samples; return true;
                case "steps": value = S.steps; return true;
                case "learningPoints": value = S.learningPoints; return true;
                case "gameSeconds": value = S.gameSeconds; return true;
                case "totalEarned": value = S.totalEarned; return true;
                case "temperature": value = S.temperature; return true;
                case "billDue": value = S.billDue; return true;
                case "cardsReviewed": value = S.cardsReviewed; return true;
                case "correctedCards": value = S.correctedCards; return true;
                case "gpuCount": value = S.gpuCount; return true;
                case "caseCount": value = S.caseCount; return true;
                case "semanticLevel": value = S.semanticLevel; return true;
                case "trainingLevel": value = S.trainingLevel; return true;
                case "examAttempts": value = S.examAttempts; return true;
                case "examActive": value = S.examActive ? 1 : 0; return true;
                case "lastExamPassed": value = S.lastExamPassed ? 1 : 0; return true;
                case "chapterOneComplete": value = S.chapterOneComplete ? 1 : 0; return true;
                case "jobEnabled": value = S.jobEnabled ? 1 : 0; return true;
                case "breakerTripped": value = S.breakerTripped ? 1 : 0; return true;
                case "unpaidPower": value = S.unpaidPower ? 1 : 0; return true;
                case "powered": value = !S.breakerTripped && !S.unpaidPower ? 1 : 0; return true;
                case "accuracy": value = s.Accuracy; return true;
                case "heartbeatsPerSecond": value = s.HeartbeatsPerSecond; return true;
                case "incomePerSecond": value = s.IncomePerSecond; return true;
                case "hasOutputPath": value = s.HasOutputPath ? 1 : 0; return true;
                case "canStartExam": value = s.CanStartExam ? 1 : 0; return true;
                case "hiddenNodes":
                    int hidden = 0;
                    for (int i = 0; i < S.nodes.Count; i++) if (S.nodes[i].kind == NodeKind.Hidden) hidden++;
                    value = hidden; return true;
                case "edges": value = S.edges.Count; return true;
                case "memoryUsed": value = s.MemoryUsed; return true;
                case "memoryRatio": value = s.MemoryCapacity > 0 ? s.MemoryUsed / s.MemoryCapacity : 0; return true;
                case "fanOutCount": value = s.FanOutCount; return true;
                case "convergeCount": value = s.ConvergeCount; return true;
                case "appInstalled": value = s.AppInstalled ? 1 : 0; return true;
                case "prologueDone": value = s.InPrologue ? 0 : 1; return true;
                case "named": value = string.IsNullOrEmpty(S.aiName) || S.aiName == "the code" ? 0 : 1; return true;
                default: return false;
            }
        }

        /// <summary>Story may not edit simulation numbers in chapter one; flags cover its needs.</summary>
        public bool TrySet(string name, double value) { return false; }
    }
}
