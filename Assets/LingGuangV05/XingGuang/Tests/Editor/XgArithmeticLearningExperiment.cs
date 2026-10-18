using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    // Opt-in experiment, not a normal fast regression. Do not run during the low-load source-only task.
    public sealed class XgArithmeticLearningExperiment
    {
        sealed class Metrics
        {
            public int tp, tn, fp, fn, guessed;
            public int Count => tp + tn + fp + fn;
            public double Accuracy => (tp + tn) / (double)Count;
            public double Balanced => .5 * (tp / (double)(tp + fn) + tn / (double)(tn + fp));
        }
        static string VisibleKey(XgCard card)
        {
            // Labels, seeds, category and explanation cannot distinguish a test claim from a training claim.
            var input = XgArithmeticInput.Input(new XgCard { dataset = "arith", question = card.question });
            return string.Join(";", input.features.Select(f => f.name + "@" + f.x));
        }
        static string ProblemKey(XgCard card)
        {
            // For equations, reject the same left side even when a different proposed answer is shown.
            // Word problems with no '=' retain their whole visible wording; this is a stated split limitation.
            string text = card.question ?? ""; int equals = text.LastIndexOf('=');
            return VisibleKey(new XgCard { dataset = "arith", question = equals < 0 ? text : text.Substring(0, equals) });
        }
        static string Digest(IEnumerable<XgCard> rows)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n", rows.Select(c => VisibleKey(c) + "|" + c.truth))))).Replace("-", "");
        }
        static XgCard Generated(int index, int salt, XgBoardData.Use use)
        {
            int level = index % 5 + 1;
            int seed = XgBoardData.Seed("arith", index / 5, use, salt);
            var q = XgArith.Generate(seed, level);
            return new XgCard { dataset = "arith", seed = seed, level = level, question = q.text, category = q.category, truth = q.truth };
        }
        static List<XgCard> Holdout(IEnumerable<XgCard> candidates, HashSet<string> trainingProblems, int wanted)
        {
            var used = new HashSet<string>(); var held = new List<XgCard>();
            foreach (var card in candidates)
            {
                if (trainingProblems.Contains(ProblemKey(card)) || !used.Add(VisibleKey(card))) continue;
                held.Add(card); if (held.Count == wanted) break;
            }
            Assert.AreEqual(wanted, held.Count, "insufficient unseen inputs: fail instead of filling with training duplicates");
            Assert.IsTrue(held.Any(c => c.truth) && held.Any(c => !c.truth), "both labels required; no label-based resampling");
            return held;
        }
        static Metrics Evaluate(XgBoard board, XgKnobs knobs, List<XgCard> held, int salt, string scope, int trained, bool cases)
        {
            long before = board.S.cards; var result = new Metrics();
            foreach (var c in held)
            {
                bool prediction = board.Predict(XgMoleInference.Input(c), knobs, out bool guessed);
                if (c.truth) { if (prediction) result.tp++; else result.fn++; }
                else { if (prediction) result.fp++; else result.tn++; }
                if (guessed) result.guessed++;
                if (cases) TestContext.WriteLine("case\t" + salt + "\t" + scope + "\t" + c.category + "\t" + c.question + "\t" + c.truth + "\t" + prediction + "\t" + guessed);
            }
            Assert.AreEqual(before, board.S.cards, "holdout evaluation must never train the model");
            TestContext.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "metric\t{0}\t{1}\t{2}\tn={3}\ttp={4}\ttn={5}\tfp={6}\tfn={7}\tguess={8}\taccuracy={9:F6}\tbalanced={10:F6}",
                salt, scope, trained, result.Count, result.tp, result.tn, result.fp, result.fn, result.guessed, result.Accuracy, result.Balanced));
            return result;
        }

        [Test, Explicit("Measure actual held-out learning; may fail. Fixed recipe, no tuning against these evaluation rows.")]
        public void FrozenUnseenClaimsBeforeAndAfterRealBoardTraining()
        {
            RunFrozenRecipe(new[] { 17, 31, 47 });
        }

        [Test, Explicit("Independent second seed set fixed in Design/算术第二组留出验证_预注册_20261019.md before any samples/metrics were inspected.")]
        public void PreregisteredSecondUnseenSetBeforeAndAfterRealBoardTraining()
        {
            RunFrozenRecipe(new[] { 73, 89, 107 });
        }

        static void RunFrozenRecipe(int[] salts)
        {
            double gain = 0, moleGain = 0;
            // Fixed a priori. No search over seeds, capacities, pass thresholds or hyperparameters.
            foreach (int salt in salts)
            {
                var train = Enumerable.Range(0, 640).Select(i => Generated(i, salt, XgBoardData.Use.Train)).ToList();
                var forbidden = new HashSet<string>(train.Select(ProblemKey));
                var held = Holdout(Enumerable.Range(0, 5120).Select(i => Generated(i, salt, XgBoardData.Use.Exam)), forbidden, 200);
                var moleQuestions = Enumerable.Range(0, 5120).Select(i => XgMoleArithmetic.Generate(7919 * i + salt, 3)).ToList();
                var kinds = moleQuestions.GroupBy(q => q.Text).ToDictionary(g => g.Key, g => g.First().Kind);
                var mole = Holdout(moleQuestions.Select(q => q.Card()), forbidden, 128);
                var basicMole = mole.Where(c => (int)kinds[c.question] <= (int)XgMoleMathKind.Precedence).ToList();
                Assert.IsTrue(basicMole.Any(c => c.truth) && basicMole.Any(c => !c.truth), "fixed basic mole slice needs both labels");
                // Full split is frozen before any learning. Report hashes for reproducibility and audit.
                TestContext.WriteLine("split\t" + salt + "\ttrain=" + Digest(train) + "\theld=" + Digest(held) + "\tmole=" + Digest(mole));
                foreach (var c in train) TestContext.WriteLine("train\t" + salt + "\t" + c.question + "\t" + c.truth);
                var board = new XgBoard();
                var knobs = new XgKnobs { depth = 3, width = 64, activation = XgActivation.Sigmoid, wiring = XgWiring.Full, lr = .1, clip = true };
                var before = Evaluate(board, knobs, held, salt, "existing-generator-unseen", 0, true);
                var moleBefore = Evaluate(board, knobs, basicMole, salt, "basic-mole-unseen", 0, false);
                Evaluate(board, knobs, mole, salt, "mole-transfer-including-untrained-calculus", 0, true);
                foreach (var group in mole.GroupBy(c => kinds[c.question])) Evaluate(board, knobs, group.ToList(), salt, "kind:" + group.Key, 0, false);
                int[] checkpoints = { 128, 320, 640 };
                int at = 0;
                foreach (int checkpoint in checkpoints)
                {
                    while (at < checkpoint)
                    {
                        var c = train[at++];
                        // Same real Make -> FromCard -> Train route as the production arithmetic pool.
                        var input = XgBoardData.Make("arith", c.seed, c.level, 20160524);
                        Assert.AreEqual(c.truth, input.truth);
                        board.Train(input, knobs);
                    }
                    Evaluate(board, knobs, held, salt, "existing-generator-unseen", at, at == 640);
                }
                var after = Evaluate(board, knobs, held, salt, "final", at, false);
                Evaluate(board, knobs, mole, salt, "mole-transfer-including-untrained-calculus", at, true);
                var moleAfter = Evaluate(board, knobs, basicMole, salt, "basic-mole-unseen", at, false);
                foreach (var group in mole.GroupBy(c => kinds[c.question])) Evaluate(board, knobs, group.ToList(), salt, "kind:" + group.Key, at, false);
                gain += after.Balanced - before.Balanced;
                moleGain += moleAfter.Balanced - moleBefore.Balanced;
            }
            gain /= 3;
            moleGain /= 3;
            TestContext.WriteLine("growth\tmean_balanced_accuracy_gain=" + gain.ToString("F6", CultureInfo.InvariantCulture));
            TestContext.WriteLine("growth\tbasic_mole_mean_balanced_accuracy_gain=" + moleGain.ToString("F6", CultureInfo.InvariantCulture));
            Assert.IsTrue(gain >= .05 && moleGain >= .05,
                "Growth not demonstrated under the frozen recipe. Preserve the metrics/failure; do not tune the game evaluation or hide this result.");
        }
    }
}
