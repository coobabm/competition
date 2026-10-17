using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>老问题: the same question at every ability, day one's wrong answer next to today's.</summary>
    public sealed class XgOldQuestionTests
    {
        [Test]
        public void EveryAbilityGivesTheOldQuestionANewAnswer()
        {
            foreach (var track in new[] { "vision", "sequence" })
                foreach (var english in new[] { false, true })
                {
                    var sim = new XgSim(); sim.S.firstTrack = track;
                    var seen = new System.Collections.Generic.HashSet<string>();
                    for (int ability = 0; ability <= XgSim.AbilityCount; ability++)
                    {
                        string answer = sim.OldAnswer(ability, english);
                        Assert.IsNotEmpty(answer, track + " " + ability);
                        Assert.IsTrue(seen.Add(answer), track + " " + ability + " says something new: " + answer);
                        StringAssert.DoesNotContain("涌现", answer);
                    }
                    Assert.AreEqual(XgSim.AbilityCount + 1, sim.OldAnswerTrail(XgSim.AbilityCount, english).Count);
                }
            var vision = new XgSim(); vision.S.firstTrack = "vision";
            var seq = new XgSim(); seq.S.firstTrack = "sequence";
            Assert.AreNotEqual(vision.OldAnswer(3, false), seq.OldAnswer(3, false), "ability 3 follows the track it chose");
        }

        [Test]
        public void DayOneIsWrongAndEveryLaterAnswerIsRight()
        {
            var sim = new XgSim();
            Assert.AreEqual(false, XgSim.TaughtAnswer(XgSim.OldQuestionZh), "the old question is a statement the 常识 desk teaches: ice is not hot");
            Assert.AreEqual("是。", sim.OldAnswer(0, false), "day one gets it wrong");
            Assert.AreEqual("否。", sim.OldAnswer(1, false));
            for (int ability = 2; ability <= XgSim.AbilityCount; ability++) StringAssert.Contains("凉", sim.OldAnswer(ability, false), ability.ToString());
            var pair = sim.OldQuestionPair(4);
            Assert.AreEqual(XgSim.OldQuestionZh, pair.Question(false));
            Assert.AreEqual(sim.OldAnswer(0, true), pair.Before(true));
            Assert.AreEqual(sim.OldAnswer(4, true), pair.After(true));
        }

        [Test]
        public void TheAbilityPairNeverRepeatsTheOldQuestion()
        {
            for (int ability = 1; ability <= XgSim.AbilityCount; ability++)
                StringAssert.DoesNotContain("冰", new XgSim().BeforeAfter(ability).Question(false), ability.ToString());
        }
    }
}
