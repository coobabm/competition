using LingGuangV05.Core;
using LingGuangV05.Runtime.Persistence;
using LingGuangV05.XingGuang;
using UnityEngine;

namespace LingGuangV05.Runtime
{
    public sealed partial class ChapterOneRuntime
    {
        static GameState ReadNarrationState(string json)
        {
            var state=JsonUtility.FromJson<GameState>(json);
            NarrationReceiptStore.EnsureIdentity(state,json);
            return state;
        }
        public bool PersistNarration(XgSim lab)
        {
            if(lab==null||Sim==null||persistenceBlocked)return false;
            NarrationReceiptStore.EnsureIdentity(Sim.S);
            if(!useDiskSave)return true;
            if(store==null)return false;
            if(NarrationReceiptStore.Merge(store.Folder,Sim.S.narrationRunId,lab.S,out string why))return true;
            Debug.LogWarning("[Narration] 首次台词记录保存/读取失败，保留待播事件："+why);
            return false;
        }
    }
}
