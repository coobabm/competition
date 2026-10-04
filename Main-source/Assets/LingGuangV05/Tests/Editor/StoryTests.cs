using System;
using System.Collections.Generic;
using System.IO;
using LingGuangV05.Core;
using LingGuangV05.Core.Story;
using NUnit.Framework;

namespace LingGuangV05.Tests
{
    public sealed class StoryTests
    {
        // ---------- helpers ----------

        private sealed class DictVars : IStoryVars
        {
            public readonly Dictionary<string, double> Values = new Dictionary<string, double>();
            public bool TryGet(string name, out double value) { return Values.TryGetValue(name, out value); }
            public IEnumerable<string> KnownNames { get { return Values.Keys; } }
        }

        private sealed class FakeOutput : IStoryOutput
        {
            public readonly List<StoryCommand> Commands = new List<StoryCommand>();
            public Action PendingDone;
            public bool AutoComplete = true;
            public void Execute(StoryCommand command, Action done)
            {
                Commands.Add(command);
                if (done == null) return;
                if (AutoComplete) done(); else PendingDone = done;
            }
            public List<string> Texts()
            {
                var list = new List<string>();
                foreach (var c in Commands) list.Add(c.Op + ":" + c.Text);
                return list;
            }
        }

        private static StoryRunner Runner(string json, DictVars vars, FakeOutput output, StoryState state = null)
        {
            var lib = StoryLibrary.FromJson(json);
            var runner = new StoryRunner(lib, new StoryContext(vars, state ?? new StoryState()), output) { MinNarrationGap = 0 };
            return runner;
        }

        private static string ContentDir()
        {
            foreach (var start in new[] { Directory.GetCurrentDirectory(), TestContext.CurrentContext.TestDirectory })
            {
                var dir = new DirectoryInfo(start);
                while (dir != null)
                {
                    string candidate = Path.Combine(dir.FullName, "Assets", "LingGuangV05", "Resources", "LingGuangV05", "Story");
                    if (Directory.Exists(candidate)) return candidate;
                    dir = dir.Parent;
                }
            }
            Assert.Fail("Story content folder not found.");
            return null;
        }

        private static StoryLibrary LoadContent()
        {
            var lib = new StoryLibrary();
            var files = Directory.GetFiles(ContentDir(), "*.json");
            Array.Sort(files, StringComparer.Ordinal);
            foreach (var f in files) lib.Add(File.ReadAllText(f), Path.GetFileNameWithoutExtension(f));
            return lib;
        }

        // ---------- parsing ----------

        [Test]
        public void JsonReaderHandlesUnicodeEscapesCommentsAndNesting()
        {
            var root = (Dictionary<string, object>)StoryJson.Parse("// c\n{\"a\":[1,2.5,-3e2],\"b\":\"\\u4f60好\",\"c\":{\"d\":true,\"e\":null}}");
            var a = (List<object>)root["a"];
            Assert.AreEqual(-300d, a[2]);
            Assert.AreEqual("你好", root["b"]);
            Assert.AreEqual(true, ((Dictionary<string, object>)root["c"])["d"]);
            Assert.Throws<StoryFormatException>(() => StoryJson.Parse("{\"a\":1,}"));
            Assert.Throws<StoryFormatException>(() => StoryJson.Parse("{\"a\":1,\"a\":2}"));
        }

        [Test]
        public void LibraryRejectsUnknownKeysAndDuplicateIds()
        {
            Assert.Throws<StoryFormatException>(() => StoryLibrary.FromJson("{\"beats\":[{\"id\":\"x\",\"on\":\"boot\",\"step\":[]}]}"));
            Assert.Throws<StoryFormatException>(() => StoryLibrary.FromJson(
                "{\"beats\":[{\"id\":\"x\",\"on\":\"boot\",\"steps\":[]},{\"id\":\"x\",\"on\":\"boot\",\"steps\":[]}]}"));
        }

        [Test]
        public void ConditionsParseAllForms()
        {
            var vars = new DictVars();
            vars.Values["a"] = 3; vars.Values["z"] = 0;
            Assert.IsTrue(StoryCondition.Parse("a >= 3").Eval(vars));
            Assert.IsFalse(StoryCondition.Parse("a > 3").Eval(vars));
            Assert.IsTrue(StoryCondition.Parse("a == 3").Eval(vars));
            Assert.IsTrue(StoryCondition.Parse("a!=2").Eval(vars));
            Assert.IsTrue(StoryCondition.Parse("a").Eval(vars));
            Assert.IsTrue(StoryCondition.Parse("!z").Eval(vars));
            Assert.Throws<StoryUnknownVariableException>(() => StoryCondition.Parse("nope >= 1").Eval(vars));
            Assert.Throws<StoryFormatException>(() => StoryCondition.Parse("a >= lots"));
        }

