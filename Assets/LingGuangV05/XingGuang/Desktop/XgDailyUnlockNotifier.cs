using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.Desktop.Media;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEngine;
namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>Only fresh condition-crossing events enter this queue. Loading old completed conditions never backfills it.</summary>
    public sealed class XgDailyUnlockNotifier:MonoBehaviour
    {
        XingGuangController owner;
        XgSim bound;
        ChapterOneSim save;
        readonly List<string> pending=new List<string>(7);
        float retryAt;
        public static void Install(XingGuangController controller)
        {
            var n=controller.GetComponent<XgDailyUnlockNotifier>()??controller.gameObject.AddComponent<XgDailyUnlockNotifier>();
            n.owner=controller;n.Rebind();
        }
        void Rebind()
        {
            if(owner==null||ReferenceEquals(bound,owner.Sim)&&ReferenceEquals(save,owner.runtime?.Sim))return;
            if(bound!=null)bound.DailyConditionCompleted-=Completed;
            pending.Clear();retryAt=0;bound=owner.Sim;save=owner.runtime?.Sim;
            if(bound!=null)bound.DailyConditionCompleted+=Completed;
        }
        void Completed(string id){if(pending.Count<7&&!pending.Contains(id))pending.Add(id);}
        bool Current=>owner!=null&&owner.runtime!=null&&ReferenceEquals(owner.Sim,bound)&&ReferenceEquals(owner.runtime.Sim,save)&&owner.BoundTo(save);
        void Update()
        {
            Rebind();if(pending.Count==0||!Current||owner.runtime.TestMode||!owner.runtime.useDiskSave||save.InPrologue||Time.unscaledTime<retryAt)return;
            retryAt=Time.unscaledTime+1;
            for(int i=0;i<pending.Count;i++)
            {
                string id=pending[i];var node=XgDailyLife.Node(id);
                if(node==null||bound.Has(id)||bound.DailyNoticeAcknowledged(id)){pending.RemoveAt(i);return;}
                if(!bound.DailyNodeReady(id))continue;
                var old=bound.S.dailyAnnouncedNodes;
                string labJson=save.S.labState;
                bound.S.dailyAnnouncedNodes=old==null?new List<string>():new List<string>(old);
                bool saved=false;
                try{saved=bound.AcknowledgeDailyNotice(id)&&owner.SaveNow();}
                finally{if(!saved){bound.S.dailyAnnouncedNodes=old;save.S.labState=labJson;}}
                if(!saved)return;
                pending.RemoveAt(i);
                var expectedSave=save;var expectedLab=bound;
                string name=GameText.IsEnglish?node.nameEn:node.name;
                DesktopNotifications.Notify(owner.runtime,GameText.T("日常发现","Daily discovery"),name,
                    GameText.T("条件已完成，可以去科技页看看。是否购买由你决定。","The condition is complete. Take a look in Tech whenever you like; buying is optional."),false,()=>{
                        if(this==null||!Current||!ReferenceEquals(save,expectedSave)||!ReferenceEquals(bound,expectedLab)||owner.View==null)return;
                        owner.View.Open("tree");owner.View.Tree.FocusNodeDetails(id);
                    });
                return;
            }
        }
        void OnDestroy(){if(bound!=null)bound.DailyConditionCompleted-=Completed;pending.Clear();}
    }
}
