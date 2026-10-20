using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgCrowdNoticePolicyTests
    {
        [TestCase("摆渡众包")]
        [TestCase("Bodu Crowd")]
        [TestCase("Bodu Crowdsourcing")]
        public void PlatformNoticesStartAtStageThree(string sender)
        {
            var sim = new XgSim();
            var guard = XgCrowdNoticePolicy.Capture(sender, sim, () => sim);
            Assert.IsNotNull(guard);
            for (int stage = 1; stage <= 6; stage++)
            { sim.S.stage = stage; Assert.AreEqual(stage >= 3, guard(), "stage " + stage); }
        }
        [Test] public void QueuedNoticeCannotCrossSaveOrStageGate()
        {
            var origin = new XgSim(); origin.S.stage = 3; XgSim current = origin;
            var guard = XgCrowdNoticePolicy.Capture("摆渡众包", origin, () => current);
            Assert.IsTrue(guard());
            origin.S.stage = 2; Assert.IsFalse(guard(), "recheck when dequeued or clicked");
            origin.S.stage = 3; current = new XgSim(); current.S.stage = 3;
            Assert.IsFalse(guard(), "a different eligible save cannot inherit the popup");
            current = null; Assert.IsFalse(guard());
        }
        [TestCase(1, false)] [TestCase(2, false)] [TestCase(3, true)]
        public void PlatformUpgradeToastsRespectStageWithoutBlockingOtherMessages(int stage, bool allowed)
        {
            Assert.AreEqual(allowed, XgCrowdNoticePolicy.AllowsMessage("摆渡众包升级：订单收入", stage));
            Assert.AreEqual(allowed, XgCrowdNoticePolicy.AllowsMessage("Bodu Crowd upgrade: orders", stage));
            Assert.IsTrue(XgCrowdNoticePolicy.AllowsMessage("训练完成", stage));
        }
        [Test] public void MissingLabBlocksOnlyCrowdNotices()
        {
            Assert.IsFalse(XgCrowdNoticePolicy.Capture("摆渡众包", null, () => null)());
            Assert.IsNull(XgCrowdNoticePolicy.Capture("Windows", null, () => null));
            Assert.IsNull(XgCrowdNoticePolicy.Capture("迅雷", null, () => null));
        }
    }
}
