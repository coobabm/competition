using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>
    /// 经济压力与破产 on the lab's side: abilities 2–5 are easy (the auto-configured model from a few cheap squares, a
    /// few rounds, about one pack per stage); pre-training needs the effective scale, which only the stacked stage-6
    /// items give; and the to-do note puts the rent first when the wallet is below zero.
    /// </summary>
    public sealed class XgEconomyTests
    {
        sealed class Host : IXgHost
        {
            public double money = 1000000;
            public double Compute => 4;
            public double VramMB => 100000;
            public double Money => money;
            public bool Spend(double amount) { if (money < amount) return false; money -= amount; return true; }
            public void Earn(double amount) { money += amount; }
            public void Train(double seconds) { }
            public string Blocker => null;
        }

        /// <summary>A lab that has the abilities before <paramref name="next"/> and owns only <paramref name="nodes"/>.</summary>
        static XgSim Lab(int next, params string[] nodes)
        {
            var state = new XgState { progressionVersion = XgSim.ProgressionSchema, stage = next - 1, stageVision = next - 1, stageSequence = next - 1 };
            for (int i = 1; i < next; i++) state.abilitiesEmerged.Add(i);
            var sim = new XgSim(state) { GoldChance = 0 };
            foreach (var id in nodes) if (!sim.Has(id)) sim.S.unlocked.Add(id);
            foreach (var d in new[] { "logic", "spam", "mnist", "danmu", "cifar", "meme" }) if (!sim.S.desksOpen.Contains(d)) sim.S.desksOpen.Add(d);
            return new XgSim(sim.S) { GoldChance = 0 };
        }

        static void Labels(XgSim sim, string dataset, double count)
        {
            var c = sim.S.labels.Find(x => x.dataset == dataset);
            if (c == null) sim.S.labels.Add(new XgLabelCount { dataset = dataset, count = count });
            else c.count += count;
        }

        // ───────────── abilities 2–5 ─────────────

        /// <summary>The cheap squares that reach each ability (the to-do note's plan), cumulative.</summary>
        static readonly string[][] CheapNodes =
        {
            new string[0], new string[0],
            new[] { "mlp", "s.d2", "s.w1", "s.w2" },
            new[] { "lenet", "v.d3", "v.d4" },
            new[] { "alexnet", "v.d5", "v.d6" },
            // LSTM is the parent of the long-text packs.
            new[] { "vgg", "v.d8", "lstm" },
        };

        /// <summary>About one pack per stage plus a few hundred hand labels: what a player picks up on the way.</summary>
        static readonly (string dataset, double samples)[][] ModestData =
        {
            new (string, double)[0], new (string, double)[0],
            new[] { ("spam", 12000.0), ("logic", 600.0) },
            new[] { ("logic", 100000.0), ("mnist", 500.0) },
            new[] { ("poems", 200000.0), ("cifar", 500.0) },
            new[] { ("longtext", 80000.0), ("crosssentence", 80000.0), ("meme", 500.0) },
        };

        [TestCase(2, "logic", XgTrack.Sequence)]
        [TestCase(3, "mnist", XgTrack.Vision)]
        [TestCase(4, "cifar", XgTrack.Vision)]
        [TestCase(5, "cifar", XgTrack.Vision)]
        public void AbilitiesTwoToFiveComeFromTheDefaultAutoModelAndModestData(int ability, string dataset, XgTrack track)
        {
            var nodes = new List<string>();
            for (int i = 2; i <= ability; i++) nodes.AddRange(CheapNodes[i]);
            var sim = Lab(ability, nodes.ToArray());
            var host = new Host();
            for (int i = 2; i <= ability; i++) foreach (var (d, n) in ModestData[i]) Labels(sim, d, n);
            // Only the auto-configured model, a few rounds on one desk, assessed along the way.
            sim.SetDataset(track, dataset);
            for (int round = 0; round < 40 && !sim.HasAbility(ability); round++) { sim.TrainEpoch(track, host); sim.Tick(.1, host); }
            Assert.That(XgSim.ParamsK(sim.Run(track)), Is.GreaterThanOrEqualTo(sim.ParamsThreshold(ability)), "the default model of the cheap squares is big enough");
            Assert.That(sim.TrainedSamples, Is.GreaterThanOrEqualTo(sim.SamplesThreshold(ability)), "about one pack per stage is enough data");
            Assert.That(sim.HasAbility(ability), Is.True, "ability " + ability + ": P " + XgSim.ParamsText(sim.TrainedParamsK) + "/" + XgSim.ParamsText(sim.ParamsThreshold(ability)));
        }

        [Test]
        public void TheEasyThresholdsStillRiseAndTheDesksKeepTheirOldLadder()
        {
            for (int i = 3; i <= XgSim.AbilityCount; i++)
            {
                Assert.That(XgSim.AbilityParamsK[i], Is.GreaterThan(XgSim.AbilityParamsK[i - 1]));
                Assert.That(XgSim.AbilitySamples[i], Is.GreaterThan(XgSim.AbilitySamples[i - 1]));
                Assert.That(XgSim.AbilityParamsK[i], Is.LessThan(XgSim.CeilingLadderK[i]), "easier than before");
            }
            Assert.That(XgSim.CeilingScaleK("mnist"), Is.EqualTo(.1 * XgSim.CeilingLadderK[3]), "grades keep their difficulty");
        }

        // ───────────── stage 6: the effective scale ─────────────

        static XgSim StageSix(params string[] items)
        {
            var sim = Lab(6);
            sim.S.stage = sim.S.stageVision = sim.S.stageSequence = 6;
            if (!sim.S.abilitiesEmerged.Contains(6)) sim.S.abilitiesEmerged.Add(6);
            foreach (var id in items) if (!sim.Has(id)) sim.S.unlocked.Add(id);
            // Both bars at ability 6's threshold.
            sim.S.trainedParamsK = XgSim.AbilityParamsK[6];
            Labels(sim, "poems", XgSim.AbilitySamples[6]);
            return sim;
        }

        static readonly string[] FullStack = { "transformer", "datacenter", "attention", "resnet", "fp16", "warmup", "batchnorm" };

        [Test]
        public void TheMultipliersStackMultiplicatively()
        {
            Assert.That(StageSix().ScaleMultiplier, Is.EqualTo(1));
            Assert.That(StageSix("transformer").ScaleMultiplier, Is.EqualTo(10));
            Assert.That(StageSix("transformer", "datacenter").ScaleMultiplier, Is.EqualTo(80));
            Assert.That(StageSix(FullStack).ScaleMultiplier, Is.EqualTo(10 * 8 * 3 * 2 * 2 * 1.5 * 1.5).Within(1e-9));
        }

        [Test]
        public void PretrainingStallsWithoutTheItemsAndPassesWithTheFullStack()
        {
            var host = new Host();
            var none = StageSix("datacenter");
            Assert.That(none.PretrainScaleReady, Is.False);
            Assert.That(none.PretrainCap, Is.LessThan(.2), "no items: the loss flattens almost at once");
            StringAssert.Contains("Transformer", none.PretrainLimit());

            var full = StageSix(FullStack);
            Assert.That(full.PretrainScaleReady, Is.True, "full stack, bars at ability 6");
            Assert.That(full.PretrainCap, Is.EqualTo(1));
            Assert.That(full.TogglePretrain(host), Is.True);
            for (int i = 0; i < 2000 && !full.S.abilities; i++) full.Tick(1, host);
            Assert.That(full.S.abilities, Is.True, "pre-training runs through and everything opens");
        }

        [TestCase("transformer")]
        [TestCase("attention")]
        [TestCase("datacenter")]
        public void OneMissingBigItemFailsEvenWithTheBarsAtTheirCap(string missing)
        {
            var items = Array.FindAll(FullStack, x => x != missing);
            var sim = StageSix(items);
            sim.S.trainedParamsK = XgSim.AbilityParamsK[6] * 10;
            Labels(sim, "news", XgSim.AbilitySamples[6] * 10);
            Assert.That(sim.ScaleBars, Is.EqualTo(XgSim.ScaleBarCap * XgSim.ScaleBarCap).Within(1e-9), "grinding the bars is capped");
            Assert.That(sim.PretrainScaleReady, Is.False, missing + " missing");
            Assert.That(sim.MissingScaleItems().ConvertAll(i => i.item), Is.EqualTo(new List<string> { missing }));
        }

        [Test]
        public void TwoMissingSmallItemsFailButOneCanBeMadeUpWithBiggerBars()
        {
            var noBoth = StageSix("transformer", "datacenter", "attention", "warmup", "batchnorm");
            noBoth.S.trainedParamsK = XgSim.AbilityParamsK[6] * 10; Labels(noBoth, "news", XgSim.AbilitySamples[6] * 10);
            Assert.That(noBoth.PretrainScaleReady, Is.False, "residuals and mixed precision both missing");

            var noBatchNorm = StageSix("transformer", "datacenter", "attention", "resnet", "fp16", "warmup");
            Assert.That(noBatchNorm.PretrainScaleReady, Is.False, "at the bare bars one small item short is not enough");
            noBatchNorm.S.trainedParamsK = XgSim.AbilityParamsK[6] * 10; Labels(noBatchNorm, "news", XgSim.AbilitySamples[6] * 10);
            Assert.That(noBatchNorm.PretrainScaleReady, Is.True, "with the bars at their cap it is");
        }

        [Test]
        public void TheNoteNamesTheMissingMultipliersInStageSix()
        {
            var sim = StageSix("transformer", "warmup", "datacenter");
            sim.S.treeIntroDelivered = true;
            sim.S.pretrain = sim.PretrainCap;
            sim.S.pretrainStalled = true;
            var steps = XgGuide.Current(sim, 0);
            var stall = steps.Find(x => x.id == "final.scale");
            Assert.IsNotNull(stall, string.Join(" | ", steps.ConvertAll(x => x.id)));
            StringAssert.Contains("×3 注意力", stall.zh);
            StringAssert.Contains("×2 混合精度", stall.zh);
            Assert.That(steps.Exists(x => x.id == "need.attention" || x.target == "node:seq2seq" || x.target == "node:attention"), Is.True, "a buy step for the biggest missing one");
        }

        // ───────────── the rent on the note ─────────────

        [Test]
        public void InDebtTheNotePutsTheRentFirst()
        {
            var sim = Lab(2, "mlp");
            Labels(sim, "logic", 40);
            var steps = XgGuide.Current(sim, -512.4, new XgGuideHouse { forumSeen = true, yySeen = true, gamesSeen = true });
            Assert.AreEqual("rent.debt", steps[0].id);
            Assert.AreEqual("赚钱交房租（欠 ¥513）", steps[0].zh);
            StringAssert.Contains("513", steps[0].en);
            var cut = XgGuide.Current(sim, -10, new XgGuideHouse { landlordCut = true });
            StringAssert.Contains("拉了电闸", cut[0].whyZh);
            Assert.AreNotEqual("rent.debt", XgGuide.Current(sim, 0)[0].id, "no line at zero");
        }
    }
}
