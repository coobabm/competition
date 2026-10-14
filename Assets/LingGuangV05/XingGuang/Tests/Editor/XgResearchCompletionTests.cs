using NUnit.Framework;
namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgResearchCompletionTests
    {
        sealed class Host : IXgHost
        {
            public double Money => 100000; public double Compute => 1; public double VramMB => 65536;
            public string Blocker => null; public bool Spend(double a) => true;
            public void Earn(double a) { } public void Train(double a) { }
        }
        [Test] public void ReluAlsoMitigatesDeepGradientDecay()
        {
            var sim=new XgSim(new XgState()); var run=sim.S.vision;
            sim.S.owned.Add("mnist");run.arch="lenet";run.depth=12;run.width=4;
            double before=sim.CapacityFraction(run,3);
            sim.S.unlocked.Add("relu");
            Assert.That(sim.CapacityFraction(run,3),Is.LessThan(before));
        }
    }
}
