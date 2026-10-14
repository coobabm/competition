using System.Collections.Generic;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>Every parameter-count hint carries its numbers: the value, the line, the gap, what each purchase adds.</summary>
    public sealed class XgParamsHintTests
    {
        sealed class Host : IXgHost
        {
            public double money = 1e6, compute = 4, vram = 1e5;
            public double Compute => compute;
            public double VramMB => vram;
            public double Money => money;
            public bool Spend(double a) { if (money < a) return false; money -= a; return true; }
            public void Earn(double a) { money += a; }
            public void Train(double s) { }
            public string Blocker => null;
        }

        /// <summary>
        /// A stage-2 lab whose second ability is in, working on the third: the story's study route (the packs, the
        /// sequence nodes) is bought, and the data wanted is there, so the parameter purchases are the main line.
        /// </summary>
        static XgSim ThirdAbility(params string[] more)
        {
            var state = new XgState { progressionVersion = XgSim.ProgressionSchema, stage = 2, stageVision = 2, stageSequence = 2 };
            var sim = new XgSim(state) { GoldChance = 0 };
            sim.S.abilitiesEmerged = new List<int> { 2 };
            sim.S.desksOpen.Add("mnist");
            foreach (var id in new[] { "spam.pack", "mlp", "s.d2", "s.w1", "s.w2", "mnist.pack", "rnn", "s.d3", "s.w3" }) sim.S.unlocked.Add(id);
            foreach (var id in more) sim.S.unlocked.Add(id);
            sim.S.labels.Add(new XgLabelCount { dataset = "mnist", count = 300 });
            sim.S.epochs = 5; sim.S.assessments = 1; sim.S.treeIntroDelivered = true;
            sim.S.trainedParamsK = 5;
            sim.SelectedTrack = XgTrack.Vision;
            sim.SetDataset(XgTrack.Vision, "mnist");
            return sim;
        }

        static string Whole(double v) => System.Math.Floor(v + 1e-9).ToString("0", System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>A stage-2 lab on the digit desk with the given samples and extra nodes owned.</summary>
        static XgSim Digits(double labels, params string[] owned)
        {
            var state = new XgState { progressionVersion = XgSim.ProgressionSchema, stage = 2, stageVision = 2, stageSequence = 2 };
            var sim = new XgSim(state) { GoldChance = 0 };
            sim.S.desksOpen.Add("mnist");
            foreach (var id in owned) sim.S.unlocked.Add(id);
            sim.S.labels.Add(new XgLabelCount { dataset = "mnist", count = labels });
            sim.SelectedTrack = XgTrack.Vision;
            sim.SetDataset(XgTrack.Vision, "mnist");
            return sim;
        }

        static XgGuideStep Find(List<XgGuideStep> steps, string id) => steps.Find(s => s.id == id);

        [Test]
        public void TheBuyStepShowsTheModelAfterEachPurchaseTheBillAndTheGradeCLine()
        {
            var sim = ThirdAbility();
            var plan = XgGuide.ParamsPlan(sim, 3);
            Assert.IsNotNull(plan); Assert.GreaterOrEqual(plan.buy.Count, 2);
            var step = Find(XgGuide.Current(sim, 20), "ability.buy");
            TestContext.WriteLine(step.zh + "\n" + step.whyZh + "\n---\n" + step.en + "\n" + step.whyEn);
            // P against the line, with the share and the gap.
            StringAssert.Contains("现在 " + XgSim.ParamsText(sim.TrainedParamsK) + " / " + XgSim.ParamsText(plan.threshold) + "（", step.whyZh);
            StringAssert.Contains("%），差 ", step.whyZh);
            // One numbered line per node, each with its price and the model before and after; the last lands on the plan's model.
            for (int i = 1; i <= plan.buy.Count; i++) StringAssert.Contains(i + ". ", step.whyZh);
            StringAssert.Contains("¥" + Whole(sim.NodeCost(plan.buy[0])) + "：模型 ", step.whyZh);
            StringAssert.Contains(" → " + XgSim.ParamsText(plan.paramsK) + "\n", step.whyZh);
            // The bill against the wallet.
            StringAssert.Contains("共 ¥" + Whole(plan.cost) + "，手头 ¥20，差 ¥" + Whole(plan.cost - 20), step.whyZh);
            StringAssert.Contains("Total ¥" + Whole(plan.cost) + ", you have ¥20, short by ¥" + Whole(plan.cost - 20), step.whyEn);
            // The line to train to afterwards.
            string line = XgSim.Pct(XgSim.GradeAccuracy(sim.Run(plan.track).dataset, XgSim.TrainedGrade));
            StringAssert.Contains("买齐后模型 " + XgSim.ParamsText(plan.paramsK) + " ≥ 门槛 " + XgSim.ParamsText(plan.threshold), step.whyZh);
            StringAssert.Contains("C 级（" + line + "）", step.whyZh);
            StringAssert.Contains("grade C (" + line + ")", step.whyEn);
            StringAssert.Contains("标注台", step.whyZh, "the money hint stays");
            StringAssert.StartsWith("科技买「视觉 3 层」", step.zh, "the title is untouched");
        }

        [Test]
        public void TheBillSaysEnoughWhenTheWalletCoversIt()
        {
            var sim = ThirdAbility();
            var plan = XgGuide.ParamsPlan(sim, 3);
            var step = Find(XgGuide.Current(sim, 5000), "ability.buy");
            StringAssert.Contains("共 ¥" + Whole(plan.cost) + "，手头 ¥5000，够了", step.whyZh);
            StringAssert.Contains("you have ¥5000, enough", step.whyEn);
        }

        [Test]
        public void TheTrainStepShowsTheAccuracyAgainstTheGradeCLine()
        {
            var sim = ThirdAbility("v.d3", "v.d4");
            foreach (var run in sim.Runs) sim.AutoConfigure(run, new Host());
            var step = XgGuide.Current(sim, 1000).Find(x => x.id == "ability.train" || x.id == "ability.assess");
            Assert.IsNotNull(step);
            TestContext.WriteLine(step.zh + "\n" + step.whyZh + "\n" + step.en + "\n" + step.whyEn);
            string line = XgSim.Pct(XgSim.GradeAccuracy(sim.Run(XgTrack.Vision).dataset, XgSim.TrainedGrade));
            StringAssert.Contains("C 线 " + line, step.zh);
            StringAssert.Contains("grade C " + line, step.en);
            StringAssert.Contains("C 级线 " + line, step.whyZh);
            StringAssert.Contains("grade C line is " + line, step.whyEn);
        }

        [Test]
        public void TheCardStepShowsWhatTheNeededModelTakesAgainstWhatTheCardHas()
        {
            var sim = ThirdAbility("v.d3", "v.d4");
            var plan = XgGuide.ParamsPlan(sim, 3);
            Assert.IsNotNull(plan);
            // A card that holds the smallest model but not the 42K one.
            var house = new XgGuideHouse { vramMB = 340 };
            var steps = XgGuide.Current(sim, 1000, house);
            foreach (var x in steps) TestContext.WriteLine(x.id + " | " + x.zh + " | " + x.whyZh);
            var step = steps.Find(x => x.id == "ability.card");
            Assert.IsNotNull(step);
            StringAssert.Contains("显存 ", step.zh); StringAssert.Contains(" / 卡 340 MB", step.zh);
            StringAssert.Contains("VRAM ", step.en); StringAssert.Contains(" / card 340 MB", step.en);
        }

        [Test]
        public void TheGradeCLineIsTheAccuracyThatScoresTheCGrade()
        {
            foreach (var d in XgCatalog.Datasets)
            {
                double acc = XgSim.GradeAccuracy(d.id, XgSim.TrainedGrade);
                Assert.AreEqual(XgSim.TrainedGrade, XgSim.Grade(XgSim.Score(d.id, acc + 1e-6)), d.id);
                Assert.Less(XgSim.Grade(XgSim.Score(d.id, acc - .01)), XgSim.TrainedGrade, d.id);
            }
        }

        [Test]
        public void TheSizeNeverCountsBeforeTheModelHasTrainedAndGradedC()
        {
            var sim = Digits(5000);
            var run = sim.S.vision;
            sim.AutoConfigure(run, new Host());
            run.shapeRounds = 0;
            Assert.AreEqual(XgCountState.Fresh, sim.CountState(run));
            run.shapeRounds = 3; run.valAcc = XgSim.GradeAccuracy("mnist", 0);
            Assert.AreEqual(XgCountState.BelowC, sim.CountState(run));
            StringAssert.Contains("未到 C 级，不算", sim.CountShort(run));
            run.valAcc = XgSim.GradeAccuracy("mnist", XgSim.TrainedGrade) + .01;
            Assert.AreEqual(XgCountState.Unassessed, sim.CountState(run));
            sim.S.trainedParamsK = XgSim.ParamsK(run);
            Assert.AreEqual(XgCountState.Counted, sim.CountState(run));
            StringAssert.Contains("已算数", sim.ParamsSplitText(run));
        }

        [Test]
        public void TheFormulaPrintsTheRealLayersWidthAndFactor()
        {
            var run = new XgRun { track = (int)XgTrack.Vision, arch = "lenet", depth = 3, width = 3, dataset = "mnist" };
            // 3 layers × 128² × 3 = 147,456 → 147K
            Assert.AreEqual("3 层 × 宽 128² × 结构 3 = " + XgSim.ParamsText(XgSim.ParamsK(run)), new XgSim().ModelFormula(run));
            Assert.AreEqual("3 layers × width 128² × structure 3 = " + XgSim.ParamsText(XgSim.ParamsK(run)), new XgSim { English = true }.ModelFormula(run));
        }

        [Test]
        public void ThePlateauTextKeepsItsPrefixAndAddsTheNumbers()
        {
            var sim = Digits(5000);
            var host = new Host();
            for (int i = 0; i < 40; i++) sim.TrainEpoch(XgTrack.Vision, host);
            var run = sim.S.vision;
            string text = sim.PlateauText(run, out var limit);
            TestContext.WriteLine(text);
            Assert.AreEqual(XgLimit.Params, limit);
            StringAssert.StartsWith("参数量不够，再练也涨不动：去科技加宽、加深，或在道具买参数更多的结构。", text);
            StringAssert.Contains("这台 " + XgSim.ParamsText(XgSim.ParamsK(run)), text);
            StringAssert.Contains("门槛 ", text);
            StringAssert.Contains("准确率最高 " + XgSim.Pct(sim.CeilingAcc(run)), text);
            StringAssert.Contains("C 级线 " + XgSim.Pct(XgSim.GradeAccuracy("mnist", XgSim.TrainedGrade)), text);
            sim.Diagnose(run, XgLimit.Params, null); // the guide names the fix once the cause is worked out
            var step = XgChanceHint.Step(sim);
            TestContext.WriteLine(step.zh + "\n" + step.whyZh + "\n" + step.whyEn);
            StringAssert.Contains("C 级线 ", step.whyZh);
            StringAssert.Contains("grade C is ", step.whyEn);
        }

        [Test]
        public void TheNextTechNodeShowsTheSizeItGives()
        {
            var sim = Digits(5000);
            var host = new Host();
            for (int i = 0; i < 40; i++) sim.TrainEpoch(XgTrack.Vision, host);
            var run = sim.S.vision;
            var node = XgChanceHint.NextScaleNode(sim, XgTrack.Vision);
            Assert.IsNotNull(node);
            double after = sim.ParamsAfterNode(run, node, host);
            Assert.GreaterOrEqual(after, XgSim.ParamsK(run), "more cap never gives a smaller model");
            StringAssert.Contains("→ " + XgSim.ParamsText(after), sim.ParamsPlateauDetail(run, false));
            // Buying it really gives that size.
            sim.S.unlocked.Add(node.id);
            sim.AutoConfigure(run, host);
            Assert.AreEqual(after, XgSim.ParamsK(run), 1e-6);
        }

        [Test]
        public void TheReportsCarryPercentFormulaCapsAndMemory()
        {
            var sim = Digits(5000);
            var host = new Host { vram = 4096 };
            for (int i = 0; i < 40; i++) sim.TrainEpoch(XgTrack.Vision, host);
            var run = sim.S.vision;
            string report = sim.ParamsReport(host);
            TestContext.WriteLine(report);
            StringAssert.Contains("已训练参数量 P：", report);
            StringAssert.Contains("%)", report);
            StringAssert.Contains(sim.ModelFormula(run), report);
            StringAssert.Contains(sim.CapsText(run), report);
            StringAssert.Contains(XgSim.MemText(XgSim.VramNeedMB(run)) + " / 4 GB", report);
            string samples = sim.SamplesReport();
            TestContext.WriteLine(samples);
            StringAssert.Contains("样本 D：", samples);
            StringAssert.Contains("%)", samples);
        }

        [Test]
        public void TheSampleSplitNamesWhatTheNoiseTookOff()
        {
            var sim = Digits(5000);
            StringAssert.Contains("已有 ", sim.SamplesSplitText());
            Assert.GreaterOrEqual(sim.RawSamples, sim.TrainedSamples - 1e-9);
        }

        [Test]
        public void MemoryReadsInMegabytesBelowAGigabyte()
        {
            Assert.AreEqual("640 MB", XgSim.MemText(640));
            Assert.AreEqual("4 GB", XgSim.MemText(4096));
            Assert.AreEqual("1.5 GB", XgSim.MemText(1536));
        }
    }
}
