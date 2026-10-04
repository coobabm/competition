using System;
using System.Collections.Generic;
using LingGuangV05.Core.Era;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>新题型 (monthly meme drift) and 甲方高价单 (SLA contracts).</summary>
    public sealed class XgMarketTests
    {
        sealed class Host : IXgHost
        {
            public double money = 100000, compute;
            public double Compute => compute;
            public double VramMB => 16000;
            public double Money => money;
            public bool Spend(double amount) { if (amount < 0 || money < amount) return false; money -= amount; return true; }
            public void Earn(double amount) { money += amount; }
            public void Train(double seconds) { }
            public string Blocker => null;
        }

        static XgSim Desks(params string[] open)
        {
            var sim = new XgSim { GoldChance = 0 };
            foreach (var d in open) if (!sim.S.desksOpen.Contains(d)) sim.S.desksOpen.Add(d);
            return sim;
        }

        // ───────────── 新题型 ─────────────

        [Test]
        public void ALoadedSaveDoesNotDriftBeforeTheDesktopRestoresItsDate()
        {
            var host = new MarketHostForLoad();
            var old = new XgSim { GoldChance = 0 };
            old.S.stage = 3; old.S.epochs = 40; old.S.totalIncome = 500;
            old.S.desksOpen.Add("danmu"); old.S.desksOpen.Add("spam");
            old.S.mkVersion = 0; // a save from before the market existed
            var loaded = new XgSim(old.S) { GoldChance = 0 };
            Assert.IsFalse(loaded.TodayKnown);
            loaded.Tick(1, host); // the catch-up tick before the desktop sets the date
            loaded.Today = 20160920; loaded.Tick(1, host);
            Assert.AreEqual(0, loaded.MemeDriftPenalty("danmu"), 1e-9);
            Assert.AreEqual(0, loaded.MemeDriftPenalty("spam"), 1e-9);
            loaded.Today = 20161020; loaded.Tick(1, host);
            Assert.Greater(loaded.MemeDriftPenalty("danmu"), 0, "the next real month still drifts");
        }

        sealed class MarketHostForLoad : IXgHost
        {
            public double money = 1e5;
            public double Compute => 1; public double VramMB => 16000; public double Money => money; public string Blocker => null;
            public bool Spend(double a) { if (money < a) return false; money -= a; return true; }
            public void Earn(double a) { money += a; }
            public void Train(double s) { }
        }

        [Test]
        public void EveryMonthMemeIsA2016WordDatedInItsMonth()
        {
            int month = 6;
            foreach (var m in XgSim.MonthMemes)
            {
                int since = XgSim.MemeSince(m.meme);
                Assert.AreEqual(2016, since / 10000, m.meme);
                Assert.AreEqual(month++, since / 100 % 100, m.meme);
                Assert.IsNull(EraLexicon.FirstBanned(m.meme), m.meme);
            }
            Assert.AreEqual("洪荒之力", XgSim.MonthMemes[XgSim.LatestMonthMeme(20160815)].meme);
            Assert.AreEqual(-1, XgSim.LatestMonthMeme(20160601));
        }

        [Test]
        public void DriftTriggersOncePerMonthOnOpenMemeDesksOnly()
        {
            var sim = Desks("danmu", "headline", "mnist", "poems", "translate"); var host = new Host();
            var notices = new List<XgMemeDriftNotice>(); sim.MemeDrifted += n => notices.Add(n);
            sim.Today = 20160601; sim.Tick(1, host);
            Assert.AreEqual(0, notices.Count);
            sim.Today = 20160608; sim.Tick(1, host);
            Assert.AreEqual(1, notices.Count);
            Assert.AreEqual("为了部落", notices[0].meme);
            CollectionAssert.AreEquivalent(new[] { "danmu", "headline", "spam" }, notices[0].desks);
            foreach (var d in new[] { "mnist", "poems", "translate", "logic", "review", "meme" }) Assert.AreEqual(0, sim.MemeDrift(d), d);
            Assert.AreEqual(15, sim.MemeDrift("danmu"), 1e-9);
            sim.Today = 20160720; sim.Tick(5, host);
            Assert.AreEqual(1, notices.Count, "same month: no second drift");

            sim.S.desksOpen.Add("review");
            sim.Answer("danmu", sim.Card("danmu").truth, host);
            Assert.AreEqual(14.5, sim.MemeDrift("danmu"), 1e-9);
            sim.Today = 20160725; sim.Tick(1, host);
            Assert.AreEqual(2, notices.Count);
            Assert.AreEqual("葛优躺", notices[1].meme);
            Assert.AreEqual(15, sim.MemeDrift("danmu"), 1e-9, "a new meme resets, never stacks");
            Assert.AreEqual(15, sim.MemeDrift("review"), 1e-9, "a desk opened since gets this month's drift");
            StringAssert.Contains("葛优躺", sim.MemeDriftChip("danmu"));
            StringAssert.Contains("−15%", sim.MemeDriftChip("danmu"));
            Assert.AreEqual("", sim.MemeDriftChip("mnist"));
        }

        [Test]
        public void ChipReadsLikeTheDesignInBothLanguages()
        {
            var sim = Desks("danmu"); var host = new Host();
            sim.Today = 20160808; sim.Tick(1, host);
            Assert.AreEqual("新题型：洪荒之力 −15%", sim.MemeDriftChip("danmu"));
            sim.English = true;
            Assert.AreEqual("New card type: 洪荒之力 (primordial power) −15%", sim.MemeDriftChip("danmu"));
        }

        [Test]
        public void DriftLowersCheckpointAccuracyByFifteenPointsForAutoAndGhost()
        {
            var sim = Desks("danmu"); var host = new Host();
            sim.S.best.Add(new XgBest { dataset = "spam", acc = .9, arch = "rnn" });
            sim.S.best.Add(new XgBest { dataset = "danmu", acc = .9, arch = "rnn" });
            var spam = sim.CreateLabelCard("spam");
            var run = sim.Run(XgTrack.Sequence);
            double clean = sim.CardAccuracy(run, spam, .9);
            sim.Today = 20160808; sim.Tick(1, host);
            Assert.AreEqual(clean - .15, sim.CardAccuracy(run, spam, .9), 1e-9);
            Assert.AreEqual(.9, sim.CardAccuracy(run, new XgCard { dataset = "logic" }, .9), 1e-9, "other desks keep their accuracy");
            // The ghost and suggestion read the same per-card accuracy.
            Assert.IsTrue(sim.Suggestion("spam", out _, out double confidence));
            Assert.AreEqual(sim.CardAccuracy(run, sim.Card("spam"), .9), confidence, 1e-9);

            // Automatic judgments: the drifted desk is visibly worse over many cards.
            sim.S.autoLevel = 1;
            int right = 0, total = 0, rightClean = 0, totalClean = 0;
            for (int i = 0; i < 400; i++)
            {
                sim.S.prefetch.Clear();
                sim.FillJudgmentBuffer();
                foreach (var c in sim.S.prefetch)
                {
                    if (c.dataset == "spam") { total++; if (c.guess == c.truth) right++; }
                    if (c.dataset == "danmu") continue;
                }
            }
            sim.S.mkDrift.Clear();
            for (int i = 0; i < 400; i++)
            {
                sim.S.prefetch.Clear();
                sim.FillJudgmentBuffer();
                foreach (var c in sim.S.prefetch) if (c.dataset == "spam") { totalClean++; if (c.guess == c.truth) rightClean++; }
            }
            double drifted = (double)right / total, cleanRate = (double)rightClean / totalClean;
            Assert.That(cleanRate - drifted, Is.GreaterThan(.09), "drifted " + drifted + " clean " + cleanRate);
        }

        [Test]
        public void HandLabellingIsUnaffectedAndRecoversHalfAPoint()
        {
            var sim = Desks(); var host = new Host();
            sim.Today = 20160608; sim.Tick(1, host);
            Assert.AreEqual(15, sim.MemeDrift("spam"), 1e-9);
            double before = host.money;
            var card = sim.Card("spam");
            var r = sim.Answer("spam", card.truth, host);
            Assert.IsTrue(r.correct); Assert.That(host.money, Is.GreaterThan(before));
            Assert.AreEqual(14.5, sim.MemeDrift("spam"), 1e-9);
            sim.Answer("spam", !sim.Card("spam").truth, host);
            Assert.AreEqual(14.5, sim.MemeDrift("spam"), 1e-9, "a wrong label teaches nothing");
            sim.ChatLabel("spam", true, host);
            Assert.AreEqual(14, sim.MemeDrift("spam"), 1e-9, "a correct chat label is a hand label too");
        }

        [Test]
        public void EpochsRecoverOnePointAndDriftEndsAtZero()
        {
            var sim = Desks(); var host = new Host { compute = 1 };
            sim.S.labels.Add(new XgLabelCount { dataset = "spam", count = 400 });
            sim.S.sequence.dataset = "spam";
            sim.Today = 20160608; sim.Tick(1, host);
            var cleared = new List<string>(); sim.MemeDriftCleared += d => cleared.Add(d);
            Assert.IsNotNull(sim.TrainEpoch(XgTrack.Sequence, host));
            Assert.AreEqual(14, sim.MemeDrift("spam"), 1e-9);
            sim.S.mkDrift[0].points = 1.5;
            sim.Answer("spam", sim.Card("spam").truth, host);
            Assert.IsNotNull(sim.TrainEpoch(XgTrack.Sequence, host));
            Assert.AreEqual(0, sim.MemeDrift("spam"));
            Assert.AreEqual(0, sim.S.mkDrift.Count);
            CollectionAssert.AreEqual(new[] { "spam" }, cleared);
            Assert.AreEqual("", sim.MemeDriftChip("spam"));
        }

        [Test]
        public void DriftSavesAndRepairs()
        {
            var sim = Desks("danmu"); var host = new Host();
            sim.Today = 20160808; sim.Tick(1, host);
            sim.S.mkDrift.Add(new XgMemeDrift { dataset = "danmu", meme = "x", points = 3 });
            sim.S.mkDrift.Add(new XgMemeDrift { dataset = "mnist", meme = "x", points = 3 });
            sim.S.mkDrift.Add(new XgMemeDrift { dataset = "review", meme = null, points = 99 });
            sim.S.mkDrift.Add(new XgMemeDrift { dataset = "headline", meme = "x", points = double.NaN });
            sim.S.mkDrift.Add(null);
            var copy = new XgSim(sim.S);
            Assert.AreEqual(15, copy.MemeDrift("danmu"), 1e-9, "the first entry per desk wins");
            Assert.AreEqual(0, copy.MemeDrift("mnist"));
            Assert.AreEqual(15, copy.MemeDrift("review"), 1e-9);
            Assert.AreEqual("", copy.MemeDriftMeme("review"));
            Assert.AreEqual(0, copy.MemeDrift("headline"));
            Assert.AreEqual(201608, copy.S.mkDriftMonth);
            copy.Today = 20160820; copy.Tick(1, host);
            Assert.AreEqual(15, copy.MemeDrift("danmu"), 1e-9, "the same month does not fire again after a reload");
        }

        [Test]
        public void ASaveFromBeforeDriftTakesTodayAsItsBaseline()
        {
            var state = new XgState { epochs = 50, totalIncome = 900 };
            state.desksOpen.Add("danmu");
            var sim = new XgSim(state) { GoldChance = 0 }; var host = new Host();
            Assert.AreEqual(-1, sim.S.mkDriftMonth);
            sim.Today = 20160815; sim.Tick(1, host);
            Assert.AreEqual(0, sim.MemeDrift("danmu"), "no drift on load");
            Assert.AreEqual(201608, sim.S.mkDriftMonth);
            sim.Today = 20160902; sim.Tick(1, host);
            Assert.AreEqual(15, sim.MemeDrift("danmu"), 1e-9, "the next month drifts as usual");
            Assert.AreEqual("非酋", sim.MemeDriftMeme("danmu"));
        }

        // ───────────── 甲方高价单 ─────────────

        static XgSim Market(double credit = 90, double acc = .95)
        {
            var sim = Desks("danmu");
            sim.S.autoLevel = 1;
            sim.S.qcCredit = credit;
            sim.S.best.Add(new XgBest { dataset = "danmu", acc = acc, arch = "rnn" });
            sim.S.best.Add(new XgBest { dataset = "spam", acc = acc, arch = "rnn" });
            return sim;
        }

        /// <summary>One automatic label on a desk, checked by the platform; never a trap item.</summary>
        static void Checked(XgSim sim, Host host, string desk, bool correct, bool truth)
        {
            double chance = sim.ForcedCheckChance;
            sim.ForcedCheckChance = 1;
            var c = sim.CreateLabelCard(desk);
            c.trapId = "";
            c.truth = truth;
            c.hasJudgment = true; c.guess = correct ? c.truth : !c.truth; c.confidence = .99; c.judgeSource = "checkpoint";
            sim.S.prefetch.Add(c);
            // Routing needs a powered GPU; the tests keep it off between labels so ticks never auto-label.
            double compute = host.compute;
            host.compute = 1;
            Assert.IsTrue(sim.Route(c.id, host));
            host.compute = compute;
            sim.ForcedCheckChance = chance;
        }

        [Test]
        public void OffersAreAboutTwiceANormalContractOnTheSameDesk()
        {
            foreach (var o in XgSim.SlaOffers)
            {
                var c = XgCatalog.Contract(o.baseContract);
                Assert.IsNotNull(c, o.id);
                Assert.AreEqual(c.dataset, o.dataset, o.id);
                Assert.AreEqual(2 * c.income, o.Income, 1e-9, o.id);
                Assert.That(o.duration, Is.InRange(480, 900), o.id);
                Assert.IsNull(EraLexicon.FirstBanned(o.client + o.job), o.id);
                Assert.IsFalse(string.IsNullOrEmpty(o.clientEn) || string.IsNullOrEmpty(o.jobEn), o.id);
            }
        }

        [Test]
        public void SigningNeedsCredit85AndACheckpointOverTheBar()
        {
            var sim = Market(credit: 84);
            Assert.IsFalse(sim.CanSignSla(XgSim.SlaOffer("sla.danmu"), out string why)); StringAssert.Contains("85", why);
            sim.S.qcCredit = 85;
            Assert.IsTrue(sim.CanSignSla(XgSim.SlaOffer("sla.danmu"), out _));
            sim.S.best.Find(b => b.dataset == "danmu").acc = .84;
            Assert.IsFalse(sim.CanSignSla(XgSim.SlaOffer("sla.danmu"), out _), "below the 85% bar of the danmaku contract");
            sim.S.best.Find(b => b.dataset == "danmu").acc = .9;
            Assert.IsFalse(sim.CanSignSla(XgSim.SlaOffer("sla.takeout"), out _), "review desk is not open");
            Assert.IsTrue(sim.SignSla("sla.danmu"));
            Assert.IsFalse(sim.SignSla("sla.bank"), "one at a time");
            var inactive = Desks("danmu"); inactive.S.qcCredit = 95; inactive.S.best.Add(new XgBest { dataset = "danmu", acc = .95 });
            Assert.IsFalse(inactive.CanSignSla(XgSim.SlaOffer("sla.danmu"), out _), "needs the platform (自动答题)");
            Assert.AreEqual(0, inactive.SlaOffersVisible().Count);
            var frozen = Market(); frozen.S.qcFrozen = 100;
            Assert.IsFalse(frozen.CanSignSla(XgSim.SlaOffer("sla.danmu"), out _));
        }

        [Test]
        public void HalfTheIncomeIsHeldAsTheBalance()
        {
            var sim = Market(); var host = new Host();
            Assert.IsTrue(sim.SignSla("sla.danmu"));
            double rate = sim.SlaIncome, before = host.money;
            Assert.That(rate, Is.GreaterThan(0));
            Assert.AreEqual(2 * XgCatalog.Contract("danmaku").income * (1 + (.95 - .85) / .15), rate, 1e-9);
            sim.Tick(10, host);
            Assert.AreEqual(rate * 10 * .5, host.money - before, 1e-6);
            Assert.AreEqual(rate * 10 * .5, sim.SlaHeld, 1e-6);
            Assert.AreEqual(480 - 10, sim.SlaSecondsLeft, 1e-6);
        }

        [Test]
        public void ChecksOnTheContractDeskCountIncludingOnlyThatDesk()
        {
            var sim = Market(); var host = new Host();
            sim.SignSla("sla.danmu");
            Checked(sim, host, "danmu", true, true);
            Checked(sim, host, "danmu", true, false);
            Checked(sim, host, "spam", false, true);
            Assert.AreEqual(2, sim.SlaChecks); Assert.AreEqual(2, sim.SlaPassed);
            Checked(sim, host, "danmu", false, true);
            Assert.AreEqual(3, sim.SlaChecks); Assert.AreEqual(2, sim.SlaPassed);
        }

        [Test]
        public void AReportCancelsForfeitsTheBalanceAndCostsFiveMoreCredit()
        {
            var sim = Market(credit: 90); var host = new Host();
            sim.SignSla("sla.danmu");
            sim.Tick(20, host);
            double held = sim.SlaHeld;
            Assert.That(held, Is.GreaterThan(0));
            XgSlaResult result = null; sim.SlaSettled += r => result = r;
            double creditBefore = 0;
            bool lastCorrect = true;
            // One failure in four on another desk: the platform reports the account at its next review.
            for (int i = 0; i < 400 && !sim.QualityFrozen; i++) { creditBefore = sim.Credit; lastCorrect = i % 4 != 0; Checked(sim, host, "spam", lastCorrect, i % 2 == 0); }
            Assert.IsTrue(sim.QualityFrozen);
            Assert.IsNotNull(result);
            Assert.AreEqual(XgSlaOutcome.Cancelled, result.outcome);
            Assert.AreEqual(held, result.forfeited, 1e-9);
            Assert.IsFalse(sim.SlaActive);
            Assert.AreEqual(0, sim.SlaHeld);
            double checkDelta = lastCorrect ? XgSim.QcCreditPass : -XgSim.QcCreditFail;
            Assert.AreEqual(creditBefore + checkDelta - XgSim.QcCreditReport - XgSim.SlaCancelCredit, sim.Credit, 1e-6);
            Assert.AreEqual(0, result.paid, "nothing of the balance was paid");
            Assert.That(sim.SlaCooldown("sla.danmu"), Is.GreaterThan(0));
        }

        XgSlaResult RunToEnd(XgSim sim, Host host)
        {
            XgSlaResult result = null; sim.SlaSettled += r => result = r;
            sim.Tick(XgSim.SlaOffer("sla.danmu").duration + 1, host);
            Assert.IsNotNull(result);
            Assert.IsFalse(sim.SlaActive);
            return result;
        }

        [Test]
        public void FiveStarsPayTheWholeBalance()
        {
            var sim = Market(); var host = new Host();
            sim.SignSla("sla.danmu");
            for (int i = 0; i < 20; i++) Checked(sim, host, "danmu", true, i % 2 == 0);
            sim.Tick(100, host);
            double held = sim.SlaHeld;
            double before = host.money;
            var r = RunToEnd(sim, host);
            Assert.AreEqual(XgSlaOutcome.FiveStar, r.outcome);
            Assert.AreEqual(0, r.forfeited, 1e-9);
            Assert.That(r.paid, Is.GreaterThan(held));
            Assert.AreEqual(1, sim.S.slaFiveStars);
            Assert.AreEqual(r.paid + r.earned * .5 - held, host.money - before, 1e-6, "held part of the remaining time is paid too");
        }

        [Test]
        public void BelowTheClauseOnlyHalfTheBalanceIsPaid()
        {
            var sim = Market(); var host = new Host();
            // Plenty of passing checks elsewhere keep the account far from a report.
            for (int i = 0; i < 30; i++) Checked(sim, host, "spam", true, i % 2 == 0);
            sim.SignSla("sla.danmu");
            for (int i = 0; i < 9; i++) Checked(sim, host, "danmu", true, i % 2 == 0);
            Checked(sim, host, "danmu", false, true);
            Assert.IsFalse(sim.QualityFrozen);
            Assert.AreEqual(.9, sim.SlaPassRate, 1e-9);
            var r = RunToEnd(sim, host);
            Assert.AreEqual(XgSlaOutcome.Docked, r.outcome);
            Assert.AreEqual(r.paid, r.forfeited, 1e-9);
            Assert.That(r.paid, Is.GreaterThan(0));
            Assert.AreEqual(1, sim.S.slaDocked);
        }

        [Test]
        public void FewerThanTenChecksPayInFull()
        {
            var sim = Market(); var host = new Host();
            sim.SignSla("sla.danmu");
            Checked(sim, host, "danmu", false, true);
            var r = RunToEnd(sim, host);
            Assert.AreEqual(XgSlaOutcome.Unjudged, r.outcome);
            Assert.AreEqual(0, r.forfeited, 1e-9);
            Assert.AreEqual(r.earned * .5, r.paid, 1e-6);
            Assert.IsFalse(sim.SignSla("sla.danmu"), "the client waits before the next order");
            sim.Tick(XgSim.SlaCooldownSeconds, host);
            Assert.IsTrue(sim.SignSla("sla.danmu"));
        }

        [Test]
        public void SlaSavesAndRepairs()
        {
            var sim = Market(); var host = new Host();
            sim.SignSla("sla.danmu"); sim.Tick(30, host);
            var copy = new XgSim(sim.S);
            Assert.IsTrue(copy.SlaActive);
            Assert.AreEqual(sim.SlaHeld, copy.SlaHeld, 1e-9);
            copy.S.slaLeft = 1e9; copy.S.slaHeld = double.NaN; copy.S.slaChecks = 3; copy.S.slaPassed = 9;
            copy.S.slaCooldowns.Add(new XgMarketTimer { id = "nope", seconds = 5 });
            copy.S.slaCooldowns.Add(new XgMarketTimer { id = "sla.bank", seconds = 1e9 });
            copy.S.slaCooldowns.Add(null);
            var fixedSim = new XgSim(copy.S);
            Assert.AreEqual(480, fixedSim.SlaSecondsLeft, 1e-9);
            Assert.AreEqual(0, fixedSim.SlaHeld);
            Assert.AreEqual(3, fixedSim.SlaPassed);
            Assert.AreEqual(1, fixedSim.S.slaCooldowns.Count);
            Assert.AreEqual(XgSim.SlaCooldownSeconds, fixedSim.SlaCooldown("sla.bank"), 1e-9);
            fixedSim.S.slaId = "sla.gone";
            var cleared = new XgSim(fixedSim.S);
            Assert.IsFalse(cleared.SlaActive);
            Assert.AreEqual(0, cleared.SlaHeld);
            // The reloaded sim still listens to the platform: a report cancels the reloaded contract.
            var again = Market(credit: 100); again.SignSla("sla.danmu");
            var reloaded = new XgSim(again.S);
            for (int i = 0; i < 400 && !reloaded.QualityFrozen; i++) Checked(reloaded, host, "spam", false, i % 2 == 0);
            Assert.IsTrue(reloaded.QualityFrozen);
            Assert.IsFalse(reloaded.SlaActive);
        }

        [Test]
        public void AuthoredMarketTextIs2016Clean()
        {
            var lines = new List<string>();
            foreach (var o in XgSim.SlaOffers) { lines.Add(o.client); lines.Add(o.job); }
            foreach (var m in XgSim.MonthMemes) lines.Add(m.meme);
            foreach (var d in XgSim.MemeDriftDesks) lines.Add(XgSim.DriftQueueName(d, false));
            Assert.IsEmpty(EraLexicon.Audit(lines));
        }
    }
}
