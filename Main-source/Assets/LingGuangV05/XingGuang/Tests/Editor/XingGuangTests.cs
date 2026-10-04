using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XingGuangTests
    {
        sealed class Host : IXgHost
        {
            public double money = 1000, compute = 1, vram = 2048, trained; public string blocker;
            public double Compute => compute;
            public double VramMB => vram;
            public double Money => money;
            public bool Spend(double a) { if (money + 1e-9 < a) return false; money -= a; return true; }
            public void Earn(double a) { money += a; }
            public void Train(double s) { trained += s; }
            public string Blocker => blocker;
        }

        [Test] public void CatalogIsConsistent() { XgCatalog.Validate(); }

        // Explicit pre-six-stage fixture: old saves began with two architectures and three desks.
        static XgSim LegacyStageThree(params string[] extraArchitectures)
        {
            var state = new XgState();
            state.desksOpen.AddRange(new[] { "mnist", "poems", "logic" });
            foreach (var arch in extraArchitectures) state.unlocked.Add(arch);
            return new XgSim(state) { GoldChance = 0 };
        }

        static XgSim WithPack(params string[] packs)
        {
            var sim = LegacyStageThree();
            foreach (var p in packs) { sim.S.owned.Add(p); sim.S.unlocked.Add(p + ".pack"); }
            foreach (var run in sim.Runs) sim.Evaluate(run);
            return sim;
        }

        static void Label(XgSim sim, string desk, int n, IXgHost host)
        {
            for (int i = 0; i < n; i++) Assert.IsTrue(sim.Answer(desk, sim.Card(desk).truth, host).correct);
        }

        // ───────────── training is one action ─────────────

        [Test]
        public void NewLabStartsWithOnlyTheLabellingDesk()
        {
            var sim = new XgSim(); var host = new Host();
            Assert.IsFalse(sim.TrainingUnlocked(XgTrack.Vision));
            Assert.IsNull(sim.TrainEpoch(XgTrack.Vision, host), "no data, no training");
            Assert.IsFalse(sim.ContractsUnlocked);
            CollectionAssert.AreEquivalent(new[] { "logic", "spam" }, sim.S.desksOpen);
            sim.Tick(60, host);
            Assert.AreEqual(1000, host.money, 1e-9, "an idle lab neither earns nor bills");
            Assert.AreEqual(0, host.trained, 1e-9);
        }

        [Test]
        public void EachPressTrainsOneEpochAndBillsPower()
        {
            var sim = WithPack("mnist"); sim.UseBoard = false; /* legacy curve */ var host = new Host();
            double before = sim.S.vision.valAcc;
            var e = sim.TrainEpoch(XgTrack.Vision, host);
            Assert.IsNotNull(e);
            Assert.AreEqual(1, sim.S.vision.epoch);
            Assert.AreEqual(XgSim.EpochSeconds, host.trained, 1e-9);
            Assert.Greater(sim.S.vision.valAcc, before);
            Assert.AreEqual(1, sim.S.combo, "a hand press is a hit");
            sim.Tick(30, host);
            Assert.AreEqual(1, sim.S.vision.epoch, "without automation nothing trains on its own");
        }

        [Test]
        public void AboutSixteenPressesConvergeLeNetOnMnist()
        {
            var sim = WithPack("mnist"); sim.UseBoard = false; /* legacy curve */ var host = new Host();
            double peak = sim.PeakAccuracy(sim.S.vision, sim.S.vision.lr);
            int presses = 0;
            while (sim.S.vision.valAcc < peak - .01 && presses < 100) { sim.TrainEpoch(XgTrack.Vision, host); presses++; }
            Assert.That(presses, Is.InRange(8, 30));
        }

        [Test]
        public void ComboMakesHandPressesStrongerThanAutoOnes()
        {
            var sim = WithPack("mnist"); var host = new Host();
            var hand = sim.TrainEpoch(XgTrack.Vision, host, true);
            var auto = sim.TrainEpoch(XgTrack.Vision, host, false);
            Assert.Greater(hand.steps, auto.steps * 1.9, "auto epochs are half strength, hand ×combo");
            for (int i = 0; i < 30; i++) sim.TrainEpoch(XgTrack.Vision, host, true);
            Assert.AreEqual(2, sim.ComboMultiplier, 1e-9, "capped at ×2");
            Assert.Greater(sim.S.combo, 30);
        }

        [Test]
        public void PowerCutStopsTraining()
        {
            var sim = WithPack("mnist"); var host = new Host { blocker = "停电" };
            Assert.IsNull(sim.TrainEpoch(XgTrack.Vision, host));
            Assert.AreEqual(0, sim.S.vision.steps, 1e-9);
        }

        // ───────────── assessment ─────────────

        [Test]
        public void ScoreRunsFromChanceToTheDatasetFloor()
        {
            Assert.AreEqual(0, XgSim.Score("logic", .5), 1e-9);
            Assert.AreEqual(1000, XgSim.Score("logic", .97), 1e-9);
            Assert.AreEqual(1000, XgSim.Score("logic", .999), 1e-9, "clamped");
            Assert.AreEqual(0, XgSim.Score("mnist", .05), 1e-9, "below chance is 0");
            Assert.AreEqual(4, XgSim.Grade(950));
            Assert.AreEqual(0, XgSim.Grade(299));
            Assert.AreEqual(1, XgSim.Grade(300));
        }

        [Test]
        public void EveryFourthEpochIsAssessedAndOnlyRecordsPay()
        {
            var sim = WithPack("mnist"); sim.UseBoard = false; /* legacy curve */ var host = new Host { money = 0 };
            XgAssessment first = null;
            for (int i = 0; i < 4; i++) { var e = sim.TrainEpoch(XgTrack.Vision, host); if (i < 3) Assert.IsNull(e.assessment); else first = e.assessment; }
            Assert.IsNotNull(first);
            Assert.IsTrue(first.record);
            Assert.Greater(first.reward, 0);
            Assert.AreEqual(sim.S.vision.valAcc, sim.BestAcc("mnist"), 1e-9, "a record saves the checkpoint");
            double expected = first.score * XgSim.RewardPerPoint + first.gradeBonus;
            Assert.AreEqual(expected, host.money, 1e-6);
            double money = host.money;
            var again = sim.Assess(XgTrack.Vision, host);
            Assert.IsFalse(again.record, "same model, no record");
            Assert.AreEqual(money, host.money, 1e-9, "no record, no pay");
        }

        [Test]
        public void GradeBonusesArePaidOnce()
        {
            var sim = WithPack("mnist"); sim.UseBoard = false; /* legacy curve */ var host = new Host { money = 0 };
            sim.S.vision.steps = 1e6; sim.S.vision.depth = 1;
            sim.Evaluate(sim.S.vision);
            var a = sim.Assess(XgTrack.Vision, host);
            Assert.GreaterOrEqual(a.grade, 1);
            Assert.AreEqual(a.grade, a.newGrade);
            double bonus = 0; for (int g = 1; g <= a.grade; g++) bonus += XgCatalog.GradeBonus[g];
            Assert.AreEqual(bonus, a.gradeBonus, 1e-9);
            sim.S.best.Clear();
            var b = sim.Assess(XgTrack.Vision, host);
            Assert.AreEqual(0, b.gradeBonus, 1e-9, "grades already reached pay nothing");
        }

        [Test]
        public void OverfittingMakesValidationPeakThenFall()
        {
            var sim = WithPack("mnist"); sim.UseBoard = false; /* legacy curve */
            var run = new XgRun { track = 0, arch = "vgg", dataset = "mnist", depth = 14, width = 5, lr = 3 };
            double tau = sim.Tau(run);
            run.steps = 6 * tau; sim.Evaluate(run); double peakish = run.valAcc;
            run.steps = 400 * tau; sim.Evaluate(run);
            Assert.Less(run.valAcc, peakish - .01, "a huge model on small data overfits");
            Assert.Greater(run.trainAcc, run.valAcc);
        }

        [Test]
        public void LayersPastTheLimitHurt()
        {
            var sim = WithPack("mnist");
            var ok = new XgRun { track = 0, arch = "lenet", dataset = "mnist", depth = 5, width = 3 };
            var deep = new XgRun { track = 0, arch = "lenet", dataset = "mnist", depth = 9, width = 3 };
            Assert.Greater(sim.PeakAccuracy(ok, 3), sim.PeakAccuracy(deep, 3));
            sim.S.unlocked.Add("resnet");
            var res = new XgRun { track = 0, arch = "resnet", dataset = "mnist", depth = 9, width = 3 };
            Assert.Greater(sim.PeakAccuracy(res, 3), sim.PeakAccuracy(deep, 3));
        }

        [Test]
        public void TranslationNeedsAnEncoderDecoder()
        {
            var sim = WithPack("translate");
            var lstm = new XgRun { track = 1, arch = "lstm", dataset = "translate", depth = 3, width = 5 };
            var s2s = new XgRun { track = 1, arch = "seq2seq", dataset = "translate", depth = 3, width = 5 };
            Assert.Greater(sim.PeakAccuracy(s2s, 3), sim.PeakAccuracy(lstm, 3) + .2);
        }

        [Test]
        public void RecordRewardScalesWithTheDatasetRewardBaseAndPaysOnce()
        {
            var sim = WithPack("mnist"); var host = new Host();
            sim.S.vision.depth = 5; sim.S.vision.width = 3; sim.S.vision.steps = 1e5; sim.Evaluate(sim.S.vision);
            var a = sim.Assess(XgTrack.Vision, host);
            Assert.IsTrue(a.record);
            Assert.AreEqual(a.score * XgCatalog.Dataset("mnist").rewardBase * XgSim.RewardPerPoint, a.reward, 1e-6);
            var again = sim.Assess(XgTrack.Vision, host);
            Assert.IsFalse(again.record); Assert.AreEqual(0, again.reward);
        }

        [Test]
        public void ContractsNeedTheThresholdAndPayIntoTheWallet()
        {
            var sim = new XgSim(); var host = new Host { money = 0 };
            Assert.IsFalse(sim.Sign("cheque", host));
            sim.S.best.Add(new XgBest { dataset = "mnist", arch = "lenet", acc = .97 });
            Assert.IsTrue(sim.Sign("cheque", host));
            Assert.AreEqual(60, host.money, 1e-9);
            sim.Tick(10, host);
            Assert.AreEqual(75, host.money, 1e-6, "¥1.5/s at exactly the threshold");
        }

        [Test]
        public void HighRateWithoutOptimizerDiverges()
        {
            var sim = WithPack("mnist"); sim.UseBoard = false; /* legacy curve */ var host = new Host { compute = 4 };
            Assert.IsFalse(sim.SetLr(XgTrack.Vision, 0), "the knob is a skill-tree node");
            sim.S.unlocked.Add("v.lr");
            Assert.IsTrue(sim.SetLr(XgTrack.Vision, 0));
            Assert.Greater(sim.Hazard(sim.S.vision), 0);
            for (int i = 0; i < 2000 && sim.S.nanEvents == 0; i++) sim.TrainEpoch(XgTrack.Vision, host);
            Assert.Greater(sim.S.nanEvents, 0);
            Assert.AreEqual(0, sim.S.combo, "NaN breaks the combo");
            sim.S.unlocked.Add("adam");
            Assert.AreEqual(0, sim.Hazard(sim.S.vision), 1e-9, "Adam is stable at 0.1 on a CNN");
        }

        // ───────────── skill tree ─────────────

        // ───────────── 模型仓库 ─────────────

        [Test]
        public void EveryRecordIsSavedToTheRepositoryAndDeployed()
        {
            var sim = WithPack("mnist"); var host = new Host();
            for (int i = 0; i < 8; i++) sim.TrainEpoch(XgTrack.Vision, host);
            Assert.AreEqual(sim.S.records, sim.S.models.Count, "one entry per record");
            var last = sim.S.models[sim.S.models.Count - 1];
            Assert.IsTrue(last.record);
            Assert.AreEqual(sim.BestAcc("mnist"), last.acc, 1e-9);
            Assert.IsTrue(sim.IsDeployed(last));
            StringAssert.EndsWith(".ckpt", last.name);
            Assert.IsNotEmpty(last.curve);
            Assert.IsFalse(sim.DeleteModel(last.id), "the deployed model stays");
        }

        [Test]
        public void LoadingAModelRestoresShapeAndProgress()
        {
            var sim = WithPack("mnist"); var host = new Host();
            sim.S.unlocked.Add("v.d3");
            sim.SetDepth(XgTrack.Vision, 3, host);
            for (int i = 0; i < 6; i++) sim.TrainEpoch(XgTrack.Vision, host);
            var saved = sim.SaveModel(XgTrack.Vision);
            double acc = sim.S.vision.valAcc;
            for (int i = 0; i < 20; i++) sim.TrainEpoch(XgTrack.Vision, host);
            sim.SetDepth(XgTrack.Vision, 1, host);
            Assert.IsTrue(sim.LoadModel(saved.id));
            Assert.AreEqual(3, sim.S.vision.depth);
            Assert.AreEqual(saved.epoch, sim.S.vision.epoch);
            Assert.AreEqual(acc, sim.S.vision.valAcc, 1e-9);
            sim.S.unlocked.Remove("v.d3");
            Assert.IsNotNull(sim.CannotLoad(saved), "a model deeper than the cap cannot be loaded");
        }

        [Test]
        public void RepositoryCleansOldModelsButKeepsStarredAndDeployed()
        {
            var sim = WithPack("mnist");
            Assert.IsNull(sim.SaveModel(XgTrack.Vision), "nothing trained, nothing to save");
            sim.TrainEpoch(XgTrack.Vision, new Host());
            var first = sim.SaveModel(XgTrack.Vision);
            sim.StarModel(first.id, true);
            for (int i = 0; i < XgSim.MaxModels + 10; i++) sim.SaveModel(XgTrack.Vision);
            Assert.AreEqual(XgSim.MaxModels, sim.S.models.Count);
            var old = new XgState(); old.best.Add(new XgBest { dataset = "mnist", arch = "lenet", acc = .9 });
            var migrated = new XgSim(old);
            Assert.AreEqual(1, migrated.S.models.Count, "a checkpoint from before the repository gets an entry");
            Assert.IsTrue(migrated.IsDeployed(migrated.S.models[0]));
            Assert.IsNotNull(sim.Model(first.id));
            var restored = new XgSim(new XgState { models = sim.S.models, nextModelId = 0 });
            Assert.Greater(restored.S.nextModelId, sim.S.models[sim.S.models.Count - 1].id, "ids keep counting after a reload");
        }

        // ───────────── 唐诗 ─────────────

        [Test]
        public void EveryPoemLineKnowsItsPoemAndCouplet()
        {
            int covered = 0;
            foreach (var poem in XgCatalog.Poems) { Assert.AreEqual(0, poem.count % 2, poem.title); covered += poem.count; }
            Assert.AreEqual(XgCatalog.PoemLines.Length, covered);
            foreach (var line in XgCatalog.PoemLines)
            {
                var poem = XgCatalog.PoemOf(line, out string partner, out bool first);
                Assert.IsNotNull(poem, line);
                Assert.IsFalse(string.IsNullOrEmpty(poem.titleEn) || string.IsNullOrEmpty(poem.authorEn), line);
                Assert.AreEqual(line.Length, partner.Length, line + " / " + partner);
                Assert.AreNotEqual(line, partner);
            }
            XgCatalog.PoemOf("床前明月光", out string p1, out bool f1);
            Assert.AreEqual("疑是地上霜", p1); Assert.IsTrue(f1);
            XgCatalog.PoemOf("低头思故乡", out string p2, out bool f2);
            Assert.AreEqual("举头望明月", p2); Assert.IsFalse(f2);
        }

        // ───────────── 加薪 ─────────────

        [Test]
        public void RaiseLadderGoesFromOneToThirtyTimes()
        {
            Assert.AreEqual(1, XgCatalog.RaiseMultipliers[0]);
            Assert.AreEqual(30, XgCatalog.RaiseMultipliers[XgCatalog.RaiseMax]);
            for (int i = 1; i <= XgCatalog.RaiseMax; i++)
            {
                Assert.GreaterOrEqual(XgCatalog.RaiseMultipliers[i] - XgCatalog.RaiseMultipliers[i - 1], .5 - 1e-9, "every raise adds at least ×0.5");
                Assert.Greater(XgCatalog.RaiseCost(i), XgCatalog.RaiseCost(i - 1));
            }
            Assert.AreEqual(10, XgCatalog.RaiseCost(0));
            Assert.AreNotEqual(XgCatalog.RaiseTitle(0, false), XgCatalog.RaiseTitle(XgCatalog.RaiseMax, false));
        }

        [Test]
        public void ZeroWalletCanEarnTheFirstRaiseEvenWithoutPower()
        {
            var sim = new XgSim { GoldChance = 0 };
            var host = new Host { money = 0, compute = 0, blocker = "停电" };
            Assert.AreEqual(0, sim.BuyRaise(host));
            for (int i = 0; i < 15; i++) sim.Answer("mnist", sim.Card("mnist").truth, host);
            Assert.GreaterOrEqual(host.money, 10);
            Assert.AreEqual(1, sim.BuyRaise(host));
            Assert.AreEqual(1.5, sim.RaiseMultiplier, 1e-9);
            Assert.AreEqual(0, sim.S.epochs);
        }

        [Test]
        public void RaisesStackUpToThirtyAndStop()
        {
            var sim = new XgSim(); var host = new Host { money = 1e9 };
            double cost = sim.RaiseCostFor(3, out int levels);
            Assert.AreEqual(3, levels);
            Assert.AreEqual(10 + 17 + 29, cost, 1e-9);
            Assert.AreEqual(3, sim.BuyRaise(host, 3));
            Assert.AreEqual(1e9 - cost, host.money, 1e-6);
            Assert.AreEqual(XgCatalog.RaiseMax - 3, sim.BuyRaise(host, 100));
            Assert.AreEqual(30, sim.RaiseMultiplier, 1e-9);
            Assert.AreEqual(0, sim.BuyRaise(host));
            Assert.IsTrue(double.IsPositiveInfinity(sim.NextRaiseCost));
        }

        [Test]
        public void AffordableRaisesCountsWhatTheWalletCovers()
        {
            var sim = new XgSim(); var host = new Host { money = 10 + 17 + 5 };
            Assert.AreEqual(2, sim.AffordableRaises(host));
            Assert.AreEqual(2, sim.BuyRaise(host, 99));
            Assert.AreEqual(5, host.money, 1e-9);
        }

        [TestCase("mnist", false)]
        [TestCase("logic", false)]
        [TestCase("mnist", true)]
        public void RaiseMultipliesHandPayButNotSamples(string desk, bool gold)
        {
            var sim = new XgSim { GoldChance = 0 }; var host = new Host { money = 0 };
            sim.S.payRaise = 10;
            var card = sim.Card(desk); card.gold = gold; card.level = 3;
            double expected = sim.PayFor(desk, card.level) * XgCatalog.RaiseMultipliers[10] * 1.05 * (gold ? 3 : 1);
            var result = sim.Answer(desk, card.truth, host);
            Assert.AreEqual(expected, result.pay, 1e-9);
            Assert.AreEqual(1, sim.Samples(desk));
            var wrong = sim.Answer(desk, !sim.Card(desk).truth, host);
            Assert.AreEqual(0, wrong.pay);
        }

        [Test]
        public void RaiseAppliesToChatLabelsSurvivesReloadAndMigratesTheOldNode()
        {
            var sim = new XgSim(); var host = new Host { money = 1000 };
            sim.BuyRaise(host, 3);
            var restored = new XgSim(new XgState { payRaise = sim.S.payRaise });
            Assert.AreEqual(restored.PayFor("logic", 1) * XgCatalog.RaiseMultipliers[3], restored.ChatLabel("logic", true, host), 1e-9);
            var old = new XgState(); old.unlocked.Add("manual_pay");
            var migrated = new XgSim(old);
            Assert.AreEqual(XgCatalog.RaiseMultipliers[5], migrated.RaiseMultiplier, 1e-9, "the old one-off ×2 node becomes raise level 5");
            Assert.IsFalse(migrated.Has("manual_pay"));
            Assert.AreEqual(1, new XgSim().RaiseMultiplier, "fresh saves start at ×1");
        }

        [Test]
        public void RaiseDoesNotBoostAutomaticAnswers()
        {
            // The 摆渡众包 spot checks would fine this 80% model's wrong labels; they are covered in XgQualityTests.
            var sim = new XgSim { PlatformChecks = false }; var host = new Host { money = 100 };
            sim.S.desksOpen.Add("mnist");
            sim.RevealAutoLabel(); // the protagonist has had the idea (XgEpiphanyTests covers the hidden state)
            sim.S.best.Add(new XgBest { dataset = "mnist", arch = "lenet", acc = .8 });
            Assert.IsTrue(sim.BuyAuto("mnist", host));
            double income = sim.AutoIncome("mnist", host);
            sim.S.payRaise = XgCatalog.RaiseMax;
            Assert.AreEqual(income, sim.AutoIncome("mnist", host), 1e-9);
            double before = host.money;
            sim.Tick(30, host);
            Assert.Greater(sim.S.autoCorrect, 0);
            Assert.AreEqual(sim.S.autoCorrect * XgCatalog.LabelPay, host.money - before, 1e-9);
        }

        [Test]
        public void NodesNeedTheirParentAndMoney()
        {
            var sim = LegacyStageThree(); var host = new Host { money = 100 };
            var d4 = XgCatalog.Node("v.d4");
            Assert.AreEqual(XgSim.NodeStatus.Locked, sim.Status(d4, host));
            Assert.IsTrue(sim.BuyNode("v.d3", host));
            Assert.AreEqual(20, host.money, 1e-9);
            Assert.AreEqual(3, sim.S.vision.depth, "buying a layer node grows the model");
            Assert.AreEqual(XgSim.NodeStatus.TooExpensive, sim.Status(d4, host));
            host.money = 1000;
            Assert.IsTrue(sim.BuyNode("alexnet", host), "architectures no longer wait for papers");
            Assert.AreEqual("alexnet", sim.S.vision.arch, "a new architecture is switched to");
            Assert.AreEqual(XgSim.NodeStatus.Locked, sim.Status(XgCatalog.Node("v.d6"), host), "needs v.d5 too");
        }

        [Test]
        public void DepthAndWidthAreFreeUpToTheirCaps()
        {
            var sim = LegacyStageThree(); var host = new Host { money = 0 };
            Assert.AreEqual(2, sim.DepthCap(XgTrack.Vision));
            Assert.IsFalse(sim.SetDepth(XgTrack.Vision, 5, host), "already at cap 2");
            Assert.IsTrue(sim.SetDepth(XgTrack.Vision, 1, host));
            sim.S.unlocked.Add("v.d3"); sim.S.unlocked.Add("v.w1");
            Assert.IsTrue(sim.SetDepth(XgTrack.Vision, 9, host));
            Assert.AreEqual(3, sim.S.vision.depth);
            Assert.IsTrue(sim.SetWidth(XgTrack.Vision, 6, host));
            Assert.AreEqual(1, sim.S.vision.width);
            Assert.AreEqual(0, host.money, 1e-9);
        }

        [Test]
        public void VramLimitsModelSize()
        {
            var sim = new XgSim(); var host = new Host { money = 1e9, vram = 100 };
            foreach (var n in XgCatalog.Nodes) if (n.kind == XgNodeKind.Width && n.tree == "vision") sim.S.unlocked.Add(n.id);
            for (int w = 1; w < XgCatalog.Widths.Length; w++) sim.SetWidth(XgTrack.Vision, w, host);
            Assert.Greater(sim.S.vision.width, 0);
            Assert.LessOrEqual(XgSim.VramNeedMB(sim.S.vision), 100);
        }

        [Test]
        public void DataNodesBuyThePack()
        {
            var sim = new XgSim(); var host = new Host { money = 1000 };
            Assert.IsFalse(sim.DatasetAvailable("news"));
            Assert.IsTrue(sim.BuyNode("spam.pack", host));
            Assert.IsTrue(sim.Owns("spam"));
            Assert.IsTrue(sim.TrainingUnlocked(XgTrack.Sequence));
        }

        [Test]
        public void AutomationTrainsOnItsOwn()
        {
            var sim = LegacyStageThree("resnet"); sim.S.owned.Add("mnist"); sim.Evaluate(sim.S.vision); var host = new Host { money = 1e6 };
            foreach (var id in new[] { "auto1", "auto2" }) Assert.IsTrue(sim.BuyNode(id, host));
            Assert.AreEqual(2, sim.AutoTrainLevel);
            Assert.IsTrue(sim.S.vision.running, "crontab switches auto-training on");
            sim.Tick(30, host);
            Assert.AreEqual(0, sim.S.vision.epoch, "crontab only runs with the window open");
            sim.WindowOpen = true;
            sim.Tick(30, host);
            Assert.AreEqual(9, sim.S.vision.epoch, "every 3 s starts an action; the tenth is still running at 30 s");
            Assert.IsTrue(sim.S.vision.epochActive);
            sim.Tick(.6, host);
            Assert.AreEqual(10, sim.S.vision.epoch, "the tenth action settles only after its duration");
            Assert.AreEqual(0, sim.S.combo, "automatic epochs do not build the combo");
            sim.WindowOpen = false;
            Assert.IsTrue(sim.BuyNode("auto3", host));
            sim.Tick(20, host);
            Assert.AreEqual(19, sim.S.vision.epoch, "daemon starts every 2 s; the last action is still running at this boundary");
            Assert.IsTrue(sim.S.vision.epochActive);
            sim.Tick(.6, host);
            Assert.AreEqual(20, sim.S.vision.epoch, "background epochs settle after the same action duration");
            Assert.IsTrue(sim.BuyNode("auto4", host));
            Assert.AreEqual(1, sim.EvalEvery);
        }

        [Test]
        public void BrokenSaveIsRepaired()
        {
            var state = new XgState { vision = null };
            state.sequence.arch = "lenet"; state.sequence.width = 99; state.sequence.depth = 40; state.unlocked.Clear(); state.owned = null; state.cards = null;
            state.autoVision = 2; state.labelTrack = 2; state.desk = "";
            var sim = new XgSim(state);
            Assert.AreEqual("perceptron", sim.S.vision.arch);
            Assert.AreEqual("perceptron", sim.S.sequence.arch);
            Assert.AreEqual(0, sim.S.sequence.width, "width is capped by the tree");
            Assert.AreEqual(1, sim.S.sequence.depth);
            Assert.IsTrue(sim.Has("perceptron"));
            Assert.IsFalse(sim.Has("lenet") || sim.Has("rnn"), "a corrupt empty unlock list cannot invent specialties");
            Assert.IsFalse(sim.Owns("mnist"), "packs are bought, never granted");
            Assert.AreEqual(2, sim.AutoLevel("mnist"), "old auto-answer levels migrate");
            Assert.AreEqual("mnist", sim.S.desk);
        }

        // ───────────── labelling and combo ─────────────

        [Test]
        public void RightAnswerPaysAndAddsASample()
        {
            var sim = new XgSim { GoldChance = 0 }; var host = new Host { money = 0 };
            var r = sim.Answer("mnist", sim.Card("mnist").truth, host);
            Assert.IsTrue(r.correct);
            Assert.AreEqual(XgCatalog.LabelPay * 1.05, host.money, 1e-9, "first combo step");
            Assert.AreEqual(1, sim.Samples("mnist"), 1e-9);
            Assert.AreEqual(1, sim.S.combo);
        }

        [Test]
        public void WrongAnswerPaysNothingResetsComboAndDiscardsTheLabel()
        {
            var sim = new XgSim { GoldChance = 0 }; var host = new Host { money = 0 };
            int broken = 0; sim.ComboBroken += n => broken = n;
            Label(sim, "mnist", 5, host);
            double money = host.money;
            var r = sim.Answer("mnist", !sim.Card("mnist").truth, host);
            Assert.IsFalse(r.correct);
            Assert.AreEqual(money, host.money, 1e-9);
            Assert.AreEqual(0, sim.S.combo);
            Assert.AreEqual(5, broken);
            Assert.AreEqual(5, sim.Samples("mnist"), 1e-9);
            Assert.AreEqual(1, sim.S.handWrong);
        }

        [Test]
        public void ComboIsSharedTimesOutAndHasTiers()
        {
            var sim = WithPack("mnist"); var host = new Host();
            var tiers = new List<int>(); sim.ComboTier += tiers.Add;
            Label(sim, "mnist", 5, host);
            for (int i = 0; i < 5; i++) sim.TrainEpoch(XgTrack.Vision, host);
            Assert.GreaterOrEqual(sim.S.combo, 10, "labels and epochs feed one combo");
            CollectionAssert.Contains(tiers, 0);
            sim.Tick(5, host);
            Assert.AreEqual(0, sim.S.combo, "idle too long");
            Assert.GreaterOrEqual(sim.S.bestCombo, 10);
        }

        [Test]
        public void EnoughLabelsUnlockTraining()
        {
            var sim = new XgSim { GoldChance = 0 }; var host = new Host();
            Label(sim, "spam", XgCatalog.SamplesToTrain - 1, host);
            Assert.IsFalse(sim.TrainingUnlocked(XgTrack.Sequence));
            Label(sim, "spam", 1, host);
            Assert.IsTrue(sim.TrainingUnlocked(XgTrack.Sequence));
            Assert.IsFalse(sim.TrainingUnlocked(XgTrack.Vision), "each track labels its own data; digits open at stage two");
            Assert.IsNotNull(sim.TrainEpoch(XgTrack.Sequence, host));
            Assert.IsTrue(sim.DeskOpen("spam"), "the initial keyword SMS desk is free");
            Assert.IsFalse(sim.DeskOpen("danmu"), "future desks require their data pack, not an epoch milestone");
        }

        [Test]
        public void MoreLabelsRaiseTheCeiling()
        {
            var sim = new XgSim { GoldChance = 0 }; var host = new Host();
            Label(sim, "mnist", 30, host);
            double few = sim.PeakAccuracy(sim.S.vision, 3);
            Label(sim, "mnist", 270, host);
            double more = sim.PeakAccuracy(sim.S.vision, 3);
            sim.S.owned.Add("mnist");
            double pack = sim.PeakAccuracy(sim.S.vision, 3);
            Assert.Greater(more, few + .05);
            Assert.Greater(pack, more);
        }

        [Test]
        public void AutoAnsweringNeedsACheckpointAndPaysOnlyRightAnswers()
        {
            // Without 拿不准才问我 an 80% model is reported and frozen by the platform; this test measures the raw rate.
            var sim = new XgSim { PlatformChecks = false }; var host = new Host { money = 1000 };
            sim.S.desksOpen.Add("mnist");
            sim.RevealAutoLabel(); // the protagonist has had the idea (XgEpiphanyTests covers the hidden state)
            Assert.IsFalse(sim.BuyAuto("mnist", host), "no checkpoint yet");
            sim.S.best.Add(new XgBest { dataset = "mnist", arch = "lenet", acc = .8 });
            Assert.IsTrue(sim.BuyAuto("mnist", host));
            Assert.AreEqual(1000 - XgCatalog.AutoCost(0), host.money, 1e-9);
            double start = host.money;
            sim.Tick(1000, host);
            double answered = sim.S.autoCorrect + sim.S.autoWrong;
            Assert.AreEqual(300, answered, 1.5, "0.3 cards/s at level 1");
            Assert.AreEqual(.8, sim.S.autoCorrect / answered, .07);
            Assert.AreEqual(sim.S.autoCorrect * XgCatalog.LabelPay, host.money - start, 1e-6);
            Assert.AreEqual(sim.S.autoCorrect, sim.Samples("mnist"), 1e-9, "wrong auto labels are discarded too");
            Assert.AreEqual(0, sim.S.combo, "auto answers never build the combo");
        }

        [Test]
        public void SuggestionIsRightAsOftenAsTheCheckpoint()
        {
            var sim = LegacyStageThree(); var host = new Host();
            Assert.IsFalse(sim.Suggestion("mnist", out _, out _));
            sim.S.best.Add(new XgBest { dataset = "mnist", arch = "lenet", acc = .75 });
            int right = 0, n = 2000;
            for (int i = 0; i < n; i++)
            {
                Assert.IsTrue(sim.Suggestion("mnist", out bool yes, out double confidence));
                if (yes == sim.Card("mnist").truth) right++;
                sim.Answer("mnist", sim.Card("mnist").truth, host);
            }
            Assert.AreEqual(.75, (double)right / n, .04);
        }

        [Test]
        public void GoldCardsPayTripleButExpire()
        {
            var sim = new XgSim { GoldChance = 1 }; var host = new Host { money = 0 };
            var card = sim.Card("mnist");
            Assert.IsTrue(card.gold);
            sim.Answer("mnist", card.truth, host);
            Assert.AreEqual(XgCatalog.LabelPay * 1.05 * 3, host.money, 1e-9);
            sim.S.desk = "mnist"; sim.WindowOpen = true;
            int timeouts = 0; sim.CardTimedOut += _ => timeouts++;
            Label(sim, "mnist", 3, host);
            sim.Tick(3.5, host);
            Assert.AreEqual(1, timeouts, "3 s limit on a picture card");
            Assert.AreEqual(0, sim.S.combo);
        }

        [Test]
        public void TrickCardsAddTwoToTheCombo()
        {
            var sim = new XgSim { GoldChance = 0 }; var host = new Host();
            sim.S.desksOpen.Add("danmu");
            sim.S.labels.Add(new XgLabelCount { dataset = "danmu", count = 200 });
            int tricks = 0;
            for (int i = 0; i < 400; i++)
            {
                var c = sim.Card("danmu");
                int before = sim.S.combo;
                sim.Answer("danmu", c.truth, host);
                if (c.trick) { tricks++; Assert.AreEqual(before + 2, sim.S.combo); }
            }
            Assert.Greater(tricks, 20, "about one in seven at level 3");
        }

        [Test]
        public void DesksOpenOnlyWhenTheirDataPackIsBought()
        {
            var sim = LegacyStageThree("lstm", "resnet"); var host = new Host { money = 1e6 };
            Assert.IsFalse(sim.DeskOpen("meme"));
            sim.Tick(1, host);
            Assert.IsFalse(sim.DeskOpen("meme"), "time alone does not open a desk");
            Assert.IsFalse(sim.DeskOpen("headline"));
            Assert.IsTrue(sim.BuyNode("meme.pack", host)); Assert.IsTrue(sim.DeskOpen("meme"));
            Assert.IsTrue(sim.BuyNode("headline.pack", host)); Assert.IsTrue(sim.DeskOpen("headline"));
            Assert.IsTrue(sim.BuyNode("review.pack", host)); Assert.IsTrue(sim.DeskOpen("review"));
            Assert.IsTrue(sim.BuyNode("cifar.pack", host)); Assert.IsTrue(sim.DeskOpen("cifar"));
            Assert.IsTrue(sim.DatasetAvailable("cifar"), "the purchased captcha desk makes CIFAR trainable");
            Assert.IsFalse(sim.DeskOpen("go"));
            sim.S.labels.Add(new XgLabelCount { dataset = "poems", count = 500 }); sim.Tick(1, host);
            Assert.IsFalse(sim.DeskOpen("go"), "label totals cannot bypass the data pack");
            Assert.IsTrue(sim.BuyNode("go.pack", host)); Assert.IsTrue(sim.DeskOpen("go"));
        }

        [Test]
        public void EveryDeskMakesWellFormedHalfYesCards()
        {
            var sim = LegacyStageThree(); var host = new Host();
            foreach (var d in XgCatalog.Desks) if (!sim.S.desksOpen.Contains(d.id)) sim.S.desksOpen.Add(d.id);
            foreach (var d in XgCatalog.Desks)
            {
                int yes = 0, n = 600;
                for (int i = 0; i < n; i++)
                {
                    var c = sim.Card(d.id);
                    if (c.truth) yes++;
                    Check(c, d);
                    sim.Answer(d.id, c.truth, host);
                }
                Assert.That((double)yes / n, Is.InRange(.38, .62), d.id + " should be about half yes");
            }
        }

        static void Check(XgCard c, XgDesk d)
        {
            if (c.bottleneckPreview && c.kind == "long")
            {
                Assert.IsNotEmpty(c.sourceText); Assert.IsNotEmpty(c.question); Assert.IsNotEmpty(c.why);
                Assert.AreEqual(c.sourceText.Contains("当前电脑贴着蓝色便签"), c.truth);
                Assert.AreEqual(c.sourceText.Split('\n').Length - 1, c.distance);
                return;
            }
            switch (d.kind)
            {
                case XgDeskKind.Digit: Assert.AreEqual(c.truth, c.digit == c.asked); break;
                case XgDeskKind.Poem: Assert.AreEqual(c.truth, c.line.Substring(c.shown, 1) == c.askedChar); break;
                case XgDeskKind.Text: case XgDeskKind.Logic: Assert.IsNotEmpty(c.question); Assert.IsNotEmpty(c.why); break;
                case XgDeskKind.Captcha: Assert.AreEqual(c.truth, c.digit == c.asked && (c.shown < 0 || c.shown == c.asked), c.question); break;
                case XgDeskKind.Meme: Assert.AreEqual(c.truth, c.digit == c.asked); break;
                case XgDeskKind.Go: CheckGo(c); break;
            }
        }

        /// <summary>Recount liberties with an independent flood fill.</summary>
        static void CheckGo(XgCard c)
        {
            Assert.AreEqual(81, c.line.Length);
            var b = c.line;
            Assert.AreEqual('B', b[c.digit]);
            var group = new HashSet<int>(); var libs = new HashSet<int>(); var todo = new Queue<int>();
            todo.Enqueue(c.digit); group.Add(c.digit);
            while (todo.Count > 0)
            {
                int p = todo.Dequeue(); int x = p % 9, y = p / 9;
                foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                {
                    int nx = x + dx, ny = y + dy; if (nx < 0 || ny < 0 || nx > 8 || ny > 8) continue;
                    int n = ny * 9 + nx;
                    if (b[n] == '.') libs.Add(n);
                    else if (b[n] == 'B' && group.Add(n)) todo.Enqueue(n);
                }
            }
            Assert.Greater(libs.Count, 0, "legal position");
            if (c.question.Contains("一口气")) Assert.AreEqual(c.truth, libs.Count == 1);
            else if (c.question.Contains("正好有")) Assert.AreEqual(c.truth, libs.Count == c.shown);
            else Assert.AreEqual(c.truth, libs.Count == 1 && libs.Contains(c.asked), c.question);
        }

        [Test]
        public void NoMemeFromTheFutureBeforeItsDay()
        {
            var sim = new XgSim { GoldChance = 0, Today = 20160529 }; var host = new Host();
            foreach (var d in XgCatalog.Desks) if (!sim.S.desksOpen.Contains(d.id)) sim.S.desksOpen.Add(d.id);
            var late = new HashSet<string>();
            foreach (var desk in new[] { "danmu", "spam", "headline", "review", "translate" })
                foreach (var p in XgMemes.Bank(desk)) if (p.since > 20160529) late.Add(p.text);
            Assert.IsNotEmpty(late, "the bank holds some later memes");
            foreach (var desk in new[] { "danmu", "spam", "headline", "review", "translate", "meme" })
            {
                sim.S.labels.Add(new XgLabelCount { dataset = desk, count = 200 });
                for (int i = 0; i < 400; i++)
                {
                    var c = sim.Card(desk);
                    Assert.IsFalse(late.Contains(c.question), c.question);
                    Assert.AreNotEqual("蓝瘦，香菇", c.line);
                    sim.Answer(desk, c.truth, host);
                }
            }
            sim.Today = 20161231;
            bool seen = false;
            for (int i = 0; i < 2000 && !seen; i++) { var c = sim.Card("danmu"); seen = late.Contains(c.question); sim.Answer("danmu", c.truth, host); }
            Assert.IsTrue(seen, "later memes appear once their day comes");
        }
    }
}
