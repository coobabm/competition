using System.Collections.Generic;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>它在学_可视化借鉴设计 §2.5: the 12-block pre-training stack (XgSim.FinaleLayers.cs).</summary>
    public sealed class XgFinaleLayersTests
    {
        sealed class Host : IXgHost
        {
            public double money = 1e7, compute = 50;
            public double Money => money; public double Compute => compute; public double VramMB => 1e5; public string Blocker => null;
            public bool Spend(double a) { if (money < a) return false; money -= a; return true; }
            public void Earn(double a) { money += a; }
            public void Train(double s) { }
        }

        static XgSim AtStage6()
        {
            var state = new XgState { progressionVersion = XgSim.ProgressionSchemaWalls, stage = 6, stageVision = 6, stageSequence = 6 };
            var sim = new XgSim(state) { GoldChance = 0 };
            foreach (var n in XgCatalog.Nodes) if (n.stage <= 6 && n.tree != "label" && n.kind != XgNodeKind.Secret && n.id != "datacenter" && !sim.Has(n.id)) sim.S.unlocked.Add(n.id);
            sim.Profile = new XgProfile { name = "小灯", self = "本机", callMe = "老大", personality = "嘴硬心软" };
            return sim;
        }

        [Test]
        public void OneBlockLightsPerTwelfthOfProgress()
        {
            Assert.AreEqual(0, XgSim.PretrainLayersLitAt(0));
            Assert.AreEqual(0, XgSim.PretrainLayersLitAt(1 / 12.0 - 1e-4));
            Assert.AreEqual(1, XgSim.PretrainLayersLitAt(1 / 12.0), "exactly a twelfth lights the first block");
            Assert.AreEqual(6, XgSim.PretrainLayersLitAt(.5));
            Assert.AreEqual(11, XgSim.PretrainLayersLitAt(.99));
            Assert.AreEqual(12, XgSim.PretrainLayersLitAt(1));
            Assert.AreEqual(12, XgSim.PretrainLayersLitAt(1.5), "clamped");
            Assert.AreEqual(0, XgSim.PretrainLayersLitAt(-1));
            Assert.AreEqual(0, XgSim.PretrainLayersLitAt(double.NaN));

            var sim = AtStage6();
            sim.S.pretrain = .3;
            Assert.AreEqual(3, sim.PretrainLayersLit);
            Assert.AreEqual(.6, sim.PretrainLayerFill, 1e-6, "0.3 × 12 = 3.6: the fourth block is 60% done");
            sim.S.pretrain = 1; sim.S.abilities = true;
            Assert.AreEqual(12, sim.PretrainLayersLit);
            Assert.AreEqual(0, sim.PretrainLayerFill);
        }

        [Test]
        public void EveryLayerHasItsOwnLineInBothLanguages()
        {
            var zh = new HashSet<string>(); var en = new HashSet<string>();
            for (int layer = 1; layer <= XgSim.PretrainLayerCount; layer++)
            {
                string z = XgSim.PretrainLayerLine(layer, false), e = XgSim.PretrainLayerLine(layer, true);
                StringAssert.StartsWith("第 " + layer + " 层：", z);
                StringAssert.StartsWith("Layer " + layer + ": ", e);
                Assert.IsFalse(z.Contains("涌现"), "the game never says 涌现");
                Assert.IsTrue(zh.Add(z) && en.Add(e), "distinct lines");
            }
            StringAssert.Contains("主语和宾语", XgSim.PretrainLayerLine(5, false));
            StringAssert.Contains("下一个字", XgSim.PretrainLayerLine(12, false));

            var sim = AtStage6();
            sim.S.unlocked.Add("datacenter");
            StringAssert.Contains("还没有一层", sim.PretrainLayerHeadline());
            sim.S.pretrain = 5 / 12.0 + .01;
            Assert.AreEqual(XgSim.PretrainLayerLine(5, false), sim.PretrainLayerHeadline(), "the newest lit layer");
            sim.English = true;
            Assert.AreEqual(XgSim.PretrainLayerLine(5, true), sim.PretrainLayerHeadline());
        }

        [Test]
        public void AStallNamesTheNextLayerAndWhyItWillNotLight()
        {
            var sim = AtStage6();
            var host = new Host();
            sim.S.trainedParamsK = XgSim.ScaleUnitParamsK;
            sim.S.labels.Add(new XgLabelCount { dataset = "poems", count = XgSim.ScaleUnitSamples });
            var scale = new[] { "fp16", "batchnorm", "resnet" };
            foreach (var id in scale) sim.S.unlocked.Remove(id);

            // No server room: the breaker trips and the first block will not light.
            Assert.IsFalse(sim.TogglePretrain(host));
            Assert.IsTrue(sim.PretrainLayersStalled);
            StringAssert.StartsWith("第 1 层点不亮：", sim.PretrainLayerHeadline());
            StringAssert.Contains("机房", sim.PretrainLayerHeadline());

            // With the rack it runs up to the effective-scale plateau, then stalls on the missing multipliers.
            sim.S.unlocked.Add("datacenter");
            Assert.IsFalse(sim.PretrainLayersStalled, "the old flag alone is not a stall once the rack is in");
            Assert.IsTrue(sim.TogglePretrain(host));
            sim.Tick(600, host);
            Assert.IsTrue(sim.PretrainLayersStalled);
            int lit = sim.PretrainLayersLit;
            Assert.Less(lit, XgSim.PretrainLayerCount);
            string headline = sim.PretrainLayerHeadline();
            StringAssert.StartsWith("第 " + (lit + 1) + " 层点不亮：", headline);
            StringAssert.EndsWith(sim.PretrainLimit(), headline, "the reason is PretrainLimit()");
            StringAssert.Contains("混合精度", headline);

            // The multipliers lift the plateau: no stall, and the stack finishes.
            foreach (var id in scale) sim.S.unlocked.Add(id);
            Assert.IsFalse(sim.PretrainLayersStalled);
            if (!sim.S.pretrainRunning) Assert.IsTrue(sim.TogglePretrain(host));
            sim.Tick(600, host);
            Assert.IsTrue(sim.S.abilities);
            Assert.IsFalse(sim.PretrainLayersStalled);
            Assert.AreEqual(XgSim.PretrainLayerCount, sim.PretrainLayersLit);
            Assert.AreEqual(XgSim.PretrainLayerLine(12, false), sim.PretrainLayerHeadline());
        }

        [Test]
        public void TheColourFollowsTheLoss()
        {
            var sim = AtStage6();
            Assert.AreEqual(1, sim.PretrainLossHeat, 1e-6, "untrained: hottest");
            sim.S.pretrain = .5;
            double mid = sim.PretrainLossHeat;
            sim.S.pretrain = 1;
            Assert.Less(sim.PretrainLossHeat, mid);
            Assert.GreaterOrEqual(sim.PretrainLossHeat, 0);
        }
    }
}
