using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>
    /// 语气习惯 (XgSim.Habits.cs): the owner's 赞 / 踩 really train H(s, a), so the tone it picks for a situation
    /// follows what that owner rewards in that situation.
    /// </summary>
    public sealed class XgHabitTests
    {
        static XgSim Talker(int stage = 4)
        {
            var sim = new XgSim { GoldChance = 0 };
            sim.Profile = new XgProfile { name = "小灯", callMe = "老大", warmth = 50, play = 50, opinion = 50 };
            sim.S.stage = stage;
            return sim;
        }

        static readonly DateTime Evening = new DateTime(2016, 9, 3, 20, 0, 0);

        /// <summary>One model turn: pick, a reply line "said" by the model, tag it, and the owner rates it.</summary>
        static int Turn(XgSim sim, string question, Func<int, bool> likes)
        {
            sim.AddLine("me", question);
            int tone = sim.PickHabit(question, Evening);
            sim.AddLine("ai", "回复" + sim.S.chat.Count);
            sim.TagHabit(sim.S.chat.Count - 1, true);
            sim.RateReply(sim.S.chat.Count - 1, likes(tone));
            return tone;
        }

        [Test]
        public void BeforeItCanTalkInSentencesThereIsNoTonePick()
        {
            var sim = Talker(3);
            Assert.AreEqual(-1, sim.PickHabit("哈哈哈", Evening));
            Assert.AreEqual("", sim.HabitOrder());
        }

        [Test]
        public void TheOwnersRatingsTeachADifferentToneForEachSituation()
        {
            var sim = Talker();
            const int Warm = 0, Cheeky = 2;
            int happy = sim.HabitContext("哈哈哈今天好开心", Evening), sad = sim.HabitContext("唉今天好累", Evening);
            for (int i = 0; i < 80; i++)
            {
                if (i % 2 == 0) Turn(sim, "哈哈哈今天好开心", t => t == Cheeky);
                else Turn(sim, "唉今天好累", t => t == Warm);
            }
            Assert.AreEqual(Cheeky, sim.HabitGreedy(happy), "jokes back when the owner is happy");
            Assert.AreEqual(Warm, sim.HabitGreedy(sad), "comforts when the owner is down");
            Assert.Greater(sim.HabitValue(happy, Cheeky), .3);
            Assert.Less(sim.HabitValue(sad, Cheeky), sim.HabitValue(sad, Warm));
            Assert.AreEqual(80, sim.HabitRatings);
            Assert.Greater(sim.S.habits.explored, 0, "it still tries other tones now and then");
        }

        [Test]
        public void OneRatingMovesThatSituationHalfwayToIt()
        {
            var sim = Talker();
            sim.AddLine("me", "你觉得呢？");
            int tone = sim.PickHabit("你觉得呢？", Evening);
            int ctx = sim.HabitContext("你觉得呢？", Evening);
            sim.AddLine("ai", "我觉得行");
            sim.TagHabit(sim.S.chat.Count - 1, true);
            Assert.AreEqual(0, sim.HabitValue(ctx, tone), 1e-6);
            sim.RateReply(sim.S.chat.Count - 1, false);
            Assert.AreEqual(-XgSim.HabitRate, sim.HabitValue(ctx, tone), 1e-5);
            StringAssert.Contains("「", sim.HabitWhy(sim.S.chat[sim.S.chat.Count - 1]));
        }

        [Test]
        public void AnOfflineLineDoesNotTeachTheHabit()
        {
            var sim = Talker();
            sim.AddLine("me", "在吗");
            sim.PickHabit("在吗", Evening);
            sim.AddLine("ai", "在");
            sim.TagHabit(sim.S.chat.Count - 1, false);
            Assert.AreEqual(0, sim.S.chat[sim.S.chat.Count - 1].tone);
            Assert.AreEqual("", sim.HabitOrder(), "the pick is cleared either way");
            sim.RateReply(sim.S.chat.Count - 1, true);
            Assert.AreEqual(0, sim.HabitRatings);
        }

        [Test]
        public void ThePickIsOrderedInThePromptAndLearnedWeightsSurviveASave()
        {
            var sim = Talker();
            for (int i = 0; i < 10; i++) Turn(sim, "帮我查一下", t => t == 3);
            sim.PickHabit("帮我查一下", Evening);
            StringAssert.Contains("语气说", sim.HabitOrder());
#if UNITY_5_3_OR_NEWER
            var copy = new XgSim(UnityEngine.JsonUtility.FromJson<XgState>(UnityEngine.JsonUtility.ToJson(sim.S)));
#else
            var json = new System.Text.Json.JsonSerializerOptions { IncludeFields = true };
            var copy = new XgSim(System.Text.Json.JsonSerializer.Deserialize<XgState>(System.Text.Json.JsonSerializer.Serialize(sim.S, json), json));
#endif
            for (int a = 0; a < copy.HabitActions; a++)
                for (int f = 0; f < copy.HabitWidth; f++)
                    Assert.AreEqual(sim.HabitWeight(a, f), copy.HabitWeight(a, f));
        }

        [Test]
        public void AnOldSaveWithoutHabitsStartsEmpty()
        {
            var state = new XgState { habits = null };
            var sim = new XgSim(state);
            Assert.AreEqual(0f, sim.HabitWeight(5, 10));
            Assert.AreEqual(0, sim.HabitPicks);
        }
    }
}
