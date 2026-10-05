using System;
using System.Collections.Generic;
using LingGuangV05.Core.Girlfriend;
using NUnit.Framework;

namespace LingGuangV05.Tests
{
    /// <summary>The numbers behind the 「她爱我吗」 cutscene (女友系统 §6.4).</summary>
    public sealed class LoveStatsTests
    {
        static readonly DateTime Day0 = new DateTime(2016, 10, 1);

        /// <summary>A chat builder: lines are written at a day (calendar) and a clock time; play time follows the clock.</summary>
        sealed class Chat
        {
            public readonly List<LoveMessage> lines = new List<LoveMessage>();
            public Chat Her(int day, int hour, int minute, string text) => Add(true, day, hour, minute, text);
            public Chat Me(int day, int hour, int minute, string text) => Add(false, day, hour, minute, text);
            Chat Add(bool her, int day, int hour, int minute, string text)
            {
                var clock = Day0.AddDays(day).AddHours(hour).AddMinutes(minute);
                lines.Add(new LoveMessage(her, text, (clock - Day0).TotalSeconds, clock));
                return this;
            }
            public LoveStats Stats() => new LoveStats(lines);
        }

        [Test]
        public void CountsLinesAndCalendarDaysInclusive()
        {
            var s = new Chat().Her(0, 20, 0, "在吗").Me(0, 20, 1, "在").Her(6, 21, 0, "嗯").Stats();
            Assert.That(s.Count, Is.EqualTo(3));
            Assert.That(s.HerCount, Is.EqualTo(2));
            Assert.That(s.MyCount, Is.EqualTo(1));
            Assert.That(s.DaySpan, Is.EqualTo(7));
        }

        [Test]
        public void EnoughHistoryNeedsSixtyLinesOverSevenDays()
        {
            var c = new Chat();
            for (int i = 0; i < 60; i++) c.Her(i % 3, 20, i % 60, "第" + i + "条");
            Assert.That(c.Stats().EnoughHistory, Is.False, "60 lines in 3 days is not enough");
            c.Me(6, 21, 0, "晚安");
            Assert.That(c.Stats().EnoughHistory, Is.True);
            var trimmed = new LoveStats(new Chat().Her(0, 20, 0, "hi").Her(8, 20, 0, "hi").lines, 500);
            Assert.That(trimmed.TotalMessages, Is.EqualTo(500), "a trimmed save counts every line ever written");
            Assert.That(trimmed.EnoughHistory, Is.True);
        }

        [Test]
        public void TrimmedHistoryUsesTheFirstLineEverForTheDaySpan()
        {
            var lines = new Chat().Her(10, 20, 0, "a").Her(11, 20, 0, "b").lines;
            Assert.That(new LoveStats(lines).DaySpan, Is.EqualTo(2));
            Assert.That(new LoveStats(lines, 2, Day0).DaySpan, Is.EqualTo(12));
        }

        [Test]
        public void ColdWordsAndGoodnightAreRecognised()
        {
            Assert.That(LoveStats.IsColdWord("哦"), Is.True);
            Assert.That(LoveStats.IsColdWord(" 嗯。"), Is.True);
            Assert.That(LoveStats.IsColdWord("随便~"), Is.True);
            Assert.That(LoveStats.IsColdWord("Whatever."), Is.True);
            Assert.That(LoveStats.IsColdWord("哦，那我先去洗澡了"), Is.False);
            Assert.That(LoveStats.SaysGoodnight("笨蛋 晚安[微笑]"), Is.True);
            Assert.That(LoveStats.SaysGoodnight("Good night!"), Is.True);
            Assert.That(LoveStats.SaysGoodnight("tonight we eat"), Is.False);
        }

        [Test]
        public void EmojiCountsYyFacesPictureCharactersAndKaomoji()
        {
            Assert.That(LoveStats.Emoji("[微笑][抠鼻]好的"), Is.EqualTo(2));
            Assert.That(LoveStats.Emoji("概率论再见👋"), Is.EqualTo(1));
            Assert.That(LoveStats.Emoji("嘿嘿(^_^)"), Is.EqualTo(1));
            Assert.That(LoveStats.Emoji("没有表情"), Is.EqualTo(0));
            Assert.That(LoveStats.Length("再见👋"), Is.EqualTo(3));
        }

