using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>The curtain call: 「你是怎么被训练出来的？」 before and after the ending.</summary>
    public sealed class XgOriginTests
    {
        static XgSim Ended(string ending)
        {
            var sim = new XgSim();
            sim.S.stage = 6; sim.S.chapterComplete = true; sim.S.ending = ending;
            return sim;
        }

        [Test]
        public void RecognisesTheQuestionInBothLanguages()
        {
            Assert.IsTrue(XgSim.IsOriginQuestion("你是怎么被训练出来的？"));
            Assert.IsTrue(XgSim.IsOriginQuestion("你从哪儿来"));
            Assert.IsTrue(XgSim.IsOriginQuestion("So how were you trained?"));
            Assert.IsFalse(XgSim.IsOriginQuestion("今天天气怎么样"));
            Assert.IsFalse(XgSim.IsOriginQuestion(""));
        }

        [Test]
        public void BeforeTheEndingItAnswersOnlyAsFarAsItCanTalk()
        {
            var sim = new XgSim();
            int asked = 0; sim.OriginVideoRequested += () => asked++;
            sim.S.stage = 1; Assert.AreEqual("否。", sim.Scripted("你是怎么被训练出来的？"));
            sim.S.stage = 2; Assert.AreEqual("……不确定。", sim.Scripted("你是怎么被训练出来的？"));
            sim.S.stage = 4; Assert.That(sim.Scripted("你从哪来"), Is.Not.Null.And.Length.LessThanOrEqualTo(8));
            sim.S.stage = 6; StringAssert.Contains("讲给你听", sim.Scripted("你是怎么被训练出来的？"));
            Assert.AreEqual(0, asked, "no video before the ending");
            Assert.IsFalse(sim.OfferOriginQuestion);
        }

        [Test]
        public void ChapterSixPlaysTheVideoOnceAndCountsAsTheExplanation()
        {
            var sim = new XgSim();
            sim.S.stage = 5; Assert.IsFalse(sim.TakeChapterSixOrigin(), "not before chapter six");
            sim.S.stage = 6;
            Assert.IsFalse(sim.TakeChapterSixOrigin(), "not before everything has opened up");
            sim.S.fullOpen = true;
            Assert.IsTrue(sim.TakeChapterSixOrigin());
            Assert.IsFalse(sim.TakeChapterSixOrigin(), "only once");
            Assert.IsTrue(sim.S.originExplained);
            StringAssert.Contains("视频", sim.Scripted("你是怎么被训练出来的？"));
            sim.S.chapterComplete = true; sim.S.ending = "E1";
            Assert.IsFalse(sim.OfferOriginQuestion, "the curtain call does not ask again");
            var seen = new XgSim(); seen.S.stage = 6; seen.S.fullOpen = true; seen.S.originVideoSeen = true;
            Assert.IsFalse(seen.TakeChapterSixOrigin(), "an old save that already watched it");
        }

        [Test]
        public void AfterTheEndingItExplainsAndAsksForTheVideoOnce()
        {
            foreach (var ending in new[] { "E1", "E2", "E3-1", "E3-2" })
            {
                var sim = Ended(ending);
                int asked = 0; sim.OriginVideoRequested += () => asked++;
                Assert.IsTrue(sim.OfferOriginQuestion);
                string reply = sim.Scripted(sim.OriginQuestion);
                StringAssert.Contains("我来给你解释", reply, ending);
                Assert.AreEqual(1, asked);
                Assert.IsFalse(sim.OfferOriginQuestion, "the suggestion goes once it has explained");
                if (ending == "E1") Assert.AreEqual("好。我来给你解释。", reply);
            }
        }

        [Test]
        public void AfterTheVideoItAsksBackOnceAndTakesOneAnswer()
        {
            var sim = Ended("E2");
            sim.Scripted(sim.OriginQuestion);
            Assert.IsTrue(sim.OriginVideoFinished());
            Assert.AreEqual("你觉得呢？", sim.S.chat[sim.S.chat.Count - 1].text);
            Assert.IsTrue(sim.S.originAwaitingAnswer);
            Assert.IsFalse(sim.OriginVideoFinished(), "watching again does not ask again");
            Assert.IsTrue(sim.AnswerOrigin(false));
            Assert.AreEqual("那我们一起看。", sim.S.chat[sim.S.chat.Count - 1].text);
            Assert.IsFalse(sim.AnswerOrigin(true));
            Assert.IsTrue(sim.S.originVideoSeen);
        }

        [Test]
        public void TheHintComesOnceAndOnlyAfterTheEnding()
        {
            var sim = new XgSim();
            Assert.IsFalse(sim.TakeOriginHint());
            sim = Ended("E1");
            Assert.IsTrue(sim.TakeOriginHint());
            Assert.IsFalse(sim.TakeOriginHint());
        }

        [Test]
        public void EnglishRepliesExist()
        {
            var sim = Ended("E3-1"); sim.English = true;
            StringAssert.Contains("explain", sim.Scripted("How were you trained?"));
            Assert.AreEqual("What do you think?", sim.OriginVideoFinished() ? sim.S.chat[sim.S.chat.Count - 1].text : "");
        }
    }
}
