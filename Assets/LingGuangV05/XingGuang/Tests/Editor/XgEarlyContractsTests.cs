using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgEarlyContractsTests
    {
        [TestCase("cheque")]
        [TestCase("acrostic")]
        [TestCase("danmaku")]
        public void StageThreeStillRequiresTheRealQualityThreshold(string id)
        {
            var sim=new XgSim();var contract=XgCatalog.Contract(id);
            sim.AppReceipt(XgSim.CrowdApp).installed=true;
            sim.S.stage=2;sim.S.best.Add(new XgBest{dataset=contract.dataset,acc=1});
            Assert.False(sim.CanSign(contract));
            sim.S.stage=3;Assert.True(sim.CanSign(contract));
            sim.S.best[0].acc=contract.threshold-.001;Assert.False(sim.CanSign(contract));
            sim.S.best[0].acc=contract.threshold;Assert.True(sim.CanSign(contract));
            sim.S.contracts.Add(id);Assert.False(sim.CanSign(contract),"A saved signed order cannot be signed twice");
        }
        [Test]
        public void AdvancedContractsKeepStageFourAndTheirQualityGates()
        {
            var sim=new XgSim();sim.S.stage=3;sim.AppReceipt(XgSim.CrowdApp).installed=true;
            foreach(var contract in XgCatalog.Contracts)
            {
                if(XgSim.ContractRequiredStage(contract)==3)continue;
                sim.S.best.Clear();sim.S.best.Add(new XgBest{dataset=contract.dataset,acc=1});
                Assert.False(sim.CanSign(contract),contract.id);
            }
            Assert.True(sim.FeatureVisible("contracts"),"An untrained player can view the requirements");
            Assert.False(sim.CanSign(null));
        }
    }
}
