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
            sim.S.stage = XgSim.FeatureUnlock["quality"]; // the platform's checks belong to the crowd stage
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

        [Test]
        public void PlatformIsInactiveUntilAutoLabellingIsOwned()
        {
            var sim = new XgSim();
            Assert.IsFalse(sim.QualityActive); Assert.AreEqual(0, sim.SpotCheckChance); Assert.AreEqual(1, sim.QualityPayMultiplier);
            Assert.AreEqual(XgSim.QcCreditStart, sim.Credit);
            sim.S.autoLevel = 1;
            Assert.IsFalse(sim.QualityActive, "the checks wait for the crowd stage even once auto-labelling is owned");
            sim.S.stage = XgSim.FeatureUnlock["quality"];
            Assert.IsTrue(sim.QualityActive); Assert.AreEqual(.3, sim.SpotCheckChance, 1e-9);
        }

        [Test]
        public void CheckedWrongLabelIsFinedItsPayAndAddsNoNoise()
        {
            var sim = Ready(coop: true); var host = new Host(); sim.ForcedCheckChance = 1;
            XgQcFine fined = null; sim.QualityFined += f => fined = f;
            var c = Judged(sim, false);
            double money = host.money, pay = sim.PayFor("spam", c.level);
            Assert.AreEqual(1, XgSim.QcFineMultiplier, "stage-four fines were softened from twice the pay");
            Assert.IsTrue(sim.Route(c.id, host));
            Assert.IsNotNull(fined);
            Assert.AreEqual(XgSim.QcFineMultiplier * pay, fined.fine, 1e-9); Assert.AreEqual(pay, fined.pay, 1e-9); Assert.AreEqual("spam", fined.dataset);
            Assert.AreEqual(money - XgSim.QcFineMultiplier * pay, host.money, 1e-9);
            Assert.AreEqual(XgSim.QcFineMultiplier * pay, sim.S.qcFines, 1e-9); Assert.AreEqual(XgSim.QcFineMultiplier * pay, sim.S.totalSpent, 1e-9);
            Assert.AreEqual(XgSim.QcCreditStart - XgSim.QcCreditFail, sim.Credit, 1e-9);
            Assert.AreEqual(0, sim.NoiseTotal, "a label the platform rejected never enters the lab's data");
            var record = sim.S.autoFeed[sim.S.autoFeed.Count - 1];
            Assert.IsTrue(record.spotChecked); Assert.AreEqual(XgSim.QcFineMultiplier * pay, record.fine, 1e-9);

            sim.ForcedCheckChance = 0;
            Assert.IsTrue(Route(sim, host, false));
            Assert.AreEqual(1, sim.NoiseTotal, "unchecked wrong labels still become noise");
            Assert.AreEqual(money - XgSim.QcFineMultiplier * pay, host.money, 1e-9);
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
            Assert.AreEqual(XgSim.QcCreditStart, sim.Credit); Assert.AreEqual(money - sim.S.handFines, host.money, 1e-6, "only the hand fines"); Assert.AreEqual(0, sim.S.qcFines);
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
        public void TheWindowIsJudgedEveryTwentyChecksAndNeverReports()
        {
            var sim = Ready(); var host = new Host(); sim.ForcedCheckChance = 1;
            var warnings = new List<double>(); int reports = 0;
            sim.QualityWarned += warnings.Add; sim.QualityReported += (r, sec) => reports++;
            Checks(sim, host, 0, 19);
            Assert.AreEqual(0, warnings.Count, "nineteen checks are not judged");
            Checks(sim, host, 0, 1);
            Assert.AreEqual(1, warnings.Count, "twenty failures warn");
            Checks(sim, host, 0, 60);
            Assert.AreEqual(0, reports, "since 2026-10-16 the account is never reported");
            Assert.IsFalse(sim.QualityFrozen); Assert.AreEqual(0, sim.S.qcReports);
            Assert.AreEqual(XgSim.QcWindow, sim.SpotChecks, "the window keeps rolling");
            Assert.AreEqual(1, warnings.Count, "one warning per crossing");
        }

        [Test]
        public void WarningAtFifteenPercentNamesTheFineAndTwentyPercentIsNoReport()
        {
            var sim = Ready(); var host = new Host(); sim.ForcedCheckChance = 1;
            var warnings = new List<double>(); int reports = 0;
            sim.QualityWarned += warnings.Add; sim.QualityReported += (r, sec) => reports++;
            Checks(sim, host, 17, 3);
            Assert.AreEqual(1, warnings.Count, "3 in 20 = 15%: warned"); Assert.AreEqual(.15, warnings[0], 1e-9);
            Assert.IsTrue(sim.QualityWarning);
            string said = sim.S.log[sim.S.log.Count - 1];
            StringAssert.Contains("85%", said);
            StringAssert.Contains("罚 1 倍", said);
            StringAssert.DoesNotContain("举报", said);
            Checks(sim, host, 15, 5);
            Assert.AreEqual(.2, sim.ErrorRate, 1e-9);
            Assert.AreEqual(0, reports, "8 in 40 = 20%: still only fines");
            Assert.AreEqual(1, warnings.Count, "no second warning on the way");
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
        public void ARunOfMistakesOnlyCostsMoneyAndAutoLabellingKeepsGoing()
        {
            var sim = Ready(); var host = new Host(); sim.ForcedCheckChance = 1;
            double money = host.money, pay = sim.PayFor("spam", 1);
            for (int i = 0; i < 2 * XgSim.QcWindow; i++)
            {
                var c = Judged(sim, false, .99, i % 2 == 0);
                Assert.IsTrue(sim.Route(c.id, host), "the account keeps submitting");
            }
            Assert.IsFalse(sim.QualityFrozen); Assert.AreEqual(0, sim.FreezeSecondsLeft);
            Assert.AreEqual(2 * XgSim.QcWindow, sim.S.qcFailedTotal);
            Assert.Greater(sim.S.qcFines, 0); Assert.Less(host.money, money);
            Assert.Greater(sim.CollaborationRate("spam", host), 0);
            Assert.IsFalse(sim.Appeal(host), "nothing to appeal");
        }

        [Test]
        public void CreditDriftsBackToTheStartButNotAboveIt()
        {
            var sim = Ready();
            // No host: the platform clock runs but nothing is routed, so no spot check moves the credit.
            sim.S.qcCredit = 10;
            sim.TickCollaboration(15 * 10, null);
            Assert.AreEqual(20, sim.Credit, 1e-6, "one point every 15 seconds");
            Assert.AreEqual(XgCreditTier.Watch, sim.CreditTier);
            sim.TickCollaboration(15 * 100, null);
            Assert.AreEqual(XgSim.QcCreditStart, sim.Credit, 1e-6, "drift stops at the starting credit");
            sim.S.qcCredit = 95;
            sim.TickCollaboration(60, null);
            Assert.AreEqual(95, sim.Credit, 1e-9, "credit above the start is kept, not drifted down or up");
        }

        [Test]
        public void AdviceSaysBuyAskWhenUnsureFirstAndNamesTheWeakDesk()
        {
            var sim = Ready();
            sim.S.unlocked.Remove("label.coop");
            for (int i = 0; i < 10; i++) sim.S.routeHistory.Add(new XgRouteRecord { dataset = "cifar", automatic = true, correct = i < 4 });
            for (int i = 0; i < 10; i++) sim.S.routeHistory.Add(new XgRouteRecord { dataset = "spam", automatic = true, correct = true });
            for (int i = 0; i < 3; i++) sim.S.routeHistory.Add(new XgRouteRecord { dataset = "logic", automatic = true, correct = false });
            Assert.AreEqual("cifar", sim.WeakestAutoDesk(out double acc), "logic has too few labels to judge");
            Assert.AreEqual(.4, acc, 1e-9);
            string advice = sim.QualityAdvice(false);
            StringAssert.Contains("买「拿不准才问我」", advice);
            StringAssert.Contains("40%", advice);
            StringAssert.DoesNotContain("阈值", advice);
            sim.S.unlocked.Add("label.coop");
            StringAssert.Contains("阈值调高", sim.QualityAdvice(false));
            StringAssert.Contains("Ask when unsure", sim.QualityAdvice(true));
        }

        [Test]
        public void MistakesCostCreditPerCheckButNoReportPenalty()
        {
            var sim = Ready(); var host = new Host(); sim.ForcedCheckChance = 1;
            sim.S.qcCredit = 100;
            Checks(sim, host, 32, 8);
            Assert.IsFalse(sim.QualityFrozen);
            Assert.AreEqual(100 - 8 * XgSim.QcCreditFail, sim.Credit, 1e-9);
        }

        [Test]
        public void MonotoneAnswersAreNotReportedEither()
        {
            var sim = Ready(); var host = new Host(); sim.ForcedCheckChance = 1;
            var reports = new List<XgReportReason>(); sim.QualityReported += (r, s) => reports.Add(r);
            for (int i = 0; i < 34; i++) Assert.IsTrue(Route(sim, host, true, true));   // answers 是, right
            for (int i = 0; i < 5; i++) Assert.IsTrue(Route(sim, host, false, false));  // answers 是, wrong
            Assert.AreEqual(0, reports.Count);
            Assert.IsTrue(Route(sim, host, false, false));                              // 6 in 40 = 15%, all 是
            Assert.IsTrue(sim.MonotoneAnswers);
            Assert.AreEqual(0, reports.Count, "a script-like run is only warned and fined");
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
        public void ASaveFrozenUnderTheOldRulesIsFreeOnLoad()
        {
            var old = new XgState { qcVersion = 1, qcFrozen = 200, qcReports = 3, qcReason = (int)XgReportReason.LowPassRate, qcAppealed = true };
            var sim = new XgSim(old); var host = new Host();
            Assert.AreEqual(0, sim.S.qcFrozen); Assert.AreEqual(0, sim.S.qcReports); Assert.IsFalse(sim.S.qcAppealed);
            Assert.IsFalse(sim.QualityFrozen);
            Assert.IsFalse(sim.CanAppeal(host, out string why)); Assert.IsNotEmpty(why);
        }

        [Test]
        public void CreditTiersChangeCheckChanceAndPay()
        {
            Assert.AreEqual(XgCreditTier.Gold, XgSim.TierOfCredit(90)); Assert.AreEqual(XgCreditTier.Normal, XgSim.TierOfCredit(89.9));
            Assert.AreEqual(XgCreditTier.Normal, XgSim.TierOfCredit(60)); Assert.AreEqual(XgCreditTier.Watch, XgSim.TierOfCredit(59.9));
            var sim = Ready(); var host = new Host();
            foreach (var (credit, chance, mult) in new[] { (95.0, .15, 1.5), (75.0, .3, 1.0), (30.0, .5, .8) })
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
            Assert.AreEqual(normal.CollaborationIncome("spam", host) * 1.5, gold.CollaborationIncome("spam", host), 1e-9);
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
            Assert.AreEqual(0, fixedSim.S.qcFrozen, "no freeze survives a load");
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
        public void ANinetyPercentModelWithoutTheThresholdIsFinedButNeverReported()
        {
            int reports = 0; double fines = 0;
            for (int seed = 0; seed < 4; seed++)
            {
                var sim = Ready(coop: false, acc: .9); var host = new Host();
                sim.S.qcRng = 0x1234567L + seed * 7919L; sim.S.rng = 0x5EED5EEDL + seed * 104729L;
                for (int t = 0; t < 3600; t++) sim.TickCollaboration(1, host);
                reports += sim.S.qcReports; fines += sim.S.qcFines;
            }
            Assert.AreEqual(0, reports);
            Assert.Greater(fines, 0, "its one-in-ten mistakes are fined when caught");
        }

        [Test]
        public void ASeventyPercentModelWithoutTheThresholdPaysFinesAndKeepsWorking()
        {
            var sim = Ready(coop: false, acc: .7); var host = new Host();
            Assert.AreEqual(2000, Simulate(sim, host, 2000));
            Assert.AreEqual(0, sim.S.qcReports); Assert.IsFalse(sim.QualityFrozen);
            Assert.Greater(sim.S.qcFines, 0);
        }
    }
}
