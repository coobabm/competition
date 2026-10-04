using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>The to-do note's objective engine: what it asks for at each moment, in which order, and what it never says.</summary>
    public sealed class XgGuideTests
    {
        sealed class Host : IXgHost
        {
            public double money, compute = 50;
            public double Money => money; public double Compute => compute; public double VramMB => 1e5; public string Blocker => null;
            public bool Spend(double a) { if (money < a) return false; money -= a; return true; }
            public void Earn(double a) { money += a; }
            public void Train(double s) { }
        }

        static XgGuideStep First(List<XgGuideStep> steps) => steps.Count > 0 ? steps[0] : null;
        static XgGuideStep Find(List<XgGuideStep> steps, string id) => steps.Find(s => s.id == id);

        /// <summary>A stage-one lab with plenty of spam labels, trained and assessed once.</summary>
        static XgSim Trained()
        {
            var sim = new XgSim { GoldChance = 0, PracticeStrict = false };
            sim.S.labels.Add(new XgLabelCount { dataset = "spam", count = 300 });
            sim.S.epochs = 5; sim.S.assessments = 1;
            return sim;
        }

        [Test]
        public void FreshStageOneSuggestsLabelling()
        {
            var sim = new XgSim { GoldChance = 0 };
            var steps = XgGuide.Current(sim, 0);
            var s = First(steps);
            StringAssert.StartsWith("label.", s.id);
            Assert.AreEqual(XgGuideKind.Setup, s.kind);
            Assert.AreEqual(XgGuide.Lab, s.app); Assert.AreEqual("label", s.tab);
            Assert.AreEqual(XgCatalog.SamplesToTrain, s.goal); Assert.AreEqual(0, s.current);
            StringAssert.Contains("（0/12）", s.Text(false));
            StringAssert.Contains("(0/12)", s.Text(true));
            Assert.IsNotEmpty(s.whyZh); Assert.IsNotEmpty(s.whyEn);
            Assert.IsFalse(steps.Exists(x => x.kind == XgGuideKind.Side), "one lonely step leaves no room for a side task");
        }

        [Test]
        public void AfterLabelsItSuggestsTrainingThenAnAssessment()
        {
            var sim = new XgSim { GoldChance = 0, PracticeStrict = false }; var host = new Host();
            sim.SelectDesk("spam");
            for (int i = 0; i < 6; i++) { var c = sim.Card("spam"); sim.Answer("spam", c.truth, host); }
            var half = First(XgGuide.Current(sim, host.money));
            Assert.AreEqual("label.spam", half.id, "the desk being labelled stays the goal");
            Assert.AreEqual(6, half.current);
            for (int i = 0; i < 6; i++) { var c = sim.Card("spam"); sim.Answer("spam", c.truth, host); }
            Assert.IsTrue(sim.TrainingUnlocked(XgTrack.Sequence));

            var train = First(XgGuide.Current(sim, host.money));
            Assert.AreEqual("train.first", train.id);
            Assert.AreEqual("train", train.tab); Assert.AreEqual("sequence", train.arg, "the track that can train");
            StringAssert.StartsWith("label:训练一轮", train.target);

            sim.TrainEpoch(XgTrack.Sequence, host);
            var steps = XgGuide.Current(sim, host.money);
            Assert.IsNull(Find(steps, "train.first"));
            Assert.AreEqual("assess.first", First(steps).id);
        }

        [Test]
        public void ASignableContractSuggestsSigning()
        {
            var sim = Trained();
            Assert.IsFalse(XgGuide.Current(sim, 0).Exists(s => s.id.StartsWith("contract.", StringComparison.Ordinal)));
            sim.S.best.Add(new XgBest { dataset = "spam", acc = .95 });
            var s = First(XgGuide.Current(sim, 0));
            Assert.AreEqual("contract.antifraud", s.id);
            Assert.AreEqual("contracts", s.tab); Assert.AreEqual("contract:antifraud", s.target);
            StringAssert.Contains("诈骗短信拦截", s.zh);
            sim.S.contracts.Add("antifraud");
            Assert.IsNull(Find(XgGuide.Current(sim, 0), "contract.antifraud"), "a signed contract is done");
        }

        [Test]
        public void AnUnpaidBillComesFirst()
        {
            var sim = Trained();
            sim.S.best.Add(new XgBest { dataset = "spam", acc = .95 });
            var steps = XgGuide.Current(sim, 0, new XgGuideHouse { unpaidPower = true, billDue = 12.5, breakerTripped = true });
            Assert.AreEqual("power.bill", steps[0].id);
            Assert.AreEqual(XgGuide.Home, steps[0].app); Assert.AreEqual("power", steps[0].tab);
            StringAssert.Contains("12.5", steps[0].zh);
            Assert.AreEqual("power.breaker", steps[1].id, "then the breaker");
            Assert.IsNotNull(Find(steps, "contract.antifraud"), "the rest of the list stays below");

            var vram = XgGuide.Current(sim, 0, new XgGuideHouse { vramMB = 1 });
            Assert.AreEqual("vram", vram[0].id, "a model too big for the card blocks training");
            StringAssert.Contains("宽度", vram[0].zh);

            var noExe = XgGuide.Current(new XgSim(), 0, new XgGuideHouse { appInstalled = false });
            Assert.AreEqual("install", noExe[0].id); Assert.AreEqual(XgGuide.YY, noExe[0].app);
        }

        [Test]
        public void BeforeTheWallItCountsPracticeEpochs()
        {
            var sim = Trained();
            sim.S.stageEpochs = 7;
            var steps = XgGuide.Current(sim, 0);
            var practice = Find(steps, "practice.1");
            Assert.IsNotNull(practice);
            Assert.AreEqual(7, practice.current); Assert.AreEqual(XgSim.WallPracticeEpochs, practice.goal);
            StringAssert.Contains("（7/20）", practice.Text(false));
            Assert.IsNotNull(Find(steps, "grade.1"), "stage one's wall also waits for a C");
            int side = steps.FindIndex(s => s.kind == XgGuideKind.Side);
            Assert.AreEqual(2, side, "side tasks only ever take the third line");
            Assert.AreEqual("side.forum", steps[side].id);
            Assert.AreEqual(-1, XgGuide.Current(sim, 0, new XgGuideHouse { forumSeen = true, yySeen = true, gamesSeen = true }).FindIndex(s => s.kind == XgGuideKind.Side));
        }

        [Test]
        public void WallHintsEscalateOverTimeAndNeverGiveGoldenNumbers()
        {
            var sim = new XgSim { GoldChance = 0, PracticeStrict = false }; var host = new Host();
            sim.S.unlocked.Add("bias");
            for (int i = 0; i < 40; i++) { var c = sim.Card("spam"); sim.Answer("spam", c.truth, host); }
            for (int i = 0; i < 40 && !sim.WallSeen("combo"); i++) sim.TrainEpoch(XgTrack.Sequence, host);
            Assert.IsTrue(sim.WallSeen("combo"));
            var wall = sim.ActiveWall;
            Assert.IsNotNull(wall);

            XgGuideStep Hint(double seconds, double money, XgGuideHouse house = null)
            {
                sim.S.stageSeconds = sim.S.wallSeenAt + seconds;
                var steps = XgGuide.Current(sim, money, house);
                foreach (var s in steps)
                {
                    if (s.kind != XgGuideKind.Wall) continue;
                    foreach (var text in new[] { s.zh, s.en, s.whyZh, s.whyEn })
                    {
                        foreach (char ch in text) Assert.IsFalse(char.IsDigit(ch), "no numbers in wall steps: " + text);
                        StringAssert.DoesNotContain(wall.golden, text); StringAssert.DoesNotContain(wall.goldenEn, text);
                    }
                }
                return Find(steps, "wall.combo");
            }

            foreach (double money in new[] { 0.0, 1e6 })
            {
                var first = Hint(0, money);
                Assert.AreEqual(0, first.level); Assert.AreEqual("wall", first.tab, "first: the diagnosis page");
                var forum = Hint(XgGuide.ForumAfter + 1, money);
                Assert.AreEqual(1, forum.level); Assert.AreEqual(XgGuide.Tieba, forum.app); Assert.AreEqual("laozhou", forum.arg);
                var post = Hint(XgGuide.NewbieAfter + 1, money);
                Assert.AreEqual(2, post.level); Assert.AreEqual("thread", post.tab); Assert.AreEqual("tip_wall", post.arg);
                var secret = Hint(XgGuide.SecretAfter + 1, money);
                Assert.AreEqual(3, secret.level); Assert.AreEqual("node:secret.1", secret.target);
                Assert.AreEqual(money < 200, secret.HasProgress, "an unaffordable secret shows how far the wallet is");
                Assert.AreEqual(2, Hint(XgGuide.ForumAfter + 1, money, new XgGuideHouse { zhouAvailable = false }).level, "no 周而复始: straight to the posts");
            }

            var steps0 = XgGuide.Current(sim, 0);
            Assert.IsNotNull(Find(steps0, "wall.data.xor"), "the wall's own data must be on the training page");
            var needs = Find(steps0, "needs.combo");
            Assert.IsNotNull(needs, "missing knobs are named");
            StringAssert.Contains("更多层数", needs.zh, "layer caps are named without their number");
            Assert.IsTrue(XgGuide.Current(sim, 1e6).Exists(s => s.id.StartsWith("need.", StringComparison.Ordinal)), "affordable knobs become buy steps");

            sim.S.unlocked.Add("secret.1");
            sim.S.stageSeconds = sim.S.wallSeenAt + XgGuide.SecretAfter + 1;
            Assert.AreEqual(2, Find(XgGuide.Current(sim, 0), "wall.combo").level, "a bought secret is not suggested again");
        }

        [Test]
        public void StageSixWalksThroughPretrainingAlignmentTheTestTheLetterAndTheRules()
        {
            var host = new Host { money = 1e7 };
            var state = new XgState { progressionVersion = XgSim.ProgressionSchemaWalls, stage = 6, stageVision = 6, stageSequence = 6 };
            var sim = new XgSim(state) { GoldChance = 0, PracticeStrict = false };
            foreach (var n in XgCatalog.Nodes) if (n.stage <= 6 && n.tree != "label" && n.kind != XgNodeKind.Secret && n.id != "datacenter" && !sim.Has(n.id)) sim.S.unlocked.Add(n.id);
            var s = sim.S;

            Assert.AreEqual("final.pretrain", First(XgGuide.Current(sim, 0)).id);
            Assert.IsFalse(sim.TogglePretrain(host), "no server room: it trips");
            var dc = First(XgGuide.Current(sim, 10));
            Assert.AreEqual("final.datacenter", dc.id); Assert.AreEqual("node:datacenter", dc.target);
            Assert.IsTrue(dc.money); Assert.AreEqual(10, dc.current); Assert.AreEqual(sim.NodeCost(XgCatalog.Node("datacenter")), dc.goal);

            s.unlocked.Add("datacenter");
            Assert.IsTrue(sim.TogglePretrain(host));
            s.pretrain = .3;
            var wait = First(XgGuide.Current(sim, 0));
            Assert.AreEqual("final.wait", wait.id); Assert.AreEqual(30, wait.current); Assert.AreEqual(100, wait.goal);

            s.pretrain = XgSim.PretrainPlateau;
            Assert.IsFalse(sim.PretrainScaleReady);
            Assert.AreEqual("final.scale", First(XgGuide.Current(sim, 0)).id, "the plateau asks for scale");

            s.pretrainRunning = false; s.pretrain = 1; s.abilities = true;
            var align = First(XgGuide.Current(sim, 0));
            Assert.AreEqual("final.align", align.id); Assert.AreEqual(XgSim.AlignCards, align.goal);

            s.alignDone = XgSim.AlignCards; s.fullOpen = true;
            var exam = First(XgGuide.Current(sim, 0));
            Assert.AreEqual("final.exam", exam.id); Assert.AreEqual(XgSim.ExamQuestions, exam.goal);

            s.examDone = XgSim.ExamQuestions;
            var letter = First(XgGuide.Current(sim, 0));
            Assert.AreEqual("final.letter", letter.id); Assert.AreEqual(XgGuide.Bodu, letter.app);
            StringAssert.DoesNotContain(Prologue2016.LongNumber, letter.zh, "the note does not type the number for you");

            Assert.IsTrue(sim.ReadLetter());
            Assert.AreEqual("final.rules", First(XgGuide.Current(sim, 0)).id);

            Assert.IsTrue(sim.WriteRules(new List<string> { "shutdown" }));
            var done = XgGuide.Current(sim, 0);
            Assert.AreEqual("done", done[0].id);
            Assert.AreEqual(1, done.Count, "after the ending the note has nothing left to ask");
        }

        [Test]
        public void AReportedAccountIsABlockerThatPointsAtTheAppeal()
        {
            var sim = Trained(); sim.S.autoLevel = 1;
            Assert.IsNull(Find(XgGuide.Current(sim, 0), "qc.frozen"));
            sim.S.qcFrozen = 125; sim.S.qcReason = (int)XgReportReason.MonotoneAnswers;
            var s = First(XgGuide.Current(sim, 0));
            Assert.AreEqual("qc.frozen", s.id); Assert.AreEqual(XgGuideKind.Blocker, s.kind);
            Assert.AreEqual(XgGuide.Lab, s.app); Assert.AreEqual("label", s.tab); Assert.AreEqual("name:QcAppeal", s.target);
            StringAssert.Contains("2:05", s.zh); StringAssert.Contains("2:05", s.en);
            StringAssert.Contains("疑似脚本", s.whyZh); Assert.IsNotEmpty(s.whyEn);
            var both = XgGuide.Current(sim, 0, new XgGuideHouse { unpaidPower = true });
            Assert.AreEqual("power.bill", both[0].id); Assert.AreEqual("qc.frozen", both[1].id);
            Assert.IsNull(Find(both, "qc.rate"), "frozen accounts get the freeze step, not the rate step");
            sim.S.autoLevel = 0;
            Assert.IsNull(Find(XgGuide.Current(sim, 0), "qc.frozen"), "no platform before auto labelling");
        }

        [Test]
        public void ALowSpotCheckPassRateSuggestsTheThresholdOrABetterModel()
        {
            var sim = Trained(); sim.S.autoLevel = 1;
            for (int i = 0; i < 19; i++) sim.S.qcChecks.Add(i >= 3);
            Assert.IsNull(Find(XgGuide.Current(sim, 0), "qc.rate"), "nineteen checks are not judged yet");
            sim.S.qcChecks.Add(true);
            var s = Find(XgGuide.Current(sim, 0), "qc.rate");
            Assert.IsNotNull(s, "3 failures in 20 is on the 15% line");
            Assert.AreEqual(XgGuideKind.Main, s.kind);
            Assert.AreEqual("tree", s.tab); Assert.AreEqual("node:label.coop", s.target, "without the node, point at the node");
            StringAssert.Contains("拿不准才问我", s.zh); StringAssert.Contains("85%", s.whyZh); StringAssert.Contains("20%", s.whyEn);
            sim.S.unlocked.Add("label.coop");
            s = Find(XgGuide.Current(sim, 0), "qc.rate");
            Assert.AreEqual("label", s.tab); Assert.AreEqual("name:ThresholdSlider", s.target);
            for (int i = 0; i < 2; i++) sim.S.qcChecks.Add(true);
            Assert.IsNull(Find(XgGuide.Current(sim, 0), "qc.rate"), "3 in 22 is under the line");
        }

        [Test]
        public void APendingCaptchaIsABlockerThatPointsAtTheCaptchaCard()
        {
            var sim = Trained(); sim.S.autoLevel = 1;
            Assert.IsNull(Find(XgGuide.Current(sim, 0), "qc.captcha"));
            sim.S.qcCaptcha = true; sim.S.qcCaptchaCode = "4821"; sim.S.qcCaptchaLeft = 25;
            var s = First(XgGuide.Current(sim, 0));
            Assert.AreEqual("qc.captcha", s.id); Assert.AreEqual(XgGuideKind.Blocker, s.kind);
            Assert.AreEqual("label", s.tab); Assert.AreEqual("name:QcCaptcha", s.target);
            StringAssert.Contains("0:25", s.zh); StringAssert.Contains("0:25", s.en);
            StringAssert.DoesNotContain("4821", s.zh, "the note never types the captcha for you");
        }

        [Test]
        public void EveryStepHasBothLanguagesAndAReason()
        {
            var sims = new List<XgSim> { new XgSim(), Trained() };
            var t = Trained(); t.S.best.Add(new XgBest { dataset = "spam", acc = .95 }); sims.Add(t);
            var frozen = Trained(); frozen.S.autoLevel = 1; frozen.S.qcFrozen = 60; sims.Add(frozen);
            var warned = Trained(); warned.S.autoLevel = 1; for (int i = 0; i < 20; i++) warned.S.qcChecks.Add(i > 3); sims.Add(warned);
            var captcha = Trained(); captcha.S.autoLevel = 1; captcha.S.qcCaptcha = true; captcha.S.qcCaptchaCode = "1234"; captcha.S.qcCaptchaLeft = 20; sims.Add(captcha);
            foreach (var sim in sims)
                foreach (var money in new[] { 0.0, 50, 1e6 })
                    foreach (var s in XgGuide.Current(sim, money, new XgGuideHouse { unpaidPower = true, breakerTripped = true, overloaded = true, noGpu = true }))
                    {
                        Assert.IsNotEmpty(s.zh, s.id); Assert.IsNotEmpty(s.en, s.id);
                        Assert.IsNotEmpty(s.whyZh, s.id); Assert.IsNotEmpty(s.whyEn, s.id);
                    }
        }
    }
}
