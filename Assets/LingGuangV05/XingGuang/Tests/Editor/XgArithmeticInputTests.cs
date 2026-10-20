using System;
using System.Linq;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgArithmeticInputTests
    {
        sealed class Host : IXgHost
        {
            public double Money { get; private set; } = 100000;
            public double Compute => 10;
            public double VramMB => 4096;
            public string Blocker => null;
            public bool Spend(double amount) { if (Money < amount) return false; Money -= amount; return true; }
            public void Earn(double amount) { Money += amount; }
            public void Train(double seconds) { }
        }
        static string[] Features(XgBoardCard b) => b.features.Select(f => f.name + "|" + f.x + "|" + f.y + "|" + f.seq).ToArray();
        static void SameInput(XgBoardCard a, XgBoardCard b)
        { CollectionAssert.AreEqual(Features(a), Features(b)); Assert.AreEqual(a.seed, b.seed); Assert.AreEqual(a.region, b.region); }

        [Test] public void LabelAndGeneratorMetadataCannotChangeArithmeticInputOrPrediction()
        {
            var sim = new XgSim();
            for (int i = 0; i < 48; i++)
            {
                var card = XgMoleArithmetic.Generate(i * 7907, 3).Card();
                var input = XgArithmeticInput.Input(card); var supervised = XgBoardData.FromCard(card);
                var before = sim.PredictMole(card);
                SameInput(input, supervised);
                Assert.AreEqual(card.truth, supervised.truth);
                Assert.IsNull(input.source, "raw inference must not carry a labelled source card");
                card.truth = !card.truth; card.seed = 99887; card.level = 99;
                card.why = "PRIVATE_ANSWER"; card.whyEn = "PRIVATE_ANSWER"; card.questionEn = "different localization";
                SameInput(input, XgBoardData.FromCard(card));
                SameInput(input, XgMoleInference.Input(card));
                var after = sim.PredictMole(card);
                Assert.AreEqual(before.AnswerYes, after.AnswerYes); Assert.AreEqual(before.Score, after.Score);
                Assert.AreEqual(before.Guessed, after.Guessed);
                Assert.IsFalse(input.features.Any(f => f.name.Contains("验算") || f.name.Contains("看着") || f.name.Contains("PRIVATE")));
            }
        }

        [Test] public void SpacesAndEquivalentOperatorGlyphsSharePositionsAndColdSeed()
        {
            var a = new XgCard { dataset = "arith", category = "乘法", question = " 4 × 5 = 20 " };
            var b = new XgCard { dataset = "arith", category = "乘法", question = "4*5=20" };
            SameInput(XgBoardData.FromCard(a), XgMoleInference.Input(b));
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4 }, XgArithmeticInput.Input(a).features.Skip(1).Select(f => f.x));
            b.question = "5*4=20";
            CollectionAssert.AreNotEqual(Features(XgArithmeticInput.Input(a)), Features(XgArithmeticInput.Input(b)), "operand order remains visible");
            a.question = "1 2+3=15"; b.question = "12+3=15";
            CollectionAssert.AreNotEqual(Features(XgArithmeticInput.Input(a)), Features(XgArithmeticInput.Input(b)), "whitespace must not join separate numeric tokens");
        }

        [Test] public void RealTrainingPoolUsesTheSameVisibleEncodingAndKeepsItsOriginalLabels()
        {
            for (int level = 1; level <= 5; level++) for (int i = 0; i < 12; i++)
            {
                int seed = XgBoardData.Seed("arith", i, XgBoardData.Use.Train, 31);
                var generated = XgArith.Generate(seed, level);
                var pool = XgBoardData.Make("arith", seed, level, 20160524);
                var visible = new XgCard { dataset = "arith", question = generated.text, category = generated.category };
                SameInput(pool, XgMoleInference.Input(visible));
                Assert.AreEqual(generated.truth, pool.truth);
                Assert.AreEqual(generated.text, pool.text);
            }
        }

        [Test] public void ActualAcceptedTeachingUpdatesSymbolsUsedByAutomaticReadout()
        {
            var sim = new XgSim { GoldChance = 0 }; var host = new Host();
            var card = sim.Card("arith"); var input = XgArithmeticInput.Input(card);
            long count = sim.S.board.cards;
            Assert.IsTrue(sim.TryAnswerStatement("arith", card.id, card.truth, host, out var answer));
            Assert.IsTrue(answer.correct); Assert.AreEqual(count + 1, sim.S.board.cards);
            Assert.IsTrue(sim.S.board.concepts.Any(c => c.key.Contains("算符:")), "genuine teaching creates symbol concepts, not just a category vote");
            var knobs = sim.Knobs(sim.Run(XgCatalog.Dataset("arith").track));
            sim.Board.Predict(input, knobs, out _, out var direct);
            var automatic = sim.PredictMole(card);
            Assert.AreEqual(direct.AnswerYes, automatic.AnswerYes); Assert.AreEqual(direct.Score, automatic.Score);
            Assert.AreEqual(count + 1, sim.S.board.cards, "automatic reading must not self-train");
        }

        [Test] public void SupervisionTargetChangesLearningButNeverTheInputs()
        {
            var card = new XgCard { dataset = "arith", category = "加减", question = "4+5=9" };
            var yes = XgBoardData.FromCard(card); var no = XgBoardData.FromCard(card);
            yes.truth = true; no.truth = false; SameInput(yes, no);
            var a = new XgBoard(); var b = new XgBoard(); var k = new XgKnobs { width = 32, lr = .1 };
            a.Train(yes, k); b.Train(no, k);
            a.Predict(XgArithmeticInput.Input(card), k, out _, out var p);
            b.Predict(XgArithmeticInput.Input(card), k, out _, out var q);
            Assert.Greater(p.Score, q.Score, "the real training rule receives the external label; no solver fixes the answer");
        }
    }
}
