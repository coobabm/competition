using LingGuangV05.XingGuang;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    public class XgAfterthoughtTests
    {
        [Test]
        public void EachAfterthoughtIsSaidOncePerSaveAndSurvivesASaveRoundTrip()
        {
            var sim = new XgSim();
            Assert.IsNotNull(sim.TakeAfterthought("first.fine"));
            Assert.IsNull(sim.TakeAfterthought("first.fine"));
            var again = new XgSim(sim.S);
            Assert.IsNull(again.TakeAfterthought("first.fine"));
            Assert.IsNotNull(again.TakeAfterthought("first.report"));
        }

        [Test]
        public void UnknownKeysSayNothing()
        {
            Assert.IsNull(new XgSim().TakeAfterthought("nope"));
        }

        [Test]
        public void EveryAfterthoughtHasMatchingChineseAndEnglishBeats()
        {
            foreach (var key in XgSim.AfterthoughtKeys)
            {
                var t = new XgSim().TakeAfterthought(key, "洪荒之力", "primordial power");
                Assert.IsNotNull(t, key);
                Assert.AreEqual(t.zh.Split('|').Length, t.en.Split('|').Length, key);
                Assert.IsFalse(t.zh.Contains("{0}") || t.en.Contains("{0}"), key);
            }
        }

        [Test]
        public void TheDriftThoughtNamesTheMemeInEachLanguage()
        {
            var t = new XgSim().TakeAfterthought("first.drift", "洪荒之力", "primordial power");
            StringAssert.Contains("洪荒之力", t.zh);
            StringAssert.Contains("primordial power", t.en);
        }

        [Test]
        public void TheAiAsksWhoItsLabelsTeachOnceFromStageFive()
        {
            var sim = new XgSim();
            sim.S.autoCorrect = XgSim.ReflectionLabels;
            sim.S.stage = 4;
            Assert.IsNull(sim.TakeReflectionLine());
            sim.S.stage = 5;
            string line = sim.TakeReflectionLine();
            Assert.IsNotNull(line);
            Assert.AreEqual(line, sim.S.chat[sim.S.chat.Count - 1].text);
            Assert.AreEqual("ai", sim.S.chat[sim.S.chat.Count - 1].from);
            Assert.IsNull(sim.TakeReflectionLine());
        }

        [Test]
        public void TheAiDoesNotAskBeforeItHasLabelledEnough()
        {
            var sim = new XgSim();
            sim.S.stage = 6; sim.S.autoCorrect = XgSim.ReflectionLabels - 1;
            Assert.IsNull(sim.TakeReflectionLine());
        }
    }
}
