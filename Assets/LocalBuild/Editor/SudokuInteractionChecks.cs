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
using System.Reflection;
using LingGuangV05.Desktop.XingGuang;
using UnityEngine.UI;

// Dedicated batch invocation: -executeMethod SudokuInteractionChecks.Run (without -quit).
// Starts an unsaved empty scene, never Main, and never changes Enter Play Mode options.
[InitializeOnLoad]
public static partial class SudokuInteractionChecks
{
    const string Key="SudokuInteractionChecks.Directory";
    const string OldOverrideKey="SudokuInteractionChecks.OldOverride";
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
        public string scope="Batch-only empty scene; real Sudoku buttons, duplicate feedback, one-time settlement and lifecycle gates. No game runtime or Main scene is loaded. Editor play-mode settings are observed, never modified.";
    }
    static SudokuInteractionChecks(){EditorApplication.playModeStateChanged+=State;}
    public static void Run()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Use a dedicated batch Editor for this runner");
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Start in Edit Mode");
        string dir=Path.Combine(Path.GetTempPath(),"LingGuang-SudokuInteractionBatch-QA-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
        SessionState.SetString(Key,dir);SessionState.SetString(OldOverrideKey,ChapterOneRuntime.VerificationSaveDirectory??"");
        ChapterOneRuntime.VerificationSaveDirectory=Path.Combine(dir,"UnusedIsolatedSave");
        // Prevent an existing editor scene (and any real save owner in it) from being played.
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
            report=NewReport();finishing=false;deadline=EditorApplication.timeSinceStartup+120;
            Application.logMessageReceived+=Log;EditorApplication.update+=Watchdog;
            // Empty scene is an invariant, not merely a setup intention.
            if(UnityEngine.Object.FindAnyObjectByType<ChapterOneRuntime>(FindObjectsInactive.Include)!=null)
            {Complete(false,"Unexpected runtime exists in empty-scene fixture");return;}
            var go=new GameObject("Sudoku interaction isolated runner");
            go.AddComponent<SudokuInteractionDriver>().StartChecks();
        }
        else if(state==PlayModeStateChange.EnteredEditMode)
        {
            Application.logMessageReceived-=Log;EditorApplication.update-=Watchdog;
            string dir=Root;bool passed=File.Exists(Path.Combine(dir,"passed.txt"));
            string previous=SessionState.GetString(OldOverrideKey,"");ChapterOneRuntime.VerificationSaveDirectory=previous.Length==0?null:previous;
            SessionState.EraseString(OldOverrideKey);SessionState.EraseString(Key);
            Debug.Log("Sudoku interaction batch "+(passed?"PASS":"FAIL")+": "+Path.Combine(dir,"report.json"));
            EditorApplication.Exit(passed?0:1);
        }
    }
    static void Log(string message,string stack,LogType type)
    {
        if(report==null||finishing)return;
        const string prefix="SudokuInteractionDetails ";
        if(message.StartsWith(prefix,StringComparison.Ordinal))
        {
            int i=message.IndexOf(": ",StringComparison.Ordinal);if(i>=0)report.detailReport=message.Substring(i+2);
        }
        if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)report.errors.Add(message+"\n"+stack);
    }
    static void Watchdog()
    {
        if(!finishing&&EditorApplication.timeSinceStartup>deadline)Complete(false,"Timed out after 120 seconds");
    }
    public static void Complete(bool passed,string failure)
    {
        if(finishing)return;
        finishing=true;
        if(report==null)report=NewReport();
        passed=passed&&report.errors.Count==0;
        report.status=passed?"passed":"failed";report.failure=failure??"";Save();
        if(passed)File.WriteAllText(Path.Combine(Root,"passed.txt"),"Sudoku interaction coroutine passed in empty scene");
        Application.logMessageReceived-=Log;EditorApplication.update-=Watchdog;EditorApplication.ExitPlaymode();
    }
    static void Save(){if(!string.IsNullOrEmpty(Root)&&report!=null)File.WriteAllText(Path.Combine(Root,"report.json"),JsonUtility.ToJson(report,true));}
}
public sealed class SudokuInteractionDriver : MonoBehaviour
{
    public void StartChecks(){StartCoroutine(Execute());}
    IEnumerator Execute()
    {
        var checks=SudokuInteractionChecks.Frames();string error=null;
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
        SudokuInteractionChecks.Complete(error==null,error);
    }
}
public static partial class SudokuInteractionChecks
{
    static void Require(bool ok,string message)
    { if(!ok)throw new InvalidOperationException("Sudoku interaction: "+message);Debug.Log("Sudoku PASS: "+message); }
    static T Field<T>(object game,string name)=>(T)game.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(game);
    static Button ButtonAt(Transform root,string name)
    { var child=root.Find(name);if(child==null)throw new InvalidOperationException("Missing Sudoku button: "+name);return child.GetComponent<Button>(); }
    static void Click(Transform root,string name)=>ButtonAt(root,name).onClick.Invoke();
    static bool Solve(int[] g)
    {
        int at=Array.IndexOf(g,0);if(at<0)return LingGuangV05.XingGuang.XgSudoku.Complete(g);
        for(int d=1;d<=9;d++)if(LingGuangV05.XingGuang.XgSudoku.Allowed(g,at,d))
        {g[at]=d;if(Solve(g))return true;g[at]=0;}return false;
    }
    public static IEnumerator Frames()
    {
        var root=new GameObject("Isolated Sudoku fixture",typeof(RectTransform),typeof(Canvas));
        LingGuangV05.Desktop.XingGuang.MiniGames.XgMiniGame game=null;
        LingGuangV05.Desktop.XingGuang.MiniGames.XgMiniContext context=null;
        try
        {
            var rect=(RectTransform)root.transform;rect.sizeDelta=new Vector2(900,480);
            root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var sim=new LingGuangV05.XingGuang.XgSim();sim.S.stage=2;sim.S.mute=true;
            bool active=true;int reports=0;double seconds=-1;
            var font=XgUiV4Fonts.Sans;Require(font!=null,"shipped UI font loads");
            context=new LingGuangV05.Desktop.XingGuang.MiniGames.XgMiniContext(rect,new XgUi(font,null,true),sim,null,"sudoku",()=>active,()=>0,_=>{},(ok,time)=>{Require(ok,"only solved puzzle reports success");reports++;seconds=time;});
            var type=typeof(LingGuangV05.Desktop.XingGuang.MiniGames.XgMiniGame).Assembly.GetType("LingGuangV05.Desktop.XingGuang.MiniGames.XgSudokuPack",true);
            game=(LingGuangV05.Desktop.XingGuang.MiniGames.XgMiniGame)Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new object[]{context},null);
            game.Build();yield return null;
            var grid=Field<int[]>(game,"grid");var clues=Field<int[]>(game,"clues");
            int blank=Array.IndexOf(clues,0),fixedAt=Array.FindIndex(clues,x=>x!=0),fixedValue=clues[fixedAt];
            Click(rect,"Cell"+fixedAt);Click(rect,"Digit"+(fixedValue%9+1));
            Require(grid[fixedAt]==fixedValue&&Field<int>(game,"selected")==-1,"fixed clue rejects selection and digit editing");
            int duplicate=0;for(int i=blank/9*9;i<blank/9*9+9;i++)if(clues[i]!=0){duplicate=clues[i];break;}
            Require(duplicate>0,"fixture row has a fixed clue for duplicate check");
            Click(rect,"Cell"+blank);Click(rect,"Digit"+duplicate);
            var cells=Field<XgBtn[]>(game,"cells");
            Require(grid[blank]==duplicate&&cells[blank].label.color==XgDark.Bad,"duplicate input is visibly red");
            Click(rect,"Erase");Require(grid[blank]==0&&cells[blank].label.text=="","erase removes duplicate value and text");
            active=false;Click(rect,"Digit1");game.Tick(4);Require(grid[blank]==0&&Field<float>(game,"elapsed")==0&&reports==0,"inactive window blocks input and timer");
            active=true;root.SetActive(false);Click(rect,"Digit1");Require(grid[blank]==0&&reports==0,"hidden root blocks callbacks");root.SetActive(true);
            var solved=(int[])clues.Clone();Require(Solve(solved),"generated fixture solves without mutating production state");
            game.Tick(2.5f);
            for(int i=0;i<81;i++)if(clues[i]==0){Click(rect,"Cell"+i);Click(rect,"Digit"+solved[i]);}
            Require(LingGuangV05.XingGuang.XgSudoku.Complete(grid)&&reports==1&&Math.Abs(seconds-2.5)<.001,"real buttons solve grid and settle exactly once with active elapsed time");
            Click(rect,"Digit1");Click(rect,"Erase");Require(reports==1&&LingGuangV05.XingGuang.XgSudoku.Complete(grid),"solved board cannot be edited or rewarded twice");
            var staleDigit=ButtonAt(rect,"Digit1").onClick;Click(rect,"Next");yield return null;
            grid=Field<int[]>(game,"grid");clues=Field<int[]>(game,"clues");blank=Array.IndexOf(clues,0);
            Click(rect,"Cell"+blank);staleDigit.Invoke();Require(grid[blank]==0&&reports==1,"previous puzzle callbacks cannot modify new board");
            context.Close();Click(rect,"Digit1");Require(grid[blank]==0&&reports==1,"closed context cannot enter or submit");
            game.Dispose();Click(rect,"Next");Require(ReferenceEquals(grid,Field<int[]>(game,"grid")),"disposed game rejects restart callback");
            Debug.Log("SudokuInteractionDetails PASS: real UI callbacks and lifecycle checks completed");
        }
        finally{context?.Close();game?.Dispose();UnityEngine.Object.Destroy(root);}
        yield return null;
    }
}
#endif
