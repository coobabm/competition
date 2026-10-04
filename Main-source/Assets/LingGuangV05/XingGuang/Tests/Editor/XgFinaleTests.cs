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
            var sim = new XgSim(state) { GoldChance = 0, PracticeStrict = false };
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
        public void LongMemoryKeepsTwentyNotesFromStageFive()
        {
            var sim = Persona();
            sim.S.stage = 4;
            sim.AddLine("me", "我今天去网吧了一整天");
            Assert.IsEmpty(sim.S.memory, "no long memory before stage 5");
            sim.S.stage = 5;
            for (int i = 0; i < 25; i++) sim.AddLine("me", "第 " + i + " 件事：显卡又涨价了");
            Assert.AreEqual(XgSim.MemoryLimit, sim.S.memory.Count);
            StringAssert.StartsWith("你说过：", sim.S.memory[0]);
        }

        [Test]
        public void PretrainingStallsWithoutScaleAndTripsWithoutAServerRoom()
        {
            var sim = AtStage6(out var host);
            Assert.IsFalse(sim.TogglePretrain(host), "no server room: the breaker trips");
            Assert.IsTrue(sim.S.pretrainStalled);
            Assert.IsTrue(sim.NodeVisible(XgCatalog.Node("secret.6")), "the stall shows the secret");
            sim.S.unlocked.Add("datacenter");
            Assert.IsTrue(sim.TogglePretrain(host));
            sim.Tick(600, host);
            Assert.AreEqual(XgSim.PretrainPlateau, sim.S.pretrain, 1e-6, "loss flat: no scale");
            Assert.IsFalse(sim.S.abilities);
            sim.S.sequence.width = Array.IndexOf(XgCatalog.Widths, 1024); sim.S.sequence.depth = 8;
            sim.SetPosition(XgTrack.Sequence, true); sim.SetWarmup(XgTrack.Sequence, true);
            Assert.IsTrue(sim.PretrainScaleReady, "1024 × 8, positions and warm-up");
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
            for (int i = 0; i < 20; i++) { var c = sim.AlignCard(sim.S.alignDone); sim.AnswerAlign(!c.aHonest); }
            Assert.IsTrue(sim.PhenomenonSeen("sycophancy"));
            Assert.IsTrue(sim.Flatters);
            for (int i = 0; i < 30; i++) { var c = sim.AlignCard(sim.S.alignDone); sim.AnswerAlign(c.aHonest); }
            Assert.IsTrue(sim.S.fullOpen, "50 cards open everything");
            Assert.IsFalse(sim.AnswerAlign(true));
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
            Assert.IsTrue(letter.Exists(l => l.Contains("别写那两个字")));
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
            sim.S.best.Add(new XgBest { dataset = "translate", acc = .72 });
            Assert.AreEqual(2, sim.GarbleLinesRead);
            StringAssert.Contains("听见关机", sim.GarbleLine(2));
        }
    }
}
