using System.Linq;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>Removed app: no task, no callable service, and harmless legacy wiring.</summary>
    public sealed class XgJuxinTests
    {
        const string RemovedTask = "life.juxin";

        [TestCase(0)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void RemovedAppNeverReturnsToTheWiringGraph(int stage)
        {
            var sim = new XgSim();
            sim.S.stage = stage;
            var graph = sim.RefreshWiring(null);
            Assert.That(graph.Node(RemovedTask), Is.Null);
            Assert.That(graph.edges.Any(e => e.from == RemovedTask || e.to == RemovedTask), Is.False);
            Assert.That(graph.Node("life.netbar"), Is.Null, "the group quiz job went away with 小刚's quiz");
            Assert.That(graph.Node("life.qingwen"), Is.Null, "so did 「替我回晴雯」: the AI no longer answers for the player");
        }

        [Test]
        public void LoadingOldWiringDiscardsOnlyTheRemovedAppTask()
        {
            var saved = new XgState();
            saved.wiring.idle.AddRange(new[] { "train:0", RemovedTask, "label:mnist", "life.qingwen", RemovedTask, "life.netbar", "life.era.delivery" });
            var sim = new XgSim(saved);
            CollectionAssert.AreEqual(new[] { "train:0", "label:mnist", "life.era.delivery" }, sim.S.wiring.idle);
            sim = new XgSim(sim.S);
            CollectionAssert.AreEqual(new[] { "train:0", "label:mnist", "life.era.delivery" }, sim.S.wiring.idle, "migration is idempotent");
        }

        [Test]
        public void DeletedAppCannotBeReconnectedAndDoesNotToggleYyJobs()
        {
            var sim = new XgSim();
            sim.S.stage = 5;
            bool delivery = false;
            sim.HookLife(XgSim.EraDeliveryTask, () => true, () => delivery, on => delivery = on);
            Assert.That(sim.Connect("self", XgSim.EraDeliveryTask, null), Is.Null);
            Assert.That(sim.Connect("self", RemovedTask, null), Is.Not.Null);
            Assert.That(sim.Disconnect("self", RemovedTask, null), Is.False);
            Assert.That(delivery, Is.True);
        }

        [Test]
        public void RemovedServiceCannotBeTickedOrSerializedIntoNewSaves()
        {
            Assert.That(typeof(XgSim).GetMethod("JuxinTick"), Is.Null);
            Assert.That(typeof(XgSim).GetMethod("SetJuxinAutoReply"), Is.Null);
            Assert.That(typeof(XgState).GetFields().Where(f => f.Name.StartsWith("jx")), Is.Empty);
        }
    }
}
