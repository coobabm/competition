using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgForgetNightTests
    {
        static XgSim Ready()
        {
            var sim = new XgSim();
            sim.S.stage = sim.S.stageVision = sim.S.stageSequence = 4;
            // A real stage-4 save has abilities 1…4 emerged. The loader repairs exactly that (GrantAbilitiesUpTo),
            // so a fixture that only sets the stage would change identity across a JsonUtility round-trip:
            // NextAbility would jump from 2 to 5 and ExamReady would flip, which is the fixture's fault, not the save's.
            sim.S.abilitiesEmerged = new System.Collections.Generic.List<int> { 1, 2, 3, 4 };
            sim.S.stageEpochs = 1;
            sim.S.trainedParamsK = sim.ParamsThreshold(sim.NextAbility) * 2;
            sim.S.labels.Add(new XgLabelCount { dataset = "poems", count = sim.SamplesThreshold(sim.NextAbility) * 100 });
            Assert.IsTrue(sim.ExamReady, "fixture satisfies actual exam prerequisites");
            return sim;
        }

        static int Deliver(XgSim sim, string answer, bool english = false)
        {
            var request = sim.BeginForgetNightRequest(sim.ForgetNightChoice(english));
            Assert.IsNotNull(request);
            int id = sim.RegisterYyReply(request.Question, answer);
            Assert.IsTrue(sim.CommitForgetNightReply(request, id, true));
            return id;
        }

        static XgSim Recall()
        {
            var sim = Ready();
            Deliver(sim, "好的，我会记着。");
            Deliver(sim, "听起来今天过得很快。");
            return sim;
        }

        [Test] public void SendingOrFailedDeliveryDoesNotAdvanceOrCreateMemory()
        {
            var sim = Ready(); var request = sim.BeginForgetNightRequest(sim.ForgetNightChoice(false));
            Assert.IsNotNull(request); Assert.AreEqual(0, sim.S.fnStep);
            Assert.IsFalse(sim.CommitForgetNightReply(request, 123, true));
            Assert.AreEqual(0, sim.RegisterYyReply(request.Question, "", false));
            Assert.IsEmpty(sim.S.fnEvidence); Assert.IsFalse(sim.ChronicleHas(XgSim.ForgetNightChronicle));
            sim.CancelForgetNightRequest();
            Assert.IsNotNull(sim.BeginForgetNightRequest(request.Question), "the failed round remains retryable");
        }

        [Test] public void AuthoredFallbackOrAnotherQuestionsReceiptCannotCountAsModelEvidence()
        {
            var sim = Ready(); var request = sim.BeginForgetNightRequest(sim.ForgetNightChoice(false));
            int other = sim.RegisterYyReply("普通聊天", "好的");
            Assert.IsFalse(sim.CommitForgetNightReply(request, other, true));
            int matching = sim.RegisterYyReply(request.Question, "记住了");
            Assert.IsFalse(sim.CommitForgetNightReply(request, matching, false));
            Assert.AreEqual(0, sim.S.fnStep); Assert.IsEmpty(sim.S.fnEvidence);
        }

        [Test] public void CancellationAndNewBindingRejectLateCallbacksAndOldReceipts()
        {
            var sim = Ready(); var request = sim.BeginForgetNightRequest(sim.ForgetNightChoice(false));
            int id = sim.RegisterYyReply(request.Question, "记住了");
            sim.CancelForgetNightRequest();
            Assert.IsFalse(sim.CommitForgetNightReply(request, id, true));
            var retry = sim.BeginForgetNightRequest(request.Question);
            Assert.IsFalse(sim.CommitForgetNightReply(retry, id, true), "an old delivery cannot stand in for this request");
            var restored = new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(sim.S)));
            Assert.IsFalse(restored.CommitForgetNightReply(retry, id, true));
            Assert.AreEqual(0, restored.S.fnStep);
            Assert.IsNotNull(restored.BeginForgetNightRequest(request.Question), "pending work is not serialized as a completed round");
        }

        [Test] public void ActualDeliveredCodeRecordsRememberedWithExactReplyAndSourceOnce()
        {
            var sim = Recall(); const string answer = "今晚吃烤冷面，多加醋。";
            var request = sim.BeginForgetNightRequest(sim.ForgetNightChoice(false));
            int id = sim.RegisterYyReply(request.Question, answer);
            Assert.IsFalse(sim.S.fnDone, "a registered response still needs this exchange's delivery acknowledgement");
            Assert.IsTrue(sim.CommitForgetNightReply(request, id, true));
            Assert.IsFalse(sim.CommitForgetNightReply(request, id, true));
            Assert.IsTrue(sim.S.fnDone); Assert.AreEqual(XgFnRecall.Remembered, sim.ForgetNightResult.recall);
            Assert.AreEqual(answer, sim.ForgetNightResult.answer); Assert.AreEqual("yy:" + id, sim.ForgetNightResult.source);
            Assert.AreEqual(3, sim.S.fnEvidence.Count);
            Assert.AreEqual(1, sim.S.chronicle.Count(c => c.key == XgSim.ForgetNightChronicle));
            Assert.That(sim.Chronicle().Single(c => c.id == XgSim.ForgetNightChronicle).body, Does.Contain(answer).And.Not.Contain("忘得一干二净"));
        }

        [Test] public void ExplicitForgettingRecordsOnlyWhatTheModelActuallySaid()
        {
            var sim = Recall(); const string answer = "I cannot remember the code.";
            Deliver(sim, answer, true);
            Assert.AreEqual(XgFnRecall.Forgot, sim.ForgetNightResult.recall);
            Assert.AreEqual(answer, sim.ForgetNightResult.answer);
            Assert.That(sim.Chronicle().Single(c => c.id == XgSim.ForgetNightChronicle).body, Does.Contain(answer).And.Not.Contain("顶出去"));
        }

        [Test] public void AmbiguousActualReplyIsNotAForgettingVerdictAndCanBeRetried()
        {
            var sim = Recall();
            Deliver(sim, "风扇声音很大。");
            Assert.IsFalse(sim.S.fnDone); Assert.AreEqual(3, sim.S.fnStep);
            Assert.IsNull(sim.ForgetNightResult); Assert.IsFalse(sim.ChronicleHas(XgSim.ForgetNightChronicle));
            Assert.AreEqual("风扇声音很大。", sim.S.fnEvidence.Last().answer);
            Deliver(sim, "烤冷面，多加醋。");
            Assert.AreEqual(XgFnRecall.Remembered, sim.ForgetNightResult.recall);
            Assert.AreEqual(4, sim.S.fnEvidence.Count, "both delivered attempts remain attributable");
        }

        [TestCase("Grilled cold noodles tonight, extra vinegar.", XgFnRecall.Remembered)]
        [TestCase("记得：烤冷面，醋多一点。", XgFnRecall.Remembered)]
        [TestCase("我想不起来了。", XgFnRecall.Forgot)]
        [TestCase("I don't remember. Can you tell me again?", XgFnRecall.Forgot)]
        [TestCase("也许是烤冷面，多加醋。", XgFnRecall.Uncertain)]
        [TestCase("我不记得是烤冷面多加醋还是别的。", XgFnRecall.Uncertain)]
        [TestCase("烤冷面。", XgFnRecall.Uncertain)]
        [TestCase("今晚吃牛肉面。", XgFnRecall.Uncertain)]
        [TestCase("I have not forgotten", XgFnRecall.Uncertain)]
        [TestCase("I haven't forgotten", XgFnRecall.Uncertain)]
        [TestCase("不是烤冷面，多加醋", XgFnRecall.Uncertain)]
        [TestCase("It was not cold noodles with extra vinegar", XgFnRecall.Uncertain)]
        [TestCase("", XgFnRecall.Uncertain)]
        public void MissingKeywordsAndUncertaintyDoNotManufactureForgetting(string answer, XgFnRecall expected)
        { Assert.AreEqual(expected, XgSim.ForgetNightRecall(answer)); }

        [Test] public void PendingExchangeDoesNotBlockExamOrFollowThePlayerIntoStageFive()
        {
            var sim = Ready(); var request = sim.BeginForgetNightRequest(sim.ForgetNightChoice(false));
            Assert.IsNotNull(sim.TakeExam(), "the optional conversation is not an exam prerequisite");
            sim.S.stage = 5;
            int id = sim.RegisterYyReply(request.Question, "记住了");
            Assert.IsFalse(sim.CommitForgetNightReply(request, id, true));
            Assert.IsFalse(sim.OfferForgetNight); Assert.IsFalse(sim.ForgetNightActive);
            Assert.AreEqual(5, sim.S.stage); Assert.IsFalse(sim.S.fnDone);
        }

        [Test] public void NewAndOldSavesNeverInventACompletedMemory()
        {
            var fresh = new XgSim(); Assert.IsFalse(fresh.OfferForgetNight);
            fresh.S.stage = 4; Assert.IsFalse(fresh.OfferForgetNight, "actual exam readiness is required");
            var old = new XgSim(); old.S.fnDone = true; old.S.fnStep = 4; old.S.fnEvidence = null;
            old.NoteChronicle(XgSim.ForgetNightChronicle);
            Assert.IsNull(old.ForgetNightResult);
            Assert.IsFalse(old.Chronicle().Exists(c => c.id == XgSim.ForgetNightChronicle), "a legacy scripted flag is not actual model evidence");
        }
    }
}
