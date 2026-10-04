using System;
using System.Collections.Generic;
using System.IO;
using LingGuangV05.Core;
using LingGuangV05.Core.Era;
using LingGuangV05.Core.Story;
using NUnit.Framework;

namespace LingGuangV05.Tests
{
    /// <summary>Design v1.1 §11.7 calendar, §14 era events and the post-2016 meme filter.</summary>
    public sealed class EraCalendarTests
    {
        static string Resources()
        {
            foreach (var start in new[] { Directory.GetCurrentDirectory(), TestContext.CurrentContext.TestDirectory })
                for (var dir = new DirectoryInfo(start); dir != null; dir = dir.Parent)
                {
                    string candidate = Path.Combine(dir.FullName, "Assets", "LingGuangV05", "Resources");
                    if (Directory.Exists(Path.Combine(candidate, "LingGuangV05", "Era"))) return candidate;
                }
            Assert.Fail("Resources folder not found.");
            return null;
        }

        static EraEvents Events() => EraEvents.Parse(File.ReadAllText(Path.Combine(Resources(), "LingGuangV05", "Era", "era_events.json")));

        [Test]
        public void EachStageIsItsMonthAndTheDateFollowsProgress()
        {
            Assert.AreEqual(new DateTime(2016, 5, 24), GameCalendar.DateFor(0, 0), "prologue");
            Assert.AreEqual(new DateTime(2016, 6, 1), GameCalendar.DateFor(1, 0));
            Assert.AreEqual(new DateTime(2016, 6, 15), GameCalendar.DateFor(1, .5));
            Assert.AreEqual(new DateTime(2016, 6, 30), GameCalendar.DateFor(1, 1));
            Assert.AreEqual(new DateTime(2016, 7, 1), GameCalendar.DateFor(2, 0));
            Assert.AreEqual(new DateTime(2016, 8, 31), GameCalendar.DateFor(3, 1));
            Assert.AreEqual(new DateTime(2016, 9, 30), GameCalendar.DateFor(4, 1));
            Assert.AreEqual(new DateTime(2016, 10, 1), GameCalendar.DateFor(5, 0), "stage 5 is October and November");
            Assert.AreEqual(new DateTime(2016, 11, 30), GameCalendar.DateFor(5, 1));
            Assert.AreEqual(new DateTime(2016, 12, 30), GameCalendar.DateFor(6, 1), "New Year's Eve is the ending");
            for (int stage = 1; stage <= 6; stage++)
                for (double p = 0; p < 1; p += .05) Assert.LessOrEqual(GameCalendar.DayFor(stage, p), GameCalendar.DayFor(stage, p + .05));
        }

        [Test]
        public void TheDateOnlyMovesForwardAndOldMomentsKeepTheirDay()
        {
            var s = new GameState();
            Assert.AreEqual(new DateTime(2016, 5, 24, 1, 47, 0), GameCalendar.Now(s), "a new save starts in the prologue night");
            s.gameSeconds = 600;
            Assert.IsTrue(GameCalendar.Advance(s, GameCalendar.DayFor(1, 0)));
            s.gameSeconds = 900;
            Assert.IsFalse(GameCalendar.Advance(s, GameCalendar.DayFor(0, 1)), "never backwards");
            Assert.IsTrue(GameCalendar.Advance(s, GameCalendar.DayFor(1, .5)));
            Assert.AreEqual(new DateTime(2016, 6, 15), GameCalendar.Now(s).Date);
            Assert.AreEqual(new DateTime(2016, 5, 24), GameCalendar.ClockFor(s, 300).Date, "a chat line from the prologue keeps its day");
            Assert.AreEqual(new DateTime(2016, 6, 1), GameCalendar.ClockFor(s, 700).Date);
            Assert.AreEqual("01:57", GameCalendar.ClockFor(s, 600).ToString("HH:mm"), "hours and minutes run in real time");
            s.gameSeconds = 23 * 3600;
            Assert.AreEqual(new DateTime(2016, 6, 15), GameCalendar.Now(s).Date, "real midnight does not move the calendar");
            GameCalendar.Advance(s, 9999);
            Assert.AreEqual(GameCalendar.Ending, GameCalendar.Now(s).Date, "never past New Year's Eve");
        }

