using System.Linq;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgArithmeticRelationsTests
    {
        static XgCard Card(string claim, bool target = false) => new XgCard { dataset = "arith", question = claim, truth = target };
        static string[] Keys(XgCard card, XgKnobs k) => XgBoard.Elements(XgArithmeticInput.Input(card), k).Select(e => e.id).ToArray();

        [Test] public void VisibleEncodingAndColdSeedStayIdenticalToTheFrozenTokenizer()
        {
            var card = Card("4+5=9"); var input = XgArithmeticInput.Input(card);
            Assert.AreEqual(6, input.features.Count);
            CollectionAssert.AreEqual(new[] { -1, 0, 1, 2, 3, 4 }, input.features.Select(f => f.x));
            uint hash = 2166136261;
            string canonical = "\n" + string.Join(";", input.features.Skip(1).Select(f => f.name)) + ";";
            foreach (char ch in canonical) { hash ^= ch; hash = unchecked(hash * 16777619); }
            Assert.AreEqual(unchecked((int)hash), input.seed, "no new cold baseline or split fingerprint");
        }

        [Test] public void LabelsExplanationsSeedsLevelsAndSourceCardsCannotChangeRelationInputs()
        {
            var k = new XgKnobs(); var c = Card("8÷6=4/3"); var before = Keys(c, k);
            c.truth = true; c.seed = 123456; c.level = 99; c.why = "PRIVATE"; c.whyEn = "PRIVATE";
            CollectionAssert.AreEqual(before, Keys(c, k));
            var input = XgBoardData.FromCard(c); input.source.truth = false; input.source.question = "999+999=1";
            CollectionAssert.AreEqual(before, XgBoard.Elements(input, k).Select(e => e.id));
            Assert.AreEqual(4, before.Count(x => x.EndsWith(":match") || x.EndsWith(":miss")), "all four relations, not a selected correct answer");
        }

        [Test] public void RelationalReadoutRequiresTrainingAndObeysReversedExternalSupervision()
        {
            var k = new XgKnobs { depth = 1, width = 64, lr = .1, clip = true };
            var normal = new XgBoard(); var reversed = new XgBoard();
            var held = XgArithmeticInput.Input(Card("41+17=58"));
            normal.Predict(held, k, out bool cold, out var trace);
            Assert.IsTrue(cold); Assert.AreEqual(0, trace.Score); Assert.AreEqual(0, normal.S.cards);
            for (int repeat = 0; repeat < 12; repeat++) foreach (var example in new[] { Card("4+5=9", true), Card("4+5=10", false), Card("12+7=19", true), Card("12+7=20", false) })
            {
                var a = XgBoardData.FromCard(example); var b = XgBoardData.FromCard(example); b.truth = !a.truth;
                normal.Train(a, k); reversed.Train(b, k);
            }
            normal.Predict(held, k, out _, out var learned); reversed.Predict(held, k, out _, out var inverted);
            Assert.Greater(learned.Score, 0); Assert.Less(inverted.Score, 0, "no symbolic solver can override learned external labels");
            long count = normal.S.cards; normal.Predict(held, k, out _); Assert.AreEqual(count, normal.S.cards);
        }

        [Test] public void UnknownSyntaxAndInvalidOrOversizedNumbersFallBackWithoutNumericClaims()
        {
            var k = new XgKnobs();
            foreach (string text in new[] { "(2+3)*4=20", "2+3*4=14", "√16=4", "lim[x→2]x=2", "1/0+2=3", "1000000001+2=1000000003", "1 2+3=15" })
                Assert.IsFalse(XgArithmeticRelations.TryElements(XgArithmeticInput.Input(Card(text)), k, out _), text);
            Assert.IsFalse(XgArithmeticRelations.TryElements(new XgBoardCard { text = "4+5=9" }, k, out _), "other datasets never enter numeric representation");
        }

        [Test] public void UnreducedRationalsAndNegativeValuesUseExactVisibleRelations()
        {
            var k = new XgKnobs();
            CollectionAssert.AreEqual(Keys(Card("8÷6=4/3"), k), Keys(Card("8/6=8/6"), k));
            Assert.IsTrue(Keys(Card("2−5=-3"), k).Any(x => x.EndsWith(":difference:match")));
            Assert.IsTrue(Keys(Card("9/0=1"), k).Any(x => x.EndsWith(":quotient:miss")), "undefined quotient is an observed missing relation, not a chosen answer");
            Assert.IsTrue(Keys(Card("1000000000+1000000000=1000000000"), k).Any(x => x.EndsWith(":sum:miss")));
        }
    }
}