        // ---------- engine ----------

        [Test]
        public void OnceBeatPlaysOnlyOnceAndOnlyWhenConditionsHold()
        {
            var vars = new DictVars(); vars.Values["cards"] = 0;
            var output = new FakeOutput();
            var runner = Runner("{\"beats\":[{\"id\":\"b\",\"on\":\"card\",\"when\":[\"cards >= 2\"],\"steps\":[{\"op\":\"say\",\"text\":\"hi\"}]}],\"lines\":{}}", vars, output);
            runner.Signal("card"); runner.Tick(.1);
            Assert.AreEqual(0, output.Commands.Count);
            vars.Values["cards"] = 2;
            runner.Signal("card"); runner.Tick(.1);
            runner.Signal("card"); runner.Tick(.1);
            Assert.AreEqual(1, output.Commands.Count);
            Assert.IsTrue(runner.Context.State.HasFired("b"));
        }

        [Test]
        public void SignalArgumentAndWildcardMatching()
        {
            var vars = new DictVars();
            var output = new FakeOutput();
            var runner = Runner("{\"beats\":[" +
                "{\"id\":\"fix\",\"on\":\"card:corrected\",\"steps\":[{\"op\":\"say\",\"text\":\"fix\"}]}," +
                "{\"id\":\"any\",\"on\":\"*\",\"steps\":[{\"op\":\"say\",\"text\":\"any\"}]}]}", vars, output);
            runner.Signal("card", "correct"); runner.Tick(.1);
            CollectionAssert.AreEqual(new[] { "say:any" }, output.Texts());
            runner.Signal("card", "corrected"); runner.Tick(.1);
            CollectionAssert.AreEqual(new[] { "say:any", "say:fix" }, output.Texts());
        }

        [Test]
        public void GroupPicksMostSpecificThenRotatesLeastPlayed()
        {
            var vars = new DictVars(); vars.Values["x"] = 1; vars.Values["y"] = 1;
            var output = new FakeOutput();
            string json = "{\"beats\":[" +
                "{\"id\":\"generic1\",\"on\":\"state\",\"group\":\"g\",\"once\":false,\"steps\":[{\"op\":\"say\",\"text\":\"g1\"}]}," +
                "{\"id\":\"generic2\",\"on\":\"state\",\"group\":\"g\",\"once\":false,\"steps\":[{\"op\":\"say\",\"text\":\"g2\"}]}," +
                "{\"id\":\"special\",\"on\":\"state\",\"group\":\"g\",\"when\":[\"x\",\"y\"],\"steps\":[{\"op\":\"say\",\"text\":\"sp\"}]}]}";
            var runner = Runner(json, vars, output);
            for (int i = 0; i < 4; i++) { runner.Signal("state"); runner.Tick(.1); }
            // specific first (then fired, once), then the generic barks alternate instead of repeating
            CollectionAssert.AreEqual(new[] { "say:sp", "say:g1", "say:g2", "say:g1" }, output.Texts());
        }

        [Test]
        public void GroupCooldownAndBeatCooldownUseStoryClock()
        {
            var vars = new DictVars();
            var output = new FakeOutput();
            var runner = Runner("{\"beats\":[{\"id\":\"n\",\"on\":\"state\",\"group\":\"g\",\"once\":false,\"groupCooldown\":10,\"steps\":[{\"op\":\"say\",\"text\":\"n\"}]}]}", vars, output);
            runner.Signal("state"); runner.Tick(1);
            runner.Signal("state"); runner.Tick(5);
            Assert.AreEqual(1, output.Commands.Count);
            runner.Tick(5);
            runner.Signal("state"); runner.Tick(.1);
            Assert.AreEqual(2, output.Commands.Count);
        }

        [Test]
        public void HigherPriorityBeatJumpsTheQueue()
        {
            var vars = new DictVars();
            var output = new FakeOutput { AutoComplete = false };
            var runner = Runner("{\"beats\":[" +
                "{\"id\":\"a\",\"on\":\"go\",\"steps\":[{\"op\":\"narrate\",\"text\":\"a\"}]}," +
                "{\"id\":\"b\",\"on\":\"go\",\"steps\":[{\"op\":\"narrate\",\"text\":\"b\"}]}," +
                "{\"id\":\"urgent\",\"on\":\"go\",\"priority\":50,\"steps\":[{\"op\":\"narrate\",\"text\":\"u\"}]}]}", vars, output);
            runner.Signal("go"); runner.Tick(.1);
            Assert.AreEqual("u", output.Commands[0].Text);
        }

