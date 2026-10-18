using System;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>老周 in YY (XgSim.ZhouChat.cs): three answers to every lesson and stage note, and steering without his model.</summary>
    public sealed class XgZhouChatTests
    {
        static void Clean(string s, string where)
        {
            Assert.That(s, Is.Not.Empty, where);
            Assert.That(s, Does.Not.Contain("灵光").And.Not.Contain("LingGuang").And.Not.Contain("涌现"), where);
        }

        static void Check(XgZhouReply[] replies, string where)
        {
            Assert.AreEqual(3, replies.Length, where);
            foreach (var r in replies) { Clean(r.zh, where); Clean(r.en, where); Clean(r.answerZh, where); Clean(r.answerEn, where); }
        }

        [Test]
        public void EveryLessonHasThreeAnswers()
        {
            foreach (var l in XgSim.TutorLessons) Check(XgSim.ZhouReplies(l.id), l.id);
            Assert.IsEmpty(XgSim.ZhouReplies("nope"));
        }

        [Test]
        public void EveryLaterStageOpensWithANote()
        {
            for (int stage = 2; stage <= 6; stage++)
            {
                var n = XgSim.ZhouStageNote(stage);
                Assert.IsNotNull(n, "stage " + stage);
                Clean(n.zh, "stage " + stage); Clean(n.en, "stage " + stage);
                Check(n.replies, "stage " + stage);
            }
            Assert.IsNull(XgSim.ZhouStageNote(1), "stage one has its lessons");
        }

        [Test]
        public void AButtonMatchesInEitherLanguageAndTypedTextDoesNot()
        {
            var r = XgSim.ZhouReplies("data");
            Assert.AreSame(r[1], XgSim.ZhouMatch(r, "模型吃什么长什么，啥意思？"));
            Assert.AreSame(r[0], XgSim.ZhouMatch(r, " Got it. I'll pick a dataset "));
            Assert.IsNull(XgSim.ZhouMatch(r, "你吃饭了吗"));
            Assert.IsNull(XgSim.ZhouMatch(null, "x"));
        }

        [Test]
        public void HumanRepliesNeverReceivePrivateState()
        {
            var sim = new XgSim();
            var step = new XgGuideStep { zh = "私密训练状态", en = "private training state" };
            foreach (bool english in new[] { false, true })
            {
                string ordinary = sim.ZhouSteerNow(step, english);
                sim.S.zhouKnows = true;
                Assert.AreEqual(ordinary, sim.ZhouSteerNow(step, english));
                Assert.IsEmpty(XgSim.ZhouGuideState(step, english));
                Assert.That(ordinary, Does.Not.Contain(step.zh).And.Not.Contain(step.en));
            }
        }
        [Test]
        public void PrivateReflectionPreservesThreeStepsWithoutTellingAnNpc()
        {
            var sim = new XgSim();
            sim.ZhouStageShown(2);
            Assert.AreEqual(1, sim.S.selfStudyReflections);
            Assert.AreEqual(0, sim.S.zhouDoubt);
            var first = XgSim.ZhouNode(sim.ZhouStageNoteNow(3).replies[1].next);
            var second = XgSim.ZhouNode(first.replies[0].next);
            var third = XgSim.ZhouNode(second.replies[1].next);
            Assert.AreEqual("confess3", third.id);
            Assert.IsTrue(sim.ZhouApply(third.replies[0].effect));
            Assert.IsTrue(sim.S.selfStudyResolved);
            Assert.IsFalse(sim.S.zhouKnows);
            Assert.IsFalse(sim.ZhouApply(third.replies[0].effect));
            foreach (var id in new[] { "confess1", "confess2", "confess3" })
                foreach (var reply in XgSim.ZhouNode(id).replies) { Clean(reply.zh, id); Clean(reply.en, id); }
        }
        [Test]
        public void LegacyKnowledgeCannotChangePrivateEnding()
        {
            var sim = new XgSim();
            string ending = sim.ZhouEndingLine(false);
            sim.S.zhouDoubt = 3; sim.S.zhouKnows = true;
            Assert.AreEqual(ending, sim.ZhouEndingLine(false));
            Clean(sim.ZhouEndingLine(true), "ending");
            Assert.AreEqual(3, sim.ZhouStageNoteNow(6).replies.Length);
        }
    }
}
