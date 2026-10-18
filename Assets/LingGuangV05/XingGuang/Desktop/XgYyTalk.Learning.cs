using System;
using System.Collections.Generic;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEngine;

namespace LingGuangV05.Desktop.XingGuang
{
    public sealed partial class XgYyTalk
    {
        readonly Dictionary<YYMessage,XgAskedEntry> notebookSources = new Dictionary<YYMessage,XgAskedEntry>();
        readonly HashSet<YYMessage> personaSources = new HashSet<YYMessage>();
        XgAskedEntry pendingNotebook;
        string pendingNotebookText;
        string pendingPersona;
        public static XgSim LearningSim => instance != null ? instance.bound : null;
        public static void EnsureLearningData()
        {
            if(XgSim.PersonaPacks.Count>0)return;
            var packs=Resources.Load<TextAsset>("LingGuangV05/YY/persona_packs");
            var unlocks=Resources.Load<TextAsset>("LingGuangV05/YY/chat_unlocks");
            if(packs!=null&&unlocks!=null)XgSim.LoadYyLearning(packs.text,unlocks.text);
        }
        public static bool AskFromNotebook(XgAskedEntry question)
        {
            var talk=instance;var hub=YYChatHub.Instance;
            if(talk==null||hub==null||question==null||hub.Conversation(Id)==null||hub.LingGuangTalk!=talk||talk.Thinking||talk.NightPlaying)return false;
            if(talk.bound==null||!ReferenceEquals(talk.bound.LearningRecord(question.id),question))return false;
            var input=talk.bound.LearningRetestInput(question.id);
            if(input==null||input.requiresImage)return false;
            talk.pendingNotebook=question;
            talk.pendingNotebookText=GameText.T(input.question,string.IsNullOrEmpty(input.questionEn)?input.question:input.questionEn);
            try{hub.Send(Id,talk.pendingNotebookText);return true;}
            finally{talk.pendingNotebook=null;talk.pendingNotebookText=null;}
        }
        public static bool AskPersona(XgPersonaPack pack,XgPersonaQuestion question)
        {
            var talk=instance;var hub=YYChatHub.Instance;var sim=LearningSim;
            if(talk==null||hub==null||sim==null||hub.Conversation(Id)==null||hub.LingGuangTalk!=talk||pack==null||question==null||talk.Thinking||talk.NightPlaying||!sim.OwnsPersonaPack(pack.id)||sim.S.stage<question.stage)return false;
            talk.pendingPersona=GameText.T(question.q,question.qEn);hub.Send(Id,talk.pendingPersona);talk.pendingPersona=null;return true;
        }
        void RememberLearningSource(YYMessage message)
        {
            if(pendingNotebook!=null&&message.text==pendingNotebookText)notebookSources[message]=pendingNotebook;
            if(pendingPersona!=null&&message.text==pendingPersona)personaSources.Add(message);
        }
        void LearningDelivered(Reply reply,YYChatHub hub)
        {
            if(reply.asked==null||reply.asked.byAi)return;
            var conv=hub.Conversation(Id);if(conv==null||conv.messages.Count==0)return;
            var message=conv.messages[conv.messages.Count-1];
            if(message.from!=Id||message.text!=reply.text||message.learningReplyId>0)return;
            // The user's raw line is preserved: normalization for model input is never used for unlock matching.
            message.learningReplyId=bound.RegisterYyReply(string.IsNullOrEmpty(reply.asked.rawPlayerText)?reply.asked.text:reply.asked.rawPlayerText,reply.text);
            message.learningPersona=personaSources.Remove(reply.asked);
            if(notebookSources.TryGetValue(reply.asked,out var source))
            {
                notebookSources.Remove(reply.asked);
                bound.NoteNotebookReply(reply.text);
                bound.RecordLearningRetest(source.id,reply.text,"yy:"+message.learningReplyId);
                string reference=AnswerToken(source.playerAnswer),answer=AnswerToken(reply.text);
                if(reference.Length>0&&answer.Length>0)message.learningComparison=reference==answer?1:-1;
            }
            if(controller.runtime!=null)controller.runtime.MarkDirty();
        }
        static string AnswerToken(string text)
        {
            string s=(text??"").Trim().Trim('。','！','!','.',' ');
            if(s=="是"||s=="对"||s=="正确"||s.Equals("yes",StringComparison.OrdinalIgnoreCase)||s.Equals("true",StringComparison.OrdinalIgnoreCase))return "yes";
            if(s=="否"||s=="错"||s=="错误"||s.Equals("no",StringComparison.OrdinalIgnoreCase)||s.Equals("false",StringComparison.OrdinalIgnoreCase))return "no";
            return s;
        }
        void OnLearningUnlocked(string node)
        {
            var sim=bound;if(sim==null)return;
            LingGuangV05.Desktop.Story.PrologueDirector.Desk?.StoryPopup(sim.AiDisplayName,GameText.T("对上了 · 科技节点可以购买了","Matched · a tech node is now available"),5);
        }
        public static bool TagLearningReply(int replyId,string tagId,out string feedback)
        {
            feedback="";var talk=instance;if(talk==null||talk.bound==null)return false;
            if(!talk.bound.TagYyReply(replyId,tagId,out feedback))return false;
            if(talk.controller.runtime!=null)talk.controller.runtime.MarkDirty();return true;
        }
        public static void OpenLearningTech()
        {
            var talk=instance;if(talk==null||talk.controller==null)return;
            talk.controller.Open();
            if(talk.controller.View!=null)talk.controller.View.ShowTab("tree");
        }
    }
}
