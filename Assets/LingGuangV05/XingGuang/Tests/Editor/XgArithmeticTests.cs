using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>
    /// The 算术 desk. Every generated claim is recomputed here from its formula with an evaluator of its own (exact
    /// fractions, × and ÷ before + and −), and the formula is tied to the numbers the player actually reads.
    /// </summary>
    public sealed class XgArithmeticTests
    {
        sealed class Host : IXgHost
        {
            public double money;
            public double Compute => 1;
            public double VramMB => 2048;
            public double Money => money;
            public bool Spend(double a) { if (money < a) return false; money -= a; return true; }
            public void Earn(double a) { money += a; }
            public void Train(double s) { }
            public string Blocker => null;
        }

        const int Seeds = 4000;
        static XgLogicQuestion Q(int seed, int level) => XgArith.Generate(seed * 7919 + level, level);

        [Test]
        public void EveryGeneratedClaimIsRecomputedIndependently()
        {
            for (int level = 1; level <= XgArith.MaxLevel; level++)
                for (int seed = 0; seed < Seeds; seed++)
                {
                    var q = Q(seed, level);
                    string where = "L" + level + " " + q.text + " | " + q.check;
                    Assert.IsFalse(string.IsNullOrEmpty(q.text) || string.IsNullOrEmpty(q.textEn) || string.IsNullOrEmpty(q.why) || string.IsNullOrEmpty(q.whyEn) || string.IsNullOrEmpty(q.category), where);
                    Assert.AreEqual(level, q.level);
                    Assert.AreEqual(q.truth, Holds(q.check, out var real), where);
                    // The formula is what the player reads: every number on the card is in it, and so is the claimed value.
                    var inCheck = new HashSet<string>(Numbers(q.check));
                    foreach (var n in Numbers(q.text)) Assert.IsTrue(inCheck.Contains(n), where + " (" + n + " is not in the formula)");
                    // The reason names the right result whenever there is one.
                    if (real != null) StringAssert.Contains(real, q.why, where);
                }
        }

        [Test]
        public void YesAndNoAreBalancedOnEveryLevel()
        {
            for (int level = 1; level <= XgArith.MaxLevel; level++)
            {
                int yes = 0;
                for (int seed = 0; seed < Seeds; seed++) if (Q(seed, level).truth) yes++;
                Assert.AreEqual(.5, (double)yes / Seeds, .06, "level " + level);
            }
        }

        [Test]
        public void WrongClaimsArePlausibleSlips()
        {
            var gaps = new HashSet<long>();
            int swapped = 0;
            for (int level = 1; level <= XgArith.MaxLevel; level++)
                for (int seed = 0; seed < Seeds; seed++)
                {
                    var q = Q(seed, level);
                    if (q.truth || q.check.StartsWith("seq", StringComparison.Ordinal) || char.IsLetter(q.check[0]) || q.check.Contains("%")) continue;
                    var sides = q.check.Split(new[] { " == " }, StringSplitOptions.None);
                    var real = Eval(sides[0]); var shown = Eval(sides[1]);
                    Assert.IsTrue(shown.n >= 0, q.text);
                    double a = (double)real.n / real.d, b = (double)shown.n / shown.d;
                    if (a >= 10 && b > 0) Assert.That(b / a, Is.InRange(.09, 10.01), q.text + " is not a slip anyone would make");
                    if (level == 2 && real.d == 1 && shown.d == 1)
                    {
                        gaps.Add(Math.Abs(shown.n - real.n));
                        if (shown.n != real.n && Reverse(shown.n) == real.n) swapped++;
                    }
                }
            Assert.That(gaps, Is.SupersetOf(new long[] { 1, 2, 10 }), "off by one, two and ten (a forgotten carry)");
            Assert.Greater(swapped, 10, "swapped digits");
        }

        [Test]
        public void LevelsFollowTheTable()
        {
            var kinds = new HashSet<string>[XgArith.MaxLevel + 1];
            for (int level = 1; level <= XgArith.MaxLevel; level++)
            {
                kinds[level] = new HashSet<string>();
                for (int seed = 0; seed < Seeds; seed++) kinds[level].Add(Q(seed, level).category);
            }
            CollectionAssert.AreEquivalent(new[] { "加减" }, kinds[1]);
            for (int seed = 0; seed < Seeds; seed++)
                foreach (var n in Numbers(Q(seed, 1).text)) Assert.LessOrEqual(long.Parse(n, CultureInfo.InvariantCulture), 21, "level 1 stays within 20 (a wrong 20 + 1 aside)");
            Assert.That(kinds[2], Is.EquivalentTo(new[] { "加减", "乘法", "应用题" }));
            Assert.That(kinds[3], Is.SupersetOf(new[] { "乘法", "除法", "奇偶", "整除", "找规律" }));
            Assert.That(kinds[4], Is.SupersetOf(new[] { "混合运算", "2 的幂", "质数" }));
            Assert.That(kinds[5], Is.SupersetOf(new[] { "百分数", "分数", "平方" }));
        }

        [Test]
        public void SameSeedSameQuestion()
        {
            for (int level = 1; level <= XgArith.MaxLevel; level++)
            {
                var a = XgArith.Generate(12345, level); var b = XgArith.Generate(12345, level);
                Assert.AreEqual(a.text, b.text); Assert.AreEqual(a.truth, b.truth); Assert.AreEqual(a.check, b.check);
            }
        }

        [Test]
        public void TheEvaluatorItselfIsRight()
        {
            Assert.IsTrue(Holds("3 + 4 * 2 == 11", out _));
            Assert.IsFalse(Holds("3 + 4 * 2 == 14", out _));
            Assert.IsTrue(Holds("1/2 + 1/3 == 5/6", out var r) && r == "5/6");
            Assert.IsTrue(Holds("80 * 15 / 100 == 12", out _));
            Assert.IsTrue(Holds("2 ^ 10 == 1024", out _));
            Assert.IsFalse(Holds("prime(91) == 1", out _));
            Assert.IsTrue(Holds("pow2(512) == 1", out _));
            Assert.IsTrue(Holds("(137 + 254) % 2 == 1", out _));
            Assert.IsTrue(Holds("seq add 4 | 3 7 11 15 | 19", out _));
            Assert.IsFalse(Holds("seq fib | 1 2 3 5 8 | 12", out _));
        }

        // ───────────── the desk ─────────────

        [Test]
        public void ArithmeticDeskOpensFreeAtStageOneAndPaysLeast()
        {
            var sim = new XgSim { GoldChance = 0 }; var host = new Host();
            Assert.IsTrue(sim.DeskOpen("arith"), "the approved free stage-one arithmetic mole opens this dataset");
            Assert.IsTrue(sim.ProgressionDeskAvailable("arith"));
            Assert.IsTrue(sim.DeskOpen("logic") && sim.DeskOpen("spam"));
            Assert.AreEqual(XgMemes.SenseDesk, sim.S.desk, "a new player starts on 常识判断, not on arithmetic");
            Assert.IsTrue(sim.TeachingPackAvailable("arith_mole"));
            Assert.IsTrue(sim.ProgressionDeskAvailable("arith"));
            for (int i = 0; i < 40; i++)
            {
                var card = sim.Card("arith");
                Assert.IsFalse(string.IsNullOrEmpty(card.question));
                Assert.IsTrue(sim.Answer("arith", card.truth, host).correct);
            }
            Assert.AreEqual(2, sim.LevelOf("arith"), "one level per 40 labels");
            Assert.AreEqual(40, sim.Samples("arith"), 1e-9);
            Assert.AreEqual(0, sim.Samples("logic"), 1e-9, "arithmetic labels are not logic labels");
            // v4 §3.3 (2026-10-22): pay follows difficulty. 算术 now pays more than the quick text desks, a calculus card about 4× 常识.
            Assert.Greater(sim.PayFor("arith", 1), 2 * sim.PayFor("spam", 1), "arithmetic pays clearly more than SMS spam");
            Assert.Greater(sim.PayFor("arith", 1), 2 * sim.PayFor("sense", 1), "arithmetic pays at least twice common sense");
            Assert.That(XgCatalog.Desk("arith").pay * XgCatalog.CalculusPayFactor / XgCatalog.Desk("sense").pay, Is.InRange(3.0, 4.5), "a calculus card ≈ 3–4× 常识");
            Assert.Greater(XgCatalog.Desk("mnist").pay, XgCatalog.Desk("sense").pay, "picture desks pay more than common sense");
            Assert.Greater(sim.PayFor("logic", 1), 2 * sim.PayFor("spam", 1), "logic pays clearly more than the other hand desks");
            foreach (var d in XgCatalog.Desks) if (d.id != "logic") Assert.Greater(XgCatalog.Desk("logic").pay, d.pay, d.id);
        }

        [Test]
        public void OldSavesKeepTheirArithmeticDesk()
        {
            var state = new XgState(); state.desksOpen.AddRange(new[] { "arith", "logic", "spam" }); state.desk = "spam";
            var sim = new XgSim(state);
            Assert.IsTrue(sim.DeskOpen("arith"), "a desk the save already had stays open");
            Assert.IsTrue(sim.ProgressionDeskAvailable("arith"));
            Assert.AreEqual("spam", sim.S.desk, "the old selection stays");
        }

        [Test]
        public void ArithmeticSamplesCountHalfTowardTheDataBar()
        {
            var a = new XgSim(); a.S.labels.Add(new XgLabelCount { dataset = "arith", count = 1000 });
            var s = new XgSim(); s.S.labels.Add(new XgLabelCount { dataset = "spam", count = 1000 });
            Assert.AreEqual(1000, a.Samples("arith"), 1e-9, "its own model still trains on every sample");
            Assert.AreEqual(s.TrainedSamples * XgCatalog.Dataset("arith").dataWeight, a.TrainedSamples, 1e-6);
            Assert.AreEqual(.5, XgCatalog.Dataset("arith").dataWeight, 1e-9);
        }

        // ───────────── an evaluator of its own ─────────────

        struct R
        {
            public long n, d;
            public static R Of(long n, long d)
            {
                if (d == 0) throw new DivideByZeroException();
                if (d < 0) { n = -n; d = -d; }
                long g = G(Math.Abs(n), d); return new R { n = n / g, d = d / g };
            }
            static long G(long a, long b) { while (b != 0) { long t = a % b; a = b; b = t; } return a == 0 ? 1 : a; }
            public override string ToString() => d == 1 ? n.ToString(CultureInfo.InvariantCulture) : n.ToString(CultureInfo.InvariantCulture) + "/" + d.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Whether a formula holds; <paramref name="real"/> is the left side's value when it is a plain number.</summary>
        static bool Holds(string check, out string real)
        {
            real = null;
            if (check.StartsWith("seq ", StringComparison.Ordinal)) return Sequence(check, out real);
            var sides = check.Split(new[] { " == " }, StringSplitOptions.None);
            Assert.AreEqual(2, sides.Length, check);
            var left = Eval(sides[0]); var right = Eval(sides[1]);
            if (!char.IsLetter(sides[0][0]) && !sides[0].Contains("%")) real = left.ToString();
            return left.n == right.n && left.d == right.d;
        }

        static bool Sequence(string check, out string real)
        {
            var parts = check.Substring(4).Split('|');
            var rule = parts[0].Trim().Split(' ');
            var terms = new List<long>();
            foreach (var t in parts[1].Trim().Split(' ')) terms.Add(long.Parse(t, CultureInfo.InvariantCulture));
            long asked = long.Parse(parts[2].Trim(), CultureInfo.InvariantCulture);
            long Next(int i) // the term at index i from the ones before it
            {
                switch (rule[0])
                {
                    case "add": return terms[i - 1] + long.Parse(rule[1], CultureInfo.InvariantCulture);
                    case "mul": return terms[i - 1] * long.Parse(rule[1], CultureInfo.InvariantCulture);
                    case "sq": { long s = (long)Math.Round(Math.Sqrt(terms[i - 1])); Assert.AreEqual(s * s, terms[i - 1], check); return (s + 1) * (s + 1); }
                    case "fib": return terms[i - 1] + terms[i - 2];
                    case "alt": return terms[i - 1] + (i % 2 == 1 ? long.Parse(rule[1], CultureInfo.InvariantCulture) : -long.Parse(rule[2], CultureInfo.InvariantCulture));
                    default: Assert.Fail("rule " + rule[0]); return 0;
                }
            }
            for (int i = 2; i < terms.Count; i++) Assert.AreEqual(Next(i), terms[i], "the shown terms follow the rule: " + check);
            if (rule[0] != "fib") Assert.AreEqual(Next(1), terms[1], check);
            terms.Add(0);
            long next = Next(terms.Count - 1);
            real = next.ToString(CultureInfo.InvariantCulture);
            return asked == next;
        }

        static R Eval(string s) { int i = 0; var v = Sum(s, ref i); Skip(s, ref i); Assert.AreEqual(s.Length, i, "trailing input in " + s); return v; }

        static void Skip(string s, ref int i) { while (i < s.Length && s[i] == ' ') i++; }

        static R Sum(string s, ref int i)
        {
            var v = Product(s, ref i);
            for (Skip(s, ref i); i < s.Length && (s[i] == '+' || s[i] == '-'); Skip(s, ref i))
            {
                char op = s[i++]; var w = Product(s, ref i);
                v = op == '+' ? R.Of(v.n * w.d + w.n * v.d, v.d * w.d) : R.Of(v.n * w.d - w.n * v.d, v.d * w.d);
            }
            return v;
        }

        static R Product(string s, ref int i)
        {
            var v = Power(s, ref i);
            for (Skip(s, ref i); i < s.Length && (s[i] == '*' || s[i] == '/' || s[i] == '%'); Skip(s, ref i))
            {
                char op = s[i++]; var w = Power(s, ref i);
                if (op == '*') v = R.Of(v.n * w.n, v.d * w.d);
                else if (op == '/') v = R.Of(v.n * w.d, v.d * w.n);
                else { Assert.IsTrue(v.d == 1 && w.d == 1, "% on whole numbers"); v = R.Of(v.n % w.n, 1); }
            }
            return v;
        }

        static R Power(string s, ref int i)
        {
            var v = Atom(s, ref i);
            Skip(s, ref i);
            if (i < s.Length && s[i] == '^')
            {
                i++; var e = Atom(s, ref i);
                long p = 1; for (long k = 0; k < e.n; k++) p *= v.n;
                v = R.Of(p, 1);
            }
            return v;
        }

        static R Atom(string s, ref int i)
        {
            Skip(s, ref i);
            if (s[i] == '(') { i++; var v = Sum(s, ref i); Skip(s, ref i); Assert.AreEqual(')', s[i], s); i++; return v; }
            if (char.IsLetter(s[i]))
            {
                int start = i; while (char.IsLetterOrDigit(s[i])) i++;
                string name = s.Substring(start, i - start);
                Assert.AreEqual('(', s[i], s); i++;
                long x = Sum(s, ref i).n; Skip(s, ref i); Assert.AreEqual(')', s[i], s); i++;
                bool yes;
                switch (name)
                {
                    case "prime": yes = x > 1; for (long d = 2; d * d <= x; d++) if (x % d == 0) yes = false; break;
                    case "pow2": yes = false; for (long p = 1; p <= x; p *= 2) if (p == x) yes = true; break;
                    case "square": yes = false; for (long k = 0; k * k <= x; k++) if (k * k == x) yes = true; break;
                    default: Assert.Fail("function " + name); yes = false; break;
                }
                return R.Of(yes ? 1 : 0, 1);
            }
            int from = i; while (i < s.Length && char.IsDigit(s[i])) i++;
            Assert.Greater(i, from, "a number at " + from + " in " + s);
            return R.Of(long.Parse(s.Substring(from, i - from), CultureInfo.InvariantCulture), 1);
        }

        static IEnumerable<string> Numbers(string s) { foreach (Match m in Regex.Matches(s, @"\d+")) yield return m.Value; }

        static long Reverse(long n) { if (n < 10) return n; long u = n % 10, t = n / 10 % 10; return n - u - t * 10 + u * 10 + t; }
    }
}
