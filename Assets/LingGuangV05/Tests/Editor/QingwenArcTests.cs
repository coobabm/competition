using System;
using LingGuangV05.Core.Girlfriend;
using NUnit.Framework;

namespace LingGuangV05.Tests
{
    /// <summary>晴雯的支线 (QingwenArc): beats in order with the lab's progress, answers that move her, typed lines for her model.</summary>
    public sealed class QingwenArcTests
    {
        static GirlfriendState Started()
        {
            var s = new GirlfriendState();
            GirlfriendRules.Begin(s, GfNow.At(new DateTime(2016, 5, 24, 2, 0, 0), 0), 12345);
            return s;
        }

        [Test]
        public void TheFirstBeatComesAtOnceWithHerNightHello()
        {
            var s = Started();
            var b = QingwenArc.Due(s, 1, 0, 0, 10);
            Assert.AreEqual("hello", b.key);
            var lines = QingwenArc.Lines(s, b, 2, 0);
            Assert.AreEqual("还没睡？", lines[0]);
            QingwenArc.Opened(s, b, 10);
            Assert.IsTrue(s.done.Contains("life:firstNight"), "her ordinary hello is not sent again");
            Assert.IsNull(QingwenArc.Due(s, 9, 9, 99, 1000), "nothing new while one is open");
        }

        [Test]
        public void BeatsWaitForTheLabAndForTheGap()
        {
            var s = Started();
            QingwenArc.Opened(s, QingwenArc.Beat("hello"), 10);
            QingwenArc.Answer(s, "嗯，还在弄电脑", out _);
            Assert.IsNull(QingwenArc.Due(s, 1, 0, 10, 50), "too soon after the last one");
            Assert.IsNull(QingwenArc.Due(s, 1, 0, 2, 500), "the lab has not trained enough yet");
            Assert.AreEqual("busy", QingwenArc.Due(s, 1, 0, 3, 500).key);
            QingwenArc.Opened(s, QingwenArc.Beat("busy"), 500);
            QingwenArc.Answer(s, "秘密，以后告诉你", out _);
            Assert.IsNull(QingwenArc.Due(s, 1, 1, 30, 1000), "the next one needs stage 2");
            Assert.AreEqual("home", QingwenArc.Due(s, 2, 1, 30, 1000).key);
        }

        [Test]
        public void APickedAnswerMovesHerAndSetsItsFlag()
        {
            var s = Started();
            s.done.Add("arc:hello");
            int before = s.affection;
            QingwenArc.Opened(s, QingwenArc.Beat("busy"), 100);
            var b = QingwenArc.Answer(s, "Teaching myself machine learning. Teaching a computer to read", out var picked);
            Assert.AreEqual("busy", b.key);
            Assert.IsNotNull(picked, "the English button matches too");
            Assert.AreEqual(before + 2, s.affection);
            Assert.IsTrue(GirlfriendRules.Has(s, QingwenArc.FlagToldMl));
            Assert.That(s.memories.Exists(m => m.Contains("教电脑认字")));
            Assert.IsEmpty(s.arcOpen);
            Assert.IsTrue(QingwenArc.Done(s, b));
            var future = QingwenArc.Beat("future");
            Assert.That(QingwenArc.Lines(s, future, 20, 0), Has.Member("你那个教电脑认字的 教会了没"), "a later beat remembers it");
        }

        [Test]
        public void ATypedAnswerGoesToHerModel()
        {
            var s = Started();
            int before = s.affection;
            QingwenArc.Opened(s, QingwenArc.Beat("hello"), 10);
            var b = QingwenArc.Answer(s, "刚把显卡装上", out var picked);
            Assert.IsNull(picked);
            Assert.AreEqual(before, s.affection, "no rule decides a free answer");
            Assert.IsTrue(QingwenArc.Done(s, b));
            Assert.That(QingwenArc.FreeSituation(b, false), Does.Contain("追问"));
        }

        [Test]
        public void AnUnansweredBeatClosesAndCosts()
        {
            var s = Started();
            int before = s.affection;
            QingwenArc.Opened(s, QingwenArc.Beat("hello"), 10);
            Assert.IsFalse(QingwenArc.Expire(s, 10 + QingwenArc.OpenFor - 1));
            Assert.IsTrue(QingwenArc.Expire(s, 10 + QingwenArc.OpenFor));
            Assert.AreEqual(before - QingwenArc.UnansweredCost, s.affection);
            Assert.IsEmpty(s.arcOpen);
        }

        [Test]
        public void EveryBeatHasThreeBilingualAnswersAndNeverNamesTheProgram()
        {
            foreach (var b in QingwenArc.Beats)
            {
                Assert.AreEqual(3, b.choices.Length, b.key);
                Assert.AreEqual(0, b.lines.Length % 2, b.key);
                foreach (var c in b.choices)
                {
                    Assert.IsNotEmpty(c.zh); Assert.IsNotEmpty(c.en);
                    Assert.AreEqual(0, c.reply.Length % 2, b.key + " " + c.zh);
                    Assert.That(c.zh + c.en + string.Join("", c.reply), Does.Not.Contain("灵光").And.Not.Contain("LingGuang").And.Not.Contain("涌现"));
                }
                Assert.That(string.Join("", b.lines), Does.Not.Contain("灵光"));
            }
        }
    }
}
