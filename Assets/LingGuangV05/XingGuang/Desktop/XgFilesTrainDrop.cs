using System;
using LingGuangV05.XingGuang;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LingGuangV05.Desktop.XingGuang
{
    // Attach to the entire Train root. Child legacy GPU target forwards virtual-file drops here.
    public sealed class XgFilesTrainDrop : MonoBehaviour, IDropHandler
    {
        public Func<XgSim> CurrentSim;
        public Func<bool> TargetVisible;
        public Action<string> Notify;
        public Action RefreshView;
        public int LastInstalled { get; private set; }
        string T(XgSim sim,string zh,string en)=>sim!=null&&sim.English?en:zh;
        public void OnDrop(PointerEventData e)
        {
            LastInstalled=0;
            if(!isActiveAndEnabled||e==null||e.button!=PointerEventData.InputButton.Left||TargetVisible==null||!TargetVisible())return;
            var row=e.pointerDrag!=null?e.pointerDrag.GetComponent<XgFilesRow>():null;
            var payload=row!=null?row.Payload:null;
            var sim=CurrentSim!=null?CurrentSim():null;
            if(payload==null||sim==null||!ReferenceEquals(payload.Source,sim))return; // Never toast into a different save.
            if(!payload.IsCurrent)
            {Notify?.Invoke(T(sim,"拖拽已失效，请重新选择数据包。","This drag expired; select the data packs again."));return;}
            if(!sim.CanInstallData)
            {
                Notify?.Invoke(sim.DataInstallBlocker());
                return;
            }
            // Claim before any core callbacks. A second drop or reentrant handler cannot spend/install twice.
            if(!payload.TryConsume(sim))
            {Notify?.Invoke(T(sim,"这次拖拽已处理，请重新选择数据包。","This drag was already handled; select the data packs again."));return;}
            int requested=payload.FileIds.Count;
            string interruption=null;
            foreach(string id in payload.FileIds)
            {
                // A notification may switch/close a save or revoke the gate mid-batch.
                if(!payload.SourceStillCurrent||CurrentSim==null||!ReferenceEquals(CurrentSim(),sim)||TargetVisible==null||!TargetVisible())
                {interruption=T(sim,"安装界面已关闭或选择已失效。","The installation view closed or the selection expired.");break;}
                if(!sim.CanInstallData){interruption=sim.DataInstallBlocker();break;}
                if(sim.InstallDataFile(id))LastInstalled++;
            }
            // Never refresh/toast into another loaded save.
            if(CurrentSim==null||!ReferenceEquals(CurrentSim(),sim))return;
            int skipped=Math.Max(0,requested-LastInstalled);
            string result=LastInstalled>0
                ?T(sim,"已安装 ","Installed ")+LastInstalled+T(sim," 个数据包。"," data packs.")
                :T(sim,"没有安装新的数据包。","No new data packs installed.");
            if(skipped>0)result+="\n"+T(sim,"未安装 ","Not installed: ")+skipped+" · "
                +(interruption??T(sim,"已安装或文件已不可用，请刷新资料文件夹。","Already installed or no longer available; refresh Training files."));
            Notify?.Invoke(result);
            RefreshView?.Invoke();
        }
    }
}
