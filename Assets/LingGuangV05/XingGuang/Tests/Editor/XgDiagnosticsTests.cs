using System.Collections.Generic;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>The long-sentence sample: real diagnostic groups and real mistakes (trials went with the knobs).</summary>
    public sealed class XgDiagnosticsTests
    {
        sealed class Host : IXgHost
        {
            public double money = 1e6, vram = 1e5;
            public double Money => money; public double Compute => 2; public double VramMB => vram; public string Blocker => null;
            public bool Spend(double a) { if (money < a) return false; money -= a; return true; }
            public void Earn(double a) { money += a; }
            public void Train(double s) { }
        }

        /// <summary>A stage-3 save training a plain loop on long sentences (the bought pack).</summary>
        static XgSim OnLongText(out Host host, int salt = 7)
        {
            var state = new XgState { progressionVersion = XgSim.ProgressionSchema, stage = 3, stageVision = 3, stageSequence = 3, dataSalt = salt };
            var sim = new XgSim(state);
            foreach (var n in XgCatalog.Nodes)
                if (n.stage <= 3 && n.tree != "label" && n.kind != XgNodeKind.Secret && n.kind != XgNodeKind.Label && !sim.Has(n.id))
                    sim.S.unlocked.Add(n.id);
            // A plain loop: without the gated ones it is the best structure the reading line owns.
            sim.S.unlocked.Remove("lstm"); sim.S.unlocked.Remove("gru"); sim.S.unlocked.Remove("mlp");
            sim.S.labels.Add(new XgLabelCount { dataset = "spam", count = 300 });
            if (!sim.S.owned.Contains("longtext")) sim.S.owned.Add("longtext");
            sim = new XgSim(sim.S) { GoldChance = 0 };
            host = new Host();
            var s = XgTrack.Sequence;
            Assert.IsTrue(sim.DatasetAvailable("longtext"));
            sim.SetDataset(s, "longtext");
            for (int i = 0; i < 30; i++) sim.TrainEpoch(s, host, false); // plain rounds: the hand bonus is pacing, not part of this
            Assert.That(sim.S.sequence.arch, Is.EqualTo("rnn"));
            return sim;
        }

        [Test]
        public void DiagnosticGroupsComeFromTheCardsRealDistance()
        {
            var sim = OnLongText(out _);
            var d = sim.Diagnose("longtext", sim.Knobs(sim.S.sequence));
            int total = 0; foreach (var g in d.groups) total += g.total;
            Assert.That(total, Is.EqualTo(sim.TestSet("longtext").Count));
            Assert.That(d.groups.ConvertAll(g => g.id), Is.EqualTo(new[] { "near", "mid", "far" }));
            foreach (var card in sim.TestSet("longtext")) Assert.That(XgSim.GroupOf(card).id, Is.EqualTo(card.distance <= 4 ? "near" : card.distance <= 9 ? "mid" : "far"));
            foreach (var m in d.mistakes)
            {
                Assert.That(m.answer, Is.Not.EqualTo(m.truth), "a mistake is a real wrong answer");
                Assert.That(m.text, Does.Contain("【"), "the sample shows the actual sentence");
                Assert.That(m.group, Is.EqualTo(XgSim.GroupOf(new XgBoardCard { distance = m.distance }).id));
            }
            Assert.That(d.mistakes.Count, Is.InRange(1, XgSim.MistakesShown));
        }

        [Test]
        public void APlainLoopShowsTheDistanceProblem()
        {
            var sim = OnLongText(out _);
            var d = sim.Diagnose("longtext", sim.Knobs(sim.S.sequence));
            Assert.That(d.Group("far").Rate, Is.LessThan(d.Group("near").Rate - .15), "near " + d.Group("near").Rate + " far " + d.Group("far").Rate);
            Assert.That(d.mainError, Does.Contain("远"));
        }
    }
}
