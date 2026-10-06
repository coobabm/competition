using System;
using System.Collections.Generic;
using LingGuangV05.Core.Story;
using LingGuangV05.Runtime.Story;
using LingGuangV05.XingGuang;
using UnityEngine;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>Maps the pure lab's durable milestones onto the existing story host. No economy is edited here.</summary>
    public sealed class XgStoryBridge : MonoBehaviour
    {
        XingGuangController controller;
        StoryDirector director;
        XgSim bound;
        StoryRunner runner;
        readonly HashSet<string> relayed = new HashSet<string>();
        bool migrationApplied;

        public void Bind(XingGuangController owner) { controller = owner; }
        void Update()
        {
            if (controller == null || controller.Sim == null || controller.runtime == null) return;
            if (director == null) director = GetComponent<StoryDirector>();
            if (director == null) return;
            director.LabFacts = ReadFact;
            director.LabText = ReadText;
            if (!ReferenceEquals(bound, controller.Sim)) Rebind(controller.Sim);
            if (!ReferenceEquals(runner, director.Runner))
            {
                if (runner != null) runner.BeatFinished -= OnBeatFinished;
                runner = director.Runner;
                if (runner != null) runner.BeatFinished += OnBeatFinished;
                relayed.Clear(); migrationApplied = false;
            }
            if (runner == null || !director.Active) return;
            ApplyMigration();
            RecoverPendingSignals();
        }

        void Rebind(XgSim sim)
        {
            Unsubscribe(); bound = sim; relayed.Clear(); migrationApplied = false;
            bound.WallObserved += OnWall;
            bound.BreakthroughDone += OnBreakthrough;
            bound.ProjectExperiment += OnExperiment;
            bound.ChapterFinished += OnEnding;
            bound.Assessed += OnAssessed;
            bound.Emerged += OnEmerged;
            bound.StageAdvanced += OnStageAdvanced;
            controller.runtime.Signal += OnRuntimeSignal;
        }
        void Unsubscribe()
        {
            if (bound != null)
            {
                bound.WallObserved -= OnWall; bound.BreakthroughDone -= OnBreakthrough;
                bound.ProjectExperiment -= OnExperiment; bound.ChapterFinished -= OnEnding; bound.Assessed -= OnAssessed; bound.Emerged -= OnEmerged;
                bound.StageAdvanced -= OnStageAdvanced;
            }
            if (controller != null && controller.runtime != null) controller.runtime.Signal -= OnRuntimeSignal;
        }
        void OnDestroy()
        {
            Unsubscribe();
            if (runner != null) runner.BeatFinished -= OnBeatFinished;
            if (director != null) { director.LabFacts = null; director.LabText = null; }
        }
        void EmitOnce(string signal, string arg = null)
        {
            if (director == null || !director.Active) return;
            if (relayed.Add(signal + ":" + arg)) controller.runtime.RaiseSignal(signal, arg);
        }
        void OnWall(string wall) { EmitOnce("lg.wall", wall); }
        void OnBreakthrough(string id)
        {
            // Old saves still finish the old research project; since design v1.1 stage 6 and the finale follow instead.
            if (id == "transformer") { if (bound.EndingAvailable) EmitOnce("lg.project", "completed"); }
            else EmitOnce("lg.breakthrough", id);
            EmitOnce("lg.stage", bound.S.stage.ToString());
            controller.runtime.MarkDirty();
        }
        void OnEmerged(int stage, string line) { EmitOnce("lg.emerge", stage.ToString()); controller.runtime.MarkDirty(); }
        /// <summary>The calendar turns to the new stage's month (month beats outrank the breakthrough lines raised in the same frame).</summary>
        void OnStageAdvanced(int from) { EmitOnce("lg.month", (from + 1).ToString()); }
        void OnExperiment(int index)
        {
            // A failed experiment may be asked again; the card state, not the one-shot story line, owns retries.
            controller.runtime.RaiseSignal("lg.project", index.ToString());
            controller.runtime.MarkDirty();
        }
        void OnEnding(bool regret) { EmitOnce("lg.ending", regret ? "yes" : "no"); controller.runtime.MarkDirty(); }
        void OnAssessed(XgAssessment assessment)
        { if (assessment != null && assessment.reward + assessment.gradeBonus > 0) EmitOnce("lg.paidAssessment"); }
        void OnRuntimeSignal(string signal, string arg)
        {
            // Hidden 下班以后 cards from the hardware shop (XgSim.Cards.cs).
            if (bound != null && signal == "gpu.bought" && arg == LingGuangV05.Core.Hardware.HardwareCatalog.TitanXp) bound.EarnSecret("life.hw.titan");
            if (bound != null && signal == "breaker.tripped" && controller.runtime.Sim.S.pcieBurned) bound.EarnSecret("life.hw.burn");
            if (signal != "story.delivered" || string.IsNullOrEmpty(arg) || bound == null) return;
            bound.MarkIntroDelivered(arg);
            if (arg.StartsWith("wall_", StringComparison.Ordinal)) bound.MarkWallExplained(arg.Substring(5));
            controller.runtime.Sim.S.story.SetFlag("delivered." + arg, 1);
            controller.runtime.MarkDirty();
        }
        void OnBeatFinished(StoryBeat beat)
        {
            if (bound == null || beat == null) return;
            if (beat.Id.StartsWith("wall_", StringComparison.Ordinal) && XgStoryMilestones.WallMayOpen(bound.S, beat.Id.Substring(5), controller.runtime.Sim.S.story.GetFlag("delivered." + beat.Id) > 0)) bound.MarkWallExplained(beat.Id.Substring(5));
            controller.runtime.MarkDirty();
        }
        void ApplyMigration()
        {
            if (migrationApplied) return;
            migrationApplied = true;
            var story = controller.runtime.Sim.S.story;
            foreach (string beat in bound.S.migratedBeats)
                if (director.Library.Get(beat) != null) story.MarkFired(beat);
            if (bound.S.migratedBeats.Count > 0)
            {
                bound.S.trainingIntroDelivered = true;
                bound.S.treeIntroDelivered = true;
            }
            // A queued-but-undelivered introductory line must be replayable after quitting.
            if (!bound.S.trainingIntroDelivered) story.Unfire("lz_trainable");
            if (!bound.S.treeIntroDelivered) story.Unfire("lz_tree");
            foreach (string wall in bound.S.walls)
                if (!bound.WallExplained(wall) && story.GetFlag("delivered.wall_" + wall) <= 0) story.Unfire("wall_" + wall);
        }
        void RecoverPendingSignals()
        {
            var story = controller.runtime.Sim.S.story;
            foreach (string wall in bound.S.walls)
                if (!bound.WallExplained(wall))
                {
                    if (story.GetFlag("delivered.wall_" + wall) > 0) bound.MarkWallExplained(wall);
                    else EmitOnce("lg.wall", wall);
                }
            foreach (int stage in XgStoryMilestones.MigrationStages(bound.S))
                if (!story.HasFired("migration_era_" + stage)) EmitOnce("lg.migration", stage.ToString());
            if (bound.S.uncertaintyObserved && !story.HasFired("ll_unsure")) EmitOnce("lg.unsure");
            // The month card of the stage the save is in: on install (June), after a quit mid-turn, or for older saves once.
            if (controller.runtime.Sim.AppInstalled && !controller.runtime.Sim.InPrologue && !story.HasFired("month_" + bound.S.stage)) EmitOnce("lg.month", bound.S.stage.ToString());
            foreach (string node in XgStoryMilestones.Pending(bound.S, story.HasFired)) EmitOnce("lg.breakthrough", node);
            if (!bound.S.trainingIntroDelivered && (bound.TrainingUnlocked(XgTrack.Vision) || bound.TrainingUnlocked(XgTrack.Sequence))) EmitOnce("lg.trainable");
            if (bound.S.paidAssessmentReached && !bound.S.treeIntroDelivered) EmitOnce("lg.paidAssessment");
            if (bound.ProjectAwaitingAnswer) EmitOnce("lg.project", (bound.S.project.experiments + 1).ToString());
            if (bound.EndingAvailable && !story.HasFired("ending_reject")) EmitOnce("lg.project", "completed");
            if (bound.EndingAvailable && story.HasFired("ending_reject") && story.GetFlag("delivered.ending_card") <= 0)
            { story.Unfire("ending_card"); EmitOnce("story.next", "ending_card"); }
            if (bound.S.chapterComplete && story.GetFlag("chapter_one_ending_shown") <= 0) EmitOnce("lg.ending", bound.S.endingRegret ? "yes" : "no");
        }
        string ReadText(string name)
        {
            if (name.StartsWith("migration", StringComparison.Ordinal) && name.Length == 10)
            {
                int stage;
                if (int.TryParse(name.Substring(9), out stage))
                {
                    string[] candidates = stage == 2 ? new[] { "mlp" } : stage == 3 ? new[] { "lenet", "alexnet", "vgg", "googlenet", "rnn" }
                        : stage == 4 ? new[] { "lstm", "gru", "resnet" } : new[] { "seq2seq", "attention", "caption" };
                    var owned = new List<string>();
                    foreach (string id in candidates)
                    {
                        var arch = XgCatalog.Arch(id);
                        if (arch != null && controller.Sim.Has(id)) owned.Add((LingGuangV05.Runtime.GameText.IsEnglish ? arch.nameEn : arch.name) + " · " + arch.year);
                    }
                    return (LingGuangV05.Runtime.GameText.IsEnglish ? "Preserved: " : "已保留：") + string.Join(" / ", owned) +
                        (LingGuangV05.Runtime.GameText.IsEnglish ? "\nNo duplicate fees or story replay." : "\n没有重复收费，也不会重播旧剧情。");
                }
            }
            string story = StoryText(name);
            if (story != null) return story;
            if (name != "rememberedLine") return null;
            var yy = LingGuangV05.Desktop.YY.YYChatHub.Instance;
            var conversation = yy != null ? yy.Conversation("lingguang") : null;
            if (conversation != null)
                foreach (var message in conversation.messages)
                    if (message.from == LingGuangV05.Desktop.YY.YYChatHub.Me && !string.IsNullOrWhiteSpace(message.text))
                    {
                        string quote = message.text.Length > 32 ? message.text.Substring(0, 32) : message.text;
                        return LingGuangV05.Runtime.GameText.IsEnglish ? "You said: “" + quote + "”. I remember." : "你之前说：“" + quote + "”。我记得。";
                    }
            return LingGuangV05.Runtime.GameText.IsEnglish ? "We have not talked much yet. I can remember more now." : "我们还没说过几句话。现在，我能记住更多了。";
        }
        /// <summary>Lines of the stage 2 / 4 / 5 story that depend on the save (Chinese only; see CLAUDE.md).</summary>
        string StoryText(string name)
        {
            var sim = controller != null ? controller.Sim : null;
            if (sim == null) return null;
            switch (name)
            {
                case "aiName": return sim.Profile.name.Length > 0 ? sim.Profile.name : LingGuangV05.Core.AppNames.AiZh;
                case "firstChoicePick": return sim.FirstTrack == "vision" ? "[阿杰] 它点了 A。哈，先学看图！" : "[小刚] 它点了 B，读弹幕。我就说吧。";
                case "firstDayAnswer":
                    int answer = sim.FirstDayAnswer;
                    return answer > 0 ? "你教我：是。" : answer < 0 ? "你教我：否。" : "你没教。我自己亮了「否」。";
                case "wishQuote":
                    string wish = sim.Profile.personality ?? "";
                    if (wish.Length == 0) return "你什么都没写。";
                    return "「" + (wish.Length > 18 ? wish.Substring(0, 18) + "……" : wish) + "」";
                case "wishVerdict": return sim.Profile.personality.Length == 0 ? "那我就长成你喂的样子。" : sim.WishDrifted ? "不太像。我被喂歪了。" : "我在学。";
                case "garble1": return sim.GarbleLine(0);
                case "garble2": return sim.GarbleLine(1);
                case "garble3": return sim.GarbleLine(2);
                case "garble4": return sim.GarbleLine(3);
                default: return null;
            }
        }

        double? ReadFact(string name)
        {
            var sim = controller != null ? controller.Sim : null;
            if (sim == null) return null;
            switch (name)
            {
                case "labCaptcha": return sim.DeskOpen("cifar") ? 1 : 0;
                case "labMeme": return sim.DeskOpen("meme") ? 1 : 0;
                case "labGo": return sim.DeskOpen("go") ? 1 : 0;
                case "labPoems": return sim.DeskOpen("poems") ? 1 : 0;
                case "labNews": return sim.DatasetAvailable("news") ? 1 : 0;
                case "labTranslate": return sim.DeskOpen("translate") ? 1 : 0;
                case "labUnsure": return sim.S.uncertaintyObserved ? 1 : 0;
                case "labStage": return sim.S.stage;
                case "labVisionStage": return sim.S.stageVision;
                case "labSequenceStage": return sim.S.stageSequence;
                case "labProjectActive": return sim.ProjectActive ? 1 : 0;
                case "labExperiment": return sim.S.project.experiments;
                case "labEndingAnswered": return sim.S.chapterComplete ? 1 : 0;
                case "labMigrated": return sim.S.migratedBeats.Count > 0 ? 1 : 0;
                case "labCanProject": return sim.HasEmerged(5) && !sim.Has("transformer") ? 1 : 0;
                case "labStageEpochs": return sim.S.stageEpochs;
                case "labChatTurns": return sim.S.chatTurns;
                case "labGarble": return sim.GarbleLinesRead;
                default: return null;
            }
        }
    }
}
