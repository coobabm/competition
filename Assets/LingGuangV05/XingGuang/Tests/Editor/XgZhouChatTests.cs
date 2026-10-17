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
        public void OfflineHeSteersBackToTheStep()
        {
            var step = new XgGuideStep { id = "ability.buy", zh = "买一个数据包", en = "Buy a dataset", whyZh = "新本事要新数据。", whyEn = "A new skill needs new data." };
            Assert.That(XgSim.ZhouSteer(step, false), Does.Contain("买一个数据包").And.Contain("新本事要新数据"));
            Assert.That(XgSim.ZhouSteer(step, true), Does.Contain("Buy a dataset"));
            Assert.That(XgSim.ZhouSteer(null, false), Is.Not.Empty);
            var note = new XgGuideStep { zh = "下一步：科技买「垃圾短信」 · ¥75", whyZh = "新本事要新数据：先买一个数据包。\n为「多选一」做准备。留些钱付电费。" };
            Assert.AreEqual("这个回头再说。眼下先科技买「垃圾短信」，新本事要新数据：先买一个数据包。", XgSim.ZhouSteer(note, false), "no 下一步, no price, one sentence of why");
            Assert.That(XgSim.ZhouGuideState(step, false), Does.Contain("他现在该做的一步：买一个数据包"));
        }

        [Test]
        public void HeGrowsSuspiciousAndTheConfessionIsOfferedFromStageThree()
        {
            var sim = new XgSim();
            var two = sim.ZhouStageNoteNow(2);
            Assert.That(two.zh, Does.Contain("一个学期"));
            sim.ZhouStageShown(2);
            Assert.AreEqual(1, sim.S.zhouDoubt);
            var three = sim.ZhouStageNoteNow(3);
            Assert.That(three.zh, Does.Contain("不像自学的"));
            var confess = XgSim.ZhouMatch(three.replies, "其实……电脑里有个东西在自己学");
            Assert.AreEqual("confess1", confess.next);
            Assert.AreEqual("confess1", sim.ZhouStageNoteNow(4).replies[2].next, "offered again at stage 4");
            Assert.IsFalse(Array.Exists(sim.ZhouStageNoteNow(5).replies, r => r.next.Length > 0), "at stage 5 he stops asking");
        }

        [Test]
        public void TheConfessionLeadsToHimBelievingIt()
        {
            var sim = new XgSim();
            string node = "confess1";
            var n1 = XgSim.ZhouNode(node);
            var n2 = XgSim.ZhouNode(n1.replies[0].next);
            var n3 = XgSim.ZhouNode(n2.replies[1].next);
            Assert.AreEqual("confess3", n3.id);
            Assert.IsFalse(sim.ZhouApply(n3.replies[1].effect), "backing out is only more doubt");
            Assert.IsTrue(sim.ZhouApply(n3.replies[0].effect), "the screenshot convinces him");
            Assert.IsTrue(sim.S.zhouKnows);
            Assert.IsFalse(sim.ZhouApply(XgSim.ZhouTold), "only once");
            int doubt = sim.S.zhouDoubt;
            sim.ZhouStageShown(4);
            Assert.AreEqual(doubt, sim.S.zhouDoubt, "no doubt once he knows");
            Assert.That(sim.ZhouStageNoteNow(4).zh, Does.Contain("你那个东西现在：").And.Contain("学会了 1 样"));
            foreach (var id in new[] { "confess1", "confess2", "confess3" })
                foreach (var r in XgSim.ZhouNode(id).replies) { Assert.IsNotEmpty(r.zh); Assert.IsNotEmpty(r.en); Assert.That(r.zh + r.answerZh, Does.Not.Contain("灵光").And.Not.Contain("涌现")); }
        }

        [Test]
        public void HisAdviceIsAboutTheProgramOnceHeKnows()
        {
            var sim = new XgSim();
            var step = new XgGuideStep { zh = "下一步：科技买「垃圾短信」 · ¥75", en = "Next: buy Spam", whyZh = "新本事要新数据。" };
            Assert.That(sim.ZhouSteerNow(step, false), Does.StartWith("这个回头再说"));
            sim.S.zhouKnows = true;
            Assert.That(sim.ZhouSteerNow(step, false), Does.StartWith("你那个东西（").And.Contain("科技买「垃圾短信」"));
        }

        [Test]
        public void TheEndingClosesWhatHeKnewOrGuessed()
        {
            var sim = new XgSim();
            Assert.That(sim.ZhouEndingLine(false), Does.Contain("请我吃饭"));
            sim.S.zhouDoubt = 3;
            Assert.That(sim.ZhouEndingLine(false), Does.Contain("我早猜到了"));
            sim.S.zhouKnows = true;
            Assert.That(sim.ZhouEndingLine(false), Does.Contain("辛苦了"));
            Assert.That(sim.ZhouStageNoteNow(6).zh, Does.Not.Contain("我一直没问你"));
            sim.S.zhouKnows = false;
            var six = sim.ZhouStageNoteNow(6);
            Assert.That(six.zh, Does.Contain("我一直没问你"));
            Assert.IsTrue(sim.ZhouApply(six.replies[0].effect), "at the end he can still be told");
        }
    }
}
