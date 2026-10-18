using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>借你一张卡 (XgSim.CardLoan.cs): the end of stage 3, 老周 asks for a card and the case is opened first.</summary>
    public sealed class XgCardLoanTests
    {
        sealed class RigHost : IXgHost, IXgRig
        {
            readonly List<XgCardSpec> cards = new List<XgCardSpec>();
            public RigHost(int count)
            {
                for (int i = 0; i < count; i++) cards.Add(new XgCardSpec { id = "gtx980ti", name = "GTX 980 Ti", nameEn = "GTX 980 Ti", compute = 1, vramMB = 6144, watts = 250 });
            }
            public double Compute => cards.Count;
            public double VramMB => 6144;
            public double Money => 1000;
            public bool Spend(double amount) => true;
            public void Earn(double amount) { }
            public void Train(double seconds) { }
            public string Blocker => null;
            public IReadOnlyList<XgCardSpec> Cards => cards;
            public double PowerLimitWatts => 3500;
            public double HouseWatts => 0;
            public bool BreakerTripped => false;
            public void TripBreaker() { }
            public bool ResetBreaker() => true;
            public double UnpluggedWatts { get; set; }
        }

        static void SetSamples(XgSim sim, double ready)
        {
            sim.S.labels.RemoveAll(l => l.dataset == "sense");
            var l = new XgLabelCount { dataset = "sense", count = 0 };
            sim.S.labels.Add(l);
            double need = sim.SamplesThreshold(sim.NextAbility) * ready;
            double lo = 0, hi = need * 10 + 100;
            for (int i = 0; i < 60; i++) { l.count = (lo + hi) / 2; if (sim.TrainedSamples < need) lo = l.count; else hi = l.count; }
            l.count = hi;
        }

        /// <summary>A lab in stage 3 with both bars of 「短句」 at <paramref name="ready"/>.</summary>
        static XgSim Lab(double ready)
        {
            var sim = new XgSim { GoldChance = 0 };
            sim.S.abilitiesEmerged.Add(2); sim.S.abilitiesEmerged.Add(3);
            sim.S.stage = 3;
            sim.S.stageEpochs = 1;
            sim.S.trainedParamsK = sim.ParamsThreshold(sim.NextAbility) * ready;
            SetSamples(sim, ready);
            Assert.That(sim.NextAbility, Is.EqualTo(4));
            return sim;
        }

        static XgRigFacts Facts(int cards, int cases = 1)
        {
            var f = new XgRigFacts { cases = cases };
            for (int i = 0; i < cards; i++) f.cards.Add(new XgCardSpec { id = "gtx980ti", name = "GTX 980 Ti", nameEn = "GTX 980 Ti", compute = 1, vramMB = 6144, watts = 250 });
            return f;
        }

        [Test]
        public void ItComesWithTheExamAndHoldsTheChapter()
        {
            Assert.That(Lab(.7).CardLoanDue, Is.False, "the exam is not open yet");
            var sim = Lab(1.25);
            Assert.That(sim.CardLoanDue, Is.True);
            Assert.That(sim.CardLoanHolds, Is.False, "without the desktop nothing waits");
            Assert.That(sim.TakeExam(), Is.Not.Null);

            var hosted = Lab(1.25);
            hosted.DesktopHosted = true;
            Assert.That(hosted.ChapterSceneHolds, Is.True);
            Assert.That(hosted.TakeExam(), Is.Null);
            Assert.That(hosted.CardLoanStart(1), Has.Count.EqualTo(3));
            Assert.That(XgGuide.ConfirmStage(hosted).zh, Does.Contain("机箱"));
            Assert.That(hosted.CardLoanDecide(true, new RigHost(1)), Is.Empty, "the case first");
            Assert.That(hosted.CardLoanTourDone(), Is.True);
            Assert.That(XgGuide.ConfirmStage(hosted).app, Is.EqualTo(XgGuide.YY), "then 老周, in YY");
            Assert.That(hosted.CardLoanDecide(false, new RigHost(1)), Is.Not.Empty);
            Assert.That(hosted.ChapterSceneHolds, Is.False);
            Assert.That(hosted.CardLoanDue, Is.False, "once per save");
            hosted.TakeExam(); hosted.RevealExam();
            Assert.That(hosted.ConfirmNextStage(), Is.True);
            Assert.That(hosted.HasAbility(4), Is.True);
        }

        [Test]
        public void TheTourUsesTheMachinesOwnNumbers()
        {
            var one = XgSim.CardLoanTour(Facts(1));
            Assert.That(one.Select(p => p.part), Is.EqualTo(new[] { "card", "vram", "power", "case" }));
            Assert.That(one[0].zh, Does.Contain("×1").And.Contain("训练就停"));
            Assert.That(one[1].zh, Does.Contain("6G"));
            Assert.That(one[2].zh, Does.Contain("295W").And.Contain("250W").And.Contain("3500W").And.Contain("¥0.52").And.Contain("¥0.13"));
            Assert.That(one[3].zh, Does.Contain("2 个显卡插槽").And.Contain("空着 1 个"));
            var two = XgSim.CardLoanTour(Facts(2));
            Assert.That(two[0].zh, Does.Contain("×2").And.Contain("慢一截"));
            Assert.That(two[3].zh, Does.Contain("插满了。"));
            Assert.That(XgSim.CardLoanTour(Facts(3, 2))[3].zh, Does.Contain("2 台机箱，4 个显卡插槽"));
            foreach (var p in one.Concat(two))
            {
                Assert.That(p.en, Is.Not.Empty); Assert.That(p.titleEn, Is.Not.Empty);
                Assert.That(p.zh + p.en, Does.Not.Contain("灵光").And.Not.Contain("涌现"));
            }
        }

        [Test]
        public void LendingTakesTheNewestCardAwayForAWhile()
        {
            var sim = Lab(1.25); var host = new RigHost(2);
            sim.CardLoanStart(2); sim.CardLoanTourDone();
            var reply = sim.CardLoanDecide(true, host);
            Assert.That(reply[0][0], Does.Contain("靠谱"));
            Assert.That(sim.S.wiring.pulled, Does.Contain("h:gtx980ti#1"), "the second card is out");
            Assert.That(sim.CardLent && sim.ZhouFavourOwed, Is.True);
            Assert.That(sim.ChronicleHas("laozhou.card"), Is.True, "the loan is the life event's first time");
            Assert.That(sim.Chronicle().Any(c => c.id == XgSim.CardLoanChronicle), Is.False, "told by 「laozhou.card」");
            sim.Tick(XgSim.CardLoanSeconds + 2, host);
            Assert.That(sim.CardLent, Is.False, "he brought it back");
            Assert.That(sim.S.wiring.pulled, Does.Not.Contain("h:gtx980ti#1"));
        }

        [Test]
        public void SayingNoKeepsTheCardAndIsRemembered()
        {
            var sim = Lab(1.25); var host = new RigHost(1);
            sim.CardLoanStart(1); sim.CardLoanTourDone();
            int cool = sim.ZhouCool;
            Assert.That(XgSim.CardLoanAnswers(1)[1][0], Does.Contain("就这一张"));
            Assert.That(sim.CardLoanDecide(false, host)[0][0], Does.Contain("没事"));
            Assert.That(sim.S.wiring.pulled, Is.Empty);
            Assert.That(sim.ZhouCool, Is.EqualTo(cool + 1));
            Assert.That(sim.ZhouFavourOwed, Is.False);
            var c = sim.Chronicle().First(x => x.id == XgSim.CardLoanChronicle);
            Assert.That(c.body, Does.Contain("机箱").And.Contain("不借"));
        }

        [Test]
        public void HeAsksDifferentlyOnceHeKnows()
        {
            var sim = Lab(1.25);
            Assert.That(sim.CardLoanStart(2).Last()[0], Does.Contain("2 张卡"));
            var told = Lab(1.25); told.S.zhouKnows = true;
            Assert.That(told.CardLoanStart(1).Last()[0], Does.Contain("程序"));
            told.CardLoanTourDone();
            Assert.That(told.CardLoanDecide(false, new RigHost(1))[0][0], Does.Contain("离不了卡"));
        }

        [Test]
        public void OnTheDesktopTheLifeEventWaitsForTheChapter()
        {
            var sim = Lab(.5); var host = new RigHost(2);
            Assert.That(sim.LifeEventsEligible(host), Does.Contain("zhou_card"), "the bot and tools keep the old event");
            sim.DesktopHosted = true;
            Assert.That(sim.LifeEventsEligible(host), Does.Not.Contain("zhou_card"), "the first ask is the chapter's");
        }

        [Test]
        public void AnOpenLifeEventKeepsItsArguments()
        {
            var sim = Lab(1.25); var host = new RigHost(2);
            sim.S.lifeArgKey = "bodu"; sim.S.lifeArgName = "某数据"; sim.S.lifeArgNameEn = "Some data";
            sim.CardLoanStart(2); sim.CardLoanTourDone(); sim.CardLoanDecide(true, host);
            Assert.That(sim.S.lifeArgKey + sim.S.lifeArgName + sim.S.lifeArgNameEn, Is.EqualTo("bodu某数据Some data"));
            Assert.That(sim.S.lifeHolds.Last().name, Is.EqualTo("GTX 980 Ti"));
        }
    }
}
