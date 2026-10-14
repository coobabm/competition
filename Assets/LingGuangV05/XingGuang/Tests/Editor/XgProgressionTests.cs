using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>
    /// Progression after 参数量与数据量主线: architectures are items with prices, the stage follows the abilities,
    /// and the stage changes keep their desks, words and story milestones.
    /// </summary>
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
            var state = new XgState { progressionVersion = XgSim.ProgressionSchema, stage = stage, stageVision = stage, stageSequence = stage, dataSalt = salt };
            var sim = new XgSim(state) { GoldChance = 0 };
            foreach (var n in XgCatalog.Nodes)
                if (n.stage <= stage && n.tree != "label" && n.kind != XgNodeKind.Secret && n.kind != XgNodeKind.Label && n.id != "transformer" && !sim.Has(n.id))
                    sim.S.unlocked.Add(n.id);
            foreach (var d in new[] { "logic", "spam", "mnist", "danmu" }) if (!sim.S.desksOpen.Contains(d)) sim.S.desksOpen.Add(d);
            sim.S.labels.Add(new XgLabelCount { dataset = "spam", count = 300 });
            sim.S.labels.Add(new XgLabelCount { dataset = "mnist", count = 300 });
            return new XgSim(sim.S) { GoldChance = 0 };
        }

        static void Train(XgSim sim, IXgHost host, int epochs, int stage = 0)
        {
            for (int i = 0; i < epochs && (stage == 0 || sim.S.stage == stage); i++)
            {
                if (sim.TrainingUnlocked(XgTrack.Sequence)) sim.TrainEpoch(XgTrack.Sequence, host);
                if (sim.TrainingUnlocked(XgTrack.Vision) && (stage == 0 || sim.S.stage == stage)) sim.TrainEpoch(XgTrack.Vision, host);
            }
        }

        /// <summary>The player's confirmation on the training page: a trained round this stage and both bars full (the bars filling alone changes nothing).</summary>
        static void Confirm(XgSim sim)
        {
            sim.S.stageEpochs = System.Math.Max(1, sim.S.stageEpochs);
            Assert.IsTrue(sim.StageReady, "both bars are full");
            Assert.IsTrue(sim.ConfirmNextStage());
        }

        static void FillBars(XgSim sim, int ability)
        {
            sim.S.trainedParamsK = Math.Max(sim.S.trainedParamsK, sim.ParamsThreshold(ability));
            double short_ = sim.SamplesThreshold(ability) - sim.TrainedSamples;
            if (short_ > 0) sim.S.labels.Add(new XgLabelCount { dataset = "poems", count = short_ + 1 });
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
            Assert.That(XgCatalog.Node("secret.1"), Is.Null, "the walls' secrets are gone");
            Assert.IsFalse(sim.NodeVisible(XgCatalog.Node("label.brain")));
        }

        [Test]
        public void TheBreakthroughsAreItemsWithPrices()
        {
            foreach (var n in XgCatalog.Nodes)
                Assert.That(n.kind, Is.Not.EqualTo(XgNodeKind.Breakthrough).And.Not.EqualTo(XgNodeKind.Project), n.id);
            foreach (var id in new[] { "bt.hidden", "bt.vision", "bt.sequence", "bt.gate", "bt.residual", "bt.attention", "bt.spatial", "project.transformer", "secret.1", "secret.4", "secret.deep" })
                Assert.That(XgCatalog.Node(id), Is.Null, id);
            foreach (var id in new[] { "mlp", "lenet", "rnn", "lstm", "resnet", "seq2seq", "attention", "transformer" })
            {
                var n = XgCatalog.Node(id);
                Assert.That(n.kind, Is.EqualTo(XgNodeKind.Arch), id);
                Assert.That(n.cost, Is.GreaterThan(0), id + " is bought, not handed out by a wall");
            }
            Assert.That(XgCatalog.Node("secret.6"), Is.Not.Null, "pre-training keeps its secret");
        }

        [Test]
        public void BuyingTheMlpMovesBothTracksAndLowersAbilityTwo()
        {
            var sim = new XgSim { GoldChance = 0 }; var host = new Host();
            double before = sim.ParamsThreshold(2);
            Assert.IsTrue(sim.BuyNode("mlp", host));
            Assert.That(sim.S.vision.arch, Is.EqualTo("mlp"));
            Assert.That(sim.S.sequence.arch, Is.EqualTo("mlp"));
            Assert.That(sim.ParamsThreshold(2), Is.LessThan(before));
        }

        [Test]
        public void AbilityTwoEndsStageOneWithItsDesksAndMilestone()
        {
            var sim = new XgSim { GoldChance = 0 }; var host = new Host(); var stages = new List<int>(); var milestones = new List<string>();
            sim.StageAdvanced += stages.Add; sim.BreakthroughDone += milestones.Add;
            FillBars(sim, 2);
            sim.Tick(1, host);
            Assert.That(sim.S.stage, Is.EqualTo(1), "full bars wait for the player's confirmation");
            Confirm(sim);
            Assert.That(sim.S.stage, Is.EqualTo(2));
            Assert.That(stages, Is.EqualTo(new[] { 1 }));
            Assert.That(milestones, Does.Contain("bt.hidden"));
            Assert.That(sim.HasEmerged(1), Is.True, "the stage's emergence moment is guaranteed when it ends");
            Assert.IsTrue(sim.DeskOpen("mnist") && sim.DeskOpen("danmu"));
        }

        [Test]
        public void AbilityThreeSpeaksTheChosenTracksFirstWords()
        {
            var sim = AtStage(2); var host = new Host(); var milestones = new List<string>();
            sim.BreakthroughDone += milestones.Add;
            FillBars(sim, 3);
            sim.Tick(1, host);
            Confirm(sim);
            Assert.That(sim.S.stage, Is.EqualTo(3));
            Assert.That(sim.S.firstTrack, Is.Not.Empty, "它的第一个选择 is made by the time it speaks");
            Assert.That(milestones, Does.Contain("bt." + sim.S.firstTrack));
        }

        [Test]
        public void AttentionTrainingLetsTheAiSayItFirstAndOpensTheTransformer()
        {
            var sim = AtStage(5); var host = new Host(); var lines = new List<string>();
            sim.Emerged += (s, line) => lines.Add(line);
            var transformer = XgCatalog.Node("transformer");
            Assert.That(sim.Status(transformer, host), Is.EqualTo(XgSim.NodeStatus.Locked), "it has not thought of it yet");
            sim.S.labels.Add(new XgLabelCount { dataset = "poems", count = 400 });
            sim.SetDataset(XgTrack.Sequence, "poems");
            sim.AutoConfigure(sim.S.sequence, host);
            Assert.That(sim.S.sequence.arch, Is.EqualTo("attention"), "the best structure owned for the reading line");
            for (int i = 0; i < 12 && !sim.HasEmerged(5); i++) sim.TrainEpoch(XgTrack.Sequence, host);
            Assert.IsTrue(sim.HasEmerged(5)); Assert.That(lines[lines.Count - 1], Does.Contain("注意力").Or.Contain("attention"));
            Assert.That(sim.Status(transformer, host), Is.EqualTo(XgSim.NodeStatus.Buyable));
            double six = sim.ParamsThreshold(6);
            Assert.IsTrue(sim.BuyNode("transformer", host));
            Assert.That(sim.ParamsThreshold(6), Is.LessThan(six));
            Assert.IsTrue(sim.Has("multihead") && sim.Has("layernorm") && sim.Has("residual"));
            Assert.That(sim.S.stage, Is.EqualTo(5), "buying the item does not end the stage");
        }

        [Test]
        public void TheSixthAbilityBringsTheTransformerWhenItWasNotBought()
        {
            var sim = AtStage(5); var host = new Host(); var milestones = new List<string>();
            sim.BreakthroughDone += milestones.Add;
            Assert.IsFalse(sim.Has("transformer"));
            FillBars(sim, 6);
            sim.Tick(1, host);
            Confirm(sim);
            Assert.That(sim.S.stage, Is.EqualTo(6));
            Assert.IsTrue(sim.Has("transformer") && sim.Has("position") && sim.Has("warmup"));
            Assert.That(milestones, Does.Contain("transformer"));
            Assert.IsFalse(sim.S.chapterComplete, "pre-training and the ending follow");
        }

        [Test]
        public void FreshGameReachesTheThirdAbilityThroughPublicActions()
        {
            // Labelling, buying and training only; the full route to the finale is the balance bot's job.
            var sim = new XgSim { GoldChance = 0, WindowOpen = true }; var host = new Host();
            for (int second = 0; second < 1800 && sim.S.stage < 3; second++)
            {
                if (second % 2 == 0) foreach (var desk in sim.OpenDesks()) { var card = sim.Card(desk.id); sim.Answer(desk.id, card.truth, host); }
                if (second % 10 == 0)
                {
                    foreach (var node in XgCatalog.Nodes)
                        if (node.tree != "label" && node.kind != XgNodeKind.Secret && sim.Status(node, host) == XgSim.NodeStatus.Buyable) sim.BuyNode(node.id, host);
                }
                if (sim.TrainingUnlocked(XgTrack.Sequence)) sim.TrainEpoch(XgTrack.Sequence, host);
                if (sim.TrainingUnlocked(XgTrack.Vision)) sim.TrainEpoch(XgTrack.Vision, host);
                sim.Tick(1, host);
                if (sim.StageReady) sim.ConfirmNextStage(); // the player's confirmation on the training page
            }
            Assert.That(sim.S.stage, Is.EqualTo(3), "stage=" + sim.S.stage + ", P=" + XgSim.ParamsText(sim.TrainedParamsK) + ", D=" + sim.TrainedSamples);
            Assert.That(sim.S.emerged, Is.EquivalentTo(new[] { 1, 2 }));
            Assert.That(sim.S.firstTrack, Is.Not.Empty);
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
        public void OldSavesKeepTheFurthestStageTheyReached()
        {
            var state = new XgState { progressionVersion = 1, stage = 3, stageVision = 3, stageSequence = 4 };
            state.unlocked.AddRange(new[] { "perceptron", "bt.hidden", "mlp", "bt.sequence", "rnn", "bt.gate", "lstm" });
            var sim = new XgSim(state);
            Assert.That(sim.S.stage, Is.EqualTo(4)); Assert.That(sim.S.stageVision, Is.EqualTo(4));
            Assert.That(sim.Board.Find("logic", XgSim.SeedKey), Is.Not.Null, "old saves get the seed too");
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
