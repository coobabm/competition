using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>The one pay-and-sample rule of every desk game (XgSim.MiniPay.cs).</summary>
    public sealed class XgMiniPayTests
    {
        sealed class Host : IXgHost
        {
            public double Money { get; private set; } = 100000;
            public double Compute => 10;
            public double VramMB => 4096;
            public string Blocker { get; set; }
            public void Earn(double amount) { Money += amount; }
            public bool Spend(double amount) { if (amount < 0 || Money < amount) return false; Money -= amount; return true; }
            public void Train(double seconds) { }
        }

        static IEnumerable<XgDesk> MappedDesks()
        {
            foreach (var d in XgCatalog.Desks) if (XgMiniGames.ForDesk(d.id) != null) yield return d;
        }

        static XgSim Open(params string[] desks)
        {
            var s = new XgSim();
            foreach (var d in desks) if (!s.S.desksOpen.Contains(d)) s.S.desksOpen.Add(d);
            return s;
        }

        static XgSim OpenAll()
        {
            var s = new XgSim();
            foreach (var d in XgCatalog.Desks) if (!s.S.desksOpen.Contains(d.id)) s.S.desksOpen.Add(d.id);
            return s;
        }

        [TestCase("dadishu")]
        [TestCase("arith_mole")]
        public void AutomaticMoleSettlesNormallyWithoutConsumingPlayerReactions(string game)
        {
            var sim = OpenAll(); var host = new Host();
            long ticket = sim.BeginMiniGame(game, host);
            Assert.AreNotEqual(0, ticket);
            double money = host.Money, labels = sim.Labels("arith");
            int right = sim.S.handCorrect, wrong = sim.S.handWrong;
            var earned = sim.SubmitMiniGame(ticket, 1, true, host, 0, automatic: true);
            var fined = sim.SubmitMiniGame(ticket, 2, false, host, automatic: true);
            Assert.IsTrue(earned.accepted && fined.accepted);
            Assert.Greater(earned.pay, 0); Assert.Greater(fined.fine, 0);
            Assert.AreEqual(money + earned.pay - fined.fine, host.Money, 1e-8);
            Assert.AreEqual(labels + earned.samples, sim.Labels("arith"));
            Assert.AreEqual(right + 1, sim.S.handCorrect); Assert.AreEqual(wrong + 1, sim.S.handWrong);
            Assert.IsFalse(sim.S.answerPayThoughtRecorded); Assert.IsFalse(sim.S.answerFineThoughtRecorded);
            Assert.IsEmpty(sim.S.pendingAnswerMoneyThoughts);
            sim.SubmitMiniGame(ticket, 3, true, host);
            sim.SubmitMiniGame(ticket, 4, false, host);
            CollectionAssert.AreEqual(new[] { "pay", "fine" }, sim.S.pendingAnswerMoneyThoughts);
        }

        [Test] public void FirstMiniIncomeAndActualFineAreQueuedOnceAndSurviveReload()
        {
            var sim = OpenAll(); var host = new Host();
            long ticket = sim.BeginMiniGame("dadishu", host);
            double before = host.Money;
            var earned = sim.SubmitMiniGame(ticket, 1, true, host, 0);
            Assert.IsTrue(earned.accepted); Assert.Greater(earned.bonus, 0);
            Assert.AreEqual(earned.pay, host.Money - before, 1e-8, "Recorded income includes the actual speed bonus");
            Assert.IsTrue(sim.S.answerPayThoughtRecorded);
            var fined = sim.SubmitMiniGame(ticket, 2, false, host);
            Assert.IsTrue(fined.accepted); Assert.Greater(fined.fine, 0);
            Assert.IsTrue(sim.S.answerFineThoughtRecorded);
            var loaded = new XgSim(UnityEngine.JsonUtility.FromJson<XgState>(UnityEngine.JsonUtility.ToJson(sim.S)));
            Assert.AreEqual("我去，答题还能赚钱？", loaded.TakeTeachingThought(true).Value.zh);
            Assert.AreEqual("我去，答错还会扣钱？", loaded.TakeTeachingThought(true).Value.zh);
            loaded.SettleMiniAction("arith", true, host, 0);
            loaded.SettleMiniAction("arith", false, host);
            var reopened = new XgSim(UnityEngine.JsonUtility.FromJson<XgState>(UnityEngine.JsonUtility.ToJson(loaded.S)));
            Assert.IsNull(reopened.TakeTeachingThought(true), "Repeated games and reloads do not duplicate either line");
        }

        [Test] public void MiniWithoutActualDeductionOrAcceptedSettlementDoesNotClaimMoneyWasLost()
        {
            var sim = OpenAll(); var host = new Host();
            Assert.IsTrue(host.Spend(host.Money));
            var unpaid = sim.SettleMiniAction("arith", false, host);
            Assert.IsTrue(unpaid.accepted); Assert.AreEqual(0, unpaid.fine);
            Assert.IsFalse(sim.S.answerFineThoughtRecorded);
            Assert.IsFalse(sim.SettleMiniAction("missing-desk", true, host).accepted);
            Assert.IsFalse(sim.SettleMiniAction("arith", true, null).accepted);
            Assert.IsFalse(sim.S.answerPayThoughtRecorded);
            Assert.IsNull(sim.TakeTeachingThought(true));
            host.Earn(100);
            Assert.Greater(sim.SettleMiniAction("arith", false, host).fine, 0);
            Assert.AreEqual("我去，答错还会扣钱？", sim.TakeTeachingThought(true).Value.zh);
        }

        [Test] public void EveryMappedGameLabelsItsOwnDeskAndNothingElse()
        {
            int checkedDesks = 0;
            foreach (var d in MappedDesks())
            {
                var s = OpenAll(); var h = new Host();
                string game = XgMiniGames.ForDesk(d.id);
                long t = s.BeginMiniGame(game, h);
                Assert.AreNotEqual(0, t, game);
                double rows = s.Labels(d.id), total = s.TotalLabels, money = h.Money;
                var r = s.SubmitMiniGame(t, 1, true, h);
                Assert.IsTrue(r.accepted, game);
                Assert.AreEqual(rows + 1, s.Labels(d.id), d.id + " gets the sample");
                Assert.AreEqual(total + 1, s.TotalLabels, "and no other dataset does");
                Assert.AreEqual(1, r.samples, game);
                Assert.Greater(h.Money, money, game);
                checkedDesks++;
            }
            Assert.AreEqual(11, checkedDesks, "spam, sense, arith, danmu, logic, cifar, go, poems, meme, headline, review");
        }

        [Test] public void ARightAnswerPaysTheDesksHandPayOnEveryDesk()
        {
            foreach (var d in MappedDesks())
            {
                var s = OpenAll(); var h = new Host();
                s.S.combo = 0;
                double basePay = s.ManualPayFor(d.id, s.LevelOf(d.id));
                Assert.AreEqual(basePay, s.MiniBasePay(d.id), 1e-9, d.id);
                var a = s.SettleMiniAction(d.id, true, h);
                Assert.AreEqual(basePay, a.pay, 1e-9, d.id + ": same base as a classic card");
                Assert.AreEqual(basePay + a.bonus, h.Money - 100000, 1e-6, d.id + ": wallet got the base and the bonus");
            }
            // Pay differs by desk only through the desk's own factor (logic pays 3.2x the base desks).
            var sim = OpenAll();
            Assert.AreEqual(sim.MiniBasePay("sense") * 3.2, sim.MiniBasePay("logic"), 1e-9);
        }

        [Test] public void EachTwoCompletedCorrectAnswersRaiseTheFollowingAnswerUpToFive()
        {
            int[] expected = { 1, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 5, 5, 5, 5, 5 };
            for (int combo = 0; combo < expected.Length; combo++)
                Assert.AreEqual(expected[combo], XgSim.StreakMultiplierOf(combo), "streak " + combo);
            Assert.AreEqual(5, XgSim.StreakMultiplierOf(500), "capped at ×5");
            Assert.AreEqual(1, XgSim.StreakMultiplierOf(-3));
            Assert.AreEqual(3, XgSim.ToNextMultiplier(0));
            Assert.AreEqual(1, XgSim.ToNextMultiplier(2), "two in a row: the next one doubles");
            Assert.AreEqual(2, XgSim.ToNextMultiplier(3));
            Assert.AreEqual(1, XgSim.ToNextMultiplier(8));
            Assert.AreEqual(0, XgSim.ToNextMultiplier(9), "nothing left at ×5");
            CollectionAssert.AreEqual(new[] { 3, 5, 7, 9 }, XgCatalog.ComboTiers, "one combo tier per multiplier");
        }

        [Test] public void TheBonusIsTheStreakPlusASpeedShare()
        {
            Assert.AreEqual(.5, XgSim.MiniSpeedShare(0), 1e-9);
            Assert.AreEqual(.25, XgSim.MiniSpeedShare(2.5), 1e-9);
            Assert.AreEqual(0, XgSim.MiniSpeedShare(XgSim.MiniFastSeconds), 1e-9);
            Assert.AreEqual(0, XgSim.MiniSpeedShare(9), 1e-9);
            Assert.AreEqual(0, XgSim.MiniSpeedShare(-1), 1e-9, "not timed");
            Assert.AreEqual(0, XgSim.MiniSpeedShare(double.NaN), 1e-9);
            Assert.AreEqual(.5, XgSim.MiniBonusShare(0, 1), 1e-9, "speed alone: +50%");
            Assert.AreEqual(1, XgSim.MiniBonusShare(-1, 2), 1e-9, "×2 streak alone: +100%");
            Assert.AreEqual(2.1, XgSim.MiniBonusShare(4, 3), 1e-9, "streak and speed add up");
            Assert.AreEqual(4.5, XgSim.MiniBonusShare(0, 5), 1e-9, "×5 and an instant answer");
            Assert.AreEqual(4.5, XgSim.MiniBonusShare(0, 9), 1e-9, "the multiplier never goes past ×5");
            Assert.AreEqual(.5, XgSim.MiniBonusShare(3, 1, true), 1e-9, "常识判断: flat +50% under 7.5 s");
            Assert.AreEqual(0, XgSim.MiniBonusShare(8, 1, true), 1e-9);

            {
                var s = OpenAll(); var h = new Host();
                s.S.combo = 500;
                double basePay = s.MiniBasePay("spam");
                var a = s.SettleMiniAction("spam", true, h, 0);
                Assert.AreEqual(5, a.multiplier);
                Assert.AreEqual(basePay * XgSim.MiniBonusCap, a.bonus, 1e-9, "a long streak plus an instant answer: ×5 and +50%");
            }
        }

        [Test] public void EveryTwoCompletedAnswersUpgradeTheNextPayoutAndOnlyOneTierEvent()
        {
            var s = OpenAll(); var h = new Host(); s.S.combo = 0;
            double basePay = s.MiniBasePay("spam");
            var events = new System.Collections.Generic.List<int>();
            s.ComboTier += events.Add;
            int[] expected = { 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 5, 5 };
            for (int i = 0; i < expected.Length; i++)
            {
                double before = h.Money;
                var answer = s.SettleMiniAction("spam", true, h);
                Assert.AreEqual(expected[i], answer.multiplier, "correct answer " + (i + 1));
                Assert.AreEqual(basePay * expected[i], answer.Total, 1e-9);
                Assert.AreEqual(answer.Total, h.Money - before, 1e-6, "exactly one payout");
            }
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, events);
            s.SettleMiniAction("spam", false, h);
            Assert.AreEqual(0, s.S.combo);
            Assert.AreEqual(basePay, s.SettleMiniAction("spam", true, h).Total, 1e-9, "a wrong answer starts again at ×1");
        }

        [Test] public void EveryDeskFinesOnlyAfterAWrongAnswer()
        {
            foreach (var d in MappedDesks())
            {
                var s = OpenAll(); var h = new Host();
                s.S.combo = 7;
                double money = h.Money, rows = s.Labels(d.id);
                double expected = (d.fine > 0 ? d.fine : XgCatalog.HandFine) * s.MiniBasePay(d.id);
                var a = s.SettleMiniAction(d.id, false, h);
                Assert.AreEqual(expected, a.fine, 1e-9, d.id);
                Assert.AreEqual(money - expected, h.Money, 1e-9, d.id);
                Assert.AreEqual(0, a.pay + a.bonus, d.id + ": nothing earned");
                Assert.AreEqual(0, a.samples, d.id);
                Assert.AreEqual(rows, s.Labels(d.id), d.id + ": a wrong answer is not a sample");
                Assert.AreEqual(0, s.S.combo, d.id + ": and it breaks the combo");
            }
            Assert.Greater(XgCatalog.Desk("arith").fine, 0);
            Assert.AreEqual(0, XgCatalog.Desk("spam").fine);
            // Through the ticket the same numbers come back.
            var sim = OpenAll(); var host = new Host();
            long t = sim.BeginMiniGame("dadishu", host);
            var r = sim.SubmitMiniGame(t, 1, false, host);
            Assert.AreEqual(sim.HandFineFor("arith", sim.LevelOf("arith")), r.fine, 1e-9);
            Assert.Greater(r.fine, 0);
        }

        [Test] public void EachActionIsOneSampleAndTheStreakNeverGrowsIt()
        {
            var s = OpenAll(); var h = new Host(); long t = s.BeginMiniGame("slice", h);
            double before = s.Labels("spam");
            for (int i = 1; i <= 300; i++)
            {
                double rows = s.Labels("spam"), basePay = s.MiniBasePay("spam");
                var r = s.SubmitMiniGame(t, i, true, h);
                Assert.AreEqual(1, r.samples, "action " + i);
                Assert.AreEqual(rows + 1, s.Labels("spam"), "action " + i);
                Assert.LessOrEqual(r.pay, basePay * XgSim.ComboMaxMultiplier + 1e-9, "a long streak never pays more than ×5 (action " + i + ")");
            }
            Assert.AreEqual(before + 300, s.Labels("spam"));
            Assert.AreEqual(300, s.MiniGameRecord("slice").samples);
        }

        [Test] public void TheNumberOfItemsInAGameNeverChangesWhatAnActionIsWorth()
        {
            // Two games on two desks: each action pays its own desk's base, however many items a round holds.
            var s = OpenAll(); var h = new Host();
            long a = s.BeginMiniGame("kaixincidian", h);
            double sensePay = s.SubmitMiniGame(a, 1, true, h).pay;
            Assert.AreEqual(s.MiniBasePay("sense"), sensePay, 1e-9);
            Assert.AreEqual(s.MiniBasePay("sense"), s.SubmitMiniGame(a, 2, true, h).pay, 1e-9);
            long b = s.BeginMiniGame("dadishu", h);
            var arith = s.SubmitMiniGame(b, 1, true, h);
            Assert.AreEqual(s.MiniBasePay("arith") * 2, arith.pay, 1e-9, "the shared combo carries on across games: the third in a row doubles");
            Assert.AreEqual(2, arith.multiplier);
        }

        [Test] public void TheWritingViewKeepsItsNumbers()
        {
            var s = Open(XgSim.HandwritingDesk); var h = new Host();
            s.S.combo = 4;
            double basePay = s.ManualPayFor(XgSim.HandwritingDesk, s.LevelOf(XgSim.HandwritingDesk));
            double money = h.Money;
            var raster = XgHandwriting.Template(3, 0);
            var r = s.SubmitHandwriting(3, raster, 2, h);
            Assert.IsTrue(r.accepted);
            Assert.AreEqual(basePay, r.pay, 1e-9);
            double expected = r.hit ? basePay * (1 - 2 / XgSim.HandwritingFastSeconds) : 0;
            Assert.AreEqual(expected, r.bonus, 1e-9, "a speed bonus only for a right reading, fading over five seconds");
            Assert.AreEqual(basePay + expected, h.Money - money, 1e-9);
            Assert.AreEqual(4, s.S.combo, "the shared combo is left alone");
            Assert.AreEqual(1, s.Labels(XgSim.HandwritingDesk));
            Assert.AreEqual(0, s.S.handFines, 1e-9, "a drawing is never fined");
        }

        [Test] public void TrainingAndCrawlGamesStayOutOfTheLabelRule()
        {
            var s = OpenAll(); var h = new Host();
            long t = s.BeginMiniGame("forgetax", h);
            double total = s.TotalLabels, money = h.Money;
            s.SubmitMiniGame(t, 1, true, h);
            Assert.AreEqual(total, s.TotalLabels, "a forge strike adds no label");
            Assert.AreEqual(money, h.Money, 1e-9);
        }
    }
}
