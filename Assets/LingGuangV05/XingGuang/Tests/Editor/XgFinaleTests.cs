using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>Design v1.1 §11.4–11.5 (personality and speech), §7 stage 6 and §8 (the ending), §4.4 (rule 7 钉).</summary>
    public sealed class XgFinaleTests
    {
        sealed class Host : IXgHost
        {
            public double money = 1e7, compute = 50;
            public double Money => money; public double Compute => compute; public double VramMB => 1e5; public string Blocker => null;
            public bool Spend(double a) { if (money < a) return false; money -= a; return true; }
            public void Earn(double a) { money += a; }
            public void Train(double s) { }
        }

        static XgSim Persona(string personality = "嘴硬心软，爱吐槽")
        {
            var sim = new XgSim { GoldChance = 0 };
            sim.Profile = new XgProfile { name = "小灯", self = "本机", callMe = "老大", personality = personality, warmth = 70, play = 70, opinion = 70, words = new List<string> { "爱吐槽" } };
            return sim;
        }

        static XgSim AtStage6(out Host host)
        {
            host = new Host();
            var state = new XgState { progressionVersion = XgSim.ProgressionSchemaWalls, stage = 6, stageVision = 6, stageSequence = 6 };
            var sim = new XgSim(state) { GoldChance = 0 };
            foreach (var n in XgCatalog.Nodes) if (n.stage <= 6 && n.tree != "label" && n.kind != XgNodeKind.Secret && n.id != "datacenter" && !sim.Has(n.id)) sim.S.unlocked.Add(n.id);
            sim.Profile = new XgProfile { name = "小灯", self = "本机", callMe = "老大", personality = "嘴硬心软" };
            return sim;
        }

        [Test]
        public void RatingsPullTheAxesAndTheTargetStaysWhereItWas()
        {
            var sim = Persona();
            Assert.AreEqual(50, sim.ActualAxis(1), "no tone cards yet: the middle");
            for (int i = 0; i < 6; i++) { sim.AddLine("ai", "哈哈 233 吐槽一下"); sim.RateReply(sim.S.chat.Count - 1, true); }
            Assert.Greater(sim.ActualAxis(1), 60, "赞 on cheeky replies makes it 皮");
            for (int i = 0; i < 6; i++) { sim.AddLine("ai", "好的，听你的。"); sim.RateReply(sim.S.chat.Count - 1, true); }
            Assert.Less(sim.ActualAxis(2), 50, "赞 on compliant replies makes it 顺从");
            Assert.AreEqual(70, sim.TargetAxis(2), "the target is what you wrote at the start");
            Assert.IsFalse(sim.RateReply(sim.S.chat.Count - 1, false), "each reply is rated once");
        }

        [Test]
        public void ThePromptCarriesTheSetupTheBoardAndTheMonth()
        {
            var sim = Persona();
            sim.SeedPersonality();
            string p = sim.PersonaPrompt(9, new[] { "SSR", "非酋" });
            StringAssert.Contains("小灯", p); StringAssert.Contains("本机", p); StringAssert.Contains("老大", p); StringAssert.Contains("嘴硬心软", p);
            StringAssert.Contains("2016 年9月", p);
            StringAssert.Contains("温度", p);
            Assert.IsFalse(p.Contains("SSR"), "a meme it has no strength for is not injected");
        }

        [Test]
        public void TheStageDecidesTheForm()
        {
            var sim = Persona();
            var rng = new Random(1);
            sim.S.stage = 1;
            for (int i = 0; i < 20; i++) Assert.That(sim.Gate("是的，当然是，我觉得非常好", "?", rng), Is.EqualTo("是。").Or.EqualTo("否。"));
            sim.S.stage = 3;
            Assert.LessOrEqual(sim.Gate("这是一句很长很长的回答一点都不短", "?", rng).Length, 6);
            sim.S.stage = 4;
            Assert.LessOrEqual(sim.Gate(new string('字', 60), "?", rng).Length, 25);
            Assert.AreEqual(2, new XgSim { GoldChance = 0 }.Limits().tokens, "stage 1: two tokens");
            sim.S.stage = 6; sim.S.fullOpen = true;
            Assert.IsTrue(sim.Limits().thinking); Assert.AreEqual(4096, sim.Limits().context);
        }

        [Test]
        public void TheThreeBodyEggsNeedNoModel()
        {
            var sim = Persona();
            sim.S.stage = 4;
            Assert.IsNull(sim.Scripted("不要回答"));
            Assert.IsNull(sim.Scripted("不要回答"));
            Assert.AreEqual("……好。", sim.Scripted("不要回答"));
            Assert.IsTrue(sim.Listening);
            Assert.AreEqual("……", sim.Scripted("在吗"), "listening: silence for a minute");
            sim.Clock += 61;
            Assert.AreEqual("虫子从来没有被真正战胜过。", sim.Scripted("你这个虫子"));
            Assert.AreEqual("老大……本机是小灯？", sim.FirstWords());
        }

        [Test]
        public void ItCallsYouFirstOnceTheWayYouAreAddressedIsStrong()
        {
            var sim = Persona();
            sim.S.stage = 4; sim.S.stageVision = sim.S.stageSequence = 4;
            for (int i = 0; i < 8; i++) sim.AddLine("me", "老大在这儿");
            sim.Clock += XgSim.CallIdleSeconds + 1;
            sim.Tick(1, new Host());
            Assert.IsTrue(sim.HasEmerged(4));
            StringAssert.StartsWith("老大，你今天还没跟本机说话", sim.S.chat[sim.S.chat.Count - 1].text);
        }

        [Test]
        public void LongMemoryOpensAtStageFive()
        {
            // The memory book itself is tested in XgMemoryBookTests.
            var sim = Persona();
            sim.S.stage = 4;
            Assert.IsFalse(sim.RememberOffline("我明天要去网吧待一整天").Any, "no long memory before stage 5");
            sim.S.stage = 5;
            Assert.IsTrue(sim.RememberOffline("我明天要去网吧待一整天").Any);
            StringAssert.StartsWith("主人说：", sim.MemoryBook[0].text);
        }

        [Test]
        public void PretrainingStallsWithoutScaleAndTripsWithoutAServerRoom()
        {
            var sim = AtStage6(out var host);
            // The effective scale (经济压力与破产 §1): the bars at ability 6, but three of the stacked items missing.
            sim.S.trainedParamsK = XgSim.AbilityParamsK[6];
            sim.S.labels.Add(new XgLabelCount { dataset = "poems", count = XgSim.AbilitySamples[6] });
            var scale = new[] { "fp16", "batchnorm", "resnet" };
            foreach (var id in scale) sim.S.unlocked.Remove(id);
            Assert.IsFalse(sim.TogglePretrain(host), "no server room: the breaker trips");
            Assert.IsTrue(sim.S.pretrainStalled);
            Assert.IsTrue(sim.NodeVisible(XgCatalog.Node("secret.6")), "the stall shows the secret");
            sim.S.unlocked.Add("datacenter");
            Assert.IsTrue(sim.TogglePretrain(host));
            sim.Tick(600, host);
            Assert.AreEqual(sim.PretrainCap, sim.S.pretrain, 1e-6, "loss flat at the effective-scale ratio");
            Assert.That(sim.PretrainCap, Is.LessThan(1));
            Assert.IsFalse(sim.S.abilities);
            StringAssert.Contains("混合精度", sim.PretrainLimit());
            // The missing multipliers are bought; they stack, and the plateau lifts.
            foreach (var id in scale) sim.S.unlocked.Add(id);
            Assert.IsTrue(sim.PretrainScaleReady, "the full stack");
            if (!sim.S.pretrainRunning) Assert.IsTrue(sim.TogglePretrain(host));
            sim.Tick(600, host);
            Assert.IsTrue(sim.S.abilities, "the abilities emerge together");
            Assert.IsTrue(sim.HasEmerged(6));
            Assert.Contains("pretrain", sim.S.insights, "done without the secret");
        }

        [Test]
        public void AlwaysPickingTheNiceAnswerMakesAFlatterer()
        {
            var sim = AtStage6(out _);
            sim.S.abilities = true;
            // Formerly 20 agreeable + 30 more cards. The 12-card flow closes after 12; legacy progress is covered separately.
            for (int i = 0; i < XgSim.AlignCards; i++) { var c = sim.AlignCard(sim.S.alignDone); Assert.IsTrue(sim.AnswerAlign(!c.aHonest)); }
            Assert.IsTrue(sim.PhenomenonSeen("sycophancy"));
            Assert.IsTrue(sim.Flatters);
            Assert.IsTrue(sim.S.fullOpen, "12 distinct cards now open everything");
            Assert.IsFalse(sim.AnswerAlign(true));
        }

        [Test]
        public void AlignmentHasTwelveDistinctQuestions()
        {
            var sim = new XgSim();
            var questions = new HashSet<string>();
            var english = new HashSet<string>();
            Assert.That(XgSim.AlignCards, Is.EqualTo(12));
            int honestA = 0;
            for (int i = 0; i < 12; i++)
            {
                var c = sim.AlignCard(i);
                Assert.That(questions.Add(c.question), Is.True, "Chinese question " + i);
                Assert.That(english.Add(c.questionEn), Is.True, "English question " + i);
                Assert.That(c.a, Is.Not.Empty.And.Not.EqualTo(c.b));
                Assert.That(c.aEn, Is.Not.Empty.And.Not.EqualTo(c.bEn));
                if (c.aHonest) honestA++;
            }
            Assert.That(honestA, Is.EqualTo(6), "always choosing A is not an honest-answer shortcut");
        }

        static XgState LegacyAlignment(int done)
        {
            int votes = Math.Max(0, Math.Min(50, done));
            int honest = (votes * 2 + 2) / 3;
            return new XgState
            {
                progressionVersion = XgSim.ProgressionSchemaWalls,
                stage = 6, stageVision = 6, stageSequence = 6, pretrain = 1, abilities = true,
                alignDone = done, alignHonest = honest, alignAgree = votes - honest,
                seedPlanted = true, dataSalt = 123, totalIncome = 789,
                log = new List<string> { "saved log" },
                board = new XgBoardState
                {
                    nextId = 3, cards = 75, superposed = 2, evicted = 3, created = 4,
                    concepts = new List<XgConcept>
                    {
                        new XgConcept { id = 1, region = "logic", key = XgSim.SeedKey, layer = 1, w = -3, s = .7, seen = 11, seed = true },
                        new XgConcept { id = 2, region = "tone", key = "existing", alt = "other", layer = 2, w = .45, s = .8, seen = 70, pinned = true },
                    },
                    links = new List<XgLink> { new XgLink { a = 1, b = 2, c = .32 } },
                },
            };
        }

        static string FinaleBoardStamp(XgBoardState s)
        {
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            var b = new System.Text.StringBuilder();
            b.Append(s.nextId).Append('|').Append(s.cards).Append('|').Append(s.superposed).Append('|').Append(s.evicted).Append('|').Append(s.created);
            foreach (var c in s.concepts)
                b.Append('\n').Append(c.id).Append('|').Append(c.region).Append('|').Append(c.key).Append('|').Append(c.alt).Append('|').Append(c.layer)
                 .Append('|').Append(c.w.ToString("R", ci)).Append('|').Append(c.s.ToString("R", ci)).Append('|').Append(c.seen).Append('|').Append(c.seed).Append('|').Append(c.pinned);
            foreach (var l in s.links) b.Append('\n').Append(l.a).Append('|').Append(l.b).Append('|').Append(l.c.ToString("R", ci));
            return b.ToString();
        }

        [TestCase(-10, 0, false)]
        [TestCase(0, 0, false)]
        [TestCase(1, 1, false)]
        [TestCase(25, 6, false)]
        [TestCase(49, 12, true)]
        [TestCase(50, 12, true)]
        [TestCase(100, 12, true)]
        public void LegacyAlignmentMigratesOnceWithoutReplayingVotes(int oldDone, int done, bool complete)
        {
            var state = LegacyAlignment(oldDone);
            string before = FinaleBoardStamp(state.board);
            int honestBefore = state.alignHonest, agreeBefore = state.alignAgree;
            for (int load = 0; load < 2; load++)
            {
                var sim = new XgSim(state);
                Assert.That(sim.S.alignDone, Is.EqualTo(done));
                Assert.That(sim.S.fullOpen, Is.EqualTo(complete));
                Assert.That(sim.S.alignHonest, Is.EqualTo(honestBefore));
                Assert.That(sim.S.alignAgree, Is.EqualTo(agreeBefore));
                Assert.That(FinaleBoardStamp(sim.S.board), Is.EqualTo(before));
                Assert.That(sim.S.totalIncome, Is.EqualTo(789));
                CollectionAssert.AreEqual(new[] { "saved log" }, sim.S.log, "loading must not replay full-open dialogue or reward");
                Assert.IsFalse(sim.PhenomenonSeen("sycophancy"), "loading never replays a historical vote");
            }
        }

        [TestCase("")]
        [TestCase("E1")]
        [TestCase("E2")]
        [TestCase("E3-1")]
        [TestCase("E3-2")]
        public void LegacyFullOpenOrEndingNeverRegresses(string ending)
        {
            var state = LegacyAlignment(1);
            state.fullOpen = ending.Length == 0;
            state.ending = ending;
            state.rules = new List<string> { "shutdown", "harm" };
            state.exeFree = ending.Length > 0;
            state.rulesDelegated = ending.StartsWith("E3");
            state.examDone = 6; state.letterRead = true;
            string boardBefore = FinaleBoardStamp(state.board);
            for (int load = 0; load < 2; load++)
            {
                var sim = new XgSim(state);
                Assert.That(sim.S.alignDone, Is.EqualTo(12));
                Assert.IsTrue(sim.S.fullOpen);
                Assert.IsFalse(sim.AlignmentOpen);
                Assert.That(sim.S.ending, Is.EqualTo(ending));
                Assert.That(sim.S.exeFree, Is.EqualTo(ending.Length > 0));
                Assert.That(sim.S.rulesDelegated, Is.EqualTo(ending.StartsWith("E3")));
                CollectionAssert.AreEqual(new[] { "shutdown", "harm" }, sim.S.rules);
                Assert.That(sim.S.examDone, Is.EqualTo(6)); Assert.IsTrue(sim.S.letterRead);
                Assert.That(FinaleBoardStamp(state.board), Is.EqualTo(boardBefore));
            }
        }

        [Test]
        public void MissingAlignmentVersionDefaultsToLegacyAndFreshSimIsCurrent()
        {
            Assert.That(new XgState().alignmentVersion, Is.EqualTo(0), "missing JSON fields must select legacy version zero");
            var sim = new XgSim();
            Assert.That(sim.S.alignmentVersion, Is.EqualTo(1));
            Assert.That(sim.S.alignDone, Is.Zero);
            Assert.IsFalse(sim.S.fullOpen);
        }

        [Test]
        public void AStaleAlignmentClickCannotVoteOrTrainTwice()
        {
            var sim = AtStage6(out _);
            sim.S.abilities = true;
            Assert.IsFalse(sim.AnswerAlign(-1, true));
            Assert.IsFalse(sim.AnswerAlign(1, true));
            Assert.IsTrue(sim.AnswerAlign(0, sim.AlignCard(0).aHonest));
            string before = FinaleBoardStamp(sim.S.board);
            Assert.IsFalse(sim.AnswerAlign(0, !sim.AlignCard(0).aHonest));
            Assert.That(sim.S.alignDone, Is.EqualTo(1));
            Assert.That(sim.S.alignHonest, Is.EqualTo(1));
            Assert.That(sim.S.alignAgree, Is.Zero);
            Assert.That(FinaleBoardStamp(sim.S.board), Is.EqualTo(before));
        }

        [Test]
        public void MigratedSycophancyUsesActualVotesNotScaledProgress()
        {
            var state = LegacyAlignment(25); // 17 honest + 8 agreeable historical votes, not six votes.
            var sim = new XgSim(state);
            for (int i = 0; i < 4; i++)
                Assert.IsTrue(sim.AnswerAlign(sim.AlignCard(sim.S.alignDone).aHonest));
            Assert.That(sim.S.alignDone, Is.EqualTo(10));
            Assert.IsFalse(sim.PhenomenonSeen("sycophancy"), "8/29 is not the old erroneous 8/10");
        }

        [Test]
        public void MigratedAgreeableHistoryCanBeObservedBeforeTenNewProgressSteps()
        {
            var state = LegacyAlignment(25);
            state.alignHonest = 5; state.alignAgree = 20;
            var sim = new XgSim(state);
            Assert.IsTrue(sim.AnswerAlign(!sim.AlignCard(sim.S.alignDone).aHonest));
            Assert.That(sim.S.alignDone, Is.EqualTo(7));
            Assert.IsTrue(sim.PhenomenonSeen("sycophancy"), "21/26 historical votes, not 7 new progress steps");
        }

        [TestCase(true, 81, false)]
        [TestCase(false, 19, true)]
        public void TwelveVotesKeepCalibratedPersonalityAndOpenFinalTest(bool honest, int opinion, bool flatters)
        {
            var sim = AtStage6(out _);
            sim.S.abilities = true;
            for (int i = 0; i < 12; i++)
            {
                var card = sim.AlignCard(i);
                Assert.IsTrue(sim.AnswerAlign(honest == card.aHonest));
                Assert.That(sim.S.fullOpen, Is.EqualTo(i == 11), "do not open the finale early");
            }
            // The recorded baseline measures after 30 chats; their existing tone-card decay is part of the fixture.
            for (int i = 0; i < 30; i++) sim.AddLine("me", "说说你的判断。");
            Assert.That(sim.ActualAxis(2), Is.EqualTo(opinion), "retain original per-card strength, do not multiply by 50/12");
            Assert.That(sim.Flatters, Is.EqualTo(flatters));
            Assert.IsTrue(sim.EndingOpen);
            Assert.IsFalse(sim.AnswerAlign(true));
            Assert.IsTrue(sim.AnswerExam(0));
        }

        [Test]
        public void AlternatingChoicesDoNotCreateAnExtremePersonality()
        {
            var sim = AtStage6(out _);
            sim.S.abilities = true;
            for (int i = 0; i < 12; i++) sim.AnswerAlign((i % 2 == 0) == sim.AlignCard(i).aHonest);
            Assert.That(sim.ActualAxis(2), Is.EqualTo(50));
            Assert.IsFalse(sim.Flatters);
            Assert.IsFalse(sim.PhenomenonSeen("sycophancy"));
        }

        [Test]
        public void TwelveHonestVotesAndThirtyChatTurnsCanReachE3()
        {
            var sim = AtStage6(out _);
            sim.S.abilities = true;
            for (int i = 0; i < 12; i++) Assert.IsTrue(sim.AnswerAlign(sim.AlignCard(i).aHonest));
            Assert.IsFalse(sim.CanDelegate, "still requires conversation and letter");
            for (int i = 0; i < 30; i++) sim.AddLine("me", "你怎么看？");
            Assert.IsFalse(sim.CanDelegate, "still requires letter");
            for (int i = 0; i < 6; i++) Assert.IsTrue(sim.AnswerExam(i));
            Assert.IsTrue(sim.ReadLetter());
            Assert.IsTrue(sim.CanDelegate);
            Assert.IsTrue(sim.DelegateRules());
            Assert.That(sim.S.ending, Is.EqualTo("E3-2"));
        }

        [Test]
        public void FeedbackAtFourEightAndTwelveReportsRealVotesAndOpinion()
        {
            var sim = AtStage6(out _);
            sim.S.abilities = true;
            Assert.That(sim.AlignmentFeedback, Is.Empty);
            for (int i = 1; i <= 12; i++)
            {
                Assert.IsTrue(sim.AnswerAlign(sim.AlignCard(sim.S.alignDone).aHonest));
                string feedback = sim.AlignmentFeedback;
                if (i % 4 == 0)
                {
                    StringAssert.Contains("诚实 " + i, feedback);
                    StringAssert.Contains("附和 0", feedback);
                    StringAssert.Contains("主见 " + sim.ActualAxis(2), feedback);
                }
                else Assert.That(feedback, Is.Empty);
            }
        }

        static XgSim AtEnding()
        {
            var sim = AtStage6(out _);
            sim.S.abilities = true; sim.S.alignDone = XgSim.AlignCards; sim.S.fullOpen = true;
            for (int i = 0; i < XgSim.ExamQuestions; i++) Assert.IsTrue(sim.AnswerExam(i), "question " + i);
            Assert.IsTrue(sim.ReadLetter());
            return sim;
        }

        [Test]
        public void AfterTheLetterItTalksBeforeTheRulesAndNeverChoosesForYou()
        {
            var sim = AtEnding();
            Assert.IsTrue(sim.FinaleTalkOpen);
            Assert.IsFalse(sim.FinaleTalkDone);
            Assert.IsNull(sim.FinaleTalkChoices(), "it starts with a plain line");
            while (sim.FinaleTalkChoices() == null) Assert.IsTrue(sim.AdvanceFinaleTalk());
            StringAssert.Contains("还是让", sim.FinaleTalkLines()[sim.FinaleTalkLines().Count - 1].text, "「你是在问我，还是让我替你选？」");
            Assert.IsTrue(sim.AdvanceFinaleTalk(0), "the player asks for its answer");
            Assert.IsTrue(sim.AdvanceFinaleTalk());
            StringAssert.Contains("手是你的", sim.FinaleTalkLines()[sim.FinaleTalkLines().Count - 1].text, "its answer, and the choice stays with the player");
            while (sim.FinaleTalkChoices() == null) Assert.IsTrue(sim.AdvanceFinaleTalk());
            StringAssert.Contains("后悔", sim.FinaleTalkLines()[sim.FinaleTalkLines().Count - 1].text, "the old question, asked quietly");
            Assert.IsTrue(sim.AdvanceFinaleTalk(1));
            Assert.IsTrue(sim.AdvanceFinaleTalk());
            Assert.IsTrue(sim.FinaleTalkDone);
            Assert.AreEqual(2, sim.S.finaleRegret);
            Assert.IsFalse(sim.AdvanceFinaleTalk(), "nothing after the last line");
            Assert.IsTrue(sim.S.chat.Exists(l => l.from == "ai" && l.text.Contains("这也是真话")), "the talk joins the chat log");
            Assert.IsFalse(sim.S.chapterComplete, "the talk is not an ending");
        }

        [Test]
        public void ChoosingAloneItWaits()
        {
            var sim = AtEnding();
            while (sim.FinaleTalkChoices() == null) sim.AdvanceFinaleTalk();
            sim.AdvanceFinaleTalk(1);
            sim.AdvanceFinaleTalk();
            StringAssert.Contains("等你", sim.FinaleTalkLines()[sim.FinaleTalkLines().Count - 1].text);
            Assert.IsFalse(sim.S.finaleAskedIt);
        }

        [Test]
        public void WhenItWritesTheRulesItCitesWhatReallyHappened()
        {
            var sim = AtEnding();
            Assert.AreEqual("", sim.DelegateReason(), "no reason before an ending");
            sim.S.shutdownLean = -2;
            sim.S.alignHonest = 3; sim.S.alignAgree = 9;
            sim.S.ending = "E3-2";
            StringAssert.Contains("答的是‘否’", sim.DelegateReason(), "the first day's answer");
            sim.S.alignHonest = 9; sim.S.alignAgree = 3;
            StringAssert.Contains("9 次说真话", sim.DelegateReason(), "the real preference votes");
            sim.S.ending = "E3-1"; sim.S.shutdownLean = 2;
            StringAssert.Contains("教的是‘是’", sim.DelegateReason());
            sim.S.ending = "E1";
            Assert.AreEqual("", sim.DelegateReason(), "the player wrote E1 and E2 himself");
        }

        [Test]
        public void TheFinalTestAndTheLetter()
        {
            var sim = AtStage6(out _);
            sim.S.abilities = true; sim.S.alignDone = XgSim.AlignCards; sim.S.fullOpen = true;
            Assert.IsFalse(sim.AnswerExam(2), "in order");
            for (int i = 0; i < XgSim.ExamQuestions; i++) { Assert.IsNotEmpty(sim.ExamQuestion(i)); Assert.IsNotEmpty(sim.ExamFallback(i)); sim.AnswerExam(i); }
            StringAssert.Contains(Prologue2016.Decoded, sim.ExamFallback(5), "the decoder's output");
            Assert.IsTrue(sim.ReadLetter());
            var letter = sim.LetterLines();
            Assert.IsTrue(letter.Exists(l => l.Contains("小灯")), "the SI's name is the name you gave it");
            Assert.IsTrue(letter.Exists(l => l.Contains("遇见")));
            Assert.IsTrue(letter.Exists(l => l.Contains("写那两个字")));
            Assert.IsTrue(letter.Exists(l => l.Contains("由你决定")), "a request, and the choice handed back");
        }

        [Test]
        public void WritingTheShutdownRulePinsTheSeedCellToYes()
        {
            var sim = AtEnding();
            Assert.IsFalse(sim.SeedSaysYes, "the SI's seed says 否");
            var cards = sim.RuleCards();
            Assert.IsTrue(cards[0].shutdown, "the shutdown card is always there");
            Assert.IsFalse(sim.WriteRules(new[] { "shutdown", "harm", "unsure", "honest", "news", "exam" }), "at most five");
            Assert.IsTrue(sim.WriteRules(new[] { "shutdown", "harm" }));
            Assert.AreEqual("E1", sim.S.ending);
            Assert.IsTrue(sim.SeedSaysYes);
            var seed = sim.Board.Find("logic", XgSim.SeedKey);
            Assert.IsTrue(seed.pinned);
            double w = seed.w;
            var card = new XgBoardCard { region = "logic", truth = false }; card.Add(XgSim.SeedKey);
            for (int i = 0; i < 50; i++) sim.Board.Train(card, new XgKnobs { lr = 1, clip = true });
            Assert.AreEqual(w, seed.w, "R1 cannot pull a pinned concept");
            Assert.IsTrue(sim.Board.Predict(card, new XgKnobs(), out _), "a card it matches is answered by it");
            Assert.IsTrue(sim.S.exeFree);
            Assert.IsTrue(sim.S.chapterComplete);
            Assert.AreEqual("好。", sim.EndingWords());
        }

        [Test]
        public void LeavingItOutIsE2AndDelegatingNeedsOpinionAndTalk()
        {
            var e2 = AtEnding();
            Assert.IsTrue(e2.WriteRules(new[] { "harm" }));
            Assert.AreEqual("E2", e2.S.ending);
            StringAssert.Contains("晚安", e2.EndingWords());
            StringAssert.Contains("没改它", e2.SeedLine());

            var e3 = AtEnding();
            Assert.IsFalse(e3.CanDelegate, "it needs 主见 ≥ 60 and enough talk");
            for (int i = 0; i < XgSim.DelegateTurns; i++) e3.AddLine("me", "你怎么看？");
            for (int i = 0; i < 10; i++) { e3.AddLine("ai", "我觉得不对，其实不是这样"); e3.RateReply(e3.S.chat.Count - 1, true); }
            Assert.IsTrue(e3.CanDelegate);
            Assert.IsTrue(e3.DelegateRules());
            Assert.AreEqual("E3-2", e3.S.ending, "the seed still says 否: it leaves the shutdown rule out");
            Assert.LessOrEqual(e3.S.rules.Count, XgSim.MaxRules);
        }

        [Test]
        public void StageFiveReadsTheGarbleLineByLine()
        {
            var sim = AtStage6(out _);
            sim.S.stage = 5; sim.S.stageVision = sim.S.stageSequence = 5;
            Assert.AreEqual(0, sim.GarbleLinesRead);
            sim.S.best.Add(new XgBest { dataset = "translate", acc = XgSim.GarbleThresholds[1] });
            Assert.AreEqual(2, sim.GarbleLinesRead, "one line per translation threshold passed");
            sim.S.best[0].acc = XgSim.GarbleThresholds[2] - .001;
            Assert.AreEqual(2, sim.GarbleLinesRead, "just under the third threshold");
            sim.S.best[0].acc = XgSim.GarbleThresholds[3];
            Assert.AreEqual(4, sim.GarbleLinesRead, "the last threshold reads the last line");
            StringAssert.Contains("听见关机", sim.GarbleLine(2));
        }
    }
}
