using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgDailyLifeIntegrationTests
    {
        sealed class Host : IXgHost
        {
            public double money=10000;
            public double Compute=>2; public double VramMB=>100000; public double Money=>money;
            public string Blocker=>null;
            public bool Spend(double amount){if(money<amount)return false;money-=amount;return true;}
            public void Earn(double amount){money+=amount;} public void Train(double seconds){}
        }
        [Test] public void CatalogUsesSevenOrdinaryPricesAndNoMainlineNeeds()
        {
            var sim=new XgSim();var ids=XgDailyLife.Nodes.Select(n=>n.id).ToArray();
            Assert.AreEqual(7,ids.Length);Assert.AreEqual(1400,XgDailyLife.Nodes.Sum(n=>n.price));
            foreach(var d in XgDailyLife.Nodes)
            {
                var n=XgCatalog.Node(d.id);Assert.NotNull(n);Assert.AreEqual(d.price,n.cost);Assert.AreEqual(d.price,sim.NodeCost(n));
                Assert.AreEqual(d.stage,n.stage);Assert.AreEqual("daily",n.lane);Assert.IsNull(n.parent);Assert.IsEmpty(n.needs);
            }
            foreach(var n in XgCatalog.Nodes.Where(n=>!ids.Contains(n.id)))
            {Assert.False(ids.Contains(n.parent));Assert.False(n.needs.Any(ids.Contains));}
        }
        [Test] public void ThreeRealGamesShareOneBucketAndRequirePurchase()
        {
            var s=new XgSim();s.S.stage=2;var h=new Host();var n=XgCatalog.Node("daily.dice");
            Assert.AreEqual(XgSim.NodeStatus.Locked,s.Status(n,h));
            Assert.False(s.RecordDailyGameCompleted("gomoku","built-in",false));
            Assert.False(s.RecordDailyGameCompleted("unknown","bad",true));
            Assert.True(s.RecordDailyGameCompleted("gomoku","1",true));Assert.False(s.RecordDailyGameCompleted("gomoku","1",true));
            Assert.True(s.RecordDailyGameCompleted("go","2",true));Assert.False(s.DailyNodeReady(n.id));
            Assert.True(s.RecordDailyGameCompleted("chess","3",true));Assert.True(s.DailyNodeReady(n.id));
            Assert.AreEqual(3,s.DailyProgress("games10").count);Assert.False(s.DailyProbabilityQuestions);
            Assert.AreEqual(XgSim.NodeStatus.Buyable,s.Status(n,h));Assert.True(s.BuyNode(n.id,h));
            Assert.AreEqual(9800,h.money);Assert.True(s.DailyProbabilityQuestions);Assert.False(s.Has("train.monitor"));
            Assert.False(s.BuyNode(n.id,h));Assert.AreEqual(9800,h.money);
        }
        [Test] public void CompletedGameConditionCannotBypassStageOrFunds()
        {
            var s=new XgSim();var h=new Host();for(int i=0;i<3;i++)s.RecordDailyGameCompleted("gomoku","g"+i,true);
            var n=XgCatalog.Node("daily.dice");Assert.AreEqual(XgSim.NodeStatus.Locked,s.Status(n,h));
            s.S.stage=2;h.money=199;Assert.AreEqual(XgSim.NodeStatus.TooExpensive,s.Status(n,h));Assert.False(s.BuyNode(n.id,h));
            Assert.AreEqual(199,h.money);Assert.False(s.DailyProbabilityQuestions);
        }
        [Test] public void LegacyTotalsCannotBackfillReceiptsAndQueriesArePure()
        {
            var s=new XgSim();s.S.games.Add(new XgGameRecord{game="gomoku",played=99});s.S.dailyReceipts=null;
            string before=JsonUtility.ToJson(s.S);Assert.AreEqual(0,s.DailyProgress("games3").count);Assert.False(s.DailyNodeReady("daily.dice"));
            Assert.AreEqual(before,JsonUtility.ToJson(s.S));
        }
        [Test] public void ReceiptReplaySurvivesSaveRoundTripAndDoesNotLeakToAnotherSave()
        {
            var s=new XgSim();s.RecordDailyGameCompleted("gomoku","one",true);s.RecordDailyNewsRead("a","article-a");
            var loaded=new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(s.S)));
            Assert.False(loaded.RecordDailyGameCompleted("gomoku","one",true));Assert.False(loaded.RecordDailyNewsRead("a","new-callback"));
            Assert.AreEqual(1,loaded.DailyProgress("games3").count);Assert.True(loaded.DailyNewsAlreadyRead("a"));
            Assert.AreEqual(0,new XgSim().DailyProgress("games3").count);
        }
        [Test] public void NewsAndMusicRequireDistinctSubjects()
        {
            var s=new XgSim();for(int i=0;i<10;i++){s.RecordDailyNewsRead("same","n"+i);s.RecordDailySongCompleted("same","m"+i);}
            Assert.AreEqual(1,s.DailyProgress("news5").count);Assert.AreEqual(1,s.DailyProgress("music3").count);
            for(int i=0;i<8;i++){s.RecordDailyNewsRead("a"+i,"new"+i);s.RecordDailySongCompleted("song"+i,"play"+i);}
            Assert.AreEqual(5,s.DailyProgress("news5").count);Assert.AreEqual(3,s.DailyProgress("music3").count);
        }
        [Test] public void ActualMessageKindsAndPostOwnershipAreRequired()
        {
            var s=new XgSim();Assert.False(s.RecordDailySentMessage("qingwen","a"," "));
            Assert.False(s.RecordDailySentMessage("qingwen","b","sticker","666"));Assert.False(s.RecordDailySentMessage("netbar","c","text"));
            Assert.True(s.RecordDailySentMessage("qingwen","d","hello"));Assert.True(s.RecordDailySentMessage("netbar","e","sticker","666"));
            Assert.False(s.RecordDailyOwnPostReply("t","r",false,true));Assert.False(s.RecordDailyOwnPostReply("t","r",true,false));
            Assert.True(s.RecordDailyOwnPostReply("t","r",true,true));
        }
        [Test] public void DailyDeskEntitlementsAreCheckedAtOpen()
        {
            var s=new XgSim();s.S.stage=2;
            Assert.NotNull(s.MiniGameBlocker("daily_probability"));Assert.NotNull(s.MiniGameBlocker("daily_affairs"));
            s.S.unlocked.Add("daily.dice");Assert.IsNull(s.MiniGameBlocker("daily_probability"));Assert.NotNull(s.MiniGameBlocker("daily_affairs"));
            s.S.unlocked.Add("daily.news");Assert.IsNull(s.MiniGameBlocker("daily_affairs"));
        }
        [Test] public void BirthdayRequiresAnAffirmativeDisclosure()
        {
            Assert.False(XgDailyLife.DisclosesBirthday("生日不是9月27日"));Assert.False(XgDailyLife.DisclosesBirthday("今天9月27日"));
            Assert.True(XgDailyLife.DisclosesBirthday("我的生日是9月27日。"));Assert.True(XgDailyLife.DisclosesBirthday("My birthday is 27 September."));
        }
        [Test] public void SeekAndPartialPlaybackCannotBecomeCompletedSongs()
        {
            var p=new XgDailyPlayback();p.Observe("s",0,10,.1,true);p.Observe("s",9.9,10,.1,true);Assert.False(p.Complete(.1));
            p.Reset();p.Observe("s",0,10,.1,true);for(int i=1;i<=99;i++)p.Observe("s",i/10d,10,.1,true);
            Assert.True(p.Complete(.1));Assert.False(p.Complete(.1));
        }
    }
}
