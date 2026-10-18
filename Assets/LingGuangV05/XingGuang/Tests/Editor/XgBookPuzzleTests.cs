using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>「它没读懂的地方」 (XgSim.BookPuzzles.cs) and the cold, short replies from stage 4 (XgSim.Persona.cs).</summary>
    public sealed class XgBookPuzzleTests
    {
        sealed class Host : IXgHost
        {
            public double money = 1e6;
            public double Money => money;
            public double Compute => 1;
            public double VramMB => 4096;
            public string Blocker => null;
            public void Earn(double amount) { money += amount; }
            public bool Spend(double amount) { if (amount < 0 || money < amount) return false; money -= amount; return true; }
            public void Train(double seconds) { }
        }

        static XgSim Lab(int stage, string book = null, int read = 0)
        {
            var sim = new XgSim { GoldChance = 0 };
            sim.Profile = new XgProfile { name = "小灯", self = "本机", callMe = "老大", personality = "嘴硬心软" };
            sim.S.stage = stage;
            sim.Today = 20160603;
            for (int i = 2; i <= stage; i++) sim.S.abilitiesEmerged.Add(i);
            if (book != null) sim.S.books.Add(new XgScore { key = book, count = read });
            return sim;
        }

        static XgBookPuzzle Ask(XgSim sim, string id)
        {
            var p = XgBooks.Puzzle(id, out _);
            Assert.IsNotNull(p, id);
            sim.AskBookPuzzle(p);
            Assert.AreSame(p, sim.BookPuzzlePending);
            return p;
        }

        // ───────────── the data ─────────────

        [Test] public void EveryBookHasQuestionsOnChaptersItHas()
        {
            var ids = new HashSet<string>();
            foreach (var book in XgBooks.LegacyBooks)
            {
                var list = XgBooks.Puzzles(book.id);
                Assert.That(list.Length, Is.GreaterThanOrEqualTo(2), book.id);
                foreach (var p in list)
                {
                    Assert.That(p.chapter, Is.InRange(0, book.chapters.Length - 1), p.id);
                    Assert.IsTrue(ids.Add(p.id), "unique " + p.id);
                    Assert.That(p.keys.Length, Is.GreaterThan(0), p.id);
                    Assert.That(p.keysEn.Length, Is.GreaterThan(0), p.id);
                    Assert.That(p.q.Length > 0 && p.qEn.Length > 0 && p.truth.Length > 0 && p.truthEn.Length > 0, p.id);
                    XgBook owner;
                    Assert.AreSame(p, XgBooks.Puzzle(p.id, out owner), p.id);
                    Assert.AreSame(book, owner, p.id);
                    Assert.IsFalse(p.q.Contains("涌现") || p.truth.Contains("涌现"), p.id);
                }
            }
            Assert.IsNull(XgBooks.Puzzle("nope.0", out _));
            Assert.IsNull(XgBooks.Puzzle("xiyouji.9", out _));
        }

        // ───────────── asking ─────────────

        [Test] public void ItAsksOnlyFromStageFourAboutChaptersItRead()
        {
            var host = new Host();
            var young = Lab(3, "xiyouji", 3);
            young.Tick(XgSim.BookAskEvery + 1, host);
            Assert.AreEqual("", young.S.bookPuzzle, "stage 3 cannot ask yet");

            var unread = Lab(4);
            unread.Tick(XgSim.BookAskEvery + 1, host);
            Assert.AreEqual("", unread.S.bookPuzzle, "nothing read, nothing to ask");

            var sim = Lab(4, "xiyouji", 3);
            sim.Tick(XgSim.BookAskEvery * .5, host);
            Assert.AreEqual("", sim.S.bookPuzzle, "not before a minute");
            sim.Tick(XgSim.BookAskEvery * .6, host);
            Assert.AreEqual("xiyouji.0", sim.S.bookPuzzle, "chapter 3 read, chapter 6's question waits");
            Assert.IsTrue(sim.S.chat.Any(l => l.from == "ai" && l.text == XgBooks.Puzzle("xiyouji.0", out _).q), "it asks in the YY chat");
        }

        [Test] public void ItAsksOneThingAtATimeAndNeverTwice()
        {
            var host = new Host();
            var sim = Lab(4, "xiyouji", 6);
            sim.Tick(XgSim.BookAskEvery + 1, host);
            Assert.AreEqual("xiyouji.0", sim.S.bookPuzzle);
            sim.Tick(XgSim.BookAskEvery * 3, host);
            Assert.AreEqual("xiyouji.0", sim.S.bookPuzzle, "it waits for the answer");
            sim.AnswerBookPuzzle("如来法力更大");
            Assert.AreEqual("", sim.S.bookPuzzle);
            sim.Tick(XgSim.BookAskEvery + 1, host);
            Assert.AreEqual("xiyouji.1", sim.S.bookPuzzle);
            sim.AnswerBookPuzzle("不知道");
            sim.Tick(XgSim.BookAskEvery * 2, host);
            Assert.AreEqual("", sim.S.bookPuzzle, "both asked; a skipped one is not asked again");
        }

        // ───────────── judging ─────────────

        [Test] public void TheOfflineJudgeReadsKeyWords()
        {
            var sim = Lab(4);
            var p = XgBooks.Puzzle("lunyu.1", out _);
            Assert.AreEqual(XgSim.PuzzleRight, sim.JudgeBookAnswer(p, "说时间过得快"));
            Assert.AreEqual(XgSim.PuzzleRight, sim.JudgeBookAnswer(p, "Time passing"));
            Assert.AreEqual(XgSim.PuzzleHalf, sim.JudgeBookAnswer(p, "说的是河水"));
            Assert.AreEqual(XgSim.PuzzleWrong, sim.JudgeBookAnswer(p, "说他朋友"));
            Assert.AreEqual(XgSim.PuzzleSkip, sim.JudgeBookAnswer(p, "不知道"));
            Assert.AreEqual(XgSim.PuzzleSkip, sim.JudgeBookAnswer(p, "  "));
            var q = XgBooks.Puzzle("lunyu.0", out _);
            Assert.AreEqual(XgSim.PuzzleSkip, sim.JudgeBookAnswer(q, "我也不明白"), "「不明白」 is not the key 「明白」");
            Assert.AreEqual(XgSim.PuzzleRight, sim.JudgeBookAnswer(q, "不清楚，大概是真学会了"));
        }

        [Test] public void AModelVerdictIsParsed()
        {
            Assert.AreEqual(XgSim.PuzzleRight, XgSim.ParseVerdict("对"));
            Assert.AreEqual(XgSim.PuzzleRight, XgSim.ParseVerdict("<think>嗯</think>「对。」"));
            Assert.AreEqual(XgSim.PuzzleHalf, XgSim.ParseVerdict("差不多"));
            Assert.AreEqual(XgSim.PuzzleWrong, XgSim.ParseVerdict("不对"));
            Assert.AreEqual(XgSim.PuzzleWrong, XgSim.ParseVerdict("Wrong."));
            Assert.AreEqual(XgSim.PuzzleHalf, XgSim.ParseVerdict("partly"));
            Assert.IsNull(XgSim.ParseVerdict(""));
            Assert.IsNull(XgSim.ParseVerdict("这个嘛"));
        }

        [Test] public void TheModelVerdictCanRaiseButNotLowerOrJudgeASkip()
        {
            var sim = Lab(4, "sanguo", 6);
            Ask(sim, "sanguo.1");
            sim.AnswerBookPuzzle("天上掉下来的", XgSim.PuzzleRight);
            Assert.AreEqual(XgSim.PuzzleRight, sim.S.bookAnswers.Last().verdict, "the model's verdict is kept");
            Ask(sim, "sanguo.0");
            sim.AnswerBookPuzzle("不知道", XgSim.PuzzleWrong);
            Assert.AreEqual(XgSim.PuzzleSkip, sim.S.bookAnswers.Last().verdict, "「不知道」 stays a skip");
            sim = Lab(4, "sanguo", 6); // A new pending question: answered ids are no longer reoffered.
            Ask(sim, "sanguo.0");
            sim.AnswerBookPuzzle("他怕曹操看穿他", XgSim.PuzzleHalf);
            Assert.AreEqual(XgSim.PuzzleRight, sim.S.bookAnswers.Last().verdict, "the book's key words are a floor under a harsh model");
        }

        // ───────────── the reply and the memory ─────────────

        [Test] public void TheReplyIsColdShortAndTellsTheTruthWhenWrong()
        {
            foreach (var book in XgBooks.LegacyBooks)
                foreach (var p in XgBooks.Puzzles(book.id))
                    foreach (var answer in new[] { p.keys[0], "随便说的", "不知道" })
                    {
                        var sim = Lab(4, book.id, book.chapters.Length);
                        Ask(sim, p.id);
                        string reply = sim.AnswerBookPuzzle(answer);
                        Assert.IsNotNull(reply, p.id);
                        Assert.That(reply.Length, Is.LessThanOrEqualTo(XgSim.PuzzleReplyChars + 1), p.id + ": " + reply);
                        Assert.IsFalse(reply.Contains("！") || reply.Contains("!") || reply.Contains("~"), p.id + ": " + reply);
                        if (answer == "随便说的") StringAssert.Contains(p.truth.Substring(0, 4), reply, p.id + " tells what the book means");
                    }
        }

        [Test] public void TheAnswerIsRememberedInTheRecordTheNoteAndThePrompt()
        {
            var sim = Lab(4, "shuihu", 5);
            Ask(sim, "shuihu.0");
            sim.AnswerBookPuzzle("打死人要偿命");
            var rec = sim.S.bookAnswers.Single();
            Assert.AreEqual("shuihu.0", rec.id);
            Assert.AreEqual("打死人要偿命", rec.answer);
            Assert.AreEqual(XgSim.PuzzleRight, rec.verdict);
            Assert.AreEqual(20160603, rec.day);
            StringAssert.Contains("主人说：打死人要偿命", sim.BookNote(XgBooks.Get("shuihu")));
            StringAssert.Contains("主人说「打死人要偿命」（对）", sim.BookAnswersLine());
            StringAssert.Contains("打死人要偿命", sim.PersonaBoard(5, new string[0]));
            Assert.AreEqual(0, sim.MemoryBook.Count, "stage 4 has no memory book yet");

            Ask(sim, "shuihu.1");
            sim.AnswerBookPuzzle("运气好");
            StringAssert.Contains("主人说：运气好（不对）", sim.BookNote(XgBooks.Get("shuihu")), "the note shows the latest answer and its verdict");
        }

        [Test] public void FromStageFiveTheMemoryBookKeepsIt()
        {
            var sim = Lab(5, "nahan", 3);
            Ask(sim, "nahan.1");
            sim.AnswerBookPuzzle("那是迷信");
            Assert.IsTrue(sim.MemoryBook.Any(e => e.text.Contains("呐喊") && e.text.Contains("那是迷信") && e.text.Contains("（对）")), "memory book entry: " + string.Join(" | ", sim.MemoryBook.Select(e => e.text)) + " open=" + sim.MemoryOpen + " stage=" + sim.S.stage);
            Ask(sim, "nahan.0");
            sim.AnswerBookPuzzle("跳过");
            Assert.IsFalse(sim.MemoryBook.Any(e => e.text.Contains("跳过")), "a skip is not remembered");
        }

        [Test] public void ABrokenSaveIsRepaired()
        {
            var sim = Lab(4, "xiyouji", 3);
            sim.S.bookPuzzle = "gone.3";
            sim.S.bookAskTimer = double.NaN;
            sim.S.bookAnswers.Add(new XgBookAnswerRecord { id = "gone.1", answer = "x" });
            sim.S.bookAnswers.Add(new XgBookAnswerRecord { id = "xiyouji.0", answer = "y", verdict = XgSim.PuzzleRight });
            sim.S.bookAnswers.Add(null);
            var loaded = new XgSim(sim.S) { GoldChance = 0 };
            Assert.AreEqual("", loaded.S.bookPuzzle);
            Assert.AreEqual(0, loaded.S.bookAskTimer);
            Assert.AreEqual(new[] { "xiyouji.0" }, loaded.S.bookAnswers.Select(a => a.id).ToArray());
            Assert.IsNull(loaded.AnswerBookPuzzle("嗯"), "nothing pending, no reply");
        }

        // ───────────── cold replies ─────────────

        [Test] public void CoolDropsTheWarmthAndCutsAtASentence()
        {
            var sim = Lab(4);
            Assert.AreEqual("好。知道了", sim.Cool("好！！知道了~", 20));
            Assert.AreEqual("今天训练了一轮。", sim.Cool("今天训练了一轮。你想看看结果吗？我可以给你讲讲。", 12));
            string cut = sim.Cool("一二三四五六七八九十一二三四五六七八九十", 10);
            Assert.That(cut.Length, Is.LessThanOrEqualTo(11), cut);
        }

        [Test] public void RepliesGetLongerOnlySlowly()
        {
            var sim = Lab(4);
            int first = sim.MaxReplyChars;
            Assert.That(first, Is.InRange(16, 28));
            sim.S.stage = 5;
            Assert.That(sim.MaxReplyChars, Is.InRange(16, 32));
            sim.S.stage = 6;
            Assert.That(sim.MaxReplyChars, Is.LessThanOrEqualTo(32));
        }
    }
}
