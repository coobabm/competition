using LingGuangV05.Core;
using NUnit.Framework;

namespace LingGuangV05.Tests
{
    public sealed class DesktopNotificationBudgetTests
    {
        [Test]
        public void FirstNoticeCanSoundImmediately()
        {
            Assert.IsTrue(new DesktopNotificationBudget().TrySound(0, false, false));
        }

        [Test]
        public void BurstSoundsAreLimitedButBoundaryIsAllowed()
        {
            var budget = new DesktopNotificationBudget();
            Assert.IsTrue(budget.TrySound(10, false, false));
            Assert.IsFalse(budget.TrySound(11.49, false, false));
            Assert.IsTrue(budget.TrySound(11.5, false, false));
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void MuteAndCutsceneDoNotSpendSoundBudget(bool muted, bool held)
        {
            var budget = new DesktopNotificationBudget();
            Assert.IsFalse(budget.TrySound(0, muted, held));
            Assert.IsTrue(budget.TrySound(0, false, false));
        }

        [Test]
        public void PopupWaitsForTrayAndCutsceneWithoutSpendingBudget()
        {
            var budget = new DesktopNotificationBudget();
            Assert.IsFalse(budget.TryPopup(0, true, false));
            Assert.IsFalse(budget.TryPopup(0, false, true));
            Assert.IsTrue(budget.TryPopup(0, false, false));
            Assert.IsFalse(budget.TryPopup(7.49, false, false));
            Assert.IsTrue(budget.TryPopup(7.5, false, false));
        }

        [Test]
        public void NewSaveResetsBothBudgets()
        {
            var budget = new DesktopNotificationBudget();
            budget.TrySound(100, false, false);
            budget.TryPopup(100, false, false);
            budget.Reset();
            Assert.IsTrue(budget.TrySound(0, false, false));
            Assert.IsTrue(budget.TryPopup(0, false, false));
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void NonFiniteClockDoesNotSpendOrPoisonBudgets(double now)
        {
            var budget = new DesktopNotificationBudget();
            Assert.IsFalse(budget.TrySound(now, false, false));
            Assert.IsFalse(budget.TryPopup(now, false, false));
            Assert.IsTrue(budget.TrySound(0, false, false));
            Assert.IsTrue(budget.TryPopup(0, false, false));
        }
    }
}
