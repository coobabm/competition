using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>The guide's plateau step: a model that stopped improving is told plainly why, and where to fix it.</summary>
    public sealed class XgChanceHintTests
    {
        sealed class Host : IXgHost
        {
            public double money = 1e6, compute = 4, vram = 1e5;
            public double Compute => compute;
            public double VramMB => vram;
            public double Money => money;
            public bool Spend(double a) { if (money < a) return false; money -= a; return true; }
            public void Earn(double a) { money += a; }
            public void Train(double s) { }
            public string Blocker => null;
        }

        /// <summary>A stage-2 lab on the digit desk with the given samples and extra nodes owned.</summary>
        static XgSim Digits(double labels, params string[] owned)
        {
            var state = new XgState { progressionVersion = XgSim.ProgressionSchema, stage = 2, stageVision = 2, stageSequence = 2 };
            var sim = new XgSim(state) { GoldChance = 0 };
            sim.S.desksOpen.Add("mnist");
            foreach (var id in owned) sim.S.unlocked.Add(id);
            sim.S.labels.Add(new XgLabelCount { dataset = "mnist", count = labels });
            sim.SelectedTrack = XgTrack.Vision;
            sim.SetDataset(XgTrack.Vision, "mnist");
            return sim;
        }

        static void Train(XgSim sim, XgTrack track, int rounds)
        {
            var host = new Host();
            for (int i = 0; i < rounds; i++) sim.TrainEpoch(track, host);
        }

        [Test]
        public void ASmallModelOnAHardDeskIsToldItLacksParameters()
        {
            var sim = Digits(5000);
            Train(sim, XgTrack.Vision, 40);
            var run = sim.S.vision;
            Assert.That(sim.Plateaued(run), Is.True, "a perceptron of a few hundred parameters cannot learn the digits");
            Assert.That(sim.PlateauText(run, out var limit), Does.Contain("参数量不够，再练也涨不动"));
            Assert.That(limit, Is.EqualTo(XgLimit.Params));
            var step = XgChanceHint.Step(sim);
            Assert.That(step, Is.Not.Null);
            Assert.That(step.id, Is.EqualTo("plateau.params"));
            Assert.That(step.tab == "tree" || step.tab == "items", Is.True, "it points at the tech tree or the Items page, never at a knob");
        }

        [Test]
        public void ABigModelShortOfSamplesIsToldToLabel()
        {
            var sim = Digits(20, "mlp", "lenet", "v.w1", "v.w2", "v.w3", "v.d3", "v.d4");
            Train(sim, XgTrack.Vision, 150);
            var run = sim.S.vision;
            Assert.That(sim.Plateaued(run), Is.True, "head " + sim.HeadroomPoints(run));
            Assert.That(sim.CeilingLimit(run), Is.EqualTo(XgLimit.Data));
            Assert.That(sim.PlateauText(run, out _), Does.Contain("样本不够"));
            Assert.That(XgChanceHint.Step(sim).tab, Is.EqualTo("label"));
        }

        [Test]
        public void AModelThatStillImprovesGetsNoHint()
        {
            var sim = Digits(60000, "mlp", "lenet", "v.w1", "v.w2", "v.w3", "v.d3", "v.d4");
            Train(sim, XgTrack.Vision, 2);
            Assert.That(sim.Plateaued(sim.S.vision), Is.False);
            Assert.That(XgChanceHint.Step(sim), Is.Null);
        }

        [Test]
        public void ALoopOnLongTextIsToldItsStructureDoesNotFit()
        {
            var state = new XgState { progressionVersion = XgSim.ProgressionSchema, stage = 3, stageVision = 3, stageSequence = 3 };
            var sim = new XgSim(state) { GoldChance = 0 };
            foreach (var id in new[] { "rnn", "s.w1", "s.w2", "s.w3", "s.d3", "longtext.pack" }) sim.S.unlocked.Add(id);
            sim.S.owned.Add("longtext");
            sim.S.labels.Add(new XgLabelCount { dataset = "longtext", count = 50000 });
            sim.SelectedTrack = XgTrack.Sequence;
            Assert.That(sim.SetDataset(XgTrack.Sequence, "longtext"), Is.True);
            Train(sim, XgTrack.Sequence, 80);
            var run = sim.S.sequence;
            Assert.That(run.arch, Is.EqualTo("rnn"));
            Assert.That(sim.Plateaued(run), Is.True);
            Assert.That(sim.CeilingLimit(run), Is.EqualTo(XgLimit.Structure), "a plain loop forgets long sentences");
            Assert.That(sim.PlateauText(run, out _), Does.Contain("结构不对路"));
        }
    }
}