        [Test]
        public void PicksHerLongestLatestColdAndUnanswered()
        {
            var s = new Chat()
                .Her(0, 20, 0, "今天室友又把我的酸奶喝了，我真的会谢，下次一定要写名字在上面")
                .Me(0, 20, 5, "哈哈")
                .Her(1, 0, 40, "你还不睡吗")
                .Me(1, 0, 41, "马上")
                .Her(2, 19, 0, "下周三考概率论，你说我能过吗？")
                .Her(2, 21, 0, "人呢")
                .Me(3, 9, 0, "早")
                .Her(3, 9, 30, "哦")
                .Stats();
            Assert.That(s.Get(LovePickKind.Longest).message.text, Does.StartWith("今天室友"));
            Assert.That(s.Get(LovePickKind.Latest).message.text, Is.EqualTo("你还不睡吗"), "00:40 is later in the night than 21:00");
            Assert.That(s.Get(LovePickKind.Cold).message.text, Is.EqualTo("哦"));
            Assert.That(s.Get(LovePickKind.Unanswered).message.text, Does.StartWith("下周三考概率论"), "a question you answered 14 hours later");
            Assert.That(s.picks.Count, Is.EqualTo(4));
        }

        [Test]
        public void OneMessageIsNeverPickedTwice()
        {
            var s = new Chat().Her(0, 23, 50, "嗯").Stats();
            Assert.That(s.picks.Count, Is.EqualTo(1));
            Assert.That(s.picks[0].kind, Is.EqualTo(LovePickKind.Longest));
            Assert.That(s.Has(LovePickKind.Cold), Is.False);
        }

        [Test]
        public void HerReplyDelayIsAveragedPerWeekFromPlayTime()
        {
            var s = new Chat()
                .Me(0, 20, 0, "在吗").Her(0, 20, 2, "在")          // 2 min, week 0
                .Me(1, 20, 0, "吃了吗").Her(1, 20, 4, "吃了")       // 4 min, week 0
                .Me(8, 20, 0, "在吗").Her(8, 21, 0, "嗯")           // 60 min, week 1
                .Stats();
            Assert.That(s.weeks.Count, Is.EqualTo(2));
            Assert.That(s.weeks[0].herReplyDelay, Is.EqualTo(180).Within(1e-6));
            Assert.That(s.weeks[1].herReplyDelay, Is.EqualTo(3600).Within(1e-6));
            Assert.That(s.HerReplyDelay, Is.EqualTo((120 + 240 + 3600) / 3.0).Within(1e-6));
            Assert.That(LoveStats.Trend(s.Series(w => w.herReplyDelay)), Is.EqualTo(1), "slower replies are a rising delay");
        }

        [Test]
        public void ReplyLaterThanADayIsNotAReply()
        {
            var s = new Chat().Me(0, 20, 0, "在吗").Her(3, 20, 0, "在").Stats();
            Assert.That(s.HerReplyDelay, Is.EqualTo(-1));
            Assert.That(s.weeks[0].herReplyDelay, Is.EqualTo(-1));
        }

        [Test]
        public void GoodnightFirstCountsOncePerNightAcrossMidnight()
        {
            var s = new Chat()
                .Her(0, 23, 30, "晚安").Me(0, 23, 31, "晚安")     // night 0: her
                .Me(2, 0, 30, "我睡了 晚安").Her(2, 0, 31, "晚安")  // 00:30 belongs to night 1: me
                .Her(2, 23, 0, "晚安笨蛋")                         // night 2: her
                .Stats();
            Assert.That(s.HerGoodnightFirst, Is.EqualTo(2));
            Assert.That(s.MyGoodnightFirst, Is.EqualTo(1));
            Assert.That(s.weeks[0].herGoodnightFirst + s.weeks[0].myGoodnightFirst, Is.EqualTo(3));
        }

