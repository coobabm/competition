using System;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>The consumables (XgSim.Consumables.cs): packs, the six effects, the scaled electricity bill, saving and loading.</summary>
    public sealed class XgConsumableTests
    {
        /// <summary>A host that also takes the scaled bill, so the factors the lab asks for can be read back.</summary>
        sealed class Host : IXgHost, IXgTrainingBill
        {
            public double money = 1000;
            public double lastSeconds, lastPrice = 1, lastEnergy = 1, lastWear = 1;
            public int bills, cooled;
            public double Compute => 1;
            public double VramMB => 4096;
            public double Money => money;
            public string Blocker => null;
            public bool Spend(double amount) { if (money < amount) return false; money -= amount; return true; }
            public void Earn(double amount) { money += amount; }
            public void Train(double seconds) { lastSeconds = seconds; lastPrice = lastEnergy = lastWear = 1; bills++; }
            public void TrainScaled(double seconds, double priceFactor, double energyFactor, double wearFactor)
            { lastSeconds = seconds; lastPrice = priceFactor; lastEnergy = energyFactor; lastWear = wearFactor; bills++; }
            public void CoolCards() { cooled++; }
        }

        /// <summary>A host that only knows IXgHost.Train, like the older hosts.</summary>
        sealed class PlainHost : IXgHost
        {
            public double money = 1000, energy;
            public double Compute => 1;
            public double VramMB => 4096;
            public double Money => money;
            public string Blocker => null;
            public bool Spend(double amount) { if (money < amount) return false; money -= amount; return true; }
            public void Earn(double amount) { money += amount; }
            public void Train(double seconds) { energy += seconds; }
        }

        static XgSim Ready()
        {
            var sim = new XgSim();
            sim.S.desksOpen.Add("mnist");
            sim.S.labels.Add(new XgLabelCount { dataset = "mnist", count = 20 });
            return sim;
        }

        static void Give(XgSim sim, string id, int n) { sim.S.consumables.Add(new XgStack { id = id, count = n }); }

        [Test]
        public void SixConsumablesWithTheirPacksAndCooldowns()
        {
            var expect = new (string id, double price, int pack, double cooldown)[]
            {
                ("offpeak", 30, 3, 0), ("paste", 30, 2, 20), ("redbull", 18, 6, 15), ("ckpt", 40, 2, 0), ("sgdr", 80, 1, 60), ("clean", 50, 1, 30),
            };
            Assert.That(XgConsumables.All.Length, Is.EqualTo(6));
            for (int i = 0; i < 6; i++)
            {
                var d = XgConsumables.All[i];
                Assert.That(d.id, Is.EqualTo(expect[i].id));
                Assert.That(d.price, Is.EqualTo(expect[i].price)); Assert.That(d.pack, Is.EqualTo(expect[i].pack)); Assert.That(d.cooldown, Is.EqualTo(expect[i].cooldown));
                Assert.That(d.name, Is.Not.Empty); Assert.That(d.nameEn, Is.Not.Empty); Assert.That(d.note, Is.Not.Empty); Assert.That(d.noteEn, Is.Not.Empty);
                Assert.That(d.note, Does.Not.Contain("涌现"));
            }
        }

        [Test]
        public void ABrandNewLabHasNoneAndNothingRunning()
        {
            var sim = new XgSim();
            foreach (var d in XgConsumables.All) { Assert.That(sim.ConsumableCount(d.id), Is.Zero); Assert.That(sim.ConsumableCooldown(d.id), Is.Zero); }
            Assert.That(sim.OffpeakActive || sim.PasteActive || sim.RedBullActive || sim.SgdrActive, Is.False);
            Assert.That(sim.S.rollbackGuards, Is.Zero);
            Assert.That(sim.TrainPriceFactor, Is.EqualTo(1)); Assert.That(sim.TrainEnergyFactor, Is.EqualTo(1)); Assert.That(sim.TrainWearFactor, Is.EqualTo(1));
        }

        [Test]
        public void ABuyPaysTheShelfPriceAndAddsAPack()
        {
            var sim = new XgSim(); var host = new Host { money = 100 };
            Assert.That(sim.BuyConsumable("redbull", host), Is.True);
            Assert.That(host.money, Is.EqualTo(82)); Assert.That(sim.ConsumableCount("redbull"), Is.EqualTo(6));
            Assert.That(sim.BuyConsumable("sgdr", host), Is.True);
            Assert.That(host.money, Is.EqualTo(2));
            Assert.That(sim.BuyConsumable("ckpt", host), Is.False, "too poor");
            Assert.That(sim.ConsumableCount("ckpt"), Is.Zero); Assert.That(host.money, Is.EqualTo(2));
            Assert.That(sim.S.totalSpent, Is.EqualTo(98));
            Assert.That(sim.BuyConsumable("nothing", host), Is.False);
        }

        [Test]
        public void TheShelfHoldsAtMostNinetyNine()
        {
            var sim = new XgSim(); var host = new Host { money = 1e6 };
            int bought = 0;
            while (sim.BuyConsumable("redbull", host) && bought < 100) bought++;
            Assert.That(sim.ConsumableCount("redbull"), Is.LessThanOrEqualTo(XgSim.MaxStock));
            Assert.That(sim.ConsumableCount("redbull"), Is.GreaterThan(XgSim.MaxStock - 6));
        }

        [Test]
        public void AnEmptyOrCoolingOneCannotBeUsed()
        {
            var sim = Ready(); var host = new Host();
            Assert.That(sim.UseConsumable("redbull", host), Is.False);
            Give(sim, "redbull", 2);
            Assert.That(sim.UseConsumable("redbull", host), Is.True);
            Assert.That(sim.ConsumableCount("redbull"), Is.EqualTo(1));
            Assert.That(sim.ConsumableCooldown("redbull"), Is.EqualTo(15));
            Assert.That(sim.UseConsumable("redbull", host), Is.False, "cooling down");
            Assert.That(sim.ConsumableCount("redbull"), Is.EqualTo(1));
            sim.Tick(15, host);
            Assert.That(sim.ConsumableCooldown("redbull"), Is.Zero);
            Assert.That(sim.UseConsumable("redbull", host), Is.True);
        }

        [Test]
        public void OffpeakHalvesTheNextTenRoundsOnly()
        {
            var sim = Ready(); var host = new Host();
            Give(sim, "offpeak", 1);
            sim.TrainEpoch(XgTrack.Vision, host);
            Assert.That(host.lastPrice, Is.EqualTo(1));
            Assert.That(sim.UseConsumable("offpeak", host), Is.True);
            Assert.That(sim.S.offpeakRounds, Is.EqualTo(10)); Assert.That(sim.TrainPriceFactor, Is.EqualTo(.5));
            for (int i = 0; i < 10; i++)
            {
                sim.TrainEpoch(XgTrack.Vision, host);
                Assert.That(host.lastPrice, Is.EqualTo(.5), "round " + (i + 1));
            }
            Assert.That(sim.S.offpeakRounds, Is.Zero);
            sim.TrainEpoch(XgTrack.Vision, host);
            Assert.That(host.lastPrice, Is.EqualTo(1));
        }

        [Test]
        public void OffpeakStacksAndIsNotUsedUpByOtherGpuWork()
        {
            var sim = Ready(); var host = new Host();
            Give(sim, "offpeak", 2);
            sim.UseConsumable("offpeak", host); sim.UseConsumable("offpeak", host);
            Assert.That(sim.S.offpeakRounds, Is.EqualTo(20));
            sim.TrainEpoch(XgTrack.Vision, host);
            Assert.That(sim.S.offpeakRounds, Is.EqualTo(19));
        }

        [Test]
        public void CuDnnTakesThirtyPercentOffTheEnergy()
        {
            var sim = Ready(); var host = new Host();
            sim.TrainEpoch(XgTrack.Vision, host);
            Assert.That(host.lastEnergy, Is.EqualTo(1));
            sim.S.unlocked.Add("cudnn");
            sim.TrainEpoch(XgTrack.Vision, host);
            Assert.That(host.lastEnergy, Is.EqualTo(.7).Within(1e-9));
            Assert.That(host.lastSeconds, Is.EqualTo(XgSim.EpochSeconds).Within(1e-9), "still the same GPU time");
        }

        [Test]
        public void AHostWithoutTheScaledBillStillBillsTheRound()
        {
            var sim = Ready(); var host = new PlainHost();
            Give(sim, "offpeak", 1);
            sim.UseConsumable("offpeak", host);
            sim.TrainEpoch(XgTrack.Vision, host);
            Assert.That(host.energy, Is.EqualTo(XgSim.EpochSeconds).Within(1e-9));
            Assert.That(sim.S.offpeakRounds, Is.EqualTo(9));
        }

        [Test]
        public void PasteStopsBurnOutForOneHundredTwentySecondsAndCoolsTheCards()
        {
            var sim = Ready(); var host = new Host();
            Give(sim, "paste", 2);
            sim.TrainEpoch(XgTrack.Vision, host);
            Assert.That(host.lastWear, Is.EqualTo(1));
            Assert.That(sim.UseConsumable("paste", host), Is.True);
            Assert.That(host.cooled, Is.EqualTo(1)); Assert.That(sim.S.pasteSeconds, Is.EqualTo(120));
            sim.TrainEpoch(XgTrack.Vision, host);
            Assert.That(host.lastWear, Is.Zero);
            sim.Tick(119, host);
            Assert.That(sim.PasteActive, Is.True);
            sim.Tick(1.5, host);
            Assert.That(sim.PasteActive, Is.False);
            sim.TrainEpoch(XgTrack.Vision, host);
            Assert.That(host.lastWear, Is.EqualTo(1));
        }

        [Test]
        public void TheCoolerHalvesTheBurnOutChance()
        {
            var sim = Ready(); var host = new Host();
            sim.S.unlocked.Add("cooler");
            sim.TrainEpoch(XgTrack.Vision, host);
            Assert.That(host.lastWear, Is.EqualTo(.5));
            Give(sim, "paste", 1);
            sim.UseConsumable("paste", host);
            sim.TrainEpoch(XgTrack.Vision, host);
            Assert.That(host.lastWear, Is.Zero, "fresh paste beats the cooler");
        }

        [Test]
        public void TheCoolerIsAnItemOnThePowerShelf()
        {
            var node = XgCatalog.Node("cooler");
            Assert.That(node, Is.Not.Null);
            Assert.That(node.kind, Is.EqualTo(XgNodeKind.Research));
            Assert.That(node.cost, Is.InRange(150, 260));
            var def = XgItems.All().Find(d => d.id == "cooler");
            Assert.That(def, Is.Not.Null); Assert.That(def.category, Is.EqualTo(XgItemCategory.Power));
            Assert.That(XgItems.Order, Does.Contain(XgItemCategory.Consumable));
            Assert.That(XgItems.CategoryName(XgItemCategory.Consumable, false), Is.EqualTo("消耗品"));
            Assert.That(XgItems.CategoryName(XgItemCategory.Consumable, true), Is.EqualTo("Consumables"));
        }

        [Test]
        public void RedBullHoldsTheComboAndRaisesItsBonusByHalf()
        {
            var sim = Ready(); var host = new Host();
            sim.S.combo = 10;
            Assert.That(sim.ComboMultiplier, Is.EqualTo(1.5).Within(1e-9));
            Give(sim, "redbull", 1);
            Assert.That(sim.UseConsumable("redbull", host), Is.True);
            Assert.That(sim.ComboMultiplier, Is.EqualTo(1.75).Within(1e-9));
            sim.S.combo = 40;
            Assert.That(sim.ComboMultiplier, Is.EqualTo(2.5).Within(1e-9), "the cap grows with the bonus");
            sim.S.combo = 20; sim.S.comboTimer = 1.5;
            sim.Tick(30, host);
            Assert.That(sim.S.combo, Is.EqualTo(20), "no decay for the 60 seconds");
            sim.Tick(35, host);
            Assert.That(sim.S.combo, Is.Zero, "once it wears off the combo fades as usual");
            Assert.That(sim.ComboMultiplier, Is.EqualTo(1));
        }

        [Test]
        public void ACheckpointRollbackUndoesTheNextDropAndNothingElse()
        {
            // Two labs that are identical until the first drop; the one with a rollback must skip exactly that drop.
            var plain = Ready(); var guarded = Ready(); var h1 = new Host(); var h2 = new Host();
            Give(guarded, "ckpt", 1);
            Assert.That(guarded.UseConsumable("ckpt", h2), Is.True);
            Assert.That(guarded.S.rollbackGuards, Is.EqualTo(1));
            bool seen = false;
            for (int i = 0; i < 600 && !seen; i++)
            {
                double before = guarded.S.vision.valAcc;
                var a = plain.TrainEpoch(XgTrack.Vision, h1);
                var b = guarded.TrainEpoch(XgTrack.Vision, h2);
                Assert.That(a, Is.Not.Null); Assert.That(b, Is.Not.Null);
                if (!a.dropped) { Assert.That(b.rolledBack, Is.False); Assert.That(b.dropped, Is.False); Assert.That(guarded.S.vision.valAcc, Is.EqualTo(plain.S.vision.valAcc).Within(1e-12)); continue; }
                seen = true;
                Assert.That(b.rolledBack, Is.True); Assert.That(b.dropped, Is.False);
                Assert.That(guarded.S.vision.valAcc, Is.EqualTo(before).Within(1e-12), "back to the last round's score");
                Assert.That(guarded.S.rollbackGuards, Is.Zero);
                Assert.That(guarded.S.drops, Is.Zero);
            }
            Assert.That(seen, Is.True, "a drop happened within 600 rounds");
        }

        [Test]
        public void TwoRollbacksCoverTwoDrops()
        {
            var sim = Ready(); var host = new Host();
            Give(sim, "ckpt", 2);
            sim.UseConsumable("ckpt", host); sim.UseConsumable("ckpt", host);
            int rolled = 0, dropped = 0;
            for (int i = 0; i < 1500 && rolled < 2; i++)
            {
                var e = sim.TrainEpoch(XgTrack.Vision, host);
                if (e.rolledBack) rolled++;
                if (e.dropped && sim.S.rollbackGuards > 0) dropped++;
            }
            Assert.That(rolled, Is.EqualTo(2)); Assert.That(dropped, Is.Zero, "no drop got through while a rollback was waiting");
        }

        [Test]
        public void SgdrRaisesTheCeilingByEightPercentForFiveRounds()
        {
            var sim = Ready(); var host = new Host();
            sim.TrainEpoch(XgTrack.Vision, host);
            var run = sim.S.vision;
            double top = 1 - XgCatalog.Dataset(run.dataset).floorError;
            double before = sim.CeilingAcc(run);
            Assume.That(before * 1.08, Is.LessThan(top), "the test dataset leaves room above the ceiling");
            Give(sim, "sgdr", 1);
            Assert.That(sim.UseConsumable("sgdr", host), Is.True);
            Assert.That(sim.S.sgdrRounds, Is.EqualTo(5));
            Assert.That(sim.CeilingAcc(run), Is.EqualTo(before * 1.08).Within(1e-9));
            Assert.That(run.ceiling, Is.EqualTo(before * 1.08).Within(1e-9), "the run shows it at once");
            for (int i = 0; i < 4; i++) sim.TrainEpoch(XgTrack.Vision, host);
            Assert.That(sim.S.sgdrRounds, Is.EqualTo(1));
            sim.TrainEpoch(XgTrack.Vision, host);
            Assert.That(sim.S.sgdrRounds, Is.Zero);
            Assert.That(sim.CeilingAcc(run), Is.EqualTo(before).Within(1e-9));
        }

        [Test]
        public void SgdrNeverLiftsTheCeilingPastTheDatasetsFloor()
        {
            var sim = Ready(); var host = new Host();
            var run = sim.S.vision;
            double top = 1 - XgCatalog.Dataset(run.dataset).floorError;
            sim.S.sgdrRounds = 5;
            Assert.That(sim.CeilingAcc(run), Is.LessThanOrEqualTo(top + 1e-12));
        }

        [Test]
        public void TheCleaningScriptRemovesFiftyNoisyLabelsOfTheCurrentDataset()
        {
            var sim = Ready(); var host = new Host();
            sim.S.noise.Add(new XgLabelCount { dataset = "mnist", count = 80 });
            Give(sim, "clean", 2);
            Assert.That(sim.UseConsumable("clean", host, "mnist"), Is.True);
            Assert.That(sim.Noise("mnist"), Is.EqualTo(30).Within(1e-9));
            Assert.That(sim.UseConsumable("clean", host, "mnist"), Is.False, "cooling down");
            sim.Tick(30, host);
            Assert.That(sim.UseConsumable("clean", host, "mnist"), Is.True);
            Assert.That(sim.Noise("mnist"), Is.Zero.Within(1e-9));
            Assert.That(sim.ConsumableCount("clean"), Is.Zero);
        }

        [Test]
        public void TheCleaningScriptIsNotWastedOnACleanDataset()
        {
            var sim = Ready(); var host = new Host();
            Give(sim, "clean", 1);
            Assert.That(sim.ConsumableBlocker("clean", "mnist"), Is.Not.Null);
            Assert.That(sim.UseConsumable("clean", host, "mnist"), Is.False);
            Assert.That(sim.ConsumableCount("clean"), Is.EqualTo(1));
        }

        static string ToJson(XgState state)
        {
#if UNITY_5_3_OR_NEWER
            return UnityEngine.JsonUtility.ToJson(state);
#else
            return System.Text.Json.JsonSerializer.Serialize(state, new System.Text.Json.JsonSerializerOptions { IncludeFields = true });
#endif
        }

        static XgState FromJson(string json)
        {
#if UNITY_5_3_OR_NEWER
            return UnityEngine.JsonUtility.FromJson<XgState>(json);
#else
            return System.Text.Json.JsonSerializer.Deserialize<XgState>(json, new System.Text.Json.JsonSerializerOptions { IncludeFields = true });
#endif
        }

        [Test]
        public void StockCooldownsAndRunningEffectsSurviveASaveAndLoad()
        {
            var sim = Ready(); var host = new Host();
            Give(sim, "redbull", 3); Give(sim, "offpeak", 2); Give(sim, "ckpt", 1); Give(sim, "sgdr", 1);
            sim.UseConsumable("redbull", host); sim.UseConsumable("offpeak", host); sim.UseConsumable("ckpt", host); sim.UseConsumable("sgdr", host);
            sim.Tick(5, host);
            var loaded = new XgSim(FromJson(ToJson(sim.S)));
            Assert.That(loaded.ConsumableCount("redbull"), Is.EqualTo(2));
            Assert.That(loaded.ConsumableCount("offpeak"), Is.EqualTo(1));
            Assert.That(loaded.ConsumableCooldown("redbull"), Is.EqualTo(10).Within(1e-4));
            Assert.That(loaded.ConsumableCooldown("sgdr"), Is.EqualTo(55).Within(1e-4));
            Assert.That(loaded.S.redbullSeconds, Is.EqualTo(55).Within(1e-4));
            Assert.That(loaded.S.offpeakRounds, Is.EqualTo(10));
            Assert.That(loaded.S.rollbackGuards, Is.EqualTo(1));
            Assert.That(loaded.S.sgdrRounds, Is.EqualTo(5));
        }

        [Test]
        public void AnOldSaveWithoutTheFieldsLoadsWithNothing()
        {
            // A real save with the new fields cut out of its text, the way a save from before the consumables looks.
            var used = Ready();
            string json = ToJson(used.S);
            json = System.Text.RegularExpressions.Regex.Replace(json, "\"consumables\"\\s*:\\s*\\[.*?\\]\\s*,?", "");
            json = System.Text.RegularExpressions.Regex.Replace(json, ",\\s*\"(pasteSeconds|redbullSeconds|offpeakRounds|rollbackGuards|sgdrRounds)\"\\s*:\\s*[-0-9.eE+]+", "");
            Assert.That(json, Does.Not.Contain("pasteSeconds"));
            Assert.That(json, Does.Not.Contain("rollbackGuards"));
            Assert.That(json, Does.Not.Contain("consumables"));
            var sim = new XgSim(FromJson(json));
            foreach (var d in XgConsumables.All) Assert.That(sim.ConsumableCount(d.id), Is.Zero);
            Assert.That(sim.OffpeakActive || sim.PasteActive || sim.RedBullActive || sim.SgdrActive || sim.S.rollbackGuards > 0, Is.False);
            Assert.That(sim.BuyConsumable("redbull", new Host()), Is.True, "and it can start buying");
        }

        [Test]
        public void ADamagedSaveIsCleanedOnLoad()
        {
            var state = new XgState();
            state.consumables.Add(new XgStack { id = "redbull", count = 500, cooldown = double.NaN });
            state.consumables.Add(new XgStack { id = "gone", count = 3 });
            state.consumables.Add(null);
            state.consumables.Add(new XgStack { id = "redbull", count = 2 });
            state.pasteSeconds = double.PositiveInfinity; state.redbullSeconds = -4; state.offpeakRounds = -2; state.sgdrRounds = -1; state.rollbackGuards = -3;
            var sim = new XgSim(state);
            Assert.That(sim.S.consumables.Count, Is.EqualTo(1));
            Assert.That(sim.ConsumableCount("redbull"), Is.EqualTo(XgSim.MaxStock));
            Assert.That(sim.ConsumableCooldown("redbull"), Is.Zero);
            Assert.That(sim.S.pasteSeconds, Is.Zero); Assert.That(sim.S.redbullSeconds, Is.Zero);
            Assert.That(sim.S.offpeakRounds, Is.Zero); Assert.That(sim.S.sgdrRounds, Is.Zero); Assert.That(sim.S.rollbackGuards, Is.Zero);
        }

        [Test]
        public void NoConsumableIsNeededToTrain()
        {
            var sim = Ready(); var host = new Host();
            for (int i = 0; i < 30; i++) Assert.That(sim.TrainEpoch(XgTrack.Vision, host), Is.Not.Null);
            Assert.That(sim.S.epochs, Is.EqualTo(30));
            Assert.That(host.lastPrice, Is.EqualTo(1)); Assert.That(host.lastEnergy, Is.EqualTo(1)); Assert.That(host.lastWear, Is.EqualTo(1));
        }
    }
}
