using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    [Serializable] public sealed class XgPublicDiscourseDeliveryState
    {
        public bool initialized;
        public int stage;
        public double safeSeconds,stageSafeSeconds,quietRemaining;
        public List<string> notifiedIds=new List<string>();
        public List<int> notifiedStages=new List<int>();
        public List<double> recentPrompts=new List<double>();
        public List<XgPublicDiscoursePlatformTime> platforms=new List<XgPublicDiscoursePlatformTime>();
        public double lastPrompt=-1000000;
    }
    [Serializable] public sealed class XgPublicDiscoursePlatformTime {public string platform="";public double at;}

    /// <summary>The owner persists this state in its existing save. This class never opens a window or posts text.</summary>
    public static class XgPublicDiscourseSchedule
    {
        public static void Initialize(XgPublicDiscourseDeliveryState state,int stage,bool newGame)
        {
            Repair(state);if(state.initialized)return;
            state.initialized=true;state.stage=stage;state.stageSafeSeconds=0;
            // A migrated save gets an archive, never a backlog of newly introduced alerts.
            if(!newGame)for(int i=1;i<=stage;i++)if(!state.notifiedStages.Contains(i))state.notifiedStages.Add(i);
        }
        public static void Advance(XgPublicDiscourseDeliveryState state,int stage,double seconds,bool foregroundSafe,bool importantEvent)
        {
            Repair(state);if(!state.initialized)Initialize(state,stage,false);
            if(stage!=state.stage){state.stage=stage;state.stageSafeSeconds=0;}
            if(importantEvent)state.quietRemaining=30;
            if(!foregroundSafe||importantEvent||double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<=0)return;
            // An offline/background delta may never count as hours of active time.
            double delta=Math.Min(seconds,1);
            state.safeSeconds+=delta;state.stageSafeSeconds+=delta;
            state.quietRemaining=Math.Max(0,state.quietRemaining-delta);
            state.recentPrompts.RemoveAll(t=>state.safeSeconds-t>=600);
        }
        public static bool Eligible(XgPublicDiscourseDeliveryState state,XgPublicDiscourseRecord record,XgPublicDiscourseContext context,XgPublicDiscourseCatalog catalog)
        {
            if(state==null||record==null||context==null||catalog==null)return false;Repair(state);
            if(!state.initialized||state.stage!=context.stage||record.stage!=context.stage||record.route.Length>0||record.since>=20170101
                ||state.stageSafeSeconds<60||state.quietRemaining>0||state.safeSeconds-state.lastPrompt<180
                ||state.notifiedIds.Contains(record.id)||state.notifiedStages.Contains(record.stage)||record.legacyNewsIds.Length>0||!catalog.Visible(record,context))return false;
            int recent=0;foreach(double time in state.recentPrompts)if(state.safeSeconds-time<600)recent++;
            if(recent>=2)return false;
            foreach(var platform in state.platforms)if(platform.platform==record.platform&&state.safeSeconds-platform.at<300)return false;
            return true;
        }
        /// <summary>Call only after the host accepted the one prompt; failed/held delivery leaves no receipt.</summary>
        public static bool RecordDelivered(XgPublicDiscourseDeliveryState state,XgPublicDiscourseRecord record,XgPublicDiscourseContext context,XgPublicDiscourseCatalog catalog)
        {
            if(!Eligible(state,record,context,catalog))return false;
            state.notifiedIds.Add(record.id);state.notifiedStages.Add(record.stage);state.lastPrompt=state.safeSeconds;
            state.recentPrompts.Add(state.safeSeconds);
            var previous=state.platforms.Find(p=>p.platform==record.platform);
            if(previous==null){previous=new XgPublicDiscoursePlatformTime{platform=record.platform};state.platforms.Add(previous);}
            previous.at=state.safeSeconds;return true;
        }
        static void Repair(XgPublicDiscourseDeliveryState state)
        {
            if(state==null)throw new ArgumentNullException(nameof(state));
            if(state.notifiedIds==null)state.notifiedIds=new List<string>();
            if(state.notifiedStages==null)state.notifiedStages=new List<int>();
            if(state.recentPrompts==null)state.recentPrompts=new List<double>();
            if(state.platforms==null)state.platforms=new List<XgPublicDiscoursePlatformTime>();
            state.platforms.RemoveAll(p=>p==null);
        }
    }
}
