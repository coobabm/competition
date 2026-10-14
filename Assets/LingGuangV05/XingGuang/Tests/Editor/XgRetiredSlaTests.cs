using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgRetiredSlaTests
    {
        sealed class Host : IXgHost
        {
            public double money = 123;
            public double Compute => 0;
            public double VramMB => 16000;
            public double Money => money;
            public string Blocker => null;
            public bool Spend(double amount)
            {
                if (amount < 0 || amount > money) return false;
                money -= amount;
                return true;
            }
            public void Earn(double amount) { money += amount; }
            public void Train(double seconds) { }
        }

        [Test]
        public void RetiredHighPriceOrdersHaveNoCatalogOrSigningApi()
        {
            Assert.IsNull(typeof(XgSim).GetField("SlaOffers"));
            Assert.IsNull(typeof(XgSim).GetMethod("SignSla"));
        }

        [Test]
        public void LegacyEarnedEscrowIsReleasedOnceAcrossReloads()
        {
            var sim = new XgSim(new XgState { slaHeld = 47.5, totalIncome = 100, fundedAbility = XgSim.AbilityCount });
            var host = new Host();
            sim.Tick(.1, host);
            Assert.AreEqual(170.5, host.money, 1e-9);
            Assert.AreEqual(147.5, sim.S.totalIncome, 1e-9);
            Assert.AreEqual(0, sim.S.slaHeld);

            var reloaded = new XgSim(sim.S);
            reloaded.Tick(1, host);
            Assert.AreEqual(170.5, host.money, 1e-9, "retired work must not keep earning or pay escrow twice");
        }

        [TestCase(-5)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void InvalidLegacyEscrowCannotChangeTheWallet(double held)
        {
            var sim = new XgSim(new XgState { slaHeld = held, fundedAbility = XgSim.AbilityCount });
            var host = new Host();
            sim.Tick(.1, host);
            Assert.AreEqual(123, host.money, 1e-9);
            Assert.AreEqual(0, sim.S.slaHeld);
        }
    }
}
