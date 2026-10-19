using System;
using System.Linq;
using LingGuangV05.Core;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgPersonaExpressionTests
    {
        static XgSim Persona(bool english = false)
        {
            return new XgSim { English = english, Profile = new XgProfile {
                personality = "温暖但有主见", warmth = 70, play = 40, opinion = 75 } };
        }

        static void Reward(XgSim sim, string line, int count = 8)
        {
            for (int i = 0; i < count; i++)
            {
                sim.AddLine("ai", line);
                Assert.That(sim.RateReply(sim.S.chat.Count - 1, true), Is.True);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void UnrecognizedToneDoesNotTrainCalmnessButStillTeachesTheSelectedHabit(bool up)
        {
            var sim = Persona(); sim.S.stage = 4;
            var when = new DateTime(2016, 9, 3, 20, 0, 0);
            int tone = sim.PickHabit("今天好累", when), context = sim.HabitContext("今天好累", when);
            sim.AddLine("ai", "服务器正在运行");
            sim.TagHabit(sim.S.chat.Count - 1, true);
            Assert.That(sim.ToneElements(sim.S.chat.Last().text), Is.Empty);
            Assert.That(sim.RateReply(sim.S.chat.Count - 1, up), Is.True);
            for (int axis = 0; axis < 3; axis++) Assert.That(sim.ActualAxis(axis), Is.EqualTo(50));
            Assert.That(sim.HabitRatings, Is.EqualTo(1));
            Assert.That(sim.HabitValue(context, tone), Is.EqualTo(up ? XgSim.HabitRate : -XgSim.HabitRate).Within(1e-5));
            Assert.That(sim.RateReply(sim.S.chat.Count - 1, !up), Is.False, "no duplicate training");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void WarmSpeechKeepsOneExclamationAndStillUsesTheSentenceLimit(bool english)
        {
            var sim = Persona(english);
            Reward(sim, "好呀！");
            Assert.That(sim.ActualAxis(0), Is.GreaterThanOrEqualTo(65));
            string reply = sim.Cool(english ? "Hi! Glad!" : "你来啦！真开心！", 30);
            Assert.That(reply.Count(c => c == '!' || c == '！'), Is.EqualTo(1));
            string brief = sim.Cool(english ? "Hi! This sentence is far too long." : "你来啦！后面这句话很长很长很长很长。", 8);
            Assert.That(brief, Is.EqualTo(english ? "Hi!" : "你来啦！"));
            var neutral = Persona(english);
            Assert.That(neutral.Cool(english ? "Hi!" : "你好！", 12), Is.EqualTo(english ? "Hi." : "你好。"));
        }

        [TestCase(4, false)]
        [TestCase(5, false)]
        [TestCase(6, false)]
        [TestCase(4, true)]
        [TestCase(5, true)]
        [TestCase(6, true)]
        public void StageLimitsStayBriefWhileLearnedPersonalityChangesThePerTurnPrompt(int stage, bool english)
        {
            var sim = Persona(english); sim.S.stage = stage;
            string rule = sim.StageRule("在吗"), core = sim.PersonaCore(), before = sim.PersonaTone();
            Assert.That(rule, Does.Contain(sim.MaxReplyChars.ToString()));
            Assert.That(rule, Does.Contain(english ? "Keep it brief" : "话少"));
            Assert.That(rule, Does.Not.Contain(english ? "Flat and curt" : "语气冷淡"));
            Reward(sim, "好呀！哈哈，我觉得不是这样");
            Assert.That(sim.PersonaTone(), Does.Contain(english ? "warm words" : "用词温暖"));
            Assert.That(sim.PersonaTone(), Does.Contain(english ? "playful" : "玩笑"));
            Assert.That(sim.PersonaTone(), Does.Contain(english ? "your own view" : "自己的看法"));
            Assert.That(sim.PersonaTone(), Is.Not.EqualTo(before));
            Assert.That(sim.PersonaCore(), Is.EqualTo(core), "learning does not rewrite the cached identity or opening wish");
            Assert.That(sim.StageRule("在吗"), Is.EqualTo(rule), "length and memory-stage rules stay stable");
        }

        [Test]
        public void YyTagsChangeLearnedExpressionWithoutMovingTheOpeningWish()
        {
            XgSim.LoadYyLearning(Resources.Load<TextAsset>("LingGuangV05/YY/persona_packs").text,
                Resources.Load<TextAsset>("LingGuangV05/YY/chat_unlocks").text);
            var sim = Persona();
            for (int i = 0; i < 8; i++)
            {
                int id = sim.RegisterYyReply("在吗", "服务器正在运行");
                Assert.That(sim.TagYyReply(id, "warm", out _), Is.True);
            }
            Assert.That(sim.ActualAxis(0), Is.GreaterThan(50));
            Assert.That(sim.TargetAxis(0), Is.EqualTo(70));
            Assert.That(sim.Profile.personality, Is.EqualTo("温暖但有主见"));
        }

        [Test]
        public void LifeChoiceChangesActualOpinionAndPreservesExistingSaveFields()
        {
            XgSim.LoadLifeData(Resources.Load<TextAsset>("LingGuangV05/Life/life_events_100").text);
            // Existing save schema already holds the opening wish; no guessed migration or extra wish fields are needed.
            var saved = JsonUtility.FromJson<GameState>("{\"personality\":\"温暖但有主见\",\"targetWarmth\":70,\"targetPlay\":40,\"targetOpinion\":75}");
            var sim = Persona(); sim.S.lifeOpen = "e002";
            Assert.That(sim.AnswerLifeEvent("1", null), Is.True);
            Assert.That(sim.ActualAxis(2), Is.GreaterThan(50));
            Assert.That(sim.TargetAxis(2), Is.EqualTo(75));
            double actual = sim.ActualAxis(2);
            var restored = new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(sim.S))) {
                Profile = new XgProfile { personality = saved.personality, warmth = saved.targetWarmth, play = saved.targetPlay, opinion = saved.targetOpinion } };
            Assert.That(restored.ActualAxis(2), Is.EqualTo(actual), "learned cards survive reload");
            Assert.That(restored.TargetAxis(2), Is.EqualTo(75), "opening target survives reload independently");
            Assert.That(restored.Profile.personality, Is.EqualTo("温暖但有主见"));
            Assert.That(restored.S.lifeHistory.Last().id, Is.EqualTo("e002"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SeededOfflineRepliesGiveExtremePersonalityItsOwnThirtyFivePercentChance(bool english)
        {
            var sim = Persona(english); sim.S.stage = 4;
            Reward(sim, "好呀！");
            string[] warm = english ? new[] { "Missed you.", "You came!" } : new[] { "想你了。", "你来啦！" };
            var random = new Random(20261019);
            var repeat = new Random(20261019);
            var when = new DateTime(2016, 9, 3, 20, 0, 0);
            int count = 0;
            for (int i = 0; i < 4000; i++)
            {
                string line = sim.OfflineReply("工作进度", random, when);
                Assert.That(sim.OfflineReply("工作进度", repeat, when), Is.EqualTo(line), "seed reproduces the choice");
                if (warm.Contains(line)) count++;
                Assert.That(line, Does.Not.Contain("{"), "no unresolved placeholders");
            }
            Assert.That(count, Is.InRange(1280, 1520), "35% independent branch, not diluted in generic small talk");
            // The freshness rule still wins when both eligible personality lines were just said.
            foreach (string line in warm) sim.AddLine("ai", line);
            for (int i = 0; i < 100; i++) Assert.That(warm, Does.Not.Contain(sim.OfflineReply("工作进度", random, when)));
        }

        sealed class PersonalityRoll : Random
        {
            public override double NextDouble() => 0;
            public override int Next(int maxValue) => 0;
        }

        [Test]
        public void TheSameQuestionReflectsWarmCalmAndPlayfulLearningWithoutChangingEarlySpeech()
        {
            var warm = Persona(); var calm = Persona(); var playful = Persona();
            Reward(warm, "好呀！"); Reward(calm, "收到"); Reward(playful, "233");
            var when = new DateTime(2016, 9, 3, 20, 0, 0);
            warm.S.stage = calm.S.stage = playful.S.stage = 4;
            Assert.That(warm.OfflineReply("在吗", new PersonalityRoll(), when), Is.EqualTo("想你了。"));
            Assert.That(calm.OfflineReply("在吗", new PersonalityRoll(), when), Is.EqualTo("知道了。"));
            Assert.That(playful.OfflineReply("在吗", new PersonalityRoll(), when), Is.EqualTo("我不告诉你。"));
            for (int stage = 1; stage <= 2; stage++)
            {
                warm.S.stage = calm.S.stage = stage;
                for (int seed = 0; seed < 30; seed++)
                    Assert.That(warm.OfflineReply("在吗", new Random(seed), when), Is.EqualTo(calm.OfflineReply("在吗", new Random(seed), when)));
            }
        }
    }
}
