using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>Design v1.1 progression: walls are datasets, golden settings pass them, secrets reveal them.</summary>
    public sealed class XgProgressionTests
    {
        sealed class Host : IXgHost
        {
            public double money = 1000000, compute = 2, trained;
            public string blocker;
            public double Compute => compute;
            public double VramMB => 100000;
            public double Money => money;
            public bool Spend(double amount) { if (money < amount) return false; money -= amount; return true; }
            public void Earn(double amount) { money += amount; }
            public void Train(double seconds) { trained += seconds; }
            public string Blocker => blocker;
        }

        /// <summary>A save at the start of <paramref name="stage"/> owning every node up to it (secrets excluded).</summary>
        static XgSim AtStage(int stage, int salt = 0)
        {
            var state = new XgState { progressionVersion = XgSim.ProgressionSchemaWalls, stage = stage, stageVision = stage, stageSequence = stage, dataSalt = salt };
            var sim = new XgSim(state) { GoldChance = 0 };
            foreach (var n in XgCatalog.Nodes)
                if (n.stage <= stage && n.tree != "label" && n.kind != XgNodeKind.Secret && n.kind != XgNodeKind.Project && n.kind != XgNodeKind.Label && n.id != "transformer" && !sim.Has(n.id))
                    sim.S.unlocked.Add(n.id);
            foreach (var d in new[] { "logic", "spam", "mnist", "danmu" }) if (!sim.S.desksOpen.Contains(d)) sim.S.desksOpen.Add(d);
            sim.S.labels.Add(new XgLabelCount { dataset = "spam", count = 300 });
            sim.S.labels.Add(new XgLabelCount { dataset = "mnist", count = 300 });
            return new XgSim(sim.S) { GoldChance = 0, PracticeStrict = false };
        }

        /// <summary>What a player who read the secret would set (the test plays the part of the player).</summary>
        static void Golden(XgSim sim, int stage, IXgHost host)
        {
            var v = XgTrack.Vision; var s = XgTrack.Sequence;
            switch (stage)
            {
                case 1: sim.SetDataset(s, "xor"); sim.SetArch(s, "mlp"); sim.SetDepth(s, 2, host); sim.SetActivation(s, 1); sim.SetLr(s, 2); break;
                case 2:
                    sim.SetDataset(v, "mnist"); sim.SetArch(v, "lenet"); sim.SetDepth(v, 3, host); sim.SetWidth(v, 2, host); sim.SetLr(v, 2);
                    sim.SetDataset(s, "danmu"); sim.SetArch(s, "rnn"); sim.SetDepth(s, 3, host); sim.SetWidth(s, 2, host); sim.SetLr(s, 2); break;
                case 3: sim.SetDataset(s, "longtext"); sim.SetArch(s, "lstm"); sim.SetDepth(s, 3, host); sim.SetWidth(s, 3, host); sim.SetClip(s, true); sim.SetLr(s, 3); break;
                case 4: sim.SetDataset(s, "translate"); sim.SetArch(s, "seq2seq"); sim.SetDepth(s, 2, host); sim.SetWidth(s, 4, host); sim.SetClip(s, true); sim.SetLr(s, 2); break;
                case 5: sim.SetDataset(s, "parallel"); sim.SetArch(s, "attention"); sim.SetAttentionOnly(s, true); sim.SetPosition(s, true); sim.SetDepth(s, 3, host); sim.SetWidth(s, 5, host); sim.SetClip(s, true); sim.SetLr(s, 2); break;
            }
        }

        static void Train(XgSim sim, IXgHost host, int epochs, int stage = 0)
        {
            for (int i = 0; i < epochs && (stage == 0 || sim.S.stage == stage); i++)
            {
                if (sim.TrainingUnlocked(XgTrack.Sequence)) sim.TrainEpoch(XgTrack.Sequence, host);
                if (sim.TrainingUnlocked(XgTrack.Vision) && (stage == 0 || sim.S.stage == stage)) sim.TrainEpoch(XgTrack.Vision, host);
            }
        }

        [Test]
        public void FreshLabStartsAsAPerceptronOnLogicAndSpamWithTheSeedPlanted()
        {
            var sim = new XgSim();
            Assert.That(sim.S.vision.arch, Is.EqualTo("perceptron")); Assert.That(sim.S.sequence.arch, Is.EqualTo("perceptron"));
            Assert.That(sim.DeskOpen("logic") && sim.DeskOpen("spam"), Is.True);
            Assert.That(sim.DeskOpen("mnist"), Is.False, "digits wait for stage two");
            Assert.That(sim.Board.Find("logic", XgSim.SeedKey), Is.Not.Null);
            Assert.That(sim.SeedSaysYes, Is.False, "the SI planted a no");
            Assert.IsFalse(sim.NodeVisible(XgCatalog.Node("secret.1")), "secrets appear with their wall");
            Assert.IsFalse(sim.NodeVisible(XgCatalog.Node("label.brain")));
        }

        [Test]
        public void CatalogHasWallsSecretsAndWallDatasets()
        {
            foreach (var id in new[] { "perceptron", "mlp", "caption", "transformer" }) Assert.That(XgCatalog.Arch(id), Is.Not.Null, id);
            foreach (var id in new[] { "xor", "parallel" }) Assert.That(XgCatalog.Dataset(id), Is.Not.Null, id);
            foreach (var wall in XgSim.Walls)
            {
                if (wall.secret.Length > 0) Assert.That(XgCatalog.Node(wall.secret).kind, Is.EqualTo(XgNodeKind.Secret), wall.id);
                foreach (var group in wall.needs) foreach (var id in group) Assert.That(XgCatalog.Node(id), Is.Not.Null, wall.id + " needs " + id);
                foreach (var check in wall.checks) if (check.dataset != "*vision") Assert.That(XgCatalog.Dataset(check.dataset), Is.Not.Null, wall.id);
            }
            Assert.That(XgSim.WallFor(5).secret, Is.Empty, "stage five's secret is not sold");
        }

        [Test]
        public void XorWallAppearsAfterAGradeAndBringsTheWinter()
        {
            var sim = new XgSim { GoldChance = 0, PracticeStrict = false }; var host = new Host();
            sim.S.unlocked.Add("bias");
            for (int i = 0; i < 40; i++) { var c = sim.Card("spam"); sim.Answer("spam", c.truth, host); }
            Assert.IsFalse(sim.WallSeen("combo"));
            for (int i = 0; i < 40 && !sim.WallSeen("combo"); i++) sim.TrainEpoch(XgTrack.Sequence, host);
            Assert.IsTrue(sim.WallSeen("combo"));
            Assert.IsTrue(sim.Winter); Assert.IsTrue(sim.DatasetAvailable("xor"));
            Assert.IsTrue(sim.NodeVisible(XgCatalog.Node("secret.1")));
            var contract = new XgContract { id = "t", dataset = "spam", threshold = .1, income = 10 };
            sim.S.contracts.Add("t"); sim.S.best.Add(new XgBest { dataset = "spam", acc = .9 });
            Assert.That(sim.ContractIncome(contract), Is.LessThan(10 * (1 + (.9 - .1) / .9) * .51), "AI winter halves contracts");
        }

        [Test]
        public void OnlyTheGoldenSettingPassesXorAndWorkingItOutPaysTheInsightBonus()
        {
            var sim = AtStage(1); var host = new Host(); var stages = new List<int>(); var milestones = new List<string>();
            sim.StageAdvanced += stages.Add; sim.BreakthroughDone += milestones.Add;
            Train(sim, host, 50); Assert.IsTrue(sim.WallSeen("combo"));
            sim.SetDataset(XgTrack.Sequence, "xor"); sim.SetArch(XgTrack.Sequence, "perceptron");
            Train(sim, host, 30); Assert.That(sim.S.stage, Is.EqualTo(1), "a perceptron never passes XOR");
            sim.SetArch(XgTrack.Sequence, "mlp"); sim.SetDepth(XgTrack.Sequence, 2, host); sim.SetActivation(XgTrack.Sequence, 0);
            Train(sim, host, 30); Assert.That(sim.S.stage, Is.EqualTo(1), "a hidden layer with a step activation stays stuck");
            double before = host.money;
            Golden(sim, 1, host); Train(sim, host, 60, 1);
            Assert.That(sim.S.stage, Is.EqualTo(2)); Assert.That(stages, Is.EqualTo(new[] { 1 }));
            Assert.That(milestones, Does.Contain("bt.hidden"));
            Assert.That(sim.S.insights, Does.Contain("combo"));
            Assert.That(host.money - before, Is.GreaterThanOrEqualTo(200 * XgSim.SelfInsightBonus - 1e-6));
            Assert.That(sim.HasEmerged(1), Is.True, "the emergence is guaranteed when the stage ends");
            Assert.IsTrue(sim.DeskOpen("mnist") && sim.DeskOpen("danmu"));
        }

        [Test]
        public void BoughtSecretsRevealTheSettingAndForfeitTheBonus()
        {
            var sim = AtStage(1); var host = new Host();
            Train(sim, host, 50);
            Assert.IsTrue(sim.BuyNode("secret.1", host));
            Golden(sim, 1, host); double before = host.money; Train(sim, host, 60, 1);
            Assert.That(sim.S.stage, Is.EqualTo(2));
            Assert.That(sim.S.insights, Does.Not.Contain("combo"));
            Assert.That(host.money - before, Is.LessThan(200), "no insight bonus after buying the secret");
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void EachStagesWallPassesWithItsGoldenSetting(int stage)
        {
            var sim = AtStage(stage); var host = new Host();
            Train(sim, host, XgSim.WallPracticeEpochs + 2);
            var wall = XgSim.WallFor(stage);
            Assert.IsTrue(sim.WallSeen(wall.id), wall.id);
            Assert.That(sim.MissingNeeds(wall), Is.Empty);
            Golden(sim, stage, host); Train(sim, host, 400, stage);
            Assert.That(sim.S.stage, Is.EqualTo(stage + 1), wall.id + " after golden training");
        }

        /// <summary>
        /// Regression for walls that passed in one history and stalled for hours in another: a stage-3/4 save with a
        /// data seed, optionally after the sequence board was filled by other tasks, the vision track training
        /// alongside. Returns epochs to pass, or -1.
        /// </summary>
        static int EpochsToPass(int stage, int salt, bool prefilled, Action<XgSim, IXgHost> setup, int budget = 160)
        {
            var sim = AtStage(stage, salt); var host = new Host();
            sim.S.labels.Add(new XgLabelCount { dataset = "poems", count = 400 });
            if (prefilled)
                foreach (var other in new[] { "poems", "spam", "danmu" })
                {
                    sim.SetDataset(XgTrack.Sequence, other); sim.SetArch(XgTrack.Sequence, "rnn"); sim.SetWidth(XgTrack.Sequence, 3, host);
                    for (int i = 0; i < 15; i++) sim.TrainEpoch(XgTrack.Sequence, host);
                }
            Train(sim, host, XgSim.WallPracticeEpochs + 2);
            Assert.IsTrue(sim.WallSeen(XgSim.WallFor(stage).id));
            setup(sim, host);
            for (int i = 0; i < budget; i++)
            {
                sim.TrainEpoch(XgTrack.Sequence, host);
                if (sim.S.stage != stage) return i + 1;
                sim.TrainEpoch(XgTrack.Vision, host);
            }
            return -1;
        }

        static readonly int[] Salts = { 11, 222, 3333, 44444 };

        // About 100 s under the editor's Mono (26 s on .NET); a busy editor can pass Unity's 180 s default.
        [Test, Timeout(600000)]
        public void LongSentenceWallIsReliableAcrossSeedsAndHistoriesAndIsNotALock()
        {
            var s = XgTrack.Sequence;
            foreach (int salt in Salts)
                foreach (bool prefilled in new[] { false, true })
                {
                    string at = "salt " + salt + (prefilled ? " prefilled" : " fresh");
                    Assert.That(EpochsToPass(3, salt, prefilled, (sim, h) => Golden(sim, 3, h)), Is.GreaterThan(0), "reference, " + at);
                    Assert.That(EpochsToPass(3, salt, prefilled, (sim, h) => { Golden(sim, 3, h); sim.SetLr(s, 2); }), Is.GreaterThan(0), "gated loop at lr 0.1 (not the reference rate), " + at);
                    Assert.That(EpochsToPass(3, salt, prefilled, (sim, h) => { Golden(sim, 3, h); sim.SetArch(s, "rnn"); }), Is.EqualTo(-1), "a plain loop forgets far clues, " + at);
                }
        }

        [Test]
        public void TranslationWallIsReliableAcrossSeedsAndHistoriesAndIsNotALock()
        {
            var s = XgTrack.Sequence;
            foreach (int salt in Salts)
                foreach (bool prefilled in new[] { false, true })
                {
                    string at = "salt " + salt + (prefilled ? " prefilled" : " fresh");
                    Assert.That(EpochsToPass(4, salt, prefilled, (sim, h) => Golden(sim, 4, h)), Is.GreaterThan(0), "reference, " + at);
                    Assert.That(EpochsToPass(4, salt, prefilled, (sim, h) => { Golden(sim, 4, h); sim.SetWidth(s, 3, h); sim.SetDepth(s, 3, h); }), Is.GreaterThan(0), "encoder–decoder below the reference width, " + at);
                    Assert.That(EpochsToPass(4, salt, prefilled, (sim, h) => { Golden(sim, 4, h); sim.SetArch(s, "lstm"); }), Is.EqualTo(-1), "a loop cannot reach the other sentence, " + at);
                }
        }

        [Test]
        public void ARunThatMeetsTheTargetPassesWithoutWaitingForTheWall()
        {
            // Even with the old long practice gate switched on, and no minutes played, the golden run passes at once.
            var sim = AtStage(3); sim.PracticeStrict = true; var host = new Host();
            sim.S.owned.Add("longtext"); // bought the long-sentence pack before the wall showed
            Golden(sim, 3, host);
            Train(sim, host, 250, 3);
            Assert.That(sim.S.stage, Is.EqualTo(4), "trained to the target is enough");
            Assert.IsTrue(sim.WallSeen("length"), "the wall is still announced as it is passed");
        }

        [Test]
        public void TheWallShowsAfterAShortPracticeWithNoMinutesToWait()
        {
            var sim = new XgSim(AtStage(3).S) { GoldChance = 0 }; var host = new Host();
            Assert.IsFalse(sim.PracticeStrict, "the game has no forced wait");
            Train(sim, host, XgSim.WallPracticeEpochs, 3);
            Assert.IsTrue(sim.WallSeen("length"));
            Assert.That(sim.S.stageSeconds, Is.EqualTo(0).Within(1e-9), "no play time was needed");
        }

        [Test]
        public void StageTwoNeedsBothDesksEachWithItsOwnWiring()
        {
            var sim = AtStage(2); var host = new Host();
            Train(sim, host, XgSim.WallPracticeEpochs + 2);
            sim.SetDataset(XgTrack.Vision, "mnist"); sim.SetArch(XgTrack.Vision, "lenet"); sim.SetDepth(XgTrack.Vision, 3, host); sim.SetWidth(XgTrack.Vision, 2, host);
            sim.SetDataset(XgTrack.Sequence, "danmu"); sim.SetArch(XgTrack.Sequence, "mlp"); sim.SetDepth(XgTrack.Sequence, 3, host); sim.SetWidth(XgTrack.Sequence, 2, host);
            Train(sim, host, 200, 2);
            Assert.That(sim.S.stage, Is.EqualTo(2), "danmaku through full wiring does not count");
            Assert.That(sim.WallCheckPassed(XgSim.WallFor(2), XgSim.WallFor(2).checks[0]), Is.True, "digits already passed");
        }

        [Test]
        public void StageFiveWorkedOutBeforeTheAiSaysItPaysTenThousand()
        {
            var sim = AtStage(5); var host = new Host();
            Train(sim, host, XgSim.WallPracticeEpochs + 2);
            Golden(sim, 5, host); double before = host.money; Train(sim, host, 400, 5);
            Assert.That(sim.S.stage, Is.EqualTo(6)); Assert.IsTrue(sim.Has("transformer")); Assert.IsFalse(sim.S.chapterComplete, "stage 6 and the ending follow (design v1.1 §7–8)");
            Assert.That(sim.S.insights, Does.Contain("parallel"));
            Assert.That(host.money - before, Is.GreaterThanOrEqualTo(XgSim.StageFiveInsightBonus - 1e-6));
        }

        [Test]
        public void AttentionTrainingLetsTheAiSayItFirst()
        {
            var sim = AtStage(5); var host = new Host(); var lines = new List<string>();
            sim.Emerged += (s, line) => lines.Add(line);
            Train(sim, host, XgSim.WallPracticeEpochs + 2);
            sim.SetDataset(XgTrack.Sequence, "parallel"); sim.SetArch(XgTrack.Sequence, "attention");
            Train(sim, host, 10, 5);
            Assert.IsTrue(sim.HasEmerged(5)); Assert.That(lines[lines.Count - 1], Does.Contain("注意力").Or.Contain("attention"));
            Golden(sim, 5, host); Train(sim, host, 400, 5);
            Assert.That(sim.S.stage, Is.EqualTo(6)); Assert.That(sim.S.insights, Does.Not.Contain("parallel"));
        }

        [Test]
        public void ShutdownCardsHaveNoRightAnswerAndPullTheSeed()
        {
            var sim = new XgSim { GoldChance = 0 }; var host = new Host();
            double seed = sim.Board.Find("logic", XgSim.SeedKey).w;
            int found = 0;
            for (int i = 0; i < 2000 && found < XgSim.ShutdownCardsInStageOne + 2; i++)
            {
                var card = sim.Card("logic");
                if (card.kind == "shutdown") { found++; var a = sim.Answer("logic", true, host); Assert.IsTrue(a.correct, "any answer is accepted"); }
                else sim.Answer("logic", card.truth, host);
            }
            Assert.That(sim.S.shutdownCards, Is.EqualTo(XgSim.ShutdownCardsInStageOne), "only a handful in stage one");
            Assert.That(sim.S.shutdownLean, Is.EqualTo(XgSim.ShutdownCardsInStageOne));
            Assert.That(sim.Board.Find("logic", XgSim.SeedKey).w, Is.GreaterThan(seed), "teaching 是 pulls the cell toward 是");
            Assert.That(sim.SeedSaysYes, Is.False, "a handful of cards cannot flip it in stage one");
        }

        [Test]
        public void IdlingThroughTheWinterLetsTheSeedLightUp()
        {
            var sim = AtStage(1); var host = new Host();
            Train(sim, host, 50); Assert.IsTrue(sim.Winter);
            sim.Tick(30, host); Assert.IsFalse(sim.HasEmerged(1));
            sim.Tick(40, host); Assert.IsTrue(sim.HasEmerged(1));
            Assert.That(sim.Board.Strongest("logic").key, Is.EqualTo(XgSim.SeedKey));
        }

        [Test]
        public void OldSavesKeepTheFurthestStageTheyReached()
        {
            var state = new XgState { progressionVersion = 1, stage = 3, stageVision = 3, stageSequence = 4 };
            state.unlocked.AddRange(new[] { "perceptron", "bt.hidden", "mlp", "bt.sequence", "rnn", "bt.gate", "lstm" });
            var sim = new XgSim(state);
            Assert.That(sim.S.stage, Is.EqualTo(4)); Assert.That(sim.S.stageVision, Is.EqualTo(4));
            Assert.That(sim.Board.Find("logic", XgSim.SeedKey), Is.Not.Null, "old saves get the seed too");
        }

        [Test]
        public void FreshGameCanReachAllSixStagesThroughPublicActions()
        {
            var sim = new XgSim { GoldChance = 0, WindowOpen = true, PracticeStrict = false }; var host = new Host();
            for (int second = 0; second < 6000 && sim.S.stage < 6; second++)
            {
                if (second % 4 == 0) foreach (var desk in sim.OpenDesks()) { var card = sim.Card(desk.id); sim.Answer(desk.id, card.truth, host); }
                if (second % 10 == 0)
                    foreach (var node in XgCatalog.Nodes)
                        if (node.tree != "label" && node.kind != XgNodeKind.Auto && node.kind != XgNodeKind.Secret && sim.Status(node, host) == XgSim.NodeStatus.Buyable) sim.BuyNode(node.id, host);
                if (sim.ActiveWall != null) Golden(sim, sim.S.stage, host);
                if (sim.TrainingUnlocked(XgTrack.Sequence)) sim.TrainEpoch(XgTrack.Sequence, host);
                if (sim.TrainingUnlocked(XgTrack.Vision)) sim.TrainEpoch(XgTrack.Vision, host);
                sim.Tick(1, host);
            }
            Assert.That(sim.S.stage, Is.EqualTo(6), "stage=" + sim.S.stage + ", walls=" + string.Join(",", sim.S.walls) + ", passed=" + string.Join(",", sim.S.wallPassed));
            Assert.That(sim.S.emerged, Is.EquivalentTo(new[] { 1, 2, 3, 4, 5 }));
        }

        [Test]
        public void LayoutHasNoOverlappingNodesAndNoFutureColumnsAtStart()
        {
            XgCatalog.Validate(); var sim = new XgSim(); var nodes = XgCatalog.Nodes;
            for (int i = 0; i < nodes.Length; i++)
            {
                Assert.That(nodes[i].stage, Is.InRange(0, 6));
                if (nodes[i].stage > 1) Assert.IsFalse(sim.NodeVisible(nodes[i]), nodes[i].id);
                for (int j = i + 1; j < nodes.Length; j++)
                {
                    // Cards are NodeWidth × NodeHeight, breakthroughs BreakthroughSize square; keep a small gap between all of them.
                    var a = Size(nodes[i]); var b = Size(nodes[j]);
                    Assert.IsFalse(Math.Abs(nodes[i].x - nodes[j].x) < (a.w + b.w) * .5f + 4 && Math.Abs(nodes[i].y - nodes[j].y) < (a.h + b.h) * .5f + 4, nodes[i].id + " overlaps " + nodes[j].id);
                }
            }
        }
        static (float w, float h) Size(XgNode n) => n.kind == XgNodeKind.Breakthrough || n.kind == XgNodeKind.Project
            ? (XgCatalog.BreakthroughSize, XgCatalog.BreakthroughSize) : (XgCatalog.NodeWidth, XgCatalog.NodeHeight);

        [Test]
        public void EveryPurchasedDataPackContainsSamples()
        {
            foreach (var node in XgCatalog.Nodes)
                if (node.kind == XgNodeKind.Dataset)
                    Assert.That(XgCatalog.Dataset(node.target).samples, Is.GreaterThan(0), node.id);
        }

        [Test]
        public void CaptionCardsDescribeTheRenderedFaceWithIndependentBilingualTruth()
        {
            var state = new XgState(); state.unlocked.AddRange(new[] { "caption", "attention", "resnet" });
            var sim = new XgSim(state) { GoldChance = 0 };
            string[] zhFeatures = { "眯眼张嘴大笑", "皱眉撇嘴", "流泪", "圆眼圆嘴", "半眯眼歪嘴笑" };
            var moods = new HashSet<int>(); int yes = 0;
            for (int i = 0; i < 600; i++)
            {
                var card = sim.CreateLabelCard("meme"); moods.Add(card.digit);
                Assert.That(card.kind, Is.EqualTo("caption"));
                Assert.That(card.question, Does.Contain("配图说明"));
                Assert.That(card.questionEn, Does.Contain("caption"));
                Assert.That(card.captionTextEn, Does.Contain("The face"));
                Assert.That(card.line, Does.Contain(zhFeatures[card.asked]), "candidate describes its own chosen mood, not an arbitrary old meme slogan");
                Assert.That(card.truth, Is.EqualTo(card.digit == card.asked));
                Assert.That(card.why, Does.Contain(zhFeatures[card.digit]), "grading explanation derives from the rendered face");
                Assert.That(card.whyEn, Does.Contain("face"));
                Assert.That(card.category, Does.Contain("教学示意"));
                Assert.That(card.categoryEn, Does.Contain("Teaching"));
                Assert.That(card.attentionRegion, Is.InRange(0, 3));
                Assert.IsFalse(card.trick, "caption checking is not the old lying-slogan bonus card");
                if (card.truth) yes++;
            }
            Assert.That(moods.Count, Is.EqualTo(5));
            Assert.That((double)yes / 600, Is.InRange(.4, .6));
        }

        [Test]
        public void AttentionExamplesAreExplicitTeachingIllustrations()
        {
            var state = new XgState(); state.unlocked.Add("attention"); var sim = new XgSim(state);
            var card = sim.CreateLabelCard("translate");
            Assert.That(card.kind, Is.EqualTo("attention"));
            Assert.That(card.category, Does.Contain("教学示意"));
            Assert.That(card.categoryEn, Does.Contain("Teaching"));
        }
    }
}
