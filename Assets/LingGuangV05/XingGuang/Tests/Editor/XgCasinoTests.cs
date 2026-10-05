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
            Assert.AreEqual(20, CasinoRules.ReturnMultiplier("slots", 0, new[] { 5, 5, 5 }));
            Assert.AreEqual(1, CasinoRules.ReturnMultiplier("slots", 0, new[] { 1, 2, 1 }));
            Assert.AreEqual(0, CasinoRules.ReturnMultiplier("slots", 0, new[] { 1, 2, 3 }));
            Assert.AreEqual(0, CasinoRules.ReturnMultiplier("dice", 0, new[] { 2, 2, 2 }));
            Assert.AreEqual(2, CasinoRules.ReturnMultiplier("dice", 0, new[] { 1, 2, 3 }));
            Assert.AreEqual(2, CasinoRules.ReturnMultiplier("dice", 1, new[] { 3, 4, 5 }));
            Assert.AreEqual(0, CasinoRules.ReturnMultiplier("roulette", 0, new[] { 0 }));
            Assert.AreEqual(2, CasinoRules.ReturnMultiplier("roulette", 0, new[] { 1 }));
            Assert.AreEqual(2, CasinoRules.ReturnMultiplier("roulette", 1, new[] { 2 }));
            Assert.AreEqual(2, CasinoRules.ReturnMultiplier("dragon", 0, new[] { 13, 1 }));
            Assert.AreEqual(0, CasinoRules.ReturnMultiplier("dragon", 1, new[] { 7, 7 }));
            Assert.AreEqual(0, CasinoRules.ReturnMultiplier("unknown", 0, new[] { 1 }));
        }

        [Test]
        public void ExhaustiveOutcomesMatchPublishedRules()
        {
            int slots = 0, small = 0, big = 0, red = 0, black = 0, dragon = 0;
            for (int a = 0; a < 6; a++) for (int b = 0; b < 6; b++) for (int c = 0; c < 6; c++)
            {
                slots += CasinoRules.ReturnMultiplier("slots", 0, new[] { a, b, c });
                small += CasinoRules.ReturnMultiplier("dice", 0, new[] { a + 1, b + 1, c + 1 });
                big += CasinoRules.ReturnMultiplier("dice", 1, new[] { a + 1, b + 1, c + 1 });
            }
            for (int n = 0; n <= 36; n++) { red += CasinoRules.ReturnMultiplier("roulette", 0, new[] { n }); black += CasinoRules.ReturnMultiplier("roulette", 1, new[] { n }); }
            for (int a = 1; a <= 13; a++) for (int b = 1; b <= 13; b++) dragon += CasinoRules.ReturnMultiplier("dragon", 0, new[] { a, b });
            Assert.AreEqual(210, slots); Assert.AreEqual(210, small); Assert.AreEqual(210, big);
            Assert.AreEqual(36, red); Assert.AreEqual(36, black); Assert.AreEqual(156, dragon);
        }

        [TestCase(0)][TestCase(-10)][TestCase(11)][TestCase(501)][TestCase(int.MaxValue)]
        public void UnsupportedStakesDoNothing(int stake)
        {
            var sim = new XgSim(); var host = new Host();
            Assert.IsFalse(sim.TryCasinoBet("dice", 0, stake, host, out _));
            Assert.AreEqual(10000, host.money); Assert.AreEqual(0, host.debits);
            Assert.AreEqual(0, sim.Casino.rounds);
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
