using System.Linq;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>
    /// 卡住时让玩家诊断 (训练页_答卷诊断能力考_设计.md §二): the plateau's cause is not told; the evidence tells the four
    /// causes apart, a wrong pick costs nothing, and only the first pick, if right, earns help.
    /// </summary>
    public sealed class XgDiagnosisTests
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

        static XgSim Lab(string dataset, double labels, params string[] owned)
        {
            var state = new XgState { progressionVersion = XgSim.ProgressionSchema, stage = 3, stageVision = 3, stageSequence = 3, trainingVersion = XgSim.TrainingSchemaAuto };
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

        static void Train(XgSim sim, XgTrack track, int rounds, Host host)
        {
            for (int i = 0; i < rounds; i++) sim.TrainEpoch(track, host);
        }

        /// <summary>A small LeNet on digits: it plateaus on its size.</summary>
        static XgSim SmallDigits(Host host)
        {
            var sim = Lab("mnist", 60000, "lenet");
            Train(sim, XgTrack.Vision, 150, host);
            return sim;
        }

        /// <summary>A plain loop on long sentences: it plateaus on its structure.</summary>
        static XgSim LoopOnLongText(Host host)
        {
            var sim = Lab("longtext", 50000, "rnn", "s.w1", "s.w2", "s.w3", "s.d3", "longtext.pack");
            Train(sim, XgTrack.Sequence, 80, host);
            return sim;
        }

        [Test]
        public void NothingIsDueWhileItStillImproves()
        {
            var host = new Host();
            var sim = Lab("mnist", 60000, "lenet");
            Assert.That(sim.DiagnosisDue(sim.S.vision), Is.EqualTo(XgLimit.None), "not trained yet");
            Train(sim, XgTrack.Vision, 1, host);
            Assume.That(sim.Plateaued(sim.S.vision), Is.False);
            Assert.That(sim.DiagnosisDue(sim.S.vision), Is.EqualTo(XgLimit.None));
        }

        [Test]
        public void APlateauAsksForTheCauseItHasNotBeenTold()
        {
            var host = new Host();
            var sim = SmallDigits(host);
            var run = sim.S.vision;
            Assume.That(sim.CeilingLimit(run), Is.EqualTo(XgLimit.Params));
            Assert.That(sim.DiagnosisDue(run), Is.EqualTo(XgLimit.Params));
        }

        [Test]
        public void TheOpeningNeverAsks()
        {
            var host = new Host();
            var sim = SmallDigits(host);
            sim.S.openingFocus = true;
            Assert.That(sim.DiagnosisDue(sim.S.vision), Is.EqualTo(XgLimit.None));
        }

        [Test]
        public void AWrongPickCostsNothingAndSaysWhyNot()
        {
            var host = new Host();
            var sim = SmallDigits(host);
            var run = sim.S.vision;
            Assume.That(sim.DiagnosisDue(run), Is.EqualTo(XgLimit.Params));
            double money = host.money;
            var r = sim.Diagnose(run, XgLimit.Data, host);
            Assert.That(r.correct, Is.False);
            Assert.That(r.line, Does.StartWith("不是「没见过」"));
            Assert.That(host.money, Is.EqualTo(money));
            Assert.That(sim.DiagnosisDue(run), Is.EqualTo(XgLimit.Params), "still open");
            Assert.That(sim.DiagnosisWrongPicks(run), Is.EqualTo(new[] { XgLimit.Data }));

            var right = sim.Diagnose(run, XgLimit.Params, host);
            Assert.That(right.correct, Is.True);
            Assert.That(right.firstTry, Is.False);
            Assert.That(right.reward, Is.Empty, "only the first pick earns help");
            Assert.That(host.money, Is.EqualTo(money));
            Assert.That(sim.S.diagnosisCoupons, Is.Empty);
            Assert.That(sim.DiagnosisDue(run), Is.EqualTo(XgLimit.None), "worked out");
        }

        [Test]
        public void TheRightFirstPickEarnsMoneyTheCardAndADiscountOnTheFix()
        {
            var host = new Host();
            var sim = SmallDigits(host);
            var run = sim.S.vision;
            Assume.That(sim.DiagnosisDue(run), Is.EqualTo(XgLimit.Params));
            var width = XgCatalog.Node("v.w1");
            double before = sim.NodeCost(width), money = host.money;
            var r = sim.Diagnose(run, XgLimit.Params, host);
            Assert.That(r.correct && r.firstTry, Is.True);
            Assert.That(host.money, Is.GreaterThan(money));
            Assert.That(sim.S.achievements, Does.Contain("cure.sight"));
            Assert.That(sim.NodeCost(width), Is.LessThan(before));
            Assert.That(sim.BuyNode("v.w1", host), Is.True);
            Assert.That(sim.S.diagnosisCoupons, Is.Empty, "the discount is used once");
        }

        [Test]
        public void ALoopOnLongSentencesShowsItsFlawAndLightsTheAmnesia()
        {
            var host = new Host();
            var sim = LoopOnLongText(host);
            var run = sim.S.sequence;
            Assume.That(sim.DiagnosisDue(run), Is.EqualTo(XgLimit.Structure));
            var ev = sim.DiagnosisEvidence(run);
            Assert.That(ev.kinds[0].text, Is.EqualTo("线索离问题 10 字以上"));
            Assert.That(ev.kinds[0].count, Is.GreaterThanOrEqualTo(ev.wrongTotal * .6));
            Assert.That(ev.trainedRight - ev.newRight, Is.LessThanOrEqualTo(1), "practice cards fail the same way");
            var r = sim.Diagnose(run, XgLimit.Structure, host);
            Assert.That(r.correct, Is.True);
            Assert.That(r.line, Does.Contain("LSTM"));
            Assert.That(sim.S.phenomena.seen, Does.Contain("amnesia"));
            Assert.That(sim.S.diagnosisCoupons, Does.Contain("structure"));
        }

        [Test]
        public void TooLittleDataShowsPracticeCardsRightAndNewOnesWrong()
        {
            var host = new Host();
            var sim = Lab("mnist", 150, "lenet", "alexnet", "v.w1", "v.w2", "v.w3", "v.d3", "v.d4");
            Train(sim, XgTrack.Vision, 150, host);
            var run = sim.S.vision;
            Assume.That(sim.DiagnosisDue(run), Is.EqualTo(XgLimit.Data));
            var ev = sim.DiagnosisEvidence(run);
            Assert.That(ev.trainedRight - ev.newRight, Is.GreaterThanOrEqualTo(4));
            double samples = sim.Samples("mnist");
            var r = sim.Diagnose(run, XgLimit.Data, host);
            Assert.That(r.firstTry, Is.True);
            Assert.That(sim.Samples("mnist"), Is.GreaterThan(samples), "a few samples for the first pick");
            Assert.That(sim.S.phenomena.seen, Does.Contain("memorize"));
        }

        [Test]
        public void ASizePlateauSpreadsItsMistakesAndHasCleanLabels()
        {
            var host = new Host();
            var sim = SmallDigits(host);
            var run = sim.S.vision;
            Assume.That(sim.DiagnosisDue(run), Is.EqualTo(XgLimit.Params));
            var ev = sim.DiagnosisEvidence(run);
            Assert.That(ev.wrongTotal, Is.EqualTo(XgSim.DiagnosisWrongCards));
            Assert.That(ev.kinds[0].count, Is.LessThan(ev.wrongTotal * .6), "no one kind dominates");
            Assert.That(ev.trainedRight - ev.newRight, Is.LessThanOrEqualTo(1));
            Assert.That(ev.labelWrong, Is.EqualTo(0));
            Assert.That(ev.examples, Is.Not.Empty);
        }

        [Test]
        public void TheEvidenceIsTheSameEachTimeItIsOpened()
        {
            var host = new Host();
            var sim = SmallDigits(host);
            var run = sim.S.vision;
            var a = sim.DiagnosisEvidence(run);
            var b = sim.DiagnosisEvidence(run);
            Assert.That(b.kinds.Select(k => k.text + k.count), Is.EqualTo(a.kinds.Select(k => k.text + k.count)));
            Assert.That(b.examples, Is.EqualTo(a.examples));
            Assert.That(b.newRight, Is.EqualTo(a.newRight));
        }

        [Test]
        public void EveryCauseHasAFixLine()
        {
            var sim = LoopOnLongText(new Host());
            foreach (var c in XgSim.DiagnosisCauses)
            {
                Assert.That(sim.DiagnosisFix(sim.S.sequence, c, false), Is.Not.Empty, c.ToString());
                Assert.That(sim.DiagnosisFix(sim.S.sequence, c, true), Is.Not.Empty, c.ToString());
                Assert.That(XgSim.CauseName(c, false), Is.Not.Empty);
            }
        }
    }
}
