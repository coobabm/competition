using System;
using System.Globalization;

namespace LingGuangV05.XingGuang
{
    // Read-only presentation shared by the browser and guide. No progress or provenance is invented.
    public static class XgEndingPresentation
    {
        public static bool UsesPreparation(XgState state) => state!=null && state.stage>=6 && state.fullOpen
            && (!state.chapterComplete || XgEpilogueFlow.HasCommittedRelease(state));
        public static bool FinalApproachDue(XgState s,bool sent)
        {
            if(sent||!UsesPreparation(s)||s.chapterComplete||!string.IsNullOrEmpty(s.ending)||XgEpilogueFlow.HasCommittedRelease(s))return false;
            var d=s.endingPreparation;if(d==null)return true;
            return !(d.pendingTrial!=null&&!string.IsNullOrEmpty(d.pendingTrial.id))&&(d.attempts==null||d.attempts.Count==0)
                &&string.IsNullOrEmpty(d.finalPageReceipt)&&string.IsNullOrEmpty(d.siReadReceipt)&&string.IsNullOrEmpty(d.route);
        }
        public static XgGuideStep Guide(XgState s)
        {
            var d=s.endingPreparation;
            string id,zh,en;
            if(XgEpilogueFlow.HasCommittedRelease(s)){id="release";zh="回终章页查看发布后的进展";en="Return to Finale to continue after the release";}
            else if(d==null||string.IsNullOrEmpty(d.finalPageReceipt)){id="redteam";zh="终章页继续发布前的红队测试";en="Continue the pre-release red-team tests on Finale";}
            else if(string.IsNullOrEmpty(d.siReadReceipt)){id="si";zh="在终章页读完 SI 的信";en="Finish reading SI's letter on Finale";}
            else if(string.IsNullOrEmpty(d.route)){id="choice";zh="终章页回看记忆，决定它的去向";en="Review the memories on Finale and decide its future";}
            else if(string.IsNullOrEmpty(d.pinReceipt)){id="pins";zh="终章页选定它带走的三张卡";en="Choose the three cards it will keep on Finale";}
            else{id="publish";zh="回终章页确认发布抉择";en="Return to Finale to confirm the release decision";}
            return new XgGuideStep{id="final.preparation."+id,kind=XgGuideKind.Finale,zh=zh,en=en,
                whyZh="接着完成终章页上的这一步。",whyEn="Continue with the current step on Finale.",app=XgGuide.Lab,tab="final"};
        }
        static XgAskedEntry SourceRecord(XgSim sim,XgEndingMemorySource source)
        {
            if(source==null||sim?.S.askedLog==null||!(source.refId??"").StartsWith("answer:",StringComparison.Ordinal)
                ||!long.TryParse(source.refId.Substring(7),NumberStyles.None,CultureInfo.InvariantCulture,out var id))return null;
            return sim.S.askedLog.Find(x=>x!=null&&x.id==id&&x.gameDate==source.day);
        }
        // Casino records are authored bilingually. Project their saved translation at read time without
        // rewriting the canonical source summary or translating arbitrary player/history text.
        public static string SourceSummary(XgSim sim,XgEndingMemorySource source,bool en)
        {
            if(source==null)return "";
            var record=SourceRecord(sim,source);
            if(record!=null&&XgSim.CasinoScamMemory(record,out _))
            {
                string selected=en?record.questionEn:record.question;
                if(!string.IsNullOrWhiteSpace(selected))
                {
                    string clean=XgEndingTextGuard.Filter(selected,en);
                    if(!string.IsNullOrWhiteSpace(clean))return clean;
                }
            }
            return source.summary??"";
        }
        public static string Evidence(XgSim sim,XgEndingMemorySource source,bool en)
        {
            string summary=SourceSummary(sim,source,en);
            if(source==null||string.IsNullOrWhiteSpace(summary))return "";
            string date;
            try{var day=new DateTime(source.day/10000,source.day/100%100,source.day%100);date=en?day.ToString("MMMM d",CultureInfo.InvariantCulture):day.Month+" 月 "+day.Day+" 日";}
            catch(ArgumentOutOfRangeException){return (en?"Source record: ":"来源记录：")+summary;}
            var record=SourceRecord(sim,source);
            string quote=en?"\""+summary+"\"":"「"+summary+"」";
            if(record!=null)
            {
                var note=sim.S.teachingNotebook?.Find(x=>x!=null&&x.id==record.teachingNoteId&&x.learningRecordId==record.id);
                if(XgSim.HasPlayerTeachingAnswer(record,note))return en?"Because on "+date+" you taught it: "+quote:"因为你在 "+date+" 教过它："+quote;
                if(record.sourceKind=="chat"&&(record.eventKey??"").StartsWith("yy:",StringComparison.Ordinal))
                    return en?"Because on "+date+" you told it: "+quote:"因为你在 "+date+" 跟它说过："+quote;
                if(XgSim.HasPlayerAnswer(record))return en?"Because on "+date+" you answered: "+quote:"因为你在 "+date+" 回答过："+quote;
            }
            // 'answer' also wraps book, retest and old unknown records. It is not proof of player teaching.
            return en?"From the record on "+date+": "+quote:"根据 "+date+" 的记录："+quote;
        }
    }
}
