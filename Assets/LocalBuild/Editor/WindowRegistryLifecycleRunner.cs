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

// Dedicated batch invocation: -executeMethod WindowRegistryLifecycleRunner.Run (without -quit).
// Starts an unsaved empty scene, never Main, and never changes Enter Play Mode options.
[InitializeOnLoad]
public static class WindowRegistryLifecycleRunner
{
    const string Key="WindowRegistryLifecycleRunner.Directory";
    const string OldOverrideKey="WindowRegistryLifecycleRunner.OldOverride";
    const string SceneKey="WindowRegistryLifecycleRunner.Scene";
    public static string FixtureScene=>SessionState.GetString(SceneKey, "");
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
        public string scope="Batch-only empty scene; actual registry coroutine. No game runtime or Main scene is loaded. Editor play-mode settings are observed, never modified.";
    }
    static WindowRegistryLifecycleRunner(){EditorApplication.playModeStateChanged+=State;}
    public static void Run()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Use a dedicated batch Editor for this runner");
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Start in Edit Mode");
        string dir=Path.Combine(Path.GetTempPath(),"LingGuang-WindowRegistryBatch-QA-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
        SessionState.SetString(Key,dir);SessionState.SetString(OldOverrideKey,ChapterOneRuntime.VerificationSaveDirectory??"");
        ChapterOneRuntime.VerificationSaveDirectory=Path.Combine(dir,"UnusedIsolatedSave");
        // Prevent an existing editor scene (and any real save owner in it) from being played.
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        string scenePath="Assets/LocalBuild/Editor/WindowRegistryFixture-"+Guid.NewGuid().ToString("N")+".unity";
        SessionState.SetString(SceneKey,scenePath);
        WindowRegistryLifecycleChecks.MakeWindow("Additive hidden window",null);
        if(!EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),scenePath))throw new InvalidOperationException("Cannot save isolated additive fixture");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        report=NewReport();Save();EditorApplication.EnterPlaymode();
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
            report=NewReport();finishing=false;deadline=EditorApplication.timeSinceStartup+60;
            Application.logMessageReceived+=Log;EditorApplication.update+=Watchdog;
            // Empty scene is an invariant, not merely a setup intention.
            if(UnityEngine.Object.FindAnyObjectByType<ChapterOneRuntime>(FindObjectsInactive.Include)!=null)
            {Complete(false,"Unexpected runtime exists in empty-scene fixture");return;}
            var go=new GameObject("Window registry isolated runner");
            go.AddComponent<WindowRegistryLifecycleDriver>().StartChecks();
        }
        else if(state==PlayModeStateChange.EnteredEditMode)
        {
            Application.logMessageReceived-=Log;EditorApplication.update-=Watchdog;
            string dir=Root;bool passed=File.Exists(Path.Combine(dir,"passed.txt"));
            string previous=SessionState.GetString(OldOverrideKey,"");ChapterOneRuntime.VerificationSaveDirectory=previous.Length==0?null:previous;
            string fixture=FixtureScene;
            if(fixture.StartsWith("Assets/LocalBuild/Editor/WindowRegistryFixture-",StringComparison.Ordinal)&&fixture.EndsWith(".unity",StringComparison.Ordinal))
                AssetDatabase.DeleteAsset(fixture);
            SessionState.EraseString(SceneKey);
            SessionState.EraseString(OldOverrideKey);SessionState.EraseString(Key);
            Debug.Log("Window registry batch "+(passed?"PASS":"FAIL")+": "+Path.Combine(dir,"report.json"));
            EditorApplication.Exit(passed?0:1);
        }
    }
    static void Log(string message,string stack,LogType type)
    {
        if(report==null||finishing)return;
        const string prefix="WindowRegistryLifecycleChecks ";
        if(message.StartsWith(prefix,StringComparison.Ordinal))
        {
            int i=message.IndexOf(": ",StringComparison.Ordinal);if(i>=0)report.detailReport=message.Substring(i+2);
        }
        if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)report.errors.Add(message+"\n"+stack);
    }
    static void Watchdog()
    {
        if(!finishing&&EditorApplication.timeSinceStartup>deadline)Complete(false,"Timed out after 60 seconds");
    }
    public static void Complete(bool passed,string failure)
    {
        if(finishing)return;
        finishing=true;
        if(report==null)report=NewReport();
        passed=passed&&report.errors.Count==0;
        if(string.IsNullOrEmpty(report.detailReport)){passed=false;failure+=" Missing detailed lifecycle report.";}
        report.status=passed?"passed":"failed";report.failure=failure??"";Save();
        if(passed)File.WriteAllText(Path.Combine(Root,"passed.txt"),"Registry lifecycle coroutine passed in empty scene");
        Application.logMessageReceived-=Log;EditorApplication.update-=Watchdog;EditorApplication.ExitPlaymode();
    }
    static void Save(){if(!string.IsNullOrEmpty(Root)&&report!=null)File.WriteAllText(Path.Combine(Root,"report.json"),JsonUtility.ToJson(report,true));}
}
public sealed class WindowRegistryLifecycleDriver : MonoBehaviour
{
    public void StartChecks(){StartCoroutine(Execute());}
    IEnumerator Execute()
    {
        var checks=WindowRegistryLifecycleChecks.Frames();string error=null;
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
        WindowRegistryLifecycleRunner.Complete(error==null,error);
    }
}
#endif
