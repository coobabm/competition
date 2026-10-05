using System.Linq;
using LingGuang.Core;
using NUnit.Framework;

namespace LingGuang.Tests
{
    public sealed class AutoSettlementTests
    {
        static RunState Ready(int light = 30)
        {
            var s = RunState.NewRun(new GameConfig(), 17);
            s.roundLight = light;
            s.startsLeft = 1;
            return s;
        }

        static bool ShouldSettle(RunState s) => s.ShouldAutoSettleRound;

        [TestCase(29, false)]
        [TestCase(30, true)]
        [TestCase(42, true)]
        public void SettlesAtTarget_EvenWithAStartRemaining(int light, bool expected)
        {
            var s = Ready(light);
            Assert.That(ShouldSettle(s), Is.EqualTo(expected));
            Assert.That(s.startsLeft, Is.EqualTo(1));
            Assert.That(s.history, Is.Empty, "Checking must not perform settlement.");
        }

        [Test]
        public void UsesFinalScoreIncludingMultiplier_NotRawLight()
        {
            var s = Ready(15); s.roundAddMult = 1;
            Assert.That(ShouldSettle(s), Is.True);
            s.effects.Add(new ActiveEffect { sourceId = "test", effect = new Effect(Op.SettleMultPercent, 50), roundsLeft = 1 });
            Assert.That(ShouldSettle(s), Is.False);
        }

        [Test]
        public void DoesNotForceALoss_WhenExtraGateRequirementIsUnmet()
        {
            var s = Ready(42);
            s.effects.Add(new ActiveEffect { sourceId = "test", effect = new Effect(Op.MinFiresRequired, 2), roundsLeft = 1 });
            Assert.That(ShouldSettle(s), Is.False);
            foreach (var node in s.nodes.Keys.Take(2)) s.firedNodesThisRound.Add(node);
            Assert.That(ShouldSettle(s), Is.True);
            Assert.That(s.Settle().score.passed, Is.True);
        }

        [Test]
        public void ExhaustedStartsStillSettleBelowTarget()
        {
            var s = Ready(0); s.startsLeft = 0;
            Assert.That(ShouldSettle(s), Is.True);
        }

        [TestCase(Phase.Playing)]
        [TestCase(Phase.RoundSettled)]
        [TestCase(Phase.GameOver)]
        [TestCase(Phase.Victory)]
        public void NeverSettlesAgainOrDuringSimulation(Phase phase)
        {
            var s = Ready(42); s.startsLeft = 0; s.phase = phase;
            Assert.That(ShouldSettle(s), Is.False);
        }
    }
}
