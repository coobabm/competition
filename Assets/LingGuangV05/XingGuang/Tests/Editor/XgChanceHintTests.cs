using System;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>The guide note's step for a model that stays at chance: which knob, and where its control is.</summary>
    public sealed class XgChanceHintTests
    {
        sealed class Host : IXgHost
        {
            public double money = 1e6, compute = 2;
            public double Money => money; public double Compute => compute; public double VramMB => 1e6; public string Blocker => null;
            public bool Spend(double a) { if (money < a) return false; money -= a; return true; }
            public void Earn(double a) { money += a; }
            public void Train(double s) { }
        }

        /// <summary>A stage-two lab owning every node up to stage two, with labels on the four desks.</summary>
        static XgSim StageTwo(int salt = 0)
        {
            var state = new XgState { progressionVersion = XgSim.ProgressionSchemaWalls, stage = 2, stageVision = 2, stageSequence = 2, dataSalt = salt };
            var sim = new XgSim(state) { GoldChance = 0 };
            foreach (var n in XgCatalog.Nodes)
                if (n.stage <= 2 && n.tree != "label" && n.kind != XgNodeKind.Secret && n.kind != XgNodeKind.Project && n.kind != XgNodeKind.Label && n.id != "transformer" && !sim.Has(n.id))
                    sim.S.unlocked.Add(n.id);
            foreach (var d in new[] { "logic", "spam", "mnist", "danmu" })
            {
                if (!sim.S.desksOpen.Contains(d)) sim.S.desksOpen.Add(d);
                sim.S.labels.Add(new XgLabelCount { dataset = d, count = 300 });
            }
            return new XgSim(sim.S) { GoldChance = 0, PracticeStrict = false };
        }

        static XgRun Setup(XgSim sim, Host host, XgTrack t, string data, string arch, int act, int depth, int width, int lr)
        {
            sim.SelectedTrack = t;
            sim.SetDataset(t, data); sim.SetArch(t, arch); sim.SetDepth(t, depth, host); sim.SetWidth(t, width, host); sim.SetLr(t, lr); sim.SetActivation(t, act);
            return sim.Run(t);
        }

        static void Train(XgSim sim, Host host, XgTrack t, int epochs) { for (int i = 0; i < epochs; i++) sim.TrainEpoch(t, host); }

        [Test]
        public void ThePlaytestLeNetThatStaysAtChanceIsToldToWiden()
        {
            // LeNet, S-curve, 2 layers, width 32, rate 0.1: stayed at chance in a playtest; width 64 reached 67–83%.
            var sim = StageTwo(); var host = new Host();
            var run = Setup(sim, host, XgTrack.Vision, "mnist", "lenet", 1, 2, 1, 2);
            Train(sim, host, XgTrack.Vision, XgChanceHint.ChanceEpochs - 1);
            Assert.IsNull(XgChanceHint.Step(sim), "not before several epochs");
            Train(sim, host, XgTrack.Vision, 5);
            var step = XgChanceHint.Step(sim);
            Assert.IsNotNull(step);
            Assert.AreEqual("chance.toonarrow", step.id);
            Assert.AreEqual(XgGuide.Lab, step.app); Assert.AreEqual("train", step.tab); Assert.AreEqual("name:Width", step.target); Assert.AreEqual("vision", step.arg);
            StringAssert.Contains("宽度 32", step.zh); StringAssert.Contains("64", step.zh);
            StringAssert.Contains("width 32", step.en); StringAssert.Contains("瞎猜也有 10%", step.whyZh); Assert.IsNotEmpty(step.whyEn);
            Assert.IsTrue(XgGuide.Current(sim, host.money).Exists(s => s.id == "chance.toonarrow"), "the sticky note shows it");

            // Widening silences it at once (new settings start a new count), and the wider net learns.
            sim.SetWidth(XgTrack.Vision, 2, host);
            Assert.IsNull(XgChanceHint.Step(sim));
            Train(sim, host, XgTrack.Vision, 12);
            Assert.IsNull(XgChanceHint.Step(sim));
            Assert.That(XgChanceHint.Progress("mnist", run.valAcc), Is.GreaterThan(XgChanceHint.ChanceSpike));
        }

        [Test]
        public void StepActivationOnTwoLayersIsNamed()
        {
            var sim = StageTwo(); var host = new Host();
            Setup(sim, host, XgTrack.Sequence, "spam", "mlp", 0, 2, 1, 2);
            Train(sim, host, XgTrack.Sequence, 8);
            var step = XgChanceHint.Step(sim);
            Assert.AreEqual("chance.stepactivation", step.id);
            Assert.AreEqual("label:ReLU|ReLU", step.target, "the best activation owned");
            StringAssert.Contains("阶跃", step.zh); StringAssert.Contains("merge concepts", step.whyEn);
        }

        [Test]
        public void RatesThatAreTooSmallOrTooBigAreNamed()
        {
            var sim = StageTwo(); var host = new Host();
            Setup(sim, host, XgTrack.Vision, "mnist", "lenet", 1, 3, 2, 4);
            Train(sim, host, XgTrack.Vision, 8);
            var low = XgChanceHint.Step(sim);
            Assert.AreEqual("chance.ratetoolow", low.id);
            Assert.AreEqual("label:0.01", low.target);

            Assert.AreEqual(XgChanceCause.RateTooHigh, XgChanceHint.Cause(sim, new XgRun { track = 0, dataset = "mnist", arch = "lenet", depth = 3, width = 2, lr = 0 }));
        }

        [Test]
        public void AHealthyRunGetsNoHint()
        {
            var sim = StageTwo(); var host = new Host();
            Setup(sim, host, XgTrack.Sequence, "spam", "mlp", 1, 2, 1, 2);
            Train(sim, host, XgTrack.Sequence, 12);
            Assert.IsNull(XgChanceHint.Step(sim));
            Assert.IsFalse(XgGuide.Current(sim, host.money).Exists(s => s.id.StartsWith("chance.", StringComparison.Ordinal)));
        }

        [Test]
        public void ItNeverGivesAWallsGoldenSettingAway()
        {
            // Fully connected on digits at stage two: the fix is the stage-two wall's golden setting (local sharing).
            var sim = StageTwo(); var host = new Host();
            Setup(sim, host, XgTrack.Vision, "mnist", "mlp", 2, 2, 2, 2);
            Train(sim, host, XgTrack.Vision, 10);
            Assert.That(XgChanceHint.EpochsAtChance(sim, sim.S.vision), Is.GreaterThanOrEqualTo(XgChanceHint.ChanceEpochs));
            Assert.AreEqual(XgChanceCause.Architecture, XgChanceHint.Cause(sim, sim.S.vision));
            Assert.IsNull(XgChanceHint.Step(sim), "the wall's own hints handle it");

            // On a desk no wall checks, the same advice is fine (a stage-three lab past the structure wall).
            Assert.IsFalse(XgChanceHint.WallKeepsQuiet(sim, new XgRun { track = 1, dataset = "spam", arch = "mlp", depth = 2, width = 1, lr = 2 }, XgChanceCause.TooNarrow));
        }

        [Test]
        public void XorOnAPerceptronStaysWithTheWallHints()
        {
            var sim = new XgSim { GoldChance = 0, PracticeStrict = false }; var host = new Host();
            sim.S.labels.Add(new XgLabelCount { dataset = "spam", count = 300 });
            sim.ObserveWall("combo");
            sim.SelectedTrack = XgTrack.Sequence;
            Assert.IsTrue(sim.SetDataset(XgTrack.Sequence, "xor"));
            Train(sim, host, XgTrack.Sequence, 10);
            var run = sim.S.sequence;
            Assert.IsTrue(XgChanceHint.WallKeepsQuiet(sim, run, XgChanceHint.Cause(sim, run)), "one layer on XOR is the wall itself");
            Assert.IsNull(XgChanceHint.Step(sim));
            // A sure cause whose fix is part of the golden setting stays quiet too (two layers, step activation).
            var twoLayers = new XgRun { track = 1, dataset = "xor", arch = "mlp", depth = 2, width = 0, lr = 2, act = 0 };
            Assert.IsTrue(XgChanceHint.WallKeepsQuiet(sim, twoLayers, XgChanceCause.StepActivation));
        }
    }
}
