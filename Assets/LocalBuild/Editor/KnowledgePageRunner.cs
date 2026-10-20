#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LingGuangV05.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Dedicated batch invocation: -executeMethod KnowledgePageRunner.Run (without -quit).
// Starts an unsaved empty scene, never Main, and never changes Enter Play Mode options.
[InitializeOnLoad]
public static class KnowledgePageRunner
{
    const string Key="KnowledgePageRunner.Directory";
    const string OldOverrideKey="KnowledgePageRunner.OldOverride";
    static string Root=>SessionState.GetString(Key,"");
    static Report report;
    static double deadline;
    static bool finishing;
    [Serializable] sealed class Report
    {
        public string status="running",failure="",unityVersion,detailReport="";
        public bool enterPlayModeOptionsEnabled;
        public int enterPlayModeOptions;
        public List<string> errors=new List<string>();
        public string scope="Batch-only empty scene; actual knowledge-page search, category, related-link, return and raycast coroutine. No game runtime or Main scene is loaded. Editor play-mode settings are observed, never modified.";
    }
    static KnowledgePageRunner(){EditorApplication.playModeStateChanged+=State;}
    public static void Run()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Use a dedicated batch Editor for this runner");
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Start in Edit Mode");
        string dir=Path.Combine(Path.GetTempPath(),"LingGuang-KnowledgeBatch-QA-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
        SessionState.SetString(Key,dir);SessionState.SetString(OldOverrideKey,ChapterOneRuntime.VerificationSaveDirectory??"");
        ChapterOneRuntime.VerificationSaveDirectory=Path.Combine(dir,"UnusedIsolatedSave");
        // Prevent an existing editor scene (and any real save owner in it) from being played.
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        report=NewReport();finishing=false;Save();
        // Startup indexing is delayed by Unity. If Play Mode starts first, a missing
        // UserSettings/Search.index cannot be created there, and Unity 6000.4 then
        // indexes an empty SearchDatabase list. Initialise in this process in Edit Mode.
        Application.logMessageReceived+=Log;
        try
        {
            EditorSearchWarmup.Run(); // Returns normally; this helper never exits the Editor.
            if(!File.Exists(Path.Combine(Directory.GetCurrentDirectory(),"UserSettings","Search.index")))
                throw new InvalidOperationException("Editor search warmup returned without its default Search.index");
            if(report.errors.Count!=0)
                throw new InvalidOperationException("Editor search warmup logged errors; see the batch report");
            Debug.Log("KNOWLEDGE_SEARCH_READY: default index initialised before EnterPlaymode");
        }
        catch(Exception exception)
        {
            report.status="failed";report.failure="Edit-mode search initialisation failed: "+exception;Save();
            string previous=SessionState.GetString(OldOverrideKey,"");
            ChapterOneRuntime.VerificationSaveDirectory=previous.Length==0?null:previous;
            SessionState.EraseString(OldOverrideKey);SessionState.EraseString(Key);
            Debug.Log("Knowledge page batch FAIL: "+Path.Combine(dir,"report.json"));
            EditorApplication.Exit(1);
            return;
        }
        finally { Application.logMessageReceived-=Log; }
        Save();EditorApplication.EnterPlaymode();
    }
    static Report NewReport()=>new Report{unityVersion=Application.unityVersion,
        enterPlayModeOptionsEnabled=EditorSettings.enterPlayModeOptionsEnabled,enterPlayModeOptions=(int)EditorSettings.enterPlayModeOptions};
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void IsolateBeforeLoad()
    {
        if(string.IsNullOrEmpty(Root))return;
        ChapterOneRuntime.VerificationSaveDirectory=Path.Combine(Root,"UnusedIsolatedSave");
    }
    static void State(PlayModeStateChange state)
    {
        if(string.IsNullOrEmpty(Root))return;
        if(state==PlayModeStateChange.EnteredPlayMode)
        {
            report=NewReport();finishing=false;deadline=EditorApplication.timeSinceStartup+90;
            Application.logMessageReceived+=Log;EditorApplication.update+=Watchdog;
            // Empty scene is an invariant, not merely a setup intention.
            if(UnityEngine.Object.FindAnyObjectByType<ChapterOneRuntime>(FindObjectsInactive.Include)!=null)
            {Complete(false,"Unexpected runtime exists in empty-scene fixture");return;}
            var go=new GameObject("Knowledge page isolated runner");
            go.AddComponent<KnowledgePageDriver>().StartChecks();
        }
        else if(state==PlayModeStateChange.EnteredEditMode)
        {
            Application.logMessageReceived-=Log;EditorApplication.update-=Watchdog;
            string dir=Root;bool passed=File.Exists(Path.Combine(dir,"passed.txt"));
            string previous=SessionState.GetString(OldOverrideKey,"");ChapterOneRuntime.VerificationSaveDirectory=previous.Length==0?null:previous;
            SessionState.EraseString(OldOverrideKey);SessionState.EraseString(Key);
            Debug.Log("Knowledge page batch "+(passed?"PASS":"FAIL")+": "+Path.Combine(dir,"report.json"));
            EditorApplication.Exit(passed?0:1);
        }
    }
    static void Log(string message,string stack,LogType type)
    {
        if(report==null||finishing)return;
        const string prefix="KnowledgePageChecks ";
        if(message.StartsWith(prefix,StringComparison.Ordinal))
        {
            int i=message.IndexOf(": ",StringComparison.Ordinal);if(i>=0)report.detailReport=message.Substring(i+2);
        }
        if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)report.errors.Add(message+"\n"+stack);
    }
    static void Watchdog()
    {
        if(!finishing&&EditorApplication.timeSinceStartup>deadline)Complete(false,"Timed out after 90 seconds");
    }
    public static void Complete(bool passed,string failure)
    {
        if(finishing)return;
        finishing=true;
        if(report==null)report=NewReport();
        passed=passed&&report.errors.Count==0;
        if(string.IsNullOrEmpty(report.detailReport)){passed=false;failure+=" Missing detailed knowledge-page report.";}
        report.status=passed?"passed":"failed";report.failure=failure??"";Save();
        if(passed)File.WriteAllText(Path.Combine(Root,"passed.txt"),"Knowledge page coroutine passed in empty scene");
        Application.logMessageReceived-=Log;EditorApplication.update-=Watchdog;EditorApplication.ExitPlaymode();
    }
    static void Save(){if(!string.IsNullOrEmpty(Root)&&report!=null)File.WriteAllText(Path.Combine(Root,"report.json"),JsonUtility.ToJson(report,true));}
}
public sealed class KnowledgePageDriver : MonoBehaviour
{
    public void StartChecks(){StartCoroutine(Execute());}
    IEnumerator Execute()
    {
        var checks=KnowledgePageChecks.Frames();string error=null;
        try
        {
            while(true)
            {
                bool more=false;object current=null;
                try{more=checks.MoveNext();if(more)current=checks.Current;}catch(Exception e){error=e.ToString();}
                if(error!=null||!more)break;
                yield return current;
            }
        }
        finally{(checks as IDisposable)?.Dispose();}
        // Allow deferred destruction/unload callbacks to run before deciding the outcome.
        yield return null;yield return null;
        KnowledgePageRunner.Complete(error==null,error);
    }
}
#endif
