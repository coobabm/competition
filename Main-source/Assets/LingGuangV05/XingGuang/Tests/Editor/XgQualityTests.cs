using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>摆渡众包 quality control: spot checks, fines, credit, warnings, reports, freezes and appeals.</summary>
    public sealed class XgQualityTests
    {
        sealed class Host : IXgHost
        {
            public double money = 100000;
            public int spendCalls;
            public double Compute => 1;
            public double VramMB => 16000;
            public double Money => money;
            public bool Spend(double amount) { spendCalls++; if (amount < 0 || money < amount) return false; money -= amount; return true; }
            public void Earn(double amount) { money += amount; }
            public void Train(double seconds) { }
            public string Blocker => null;
        }

        static XgSim Ready(bool coop = false, double acc = .8)
        {
            var sim = new XgSim { GoldChance = 0 };
            sim.S.desksOpen.Add("mnist");
            sim.S.autoLevel = 1;
            sim.S.best.Add(new XgBest { dataset = "mnist", acc = acc, arch = "perceptron" });
            sim.S.best.Add(new XgBest { dataset = "spam", acc = acc, arch = "perceptron" });
            if (coop) sim.S.unlocked.Add("label.coop");
            return sim;
        }

        /// <summary>A judged card in the prefetch buffer. truth null keeps the generated truth.</summary>
        static XgCard Judged(XgSim sim, bool correct, double confidence = .99, bool? truth = null, string desk = "spam")
        {
            var c = sim.CreateLabelCard(desk);
            if (truth.HasValue) c.truth = truth.Value;
            c.hasJudgment = true; c.guess = correct ? c.truth : !c.truth; c.confidence = confidence;
            c.judgeSource = "checkpoint";
            sim.S.prefetch.Add(c);
            return c;
        }

        static bool Route(XgSim sim, Host host, bool correct, bool? truth = null)
        {
            var c = Judged(sim, correct, .99, truth);
            return sim.Route(c.id, host);
        }

        /// <summary>A full window of failed checks: reported for a low pass rate.</summary>
        static void ReportOnce(XgSim sim, Host host)
        {
            double chance = sim.ForcedCheckChance;
            sim.ForcedCheckChance = 1;
            int before = sim.S.qcReports;
            for (int i = 0; i < XgSim.QcWindow && sim.S.qcReports == before; i++) Assert.IsTrue(Route(sim, host, false, i % 2 == 0));
            sim.ForcedCheckChance = chance;
            Assert.AreEqual(before + 1, sim.S.qcReports);
            Assert.IsTrue(sim.QualityFrozen);
        }

        [Test]
        public void PlatformIsInactiveUntilAutoLabellingIsOwned()
        {
            var sim = new XgSim();
            Assert.IsFalse(sim.QualityActive); Assert.AreEqual(0, sim.SpotCheckChance); Assert.AreEqual(1, sim.QualityPayMultiplier);
            Assert.AreEqual(XgSim.QcCreditStart, sim.Credit);
            sim.S.autoLevel = 1;
            Assert.IsTrue(sim.QualityActive); Assert.AreEqual(.4, sim.SpotCheckChance, 1e-9);
        }

        [Test]
        public void CheckedWrongLabelIsFinedTwiceItsPayAndAddsNoNoise()
        {
            var sim = Ready(coop: true); var host = new Host(); sim.ForcedCheckChance = 1;
            XgQcFine fined = null; sim.QualityFined += f => fined = f;
            var c = Judged(sim, false);
            double money = host.money, pay = sim.PayFor("spam", c.level);
            Assert.IsTrue(sim.Route(c.id, host));
            Assert.IsNotNull(fined);
            Assert.AreEqual(2 * pay, fined.fine, 1e-9); Assert.AreEqual(pay, fined.pay, 1e-9); Assert.AreEqual("spam", fined.dataset);
            Assert.AreEqual(money - 2 * pay, host.money, 1e-9);
            Assert.AreEqual(2 * pay, sim.S.qcFines, 1e-9); Assert.AreEqual(2 * pay, sim.S.totalSpent, 1e-9);
            Assert.AreEqual(XgSim.QcCreditStart - XgSim.QcCreditFail, sim.Credit, 1e-9);
            Assert.AreEqual(0, sim.NoiseTotal, "a label the platform rejected never enters the lab's data");
            var record = sim.S.autoFeed[sim.S.autoFeed.Count - 1];
            Assert.IsTrue(record.spotChecked); Assert.AreEqual(2 * pay, record.fine, 1e-9);

            sim.ForcedCheckChance = 0;
            Assert.IsTrue(Route(sim, host, false));
            Assert.AreEqual(1, sim.NoiseTotal, "unchecked wrong labels still become noise");
            Assert.AreEqual(money - 2 * pay, host.money, 1e-9);
        }

        [Test]
        public void FinesNeverTakeTheWalletBelowZero()
        {
            var sim = Ready(); var host = new Host { money = .3 }; sim.ForcedCheckChance = 1;
            Assert.Greater(2 * sim.PayFor("spam", 1), .3);
            Assert.IsTrue(Route(sim, host, false));
            Assert.AreEqual(0, host.money, 1e-12);
            Assert.AreEqual(.3, sim.S.qcFines, 1e-12);
            int spends = host.spendCalls;
            Assert.IsTrue(Route(sim, host, false));
            Assert.AreEqual(0, host.money); Assert.AreEqual(spends, host.spendCalls, "an empty wallet is not charged");
            Assert.AreEqual(.3, sim.S.qcFines, 1e-12);
        }

        [Test]
        public void CheckedCorrectLabelRaisesCreditUpToTheCap()
        {
            var sim = Ready(); var host = new Host(); sim.ForcedCheckChance = 1;
            Assert.IsTrue(Route(sim, host, true));
            Assert.AreEqual(XgSim.QcCreditStart + XgSim.QcCreditPass, sim.Credit, 1e-9);
            sim.S.qcCredit = 99.8;
            Assert.IsTrue(Route(sim, host, true));
            Assert.AreEqual(100, sim.Credit, 1e-9);
        }

        [Test]
        public void HandLabelsAreNeverChecked()
        {
            var sim = Ready(coop: true); var host = new Host(); sim.ForcedCheckChance = 1;
            double money = host.money;
            for (int i = 0; i < 30; i++) { var card = sim.Card("spam"); sim.Answer("spam", !card.truth, host); }
            var review = Judged(sim, false, .55); Assert.IsTrue(sim.Route(review.id, host));
            Assert.AreEqual(1, sim.ReviewCount());
            sim.AnswerQueued(review.id, !review.truth, host);
            Assert.AreEqual(0, sim.SpotChecks); Assert.AreEqual(0, sim.S.qcCheckedTotal);
            Assert.AreEqual(XgSim.QcCreditStart, sim.Credit); Assert.AreEqual(money, host.money); Assert.AreEqual(0, sim.S.qcFines);
        }

        [Test]
        public void OwnAuditInterceptionIsNotSpotChecked()
        {
            var sim = Ready(coop: true); var host = new Host(); sim.S.unlocked.Add("label.audit"); sim.ForcedCheckChance = 1;
            int intercepted = 0;
            for (int i = 0; i < 40 && intercepted == 0; i++)
            {
                var c = Judged(sim, false, .99, i % 2 == 0);
                int checks = sim.S.qcCheckedTotal;
                Assert.IsTrue(sim.Route(c.id, host));
                if (sim.ReviewCount() > 0) { intercepted++; Assert.AreEqual(checks, sim.S.qcCheckedTotal); sim.S.queue.Clear(); }
                if (sim.QualityFrozen) break;
            }
            Assert.AreEqual(1, intercepted);
        }

        /// <summary>Routes checked cards: right ones first, then wrong ones, truths alternating (no script pattern).</summary>
        static void Checks(XgSim sim, Host host, int right, int wrong)
        {
            for (int i = 0; i < right; i++) Assert.IsTrue(Route(sim, host, true, i % 2 == 0));
            for (int i = 0; i < wrong; i++) Assert.IsTrue(Route(sim, host, false, i % 2 == 1));
        }

        [Test]
        public void TheWindowIsJudgedEveryTwentyChecksAndReportsNeedAFullWindow()
        {
            var sim = Ready(); var host = new Host(); sim.ForcedCheckChance = 1;
            var warnings = new List<double>(); var reports = new List<(XgReportReason, double)>();
            sim.QualityWarned += warnings.Add; sim.QualityReported += (r, s) => reports.Add((r, s));
            Checks(sim, host, 0, 19);
            Assert.AreEqual(0, warnings.Count, "nineteen checks are not judged");
            Checks(sim, host, 0, 1);
            Assert.AreEqual(1, warnings.Count, "twenty failures warn");
            Assert.AreEqual(0, reports.Count, "but a short streak is not a report: the window is not full");
            Assert.IsFalse(sim.QualityFrozen);
            Checks(sim, host, 0, 19);
            Assert.AreEqual(0, reports.Count, "between judgements nothing happens");
            Checks(sim, host, 0, 1);
            Assert.AreEqual(1, reports.Count);
            Assert.AreEqual(XgReportReason.LowPassRate, reports[0].Item1); Assert.AreEqual(180, reports[0].Item2, 1e-9);
            Assert.AreEqual(1, sim.S.qcReports); Assert.AreEqual(0, sim.SpotChecks, "the check window clears");
            Assert.IsTrue(sim.QualityFrozen); Assert.AreEqual(180, sim.FreezeSecondsLeft, 1e-9);
        }

        [Test]
        public void WarningAtFifteenPercentThenReportAtTwentyPercent()
        {
            var sim = Ready(); var host = new Host(); sim.ForcedCheckChance = 1;
            var warnings = new List<double>(); int reports = 0;
            sim.QualityWarned += warnings.Add; sim.QualityReported += (r, s) => reports++;
            Checks(sim, host, 17, 3);
            Assert.AreEqual(1, warnings.Count, "3 in 20 = 15%: warned"); Assert.AreEqual(.15, warnings[0], 1e-9);
            Assert.IsTrue(sim.QualityWarning);
            StringAssert.Contains("85%", sim.S.log[sim.S.log.Count - 1]);
            StringAssert.Contains("80%", sim.S.log[sim.S.log.Count - 1]);
            Checks(sim, host, 15, 4);
            Assert.AreEqual(0, reports);
            Checks(sim, host, 0, 1);
            Assert.AreEqual(1, reports, "8 in 40 = 20%: reported");
            Assert.AreEqual(1, warnings.Count, "no second warning on the way");
            Assert.AreEqual(XgReportReason.LowPassRate, sim.LastReportReason);
        }

        [Test]
        public void NineteenPercentOverAFullWindowOnlyWarns()
        {
            var sim = Ready(); var host = new Host(); sim.ForcedCheckChance = 1;
            int warnings = 0, reports = 0; sim.QualityWarned += _ => warnings++; sim.QualityReported += (r, s) => reports++;
            Checks(sim, host, 20, 0);
            Checks(sim, host, 13, 7);
            Assert.AreEqual(.175, sim.ErrorRate, 1e-9);
            Assert.AreEqual(0, reports); Assert.AreEqual(1, warnings);
        }

        [Test]
        public void WarningReArmsAfterTheRateDropsBackUnderTheLine()
        {
            var sim = Ready(); var host = new Host(); sim.ForcedCheckChance = 1;
            int warnings = 0; sim.QualityWarned += _ => warnings++;
            Checks(sim, host, 17, 3);
            Assert.AreEqual(1, warnings);
            Checks(sim, host, 20, 0); // 3 in 40 = 7.5%: re-armed
            Assert.IsFalse(sim.S.qcWarned);
            Checks(sim, host, 14, 6); // the window drops the first twenty: 6 in 40 = 15%
            Assert.AreEqual(2, warnings);
            Assert.IsFalse(sim.QualityFrozen);
        }

        [Test]
        public void FreezeStopsRoutingAndCountsDownWhileHandLabellingStillWorks()
        {
            var sim = Ready(); var host = new Host();
            ReportOnce(sim, host);
            int unfrozen = 0; bool? appealed = null; sim.QualityUnfrozen += a => { unfrozen++; appealed = a; };
            Assert.AreEqual(0, sim.CollaborationRate("spam", host)); Assert.AreEqual(0, sim.CollaborationIncome("spam", host));
            var c = Judged(sim, true);
            Assert.IsFalse(sim.Route(c.id, host), "frozen accounts cannot submit");
            double answered = sim.S.autoCorrect + sim.S.autoWrong;
            for (int i = 0; i < 179; i++) sim.TickCollaboration(1, host);
            Assert.AreEqual(answered, sim.S.autoCorrect + sim.S.autoWrong, "no routing while frozen");
            Assert.IsTrue(sim.QualityFrozen); Assert.AreEqual(1, sim.FreezeSecondsLeft, 1e-6);
            Assert.AreEqual("0:01", XgSim.FreezeClock(sim.FreezeSecondsLeft));

            double credit = sim.Credit, money = host.money;
            var card = sim.Card("spam");
            Assert.IsTrue(sim.Answer("spam", card.truth, host).correct);
            Assert.Greater(host.money, money, "hand labels still pay");
            Assert.AreEqual(credit + XgSim.QcCreditHand, sim.Credit, 1e-9, "good hand labels earn trust back while frozen");

            sim.TickCollaboration(1, host);
            Assert.IsFalse(sim.QualityFrozen); Assert.AreEqual(1, unfrozen); Assert.AreEqual(false, appealed);
            Assert.Greater(sim.CollaborationRate("spam", host), 0);
            Assert.IsTrue(sim.Route(c.id, host));

            credit = sim.Credit; card = sim.Card("spam");
            sim.Answer("spam", card.truth, host);
            Assert.AreEqual(credit, sim.Credit, 1e-9, "the hand bonus only applies while frozen");
        }

        [Test]
        public void TickCountsTheFreezeDownToo()
        {
            var sim = Ready(); var host = new Host();
            ReportOnce(sim, host);
            sim.Tick(100, host);
            Assert.AreEqual(80, sim.FreezeSecondsLeft, 1e-6);
            sim.Tick(80, host);
            Assert.IsFalse(sim.QualityFrozen);
        }

        [Test]
        public void ReportsEscalateThreeEightTwentyMinutes()
        {
            var sim = Ready(); var host = new Host();
            var seconds = new List<double>(); sim.QualityReported += (r, s) => seconds.Add(s);
            for (int n = 0; n < 4; n++)
            {
                ReportOnce(sim, host);
                Assert.AreEqual(seconds[n], sim.FreezeSecondsLeft, 1e-9);
                sim.TickCollaboration(seconds[n] / 2, host); sim.TickCollaboration(seconds[n] / 2, host);
                for (int i = 0; i < 20 && sim.QualityFrozen; i++) sim.TickCollaboration(1, host);
                Assert.IsFalse(sim.QualityFrozen);
            }
            CollectionAssert.AreEqual(new[] { 180.0, 480, 1200, 1200 }, seconds);
            Assert.AreEqual(4, sim.S.qcReports);
            Assert.Less(sim.Credit, 5, "four reports and forty failed checks all but empty the credit");
            Assert.AreEqual(XgCreditTier.Watch, sim.CreditTier);
        }

        [Test]
        public void ReportCostsTwentyCredit()
        {
            var sim = Ready(); var host = new Host(); sim.ForcedCheckChance = 1;
            sim.S.qcCredit = 100;
            Checks(sim, host, 32, 8);
            Assert.IsTrue(sim.QualityFrozen);
            Assert.AreEqual(100 - 8 * XgSim.QcCreditFail - XgSim.QcCreditReport, sim.Credit, 1e-9);
        }

        [Test]
        public void MonotoneAnswersAreReportedAsAScriptAtTheWarningLine()
        {
            var sim = Ready(); var host = new Host(); sim.ForcedCheckChance = 1;
            var reports = new List<XgReportReason>(); sim.QualityReported += (r, s) => reports.Add(r);
            for (int i = 0; i < 34; i++) Assert.IsTrue(Route(sim, host, true, true));   // answers 是, right
            for (int i = 0; i < 5; i++) Assert.IsTrue(Route(sim, host, false, false));  // answers 是, wrong
            Assert.AreEqual(0, reports.Count);
            Assert.IsTrue(Route(sim, host, false, false));                              // 6 in 40 = 15%, all 是
            Assert.AreEqual(1, reports.Count);
            Assert.AreEqual(XgReportReason.MonotoneAnswers, reports[0]);
            Assert.AreEqual("答案高度雷同，疑似脚本", XgSim.ReportReasonText(XgReportReason.MonotoneAnswers, false));
            Assert.IsNotEmpty(XgSim.ReportReasonText(XgReportReason.MonotoneAnswers, true));
            Assert.IsNotEmpty(XgSim.ReportReasonText(XgReportReason.LowPassRate, true));
            Assert.AreEqual("疑似机器操作", XgSim.ReportReasonText(XgReportReason.SuspectedBot, false));
        }

        [Test]
        public void MixedAnswersAtTheSameErrorRateOnlyWarn()
        {
            var sim = Ready(); var host = new Host(); sim.ForcedCheckChance = 1;
            int warnings = 0, reports = 0; sim.QualityWarned += _ => warnings++; sim.QualityReported += (r, s) => reports++;
            Checks(sim, host, 34, 6);
            Assert.IsFalse(sim.MonotoneAnswers);
            Assert.AreEqual(0, reports); Assert.AreEqual(1, warnings);
        }

        [Test]
        public void AppealCostsAtLeastAHundredOrTenPercentAndWorksOncePerFreeze()
        {
            var sim = Ready(); var host = new Host { money = 500 };
            Assert.IsFalse(sim.Appeal(host), "nothing to appeal");
            ReportOnce(sim, host);
            host.money = 500;
            Assert.AreEqual(100, sim.AppealCost(host), 1e-9);
            host.money = 5000;
            Assert.AreEqual(500, sim.AppealCost(host), 1e-9);
            host.money = 50;
            Assert.IsFalse(sim.CanAppeal(host, out string why)); Assert.IsNotEmpty(why);
            Assert.IsFalse(sim.Appeal(host)); Assert.AreEqual(50, host.money); Assert.IsTrue(sim.QualityFrozen);

            host.money = 5000;
            bool? appealed = null; sim.QualityUnfrozen += a => appealed = a;
            double credit = sim.Credit, spent = sim.S.totalSpent;
            Assert.IsTrue(sim.CanAppeal(host, out _));
            Assert.IsTrue(sim.Appeal(host));
            Assert.AreEqual(4500, host.money, 1e-9); Assert.AreEqual(spent + 500, sim.S.totalSpent, 1e-9);
            Assert.IsFalse(sim.QualityFrozen); Assert.AreEqual(true, appealed); Assert.AreEqual(credit, sim.Credit, "credit unchanged");
            Assert.IsTrue(sim.S.qcAppealed);
            int spends = host.spendCalls;
            Assert.IsFalse(sim.Appeal(host)); Assert.AreEqual(4500, host.money); Assert.AreEqual(spends, host.spendCalls);

            ReportOnce(sim, host);
            Assert.IsFalse(sim.S.qcAppealed, "a new freeze can be appealed again");
            Assert.IsTrue(sim.Appeal(host));
        }

        [Test]
        public void CreditTiersChangeCheckChanceAndPay()
        {
            Assert.AreEqual(XgCreditTier.Gold, XgSim.TierOfCredit(90)); Assert.AreEqual(XgCreditTier.Normal, XgSim.TierOfCredit(89.9));
            Assert.AreEqual(XgCreditTier.Normal, XgSim.TierOfCredit(60)); Assert.AreEqual(XgCreditTier.Watch, XgSim.TierOfCredit(59.9));
            var sim = Ready(); var host = new Host();
            foreach (var (credit, chance, mult) in new[] { (95.0, .25, 1.2), (75.0, .4, 1.0), (30.0, .65, .8) })
            {
                sim.S.qcCredit = credit;
                Assert.AreEqual(chance, sim.SpotCheckChance, 1e-9); Assert.AreEqual(mult, sim.QualityPayMultiplier, 1e-9);
                var c = Judged(sim, true); double money = host.money;
                Assert.IsTrue(sim.Route(c.id, host));
                Assert.AreEqual(sim.PayFor("spam", c.level) * mult, host.money - money, 1e-9);
                int checkedCount = 0, n = 3000;
                for (int i = 0; i < n; i++)
                {
                    sim.S.qcCredit = credit;
                    Assert.IsTrue(Route(sim, host, true));
                    if (sim.S.autoFeed[sim.S.autoFeed.Count - 1].spotChecked) checkedCount++;
                }
                Assert.AreEqual(chance, (double)checkedCount / n, .03, "credit " + credit);
            }
            var normal = Ready(); var gold = Ready(); gold.S.qcCredit = 95;
            Assert.AreEqual(normal.CollaborationIncome("spam", host) * 1.2, gold.CollaborationIncome("spam", host), 1e-9);
        }

        [Test]
        public void LegacyAndCorruptSavesAreRepaired()
        {
            var legacy = new XgState { qcVersion = 0, qcCredit = 0, qcFrozen = double.NaN, qcFines = -5, qcReason = 99, qcChecks = null, qcAnswers = null, qcRng = 0, qcReports = -3 };
            var sim = new XgSim(legacy);
            Assert.AreEqual(XgSim.QcCreditStart, sim.Credit, "old saves start at 80");
            Assert.AreEqual(1, sim.S.qcVersion); Assert.AreEqual(0, sim.S.qcFrozen); Assert.AreEqual(0, sim.S.qcFines);
            Assert.AreEqual(XgReportReason.None, sim.LastReportReason); Assert.AreEqual(0, sim.S.qcReports);
            Assert.IsNotNull(sim.S.qcChecks); Assert.IsNotNull(sim.S.qcAnswers); Assert.AreNotEqual(0, sim.S.qcRng);

            var corrupt = new XgState { qcVersion = 1, qcCredit = double.PositiveInfinity, qcFrozen = 1e9 };
            for (int i = 0; i < 100; i++) { corrupt.qcChecks.Add(true); corrupt.qcAnswers.Add(false); }
            var fixedSim = new XgSim(corrupt);
            Assert.AreEqual(XgSim.QcCreditStart, fixedSim.Credit);
            Assert.AreEqual(1200, fixedSim.S.qcFrozen, 1e-9); Assert.AreEqual(XgReportReason.LowPassRate, fixedSim.LastReportReason);
            Assert.AreEqual(XgSim.QcWindow, fixedSim.S.qcChecks.Count); Assert.AreEqual(XgSim.QcWindow, fixedSim.S.qcAnswers.Count);

            var high = new XgSim(new XgState { qcVersion = 1, qcCredit = 250 });
            Assert.AreEqual(100, high.Credit);
            var kept = new XgSim(new XgState { qcVersion = 1, qcCredit = 42 });
            Assert.AreEqual(42, kept.Credit, "a repaired save keeps its credit");
            Assert.AreEqual(42, new XgSim(kept.S).Credit, "repair is idempotent");
        }

        /// <summary>Routes judged cards the way the game does (checkpoint estimates) until enough left the review path.</summary>
        static int Simulate(XgSim sim, Host host, int routes)
        {
            int routed = 0, guard = 0;
            while (routed < routes && guard++ < routes * 10)
            {
                sim.FillJudgmentBuffer();
                foreach (var card in sim.S.prefetch.ToArray())
                {
                    if (!card.hasJudgment) continue;
                    if (sim.QualityFrozen) sim.TickCollaboration(XgSim.QcFreezeSeconds[2], host);
                    if (sim.Route(card.id, host)) routed++;
                    if (sim.ReviewFull) sim.S.queue.Clear();
                    if (routed >= routes) break;
                }
            }
            return routed;
        }

        [Test]
        public void ANinetyPercentModelBehindTheConfidenceThresholdIsNotReported()
        {
            var sim = Ready(coop: true, acc: .9); var host = new Host();
            sim.SetCoopThreshold(.8);
            Assert.AreEqual(2000, Simulate(sim, host, 2000));
            Assert.AreEqual(0, sim.S.qcReports);
            Assert.Greater(sim.S.qcCheckedTotal, 100, "the platform did check it");
            Assert.GreaterOrEqual(sim.Credit, XgSim.QcCreditStart);
        }

        [Test]
        public void ANinetyPercentModelWithoutTheThresholdIsRarelyReported()
        {
            int reports = 0;
            for (int seed = 0; seed < 4; seed++)
            {
                var sim = Ready(coop: false, acc: .9); var host = new Host();
                sim.S.qcRng = 0x1234567L + seed * 7919L; sim.S.rng = 0x5EED5EEDL + seed * 104729L;
                for (int t = 0; t < 3600; t++) sim.TickCollaboration(1, host);
                reports += sim.S.qcReports;
            }
            Assert.LessOrEqual(reports, 6, "about one report an hour or fewer on average");
        }

        [Test]
        public void ASeventyPercentModelWithoutTheThresholdIsReported()
        {
            var sim = Ready(coop: false, acc: .7); var host = new Host();
            Simulate(sim, host, 2000);
            Assert.GreaterOrEqual(sim.S.qcReports, 1);
            Assert.Greater(sim.S.qcFines, 0);
        }
    }
}
