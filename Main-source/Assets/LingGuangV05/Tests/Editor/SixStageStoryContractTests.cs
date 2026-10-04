using System;
using System.Linq;
using System.IO;
using LingGuangV05.Core.Story;
using LingGuangV05.Core;
using NUnit.Framework;

namespace LingGuangV05.Tests
{
    public sealed class SixStageStoryContractTests
    {
        [Test]
        public void StageSignalsAndFactsAreRegisteredWithoutDependingOnLabAssembly()
        {
            var vars = new ChapterOneStoryVars(() => new ChapterOneSim());
            foreach (var signal in new[] { "lg.unsure", "lg.wall:combo", "lg.breakthrough:bt.hidden", "lg.stage:2", "lg.project:1", "lg.ending:no" })
                Assert.That(ChapterOneStoryVars.IsKnownSignal(signal), Is.True, signal);
            foreach (var name in new[] { "labStage", "labVisionStage", "labSequenceStage", "labProjectActive", "labExperiment", "labEndingAnswered", "labMigrated" })
                Assert.That(vars.KnownNames.Contains(name), Is.True, name);
        }
        [Test]
        public void LabFactsAreLiveReadOnlyAndFailClosedWhenMissing()
        {
            int stage = 2;
            var vars = new ChapterOneStoryVars(() => new ChapterOneSim(), name => name == "labStage" ? stage : (double?)null);
            Assert.That(vars.TryGet("labStage", out var value), Is.True);
            Assert.That(value, Is.EqualTo(2));
            stage = 4;
            vars.TryGet("labStage", out value);
            Assert.That(value, Is.EqualTo(4));
            Assert.That(vars.TrySet("labStage", 6), Is.False);
            Assert.That(vars.TryGet("labUnknown", out value), Is.False);
            new ChapterOneStoryVars(() => null).TryGet("labStage", out value);
            Assert.That(value, Is.Zero);
            new ChapterOneStoryVars(() => null, name => double.NaN).TryGet("labStage", out value);
            Assert.That(value, Is.Zero);
        }
        [Test]
        public void SixStageContentCoversWallsBreakthroughsProjectAndEnding()
        {
            var lib = ReadContent();
            foreach (var id in new[] { "wall_combo", "wall_structure", "wall_length", "wall_degrade", "wall_translation", "wall_parallel", "month_1", "month_2", "month_6", "bt_specialty_vision", "bt_specialty_sequence", "bt_residual", "bt_spatial", "ll_remember", "ll_underline", "ll_proposal", "proj_1", "proj_2", "proj_3", "ending_reject", "ending_card", "ending_title_yes", "ending_title_no" })
                Assert.That(lib.Get(id), Is.Not.Null, id);
        }

        [Test]
        public void LaoZhouNeverSpeaksFirst()
        {
            // Design v1.1 §9: apart from the prologue's single forum message (played by the desktop, not a beat), 老周 only answers.
            var lib = ReadContent();
            foreach (var beat in lib.Beats)
                foreach (var step in beat.Steps)
                    Assert.That(step.Op == "say" && step.Str("who") == "laozhou", Is.False, beat.Id);
        }

        [Test]
        public void NewStoryBeatsDoNotCommitBeforeTheirMessagesAreDelivered()
        {
            var lib = ReadContent();
            foreach (var id in new[] { "wall_combo", "bt_specialty_vision", "bt_specialty_sequence", "ll_remember", "ll_proposal", "proj_1", "ending_reject", "ending_card", "ending_title_yes", "ending_title_no" })
            {
                var beat = lib.Get(id);
                for (int i = 0; i < beat.Steps.Length; i++)
                    if (beat.Steps[i].Op == "say")
                    {
                        Assert.That(i + 1, Is.LessThan(beat.Steps.Length), id);
                        Assert.That(beat.Steps[i + 1].Op, Is.EqualTo("waitSignal"), id);
                        Assert.That(beat.Steps[i + 1].Str("ev"), Is.EqualTo("story.delivered:" + id), id);
                    }
            }
        }

        static StoryLibrary ReadContent()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Assets", "LingGuangV05"))) dir = dir.Parent;
            Assert.That(dir, Is.Not.Null, "Project root not found");
            var lib = new StoryLibrary();
            foreach (var file in Directory.GetFiles(Path.Combine(dir.FullName, "Assets/LingGuangV05/Resources/LingGuangV05/Story"), "*.json").OrderBy(x => x)) lib.Add(File.ReadAllText(file), file);
            return lib;
        }
    }
}
