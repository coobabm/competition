using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>The resident spider's rules (XgResident.cs): presence, growth, privacy, the perch and the watching mood.</summary>
    public sealed class XgResidentTests
    {
        [Test]
        public void ItGrowsWithTheAbilities()
        {
            Assert.AreEqual(4, XgResident.LegCount(1));
            Assert.AreEqual(6, XgResident.LegCount(2));
            Assert.AreEqual(6, XgResident.LegCount(4));
            Assert.AreEqual(8, XgResident.LegCount(5));
            Assert.AreEqual(8, XgResident.LegCount(6));
            double last = 0;
            for (int a = 1; a <= 6; a++)
            {
                Assert.That(XgResident.Size(a), Is.GreaterThan(last), "grows at ability " + a);
                last = XgResident.Size(a);
            }
            Assert.AreEqual(.7, XgResident.Size(1), 1e-9);
            Assert.AreEqual(1.2, XgResident.Size(6), 1e-9);
            Assert.AreEqual(XgResident.Size(6), XgResident.Size(9), 1e-9);
            Assert.AreEqual(1, new XgSim().AbilitiesCount, "a new lab starts at the first ability: four legs");
        }

        [Test]
        public void ItIsOnlyThereOnceInstalledAndSetUpAndNothingOwnsTheScreen()
        {
            Assert.IsFalse(XgResident.Present(false, false, false, false), "not installed");
            Assert.IsFalse(XgResident.Present(true, true, false, false), "prologue / setup");
            Assert.IsFalse(XgResident.Present(true, false, true, false), "cutscene");
            Assert.IsFalse(XgResident.Present(true, false, false, true), "fullscreen video");
            Assert.IsTrue(XgResident.Present(true, false, false, false));
        }

        [Test]
        public void ItKeepsOffQingwensChatOnlyAfterTheNo()
        {
            var s = new XgState();
            Assert.IsTrue(XgResident.MayReadQingwen(s), "before the love question");
            s.loveConsent = 1;
            Assert.IsTrue(XgResident.MayReadQingwen(s), "with consent");
            s.loveConsent = -1;
            Assert.IsFalse(XgResident.MayReadQingwen(s), "【算了】");
        }

        [Test]
        public void ItPerchesBesideTheCardOnTheScreen()
        {
            var screen = new XgBox(-960, -515, 1920, 1030);
            var card = new XgBox(-200, -100, 400, 300);
            XgResident.Perch(card, screen, 1, out double x, out double y, out bool left);
            Assert.IsTrue(left, "right of the card, facing it");
            Assert.That(x, Is.GreaterThan(card.XMax));
            Assert.That(y, Is.InRange(card.y, card.YMax));
            Assert.IsFalse(card.Contains(x, y));

            var edge = new XgBox(600, -100, 340, 300);
            XgResident.Perch(edge, screen, 1, out x, out y, out left);
            Assert.IsFalse(left, "no room on the right: it sits on the left");
            Assert.That(x, Is.LessThan(edge.x));
            Assert.IsTrue(screen.Contains(x, y));

            XgResident.ParkingSpot(screen, 1, out x, out y);
            Assert.IsTrue(screen.Contains(x, y));
            Assert.That(x, Is.GreaterThan(screen.CenterX));
            Assert.That(y, Is.LessThan(screen.CenterY));
        }

        [Test]
        public void ItComesToWatchLeansInPondersAndWandersOff()
        {
            var m = new XgResidentMind();
            Assert.AreEqual(XgResidentMode.Hidden, m.Tick(.1, false, false, true, 0, false));
            Assert.AreEqual(XgResidentMode.Roam, m.Tick(.1, true, false, false, 0, false));
            Assert.AreEqual(XgResidentMode.Watch, m.Tick(.1, true, false, true, 0, false), "the 标注台 came up");
            Assert.IsFalse(m.Leaning);
            m.Tick(XgResidentMind.HesitateAfter, true, false, true, 0, false);
            Assert.IsTrue(m.Leaning, "the player is taking a while");
            m.Tick(.1, true, false, true, 1, false);
            Assert.AreEqual(1, m.Pulses, "a pulse after the answer");
            Assert.IsFalse(m.Leaning);
            m.Tick(.1, true, false, true, 2, true);
            Assert.That(m.Pondering, Is.GreaterThan(0), "answered against its guess");
            Assert.IsFalse(m.Leaning, "pondering, not leaning");
            for (int i = 0; i < 9; i++) m.Tick(1, true, false, true, 2 + i, false);
            Assert.AreEqual(XgResidentMode.Watch, m.Mode, "answers keep coming");
            Assert.AreEqual(XgResidentMode.Roam, m.Tick(XgResidentMind.WanderAfter + .1, true, false, true, 10, false), "no answer for a while");
            Assert.AreEqual(XgResidentMode.Watch, m.Tick(.1, true, false, true, 11, false), "back when answering resumes");
            Assert.AreEqual(XgResidentMode.Roam, m.Tick(.1, true, false, false, 11, false), "the 标注台 closed");
            Assert.AreEqual(XgResidentMode.Parked, m.Tick(.1, true, true, true, 11, false), "told to keep quiet");
            Assert.AreEqual(XgResidentMode.Hidden, m.Tick(.1, false, true, true, 11, false));
        }

        [Test]
        public void QuietIsSavedWithTheLab()
        {
            var sim = new XgSim();
            Assert.IsFalse(sim.S.residentQuiet, "old saves and new games: it roams");
            sim.S.residentQuiet = true;
            Assert.IsTrue(new XgSim(sim.S).S.residentQuiet);
        }

        // ───── the lab side: first appearance and answering for the player (XgSim.Spider.cs) ─────

        sealed class Host : IXgHost
        {
            public double money = 1000;
            public double Compute => 1;
            public double VramMB => 16000;
            public double Money => money;
            public bool Spend(double amount) { if (amount < 0 || money < amount) return false; money -= amount; return true; }
            public void Earn(double amount) { money += amount; }
            public void Train(double seconds) { }
            public string Blocker => null;
        }

        static XgSim WithCheckpoint(string desk, double acc)
        {
            var sim = new XgSim { GoldChance = 0 };
            sim.S.stage = sim.S.stageVision = sim.S.stageSequence = 3;
            sim.S.desksOpen.Add(desk);
            sim.S.best.Add(new XgBest { dataset = desk, arch = "rnn", acc = acc });
            return sim;
        }

        [Test]
        public void ItFirstShowsUpALittleIntoStageOneWhenNothingElsePlays()
        {
            var sim = new XgSim();
            Assert.IsFalse(sim.SpiderIntroDue(true, false, false), "no epoch trained yet");
            sim.S.epochs = XgSim.SpiderIntroEpochs;
            Assert.IsTrue(sim.SpiderIntroDue(true, false, false));
            Assert.IsFalse(sim.SpiderIntroDue(false, false, false), "灵光.exe not installed");
            Assert.IsFalse(sim.SpiderIntroDue(true, true, false), "setup not done");
            Assert.IsFalse(sim.SpiderIntroDue(true, false, true), "something else is playing");
            Assert.IsFalse(sim.SpiderAround, "no spider before its first appearance");
            sim.S.spiderIntroDone = true;
            Assert.IsFalse(sim.SpiderIntroDue(true, false, false), "plays once");
            Assert.IsTrue(sim.SpiderAround);
            Assert.IsTrue(new XgSim(sim.S).S.spiderIntroDone, "saved with the lab");
        }

        [Test]
        public void ItTakesMoreCardsAsItGrows()
        {
            Assert.AreEqual(.08, XgSim.SpiderLabelChance(1), 1e-9);
            double last = 0;
            for (int a = 1; a <= 6; a++) { Assert.That(XgSim.SpiderLabelChance(a), Is.GreaterThan(last)); last = XgSim.SpiderLabelChance(a); }
            Assert.That(XgSim.SpiderLabelChance(9), Is.LessThanOrEqualTo(.3));
            Assert.IsTrue(XgSim.SpiderTakesCard(1, .05));
            Assert.IsFalse(XgSim.SpiderTakesCard(1, .09));
            Assert.IsTrue(XgSim.SpiderTakesCard(6, .25));
        }

        [Test]
        public void ItLeavesDesksAboveItsAbilityToThePlayer()
        {
            var sim = WithCheckpoint("spam", .5); var host = new Host();
            Assert.AreEqual("ability", sim.SpiderAnswerBlocker("spam", host));
            Assert.IsFalse(sim.SpiderAnswer("spam", host).accepted);
            Assert.AreEqual(1000, host.money);
            Assert.AreEqual("ability", sim.SpiderAnswerBlocker("danmu", host), "the free stage-two teaching desk is open but has no checkpoint");
            Assert.IsFalse(sim.DeskOpen("review"));
            Assert.AreEqual("closed", sim.SpiderAnswerBlocker("review", host));
            var ok = WithCheckpoint("spam", .9);
            Assert.IsNull(ok.SpiderAnswerBlocker("spam", host));
            ok.S.residentQuiet = true;
            Assert.AreEqual("quiet", ok.SpiderAnswerBlocker("spam", host), "the quiet toggle turns it off too");
        }

        [Test]
        public void ItAnswersWithTheCheckpointsGuessAndSettlesLikeAnAutoLabel()
        {
            var sim = WithCheckpoint("spam", .85); var host = new Host();
            int hand = sim.S.handCorrect + sim.S.handWrong, combo = sim.S.combo, right = 0, n = 300;
            for (int i = 0; i < n; i++)
            {
                // A text from the family is the player's own opinion to give: the spider never presses it.
                while (sim.Card("spam").family) { Assert.AreEqual("special", sim.SpiderAnswerBlocker("spam", host)); sim.Answer("spam", false, host); }
                var card = sim.Card("spam");
                bool truth = card.truth; long id = card.id;
                Assert.IsTrue(sim.Suggestion("spam", out bool guess, out _));
                double before = host.money;
                double autoRight = sim.S.autoCorrect;
                var r = sim.SpiderAnswer("spam", host);
                Assert.IsTrue(r.accepted);
                Assert.AreEqual(id, r.cardId);
                Assert.AreEqual(guess, r.yes, "it presses what the checkpoint guessed");
                Assert.AreEqual(r.yes == truth, r.correct);
                if (r.correct)
                {
                    right++;
                    Assert.AreEqual(sim.PayFor("spam", card.level) * sim.QualityPayMultiplier, r.pay, 1e-9, "automatic pay, no raise multiplier");
                    Assert.AreEqual(autoRight + 1, sim.S.autoCorrect);
                }
                else Assert.AreEqual(0, r.pay);
                Assert.AreEqual(before + r.pay - r.fine, host.money, 1e-9);
                Assert.AreNotEqual(id, sim.Card("spam").id, "the next card comes up");
            }
            Assert.AreEqual(hand, sim.S.handCorrect + sim.S.handWrong, "hand counts untouched");
            Assert.AreEqual(combo, sim.S.combo, "no combo");
            Assert.AreEqual(n, sim.S.spiderAnswers);
            Assert.That(right / (double)n, Is.InRange(.6, 1.0), "about as right as an 85% checkpoint");
            Assert.That(right, Is.LessThan(n), "and not always right");
        }
    }
}
