using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>The 书架 (名著喂养与结局_设计.md §一、§二): chapters read on the card, samples, parameters, reactions, quiz and quotes.</summary>
    public sealed class XgBooksTests
    {
        sealed class Host : IXgHost
        {
            public double money = 1e6, compute = 1, trained;
            public string blocker;
            public double Money => money;
            public double Compute => compute;
            public double VramMB => 4096;
            public string Blocker => blocker;
            public void Earn(double amount) { money += amount; }
            public bool Spend(double amount) { if (amount < 0 || money < amount) return false; money -= amount; return true; }
            public void Train(double seconds) { trained += seconds; }
        }

        static XgSim Able(int ability)
        {
            var sim = new XgSim { GoldChance = 0 };
            for (int i = 2; i <= ability; i++) sim.S.abilitiesEmerged.Add(i);
            return sim;
        }

        static void ReadNext(XgSim sim, string id, Host host)
        {
            Assert.IsNull(sim.StartReading(id, host), "it can start the next chapter of " + id);
            sim.Tick(XgSim.ReadMaxSeconds + 1, host);
            Assert.IsFalse(sim.Reading, "the chapter is read within " + XgSim.ReadMaxSeconds + " s");
        }

        static void ReadAll(XgSim sim, string id, Host host)
        {
            var book = XgBooks.Get(id);
            while (!sim.BookDone(book)) ReadNext(sim, id, host);
        }

        // ───────────── the data ─────────────

        [Test] public void TheShelfHoldsTheEightBooksInOrder()
        {
            CollectionAssert.AreEqual(new[] { "sanzijing", "lunyu", "tangshi", "xiyouji", "shuihu", "sanguo", "nahan", "hongloumeng" }, XgBooks.All.Select(b => b.id).ToArray());
            CollectionAssert.AreEqual(new[] { 2.0, 6, 20, 40, 80, 100, 150, 300 }, XgBooks.All.Select(b => b.paramsK).ToArray());
            foreach (var b in XgBooks.All)
            {
                Assert.That(b.ability, Is.InRange(2, 5), b.id);
                Assert.That(b.chapters.Length, Is.GreaterThanOrEqualTo(4), b.id);
                Assert.That(b.dataset == "poems" || b.dataset == "longtext", b.id + " feeds poems or longtext");
                Assert.IsNotNull(XgCatalog.Dataset(b.dataset), b.dataset);
                Assert.That(b.quiz.Length, Is.GreaterThanOrEqualTo(1), b.id);
                Assert.That(b.quotes.Length, Is.GreaterThanOrEqualTo(1), b.id);
                Assert.That(b.note.Length, Is.InRange(1, 60), b.id + " note");
                foreach (var c in b.chapters)
                {
                    Assert.That(c.excerpt.Length, Is.InRange(20, 220), b.id + " " + c.title);
                    Assert.That(c.chars, Is.GreaterThan(0), b.id + " " + c.title);
                    Assert.That(c.words.Length, Is.EqualTo(3), b.id + " " + c.title + " words");
                    Assert.That(c.names.Length, Is.GreaterThanOrEqualTo(1), b.id + " " + c.title + " names");
                    Assert.That(c.gist.Length, Is.InRange(2, 30), b.id + " " + c.title + " gist");
                    CollectionAssert.Contains(XgBooks.Tags, c.tag, b.id + " " + c.title + " tag");
                    Assert.IsFalse(c.excerpt.Contains("\""), "no straight quotes in " + c.title);
                }
                foreach (var q in b.quiz)
                {
                    Assert.That(q.chapter, Is.InRange(0, b.chapters.Length - 1), b.id + " " + q.q);
                    Assert.That(q.wrong.Length, Is.GreaterThanOrEqualTo(1), b.id + " " + q.q);
                    CollectionAssert.DoesNotContain(q.wrong, q.answer, b.id + " " + q.q);
                }
                foreach (var q in b.quotes)
                    foreach (var t in q.topics) Assert.IsTrue(XgBooks.Topics.ContainsKey(t), b.id + " quote topic " + t);
            }
            var red = XgBooks.Get(XgSim.RedMansionId);
            StringAssert.StartsWith("第一回", red.chapters[0].title);
            StringAssert.StartsWith("第八十回", red.chapters[red.chapters.Length - 1].title, "红楼梦 stops at 第八十回");
        }

        // ───────────── reading ─────────────

        [Test] public void BooksOpenWithTheirAbility()
        {
            var host = new Host();
            var sim = Able(1);
            Assert.IsFalse(sim.BookOpen(XgBooks.Get("sanzijing")));
            Assert.IsNotNull(sim.StartReading("sanzijing", host), "ability 1 cannot read yet");
            sim = Able(2);
            Assert.IsTrue(sim.BookOpen(XgBooks.Get("sanzijing")));
            Assert.IsTrue(sim.BookOpen(XgBooks.Get("lunyu")));
            Assert.IsFalse(sim.BookOpen(XgBooks.Get("xiyouji")), "西游记 waits for ability 3");
            Assert.IsFalse(sim.BookOpen(XgBooks.Get(XgSim.RedMansionId)), "红楼梦 waits for ability 5");
        }

        [Test] public void AChapterRunsOnTheCardBooksItsCharactersAndBillsPower()
        {
            var host = new Host();
            var sim = Able(2);
            var book = XgBooks.Get("sanzijing");
            double before = sim.ExtraSamples(book.dataset);
            Assert.IsNull(sim.StartReading(book.id, host));
            Assert.IsTrue(sim.Reading);
            Assert.That(sim.S.bookDuration, Is.InRange(XgSim.ReadMinSeconds, XgSim.ReadMaxSeconds));
            Assert.IsNotNull(sim.StartReading("lunyu", host), "one book at a time");
            sim.Tick(1, host);
            Assert.That(sim.ReadingProgress, Is.GreaterThan(0).And.LessThan(1));
            sim.Tick(XgSim.ReadMaxSeconds, host);
            Assert.AreEqual(1, sim.ChaptersRead(book));
            Assert.AreEqual(before + book.chapters[0].chars, sim.ExtraSamples(book.dataset), 1e-6, "1 character = 1 sample");
            Assert.That(host.trained, Is.GreaterThan(0), "reading pays training power");
            Assert.IsNotEmpty(sim.S.bookLine, "it says something about the chapter");
        }

        [Test] public void NoPowerNoReading()
        {
            var host = new Host();
            var sim = Able(2);
            Assert.IsNull(sim.StartReading("sanzijing", host));
            host.compute = 0;
            sim.Tick(30, host);
            Assert.IsTrue(sim.Reading);
            Assert.AreEqual(0, sim.ReadingProgress, 1e-9);
            host.compute = 1;
            sim.Tick(30, host);
            Assert.AreEqual(1, sim.ChaptersRead("sanzijing"));
        }

        [Test] public void ABookFinishedAddsItsParametersOutsideTheGradeRule()
        {
            var host = new Host();
            var sim = Able(2);
            sim.S.trainedParamsK = 5;
            var cards = new List<string>();
            sim.AchievementEarned += a => cards.Add(a.id);
            ReadAll(sim, "sanzijing", host);
            Assert.AreEqual(5 + 2, sim.TrainedParamsK, 1e-9, "三字经 adds 2K straight to P");
            Assert.AreEqual(5, sim.ModelParamsK, 1e-9, "the model part is unchanged");
            CollectionAssert.Contains(cards, "book.sanzijing");
            Assert.AreNotEqual(0, sim.BookDoneDay(XgBooks.Get("sanzijing")));
            Assert.IsNotNull(sim.StartReading("sanzijing", host), "a finished book cannot be read again");
        }

        // ───────────── what it makes of it ─────────────

        [Test] public void ItsReactionGrowsWithAbility()
        {
            var book = XgBooks.Get("xiyouji");
            var ch = book.chapters[0];
            StringAssert.EndsWith("？", Able(2).BookReaction(book, 0), "ability 2: a name and a question mark");
            Assert.AreEqual(string.Join(" / ", ch.words), Able(3).BookReaction(book, 0), "ability 3: three words");
            Assert.AreEqual(ch.gist, Able(4).BookReaction(book, 0), "ability 4: the gist");

            // Ability 5: a chapter of another book it read with the same theme.
            var sim = Able(5);
            XgBook other = null; int at = -1;
            foreach (var b in XgBooks.All)
            {
                if (b == book) continue;
                for (int c = 0; c < b.chapters.Length && other == null; c++) if (b.chapters[c].tag == ch.tag) { other = b; at = c; }
            }
            Assert.AreEqual(ch.gist, sim.BookReaction(book, 0), "nothing linked before another book is read");
            if (other == null) Assert.Inconclusive("no other book shares the tag " + ch.tag);
            sim.S.books.Add(new XgScore { key = other.id, count = at + 1 });
            string line = sim.BookReaction(book, 0);
            StringAssert.StartsWith(ch.gist, line);
            StringAssert.Contains("《" + other.name + "》", line, "ability 5 links a book it read");
        }

        [Test] public void ItsNoteFillsInWithAbility()
        {
            var book = XgBooks.Get("lunyu");
            var sim = Able(2);
            Assert.AreEqual("", sim.BookNote(book), "nothing before a chapter is read");
            sim.S.books.Add(new XgScore { key = book.id, count = book.chapters.Length });
            StringAssert.EndsWith("？", sim.BookNote(book));
            sim.S.abilitiesEmerged.AddRange(new[] { 3, 4, 5 });
            Assert.AreEqual(book.note, sim.BookNote(book), "ability 5 writes its whole-book note");
        }

        [Test] public void TheQuizGetsRightAsItReadsAndLearns()
        {
            var book = XgBooks.Get("xiyouji");
            var sim = Able(3);
            Assert.IsEmpty(sim.BookQuiz(), "no question before a book is started");
            Assert.AreEqual(0, sim.BookQuizChance(book), 1e-9);
            sim.S.books.Add(new XgScore { key = book.id, count = book.chapters.Length });
            sim.S.abilitiesEmerged.AddRange(new[] { 4, 5, 6 });
            var answer = sim.BookQuiz().Single();
            Assert.AreEqual(1, sim.BookQuizChance(book), 1e-9, "all read at ability 5+: always right");
            Assert.IsTrue(answer.right);
            Assert.AreEqual(answer.answer, answer.said);

            // A question whose chapter is unread is never right, and the wrong answer is a plausible one.
            var q = book.quiz[0];
            var early = Able(6);
            if (q.chapter == 0) Assert.Inconclusive("the first question is answered by chapter 1");
            early.S.books.Add(new XgScore { key = book.id, count = Math.Max(1, q.chapter) });
            var wrong = early.BookQuiz().Single();
            if (wrong.question == q.q)
            {
                Assert.IsFalse(wrong.right);
                CollectionAssert.Contains(q.wrong, wrong.said);
            }
        }

        [Test] public void ItQuotesOnlyBooksItFinished()
        {
            var book = XgBooks.Get("lunyu");
            var quote = book.quotes[0];
            string message = XgBooks.Topics[quote.topics[0]][0] + "了";
            var sim = Able(4);
            Assert.IsNull(sim.BookQuote(message, new Random(1)), "nothing read, nothing quoted");
            sim.S.books.Add(new XgScore { key = book.id, count = book.chapters.Length - 1 });
            Assert.IsNull(sim.BookQuote(message, new Random(1)), "half a book is not quoted");
            sim.S.books[0].count = book.chapters.Length;
            string said = sim.BookQuote(message, new Random(1));
            Assert.IsNotNull(said);
            StringAssert.StartsWith("《", said);
            Assert.IsTrue(XgBooks.All.Where(b => sim.BookDone(b)).Any(b => said.StartsWith("《" + b.name + "》")), "only a finished book");
        }

        [Test] public void ThePromptListsWhatItRead()
        {
            var sim = Able(4);
            sim.S.stage = 4;
            Assert.AreEqual("", sim.BooksReadLine());
            sim.S.books.Add(new XgScore { key = "sanzijing", count = XgBooks.Get("sanzijing").chapters.Length });
            sim.S.books.Add(new XgScore { key = "xiyouji", count = 2 });
            string line = sim.BooksReadLine();
            StringAssert.Contains("《三字经》", line);
            StringAssert.Contains("《西游记》读到", line);
            StringAssert.DoesNotContain("红楼梦", line);
            StringAssert.Contains(line, sim.PersonaBoard(9, new string[0]));
        }

        // ───────────── 红楼梦 stops at 第八十回 ─────────────

        [Test] public void AfterTheEightiethChapterItAsksWhatComesNext()
        {
            var host = new Host();
            var sim = Able(5);
            var red = XgBooks.Get(XgSim.RedMansionId);
            sim.S.books.Add(new XgScore { key = red.id, count = red.chapters.Length - 1 });
            var log = new List<string>();
            sim.Message += log.Add;
            ReadNext(sim, red.id, host);
            Assert.IsTrue(sim.BookDone(red));
            Assert.AreEqual(1, sim.S.redMansionBeat);
            Assert.IsTrue(log.Any(l => l.Contains("后面呢")), "it asks 「后面呢？」");
            StringAssert.Contains("那我也续一回", sim.AnswerRedMansion());
            Assert.AreEqual(2, sim.S.redMansionBeat);
            Assert.AreEqual("", sim.AnswerRedMansion(), "asked once");
        }
    }
}
