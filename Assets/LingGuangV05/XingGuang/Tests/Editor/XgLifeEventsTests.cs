using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>节奏重做 §2.5: the life-event pool (XgSim.LifeEvents.cs).</summary>
    public sealed class XgLifeEventsTests
    {
        /// <summary>A wallet with or without the house's cards (接线).</summary>
        class Host : IXgHost
        {
            public double money, spent;
            /// <summary>False: the lab's own income is not paid in (a wallet that stays poor).</summary>
            public bool earns = true;
            public readonly List<double> earned = new List<double>();
            public Host(double money) { this.money = money; }
            public double Compute => 1;
            public double VramMB => 16000;
            public double Money => money;
            public bool Spend(double amount) { if (amount < 0 || money + 1e-9 < amount) return false; money -= amount; spent += amount; return true; }
            public void Earn(double amount) { earned.Add(amount); if (earns) money += amount; }
            public void Train(double seconds) { }
            public string Blocker => null;
        }

        sealed class RigHost : Host, IXgRig
        {
            readonly List<XgCardSpec> cards = new List<XgCardSpec>();
            public RigHost(double money, int count) : base(money)
            {
                for (int i = 0; i < count; i++) cards.Add(new XgCardSpec { id = "gtx970", name = "GTX 970", nameEn = "GTX 970", compute = 1, vramMB = 4000, watts = 150 });
            }
            public IReadOnlyList<XgCardSpec> Cards => cards;
            public double PowerLimitWatts => 3500;
            public double HouseWatts => 0;
            public bool BreakerTripped => false;
            public void TripBreaker() { }
            public bool ResetBreaker() => true;
            public double UnpluggedWatts { get; set; }
        }

        sealed class House : IXgHousehold
        {
            public bool BillsActive { get; set; } = true;
            public double MonthKwh { get; set; }
            public double HighKwh => 400;
            public double SecondsOfDay { get; set; } = 12 * 3600;
        }

        /// <summary>Training curve is owned; the requested ability stage is available.</summary>
        static XgSim Lab(long seed = 7, int stage = 2)
        {
            var sim = new XgSim { GoldChance = 0 };
            sim.S.rng = seed;
            sim.S.unlocked.Add("train.curve");
            sim.S.stage = sim.S.stageVision = sim.S.stageSequence = stage;
            for (int a = 2; a <= stage; a++) sim.S.abilitiesEmerged.Add(a);
            return sim;
        }

        /// <summary>The scheduler is held (effects are tested on events opened by hand).</summary>
        static XgSim Quiet(int stage = 3)
        {
            var sim = Lab(stage: stage);
            sim.LifeEventHold = () => true;
            return sim;
        }

        static List<KeyValuePair<double, string>> Run(XgSim sim, IXgHost host, double seconds, int answer = -1, bool clearCooldowns = false)
        {
            var opened = new List<KeyValuePair<double, string>>();
            double t = 0;
            sim.LifeEventOpened += e => opened.Add(new KeyValuePair<double, string>(t, e.id));
            for (; t < seconds; t += 1)
            {
                sim.Tick(1, host);
                // A player takes the rare item offered after an ability (it holds the pool while it waits).
                while (sim.RelicPickPending && sim.PickRelic(sim.RelicPick[0])) { }
                if (answer >= 0 && sim.LifeEventOpen)
                {
                    var v = sim.LifeEventView(host);
                    int pick = answer < v.choices.Count && v.choices[answer].enabled ? answer : v.choices.FindIndex(c => c.enabled);
                    Assert.IsTrue(sim.AnswerLifeEvent(pick, host));
                    Assert.IsTrue(sim.AcknowledgeLifeEventResult(), "result is explicitly closed after the committed choice");
                    if (clearCooldowns) sim.S.lifeCooldowns.Clear();
                }
            }
            return opened;
        }

        [Test]
        public void TheDrawsAreDeterministicFromTheSeed()
        {
            string Trace(long seed)
            {
                var sim = Lab(seed); sim.S.best.Add(new XgBest { dataset = "spam", acc = .9 });
                var host = new Host(1000000);
                return string.Join(",", Run(sim, host, 2400, answer: 1).Select(p => p.Key + ":" + p.Value));
            }
            string a = Trace(11), b = Trace(11), c = Trace(12345);
            Assert.IsNotEmpty(a);
            Assert.AreEqual(a, b, "the same seed draws the same events at the same times");
            Assert.AreNotEqual(a, c, "another seed draws differently");
        }

        [Test]
        public void TheDrawsLeaveTheLabsOwnDiceAlone()
        {
            var sim = Lab(); var host = new Host(100);
            long before = sim.S.rng;
            sim.S.best.Add(new XgBest { dataset = "spam", acc = .9 });
            sim.LifeEventOpened += e => Assert.AreEqual(before, sim.S.rng, "the pool rolls its own stream");
            Assert.IsTrue(sim.OpenLifeEventNow("forum_post", host));
        }

        [Test]
        public void OnlyEligibleEventsFire()
        {
            // Poor (no hoarding), no cards, a full-enough disk nowhere, ADSL: only the forum thread can come.
            var sim = Lab(); var host = new Host(100) { earns = false };
            Assert.IsEmpty(sim.LifeEventsEligible(host), "nothing to talk about yet");
            Assert.IsEmpty(Run(sim, host, 900), "an empty pool draws nothing");
            sim.S.best.Add(new XgBest { dataset = "spam", acc = .9 });
            CollectionAssert.AreEqual(new[] { "forum_post" }, sim.LifeEventsEligible(host));
            var opened = Run(sim, host, 900, answer: 0);
            Assert.IsNotEmpty(opened);
            Assert.IsTrue(opened.All(p => p.Value == "forum_post"), string.Join(",", opened.Select(p => p.Value)));

            // Cards and night training bring the roommate; two cards at stage 3 bring 老周.
            var rich = Lab(stage: 3); var rig = new RigHost(100, 2);
            var house = new House { SecondsOfDay = 3 * 3600 };
            rich.Household = house;
            Assert.IsTrue(rich.LifeEventsEligible(rig).Contains("zhou_card"));
            Assert.IsFalse(rich.LifeEventsEligible(rig).Contains("roommate_fan"), "no training lately");
            rich.S.lifeLastTrain = rich.S.lifeClock;
            Assert.IsTrue(rich.LifeEventsEligible(rig).Contains("roommate_fan"));
            house.SecondsOfDay = 9 * 3600;
            Assert.IsFalse(rich.LifeEventsEligible(rig).Contains("roommate_fan"), "only between midnight and six");
            Assert.IsFalse(rich.LifeEventsEligible(new RigHost(100, 1)).Contains("zhou_card"), "one card is not lent");
        }

        [Test]
        public void HoardingBringsANudgeThatNeverTakesMoneyByItself()
        {
            var sim = Lab(); var host = new Host(1000000);
            Assert.IsTrue(sim.Hoarding(host));
            var pool = sim.LifeEventsEligible(host);
            Assert.IsTrue(pool.Contains("zhou_loan") && pool.Contains("licai_call"));
            Assert.IsFalse(sim.Hoarding(new Host(sim.NextBigPurchase(host) * 2)), "twice the next purchase is saving, not hoarding");
            var opened = Run(sim, host, 1800);
            Assert.AreEqual(1, opened.Count);
            Assert.AreEqual(0, host.spent, 1e-6, "an open nudge costs nothing");
        }

        [Test]
        public void NeverTwoAtOnce()
        {
            var sim = Lab(); var host = new Host(1000000);
            sim.S.best.Add(new XgBest { dataset = "spam", acc = .9 });
            var opened = Run(sim, host, 3000);
            Assert.AreEqual(1, opened.Count, "the open card waits for its answer; nothing is drawn meanwhile");
            string id = sim.LifeEventId;
            Assert.IsFalse(sim.OpenLifeEventNow("forum_post", host, force: true));
            Assert.AreEqual(id, sim.LifeEventId);
        }

        [Test]
        public void TheIntervalIsEightMinutesFromResultClose()
        {
            var sim = Lab(3); var host = new Host(1000000);
            sim.S.best.Add(new XgBest { dataset = "spam", acc = .9 });
            var opened = Run(sim, host, 4000, answer: 1, clearCooldowns: true);
            Assert.That(opened.Count, Is.GreaterThanOrEqualTo(8));
            Assert.That(opened[0].Key, Is.InRange(479, 481));
            for (int i = 1; i < opened.Count; i++)
                Assert.That(opened[i].Key - opened[i - 1].Key, Is.InRange(479, 481));
        }

        [Test]
        public void ABusyPlayerPausesTimeAndAnAlreadyDueDrawGetsGrace()
        {
            var sim = Lab(); var host = new Host(1000000);
            bool busy = true;
            sim.LifeEventHold = () => busy;
            Assert.IsEmpty(Run(sim, host, 900), "nothing during a cutscene, exam or story");
            Assert.AreEqual(480, sim.LifeEventNext, 1e-6, "busy time never consumes the interval");
            busy = false;
            var opened = Run(sim, host, 481);
            Assert.AreEqual(1, opened.Count, "the paused interval resumes in full");

            var due = Lab(); due.LifeEventHold = () => busy;
            _ = due.LifeEventNext; due.S.lifeNext = 0; busy = true;
            due.ObserveLifeEventConflict();
            Assert.IsTrue(due.S.lifeCadenceDeferred, "already due conflict is observed even without Tick");
            busy = false;
            Assert.AreEqual(1, Run(due, host, 31).Count, "due conflict receives thirty active seconds after it ends");
        }

        [Test]
        public void NoEventBeforeTrainingCurveAndFinaleRemainsQuiet()
        {
            var sim = new XgSim { GoldChance = 0 }; var host = new Host(1000000);
            sim.S.openingFocus = true;
            sim.S.best.Add(new XgBest { dataset = "spam", acc = .9 });
            Assert.IsFalse(sim.LifeEventsUnlocked);
            Assert.IsEmpty(Run(sim, host, 1200), "the first minutes stay quiet");
            sim.S.openingFocus = false;
            Assert.IsEmpty(Run(sim, host, 1200), "still nothing before training curve purchase");
            Assert.IsTrue(sim.BuyNode("train.curve", host), "curve purchase starts the cadence");
            Assert.IsTrue(sim.LifeEventsUnlocked);
            var opened = Run(sim, host, XgSim.LifeIntervalMax + 1);
            Assert.AreEqual(1, opened.Count);
            Assert.That(opened[0].Key, Is.GreaterThanOrEqualTo(XgSim.LifeIntervalMin - 1), "a whole interval after the curve purchase");
            sim.S.fullOpen = true;
            Assert.IsFalse(sim.LifeEventsUnlocked, "the finale is left alone");
        }

        [Test]
        public void OldSavesLoad()
        {
            var state = new XgState();
            state.lifeHistory = null; state.lifeCooldowns = null; state.lifeHolds = null; state.lifeReturns = null;
            state.lifeOpen = "retired_event"; state.lifeArgKey = null; state.lifeVersion = 0; state.lifeNext = double.NaN;
            state.stage = 2; state.abilitiesEmerged.Add(2);
            var sim = new XgSim(state); var host = new Host(100);
            Assert.IsFalse(sim.LifeEventOpen, "an event this build does not know is dropped");
            sim.Tick(1, host);
            Assert.AreEqual(XgSim.LifeSchema, sim.S.lifeVersion);
            Assert.That(sim.LifeEventNext, Is.InRange(XgSim.LifeIntervalMin - 2, XgSim.LifeIntervalMax));
            Assert.IsNotNull(sim.S.lifeHistory); Assert.IsNotNull(sim.S.lifeHolds);
            Assert.IsFalse(sim.ZhouFavourOwed);
        }

        // ───────────── each choice ─────────────

        [Test]
        public void LendingACardUnplugsItForAWhileAndEarnsAFavour()
        {
            var sim = Quiet(); var host = new RigHost(1000, 2);
            Assert.IsTrue(sim.OpenLifeEventNow("zhou_card", host));
            Assert.IsTrue(sim.AnswerLifeEvent("lend", host));
            Assert.IsTrue(sim.AcknowledgeLifeEventResult(), "result is explicitly closed after the committed choice");
            Assert.IsTrue(sim.S.wiring.pulled.Contains("h:gtx970#1"), "the second card is out");
            Assert.IsTrue(sim.ZhouFavourOwed && sim.CardLent);
            sim.Tick(XgSim.LifeLendSeconds + 2, host);
            Assert.IsFalse(sim.S.wiring.pulled.Contains("h:gtx970#1"), "he brought it back");
            Assert.IsFalse(sim.CardLent);
            Assert.IsTrue(sim.ZhouFavourOwed, "the favour stays until it is used");
            Assert.AreEqual("zhou_card", sim.LifeHistory.Last().id);
        }

        [Test]
        public void ZhousServerCardBurnsOutOnlyOnce()
        {
            var sim = Quiet(); var host = new RigHost(1000, 2);
            Assert.IsTrue(sim.OpenLifeEventNow("zhou_card", host));
            StringAssert.Contains("卡烧了", sim.LifeEventView(host).body);
            Assert.IsTrue(sim.AnswerLifeEvent("refuse", host));
            Assert.IsTrue(sim.AcknowledgeLifeEventResult(), "result is explicitly closed after the committed choice");
            Assert.IsTrue(sim.OpenLifeEventNow("zhou_card", host, true));
            StringAssert.Contains("还是上回那事儿", sim.LifeEventView(host).body, "asked again after a no");
            Assert.IsTrue(sim.AnswerLifeEvent("lend", host));
            Assert.IsTrue(sim.AcknowledgeLifeEventResult(), "result is explicitly closed after the committed choice");
            sim.Tick(XgSim.LifeLendSeconds + 2, host);
            sim.S.lifeFavour = 0;
            Assert.IsTrue(sim.OpenLifeEventNow("zhou_card", host, true));
            string again = sim.LifeEventView(host).body;
            StringAssert.Contains("再借", again, "after a loan he asks for another one");
            StringAssert.DoesNotContain("卡烧了", again);
        }

        [Test]
        public void RefusingCoolsZhouALittle()
        {
            var sim = Quiet(); var host = new RigHost(1000, 2);
            Assert.IsTrue(sim.OpenLifeEventNow("zhou_card", host));
            Assert.IsTrue(sim.AnswerLifeEvent("refuse", host));
            Assert.IsTrue(sim.AcknowledgeLifeEventResult(), "result is explicitly closed after the committed choice");
            Assert.AreEqual(1, sim.ZhouCool);
            Assert.IsFalse(sim.ZhouFavourOwed);
            Assert.IsFalse(sim.S.wiring.pulled.Any());
            Assert.IsFalse(sim.LifeEventsEligible(new Host(1000000)).Contains("zhou_loan"), "a cooled 老周 does not ask for money");
        }

        [Test]
        public void TheFavourBringsTheLongestDownloadAtOnce()
        {
            var sim = Quiet(5); var host = new Host(1000000);
            foreach (var id in new[] { "mlp", "rnn", "lenet", "resnet" }) sim.S.unlocked.Add(id);
            Assert.IsTrue(sim.BuyNode("imagenet.pack", host));
            Assert.IsTrue(sim.Downloading("imagenet"));
            Assert.IsFalse(sim.LifeEventsEligible(host).Contains("zhou_favour"), "no favour owed yet");
            sim.S.lifeFavour = 1;
            Assert.IsTrue(sim.OpenLifeEventNow("zhou_favour", host));
            Assert.IsTrue(sim.AnswerLifeEvent("redeem", host));
            Assert.IsTrue(sim.AcknowledgeLifeEventResult(), "result is explicitly closed after the committed choice");
            Assert.IsFalse(sim.Downloading("imagenet"));
            Assert.IsFalse(sim.Owns("imagenet"), "a copied file is downloaded, not installed");
            Assert.AreEqual(XgSim.PackBatches("imagenet"), sim.BatchesArrived("imagenet"), "the purchased teaching file contains the full dataset");
            var copied = sim.S.dataFiles.Single(f => f.dataset == "imagenet" && sim.DataFileReady(f.fileId));
            Assert.IsTrue(sim.BuyNode(XgSim.DataEntryNode, host), "data import is purchased through the real gate");
            double afterPurchase = host.money;
            Assert.IsTrue(sim.InstallDataFile(copied.fileId), "copied file is installed explicitly");
            Assert.IsTrue(sim.Owns("imagenet"));
            Assert.AreEqual(afterPurchase, host.money, 1e-6, "installation never charges the file again");
            Assert.IsFalse(sim.InstallDataFile(copied.fileId), "repeat installation is rejected");
            Assert.IsFalse(sim.ZhouFavourOwed, "used up");
            Assert.IsFalse(sim.RedeemZhouFavour(), "only once");
        }

        [Test]
        public void KeepingTheFavourLeavesTheDownloadAlone()
        {
            var sim = Quiet(5); var host = new Host(1000000);
            foreach (var id in new[] { "mlp", "rnn", "lenet", "resnet" }) sim.S.unlocked.Add(id);
            Assert.IsTrue(sim.BuyNode("imagenet.pack", host));
            sim.S.lifeFavour = 1;
            Assert.IsTrue(sim.OpenLifeEventNow("zhou_favour", host));
            Assert.IsTrue(sim.AnswerLifeEvent("later", host));
            Assert.IsTrue(sim.AcknowledgeLifeEventResult(), "result is explicitly closed after the committed choice");
            Assert.IsTrue(sim.Downloading("imagenet") && sim.ZhouFavourOwed);
        }

        static XgSim DiskLab(out Host host, double money)
        {
            var sim = Quiet(); host = new Host(money);
            sim.S.owned.Add("imagenet");
            // This fixture represents pre-download installed data, so use the real migration before testing loss.
            sim.S.dataFilesVersion = 0;
            sim = new XgSim(sim.S);
            sim.LifeEventHold = () => true;
            Assert.AreEqual(sim.BatchesArrived("imagenet"), sim.InstalledBatches("imagenet"), "legacy fixture preserves already installed batches");
            return sim;
        }

        [Test]
        public void BadSectorsLoseABatchOrCostABackup()
        {
            var sim = DiskLab(out var host, 100000);
            int batches = sim.BatchesArrived("imagenet");
            double samples = sim.PackSamples("imagenet");
            Assert.That(batches, Is.GreaterThanOrEqualTo(2));
            Assert.IsTrue(sim.OpenLifeEventNow("bad_sectors", host, force: true));
            Assert.IsTrue(sim.AnswerLifeEvent("lose", host));
            Assert.IsTrue(sim.AcknowledgeLifeEventResult(), "result is explicitly closed after the committed choice");
            Assert.AreEqual(batches - 1, sim.BatchesArrived("imagenet"));
            Assert.AreEqual(batches - 1, sim.BatchesBought("imagenet"), "the batch can be bought again");
            Assert.AreEqual(samples - XgSim.BatchSamples(XgCatalog.Dataset("imagenet")), sim.PackSamples("imagenet"), 1e-6);
            Assert.AreEqual(100000, host.money, 1e-6);

            var paid = DiskLab(out var payer, 100000);
            Assert.IsTrue(paid.OpenLifeEventNow("bad_sectors", payer, force: true));
            double price = paid.S.lifeArg;
            Assert.That(price, Is.GreaterThan(0));
            Assert.IsTrue(paid.AnswerLifeEvent("backup", payer));
            Assert.IsTrue(paid.AcknowledgeLifeEventResult(), "result is explicitly closed after the committed choice");
            Assert.AreEqual(100000 - price, payer.money, 1e-6);
            Assert.AreEqual(batches, paid.BatchesArrived("imagenet"));

            var poor = DiskLab(out var broke, 10);
            Assert.IsTrue(poor.OpenLifeEventNow("bad_sectors", broke, force: true));
            Assert.IsFalse(poor.LifeEventView(broke).choices[0].enabled, "a backup needs the money");
            Assert.IsFalse(poor.AnswerLifeEvent("backup", broke));
            Assert.IsTrue(poor.LifeEventOpen);
        }

        [Test]
        public void BadSectorsNeedAFullDisk()
        {
            var sim = DiskLab(out var host, 100);
            Assert.That(sim.DiskUsedMB / sim.DiskCapacityMB, Is.LessThan(XgSim.LifeDiskShare));
            Assert.IsFalse(sim.LifeEventsEligible(host).Contains("bad_sectors"));
            foreach (var id in new[] { "voc", "coco", "news", "cifar10", "imdb" }) if (XgCatalog.Dataset(id) != null) sim.S.owned.Add(id);
            sim.S.diskBonusMB = 0;
            // Room taken by other data until the disk is over 80% full.
            while (sim.DiskUsedMB / sim.DiskCapacityMB < XgSim.LifeDiskShare) sim.S.owned.Add("imagenet");
            Assert.IsTrue(sim.LifeEventsEligible(host).Contains("bad_sectors"));
        }

        [Test]
        public void BroadbandRenewsForThreeMonthsOrDropsATier()
        {
            var sim = Quiet(); var host = new Host(10000);
            sim.Today = 20160710;
            sim.S.broadband = 1; sim.S.broadbandBilled = 201607;
            sim.Tick(1, host);
            Assert.IsFalse(sim.LifeEventsEligible(host).Contains("broadband"), "not in the month it was installed");
            sim.Today = 20160805; sim.Tick(1, host);
            Assert.IsTrue(sim.LifeEventsEligible(host).Contains("broadband"));
            double before = host.money;
            Assert.IsTrue(sim.OpenLifeEventNow("broadband", host));
            Assert.IsTrue(sim.AnswerLifeEvent("renew", host));
            Assert.IsTrue(sim.AcknowledgeLifeEventResult(), "result is explicitly closed after the committed choice");
            Assert.AreEqual(before - 2 * (XgSim.BroadbandMonthly[1] - XgSim.BroadbandMonthly[0]), host.money, 1e-6);
            Assert.AreEqual(201611, sim.S.broadbandBilled, "August is paid; September to November are covered");
            Assert.IsFalse(sim.LifeEventsEligible(host).Contains("broadband"), "once a month");
            sim.Today = 20160905; sim.Tick(1, host);
            Assert.IsFalse(sim.LifeEventsEligible(host).Contains("broadband"), "no reminder while the renewal covers the month");
            sim.Today = 20161105; double mid = host.spent; sim.Tick(1, host);
            // Newly eligible unseen nudges precede a recurring reminder. Their free choices preserve the billing assertions.
            for (int pending = 0; pending < XgSim.LifeEventIds.Length && !sim.LifeEventsEligible(host).Contains("broadband"); pending++)
            {
                var pool = sim.LifeEventsEligible(host);
                string other = pool.Find(id => id == "zhou_loan" || id == "licai_call" || id == "forum_post");
                Assert.IsNotNull(other, "an unseen nudge explains why the repeat reminder is deferred");
                Assert.IsFalse(sim.S.lifeDataSeen.Contains(other), "new events take priority over repeat reminders");
                Assert.IsTrue(sim.OpenLifeEventNow(other, host));
                Assert.IsTrue(sim.AnswerLifeEvent(other == "zhou_loan" ? "decline" : other == "licai_call" ? "hangup" : "tip", host));
                Assert.IsTrue(sim.AcknowledgeLifeEventResult());
            }
            Assert.IsTrue(sim.LifeEventsEligible(host).Contains("broadband"), "the last covered month reminder is eligible after unseen nudges");
            Assert.AreEqual(mid, host.spent, 1e-6, "November is covered");
            sim.Today = 20161205; sim.Tick(1, host);
            Assert.AreEqual(mid + XgSim.BroadbandMonthly[1] - XgSim.BroadbandMonthly[0], host.spent, 1e-6, "December is billed again");

            var drop = Quiet(); var h2 = new Host(10000);
            drop.Today = 20160805; drop.S.broadband = 2; drop.S.lifeBroadbandMonth = 201607;
            Assert.IsTrue(drop.OpenLifeEventNow("broadband", h2));
            Assert.IsTrue(drop.AnswerLifeEvent("drop", h2));
            Assert.IsTrue(drop.AcknowledgeLifeEventResult(), "result is explicitly closed after the committed choice");
            Assert.AreEqual(1, drop.BroadbandTier);
        }

        [Test]
        public void TheLandlordComesAfterTwoHighMonths()
        {
            var sim = Quiet(); var host = new Host(10000); var house = new House();
            sim.Household = house;
            foreach (int date in new[] { 20160710, 20160810, 20160910 })
            {
                sim.Today = date; house.MonthKwh = 0; sim.Tick(1, host);
                house.MonthKwh = 500; sim.Tick(1, host);
                if (date == 20160810) Assert.IsFalse(sim.LifeEventsEligible(host).Contains("landlord_meter"), "one high month is not enough");
            }
            Assert.AreEqual(2, sim.S.lifeHighMonths);
            Assert.IsTrue(sim.LifeEventsEligible(host).Contains("landlord_meter"));
            Assert.IsTrue(sim.OpenLifeEventNow("landlord_meter", host));
            double before = host.spent;
            Assert.IsTrue(sim.AnswerLifeEvent("pay", host));
            Assert.IsTrue(sim.AcknowledgeLifeEventResult(), "result is explicitly closed after the committed choice");
            Assert.AreEqual(before + XgSim.LifeLandlordExtra, host.spent, 1e-6);
            Assert.AreEqual(0, sim.S.lifeHighMonths);
            Assert.IsFalse(sim.LifeEventsEligible(host).Contains("landlord_meter"));

            // Explaining: either he believes it or he takes the wiring fee; he is wary afterwards either way.
            int believed = 0, fined = 0;
            for (long seed = 1; seed <= 30; seed++)
            {
                var s = Quiet(); var h = new Host(10000);
                s.S.rng = seed;
                s.S.lifeHighMonths = 2;
                Assert.IsTrue(s.OpenLifeEventNow("landlord_meter", h, force: true));
                Assert.IsTrue(s.AnswerLifeEvent("explain", h));
                Assert.IsTrue(s.AcknowledgeLifeEventResult(), "result is explicitly closed after the committed choice");
                Assert.IsTrue(s.S.lifeLandlordWary);
                if (h.money == 10000) believed++; else { fined++; Assert.AreEqual(10000 - XgSim.LifeLandlordFine, h.money, 1e-6); }
            }
            Assert.That(believed, Is.GreaterThan(0)); Assert.That(fined, Is.GreaterThan(0));
        }

        [Test]
        public void PeopleRespectYouMoreOnceYouAreFurtherAlongAndNeverBefore()
        {
            // The forum calls him 大神 only from the fourth ability on, and only then does the card come.
            var early = Quiet(2); var he = new Host(100);
            early.S.best.Add(new XgBest { dataset = "spam", acc = .9 });
            Assert.IsTrue(early.OpenLifeEventNow("forum_post", he));
            StringAssert.DoesNotContain("大神", early.LifeEventView(he).body);
            Assert.IsTrue(early.AnswerLifeEvent("tip", he));
            Assert.IsTrue(early.AcknowledgeLifeEventResult(), "result is explicitly closed after the committed choice");
            Assert.IsFalse(early.HasAchievement("life.praise.forum"));
            var late = Quiet(4); var hl = new Host(100);
            late.S.best.Add(new XgBest { dataset = "spam", acc = .9 });
            Assert.IsTrue(late.OpenLifeEventNow("forum_post", hl));
            StringAssert.DoesNotContain("大神", late.LifeEventView(hl).body, "the neutral invitation does not praise a post before it exists");
            StringAssert.Contains("分享自己的训练心得", late.LifeEventView(hl).body);
            Assert.IsTrue(late.AnswerLifeEvent("tip", hl));
            Assert.IsTrue(late.AcknowledgeLifeEventResult(), "result is explicitly closed after the committed choice");
            Assert.IsTrue(late.HasAchievement("life.praise.forum"));
            StringAssert.Contains("大神", late.S.log.Last());

            // The landlord who believes the explanation says so in kind once the tenant has got somewhere.
            int nods = 0;
            for (long seed = 1; seed <= 30; seed++)
            {
                var s = Quiet(5); var h = new Host(10000);
                s.S.rng = seed;
                Assert.IsTrue(s.OpenLifeEventNow("landlord_meter", h, force: true));
                Assert.IsTrue(s.AnswerLifeEvent("explain", h));
                Assert.IsTrue(s.AcknowledgeLifeEventResult(), "result is explicitly closed after the committed choice");
                if (h.money == 10000) { nods++; Assert.IsTrue(s.HasAchievement("life.praise.landlord")); StringAssert.Contains("有出息", s.S.log.Last()); }
                else Assert.IsFalse(s.HasAchievement("life.praise.landlord"));
            }
            Assert.That(nods, Is.GreaterThan(0));
            var young = Quiet(2); var hy = new Host(10000);
            Assert.IsTrue(young.OpenLifeEventNow("landlord_meter", hy, force: true));
            Assert.IsTrue(young.AnswerLifeEvent("explain", hy));
            Assert.IsTrue(young.AcknowledgeLifeEventResult(), "result is explicitly closed after the committed choice");
            Assert.IsFalse(young.HasAchievement("life.praise.landlord"));
        }

        [Test]
        public void TheRoommateGetsQuietOrRedBull()
        {
            var sim = Quiet(); var host = new RigHost(1000, 2);
            sim.Household = new House { SecondsOfDay = 2 * 3600 };
            sim.S.lifeLastTrain = sim.S.lifeClock;
            Assert.IsTrue(sim.OpenLifeEventNow("roommate_fan", host));
            Assert.IsTrue(sim.AnswerLifeEvent("throttle", host));
            Assert.IsTrue(sim.AcknowledgeLifeEventResult(), "result is explicitly closed after the committed choice");
            Assert.IsTrue(sim.S.wiring.pulled.Contains("h:gtx970#1"));
            sim.Tick(XgSim.LifeQuietSeconds + 2, host);
            Assert.IsFalse(sim.S.wiring.pulled.Contains("h:gtx970#1"), "back on after the quiet spell");

            var bull = Quiet(); var h2 = new RigHost(1000, 1);
            Assert.IsTrue(bull.OpenLifeEventNow("roommate_fan", h2, force: true));
            Assert.IsTrue(bull.AnswerLifeEvent("redbull", h2));
            Assert.IsTrue(bull.AcknowledgeLifeEventResult(), "result is explicitly closed after the committed choice");
            Assert.AreEqual(1000 - XgSim.LifeRedBull, h2.money, 1e-6);
            Assert.IsFalse(bull.S.wiring.pulled.Any());
        }

        [Test]
        public void APlayersOwnUnplugIsLeftAlone()
        {
            var sim = Quiet(); var host = new RigHost(1000, 2);
            sim.S.wiring.pulled.Add("h:gtx970#1");
            Assert.IsTrue(sim.OpenLifeEventNow("roommate_fan", host, force: true));
            Assert.IsTrue(sim.AnswerLifeEvent("throttle", host));
            Assert.IsTrue(sim.AcknowledgeLifeEventResult(), "result is explicitly closed after the committed choice");
            sim.Tick(XgSim.LifeQuietSeconds + 2, host);
            Assert.IsTrue(sim.S.wiring.pulled.Contains("h:gtx970#1"), "it was off before the roommate came");
        }

        [Test]
        public void TheForumGivesSamplesOrATip()
        {
            var sim = Quiet(); var host = new Host(100);
            sim.S.best.Add(new XgBest { dataset = "spam", acc = .9 });
            string ds = sim.Selected.dataset;
            double before = sim.Labels(ds);
            Assert.IsTrue(sim.OpenLifeEventNow("forum_post", host));
            double n = sim.S.lifeArg;
            Assert.That(n, Is.GreaterThanOrEqualTo(200));
            Assert.IsTrue(sim.AnswerLifeEvent("data", host));
            Assert.IsTrue(sim.AcknowledgeLifeEventResult(), "result is explicitly closed after the committed choice");
            Assert.AreEqual(before + n, sim.Labels(ds), 1e-6);

            var tip = Quiet(); var h2 = new Host(100);
            tip.S.best.Add(new XgBest { dataset = "spam", acc = .9 });
            Assert.IsTrue(tip.OpenLifeEventNow("forum_post", h2));
            Assert.IsTrue(tip.AnswerLifeEvent("tip", h2));
            Assert.IsTrue(tip.AcknowledgeLifeEventResult(), "result is explicitly closed after the committed choice");
            Assert.That(h2.money, Is.GreaterThan(100));
        }

        [Test]
        public void LentMoneyComesBack()
        {
            var sim = Quiet(); var host = new Host(1000000);
            Assert.IsTrue(sim.OpenLifeEventNow("zhou_loan", host));
            double amount = sim.S.lifeArg;
            Assert.That(amount, Is.InRange(100, 5000));
            Assert.IsTrue(sim.AnswerLifeEvent("lend", host));
            Assert.IsTrue(sim.AcknowledgeLifeEventResult(), "result is explicitly closed after the committed choice");
            Assert.AreEqual(1000000 - amount, host.money, 1e-6);
            Assert.IsFalse(host.earned.Contains(amount));
            sim.Tick(XgSim.LifeReturnSeconds + 2, host);
            Assert.IsTrue(host.earned.Contains(amount), "老周 pays it all back");

            var no = Quiet(); var h2 = new Host(1000000);
            Assert.IsTrue(no.OpenLifeEventNow("zhou_loan", h2));
            Assert.IsTrue(no.AnswerLifeEvent("decline", h2));
            Assert.IsTrue(no.AcknowledgeLifeEventResult(), "result is explicitly closed after the committed choice");
            Assert.AreEqual(0, h2.spent, 1e-6);
            Assert.AreEqual(0, no.ZhouCool, "saying no to a loan is fine");
        }

        [Test]
        public void YuebaoPaysALittleAndHangingUpCostsNothing()
        {
            var sim = Quiet(); var host = new Host(1000000);
            Assert.IsTrue(sim.OpenLifeEventNow("licai_call", host));
            double amount = sim.S.lifeArg;
            Assert.IsTrue(sim.AnswerLifeEvent("yuebao", host));
            Assert.IsTrue(sim.AcknowledgeLifeEventResult(), "result is explicitly closed after the committed choice");
            Assert.AreEqual(1000000 - amount, host.money, 1e-6);
            sim.Tick(XgSim.LifeReturnSeconds + 2, host);
            Assert.IsTrue(host.earned.Any(e => e > amount && e < amount * 1.02), "back with a little interest");

            var hang = Quiet(); var h2 = new Host(1000000);
            Assert.IsTrue(hang.OpenLifeEventNow("licai_call", h2));
            Assert.IsTrue(hang.AnswerLifeEvent("hangup", h2));
            Assert.IsTrue(hang.AcknowledgeLifeEventResult(), "result is explicitly closed after the committed choice");
            Assert.AreEqual(0, h2.spent, 1e-6);
            Assert.IsFalse(hang.LifeEventsEligible(h2).Contains("licai_call"), "the nudge rests a while");
        }

        [Test]
        public void EveryEventHasAFreeChoiceAndBilingualText()
        {
            foreach (var english in new[] { false, true })
                foreach (var id in XgSim.LifeEventIds)
                {
                    var sim = Quiet(5); var host = new RigHost(0, 2);
                    sim.English = english;
                    sim.S.owned.Add("imagenet");
                    Assert.IsTrue(sim.OpenLifeEventNow(id, host, force: true), id);
                    var v = sim.LifeEventView(host);
                    Assert.That(v.choices.Count, Is.EqualTo(2), id);
                    Assert.IsTrue(v.choices.Any(c => c.enabled), id + " can always be answered");
                    foreach (var text in new[] { v.who, v.title, v.body }.Concat(v.choices.Select(c => c.label + c.detail)))
                    {
                        Assert.IsFalse(string.IsNullOrEmpty(text), id);
                        Assert.IsFalse(text.Contains("涌现"), id);
                        Assert.IsFalse(text.Contains("✗"), id);
                    }
                    Assert.IsTrue(sim.AnswerLifeEvent(v.choices.FindIndex(c => c.enabled), host), id);
                    Assert.IsTrue(sim.AcknowledgeLifeEventResult(), "result is explicitly closed after the committed choice");
                    Assert.IsFalse(sim.LifeEventOpen);
                    Assert.IsFalse(sim.S.log.Last().Contains("涌现"));
                }
        }
    }
}
