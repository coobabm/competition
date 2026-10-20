using System;
using System.IO;
using LingGuangV05.Core;
using LingGuangV05.Runtime.Persistence;
using UnityEngine;

namespace LingGuangV05.Runtime
{
    public sealed partial class ChapterOneRuntime
    {
        public Func<XgOfflineQuote> OfflineIncomeQuoteProvider;
        public double OfflineIncome { get; private set; }
        public string OfflineIncomeStatus { get; private set; } = "";
        long offlineSessionStartedUnix;

        public bool SetOfflineIncomeEnabled(bool enabled)
        {
            EnsureInitialized();
            if(Sim==null||Sim.InPrologue||persistenceBlocked)return false;
            var quote=OfflineIncomeQuoteProvider?.Invoke();
            if(enabled&&(quote==null||!quote.unlocked))return false;
            var state=Sim.S.offlineIncome??(Sim.S.offlineIncome=new XgOfflineState());
            bool old=state.enabled;state.enabled=enabled;MarkDirty();
            if(SaveNow())return true;
            state.enabled=old;return false;
        }

        void CaptureOfflineIncome()
        {
            var state=Sim.S.offlineIncome??(Sim.S.offlineIncome=new XgOfflineState());
            if(!Guid.TryParseExact(state.lineage,"N",out _))state.lineage=Guid.NewGuid().ToString("N");
            var quote=OfflineIncomeQuoteProvider?.Invoke();
            // During initial runtime load the desktop has not installed the provider yet.
            // Settlement already used the saved quote; retain it until a real live capture is available.
            if(quote==null)return;
            state.eligible=quote.unlocked&&quote.eligible&&XgOfflineRules.Finite(quote.incomePerSecond)&&quote.incomePerSecond>0;
            state.incomePerSecond=state.eligible?quote.incomePerSecond:0;
            state.capturedUnix=Math.Max(state.capturedUnix,UtcNow);
        }

        bool ReadOfflineReceipt(XgOfflineState state,out AtomicSaveStore receipts,out XgOfflineReceipt receipt)
        {
            receipts=null;receipt=null;
            if(store==null||!Guid.TryParseExact(state.lineage,"N",out _))return false;
            receipts=new AtomicSaveStore(Path.Combine(store.Folder,"offline-income",state.lineage));
            if(!File.Exists(receipts.SavePath)&&!File.Exists(receipts.SavePath+".bak"))
            {
                // A previously settled save with a missing ledger is not safe to settle again.
                if(state.receipt>0)return false;
                receipt=new XgOfflineReceipt{lineage=state.lineage};return true;
            }
            var loaded=receipts.Load(payload=>XgOfflineRules.ValidReceipt(JsonUtility.FromJson<XgOfflineReceipt>(payload),state.lineage));
            if(!loaded.Success)return false;
            receipt=JsonUtility.FromJson<XgOfflineReceipt>(loaded.Payload);return true;
        }

        void ApplyOfflineIncome()
        {
            OfflineSeconds=0;OfflineIncome=0;OfflineIncomeStatus="";
            var state=Sim.S.offlineIncome;
            if(state==null||string.IsNullOrEmpty(state.lineage))return; // Legacy save: disabled, no retroactive income.
            if(!ReadOfflineReceipt(state,out var receipts,out var previous))
            { OfflineIncomeStatus=GameText.T("离线结算记录暂不可读，本次未结算；游戏进度保留。","Offline settlement records are unavailable; no income was settled. Game progress is preserved.");return; }
            // Reloading during this process must not award offline pay for time already spent playing.
            long now=Math.Min(UtcNow,offlineSessionStartedUnix);
            XgOfflineReceipt next;
            try { next=XgOfflineRules.Prepare(state,Sim.S.lastSeenUnix,now,previous); }
            catch(ArgumentException) { OfflineIncomeStatus=GameText.T("离线结算记录无效，本次未结算。","Invalid offline settlement record; no income was settled.");return; }
            catch(OverflowException) { OfflineIncomeStatus=GameText.T("离线结算编号无效，本次未结算。","Invalid offline settlement sequence; no income was settled.");return; }
            if(!receipts.TrySave(JsonUtility.ToJson(next),out _))
            { OfflineIncomeStatus=GameText.T("离线结算记录保存失败，本次未结算。","Offline receipt could not be saved; no income was settled.");return; }
            Sim.RecordFunds("其他收支");
            if(XgOfflineRules.Apply(Sim.S,next))
            { OfflineIncome=next.amount;OfflineSeconds=next.seconds;Sim.RecordFunds("离线收益"); }
        }

        // Manual archives/imports are explicit rewinds: suppress historical replay, while retaining the
        // per-lineage high-water mark so clock rollback and switching slots cannot farm offline intervals.
        bool PrepareOfflineArchive(GameState restored)
        {
            OfflineIncomeStatus="";OfflineIncome=0;OfflineSeconds=0;
            var state=restored.offlineIncome;
            if(state==null||string.IsNullOrEmpty(state.lineage))return true;
            if(!ReadOfflineReceipt(state,out _,out var receipt))
            {
                // An imported archive need not carry another machine's receipt directory.
                // Preserve the game, but require a new opt-in and never replay its historical interval.
                state.enabled=false;state.eligible=false;state.incomePerSecond=0;
                state.lineage=Guid.NewGuid().ToString("N");state.receipt=0;
                state.capturedUnix=Math.Max(state.capturedUnix,UtcNow);
                restored.lastSeenUnix=Math.Max(restored.lastSeenUnix,state.capturedUnix);
                OfflineIncomeStatus=GameText.T("该存档的离线记录不可读，离线收益已关闭；进度正常保留。","This archive's offline records are unavailable; offline income is disabled. Game progress is preserved.");
                return true;
            }
            state.receipt=Math.Max(state.receipt,receipt.serial);
            state.capturedUnix=Math.Max(Math.Max(state.capturedUnix,receipt.watermark),UtcNow);
            restored.lastSeenUnix=Math.Max(restored.lastSeenUnix,state.capturedUnix);
            OfflineIncomeStatus="";OfflineIncome=0;OfflineSeconds=0;
            return true;
        }
    }
}
