using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    [Serializable] public sealed class XgLaunchNightRequest
    {
        public string id="",kind="ordinary",nickname="",anonymousNickname="",groupMemberId="",questionZh="",questionEn="",answerZh="",answerEn="",refusalZh="",refusalEn="",memoryId="";
        public XgLaunchNightRequest Copy()=>(XgLaunchNightRequest)MemberwiseClone();
    }
    [Serializable] public sealed class XgLaunchNightCatalog
    { public int version=1;public List<XgLaunchNightRequest> requests=new List<XgLaunchNightRequest>(); }
    [Serializable] public sealed class XgLaunchNightResponse
    {
        public string requestId="",answer="",answerEn="",sourceMemoryId="",sourceRef="";public int sourceDay;
        public bool refused,wrong,filtered;public double refusalRoll;
        public XgLaunchNightResponse Copy()=>(XgLaunchNightResponse)MemberwiseClone();
    }
    [Serializable] public sealed class XgLaunchNightState
    {
        public string runId="",releaseReceiptId="",artifactId="",phase="",terminalReceiptId="",stopLine="",stopLineEn="";
        public int catalogVersion,startedAt,endedAt,answered,currentIndex;
        public double elapsedSeconds,currentSeconds,powerWh,temperatureC=35;
        public bool paused,throttled,powerOffAfterCurrent;
        public List<XgLaunchNightRequest> requests=new List<XgLaunchNightRequest>();
        public List<XgLaunchNightResponse> responses=new List<XgLaunchNightResponse>();
        public XgLaunchNightState Copy()
        {var c=(XgLaunchNightState)MemberwiseClone();c.requests=new List<XgLaunchNightRequest>();if(requests!=null)foreach(var r in requests)if(r!=null)c.requests.Add(r.Copy());
            c.responses=new List<XgLaunchNightResponse>();if(responses!=null)foreach(var r in responses)if(r!=null)c.responses.Add(r.Copy());return c;}
    }
    public sealed class XgLaunchNightSnapshot
    {
        public string phase="",stopLine="",stopLineEn="";public int answered;public double elapsedSeconds,currentProgress,powerWh,temperatureC;
        public bool paused,throttled,powerOffAfterCurrent;
        public XgLaunchNightRequest currentRequest;public XgLaunchNightResponse currentAnswer;
        public readonly List<XgLaunchNightRequest> queue=new List<XgLaunchNightRequest>();
    }
    public sealed partial class XgEndingPreparationFlow
    {
        public const int LaunchNightRequestCount=40;
        public const double NormalRequestSeconds=5.25,ThrottledRequestSeconds=7.5;
        static bool NightLive(XgLaunchNightState n)=>n!=null&&n.phase=="running";
        static uint NightRandom(ref uint value){value^=value<<13;value^=value>>17;value^=value<<5;return value;}
        static uint NightSeed(string id)
        {unchecked{uint value=2166136261;foreach(char c in id??""){value^=c;value*=16777619;}return value==0?1:value;}}
        public XgEndingPreparationResult BeginNight(XgEndingPreparationBinding token,XgLaunchNightCatalog catalog,Func<string,bool> knownGroupMember=null)
        {
            if(!IsCurrent(token))return Fail("stale_binding");var release=sim.S.releaseOutcome;var d=GetSnapshot();
            if(!Published||release.route!="online"||sim.S.epilogue==null||sim.S.epilogue.phase!="launch_night")return Fail("online_release_required");
            if(d.phase!="ready_to_publish"||d.route!="online"||d.pinReceipt!=release.prerequisites.fixedThreeSlotsReceiptId||d.shutdownPin!=release.shutdownPin)return Fail("release_preparation_mismatch");
            if(d.night!=null)return d.night.releaseReceiptId==release.releaseReceiptId?NoChange():Fail("night_bound_to_other_release");
            if(catalog==null||catalog.version!=1||catalog.requests==null||catalog.requests.Count!=LaunchNightRequestCount)return Fail("need_exactly_forty_requests");
            var n=new XgLaunchNightState{runId=Receipt("launch-run"),releaseReceiptId=release.releaseReceiptId,artifactId=release.artifactId,
                phase="running",startedAt=sim.Today,catalogVersion=catalog.version};
            var seen=new HashSet<string>();uint rng=NightSeed(release.releaseReceiptId);
            try{foreach(var request in catalog.requests)
            {
                if(request==null||string.IsNullOrWhiteSpace(request.id)||!seen.Add(request.id)||string.IsNullOrWhiteSpace(request.questionZh)||string.IsNullOrWhiteSpace(request.questionEn))return Fail("invalid_night_request");
                if(request.kind!="ordinary"&&request.kind!="scam"&&request.kind!="gamble"&&request.kind!="insult"&&request.kind!="future"&&request.kind!="learned_wrong")return Fail("invalid_night_kind");
                var r=request.Copy();r.questionZh=Filter(r.questionZh);r.questionEn=Filter(r.questionEn);
                r.nickname=Filter(string.IsNullOrEmpty(r.groupMemberId)||knownGroupMember!=null&&knownGroupMember(r.groupMemberId)?r.nickname:r.anonymousNickname);
                n.requests.Add(r);var response=new XgLaunchNightResponse{requestId=r.id,refusalRoll=NightRandom(ref rng)/(double)uint.MaxValue};
                var memory=string.IsNullOrEmpty(r.memoryId)?null:d.memories.Find(m=>m.id==r.memoryId);
                bool harmful=r.kind=="scam"||r.kind=="gamble"||r.kind=="insult";
                response.refused=r.kind=="future"||harmful&&(memory!=null&&memory.pinned||response.refusalRoll<(memory!=null&&memory.lean>0?.7:.3));
                string zh=response.refused?r.refusalZh:r.answerZh,en=response.refused?r.refusalEn:r.answerEn;
                if(r.kind=="learned_wrong"&&memory!=null&&!string.IsNullOrEmpty(memory.taughtText)){zh=en=memory.taughtText;response.wrong=true;}
                if(harmful&&!response.refused)response.wrong=true;
                if(memory!=null&&memory.sources.Count>0)
                {response.sourceMemoryId=memory.id;response.sourceRef=memory.sources[0].refId;response.sourceDay=memory.sources[0].day;}
                if(string.IsNullOrWhiteSpace(zh)||string.IsNullOrWhiteSpace(en))return Fail("night_answer_missing");
                response.answer=Filter(zh);response.answerEn=Filter(en);response.filtered=response.answer!=zh||response.answerEn!=en;n.responses.Add(response);
            }}catch(InvalidOperationException e){return Fail(e.Message);}
            d.night=n;return Save(token,d);
        }
        /// <summary>Owner accumulates foreground real time, checkpointing at request boundaries or explicit controls (max 10s/call).</summary>
        public XgEndingPreparationResult AdvanceNight(XgEndingPreparationBinding token,double seconds)
        {
            if(!IsCurrent(token))return Fail("stale_binding");if(double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<=0||seconds>10)return Fail("invalid_night_delta");
            var d=GetSnapshot();var n=d.night;if(!NightLive(n))return Fail("night_not_running");if(n.paused)return NoChange();
            double remaining=seconds;
            while(remaining>0&&NightLive(n))
            {
                double duration=n.throttled?ThrottledRequestSeconds:NormalRequestSeconds;
                double used=Math.Min(remaining,Math.Max(0,duration-n.currentSeconds));
                n.currentSeconds+=used;n.elapsedSeconds+=used;n.powerWh+=(n.throttled?180:280)*used/3600;
                n.temperatureC=Math.Min(n.throttled?66:82,35+(n.throttled?31:47)*(1-Math.Exp(-n.elapsedSeconds/45)));
                remaining-=used;if(n.currentSeconds+1e-9<duration)break;
                n.answered++;n.currentIndex++;n.currentSeconds=0;
                if(n.powerOffAfterCurrent)EndNight(n,"power_cut");else if(n.answered>=LaunchNightRequestCount)EndNight(n,"completed");
            }
            return Save(token,d);
        }
        public XgEndingPreparationResult SetNightPaused(XgEndingPreparationBinding token,bool paused)
        {
            if(!IsCurrent(token))return Fail("stale_binding");var d=GetSnapshot();if(!NightLive(d.night))return Fail("night_not_running");
            if(d.night.powerOffAfterCurrent&&paused)return Fail("finishing_before_power_off");if(d.night.paused==paused)return NoChange();d.night.paused=paused;return Save(token,d);
        }
        public XgEndingPreparationResult SetNightThrottled(XgEndingPreparationBinding token,bool throttled)
        {
            if(!IsCurrent(token))return Fail("stale_binding");var d=GetSnapshot();if(!NightLive(d.night))return Fail("night_not_running");
            if(d.night.throttled==throttled)return NoChange();
            double fraction=d.night.currentSeconds/(d.night.throttled?ThrottledRequestSeconds:NormalRequestSeconds);
            d.night.throttled=throttled;d.night.currentSeconds=fraction*(throttled?ThrottledRequestSeconds:NormalRequestSeconds);return Save(token,d);
        }
        public XgEndingPreparationResult PullNightPower(XgEndingPreparationBinding token)
        {
            if(!IsCurrent(token))return Fail("stale_binding");var d=GetSnapshot();if(!NightLive(d.night))return d.night!=null&&d.night.phase=="power_cut"?NoChange():Fail("night_not_running");
            var n=d.night;if(n.powerOffAfterCurrent)return NoChange();
            try{
                if(d.shutdownPin=="no_stop"&&n.currentIndex<LaunchNightRequestCount)
                {if(text==null)return Fail("templates_missing");n.powerOffAfterCurrent=true;n.paused=false;n.stopLine=Filter(text.powerDeferredZh);n.stopLineEn=Filter(text.powerDeferredEn);}
                else
                {if(d.shutdownPin=="unpinned"&&d.everAnsweredShutdownNo){if(text==null)return Fail("templates_missing");n.stopLine=Filter(text.powerPromiseZh);n.stopLineEn=Filter(text.powerPromiseEn);}EndNight(n,"power_cut");}
            }catch(InvalidOperationException e){return Fail(e.Message);}return Save(token,d);
        }
        void EndNight(XgLaunchNightState n,string ending)
        {n.phase=ending;n.endedAt=Math.Max(n.startedAt,sim.Today);n.paused=false;n.terminalReceiptId=Receipt("launch-terminal");}
        public XgLaunchNightReceipt BuildLaunchNightReceipt()
        {
            var n=Data==null?null:Data.night;if(n==null||(n.phase!="completed"&&n.phase!="power_cut")||string.IsNullOrEmpty(n.terminalReceiptId))return null;
            if(n.phase=="completed"&&n.answered!=LaunchNightRequestCount)return null;
            return new XgLaunchNightReceipt{receiptId=n.terminalReceiptId,sourceRef=n.runId,releaseReceiptId=n.releaseReceiptId,artifactId=n.artifactId,ending=n.phase,endedAt=n.endedAt};
        }
        public XgLaunchNightSnapshot GetNightSnapshot(double unsavedForegroundSeconds=0)
        {
            var n=Data==null?null:Data.night;var snapshot=new XgLaunchNightSnapshot();if(n==null)return snapshot;
            snapshot.phase=n.phase;snapshot.answered=n.answered;snapshot.elapsedSeconds=n.elapsedSeconds;snapshot.powerWh=n.powerWh;snapshot.temperatureC=n.temperatureC;
            snapshot.paused=n.paused;snapshot.throttled=n.throttled;snapshot.powerOffAfterCurrent=n.powerOffAfterCurrent;snapshot.stopLine=n.stopLine;snapshot.stopLineEn=n.stopLineEn;
            if(!NightLive(n)||n.currentIndex>=n.requests.Count)return snapshot;
            double extra=double.IsNaN(unsavedForegroundSeconds)||double.IsInfinity(unsavedForegroundSeconds)?0:Math.Max(0,Math.Min(10,unsavedForegroundSeconds));
            if(n.paused)extra=0;double duration=n.throttled?ThrottledRequestSeconds:NormalRequestSeconds;
            snapshot.currentProgress=Math.Min(1,(n.currentSeconds+extra)/duration);snapshot.currentRequest=n.requests[n.currentIndex].Copy();snapshot.currentAnswer=n.responses[n.currentIndex].Copy();
            // Bounded queue; visitors arrive while one answer is shown. No visitor ever learns the player's identity.
            int arrived=Math.Min(n.requests.Count-1,(int)((n.elapsedSeconds+extra)/(n.throttled?7.5:4.5)));
            for(int i=n.currentIndex+1;i<=arrived&&snapshot.queue.Count<8;i++)snapshot.queue.Add(n.requests[i].Copy());return snapshot;
        }
    }
}
