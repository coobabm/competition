using System;
using System.Collections.Generic;
using System.Text;
using LingGuangV05.Core.Story;

namespace LingGuangV05.XingGuang
{
    [Serializable] public sealed class XgAskedEntry
    {
        public string bankId, question, playerAnswer, aiAnswer;
        public double at; // Legacy elapsed clock; retained but never displayed as a calendar date.
        public long id, originalId, teachingNoteId;
        public int gameDate;
        public int learningStage; // 0 = older history did not record its stage.
        public double gameSecondsOfDay = -1;
        public string sourceKind = "", eventKey = "", questionEn = "", correction = "";
        public string answerSource = ""; // Absent in old saves: retain unknown provenance.
    }
    [Serializable] public sealed class XgPersonaQuestion { public string q, qEn; public int stage; }
    [Serializable] public sealed class XgPersonaPack { public string id, name, nameEn, desc, descEn; public int stage; public double price; public List<XgPersonaQuestion> questions = new List<XgPersonaQuestion>(); }
    [Serializable] public sealed class XgPersonaTag { public string id, name, nameEn, axis, flag; public int delta; }
    [Serializable] public sealed class XgChatUnlock { public string node, phrase, phraseEn, hint, hintEn, answer, answerEn; public int stage; }
    [Serializable] public sealed class XgYyReply { public int id; public string question, answer; public List<string> tags = new List<string>(); }
    [Serializable] public sealed class XgTalkSample { public string tag, text; }
    [Serializable] public sealed class XgUnlockedChat { public string node, answer; }
    public sealed partial class XgState
    {
        public List<XgAskedEntry> askedLog = new List<XgAskedEntry>();
        public List<string> personaPacks = new List<string>();
        public List<XgYyReply> yyReplies = new List<XgYyReply>();
        public List<XgTalkSample> talkSamples = new List<XgTalkSample>();
        public List<XgUnlockedChat> chatUnlocked = new List<XgUnlockedChat>();
        public int yyReplySerial, honestTags, flatterTags;
    }
    public sealed partial class XgSim
    {
        static readonly List<XgPersonaPack> personaCatalog = new List<XgPersonaPack>();
        static readonly List<XgPersonaTag> personaTags = new List<XgPersonaTag>();
        static readonly List<XgChatUnlock> chatUnlockCatalog = new List<XgChatUnlock>();
        public static IReadOnlyList<XgPersonaPack> PersonaPacks => personaCatalog;
        public static IReadOnlyList<XgPersonaTag> PersonaTags => personaTags;
        public static IReadOnlyList<XgChatUnlock> ChatUnlocks => chatUnlockCatalog;
        public event Action<string> ChatNodeUnlocked;
        static string YS(Dictionary<string,object> d,string k) => d != null && d.TryGetValue(k,out var v) ? v as string ?? "" : "";
        static double YN(Dictionary<string,object> d,string k) => d != null && d.TryGetValue(k,out var v) && v is double n ? n : 0;
        static List<object> YA(Dictionary<string,object> d,string k) => d != null && d.TryGetValue(k,out var v) ? v as List<object> : null;
        public static void LoadYyLearning(string personas, string unlocks)
        {
            var root=StoryJson.Parse(personas) as Dictionary<string,object>;
            var packs=YA(root,"packs");var tags=YA(root,"tags");
            var chat=YA(StoryJson.Parse(unlocks) as Dictionary<string,object>,"unlocks");
            if(packs==null||tags==null||chat==null) throw new FormatException("YY learning data is incomplete");
            personaCatalog.Clear();personaTags.Clear();chatUnlockCatalog.Clear();
            var ids=new HashSet<string>();
            foreach(var row in packs)
            {
                var d=(Dictionary<string,object>)row;var p=new XgPersonaPack{id=YS(d,"id"),name=YS(d,"name"),nameEn=YS(d,"nameEn"),desc=YS(d,"desc"),descEn=YS(d,"descEn"),stage=(int)YN(d,"stage"),price=YN(d,"price")};
                if(p.id.Length==0||!ids.Add(p.id))throw new FormatException("Duplicate persona pack");
                foreach(var raw in YA(d,"questions")) {var q=(Dictionary<string,object>)raw;p.questions.Add(new XgPersonaQuestion{q=YS(q,"q"),qEn=YS(q,"qEn"),stage=(int)YN(q,"stage")});}
                p.price = LingGuangV05.Core.PurchasePricing.AtLeastHundred(p.price);
                personaCatalog.Add(p);
            }
            ids.Clear();
            foreach(var row in tags)
            {
                var d=(Dictionary<string,object>)row;var t=new XgPersonaTag{id=YS(d,"id"),name=YS(d,"name"),nameEn=YS(d,"nameEn"),axis=YS(d,"axis"),flag=YS(d,"flag"),delta=(int)YN(d,"delta")};
                if(t.id.Length==0||!ids.Add(t.id))throw new FormatException("Duplicate persona tag");personaTags.Add(t);
            }
            ids.Clear();
            foreach(var row in chat)
            {
                var d=(Dictionary<string,object>)row;var u=new XgChatUnlock{node=YS(d,"node"),stage=(int)YN(d,"stage"),phrase=YS(d,"phrase"),phraseEn=YS(d,"phraseEn"),hint=YS(d,"hint"),hintEn=YS(d,"hintEn"),answer=YS(d,"answer"),answerEn=YS(d,"answerEn")};
                if(u.node=="attention_only")u.node="transformer"; // design data uses the older working title
                if(u.node.Length==0||!ids.Add(u.node))throw new FormatException("Duplicate chat unlock");chatUnlockCatalog.Add(u);
            }
            XgCatalog.ReloadPersonaNodes();
        }
        public void RepairYyLearning()
        {
            RepairLearningRecords();
            if(S.personaPacks==null)S.personaPacks=new List<string>();
            if(S.yyReplies==null)S.yyReplies=new List<XgYyReply>();
            if(S.talkSamples==null)S.talkSamples=new List<XgTalkSample>();
            if(S.chatUnlocked==null)S.chatUnlocked=new List<XgUnlockedChat>();
            foreach(var node in S.unlocked) if(node!=null&&node!=PersonaArchiveNode&&node.StartsWith("persona.",StringComparison.Ordinal)&&!S.personaPacks.Contains(node.Substring(8)))S.personaPacks.Add(node.Substring(8));
            if(!S.personaPacks.Contains("daily"))S.personaPacks.Add("daily");
        }
        public void RecordAsked(string bankId,string question,string playerAnswer,string aiAnswer="",double at=-1)
        {
            if(string.IsNullOrWhiteSpace(question))return;RepairYyLearning();bankId=bankId??"general";
            AppendLearning(new XgAskedEntry{bankId=bankId,answerSource=bankId.StartsWith("train.",StringComparison.Ordinal)?"dataset_label":"",sourceKind=bankId.StartsWith("train.",StringComparison.Ordinal)?"train":"question",question=question,playerAnswer=playerAnswer??"",aiAnswer=aiAnswer??"",at=at<0?S.lifeClock:at});
        }
        public static double PersonaPrice(string id) { var p=personaCatalog.Find(x=>x.id==id);return p==null?double.PositiveInfinity:p.price; }
        public bool OwnsPersonaPack(string id){RepairYyLearning();return S.personaPacks.Contains(id);}
        public string PersonaPackBlocker(string id,IXgHost host)
        {
            var p=personaCatalog.Find(x=>x.id==id);if(p==null)return T("题包不存在","Unknown question pack");
            if(OwnsPersonaPack(id))return T("已拥有","Owned");
            if(S.teachingFlow&&!Has("train.curve"))return T("先买训练曲线","Buy Training curve first");
            if(S.stage<p.stage)return T("第 "+p.stage+" 阶段开放","Opens at stage "+p.stage);
            if(host==null||host.Money<p.price)return T("余额不足","Not enough money");return null;
        }
        public bool BuyPersonaPack(string id,IXgHost host)
        {
            if(PersonaPackBlocker(id,host)!=null)return false;var p=personaCatalog.Find(x=>x.id==id);
            if(p.price>0&&!host.Spend(p.price))return false;
            S.totalSpent+=p.price;S.personaPacks.Add(id);
            string nodeId="persona."+id;if(!S.unlocked.Contains(nodeId))S.unlocked.Add(nodeId);
            Say(T("获得性格题包："+p.name,"Personality pack added: "+p.nameEn));
            var node=XgCatalog.Node(nodeId);if(node!=null)NodeBought?.Invoke(node);return true;
        }
        public bool ChatNodeReady(string node)
        {
            RepairYyLearning();var u=chatUnlockCatalog.Find(x=>x.node==node);return u==null||S.chatUnlocked.Exists(x=>x.node==node);
        }
        public string ChatNodeHint(string node)
        {
            var u=chatUnlockCatalog.Find(x=>x.node==node);return u==null?"":T(u.hint,u.hintEn);
        }
        public string ChatNodePurchaseHint(string node)
        {
            var u=chatUnlockCatalog.Find(x=>x.node==node);
            // MLP is available before the library: its first conversation must be discoverable here.
            if(node=="mlp" && u!=null)
                return T("在 YY 对它发送原句「"+u.phrase+"」，收到回复后可购买。",
                    "In YY, send the exact phrase \""+u.phraseEn+"\" and receive its reply before purchasing.");
            return T("对它说一句话：____。", "Say something to it: ____. ")+ChatNodeHint(node);
        }
        public string ChatNodeAnswer(string node){RepairYyLearning();return S.chatUnlocked.Find(x=>x.node==node)?.answer??"";}
        // Call only once a response to this exact player message has actually been delivered.
        public int RegisterYyReply(string rawQuestion,string answer,bool deliveredSuccessfully=true)
        {
            if(!deliveredSuccessfully||string.IsNullOrWhiteSpace(answer)||string.IsNullOrWhiteSpace(rawQuestion))return 0;
            RepairYyLearning();var reply=new XgYyReply{id=++S.yyReplySerial,question=rawQuestion,answer=answer};S.yyReplies.Add(reply);
            // Keep the short interaction cache bounded; the durable learning record is separate.
            RecordDeliveredLearningReply(reply.id,rawQuestion,answer);
            while(S.yyReplies.Count>50)S.yyReplies.RemoveAt(0);
            foreach(var u in chatUnlockCatalog)
                if(S.stage>=u.stage&&!S.chatUnlocked.Exists(x=>x.node==u.node)&&(string.Equals(rawQuestion,u.phrase,StringComparison.Ordinal)||string.Equals(rawQuestion,u.phraseEn,StringComparison.Ordinal)))
                {S.chatUnlocked.Add(new XgUnlockedChat{node=u.node,answer=answer});ChatNodeUnlocked?.Invoke(u.node);}
            return reply.id;
        }
        // Only explicit continuation of a notebook problem counts, never a casual follow-up question.
        public void NoteNotebookReply(string deliveredAnswer)
        {
            if(S.stage!=6 || string.IsNullOrWhiteSpace(deliveredAnswer)) return;
            string text=deliveredAnswer.Trim();
            bool marker=text.StartsWith("下一题：",StringComparison.Ordinal)||text.StartsWith("下一题:",StringComparison.Ordinal)
                ||text.StartsWith("接着出题：",StringComparison.Ordinal)||text.StartsWith("接着出题:",StringComparison.Ordinal)
                ||text.StartsWith("Next question:",StringComparison.OrdinalIgnoreCase)||text.StartsWith("Next problem:",StringComparison.OrdinalIgnoreCase);
            if(marker&&(text.IndexOf('？')>=0||text.IndexOf('?')>=0)) NoteTrainingPhenomenon("continues");
        }
        public bool TagYyReply(int replyId,string tagId,out string feedback)
        {
            feedback="";RepairYyLearning();var r=S.yyReplies.Find(x=>x.id==replyId);var tag=personaTags.Find(x=>x.id==tagId);
            if(r==null||tag==null)return false;if(r.tags==null)r.tags=new List<string>();if(r.tags.Count>=2||r.tags.Contains(tagId))return false;r.tags.Add(tagId);
            int axis=tag.axis=="温度"?0:tag.axis=="玩心"?1:tag.axis=="主见"?2:-1;
            // Tags teach the actual tone board; they never rewrite the player's opening wish.
            if(axis>=0){ToneCard(tag.delta>=0?High[axis]:Low[axis],true,Math.Abs(tag.delta)/10.0,"tag");feedback=T(AxisNames[axis],AxisNamesEn[axis])+" "+(tag.delta>0?"+":"")+tag.delta;}
            else feedback=T("记下这次答歪了","Noted: off the mark");
            if(tag.flag=="honest")S.honestTags++;if(tag.flag=="flatter")S.flatterTags++;
            S.talkSamples.Add(new XgTalkSample{tag=tagId,text=r.answer});int count=0;for(int i=S.talkSamples.Count-1;i>=0;i--)if(S.talkSamples[i].tag==tagId&&++count>5)S.talkSamples.RemoveAt(i);
            return true;
        }
        public string YyLearningPrompt()
        {
            RepairYyLearning();var b=new StringBuilder();
            foreach(var p in personaCatalog)if(p.id!="daily"&&S.personaPacks.Contains(p.id))b.AppendLine(T(p.desc,p.descEn));
            foreach(var s in S.talkSamples){var tag=personaTags.Find(x=>x.id==s.tag);if(tag!=null)b.AppendLine(T("你曾说：「"+s.text+"」；主人给这句贴的标签是「"+tag.name+"」。","You once said: \""+s.text+"\"; the player tagged it \""+tag.nameEn+"\"."));}
            // 888.vip (XgSim.Gambling.cs): it saw the player gamble; it talks people out of it. Empty until then.
            string gambling=GamblingPromptLine();if(gambling.Length>0)b.AppendLine(gambling);
            return b.ToString();
        }
    }
}
