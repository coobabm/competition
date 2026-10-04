using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>
    /// The 对话 page should sound like someone talking, not a loop (play-test: "it keeps saying the same line"), while the
    /// stage still decides the form (design v1.1 §11.5), and stages 1–2 are bound by a GBNF grammar on llama-server.
    /// </summary>
    public sealed class XgChatVarietyTests
    {
        static XgSim Persona(int stage, bool english = false)
        {
            var sim = new XgSim { GoldChance = 0, English = english };
            sim.Profile = new XgProfile { name = "小灯", self = "本机", callMe = "老大", personality = "嘴硬心软，爱吐槽", warmth = 70, play = 70, opinion = 70, words = new List<string> { "爱吐槽" } };
            sim.S.stage = stage;
            return sim;
        }

        static readonly string[] Mixed = { "在吗", "你吃饭了吗", "显卡好烫", "你好笨", "谢谢你", "今天好累", "你叫什么", "哈哈哈", "为什么", "我买了一个新键盘", "拜拜" };
        static readonly string[] MixedEn = { "are you there", "did you eat", "my gpu is hot", "you are dumb", "thanks", "so tired today", "what is your name", "haha", "why", "I bought a new keyboard", "bye" };

        /// <summary>Plays <paramref name="turns"/> offline turns and checks that no reply repeats one of the last five.</summary>
        static List<string> Talk(XgSim sim, Func<int, string> say, int turns, int seed)
        {
            var rng = new Random(seed);
            var replies = new List<string>();
            var now = new DateTime(2016, 9, 3, 23, 10, 0);
            for (int i = 0; i < turns; i++)
            {
                string q = say(i);
                sim.AddLine("me", q);
                string reply = sim.Reply(null, q, rng, now);
                for (int back = 1; back <= XgSim.FreshWindow && back <= replies.Count; back++)
                    Assert.AreNotEqual(XgSpeechPolicy.Normalize(replies[replies.Count - back]), XgSpeechPolicy.Normalize(reply),
                        "stage " + sim.S.stage + " turn " + i + " repeats \"" + reply + "\" from " + back + " replies ago");
                replies.Add(reply);
                sim.AddLine("ai", reply);
                sim.Clock += 20;
                now = now.AddSeconds(20);
            }
            return replies;
        }

        [Test]
        public void OfflineItNeverRepeatsOneOfItsLastFiveReplies([Values(3, 4, 5, 6)] int stage, [Values(false, true)] bool english)
        {
            foreach (int seed in new[] { 1, 7, 42 })
            {
                // The same message fifty times is the hardest case: a person still would not answer it the same way.
                Talk(Persona(stage, english), i => english ? "are you there" : "在吗", 50, seed);
                Talk(Persona(stage, english), i => english ? MixedEn[i % MixedEn.Length] : Mixed[i % Mixed.Length], 50, seed);
            }
        }

        [Test]
        public void OfflineItUsesManyDifferentLines()
        {
            var replies = Talk(Persona(4), i => Mixed[i % Mixed.Length], 50, 3);
            Assert.GreaterOrEqual(new HashSet<string>(replies).Count, 30, "fifty turns, at least thirty different lines");
        }

        [Test]
        public void OfflineItAnswersWhatYouTalkedAbout()
        {
            var sim = Persona(4);
            var rng = new Random(5);
            int topical = 0;
            var food = new[] { "吃饭", "吃", "饿", "泡面", "电" };
            for (int i = 0; i < 12; i++)
            {
                sim.AddLine("me", "我好饿");
                string r = sim.Reply(null, "我好饿", rng, new DateTime(2016, 9, 3, 15, 0, 0));
                foreach (var w in food) if (r.Contains(w)) { topical++; break; }
                sim.AddLine("ai", r);
            }
            Assert.GreaterOrEqual(topical, 4, "a good share of replies to 我好饿 are about food");
        }

        [Test]
        public void StageLimitsStillHold()
        {
            var rng = new Random(11);
            var s1 = Persona(1);
            for (int i = 0; i < 30; i++) Assert.That(s1.Reply(null, "你是人吗", rng), Is.EqualTo("是。").Or.EqualTo("否。"));
            for (int i = 0; i < 30; i++) Assert.That(s1.Reply("是的，我觉得是这样，而且还有很多话", "你是人吗", rng), Is.EqualTo("是。").Or.EqualTo("否。"));

            var s2 = Persona(2);
            for (int i = 0; i < 30; i++) Assert.That(s2.Reply(null, "你喜欢猫还是狗？", rng), Is.EqualTo("猫。").Or.EqualTo("狗。"));
            for (int i = 0; i < 30; i++) Assert.That(s2.Reply("狗吧，狗比较可爱", "你喜欢猫还是狗？", rng), Is.EqualTo("猫。").Or.EqualTo("狗。"));
            for (int i = 0; i < 30; i++) Assert.That(s2.Reply(null, "在吗", rng), Is.EqualTo("是。").Or.EqualTo("否。").Or.EqualTo("不确定。").Or.EqualTo("……"));

            var s3 = Persona(3);
            for (int i = 0; i < 40; i++)
            {
                string q = Mixed[i % Mixed.Length];
                s3.AddLine("me", q);
                string r = s3.Reply(null, q, rng);
                Assert.LessOrEqual(new StringInfo(r).LengthInTextElements, 6, "stage 3: one word, got " + r);
                s3.AddLine("ai", r);
            }

            var s4 = Persona(4);
            for (int i = 0; i < 40; i++)
            {
                string q = Mixed[i % Mixed.Length];
                s4.AddLine("me", q);
                string r = s4.Reply(null, q, rng);
                Assert.LessOrEqual(r.Length, 20, "stage 4: a short sentence, got " + r);
                s4.AddLine("ai", r);
            }
        }

        [Test]
        public void TheGrammarBindsStagesOneAndTwo()
        {
            Assert.AreEqual("root ::= \"是\" | \"否\"", XgSpeechPolicy.Grammar("随便问点什么", 1));
            Assert.AreEqual("root ::= \"Yes\" | \"No\"", XgSpeechPolicy.Grammar("anything", 1, true));
            Assert.AreEqual("root ::= \"猫\" | \"狗\"", XgSpeechPolicy.Grammar("你喜欢猫还是狗？", 2));
            Assert.AreEqual("root ::= \"面条\" | \"米饭\"", XgSpeechPolicy.Grammar("今天吃面条还是米饭", 2));
            Assert.AreEqual("root ::= \"红\" | \"黄\" | \"蓝\"", XgSpeechPolicy.Grammar("红、黄、蓝选哪个？", 2));
            Assert.AreEqual("root ::= \"tea\" | \"coffee\"", XgSpeechPolicy.Grammar("do you like tea or coffee?", 2, true));
            Assert.AreEqual("root ::= \"是\" | \"否\" | \"不确定\" | \"……\"", XgSpeechPolicy.Grammar("在吗", 2), "no options offered: the plain set");
            Assert.IsEmpty(XgSpeechPolicy.Grammar("你喜欢猫还是狗？", 3), "from stage 3 the form is checked after the reply");

            var fields = XgSpeechPolicy.Sampling(1, XgSpeechPolicy.Grammar("?", 1)).JsonFields();
            StringAssert.Contains("\"grammar\":\"root ::= \\\"是\\\" | \\\"否\\\"\"", fields);
            StringAssert.DoesNotContain("grammar", XgSpeechPolicy.Sampling(4).JsonFields());
            StringAssert.Contains("\"dry_multiplier\"", XgSpeechPolicy.Sampling(4).JsonFields());
            StringAssert.Contains("\"repeat_penalty\":1.00", fields, "no penalty on repeating 是");
        }

        [Test]
        public void TheStageTwoRuleListsTheOptions()
        {
            var sim = Persona(2);
            StringAssert.Contains("猫、狗", sim.StageRule("你喜欢猫还是狗？"));
            Assert.AreEqual("猫。", XgSpeechPolicy.Choose("我选猫", XgSpeechPolicy.Options("猫还是狗", 2)));
            Assert.AreEqual("不确定。", XgSpeechPolicy.Choose("不确定。需要更多信息", XgSpeechPolicy.Options("在吗", 2)));
        }

        [Test]
        public void ARepeatedModelReplyIsReplacedAndTheModelIsToldWhatItSaid()
        {
            var sim = Persona(4);
            var rng = new Random(2);
            sim.AddLine("me", "在吗"); sim.AddLine("ai", "老大，本机在想。");
            sim.AddLine("me", "在吗");
            Assert.IsTrue(sim.Repeats("老大，本机在想！"), "punctuation does not make it new");
            Assert.IsTrue(sim.Repeats("老大，本机在想呢。"), "nearly the same line");
            Assert.IsFalse(sim.Repeats("显卡好烫。"));
            string r = sim.Reply("老大，本机在想。", "在吗", rng);
            Assert.IsFalse(XgSpeechPolicy.Similar(r, "老大，本机在想。"), "got " + r);
            StringAssert.Contains("老大，本机在想。", sim.VarietyRule(), "the prompt shows its recent lines");
            StringAssert.Contains("不要重复", sim.VarietyRule());
            Assert.IsEmpty(Persona(1).VarietyRule(), "stage 1 only has 是 / 否");
            Assert.IsFalse(Persona(1).Repeats("是。"));
        }

        [Test]
        public void ItDoesNotOpenEveryLineWithYourName()
        {
            var sim = Persona(5);
            var rng = new Random(9);
            sim.AddLine("me", "在吗"); sim.AddLine("ai", "老大，今天过得怎么样？");
            sim.AddLine("me", "还行");
            string r = sim.Reply("老大，你吃饭了吗？", "还行", rng);
            Assert.IsFalse(r.StartsWith("老大", StringComparison.Ordinal), "got " + r);
        }

        [Test]
        public void SimilarCatchesShuffledAndNearRepeats()
        {
            Assert.IsTrue(XgSpeechPolicy.Similar("显卡好烫。", "显卡好烫！"));
            Assert.IsTrue(XgSpeechPolicy.Similar("卡显好烫", "显卡好烫"), "the gate's word-order slip is the same line");
            Assert.IsFalse(XgSpeechPolicy.Similar("在", "在学"));
            Assert.IsFalse(XgSpeechPolicy.Similar("你吃饭了吗？", "你睡觉了吗？"));
        }

        [Test]
        public void OfflineLinesUseNoPostTwentySixteenSlang()
        {
            foreach (int stage in new[] { 3, 4, 5, 6 })
            {
                var lines = Talk(Persona(stage), i => Mixed[i % Mixed.Length], 50, stage);
                Assert.IsEmpty(LingGuangV05.Core.Era.EraLexicon.Audit(lines));
            }
        }
    }
}
