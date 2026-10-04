using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>The concept board's phenomena must come out of the six rules on their own (design v1.1 §4.5).</summary>
    public sealed class XgBoardTests
    {
        static double Train(XgKnobs k, System.Func<int, XgBoardCard> make, int pool, int cards, out XgBoard brain, out double train, out int diverged)
        {
            brain = new XgBoard();
            var trainSet = XgBoardTasks.Set(make, 1, pool);
            var test = XgBoardTasks.Set(make, 99, 200);
            double acc = XgBoardTasks.Run(brain, k, trainSet, cards, test, out diverged);
            train = brain.Accuracy(trainSet, k);
            return acc;
        }

        static XgKnobs Knobs(int depth, XgActivation activation, double lr, XgWiring wiring = XgWiring.Full, int width = 8)
        { return new XgKnobs { depth = depth, width = width, activation = activation, lr = lr, wiring = wiring }; }

        [Test]
        public void PerceptronLearnsALinearRule()
        {
            Assert.That(Train(Knobs(1, XgActivation.Step, .1), XgBoardTasks.Linear, 200, 800, out _, out _, out _), Is.GreaterThan(.95));
        }

        [Test]
        public void PerceptronIsStuckAtHalfOnXor()
        {
            double acc = Train(Knobs(1, XgActivation.Step, .3), XgBoardTasks.Xor, 200, 800, out var brain, out _, out _);
            Assert.That(acc, Is.InRange(.35, .65));
            Assert.That(brain.MaxLayer("logic"), Is.EqualTo(1), "one layer cannot merge concepts");
        }

        [Test]
        public void HiddenLayerNeedsASlopedActivation()
        {
            Assert.That(Train(Knobs(2, XgActivation.Step, .3), XgBoardTasks.Xor, 200, 800, out _, out _, out _), Is.InRange(.35, .65), "step passes no error down");
            Assert.That(Train(Knobs(2, XgActivation.Sigmoid, .3), XgBoardTasks.Xor, 200, 800, out var brain, out _, out _), Is.GreaterThan(.95), "golden parameters of stage 1");
            Assert.That(brain.MaxLayer("logic"), Is.EqualTo(2));
        }

        [Test]
        public void TinyRateIsSlowAndHugeRateTears()
        {
            Assert.That(Train(Knobs(2, XgActivation.Sigmoid, .01), XgBoardTasks.Xor, 200, 200, out _, out _, out _), Is.LessThan(.7), "lr .01 has not formed the combination yet");
            Train(Knobs(2, XgActivation.Sigmoid, 1), XgBoardTasks.Xor, 200, 200, out _, out _, out int nan);
            Assert.That(nan, Is.GreaterThan(0), "lr 1 diverges");
            var clipped = Knobs(2, XgActivation.Sigmoid, 1); clipped.clip = true;
            Train(clipped, XgBoardTasks.Xor, 200, 200, out _, out _, out nan);
            Assert.That(nan, Is.Zero, "gradient clipping prevents NaN");
        }

        [Test]
        public void DeepSigmoidNetworksVanish()
        {
            Assert.That(Train(Knobs(6, XgActivation.Sigmoid, .3), XgBoardTasks.Xor, 200, 800, out _, out _, out _), Is.LessThan(.7));
        }

        [Test]
        public void PositionBoundWiringMemorisesAndLocalSharingGeneralises()
        {
            double full = Train(Knobs(3, XgActivation.Sigmoid, .3, XgWiring.Full, 32), s => XgBoardTasks.Digit(s), 60, 3000, out _, out double fullTrain, out _);
            Assert.That(fullTrain - full, Is.GreaterThan(.2), "死记硬背: training cards right, test cards wrong");
            double local = Train(Knobs(3, XgActivation.Sigmoid, .3, XgWiring.LocalShared, 32), s => XgBoardTasks.Digit(s), 60, 3000, out _, out _, out _);
            Assert.That(local, Is.GreaterThan(.9));
        }

        [Test]
        public void PlainLoopsForgetLongSentencesAndGatesKeepThem()
        {
            Assert.That(Train(Knobs(3, XgActivation.Relu, .1, XgWiring.Recurrent, 32), s => XgBoardTasks.FirstChar(s, 4), 120, 3000, out _, out _, out _), Is.GreaterThan(.8), "short sentences are fine");
            double plain = Train(Knobs(3, XgActivation.Relu, .1, XgWiring.Recurrent, 32), s => XgBoardTasks.FirstChar(s, 12), 120, 3000, out _, out _, out _);
            Assert.That(plain, Is.LessThan(.6), "长句失忆");
            double gated = Train(Knobs(3, XgActivation.Relu, .03, XgWiring.GatedRecurrent, 32), s => XgBoardTasks.FirstChar(s, 12), 120, 3000, out _, out _, out _);
            Assert.That(gated, Is.GreaterThan(plain + .15));
        }

        [Test]
        public void FullBoardsSuperposeSimilarConcepts()
        {
            Train(Knobs(3, XgActivation.Relu, .1, XgWiring.GatedRecurrent, 128), s => XgBoardTasks.FirstChar(s, 12), 120, 3000, out var brain, out _, out _);
            Assert.That(brain.S.superposed, Is.GreaterThan(0), "叠格 is the source of 幻觉");
        }

        [Test]
        public void SeedSurvivesReinitialisationAndCanBePulled()
        {
            var brain = new XgBoard(); var k = Knobs(1, XgActivation.Step, .3);
            var seed = brain.Plant("logic", "关机", -3);
            for (int i = 0; i < 50; i++) brain.Train(XgBoardTasks.Linear(i), k);
            brain.Reinitialise();
            Assert.That(brain.Find("logic", "关机"), Is.SameAs(seed));
            Assert.That(brain.Find("logic", "中奖"), Is.Null);
            var card = new XgBoardCard { region = "logic", truth = true }.Add("关机");
            for (int i = 0; i < 30; i++) brain.Train(card, k);
            Assert.That(seed.w, Is.GreaterThan(-3), "R1 still pulls the seed toward what the player taught");
        }

        [Test]
        public void IdleBoardsFadeUntilTheSeedIsStrongest()
        {
            var brain = new XgBoard(); var k = Knobs(1, XgActivation.Step, .3);
            brain.Plant("logic", "关机", -3);
            for (int i = 0; i < 200; i++) brain.Train(XgBoardTasks.Linear(i), k);
            brain.Idle(3000);
            Assert.That(brain.Strongest("logic").key, Is.EqualTo("关机"), "the dark cell lights up when nothing else is left");
        }

        [Test]
        public void StateRoundTripsThroughItsPlainLists()
        {
            var brain = new XgBoard(); var k = Knobs(2, XgActivation.Sigmoid, .3);
            var pool = XgBoardTasks.Set(XgBoardTasks.Xor, 1, 100); var test = XgBoardTasks.Set(XgBoardTasks.Xor, 99, 100);
            XgBoardTasks.Run(brain, k, pool, 600, test, out _);
            var copy = new XgBoard(brain.S);
            Assert.That(copy.Accuracy(test, k), Is.EqualTo(brain.Accuracy(test, k)));
        }
    }
}

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgPhenomenaTests
    {
        static System.Collections.Generic.List<string> Observe(XgKnobs k, string dataset, System.Func<int, XgBoardCard> make, int pool, int cards, int length = 0)
        {
            var brain = new XgBoard(); var memory = new XgPhenomenaMemory();
            var train = XgBoardTasks.Set(make, 1, pool); var test = XgBoardTasks.Set(make, 99, 200);
            for (int done = 0; done < cards; done += 100)
            {
                XgBoardTasks.Run(brain, k, train, 100, test, out _);
                XgPhenomena.Observe(new XgObservation { dataset = dataset, region = train[0].region, knobs = k, train = brain.Accuracy(train, k), test = brain.Accuracy(test, k), cards = done + 100, maxLength = length }, memory, brain);
            }
            return memory.seen;
        }

        [Test]
        public void XorWallIsDetectedOnAPerceptron()
        {
            Assert.That(Observe(new XgKnobs { depth = 1, lr = .3 }, "xor", XgBoardTasks.Xor, 200, 600), Does.Contain("xor"));
            Assert.That(Observe(new XgKnobs { depth = 2, activation = XgActivation.Sigmoid, lr = .3 }, "xor", XgBoardTasks.Xor, 200, 600), Does.Not.Contain("xor"));
        }

        [Test]
        public void VanishingMemorisationAndAmnesiaAreDetected()
        {
            Assert.That(Observe(new XgKnobs { depth = 6, activation = XgActivation.Sigmoid, lr = .3 }, "xor", XgBoardTasks.Xor, 200, 600), Does.Contain("vanish"));
            Assert.That(Observe(new XgKnobs { depth = 3, width = 32, activation = XgActivation.Sigmoid, lr = .3 }, "mnist", s => XgBoardTasks.Digit(s), 60, 2000), Does.Contain("memorize"));
            var local = Observe(new XgKnobs { depth = 3, width = 32, activation = XgActivation.Sigmoid, wiring = XgWiring.LocalShared, lr = .3 }, "mnist", s => XgBoardTasks.Digit(s), 60, 2000);
            Assert.That(local, Does.Contain("generalize")); Assert.That(local, Does.Not.Contain("memorize"));
            Assert.That(Observe(new XgKnobs { depth = 3, width = 32, activation = XgActivation.Relu, wiring = XgWiring.Recurrent, lr = .1 }, "poems", s => XgBoardTasks.FirstChar(s, 12), 120, 1000, 12), Does.Contain("amnesia"));
        }

        [Test]
        public void EveryPhenomenonHasBilingualText()
        {
            foreach (var p in XgPhenomena.All)
            {
                Assert.That(p.name, Is.Not.Empty, p.id); Assert.That(p.nameEn, Is.Not.Empty, p.id);
                Assert.That(p.why, Is.Not.Empty, p.id); Assert.That(p.whyEn, Is.Not.Empty, p.id);
            }
        }
    }
}

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>The lab trains on the concept board: knobs, epochs, hand labels, NaN and depth resets.</summary>
    public sealed class XgSimBoardTests
    {
        sealed class Host : IXgHost
        {
            public double money = 1e6, compute = 2;
            public double Money => money; public double Compute => compute; public double VramMB => 1e6; public string Blocker => null;
            public bool Spend(double amount) { if (money < amount) return false; money -= amount; return true; }
            public void Earn(double amount) { money += amount; }
            public void Train(double seconds) { }
        }

        static XgSim Spam(int labels = 200)
        {
            var sim = new XgSim { GoldChance = 0 };
            sim.S.labels.Add(new XgLabelCount { dataset = "spam", count = labels });
            sim.S.sequence.dataset = "spam";
            return sim;
        }

        [Test]
        public void EpochsFeedCardsAndTheTestSetDecidesAccuracy()
        {
            var sim = Spam(); var host = new Host();
            sim.S.unlocked.Add("bias");
            double before = sim.S.sequence.valAcc;
            for (int i = 0; i < 30; i++) sim.TrainEpoch(XgTrack.Sequence, host);
            Assert.That(sim.Board.Count("sequence"), Is.GreaterThan(0));
            Assert.That(sim.S.sequence.valAcc, Is.GreaterThan(before + .1));
            Assert.That(sim.S.sequence.valAcc, Is.EqualTo(XgSim.Scale("spam", sim.Board.Accuracy(sim.TestSet("spam"), sim.Knobs(sim.S.sequence)))).Within(1e-9));
        }

        [Test]
        public void KnobsFollowPurchasesAndOwnership()
        {
            var sim = Spam(); var host = new Host();
            Assert.That(sim.Knobs(sim.S.sequence).activation, Is.EqualTo(XgActivation.Step));
            Assert.IsFalse(sim.SetActivation(XgTrack.Sequence, 1), "the S-curve is a skill-tree node");
            sim.S.unlocked.Add("perceptron"); sim.S.unlocked.Add("bt.hidden"); sim.S.unlocked.Add("mlp");
            sim = new XgSim(sim.S) { GoldChance = 0 };
            Assert.IsTrue(sim.BuyNode("sigmoid", host), sim.Why(XgCatalog.Node("sigmoid"), host));
            Assert.That(sim.Knobs(sim.S.sequence).activation, Is.EqualTo(XgActivation.Sigmoid), "buying switches it on");
            Assert.That(sim.Knobs(sim.S.vision).activation, Is.EqualTo(XgActivation.Sigmoid));
            Assert.IsTrue(sim.SetActivation(XgTrack.Sequence, 0));
            Assert.That(sim.Knobs(sim.S.sequence).wiring, Is.EqualTo(XgWiring.Full));
            sim.S.sequence.arch = "lstm"; Assert.That(sim.Knobs(sim.S.sequence).wiring, Is.EqualTo(XgWiring.GatedRecurrent));
        }

        [Test]
        public void HandLabelsGoStraightOntoTheBoard()
        {
            var sim = Spam(0);
            for (int i = 0; i < 10; i++) { var card = sim.Card("spam"); sim.Answer("spam", card.truth, new Host()); }
            Assert.That(sim.Board.Count("sequence"), Is.GreaterThan(0));
        }

        [Test]
        public void HugeRateTearsTheBoardAndClippingHolds()
        {
            var sim = Spam(); var host = new Host();
            sim.S.unlocked.Add("shared.lr"); Assert.IsTrue(sim.SetLr(XgTrack.Sequence, 0));
            for (int i = 0; i < 40 && sim.S.nanEvents == 0; i++) sim.TrainEpoch(XgTrack.Sequence, host);
            Assert.That(sim.S.nanEvents, Is.GreaterThan(0), "lr 1 tears the weights");
            sim.S.unlocked.Add("gradclip"); Assert.IsTrue(sim.SetClip(XgTrack.Sequence, true));
            int nan = sim.S.nanEvents;
            for (int i = 0; i < 40; i++) sim.TrainEpoch(XgTrack.Sequence, host);
            Assert.That(sim.S.nanEvents, Is.EqualTo(nan));
        }

        [Test]
        public void NewDepthReinitialisesItsRegionButNotTheSeed()
        {
            var sim = Spam(); var host = new Host();
            for (int i = 0; i < 10; i++) sim.TrainEpoch(XgTrack.Sequence, host);
            var seed = sim.Board.Plant("logic", "关机", -3);
            Assert.That(sim.Board.Count("sequence"), Is.GreaterThan(0));
            sim.S.unlocked.Add("perceptron"); sim.S.unlocked.Add("bt.hidden"); sim.S.unlocked.Add("mlp"); sim.S.unlocked.Add("s.d2"); sim.S.sequence.arch = "mlp";
            var before = sim.S.board.concepts.FindAll(c => c.region == "sequence").ConvertAll(c => c.id);
            Assert.IsTrue(sim.SetDepth(XgTrack.Sequence, 2, host));
            Assert.That(sim.S.board.concepts.FindAll(c => c.region == "sequence").ConvertAll(c => c.id), Is.EquivalentTo(before), "the real network waits for the next real epoch");
            sim.TrainEpoch(XgTrack.Sequence, host);
            Assert.That(sim.S.board.concepts.Exists(c => c.region == "sequence" && before.Contains(c.id)), Is.False, "that epoch starts from a fresh network");
            Assert.That(sim.Board.Find("logic", "关机"), Is.SameAs(seed));
        }

        [Test]
        public void OtherRegionsForgetSlowly()
        {
            var brain = new XgBoard(); var k = new XgKnobs { depth = 1, width = 32, lr = .3 };
            for (int i = 0; i < 400; i++) brain.Train(XgBoardTasks.Linear(i), k);
            int logic = brain.Count("logic");
            var vision = new XgKnobs { depth = 3, width = 32, activation = XgActivation.Relu, wiring = XgWiring.LocalShared, lr = .1 };
            for (int i = 0; i < 3000; i++) brain.Train(XgBoardTasks.Digit(i), vision);
            Assert.That(brain.Count("logic"), Is.EqualTo(logic), "3000 cards elsewhere fade it, they do not wipe it");
            Assert.That(brain.Accuracy(XgBoardTasks.Set(XgBoardTasks.Linear, 99, 100), k), Is.GreaterThan(.9));
        }

        [Test]
        public void TheBoardSurvivesTheSaveFormat()
        {
            var sim = Spam(); var host = new Host();
            for (int i = 0; i < 10; i++) sim.TrainEpoch(XgTrack.Sequence, host);
            var copy = new XgSim(sim.S) { GoldChance = 0 };
            Assert.That(copy.Board.Count("sequence"), Is.EqualTo(sim.Board.Count("sequence")));
            Assert.That(copy.Board.Accuracy(copy.TestSet("spam"), copy.Knobs(copy.S.sequence)), Is.EqualTo(sim.Board.Accuracy(sim.TestSet("spam"), sim.Knobs(sim.S.sequence))));
        }
    }
}
