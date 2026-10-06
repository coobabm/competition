using System.Collections.Generic;
using LingGuangV05.Core.Era;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>转包: 网吧 friends labelling under the player's 摆渡众包 account, and 阿杰's click script.</summary>
    public sealed class XgSubcontractTests
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

        /// <summary>Stage 2 with 自动答题 owned: the panel opens on the first tick. Noon on the desktop clock.</summary>
        static XgSim Open(Host host, double checkChance = 0)
        {
            var sim = new XgSim { GoldChance = 0 };
            sim.S.stage = 2;
            sim.S.autoLevel = 1;
            sim.ForcedCheckChance = checkChance;
            sim.ClockOfDaySeconds = 12 * 3600;
            sim.Tick(1, host);
            Assert.IsTrue(sim.SubcontractUnlocked);
            sim.TakeYYLines();
            return sim;
        }

        [Test]
        public void UnlocksAtStageTwoWithAutoLabellingAndAjieAsksInYY()
        {
            var host = new Host();
            var sim = new XgSim { GoldChance = 0 };
            int opened = 0; sim.SubcontractOpened += () => opened++;
            sim.S.autoLevel = 1; sim.Tick(5, host);
            Assert.IsFalse(sim.SubcontractUnlocked, "stage 1");
            sim.S.autoLevel = 0; sim.S.stage = 2; sim.Tick(5, host);
            Assert.IsFalse(sim.SubcontractUnlocked, "no 自动答题 yet");
            Assert.IsFalse(sim.Hire("ajie", host));
            sim.S.autoLevel = 1; sim.Tick(1, host); sim.Tick(5, host);
            Assert.IsTrue(sim.SubcontractUnlocked);
            Assert.AreEqual(1, opened);
            var lines = sim.TakeYYLines();
            Assert.AreEqual(1, lines.Count);
            Assert.AreEqual("ajie", lines[0].from);
            Assert.AreEqual("你那个众包还缺人不？我反正在网吧坐着。", lines[0].zh);
            Assert.IsFalse(string.IsNullOrEmpty(lines[0].en));
            Assert.AreEqual(0, sim.PendingYYLines);
        }

        [Test]
        public void WagesRunEveryMinuteEvenWhileIdle()
        {
            var host = new Host(); var sim = Open(host);
            sim.S.qcFrozen = 1000;
            double before = host.money;
            Assert.IsTrue(sim.Hire("ajie", host));
            Assert.AreEqual(2, before - host.money, 1e-9, "first minute up front");
            sim.Tick(59, host);
            Assert.AreEqual(2, before - host.money, 1e-9);
            sim.Tick(1, host);
            Assert.AreEqual(4, before - host.money, 1e-9);
            sim.Tick(60, host);
            Assert.AreEqual(6, before - host.money, 1e-9);
            Assert.AreEqual(0, sim.Worker("ajie").labelsTotal, "a frozen account: no labels, wages still due");
            StringAssert.Contains("冻结", sim.WorkerIdleReason("ajie"));
            Assert.AreEqual(2, sim.WagesPerMinute, 1e-9);
        }

        [Test]
        public void WorkersWaitForAPendingCaptcha()
        {
            var host = new Host(); var sim = Open(host);
            sim.Hire("xiaogang", host);
            sim.S.qcCaptcha = true; sim.S.qcCaptchaCode = "1234"; sim.S.qcCaptchaLeft = 25;
            Assert.IsTrue(sim.CaptchaPending);
            sim.Tick(20, host);
            Assert.AreEqual(0, sim.Worker("xiaogang").labelsTotal);
            Assert.IsNotNull(sim.WorkerIdleReason("xiaogang"));
        }

        [Test]
        public void WorkersWaitOutTheAutomaticPauseAfterAFailedCaptcha()
        {
            var host = new Host(); var sim = Open(host);
            sim.Hire("ajie", host);
            sim.S.qcAutoPause = 108;
            Assert.Greater(sim.CaptchaPauseLeft, 0);
            for (int i = 0; i < 30; i++) sim.Tick(1, host);
            Assert.AreEqual(0, sim.Worker("ajie").labelsTotal, "the pause holds every label leaving the account");
            Assert.IsNotNull(sim.WorkerIdleReason("ajie"));
            var card = new XgCard { dataset = "mnist", truth = true, guess = true, hasJudgment = true };
            Assert.IsNull(sim.SubmitExternalLabel(card, host), "the platform refuses direct submissions too");
        }

        [Test]
        public void TheForemanThoughtWaitsForARealHire()
        {
            var host = new Host(); var sim = Open(host);
            var hired = new List<string>(); sim.WorkerHired += id => hired.Add(id);
            Assert.AreEqual(0, hired.Count, "opening the panel hires nobody");
            Assert.IsTrue(sim.Hire("xiaogang", host));
            Assert.AreEqual(new[] { "xiaogang" }, hired.ToArray());
            var thought = sim.PeekAfterthought("first.hire", XgSim.WorkerInfo("xiaogang").name, XgSim.WorkerInfo("xiaogang").nameEn);
            StringAssert.Contains(XgSim.WorkerInfo("xiaogang").name, thought.zh);
        }

        [Test]
        public void AnUnpaidWorkerWalksOff()
        {
            var host = new Host { money = 5 }; var sim = Open(host);
            var left = new List<string>(); sim.WorkerLeft += (id, why) => left.Add(id + ":" + why);
            sim.S.qcFrozen = 1000;
            Assert.IsTrue(sim.Hire("boss", host) == false, "noon: the boss is not on shift");
            Assert.IsTrue(sim.Hire("xiaogang", host));
            sim.Tick(60, host);
            Assert.IsFalse(sim.Hired("xiaogang"));
            CollectionAssert.AreEqual(new[] { "xiaogang:unpaid" }, left);
            Assert.AreEqual(2, host.money, 1e-9);
        }

        [Test]
        public void TheBossOnlyWorksTheNightShift()
        {
            var host = new Host(); var sim = Open(host);
            Assert.IsFalse(sim.NightShift);
            Assert.IsFalse(sim.CanHire("boss", host, out string why)); StringAssert.Contains("22:00", why);
            sim.ClockOfDaySeconds = 23 * 3600;
            Assert.IsTrue(sim.Hire("boss", host));
            sim.ClockOfDaySeconds = 3 * 3600;
            sim.Tick(40, host);
            Assert.IsTrue(sim.Hired("boss"));
            Assert.AreEqual(10, sim.Worker("boss").labelsTotal, "one card every 4 s");
            sim.TakeYYLines();
            sim.ClockOfDaySeconds = 6 * 3600 + 1;
            sim.Tick(1, host);
            Assert.IsFalse(sim.Hired("boss"));
            var lines = sim.TakeYYLines();
            Assert.AreEqual(1, lines.Count); Assert.AreEqual("boss", lines[0].from);
            // Without a desktop clock the time of day follows the game clock from 01:47.
            sim.ClockOfDaySeconds = -1;
            sim.Clock = 0; Assert.IsTrue(sim.NightShift);
            sim.Clock = 5 * 3600; Assert.IsFalse(sim.NightShift);
        }

        [Test]
        public void SpeedsAndAccuraciesMatchTheTable()
        {
            var host = new Host(); var sim = Open(host);
            sim.Hire("ajie", host); sim.Hire("xiaogang", host);
            sim.Tick(600, host);
            var a = sim.Worker("ajie"); var x = sim.Worker("xiaogang");
            Assert.AreEqual(200, a.labelsTotal);
            Assert.AreEqual(100, x.labelsTotal);
            Assert.That((double)a.correctTotal / a.labelsTotal, Is.InRange(.78, .92));
            Assert.That((double)x.correctTotal / x.labelsTotal, Is.InRange(.92, 1));
            Assert.AreEqual(300, a.labelsToday + x.labelsToday);
            sim.Today = 20160702; sim.Tick(1, host);
            Assert.That(sim.Worker("ajie").labelsToday, Is.LessThan(5), "a new calendar day resets the counter");
        }

        [Test]
        public void WorkerLabelsGoThroughThePlatformsSpotChecks()
        {
            var host = new Host(); var sim = Open(host, checkChance: 1);
            sim.S.qcCredit = 70; // stays in the 普通 tier: one pay multiplier throughout
            var external = new List<XgAutoRecord>(); sim.ExternalLabelled += (who, r) => external.Add(r);
            var fines = new List<XgQcFine>(); sim.QualityFined += f => fines.Add(f);
            Assert.IsTrue(sim.AssignWorkerDesk("xiaogang", "logic"));
            sim.Hire("xiaogang", host);
            int checkedBefore = sim.S.qcCheckedTotal;
            double samples = sim.Samples("logic");
            sim.Tick(120, host);
            Assert.AreEqual(20, external.Count);
            Assert.AreEqual(checkedBefore + 20, sim.S.qcCheckedTotal, "every worker label was spot-checked");
            int wrong = external.FindAll(r => !r.correct).Count;
            Assert.AreEqual(wrong, fines.Count);
            Assert.AreEqual(wrong, sim.Worker("xiaogang").errorsSeen);
            Assert.AreEqual(samples + 20 - wrong, sim.Samples("logic"), 1e-9, "right labels add samples");
            Assert.AreEqual(0, sim.Noise("logic"), "checked wrong labels never become noise");
            foreach (var r in external) if (r.correct) Assert.AreEqual(sim.PayFor("logic", 1) * sim.QualityPayMultiplier, r.pay, 1e-9);
            Assert.That(sim.LabelRate, Is.GreaterThan(0), "worker labels move the platform's speed meter");
            Assert.AreEqual(0, sim.S.routeHistory.Count, "the model's own routing stats stay untouched");
            Assert.AreEqual(0, sim.S.autoFeed.Count);
        }

        [Test]
        public void UncheckedWrongWorkerLabelsBecomeNoise()
        {
            var host = new Host(); var sim = Open(host, checkChance: 0);
            sim.AssignWorkerDesk("ajie", "spam");
            sim.Hire("ajie", host);
            sim.Tick(300, host);
            var a = sim.Worker("ajie");
            Assert.AreEqual(a.labelsTotal - a.correctTotal, sim.Noise("spam"), 1e-9);
            Assert.AreEqual(0, a.errorsSeen);
        }

        [Test]
        public void WorkerChecksCountForTheSlaClause()
        {
            var host = new Host(); var sim = Open(host, checkChance: 1);
            sim.S.qcCredit = 95;
            sim.S.desksOpen.Add("danmu");
            sim.S.best.Add(new XgBest { dataset = "danmu", acc = .95, arch = "rnn" });
            Assert.IsTrue(sim.SignSla("sla.danmu"));
            sim.AssignWorkerDesk("xiaogang", "danmu");
            sim.Hire("xiaogang", host);
            sim.Tick(60, host);
            Assert.AreEqual(10, sim.SlaChecks);
        }

        [Test]
        public void AjieSwitchesToAClickScriptAfterTwentyMinutesWithAHintThreeMinutesBefore()
        {
            var host = new Host(); var sim = Open(host, checkChance: 0);
            sim.AssignWorkerDesk("ajie", "spam");
            sim.Hire("ajie", host);
            sim.TakeYYLines();
            sim.Tick(XgSim.AjieScriptAfter - XiaogangHint - 2, host);
            Assert.AreEqual(0, sim.SubcontractTwist);
            sim.Tick(3, host);
            Assert.AreEqual(1, sim.SubcontractTwist);
            var hint = sim.TakeYYLines();
            Assert.AreEqual(1, hint.Count); Assert.AreEqual("xiaogang", hint[0].from);
            StringAssert.Contains("阿杰", hint[0].zh); StringAssert.Contains("飞快", hint[0].zh);
            Assert.IsFalse(sim.AjieScripting);
            sim.Tick(XiaogangHint, host);
            Assert.IsTrue(sim.AjieScripting);
            Assert.AreEqual(1.5, sim.WorkerInterval("ajie"), 1e-9);
            Assert.AreEqual(0, sim.PendingYYLines, "the switch itself is quiet");

            var answers = new List<bool>(); sim.ExternalLabelled += (who, r) => answers.Add(r.guess);
            int before = sim.Worker("ajie").labelsTotal;
            sim.Tick(60, host);
            Assert.AreEqual(40, sim.Worker("ajie").labelsTotal - before, "twice as fast");
            Assert.IsTrue(answers.TrueForAll(a => a), "all 是");
            Assert.IsTrue(sim.MonotoneAnswers);

            // Once the platform looks, the identical answers get the account reported as a script.
            var reports = new List<XgReportReason>(); sim.QualityReported += (r, s) => reports.Add(r);
            sim.ForcedCheckChance = 1;
            sim.Tick(150, host);
            Assert.AreEqual(1, reports.Count);
            Assert.AreEqual(XgReportReason.MonotoneAnswers, reports[0]);
            Assert.IsTrue(sim.QualityFrozen);
        }

        const double XiaogangHint = XgSim.XiaogangHintBefore;

        [Test]
        public void FiringAjieAfterTheScriptBringsAnApologyAndEndsTheTwist()
        {
            var host = new Host(); var sim = Open(host, checkChance: 0);
            sim.Hire("ajie", host);
            sim.Tick(XgSim.AjieScriptAfter + 120, host);
            Assert.IsTrue(sim.AjieScripting);
            sim.TakeYYLines();
            double scriptWages = sim.S.scScriptWages, before = host.money;
            Assert.That(scriptWages, Is.GreaterThanOrEqualTo(4));
            Assert.IsTrue(sim.Fire("ajie", host));
            Assert.AreEqual(3, sim.SubcontractTwist);
            Assert.AreEqual(scriptWages, host.money - before, 1e-9, "he gives back the script-time wages");
            var lines = sim.TakeYYLines();
            Assert.AreEqual(1, lines.Count); Assert.AreEqual("ajie", lines[0].from);
            StringAssert.Contains("按键精灵", lines[0].zh);

            // Rehired, he labels by hand for good: the twist happens once per save.
            Assert.IsTrue(sim.Hire("ajie", host));
            var answers = new List<bool>(); sim.ExternalLabelled += (who, r) => answers.Add(r.guess);
            sim.Tick(XgSim.AjieScriptAfter + 60, host);
            Assert.AreEqual(3, sim.SubcontractTwist);
            Assert.AreEqual(3, sim.WorkerInterval("ajie"), 1e-9);
            Assert.IsTrue(answers.Contains(false));
        }

        [Test]
        public void FiringBeforeTheScriptIsQuietAndTheClockKeepsCounting()
        {
            var host = new Host(); var sim = Open(host, checkChance: 0);
            sim.Hire("ajie", host); sim.Tick(600, host);
            sim.TakeYYLines();
            Assert.IsTrue(sim.Fire("ajie", host));
            Assert.AreEqual(0, sim.PendingYYLines);
            Assert.AreEqual(0, sim.SubcontractTwist);
            sim.Tick(600, host);
            Assert.AreEqual(600, sim.AjieWorkedSeconds, 1, "the twist clock only runs while he works for you");
            sim.Hire("ajie", host);
            sim.Tick(XgSim.AjieScriptAfter - 600 + 1, host);
            Assert.IsTrue(sim.AjieScripting);
        }

        [Test]
        public void WorkersGoHomeWhileTheGameIsClosed()
        {
            var host = new Host(); var sim = Open(host);
            sim.Hire("xiaogang", host);
            double money = host.money;
            sim.OfflineSimulation = true;
            sim.Tick(3600, host);
            sim.OfflineSimulation = false;
            Assert.AreEqual(money, host.money, 1e-9);
            Assert.AreEqual(0, sim.Worker("xiaogang").labelsTotal);
            Assert.AreEqual(0, sim.AjieWorkedSeconds);
        }

        [Test]
        public void SubcontractSavesAndRepairs()
        {
            var host = new Host(); var sim = Open(host);
            sim.Hire("ajie", host); sim.Tick(30, host);
            var copy = new XgSim(sim.S);
            Assert.IsTrue(copy.Hired("ajie"));
            Assert.AreEqual(sim.Worker("ajie").labelsTotal, copy.Worker("ajie").labelsTotal);

            var s = copy.S;
            s.scWorkers.Add(new XgWorkerState { id = "ajie", hired = true });
            s.scWorkers.Add(new XgWorkerState { id = "stranger", hired = true });
            s.scWorkers.Add(null);
            s.scWorkers.RemoveAll(w => w != null && w.id == "boss");
            s.scWorkers[0].timer = double.NaN; s.scWorkers[0].wageTimer = 1e9; s.scWorkers[0].correctTotal = 1 << 20; s.scWorkers[0].fines = -3;
            s.scTwist = 9; s.scAjieSeconds = double.PositiveInfinity; s.scRng = 0;
            for (int i = 0; i < 30; i++) s.scOutbox.Add(new XgYYLine { from = "ajie", zh = "嗨" });
            s.scOutbox.Add(new XgYYLine { from = "nobody", zh = "x" });
            var fixedSim = new XgSim(s);
            Assert.AreEqual(3, fixedSim.S.scWorkers.Count);
            CollectionAssert.AreEqual(new[] { "ajie", "xiaogang", "boss" }, fixedSim.S.scWorkers.ConvertAll(w => w.id));
            var a = fixedSim.Worker("ajie");
            Assert.AreEqual(0, a.timer); Assert.AreEqual(XgSim.WageSeconds, a.wageTimer); Assert.AreEqual(a.labelsTotal, a.correctTotal); Assert.AreEqual(0, a.fines);
            Assert.AreEqual(3, fixedSim.SubcontractTwist);
            Assert.AreEqual(0, fixedSim.AjieWorkedSeconds);
            Assert.AreEqual(XgSim.OutboxLimit, fixedSim.PendingYYLines);
            Assert.AreNotEqual(0, fixedSim.S.scRng);

            // A hired worker in a save whose panel is locked cannot keep working.
            var locked = new XgState();
            locked.scWorkers.Add(new XgWorkerState { id = "ajie", desk = "spam", hired = true });
            Assert.IsFalse(new XgSim(locked).Hired("ajie"));
        }

        [Test]
        public void SubcontractLinesAre2016CleanAndBilingual()
        {
            var host = new Host(); var sim = Open(host, checkChance: 0);
            var lines = new List<string>();
            sim.Hire("ajie", host); sim.Hire("xiaogang", host);
            sim.ClockOfDaySeconds = 23 * 3600; sim.Hire("boss", host);
            sim.Tick(XgSim.AjieScriptAfter + 5, host);
            sim.Fire("ajie", host);
            sim.ClockOfDaySeconds = 7 * 3600; sim.Tick(1, host);
            foreach (var l in sim.TakeYYLines()) { lines.Add(l.zh); Assert.IsFalse(string.IsNullOrEmpty(l.en)); Assert.AreNotEqual(l.zh, l.en); }
            Assert.That(lines.Count, Is.GreaterThanOrEqualTo(6));
            Assert.IsEmpty(EraLexicon.Audit(lines));
        }
    }
}
