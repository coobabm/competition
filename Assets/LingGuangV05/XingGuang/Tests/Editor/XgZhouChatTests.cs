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
    }
}