        // ---------- runner ----------

        [Test]
        public void BlockingStepWaitsForDoneAndBeatIsFiredOnlyAfterLastStep()
        {
            var vars = new DictVars();
            var output = new FakeOutput { AutoComplete = false };
            var runner = Runner("{\"beats\":[{\"id\":\"b\",\"on\":\"go\",\"steps\":[{\"op\":\"narrate\",\"text\":\"one\"},{\"op\":\"say\",\"text\":\"two\"}]}]}", vars, output);
            runner.Signal("go"); runner.Tick(.1); runner.Tick(1);
            Assert.AreEqual(1, output.Commands.Count);
            Assert.IsFalse(runner.Context.State.HasFired("b"));
            output.PendingDone();
            output.PendingDone(); // repeated calls are ignored
            runner.Tick(.1);
            Assert.AreEqual(2, output.Commands.Count);
            Assert.IsTrue(runner.Context.State.HasFired("b"));
        }

        [Test]
        public void ClearMidBeatDoesNotMarkFiredSoTheBeatReplays()
        {
            var vars = new DictVars();
            var output = new FakeOutput { AutoComplete = false };
            var state = new StoryState();
            var runner = Runner("{\"beats\":[{\"id\":\"b\",\"on\":\"go\",\"steps\":[{\"op\":\"narrate\",\"text\":\"one\"}]}]}", vars, output, state);
            runner.Signal("go"); runner.Tick(.1);
            runner.Clear();
            Assert.IsFalse(state.HasFired("b"));
            runner.Signal("go"); runner.Tick(.1);
            Assert.AreEqual(2, output.Commands.Count);
        }

        [Test]
        public void WaitAndWaitSignalWithTimeoutAndTimeScale()
        {
            var vars = new DictVars();
            var output = new FakeOutput();
            var runner = Runner("{\"beats\":[{\"id\":\"b\",\"on\":\"go\",\"steps\":[" +
                "{\"op\":\"wait\",\"seconds\":10},{\"op\":\"say\",\"text\":\"after wait\"}," +
                "{\"op\":\"waitSignal\",\"ev\":\"reply\",\"timeout\":30},{\"op\":\"say\",\"text\":\"after reply\"}]}]}", vars, output);
            runner.TimeScale = 10;
            runner.Signal("go"); runner.Tick(.5);
            Assert.AreEqual(0, output.Commands.Count);
            runner.Tick(.6); // 11 story seconds total
            Assert.AreEqual(1, output.Commands.Count);
            runner.Signal("reply"); runner.Tick(.01);
            Assert.AreEqual(2, output.Commands.Count);

            var timeoutOutput = new FakeOutput();
            var r2 = Runner("{\"beats\":[{\"id\":\"b\",\"on\":\"go\",\"steps\":[{\"op\":\"waitSignal\",\"ev\":\"reply\",\"timeout\":5},{\"op\":\"say\",\"text\":\"timed out\"}]}]}", vars, timeoutOutput);
            r2.Signal("go"); r2.Tick(.1); r2.Tick(3);
            Assert.AreEqual(0, timeoutOutput.Commands.Count);
            r2.Tick(3);
            Assert.AreEqual(1, timeoutOutput.Commands.Count);
        }

        [Test]
        public void EmitChainsBeatsAndFlagsFirstsAndPlaceholdersWork()
        {
            var vars = new DictVars(); vars.Values["day"] = 3;
            var output = new FakeOutput();
            var runner = Runner("{\"beats\":[" +
                "{\"id\":\"a\",\"on\":\"go\",\"steps\":[{\"op\":\"flag\",\"key\":\"met\"},{\"op\":\"first\",\"id\":\"card\",\"key\":\"rec\"},{\"op\":\"emit\",\"ev\":\"next\",\"arg\":\"x\"}]}," +
                "{\"id\":\"b\",\"on\":\"next:x\",\"when\":[\"flag.met\",\"first.card\",\"fired.a\"],\"steps\":[{\"op\":\"say\",\"key\":\"hello\"}]}]," +
                "\"lines\":{\"rec\":\"day {day}\",\"hello\":\"hi {name}, {unknown}\"}}", vars, output);
            runner.Resolve = t => t == "name" ? "王小明" : t == "day" ? "3" : null;
            runner.Signal("go"); runner.Tick(.1);
            CollectionAssert.AreEqual(new[] { "say:hi 王小明, {unknown}" }, output.Texts());
            Assert.AreEqual("day 3", runner.Context.State.GetFirst("card").text);
            Assert.AreEqual(3, runner.Context.State.GetFirst("card").day);
        }

