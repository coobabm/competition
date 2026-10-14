using System;
using System.Collections.Generic;
using LingGuangV05.Core.Era;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>新题型 (monthly meme drift).</summary>
    public sealed class XgMarketTests
    {
        sealed class Host : IXgHost
        {
            public double money = 100000, compute;
            public double Compute => compute;
            public double VramMB => 16000;
            public double Money => money;
            public bool Spend(double amount) { if (amount < 0 || money < amount) return false; money -= amount; return true; }
            public void Earn(double amount) { money += amount; }
            public void Train(double seconds) { }
            public string Blocker => null;
        }

        static XgSim Desks(params string[] open)
        {
            var sim = new XgSim { GoldChance = 0 };
            foreach (var d in open) if (!sim.S.desksOpen.Contains(d)) sim.S.desksOpen.Add(d);
            return sim;
        }

        // ───────────── 新题型 ─────────────

        [Test]
        public void ALoadedSaveDoesNotDriftBeforeTheDesktopRestoresItsDate()
        {
            var host = new MarketHostForLoad();
            var old = new XgSim { GoldChance = 0 };
            old.S.stage = 3; old.S.epochs = 40; old.S.totalIncome = 500;
            old.S.desksOpen.Add("danmu"); old.S.desksOpen.Add("spam");
            old.S.mkVersion = 0; // a save from before the market existed
            var loaded = new XgSim(old.S) { GoldChance = 0 };
            Assert.IsFalse(loaded.TodayKnown);
            loaded.Tick(1, host); // the catch-up tick before the desktop sets the date
            loaded.Today = 20160920; loaded.Tick(1, host);
            Assert.AreEqual(0, loaded.MemeDriftPenalty("danmu"), 1e-9);
            Assert.AreEqual(0, loaded.MemeDriftPenalty("spam"), 1e-9);
            loaded.Today = 20161020; loaded.Tick(1, host);
            Assert.Greater(loaded.MemeDriftPenalty("danmu"), 0, "the next real month still drifts");
        }

        sealed class MarketHostForLoad : IXgHost
        {
            public double money = 1e5;
            public double Compute => 1; public double VramMB => 16000; public double Money => money; public string Blocker => null;
            public bool Spend(double a) { if (money < a) return false; money -= a; return true; }
            public void Earn(double a) { money += a; }
            public void Train(double s) { }
        }

        [Test]
        public void EveryMonthMemeIsA2016WordDatedInItsMonth()
        {
            int month = 6;
            foreach (var m in XgSim.MonthMemes)
            {
                int since = XgSim.MemeSince(m.meme);
                Assert.AreEqual(2016, since / 10000, m.meme);
                Assert.AreEqual(month++, since / 100 % 100, m.meme);
                Assert.IsNull(EraLexicon.FirstBanned(m.meme), m.meme);
            }
            Assert.AreEqual("洪荒之力", XgSim.MonthMemes[XgSim.LatestMonthMeme(20160815)].meme);
            Assert.AreEqual(-1, XgSim.LatestMonthMeme(20160601));
        }

        [Test]
        public void DriftTriggersOncePerMonthOnOpenMemeDesksOnly()
        {
            var sim = Desks("danmu", "headline", "mnist", "poems", "translate"); var host = new Host();
            var notices = new List<XgMemeDriftNotice>(); sim.MemeDrifted += n => notices.Add(n);
            sim.Today = 20160601; sim.Tick(1, host);
            Assert.AreEqual(0, notices.Count);
            sim.Today = 20160608; sim.Tick(1, host);
            Assert.AreEqual(1, notices.Count);
            Assert.AreEqual("为了部落", notices[0].meme);
            CollectionAssert.AreEquivalent(new[] { "danmu", "headline", "spam" }, notices[0].desks);
            foreach (var d in new[] { "mnist", "poems", "translate", "logic", "review", "meme" }) Assert.AreEqual(0, sim.MemeDrift(d), d);
            Assert.AreEqual(15, sim.MemeDrift("danmu"), 1e-9);
            sim.Today = 20160720; sim.Tick(5, host);
            Assert.AreEqual(1, notices.Count, "same month: no second drift");

            sim.S.desksOpen.Add("review");
            sim.Answer("danmu", sim.Card("danmu").truth, host);
            Assert.AreEqual(14.5, sim.MemeDrift("danmu"), 1e-9);
            sim.Today = 20160725; sim.Tick(1, host);
            Assert.AreEqual(2, notices.Count);
            Assert.AreEqual("葛优躺", notices[1].meme);
            Assert.AreEqual(15, sim.MemeDrift("danmu"), 1e-9, "a new meme resets, never stacks");
            Assert.AreEqual(15, sim.MemeDrift("review"), 1e-9, "a desk opened since gets this month's drift");
            StringAssert.Contains("葛优躺", sim.MemeDriftChip("danmu"));
            StringAssert.Contains("−15%", sim.MemeDriftChip("danmu"));
            Assert.AreEqual("", sim.MemeDriftChip("mnist"));
        }

        [Test]
        public void ChipReadsLikeTheDesignInBothLanguages()
        {
            var sim = Desks("danmu"); var host = new Host();
            sim.Today = 20160808; sim.Tick(1, host);
            Assert.AreEqual("新题型：洪荒之力 −15%", sim.MemeDriftChip("danmu"));
            sim.English = true;
            Assert.AreEqual("New card type: 洪荒之力 (primordial power) −15%", sim.MemeDriftChip("danmu"));
        }

        [Test]
        public void DriftLowersCheckpointAccuracyByFifteenPointsForAutoAndGhost()
        {
            var sim = Desks("danmu"); var host = new Host();
            sim.S.best.Add(new XgBest { dataset = "spam", acc = .9, arch = "rnn" });
            sim.S.best.Add(new XgBest { dataset = "danmu", acc = .9, arch = "rnn" });
            var spam = sim.CreateLabelCard("spam");
            var run = sim.Run(XgTrack.Sequence);
            double clean = sim.CardAccuracy(run, spam, .9);
            sim.Today = 20160808; sim.Tick(1, host);
            Assert.AreEqual(clean - .15, sim.CardAccuracy(run, spam, .9), 1e-9);
            Assert.AreEqual(XgSim.BinaryAccuracy("logic", .9), sim.CardAccuracy(run, new XgCard { dataset = "logic" }, .9), 1e-9, "other desks keep their accuracy (no drift taken off)");
            // The ghost and suggestion read the same per-card accuracy.
            Assert.IsTrue(sim.Suggestion("spam", out _, out double confidence));
            Assert.AreEqual(sim.CardAccuracy(run, sim.Card("spam"), .9), confidence, 1e-9);

            // Automatic judgments: the drifted desk is visibly worse over many cards.
            sim.S.autoLevel = 1;
            int right = 0, total = 0, rightClean = 0, totalClean = 0;
            for (int i = 0; i < 400; i++)
            {
                sim.S.prefetch.Clear();
                sim.FillJudgmentBuffer();
                foreach (var c in sim.S.prefetch)
                {
                    if (c.dataset == "spam") { total++; if (c.guess == c.truth) right++; }
                    if (c.dataset == "danmu") continue;
                }
            }
            sim.S.mkDrift.Clear();
            for (int i = 0; i < 400; i++)
            {
                sim.S.prefetch.Clear();
                sim.FillJudgmentBuffer();
                foreach (var c in sim.S.prefetch) if (c.dataset == "spam") { totalClean++; if (c.guess == c.truth) rightClean++; }
            }
            double drifted = (double)right / total, cleanRate = (double)rightClean / totalClean;
            Assert.That(cleanRate - drifted, Is.GreaterThan(.09), "drifted " + drifted + " clean " + cleanRate);
        }

        [Test]
        public void HandLabellingIsUnaffectedAndRecoversHalfAPoint()
        {
            var sim = Desks(); var host = new Host();
            sim.Today = 20160608; sim.Tick(1, host);
            Assert.AreEqual(15, sim.MemeDrift("spam"), 1e-9);
            double before = host.money;
            var card = sim.Card("spam");
            var r = sim.Answer("spam", card.truth, host);
            Assert.IsTrue(r.correct); Assert.That(host.money, Is.GreaterThan(before));
            Assert.AreEqual(14.5, sim.MemeDrift("spam"), 1e-9);
            sim.Answer("spam", !sim.Card("spam").truth, host);
            Assert.AreEqual(14.5, sim.MemeDrift("spam"), 1e-9, "a wrong label teaches nothing");
            sim.ChatLabel("spam", true, host);
            Assert.AreEqual(14, sim.MemeDrift("spam"), 1e-9, "a correct chat label is a hand label too");
        }

        [Test]
        public void EpochsRecoverOnePointAndDriftEndsAtZero()
        {
            var sim = Desks(); var host = new Host { compute = 1 };
            sim.S.labels.Add(new XgLabelCount { dataset = "spam", count = 400 });
            sim.S.sequence.dataset = "spam";
            sim.Today = 20160608; sim.Tick(1, host);
            var cleared = new List<string>(); sim.MemeDriftCleared += d => cleared.Add(d);
            Assert.IsNotNull(sim.TrainEpoch(XgTrack.Sequence, host));
            Assert.AreEqual(14, sim.MemeDrift("spam"), 1e-9);
            sim.S.mkDrift[0].points = 1.5;
            sim.Answer("spam", sim.Card("spam").truth, host);
            Assert.IsNotNull(sim.TrainEpoch(XgTrack.Sequence, host));
            Assert.AreEqual(0, sim.MemeDrift("spam"));
            Assert.AreEqual(0, sim.S.mkDrift.Count);
            CollectionAssert.AreEqual(new[] { "spam" }, cleared);
            Assert.AreEqual("", sim.MemeDriftChip("spam"));
        }

        [Test]
        public void DriftSavesAndRepairs()
        {
            var sim = Desks("danmu"); var host = new Host();
            sim.Today = 20160808; sim.Tick(1, host);
            sim.S.mkDrift.Add(new XgMemeDrift { dataset = "danmu", meme = "x", points = 3 });
            sim.S.mkDrift.Add(new XgMemeDrift { dataset = "mnist", meme = "x", points = 3 });
            sim.S.mkDrift.Add(new XgMemeDrift { dataset = "review", meme = null, points = 99 });
            sim.S.mkDrift.Add(new XgMemeDrift { dataset = "headline", meme = "x", points = double.NaN });
            sim.S.mkDrift.Add(null);
            var copy = new XgSim(sim.S);
            Assert.AreEqual(15, copy.MemeDrift("danmu"), 1e-9, "the first entry per desk wins");
            Assert.AreEqual(0, copy.MemeDrift("mnist"));
            Assert.AreEqual(15, copy.MemeDrift("review"), 1e-9);
            Assert.AreEqual("", copy.MemeDriftMeme("review"));
            Assert.AreEqual(0, copy.MemeDrift("headline"));
            Assert.AreEqual(201608, copy.S.mkDriftMonth);
            copy.Today = 20160820; copy.Tick(1, host);
            Assert.AreEqual(15, copy.MemeDrift("danmu"), 1e-9, "the same month does not fire again after a reload");
        }

        [Test]
        public void ASaveFromBeforeDriftTakesTodayAsItsBaseline()
        {
            var state = new XgState { epochs = 50, totalIncome = 900 };
            state.desksOpen.Add("danmu");
            var sim = new XgSim(state) { GoldChance = 0 }; var host = new Host();
            Assert.AreEqual(-1, sim.S.mkDriftMonth);
            sim.Today = 20160815; sim.Tick(1, host);
            Assert.AreEqual(0, sim.MemeDrift("danmu"), "no drift on load");
            Assert.AreEqual(201608, sim.S.mkDriftMonth);
            sim.Today = 20160902; sim.Tick(1, host);
            Assert.AreEqual(15, sim.MemeDrift("danmu"), 1e-9, "the next month drifts as usual");
            Assert.AreEqual("非酋", sim.MemeDriftMeme("danmu"));
        }

        [Test]
        public void AuthoredMarketTextIs2016Clean()
        {
            var lines = new List<string>();
            foreach (var m in XgSim.MonthMemes) lines.Add(m.meme);
            foreach (var d in XgSim.MemeDriftDesks) lines.Add(XgSim.DriftQueueName(d, false));
            Assert.IsEmpty(EraLexicon.Audit(lines));
        }
    }
}
