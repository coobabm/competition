using LingGuangV05.Core.Story;
using LingGuangV05.Core;
using NUnit.Framework;

namespace LingGuangV05.Tests
{
    public sealed class EraAdvantageStoryTests
    {
        static StoryState Begun()
        {
            var s = new StoryState(); s.MarkFired("era_delivery_brief");
            Assert.IsTrue(EraAdvantageStory.BeginBatch(s, true)); return s;
        }
        [Test] public void BriefAndCapabilityAreRequiredAndStartDoesNotConnectOrDeliver()
        {
            var s = new StoryState(); Assert.IsFalse(EraAdvantageStory.BeginBatch(s, true));
            s.MarkFired("era_delivery_brief"); Assert.IsFalse(EraAdvantageStory.BeginBatch(s, false));
            Assert.IsTrue(EraAdvantageStory.BeginBatch(s, true));
            Assert.AreEqual(0, s.GetFlag("era.batch.on")); Assert.AreEqual(0,s.GetFlag("era.batch.delivered"));
            Assert.IsFalse(EraAdvantageStory.BeginBatch(s, true));
        }
        [Test] public void NoPowerOrWireMeansNoProgressAndReconnectResumes()
        {
            var s = Begun(); s.SetFlag("era.batch.on",1);
            EraAdvantageStory.AdvanceBatch(s,10,0,1,10); Assert.AreEqual(0,s.GetFlag("era.batch.good"));
            EraAdvantageStory.AdvanceBatch(s,10,1,1,20); Assert.Greater(s.GetFlag("era.batch.good"),0);
            double before=s.GetFlag("era.batch.good"); s.SetFlag("era.batch.on",0);
            EraAdvantageStory.AdvanceBatch(s,10,1,1,30); Assert.AreEqual(before,s.GetFlag("era.batch.good"));
        }
        [Test] public void DraftFilesAccumulateButNeverSendUntilPlayerConfirms()
        {
            var s = Begun(); s.SetFlag("era.batch.on",1);
            EraAdvantageStory.AdvanceBatch(s,100,1,2,100);
            Assert.AreEqual(20,s.GetFlag("era.batch.good")); Assert.AreEqual(1,s.GetFlag("era.batch.ready"));
            Assert.AreEqual(0,s.GetFlag("era.batch.delivered")); Assert.IsNotNull(s.GetFirst("era.delivery.item.01"));
            Assert.IsFalse(EraAdvantageStory.ConfirmDelivery(s)); s.MarkFired("era_delivery_ready");
            Assert.IsTrue(EraAdvantageStory.ConfirmDelivery(s)); Assert.IsFalse(EraAdvantageStory.ConfirmDelivery(s));
            string receipt=s.GetFirst("era.delivery.receipt").text;
            EraAdvantageStory.AdvanceBatch(s,100,1,3,500); Assert.AreEqual(receipt,s.GetFirst("era.delivery.receipt").text);
        }
        [Test] public void AmbiguousDetailsStayOutOfTheApprovedBatch()
        {
            var s = Begun(); s.SetFlag("era.batch.on",1); EraAdvantageStory.AdvanceBatch(s,100,1,2,100);
            Assert.IsNotNull(s.GetFirst("era.delivery.review"));
            StringAssert.Contains("不在已确认交付",s.GetFirst("era.delivery.review").text);
            Assert.AreEqual(20,s.GetFlag("era.batch.good"));
        }
        [Test] public void PlayerChoicesNeedTheirSceneAndAreOneShot()
        {
            var s=new StoryState(); Assert.IsFalse(EraAdvantageStory.ChooseIdentity(s,1));
            s.SetFlag("era.batch.delivered",1);s.MarkFired("era_delivery_confirmed");
            Assert.IsTrue(EraAdvantageStory.ChooseIdentity(s,2));Assert.IsFalse(EraAdvantageStory.ChooseIdentity(s,1));
            Assert.IsFalse(EraAdvantageStory.ChooseContext(s,1));s.MarkFired("era_context_brief");
            Assert.IsTrue(EraAdvantageStory.ChooseContext(s,2));Assert.AreEqual(2,s.GetFlag("era.context.choice"));
        }
        [Test] public void ExpertNeedsInvitationAndANewManualAssessment()
        {
            var s = new StoryState(); Assert.IsFalse(EraAdvantageStory.RequestExpert(s,5));
            s.SetFlag("era.batch.delivered",1); Assert.IsTrue(EraAdvantageStory.RequestExpert(s,5));
            Assert.IsFalse(EraAdvantageStory.RecordExam(s,"translate",19,20,"本次翻译新题：95.0%",true));
            s.MarkFired("era_expert_invite"); Assert.IsFalse(EraAdvantageStory.RecordExam(s,"translate",19,20,"非独立旧题：95.0%",false));
            Assert.IsFalse(EraAdvantageStory.RecordExam(s,"translate",14,20,"70%",true));
            Assert.IsFalse(EraAdvantageStory.RecordExam(s,"mnist",20,20,"数字旧模型",true));
            Assert.IsTrue(EraAdvantageStory.RecordExam(s,"translate",19,20,"本次翻译新题：95.0%",true));
            Assert.AreEqual(1,s.GetFlag("era.expert.passed"));StringAssert.Contains("95.0",s.GetFirst("era.expert.receipt").text);
        }
        [Test] public void ChallengeDoesNotAutoPassFromOldScoresOrBadNumbers()
        {
            var s=new StoryState(); Assert.IsFalse(EraAdvantageStory.RequestChallenge(s,3));
            Assert.IsTrue(EraAdvantageStory.RequestChallenge(s,4));
            Assert.IsFalse(EraAdvantageStory.RecordExam(s,"logic",30,20,"非法计数",true));
            Assert.IsFalse(EraAdvantageStory.RecordExam(s,"logic",20,20,"旧题",false));
            Assert.IsTrue(EraAdvantageStory.RecordExam(s,"logic",17,20,"85%",true));
            Assert.AreEqual(1,s.GetFlag("era.challenge.passed"));
        }
        [Test] public void PersonalIntroductionPrecedesTheDetailedBatchBrief()
        {
            var dir=new System.IO.DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while(dir!=null&&!System.IO.Directory.Exists(System.IO.Path.Combine(dir.FullName,"Assets","LingGuangV05")))dir=dir.Parent;
            Assert.IsNotNull(dir);
            var library=new StoryLibrary();
            foreach(string file in System.IO.Directory.GetFiles(System.IO.Path.Combine(dir.FullName,"Assets/LingGuangV05/Resources/LingGuangV05/Story"),"*.json"))library.Add(System.IO.File.ReadAllText(file),file);
            var state=new StoryState();
            var game=new ChapterOneStoryVars(()=>new ChapterOneSim(),name=>name=="labStage"||name=="labSequenceStage"?5:(double?)0);
            var context=new StoryContext(game,state);context.SetExternal("era.translateReady",1);
            var engine=new StoryEngine(library);var matches=new System.Collections.Generic.List<StoryBeat>();
            engine.Evaluate("state",null,context,null,matches);
            Assert.IsTrue(matches.Exists(x=>x.Id=="cousin_translation_order"));Assert.IsFalse(matches.Exists(x=>x.Id=="era_delivery_brief"));
            state.MarkFired("cousin_translation_order");engine.Evaluate("state",null,context,null,matches);
            Assert.IsTrue(matches.Exists(x=>x.Id=="era_delivery_brief"));
        }
        [Test] public void ReconstructedFlagsResumeWithoutDuplicatingDocuments()
        {
            var s=Begun();s.SetFlag("era.batch.on",1);EraAdvantageStory.AdvanceBatch(s,7,1,1,7);
            var restored=new StoryState { flags=s.flags, firsts=s.firsts, fired=s.fired };restored.Repair();
            EraAdvantageStory.AdvanceBatch(restored,100,1,2,100);
            Assert.AreEqual(20,restored.GetFlag("era.batch.good"));
            Assert.AreEqual(22,restored.firsts.Count); // 20 drafts + the brief + the review list
        }
    }
}
