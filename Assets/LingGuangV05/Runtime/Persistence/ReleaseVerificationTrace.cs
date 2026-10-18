using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;

namespace LingGuangV05.Runtime.Persistence
{
    /// <summary>Metadata only, opt-in through the validated verification directory. No ordinary-player log or content.</summary>
    public static class ReleaseVerificationTrace
    {
        static readonly object gate=new object();
        static string path;
        static long nextId,sequence;
        static bool failed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            // Validation errors retain the existing fail-closed startup behavior; never fall back to user storage.
            string root=VerificationSavePaths.Root;if(root==null)return;
            lock(gate)
            {
                if(path!=null)return;
                string candidate=Path.Combine(root,"native-verification-"+Guid.NewGuid().ToString("N")+".jsonl");
                try
                {
                    using(var file=new FileStream(candidate,FileMode.CreateNew,FileAccess.Write,FileShare.Read)){}
                    path=candidate;
                }
                catch(Exception error){failed=true;Debug.LogWarning("VERIFICATION_TRACE_FAILED "+error.GetType().Name);return;}
            }
            Emit(0,"session_start","metadata_only");Debug.Log("VERIFICATION_NATIVE_TRACE "+path);
        }
        public static long NewRequest()=>path==null||failed?0:Interlocked.Increment(ref nextId);
        // All event/status strings at call sites are fixed program labels. Never pass prompts, responses or exceptions.
        public static void Emit(long request,string kind,string status,int service=0,int slot=-1)
        {
            if(path==null||failed)return;
            lock(gate)
            {
                if(failed)return;
                try
                {
                    if(sequence>=10000){failed=true;Debug.LogWarning("VERIFICATION_TRACE_FAILED event_limit");return;}
                    string line="{\"sequence\":"+(++sequence).ToString(CultureInfo.InvariantCulture)+
                        ",\"utc\":\""+DateTime.UtcNow.ToString("o",CultureInfo.InvariantCulture)+"\",\"monotonic_ticks\":"+System.Diagnostics.Stopwatch.GetTimestamp().ToString(CultureInfo.InvariantCulture)+
                        ",\"clock_frequency\":"+System.Diagnostics.Stopwatch.Frequency.ToString(CultureInfo.InvariantCulture)+
                        ",\"request_id\":"+request.ToString(CultureInfo.InvariantCulture)+",\"event\":\""+Label(kind)+"\",\"status\":\""+Label(status)+
                        "\",\"service\":"+service.ToString(CultureInfo.InvariantCulture)+",\"slot\":"+slot.ToString(CultureInfo.InvariantCulture)+"}";
                    File.AppendAllText(path,line+"\n",new UTF8Encoding(false));
                }
                catch(Exception error){failed=true;Debug.LogWarning("VERIFICATION_TRACE_FAILED "+error.GetType().Name);}
            }
        }
        static string Label(string value)
        {
            if(string.IsNullOrEmpty(value))return "none";
            // Fixed metadata labels only: disallow strings that might accidentally carry user/model content.
            foreach(char c in value)if(!(c>='a'&&c<='z'||c>='0'&&c<='9'||c=='_'))return "other";
            return value.Length<=48?value:"other";
        }
    }
}
