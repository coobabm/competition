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
            // The desktop's real-time clock is a static that outlives a Play session; this test runs from 01:47.
            var wall = GameCalendar.WallClock;
            GameCalendar.WallClock = null;
            try { DateOnlyMovesForward(); }
            finally { GameCalendar.WallClock = wall; }
        }

        static void DateOnlyMovesForward()
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
        public void WithAWallClockTheTimeOfDayIsTheRealOneAndOnlyTheDateIs2016()
        {
            var before = GameCalendar.WallClock;
            try
            {
                GameCalendar.WallClock = () => new DateTime(2026, 10, 5, 15, 30, 0);
                var s = new GameState { gameSeconds = 1000 };
                GameCalendar.Advance(s, GameCalendar.DayFor(1, 0));
                Assert.AreEqual(new DateTime(2016, 6, 1, 15, 30, 0), GameCalendar.Now(s), "the 2016 date with the real hours and minutes");
                Assert.AreEqual("15:20", GameCalendar.ClockFor(s, 400).ToString("HH:mm"), "a line written ten minutes of play ago");
                Assert.AreEqual(new DateTime(2016, 5, 24), GameCalendar.ClockFor(s, 400).Date, "and it keeps the day it was written on");
                GameCalendar.WallClock = () => new DateTime(2026, 10, 6, 0, 5, 0);
                Assert.AreEqual("23:55", GameCalendar.ClockFor(s, 400).ToString("HH:mm"), "times before the real midnight wrap back");
                Assert.AreEqual(new DateTime(2016, 6, 1), GameCalendar.Now(s).Date, "the real midnight never moves the calendar");
            }
            finally { GameCalendar.WallClock = before; }
        }

        [Test]
        public void StoryTimeNeverGoesBackwards()
        {
            var s = new GameState();
            double last = -1;
            for (int i = 0; i < 400; i++)
            {
                s.gameSeconds = i * 7.5;
                if (i % 9 == 0) GameCalendar.Advance(s, GameCalendar.CurrentDay(s) + 1 + i % 3);
                double now = GameCalendar.StorySeconds(s, s.gameSeconds);
                Assert.GreaterOrEqual(now, last);
                last = now;
            }
            Assert.Greater(GameCalendar.StorySeconds(s, s.gameSeconds), GameCalendar.CurrentDay(s) * 86400.0, "each calendar day counts as a whole day");
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
            DateTime previous = DateTime.MinValue;
            foreach (var e in due)
            {
                Assert.GreaterOrEqual(e.date, today.AddMonths(-1), e.id + ": chat older than a month is not replayed");
                Assert.LessOrEqual(e.date, today);
                Assert.GreaterOrEqual(e.date, previous, e.id + ": oldest first");
                previous = e.date;
            }
            Assert.IsTrue(due.Exists(x => x.id == "iceland"), "a line from the end of last month that never arrived is still delivered");
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

        [Test]
        public void LaterAiTermsAreRewordedButPlain2016WordsStay()
        {
            foreach (var term in new[] { "大模型", "大语言模型", "LLM", "ChatGPT", "提示词", "AI 幻觉", "对齐微调", "RAG", "思维链", "AIGC", "扩散模型", "词元" })
                Assert.IsNotNull(EraLexicon.FirstBannedAiTerm("它说：" + term), term);
            foreach (var plain in new[] { "把图放大模型就糊了", "他发烧出现了幻觉", "多智能体强化学习", "a rag doll", "语言模型", "Fragile", "偏好对齐" })
            {
                Assert.IsNull(EraLexicon.FirstBannedAiTerm(plain), plain);
                Assert.AreEqual(plain, EraLexicon.Scrub(plain), plain);
            }
            Assert.AreEqual("我是一个模型，不是聊天程序。", EraLexicon.Scrub("我是一个大模型，不是ChatGPT。"));
            Assert.AreEqual("I am a language model.", EraLexicon.Scrub("I am an LLM."));
            Assert.AreEqual("先分步思考，再按提示回答。", EraLexicon.Scrub("先思维链，再按提示词回答。"));
            // The player layer may say these words: authored-content audits stay meme-only.
            Assert.IsNull(EraLexicon.FirstBanned("不调用真实大模型"));
            CollectionAssert.IsEmpty(EraLexicon.AuditAiTerms(new[] { "训练好了", "AlphaGo 赢了" }));
            Assert.AreEqual(1, EraLexicon.AuditAiTerms(new[] { "训练好了", "我是 LLM" }).Count);
            StringAssert.Contains("ChatGPT", EraLexicon.PromptRule(false));
            StringAssert.Contains("ChatGPT", EraLexicon.PromptRule(true));
        }

        [Test]
        public void EraEventsUseNoLaterAiTerms()
        {
            var lines = new List<string>();
            foreach (var e in Events().All) { lines.Add(e.title); lines.Add(e.text); lines.Add(e.who); }
            Assert.IsEmpty(EraLexicon.AuditAiTerms(lines));
        }

        [Test]
        public void FreestyleKeeps2016SportsAndMusicButRejectsTheLaterCatchphrase()
        {
            foreach (var plain in new[] {
                "Sun Yang won the 200 m freestyle gold in Rio.",
                "Katie Ledecky won the women's 800 m freestyle in Rio in 2016.",
                "The freestyle swimming final starts tonight.",
                "Freestyle rap existed before 2016.",
                "孙杨拿到了里约男子200米自由泳金牌。" })
            {
                Assert.IsNull(EraLexicon.FirstBanned(plain), plain);
                Assert.AreEqual(plain, EraLexicon.Scrub(plain), plain);
            }
            foreach (var meme in new[] { "你有freestyle吗？", "你有 FREE STYLE 吗", "有 freestyle 么？", "你 有 freestyle 嘛！" })
            {
                Assert.IsNotNull(EraLexicon.FirstBanned(meme), meme);
                StringAssert.DoesNotContain("freestyle", EraLexicon.Scrub(meme).ToLowerInvariant().Replace(" ", ""));
            }
            string mixed = "Sun Yang won the 200 m freestyle gold in Rio. 你有freestyle吗？";
            StringAssert.StartsWith("Sun Yang won the 200 m freestyle gold in Rio.", EraLexicon.Scrub(mixed));
            Assert.IsNull(EraLexicon.FirstBanned(EraLexicon.Scrub(mixed)));
            CollectionAssert.DoesNotContain(EraLexicon.Banned, "freestyle");
        }
    }
}
