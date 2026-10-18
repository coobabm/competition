using System.Linq;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>The opening's doubts and first glimpse, and 《AI是怎么炼成的》 (XgSim.Primer.cs).</summary>
    public sealed class XgPrimerTests
    {
        static XgSim Opening()
        {
            var sim = new XgSim { GoldChance = 0 };
            sim.S.gradualSystems = true;
            sim.BeginOpening();
            Assert.That(sim.OpeningFocus, Is.True);
            return sim;
        }

        static void Cards(XgSim sim, double n)
        {
            sim.S.labels.RemoveAll(l => l.dataset == XgMemes.SenseDesk);
            sim.S.labels.Add(new XgLabelCount { dataset = XgMemes.SenseDesk, count = n });
        }

        [Test]
        public void HeDoubtsFirstAndOnlyThenGlimpsesThatItLearns()
        {
            var sim = Opening();
            sim.S.teachingFlow = false; // Only legacy openings use these lines.
            Assert.That(sim.OpeningVoice(), Is.Empty, "nothing before the first card");
            Cards(sim, 1);
            var what = sim.OpeningVoice();
            Assert.That(what.Select(l => l.zh), Is.EqualTo(new[] { "……这都是干什么的？", "让我给一个程序判对错？" }));
            Assert.That(sim.OpeningVoice(), Is.Empty, "once");
            Cards(sim, XgCatalog.SamplesToTrain);
            Assert.That(sim.OpeningVoice().Single().zh, Does.Contain("训练什么"));
            Assert.That(sim.YesNoAccuracy, Is.EqualTo(.5).Within(1e-9), "untrained: a coin toss");
            Assert.That(sim.OpeningVoice(), Is.Empty, "no glimpse while it still guesses");
            foreach (var line in what) Assert.That(line.en, Is.Not.Empty);
            foreach (var line in what) Assert.That(line.zh + line.en, Does.Not.Contain("灵光").And.Not.Contain("涌现"));
        }

        [Test]
        public void TwelveQuestionOpeningKeepsItsOwnNarrationAndAllocatesNoEmptyLists()
        {
            var sim = Opening();
            Cards(sim, 12);
            var first = sim.OpeningVoice();
            Assert.That(first, Is.Empty);
            Assert.That(sim.OpeningVoice(), Is.SameAs(first));
            Assert.That(sim.S.openingVoiceWhat || sim.S.openingVoiceTrain || sim.S.openingVoiceGlimpse, Is.False);
            sim.S.teachingFlow = false;
            sim.S.openingFocus = false;
            Assert.That(sim.OpeningVoice(), Is.SameAs(first));
        }

        [Test]
        public void TheBookIsReadButEmptyOfUse()
        {
            var sim = Opening();
            Assert.That(sim.PrimerChaptersShown, Is.EqualTo(0));
            string page = sim.PrimerPage();
            Assert.That(page, Does.Contain("打不开").And.Contain("已读 100%").And.Contain("用上的 0 章").And.Contain("最后一页"));
            Assert.That(page, Does.Not.Contain("是/否"), "no chapter title before it learns");
            sim.S.senseVoiceLearned = true;
            Assert.That(sim.PrimerChaptersShown, Is.EqualTo(1));
            Assert.That(sim.PrimerPage(), Does.Contain(XgSim.AbilityName(1, false)).And.Contain("用上的 1 章——都是你教的"));
            sim.S.abilitiesEmerged.Add(2); sim.S.abilitiesEmerged.Add(3);
            Assert.That(sim.PrimerChaptersShown, Is.EqualTo(3));
            Assert.That(sim.PrimerPage(), Does.Contain(XgSim.AbilityName(3, false)).And.Not.Contain(XgSim.AbilityName(4, false)));
            sim.English = true;
            Assert.That(sim.PrimerPage(), Does.Contain("Read 100%"));
        }
    }
}
