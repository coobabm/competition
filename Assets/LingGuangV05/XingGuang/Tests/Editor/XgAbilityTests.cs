using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>参数量与数据量主线: two bars, six abilities, item discounts, and old saves moved over without loss.</summary>
    public sealed class XgAbilityTests
    {
        sealed class Host : IXgHost
        {
            public double money = 1000000, compute = 2;
            public double Compute => compute;
            public double VramMB => 100000;
            public double Money => money;
            public bool Spend(double amount) { if (money < amount) return false; money -= amount; return true; }
            public void Earn(double amount) { money += amount; }
            public void Train(double seconds) { }
            public string Blocker => null;
        }

        static void AddLabels(XgSim sim, string dataset, double count)
        {
            foreach (var l in sim.S.labels) if (l.dataset == dataset) { l.count += count; return; }
            sim.S.labels.Add(new XgLabelCount { dataset = dataset, count = count });
        }

        [Test]
        public void AFreshGameHasOnlyTheFirstAbility()
        {
            var sim = new XgSim();
            Assert.That(sim.HasAbility(1), Is.True);
            Assert.That(sim.HasAbility(2), Is.False);
            Assert.That(sim.AbilitiesCount, Is.EqualTo(1));
            Assert.That(sim.NextAbility, Is.EqualTo(2));
            Assert.That(sim.S.stage, Is.EqualTo(1));
            Assert.That(sim.TrainedParamsK, Is.EqualTo(0));
        }

        [Test]
        public void TheThresholdsRiseWithEveryAbility()
        {
            for (int i = 3; i <= XgSim.AbilityCount; i++)
            {
                Assert.That(XgSim.AbilityParamsK[i], Is.GreaterThan(XgSim.AbilityParamsK[i - 1]), "parameters " + i);
                Assert.That(XgSim.AbilitySamples[i], Is.GreaterThan(XgSim.AbilitySamples[i - 1]), "samples " + i);
            }
            for (int i = 1; i <= XgSim.AbilityCount; i++)
            {
                Assert.That(XgSim.AbilityName(i, false), Is.Not.Empty);
                Assert.That(XgSim.AbilityName(i, true), Is.Not.Empty);
            }
        }

        [Test]
        public void AnAbilityEmergesOnlyWhenBothBarsAreFull()
        {
            var sim = new XgSim { GoldChance = 0 };
            var host = new Host();
            var emerged = new List<int>();
            sim.AbilityEmerged += a => emerged.Add(a);
            sim.S.stageEpochs = 1; // a round has been trained in this stage

            sim.S.trainedParamsK = sim.ParamsThreshold(2);
            sim.Tick(1, host);
            Assert.That(sim.StageReady, Is.False, "parameters alone are not enough");
            Assert.That(sim.ConfirmNextStage(), Is.False);
            Assert.That(sim.HasAbility(2), Is.False);

            sim.S.trainedParamsK = 0;
            AddLabels(sim, "logic", sim.SamplesThreshold(2) + 10);
            sim.Tick(1, host);
            Assert.That(sim.StageReady, Is.False, "data alone is not enough");
            Assert.That(sim.HasAbility(2), Is.False);

            sim.S.trainedParamsK = sim.ParamsThreshold(2);
            sim.Tick(1, host);
            Assert.That(sim.HasAbility(2), Is.False, "full bars do not start the next chapter by themselves");
            Assert.That(sim.StageReady, Is.True);
            sim.S.stageEpochs = 0;
            Assert.That(sim.StageReady, Is.False, "and a stage nothing was trained in cannot be confirmed");
            sim.S.stageEpochs = 1;
            Assert.That(sim.ConfirmNextStage(), Is.True);
            Assert.That(sim.HasAbility(2), Is.True);
            Assert.That(sim.S.stage, Is.EqualTo(2), "the stage is the number of abilities");
            Assert.That(emerged, Is.EqualTo(new[] { 2 }));

            sim.Tick(1, host);
            Assert.That(emerged, Is.EqualTo(new[] { 2 }), "an ability emerges once");
            Assert.That(sim.HasAbility(3), Is.False);
        }

        [Test]
        public void AbilitiesEmergeInOrderOneMomentAtATime()
        {
            var sim = new XgSim { GoldChance = 0 };
            var host = new Host();
            var emerged = new List<int>();
            sim.AbilityEmerged += a => emerged.Add(a);
            sim.S.trainedParamsK = XgSim.AbilityParamsK[4];
            AddLabels(sim, "logic", XgSim.AbilitySamples[4]);
            sim.S.stageEpochs = 1;
            for (int i = 0; i < 5; i++) sim.Tick(1, host);
            Assert.That(emerged, Is.Empty, "bars that are full for several abilities still change nothing until confirmed");
            for (int ability = 2; ability <= 4; ability++)
            {
                sim.S.stageEpochs = 0;
                Assert.That(sim.StageReady, Is.False, "each new stage needs a trained round before it can be confirmed");
                sim.S.stageEpochs = 1;
                Assert.That(sim.ConfirmNextStage(), Is.True);
                Assert.That(emerged, Is.EqualTo(Enumerable.Range(2, ability - 1).ToArray()), "never out of order");
                Assert.That(sim.S.stage, Is.EqualTo(ability));
            }
            Assert.That(sim.StageReady, Is.False, "the fifth ability's bars are not full");
        }

        [Test]
        public void ItemsLowerTheirAbilitysThresholds()
        {
            var sim = new XgSim();
            double baseParams = sim.ParamsThreshold(2);
            Assert.That(baseParams, Is.EqualTo(XgSim.AbilityParamsK[2]));
            sim.S.unlocked.Add("mlp");
            Assert.That(sim.ParamsThreshold(2), Is.LessThan(baseParams), "MLP lowers ability 2's parameters");
            Assert.That(sim.SamplesThreshold(2), Is.EqualTo(XgSim.AbilitySamples[2]), "but not its data");

            double six = sim.ParamsThreshold(6), sixData = sim.SamplesThreshold(6);
            sim.S.unlocked.Add("transformer");
            Assert.That(sim.ParamsThreshold(6), Is.LessThan(six));
            Assert.That(sim.SamplesThreshold(6), Is.LessThan(sixData));

            foreach (var d in XgSim.AbilityDiscounts)
            {
                Assert.That(d.paramsFactor, Is.GreaterThan(0).And.LessThanOrEqualTo(1));
                Assert.That(d.samplesFactor, Is.GreaterThan(0).And.LessThanOrEqualTo(1));
                foreach (var id in d.item.Length > 0 ? new[] { d.item } : d.itemsAny) Assert.That(XgCatalog.Node(id), Is.Not.Null, id);
            }
        }

        [Test]
        public void OnlyAModelGradedCOrBetterCountsAndWideningAloneBuysNothing()
        {
            var sim = new XgSim { GoldChance = 0 };
            var host = new Host();
            AddLabels(sim, "spam", 200);
            var s = XgTrack.Sequence;
            sim.SetDataset(s, "spam");
            var first = sim.Assess(s, host);
            Assert.That(first, Is.Not.Null);
            Assert.That(sim.TrainedParamsK, Is.EqualTo(0), "an untrained model does not count");

            XgAssessment last = first;
            for (int i = 0; i < 200 && (last == null || last.grade < XgSim.TrainedGrade); i++)
            {
                var e = sim.TrainEpoch(s, host);
                last = e != null ? e.assessment : null;
            }
            Assert.That(last, Is.Not.Null);
            Assert.That(last.grade, Is.GreaterThanOrEqualTo(XgSim.TrainedGrade), "spam reaches C");
            double trained = sim.TrainedParamsK;
            Assert.That(trained, Is.EqualTo(XgSim.TrainedShapeParamsK(sim.S.sequence)).Within(1e-9));
            Assert.That(trained, Is.GreaterThan(0));

            // A wider model the lab switches to by itself does not count before it has trained, even when assessed.
            sim.S.unlocked.Add("s.w1"); sim.S.unlocked.Add("s.w2");
            Assert.That(sim.AutoConfigure(sim.S.sequence, host), Is.True);
            Assert.That(XgSim.ParamsK(sim.S.sequence), Is.GreaterThan(trained));
            sim.Assess(s, host);
            Assert.That(sim.TrainedParamsK, Is.EqualTo(trained).Within(1e-9));
        }

        [Test]
        public void ANewStageSetFromOutsideCountsItsAbilities()
        {
            var sim = new XgSim();
            sim.S.stage = 4;
            sim.Tick(1, new Host());
            Assert.That(sim.S.stage, Is.EqualTo(4));
            for (int i = 1; i <= 4; i++) Assert.That(sim.HasAbility(i), Is.True, "ability " + i);
            Assert.That(sim.HasAbility(5), Is.False);
        }

        [Test]
        public void AnOldStageFiveSaveKeepsEverythingItReached()
        {
            var state = new XgState { progressionVersion = 2, stage = 5, stageVision = 5, stageSequence = 5 };
            state.unlocked.AddRange(new[] { "perceptron", "bt.hidden", "mlp", "bt.vision", "bt.sequence", "bt.gate", "bt.attention", "bt.spatial" });
            state.walls.AddRange(new[] { "combo", "structure", "length", "translation" });
            state.models.Add(new XgModelEntry { id = 1, track = 1, dataset = "longtext", arch = "lstm", depth = 3, width = 3, score = 700 });
            state.models.Add(new XgModelEntry { id = 2, track = 0, dataset = "mnist", arch = "vgg", depth = 16, width = 5, score = 100 });
            var sim = new XgSim(state) { GoldChance = 0 };

            Assert.That(sim.S.stage, Is.EqualTo(5), "never moved back");
            for (int i = 1; i <= 5; i++) Assert.That(sim.HasAbility(i), Is.True, "ability " + i);
            Assert.That(sim.HasAbility(6), Is.False);
            foreach (var id in new[] { "mlp", "lenet", "rnn", "lstm", "seq2seq", "caption" }) Assert.That(sim.Has(id), Is.True, "item " + id);
            Assert.That(sim.TrainedParamsK, Is.EqualTo(XgSim.ParamsK(new XgRun { arch = "lstm", depth = 3, width = 3 })).Within(1e-9),
                "graded checkpoints count; the D-graded VGG does not");
            foreach (var beat in new[] { "wall_combo", "wall_parallel", "emerge_2", "emerge_5", "specialty_motivation", "s2_moved" })
                Assert.That(sim.S.migratedBeats, Does.Contain(beat), beat);
            Assert.That(sim.S.migratedBeats, Does.Not.Contain("emerge_6"));
            Assert.That(sim.S.progressionVersion, Is.EqualTo(XgSim.ProgressionSchema));

            sim.Tick(5, new Host());
            Assert.That(sim.S.stage, Is.GreaterThanOrEqualTo(5), "a tick never takes a stage away");

            // Loading the migrated save again changes nothing.
            var again = new XgSim(sim.S);
            Assert.That(again.S.stage, Is.EqualTo(sim.S.stage));
            Assert.That(again.AbilitiesCount, Is.EqualTo(sim.AbilitiesCount));
        }

        [Test]
        public void AStageTwoSaveThatNeverSawItsFirstChoiceStillGetsIt()
        {
            var state = new XgState { progressionVersion = 2, stage = 2, stageVision = 2, stageSequence = 2 };
            state.unlocked.AddRange(new[] { "perceptron", "bt.hidden", "mlp" });
            state.walls.Add("combo");
            var sim = new XgSim(state);
            Assert.That(sim.HasAbility(2), Is.True);
            Assert.That(sim.S.migratedBeats, Does.Contain("emerge_2"));
            Assert.That(sim.S.migratedBeats, Does.Not.Contain("specialty_motivation"));
        }
    }
}
