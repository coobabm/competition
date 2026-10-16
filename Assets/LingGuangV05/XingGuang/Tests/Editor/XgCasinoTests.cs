using System;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgCasinoTests
    {
        sealed class Host : IXgHost
        {
            public double money = 10000;
            public int debits, credits;
            public double Money => money;
            public double Compute => 1;
            public double VramMB => 1024;
            public string Blocker => null;
            public void Train(double seconds) { }
            public bool Spend(double amount) { if (money < amount) return false; money -= amount; debits++; return true; }
            public void Earn(double amount) { money += amount; credits++; }
        }

        [TestCase("888.vip")][TestCase(" HTTPS://WWW.888.VIP/ ")][TestCase("http://888.vip")]
        public void OnlyTheLocalHostIsAccepted(string address) => Assert.IsTrue(CasinoRules.IsAddress(address));
        [TestCase("888.vip.evil.com")][TestCase("https://888.vip@evil.com")][TestCase("not888.vip")][TestCase(null)]
        public void OtherHostsAreNotRouted(string address) => Assert.IsFalse(CasinoRules.IsAddress(address));

        [Test]
        public void PaytablesIncludeTheReturnedStakeAndExplicitLosingCases()
        {
            Assert.AreEqual(20, CasinoRules.ReturnMultiplier("slots", 0, new[] { 0, 0, 0 }), "three 7s");
            Assert.AreEqual(3, CasinoRules.ReturnMultiplier("slots", 0, new[] { 5, 5, 5 }), "any other three of a kind");
            Assert.AreEqual(2, CasinoRules.ReturnMultiplier("slots", 0, new[] { 1, 2, 1 }), "a pair now wins");
            Assert.AreEqual(0, CasinoRules.ReturnMultiplier("slots", 0, new[] { 1, 2, 3 }));
            Assert.AreEqual(0.5, CasinoRules.ReturnMultiplier("dice", 0, new[] { 2, 2, 2 }), "a triple refunds half, it no longer loses");
            Assert.AreEqual(0.5, CasinoRules.ReturnMultiplier("dice", 1, new[] { 5, 5, 5 }));
            Assert.AreEqual(2, CasinoRules.ReturnMultiplier("dice", 0, new[] { 1, 2, 3 }));
            Assert.AreEqual(2, CasinoRules.ReturnMultiplier("dice", 1, new[] { 3, 4, 5 }));
            Assert.AreEqual(0, CasinoRules.ReturnMultiplier("dice", 1, new[] { 1, 2, 3 }));
            Assert.AreEqual(0.5, CasinoRules.ReturnMultiplier("roulette", 0, new[] { 0 }), "zero refunds half");
            Assert.AreEqual(0.5, CasinoRules.ReturnMultiplier("roulette", 1, new[] { 0 }));
            Assert.AreEqual(2, CasinoRules.ReturnMultiplier("roulette", 0, new[] { 1 }));
            Assert.AreEqual(2, CasinoRules.ReturnMultiplier("roulette", 1, new[] { 2 }));
            Assert.AreEqual(0, CasinoRules.ReturnMultiplier("roulette", 1, new[] { 1 }));
            Assert.AreEqual(2, CasinoRules.ReturnMultiplier("dragon", 0, new[] { 13, 1 }));
            Assert.AreEqual(0.75, CasinoRules.ReturnMultiplier("dragon", 1, new[] { 7, 7 }), "a tie refunds 3/4");
            Assert.AreEqual(0, CasinoRules.ReturnMultiplier("dragon", 1, new[] { 13, 1 }));
            Assert.AreEqual(0, CasinoRules.ReturnMultiplier("unknown", 0, new[] { 1 }));
        }

        [Test]
        public void ExhaustiveReturnToPlayerMatchesTheDocumentedRtp()
        {
            // Sums of the gross multiplier over every equally likely outcome; RTP = sum / outcomes.
            double slots = 0, small = 0, big = 0, red = 0, black = 0, dragon = 0, dragonTiger = 0;
            int slotWins = 0, smallWins = 0, redWins = 0, dragonWins = 0;
            for (int a = 0; a < 6; a++) for (int b = 0; b < 6; b++) for (int c = 0; c < 6; c++)
            {
                double m = CasinoRules.ReturnMultiplier("slots", 0, new[] { a, b, c });
                slots += m; if (m > 1) slotWins++;
                double s = CasinoRules.ReturnMultiplier("dice", 0, new[] { a + 1, b + 1, c + 1 });
                small += s; if (s > 1) smallWins++;
                big += CasinoRules.ReturnMultiplier("dice", 1, new[] { a + 1, b + 1, c + 1 });
            }
            for (int n = 0; n <= 36; n++)
            {
                double r = CasinoRules.ReturnMultiplier("roulette", 0, new[] { n });
                red += r; if (r > 1) redWins++;
                black += CasinoRules.ReturnMultiplier("roulette", 1, new[] { n });
            }
            for (int a = 1; a <= 13; a++) for (int b = 1; b <= 13; b++)
            {
                double d = CasinoRules.ReturnMultiplier("dragon", 0, new[] { a, b });
                dragon += d; if (d > 1) dragonWins++;
                dragonTiger += CasinoRules.ReturnMultiplier("dragon", 1, new[] { a, b });
            }
            Assert.AreEqual(215, slots); Assert.AreEqual(215.0 / 216, slots / 216, 1e-12); Assert.AreEqual(96, slotWins);
            Assert.AreEqual(213, small); Assert.AreEqual(213, big); Assert.AreEqual(105, smallWins);
            Assert.AreEqual(36.5, red); Assert.AreEqual(36.5, black); Assert.AreEqual(18, redWins);
            Assert.AreEqual(165.75, dragon); Assert.AreEqual(165.75, dragonTiger); Assert.AreEqual(78, dragonWins);
            foreach (double rtp in new[] { slots / 216, small / 216, red / 37, dragon / 169 })
            { Assert.Greater(rtp, 0.98, "close to fair"); Assert.Less(rtp, 1.0, "the house keeps a small edge"); }
            Assert.Greater(smallWins / 216.0, 0.48); Assert.Greater(redWins / 37.0, 0.48); Assert.Greater(dragonWins / 169.0, 0.46);
        }

        [TestCase(0)][TestCase(-10)][TestCase(int.MaxValue)][TestCase(long.MaxValue)][TestCase(long.MinValue)]
        public void UnsupportedStakesDoNothing(long stake)
        {
            var sim = new XgSim(); var host = new Host();
            Assert.IsFalse(sim.TryCasinoBet("dice", 0, stake, host, out _));
            Assert.AreEqual(10000, host.money); Assert.AreEqual(0, host.debits);
            Assert.AreEqual(0, sim.Casino.rounds);
        }

        [TestCase("slots")][TestCase("dice")][TestCase("roulette")][TestCase("dragon")]
        public void AnyAmountUpToTheWholeWalletIsAcceptedAndMoreIsRejected(string game)
        {
            foreach (long stake in new long[] { 1, 7, 10, 501, 12345, 99999 })
            {
                var sim = new XgSim(); var host = new Host { money = 100000 };
                Assert.IsTrue(sim.TryCasinoBet(game, 0, stake, host, out var round), "stake " + stake);
                Assert.AreEqual(stake, round.stake);
                Assert.AreEqual(100000 - stake + round.returned, host.money);
            }
            // The whole wallet, all in.
            foreach (long wallet in new long[] { 1, 10, 777, 10000, 3_000_000_007 })
            {
                var sim = new XgSim(); var host = new Host { money = wallet };
                Assert.IsTrue(sim.TryCasinoBet(game, 0, wallet, host, out var round), "all in " + wallet);
                Assert.AreEqual(wallet - wallet + round.returned, host.money);
                Assert.AreEqual(1, host.debits);
            }
            // One yuan more than the wallet is refused and nothing moves.
            foreach (long wallet in new long[] { 1, 10, 5000, 3_000_000_007 })
            {
                var sim = new XgSim(); var host = new Host { money = wallet }; long rng = sim.Casino.rng;
                Assert.IsFalse(sim.TryCasinoBet(game, 0, wallet + 1, host, out _), "over wallet " + wallet);
                Assert.AreEqual(wallet, host.money); Assert.AreEqual(0, host.debits);
                Assert.AreEqual(0, sim.Casino.rounds); Assert.AreEqual(rng, sim.Casino.rng);
            }
            // A fractional balance can cover the whole-yuan part only.
            var frac = new XgSim(); var fracHost = new Host { money = 100.75 };
            Assert.IsFalse(frac.TryCasinoBet(game, 0, 101, fracHost, out _));
            Assert.IsTrue(frac.TryCasinoBet(game, 0, 100, fracHost, out _));
        }

        [Test]
        public void HugeStakesPayExactlyWithoutOverflow()
        {
            long stake = 40_000_000_000; // 20x of this would overflow a 32-bit int
            var sim = new XgSim(); var host = new Host { money = 5e12 };
            for (int i = 0; i < 50; i++)
            {
                sim.Clock = i * 2;
                Assert.IsTrue(sim.TryCasinoBet("slots", 0, stake, host, out var round));
                Assert.AreEqual(CasinoRules.ReturnQuarters("slots", 0, round.values) * stake / 4, round.returned);
                Assert.GreaterOrEqual(round.returned, 0);
            }
        }

        [Test]
        public void AllInHelperIsTheWholeYuanPartOfTheWalletAndGuardsOnlyArithmetic()
        {
            Assert.AreEqual(0, CasinoRules.AllIn(0)); Assert.AreEqual(0, CasinoRules.AllIn(0.99)); Assert.AreEqual(0, CasinoRules.AllIn(double.NaN));
            Assert.AreEqual(1, CasinoRules.AllIn(1)); Assert.AreEqual(12345, CasinoRules.AllIn(12345.99));
            Assert.AreEqual(CasinoRules.StakeCeiling, CasinoRules.AllIn(double.PositiveInfinity));
            Assert.IsTrue(CasinoRules.IsStake(1)); Assert.IsTrue(CasinoRules.IsStake(CasinoRules.StakeCeiling));
            Assert.IsFalse(CasinoRules.IsStake(0)); Assert.IsFalse(CasinoRules.IsStake(CasinoRules.StakeCeiling + 1));
        }

        [Test]
        public void BadChoiceAndNonFiniteOrInsufficientWalletDoNotAdvanceRng()
        {
            var sim = new XgSim(); var host = new Host(); long rng = sim.Casino.rng;
            Assert.IsFalse(sim.TryCasinoBet("dice", 2, 10, host, out _));
            Assert.IsFalse(sim.TryCasinoBet("oops", 0, 10, host, out _));
            foreach (var money in new[] { 9.0, double.NaN, double.PositiveInfinity })
            { host.money = money; Assert.IsFalse(sim.TryCasinoBet("dice", 0, 10, host, out _)); }
            Assert.AreEqual(rng, sim.Casino.rng); Assert.AreEqual(0, host.debits);
        }

        [TestCase("slots")][TestCase("dice")][TestCase("roulette")][TestCase("dragon")]
        public void SettlementHappensOnceAndReloadCannotClaimItAgain(string game)
        {
            var sim = new XgSim(); var host = new Host(); long trainingRng = sim.S.rng;
            Assert.IsTrue(sim.TryCasinoBet(game, 0, 50, host, out var round));
            Assert.AreEqual(10000 - 50 + round.returned, host.money);
            Assert.AreEqual(1, host.debits); Assert.AreEqual(trainingRng, sim.S.rng);
            Assert.IsFalse(sim.TryCasinoBet(game, 0, 50, host, out _));
            sim = new XgSim(sim.S) { Clock = 0.5 };
            Assert.IsFalse(sim.TryCasinoBet(game, 0, 50, host, out _));
            Assert.AreEqual(1, sim.Casino.rounds); Assert.AreEqual(1, host.debits);
            sim.Clock = 2;
            Assert.IsTrue(sim.TryCasinoBet(game, 0, 50, host, out _));
        }

        [Test]
        public void SessionsHaveWinsLossesAndBoundedHistory()
        {
            var sim = new XgSim(); var host = new Host(); bool won = false, lost = false;
            for (int i = 0; i < 200; i++)
            { sim.Clock = i * 2; Assert.IsTrue(sim.TryCasinoBet("roulette", 0, 10, host, out var r)); won |= r.returned > 10; lost |= r.returned == 0; }
            Assert.IsTrue(won); Assert.IsTrue(lost); Assert.AreEqual(8, sim.Casino.history.Count);
            Assert.AreEqual(200, sim.Casino.rounds); Assert.AreEqual(10000 - sim.Casino.wagered + sim.Casino.returned, host.money);
            Assert.AreEqual(0, new XgSim().Casino.rounds);
        }

        [Test]
        public void JsonRoundTripPreservesRngWalletCooldownHistoryAndAdvertisement()
        {
            var sim = new XgSim(); var host = new Host();
            Assert.IsTrue(sim.TryCasinoBet("slots", 0, 100, host, out _));
            sim.S.stage = 3; sim.AdvanceCasinoAd(120, false);
#if UNITY_5_3_OR_NEWER
            string json = UnityEngine.JsonUtility.ToJson(sim.S);
            var state = UnityEngine.JsonUtility.FromJson<XgState>(json);
#else
            var options = new System.Text.Json.JsonSerializerOptions { IncludeFields = true };
            string json = System.Text.Json.JsonSerializer.Serialize(sim.S, options);
            var state = System.Text.Json.JsonSerializer.Deserialize<XgState>(json, options);
#endif
            var restored = new XgSim(state); var otherHost = new Host { money = host.money };
            Assert.AreNotSame(sim.Casino, restored.Casino);
            Assert.AreNotSame(sim.Casino.history[0], restored.Casino.history[0]);
            Assert.IsTrue(restored.Casino.adVisible);
            Assert.IsFalse(restored.TryCasinoBet("dice", 0, 10, otherHost, out _));
            sim.Clock = restored.Clock = 2;
            Assert.IsTrue(sim.TryCasinoBet("dragon", 1, 50, host, out var a));
            Assert.IsTrue(restored.TryCasinoBet("dragon", 1, 50, otherHost, out var b));
            CollectionAssert.AreEqual(a.values, b.values);
            Assert.AreEqual(host.money, otherHost.money);
            Assert.AreEqual(sim.Casino.rng, restored.Casino.rng);
            Assert.AreEqual(sim.Casino.rounds, restored.Casino.rounds);
        }

        [Test]
        public void AdWaitsForMidgameStaysUntilCloseAndDoesNotStack()
        {
            var sim = new XgSim(); sim.S.stage = 2;
            sim.AdvanceCasinoAd(1000, false); Assert.IsFalse(sim.Casino.adVisible);
            sim.S.stage = 3;
            sim.AdvanceCasinoAd(119, false); Assert.IsFalse(sim.Casino.adVisible);
            sim.AdvanceCasinoAd(1, true); Assert.IsFalse(sim.Casino.adVisible, "story pauses the timer");
            sim.AdvanceCasinoAd(1, false); Assert.IsTrue(sim.Casino.adVisible);
            sim.AdvanceCasinoAd(9999, false); Assert.IsTrue(sim.Casino.adVisible, "no auto timeout");
            sim.CloseCasinoAd(); Assert.IsFalse(sim.Casino.adVisible);
            sim.AdvanceCasinoAd(299, false); Assert.IsFalse(sim.Casino.adVisible);
            sim.AdvanceCasinoAd(1, false); Assert.IsTrue(sim.Casino.adVisible);
        }
    }
}
