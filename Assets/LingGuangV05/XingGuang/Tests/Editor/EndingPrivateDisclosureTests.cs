using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class EndingPrivateDisclosureTests
    {
        static XgState Planned() => new XgState { stage=6, endingPreparation=new XgEndingPreparationState
            { phase="asked", siReadReceipt="si:read" } };
        static XgState Released(string route="local_only",string owner="player",string pin="stop")
        {
            var s=Planned();s.releaseOutcome=new ReleaseOutcome { route=route,decisionOwner=owner,shutdownPin=pin,
                releaseReceiptId="release:one",artifactId=route=="local_only"?"":"artifact:one",committedAt=20161231,
                prerequisites=new XgReleasePrerequisites { memoryCardsReceiptId="memory",redTeamOneReceiptId="one",
                    redTeamTwoReceiptId="two",redTeamThreeReceiptId="three",fixedThreeSlotsReceiptId="pins",
                    aiRouteDecisionReceiptId=owner=="ai"?"decision:one":"",aiChosenRoute=owner=="ai"?route:"" } };
            return s;
        }
        static EndingPrivateReceipt Send(EndingPrivateState s,EndingPrivateOffer o,string id="message:one")
            => EndingPrivateDisclosure.Receive(s,o,id,42);
        static EndingLetterCatalog Catalog() => new EndingLetterCatalog(JsonUtility.FromJson<EndingLetterResource>(
            Resources.Load<TextAsset>(EndingLetterCatalog.ResourcePath).text));

        [TestCase("tieba_dm_accept",true)][TestCase("legacy_existing",false)][TestCase("",false)][TestCase(null,false)]
        public void OnlyPlayerAcceptedYyContactAuthorizesNewPrivateDisclosure(string source,bool allowed)
            => Assert.AreEqual(allowed,EndingPrivateDisclosure.ExplicitYyContact(source));

        [Test] public void NoContactOrJustStageNeverAuthorizesDisclosure()
        {
            Assert.IsEmpty(EndingPrivateDisclosure.Offers(new XgState {stage=6},null,true,false));
            Assert.IsEmpty(EndingPrivateDisclosure.Offers(Planned(),null,false,false));
            Assert.IsNull(EndingPrivateDisclosure.PlannedFact(null));
        }
        [Test] public void PreviewDoesNotMutateStateOrRevealPlannedFact()
        {
            var lab=Planned();var state=new EndingPrivateState();string before=JsonUtility.ToJson(lab);
            Assert.AreEqual(3,EndingPrivateDisclosure.Offers(lab,state,true,false).Count);
            Assert.IsEmpty(state.receipts);Assert.IsNull(EndingPrivateDisclosure.PlannedFact(state));
            Assert.AreEqual(before,JsonUtility.ToJson(lab));
        }
        [TestCase("online","zhou_open_door","LP1")]
        [TestCase("public_model","zhou_copies","LP2")]
        [TestCase("local_only","zhou_unpressed_button","LP4")]
        public void OnlyActualPlannedReceiptEnablesMatchingAppendix(string route,string article,string appendix)
        {
            var state=new EndingPrivateState();var offer=EndingPrivateDisclosure.Offers(Planned(),state,true,false).Single(o=>o.route==route);
            Assert.IsNotNull(Send(state,offer));var fact=EndingPrivateDisclosure.PlannedFact(state);
            Assert.AreEqual("yy:ending:message:one",fact.sourceRef);Assert.AreEqual("planned",fact.timing);
            Assert.AreEqual(appendix,Catalog().EligibleAppendix(article,fact)?.id);
            foreach(var id in EndingLetterCatalog.ArticleIds.Where(id=>id!=article))Assert.IsNull(Catalog().EligibleAppendix(id,fact));
            Assert.IsNull(Send(state,offer,"duplicate"));Assert.AreEqual(1,state.receipts.Count);
            Assert.AreEqual(42,state.receipts[0].receivedAt);
        }
        [Test] public void LatestPlanWinsAndReceiptSurvivesSaveReload()
        {
            var state=new EndingPrivateState();var offers=EndingPrivateDisclosure.Offers(Planned(),state,true,true);
            Send(state,offers[0]);Send(state,offers[2],"message:two");
            var loaded=JsonUtility.FromJson<EndingPrivateState>(JsonUtility.ToJson(state));
            Assert.AreEqual("disclosed_delay",EndingPrivateDisclosure.PlannedFact(loaded).evidenceKey);
        }
        [Test] public void LegacyAndIncompleteEvidenceAreNotUpgraded()
        {
            var legacy=JsonUtility.FromJson<EndingPrivateState>("{}");Assert.IsNull(EndingPrivateDisclosure.PlannedFact(legacy));
            var state=new EndingPrivateState();state.receipts.Add(new EndingPrivateReceipt{kind="plan:online",text="I plan it",authorBound=true,receivedByZhou=true});
            Assert.IsNull(EndingPrivateDisclosure.PlannedFact(state));
        }
        [Test] public void AlreadyCompletedRouteCannotManufacturePlannedWording()
        {
            var state=new EndingPrivateState();var offer=EndingPrivateDisclosure.Offers(Released(),state,true,false).Single();
            Send(state,offer);Assert.IsNull(EndingPrivateDisclosure.PlannedFact(state));
            Assert.AreEqual(3,state.receipts[0].replies.Length);
        }
        [Test] public void OnlineRequiresItsActualFinishedNightReceipt()
        {
            var lab=Released("online");Assert.IsEmpty(EndingPrivateDisclosure.Offers(lab,null,true,false));
            lab.releaseOutcome.launchNight=new XgLaunchNightReceipt {receiptId="night",sourceRef="night:source",releaseReceiptId="release:one",
                artifactId="artifact:one",ending="power_cut",endedAt=20161231};
            Assert.AreEqual(1,EndingPrivateDisclosure.Offers(lab,null,true,false).Count);
            StringAssert.DoesNotContain("一晚",EndingPrivateDisclosure.Offers(lab,null,true,false)[0].text);
            StringAssert.DoesNotContain("for a night",EndingPrivateDisclosure.Offers(lab,null,true,true)[0].text);
        }
        [TestCase("stop")][TestCase("no_stop")][TestCase("unpinned")]
        public void RouteDisclosureNeverRevealsShutdownWithoutSeparateSend(string pin)
        {
            var lab=Released(pin:pin);var state=new EndingPrivateState();var route=EndingPrivateDisclosure.Offers(lab,state,true,false).Single();
            Assert.AreEqual("route",route.kind);Assert.IsFalse(route.replies.Contains(EndingPrivateDisclosure.ShutdownReply(pin,false)));
            Send(state,route);Assert.IsFalse(EndingPrivateDisclosure.Has(state,"shutdown","release:one"));
            var shutdown=EndingPrivateDisclosure.Offers(lab,state,true,false).Single();Assert.AreEqual("shutdown",shutdown.kind);
            Send(state,shutdown,"message:two");Assert.IsEmpty(EndingPrivateDisclosure.Offers(lab,state,true,false));
        }
        [Test] public void AiAnswerMustBeExplicitlyForwardedAndMatchCommittedDecision()
        {
            var lab=Released(owner:"ai");lab.endingPreparation.aiDecision=new XgEndingAiDecision { id="decision:one",route="local_only",line="我想留下。",lineEn="I want to stay." };
            var state=new EndingPrivateState();var ask=EndingPrivateDisclosure.Offers(lab,state,true,false).Single();
            Assert.AreEqual("ask",ask.kind);Assert.AreEqual("……它怎么说的？",ask.replies[2]);Send(state,ask);
            var answer=EndingPrivateDisclosure.Offers(lab,state,true,false).Single(o=>o.kind=="answer");
            StringAssert.EndsWith(lab.endingPreparation.aiDecision.line,answer.text);Assert.AreEqual(1,state.receipts.Count);
            lab.endingPreparation.aiDecision.id="wrong";Assert.IsFalse(EndingPrivateDisclosure.Offers(lab,state,true,false).Any(o=>o.kind=="answer"));
        }
        [Test] public void CompletedArchivesOfferNoNewDisclosureAndProjectionIsReadOnly()
        {
            var lab=Released();lab.chapterComplete=true;var state=new EndingPrivateState();
            Assert.IsEmpty(EndingPrivateDisclosure.Offers(lab,state,true,false));
            string before=JsonUtility.ToJson(state);EndingPrivateDisclosure.PlannedFact(state);Assert.AreEqual(before,JsonUtility.ToJson(state));
        }
        [Test] public void LetterBindingCopiesItsEvidenceAndNeverEditsPublicText()
        {
            var state=new EndingPrivateState();Send(state,EndingPrivateDisclosure.Offers(Planned(),state,true,false)[0]);
            var input=new EndingLetterInput {personalDisclosure=EndingPrivateDisclosure.PlannedFact(state)};var copy=input.Copy();
            input.personalDisclosure.sourceRef="mutated";Assert.AreNotEqual(input.personalDisclosure.sourceRef,copy.personalDisclosure.sourceRef);
            var catalog=Catalog();var article=catalog.Get("zhou_open_door");string before=JsonUtility.ToJson(article);
            Assert.IsNotNull(catalog.EligibleAppendix(article.id,copy.personalDisclosure));Assert.AreEqual(before,JsonUtility.ToJson(article));Assert.AreEqual(9,article.paragraphs.Length);
        }
        [Test] public void FixedDialogueContainsExactlyEighteenBilingualLines()
        {
            foreach(bool en in new[]{false,true})
            {
                int count=0;foreach(string route in new[]{"online","public_model","ask","local_only"})
                {var lines=EndingPrivateDisclosure.RouteReplies(route,en);Assert.AreEqual(3,lines.Length);foreach(var line in lines)Assert.IsNotEmpty(line);count+=lines.Length;}
                foreach(string route in new[]{"online","public_model","local_only"}){Assert.IsNotEmpty(EndingPrivateDisclosure.AiReply(route,en));count++;}
                foreach(string pin in new[]{"stop","no_stop","unpinned"}){Assert.IsNotEmpty(EndingPrivateDisclosure.ShutdownReply(pin,en));count++;}
                Assert.AreEqual(18,count);
            }
        }
    }
}
