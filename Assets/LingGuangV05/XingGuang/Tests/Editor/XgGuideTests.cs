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

        /// <summary>A lab with plenty of spam labels, trained and assessed once (so the 科技 page is open). Stage one unless the test needs the crowd stage.</summary>
        static XgSim Trained(int stage = 1)
        {
            var sim = new XgSim { GoldChance = 0 };
            sim.S.labels.Add(new XgLabelCount { dataset = "spam", count = 300 });
            sim.S.epochs = 5; sim.S.assessments = 1;
            sim.S.treeIntroDelivered = true;
            // These routing tests cover the established plan after the one-time explanations.
            sim.S.parameterLessonDone=sim.S.sampleLessonDone=true;
            if (stage > 1)
            {
                // The abilities up to the stage have emerged and their milestone payments are not what these tests are about.
                sim.S.stage = stage; sim.S.fundedAbility = stage;
                sim.Tick(.01, new Host { money = 1e6 });
                Assert.AreEqual(stage, sim.AbilitiesCount);
            }
            return sim;
        }

        /// <summary>The first stage with the crowd platform (checks, captchas, freezes) and signable orders.</summary>
        static int CrowdStage => XgSim.FeatureUnlock["quality"];

        [Test]
        public void FreshStageOneSuggestsLabelling()
        {
            var sim = new XgSim { GoldChance = 0 };
            var steps = XgGuide.Current(sim, 0);
            var s = First(steps);
            StringAssert.StartsWith("label.", s.id);
            Assert.AreEqual(XgGuideKind.Setup, s.kind);
            Assert.AreEqual(XgGuide.Lab, s.app, "teaching happens on the lab's own 教 page"); Assert.AreEqual("teach", s.tab);
            Assert.AreEqual(XgCatalog.SamplesToTrain, s.goal); Assert.AreEqual(0, s.current);
            StringAssert.Contains("（0/" + XgCatalog.SamplesToTrain + "）", s.Text(false));
            StringAssert.Contains("(0/" + XgCatalog.SamplesToTrain + ")", s.Text(true));
            Assert.IsNotEmpty(s.whyZh); Assert.IsNotEmpty(s.whyEn);
            Assert.IsFalse(steps.Exists(x => x.kind == XgGuideKind.Side), "one lonely step leaves no room for a side task");
        }

        [Test]
        public void AfterLabelsItSuggestsTrainingThenAnAssessment()
        {
            var sim = new XgSim { GoldChance = 0 }; var host = new Host();
            sim.S.parameterLessonDone=sim.S.sampleLessonDone=true;
            sim.SelectDesk("spam");
            int half = XgCatalog.SamplesToTrain / 2;
            for (int i = 0; i < half; i++) { var c = sim.Card("spam"); sim.Answer("spam", c.truth, host); }
            var partway = First(XgGuide.Current(sim, host.money));
            Assert.AreEqual("label.spam", partway.id, "the desk being labelled stays the goal");
            Assert.AreEqual(half, partway.current);
            for (int i = half; i < XgCatalog.SamplesToTrain; i++) { var c = sim.Card("spam"); sim.Answer("spam", c.truth, host); }
            Assert.IsTrue(sim.TrainingUnlocked(XgTrack.Sequence));

            var train = First(XgGuide.Current(sim, host.money));
            Assert.AreEqual("train.first", train.id);
            Assert.AreEqual("train", train.tab); Assert.AreEqual("sequence", train.arg, "the track that can train");
            StringAssert.StartsWith("label:训练一轮", train.target);

            sim.TrainEpoch(XgTrack.Sequence, host);
            var steps = XgGuide.Current(sim, host.money);
            Assert.IsNull(Find(steps, "train.first"));
            // One press trains and assesses; the first record opens the 科技 page, and the road to the next ability follows.
            Assert.Greater(sim.S.assessments, 0);
            Assert.IsNull(Find(steps, "assess.first"));
            Assert.AreEqual("ability.study", First(steps).id, "the study route comes first: one tech purchase at a time");
            sim.S.assessments = 0;
            Assert.AreEqual("assess.first", First(XgGuide.Current(sim, host.money)).id, "an epoch that was not assessed asks for one");
        }

        [Test]
        public void ASignableContractSuggestsSigning()
        {
            var sim = Trained(CrowdStage);
            sim.AppReceipt(XgSim.CrowdApp).installed = true; // orders are signed in 摆渡众包
            Assert.IsFalse(XgGuide.Current(sim, 0).Exists(s => s.id.StartsWith("contract.", StringComparison.Ordinal)));
            sim.S.best.Add(new XgBest { dataset = "spam", acc = .95 });
            var s = First(XgGuide.Current(sim, 0));
            Assert.AreEqual("contract.antifraud", s.id);
            Assert.AreEqual(XgGuide.Crowd, s.app, "orders live on 摆渡众包"); Assert.AreEqual("contracts", s.tab); Assert.AreEqual("contract:antifraud", s.target);
            StringAssert.Contains("诈骗短信拦截", s.zh);
            sim.S.contracts.Add("antifraud");
            Assert.IsNull(Find(XgGuide.Current(sim, 0), "contract.antifraud"), "a signed contract is done");
        }

        [Test]
        public void AnUnpaidBillComesFirst()
        {
            var sim = Trained(CrowdStage);
            sim.AppReceipt(XgSim.CrowdApp).installed = true; // orders are signed in 摆渡众包
            sim.S.best.Add(new XgBest { dataset = "spam", acc = .95 });
            var steps = XgGuide.Current(sim, 0, new XgGuideHouse { unpaidPower = true, billDue = 12.5, breakerTripped = true });
            Assert.AreEqual("power.bill", steps[0].id);
            Assert.AreEqual(XgGuide.Home, steps[0].app); Assert.AreEqual("power", steps[0].tab);
            StringAssert.Contains("12.5", steps[0].zh);
            Assert.AreEqual("power.breaker", steps[1].id, "then the breaker");
            Assert.IsNotNull(Find(steps, "contract.antifraud"), "the rest of the list stays below");

            var vram = XgGuide.Current(sim, 0, new XgGuideHouse { vramMB = 1 });
            Assert.AreEqual("vram", vram[0].id, "a model too big for the card blocks training");
            StringAssert.Contains("加显卡", vram[0].zh, "no knobs to turn down: the card is what grows");

            // The program arrives as a download: until it is opened, the note says so and nothing else.
            var noExe = XgGuide.Current(new XgSim(), 0, new XgGuideHouse { appInstalled = false });
            Assert.AreEqual(1, noExe.Count);
            Assert.AreEqual("install", noExe[0].id); Assert.AreEqual(XgGuideKind.Setup, noExe[0].kind);
            StringAssert.Contains(LingGuangV05.Core.AppNames.ExeZh, noExe[0].zh); StringAssert.Contains(LingGuangV05.Core.AppNames.ExeEn, noExe[0].en);
            Assert.IsNull(Find(XgGuide.Current(new XgSim(), 0), "install"), "once it is installed the step is gone");
        }

        [Test]
        public void TheNextAbilityNamesItsShorterBar()
        {
            var sim = Trained();
            var steps = XgGuide.Current(sim, 1000);
            var main = steps.Find(x => x.kind == XgGuideKind.Ability);
            Assert.IsNotNull(main, "the main line is the next ability");
            Assert.AreEqual("ability.study", main.id, "nothing trained yet: the study route starts with the data pack, bought one node at a time");
            Assert.AreEqual("node:spam.pack", main.target);
            StringAssert.Contains(sim.NodeCost(XgCatalog.Node("spam.pack")).ToString("0"), main.zh, "the price of the one node is on the line");
            StringAssert.Contains(XgSim.AbilityName(2, false), main.whyZh, "and the ability it is for");
            Assert.IsNotEmpty(main.en); Assert.IsNotEmpty(main.whyEn);

            sim.S.unlocked.Add("spam.pack"); sim.S.owned.Add("spam");
            var params1 = XgGuide.Current(sim, 1000).Find(x => x.kind == XgGuideKind.Ability);
            Assert.AreEqual("node:mlp", params1.target, "with the data there, the parameter bar is what is left");

            sim = Trained();
            sim.S.trainedParamsK = sim.ParamsThreshold(2);
            var data = XgGuide.Current(sim, 0).Find(x => x.kind == XgGuideKind.Ability);
            Assert.AreEqual("ability.pack", data.id, "with the parameters there, the data is what is left");
            Assert.AreEqual("node:spam.pack", data.target);

            // 第一阶段全程引导: stage one keeps to the main line; side tasks come from stage two, only ever on the third line.
            Assert.AreEqual(-1, steps.FindIndex(x => x.kind == XgGuideKind.Side), "no side task in stage one");
            var two = Trained(2);
            var steps2 = XgGuide.Current(two, 1000);
            int side = steps2.FindIndex(x => x.kind == XgGuideKind.Side);
            Assert.AreEqual(2, side, "side tasks only ever take the third line");
            Assert.AreEqual("side.forum", steps2[side].id);
            Assert.AreEqual(-1, XgGuide.Current(two, 0, new XgGuideHouse { forumSeen = true, yySeen = true, gamesSeen = true }).FindIndex(x => x.kind == XgGuideKind.Side));
        }

        [Test]
        public void StageSixWalksThroughPretrainingAlignmentTheTestTheLetterAndTheRules()
        {
            var host = new Host { money = 1e7 };
            var state = new XgState { progressionVersion = XgSim.ProgressionSchema, stage = 6, stageVision = 6, stageSequence = 6 };
            var sim = new XgSim(state) { GoldChance = 0 };
            sim.S.treeIntroDelivered = true;
            foreach (var n in XgCatalog.Nodes) if (n.stage <= 6 && n.tree != "label" && n.kind != XgNodeKind.Secret && n.id != "datacenter" && !sim.Has(n.id)) sim.S.unlocked.Add(n.id);
            var s = sim.S;

            Assert.AreEqual("final.pretrain", First(XgGuide.Current(sim, 0)).id);
            Assert.IsFalse(sim.TogglePretrain(host), "no server room: it trips");
            var dc = First(XgGuide.Current(sim, 10));
            Assert.AreEqual("final.datacenter", dc.id); Assert.AreEqual("node:datacenter", dc.target);
            Assert.IsTrue(dc.money); Assert.AreEqual(10, dc.current); Assert.AreEqual(sim.NodeCost(XgCatalog.Node("datacenter")), dc.goal);

            s.unlocked.Add("datacenter");
            // The effective scale (XgSim.ScaleItems): every item owned and both bars at one scale unit.
            s.trainedParamsK = XgSim.ScaleUnitParamsK;
            s.labels.Add(new XgLabelCount { dataset = "poems", count = XgSim.ScaleUnitSamples });
            Assert.IsTrue(sim.TogglePretrain(host));
            s.pretrain = .3;
            var wait = First(XgGuide.Current(sim, 0));
            Assert.AreEqual("final.wait", wait.id); Assert.AreEqual(30, wait.current); Assert.AreEqual(100, wait.goal);

            // Without mixed precision and BatchNorm the scale falls short and the plateau names what is missing.
            s.unlocked.Remove("fp16"); s.unlocked.Remove("batchnorm");
            Assert.IsFalse(sim.PretrainScaleReady);
            s.pretrain = sim.PretrainCap;
            var plateau = First(XgGuide.Current(sim, 0));
            Assert.AreEqual("final.scale", plateau.id, "the plateau asks for scale");
            StringAssert.Contains("混合精度", plateau.zh);
            Assert.IsTrue(XgGuide.Current(sim, 0).Exists(x => x.id == "need.fp16"), "and the note offers the missing multiplier");
            s.unlocked.Add("fp16"); s.unlocked.Add("batchnorm");

            s.pretrainRunning = false; s.pretrain = 1; s.abilities = true;
            var align = First(XgGuide.Current(sim, 0));
            Assert.AreEqual("final.align", align.id); Assert.AreEqual(XgSim.AlignCards, align.goal);

            s.alignDone = XgSim.AlignCards; s.fullOpen = true;
            var preparation = First(XgGuide.Current(sim, 0));
            Assert.AreEqual("final.preparation.redteam", preparation.id);
            Assert.AreEqual(XgGuide.Lab, preparation.app); Assert.AreEqual("final", preparation.tab);
            Assert.AreEqual("", preparation.target, "new flow must not target a retired legacy control");

            // Explicit legacy completed marker, without a committed new release: retain the old
            // guide branch and its legacy letter/rules helpers. New active saves stay on preparation.
            s.chapterComplete = true;
            var exam = First(XgGuide.Current(sim, 0));
            Assert.AreEqual("final.exam", exam.id); Assert.AreEqual(XgSim.ExamQuestions, exam.goal);

            s.examDone = XgSim.ExamQuestions;
            var letter = First(XgGuide.Current(sim, 0));
            Assert.AreEqual("final.letter", letter.id); Assert.AreEqual(XgGuide.Bodu, letter.app);
            StringAssert.DoesNotContain(Prologue2016.LongNumber, letter.zh, "the note does not type the number for you");

            Assert.IsTrue(sim.ReadLetter());
            Assert.AreEqual("final.rules", First(XgGuide.Current(sim, 0)).id);

            Assert.IsTrue(sim.WriteRules(new List<string> { "shutdown" }));
            // The curtain call: one question to ask it, then nothing left.
            var ask = XgGuide.Current(sim, 0);
            Assert.AreEqual("origin.ask", ask[0].id);
            Assert.AreEqual(XgGuide.YY, ask[0].app, "the chat with it lives in YY");
            Assert.AreEqual(XgGuide.YYChat, ask[0].tab);
            Assert.AreEqual(1, ask.Count);
            sim.Scripted(sim.OriginQuestion);
            var done = XgGuide.Current(sim, 0);
            Assert.AreEqual("done", done[0].id);
            Assert.AreEqual(1, done.Count, "after the ending the note has nothing left to ask");
        }

        [Test]
        public void ALowSpotCheckPassRateSuggestsTheThresholdOrABetterModel()
        {
            var sim = Trained(CrowdStage); sim.S.autoLevel = 1;
            for (int i = 0; i < 19; i++) sim.S.qcChecks.Add(i >= 3);
            Assert.IsNull(Find(XgGuide.Current(sim, 0), "qc.rate"), "nineteen checks are not judged yet");
            sim.S.qcChecks.Add(true);
            var s = Find(XgGuide.Current(sim, 0), "qc.rate");
            Assert.IsNotNull(s, "3 failures in 20 is on the 15% line");
            Assert.AreEqual(XgGuideKind.Main, s.kind);
            Assert.AreEqual(XgGuide.Lab, s.app); Assert.AreEqual("tree", s.tab); Assert.AreEqual("node:label.coop", s.target, "without the node, point at the node");
            StringAssert.Contains("拿不准才问我", s.zh); StringAssert.Contains("85%", s.whyZh); StringAssert.Contains("罚 1 倍", s.whyZh); StringAssert.Contains("1× its pay", s.whyEn); StringAssert.DoesNotContain("举报", s.whyZh);
            sim.S.unlocked.Add("label.coop");
            s = Find(XgGuide.Current(sim, 0), "qc.rate");
            Assert.AreEqual(XgGuide.Crowd, s.app); Assert.AreEqual("label", s.tab); Assert.AreEqual("name:ThresholdSlider", s.target);
            for (int i = 0; i < 2; i++) sim.S.qcChecks.Add(true);
            Assert.IsNull(Find(XgGuide.Current(sim, 0), "qc.rate"), "3 in 22 is under the line");
        }

        [Test]
        public void APendingCaptchaIsABlockerThatPointsAtTheCaptchaCard()
        {
            var sim = Trained(CrowdStage); sim.S.autoLevel = 1;
            Assert.IsNull(Find(XgGuide.Current(sim, 0), "qc.captcha"));
            sim.S.qcCaptcha = true; sim.S.qcCaptchaCode = "4821"; sim.S.qcCaptchaLeft = 25;
            var s = First(XgGuide.Current(sim, 0));
            Assert.AreEqual("qc.captcha", s.id); Assert.AreEqual(XgGuideKind.Blocker, s.kind);
            Assert.AreEqual(XgGuide.Lab, s.app); Assert.AreEqual("teach", s.tab); Assert.AreEqual("name:QcCaptcha", s.target);
            StringAssert.Contains("0:25", s.zh); StringAssert.Contains("0:25", s.en);
            StringAssert.DoesNotContain("4821", s.zh, "the note never types the captcha for you");
        }

        [Test]
        public void EveryStepHasBothLanguagesAndAReason()
        {
            var sims = new List<XgSim> { new XgSim(), Trained() };
            var t = Trained(); t.S.best.Add(new XgBest { dataset = "spam", acc = .95 }); sims.Add(t);
            var frozen = Trained(CrowdStage); frozen.S.autoLevel = 1; frozen.S.qcFrozen = 60; sims.Add(frozen);
            var warned = Trained(CrowdStage); warned.S.autoLevel = 1; for (int i = 0; i < 20; i++) warned.S.qcChecks.Add(i > 3); sims.Add(warned);
            var captcha = Trained(CrowdStage); captcha.S.autoLevel = 1; captcha.S.qcCaptcha = true; captcha.S.qcCaptchaCode = "1234"; captcha.S.qcCaptchaLeft = 20; sims.Add(captcha);
            foreach (var sim in sims)
                foreach (var money in new[] { 0.0, 50, 1e6 })
                    foreach (var s in XgGuide.Current(sim, money, new XgGuideHouse { unpaidPower = true, breakerTripped = true, overloaded = true, noGpu = true }))
                    {
                        Assert.IsNotEmpty(s.zh, s.id); Assert.IsNotEmpty(s.en, s.id);
                        Assert.IsNotEmpty(s.whyZh, s.id); Assert.IsNotEmpty(s.whyEn, s.id);
                    }
        }

        // ───────────── the road to the second ability ─────────────

        [Test]
        public void TheParameterStepNamesTheFourNodesInOrderAndRingsTheNextOne()
        {
            var sim = Trained();
            var plan = XgGuide.ParamsPlan(sim, 2);
            Assert.IsNotNull(plan);
            CollectionAssert.AreEqual(new[] { "mlp", "s.d2", "s.w1", "s.w2" }, plan.buy.ConvertAll(n => n.id), "多层感知机, 2 层, 宽 32, 宽 64");
            double total = 0; foreach (var n in plan.buy) total += sim.NodeCost(n);
            Assert.AreEqual(total, plan.cost, 1e-9);
            Assert.GreaterOrEqual(plan.paramsK, plan.threshold);

            // The data pack is the route's first purchase; once it is in, the note walks the parameter nodes one at a time (the study step) and rings the next one.
            sim.S.unlocked.Add("spam.pack"); sim.S.owned.Add("spam");
            var step = Find(XgGuide.Current(sim, 20), "ability.study");
            Assert.AreEqual(XgGuide.Lab, step.app); Assert.AreEqual("tree", step.tab); Assert.AreEqual("node:mlp", step.target);
            StringAssert.StartsWith("下一步：科技买「多层感知机」", step.zh);
            StringAssert.Contains("MLP", step.en);
            StringAssert.Contains("¥" + sim.NodeCost(XgCatalog.Node("mlp")).ToString("0"), step.zh);
            StringAssert.Contains(XgSim.AbilityName(2, false), step.whyZh);

            foreach (var (bought, next) in new[] { ("mlp", "node:s.d2"), ("s.d2", "node:s.w1"), ("s.w1", "node:s.w2") })
            {
                sim.S.unlocked.Add(bought);
                var s = Find(XgGuide.Current(sim, 1000), "ability.study");
                Assert.AreEqual(next, s.target, "after " + bought);
                StringAssert.DoesNotContain(XgCatalog.Node(bought).name + "」", s.zh, "a bought node leaves the line");
            }
            sim.S.unlocked.Add("s.w2");
            var steps = XgGuide.Current(sim, 1000);
            Assert.IsNull(Find(steps, "ability.study"), "everything is bought");
            Assert.IsNull(Find(steps, "ability.buy"), "everything is bought");
            Assert.IsNotNull(steps.Find(x => x.id == "ability.train" || x.id == "ability.assess" || x.id == "ability.card"), "then the model is trained to grade C");
        }

        [Test]
        public void TheDataStepNamesTheSmsPackThenTheLabelsThatCountInFull()
        {
            var sim = Trained();
            sim.S.trainedParamsK = sim.ParamsThreshold(2);
            var steps = XgGuide.Current(sim, 1000);
            var pack = Find(steps, "ability.pack");
            Assert.IsNotNull(pack);
            Assert.AreEqual("node:spam.pack", pack.target); Assert.AreEqual("tree", pack.tab);
            StringAssert.Contains("「垃圾短信」数据包", pack.zh);
            StringAssert.DoesNotContain("或在科技买数据包", pack.whyZh);

            // Ownership alone is not installed data in the current data-file schema.
            sim.S.unlocked.Add("spam.pack"); sim.S.owned.Add("spam");
            Assert.AreEqual(0, sim.InstalledBatches("spam"));
            Assert.Less(sim.TrainedSamples, sim.SamplesThreshold(2), "uninstalled ownership cannot fill the data bar");
            // This fixture represents the full pack after import, before noise is added below.
            sim.S.installedDataBatches.Add(new XgLabelCount { dataset = "spam", count = XgSim.PackBatches("spam") });
            Assert.GreaterOrEqual(sim.TrainedSamples, sim.SamplesThreshold(2), "the installed fixture must actually fill the data bar");
            Assert.IsNull(Find(XgGuide.Current(sim, 0), "ability.label"), "a full bar needs no labels");
            // A dirty pack (noise subtracts from the effective samples) leaves a gap: what is left is labels, on the desks that count in full (算术 counts half).
            sim.S.noise.Add(new XgLabelCount { dataset = "spam", count = 4000 });
            Assert.Less(sim.TrainedSamples, sim.SamplesThreshold(2));
            steps = XgGuide.Current(sim, 0);
            Assert.IsNull(Find(steps, "ability.pack"));
            var label = Find(steps, "ability.label");
            Assert.IsNotNull(label);
            Assert.AreEqual(XgGuide.Crowd, label.app); Assert.AreEqual("label", label.tab);
            Assert.AreEqual(1, XgCatalog.Dataset(label.arg).dataWeight, 1e-9, "never 算术");
            // It says where to label, never how many: the count would give the hidden line away (XgSim.Learned.cs).
            StringAssert.Contains("接着标", label.zh);
            Assert.IsFalse(label.HasProgress);
            StringAssert.DoesNotContain(XgSim.SamplesText(sim.SamplesThreshold(2)), label.whyZh);
            StringAssert.Contains("算术", label.whyZh, "the half weight is explained");
        }

        [Test]
        public void WhileAnAbilityStepWaitsTheCheapLineNeverSuggestsADetour()
        {
            var sim = Trained();
            var plan = XgGuide.ParamsPlan(sim, 2);
            foreach (var money in new[] { 40.0, 200, 1000 })
            {
                var steps = XgGuide.Current(sim, money);
                Assert.IsTrue(steps.Exists(x => x.kind == XgGuideKind.Ability));
                foreach (var s in steps)
                {
                    if (!s.id.StartsWith("node.", StringComparison.Ordinal)) continue;
                    string id = s.id.Substring(5);
                    bool advances = plan.buy.Exists(n => n.id == id) || XgCatalog.Node(id).kind == XgNodeKind.Dataset;
                    Assert.IsTrue(advances, s.id + " does not move a bar");
                }
                foreach (var detour in new[] { "weights", "bias", "step" })
                    Assert.IsNull(Find(steps, "node." + detour), detour + " with ¥" + money);
            }
        }

        [Test]
        public void NoTechStepShowsBeforeTheTechPageOpens()
        {
            var sim = new XgSim { GoldChance = 0 };
            sim.S.labels.Add(new XgLabelCount { dataset = "spam", count = 12 });
            sim.S.epochs = 1;
            Assert.IsFalse(sim.FeatureVisible("tree"));
            foreach (var money in new[] { 0.0, 100, 1e5 })
            {
                var steps = XgGuide.Current(sim, money);
                Assert.IsNotEmpty(steps);
                foreach (var s in steps) Assert.IsTrue(XgGuide.TabOpen(sim, s.app, s.tab), s.id + " on " + s.tab);
                Assert.IsFalse(steps.Exists(x => x.tab == "tree"), "the 科技 page is still closed");
            }
        }

        [Test]
        public void TheFallbackLabelsBeforeTrainingAndTrainsAfter()
        {
            var fresh = new XgSim { GoldChance = 0 };
            var keep = XgGuide.Keep(fresh);
            Assert.AreEqual(XgGuide.Crowd, keep.app); Assert.AreEqual("label", keep.tab);
            var trained = XgGuide.Keep(Trained());
            Assert.AreEqual(XgGuide.Lab, trained.app); Assert.AreEqual("train", trained.tab);
            Assert.IsNotEmpty(keep.whyEn); Assert.IsNotEmpty(trained.whyEn);
        }

        [Test]
        public void ThePrologueAndSetupStepsPointAtTheExeAndTheFirstAnswer()
        {
            var exe = XgGuide.OpenExe();
            Assert.AreEqual(XgGuide.Desktop, exe.app);
            StringAssert.Contains(LingGuangV05.Core.AppNames.ExeZh, exe.zh); StringAssert.Contains(LingGuangV05.Core.AppNames.ExeEn, exe.en);
            var first = XgGuide.AnswerFirst();
            StringAssert.Contains("是", first.zh); Assert.IsNotEmpty(first.en);
        }

        [Test]
        public void AutoLabellingIsOnTheNoteOnceItsIdeaHasCome()
        {
            var sim = Trained(2); // stage one keeps to the main line (第一阶段全程引导)
            sim.S.best.Add(new XgBest { dataset = "spam", acc = .9 });
            Assert.IsTrue(sim.AutoLabelHidden);
            Assert.IsNull(Find(XgGuide.Current(sim, 1e4), "label.auto"), "not before the idea");
            sim.S.autoLabelRevealed = true;
            var s = Find(XgGuide.Current(sim, 0), "label.auto");
            Assert.IsNotNull(s);
            Assert.AreEqual(XgGuide.Lab, s.app); Assert.AreEqual("tree", s.tab); Assert.AreEqual("node:label.auto", s.target);
            Assert.IsTrue(s.money, "its price is shown while short");
            sim.S.autoLevel = 1;
            Assert.IsNull(Find(XgGuide.Current(sim, 1e4), "label.auto"), "bought");
        }

        [Test]
        public void ReadyDataUsesPaidImportPrerequisitesThenOnlyTheExplicitDragStepInstalls()
        {
            var sim = Trained(); var host = new Host { money = 10000 };
            Assert.IsTrue(sim.BuyNode("spam.pack", host));
            for (int i = 0; i < 20 && sim.S.dataFiles.Count == 0; i++) sim.Tick(.5, host);
            Assert.AreEqual(0, sim.InstalledBatches("spam"));
            foreach (var id in new[] { "train.curve", XgSim.DataEntryNode })
            {
                var step = First(XgGuide.Current(sim, host.money));
                Assert.AreEqual("data.import.unlock", step.id);
                Assert.AreEqual("node:" + id, step.target);
                Assert.AreEqual(step.id, XgGuide.BarStep(sim, host.money, host.VramMB, out _)?.id);
                double price = sim.NodeCost(XgCatalog.Node(id)), before = host.money;
                Follow(sim, host, step);
                Assert.AreEqual(before - price, host.money, 1e-8, "existing purchase deducts its ordinary price");
                Assert.AreEqual(0, sim.InstalledBatches("spam"), "a purchase cannot import a file");
            }
            var install = First(XgGuide.Current(sim, host.money));
            Assert.AreEqual("data.install", install.id);
            Assert.IsTrue(sim.DataFileReady(install.arg));
            Assert.IsTrue(XgGuide.TabOpen(sim, install.app, install.tab));
            StringAssert.Contains("拖", install.zh); StringAssert.Contains("drag", install.en);
            Assert.AreEqual(0, sim.InstalledBatches("spam"), "reading the note does not install");
            double wallet = host.money;
            Follow(sim, host, install);
            Assert.Greater(sim.InstalledBatches("spam"), 0);
            Assert.AreEqual(wallet, host.money, "no second installation fee");
            Assert.IsFalse(sim.InstallDataFile(install.arg), "the same file cannot install twice");
            Assert.AreNotEqual("data.install", First(XgGuide.Current(sim, host.money)).id);
        }

        /// <summary>
        /// Mirrors the Files UI guide fixture (FilesWindowUiChecks): an early assessed lab that buys 训练曲线, 数据导入 and the
        /// spam pack in that order and downloads it. Without labelled samples the 练 page is closed, so the import step
        /// (which points at 练) cannot appear; a few hand labels through the product answer API open it. Nothing installs.
        /// </summary>
        [Test]
        public void TheUiImportFixtureNeedsTheTrainPageThatItsImportStepPointsAt()
        {
            XgSim Setup(bool labelled, Host host)
            {
                var sim = new XgSim { GoldChance = 0 };
                sim.S.epochs = 1; sim.S.assessments = 1; sim.S.treeIntroDelivered = true;
                Assert.IsFalse(XgGuide.TabOpen(sim, XgGuide.Lab, "train"), "setup: no labels, 练 closed");
                if (labelled)
                {
                    sim.SelectDesk("spam");
                    for (int i = 0; i < 40 && sim.Samples("spam") < XgCatalog.SamplesToTrain; i++) { var c = sim.Card("spam"); sim.Answer("spam", c.truth, host); }
                    Assert.GreaterOrEqual(sim.Labels("spam"), XgCatalog.SamplesToTrain, "setup: hand labels only up to the training line");
                }
                foreach (var id in new[] { "train.curve", XgSim.DataEntryNode, "spam.pack" })
                {
                    double before = host.money, price = sim.NodeCost(XgCatalog.Node(id));
                    Assert.IsTrue(sim.BuyNode(id, host), id);
                    Assert.AreEqual(before - price, host.money, 1e-8, id + " charges its ordinary price once");
                }
                for (int i = 0; i < 20 && sim.S.dataFiles.Count == 0; i++) sim.Tick(.5, host);
                Assert.IsTrue(sim.S.dataFiles.Exists(f => sim.DataFileReady(f.fileId)), "a downloaded, ready pack");
                Assert.AreEqual(0, sim.InstalledBatches("spam"), "downloading installs nothing");
                return sim;
            }

            var bare = Setup(false, new Host { money = 10000 });
            Assert.IsTrue(bare.CanInstallData && XgGuide.TabOpen(bare, XgGuide.Lab, "tree"));
            Assert.IsFalse(XgGuide.TabOpen(bare, XgGuide.Lab, "train"), "no labelled samples: 练 stays closed after the purchases");
            var bareSteps = XgGuide.Current(bare, 10000);
            Assert.IsNull(Find(bareSteps, "data.install"), "the import step never points at a closed page");
            Assert.AreNotEqual("data.install", XgGuide.BarStep(bare, 10000, 1e5, out _)?.id, "nor does the top bar's 去做 link");

            var host2 = new Host { money = 10000 };
            var labelled = Setup(true, host2);
            Assert.IsTrue(XgGuide.TabOpen(labelled, XgGuide.Lab, "train"));
            var first = First(XgGuide.Current(labelled, host2.money));
            Assert.AreEqual("data.install", first.id, "with 练 open the downloaded pack's import leads");
            Assert.AreEqual(XgGuide.Lab, first.app); Assert.AreEqual("train", first.tab);
            Assert.IsTrue(labelled.DataFileReady(first.arg));
            StringAssert.Contains("训练数据", first.zh);
            Assert.AreEqual(0, labelled.InstalledBatches("spam"), "hand labels and reading the note install nothing");
        }

        /// <summary>
        /// Plays a new game doing only what the note's first line says (and labelling while it waits for money), and
        /// checks every line on the way: never an empty note, never a blank line, never a closed page. It must reach
        /// the second ability, and the lines it read must name each step of the road.
        /// </summary>
        [Test]
        public void FollowingOnlyTheNoteReachesTheSecondAbility()
        {
            var sim = new XgSim { GoldChance = 0 }; var host = new Host();
            var read = new List<string>();
            for (int i = 0; i < 6000 && sim.AbilitiesCount < 2; i++)
            {
                sim.Tick(.5, host);
                var steps = XgGuide.Current(sim, host.money);
                Assert.IsNotEmpty(steps, "the note is never empty (action " + i + ")");
                foreach (var s in steps)
                {
                    Assert.IsTrue(XgGuide.TabOpen(sim, s.app, s.tab), s.id + " points at a closed page (action " + i + ")");
                    Assert.IsNotEmpty(s.zh, s.id); Assert.IsNotEmpty(s.en, s.id); Assert.IsNotEmpty(s.whyZh, s.id); Assert.IsNotEmpty(s.whyEn, s.id);
                    // While the chapter button waits, the lines below it are leftovers (cheap boosts), not the road.
                    if (!read.Contains(s.zh) && (!sim.StageReady || s.id == XgGuide.ConfirmStageId)) read.Add(s.zh);
                }
                // A finished stage is confirmed by the player on the training page, and only because the note says so.
                if (sim.StageReady)
                {
                    Assert.IsTrue(steps.Exists(x => x.id == XgGuide.ConfirmStageId), "the note offers the chapter button (action " + i + ")");
                    Assert.IsTrue(sim.ConfirmNextStage());
                    continue;
                }
                Follow(sim, host, steps[0]);
            }
            Assert.AreEqual(2, sim.AbilitiesCount, "the note alone leads to 多选一");
            string all = string.Join("\n", read);
            StringAssert.Contains("再教它 " + XgCatalog.SamplesToTrain + " 张", all);
            StringAssert.Contains("训练一轮", all);
            // The study route: one purchase per line, in order (the data pack first, then the four parameter nodes).
            int at = -1;
            foreach (var bought in new[] { "科技买「垃圾短信」", "科技买「多层感知机」", "科技买「2 层」", "科技买「宽 32」", "科技买「宽 64」" })
            {
                int found = all.IndexOf(bought, at + 1, StringComparison.Ordinal);
                Assert.Greater(found, at, bought + " comes after the step before it");
                at = found;
            }
            StringAssert.Contains("数据导入", all);
            StringAssert.Contains("拖到「练」", all);
            Assert.Greater(sim.InstalledBatches("spam"), 0, "the note-led route imports its downloaded pack");
            StringAssert.Contains("练到 C 级", all);
            StringAssert.Contains("科技买「垃圾短信」", all);
            StringAssert.DoesNotContain("科技买「垃圾短信」数据包", all, "the pack is named once (the study line), not twice");
            foreach (var detour in new[] { "weights", "bias", "step" })
                StringAssert.DoesNotContain("「" + XgCatalog.Node(detour).name + "」", all, "no cheap detour on the way");
        }

        [Test]
        public void TheExamLeadsOnlyWhenAPassIsLikely()
        {
            var sim = BarsFull();
            Assert.AreEqual(XgGuide.ConfirmStageId, First(XgGuide.Current(sim, 0)).id, "full bars: the exam leads");
            sim.S.trainedParamsK = sim.ParamsThreshold(2) * .85;
            Assert.IsTrue(sim.ExamReady, "setup: the exam can be sat at 85%");
            var steps = XgGuide.Current(sim, 0);
            Assert.IsNotNull(Find(steps, XgGuide.ConfirmStageId), "it is still offered");
            Assert.AreNotEqual(XgGuide.ConfirmStageId, First(steps).id, "but the bars come first");
        }

        [Test]
        public void AFailedExamSaysHowManyRoundsToTheRetry()
        {
            var sim = BarsFull();
            sim.S.examRetryEpoch = sim.S.stageEpochs + 3;
            Assert.IsFalse(sim.ExamReady);
            var retry = Find(XgGuide.Current(sim, 0), "exam.retry");
            Assert.IsNotNull(retry);
            StringAssert.Contains("3 轮", retry.zh);
            Assert.AreEqual("train", retry.tab);
        }

        [Test]
        public void StageFiveWithAttentionNamesTheTransformerIdea()
        {
            var sim = Trained(5);
            sim.S.unlocked.Add("attention");
            var step = Find(XgGuide.Current(sim, 1e6), "ability.transformer");
            Assert.IsNotNull(step);
            StringAssert.Contains(XgSim.AttentionIdeaRounds + " 轮", step.zh);
            sim.S.attentionEpochs = XgSim.AttentionIdeaRounds;
            sim.S.emerged.Add(5);
            Assert.IsNull(Find(XgGuide.Current(sim, 1e6), "ability.transformer"), "gone once it thought of it");
        }

        [Test]
        public void RaisesStopBeingNamedAfterStageThree()
        {
            var sim = Trained(4);
            Assert.IsNull(Find(XgGuide.Current(sim, 1e9), "raise"));
        }

        /// <summary>A stage-one lab with both bars full for the second ability: only the player's press is missing.</summary>
        static XgSim BarsFull()
        {
            var sim = Trained();
            sim.S.stageEpochs = 1;
            sim.S.trainedParamsK = sim.ParamsThreshold(2);
            foreach (var l in sim.S.labels) if (l.dataset == "logic") { l.count = 0; }
            sim.S.labels.Add(new XgLabelCount { dataset = "logic", count = sim.SamplesThreshold(2) + 50 });
            Assert.IsTrue(sim.StageReady, "setup: both bars are full");
            return sim;
        }

        [Test]
        public void WhenBothBarsAreFullTheNoteLeadsWithTheChapterButtonAndItGoesOnceConfirmed()
        {
            var sim = BarsFull();
            var steps = XgGuide.Current(sim, 1000);
            var step = Find(steps, XgGuide.ConfirmStageId);
            Assert.IsNotNull(step, "the step appears while a confirmation is pending");
            Assert.AreEqual(XgGuide.ConfirmStageId, First(steps).id, "and it is the first line");
            Assert.AreEqual(XgGuide.Lab, step.app); Assert.AreEqual("train", step.tab);
            Assert.AreEqual("name:ConfirmNextStage", step.target, "the training page's chapter button, by its object name");
            Assert.IsTrue(XgGuide.TabOpen(sim, step.app, step.tab));
            // The chapter turns through the 能力考 (XgSim.AbilityExam.cs).
            StringAssert.Contains("去训练页让它考「" + XgSim.AbilityName(2, false) + "」", step.zh);
            StringAssert.Contains("let it sit the exam for " + XgSim.AbilityName(2, true), step.en);
            StringAssert.DoesNotContain("涌现", step.zh + step.whyZh);
            Assert.IsNotEmpty(step.whyZh); Assert.IsNotEmpty(step.whyEn);

            // The top bar's 去做 › link leads to the same step instead of another purchase.
            var bar = XgGuide.BarStep(sim, 1000, 100000, out _);
            Assert.IsNotNull(bar);
            Assert.AreEqual(XgGuide.ConfirmStageId, bar.id);

            Assert.IsTrue(sim.ConfirmNextStage());
            Assert.IsNull(Find(XgGuide.Current(sim, 1000), XgGuide.ConfirmStageId), "gone once confirmed");
            var after = XgGuide.BarStep(sim, 1000, 100000, out _);
            Assert.IsTrue(after == null || after.id != XgGuide.ConfirmStageId);
        }

        [Test]
        public void TheChapterButtonStepWaitsForBothBarsAndATrainedRound()
        {
            var sim = BarsFull();
            sim.S.stageEpochs = 0;
            Assert.IsNull(Find(XgGuide.Current(sim, 1000), XgGuide.ConfirmStageId), "nothing was trained in this stage: the button would refuse");
            sim.S.stageEpochs = 1;
            sim.S.trainedParamsK = 0;
            Assert.IsNull(Find(XgGuide.Current(sim, 1000), XgGuide.ConfirmStageId), "one full bar is not enough");
            Assert.AreNotEqual(XgGuide.ConfirmStageId, XgGuide.BarStep(sim, 1000, 100000, out _)?.id);
        }

        /// <summary>Does what a step says, the way a player clicking it would; labels when a purchase is not affordable yet.</summary>
        static void Follow(XgSim sim, Host host, XgGuideStep s)
        {
            XgTrack Track() { var t = s.arg == "vision" ? XgTrack.Vision : XgTrack.Sequence; return sim.TrainingUnlocked(t) ? t : 1 - t; }
            if (s.target.StartsWith("node:", StringComparison.Ordinal)) { if (!sim.BuyNode(s.target.Substring(5), host)) Label(sim, host, ""); return; }
            // The next batch of a pack (节奏重做 §2.3): bought like a node, labelling while the money comes in.
            if (s.target.StartsWith("batch:", StringComparison.Ordinal)) { if (!sim.BuyBatch(s.target.Substring(6), host)) Label(sim, host, ""); return; }
            if (s.target.StartsWith("contract:", StringComparison.Ordinal)) { sim.Sign(s.target.Substring(9), host); return; }
            if (s.target.StartsWith("label:训练一轮", StringComparison.Ordinal)) { sim.TrainEpoch(Track(), host); return; }
            if (s.target.StartsWith("label:评估", StringComparison.Ordinal)) { sim.Assess(Track(), host); return; }
            // This simulates only the explicit drag/import instruction; buying or downloading never installs.
            if (s.id == "data.install") { Assert.IsTrue(sim.InstallDataFile(s.arg), "the named ready file imports through the product API"); return; }
            if (s.id == "raise") { sim.BuyNode("label.raise", host); return; }
            if (s.id == "label.auto") { if (!sim.BuyNode("label.auto", host)) Label(sim, host, ""); return; }
            Label(sim, host, s.arg);
        }

        static void Label(XgSim sim, Host host, string desk)
        {
            if (string.IsNullOrEmpty(desk) || !sim.DeskOpen(desk)) desk = sim.S.desk;
            if (string.IsNullOrEmpty(desk) || !sim.DeskOpen(desk)) desk = sim.OpenDesks()[0].id;
            var card = sim.Card(desk);
            sim.Answer(desk, card.truth, host);
        }
    }
}
