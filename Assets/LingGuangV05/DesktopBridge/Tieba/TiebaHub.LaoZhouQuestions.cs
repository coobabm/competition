using System;
using LingGuangV05.Core;
using LingGuangV05.Core.Forum;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using UnityEngine;

namespace LingGuangV05.Desktop.Tieba
{
    public sealed partial class TiebaHub
    {
        public LaoZhouQaState ZhouQa => S == null ? null : S.laoZhouQa;
        public bool ZhouYyLinked => LaoZhouQuestions.Linked(ZhouQa);
        public bool ZhouStateBoundTo(ChapterOneSim sim) => ReferenceEquals(linked,sim);
        float nextQaCheck;
        bool legacyQaSave;
        void RegisterZhouThread()
        {
            if(Library.Get(LaoZhouQuestions.ThreadId)!=null)return;
            var t=new ForumThread{id=LaoZhouQuestions.ThreadId,board="ai",author=ForumLibrary.LaoZhou,
                title=LaoZhouQuestions.Title(false),titleEn=LaoZhouQuestions.Title(true),date=GameCalendar.Start.Date.AddDays(-3),minStage=1};
            t.floors.Add(new ForumFloor{author=ForumLibrary.LaoZhou,text=LaoZhouQuestions.Body(false),textEn=LaoZhouQuestions.Body(true)});
            Library.Threads.Add(t);
        }
        void RepairZhouQa()
        {
            // A persisted pre-feature YY conversation may establish legacy contact, never an acceptance or secret access.
            string priorForum = runtime?.Sim?.S.forumState ?? "";
            legacyQaSave = priorForum.IndexOf("\"laoZhouQa\"",StringComparison.Ordinal)<0
                && !string.IsNullOrEmpty(runtime?.Sim?.S.chatState);
            if(S.laoZhouQa==null)S.laoZhouQa=new LaoZhouQaState();
            LaoZhouQuestions.Repair(ZhouQa);nextQaCheck=0;
            // Only direct player lines are recoverable evidence. NPC assertions and private game fields are not.
            var c=S.Conversation(ForumLibrary.LaoZhou);
            for(int i=0;i<c.messages.Count;i++)if(c.messages[i].from==ForumLibrary.Me)
                LaoZhouQuestions.Observe(ZhouQa,"tieba:"+i+":"+c.messages[i].gameSeconds,"tieba_dm",c.messages[i].text,c.messages[i].gameSeconds);
        }
        public void ZhouObserveYy(string text,double at,int index)
        {
            if(ZhouQa!=null)LaoZhouQuestions.Observe(ZhouQa,"yy:"+index+":"+at,"yy_dm",text,at);
        }
        // The displayed choices are derived from persisted state, so reloads never lose an unanswered option.
        public string[] ZhouChoices(string id)
        {
            if(ZhouQa==null||runtime==null||runtime.Sim==null||runtime.Sim.InPrologue)return null;
            if(id==ForumLibrary.LaoZhou&&ZhouQa.yyInvitationSent&&!ZhouYyLinked)return new[]{LaoZhouQuestions.AcceptLabel(GameText.IsEnglish)};
            if(id==LaoZhouQuestions.DiscussionId&&ZhouQa.first.choice==0)return LaoZhouQuestions.Choices(1,GameText.IsEnglish);
            return null;
        }
        public string[] ZhouYyChoices()
        {
            if(!ZhouYyLinked)return null;
            if(ZhouQa.second.yyOffered&&ZhouQa.second.choice==0)return LaoZhouQuestions.Choices(2,GameText.IsEnglish);
            if(ZhouQa.first.yyOffered&&ZhouQa.first.choice==0)return LaoZhouQuestions.Choices(1,GameText.IsEnglish);
            return null;
        }
        void ZhouForumLine(string conversation,string from,string text)
        {
            var c=S.Conversation(conversation);c.messages.Add(new ForumMessage{from=from,text=text,gameSeconds=Now});
            if(from!=ForumLibrary.Me&&!IsShowing(conversation))c.unread++;
        }
        public bool TryZhouForumChoice(string id,string line)
        {
            if(ZhouQa==null||runtime==null||runtime.Sim==null||runtime.Sim.InPrologue)return false;
            bool en=GameText.IsEnglish;
            if(id==ForumLibrary.LaoZhou&&(line==LaoZhouQuestions.AcceptLabel(false)||line==LaoZhouQuestions.AcceptLabel(true)))
            {
                if(!ZhouQa.yyInvitationSent)return false;
                if(!LaoZhouQuestions.Accept(ZhouQa,Now))return true;
                ZhouForumLine(id,ForumLibrary.Me,line);
                LaoZhouQuestions.Observe(ZhouQa,"yy_accept","tieba_dm",line,Now);
                var yy=YYChatHub.Instance;
                if(yy!=null&&yy.S!=null)yy.NoteZhouLinked();
                Touch();return true;
            }
            int choice=id==LaoZhouQuestions.DiscussionId?LaoZhouQuestions.ChoiceIndex(1,line):0;
            if(choice==0)return false;
            if(!LaoZhouQuestions.Choose(ZhouQa,1,choice,"tieba_public",Now))return true;
            ZhouForumLine(id,ForumLibrary.Me,line);
            // He reads this reply while answering his own thread; the same forum account establishes identity.
            LaoZhouQuestions.Observe(ZhouQa,"q1_public","tieba_public_read_linked",line,Now);
            ZhouQa.first.responseSent=true;
            ZhouForumLine(id,ForumLibrary.LaoZhou,LaoZhouQuestions.Response(1,choice,en));
            foreach(var text in LaoZhouQuestions.Closing(1,en))ZhouForumLine(id,ForumLibrary.LaoZhou,text);
            Touch();return true;
        }
        public bool TryZhouYyChoice(string line)
        {
            if(!ZhouYyLinked)return false;
            var yy=YYChatHub.Instance;if(yy==null||yy.S==null)return false;
            for(int number=1;number<=2;number++)
            {
                var q=LaoZhouQuestions.Question(ZhouQa,number);int choice=LaoZhouQuestions.ChoiceIndex(number,line);
                if(choice==0||!q.yyOffered&&q.choice==0)continue;
                if(!LaoZhouQuestions.Choose(ZhouQa,number,choice,"yy",Now))return true;
                yy.AppendZhouAuthored(YYChatHub.Me,line);
                LaoZhouQuestions.Observe(ZhouQa,"q"+number+"_yy","yy_dm",line,Now);
                q.responseSent=true;
                yy.AppendZhouAuthored(YYChatHub.LaoZhou,LaoZhouQuestions.Response(number,choice,GameText.IsEnglish));
                foreach(var text in LaoZhouQuestions.Closing(number,GameText.IsEnglish))yy.AppendZhouAuthored(YYChatHub.LaoZhou,text);
                yy.RefreshZhouAuthored();Touch();return true;
            }
            return false;
        }
        bool ZhouIdle()
        {
            if(StoryFocus.Active||InnerVoice.Busy||AiJoinsYy.Playing||AutoLabelEpiphany.Playing)return false;
            var lab=Lab();if(lab==null||lab.ReadingBook!=null||lab.CardLoanActive&&ZhouYyLinked||lab.RelicPickPending||lab.S.chapterComplete||!string.IsNullOrEmpty(lab.S.ending))return false;
            var view=XingGuangController.Instance?.View;
            if(view!=null&&(view.SetupShowing||view.Holo!=null&&view.Holo.Showing))return false;
            var train=view?.Train;
            if(train!=null&&(train.ExamOpen||train.DiagnosisOpen||train.AssessmentBlocksInput))return false;
            var yy=YYChatHub.Instance;
            return yy==null||!yy.IsTyping(YYChatHub.LaoZhou)&&yy.ChoicesFor!=YYChatHub.LaoZhou;
        }
        void TickZhouQuestions()
        {
            if(ZhouQa==null||Time.unscaledTime<nextQaCheck)return;nextQaCheck=Time.unscaledTime+.5f;
            var yy=YYChatHub.Instance;
            // Wait for both hubs to bind the same save before classifying legacy contacts.
            if(yy!=null&&yy.BoundTo(runtime.Sim)&&!ZhouQa.legacyChecked)
            {
                LaoZhouQuestions.MigrateLegacy(ZhouQa,legacyQaSave,yy.Conversation(YYChatHub.LaoZhou).messages.Count>0);
                if(ZhouYyLinked){var c=yy.Conversation(YYChatHub.LaoZhou);for(int i=0;i<c.messages.Count;i++)if(c.messages[i].from==YYChatHub.Me&&!c.messages[i].byAi)ZhouObserveYy(c.messages[i].text,c.messages[i].gameSeconds,i);}
                Touch();
            }
            if(runtime.TestMode||runtime.Sim.InPrologue||!ZhouIdle())return;
            if(!ZhouYyLinked&&!ZhouQa.yyInvitationSent)
            {
                ZhouQa.yyInvitationSent=true;
                ZhouForumLine(ForumLibrary.LaoZhou,ForumLibrary.LaoZhou,LaoZhouQuestions.Invitation(GameText.IsEnglish));Touch();return;
            }
            if(!ZhouYyLinked||yy==null||!yy.BoundTo(runtime.Sim)||!yy.AppInstalled)return;
            yy.NoteZhouLinked();
            if(ZhouQa.first.choice>0&&ZhouQa.first.channel=="tieba_public"&&!ZhouQa.first.yyContinued)
            {
                ZhouQa.first.yyContinued=true;
                yy.AppendZhouAuthored(YYChatHub.LaoZhou,LaoZhouQuestions.T(GameText.IsEnglish,"你在那个帖子里的回复，我后来又想了想。","I've been thinking some more about your reply to that thread."));
                yy.AppendZhouAuthored(YYChatHub.LaoZhou,"“"+LaoZhouQuestions.Choices(1,GameText.IsEnglish)[ZhouQa.first.choice-1]+"”");
                if(!ZhouQa.first.responseSent){ZhouQa.first.responseSent=true;yy.AppendZhouAuthored(YYChatHub.LaoZhou,LaoZhouQuestions.Response(1,ZhouQa.first.choice,GameText.IsEnglish));}
                else yy.AppendZhouAuthored(YYChatHub.LaoZhou,LaoZhouQuestions.T(GameText.IsEnglish,"下次看到“越聊越聪明”的说法，我得先问问，它到底改了什么。","Next time I see a claim that it gets smarter the more you chat, I'll first ask what actually changed."));
                yy.RefreshZhouAuthored();Touch();return;
            }
            int due=LaoZhouQuestions.Due(ZhouQa,Lab().S.stage,true);if(due==0)return;
            LaoZhouQuestions.Question(ZhouQa,due).yyOffered=true;
            foreach(var text in LaoZhouQuestions.Opening(due,ZhouQa.first.choice>0,GameText.IsEnglish))yy.AppendZhouAuthored(YYChatHub.LaoZhou,text);
            yy.RefreshZhouAuthored();Touch();
        }
    }
}
