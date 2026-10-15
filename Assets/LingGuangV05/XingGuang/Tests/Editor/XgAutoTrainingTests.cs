using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>
    /// Training without knobs (训练不再需要调参数): the model configures itself, every round moves towards a ceiling set by
    /// parameters and data, and sometimes a round goes down a little — never taking an ability, a grade or P away.
    /// </summary>
    public sealed class XgAutoTrainingTests
    {
        sealed class Host : IXgHost
        {
            public double money = 1e7, compute = 4, vram = 1e5;
            public double Compute => compute;
            public double VramMB => vram;
            public double Money => money;
            public bool Spend(double a) { if (money < a) return false; money -= a; return true; }
            public void Earn(double a) { money += a; }
            public void Train(double s) { }
            public string Blocker => null;
        }

        static XgSim Lab(int stage, double labels, string dataset, params string[] owned)
        {
            var state = new XgState { progressionVersion = XgSim.ProgressionSchema, stage = stage, stageVision = stage, stageSequence = stage, trainingVersion = XgSim.TrainingSchemaAuto };
            state.unlocked.Clear();
            var sim = new XgSim(state) { GoldChance = 0 };
            foreach (var id in owned) sim.S.unlocked.Add(id);
            var d = XgCatalog.Dataset(dataset);
            if (XgCatalog.Desk(dataset) != null && !sim.S.desksOpen.Contains(dataset)) sim.S.desksOpen.Add(dataset);
            if (!d.handLabel || XgCatalog.Desk(dataset) == null) sim.S.owned.Add(dataset);
            sim.S.labels.Add(new XgLabelCount { dataset = dataset, count = labels });
            sim.SelectedTrack = d.track;
            sim.SetDataset(d.track, dataset);
            return sim;
        }

        static readonly string[] VisionScale = { "mlp", "lenet", "alexnet", "v.w1", "v.w2", "v.w3", "v.w4", "v.d3", "v.d4", "v.d5", "v.d6", "v.d8" };

        // ───────────── automatic configuration ─────────────

        [Test]
        public void TheLargestModelTheCapsAllowIsChosenWhenTheCardHoldsIt()
        {
            var sim = Lab(3, 60000, "mnist", VisionScale);
            var host = new Host();
            sim.AutoConfigure(sim.S.vision, host);
            var run = sim.S.vision;
            Assert.That(run.width, Is.EqualTo(sim.WidthCap(XgTrack.Vision)));
            Assert.That(run.depth, Is.EqualTo(Math.Min(sim.DepthCap(XgTrack.Vision), sim.MaxDepth(run))));
        }

        [Test]
        public void ASmallCardGetsTheLargestModelThatFits()
        {
            var sim = Lab(3, 60000, "mnist", VisionScale);
            var host = new Host { vram = 600 };
            sim.AutoConfigure(sim.S.vision, host);
            var run = sim.S.vision;
            double chosen = XgSim.ParamsK(run);
            Assert.That(XgSim.VramNeedMB(run), Is.LessThanOrEqualTo(600));
            // No other size within the caps that fits the card has more parameters.
            int depthCap = Math.Min(sim.DepthCap(XgTrack.Vision), sim.MaxDepth(run));
            for (int d = 1; d <= depthCap; d++)
                for (int w = 0; w <= sim.WidthCap(XgTrack.Vision); w++)
                {
                    var c = new XgRun { track = 0, arch = run.arch, dataset = run.dataset, depth = d, width = w };
                    if (XgSim.VramNeedMB(c) <= 600) Assert.That(XgSim.ParamsK(c), Is.LessThanOrEqualTo(chosen + 1e-9), "d" + d + " w" + w);
                }
            Assert.That(chosen, Is.LessThan(XgSim.ParamsK(new XgRun { arch = run.arch, depth = depthCap, width = sim.WidthCap(XgTrack.Vision) })), "the card, not the caps, is the limit here");
        }

        [Test]
        public void TheBestOwnedArchitectureIsUsedAndEveryOwnedTechniqueIsOn()
        {
            var sim = Lab(3, 60000, "mnist", VisionScale);
            foreach (var id in new[] { "relu", "dropout", "augment", "batchnorm", "gradclip", "warmup", "position" }) sim.S.unlocked.Add(id);
            var host = new Host();
            sim.AutoConfigure(sim.S.vision, host);
            var run = sim.S.vision;
            Assert.That(run.arch, Is.EqualTo("alexnet"), "of perceptron, MLP, LeNet and AlexNet, the one that grades best and is biggest");
            var k = sim.Knobs(run);
            Assert.That(k.activation, Is.EqualTo(XgActivation.Relu));
            Assert.That(k.dropout && k.batchNorm && k.clip && k.warmup, Is.True);
            Assert.That(run.lr, Is.EqualTo(sim.SafeLr(run)), "the rate is the largest that cannot tear");
        }

        [Test]
        public void ANewArchitectureItemIsSwitchedToByItself()
        {
            var sim = Lab(3, 60000, "mnist", "mlp", "lenet", "v.w1", "v.w2");
            var host = new Host();
            sim.Tick(1, host);
            Assert.That(sim.S.vision.arch, Is.EqualTo("lenet"));
            sim.S.unlocked.Add("alexnet");
            sim.Tick(1, host);
            Assert.That(sim.S.vision.arch, Is.EqualTo("alexnet"));
        }

        // ───────────── gains and plateaus ─────────────

        [Test]
        public void EnoughParametersKeepImprovingTowardsTheCeiling()
        {
            var sim = Lab(3, 60000, "mnist", VisionScale);
            var host = new Host();
            var run = sim.S.vision;
            double before = run.valAcc;
            int rises = 0, drops = 0;
            for (int i = 0; i < 80; i++)
            {
                var e = sim.TrainEpoch(XgTrack.Vision, host);
                if (e.dropped) drops++;
                else if (e.gain > 0) rises++;
                Assert.That(run.valAcc, Is.LessThanOrEqualTo(run.ceiling + 1e-9));
            }
            Assert.That(rises, Is.GreaterThan(drops * 2), "most rounds go up");
            Assert.That(run.valAcc, Is.GreaterThan(before + .5));
            Assert.That(sim.HeadroomPoints(run), Is.LessThan(150), "it gets close to what its parameters and data allow");
        }

        [Test]
        public void GainsPlateauWhenParametersAreShortAndResumeWithAWiderModel()
        {
            var sim = Lab(3, 60000, "mnist", "mlp", "lenet");
            var host = new Host();
            var run = sim.S.vision;
            for (int i = 0; i < 80; i++) sim.TrainEpoch(XgTrack.Vision, host);
            Assert.That(sim.Plateaued(run), Is.True);
            Assert.That(sim.CeilingLimit(run), Is.EqualTo(XgLimit.Params));
            Assert.That(sim.PlateauText(run, out _), Does.StartWith("参数量不够，再练也涨不动"));
            double flat = sim.CeilingAcc(run);
            // Wider and deeper: the lab grows into it and the ceiling rises.
            foreach (var id in new[] { "v.w1", "v.w2", "v.w3", "v.d3", "v.d4" }) sim.S.unlocked.Add(id);
            Assert.That(sim.AutoConfigure(run, host), Is.True);
            Assert.That(sim.CeilingAcc(run), Is.GreaterThan(flat + .1));
            for (int i = 0; i < 80; i++) sim.TrainEpoch(XgTrack.Vision, host);
            Assert.That(run.valAcc, Is.GreaterThan(flat + .05));
        }

        [Test]
        public void TheModelIsToldWhenItIsTooSmallForTheNextAbility()
        {
            var sim = Lab(2, 60000, "mnist", "mlp", "lenet");
            var host = new Host();
            sim.TrainEpoch(XgTrack.Vision, host);
            Assert.That(XgSim.ParamsK(sim.S.vision), Is.LessThan(sim.ParamsThreshold(3)));
            Assert.That(sim.ParamsShortForNext(sim.S.vision), Is.True);
        }

        // ───────────── drops ─────────────

        [Test]
        public void DropRiskRisesWithScarceDataAndFallsWithPlentifulData()
        {
            var scarce = Lab(3, 30, "mnist", VisionScale);
            var plenty = Lab(3, 60000, "mnist", VisionScale);
            var host = new Host();
            scarce.AutoConfigure(scarce.S.vision, host); plenty.AutoConfigure(plenty.S.vision, host);
            var a = scarce.DropRisk(scarce.S.vision); var b = plenty.DropRisk(plenty.S.vision);
            Assert.That(a.chance, Is.GreaterThan(b.chance));
            Assert.That(a.overfit, Is.GreaterThan(b.overfit));
            Assert.That(a.reason, Does.Contain("背答案"));
            Assert.That(scarce.DropRiskText(scarce.S.vision), Does.StartWith("下降风险："));
        }

        [Test]
        public void DropRiskRisesWithNoisyDataAndNewMemes()
        {
            var sim = Lab(3, 60000, "mnist", VisionScale);
            var host = new Host();
            sim.AutoConfigure(sim.S.vision, host);
            double clean = sim.DropRisk(sim.S.vision).chance;
            sim.S.noise.Add(new XgLabelCount { dataset = "mnist", count = 15000 });
            var noisy = sim.DropRisk(sim.S.vision);
            Assert.That(noisy.chance, Is.GreaterThan(clean));
            Assert.That(noisy.noise, Is.GreaterThan(0));
        }

        [Test]
        public void DropRiskRisesRightAfterABigJumpAndWarmupSoftensIt()
        {
            // A small model on plenty of data: nothing to memorise, so the jump is what stands out.
            var sim = Lab(3, 60000, "mnist", "mlp", "lenet");
            var host = new Host();
            sim.AutoConfigure(sim.S.vision, host);
            var run = sim.S.vision;
            run.lastGain = 0;
            double calm = sim.DropRisk(run).chance;
            run.lastGain = .15;
            var jumped = sim.DropRisk(run);
            Assert.That(jumped.chance, Is.GreaterThan(calm));
            Assert.That(jumped.reason, Does.Contain("刚涨了一大截"));
            sim.S.unlocked.Add("warmup");
            Assert.That(sim.DropRisk(run).jump, Is.LessThan(jumped.jump));
        }

        [Test]
        public void TechniqueItemsLowerTheDropRisk()
        {
            var sim = Lab(3, 300, "mnist", VisionScale);
            var host = new Host();
            sim.AutoConfigure(sim.S.vision, host);
            var run = sim.S.vision;
            run.lastGain = .1;
            double bare = sim.DropRisk(run).chance;
            foreach (var id in new[] { "dropout", "augment", "batchnorm", "warmup", "gradclip" }) sim.S.unlocked.Add(id);
            Assert.That(sim.DropRisk(run).chance, Is.LessThan(bare * .7));
        }

        [Test]
        public void DropsNeverTakeAnAbilityAGradeOrTrainedParametersAway()
        {
            // A big model on a handful of noisy samples drops often.
            var sim = Lab(3, 40, "mnist", VisionScale);
            sim.S.noise.Add(new XgLabelCount { dataset = "mnist", count = 15 });
            var host = new Host();
            int drops = 0; double best = 0, p = 0; int abilities = sim.AbilitiesCount, grades = 0;
            for (int i = 0; i < 300; i++)
            {
                var e = sim.TrainEpoch(XgTrack.Vision, host);
                if (e.dropped) { drops++; Assert.That(e.gain, Is.LessThan(0)); Assert.That(e.dropReason, Is.Not.Empty); }
                Assert.That(sim.BestAcc("mnist"), Is.GreaterThanOrEqualTo(best), "the best checkpoint never goes down");
                Assert.That(sim.TrainedParamsK, Is.GreaterThanOrEqualTo(p), "P never goes down");
                Assert.That(sim.AbilitiesCount, Is.GreaterThanOrEqualTo(abilities), "a learned ability stays learned");
                Assert.That(sim.S.grades.Count, Is.GreaterThanOrEqualTo(grades));
                best = sim.BestAcc("mnist"); p = sim.TrainedParamsK; abilities = sim.AbilitiesCount; grades = sim.S.grades.Count;
            }
            Assert.That(drops, Is.GreaterThan(10), "drops happened");
            Assert.That(sim.S.drops, Is.EqualTo(drops));
        }

        [Test]
        public void AnOverfitDropSaysItIsOverfitting()
        {
            // A big model on a handful of clean samples: the drops come from 背答案, and each one says so.
            var sim = Lab(3, 40, "mnist", VisionScale);
            var host = new Host();
            int overfits = 0;
            for (int i = 0; i < 300; i++)
            {
                var e = sim.TrainEpoch(XgTrack.Vision, host);
                if (!e.overfit) continue;
                overfits++;
                Assert.IsTrue(e.dropped, "only a real drop is flagged");
                Assert.That(sim.S.log[sim.S.log.Count - 1], Does.Contain("过拟合"));
            }
            Assert.That(overfits, Is.GreaterThan(3), "overfit drops happened and were flagged");
        }

        [Test]
        public void RoundsAreDeterministicThroughTheSavesRng()
        {
            List<double> Play()
            {
                var sim = Lab(3, 200, "mnist", VisionScale);
                sim.S.rng = 12345;
                var host = new Host(); var trace = new List<double>();
                for (int i = 0; i < 60; i++) trace.Add(sim.TrainEpoch(XgTrack.Vision, host).valAcc);
                return trace;
            }
            Assert.That(Play(), Is.EqualTo(Play()));
        }

        [Test]
        public void EachDatasetKeepsItsProgressWhenTheLineSwitches()
        {
            var sim = Lab(3, 60000, "mnist", VisionScale);
            sim.S.labels.Add(new XgLabelCount { dataset = "meme", count = 5000 }); sim.S.desksOpen.Add("meme");
            var host = new Host();
            for (int i = 0; i < 30; i++) sim.TrainEpoch(XgTrack.Vision, host);
            double digits = sim.S.vision.valAcc;
            Assert.That(sim.SetDataset(XgTrack.Vision, "meme"), Is.True);
            Assert.That(sim.S.vision.valAcc, Is.EqualTo(1 - XgCatalog.Dataset("meme").chanceError).Within(1e-9), "a desk it never trained starts at chance");
            sim.TrainEpoch(XgTrack.Vision, host);
            Assert.That(sim.SetDataset(XgTrack.Vision, "mnist"), Is.True);
            Assert.That(sim.S.vision.valAcc, Is.EqualTo(digits).Within(1e-9));
        }

        // ───────────── old saves ─────────────

        [Test]
        public void AKnobEraSaveKeepsItsAccuracyGradesAndParameters()
        {
            var state = new XgState { progressionVersion = XgSim.ProgressionSchema, stage = 3, stageVision = 3, stageSequence = 3, trainingVersion = 0 };
            foreach (var id in VisionScale) state.unlocked.Add(id);
            state.desksOpen.AddRange(new[] { "mnist", "meme" });
            state.labels.Add(new XgLabelCount { dataset = "mnist", count = 60000 });
            state.labels.Add(new XgLabelCount { dataset = "meme", count = 3000 });
            // Tuned by hand: a small LeNet at 97% on digits, a best checkpoint on memes, P and grades.
            state.vision = new XgRun { track = 0, arch = "lenet", dataset = "mnist", depth = 2, width = 1, lr = 1, valAcc = .97, trainAcc = .98, epoch = 120 };
            state.best.Add(new XgBest { dataset = "mnist", arch = "lenet", acc = .97, modelId = 0 });
            state.best.Add(new XgBest { dataset = "meme", arch = "lenet", acc = .9, modelId = 0 });
            state.grades.AddRange(new[] { "mnist#1", "mnist#2", "mnist#3", "meme#1" });
            state.trainedParamsK = 150;
            var sim = new XgSim(state) { GoldChance = 0 };
            var host = new Host();
            sim.Tick(1, host);

            var run = sim.S.vision;
            Assert.That(run.arch, Is.EqualTo("alexnet"), "it now runs the best structure it owns");
            Assert.That(XgSim.ParamsK(run), Is.GreaterThan(XgSim.ParamsK(new XgRun { arch = "lenet", depth = 2, width = 1 })));
            Assert.That(run.valAcc, Is.EqualTo(.97).Within(1e-9), "nothing it learnt goes back");
            Assert.That(sim.BestAcc("mnist"), Is.EqualTo(.97).Within(1e-9));
            Assert.That(sim.TrainedParamsK, Is.EqualTo(150).Within(1e-9));
            Assert.That(sim.S.grades, Is.SupersetOf(new[] { "mnist#1", "mnist#2", "mnist#3", "meme#1" }));
            Assert.That(sim.S.trainingVersion, Is.EqualTo(XgSim.TrainingSchemaAuto));
            Assert.That(sim.SetDataset(XgTrack.Vision, "meme"), Is.True);
            Assert.That(run.valAcc, Is.EqualTo(.9).Within(1e-9), "other desks start from their best checkpoint");

            // Saved and loaded again, it is already migrated and stays put.
            var again = new XgSim(sim.S) { GoldChance = 0 };
            again.Tick(1, host);
            Assert.That(again.S.vision.valAcc, Is.EqualTo(.9).Within(1e-9));
        }
    }
}
