using System;
using System.Collections.Generic;
namespace LingGuangV05.XingGuang
{
    // Presentation ledger only. Never awards, completes objectives, or mutates game progress.
    public sealed class XgGuideNotificationState
    {
        readonly Dictionary<string,string> known=new Dictionary<string,string>();
        readonly Dictionary<string,XgGuideStep> queued=new Dictionary<string,XgGuideStep>();
        bool initialized; double due; public bool Unread {get;private set;}
        public bool HasNew {get;private set;}
        public int PendingCount=>queued.Count;
        static string Signature(XgGuideStep s)=>s.kind+"|"+s.level+"|"+s.app+"|"+s.tab+"|"+s.target+"|"+s.arg+"|"+(s.goal>0&&s.current>=s.goal);
        public void Observe(IList<XgGuideStep> steps,double now,bool quietInitial)
        {
            foreach(var s in steps)
            {
                if(s==null||string.IsNullOrEmpty(s.id))continue;
                string signature=Signature(s);bool exists=known.TryGetValue(s.id,out var old);
                if((!exists||old!=signature)&&!(quietInitial&&!initialized))
                {if(queued.Count==0)due=now+2;queued[s.id]=s;HasNew|=!exists;Unread=true;}
                known[s.id]=signature;
            }
            // Disappearance can mean guide reprioritisation, not completion. Do not invent a completion.
            initialized=true;
        }
        public List<XgGuideStep> Drain(double now,bool blocked)
        {
            if(blocked||queued.Count==0||now<due)return null;
            var result=new List<XgGuideStep>(queued.Values);queued.Clear();return result;
        }
        public void Acknowledge(){Unread=false;queued.Clear();HasNew=false;}
        public void Displayed(){HasNew=false;}
        public void Reset(){known.Clear();queued.Clear();initialized=false;Unread=false;HasNew=false;due=0;}
    }
}
