using NUnit.Framework;
namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgEraDeliveryWiringTests
    {
        sealed class Host : IXgHost
        {
            public double Compute=>1; public double VramMB=>8192; public double Money=>1000;
            public bool Spend(double v)=>true;public void Earn(double v){}public void Train(double t){}public string Blocker=>null;
        }
        [Test] public void CommissionUsesExistingLifeSwitchWithoutSigningAContract()
        {
            var s=new XgSim();s.S.stage=5;bool on=false;
            s.HookLife("life.era.delivery",()=>true,()=>on,v=>on=v);var h=new Host();
            var n=s.RefreshWiring(h).Node("life.era.delivery");Assert.IsNotNull(n);
            Assert.AreEqual(XgTaskState.Idle,n.state);Assert.IsNull(s.Connect("self","life.era.delivery",h));
            Assert.IsTrue(on);Assert.AreEqual(0,s.S.contracts.Count);
            Assert.AreEqual(XgTaskState.Run,s.Wiring.Node("life.era.delivery").state);
            Assert.IsTrue(s.Disconnect("self","life.era.delivery",h));Assert.IsFalse(on);
        }
        [Test] public void NoNarrativeHookMeansNoOrphanCommissionNode()
        {
            var s=new XgSim();s.S.stage=5;Assert.IsNull(s.RefreshWiring(new Host()).Node("life.era.delivery"));
        }
    }
}
