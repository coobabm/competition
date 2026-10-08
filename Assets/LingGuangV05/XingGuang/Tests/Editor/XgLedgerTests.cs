using System.Collections.Generic;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>The daily balance history behind 摆渡众包's 结算 sparkline.</summary>
    public sealed class XgLedgerTests
    {
        [Test]
        public void ADayIsRecordedOnceAndInOrder()
        {
            var sim = new XgSim();
            Assert.That(sim.BalanceHistory(), Is.Empty);
            Assert.That(sim.RecordDayBalance(10, 1200), Is.True);
            Assert.That(sim.RecordDayBalance(10, 999), Is.False, "same day twice");
            Assert.That(sim.RecordDayBalance(9, 999), Is.False, "an earlier day");
            Assert.That(sim.RecordDayBalance(11, -50), Is.True);
            Assert.That(sim.BalanceHistory(), Is.EqualTo(new List<double> { 1200, -50 }));
        }

        [Test]
        public void OnlyTheLastTenDaysAreKept()
        {
            var sim = new XgSim();
            for (int d = 1; d <= 15; d++) sim.RecordDayBalance(d, d * 100);
            var h = sim.BalanceHistory();
            Assert.That(h.Count, Is.EqualTo(XgSim.BalanceDaysKept));
            Assert.That(h[0], Is.EqualTo(600));
            Assert.That(h[h.Count - 1], Is.EqualTo(1500));
        }

        [Test]
        public void BadNumbersAreIgnoredAndAMissingListIsHarmless()
        {
            var sim = new XgSim();
            Assert.That(sim.RecordDayBalance(1, double.NaN), Is.False);
            Assert.That(sim.RecordDayBalance(1, double.PositiveInfinity), Is.False);
            sim.S.balanceDays = null; // what an odd save could leave behind
            Assert.That(sim.BalanceHistory(), Is.Empty);
            Assert.That(sim.RecordDayBalance(2, 5), Is.True);
            Assert.That(sim.BalanceHistory(), Is.EqualTo(new List<double> { 5 }));
        }
    }
}
