using System.Collections.Generic;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>人机验证: the speed meter, the captcha, its pauses and reports, and 验证码代填.</summary>
    public sealed class XgCaptchaTests
    {
        sealed class Host : IXgHost
        {
            public double money = 100000;
            public double Compute => 1;
            public double VramMB => 16000;
            public double Money => money;
            public bool Spend(double amount) { if (amount < 0 || money < amount) return false; money -= amount; return true; }
            public void Earn(double amount) { money += amount; }
            public void Train(double seconds) { }
            public string Blocker => null;
        }

        static XgSim Ready()
        {
            var sim = new XgSim { GoldChance = 0 };
            sim.S.desksOpen.Add("mnist");
            sim.S.autoLevel = 1;
            sim.S.best.Add(new XgBest { dataset = "mnist", acc = .8, arch = "perceptron" });
            sim.S.best.Add(new XgBest { dataset = "spam", acc = .8, arch = "perceptron" });
            sim.ForcedCheckChance = 0;
            return sim;
        }

        /// <summary>One second of play with this many hand labels in it.</summary>
        static void Second(XgSim sim, Host host, int handLabels)
        {
            for (int i = 0; i < handLabels; i++) { var c = sim.Card("spam"); sim.Answer("spam", c.truth, host); }
            sim.TickCollaboration(1, host);
        }

        /// <summary>A captcha right now: a full meter, a fast minute and no cooldown.</summary>
        static void Require(XgSim sim, Host host)
        {
            for (int i = 0; i < XgSim.SpeedWindow; i++) sim.S.qcSpeed[i] = 3;
            sim.S.qcSuspicion = 1; sim.S.qcCaptchaCooldown = 0;
            sim.TickCollaboration(.01, host);
            Assert.IsTrue(sim.CaptchaPending);
        }

        static string Wrong(string code) => code == "0000" ? "1111" : "0000";

        [Test]
        public void TheMeterFillsWhileTheMinuteIsFasterThanOneAndAHalfLabelsASecond()
        {
            var sim = Ready(); var host = new Host();
            int asked = 0; sim.CaptchaRequired += () => asked++;
            for (int t = 0; t < 30; t++) Second(sim, host, 3);
            Assert.IsFalse(sim.CaptchaPending, "half a minute is not enough");
            Assert.Greater(sim.LabelRate, XgSim.SpeedLimit);
            Assert.Greater(sim.Suspicion, 0);
            for (int t = 0; t < 240 && !sim.CaptchaPending; t++) Second(sim, host, 3);
            Assert.IsTrue(sim.CaptchaPending); Assert.AreEqual(1, asked);
            Assert.AreEqual(4, sim.CaptchaCode.Length); Assert.AreEqual(XgSim.CaptchaTimeout, sim.CaptchaSecondsLeft, 1e-9);
            foreach (char c in sim.CaptchaCode) Assert.IsTrue(char.IsDigit(c));
            Assert.AreEqual(0, sim.Suspicion);
        }

        [Test]
        public void ASlowMinuteDrainsTheMeter()
        {
            var sim = Ready(); var host = new Host();
            sim.S.qcSuspicion = .5;
            for (int t = 0; t < 30; t++) Second(sim, host, 1);
            Assert.Less(sim.Suspicion, .5);
            Assert.IsFalse(sim.CaptchaPending);
            Assert.AreEqual(.5 - 30 / XgSim.SuspicionDrainSeconds, sim.Suspicion, .02);
        }

        [Test]
        public void APendingCaptchaPausesAutomaticRoutingButNotHandLabels()
        {
            var sim = Ready(); var host = new Host();
            Require(sim, host);
            Assert.IsTrue(sim.AutomationHeld);
            Assert.AreEqual(0, sim.CollaborationRate("spam", host));
            sim.FillJudgmentBuffer();
            var card = sim.S.prefetch.Find(c => c.dataset == "spam" && c.hasJudgment);
            Assert.IsFalse(sim.Route(card.id, host));
            double money = host.money; var hand = sim.Card("spam");
            Assert.IsTrue(sim.Answer("spam", hand.truth, host).correct); Assert.Greater(host.money, money);
        }

        [Test]
        public void ARightAnswerResumesAndAddsCredit()
        {
            var sim = Ready(); var host = new Host();
            Require(sim, host);
            bool? auto = null; sim.CaptchaSolved += a => auto = a;
            double credit = sim.Credit;
            Assert.IsTrue(sim.SubmitCaptcha(" " + sim.CaptchaCode[0] + "-" + sim.CaptchaCode.Substring(1)), "only digits count");
            Assert.IsFalse(sim.CaptchaPending); Assert.IsFalse(sim.AutomationHeld);
            Assert.AreEqual(credit + XgSim.CaptchaCreditPass, sim.Credit, 1e-9);
            Assert.AreEqual(false, auto);
            Assert.Greater(sim.CollaborationRate("spam", host), 0);
            Assert.IsFalse(sim.SubmitCaptcha("1234"), "nothing to answer");
        }

        [Test]
        public void AWrongOrLateAnswerPausesTwoMinutesAndCostsCredit()
        {
            var sim = Ready(); var host = new Host();
            var fails = new List<(bool timeout, int row)>(); sim.CaptchaFailed += (t, n) => fails.Add((t, n));
            Require(sim, host);
            double credit = sim.Credit;
            Assert.IsFalse(sim.SubmitCaptcha(Wrong(sim.CaptchaCode)));
            Assert.AreEqual(credit - XgSim.CaptchaCreditFail, sim.Credit, 1e-9);
            Assert.AreEqual(XgSim.CaptchaPause, sim.CaptchaPauseLeft, 1e-9);
            Assert.IsTrue(sim.AutomationHeld); Assert.AreEqual(0, sim.CollaborationRate("spam", host));
            for (int t = 0; t < 119; t++) sim.TickCollaboration(1, host);
            Assert.IsTrue(sim.AutomationHeld);
            sim.TickCollaboration(1, host);
            Assert.IsFalse(sim.AutomationHeld, "two minutes later automatic labelling resumes");

            Require(sim, host);
            for (int t = 0; t < 29; t++) sim.TickCollaboration(1, host);
            Assert.IsTrue(sim.CaptchaPending); Assert.AreEqual(1, sim.CaptchaSecondsLeft, 1e-6);
            sim.TickCollaboration(1, host);
            Assert.IsFalse(sim.CaptchaPending);
            Assert.AreEqual(2, fails.Count); Assert.IsFalse(fails[0].timeout); Assert.IsTrue(fails[1].timeout);
            Assert.AreEqual(2, fails[1].row);
        }

        [Test]
        public void ThreeFailuresInARowAreReportedAsABot()
        {
            var sim = Ready(); var host = new Host();
            var reasons = new List<XgReportReason>(); sim.QualityReported += (r, s) => reasons.Add(r);
            Require(sim, host); sim.SubmitCaptcha(Wrong(sim.CaptchaCode));
            Require(sim, host); Assert.IsTrue(sim.SubmitCaptcha(sim.CaptchaCode), "a right answer resets the streak");
            for (int i = 0; i < 2; i++) { Require(sim, host); sim.SubmitCaptcha(Wrong(sim.CaptchaCode)); }
            Assert.AreEqual(0, reasons.Count);
            Require(sim, host); sim.SubmitCaptcha(Wrong(sim.CaptchaCode));
            CollectionAssert.AreEqual(new[] { XgReportReason.SuspectedBot }, reasons);
            Assert.IsTrue(sim.QualityFrozen); Assert.AreEqual(XgReportReason.SuspectedBot, sim.LastReportReason);
            Assert.AreEqual(0, sim.CaptchaPauseLeft, "the freeze replaces the pause");
            Assert.AreEqual(0, sim.S.qcCaptchaFails);
        }

        [Test]
        public void CaptchasAreAtLeastSixMinutesApart()
        {
            var sim = Ready(); var host = new Host();
            Require(sim, host);
            Assert.IsTrue(sim.SubmitCaptcha(sim.CaptchaCode));
            for (int t = 0; t < (int)XgSim.CaptchaGap - 10; t++) Second(sim, host, 3);
            Assert.IsFalse(sim.CaptchaPending, "not within six minutes of the last one");
            Assert.AreEqual(1, sim.Suspicion, 1e-9);
            for (int t = 0; t < 15 && !sim.CaptchaPending; t++) Second(sim, host, 3);
            Assert.IsTrue(sim.CaptchaPending);
            Assert.AreEqual(2, sim.S.qcCaptchaTotal);
        }

        [Test]
        public void NoCaptchaWhileTheGameCatchesUpOnClosedTime()
        {
            var sim = Ready(); var host = new Host();
            for (int i = 0; i < XgSim.SpeedWindow; i++) sim.S.qcSpeed[i] = 3;
            sim.S.qcSuspicion = 1;
            sim.OfflineSimulation = true;
            sim.TickCollaboration(1, host);
            Assert.IsFalse(sim.CaptchaPending);
        }

        [Test]
        public void AutofillNeedsStageThreeAndANinetyFivePercentDigitsCheckpoint()
        {
            var sim = Ready(); var host = new Host(); sim.S.unlocked.Add("label.coop");
            var node = XgCatalog.Node(XgSim.CaptchaAutofillNode);
            Assert.IsNotNull(node);
            sim.S.stage = 2; sim.S.best.Find(b => b.dataset == "mnist").acc = .99;
            Assert.AreEqual(XgSim.NodeStatus.Locked, sim.StatusLabelNode(node, host));
            StringAssert.Contains("95%", sim.WhyLabelNode(node, host));
            sim.S.stage = 3; sim.S.best.Find(b => b.dataset == "mnist").acc = .94;
            Assert.AreEqual(XgSim.NodeStatus.Locked, sim.StatusLabelNode(node, host));
            sim.S.best.Find(b => b.dataset == "mnist").acc = .95;
            Assert.AreEqual(XgSim.NodeStatus.Buyable, sim.StatusLabelNode(node, host));
        }

        [Test]
        public void AutofillAnswersAfterAHumanLikeDelay()
        {
            var sim = Ready(); var host = new Host(); sim.S.unlocked.Add(XgSim.CaptchaAutofillNode);
            sim.ForcedAutofillRoll = .5;
            bool? auto = null; sim.CaptchaSolved += a => auto = a;
            Require(sim, host);
            Assert.That(sim.S.qcAutofillIn, Is.InRange(XgSim.AutofillMinDelay, XgSim.AutofillMaxDelay));
            double credit = sim.Credit;
            for (int i = 0; i < 29; i++) sim.TickCollaboration(.1, host);
            Assert.IsTrue(sim.CaptchaPending, "never faster than three seconds");
            for (int i = 0; i < 40 && sim.CaptchaPending; i++) sim.TickCollaboration(.1, host);
            Assert.IsFalse(sim.CaptchaPending); Assert.AreEqual(true, auto);
            Assert.AreEqual(credit + XgSim.CaptchaCreditPass, sim.Credit, 1e-9);
            Assert.AreEqual(1, sim.S.qcAutofills); Assert.AreEqual(0, sim.S.qcAutofillFlags);
        }

        [Test]
        public void AFlaggedAutofillCostsTenCreditButNoFreeze()
        {
            var sim = Ready(); var host = new Host(); sim.S.unlocked.Add(XgSim.CaptchaAutofillNode);
            int flagged = 0; sim.CaptchaFlagged += () => flagged++;
            sim.ForcedAutofillRoll = .05;
            Require(sim, host);
            double credit = sim.Credit;
            sim.TickCollaboration(6.5, host);
            Assert.IsFalse(sim.CaptchaPending);
            Assert.AreEqual(1, flagged);
            Assert.AreEqual(credit + XgSim.CaptchaCreditPass - XgSim.AutofillFlagCredit, sim.Credit, 1e-9);
            Assert.IsFalse(sim.QualityFrozen); Assert.IsFalse(sim.AutomationHeld);
        }

        [Test]
        public void AboutEightPercentOfAutofillsAreFlagged()
        {
            var sim = Ready(); var host = new Host(); sim.S.unlocked.Add(XgSim.CaptchaAutofillNode);
            for (int i = 0; i < 2000; i++) { sim.S.qcCredit = 80; Require(sim, host); sim.TickCollaboration(6.5, host); }
            Assert.AreEqual(2000, sim.S.qcAutofills);
            Assert.AreEqual(XgSim.AutofillFlagChance, sim.S.qcAutofillFlags / 2000.0, .02);
        }

        [Test]
        public void CaptchaStateIsRepairedOnLoad()
        {
            var sim = Ready(); var host = new Host();
            Require(sim, host);
            sim.TickCollaboration(20, host);
            var reloaded = new XgSim(sim.S);
            Assert.IsTrue(reloaded.CaptchaPending);
            Assert.AreEqual(XgSim.CaptchaTimeout, reloaded.CaptchaSecondsLeft, 1e-9, "a waiting captcha gets a fresh clock");
            var bad = new XgState { autoLevel = 1, qcCaptcha = true, qcCaptchaCode = "12a", qcSuspicion = double.NaN, qcAutoPause = 1e9, qcSpeed = null, qcSpeedHead = 99, qcCaptchaFails = 7 };
            var fixedSim = new XgSim(bad);
            Assert.IsFalse(fixedSim.CaptchaPending); Assert.AreEqual(0, fixedSim.Suspicion);
            Assert.AreEqual(XgSim.CaptchaPause, fixedSim.CaptchaPauseLeft, 1e-9);
            Assert.AreEqual(XgSim.SpeedWindow, fixedSim.S.qcSpeed.Count); Assert.AreEqual(0, fixedSim.S.qcSpeedHead);
            Assert.AreEqual(XgSim.CaptchaFailsToReport - 1, fixedSim.S.qcCaptchaFails);
        }
    }
}
