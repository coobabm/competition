using System;
using LingGuangV05.Core.Forum;
using NUnit.Framework;
using UnityEngine;

namespace LingGuangV05.Tests
{
    public sealed class LaoZhouNewsCommentTests
    {
        static readonly string[] Ids = {"lz2_t01_alphago","lz2_t03_tay","lz2_t04_resnet"};
        static readonly DateTime[] Dates = {new DateTime(2016,5,25),new DateTime(2016,6,2),new DateTime(2016,6,6)};
        const string Source = "https://example.org/public-article";
        static bool Share(LaoZhouQaState state,string id,DateTime published,string channel,bool en=false)
            => LaoZhouNewsComments.Record(state,id,channel,published,published,Source,
                LaoZhouNewsComments.SharedText("Public headline",Source,en),en,10);

        [Test] public void OldSaveAndPassiveQueriesNeverCreateShareOrKnowledge()
        {
            // JsonUtility can materialize a null collection as empty; both are old-save inputs.
            foreach(var state in new[]{
                JsonUtility.FromJson<LaoZhouQaState>("{\"newsComments\":null,\"yyLinkSource\":\"\"}"),
                JsonUtility.FromJson<LaoZhouQaState>("{\"yyLinkSource\":\"\"}"),
                new LaoZhouQaState {newsComments=null}})
            {
            LaoZhouQuestions.Repair(state);
            var originalReceipts=state.newsComments;
            foreach(string id in Ids)Assert.IsNull(LaoZhouNewsComments.Receipt(state,id));
            Assert.AreEqual(LaoZhouNewsComments.Tieba,LaoZhouNewsComments.Channel(state));
            Assert.IsTrue(LaoZhouNewsComments.CanShare(state,Ids[0],LaoZhouNewsComments.Tieba,Dates[0],Dates[0]));
            Assert.AreSame(originalReceipts,state.newsComments,"Passive queries do not create or replace receipts");
            Assert.AreEqual(0,state.newsComments?.Count??0);
            Assert.IsEmpty(state.facts);
            Assert.IsFalse(LaoZhouQuestions.Linked(state));
            }
        }
        [Test] public void OnlyExplicitAcceptanceChoosesYyAndCannotSwitchBehindDisplayedButton()
        {
            foreach(string source in new[]{"","legacy_existing","unrecognized"})
            {
                var state = new LaoZhouQaState {yyLinkSource=source};
                Assert.AreEqual(LaoZhouNewsComments.Tieba,LaoZhouNewsComments.Channel(state));
                Assert.IsFalse(Share(state,Ids[0],Dates[0],LaoZhouNewsComments.Yy));
                Assert.IsTrue(Share(state,Ids[0],Dates[0],LaoZhouNewsComments.Tieba));
                Assert.AreEqual(source,state.yyLinkSource);
            }
            var accepted = new LaoZhouQaState {yyInvitationSent=true};
            Assert.IsTrue(LaoZhouQuestions.Accept(accepted,5));
            Assert.IsFalse(Share(accepted,Ids[0],Dates[0],LaoZhouNewsComments.Tieba));
            Assert.IsTrue(Share(accepted,Ids[0],Dates[0],LaoZhouNewsComments.Yy));
        }
        [Test] public void PublicationDateAndHistoricalBoundaryBothApply()
        {
            for(int i=0;i<Ids.Length;i++)
            {
                var state = new LaoZhouQaState();
                Assert.IsFalse(LaoZhouNewsComments.CanShare(state,Ids[i],LaoZhouNewsComments.Tieba,Dates[i],Dates[i].AddDays(-1)));
                Assert.IsTrue(LaoZhouNewsComments.CanShare(state,Ids[i],LaoZhouNewsComments.Tieba,Dates[i],Dates[i]));
                Assert.IsFalse(LaoZhouNewsComments.CanShare(state,Ids[i],LaoZhouNewsComments.Tieba,new DateTime(2017,1,1),new DateTime(2017,1,2)));
            }
            foreach(string id in Ids)Assert.IsFalse(LaoZhouNewsComments.CanShare(new LaoZhouQaState(),id,LaoZhouNewsComments.Tieba,new DateTime(2015,1,1),Dates[2]));
            Assert.IsFalse(LaoZhouNewsComments.CanShare(new LaoZhouQaState(),Ids[0],LaoZhouNewsComments.Tieba,new DateTime(2016,3,15),Dates[2]));
            Assert.IsFalse(LaoZhouNewsComments.CanShare(new LaoZhouQaState(),Ids[1],LaoZhouNewsComments.Tieba,new DateTime(2016,3,25),Dates[2]));
        }
        [Test] public void EachArticleIsOnceAcrossLanguageAndLaterContactAcceptance()
        {
            var state = new LaoZhouQaState {newsComments=null};
            for(int i=0;i<Ids.Length;i++)Assert.IsTrue(Share(state,Ids[i],Dates[i],LaoZhouNewsComments.Tieba));
            var restored = JsonUtility.FromJson<LaoZhouQaState>(JsonUtility.ToJson(state));
            restored.yyInvitationSent=true;Assert.IsTrue(LaoZhouQuestions.Accept(restored,20));
            for(int i=0;i<Ids.Length;i++)foreach(bool en in new[]{false,true})
                Assert.IsFalse(Share(restored,Ids[i],Dates[i],LaoZhouNewsComments.Yy,en));
            Assert.AreEqual(3,restored.newsComments.Count);
            Assert.AreEqual(LaoZhouNewsComments.Tieba,restored.newsComments[0].channel);
            Assert.AreEqual(10,restored.newsComments[0].sharedAt);
            Assert.AreEqual(Source,restored.newsComments[0].sourceUrl);
            Assert.IsTrue(Share(new LaoZhouQaState(),Ids[0],Dates[0],LaoZhouNewsComments.Tieba));
        }
        [Test] public void InvalidCommitNeverConsumesArticle()
        {
            var state = new LaoZhouQaState();
            Assert.IsFalse(Share(state,"lz2_t02_xiaoice",Dates[0],LaoZhouNewsComments.Tieba));
            Assert.IsFalse(Share(state,Ids[0],Dates[0],"public_unread"));
            foreach(double at in new[]{-1,double.NaN,double.PositiveInfinity})
                Assert.IsFalse(LaoZhouNewsComments.Record(state,Ids[0],LaoZhouNewsComments.Tieba,Dates[0],Dates[0],Source,"title",false,at));
            Assert.IsFalse(LaoZhouNewsComments.Record(state,Ids[0],LaoZhouNewsComments.Tieba,Dates[0],Dates[0],"","title",false,10));
            Assert.IsFalse(LaoZhouNewsComments.Record(state,Ids[0],LaoZhouNewsComments.Tieba,Dates[0],Dates[0],Source,"",false,10));
            Assert.IsEmpty(state.newsComments);
            Assert.IsTrue(Share(state,Ids[0],Dates[0],LaoZhouNewsComments.Tieba));
        }
        [Test] public void BilingualDialogueReferencesOnlyThePublicTopic()
        {
            foreach(string id in Ids)foreach(bool en in new[]{false,true})
            {
                string reply=LaoZhouNewsComments.Reply(id,en);
                Assert.IsNotEmpty(reply);
                foreach(string forbidden in new[]{"你训练","你的程序","你读","阶段","your training","your program","you read","stage"})
                    StringAssert.DoesNotContain(forbidden,reply);
                string shared=LaoZhouNewsComments.SharedText("PUBLIC_HEADLINE",Source,en);
                StringAssert.Contains("PUBLIC_HEADLINE",shared);StringAssert.Contains(Source,shared);
                foreach(string channel in new[]{LaoZhouNewsComments.Yy,LaoZhouNewsComments.Tieba})
                    Assert.IsNotEmpty(LaoZhouNewsComments.Label(channel,false,en));
            }
            Assert.AreEqual("",LaoZhouNewsComments.Reply("unknown",false));
        }
    }
}
