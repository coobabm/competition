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
            Assert.That(sim.FeatureVisible("contracts"), Is.True);
        }
        [Test] public void WallsWaitForActualDeliveryAndUnfinishedBreakthroughsResume()
        {
            var state = new XgState(); state.walls.Add("combo"); state.stage = 2;
            Assert.That(XgStoryMilestones.WallMayOpen(state, "combo", false), Is.False);
            Assert.That(XgStoryMilestones.WallMayOpen(state, "combo", true), Is.True);
            Assert.That(XgStoryMilestones.Pending(state, beat => false), Does.Contain("bt.hidden"));
            Assert.That(XgStoryMilestones.Pending(state, beat => beat == "bt_hidden"), Does.Not.Contain("bt.hidden"));
            state.migratedBeats.Add("bt_hidden");
            Assert.That(XgStoryMilestones.Pending(state, beat => false), Does.Not.Contain("bt.hidden"));
        }
    }
}