        [Test]
        public void TheEventTableCoversEveryMonthWithEnglish()
        {
            var events = Events();
            for (int month = 5; month <= 12; month++)
            {
                int count = 0;
                foreach (var channel in EraEvents.Channels) count += events.InMonth(channel, 2016, month).Count;
                Assert.GreaterOrEqual(count, 6, "§14.1: 6–8 events a month, month " + month);
                Assert.GreaterOrEqual(events.InMonth(EraEvents.News, 2016, month).Count, 2, "news in month " + month);
                Assert.GreaterOrEqual(events.InMonth(EraEvents.YY, 2016, month).Count + events.InMonth(EraEvents.Tieba, 2016, month).Count, 1, "chatter in month " + month);
            }
            foreach (var e in events.All)
            {
                Assert.AreEqual(e.title.Length > 0, e.titleEn.Length > 0, e.id);
                Assert.AreEqual(e.text.Length > 0, e.textEn.Length > 0, e.id);
            }
        }

        [Test]
        public void NothingDatedAfterTodayAndPushEventsStayInTheirMonth()
        {
            var events = Events();
            var today = new DateTime(2016, 7, 15);
            foreach (var e in events.Visible(EraEvents.News, today)) Assert.LessOrEqual(e.date, today, e.id);
            Assert.IsNotEmpty(events.Visible(EraEvents.News, today));
            var due = events.Due(EraEvents.YY, today, new List<string>());
            foreach (var e in due) { Assert.AreEqual(7, e.date.Month, e.id + ": an older month's chat is not replayed"); Assert.LessOrEqual(e.date, today); }
            var delivered = new List<string>();
            foreach (var e in due) delivered.Add(e.id);
            Assert.IsEmpty(events.Due(EraEvents.YY, today, delivered), "delivered once");
            Assert.IsNotEmpty(events.Repeating(EraEvents.Tray, today), "the Windows 10 offer keeps coming back in July");
            Assert.IsEmpty(events.Repeating(EraEvents.Tray, new DateTime(2016, 7, 30)), "and stops after 29 July");
            Assert.IsEmpty(events.Repeating(EraEvents.Tray, new DateTime(2016, 6, 30)));
        }

        [Test]
        public void SensitiveEventsStayOneLineAndTragediesAreLeftOut()
        {
            var events = Events();
            foreach (var id in new[] { "brexit", "south_china_sea", "g20", "us_election", "yangjiang" }) Assert.IsTrue(events.Get(id).brief, id);
            string[] avoided = { "乔任梁", "雷洋", "和颐", "江歌", "罗尔", "辱母", "魏则西" };
            foreach (var e in events.All)
                foreach (var word in avoided) Assert.IsFalse((e.title + e.text).Contains(word), e.id + " " + word);
        }

        [Test]
        public void NoPost2016MemeInAnyAuthoredLine()
        {
            var lines = new List<string>();
            foreach (var e in Events().All) { lines.Add(e.title); lines.Add(e.text); lines.Add(e.who); }
            foreach (var file in Directory.GetFiles(Path.Combine(Resources(), "LingGuangV05", "Story"), "*.json"))
                Collect(StoryJson.Parse(File.ReadAllText(file)), lines);
            foreach (var file in Directory.GetFiles(Path.Combine(Resources(), "Localization"), "*.json"))
                Collect(StoryJson.Parse(File.ReadAllText(file)), lines);
            Assert.IsEmpty(EraLexicon.Audit(lines));
        }

        static void Collect(object node, List<string> into)
        {
            if (node is string s) into.Add(s);
            else if (node is List<object> list) foreach (var item in list) Collect(item, into);
            else if (node is Dictionary<string, object> map) foreach (var value in map.Values) Collect(value, into);
        }

        [Test]
        public void TheFilterCatchesMemeFormsButKeepsPlain2016Words()
        {
            foreach (var meme in new[] { "给你安排上了", "真香！", "扎心了，老铁", "吃鸡吗", "打call", "C位出道", "油腻中年", "佛系训练", "skr skr" })
                Assert.IsNotNull(EraLexicon.FirstBanned(meme), meme);
            foreach (var plain in new[] { "这菜太油腻了", "晚饭吃鸡腿", "这饭真香", "给你安排一个任务", "SKT 三冠", "Faker" })
                Assert.IsNull(EraLexicon.FirstBanned(plain), plain);
            Assert.AreEqual("训练好了，", EraLexicon.Scrub("训练好了，真香"));
            StringAssert.Contains("真香", EraLexicon.PromptRule(false));
            StringAssert.Contains("2016", EraLexicon.PromptRule(true));
        }
    }
}
