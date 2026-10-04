using System;
using System.Collections.Generic;
using LingGuangV05.Core.Era;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>The protagonist works out auto labelling alone: the ghost guess, the trigger, the hidden node, the monologue.</summary>
    public sealed class XgEpiphanyTests
    {
        sealed class Host : IXgHost
        {
            public double money = 100000;
            public double Compute => 1;
            public double VramMB => 16000;
            public double Money => money;
            public bool Spend(double amount) { if (money + 1e-9 < amount) return false; money -= amount; return true; }
            public void Earn(double amount) { money += amount; }
            public void Train(double seconds) { }
            public string Blocker => null;
        }

        const string Desk = "spam";

        static XgSim Fresh(double acc)
        {
            var sim = new XgSim { GoldChance = 0, PlatformChecks = false };
            Assert.IsTrue(sim.DeskOpen(Desk));
            sim.SelectDesk(Desk);
            if (acc > 0) sim.S.best.Add(new XgBest { dataset = Desk, acc = acc, arch = "perceptron" });
            return sim;
        }

        /// <summary>Answers the visible card agreeing (or not) with its ghost; the ghost must be there.</summary>
        static void AnswerGhost(XgSim sim, Host host, bool agree)
        {
            Assert.IsTrue(sim.GhostGuess(Desk, out bool ghost, out double confidence), "the ghost shows on this card");
            Assert.That(confidence, Is.InRange(0.0, 1.0));
            sim.Answer(Desk, agree ? ghost : !ghost, host);
        }

        [Test]
        public void GhostOnlyAppearsWithASixtyPercentCheckpoint()
        {
            Assert.IsFalse(Fresh(0).GhostGuess(Desk, out _, out _), "no checkpoint");
            Assert.IsFalse(Fresh(.59).GhostGuess(Desk, out _, out _), "below 60%");
            var sim = Fresh(.6);
            Assert.IsTrue(sim.GhostGuess(Desk, out _, out _), "exactly 60%");
            Assert.IsFalse(sim.GhostGuess("mnist", out _, out _), "a desk that is not open");
            Assert.IsFalse(sim.GhostGuess("nope", out _, out _));
        }

        [Test]
        public void GhostIsDeterministicAndAgreesWithTheVisibleSuggestion()
        {
            var sim = Fresh(.8); var host = new Host();
            for (int i = 0; i < 60; i++)
            {
                Assert.IsTrue(sim.GhostGuess(Desk, out bool a, out double ca));
                Assert.IsTrue(sim.GhostGuess(Desk, out bool b, out double cb));
                Assert.AreEqual(a, b); Assert.AreEqual(ca, cb, 1e-12);
                Assert.IsTrue(sim.Suggestion(Desk, out bool shown, out _));
                Assert.AreEqual(shown, a, "the ghost never contradicts the suggestion line");
                sim.Answer(Desk, sim.Card(Desk).truth, host);
                if (sim.EpiphanyDone) break;
            }
        }

        [Test]
        public void StreakCountsAgreementAndResetsOnAMismatch()
        {
            var sim = Fresh(.8); var host = new Host();
            for (int i = 0; i < 5; i++) AnswerGhost(sim, host, true);
            Assert.AreEqual(5, sim.S.ghostStreak);
            Assert.IsTrue(sim.S.ghostSeen);
            AnswerGhost(sim, host, false);
            Assert.AreEqual(0, sim.S.ghostStreak, "a mismatch starts over");
            Assert.IsFalse(sim.S.epiphany);
            for (int i = 0; i < 7; i++) AnswerGhost(sim, host, true);
            Assert.AreEqual(7, sim.S.ghostStreak);
            Assert.IsFalse(sim.S.epiphany, "seven is not yet eight");
        }

        [Test]
        public void TheEventFiresOnceAtEightInARow()
        {
            var sim = Fresh(.8); var host = new Host(); int fired = 0;
            sim.Epiphany += () => fired++;
            for (int i = 0; i < XgSim.EpiphanyStreak - 1; i++) AnswerGhost(sim, host, true);
            Assert.AreEqual(0, fired);
            Assert.IsTrue(sim.AutoLabelHidden);
            AnswerGhost(sim, host, true);
            Assert.AreEqual(1, fired);
            Assert.IsTrue(sim.S.epiphany); Assert.IsTrue(sim.AutoLabelRevealed, "headless runs reveal at once");
            Assert.IsFalse(sim.GhostGuess(Desk, out _, out _), "the ghost has done its job");
            for (int i = 0; i < 40; i++) sim.Answer(Desk, sim.Card(Desk).truth, host);
            Assert.AreEqual(1, fired, "only once");
        }

        [Test]
        public void FallbackFiresAHundredAndFiftyHandLabelsAfterTheGhostFirstAppeared()
        {
            var sim = Fresh(0); var host = new Host(); int fired = 0;
            sim.Epiphany += () => fired++;
            for (int i = 0; i < 30; i++) sim.Answer(Desk, sim.Card(Desk).truth, host);
            Assert.AreEqual(0, sim.S.ghostLabels, "labels before the ghost do not count");
            sim.S.best.Add(new XgBest { dataset = Desk, acc = .7, arch = "perceptron" });
            for (int i = 0; i < XgSim.EpiphanyFallbackLabels - 1; i++)
            {
                if (sim.GhostGuess(Desk, out bool ghost, out _)) sim.Answer(Desk, !ghost, host); // never agree
                else sim.Answer(Desk, sim.Card(Desk).truth, host);
                Assert.AreEqual(0, fired, "label " + i);
            }
            Assert.AreEqual(0, sim.S.ghostStreak);
            AnswerGhost(sim, host, false);
            Assert.AreEqual(1, fired, "the fallback");
            Assert.IsTrue(sim.EpiphanyDone);
        }

        [Test]
        public void AutoLabelIsHiddenAndUnbuyableBeforeAndBuyableAfter()
        {
            var sim = Fresh(.9); var host = new Host();
            var node = XgCatalog.Node("label.auto");
            Assert.IsTrue(sim.AutoLabelHidden);
            Assert.AreEqual("？？？", sim.NodeName(node));
            Assert.IsTrue(sim.NodeMystery(node));
            StringAssert.Contains("更省力", sim.NodeNote(node));
            Assert.AreEqual(XgSim.NodeStatus.Locked, sim.Status(node, host));
            Assert.IsFalse(sim.CanBuyGlobalAuto(out string why));
            StringAssert.Contains("更省力", why);
            StringAssert.Contains("更省力", sim.Why(node, host));
            Assert.IsFalse(sim.BuyNode(node.id, host));
            Assert.IsFalse(sim.BuyGlobalAuto(host));
            Assert.AreEqual(0, sim.GlobalAutoLevel);
            Assert.AreEqual("拿不准才问我", sim.NodeName(XgCatalog.Node("label.coop")), "only label.auto is a mystery");

            for (int i = 0; i < XgSim.EpiphanyStreak; i++) AnswerGhost(sim, host, true);
            Assert.IsFalse(sim.AutoLabelHidden);
            Assert.AreEqual("自动答题", sim.NodeName(node));
            Assert.IsFalse(sim.NodeMystery(node));
            Assert.AreEqual(XgSim.NodeStatus.Buyable, sim.Status(node, host));
            Assert.IsTrue(sim.BuyNode(node.id, host));
            Assert.AreEqual(1, sim.GlobalAutoLevel);
        }

        [Test]
        public void TheDesktopCanDeferTheRevealUntilItsCutsceneEnds()
        {
            var sim = Fresh(.9); var host = new Host(); int fired = 0;
            sim.DeferAutoLabelReveal = true;
            sim.Epiphany += () => fired++;
            for (int i = 0; i < XgSim.EpiphanyStreak; i++) AnswerGhost(sim, host, true);
            Assert.AreEqual(1, fired);
            Assert.IsTrue(sim.EpiphanyPending);
            Assert.IsTrue(sim.AutoLabelHidden, "still a mystery while the cutscene plays");
            Assert.IsFalse(sim.CanBuyGlobalAuto(out _));
            Assert.IsFalse(sim.GhostGuess(Desk, out _, out _));

            var reloaded = new XgSim(sim.S);
            Assert.IsTrue(reloaded.EpiphanyPending, "a reload before the cutscene keeps it pending");
            Assert.IsTrue(reloaded.RevealAutoLabel());
            Assert.IsFalse(reloaded.RevealAutoLabel(), "only once");
            Assert.IsFalse(reloaded.EpiphanyPending);
            Assert.IsTrue(reloaded.CanBuyGlobalAuto(out _));
        }

        [Test]
        public void LegacySavesThatOwnAutoLabellingCountAsDone()
        {
            var owned = new XgState { autoLevel = 2 };
            var sim = new XgSim(owned);
            Assert.IsTrue(sim.EpiphanyDone); Assert.IsTrue(sim.AutoLabelRevealed); Assert.IsFalse(sim.EpiphanyPending);
            Assert.AreEqual(1, sim.S.epiphanyVersion);

            var coop = new XgState(); coop.unlocked.Add("label.coop");
            Assert.IsTrue(new XgSim(coop).AutoLabelRevealed, "anything behind 自动答题 means it was bought once");

            var plain = new XgSim(new XgState());
            Assert.IsTrue(plain.AutoLabelHidden, "a legacy save without it still has the idea ahead");
            Assert.IsFalse(plain.S.epiphany);

            var broken = new XgState { epiphanyVersion = 1, ghostStreak = -5, ghostLabels = -3 };
            var repaired = new XgSim(broken);
            Assert.AreEqual(0, repaired.S.ghostStreak); Assert.AreEqual(0, repaired.S.ghostLabels);

            var revealedOnly = new XgState { epiphanyVersion = 1, autoLabelRevealed = true };
            Assert.IsTrue(new XgSim(revealedOnly).S.epiphany, "revealed implies the idea happened");
        }

        [Test]
        public void StateSurvivesAReload()
        {
            var sim = Fresh(.8); var host = new Host();
            for (int i = 0; i < 3; i++) AnswerGhost(sim, host, true);
            var again = new XgSim(sim.S);
            Assert.AreEqual(3, again.S.ghostStreak);
            Assert.IsTrue(again.S.ghostSeen);
            Assert.IsTrue(again.AutoLabelHidden);
        }

        [Test]
        public void TheFlashCardHasTextInBothLanguages()
        {
            XgSim.InsightCardText(XgSim.EpiphanyCardId, out int stage, out string name, out string nameEn, out string golden, out string goldenEn, out string why, out string whyEn);
            Assert.AreEqual("让它替我标", name);
            Assert.IsNotEmpty(nameEn); Assert.IsNotEmpty(golden); Assert.IsNotEmpty(goldenEn); Assert.IsNotEmpty(why); Assert.IsNotEmpty(whyEn);
            Assert.That(stage, Is.InRange(1, 6));
        }

        // ───────────── inner monologue ─────────────

        static XgMonologueContext Tired(int labels) => new XgMonologueContext
        {
            handLabels = labels, desk = "spam", deskLabels = labels, hour = 3, minute = 12, money = 5, nextBill = 40, streak = 25,
        };

        [Test]
        public void MonologueIsRateLimitedByLabelsAndTime()
        {
            var m = new XgInnerMonologue(7);
            Assert.IsNull(m.Next(Tired(100), 0), "the first call only starts the clock");
            Assert.IsNull(m.Next(Tired(139), 1000), "39 labels is too few");
            var first = m.Next(Tired(140), 60);
            Assert.IsNotNull(first, "40 labels and a minute after the start");
            Assert.IsNull(m.Next(Tired(400), 60 + XgInnerMonologue.SecondsGap - 1), "enough labels, too soon");
            Assert.IsNull(m.Next(Tired(150), 60 + XgInnerMonologue.SecondsGap * 3), "enough time, too few labels");
            Assert.IsNotNull(m.Next(Tired(180), 60 + XgInnerMonologue.SecondsGap * 3));
        }

        [Test]
        public void MonologueNeverRepeatsALineBackToBackAndVariesTheContext()
        {
            var m = new XgInnerMonologue(11);
            m.Next(Tired(0), 0);
            string last = null; var pools = new HashSet<string>(); var ids = new HashSet<string>();
            int labels = 0; double now = 0;
            for (int i = 0; i < 300; i++)
            {
                labels += XgInnerMonologue.LabelGap; now += XgInnerMonologue.SecondsGap;
                var line = m.Next(Tired(labels), now);
                Assert.IsNotNull(line);
                Assert.AreNotEqual(last, line.id, "never the same line twice in a row");
                last = line.id; pools.Add(line.pool); ids.Add(line.id);
                Assert.IsFalse(line.zh.Contains("{") || line.en.Contains("{"), "placeholders are filled: " + line.zh);
            }
            Assert.AreEqual(4, pools.Count, "night, money, streak and fatigue all come up");
            Assert.Greater(ids.Count, 10);
        }

        [Test]
        public void MonologueFollowsTheContext()
        {
            var calm = new XgMonologueContext { handLabels = 10, desk = "spam", deskLabels = 10, hour = 14, money = 500, nextBill = 20 };
            Assert.IsEmpty(XgInnerMonologue.Pools(calm), "a calm afternoon early on: nothing to think about");
            var m = new XgInnerMonologue(3);
            m.Next(calm, 0);
            calm.handLabels = XgInnerMonologue.FatigueFrom - 1;
            Assert.IsNull(m.Next(calm, 1000), "nothing applies yet");
            calm.handLabels = XgInnerMonologue.FatigueFrom + XgInnerMonologue.LabelGap;
            var tired = m.Next(calm, 1000);
            Assert.IsNotNull(tired, "now the hand is tired");
            Assert.AreEqual(XgInnerMonologue.Fatigue, tired.pool);
            var night = new XgMonologueContext { handLabels = 1, hour = 3, minute = 12, money = 500, nextBill = 20 };
            CollectionAssert.AreEqual(new[] { XgInnerMonologue.Night }, XgInnerMonologue.Pools(night));
            var line = XgInnerMonologue.Fill(Array.Find(XgInnerMonologue.Lines, l => l.id == "night.work"), night);
            Assert.AreEqual("3:12 了，明天还要上班。", line.zh);
            var count = XgInnerMonologue.Fill(Array.Find(XgInnerMonologue.Lines, l => l.id == "fatigue.count"), new XgMonologueContext { desk = "spam", deskLabels = 300 });
            Assert.AreEqual("这已经是第 300 条短信了。", count.zh);
            Assert.AreEqual("That's 300 texts now.", count.en);
            var sevens = XgInnerMonologue.Fill(Array.Find(XgInnerMonologue.Lines, l => l.id == "fatigue.count"), new XgMonologueContext { desk = "mnist", deskLabels = 300 });
            Assert.AreEqual("这已经是第 300 个 7 了。", sevens.zh);
            var broke = new XgMonologueContext { money = 12, nextBill = 30 };
            CollectionAssert.Contains(XgInnerMonologue.Pools(broke), XgInnerMonologue.Money);
        }

        [Test]
        public void EveryMonologueLineHasEnglishAndFits2016()
        {
            var ids = new HashSet<string>(); var zh = new List<string>();
            foreach (var line in XgInnerMonologue.Lines)
            {
                Assert.IsTrue(ids.Add(line.id), "unique id " + line.id);
                Assert.IsFalse(string.IsNullOrWhiteSpace(line.zh), line.id);
                Assert.IsFalse(string.IsNullOrWhiteSpace(line.en), line.id);
                zh.Add(line.zh);
            }
            foreach (var desk in XgCatalog.Desks)
            {
                XgInnerMonologue.Noun(desk.id, out string nounZh, out string nounEn);
                Assert.IsNotEmpty(nounZh); Assert.IsNotEmpty(nounEn);
            }
            zh.Add(XgSim.EpiphanyCardId);
            XgSim.EpiphanyCardText(out string name, out _, out string golden, out _, out string why, out _);
            zh.Add(name); zh.Add(golden); zh.Add(why);
            Assert.IsEmpty(EraLexicon.Audit(zh));
        }
    }
}