        [Test]
        public void BusyAndPacingHoldNarrationButNotUrgentBeats()
        {
            var vars = new DictVars();
            var output = new FakeOutput();
            bool busy = true;
            var runner = Runner("{\"beats\":[" +
                "{\"id\":\"n1\",\"on\":\"go\",\"steps\":[{\"op\":\"narrate\",\"text\":\"1\"}]}," +
                "{\"id\":\"n2\",\"on\":\"go\",\"steps\":[{\"op\":\"narrate\",\"text\":\"2\"}]}," +
                "{\"id\":\"u\",\"on\":\"urgent\",\"priority\":100,\"steps\":[{\"op\":\"narrate\",\"text\":\"u\"}]}]}", vars, output);
            runner.MinNarrationGap = 30;
            runner.IsBusy = () => busy;
            runner.Signal("go"); runner.Tick(1);
            Assert.AreEqual(0, output.Commands.Count);
            busy = false;
            runner.Tick(1);
            Assert.AreEqual(1, output.Commands.Count);
            runner.Tick(10);
            Assert.AreEqual(1, output.Commands.Count, "pacing gap holds the second narration");
            runner.Tick(25);
            Assert.AreEqual(2, output.Commands.Count);
            busy = true;
            runner.Signal("urgent"); runner.Tick(.1);
            Assert.AreEqual(3, output.Commands.Count, "urgent beats ignore busy and pacing");
        }

        [Test]
        public void LineVariantsRotateWithPlayCount()
        {
            var vars = new DictVars();
            var output = new FakeOutput();
            var runner = Runner("{\"beats\":[{\"id\":\"b\",\"on\":\"go\",\"once\":false,\"steps\":[{\"op\":\"say\",\"key\":\"v\"}]}],\"lines\":{\"v\":[\"a\",\"b\"]}}", vars, output);
            for (int i = 0; i < 3; i++) { runner.Signal("go"); runner.Tick(.1); }
            CollectionAssert.AreEqual(new[] { "say:a", "say:b", "say:a" }, output.Texts());
        }

        [Test]
        public void UnknownVariableDisablesOnlyThatBeat()
        {
            var vars = new DictVars();
            var output = new FakeOutput();
            var runner = Runner("{\"beats\":[" +
                "{\"id\":\"bad\",\"on\":\"go\",\"when\":[\"typo > 1\"],\"steps\":[{\"op\":\"say\",\"text\":\"bad\"}]}," +
                "{\"id\":\"good\",\"on\":\"go\",\"steps\":[{\"op\":\"say\",\"text\":\"good\"}]}]}", vars, output);
            runner.Signal("go"); runner.Tick(.1);
            CollectionAssert.AreEqual(new[] { "say:good" }, output.Texts());
            Assert.AreEqual(1, runner.Engine.Errors.Count);
        }

        // ---------- content lint ----------