        [Test]
        public void LengthAndEmojiAreHerAveragesPerWeek()
        {
            var s = new Chat().Her(0, 20, 0, "abcd[微笑]").Her(0, 20, 1, "ab").Me(0, 20, 2, "长长长长长长长长长长").Stats();
            Assert.That(s.weeks[0].herLength, Is.EqualTo((8 + 2) / 2.0).Within(1e-6));
            Assert.That(s.weeks[0].herEmoji, Is.EqualTo(.5).Within(1e-6));
            Assert.That(s.weeks[0].herMessages, Is.EqualTo(2));
        }

        [Test]
        public void MyRecentReplyDelayLooksAtTheLastSevenDays()
        {
            var s = new Chat()
                .Her(0, 10, 0, "在吗").Me(0, 10, 1, "在")                // old and quick, ignored
                .Her(20, 10, 0, "在吗").Me(20, 13, 12, "刚看到")          // 3 h 12 min
                .Stats();
            Assert.That(s.MyRecentReplyDelay, Is.EqualTo(3 * 3600 + 12 * 60).Within(1e-6));
            Assert.That(LoveStats.Duration(s.MyRecentReplyDelay, false), Is.EqualTo("3 小时 12 分"));
            Assert.That(LoveStats.Duration(s.MyRecentReplyDelay, true), Is.EqualTo("3 h 12 min"));
        }

        [Test]
        public void ChartsShowTheLatestEightWeeks()
        {
            var c = new Chat();
            for (int d = 0; d < 7 * 12; d += 7) c.Her(d, 20, 0, "第" + d + "天");
            var s = c.Stats();
            Assert.That(s.weeks.Count, Is.EqualTo(12));
            Assert.That(s.ChartSlice().Count, Is.EqualTo(LoveStats.ChartWeeks));
        }

        [Test]
        public void TrendNeedsFifteenPercent()
        {
            Assert.That(LoveStats.Trend(new double[] { 10, 10, 11, 11 }), Is.EqualTo(0));
            Assert.That(LoveStats.Trend(new double[] { 10, 10, 5, 5 }), Is.EqualTo(-1));
            Assert.That(LoveStats.Trend(new double[] { -1, 10, -1, 20 }), Is.EqualTo(1), "missing weeks are skipped");
            Assert.That(LoveStats.Trend(new double[] { 3 }), Is.EqualTo(0));
        }

        [Test]
        public void PromptSlotsQuoteRealNumbersInBothLanguages()
        {
            var s = new Chat().Me(0, 20, 0, "在吗").Her(0, 20, 2, "在").Me(8, 20, 0, "在吗").Her(8, 21, 0, "嗯").Her(8, 23, 0, "晚安").Stats();
            Assert.That(s.ReplyTrendText(false), Does.Contain("2 分钟").And.Contain("1 小时 0 分"));
            Assert.That(s.ReplyTrendText(true), Does.Contain("2 min").And.Contain("1 h 0 min"));
            Assert.That(s.GoodnightText(false), Is.EqualTo("先说晚安的：她 1 晚，你 0 晚"));
            Assert.That(s.ColdText(false), Does.Contain("回了「嗯」"));
            Assert.That(s.ColdText(true, "Some people never get it."), Does.Contain("\"嗯\"").And.Contain("signature"));
            Assert.That(new Chat().Her(0, 20, 0, "你好呀").Stats().ColdText(false), Is.EqualTo("没有"));
            Assert.That(LoveStats.Spoken(new DateTime(2016, 10, 1, 0, 40, 0), false), Is.EqualTo("12 点 40"));
            Assert.That(LoveStats.Spoken(new DateTime(2016, 10, 1, 0, 40, 0), true), Is.EqualTo("12:40 a.m."));
            Assert.That(LoveStats.Spoken(new DateTime(2016, 10, 1, 23, 5, 0), false), Is.EqualTo("23 点 05"));
        }

        [Test]
        public void EmptyChatIsSafe()
        {
            var s = new LoveStats(null);
            Assert.That(s.Count, Is.EqualTo(0));
            Assert.That(s.DaySpan, Is.EqualTo(0));
            Assert.That(s.picks, Is.Empty);
            Assert.That(s.weeks, Is.Empty);
            Assert.That(s.EnoughHistory, Is.False);
            Assert.That(s.ReplyTrendText(false), Is.EqualTo("没有可算的回复"));
        }
    }
}
