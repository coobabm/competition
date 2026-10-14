using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgRetiredTaskTests
    {
        sealed class Host : IXgHost
        {
            public double money = 100000;
            public double Compute => 2;
            public double VramMB => 100000;
            public double Money => money;
            public string Blocker => null;
            public bool Spend(double amount) { if (money < amount) return false; money -= amount; return true; }
            public void Earn(double amount) { money += amount; }
            public void Train(double seconds) { }
        }

        static XgState Saved(int stage = 5) => new XgState
        {
            progressionVersion = XgSim.ProgressionSchemaWalls,
            stage = stage, stageVision = stage, stageSequence = stage,
            fundedAbility = XgSim.AbilityCount, // the milestone payments of the stages it reached were collected long ago
        };

        [TestCase("goclub", "go")]
        [TestCase("parking", "mnist")]
        [TestCase("support", "news")]
        public void RetiredContractsCannotBeSignedOrEarnAfterReload(string id, string dataset)
        {
            var state = Saved();
            state.contracts.Add(id);
            state.best.Add(new XgBest { dataset = dataset, acc = 1 });
            var sim = new XgSim(state); var host = new Host();
            Assert.IsNull(XgCatalog.Contract(id));
            Assert.IsFalse(sim.Signed(id));
            Assert.IsFalse(sim.Sign(id, host));
            Assert.AreEqual(0, sim.IncomePerSecond);
            sim.Tick(2, host);
            Assert.AreEqual(100000, host.money, "a saved retired contract must not keep paying");
        }

        [TestCase("translate")]
        [TestCase("crosssentence")]
        public void RetiredCrowdTasksCannotBeOpenedPostedOrSelected(string id)
        {
            var sim = new XgSim(Saved());
            sim.S.owned.Add(id); sim.S.unlocked.Add(id + ".pack");
            sim.S.desksOpen.Add(id); // Old UI/state references are not authorization to resume a task.
            string selected = sim.S.desk;
            Assert.IsNull(XgCatalog.Desk(id));
            Assert.IsFalse(sim.DeskOpen(id));
            Assert.IsFalse(sim.ProgressionDeskAvailable(id));
            Assert.IsFalse(sim.SetCrowd(id, true));
            sim.SelectDesk(id);
            Assert.AreEqual(selected, sim.S.desk);
        }

        [TestCase("translate")]
        [TestCase("crosssentence")]
        public void ReloadDropsRetiredCardsQueuesTimersAndWorkerAssignmentButKeepsTrainingData(string id)
        {
            var state = Saved();
            state.desk = id; state.desksOpen.Add(id); state.owned.Add(id); state.crowdOn.Add(id);
            state.labels.Add(new XgLabelCount { dataset = id, count = 12 });
            state.autoTimers.Add(new XgLabelCount { dataset = id, count = .8 });
            state.cards.Add(new XgCard { id = 10, dataset = id, question = "old card" });
            state.queue.Add(new XgCard { id = 11, dataset = id, question = "old review" });
            state.prefetch.Add(new XgCard { id = 12, dataset = id, question = "old prediction" });
            state.scUnlocked = true;
            state.scWorkers.Add(new XgWorkerState { id = "ajie", desk = id, hired = true });
            var sim = new XgSim(state);
            Assert.AreNotEqual(id, sim.S.desk);
            Assert.IsTrue(sim.DeskOpen(sim.S.desk), "the retired selection must fall back to an open task");
            Assert.That(sim.S.desksOpen, Does.Not.Contain(id));
            Assert.IsFalse(sim.S.cards.Exists(c => c.dataset == id));
            Assert.IsFalse(sim.S.queue.Exists(c => c.dataset == id));
            Assert.IsFalse(sim.S.prefetch.Exists(c => c.dataset == id));
            Assert.IsFalse(sim.S.autoTimers.Exists(c => c.dataset == id));
            Assert.IsFalse(sim.CrowdPosted(id));
            Assert.IsFalse(sim.Worker("ajie").hired);
            Assert.AreEqual("", sim.Worker("ajie").desk);
            Assert.AreEqual(12, sim.Labels(id));
            Assert.IsTrue(sim.DatasetAvailable(id));
            Assert.IsTrue(sim.SetDataset(XgTrack.Sequence, id));
        }

        [TestCase("translate")]
        [TestCase("crosssentence")]
        public void InternalTeachingCardsCannotBeSubmittedAsPaidCrowdWork(string id)
        {
            var sim = new XgSim(Saved()); var host = new Host();
            var card = sim.CreateLabelCard(id);
            Assert.IsNotNull(card);
            Assert.IsNotEmpty(card.question);
            Assert.IsFalse(sim.Answer(id, card.truth, host).correct);
            Assert.AreEqual(0, sim.ChatLabel(id, true, host));
            card.hasJudgment = true; card.guess = card.truth; card.confidence = 1;
            sim.S.prefetch.Add(card);
            Assert.IsFalse(sim.Route(card.id, host));
            sim.S.queue.Add(card);
            Assert.IsFalse(sim.AnswerQueued(card.id, card.truth, host).accepted);
            Assert.AreEqual(0, sim.Labels(id));
            Assert.AreEqual(100000, host.money);
        }

        [TestCase("translate")]
        [TestCase("crosssentence")]
        public void RetiredTaskDatasetsDoNotUnlockEarlyButRemainPurchasableForTraining(string id)
        {
            var sim = new XgSim();
            Assert.IsFalse(sim.DatasetAvailable(id));
            Assert.IsFalse(sim.SetDataset(XgTrack.Sequence, id));
            Assert.IsNotNull(XgCatalog.Node(id + ".pack"));
            sim.S.owned.Add(id);
            Assert.IsTrue(sim.DatasetAvailable(id));
            Assert.IsTrue(sim.SetDataset(XgTrack.Sequence, id));
            Assert.Greater(sim.Samples(id), 0);
        }

        [TestCase("crosssentence", 0)]
        [TestCase("crosssentence", XgSim.ProgressionSchemaWalls)]
        [TestCase("translate", 0)]
        [TestCase("translate", XgSim.ProgressionSchemaWalls)]
        public void FreeLegacyDeskKeepsTrainingAccessWithoutGrantingAFullPack(string id, int version)
        {
            var state = Saved(4); state.progressionVersion = version;
            state.desksOpen.Add(id);
            if (version > 0) state.unlocked.Add(id + ".pack");
            state.labels.Add(new XgLabelCount { dataset = id, count = 60 });
            state.sequence.dataset = id;
            var sim = new XgSim(state);
            Assert.IsTrue(sim.Has(id + ".pack"));
            Assert.IsTrue(sim.DatasetAvailable(id), "legacy free-desk access must survive retirement");
            Assert.AreEqual(id, sim.S.sequence.dataset);
            Assert.IsTrue(sim.TrainingUnlocked(XgTrack.Sequence));
            Assert.IsFalse(sim.DeskOpen(id));
            Assert.IsFalse(sim.Owns(id), "a free legacy desk did not include a full data pack");
            Assert.AreEqual(60, sim.Samples(id));
            Assert.IsTrue(new XgSim(sim.S).DatasetAvailable(id), "access must survive a second reload after the desk is removed");
        }

        [TestCase("translate")]
        [TestCase("crosssentence")]
        public void RetiredDatasetUnlockDoesNotBypassAnActivePackDownload(string id)
        {
            var state = Saved(); state.owned.Add(id); state.unlocked.Add(id + ".pack");
            state.downloads.Add(new XgScore { key = id, value = 120 });
            var sim = new XgSim(state);
            Assert.IsFalse(sim.DatasetAvailable(id));
        }

        [Test]
        public void HumanRuleDoesNotDisplayARetiredContractId()
        {
            var state = Saved(6); state.best.Add(new XgBest { dataset = "translate", acc = .75 });
            var sim = new XgSim(state);
            var rule = sim.RuleCards().Find(c => c.id == "human");
            Assert.IsNotNull(rule);
            Assert.AreEqual("你的选择", rule.source);
        }
    }
}
