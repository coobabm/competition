using System.Collections.Generic;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>A model trained to grade C counts towards P at once; no assessment is needed (and the old one still counts).</summary>
    public sealed class XgAbilityAutoCountTests
    {
        sealed class Host : IXgHost
        {
            public double money = 1000000, compute = 2;
            public double Compute => compute;
            public double VramMB => 100000;
            public double Money => money;
            public bool Spend(double amount) { if (money < amount) return false; money -= amount; return true; }
            public void Earn(double amount) { money += amount; }
            public void Train(double seconds) { }
            public string Blocker => null;
        }

        static XgSim SpamLab()
        {
            var sim = new XgSim { GoldChance = 0 };
            sim.S.labels.Add(new XgLabelCount { dataset = "spam", count = 200 });
            sim.SetDataset(XgTrack.Sequence, "spam");
            return sim;
        }

        [Test]
        public void TrainingToGradeCCountsTheParametersWithoutAnAssessmentAndSaysSo()
        {
            var sim = SpamLab(); var host = new Host(); var run = sim.S.sequence;
            int said = 0;
            sim.Message += m => { if (m.Contains("练到 C 级，参数算数了") || m.Contains("Grade C reached")) said++; };
            bool counted = false;
            for (int i = 0; i < 300 && !counted; i++)
            {
                double before = sim.TrainedParamsK;
                var e = sim.TrainEpoch(XgTrack.Sequence, host);
                Assert.IsNotNull(e);
                bool c = sim.ModelGrade(run) >= XgSim.TrainedGrade;
                if (!c) Assert.AreEqual(before, sim.TrainedParamsK, 1e-9, "below grade C nothing counts (epoch " + i + ")");
                else { counted = true; Assert.AreEqual(XgSim.ParamsK(run), sim.TrainedParamsK, 1e-9); }
            }
            Assert.IsTrue(counted, "spam reaches grade C");
            Assert.AreEqual(1, said, "one line when P rises");
            Assert.AreEqual(XgCountState.Counted, sim.CountState(run));
            StringAssert.Contains("→", sim.S.log.Find(l => l.Contains("参数算数了")) ?? sim.S.log.Find(l => l.Contains("Grade C reached")));
            // The next round at the same size says nothing more.
            sim.TrainEpoch(XgTrack.Sequence, host);
            Assert.AreEqual(1, said);
        }

        [Test]
        public void AModelAtGradeCButNotCountedYetCountsOnTheNextRound()
        {
            var sim = SpamLab(); var host = new Host(); var run = sim.S.sequence;
            // Grade C reached by another road (a loaded model, a changed desk): the next round counts it with no assessment.
            sim.AutoConfigure(run, host);
            run.shapeRounds = 2;
            run.valAcc = XgSim.GradeAccuracy("spam", XgSim.TrainedGrade) + .05;
            Assert.AreEqual(XgCountState.Unassessed, sim.CountState(run));
            sim.TrainEpoch(XgTrack.Sequence, host);
            Assert.AreEqual(XgSim.ParamsK(run), sim.TrainedParamsK, 1e-9);
        }

        [Test]
        public void BelowGradeCDoesNotCountAndAFreshShapeNeedsARound()
        {
            var sim = SpamLab(); var host = new Host(); var run = sim.S.sequence;
            sim.AutoConfigure(run, host);
            run.shapeRounds = 3; run.valAcc = XgSim.GradeAccuracy("spam", 0);
            Assert.AreEqual(XgCountState.BelowC, sim.CountState(run));
            Assert.IsNotNull(sim.Assess(XgTrack.Sequence, host));
            Assert.AreEqual(0, sim.TrainedParamsK, "an assessment below C counts nothing");
            run.shapeRounds = 0; run.valAcc = XgSim.GradeAccuracy("spam", XgSim.TrainedGrade) + .02;
            sim.Assess(XgTrack.Sequence, host);
            Assert.AreEqual(0, sim.TrainedParamsK, "grade C but not trained a round in this shape");
        }

        [Test]
        public void TheOldAssessmentPathStillCountsAndPays()
        {
            var sim = SpamLab(); var host = new Host { money = 0 }; var run = sim.S.sequence;
            sim.AutoConfigure(run, host);
            run.shapeRounds = 3; run.valAcc = XgSim.GradeAccuracy("spam", XgSim.TrainedGrade) + .02;
            var a = sim.Assess(XgTrack.Sequence, host);
            Assert.IsNotNull(a);
            Assert.GreaterOrEqual(a.grade, XgSim.TrainedGrade);
            Assert.AreEqual(XgSim.ParamsK(run), sim.TrainedParamsK, 1e-9);
            Assert.IsTrue(a.record, "the record is saved");
            Assert.Greater(host.money, 0, "and it pays");
            Assert.Greater(sim.S.assessments, 0);
        }

        [Test]
        public void TheCountOpensTheTechPageAndSurvivesASaveRoundTrip()
        {
            var sim = new XgSim();
            Assert.IsFalse(sim.FeatureVisible("tree"));
            sim.S.trainedParamsK = 12;
            Assert.IsTrue(sim.FeatureVisible("tree"), "counted parameters open the tech page");
            var copy = new XgSim(UnityEngine.JsonUtility.FromJson<XgState>(UnityEngine.JsonUtility.ToJson(sim.S)));
            Assert.AreEqual(12, copy.TrainedParamsK, 1e-9);
            Assert.IsTrue(copy.FeatureVisible("tree"));
        }

        [Test]
        public void TheBarStepPointsAtTheShorterBarAndIsNullOnceAllSixAreIn()
        {
            var sim = new XgSim { GoldChance = 0 };
            sim.S.treeIntroDelivered = true;
            sim.S.labels.Add(new XgLabelCount { dataset = "spam", count = 5 });
            sim.S.trainedParamsK = 0;
            var step = XgGuide.BarStep(sim, 1000, 100000, out bool paramsBar);
            Assert.IsNotNull(step);
            StringAssert.StartsWith("ability.", step.id);
            Assert.AreEqual(sim.ParamsProgress(sim.NextAbility) <= sim.SamplesProgress(sim.NextAbility), paramsBar);
            sim.S.abilitiesEmerged = new List<int> { 1, 2, 3, 4, 5, 6 };
            Assert.AreEqual(0, sim.NextAbility);
            Assert.IsNull(XgGuide.BarStep(sim, 1000, 100000, out _));
        }
    }
}
