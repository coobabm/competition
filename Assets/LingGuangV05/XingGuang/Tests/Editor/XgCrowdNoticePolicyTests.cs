using NUnit.Framework;
namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgCrowdNoticePolicyTests
    {
        [TestCase("摆渡众包")]
        [TestCase("Bodu Crowd")]
        [TestCase("Bodu Crowdsourcing")]
        public void PlatformNoticesRequireInstallationAtStageTwo(string sender)
        {
            var sim=new XgSim(); sim.S.stage=2;
            var guard=XgCrowdNoticePolicy.Capture(sender,sim,()=>sim);
            Assert.IsFalse(guard());
            sim.AppReceipt("zhongbao").installed=true;
            Assert.IsTrue(guard());
            sim.S.stage=1; Assert.IsFalse(guard());
        }
        [Test] public void QueuedNoticeRechecksInstallationAndSave()
        {
            var origin=new XgSim(); origin.S.stage=2;origin.AppReceipt("zhongbao").installed=true;
            var current=origin;var guard=XgCrowdNoticePolicy.Capture("摆渡众包",origin,()=>current);
            Assert.IsTrue(guard());origin.AppReceipt("zhongbao").installed=false;Assert.IsFalse(guard());
            origin.AppReceipt("zhongbao").installed=true;current=new XgSim();Assert.IsFalse(guard());
        }
        [Test] public void GroupGuidanceRemainsAvailableBeforeInstall()
        {
            var sim=new XgSim();sim.S.stage=2;
            Assert.IsNull(XgCrowdNoticePolicy.Capture("YY群聊",sim,()=>sim));
            Assert.IsTrue(XgCrowdNoticePolicy.AllowsMessage("群友发来了摆渡众包下载链接",sim));
            Assert.IsFalse(XgCrowdNoticePolicy.AllowsMessage("摆渡众包升级：订单收入",sim));
            sim.AppReceipt("zhongbao").installed=true;
            Assert.IsTrue(XgCrowdNoticePolicy.AllowsMessage("Bodu Crowd upgrade: orders",sim));
        }
        [TestCase(1,0,false)] [TestCase(2,0,true)] [TestCase(1,2,true)]
        public void LegacyMigrationRequiresCrowdLedgerEvidence(int funded,int tier,bool installed)
        {
            var state=new XgState {stage=2,stageVision=2,stageSequence=2,fundedAbility=funded,crowdTier=tier,totalIncome=99999};
            var sim=new XgSim(state);
            Assert.AreEqual(installed,sim.DesktopAppInstalled("zhongbao"));
            Assert.AreEqual(installed,new XgSim(sim.S).DesktopAppInstalled("zhongbao"));
        }
    }
}