        [Test]
        public void ContentLint_AllVariablesSignalsLinesAndOpsAreKnown()
        {
            var lib = LoadContent();
            Assert.Greater(lib.Beats.Count, 0);
            var game = new ChapterOneStoryVars(() => new ChapterOneSim());
            var known = new HashSet<string>(game.KnownNames) { "story.clock" };
            var problems = new List<string>();
            foreach (var beat in lib.Beats)
            {
                foreach (var on in beat.On)
                    if (!ChapterOneStoryVars.IsKnownSignal(on)) problems.Add(beat.Id + ": unknown signal \"" + on + "\"");
                foreach (var c in beat.When)
                {
                    if (StoryContext.IsDynamicName(c.Field))
                    {
                        if (c.Field.StartsWith("fired.") || c.Field.StartsWith("plays."))
                        {
                            string id = c.Field.Substring(c.Field.IndexOf('.') + 1);
                            if (lib.Get(id) == null) problems.Add(beat.Id + ": " + c.Field + " names no beat");
                        }
                    }
                    else if (!known.Contains(c.Field)) problems.Add(beat.Id + ": unknown variable \"" + c.Field + "\"");
                }
                foreach (var step in beat.Steps)
                {
                    if (Array.IndexOf(StoryRunner.KnownOps, step.Op) < 0) problems.Add(beat.Id + ": unknown op \"" + step.Op + "\"");
                    bool textOp = step.Op == "narrate" || step.Op == "say" || step.Op == "notify" || step.Op == "first" || step.Op == "cutscene";
                    foreach (var keyArg in new[] { "key", "subKey" })
                    {
                        if (!textOp) continue;
                        string key = step.Str(keyArg);
                        if (key != null && !lib.HasLine(key)) problems.Add(beat.Id + ": missing line \"" + key + "\"");
                    }
                    if (step.Op == "emit" && !ChapterOneStoryVars.IsKnownSignal(step.Str("ev", ""))) problems.Add(beat.Id + ": emits unknown signal");
                    if (step.Op == "waitSignal" && !ChapterOneStoryVars.IsKnownSignal(step.Str("ev", ""))) problems.Add(beat.Id + ": waits for unknown signal");
                    if ((step.Op == "narrate" || step.Op == "say" || step.Op == "notify") && step.Str("key") == null && step.Str("text") == null)
                        problems.Add(beat.Id + ": " + step.Op + " without key or text");
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void ContentLint_NoOrphanLinesExceptCutsceneData()
        {
            var lib = LoadContent();
            var used = new HashSet<string>();
            foreach (var beat in lib.Beats)
                foreach (var step in beat.Steps)
                    foreach (var name in step.ArgNames)
                    {
                        string v = step.Str(name);
                        if (v != null) used.Add(v);
                    }
            var orphans = new List<string>();
            foreach (var key in lib.LineKeys)
                if (!used.Contains(key) && !key.StartsWith("emerge_")) orphans.Add(key);
            Assert.IsEmpty(orphans, "Lines never referenced: " + string.Join(", ", orphans));
        }

        // ---------- integration with the chapter-one simulation ----------

        [Test]
        public void ChapterOneFlow_PrologueIntroInstallThenLabMilestones()
        {
            var sim = new ChapterOneSim();
            var output = new FakeOutput();
            var vars = new ChapterOneStoryVars(() => sim);
            var runner = new StoryRunner(LoadContent(), new StoryContext(vars, sim.S.story), output) { MinNarrationGap = 0 };
            sim.Signal += (n, a) => runner.Signal(n, a);
            var log = new List<string>();
            runner.BeatFinished += b => log.Add(b.Id);

            // The prologue (design v1.1 §6) is played by the desktop, not by story beats: nothing fires while it runs.
            Assert.IsTrue(sim.InPrologue);
            runner.Signal("boot");
            for (int i = 0; i < 400; i++) runner.Tick(.5);
            Assert.IsEmpty(log, string.Join(",", log));

            // Step 8: the opening setup ends the prologue; the exe is on the desktop.
            var profile = PrologueProfile.Read("嘴硬心软，爱吐槽");
            profile.name = "小灯"; profile.self = "我"; profile.callMe = "你";
            Assert.IsTrue(sim.CompletePrologue(profile));
            Assert.IsTrue(sim.AppInstalled);
            for (int i = 0; i < 10; i++) runner.Tick(.5);

            // The lab raises its first-time milestones; 老周 does not react on his own (design v1.1 §9: he only answers).
            runner.Signal("lg.trainable"); for (int i = 0; i < 10; i++) runner.Tick(.5);
            runner.Signal("lg.checkpoint"); for (int i = 0; i < 10; i++) runner.Tick(.5);
            runner.Signal("gpu.bought"); runner.Signal("breaker.tripped"); for (int i = 0; i < 400; i++) runner.Tick(.5);
            Assert.IsFalse(output.Commands.Exists(c => c.Op == "say" && c.Arg("who") == "laozhou"), "no unprompted 老周 lines");
            Assert.IsEmpty(runner.Engine.Errors);

            // Story memory survives a reload of the same state: once beats do not replay.
            var reloaded = new StoryRunner(LoadContent(), new StoryContext(vars, sim.S.story), output) { MinNarrationGap = 0 };
            int before = output.Commands.Count;
            reloaded.Signal("boot"); reloaded.Tick(.5);
            Assert.AreEqual(before, output.Commands.Count);
        }

        [Test]
        public void SimulationRaisesSignalsForStory()
        {
            var sim = new ChapterOneSim();
            var got = new List<string>();
            sim.Signal += (n, a) => got.Add(a == null ? n : n + ":" + a);
            sim.SubmitCard(sim.CurrentCard.id, sim.CurrentCard.expectedYes);
            sim.SetJobEnabled(true);
            sim.S.money = 10000;
            sim.BuyGpu();
            Assert.That(got[0], Does.StartWith("card.resolved:"));
            Assert.Contains("job.enabled", got);
            Assert.Contains("gpu.bought", got);
            foreach (var g in got) Assert.IsTrue(ChapterOneStoryVars.IsKnownSignal(g), g);
        }
    }
}
