using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    [Serializable] public sealed class XgAppDefinition
    {
        public string id="",name="",nameEn="",how="",script="",bubble="",bubbleEn="";
        public int stage;public double sizeMB;
    }
    [Serializable] public sealed class XgAppCatalog {public List<XgAppDefinition> apps=new List<XgAppDefinition>();}
    [Serializable] public sealed class XgGroupIntroTrigger
    {
        public int stageEnter,minStage,minDate;public double delaySec;
        public string firstTime="",requires="",after="",orEra="";public bool afterStageCutscene;
    }
    [Serializable] public sealed class XgGroupIntroOption
    {public string text="",textEn="";public List<string> replies=new List<string>(),repliesEn=new List<string>();}
    [Serializable] public sealed class XgGroupIntroStep
    {
        public string type="",text="",textEn="",action="",button="",buttonEn="",who="",app="",label="",labelEn="",done="";
        public List<XgGroupIntroOption> options=new List<XgGroupIntroOption>();
    }
    [Serializable] public sealed class XgGroupIntroScript
    {public string id="",app="";public XgGroupIntroTrigger trigger=new XgGroupIntroTrigger();public List<XgGroupIntroStep> steps=new List<XgGroupIntroStep>();}
    [Serializable] public sealed class XgGroupIntroCatalog {public List<XgGroupIntroScript> scripts=new List<XgGroupIntroScript>();}
    [Serializable] public sealed class XgAppReceipt
    {
        public string id="";public bool offered,downloading,downloaded,installed,legacyGranted;
        public double receivedMB,installedAt;public int installStep;
    }
    [Serializable] public sealed class XgGroupIntroProgress
    {
        public string id="";public int step,choice=-1,reply;public double eligibleSince=-1,wait;
        public bool completed,skipped,legacySkipped,todoOffered;
    }
    [Serializable] public sealed class XgAppAcquisitionState
    {
        public int version=1,revision;public double activeSeconds;public string activeScript="";
        public bool sampleHelpEligible,sampleHelpRequested,gamesOpened;
        public List<XgAppReceipt> apps=new List<XgAppReceipt>();
        public List<XgGroupIntroProgress> intros=new List<XgGroupIntroProgress>();
        public List<string> deferredGroup=new List<string>();
    }
    public sealed partial class XgState { public XgAppAcquisitionState appAcquisition; }
    public sealed partial class XgSim
    {
        // Unity may restore an absent nested field as a default empty object. A real initialized
        // acquisition state always contains the preinstalled Game Hub receipt, even at stage 1.
        static bool EmptyLegacyAcquisition(XgAppAcquisitionState value)=>value==null||
            (value.version<=1&&value.revision==0&&value.activeSeconds==0&&string.IsNullOrEmpty(value.activeScript)
                &&!value.sampleHelpEligible&&!value.sampleHelpRequested&&!value.gamesOpened
                &&(value.apps==null||value.apps.Count==0)&&(value.intros==null||value.intros.Count==0)
                &&(value.deferredGroup==null||value.deferredGroup.Count==0));
        // Absence or the serializer's empty placeholder is a legacy marker, never a claim of use.
        void InitializeAppAcquisition(bool fresh)
        {
            if(!EmptyLegacyAcquisition(S.appAcquisition))return;
            S.appAcquisition=new XgAppAcquisitionState();
            AppReceipt("gamecenter").installed=true;
            if(fresh)return;
            foreach(var pair in new[]{new KeyValuePair<string,int>("taohuo",2),new KeyValuePair<string,int>("miaoyu",3)})
                if(!S.gradualSystems||S.stage>=pair.Value)
                {var app=AppReceipt(pair.Key);app.installed=app.legacyGranted=true;IntroProgress(pair.Key+"_intro").legacySkipped=true;}
            // Only the paid milestone ledger or historical crowd tier proves prior crowd use.
            // totalIncome and chapter alone include unrelated work and are not installation evidence.
            if(S.fundedAbility>=2||S.crowdTier>=2)
            {var crowd=AppReceipt("zhongbao");crowd.installed=crowd.legacyGranted=true;IntroProgress("zhongbao_intro").legacySkipped=true;}
            IntroProgress("gamecenter_intro").legacySkipped=true;
            if(!S.gradualSystems||S.stage>=2)IntroProgress("datapack_intro").legacySkipped=true;
            if(!S.gradualSystems||S.stage>=3)IntroProgress("enterprise_intro").legacySkipped=true;
        }
        public XgAppReceipt AppReceipt(string id)
        {
            if(id!="taohuo"&&id!="miaoyu"&&id!="zhongbao"&&id!="gamecenter")return null;
            var s=S.appAcquisition;if(s==null)return null;
            var a=s.apps.Find(x=>x.id==id);if(a==null){a=new XgAppReceipt{id=id};s.apps.Add(a);}return a;
        }
        public bool DesktopAppInstalled(string id)=>id=="gamecenter"||S.appAcquisition?.apps.Exists(a=>a.id==id&&a.installed)==true;
        public XgGroupIntroProgress IntroProgress(string id)
        {
            var s=S.appAcquisition;if(s==null)return null;
            var p=s.intros.Find(x=>x.id==id);if(p==null){p=new XgGroupIntroProgress{id=id};s.intros.Add(p);}return p;
        }
        public static string DesktopAppId(string route)
        {switch(route){case "xunbao":case "taohuo":return "taohuo";case "miaoyu":case "zhongbao":return route;case "games":case "gamecenter":return "gamecenter";default:return "";}}
        public static string DesktopRoute(string id)=>id=="taohuo"?"xunbao":id=="gamecenter"?"games":id;
        public bool IntroCondition(string condition)
        {
            const string prefix="appInstalled:";
            if(string.IsNullOrEmpty(condition))return true;
            if(condition.StartsWith(prefix,StringComparison.Ordinal))return DesktopAppInstalled(condition.Substring(prefix.Length));
            if(condition=="examBlockedBySamples")return S.appAcquisition.sampleHelpRequested;
            return false;
        }
        public bool IntroEligible(XgGroupIntroScript script,Func<string,bool> eraDelivered)
        {
            if(script?.trigger==null||S.appAcquisition==null)return false;
            var p=IntroProgress(script.id);if(p.completed||p.legacySkipped)return false;
            var t=script.trigger;
            if(S.stage<Math.Max(t.stageEnter,t.minStage)||Today<t.minDate||!IntroCondition(t.requires)||!IntroCondition(t.firstTime))return false;
            if(!string.IsNullOrEmpty(t.after)&&!IntroCondition(t.after)&&!(eraDelivered?.Invoke(t.orEra)==true))return false;
            return true;
        }
        public string NextGroupIntro(XgGroupIntroCatalog catalog,Func<string,bool> eraDelivered)
        {
            var a=S.appAcquisition;if(a==null||catalog==null||!string.IsNullOrEmpty(a.activeScript))return "";
            foreach(var script in catalog.scripts)
            {
                if(!IntroEligible(script,eraDelivered))continue;
                var p=IntroProgress(script.id);var t=script.trigger;
                if(p.eligibleSince<0)p.eligibleSince=a.activeSeconds;
                bool era=!string.IsNullOrEmpty(t.orEra)&&eraDelivered?.Invoke(t.orEra)==true;
                double since=p.eligibleSince;
                if((t.after??"").StartsWith("appInstalled:",StringComparison.Ordinal)&&IntroCondition(t.after))
                {var id=t.after.Substring("appInstalled:".Length);since=AppReceipt(id).installedAt;}
                if(era||a.activeSeconds-since+1e-9>=Math.Max(0,t.delaySec))return script.id;
            }
            return "";
        }
        // Timing only. The desktop provides actual active play and actual network state.
        public void TickAppAcquisition(double seconds,bool held,bool online,XgAppCatalog catalog)
        {
            var a=S.appAcquisition;if(a==null||held||double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<=0)return;
            double dt=Math.Min(1,seconds);a.activeSeconds+=dt;
            if(!online||catalog==null)return;
            foreach(var r in a.apps)
            {
                if(!r.downloading||r.downloaded||r.installed)continue;
                var app=catalog.apps.Find(x=>x.id==r.id);if(app==null||app.sizeMB<=0)continue;
                r.receivedMB=Math.Min(app.sizeMB,r.receivedMB+2.5*dt);
                if(r.receivedMB+1e-9>=app.sizeMB){r.receivedMB=app.sizeMB;r.downloading=false;r.downloaded=true;}
            }
        }
        public bool StartDesktopDownload(string id,XgAppCatalog catalog)
        {
            var app=catalog?.apps.Find(x=>x.id==id&&x.how=="group_link");var a=AppReceipt(id);
            if(app==null||a==null||!a.offered||a.installed||a.downloaded||a.downloading||S.stage<app.stage)return false;
            a.downloading=true;return true;
        }
        public bool AdvanceDesktopInstall(string id)
        {
            var a=AppReceipt(id);if(a==null||!a.offered||!a.downloaded||a.installed||a.installStep<0||a.installStep>2)return false;
            a.installStep++;
            if(a.installStep==3){a.installed=true;a.installedAt=S.appAcquisition.activeSeconds;}
            return true;
        }
    }
}
