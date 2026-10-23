using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>赢棋奖钱 (玩家反馈 F8, v4 §3.7): a win against its own AI pays about a minute of income × its strength, three times a real day.</summary>
    public sealed class XgGameRewardTests
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

        [Test]
        public void TheRewardIsAMinuteOfIncomeTimesTheTierWithAFloor()
        {
            Assert.AreEqual(200, XgSim.GameRewardFor(XgGameOutcome.PlayerWon, 0, 0), 1e-9, "the floor");
            Assert.AreEqual(600, XgSim.GameRewardFor(XgGameOutcome.PlayerWon, 0, 10), 1e-9, "60 s of ¥10/s");
            Assert.AreEqual(900, XgSim.GameRewardFor(XgGameOutcome.PlayerWon, 1, 10), 1e-9, "×1.5");
            Assert.AreEqual(1500, XgSim.GameRewardFor(XgGameOutcome.PlayerWon, 2, 10), 1e-9, "×2.5");
            Assert.AreEqual(180, XgSim.GameRewardFor(XgGameOutcome.Draw, 0, 10), 1e-9, "a draw pays 30%");
            Assert.AreEqual(0, XgSim.GameRewardFor(XgGameOutcome.PlayerLost, 2, 10), 1e-9);
            Assert.AreEqual(200, XgSim.GameRewardFor(XgGameOutcome.PlayerWon, 0, double.NaN), 1e-9, "bad income never breaks it");
        }

        [Test]
        public void OnlyTheFirstThreePaidResultsOfADayPay()
        {
            var sim = new XgSim(); var host = new Host();
            for (int i = 1; i <= XgSim.GameRewardsPerDay; i++)
            {
                double before = host.money;
                var r = sim.ClaimGameReward(XgGameOutcome.PlayerWon, true, host, 20261022);
                Assert.AreEqual(i, r.paidToday);
                Assert.AreEqual(XgSim.GameRewardFloor, host.money - before, 1e-9);
            }
            double money = host.money;
            var capped = sim.ClaimGameReward(XgGameOutcome.PlayerWon, true, host, 20261022);
            Assert.IsTrue(capped.capped); Assert.AreEqual(0, capped.amount); Assert.AreEqual(money, host.money, 1e-9, "the fourth win only counts in the record");
            Assert.IsTrue(sim.ClaimGameReward(XgGameOutcome.PlayerWon, true, host, 20261021).capped, "turning the clock back does not open a new day");
            Assert.AreEqual(1, sim.ClaimGameReward(XgGameOutcome.PlayerWon, true, host, 20261023).paidToday, "the next day pays again");
        }

        [Test]
        public void LossesAndTheBuiltInProgramPayNothing()
        {
            var sim = new XgSim(); var host = new Host();
            Assert.AreEqual(0, sim.ClaimGameReward(XgGameOutcome.PlayerLost, true, host, 20261022).amount);
            Assert.AreEqual(0, sim.ClaimGameReward(XgGameOutcome.PlayerWon, false, host, 20261022).amount, "only its own AI counts");
            Assert.AreEqual(1000, host.money, 1e-9);
            Assert.AreEqual(XgSim.GameRewardsPerDay, sim.GameRewardsLeft(20261022), "nothing used up");
        }

        [Test]
        public void TheTierFollowsTheBoardLogicTheAiLearned()
        {
            var sim = new XgSim();
            Assert.AreEqual(0, sim.GameRewardTier);
            sim.S.unlocked.Add("board.logic2");
            Assert.AreEqual(1, sim.GameRewardTier);
            sim.S.unlocked.Add("board.logic4");
            Assert.AreEqual(2, sim.GameRewardTier);
        }

        [Test]
        public void PayFollowsDifficulty()
        {
            // 技能树与自动化 v4 §3.3: 高数/逻辑 > 识图 > 常识/垃圾短信, with every desk levelling at the same pace.
            double sense = XgCatalog.Desk("sense").pay;
            Assert.Greater(XgCatalog.Desk("logic").pay, XgCatalog.Desk("mnist").pay);
            Assert.Greater(XgCatalog.Desk("arith").pay, XgCatalog.Desk("mnist").pay);
            Assert.Greater(XgCatalog.Desk("mnist").pay, XgCatalog.Desk("spam").pay);
            Assert.GreaterOrEqual(XgCatalog.Desk("spam").pay, sense);
            Assert.That(XgCatalog.Desk("arith").pay * XgCatalog.CalculusPayFactor / sense, Is.InRange(2.0, 4.5), "高数 ≈ 2–4× 常识");
            foreach (var d in XgCatalog.Desks) Assert.AreEqual(40, XgSim.LabelsPerLevel(d.id), d.id);
        }
    }
}
