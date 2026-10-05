using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgLogicTests
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

        [Test]
        public void EveryLevelGeneratesWellFormedBalancedQuestions()
        {
            for (int level = 1; level <= XgLogic.MaxLevel; level++)
            {
                int yes = 0, n = 3000;
                for (int seed = 0; seed < n; seed++)
                {
                    var q = XgLogic.Generate(seed * 7919 + level, level);
                    Assert.IsFalse(string.IsNullOrEmpty(q.text) || string.IsNullOrEmpty(q.textEn) || string.IsNullOrEmpty(q.why) || string.IsNullOrEmpty(q.category), q.category + " " + q.text);
                    Assert.AreEqual(level, q.level);
                    if (q.truth) yes++;
                }
                Assert.AreEqual(.5, (double)yes / n, .1, "level " + level + " yes ratio");
            }
        }

        [Test]
        public void HarderKindsUnlockWithLevel()
        {
            var low = new HashSet<string>(); var high = new HashSet<string>();
            for (int seed = 0; seed < 2000; seed++) { low.Add(XgLogic.Generate(seed, 1).category); high.Add(XgLogic.Generate(seed, 5).category); }
            Assert.IsFalse(low.Contains("真话假话") || low.Contains("三段论") || low.Contains("条件推理"));
            Assert.That(high, Is.SupersetOf(new[] { "算术", "数列", "条件推理", "三段论", "真假运算", "日历", "真话假话", "比较", "数的性质",
                "否定", "必要条件", "或者推理", "换位推理", "逆否命题", "排除推理" }));
            Assert.That(low, Is.SupersetOf(new[] { "否定", "必要条件" }), "pure reasoning starts at level 1");
        }

        [Test]
        public void ReasoningKindsOutweighArithmeticAtTheFirstLevel()
        {
            int reasoning = 0, n = 3000;
            var kinds = new HashSet<string> { "否定", "必要条件", "比较" };
            for (int seed = 0; seed < n; seed++) if (kinds.Contains(XgLogic.Generate(seed, 1).category)) reasoning++;
            Assert.Greater(reasoning, n / 2, "the logic desk is mostly logic, not sums");
        }

        [Test]
        public void EliminationPuzzlesHaveOneAnswerThatMatchesTheTruth()
        {
            var said = new Regex(@"^(.+?)各(点了|坐在|用的是)(.+?)中的一个，互不相同。");
            int checkedCount = 0;
            for (int seed = 0; seed < 6000 && checkedCount < 150; seed++)
            {
                var q = XgLogic.Generate(seed, seed % 2 == 0 ? 4 : 5);
                if (q.category != "排除推理") continue;
                var m = said.Match(q.text);
                Assert.IsTrue(m.Success, q.text);
                var names = m.Groups[1].Value.Split('、'); var items = m.Groups[3].Value.Split('、');
                Assert.AreEqual(names.Length, items.Length, q.text);
                // The reason lists the only assignment; the asked pair is in it exactly when the answer is yes.
                string tail = q.text.Substring(q.text.LastIndexOf('。') + 1);
                bool inWhy = false;
                for (int i = 0; i < names.Length; i++) foreach (var item in items)
                        if (tail.StartsWith(names[i], StringComparison.Ordinal) && tail.Contains(item) && q.why.Contains(names[i] + "—" + item)) inWhy = true;
                Assert.AreEqual(q.truth, inWhy, q.text + " | " + q.why);
                checkedCount++;
            }
            Assert.Greater(checkedCount, 50);
        }

        [Test]
        public void SameSeedSameQuestion()
        {
            var a = XgLogic.Generate(12345, 4); var b = XgLogic.Generate(12345, 4);
            Assert.AreEqual(a.text, b.text); Assert.AreEqual(a.truth, b.truth);
        }

        [Test]
        public void ArithmeticAnswersCheckIndependently()
        {
            var pattern = new Regex(@"^(-?\d+) ([+−×]) (-?\d+) = (-?\d+) 吗？$");
            int checkedCount = 0;
            for (int seed = 0; seed < 4000; seed++)
            {
                var q = XgLogic.Generate(seed, 3);
                if (q.category != "算术") continue;
                var m = pattern.Match(q.text);
                Assert.IsTrue(m.Success, q.text);
                long a = long.Parse(m.Groups[1].Value), b = long.Parse(m.Groups[3].Value), c = long.Parse(m.Groups[4].Value);
                long real = m.Groups[2].Value == "+" ? a + b : m.Groups[2].Value == "−" ? a - b : a * b;
                Assert.AreEqual(real == c, q.truth, q.text);
                checkedCount++;
            }
            Assert.Greater(checkedCount, 100);
        }

        [Test]
        public void CalendarAnswersMatchTheRealCalendar()
        {
            var pattern = new Regex(@"2016 年 (\d+) 月 (\d+) 日是星期(.)吗？$");
            string days = "日一二三四五六";
            int checkedCount = 0;
            for (int seed = 0; seed < 6000; seed++)
            {
                var q = XgLogic.Generate(seed, 4);
                if (q.category != "日历") continue;
                var m = pattern.Match(q.text);
                Assert.IsTrue(m.Success, q.text);
                var date = new DateTime(2016, int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value));
                Assert.AreEqual((int)date.DayOfWeek == days.IndexOf(m.Groups[3].Value, StringComparison.Ordinal), q.truth, q.text);
                checkedCount++;
            }
            Assert.Greater(checkedCount, 50);
        }

        [Test]
        public void LogicDeskGetsHarderAndPaysMore()
        {
            var sim = new XgSim { GoldChance = 0 }; var host = new Host();
            Assert.AreEqual(1, sim.LogicLevel);
            double first = 0;
            for (int i = 0; i < 80; i++)
            {
                var card = sim.Card("logic");
                Assert.IsFalse(string.IsNullOrEmpty(card.question));
                double before = host.money;
                Assert.IsTrue(sim.Answer("logic", card.truth, host).correct);
                if (i == 0) first = host.money - before;
            }
            Assert.AreEqual(3, sim.LogicLevel, "one level per 40 labels");
            Assert.AreEqual(80, sim.Samples("logic"), 1e-9);
            sim.S.combo = 0;
            var c = sim.Card("logic");
            double b2 = host.money;
            sim.Answer("logic", c.truth, host);
            Assert.Greater(host.money - b2, first * 1.9, "level 3 pays double");
            Assert.AreEqual(0, sim.Samples("poems"), 1e-9, "logic labels do not count as poems");
        }

        [Test]
        public void GlobalAutoKeepsEachDesksCheckpointAndSamplesSeparate()
        {
            var state = new XgState(); state.desksOpen.AddRange(new[] { "mnist", "poems", "logic" });
            var sim = new XgSim(state); var host = new Host { money = 1000 };
            sim.S.best.Add(new XgBest { dataset = "logic", arch = "lstm", acc = .9 });
            sim.RevealAutoLabel();
            Assert.IsTrue(sim.BuyAuto("poems", host), "global upgrade may be bought from any open desk once one checkpoint qualifies");
            Assert.AreEqual(1, sim.AutoLevel("logic"));
            Assert.AreEqual(1, sim.AutoLevel("poems"));
            sim.Tick(100, host);
            Assert.Greater(sim.Samples("logic"), 15);
            Assert.AreEqual(0, sim.Samples("poems"), 1e-9);
        }
    }
}
