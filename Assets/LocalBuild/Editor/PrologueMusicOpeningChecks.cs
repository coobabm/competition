#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Dedicated batch Editor: -executeMethod PrologueMusicOpeningChecks.Run (without -quit).
// Empty scene: no save/runtime, model or OS pointer manipulation. Boot rendering is outside this fixture.
[InitializeOnLoad]
public static class PrologueMusicOpeningChecks
{
    const string Key="PrologueMusicOpeningChecks.Root";
    static string Root=>SessionState.GetString(Key, "");
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    static Report report; static bool finishing; static double deadline;
    [Serializable] sealed class Report
    {
        public string status="running", failure="";
        public string scope="Isolated empty-scene production prologue coroutine and UI. Original title/boot iterator skipped; no Main scene, save owner, model or real music app. Verifies event order, no icon click dispatched, cursor/input ownership cleanup, missing-icon fallback and interruption. Full LCD input relay, actual music player and new/load menu integration require scene QA.";
        public List<string> checks=new List<string>(),steps=new List<string>(),errors=new List<string>();
    }
    static PrologueMusicOpeningChecks(){EditorApplication.playModeStateChanged+=State;}
    public static void Run()
    {
        if(!Application.isBatchMode||EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Use dedicated batch Editor in Edit Mode");
        var root=Path.Combine(Path.GetTempPath(),"LingGuang-PrologueMusic-QA-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);SessionState.SetString(Key,root);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }
    static void State(PlayModeStateChange state)
    {
        if(Root.Length==0)return;
        if(state==PlayModeStateChange.EnteredPlayMode)
        {
            report=new Report();finishing=false;deadline=EditorApplication.timeSinceStartup+60;
            Application.logMessageReceived+=Log;EditorApplication.update+=Watch;
            new GameObject("Isolated prologue QA").AddComponent<PrologueMusicOpeningDriver>().Run();
        }
        if(state==PlayModeStateChange.EnteredEditMode)
        {
            Application.logMessageReceived-=Log;EditorApplication.update-=Watch;
            string root=Root;bool pass=File.Exists(Path.Combine(root,"passed.txt"));SessionState.EraseString(Key);
            Debug.Log("PROLOGUE_MUSIC_OPENING_"+(pass?"PASS":"FAIL")+": "+root);EditorApplication.Exit(pass?0:1);
        }
    }
    static void Watch(){if(!finishing&&EditorApplication.timeSinceStartup>deadline)Finish("60-second timeout");}
    static void Log(string text,string stack,LogType type)
    {
        if(report==null||finishing)return;
        if(text.StartsWith("[Prologue] "))report.steps.Add(text.Substring(11));
        if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)report.errors.Add(text+"\n"+stack);
    }
    public static void Finish(string error)
    {
        if(finishing)return;finishing=true;
        bool pass=error==null&&report.errors.Count==0;report.status=pass?"passed":"failed";report.failure=error??"";
        File.WriteAllText(Path.Combine(Root,"report.json"),JsonUtility.ToJson(report,true));
        if(pass)File.WriteAllText(Path.Combine(Root,"passed.txt"),"Production prologue coroutine checks passed; scope in report.json");
        Application.logMessageReceived-=Log;EditorApplication.update-=Watch;EditorApplication.ExitPlaymode();
    }
    static void Need(bool ok,string text){if(!ok)throw new InvalidOperationException(text);report.checks.Add(text);}
    static object Get(object obj,string field)=>obj.GetType().GetField(field,F).GetValue(obj);
    static object Call(object obj,string method,params object[] args)=>obj.GetType().GetMethod(method,F).Invoke(obj,args);
    public static IEnumerator Frames()
    {
        Need(UnityEngine.Object.FindAnyObjectByType<ChapterOneRuntime>()==null,"No real save owner exists");
        bool oldCursor=Cursor.visible;
        var desktop=new GameObject("Fixture desktop",typeof(RectTransform),typeof(Canvas));
        var host=new GameObject("Fixture director");
        var root=(RectTransform)desktop.transform;root.sizeDelta=new Vector2(1920,1080);
        desktop.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var icons=PrologueDesk.Rect("Icons",root,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
        var windows=PrologueDesk.Rect("Windows",root,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
        var music=PrologueDesk.Centered("Music Player",icons,new Vector2(-450,180),new Vector2(64,64));
        int opens=0;music.gameObject.AddComponent<PrologueClick>().Open=()=>opens++;
        var desk=new PrologueDesk(root,icons,windows,PrologueDesk.CjkFont());
        var director=host.AddComponent<PrologueDirector>();
        typeof(PrologueDirector).GetField("desk",F).SetValue(director,desk);Call(director,"BuildScreen");
        try
        {
            // Follow the real Run iterator at the post-boot boundary, not a duplicate test sequence.
            var run=(IEnumerator)Call(director,"Run");
            Need(run.MoveNext()&&director.Step=="title","Production Run first yields original title/boot");
            Need(run.MoveNext(),"Production Run has a post-boot sequence");
            yield return run.Current;
            Need(director.Step=="music-interrupted","Music approach finishes before virus creation");
            Need(Vector2.Distance(desk.Cursor.anchoredPosition,desk.LocalOf(music))<.01f,"Simulated cursor reaches actual music shortcut center");
            Need(opens==0,"Approach never clicks or opens music shortcut");
            Need((bool)Get(director,"hijacked")&&!Cursor.visible,"Approach owns the simulated cursor until alert construction");
            Need(run.MoveNext()&&director.Step=="virus","Production Run immediately continues into virus alert");
            var virus=(IEnumerator)run.Current;
            Need(virus.MoveNext(),"Production virus alert is constructed and awaits player answer");
            Need(Get(director,"virusBox")!=null,"Virus window exists before input is released");
            Need(!(bool)Get(director,"hijacked")&&Cursor.visible&&!desk.Cursor.gameObject.activeSelf,"Virus restores cursor and relinquishes input ownership");
            var expected=new[]{"music-intent","music-approach","music-interrupted","virus"};
            var ordered=report.steps.Where(x=>expected.Contains(x)).ToArray();
            Need(ordered.SequenceEqual(expected),"Exact production music-intent -> music-approach -> music-interrupted -> virus order");
            Call(director,"StopPrologue");
            music.gameObject.SetActive(false);
            yield return (IEnumerator)Call(director,"ApproachMusicBeforeVirus");
            Need(!(bool)Get(director,"hijacked")&&Cursor.visible,"Unavailable music shortcut returns control without hanging");
            Call(director,"StopPrologue");music.gameObject.SetActive(true);
            var interrupted=(IEnumerator)Call(director,"ApproachMusicBeforeVirus");
            Need(interrupted.MoveNext()&&(bool)Get(director,"hijacked"),"Interruption fixture starts while cursor is owned");
            director.enabled=false;
            Need(!(bool)Get(director,"hijacked")&&Cursor.visible&&!desk.Cursor.gameObject.activeSelf,"Disabling director restores cursor and input ownership");
            Need(Get(director,"thinking")==null,"Disabling director cancels outstanding opening thought");
            Need(opens==0,"Normal, fallback and interrupted paths dispatch zero music-open events");
        }
        finally
        {
            Call(director,"StopPrologue");UnityEngine.Object.Destroy(host);UnityEngine.Object.Destroy(desktop);Cursor.visible=oldCursor;
        }
    }
}
public sealed class PrologueMusicOpeningDriver:MonoBehaviour
{
    public void Run(){StartCoroutine(Execute());}
    IEnumerator Execute()
    {
        var pending=new Stack<IEnumerator>();pending.Push(PrologueMusicOpeningChecks.Frames());string error=null;
        while(pending.Count>0&&error==null)
        {
            object current=null;bool more=false;
            try
            {
                more=pending.Peek().MoveNext();
                if(more)current=pending.Peek().Current;
                else{(pending.Pop() as IDisposable)?.Dispose();}
            }
            catch(Exception e){error=e.ToString();}
            if(error!=null)break;
            if(!more)continue;
            if(current is IEnumerator nested)pending.Push(nested);else yield return current;
        }
        while(pending.Count>0){try{(pending.Pop() as IDisposable)?.Dispose();}catch(Exception e){error=error??e.ToString();}}
        yield return null;yield return null;PrologueMusicOpeningChecks.Finish(error);
    }
}
#endif
