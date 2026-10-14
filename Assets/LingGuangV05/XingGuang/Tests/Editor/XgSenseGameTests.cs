using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>
    /// 开心辞典 on the 常识判断 desk: true/false statements from the desk's own bank, 求助灵光 answering with its real yes/no
    /// ability (the one the chat gate uses), and every answer labelling the sense dataset through the one MiniPay rule.
    /// </summary>
    public sealed class XgSenseGameTests
    {
        sealed class Host : IXgHost
        {
            public double Money { get; private set; } = 100000;
            public double Compute => 10;
            public double VramMB => 4096;
            public string Blocker => null;
            public void Earn(double amount) { Money += amount; }
            public bool Spend(double amount) { if (amount < 0 || Money < amount) return false; Money -= amount; return true; }
            public void Train(double seconds) { }
        }

        sealed class FixedRandom : Random
        {
            readonly double value;
            public FixedRandom(double value) { this.value = value; }
            public override double NextDouble() => value;
        }

        const int Today = 20160529;

        static void Train(XgSim sim, string dataset, double accuracy)
        {
            sim.S.best.RemoveAll(b => b.dataset == dataset);
            sim.S.best.Add(new XgBest { dataset = dataset, arch = "perceptron", acc = accuracy });
        }

        static XgSim StageOne() { var sim = new XgSim { GoldChance = 0 }; sim.S.stage = 1; return sim; }

        [Test]
        public void TheLadderAsksStatementsFromTheSenseBankInRisingLevels()
        {
            var bank = new HashSet<XgPhrase>(XgMemes.SenseBank);
            for (int game = 0; game < 30; game++)
            {
                var random = new Random(100 + game); var used = new HashSet<string>();
                for (int step = 0; step < XgMiniContent.SenseLadder; step++)
                {
                    var p = XgMiniContent.SenseStatement(random, step, Today, used);
                    Assert.IsNotNull(p);
                    Assert.IsTrue(bank.Contains(p), "straight from XgMemes.SenseBank");
                    Assert.LessOrEqual(p.level, XgMiniContent.SenseLevel(step), p.text + ": no harder than its rung");
                    Assert.LessOrEqual(p.since, Today, p.text + ": nothing dated after today");
                    Assert.IsTrue(used.Add(p.text), p.text + ": never asked twice in one game");
                    Assert.IsNotEmpty(p.textEn, "every statement has an English wording");
                }
            }
            Assert.AreEqual(1, XgMiniContent.SenseLevel(0)); Assert.AreEqual(1, XgMiniContent.SenseLevel(3));
            Assert.AreEqual(2, XgMiniContent.SenseLevel(4)); Assert.AreEqual(3, XgMiniContent.SenseLevel(8)); Assert.AreEqual(3, XgMiniContent.SenseLevel(11));
        }

        [Test]
        public void TheStatementsAreBothTrueAndFalse()
        {
            var random = new Random(5); int yes = 0, no = 0;
            for (int i = 0; i < 200; i++) { var p = XgMiniContent.SenseStatement(random, i % 12, Today, new HashSet<string>()); if (p.yes) yes++; else no++; }
            Assert.Greater(yes, 60); Assert.Greater(no, 60);
        }

        [Test]
        public void TheHelpIsOftenWrongWhileItIsStillGuessing()
        {
            var sim = StageOne();
            Assert.AreEqual(.5, sim.YesNoAccuracy, 1e-9, "untrained is a coin toss");
            var random = new Random(11); int wrong = 0, total = 0;
            foreach (var p in XgMemes.SenseBank) for (int i = 0; i < 10; i++) { total++; if (sim.SenseHelp(p, random) != p.yes) wrong++; }
            double rate = wrong / (double)total;
            Assert.That(rate, Is.InRange(.4, .6), "about half wrong at a coin toss: " + rate);

            Train(sim, "sense", .7);
            wrong = total = 0;
            foreach (var p in XgMemes.SenseBank) for (int i = 0; i < 10; i++) { total++; if (sim.SenseHelp(p, random) != p.yes) wrong++; }
            rate = wrong / (double)total;
            Assert.That(rate, Is.InRange(.2, .4), "about 30% wrong at 70%: " + rate);
        }

        [Test]
        public void OnceLearnedTheHelpIsRightOnEveryTaughtStatement()
        {
            var sim = StageOne();
            Train(sim, "sense", .8);
            var random = new Random(3);
            foreach (var p in XgMemes.SenseBank)
                for (int i = 0; i < 20; i++) Assert.AreEqual(p.yes, sim.SenseHelp(p, random), p.text);
            Train(sim, "sense", .95);
            Assert.AreEqual(true, sim.SenseHelp(XgMemes.SenseBank[0], new FixedRandom(.999)), "no roll flips it");
        }

        [Test]
        public void TheHelpFollowsTheSameRuleAsTheChatGate()
        {
            var sim = StageOne();
            var yes = new XgPhrase { text = "太阳从东边升起。", yes = true };
            var no = new XgPhrase { text = "鱼会爬树。", yes = false };
            foreach (double acc in new[] { .5, .65, .8, .95 })
            {
                Train(sim, "sense", acc);
                foreach (double roll in new[] { .1, .55, .75, .99 })
                {
                    foreach (var p in new[] { yes, no })
                    {
                        string reply = p.yes ? "是。" : "否。";
                        bool gate = sim.Gate(reply, p.text, new FixedRandom(roll)).StartsWith("是", StringComparison.Ordinal);
                        Assert.AreEqual(gate, sim.SenseHelp(p, new FixedRandom(roll)), "accuracy " + acc + ", roll " + roll + ", " + p.text);
                    }
                }
            }
        }

        [Test]
        public void EveryAnswerLabelsTheSenseDatasetAndAWrongOneDoesNot()
        {
            var sim = new XgSim(); var host = new Host();
            foreach (var d in XgCatalog.Desks) if (!sim.S.desksOpen.Contains(d.id)) sim.S.desksOpen.Add(d.id);
            Assert.AreEqual("kaixincidian", XgMiniGames.ForDesk("sense"));
            Assert.AreEqual("sense", XgMiniGames.Find("kaixincidian").Desk);
            long ticket = sim.BeginMiniGame("kaixincidian", host);
            Assert.AreNotEqual(0, ticket);
            double rows = sim.Labels("sense"), total = sim.TotalLabels, money = host.Money;
            var right = sim.SubmitMiniGame(ticket, 1, true, host, 3);
            Assert.IsTrue(right.accepted);
            Assert.AreEqual(rows + 1, sim.Labels("sense"), "a right answer is one sample of 常识判断");
            Assert.AreEqual(total + 1, sim.TotalLabels, "and of nothing else");
            Assert.AreEqual(sim.MiniBasePay("sense") + right.bonus, host.Money - money, 1e-6, "paid by the desk's own rule");
            var miss = sim.SubmitMiniGame(ticket, 2, false, host, 3);
            Assert.IsTrue(miss.accepted);
            Assert.AreEqual(rows + 1, sim.Labels("sense"), "a wrong answer adds no sample");
            Assert.AreEqual(0, miss.samples);
        }
    }
}
