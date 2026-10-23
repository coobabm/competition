using NUnit.Framework;
using UnityEngine;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgTeachingFlowTests
    {
        sealed class Host : IXgHost
        {
            public double Money { get; private set; } = 100;
            public double Compute => 50;
            public double VramMB => 100000;
            public string Blocker => null;
            public void Earn(double amount) { Money += amount; }
            public bool Spend(double amount) { if (Money < amount) return false; Money -= amount; return true; }
            public void Train(double seconds) { }
        }

        [Test]
        public void TwelveAnswersOpenTechButOnlyBuyingTheCurveOpensTrain()
        {
            var sim = new XgSim(); var host = new Host(); sim.BeginOpening();
            Assert.IsFalse(sim.FeatureVisible("tree"));
            Assert.IsFalse(sim.TrainingUnlocked(XgTrack.Sequence));
            for (int i = 0; i < 11; i++) sim.SettleMiniAction("sense", true, host, 1);
            Assert.IsFalse(sim.TeachingTreeOpen);
            sim.SettleMiniAction("sense", false, host, 10);
            Assert.IsTrue(sim.FeatureVisible("tree"), "All submitted answers count, including errors");
            Assert.IsFalse(sim.TeachingTrainOpen);
            // The curve is ¥10 (定价重做 2026-10-24); fund it so the test is about the gate, not the wallet.
            host.Earn(sim.NodeCost(XgCatalog.Node("train.curve")));
            Assert.IsTrue(sim.BuyNode("train.curve", host));
            Assert.IsTrue(sim.FeatureVisible("train"));
            Assert.IsTrue(sim.TrainingUnlocked(XgTrack.Sequence));
        }

        [Test]
        public void ConsecutiveSetsNeverResetTheStreakAndErrorsDo()
        {
            var sim = new XgSim(); var host = new Host(); sim.BeginOpening();
            for (int i = 0; i < 121; i++)
            {
                sim.SettleMiniAction("sense", true, host, 20);
                sim.Tick(4, host); // Past the audio restart and old combo windows.
            }
            Assert.AreEqual(121, sim.S.combo);
            Assert.AreEqual(121, sim.TeachingAnswers);
            sim.SettleMiniAction("sense", false, host, 20);
            Assert.AreEqual(0, sim.S.combo);
            Assert.AreEqual(122, sim.TeachingAnswers);
        }

        [Test]
        public void AiAnswerRequiresTwoSetsAndOneTrainingRound()
        {
            var sim = new XgSim(); var host = new Host(); sim.BeginOpening();
            for (int i = 0; i < 24; i++) sim.SettleMiniAction("sense", true, host, 1);
            // Fund both purchases so the label.ai refusal below is about the training round, not the wallet.
            host.Earn(sim.NodeCost(XgCatalog.Node("train.curve")) + sim.NodeCost(XgCatalog.Node("label.ai")));
            Assert.IsTrue(sim.BuyNode("train.curve", host));
            Assert.IsFalse(sim.BuyNode("label.ai", host));
            sim.S.epochs = 1;
            Assert.IsTrue(sim.BuyNode("label.ai", host));
            Assert.IsTrue(sim.TeachingAiReady);
        }

        [Test]
        public void NewFlowSurvivesSavingAndOldProgressIsNotLocked()
        {
            var sim = new XgSim(); sim.BeginOpening(); sim.S.handCorrect = 12;
            var reloaded = new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(sim.S)));
            Assert.IsTrue(reloaded.S.teachingFlow);
            Assert.IsTrue(reloaded.TeachingTreeOpen);
            Assert.IsFalse(reloaded.TeachingTrainOpen);
            var old = new XgSim(); old.S.epochs = 2; old.BeginOpening();
            Assert.IsFalse(old.S.teachingFlow);
            Assert.IsTrue(old.TeachingTrainOpen);
        }
    }
}
