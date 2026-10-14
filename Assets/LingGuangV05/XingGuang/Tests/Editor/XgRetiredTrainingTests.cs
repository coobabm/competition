using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>
    /// Auto-training and the consumables are gone. An old save gets one refund at the purchase price (with a feed line) and
    /// loses the state; nothing for sale or in the tech tree is left of either.
    /// </summary>
    public sealed class XgRetiredTrainingTests
    {
        sealed class Host : IXgHost
        {
            public double money = 100;
            public double Money => money;
            public double Compute => 2;
            public double VramMB => 4096;
            public string Blocker => null;
            public void Earn(double amount) { money += amount; }
            public bool Spend(double amount) { if (amount < 0 || money < amount) return false; money -= amount; return true; }
            public void Train(double seconds) { }
        }

        /// <summary>What a save with nothing owed earns from ticking <paramref name="seconds"/> (other things pay the wallet too).</summary>
        static double Control(double start, double seconds)
        {
            var host = new Host { money = start };
            Load(new XgState()).Tick(seconds, host);
            return host.money;
        }

        static XgSim Load(XgState state, List<string> messages = null)
        {
            var sim = new XgSim(state) { GoldChance = 0 };
            if (messages != null) sim.Message += messages.Add;
            return sim;
        }

        // ───────────── auto-training ─────────────

        [Test] public void NoAutomationNodeIsInTheCatalogAnymore()
        {
            Assert.IsFalse(XgCatalog.Nodes.Any(n => n.id.StartsWith("auto") && n.id.Length <= 6 && char.IsDigit(n.id[n.id.Length - 1])), "auto1…auto5 are gone");
            Assert.IsFalse(Enum.GetNames(typeof(XgNodeKind)).Contains("Auto"), "and so is their kind");
        }

        [Test] public void TrainingNeverStartsByItself()
        {
            var sim = new XgSim { GoldChance = 0 };
            var host = new Host { money = 1e6 };
            sim.S.labels.Add(new XgLabelCount { dataset = sim.Selected.dataset, count = 5000 });
            int rounds = 0;
            sim.EpochStarted += e => rounds++;
            sim.Tick(600, host);
            Assert.AreEqual(0, rounds, "ten minutes pass and no round starts without a press");
        }

        [Test] public void AnOldSaveGetsThePriceOfItsAutomationNodesBackOnce()
        {
            var state = new XgState();
            state.unlocked.Add("auto1"); state.unlocked.Add("auto3"); state.unlocked.Add("lenet");
            var said = new List<string>();
            var sim = Load(state, said);
            Assert.IsFalse(sim.S.unlocked.Contains("auto1") || sim.S.unlocked.Contains("auto3"), "the nodes are taken out");
            Assert.IsTrue(sim.S.unlocked.Contains("lenet"), "everything else stays");
            double owed = Math.Ceiling(250 * XgSim.StoryResearchPrice) + Math.Ceiling(3000 * XgSim.StoryResearchPrice);
            Assert.AreEqual(owed, sim.S.autoRefund, 1e-9);
            var host = new Host { money = 10 };
            sim.Tick(1, host);
            Assert.AreEqual(Control(10, 1) + owed, host.money, 1e-6, "paid into the wallet");
            Assert.AreEqual(0, sim.S.autoRefund, 1e-9);
            Assert.AreEqual(1, said.Count(m => m.Contains("自动训练下线了，退回 ¥" + owed.ToString("0"))), "with the feed line");
            sim.Tick(5, host);
            Assert.AreEqual(Control(10, 6) + owed, host.money, 1e-6, "and only once");
            Assert.AreEqual(1, said.Count(m => m.Contains("自动训练下线了")));
        }

        [Test] public void TheRefundSurvivesASaveBeforeItIsPaid()
        {
            var state = new XgState();
            state.unlocked.Add("auto2");
            var first = Load(state);
            double owed = first.S.autoRefund;
            Assert.Greater(owed, 0);
            var again = Load(UnityEngine.JsonUtility.FromJson<XgState>(UnityEngine.JsonUtility.ToJson(first.S)));
            Assert.AreEqual(owed, again.S.autoRefund, 1e-9, "still owed after a reload");
            var host = new Host { money = 0 };
            again.Tick(1, host);
            Assert.AreEqual(Control(0, 1) + owed, host.money, 1e-6);
            var third = Load(UnityEngine.JsonUtility.FromJson<XgState>(UnityEngine.JsonUtility.ToJson(again.S)));
            Assert.AreEqual(0, third.S.autoRefund, 1e-9, "paid, so nothing is owed again");
        }

        [Test] public void AFreshGameOwesNoAutomationRefund()
        {
            var sim = new XgSim();
            Assert.AreEqual(0, sim.S.autoRefund);
            var host = new Host { money = 7 };
            sim.Tick(3, host);
            Assert.AreEqual(7, host.money, 1e-9);
        }

        [Test] public void TheAutoLabellingDeskStillWorks()
        {
            var sim = new XgSim { GoldChance = 0 };
            Assert.IsNotNull(XgCatalog.Desk("spam"));
            // The AutoModel (training without knobs) is untouched: a run configures itself.
            var host = new Host { money = 1e6 };
            Assert.DoesNotThrow(() => sim.AutoConfigure(sim.S.vision, host));
            Assert.IsNotNull(XgCatalog.Dataset(sim.S.vision.dataset));
        }

        // ───────────── consumables ─────────────

        [Test] public void NoConsumableCategoryOrNodeIsLeftForSale()
        {
            Assert.IsFalse(Enum.GetNames(typeof(XgItemCategory)).Any(n => n.IndexOf("Consum", StringComparison.OrdinalIgnoreCase) >= 0));
            foreach (var id in new[] { "offpeak", "paste", "redbull", "ckpt", "sgdr", "clean" })
            {
                Assert.IsNull(XgCatalog.Node(id), id + " is not a node");
                Assert.IsFalse(XgItems.IsItem(id), id + " is not an item");
            }
            Assert.IsTrue(XgItems.IsItem("cooler") && XgItems.IsItem("cudnn"), "the permanent items stay");
        }

        [Test] public void AnOldSaveGetsTheUnusedConsumablesBackAtTheirPriceOnce()
        {
            var state = new XgState();
            state.consumables.Add(new XgStack { id = "offpeak", count = 6 });   // 30 for 3 → 10 each
            state.consumables.Add(new XgStack { id = "paste", count = 2 });     // 30 for 2 → 15 each
            state.consumables.Add(new XgStack { id = "redbull", count = 12 });  // 18 for 6 → 3 each
            state.consumables.Add(new XgStack { id = "sgdr", count = 1 });      // 80
            state.consumables.Add(new XgStack { id = "clean", count = 2 });     // 50 each
            state.consumables.Add(new XgStack { id = "unknown", count = 5 });
            state.rollbackGuards = 2;                                           // 20 each
            var said = new List<string>();
            var sim = Load(state, said);
            double owed = 6 * 10 + 2 * 15 + 12 * 3 + 80 + 2 * 50 + 2 * 20;
            Assert.AreEqual(owed, sim.S.consumableRefund, 1e-9);
            Assert.AreEqual(0, sim.S.consumables.Count, "the stock is cleared");
            Assert.AreEqual(0, sim.S.rollbackGuards);
            var host = new Host { money = 0 };
            sim.Tick(1, host);
            Assert.AreEqual(Control(0, 1) + owed, host.money, 1e-6);
            Assert.AreEqual(1, said.Count(m => m.Contains("消耗品下线了，退回 ¥" + owed.ToString("0"))));
            sim.Tick(3, host);
            Assert.AreEqual(Control(0, 4) + owed, host.money, 1e-6, "paid once");
            var reloaded = Load(UnityEngine.JsonUtility.FromJson<XgState>(UnityEngine.JsonUtility.ToJson(sim.S)));
            Assert.AreEqual(0, reloaded.S.consumableRefund, 1e-9);
        }

        [Test] public void ASaveWithNoConsumablesOwesNothing()
        {
            var sim = Load(new XgState());
            Assert.AreEqual(0, sim.S.consumableRefund);
            var host = new Host { money = 5 };
            sim.Tick(2, host);
            Assert.AreEqual(Control(5, 2), host.money, 1e-9);
        }

        [Test] public void TheCoolerAndCuDnnStillCutTheTrainingBill()
        {
            var sim = new XgSim { GoldChance = 0 };
            Assert.AreEqual(1, sim.TrainEnergyFactor, 1e-9); Assert.AreEqual(1, sim.TrainWearFactor, 1e-9);
            sim.S.unlocked.Add("cudnn"); sim.S.unlocked.Add("cooler");
            Assert.AreEqual(XgSim.CuDnnEnergyFactor, sim.TrainEnergyFactor, 1e-9);
            Assert.AreEqual(XgSim.CoolerWearFactor, sim.TrainWearFactor, 1e-9);
        }
    }
}
