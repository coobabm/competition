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
        [Test] public void LevelFourStopsAutomaticTrainingAfterThreeStaleAssessments()
        {
            var sim=new XgSim(new XgState());var host=new Host();sim.S.owned.Add("mnist");
            for(int i=1;i<=4;i++)sim.S.unlocked.Add("auto"+i);
            sim.S.best.Add(new XgBest{dataset="mnist",arch="lenet",acc=1});sim.S.vision.running=true;
            for(int i=0;i<3;i++)sim.Assess(XgTrack.Vision,host,false);
            Assert.That(sim.S.vision.running,Is.False,"auto4 includes early stopping, not only frequent scoring");
            Assert.That(sim.BestAcc("mnist"),Is.EqualTo(1),"early stopping keeps deployed best checkpoint");
        }
    }
}
