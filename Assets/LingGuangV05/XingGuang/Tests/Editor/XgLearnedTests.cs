using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>
    /// 学会的感觉 (XgSim.Learned.cs): the next ability's line is hidden, its 「同一道题」 stays wrong every round and every
    /// failed sitting and turns right in the sitting that passes, and a couple of rounds after each ability it does one
    /// thing nobody trained it to do.
    /// </summary>
    public sealed class XgLearnedTests
    {
        static XgSim Lab(double ready, int salt = 0)
        {
            var sim = new XgSim { GoldChance = 0 };
            sim.S.dataSalt = salt;
            sim.S.stageEpochs = 1;
            sim.S.trainedParamsK = sim.ParamsThreshold(sim.NextAbility) * ready;
            var l = new XgLabelCount { dataset = "sense", count = 0 };
            sim.S.labels.Add(l);
            double need = sim.SamplesThreshold(sim.NextAbility) * ready, lo = 0, hi = need * 10 + 100;
            for (int i = 0; i < 60; i++) { l.count = (lo + hi) / 2; if (sim.TrainedSamples < need) lo = l.count; else hi = l.count; }
            l.count = hi;
            return sim;
        }

        [Test]
        public void TheLineIsAQuestionMarkUntilTheBarIsPastIt()
        {
            var sim = Lab(.9);
            int next = sim.NextAbility;
            Assert.That(sim.ParamsLineText(next), Is.EqualTo("?"));
            Assert.That(sim.SamplesLineText(next), Is.EqualTo("?"));
            Assert.That(sim.ParamsLineText(next + 1), Is.EqualTo("?"), "a later ability's line too");
            sim.S.trainedParamsK = sim.ParamsThreshold(next);
            Assert.That(sim.ParamsLineKnown(next), Is.True);
            Assert.That(sim.ParamsLineText(next), Does.Contain("✓"));
            Assert.That(sim.SamplesLineText(next), Is.EqualTo("?"));
        }

        [Test]
        public void TheBarsFillOnOneScaleThatSaysNothingAboutTheLine()
        {
            var a = new XgSim(); a.S.trainedParamsK = 50;
            var b = new XgSim(); b.S.trainedParamsK = 50; b.S.unlocked.Add("mlp");
            Assert.That(b.ParamsThreshold(2), Is.LessThan(a.ParamsThreshold(2)));
            Assert.That(b.ParamsFill, Is.EqualTo(a.ParamsFill), "a lower line does not move the bar");
            a.S.trainedParamsK = 500;
            Assert.That(a.ParamsFill, Is.GreaterThan(b.ParamsFill));
            Assert.That(new XgSim().ParamsFill, Is.EqualTo(0));
        }

        [Test]
        public void TheSameQuestionNeverGetsRightBeforeItLearns()
        {
            var sim = Lab(1);
            var probe = sim.Probe;
            Assert.That(probe, Is.Not.Null);
            for (int round = 1; round < 40; round++)
            {
                Assert.That(sim.ProbeWrong(sim.NextAbility, round, false), Is.Not.EqualTo(probe.rightZh));
                Assert.That(sim.ProbeWrong(sim.NextAbility, round, true), Is.Not.EqualTo(probe.rightEn));
            }
            // Garbled differently from round to round, not one fixed wrong answer.
            var said = Enumerable.Range(1, 12).Select(r => sim.ProbeWrong(sim.NextAbility, r, false)).Distinct().Count();
            Assert.That(said, Is.GreaterThan(1));
            sim.ProbeLine(out bool right);
            Assert.That(right, Is.False);
        }

        [Test]
        public void EveryAbilityHasAProbeAndEveryProbeHasAWrongAnswer()
        {
            var sim = new XgSim();
            sim.Profile.name = "小光";
            for (int a = 2; a <= XgSim.AbilityCount; a++)
            {
                var p = sim.ProbeOf(a);
                Assert.That(p, Is.Not.Null, "ability " + a);
                Assert.That(p.rightZh, Is.Not.Empty); Assert.That(p.rightEn, Is.Not.Empty);
                Assert.That(sim.ProbeWrong(a, 3, false), Is.Not.Empty.And.Not.EqualTo(p.rightZh));
            }
            Assert.That(sim.ProbeOf(6).rightZh, Does.Contain("小光"));
        }

        [Test]
        public void TheProbeIsInEverySittingAndRightOnlyInTheOneThatPasses()
        {
            int passes = 0, fails = 0;
            for (int salt = 0; salt < 40; salt++)
            {
                var sim = Lab(.85, salt);
                var sheet = sim.TakeExam();
                Assert.That(sheet.items.Count, Is.EqualTo(XgSim.AbilityExamQuestions));
                var probe = sheet.items.Single(i => i.probe);
                Assert.That(sheet.items[0], Is.SameAs(probe), "the page opens on it");
                Assert.That(probe.right, Is.EqualTo(sheet.passed));
                Assert.That(sheet.rightCount, Is.EqualTo(sheet.items.Count(i => i.right)));
                if (sheet.passed) passes++; else fails++;
                if (!sheet.passed) continue;
                sim.RevealExam();
                Assert.That(sim.ClaimExamPass(), Is.True);
                var pair = sim.BeforeAfter(sheet.ability);
                Assert.That(pair.Question(false), Is.EqualTo(probe.qZh), "the cutscene shows the same question");
                Assert.That(pair.After(false), Is.EqualTo(probe.rightZh));
            }
            Assert.That(passes, Is.GreaterThan(0)); Assert.That(fails, Is.GreaterThan(0));
        }

        [Test]
        public void AFailedSittingDoesNotGiveTheLineAway()
        {
            for (int salt = 0; salt < 20; salt++)
            {
                var sim = Lab(.8, salt);
                var sheet = sim.TakeExam();
                foreach (var item in sheet.items.Where(i => !i.right))
                    Assert.That(item.reasonZh, Does.Not.Contain("%"));
            }
        }

        [Test]
        public void ACoupleOfRoundsAfterAnAbilityItDoesSomethingNobodyTaughtIt()
        {
            var sim = new XgSim { GoldChance = 0 };
            var host = new Host();
            var said = new List<int>();
            sim.UnpromptedDone += a => said.Add(a);
            sim.S.labels.Add(new XgLabelCount { dataset = "sense", count = 500 });
            sim.Profile.callMe = "老板";
            sim.AddLine("me", "明天我要去面试，祝我好运");
            sim.AddLine("me", "?");
            sim.S.stage = 5; sim.S.abilitiesEmerged = new List<int> { 1, 2, 3, 4, 5 };
            sim.S.stageEpochs = XgSim.UnpromptedAfterRounds;
            int lines = sim.S.chat.Count;
            sim.TrainEpoch(XgTrack.Sequence, host);
            sim.TrainEpoch(XgTrack.Vision, host);
            Assert.That(said, Is.EqualTo(new[] { 5 }), "once, for the ability it has now");
            var ai = sim.S.chat.Skip(lines).Where(l => l.from == "ai").ToList();
            Assert.That(ai.Count, Is.EqualTo(1));
            Assert.That(ai[0].text, Does.Contain("明天我要去面试"), "it recalls what you said days ago");
            Assert.That(sim.S.unprompted, Has.Member(2).And.Member(4), "older ones are skipped, not played late");
        }

        [Test]
        public void EachMomentHasBothLanguagesAndNeverSaysTheForbiddenWord()
        {
            foreach (bool english in new[] { false, true })
                for (int a = 2; a <= XgSim.AbilityCount; a++)
                {
                    var sim = new XgSim { English = english };
                    int before = sim.S.chat.Count;
                    sim.Unprompted(a);
                    foreach (var l in sim.S.chat.Skip(before)) Assert.That(l.text, Does.Not.Contain("涌现"));
                    if (a >= 3) Assert.That(sim.S.chat.Count, Is.EqualTo(before + 1), "ability " + a + " speaks on YY");
                }
        }

        sealed class Host : IXgHost
        {
            public double Money => 1e9; public bool Spend(double amount) => true; public void Earn(double amount) { }
            public double Compute => 4; public double VramMB => 1e5;
            public void Train(double seconds) { }
            public string Blocker => null;
        }
    }
}
