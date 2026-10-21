using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using LingGuangV05.Core;
using LingGuangV05.XingGuang;
using UnityEngine;

namespace LingGuangV05.Runtime.Persistence
{
    [Serializable] public sealed class NarrationReceipt
    {
        public int version = 1;
        public string runId;
        public List<string> consumed = new List<string>();
    }

    /// <summary>Append-only heard-event receipts outside rollback snapshots, inside the configured save directory.</summary>
    public static class NarrationReceiptStore
    {
        public static void EnsureIdentity(GameState state, string source = null)
        {
            if (state == null) return;
            if (ValidId(state.narrationRunId)) return;
            if (Guid.TryParseExact(state.offlineIncome?.lineage,"N",out var lineage))
                state.narrationRunId = lineage.ToString("N");
            else
            {
                // No safe relationship can be inferred between two different ancient snapshots without lineage.
                // Exact source fingerprints migrate repeat loads stably without combining unrelated games.
                using(var sha=SHA256.Create())
                    state.narrationRunId=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(source??JsonUtility.ToJson(state)))).Replace("-","").ToLowerInvariant();
            }
        }
        static bool ValidId(string id)
        {
            if(string.IsNullOrEmpty(id)||(id.Length!=32&&id.Length!=64))return false;
            foreach(char c in id)if(!((c>='0'&&c<='9')||(c>='a'&&c<='f')))return false;
            return true;
        }
        public static List<string> Consumed(XgState s)
        {
            var keys=new List<string>();
            if(s.answerPayThoughtConsumed)keys.Add("answer.pay");
            if(s.answerFineThoughtConsumed)keys.Add("answer.fine");
            if(s.teachIntroSaid)keys.Add("teach.intro");
            if(s.teachThirdSaid)keys.Add("teach.third");
            if(s.teachTreeSaid)keys.Add("teach.tree");
            if(s.teachTrainSaid)keys.Add("teach.train");
            if(s.afterthoughts!=null)foreach(var k in s.afterthoughts)if(!string.IsNullOrEmpty(k))keys.Add("afterthought:"+k);
            if(s.tutorSeen!=null)foreach(var k in s.tutorSeen)if(!string.IsNullOrEmpty(k))keys.Add("tutor:"+k);
            return keys;
        }
        public static void Apply(XgState s,IEnumerable<string> keys)
        {
            if(s.afterthoughts==null)s.afterthoughts=new List<string>();
            if(s.tutorSeen==null)s.tutorSeen=new List<string>();
            foreach(string key in keys)
            {
                switch(key)
                {
                    case "answer.pay":s.answerPayThoughtRecorded=s.answerPayThoughtConsumed=true;break;
                    case "answer.fine":s.answerFineThoughtRecorded=s.answerFineThoughtConsumed=true;break;
                    case "teach.intro":s.teachIntroSaid=true;break;
                    case "teach.third":s.teachThirdSaid=true;break;
                    case "teach.tree":s.teachTreeSaid=true;break;
                    case "teach.train":s.teachTrainSaid=true;break;
                    default:
                        if(key.StartsWith("afterthought:",StringComparison.Ordinal)){var k=key.Substring(13);if(!s.afterthoughts.Contains(k))s.afterthoughts.Add(k);}
                        else if(key.StartsWith("tutor:",StringComparison.Ordinal)){var k=key.Substring(6);if(!s.tutorSeen.Contains(k))s.tutorSeen.Add(k);}
                        break;
                }
            }
            s.pendingAnswerMoneyThoughts?.RemoveAll(k=>k=="pay"&&s.answerPayThoughtConsumed||k=="fine"&&s.answerFineThoughtConsumed);
            s.pendingAfterthoughts?.RemoveAll(x=>x!=null&&s.afterthoughts.Contains(x.key));
        }
        public static bool Merge(string saveDirectory,string runId,XgState state,out string error)
        {
            try { return MergeCore(saveDirectory,runId,state,out error); }
            catch(Exception failure) when(failure is IOException || failure is UnauthorizedAccessException || failure is ArgumentException || failure is System.Security.SecurityException)
            { error="Narration receipt unavailable: "+failure.Message;return false; }
        }
        static bool MergeCore(string saveDirectory,string runId,XgState state,out string error)
        {
            error="";
            if(!ValidId(runId)){error="Invalid narration playthrough identity";return false;}
            var store=new AtomicSaveStore(Path.Combine(saveDirectory,"narration",runId));
            var receipt=new NarrationReceipt{runId=runId};
            bool exists=File.Exists(store.SavePath)||File.Exists(store.SavePath+".bak");
            if(exists)
            {
                var loaded=store.Load(json=>{var r=JsonUtility.FromJson<NarrationReceipt>(json);return r!=null&&r.version==1&&r.runId==runId&&r.consumed!=null&&!r.consumed.Exists(string.IsNullOrEmpty);});
                if(!loaded.Success){error=loaded.Message;return false;}
                // A backup may predate a heard line. Never silently roll back this nonrollback ledger.
                if(loaded.Recovered){error="Narration receipt primary is damaged; older backup cannot prove all heard events. "+loaded.Message;return false;}
                receipt=JsonUtility.FromJson<NarrationReceipt>(loaded.Payload);
            }
            bool changed=false;
            foreach(var key in Consumed(state))if(!receipt.consumed.Contains(key)){receipt.consumed.Add(key);changed=true;}
            if(changed&&!store.TrySave(JsonUtility.ToJson(receipt),out error))return false;
            Apply(state,receipt.consumed);
            return true;
        }
    }
}
