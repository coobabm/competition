using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgMoleArithmeticTests
    {
        sealed class Host : IXgHost
        {
            public double Money { get; private set; } = 100000;
            public double Compute => 10;
            public double VramMB => 4096;
            public string Blocker => null;
            public Action Earned;
            public bool Spend(double amount) { if (Money < amount) return false; Money -= amount; return true; }
            public void Earn(double amount) { Money += amount; Earned?.Invoke(); }
            public void Train(double seconds) { }
        }
        // Independent parser for what the player reads, including unary-minus/exponent precedence.
        sealed class Arithmetic
        {
            readonly string text;
            int at;
            public Arithmetic(string text) { this.text = text.Replace("−", "-").Replace("×", "*").Replace("÷", "/").Replace("²", "^2"); }
            bool Eat(char c) { if (at >= text.Length || text[at] != c) return false; at++; return true; }
            double Atom()
            {
                if (Eat('√')) return Math.Sqrt(Atom());
                if (Eat('(')) { double v = Sum(); Assert.IsTrue(Eat(')')); return v; }
                int begin = at; while (at < text.Length && char.IsDigit(text[at])) at++;
                Assert.Greater(at, begin, text);
                return double.Parse(text.Substring(begin, at - begin), CultureInfo.InvariantCulture);
            }
            double Unary()
            {
                if (Eat('-')) return -Unary();
                double v = Atom(); return Eat('^') ? Math.Pow(v, Unary()) : v;
            }
            double Product()
            {
                double v = Unary();
                while (at < text.Length) { if (Eat('*')) v *= Unary(); else if (Eat('/')) v /= Unary(); else break; }
                return v;
            }
            double Sum()
            {
                double v = Product();
                while (at < text.Length) { if (Eat('+')) v += Product(); else if (Eat('-')) v -= Product(); else break; }
                return v;
            }
            public double Value() { double v = Sum(); Assert.AreEqual(text.Length, at, text); return v; }
        }
        static double Eval(string s) => new Arithmetic(s).Value();
        static double Number(Match match, int group) => double.Parse(match.Groups[group].Value, CultureInfo.InvariantCulture);
        static double RealFromDisplayed(XgMoleQuestion q, out double shown)
        {
            string text = q.Text;
            if (q.Kind == XgMoleMathKind.Logarithm)
            {
                var logarithm = Regex.Match(text, @"^log\[(\d+)\]\((\d+)\)=(.+)$");
                Assert.IsTrue(logarithm.Success, text);
                shown = Eval(logarithm.Groups[3].Value);
                return Math.Log(Number(logarithm, 2), Number(logarithm, 1));
            }
            if (q.Kind <= XgMoleMathKind.RootOfSquare || q.Kind == XgMoleMathKind.Exponential)
            {
                var sides = text.Split('='); shown = Eval(sides[1]); return Eval(sides[0]);
            }
            if (q.Kind == XgMoleMathKind.Limit)
            {
                var m = Regex.Match(text, @"^lim\[x→(\d+)\] \(x²−(\d+)\)/\(x−(\d+)\)=(.+)$");
                Assert.IsTrue(m.Success, text); double point = Number(m, 1), constant = Number(m, 2), pole = Number(m, 3);
                Assert.AreEqual(point, pole); Assert.AreEqual(point * point, constant);
                // Separate one-sided probes confirm the stated two-sided limit, without evaluating at the pole.
                double left = point - .001, right = point + .001;
                double l = (left * left - constant) / (left - pole), r = (right * right - constant) / (right - pole);
                Assert.AreEqual(l, r, .00201); shown = Eval(m.Groups[4].Value); return (l + r) / 2;
            }
            if (q.Kind == XgMoleMathKind.Derivative)
            {
                var m = Regex.Match(text, @"^\[d\((\d+)x\^(\d+)\)/dx\]x=(\d+): (.+)$");
                Assert.IsTrue(m.Success, text); double a = Number(m, 1), n = Number(m, 2), x = Number(m, 3);
                const double h = .0001;
                shown = Eval(m.Groups[4].Value);
                return (a * Math.Pow(x + h, n) - a * Math.Pow(x - h, n)) / (2 * h);
            }
            if (q.Kind == XgMoleMathKind.DefiniteIntegral)
            {
                var m = Regex.Match(text, @"^∫\[0,(\d+)\] (\d+)x\^(\d+) dx=(.+)$");
                Assert.IsTrue(m.Success, text); double bound = Number(m, 1), a = Number(m, 2), n = Number(m, 3);
                // Independent composite Simpson quadrature over the displayed integrand.
                const int count = 200; double step = bound / count, total = a * Math.Pow(bound, n);
                for (int i = 1; i < count; i++) total += (i % 2 == 0 ? 2 : 4) * a * Math.Pow(i * step, n);
                shown = Eval(m.Groups[4].Value); return total * step / 3;
            }
            var integral = Regex.Match(text, @"^∫ (\d+)x\^(\d+) dx=\((.+)\)x\^(\d+)\+C$");
            Assert.IsTrue(integral.Success, text);
            double power = Number(integral, 4); Assert.AreEqual(Number(integral, 2) + 1, power);
            StringAssert.Contains("任意常数", q.ConditionsZh); StringAssert.Contains("any constant", q.ConditionsEn);
            shown = Eval(integral.Groups[3].Value); return Number(integral, 1) / power;
        }

        [Test] public void DisplayedMathematicsHasIndependentJudgmentForEveryTier()
        {
            for (int tier = 1; tier <= 3; tier++)
            {
                var kinds = new HashSet<XgMoleMathKind>(); int trueCount = 0;
                for (int seed = 0; seed < 3500; seed++)
                {
                    var q = XgMoleArithmetic.Generate(seed * 7919 + tier, tier);
                    kinds.Add(q.Kind); if (q.Truth) trueCount++;
                    double real = RealFromDisplayed(q, out double shown);
                    Assert.AreEqual(real, (double)q.Expected.Numerator / q.Expected.Denominator, .0002, q.Text);
                    Assert.AreEqual(Math.Abs(real - shown) < .0002, q.Truth, q.Text);
                    Assert.IsFalse(double.IsNaN(real) || double.IsInfinity(real), q.Text);
                    Assert.IsTrue(q.AsTrue().Truth); Assert.AreEqual(q.CorrectText, q.AsTrue().Text);
                }
                Assert.AreEqual(tier == 1 ? 4 : tier == 2 ? 12 : 16, kinds.Count);
                Assert.That((double)trueCount / 3500, Is.InRange(.44, .56));
            }
        }

        [Test] public void TopicSelectionGatesPurchasesAndPreservesOldOwnedPacks()
        {
            var sim = new XgSim();
            Assert.AreEqual(XgMoleTopic.Basic, sim.MoleTopic);
            Assert.IsFalse(sim.SelectMoleTopic(XgMoleTopic.Mixed));
            Assert.IsFalse(sim.SelectMoleTopic(XgMoleTopic.Powers));
            Assert.IsFalse(sim.SelectMoleTopic(XgMoleTopic.Calculus));
            sim.S.unlocked.Add(XgSim.MoleMixedNode);
            Assert.IsTrue(sim.SelectMoleTopic(XgMoleTopic.Mixed));
            Assert.IsFalse(sim.SelectMoleTopic(XgMoleTopic.Powers));
            sim.S.unlocked.Add(XgSim.MolePowersNode);
            Assert.IsTrue(sim.SelectMoleTopic(XgMoleTopic.Powers));
            Assert.IsFalse(sim.SelectMoleTopic(XgMoleTopic.Calculus));
            sim.S.unlocked.Add(XgSim.MoleCalculusNode);
            Assert.IsTrue(sim.SelectMoleTopic(XgMoleTopic.Calculus));
            Assert.AreEqual(XgMoleTopic.Calculus, new XgSim(sim.S).MoleTopic);
            var old = new XgSim(); old.S.unlocked.Add(XgSim.MoleCalculusNode);
            old.RepairMoleTopics(); old.RepairMoleTopics();
            Assert.AreEqual(1, old.S.unlocked.Count(x => x == XgSim.MoleMixedNode));
            Assert.AreEqual(1, old.S.unlocked.Count(x => x == XgSim.MolePowersNode));
            old.S.moleArithmeticTopic = 99; old.RepairMoleTopics();
            Assert.AreEqual(XgMoleTopic.Basic, old.MoleTopic);
        }

        [Test] public void SelectedTopicNeverLeaksOtherOrLockedQuestionKinds()
        {
            var sim = new XgSim();
            sim.S.unlocked.Add(XgSim.MoleMixedNode); sim.S.unlocked.Add(XgSim.MolePowersNode); sim.S.unlocked.Add(XgSim.MoleCalculusNode);
            var allowed = new[] {
                new[] { XgMoleMathKind.Add, XgMoleMathKind.Subtract, XgMoleMathKind.Multiply, XgMoleMathKind.Divide },
                new[] { XgMoleMathKind.Parentheses, XgMoleMathKind.Precedence },
                new[] { XgMoleMathKind.Power, XgMoleMathKind.SignedPower, XgMoleMathKind.SquareRoot, XgMoleMathKind.RootOfSquare, XgMoleMathKind.Exponential, XgMoleMathKind.Logarithm },
                new[] { XgMoleMathKind.Limit, XgMoleMathKind.Derivative, XgMoleMathKind.DefiniteIntegral, XgMoleMathKind.IndefiniteIntegral }
            };
            for (int topic = 0; topic < allowed.Length; topic++)
            {
                Assert.IsTrue(sim.SelectMoleTopic((XgMoleTopic)topic));
                var seen = new HashSet<XgMoleMathKind>();
                for (int seed = 0; seed < 200; seed++)
                {
                    var q = sim.GenerateMoleQuestion(seed * 7919);
                    CollectionAssert.Contains(allowed[topic], q.Kind); seen.Add(q.Kind);
                    double actual = RealFromDisplayed(q, out double shown);
                    Assert.AreEqual(Math.Abs(actual-shown) < .0002, q.Truth, q.Text);
                    if (topic == 0) { StringAssert.DoesNotContain("(", q.Text); StringAssert.DoesNotContain(")", q.Text); }
                }
                CollectionAssert.AreEquivalent(allowed[topic], seen);
            }
            var locked = new XgSim(); locked.S.moleArithmeticTopic = (int)XgMoleTopic.Calculus;
            for (int seed=0; seed<100; seed++) CollectionAssert.Contains(allowed[0], locked.GenerateMoleQuestion(seed).Kind);
        }

        [Test] public void EquivalentRationalsNormalizeAndZeroDenominatorsAreRejected()
        {
            Assert.AreEqual(new XgMoleNumber(1, 2), new XgMoleNumber(2, 4));
            Assert.AreEqual(new XgMoleNumber(-1, 2), new XgMoleNumber(1, -2));
            Assert.AreEqual(new XgMoleNumber(0), new XgMoleNumber(0, 7));
            Assert.Throws<DivideByZeroException>(() => new XgMoleNumber(3, 0));
        }

        static string[] Features(XgBoardCard input) => input.features.Select(f => f.name + ":" + f.x + ":" + f.y + ":" + f.seq).ToArray();

        [Test] public void InferenceInputAndPredictionCannotReadTruthOrGeneratorSeed()
        {
            var sim = new XgSim();
            for (int seed = 0; seed < 150; seed++)
            {
                var displayed = XgMoleArithmetic.Generate(seed, 3).Card();
                var before = XgMoleInference.Input(displayed); var prediction = sim.PredictMole(displayed);
                displayed.truth = !displayed.truth; displayed.seed = seed ^ 7919; displayed.level = 99;
                displayed.questionEn = "different localization";
                var after = XgMoleInference.Input(displayed); var repeat = sim.PredictMole(displayed);
                CollectionAssert.AreEqual(Features(before), Features(after));
                Assert.AreEqual(before.seed, after.seed); Assert.AreEqual(before.truth, after.truth);
                Assert.AreEqual(prediction.AnswerYes, repeat.AnswerYes); Assert.AreEqual(prediction.Score, repeat.Score);
                Assert.AreEqual(prediction.Guessed, repeat.Guessed);
                Assert.IsFalse(before.features.Any(f => f.name.Contains("验算") || f.name.Contains("看着")));
                Assert.IsFalse(before.features.Any(f => f.name.Contains("+") || f.name.Contains("@")), "literal operators must not collide with concept-key separators");
            }
        }

        [Test] public void ColdModelCanBeWrongAndInferenceDoesNotTrainIt()
        {
            var sim = new XgSim(); long cards = sim.S.board.cards; int count = sim.S.board.concepts.Count, wrong = 0;
            for (int seed = 0; seed < 200; seed++)
            {
                var q = XgMoleArithmetic.Generate(seed * 3571, 3);
                if (sim.PredictMole(q.Card()).AnswerYes != q.Truth) wrong++;
            }
            Assert.Greater(wrong, 0); Assert.Less(wrong, 200);
            Assert.AreEqual(cards, sim.S.board.cards); Assert.AreEqual(count, sim.S.board.concepts.Count);
        }

        [Test] public void OutputReadsLiveModelWeightsRatherThanAccuracyOrAnswerLabels()
        {
            // This checks the live-board connection, not training efficacy of the new text vocabulary.
            var sim = new XgSim();
            var displayed = new XgCard { dataset="arith", question="2+3=5", category="加减" };
            // Simple arithmetic uses visible numeric-relation features, not category tokens.
            // Pin its operator context, never a label or the question generator's random kind.
            var encoded = XgMoleInference.Input(displayed);
            var knobs = sim.Knobs(sim.Run(XgCatalog.Dataset("arith").track));
            Assert.IsTrue(XgArithmeticRelations.TryElements(encoded, knobs, out var elements));
            string visibleOperator = elements.Single(e => e.id.StartsWith("numrel:v1:算符:", StringComparison.Ordinal)).id;
            sim.S.board = new XgBoardState();
            var concept = new XgConcept { id=999, layer=1, region="logic", key=visibleOperator, w=1, s=1, pinned=true };
            sim.S.board.concepts.Add(concept);
            var positive = sim.PredictMole(displayed);
            Assert.IsFalse(positive.Guessed); Assert.Greater(positive.Score, 0); Assert.IsTrue(positive.AnswerYes);
            concept.w = -1;
            var negative = sim.PredictMole(displayed);
            Assert.IsFalse(negative.Guessed); Assert.Less(negative.Score, 0); Assert.IsFalse(negative.AnswerYes);
        }

        [Test] public void TurnRejectsDuplicateInactiveExpiredAndPreviousQuestionCallbacks()
        {
            var turn = new XgMoleTurn(); turn.Begin(11);
            Assert.IsFalse(turn.TryAutoDecision(11, false)); Assert.IsFalse(turn.TryWhack(11, false));
            Assert.IsTrue(turn.TryAutoDecision(11, true)); Assert.IsFalse(turn.TryAutoDecision(11, true));
            Assert.IsTrue(turn.TryWhack(11, true)); Assert.IsFalse(turn.TryWhack(11, true));
            turn.Begin(12); Assert.IsFalse(turn.TryWhack(11, true));
            turn.Invalidate(); Assert.IsFalse(turn.TryWhack(12, true)); Assert.IsFalse(turn.TryAutoDecision(12, true));
            turn.Begin(13); Assert.IsTrue(turn.TryWhack(13, true)); // manual wins before scheduled auto
            Assert.IsFalse(turn.TryAutoDecision(13, true));
        }

        [Test] public void ReentrantAutomaticWhackUsesNormalPayExactlyOnce()
        {
            var sim = new XgSim(); var host = new Host();
            long ticket = sim.BeginMiniGame("arith_mole", host); Assert.AreNotEqual(0, ticket);
            var turn = new XgMoleTurn(); turn.Begin(31);
            int sequence = 0; double before = sim.Labels("arith"), money = host.Money;
            bool Submit()
            {
                if (!turn.TryWhack(31, true)) return false;
                var result = sim.SubmitMiniGame(ticket, sequence + 1, true, host, automatic: true);
                if (result.accepted) sequence++;
                return result.accepted;
            }
            host.Earned = () => Assert.IsFalse(Submit(), "host callbacks must not resettle the same mole");
            Assert.IsTrue(turn.TryAutoDecision(31, true)); Assert.IsTrue(Submit());
            Assert.IsFalse(Submit()); Assert.AreEqual(1, sequence);
            Assert.IsFalse(sim.S.answerPayThoughtRecorded); Assert.IsEmpty(sim.S.pendingAnswerMoneyThoughts);
            Assert.AreEqual(before + 1, sim.Labels("arith"));
            Assert.Greater(host.Money, money);
            Assert.LessOrEqual(host.Money - money, sim.MiniBasePay("arith") * 2 + 1e-9);
            sim.CancelMiniGame(ticket);
            Assert.IsFalse(sim.SubmitMiniGame(ticket, 2, true, host).accepted);
        }

        [Test] public void AutoHasItsOwnPurchaseAndNeverEnablesOtherGames()
        {
            var sim = new XgSim(); sim.S.stage = 2;
            Assert.IsFalse(sim.MoleAutoAvailable("arith_mole"));
            sim.S.unlocked.Add(XgSim.MoleAutoNode);
            Assert.IsFalse(sim.TeachingAiReady);
            Assert.IsTrue(sim.MoleAutoAvailable("arith_mole")); Assert.IsTrue(sim.MoleAutoAvailable("dadishu"));
            Assert.IsFalse(sim.MoleAutoAvailable("slice"));
            sim.S.stage = 1; Assert.IsFalse(sim.MoleAutoAvailable("arith_mole"));
            Assert.AreEqual(1, sim.MoleArithmeticTier);
            sim.S.unlocked.Add(XgSim.MoleCalculusNode); Assert.AreEqual(1, sim.MoleArithmeticTier);
            sim.S.unlocked.Add(XgSim.MolePowersNode); Assert.AreEqual(3, sim.MoleArithmeticTier);
            sim.UseBoard = false; Assert.IsNull(sim.PredictMole(XgMoleArithmetic.Generate(0, 1).Card()));
        }

        [Test] public void CatalogKeepsExistingPricesAndChainsTheNewMixedPackFirst()
        {
            var nodes = new List<XgNode>(); XgTrainingCatalog.AddNodes(nodes);
            var mixed = nodes.Single(n => n.id == XgSim.MoleMixedNode);
            var powers = nodes.Single(n => n.id == XgSim.MolePowersNode);
            var calculus = nodes.Single(n => n.id == XgSim.MoleCalculusNode);
            var auto = nodes.Single(n => n.id == XgSim.MoleAutoNode);
            Assert.AreEqual(mixed.id, powers.parent); Assert.AreEqual(1, mixed.stage);
            Assert.Greater(mixed.cost, 0);
            Assert.AreEqual(800, powers.cost); Assert.AreEqual(2, powers.stage);
            Assert.AreEqual(3000, calculus.cost); Assert.AreEqual(3, calculus.stage); Assert.AreEqual(powers.id, calculus.parent);
            Assert.AreEqual(1500, auto.cost); Assert.AreEqual(2, auto.stage); Assert.IsTrue(string.IsNullOrEmpty(auto.parent));
            var sim = new XgSim();
            Assert.AreEqual(800, sim.NodeCost(powers)); Assert.AreEqual(3000, sim.NodeCost(calculus)); Assert.AreEqual(1500, sim.NodeCost(auto));
        }
    }
}
