using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>
    /// The casino is fair: seeded 200k-round simulations through the real settlement path (XgSim.TryCasinoBet /
    /// StartBlackjack / PlayBlackjack) must land on the theoretical return to player documented on CasinoRules, so a hidden
    /// tilt (streak, balance or mood dependent) would fail here. The seed is fixed, so the numbers are reproducible.
    /// </summary>
    public sealed class XgCasinoFairnessTests
    {
        const int Rounds = 200000;
        const long Stake = 100;

        sealed class Host : IXgHost
        {
            public double money = 1e12;
            public double Money => money;
            public double Compute => 1;
            public double VramMB => 1024;
            public string Blocker => null;
            public void Train(double seconds) { }
            public bool Spend(double amount) { if (money < amount) return false; money -= amount; return true; }
            public void Earn(double amount) { money += amount; }
        }

        static XgSim Seeded(long seed) { var sim = new XgSim(); sim.Casino.rng = seed; return sim; }

        // game, theoretical RTP, theoretical win frequency (gross return above the stake)
        static IEnumerable<object[]> Games()
        {
            yield return new object[] { "slots", 215.0 / 216, 96.0 / 216 };
            yield return new object[] { "dice", 213.0 / 216, 105.0 / 216 };
            yield return new object[] { "roulette", 36.5 / 37, 18.0 / 37 };
            yield return new object[] { "dragon", 165.75 / 169, 78.0 / 169 };
        }

        [TestCaseSource(nameof(Games))]
        public void SimulatedReturnMatchesTheTheoreticalRtp(string game, double rtp, double winRate)
        {
            foreach (int choice in new[] { 0, 1 })
            {
                if (game == "slots" && choice == 1) continue;
                var sim = Seeded(20161005 + choice); var host = new Host();
                int wins = 0, losses = 0;
                for (int i = 0; i < Rounds; i++)
                {
                    sim.Clock = i * 2.0;
                    Assert.IsTrue(sim.TryCasinoBet(game, choice, Stake, host, out var round));
                    if (round.returned > round.stake) wins++; else if (round.returned == 0) losses++;
                }
                double measured = sim.Casino.returned / sim.Casino.wagered;
                Assert.AreEqual(Rounds * Stake, sim.Casino.wagered);
                TestContext.Out.WriteLine(game + " choice " + choice + " RTP " + measured + " (theory " + rtp + ") win " + wins / (double)Rounds);
                Assert.AreEqual(rtp, measured, 0.02, game + " choice " + choice + " RTP " + measured);
                Assert.AreEqual(winRate, wins / (double)Rounds, 0.01, game + " win rate");
                Assert.Greater(losses, 0);
                Assert.AreEqual(1e12 - sim.Casino.wagered + sim.Casino.returned, host.money, 1, "wallet matches the ledger");
            }
        }

        [Test]
        public void ReturnDoesNotDependOnTheWalletOrOnRecentResults()
        {
            // Same seed, same bets: a rich player and a nearly broke player see identical dice, and a streak never changes them.
            var rich = Seeded(777); var poor = Seeded(777);
            var richHost = new Host { money = 1e9 }; var poorHost = new Host { money = 1e9 };
            for (int i = 0; i < 2000; i++)
            {
                rich.Clock = poor.Clock = i * 2.0;
                Assert.IsTrue(rich.TryCasinoBet("roulette", i % 2, 10, richHost, out var a));
                Assert.IsTrue(poor.TryCasinoBet("roulette", i % 2, 10, poorHost, out var b));
                CollectionAssert.AreEqual(a.values, b.values);
                if (i == 1000) { poorHost.money = 5000; poor.Casino.history.Clear(); }
            }
        }

        [Test]
        public void EveryRouletteNumberAndDragonCardComesUpAtItsFairFrequency()
        {
            var sim = Seeded(31337); var host = new Host();
            var pockets = new int[37]; var cards = new int[14];
            const int n = 111000;
            for (int i = 0; i < n; i++)
            {
                sim.Clock = i * 4.0;
                Assert.IsTrue(sim.TryCasinoBet("roulette", 0, 1, host, out var r)); pockets[r.values[0]]++;
                if (i < 78000) { sim.Clock += 1.6; Assert.IsTrue(sim.TryCasinoBet("dragon", 0, 1, host, out var d)); cards[d.values[0]]++; cards[d.values[1]]++; }
            }
            for (int k = 0; k < 37; k++) Assert.AreEqual(n / 37.0, pockets[k], n / 37.0 * 0.08, "pocket " + k);
            for (int k = 1; k <= 13; k++) Assert.AreEqual(78000 * 2 / 13.0, cards[k], 78000 * 2 / 13.0 * 0.05, "card " + k);
        }

        // ───── blackjack: basic strategy (no split), the strongest play the table allows ─────

        static int Hard(IList<int> hand)
        {
            int total = 0;
            foreach (int card in hand) total += Math.Min(BlackjackRules.Rank(card), 10);
            return total;
        }

        /// <summary>"hit", "stand" or "double" from the player's hand and the dealer's up card (dealer stands on all 17s).</summary>
        static string BasicStrategy(IList<int> player, int upRank)
        {
            int up = upRank == 1 ? 11 : Math.Min(upRank, 10);
            int score = BlackjackRules.Score(player), hard = Hard(player);
            bool soft = score != hard, two = player.Count == 2;
            if (soft)
            {
                if (score >= 19) return "stand";
                if (score == 18) return up >= 3 && up <= 6 && two ? "double" : up == 2 || up == 7 || up == 8 ? "stand" : "hit";
                if (score == 17) return two && up >= 3 && up <= 6 ? "double" : "hit";
                if (score >= 15) return two && up >= 4 && up <= 6 ? "double" : "hit";
                return two && up >= 5 && up <= 6 ? "double" : "hit";
            }
            if (score >= 17) return "stand";
            if (score >= 13) return up <= 6 ? "stand" : "hit";
            if (score == 12) return up >= 4 && up <= 6 ? "stand" : "hit";
            if (score == 11) return two && up <= 10 ? "double" : "hit";
            if (score == 10) return two && up <= 9 ? "double" : "hit";
            if (score == 9) return two && up >= 3 && up <= 6 ? "double" : "hit";
            return "hit";
        }

        [Test]
        public void BlackjackWithBasicStrategyReturnsAboutNinetyNinePointFivePercent()
        {
            var sim = Seeded(21212121); var host = new Host();
            double clock = 0; int natural = 0, hands = 0, wins = 0;
            for (int i = 0; i < Rounds; i++)
            {
                sim.Clock = clock += 2;
                Assert.IsTrue(sim.StartBlackjack(Stake, host));
                var h = sim.Casino.blackjack;
                while (sim.BlackjackActive)
                {
                    sim.Clock = clock += 1;
                    string move = BasicStrategy(h.player, BlackjackRules.Rank(h.dealer[0]));
                    if (move == "double" && !sim.PlayBlackjack("double", host)) move = "hit";
                    if (move == "hit") Assert.IsTrue(sim.PlayBlackjack("hit", host));
                    else if (move == "stand") Assert.IsTrue(sim.PlayBlackjack("stand", host));
                }
                var last = sim.Casino.history[sim.Casino.history.Count - 1];
                hands++; if (last.returned > last.stake) wins++;
                if (BlackjackRules.Natural(h.player)) natural++;
            }
            double rtp = sim.Casino.returned / sim.Casino.wagered;
            Assert.AreEqual(Rounds, hands);
            TestContext.Out.WriteLine("blackjack RTP " + rtp + " win " + wins / (double)Rounds);
            Assert.AreEqual(0.995, rtp, 0.02, "blackjack RTP " + rtp);
            Assert.AreEqual(0.0483, natural / (double)Rounds, 0.005, "naturals ≈ 4.8 % of hands");
            Assert.Greater(wins / (double)Rounds, 0.40, "a normal blackjack win rate");
        }

        [Test]
        public void DealerNeverHitsSeventeenAndEveryHandUsesAFreshFullDeck()
        {
            var sim = Seeded(99); var host = new Host(); var firstCards = new HashSet<int>();
            for (int i = 0; i < 3000; i++)
            {
                sim.Clock = i * 4.0;
                Assert.IsTrue(sim.StartBlackjack(10, host));
                var h = sim.Casino.blackjack;
                Assert.AreEqual(52, new HashSet<int>(h.deck).Count);
                firstCards.Add(h.deck[0]);
                if (sim.BlackjackActive) { sim.Clock += 1; Assert.IsTrue(sim.PlayBlackjack("stand", host)); }
                int dealer = BlackjackRules.Score(h.dealer);
                if (BlackjackRules.Score(h.player) <= 21 && !BlackjackRules.Natural(h.dealer) && !BlackjackRules.Natural(h.player))
                    Assert.GreaterOrEqual(dealer, 17, "dealer draws to at least 17");
                if (h.dealer.Count > 2) Assert.Less(BlackjackRules.Score(h.dealer.GetRange(0, h.dealer.Count - 1)), 17, "never hits a made hand");
            }
            Assert.AreEqual(52, firstCards.Count, "every card opens a hand at some point (a real shuffle)");
        }
    }
}
