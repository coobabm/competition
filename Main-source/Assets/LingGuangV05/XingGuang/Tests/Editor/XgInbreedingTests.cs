using System.Collections.Generic;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>近亲繁殖: uncaught wrong automatic labels holding the model down, and the two cures.</summary>
    public sealed class XgInbreedingTests
    {
        sealed class Host : IXgHost
        {
            public double money = 100000;
            public double Compute => 1;
            public double VramMB => 16000;
            public double Money => money;
            public bool Spend(double amount) { if (amount < 0 || money < amount) return false; money -= amount; return true; }
            public void Earn(double amount) { money += amount; }
            public void Train(double seconds) { }
            public string Blocker => null;
        }

        /// <summary>A spam model trained well (closed-form curve, so the numbers are exact).</summary>
        static XgSim Trained(double noise, double labels = 100)
        {
            var sim = new XgSim { GoldChance = 0, UseBoard = false };
            sim.S.autoLevel = 1; sim.S.unlocked.Add("label.coop");
            sim.S.best.Add(new XgBest { dataset = "spam", acc = .8, arch = "perceptron" });
            sim.S.labels.Add(new XgLabelCount { dataset = "spam", count = labels });
            if (noise > 0) sim.S.noise.Add(new XgLabelCount { dataset = "spam", count = noise });
            sim.S.sequence.dataset = "spam"; sim.S.sequence.steps = 10000;
            sim.Evaluate(sim.S.sequence);
            return sim;
        }

        [Test]
        public void UpToTenPercentNoiseCostsNothingExtra()
        {
            var sim = Trained(10);
            Assert.AreEqual(.1, sim.NoiseShare("spam"), 1e-9);
            Assert.AreEqual(0, sim.InbreedingPenaltyFor("spam"));
            Assert.AreEqual(0, sim.S.sequence.inbreedingPenalty);
            Assert.IsFalse(sim.PhenomenonSeen(XgSim.InbreedingId));
        }

        [Test]
        public void NoiseAboveTenPercentCapsValidationAccuracy()
        {
            var clean = Trained(0, 1000); var noisy = Trained(300, 1000);
            var reference = Trained(0, 1000);
            // Same effective samples as the noisy run, without the cap: isolates the 近亲繁殖 penalty.
            reference.S.labels[0].count = noisy.EffectiveLabelSamples("spam");
            reference.Evaluate(reference.S.sequence);
            Assert.AreEqual(.1, noisy.InbreedingPenaltyFor("spam"), 1e-9, "0.5 × (30% − 10%)");
            Assert.AreEqual(.1, noisy.S.sequence.inbreedingPenalty, 1e-9);
            Assert.AreEqual(reference.S.sequence.valAcc - .1, noisy.S.sequence.valAcc, 1e-9);
            Assert.Less(noisy.S.sequence.valAcc, clean.S.sequence.valAcc);
            Assert.IsTrue(noisy.PhenomenonSeen(XgSim.InbreedingId));
        }

        [Test]
        public void TheCapNeverGoesBelowACoinToss()
        {
            var sim = Trained(500);
            var d = XgCatalog.Dataset("spam");
            Assert.GreaterOrEqual(sim.S.sequence.valAcc, 1 - d.chanceError - 1e-9);
        }

        [Test]
        public void AssessmentsShowTheLoss()
        {
            var sim = Trained(300, 1000); var host = new Host();
            var a = sim.Assess(XgTrack.Sequence, host);
            Assert.IsNotNull(a);
            Assert.AreEqual(.1, a.inbreeding, 1e-9);
            Assert.AreEqual(sim.S.sequence.valAcc, a.acc, 1e-9);
            Assert.IsTrue(sim.S.log.Exists(l => l.Contains("近亲繁殖")), "the assessment says why");
            var clean = Trained(0).Assess(XgTrack.Sequence, host);
            Assert.AreEqual(0, clean.inbreeding);
        }

        [Test]
        public void TheFirstCapLightsTheAtlasAndShowsOneInsightCard()
        {
            var node = XgCatalog.Node("ph." + XgSim.InbreedingId);
            Assert.IsNotNull(node, "an atlas node");
            var sim = Trained(30); var host = new Host();
            Assert.IsTrue(sim.AtlasLit(node));
            var cards = new List<string>(); sim.InsightCard += cards.Add;
            sim.Tick(2, host);
            CollectionAssert.AreEqual(new[] { XgSim.InbreedingId }, cards);
            sim.Tick(2, host);
            Assert.AreEqual(1, cards.Count, "once");
            XgSim.InsightCardText(XgSim.InbreedingId, out int stage, out string name, out string nameEn, out string golden, out string goldenEn, out string why, out string whyEn);
            Assert.AreEqual("近亲繁殖", name); Assert.AreEqual("Inbreeding", nameEn);
            StringAssert.Contains("自己的错", why); Assert.IsNotEmpty(whyEn); Assert.IsNotEmpty(golden); Assert.IsNotEmpty(goldenEn);
        }

        [Test]
        public void UncaughtWrongAutomaticLabelsBecomeTheNoise()
        {
            var sim = Trained(0); var host = new Host(); sim.ForcedCheckChance = 0; sim.ForcedTrapChance = 0;
            for (int i = 0; i < 25; i++)
            {
                var c = sim.CreateLabelCard("spam");
                c.hasJudgment = true; c.guess = !c.truth; c.confidence = .99; c.judgeSource = "checkpoint";
                sim.S.prefetch.Add(c);
                Assert.IsTrue(sim.Route(c.id, host));
            }
            Assert.AreEqual(25, sim.Noise("spam"));
            Assert.Greater(sim.S.sequence.inbreedingPenalty, 0);
        }

        [Test]
        public void CleaningNoiseLiftsTheCap()
        {
            var sim = Trained(30); var host = new Host(); sim.S.unlocked.Add("label.audit");
            double before = sim.S.sequence.valAcc;
            Assert.AreEqual(10, sim.CleanNoise("spam", host));
            Assert.AreEqual(.05, sim.InbreedingPenaltyFor("spam"), 1e-9, "20 noisy rows in 100");
            Assert.Greater(sim.S.sequence.valAcc, before);
        }

        [Test]
        public void RecheckSendsUpToTenOldRowsToTheInbox()
        {
            var sim = Trained(12);
            Assert.AreEqual(10, sim.RecheckableNoise("spam"));
            Assert.AreEqual(10, sim.RecheckNoise("spam"));
            Assert.AreEqual(10, sim.ReviewCount("spam")); Assert.AreEqual(10, sim.PendingRechecks("spam"));
            foreach (var c in sim.S.queue) { Assert.IsTrue(c.recheck); Assert.IsFalse(c.hasJudgment, "no model suggestion: a fresh human look"); }
            Assert.AreEqual(12, sim.Noise("spam"), "nothing is removed until a human answers");
            Assert.AreEqual(2, sim.RecheckNoise("spam"), "only rows not already waiting");
            Assert.AreEqual(0, sim.RecheckNoise("spam"));
            Assert.AreEqual(0, sim.RecheckNoise("nope"));
        }

        [Test]
        public void RecheckRespectsTheInboxCapacity()
        {
            var sim = Trained(30);
            for (int i = 0; i < 15; i++) sim.S.queue.Add(sim.CreateLabelCard("spam"));
            Assert.AreEqual(5, sim.RecheckNoise("spam"));
            Assert.IsTrue(sim.ReviewFull);
            Assert.AreEqual(0, sim.RecheckNoise("spam"));
        }

        [Test]
        public void ARightRecheckRemovesOneNoisyRowAndPaysNothingExtra()
        {
            var sim = Trained(30); var host = new Host(); sim.S.unlocked.Add("label.hard");
            sim.RecheckNoise("spam");
            var card = sim.ReviewCard("spam");
            double penalty = sim.InbreedingPenaltyFor("spam");
            var result = sim.AnswerQueued(card.id, card.truth, host);
            Assert.IsTrue(result.correct);
            Assert.AreEqual(29, sim.Noise("spam"));
            Assert.AreEqual(sim.ManualPayFor("spam", card.level) * sim.ComboMultiplier, result.pay, 1e-9, "no hard-case bonus on rechecks");
            Assert.AreEqual(1, result.samples);
            Assert.Less(sim.InbreedingPenaltyFor("spam"), penalty);

            var wrong = sim.ReviewCard("spam");
            Assert.IsFalse(sim.AnswerQueued(wrong.id, !wrong.truth, host).correct);
            Assert.AreEqual(29, sim.Noise("spam"), "a wrong answer leaves the row");
        }
    }
}
