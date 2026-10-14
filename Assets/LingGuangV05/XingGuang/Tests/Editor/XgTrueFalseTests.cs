using System;
using System.Globalization;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>
    /// The 算术 desk as a true/false desk (an equation shown with an answer, 对 / 错), the money a wrong pick costs,
    /// the 2.5× hand pay, and the spider the player drags onto the question (a hand answer that can cost money, unlike
    /// the random help of the perched spider).
    /// </summary>
    public sealed class XgTrueFalseTests
    {
        sealed class Host : IXgHost
        {
            public double money = 1000;
            public double Compute => 1;
            public double VramMB => 16000;
            public double Money => money;
            public bool Spend(double amount) { if (amount < 0 || money < amount) return false; money -= amount; return true; }
            public void Earn(double amount) { money += amount; }
            public void Train(double seconds) { }
            public string Blocker => null;
        }

        static XgSim WithCheckpoint(string desk, double acc)
        {
            var sim = new XgSim { GoldChance = 0 };
            sim.S.stage = sim.S.stageVision = sim.S.stageSequence = 3;
            sim.S.desksOpen.Add(desk);
            sim.S.best.Add(new XgBest { dataset = desk, arch = "rnn", acc = acc });
            return sim;
        }

        static readonly Regex Equation = new Regex(@"^(\d+) ([+−×÷]) (\d+) = (\d+)$");

        static long Real(long a, string op, long b) => op == "+" ? a + b : op == "−" ? a - b : op == "×" ? a * b : b == 0 ? 0 : a / b;

        // ───────────── the equations ─────────────

        [Test]
        public void TheDeskShowsEquationsWithAnAnswerAndAboutHalfAreWrong()
        {
            int equations = 0, right = 0, wrong = 0;
            var gaps = new System.Collections.Generic.HashSet<long>();
            for (int level = 1; level <= 2; level++)
                for (int seed = 0; seed < 3000; seed++)
                {
                    var q = XgArith.Generate(seed * 31 + level, level);
                    if (q.category == "加减" || q.category == "乘法")
                    {
                        Assert.IsTrue(Equation.IsMatch(q.text), "an equation is shown as it is: " + q.text);
                        Assert.IsTrue(Equation.IsMatch(q.textEn), q.textEn);
                    }
                    var m = Equation.Match(q.text);
                    if (!m.Success) continue;
                    equations++;
                    long a = long.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture), b = long.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture), shown = long.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture);
                    long real = Real(a, m.Groups[2].Value, b);
                    Assert.AreEqual(q.truth, shown == real, q.text);
                    if (q.truth) { right++; continue; }
                    wrong++;
                    gaps.Add(Math.Abs(shown - real));
                    // A slip a person makes, not a random number: within a decimal point of the real result.
                    if (real >= 10 && shown > 0) Assert.That((double)shown / real, Is.InRange(.09, 10.01), q.text);
                }
            Assert.Greater(equations, 3000, "most cards on the first two levels are bare equations");
            Assert.Greater(right, 0); Assert.Greater(wrong, 0);
            Assert.AreEqual(.5, (double)right / equations, .06, "about half are right");
            Assert.That(gaps, Is.SupersetOf(new long[] { 1, 2, 10 }), "off by one, two and ten");
        }

        [Test]
        public void AMultiplicationSlipIsOneFactorOffNotARandomNumber()
        {
            int factorSlips = 0;
            for (int seed = 0; seed < 4000; seed++)
            {
                var q = XgArith.Generate(seed * 17 + 2, 2);
                var m = Equation.Match(q.text);
                if (!m.Success || q.truth || m.Groups[2].Value != "×") continue;
                long a = long.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture), b = long.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture), shown = long.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture);
                long gap = Math.Abs(shown - a * b);
                if (gap == a || gap == b) factorSlips++;
            }
            Assert.Greater(factorSlips, 20, "one factor too many or too few");
        }

        // ───────────── the money ─────────────

        [Test]
        public void EveryDeskPaysTwoAndAHalfTimesAsMuchByHand()
        {
            Assert.AreEqual(2.5, XgCatalog.HandPayScale, 1e-9);
            var sim = new XgSim();
            foreach (var d in XgCatalog.Desks)
                Assert.AreEqual(XgCatalog.LabelPay * d.pay * 2.5, sim.ManualPayFor(d.id, 1), 1e-9, d.id);
            sim.S.payRaise = 4;
            Assert.AreEqual(sim.PayFor("arith", 3) * 2.5 * XgCatalog.RaiseMultipliers[4], sim.ManualPayFor("arith", 3), 1e-9);
        }

        [Test]
        public void AutomaticLabelsKeepTheirOldRate()
        {
            var sim = WithCheckpoint("spam", .9); var host = new Host();
            for (int i = 0; i < 20; i++)
            {
                var r = sim.SpiderAnswer("spam", host);
                if (r.correct) Assert.AreEqual(XgCatalog.LabelPay * XgCatalog.Desk("spam").pay * sim.QualityPayMultiplier, r.pay, 1e-9);
            }
        }

        [Test]
        public void AWrongPickOnTheArithmeticDeskCostsHalfARightAnswerAndBreaksTheCombo()
        {
            var sim = new XgSim { GoldChance = 0 }; var host = new Host();
            sim.S.teachMode = false; // the plain combo and the fine are what is being priced
            sim.SelectDesk("arith");
            Assert.AreEqual("arith", sim.S.desk);
            var first = sim.Card("arith");
            Assert.IsTrue(sim.Answer("arith", first.truth, host).correct);
            Assert.AreEqual(1, sim.S.combo);
            double before = host.money;
            var card = sim.Card("arith");
            var wrong = sim.Answer("arith", !card.truth, host);
            Assert.IsFalse(wrong.correct);
            double expected = .5 * sim.ManualPayFor("arith", card.level);
            Assert.AreEqual(expected, wrong.fine, 1e-9);
            Assert.AreEqual(before - expected, host.money, 1e-9, "taken from the wallet");
            Assert.AreEqual(0, wrong.pay);
            Assert.AreEqual(0, sim.S.combo, "and the combo breaks");
            Assert.AreEqual(expected, sim.S.handFines, 1e-9);
            Assert.AreEqual(expected, sim.S.totalSpent, 1e-9);
        }

        [Test]
        public void TheFineScalesWithTheRaiseAndNotWithTheCombo()
        {
            var plain = new XgSim { GoldChance = 0 }; var raised = new XgSim { GoldChance = 0 };
            plain.S.teachMode = raised.S.teachMode = false;
            raised.S.payRaise = 5;
            var host = new Host { money = 1000 };
            raised.S.combo = 40; raised.S.comboTimer = 5;
            var a = plain.Card("arith"); var b = raised.Card("arith");
            Assert.AreEqual(a.level, b.level);
            double fineA = plain.Answer("arith", !a.truth, host).fine, fineB = raised.Answer("arith", !b.truth, host).fine;
            Assert.AreEqual(XgCatalog.RaiseMultipliers[5] / XgCatalog.RaiseMultipliers[0], fineB / fineA, 1e-6);
        }

        [Test]
        public void TheFineNeverTakesMoreThanTheWalletHolds()
        {
            var sim = new XgSim { GoldChance = 0 }; var host = new Host { money = .1 };
            sim.S.teachMode = false; // fines belong to the classic scoring; taught answers are never fined
            var card = sim.Card("arith");
            var r = sim.Answer("arith", !card.truth, host);
            Assert.AreEqual(.1, r.fine, 1e-9);
            Assert.AreEqual(0, host.money, 1e-9);
            var broke = new Host { money = -50 };
            var again = sim.Answer("arith", !sim.Card("arith").truth, broke);
            Assert.AreEqual(0, again.fine); Assert.AreEqual(-50, broke.money, 1e-9, "a wallet in debt is not fined further");
        }

        [Test]
        public void OtherDesksOnlyBreakTheComboOnAMistake()
        {
            var sim = new XgSim { GoldChance = 0 }; var host = new Host();
            sim.S.desk = "spam";
            var card = sim.Card("spam");
            var r = sim.Answer("spam", !card.truth, host);
            Assert.IsFalse(r.correct);
            Assert.AreEqual(0, r.fine);
            Assert.AreEqual(1000, host.money, 1e-9);
        }

        [Test]
        public void ATaughtAnswerIsNeverFined()
        {
            var sim = new XgSim { GoldChance = 0 }; var host = new Host();
            Assert.IsTrue(sim.S.teachMode, "teaching mode is the default");
            int taught = 0;
            for (int i = 0; i < 60; i++)
            {
                var card = sim.Card("arith");
                if (!sim.TeachingGuess(card, out _)) continue;
                taught++;
                double before = host.money;
                var r = sim.Answer("arith", !card.truth, host);
                Assert.IsFalse(r.correct);
                Assert.AreEqual(0, r.fine); Assert.AreEqual(before, host.money, 1e-9);
            }
            Assert.Greater(taught, 0);
            Assert.AreEqual(0, sim.S.handFines);
        }

        [Test]
        public void TheFineSurvivesAReload()
        {
            var sim = new XgSim { GoldChance = 0 }; var host = new Host();
            sim.S.teachMode = false;
            sim.Answer("arith", !sim.Card("arith").truth, host);
            double fines = sim.S.handFines;
            Assert.Greater(fines, 0);
            Assert.AreEqual(fines, new XgSim(sim.S).S.handFines, 1e-12);
        }

        // ───────────── the dragged spider ─────────────

        [Test]
        public void TheDraggedSpiderAnswersLikeAHandAndItsMistakesCostMoney()
        {
            var sim = WithCheckpoint("arith", .75); var host = new Host { money = 5000 };
            sim.S.teachMode = false; // a taught answer builds its own streak and is never fined
            sim.S.desk = "arith";
            int right = 0, wrong = 0, n = 400; double fines = 0, paid = 0;
            for (int i = 0; i < n; i++)
            {
                var card = sim.Card("arith");
                Assert.IsTrue(sim.Suggestion("arith", out bool guess, out _));
                double before = host.money; int combo = sim.S.combo;
                var r = sim.SpiderDragAnswer("arith", host);
                Assert.IsTrue(r.accepted);
                Assert.AreEqual(guess, r.yes, "it presses what the checkpoint guesses");
                Assert.AreEqual(guess == card.truth, r.correct);
                if (r.correct) { right++; paid += r.pay; Assert.Greater(r.pay, 0); Assert.AreEqual(0, r.fine); Assert.AreEqual(combo + 1, sim.S.combo, "a right answer builds the combo like a hand answer"); }
                else { wrong++; fines += r.fine; Assert.AreEqual(.5 * sim.ManualPayFor("arith", card.level), r.fine, 1e-9, "a wrong one costs the player's fine"); Assert.AreEqual(0, sim.S.combo); }
                Assert.AreEqual(before + r.pay - r.fine, host.money, 1e-9);
            }
            Assert.Greater(wrong, 0); Assert.Greater(right, wrong);
            Assert.AreEqual(n, sim.S.spiderDragAnswers);
            Assert.AreEqual(fines, sim.S.handFines, 1e-6);
            Assert.That(right / (double)n, Is.InRange(.6, .9), "as right as a 75% checkpoint");
        }

        [Test]
        public void ThePerchedSpidersRandomHelpNeverCostsMoney()
        {
            var sim = WithCheckpoint("arith", .62); var host = new Host { money = 5000 };
            sim.S.desk = "arith";
            int wrong = 0;
            for (int i = 0; i < 400; i++)
            {
                double before = host.money;
                var r = sim.SpiderAnswer("arith", host);
                Assert.IsTrue(r.accepted);
                Assert.GreaterOrEqual(host.money, before - r.fine, "only a platform spot check can take money");
                if (!r.correct) { wrong++; Assert.AreEqual(0, r.pay); if (!r.spotChecked) Assert.AreEqual(before, host.money, 1e-9, "a wrong random help is free"); }
            }
            Assert.Greater(wrong, 0, "a 62% checkpoint is wrong now and then");
            Assert.AreEqual(0, sim.S.handFines, "no hand fine for the spider's own help");
            Assert.AreEqual(0, sim.S.spiderDragAnswers);
        }

        [Test]
        public void TheDraggedSpiderStaysOffDesksItCannotDo()
        {
            var sim = WithCheckpoint("arith", .5); var host = new Host();
            sim.S.desk = "arith";
            Assert.AreEqual("ability", sim.SpiderDragBlocker("arith", host));
            Assert.IsFalse(sim.SpiderDragAnswer("arith", host).accepted);
            Assert.AreEqual(1000, host.money);
            Assert.AreEqual("closed", sim.SpiderDragBlocker("mnist", host));
            var ok = WithCheckpoint("arith", .9);
            Assert.IsNull(ok.SpiderDragBlocker("arith", host));
            ok.S.residentQuiet = true;
            Assert.AreEqual("quiet", ok.SpiderDragBlocker("arith", host));
        }
    }
}
