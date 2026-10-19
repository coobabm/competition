using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgForgetNightTests
    {
        [Test]
        public void StageFourOffersThreeRoundMemoryTestAndRecordsChronicle()
        {
            var sim = new XgSim();
            sim.S.stage = 4;
            sim.ForgetNightStart();
            Assert.IsTrue(sim.OfferForgetNight);
            Assert.AreEqual(XgSim.FnRounds[0].askZh, sim.ForgetNightChoice(false));
            Assert.AreEqual(XgSim.FnRounds[0].askEn, sim.ForgetNightChoice(true));

            for (int i = 0; i < XgSim.FnRounds.Length; i++)
            {
                string ask = sim.ForgetNightChoice(false);
                Assert.IsTrue(sim.ForgetNightMatches(ask));
                var round = sim.ForgetNightAnswer(ask);
                Assert.IsNotNull(round);
                Assert.AreEqual(XgSim.FnRounds[i].id, round.id);
                Assert.IsNotEmpty(round.replyZh);
                Assert.IsNotEmpty(round.replyEn);
            }

            Assert.IsTrue(sim.S.fnDone);
            Assert.IsFalse(sim.OfferForgetNight);
            Assert.IsTrue(sim.ChronicleHas(XgSim.ForgetNightChronicle));

            var chapters = sim.Chronicle();
            Assert.IsTrue(chapters.Exists(c => c.id == XgSim.ForgetNightChronicle));
        }
    }
}
