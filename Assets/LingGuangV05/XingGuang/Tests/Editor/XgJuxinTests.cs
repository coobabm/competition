using System.Linq;
using LingGuangV05.Core.Era;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    public class XgJuxinTests
    {
        sealed class Host : IXgHost
        {
            public double money = 1000;
            public double Compute => 1;
            public double VramMB => 2048;
            public double Money => money;
            public bool Spend(double a) { if (money + 1e-9 < a) return false; money -= a; return true; }
            public void Earn(double a) { money += a; }
            public void Train(double s) { }
            public string Blocker => null;
        }

        static XgSim Lab(int stage, int today = 20160905)
        {
            var sim = new XgSim();
            sim.S.stage = stage;
            sim.Today = today;
            return sim;
        }

        static void Run(XgSim sim, Host host, double seconds)
        {
            for (double t = 0; t < seconds; t += 1) sim.JuxinTick(1, host);
        }

        static void SignCheque(XgSim sim, Host host)
        {
            sim.S.best.Add(new XgBest { dataset = "mnist", acc = .98 });
            Assert.IsTrue(sim.Sign("cheque", host));
        }

        [Test]
        public void JuxinOpensAtStageFourWithTheMiniProgramPush()
        {
            var host = new Host();
            var sim = Lab(3);
            Run(sim, host, 10);
            Assert.IsFalse(sim.JuxinOpen);
            Assert.AreEqual(0, sim.S.jxThreads.Count);
            sim.S.stage = 4;
            sim.JuxinTick(1, host);
            Assert.IsTrue(sim.JuxinOpen);
            var team = sim.JuxinThread(XgSim.JxTeam);
            Assert.IsNotNull(team);
            StringAssert.Contains("小程序", team.messages[0].titleZh);
            Assert.AreEqual((int)XgJxKind.Article, team.messages[0].kind);
            Assert.Greater(sim.JuxinUnread, 0);
            // 老周 never writes first and is in no group (design v1.1 §9): he has no 巨信 thread at all.
            Assert.IsFalse(sim.S.jxThreads.Any(t => t.id.Contains("zhou")));
        }

        [Test]
        public void AutoReplyIsGatedToStageFive()
        {
            var sim = Lab(4);
            Assert.IsFalse(sim.SetJuxinAutoReply(true));
            Assert.IsFalse(sim.JuxinAutoReply);
            sim.S.jxAutoReply = true; // a damaged or older save
            sim.JuxinTick(1, new Host());
            Assert.IsFalse(sim.S.jxAutoReply);
            Assert.IsNull(sim.TakeAutoReply());
            sim.S.stage = 5;
            Assert.IsTrue(sim.SetJuxinAutoReply(true));
            Assert.IsTrue(sim.JuxinAutoReply);
        }

        [Test]
        public void NewsFollowsTheCalendarWithoutABacklog()
        {
            var host = new Host();
            var sim = Lab(4, 20160907);
            sim.JuxinTick(1, host);
            // Opening on 9-07: the 9-02 and 9-04 items are history.
            Assert.IsNull(sim.JuxinThread(XgSim.JxNews));
            sim.Today = 20160908;
            sim.JuxinTick(1, host);
            var news = sim.JuxinThread(XgSim.JxNews);
            Assert.AreEqual(1, news.messages.Count);
            StringAssert.Contains("iPhone 7", news.messages[0].titleZh);
            // A jump to mid-November skips September and October and pushes November one item per tick.
            sim.Today = 20161113;
            sim.JuxinTick(1, host);
            sim.JuxinTick(1, host);
            sim.JuxinTick(1, host);
            Assert.AreEqual(3, news.messages.Count);
            StringAssert.Contains("1207", news.messages[1].titleZh);
            StringAssert.Contains("鲁班", news.messages[2].titleZh);
            Assert.IsFalse(news.messages.Any(m => m.titleZh.Contains("诺贝尔")));
        }

        [Test]
        public void ASignedClientPostsAnArticleAndItsContactWritesAndCanBeAnswered()
        {
            var host = new Host();
            var sim = Lab(4);
            sim.JuxinTick(1, host);
            SignCheque(sim, host);
            Run(sim, host, 6);
            var oa = sim.JuxinThread(XgSim.JuxinOa("cheque"));
            Assert.IsNotNull(oa);
            StringAssert.Contains("喜报", oa.messages[0].titleZh);
            StringAssert.Contains("点击阅读原文", oa.messages[0].zh);
            var dm = sim.JuxinThread(XgSim.JuxinDm("cheque"));
            Assert.IsNotNull(dm);
            Assert.GreaterOrEqual(dm.waitingSince, 0);
            Assert.IsTrue(XgSim.JuxinIsSubscription(oa.id));
            Assert.IsFalse(XgSim.JuxinIsSubscription(dm.id));
            Assert.IsTrue(sim.JuxinSend(dm.id, "好的，合作愉快"));
            Assert.Less(dm.waitingSince, 0);
            Assert.AreEqual(1, dm.mood);
            Assert.IsTrue(dm.messages.Last().mine);
        }

        [Test]
        public void HappyClientsSendASmallBonusRedPacket()
        {
            var host = new Host();
            var sim = Lab(4);
            sim.JuxinTick(1, host);
            SignCheque(sim, host);
            Run(sim, host, 6);
            var dm = sim.JuxinThread(XgSim.JuxinDm("cheque"));
            for (int i = 0; i < XgSim.JuxinMoodLimit; i++)
            {
                dm.waitingSince = sim.JuxinClock;
                sim.JuxinSend(dm.id, "收到");
            }
            Run(sim, host, 10);
            int index = dm.messages.FindIndex(m => m.kind == (int)XgJxKind.RedPacket);
            Assert.GreaterOrEqual(index, 0);
            double amount = dm.messages[index].amount;
            Assert.AreEqual(XgSim.JuxinBonusMin, amount, 1e-9, "3% of a ¥60 signing bonus, raised to the ¥5 floor");
            double before = host.money;
            Assert.AreEqual(amount, sim.OpenRedPacket(dm.id, index, host), 1e-9);
            Assert.AreEqual(before + amount, host.money, 1e-9);
            Assert.AreEqual(-1, sim.OpenRedPacket(dm.id, index, host), 1e-9, "opened once");
        }

        [Test]
        public void IgnoredContactsAskAgainAndGrowUnhappy()
        {
            var host = new Host();
            var sim = Lab(4);
            sim.JuxinTick(1, host);
            var ajie = sim.JuxinThread(XgSim.JxAjie);
            Assert.GreaterOrEqual(ajie.waitingSince, 0, "阿杰 says hello and waits");
            int count = ajie.messages.Count;
            Run(sim, host, XgSim.JuxinIgnoreSeconds + 2);
            Assert.Greater(ajie.messages.Count, count);
            Assert.AreEqual(1, ajie.nudges);
            Assert.AreEqual(-1, ajie.mood);
        }

        [Test]
        public void GroupRedPacketsAreGoneForTheSlow()
        {
            var host = new Host();
            var sim = Lab(4, 20160916);
            sim.JuxinTick(1, host);
            var family = sim.JuxinThread(XgSim.JxFamily);
            int packet = -1;
            for (int i = 0; i < 4000 && packet < 0; i++) { sim.JuxinTick(1, host); packet = family.messages.FindIndex(m => m.kind == (int)XgJxKind.RedPacket); }
            Assert.GreaterOrEqual(packet, 0, "老爸's Mid-Autumn packet");
            Run(sim, host, XgSim.JuxinRedPacketLife + 1);
            Assert.AreEqual(0, sim.OpenRedPacket(family.id, packet, host), 1e-9);
            Assert.IsTrue(family.messages[packet].opened);
        }

        [Test]
        public void TheAiAnswersForThePlayerAndMarksItsReply()
        {
            var host = new Host();
            var sim = Lab(5);
            sim.JuxinGaffeOverride = 0;
            sim.JuxinTick(1, host);
            Assert.IsTrue(sim.SetJuxinAutoReply(true));
            Assert.IsNull(sim.TakeAutoReply(), "the message sits a moment first");
            Run(sim, host, XgSim.JuxinAutoDelay + 1);
            var job = sim.TakeAutoReply();
            Assert.IsNotNull(job);
            Assert.IsFalse(job.gaffe);
            StringAssert.Contains("巨信", job.system);
            Assert.IsNull(sim.TakeAutoReply(), "one job per waiting thread");
            Assert.IsTrue(sim.CompleteAutoReply(job, null), "offline: a pool line");
            var t = sim.JuxinThread(job.thread);
            var sent = t.messages.Last();
            Assert.IsTrue(sent.mine && sent.ai && !sent.gaffe);
            Assert.IsNotEmpty(sent.zh);
            Assert.Less(t.waitingSince, 0);
            Assert.AreEqual(1, sim.S.jxAiReplies);
        }

        [Test]
        public void TheModelReplyIsCleanedAndAGaffeDrawsAReaction()
        {
            var host = new Host();
            var sim = Lab(5);
            sim.JuxinTick(1, host);
            sim.SetJuxinAutoReply(true);
            sim.JuxinGaffeOverride = 0;
            Run(sim, host, XgSim.JuxinAutoDelay + 1);
            var job = sim.TakeAutoReply();
            sim.CompleteAutoReply(job, "<think>嗯</think>阿杰：哈哈，晚上见");
            Assert.AreEqual("哈哈，晚上见", sim.JuxinThread(job.thread).messages.Last().zh);

            SignCheque(sim, host);
            Run(sim, host, 6 + XgSim.JuxinAutoDelay);
            sim.JuxinGaffeOverride = 1;
            var gaffe = sim.TakeAutoReply();
            Assert.IsNotNull(gaffe);
            Assert.IsTrue(gaffe.gaffe);
            var t = sim.JuxinThread(gaffe.thread);
            int mood = t.mood;
            sim.CompleteAutoReply(gaffe, "这句不会被用上");
            var sent = t.messages.Last();
            Assert.IsTrue(sent.ai && sent.gaffe);
            Assert.AreNotEqual("这句不会被用上", sent.zh);
            Assert.AreEqual(mood - 1, t.mood);
            int count = t.messages.Count;
            Run(sim, host, 7);
            Assert.Greater(t.messages.Count, count, "the other side reacts");
            Assert.IsFalse(t.messages.Last().mine);
            Assert.AreEqual(1, sim.S.jxGaffes);
        }

        [Test]
        public void WeeklySettlementNoticesFollowContractIncome()
        {
            var host = new Host();
            var sim = Lab(4, 20160905);
            sim.JuxinTick(1, host);
            SignCheque(sim, host);
            Run(sim, host, 30);
            var oa = sim.JuxinThread(XgSim.JuxinOa("cheque"));
            int before = oa.messages.Count;
            sim.Today = 20160913;
            sim.JuxinTick(1, host);
            Assert.AreEqual(before + 1, oa.messages.Count);
            StringAssert.Contains("结算通知", oa.messages.Last().titleZh);
        }

        [Test]
        public void JuxinSurvivesASaveRoundTripAndOldSavesLoad()
        {
            var host = new Host();
            var sim = Lab(5);
            sim.JuxinTick(1, host);
            sim.SetJuxinAutoReply(true);
            sim.JuxinLike("m.aj1080");
            var again = new XgSim(sim.S) { Today = 20160905 };
            Assert.IsTrue(again.JuxinOpen);
            Assert.IsTrue(again.JuxinAutoReply);
            Assert.IsTrue(again.JuxinLiked("m.aj1080"));
            Assert.AreEqual(sim.JuxinUnread, again.JuxinUnread);
            again.JuxinTick(1, host);
            Assert.AreEqual(1, again.JuxinThread(XgSim.JxTeam).messages.Count, "opened once");

            // A save from before 巨信: the fields are missing (JsonUtility leaves lists null in the worst case).
            var old = new XgState { stage = 4 };
            old.jxThreads = null; old.jxSeen = null; old.jxLikes = null; old.jxEarned = null;
            var loaded = new XgSim(old) { Today = 20160905 };
            Assert.IsFalse(loaded.JuxinOpen);
            Assert.AreEqual(0, loaded.JuxinUnread);
            Assert.AreEqual(0, loaded.JuxinChats().Count);
            loaded.JuxinTick(1, host);
            Assert.IsTrue(loaded.JuxinOpen);
        }

        [Test]
        public void ContractsSignedBeforeJuxinAreHistoryNotNews()
        {
            var host = new Host();
            var sim = Lab(4);
            SignCheque(sim, host);
            sim.JuxinTick(1, host);
            Run(sim, host, 6);
            var oa = sim.JuxinThread(XgSim.JuxinOa("cheque"));
            Assert.AreEqual(1, oa.messages.Count);
            Assert.AreEqual(0, oa.unread);
            Assert.IsNull(sim.JuxinThread(XgSim.JuxinDm("cheque")));
        }

        [Test]
        public void AuthoredLinesAreBilingualAnd2016Only()
        {
            var lines = XgSim.JuxinAuthoredLines().ToList();
            Assert.Greater(lines.Count, 100);
            Assert.IsFalse(lines.Any(string.IsNullOrWhiteSpace));
            CollectionAssert.IsEmpty(EraLexicon.Audit(lines));
            foreach (var c in XgSim.JuxinClients)
                Assert.IsTrue(XgCatalog.Contract(c.id) != null || XgSim.SlaOffer(c.id) != null, c.id);
            foreach (var c in XgCatalog.Contracts) Assert.IsNotNull(XgSim.JuxinClient(c.id), c.id);
            foreach (var o in XgSim.SlaOffers) Assert.IsNotNull(XgSim.JuxinClient(o.id), o.id);
            foreach (var n in XgSim.JuxinNewsItems) Assert.IsTrue(n.date >= 20160901 && n.date <= 20161231);
        }
    }
}
