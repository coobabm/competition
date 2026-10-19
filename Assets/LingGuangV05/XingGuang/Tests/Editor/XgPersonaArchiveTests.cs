using System;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgPersonaArchiveTests
    {
        sealed class Host : IXgHost
        {
            public double money=1000;public int charges;
            public double Money=>money;public double Compute=>50;public double VramMB=>100000;public string Blocker=>null;
            public bool Spend(double amount){if(money<amount)return false;money-=amount;charges++;return true;}
            public void Earn(double amount){money+=amount;} public void Train(double seconds){}
        }
        static void Rate(XgSim sim,string text="服务器运行中")
        {sim.AddLine("ai",text);Assert.That(sim.RateReply(sim.S.chat.Count-1,true),Is.True);}

        [Test] public void RatingsAndStageAreBothRequiredAndPurchaseIsExactlyOnce()
        {
            var sim=new XgSim();var host=new Host();var node=XgCatalog.Node(XgSim.PersonaArchiveNode);
            Assert.That(node,Is.Not.Null);Assert.That(sim.NodeCost(node),Is.EqualTo(300));
            for(int i=0;i<15;i++)Rate(sim);
            Assert.That(sim.BuyNode(node.id,host),Is.False);Assert.That(host.money,Is.EqualTo(1000));
            sim.S.stage=2;Assert.That(sim.PersonaArchiveEligible,Is.True);
            Assert.That(sim.BuyNode(node.id,host),Is.True);Assert.That(host.money,Is.EqualTo(700));
            Assert.That(sim.BuyNode(node.id,host),Is.False);Assert.That(host.charges,Is.EqualTo(1));
            Assert.That(sim.Has(node.id),Is.True);sim.RepairYyLearning();
            Assert.That(sim.S.personaPacks,Does.Not.Contain("archive"));
        }
        [Test] public void FourteenRatingsDoNotUnlockAndInvalidRatingsNeverCount()
        {
            var sim=new XgSim();sim.S.stage=2;var host=new Host();
            for(int i=0;i<14;i++)Rate(sim);
            Assert.That(sim.RateReply(sim.S.chat.Count-1,false),Is.False);
            Assert.That(sim.RateReply(-1,true),Is.False);
            Assert.That(sim.PersonaRatingCount,Is.EqualTo(14));
            Assert.That(sim.BuyNode(XgSim.PersonaArchiveNode,host),Is.False);Assert.That(host.charges,Is.Zero);
        }
        [Test] public void ObservedDriftUnlocksAnAlternativeButInsufficientFundsNeverCharge()
        {
            var sim=new XgSim();sim.S.stage=2;sim.S.phenomena.seen.Add("drift");var host=new Host{money=299};
            Assert.That(sim.PersonaArchiveEligible,Is.True);
            Assert.That(sim.Status(XgCatalog.Node(XgSim.PersonaArchiveNode),host),Is.EqualTo(XgSim.NodeStatus.TooExpensive));
            Assert.That(sim.BuyNode(XgSim.PersonaArchiveNode,host),Is.False);Assert.That(host.money,Is.EqualTo(299));Assert.That(host.charges,Is.Zero);
        }
        [Test] public void OldStateAndOpeningTheArchiveDoNotInventHistory()
        {
            var sim=new XgSim();sim.S.personaDays=null;sim.S.personaReasons=null;
            sim.Today=20160903;Assert.That(sim.PersonaArchiveDays,Is.Empty);Assert.That(sim.PersonaArchiveReasons,Is.Empty);
            Assert.That(sim.S.personaArchiveRevision,Is.Zero);
            Rate(sim);Assert.That(sim.PersonaArchiveDays,Is.Empty,"no axis change from unknown tone");
        }
        [Test] public void RealToneChangesUseGameDatesAndTrimAtFourteenCalendarDays()
        {
            var sim=new XgSim();var start=new DateTime(2016,9,1);
            for(int d=0;d<25;d++)
            {
                sim.Today=int.Parse(start.AddDays(d).ToString("yyyyMMdd"));
                for(int i=0;i<6;i++)Rate(sim,d%2==0?"好呀！":"收到");
            }
            Assert.That(sim.PersonaArchiveDays.Count,Is.LessThanOrEqualTo(14));Assert.That(sim.PersonaArchiveDays.Count,Is.GreaterThan(0));
            Assert.That(sim.PersonaArchiveDays[0].day,Is.GreaterThanOrEqualTo(20160912));
            Assert.That(sim.PersonaArchiveReasons.Count,Is.LessThanOrEqualTo(64));
            foreach(var row in sim.PersonaArchiveReasons){Assert.That(row.day,Is.GreaterThanOrEqualTo(20160912));Assert.That(row.source,Is.EqualTo("like"));Assert.That(row.before[0],Is.Not.EqualTo(row.after[0]));}
            sim.Today=20161020;Assert.That(sim.PersonaArchiveDays,Is.Empty);Assert.That(sim.PersonaArchiveReasons,Is.Empty);
        }
        [Test] public void MissingDateIsHonestAndDailySamplesAreNotDuplicated()
        {
            var sim=new XgSim();Rate(sim,"好呀！");Assert.That(sim.PersonaArchiveDays,Is.Empty);
            Assert.That(sim.PersonaArchiveReasons[0].day,Is.Zero);
            sim.Today=20160903;for(int i=0;i<8;i++)Rate(sim,"好呀！");
            Assert.That(sim.PersonaArchiveDays.Count,Is.EqualTo(1));
            Assert.That(sim.PersonaArchiveDays[0].warmth,Is.EqualTo(sim.ActualAxis(0)));
            Assert.That(sim.PersonaRatingCount,Is.EqualTo(9));
        }
#if UNITY_EDITOR
        [Test] public void UnityJsonRoundTripKeepsOwnershipRatingsAndRecordedHistory()
        {
            var sim=new XgSim();sim.S.stage=2;sim.Today=20160903;
            for(int i=0;i<15;i++)Rate(sim,i%2==0?"好呀！":"收到");
            Assert.That(sim.BuyNode(XgSim.PersonaArchiveNode,new Host()),Is.True);
            var state=UnityEngine.JsonUtility.FromJson<XgState>(UnityEngine.JsonUtility.ToJson(sim.S));
            Assert.That(state.personaRatedReplies,Is.EqualTo(15));
            Assert.That(state.unlocked,Does.Contain(XgSim.PersonaArchiveNode));
            Assert.That(state.personaDays.Count,Is.EqualTo(sim.S.personaDays.Count));
            Assert.That(state.personaDays[0].day,Is.EqualTo(20160903));
            Assert.That(state.personaReasons.Count,Is.EqualTo(sim.S.personaReasons.Count));
            Assert.That(state.personaReasons[0].before,Is.EqualTo(sim.S.personaReasons[0].before));
            Assert.That(state.personaReasons[0].after,Is.EqualTo(sim.S.personaReasons[0].after));
            Assert.That(state.personaReasons[0].source,Is.EqualTo("like"));
            var restored=new XgSim(state);restored.Today=20160903;
            Assert.That(restored.PersonaArchiveUnlocked,Is.True);
            Assert.That(restored.PersonaRatingCount,Is.EqualTo(15));
            Assert.That(restored.PersonaArchiveReasons.Count,Is.EqualTo(sim.S.personaReasons.Count));
            Assert.That(restored.Status(XgCatalog.Node(XgSim.PersonaArchiveNode),new Host()),Is.EqualTo(XgSim.NodeStatus.Owned));
        }
#endif
    }
}
