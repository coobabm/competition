using System.Collections.Generic;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>接线: the default layout changes nothing; VRAM, the breaker, 阿杰's café cards.</summary>
    public sealed class XgWiringTests
    {
        /// <summary>A household with real cards, a 3500 W PSU and a breaker.</summary>
        sealed class Rig : IXgHost, IXgRig, IXgHousePower
        {
            public double money = 100000, extraWatts = 45;
            public bool tripped, unpaid;
            public string thermalBlocker;
            public readonly List<XgCardSpec> cards = new List<XgCardSpec>();
            public Rig(params XgCardSpec[] owned) { cards.AddRange(owned); }
            public double Compute { get { if (tripped || unpaid) return 0; double c = 0; foreach (var g in cards) c += g.compute; return c; } }
            public double VramMB { get { double m = 0; foreach (var g in cards) m = System.Math.Max(m, g.vramMB); return m; } }
            public double Money => money;
            public bool Spend(double amount) { if (amount < 0 || money + 1e-9 < amount) return false; money -= amount; return true; }
            public void Earn(double amount) { if (amount > 0) money += amount; }
            public void Train(double seconds) { }
            public string HousePowerBlocker => unpaid ? "停电：电费欠缴" : tripped ? "跳闸" : null;
            public string Blocker => HousePowerBlocker ?? thermalBlocker;
            public IReadOnlyList<XgCardSpec> Cards => cards;
            public double PowerLimitWatts => 3500;
            public double HouseWatts { get { double w = extraWatts; foreach (var g in cards) w += g.watts; return w; } }
            public bool BreakerTripped => tripped;
            public void TripBreaker() { tripped = true; }
            public bool ResetBreaker() { if (HouseWatts - UnpluggedWatts > PowerLimitWatts) return false; tripped = false; return true; }
            public double UnpluggedWatts { get; set; }
        }

        /// <summary>The same household seen through the plain host (no wiring information at all).</summary>
        sealed class Plain : IXgHost
        {
            public double money = 100000, compute = 2.6, vram = 8192;
            public double Compute => compute;
            public double VramMB => vram;
            public double Money => money;
            public bool Spend(double amount) { if (amount < 0 || money + 1e-9 < amount) return false; money -= amount; return true; }
            public void Earn(double amount) { if (amount > 0) money += amount; }
            public void Train(double seconds) { }
            public string Blocker => null;
        }

        static XgCardSpec Ti980 => new XgCardSpec { id = "gtx980ti", name = "GTX 980 Ti 6G", compute = 1, vramMB = 6144, watts = 250 };
        static XgCardSpec G1080 => new XgCardSpec { id = "gtx1080", name = "GTX 1080 8G", compute = 1.6, vramMB = 8192, watts = 180 };

        /// <summary>A deployed checkpoint: the repository entry and the dataset's best.</summary>
        static XgModelEntry Deploy(XgSim sim, string dataset, string arch, double acc, int depth = 2, int width = 2)
        {
            var d = XgCatalog.Dataset(dataset);
            var e = new XgModelEntry { id = sim.S.nextModelId++, track = (int)d.track, dataset = dataset, arch = arch, depth = depth, width = width, acc = acc, record = true };
            e.name = XgSim.FileName(e);
            sim.S.models.Add(e);
            var b = sim.Best(dataset);
            if (b == null) { b = new XgBest { dataset = dataset }; sim.S.best.Add(b); }
            b.acc = acc; b.arch = arch; b.modelId = e.id;
            if (!sim.S.owned.Contains(dataset)) sim.S.owned.Add(dataset);
            return e;
        }

        /// <summary>A stage-2 lab with digits and logic checkpoints, two signed orders and 自动标注 running. Noon.</summary>
        static XgSim Lab(int stage = 2)
        {
            var sim = new XgSim { GoldChance = 0, ForcedCheckChance = 0, ClockOfDaySeconds = 12 * 3600 };
            sim.S.stage = stage;
            Deploy(sim, "mnist", "lenet", .975);
            Deploy(sim, "logic", "mlp", .9, 3, 4);
            sim.S.contracts.Add("cheque");
            sim.S.contracts.Add("civilexam");
            sim.S.autoLevel = 2;
            sim.S.labels.Add(new XgLabelCount { dataset = "mnist", count = 40 });
            sim.S.labels.Add(new XgLabelCount { dataset = "logic", count = 40 });
            return sim;
        }

        [Test]
        public void IdleDefaultWiringPreservesIncomeAndHasReadyTraining()
        {
            var a = Lab();
            var b = Lab();
            var rig = new Rig(Ti980, G1080);
            var plain = new Plain { compute = rig.Compute, vram = rig.VramMB };
            for (int i = 0; i < 120; i++) { a.Tick(.5, rig); b.Tick(.5, plain); }

            var v = a.Wiring;
            // Every owned card on the PSU, every deployed model on a card, every running job wired.
            Assert.IsEmpty(a.S.wiring.pulled);
            foreach (var key in new[] { "h:gtx980ti#0", "h:gtx1080#0" }) { Assert.AreEqual("psu", v.Node(key).source, key); Assert.IsTrue(v.Node(key).online, key); }
            foreach (var m in a.S.best) { var n = v.Node("m:" + m.modelId); Assert.IsTrue(n.online, n.name); StringAssert.StartsWith("h:", n.card); }
            foreach (var key in new[] { "contract:cheque", "contract:civilexam", "label:mnist", "label:logic" })
            {
                Assert.AreEqual(XgTaskState.Run, v.Node(key).state, key + " " + v.Node(key).why);
                Assert.AreEqual(1.0, a.WireSpeed(key), 0, key);
            }
            Assert.AreEqual(1.0, a.TrainFactor(XgTrack.Vision), 0);
            Assert.AreEqual(1.0, a.TrainFactor(XgTrack.Sequence), 0);
            Assert.AreEqual(XgTaskState.Idle, v.Node("train:0").state);
            Assert.AreEqual(XgTaskState.Idle, v.Node("train:1").state);
            Assert.Less(a.TrainVram(a.S.vision, rig), a.Vram(rig), "deployed inference models reserve real memory");
            Assert.Greater(a.TrainVram(a.S.vision, rig), XgSim.VramNeedMB(a.S.vision));
            Assert.AreEqual(0, rig.UnpluggedWatts, 0);
            // Same income, same labels and same spending as the lab without any wiring behind it.
            Assert.AreEqual(b.IncomePerSecond, a.IncomePerSecond, 1e-12);
            Assert.AreEqual(b.S.totalIncome, a.S.totalIncome, 1e-9);
            Assert.AreEqual(plain.money, rig.money, 1e-9);
            Assert.AreEqual(b.CollaborationStats().total, a.CollaborationStats().total);
            Assert.AreEqual(b.CardsPerEpoch(b.S.vision, plain.Compute, true), a.CardsPerEpoch(a.S.vision, rig.Compute * a.TrainFactor(XgTrack.Vision), true));
            Assert.IsNull(a.Blocker(a.S.vision, rig));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void NewRecordInheritsManualPlacementOrExplicitDisconnection(bool disconnected)
        {
            var sim = Lab();
            var rig = new Rig(Ti980, G1080);
            sim.RefreshWiring(rig);
            string oldKey = "m:" + sim.Best("mnist").modelId;
            Assert.IsNull(sim.Connect("h:gtx980ti#0", oldKey, rig));
            if (disconnected) Assert.IsTrue(sim.Disconnect("h:gtx980ti#0", oldKey, rig));

            var record = Deploy(sim, "mnist", "lenet", .99);
            string newKey = "m:" + record.id;
            var view = sim.RefreshWiring(rig);
            var placement = sim.S.wiring.placed.Find(p => p.model == newKey);
            Assert.IsNotNull(placement, "the deployed successor, not a repository node, inherits the entry");
            Assert.IsTrue(placement.manual);
            Assert.AreEqual(disconnected ? "" : "h:gtx980ti#0", placement.card);
            Assert.IsFalse(sim.S.wiring.placed.Exists(p => p.model == oldKey));
            Assert.AreEqual(!disconnected, view.Node(newKey).online);
        }

        [Test]
        public void ConnectedTrainingWaitsForAnActualEpoch()
        {
            var sim = Lab();
            var rig = new Rig(Ti980);
            var view = sim.RefreshWiring(rig);
            Assert.AreEqual("run:0", view.Node("train:0").model, "the line can remain physically wired");
            Assert.AreEqual(XgTaskState.Idle, view.Node("train:0").state, "unlocked is not executing");
            Assert.AreEqual(0, view.Node("h:gtx980ti#0").busy);
            Assert.IsTrue(sim.Disconnect("run:0", "train:0", rig));
            Assert.IsNull(sim.Connect("run:0", "train:0", rig));
            Assert.IsFalse(sim.S.vision.epochActive, "reconnecting permits training but does not start it");
            Assert.AreEqual(XgTaskState.Idle, sim.Wiring.Node("train:0").state);

            Assert.IsTrue(sim.BeginEpoch(XgTrack.Vision, rig));
            Assert.AreEqual(XgTaskState.Run, sim.RefreshWiring(rig).Node("train:0").state);
            sim.S.vision.epochActive = false;
            Assert.AreEqual(XgTaskState.Idle, sim.RefreshWiring(rig).Node("train:0").state, "between epochs the line is waiting");
        }

        [Test]
        public void UnpaidHousePowerStopsLocalOrdersButNotCafePower()
        {
            var sim = Lab(3);
            var rig = new Rig(Ti980);
            sim.RefreshWiring(rig);
            Assert.IsNull(sim.Connect("cafe", "cafe7", rig));
            rig.unpaid = true;
            var view = sim.RefreshWiring(rig);
            Assert.IsFalse(view.Node("psu").online);
            Assert.IsFalse(view.Node("h:gtx980ti#0").online);
            Assert.IsTrue(view.Node("cafe7").online, "the café uses a separate paid line");
            Assert.AreEqual(0, sim.ContractIncome(XgCatalog.Contract("cheque")));
            double income = sim.S.totalIncome;
            sim.Tick(1, rig);
            Assert.AreEqual(income, sim.S.totalIncome, 1e-9, "unpaid local power cannot earn order income");

            rig.unpaid = false;
            sim.RefreshWiring(rig);
            Assert.Greater(sim.ContractIncome(XgCatalog.Contract("cheque")), 0);
        }

        [Test]
        public void NonPowerHostBlockerDoesNotDisconnectTheHousePsu()
        {
            var sim = Lab();
            var rig = new Rig(Ti980) { thermalBlocker = "温度保护：训练暂停" };
            var view = sim.RefreshWiring(rig);
            Assert.IsTrue(view.Node("psu").online);
            Assert.IsTrue(view.Node("h:gtx980ti#0").online);
            Assert.AreEqual(rig.thermalBlocker, sim.Blocker(sim.S.vision, rig));
        }

        [Test]
        public void ConcurrentEpochsShareComputeWithInferenceAndReleaseItAfterwards()
        {
            var sim = Lab();
            var rig = new Rig(Ti980);
            var card = sim.RefreshWiring(rig).Node("h:gtx980ti#0");
            double residentMemory = card.vramUse;
            double fullIncome = sim.ContractIncome(XgCatalog.Contract("cheque"));
            Assert.IsTrue(sim.BeginEpoch(XgTrack.Vision, rig));
            Assert.IsTrue(sim.BeginEpoch(XgTrack.Sequence, rig));
            card = sim.RefreshWiring(rig).Node(card.key);
            double vision = sim.TrainFactor(XgTrack.Vision), sequence = sim.TrainFactor(XgTrack.Sequence);
            Assert.Greater(vision, 0);
            Assert.AreEqual(vision, sequence, 1e-9, "equal training demand shares the card equally");
            Assert.Less(vision + sequence, 1, "inference consumes part of the same finite card");
            Assert.LessOrEqual(card.load * card.share, card.cu + 1e-9);
            Assert.GreaterOrEqual(card.vramUse, residentMemory + XgSim.VramNeedMB(sim.S.vision) + XgSim.VramNeedMB(sim.S.sequence) - 1e-9);
            Assert.Less(sim.ContractIncome(XgCatalog.Contract("cheque")), fullIncome);

            sim.S.sequence.epochActive = false;
            sim.RefreshWiring(rig);
            Assert.Greater(sim.TrainFactor(XgTrack.Vision), vision);
            sim.S.vision.epochActive = false;
            sim.RefreshWiring(rig);
            Assert.AreEqual(fullIncome, sim.ContractIncome(XgCatalog.Contract("cheque")), 1e-9);
        }

        [Test]
        public void SecondEpochCannotReuseMemoryReservedByTheFirst()
        {
            var sim = Lab();
            var gpu = Ti980;
            var rig = new Rig(gpu);
            double residentMemory = sim.RefreshWiring(rig).Node("h:gtx980ti#0").vramUse;
            gpu.vramMB = residentMemory + XgSim.VramNeedMB(sim.S.vision) + XgSim.VramNeedMB(sim.S.sequence) - 1;
            sim.RefreshWiring(rig);
            Assert.IsTrue(sim.BeginEpoch(XgTrack.Vision, rig));
            // No refresh between clicks: reservations must use the actual epoch state, not a stale graph.
            Assert.IsFalse(sim.BeginEpoch(XgTrack.Sequence, rig));
            Assert.IsFalse(sim.S.sequence.epochActive);
            Assert.IsNotNull(sim.Blocker(sim.S.sequence, rig));
            Assert.GreaterOrEqual(sim.TrainVram(sim.S.vision, rig), XgSim.VramNeedMB(sim.S.vision));
            sim.S.vision.epochActive = false;
            Assert.IsTrue(sim.BeginEpoch(XgTrack.Sequence, rig), "completed work releases its memory immediately");
        }

        [Test]
        public void FirstEpochStartChecksTheRigBeforeThereIsAWiringView()
        {
            var sim = Lab();
            var gpu = Ti980;
            gpu.vramMB = XgSim.VramNeedMB(sim.S.vision) + 1;
            var rig = new Rig(gpu);
            Assert.IsFalse(sim.BeginEpoch(XgTrack.Vision, rig), "deployed inference memory counts even before the first tick");
            Assert.IsFalse(sim.S.vision.epochActive);
        }

        [Test]
        public void RestoredOverlappingEpochsWaitForMemoryWithoutDeadlocking()
        {
            var sim = Lab();
            var gpu = Ti980;
            var rig = new Rig(gpu);
            double resident = sim.RefreshWiring(rig).Node("h:gtx980ti#0").vramUse;
            gpu.vramMB = resident + XgSim.VramNeedMB(sim.S.vision) + XgSim.VramNeedMB(sim.S.sequence) - 1;
            sim.S.vision.epochActive = sim.S.sequence.epochActive = true;
            sim.S.vision.epochDuration = sim.S.sequence.epochDuration = 10;
            sim.RefreshWiring(rig);
            Assert.Greater(sim.TrainFactor(XgTrack.Vision), 0, "one restored epoch is admitted deterministically");
            Assert.AreEqual(0, sim.TrainFactor(XgTrack.Sequence));
            Assert.LessOrEqual(sim.Wiring.Node("h:gtx980ti#0").vramUse, gpu.vramMB);
            sim.Tick(.1, rig);
            Assert.Greater(sim.S.vision.epochProgress, 0);
            Assert.AreEqual(0, sim.S.sequence.epochProgress);
            sim.S.vision.epochDuration = sim.S.sequence.epochDuration = .3;
            sim.Tick(2, rig);
            Assert.AreEqual(1, sim.S.vision.epoch, "the admitted epoch settles before releasing its reservation");
            Assert.AreEqual(1, sim.S.sequence.epoch, "the waiting epoch resumes and completes after memory is released");
        }

        [Test]
        public void MemoryWaiterCannotReuseTheCompletedEpochsTimeSlice()
        {
            var sim = Lab();
            var gpu = Ti980;
            var rig = new Rig(gpu);
            double resident = sim.RefreshWiring(rig).Node("h:gtx980ti#0").vramUse;
            gpu.vramMB = resident + XgSim.VramNeedMB(sim.S.vision) + XgSim.VramNeedMB(sim.S.sequence) - 1;
            sim.S.vision.epochActive = sim.S.sequence.epochActive = true;
            sim.S.vision.epochDuration = sim.S.sequence.epochDuration = .6;
            sim.RefreshWiring(rig);
            sim.Tick(1, rig);
            Assert.AreEqual(1, sim.S.vision.epoch);
            Assert.AreEqual(0, sim.S.sequence.epoch, "a waiting epoch cannot consume the same entire time slice twice");
            Assert.AreEqual(0, sim.S.sequence.epochProgress);
            sim.Tick(1, rig);
            Assert.AreEqual(1, sim.S.sequence.epoch);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ActiveEpochPausesWithoutPowerOrItsTaskWire(bool unplugPower)
        {
            var sim = Lab();
            var rig = new Rig(Ti980);
            sim.RefreshWiring(rig);
            Assert.IsTrue(sim.BeginEpoch(XgTrack.Vision, rig));
            sim.Tick(.1, rig);
            double progress = sim.S.vision.epochProgress;
            string from = unplugPower ? "psu" : "run:0";
            string to = unplugPower ? "h:gtx980ti#0" : "train:0";
            Assert.IsTrue(sim.Disconnect(from, to, rig));
            sim.Tick(1, rig);
            Assert.IsTrue(sim.S.vision.epochActive, "blocked work waits instead of being discarded at completion");
            Assert.AreEqual(progress, sim.S.vision.epochProgress, 1e-12);
            Assert.AreEqual(0, sim.S.vision.epoch);
            Assert.IsNull(sim.Connect(from, to, rig));
            sim.Tick(.1, rig);
            Assert.Greater(sim.S.vision.epochProgress, progress);
        }

        [Test]
        public void ContentionSlowsEpochProgressWithoutPenalizingLearningTwice()
        {
            var solo = Lab();
            var shared = Lab();
            solo.UseBoard = shared.UseBoard = false;
            var soloRig = new Rig(Ti980);
            var sharedRig = new Rig(Ti980);
            solo.RefreshWiring(soloRig);
            shared.RefreshWiring(sharedRig);
            Assert.IsTrue(solo.BeginEpoch(XgTrack.Vision, soloRig));
            Assert.IsTrue(shared.BeginEpoch(XgTrack.Vision, sharedRig));
            Assert.IsTrue(shared.BeginEpoch(XgTrack.Sequence, sharedRig));
            solo.RefreshWiring(soloRig);
            shared.RefreshWiring(sharedRig);
            solo.Tick(.2, soloRig);
            shared.Tick(.2, sharedRig);
            Assert.Greater(solo.S.vision.epochProgress, shared.S.vision.epochProgress);
            for (int i = 0; i < 200 && solo.S.vision.epoch == 0; i++) solo.Tick(.1, soloRig);
            for (int i = 0; i < 200 && shared.S.vision.epoch == 0; i++) shared.Tick(.1, sharedRig);
            Assert.AreEqual(1, solo.S.vision.epoch);
            Assert.AreEqual(1, shared.S.vision.epoch);
            Assert.AreEqual(solo.S.vision.steps, shared.S.vision.steps, 1e-9, "the same completed work took longer, not less learning per completed epoch");
        }

        [Test]
        public void OldSaveWithoutWiringLoadsIntoTheDefaultLayout()
        {
            var lab = Lab();
            lab.S.wiring = null;
            var sim = new XgSim(lab.S);
            Assert.IsNotNull(sim.S.wiring);
            Assert.AreEqual(1, sim.S.wiring.version);
            var rig = new Rig(Ti980);
            sim.Tick(1, rig);
            Assert.AreEqual(2, sim.S.wiring.placed.Count, "both deployed models placed");
            foreach (var p in sim.S.wiring.placed) { Assert.AreEqual("h:gtx980ti#0", p.card); Assert.IsFalse(p.manual); }
        }

        [Test]
        public void ModelsMustFitInTheirCardsVram()
        {
            var sim = Lab(3);
            var rig = new Rig(Ti980);
            // 本体 as a 400M-parameter Transformer: about 3.2 GB of weights at inference.
            sim.S.unlocked.Add("transformer");
            sim.Tick(1, rig);
            // Larger than the caps would configure: the wiring rules, not the size, are under test here.
            sim.S.sequence.arch = "transformer"; sim.S.sequence.depth = 32; sim.S.sequence.width = 6;
            sim.RefreshWiring(rig);
            var self = sim.Wiring.Node("self");
            Assert.Greater(self.vramNeed, XgSim.CafeVramMB);
            Assert.IsTrue(self.online, "auto-placed on the 6 GB card");

            Assert.IsNull(sim.Connect("cafe", "cafe7", rig));
            Assert.IsNull(sim.Connect("cafe7", "self", rig), "the wire is allowed …");
            var v = sim.Wiring;
            Assert.IsFalse(v.Node("self").online, "… but it does not fit");
            StringAssert.StartsWith("显存爆了", v.Node("self").why);
            Assert.AreEqual("显存爆了", v.Node("cafe7").tag);
            Assert.IsTrue(v.Node("cafe7").bad);

            // A digits model on the same card goes down with it, and its order stops paying.
            var digits = "m:" + sim.Best("mnist").modelId;
            Assert.IsNull(sim.Connect("cafe7", digits, rig));
            Assert.IsFalse(sim.Wiring.Node(digits).online);
            Assert.AreEqual(0, sim.WireSpeed("contract:cheque"));
            Assert.AreEqual(0, sim.ContractIncome(XgCatalog.Contract("cheque")));
            // Back on the 980 Ti it pays again.
            Assert.IsNull(sim.Connect("h:gtx980ti#0", digits, rig));
            Assert.IsTrue(sim.Wiring.Node(digits).online);
            Assert.Greater(sim.ContractIncome(XgCatalog.Contract("cheque")), 0);

            // Auto placement never overfills: a second small card takes what does not fit.
            var small = new XgSim { GoldChance = 0 };
            small.S.stage = 2;
            for (int i = 0; i < 12; i++) Deploy(small, XgCatalog.Desks[i % XgCatalog.Desks.Length].id, "mlp", .8, 3, 6);
            var two = new Rig(new XgCardSpec { id = "gtx750ti", name = "750", compute = .25, vramMB = 2048, watts = 60 }, new XgCardSpec { id = "gtx750ti", name = "750", compute = .25, vramMB = 2048, watts = 60 });
            small.Tick(1, two);
            foreach (var n in small.Wiring.nodes) if (n.lane == XgWireLane.Gpu && n.house) Assert.LessOrEqual(n.vramUse, n.vramMB, n.key);
        }

        [Test]
        public void PretrainingOnTheHousePsuTripsTheBreakerUntilReset()
        {
            var sim = Lab(6);
            var rig = new Rig(Ti980, G1080);
            sim.Tick(1, rig);
            // No server room: the old 「跳闸了」 line, and now the breaker really trips.
            Assert.IsFalse(sim.TogglePretrain(rig));
            Assert.IsTrue(rig.tripped);
            StringAssert.StartsWith("跳闸了", sim.S.log[sim.S.log.Count - 1]);
            sim.Tick(1, rig);
            var v = sim.Wiring;
            Assert.IsTrue(v.tripped);
            Assert.IsFalse(v.Node("h:gtx980ti#0").online, "cards go offline");
            Assert.AreEqual(0, sim.WireSpeed("contract:cheque"), "and the jobs on them stop");
            Assert.IsTrue(sim.ResetBreakerFromWiring(rig));
            Assert.IsFalse(rig.tripped);

            // With the server room 本体 sits on the IDC rack and pre-training runs.
            sim.S.unlocked.Add("datacenter");
            Assert.IsTrue(sim.TogglePretrain(rig), sim.S.log[sim.S.log.Count - 1]);
            sim.RefreshWiring(rig);
            Assert.AreEqual("rack", sim.Wiring.Node("self").card);
            Assert.AreEqual("self", sim.Wiring.Node("pretrain").model, "the wire is the running switch");
            sim.Tick(1, rig);
            Assert.IsTrue(sim.S.pretrainRunning);
            Assert.IsFalse(rig.tripped);

            // Moving 本体 onto a house card while it pre-trains overloads the PSU.
            Assert.IsNull(sim.Connect("h:gtx1080#0", "self", rig));
            sim.Tick(1, rig);
            Assert.IsTrue(rig.tripped);
            Assert.IsFalse(sim.S.pretrainRunning);
            StringAssert.StartsWith("跳闸了", sim.S.log[sim.S.log.Count - 1]);

            // Over the limit the reset is refused until something is unplugged.
            rig.extraWatts = 3200;
            Assert.IsFalse(sim.ResetBreakerFromWiring(rig));
            Assert.IsTrue(rig.tripped);
            Assert.IsTrue(sim.Disconnect("psu", "h:gtx980ti#0", rig));
            Assert.AreEqual(250, rig.UnpluggedWatts, 1e-9, "the house stops drawing the unplugged card");
            Assert.IsTrue(sim.ResetBreakerFromWiring(rig));
            Assert.IsFalse(rig.tripped);
            Assert.IsFalse(sim.Wiring.Node("h:gtx980ti#0").online);
        }

        [Test]
        public void CafeCardsCostRentAndHalfNightsAreCheaper()
        {
            var sim = Lab(3);
            var rig = new Rig(Ti980);
            sim.Tick(1, rig);
            Assert.IsNotNull(sim.Connect("cafe", "h:gtx980ti#0", rig), "your own card does not go to the café");
            Assert.IsNotNull(sim.Connect("psu", "cafe7", rig), "the café PCs stay at the café");
            Assert.IsNull(sim.Connect("cafe", "cafe7", rig));
            Assert.IsNull(sim.Connect("cafe", "cafe8", rig));
            double before = sim.S.wiring.rentPaid;
            sim.Tick(10, rig);
            Assert.AreEqual(2 * XgSim.CafeRent * 10, sim.S.wiring.rentPaid - before, 1e-6, "¥0.4 per card per second, day rate");
            Assert.AreEqual(2 * XgSim.CafeRent, sim.Wiring.rentPerSecond, 1e-9);

            // 00:00 包夜: 60%.
            sim.ClockOfDaySeconds = 30;
            sim.Tick(1, rig);
            Assert.IsTrue(sim.CafeNight);
            Assert.IsTrue(sim.WiringLog[sim.WiringLog.Count - 1].text.StartsWith("00:00"));
            before = sim.S.wiring.rentPaid;
            double money = rig.money, income = sim.S.totalIncome;
            sim.Tick(10, rig);
            Assert.AreEqual(2 * XgSim.CafeRent * XgSim.NightRate * 10, sim.S.wiring.rentPaid - before, 1e-6);
            Assert.AreEqual(2 * XgSim.CafeRent * XgSim.NightRate * 10, money + (sim.S.totalIncome - income) - rig.money, 1e-6, "it comes out of the wallet");
            Assert.AreEqual(2 * XgSim.CafeRent * XgSim.NightRate, sim.Wiring.rentPerSecond, 1e-9);

            // A rented card trains: the vision line on the 980 Ti plus a café 960 is 1.45×.
            Assert.IsNull(sim.Connect("cafe7", "run:0", rig));
            Assert.AreEqual(1 + XgSim.CafeCompute, sim.TrainFactor(XgTrack.Vision), 1e-9);
            // Unplugged from 阿杰's line, the rent stops.
            Assert.IsTrue(sim.Disconnect("cafe", "cafe7", rig));
            Assert.IsTrue(sim.Disconnect("cafe", "cafe8", rig));
            before = sim.S.wiring.rentPaid;
            sim.Tick(10, rig);
            Assert.AreEqual(0, sim.S.wiring.rentPaid - before, 1e-9);
            Assert.AreEqual(1, sim.TrainFactor(XgTrack.Vision), 1e-9);
        }

        static XgSim Renting(Rig rig, out List<bool> asked)
        {
            var sim = Lab(3);
            var list = new List<bool>();
            sim.AjieAsked += suspicious => list.Add(suspicious);
            sim.Tick(1, rig);
            Assert.IsNull(sim.Connect("cafe", "cafe7", rig));
            Assert.IsNull(sim.Connect("cafe", "cafe8", rig));
            sim.TakeYYLines();
            sim.Tick(XgSim.AjieFirstAsk - 2, rig);
            Assert.IsFalse(sim.AjiePending);
            sim.Tick(3, rig);
            Assert.IsTrue(sim.AjiePending);
            Assert.AreEqual(1, list.Count);
            Assert.IsFalse(list[0]);
            asked = list;
            return sim;
        }

        [Test]
        public void AjieTruthDiscountsTheRentAndOpensHisReceipts()
        {
            var rig = new Rig(Ti980);
            var sim = Renting(rig, out _);
            Assert.IsNull(sim.Wiring.Node("ajie"), "his job is hidden until you tell him");
            Assert.IsTrue(sim.AnswerAjie("ai"));
            Assert.IsFalse(sim.AjiePending);
            Assert.AreEqual(XgSim.AjieTruthRate, sim.AjieRentMultiplier, 1e-9);
            Assert.AreEqual(2 * XgSim.CafeRent * .8, sim.Wiring.rentPerSecond, 1e-9);
            var lines = sim.TakeYYLines();
            Assert.AreEqual("ajie", lines[0].from);
            StringAssert.Contains("收银小票", lines[0].zh);
            // The receipt job: digits ≥ 95% (the 97.5% checkpoint qualifies) and wired by default.
            var job = sim.Wiring.Node("ajie");
            Assert.AreEqual(XgTaskState.Run, job.state, job.why);
            double paid = sim.S.wiring.receiptsPaid;
            sim.Tick(20, rig);
            Assert.Greater(sim.S.wiring.receiptsPaid, paid);
            // A worse digits model is refused.
            sim.Best("mnist").acc = .9;
            sim.Tick(1, rig);
            StringAssert.StartsWith("准确率不够", sim.Wiring.Node("ajie").why);
            // He never asks again once he knows.
            sim.Tick(XgSim.AjieAskAgain * 2, rig);
            Assert.IsFalse(sim.AjiePending);
        }

        [TestCase(1, false)]
        [TestCase(2, false)]
        [TestCase(1, true)]
        [TestCase(2, true)]
        public void AjieFirstAsksWhatIdleRentalsAreForWithoutClaimingHighLoad(int cards, bool english)
        {
            var rig = new Rig(Ti980);
            var sim = Lab(3);
            sim.English = english;
            bool? suspicious = null;
            sim.AjieAsked += value => suspicious = value;
            sim.Tick(1, rig);
            Assert.IsNull(sim.Connect("cafe", "cafe7", rig));
            if (cards == 2) Assert.IsNull(sim.Connect("cafe", "cafe8", rig));
            sim.Tick(XgSim.AjieFirstAsk + 1, rig);

            Assert.AreEqual(0, XgSim.CafeLoad(sim.Wiring), 1e-9);
            Assert.AreEqual(cards, sim.CafeCardsOnline);
            Assert.IsTrue(sim.AjiePending);
            Assert.AreEqual(false, suspicious);
            string question = sim.WiringLog[sim.WiringLog.Count - 1].text;
            StringAssert.DoesNotContain("满载", question);
            StringAssert.DoesNotContain("maxed out", question);
            StringAssert.DoesNotContain("屏幕都黑", question);
            StringAssert.DoesNotContain("screen off", question);
            StringAssert.Contains(english ? "What" : "干啥", question);
        }

        [Test]
        public void AjieMiningCostsMoreAndHeDoesNotAskAgain()
        {
            var rig = new Rig(Ti980);
            var sim = Renting(rig, out var asked);
            Assert.IsTrue(sim.AnswerAjie("mine"));
            Assert.AreEqual(XgSim.AjieMiningRate, sim.AjieRentMultiplier, 1e-9);
            Assert.AreEqual(2 * XgSim.CafeRent * 1.6, sim.Wiring.rentPerSecond, 1e-9);
            Assert.IsNull(sim.Wiring.Node("ajie"));
            Assert.IsNull(sim.Connect("cafe7", "run:0", rig));
            Assert.IsNull(sim.Connect("cafe8", "run:0", rig));
            sim.Tick(XgSim.AjieAskAgain + 10, rig);
            Assert.IsFalse(sim.AjiePending);
            Assert.AreEqual(1, asked.Count);
        }

        [Test]
        public void AjieAcceptsOneGameLieAndTakesTheMachinesBackAfterTheSecond()
        {
            var rig = new Rig(Ti980);
            var sim = Renting(rig, out var asked);
            Assert.IsTrue(sim.AnswerAjie("game"));
            Assert.AreEqual(1, sim.AjieRentMultiplier, 1e-9, "he accepts it once");
            Assert.IsFalse(sim.AjieCutoff);
            // Cards idle: no suspicion.
            sim.Tick(XgSim.AjieAskAgain + 10, rig);
            Assert.IsFalse(sim.AjiePending, "idle cards do not make him ask");
            // Real work makes both café cards busy.
            Assert.IsNull(sim.Connect("cafe7", "run:0", rig));
            Assert.IsNull(sim.Connect("cafe8", "run:0", rig));
            Assert.IsTrue(sim.BeginEpoch(XgTrack.Vision, rig));
            sim.S.vision.epochDuration = XgSim.AjieAskAgain + 10;
            sim.Tick(1, rig);
            Assert.GreaterOrEqual(XgSim.CafeLoad(sim.Wiring), XgSim.AjieSuspicionLoad);
            sim.Tick(XgSim.AjieAskAgain, rig);
            Assert.IsTrue(sim.AjiePending);
            Assert.AreEqual(2, asked.Count);
            Assert.IsTrue(asked[1], "the second time he is suspicious");
            StringAssert.Contains("说实话", sim.WiringLog[sim.WiringLog.Count - 1].text);
            sim.TakeYYLines();
            Assert.IsTrue(sim.AnswerAjie("game"));
            Assert.IsTrue(sim.AjieCutoff);
            StringAssert.Contains("机器我收回了", sim.TakeYYLines()[0].zh);
            var v = sim.Wiring;
            Assert.IsFalse(v.Node("cafe7").online);
            Assert.IsFalse(v.Node("cafe").available);
            Assert.AreEqual(0, v.rentPerSecond, 1e-9);
            Assert.Greater(sim.TrainFactor(XgTrack.Vision), 0, "house cards keep working after the café is cut off");
            Assert.LessOrEqual(sim.TrainFactor(XgTrack.Vision), 1, "no café compute remains");
            Assert.IsNotNull(sim.Connect("cafe", "cafe7", rig), "no renting again in this save");
            // It survives a save.
            var reloaded = new XgSim(sim.S);
            Assert.IsTrue(reloaded.AjieCutoff);
            Assert.IsNotNull(reloaded.Connect("cafe", "cafe8", rig));
        }

        [Test]
        public void AStageLockedOrderSaysWhenItOpensNotThatTheAccuracyIsLow()
        {
            var sim = Lab(XgSim.ContractStage - 1);
            var rig = new Rig(Ti980);
            sim.Tick(1, rig);
            string digits = "m:" + sim.Best("mnist").modelId;
            sim.Best("mnist").acc = .995; // above the order's 99% line: the score is not the reason
            Assert.AreEqual("第 " + XgSim.ContractStage + " 阶段才开放", sim.Connect(digits, "contract:zipcode", rig));
            Assert.IsFalse(sim.Signed("zipcode"));
            sim.English = true;
            Assert.AreEqual("Opens at stage " + XgSim.ContractStage, sim.Connect(digits, "contract:zipcode", rig));
            sim.English = false;
            // The lock is the stage, whatever the score: a low score does not hide it behind a second-best reason.
            sim.Best("mnist").acc = .9;
            StringAssert.StartsWith("第 ", sim.Connect(digits, "contract:zipcode", rig));
            // Once the stage is there, the score is judged and signs it.
            sim.S.stage = XgSim.ContractStage;
            sim.Best("mnist").acc = .995;
            Assert.IsNull(sim.Connect(digits, "contract:zipcode", rig));
            Assert.IsTrue(sim.Signed("zipcode"));
        }

        [Test]
        public void TheAccuracyMessageComparesTheRealNumbers()
        {
            var sim = Lab(XgSim.ContractStage);
            var rig = new Rig(Ti980);
            sim.Tick(1, rig);
            string digits = "m:" + sim.Best("mnist").modelId;
            sim.Best("mnist").acc = .975;
            Assert.AreEqual("准确率不够 97.5% < 99%", sim.Connect(digits, "contract:zipcode", rig));
            // Just under the line must not round up to it ("99% < 99%").
            sim.Best("mnist").acc = .9899;
            Assert.AreEqual("准确率不够 98.9% < 99%", sim.Connect(digits, "contract:zipcode", rig));
            sim.English = true;
            Assert.AreEqual("Accuracy too low 98.9% < 99%", sim.Connect(digits, "contract:zipcode", rig));
            // A plain low score.
            sim.English = false;
            sim.Best("mnist").acc = .9;
            Assert.AreEqual("准确率不够 90% < 99%", sim.Connect(digits, "contract:zipcode", rig));
            Assert.IsFalse(sim.Signed("zipcode"));
        }

        [Test]
        public void JobWiresUseTheOrdersRules()
        {
            var sim = Lab(XgSim.FeatureUnlock["crowd"]); // orders can only be signed from the crowd stage on
            var rig = new Rig(Ti980);
            sim.Tick(1, rig);
            string digits = "m:" + sim.Best("mnist").modelId, logic = "m:" + sim.Best("logic").modelId;
            StringAssert.StartsWith("数据不对口", sim.Connect(logic, "contract:zipcode", rig));
            StringAssert.StartsWith("准确率不够", sim.Connect(digits, "contract:zipcode", rig), "97.5% < 99%");
            Assert.IsFalse(sim.Signed("zipcode"));
            sim.Best("mnist").acc = .995;
            Assert.IsNull(sim.Connect(digits, "contract:zipcode", rig), "wiring an order signs it");
            Assert.IsTrue(sim.Signed("zipcode"));
            // Unplugging pauses the pay; plugging back resumes it.
            Assert.IsTrue(sim.Disconnect(digits, "contract:zipcode", rig));
            Assert.AreEqual(0, sim.ContractIncome(XgCatalog.Contract("zipcode")));
            Assert.IsNull(sim.Connect(digits, "contract:zipcode", rig));
            Assert.Greater(sim.ContractIncome(XgCatalog.Contract("zipcode")), 0);
            // An unplugged training line blocks training.
            Assert.IsTrue(sim.Disconnect("run:0", "train:0", rig));
            StringAssert.StartsWith("接线", sim.Blocker(sim.S.vision, rig));
            Assert.IsNull(sim.Connect("run:0", "train:0", rig));
            Assert.IsNull(sim.Blocker(sim.S.vision, rig));
            // Unplugging a desk holds its auto-labelling.
            Assert.Greater(sim.CollaborationRate("mnist", rig), 0);
            Assert.IsTrue(sim.Disconnect(digits, "label:mnist", rig));
            Assert.AreEqual(0, sim.CollaborationRate("mnist", rig));
        }
    }
}
