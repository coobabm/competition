using System.Linq;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>
    /// 答卷 (训练页_答卷诊断能力考_设计.md §一): a few questions kept for many rounds, answered right once the run's
    /// mastery passes each question's fixed difficulty, so the player sees ✗ turn into ✓ (and back after a drop).
    /// </summary>
    public sealed class XgSheetTests
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

        /// <summary>A digits lab after one round, so the model is configured and has a ceiling.</summary>
        static XgSim Digits()
        {
            var sim = Lab("mnist", 60000, "mlp", "lenet", "v.w1", "v.w2", "v.d3");
            sim.TrainEpoch(XgTrack.Vision, new Host());
            return sim;
        }

        static void SetMastery(XgSim sim, XgRun run, double mastery)
        {
            var d = XgCatalog.Dataset(run.dataset);
            double chance = 1 - d.chanceError, top = 1 - d.floorError;
            run.valAcc = chance + (top - chance) * mastery;
        }

        [Test]
        public void ANewSheetHasFourDifferentQuestionsWithText()
        {
            var sim = Lab("mnist", 60000, "lenet");
            var sheet = sim.Sheet(sim.S.vision);
            Assert.That(sheet.Count, Is.EqualTo(XgSim.SheetSize));
            foreach (var item in sheet) Assert.That(sim.SheetQuestion(item), Is.Not.Empty);
            Assert.That(sheet.Select(i => sim.SheetQuestion(i)).Distinct().Count(), Is.EqualTo(XgSim.SheetSize));
        }

        [Test]
        public void ANewSheetHoldsOneKnownQuestionAndOneTheCeilingHoldsBack()
        {
            var sim = Digits();
            var run = sim.S.vision;
            sim.Evaluate(run);
            double ceiling = sim.SheetCeiling(run);
            Assume.That(ceiling, Is.LessThan(1 - XgSim.SheetStuckRoom), "the starting model has a ceiling below the desk's best");
            SetMastery(sim, run, ceiling * .5);
            run.sheet.Clear();
            var sheet = sim.Sheet(run);
            Assert.That(sheet.Count(i => i.right), Is.EqualTo(1), "one question it knows already");
            Assert.That(sheet.Count(i => i.stuck), Is.EqualTo(1));
            var stuck = sheet.Single(i => i.stuck);
            Assert.That(stuck.u, Is.GreaterThan(ceiling), "the stuck question sits above what this model can reach");
            foreach (var item in sheet.Where(i => !i.right && !i.stuck))
                Assert.That(item.u, Is.LessThan(ceiling), "the others can be learned by training this model");
        }

        [Test]
        public void AnswersFlipAsMasteryPassesTheirDifficultyAndBackAfterADrop()
        {
            var sim = Digits();
            var run = sim.S.vision;
            SetMastery(sim, run, .3);
            run.sheet.Clear();
            var sheet = sim.Sheet(run);
            var item = sheet.Where(i => !i.right && !i.stuck).OrderBy(i => i.u).First();
            SetMastery(sim, run, item.u + .001);
            sim.SheetAfterRound(run);
            Assert.That(item.right, Is.True);
            Assert.That(item.flip, Is.EqualTo(1));
            sim.SheetAfterRound(run);
            Assert.That(item.flip, Is.EqualTo(0), "a flip is shown once");
            SetMastery(sim, run, item.u - .001);
            sim.SheetAfterRound(run);
            Assert.That(item.right, Is.False);
            Assert.That(item.flip, Is.EqualTo(-1));
        }

        [Test]
        public void TrainingTheSameModelNeverAnswersTheStuckQuestion()
        {
            var sim = Lab("mnist", 60000, "lenet");
            var run = sim.S.vision;
            var host = new Host();
            sim.TrainEpoch(XgTrack.Vision, host);
            run.sheet.Clear();
            var sheet = sim.Sheet(run);
            var stuck = sheet.SingleOrDefault(i => i.stuck);
            Assume.That(stuck, Is.Not.Null);
            for (int i = 0; i < 150; i++) sim.TrainEpoch(XgTrack.Vision, host);
            Assert.That(run.sheet, Does.Contain(stuck), "the sheet waits on the held-back question");
            Assert.That(stuck.right, Is.False);
            Assert.That(sheet.Count(x => !x.stuck && x.right), Is.EqualTo(XgSim.SheetSize - 1), "the rest were learned");
        }

        [Test]
        public void AFinishedSheetIsReplacedOnTheNextRound()
        {
            var sim = Digits();
            var run = sim.S.vision;
            sim.Sheet(run);
            int count = run.sheetCount;
            SetMastery(sim, run, 1);
            sim.SheetAfterRound(run);
            Assert.That(run.sheetDone, Is.True);
            Assert.That(run.sheetCount, Is.EqualTo(count), "the all-right sheet is shown for one round");
            sim.SheetAfterRound(run);
            Assert.That(run.sheetCount, Is.EqualTo(count + 1));
            Assert.That(run.sheetFresh, Is.True);
            Assert.That(run.sheet.All(i => i.flip == 0), "a new sheet does not flip");
        }

        [Test]
        public void SwitchingTheDeskMakesANewSheet()
        {
            var sim = Lab("logic", 2000);
            sim.S.owned.Add("arith");
            sim.S.labels.Add(new XgLabelCount { dataset = "arith", count = 2000 });
            var run = sim.S.sequence;
            sim.Sheet(run);
            Assume.That(sim.SetDataset(XgTrack.Sequence, "arith"), Is.True);
            var sheet = sim.Sheet(run);
            Assert.That(sheet.All(i => i.dataset == "arith"));
            Assert.That(run.sheetFresh, Is.True);
        }

        [Test]
        public void HandLabelledCardsAreAskedFirst()
        {
            var sim = Lab("logic", 2000);
            for (int i = 0; i < 3; i++)
            {
                var card = sim.CreateLabelCard("logic");
                sim.S.sheetHand.Add(card);
            }
            var run = sim.S.sequence;
            run.sheet.Clear();
            var sheet = sim.Sheet(run);
            Assert.That(sheet.Count(i => i.hand), Is.GreaterThanOrEqualTo(2));
            foreach (var item in sheet.Where(i => i.hand))
                Assert.That(sim.S.sheetHand.Any(c => c.seed == item.card.seed && c.question == item.card.question));
        }

        [Test]
        public void TheCardIsTheSameAfterLoading()
        {
            var sim = Lab("translate", 0, "rnn", "seq2seq");
            var run = sim.S.sequence;
            var sheet = sim.Sheet(run);
            var before = sheet.Select(i => sim.SheetQuestion(i)).ToList();
            var fresh = sheet.Select(i => new XgSheetItem { dataset = i.dataset, card = i.card, hand = i.hand, seed = i.seed, level = i.level, today = i.today, u = i.u }).ToList();
            Assert.That(fresh.Select(i => sim.SheetQuestion(i)), Is.EqualTo(before));
        }

        [Test]
        public void ACleanedDirtyCardTurnsRight()
        {
            var sim = Digits();
            var run = sim.S.vision;
            var item = sim.Sheet(run)[0];
            item.dirty = true; item.u = .99;
            Assume.That(sim.BoardFlipRate("mnist"), Is.LessThanOrEqualTo(XgSim.SheetDirtyRate));
            Assert.That(sim.SheetRight(run, item), Is.True, "clean data: the label is fixed and its answer counts");
            Assert.That(item.dirty, Is.False);
        }

        [Test]
        public void TheLongSentenceLoopIsHeldBackOnAFarClue()
        {
            var sim = Lab("longtext", 50000, "rnn", "s.w1", "s.w2", "s.w3", "s.d3", "longtext.pack");
            var run = sim.S.sequence;
            var host = new Host();
            for (int i = 0; i < 80; i++) sim.TrainEpoch(XgTrack.Sequence, host);
            Assume.That(sim.CeilingLimit(run), Is.EqualTo(XgLimit.Structure));
            run.sheet.Clear();
            var stuck = sim.Sheet(run).SingleOrDefault(i => i.stuck);
            Assert.That(stuck, Is.Not.Null);
            Assert.That(sim.SheetKind(stuck), Does.Contain("10"), "the clue is 10+ characters back");
        }

        [Test]
        public void ASheetMadeBeforeTheModelGrewIsPlacedOnTheNewCeiling()
        {
            var sim = Lab("mnist", 60000, "mlp", "lenet", "v.w1", "v.w2", "v.d3");
            var run = sim.S.vision;
            sim.Sheet(run); // made on the starting shape, before any round configured the model
            string shape = run.sheetShape;
            sim.TrainEpoch(XgTrack.Vision, new Host());
            Assume.That(run.arch + "|" + run.depth + "|" + run.width, Is.Not.EqualTo(shape), "the first round grows the model");
            Assert.That(run.sheetCount, Is.EqualTo(1), "re-placed quietly, not a new sheet");
            var stuck = run.sheet.SingleOrDefault(i => i.stuck);
            Assume.That(stuck, Is.Not.Null);
            Assert.That(stuck.u, Is.GreaterThan(sim.SheetCeiling(run)), "held back by this model's ceiling, not the old one");
        }

        [Test]
        public void TheHeldBackQuestionFlipsOnceTheModelGrows()
        {
            var sim = Digits();
            var run = sim.S.vision;
            var host = new Host();
            for (int i = 0; i < 20; i++) sim.TrainEpoch(XgTrack.Vision, host);
            var stuck = run.sheet.SingleOrDefault(x => x.stuck);
            Assume.That(stuck, Is.Not.Null);
            Assume.That(stuck.right, Is.False);
            foreach (var id in new[] { "v.w3", "v.w4", "v.d4", "v.d5" }) sim.S.unlocked.Add(id);
            for (int i = 0; i < 80 && !stuck.right; i++) sim.TrainEpoch(XgTrack.Vision, host);
            Assert.That(stuck.right, Is.True, "a bigger model reaches the question the small one could not");
        }
    }
}
