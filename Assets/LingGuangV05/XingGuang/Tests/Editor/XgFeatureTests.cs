using NUnit.Framework;
namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgFeatureTests
    {
        [Test] public void FeatureVisibilityIsACorePolicyNotOnlyDisabledButtons()
        { Assert.That(typeof(XgSim).GetMethod("FeatureVisible"), Is.Not.Null); }
        [Test]
        public void FreshUiHidesFuturePagesUntilTheirActualMilestones()
        {
            var sim = new XgSim();
            Assert.That(sim.FeatureVisible("label"), Is.True);
            foreach (var feature in new[] { "train", "tree", "contracts", "repo", "save-model", "coop", "attention", "lingguang-contact", "project", "unknown" })
                Assert.That(sim.FeatureVisible(feature), Is.False, feature);
            sim.S.labels.Add(new XgLabelCount { dataset = "spam", count = 12 });
            // 老周 only answers (design v1.1 §9), so no page waits for his introduction: it opens with the progress itself.
            Assert.That(sim.FeatureVisible("train"), Is.True, "training opens as soon as there are enough samples");
            Assert.That(sim.FeatureVisible("tree"), Is.False);
            sim.S.paidAssessmentReached = true; Assert.That(sim.FeatureVisible("tree"), Is.True, "the tree opens with the first paid assessment");
            Assert.That(sim.FeatureVisible("contracts"), Is.False, "contracts need a checkpoint");
            sim.S.best.Add(new XgBest { dataset = "mnist", acc = .8 }); Assert.That(sim.FeatureVisible("repo"), Is.True);
            Assert.That(sim.FeatureVisible("contracts"), Is.False, "orders belong to the crowd stage, whatever the checkpoint");
            sim.S.stage = XgSim.FeatureUnlock["crowd"];
            Assert.That(XgSim.Grade(sim.BestScore("mnist")), Is.GreaterThanOrEqualTo(1));
            Assert.That(sim.FeatureVisible("contracts"), Is.True, "the crowd stage with a graded checkpoint");
            sim.S.best[0].acc = .1;
            Assert.That(sim.FeatureVisible("contracts"), Is.False, "and a checkpoint that earned a grade");
        }
        [Test] public void UnfinishedStageMilestonesResume()
        {
            var state = new XgState(); state.stage = 2;
            Assert.That(XgStoryMilestones.Pending(state, beat => false), Does.Contain("bt.hidden"));
            Assert.That(XgStoryMilestones.Pending(state, beat => false), Does.Not.Contain("bt.residual"), "ResNet is an item, not a stage");
            state.unlocked.Add("resnet");
            Assert.That(XgStoryMilestones.Pending(state, beat => false), Does.Contain("bt.residual"), "buying the item reaches its milestone");
            Assert.That(XgStoryMilestones.Pending(state, beat => beat == "bt_hidden"), Does.Not.Contain("bt.hidden"));
            state.migratedBeats.Add("bt_hidden");
            Assert.That(XgStoryMilestones.Pending(state, beat => false), Does.Not.Contain("bt.hidden"));
        }
    }
}
