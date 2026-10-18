#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LingGuangV05.Core;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.LLM;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.XingGuang.MiniGames;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// -executeMethod GoldenMoleInteractionChecks.Run, without -quit.
// This is a fully unlocked QA fixture, NOT evidence that a new player can buy every pack.
[InitializeOnLoad]
public static class GoldenMoleInteractionChecks
{
    const string Key="GoldenMoleInteractionChecks.Root";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static string Root=>SessionState.GetString(Key,"");
    static XingGuangController ctl;
    static ChapterOneRuntime runtime;
    static XgDeskGamePage desk;
    static int index=-1,phase,lastFrame=-1,frames;
    static double timeout;
    static double nextRealHit;
    static bool finishing;
    static Report report;static int goldSlot;
    static XgTeachingPack[] Packs => new[]{XgTeachingPacks.Find("arith_mole")};
    [Serializable] sealed class Entry { public string id;public int answersBefore,answersAfter; }
    [Serializable] sealed class Report
    {
        public string scope="Main PlayMode, isolated save, explicit stage-six/all-research QA fixture; actual teach page and Unity Button handlers. At least build every pack. Clicking a chess source/puzzle setup button does not claim solving it.";
        public string status="running",failure="",unityVersion;
        public List<Entry> packs=new List<Entry>();public List<string> runtimeErrors=new List<string>();
        public List<string> editorInfrastructureErrors=new List<string>();
        public string desktopReadiness="";public List<string> checks=new List<string>();
    }
    static GoldenMoleInteractionChecks(){EditorApplication.playModeStateChanged+=State;}
    [MenuItem("Tools/Local Verification/Golden mole scoring")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Start in Edit Mode");
        string dir=Path.GetFullPath(Path.Combine("LocalVerification","GoldenMole",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));
        Directory.CreateDirectory(dir);Directory.CreateDirectory(Path.Combine(dir,"Save"));Directory.CreateDirectory(Path.Combine(dir,"DesktopProfile"));
        SessionState.SetString(Key,dir);EditorSceneManager.OpenScene("Assets/LingGuangV05/Scenes/Main.unity");EditorApplication.EnterPlaymode();
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Isolate()
    {
        if(string.IsNullOrEmpty(Root))return;
        ChapterOneRuntime.VerificationSaveDirectory=Path.Combine(Root,"Save");
        typeof(Michsky.DreamOS.DreamOSDataManager).GetField("dataPath",BindingFlags.Static|BindingFlags.NonPublic)?.SetValue(null,Path.Combine(Root,"DesktopProfile")+Path.DirectorySeparatorChar);
        SceneManager.sceneLoaded+=DisableModel;
    }
    static void DisableModel(Scene scene,LoadSceneMode mode)
    {
        if(string.IsNullOrEmpty(Root))return;
        foreach(var root in scene.GetRootGameObjects())foreach(var rt in root.GetComponentsInChildren<ChapterOneRuntime>(true))
        {var llm=rt.GetComponent<LocalLlm>()??rt.gameObject.AddComponent<LocalLlm>();llm.enabled=false;}
    }
    static void State(PlayModeStateChange state)
    {
        if(string.IsNullOrEmpty(Root))return;
        if(state==PlayModeStateChange.EnteredPlayMode)
        {
            index=-1;phase=frames=0;lastFrame=-1;finishing=false;timeout=EditorApplication.timeSinceStartup+180;
            report=new Report{unityVersion=Application.unityVersion};Application.logMessageReceived+=Log;EditorApplication.update+=Tick;
        }
        if(state==PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.update-=Tick;Application.logMessageReceived-=Log;SceneManager.sceneLoaded-=DisableModel;
            string path=Root;bool ok=File.Exists(Path.Combine(path,"passed.txt"));SessionState.EraseString(Key);ChapterOneRuntime.VerificationSaveDirectory=null;
            Debug.Log("Teaching packs "+(ok?"PASS":"FAIL/INTERRUPTED")+": "+path);if(Application.isBatchMode)EditorApplication.Exit(ok?0:1);
        }
    }
    static void Log(string message,string stack,LogType type)
    {
        if(report==null||finishing)return;
        if(type!=LogType.Exception&&type!=LogType.Error&&type!=LogType.Assert)return;
        // Only Unity's known editor search-index failure is separated. Game stack frames are never filtered.
        if(stack.Contains("UnityEditor.Search.SearchDatabase")&&!stack.Contains("Assets/"))
            report.editorInfrastructureErrors.Add(message+"\n"+stack);
        else report.runtimeErrors.Add(message+"\n"+stack);
    }
    static void Save()=>File.WriteAllText(Path.Combine(Root,"report.json"),JsonUtility.ToJson(report,true));
    static void Require(bool test,string reason){if(!test)throw new InvalidOperationException(reason);}
    static void Tick()
    {
        if(!EditorApplication.isPlaying||finishing)return;
        try
        {
            if(EditorApplication.timeSinceStartup>timeout)throw new TimeoutException("Pack check timeout at "+index+", phase "+phase);
            if(Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;
            if(report.runtimeErrors.Count>0)throw new InvalidOperationException("Runtime error during pack "+index+": "+report.runtimeErrors[0]);
            if(phase==0)
            {
                runtime=UnityEngine.Object.FindAnyObjectByType<ChapterOneRuntime>();ctl=runtime==null?null:runtime.GetComponent<XingGuangController>();
                if(ctl==null||ctl.Sim==null||ctl.View==null)return;
                runtime.useDiskSave=false;
                var lab=new XgSim();lab.S.stage=6;lab.S.firstSpecialty="vision";lab.S.handCorrect=200;lab.S.epochs=20;
                foreach(var node in XgCatalog.Nodes)if(!lab.S.unlocked.Contains(node.id))lab.S.unlocked.Add(node.id);
                foreach(var pack in XgTeachingPacks.All){if(!lab.S.unlocked.Contains(pack.NodeId))lab.S.unlocked.Add(pack.NodeId);if(!lab.S.owned.Contains(pack.dataset))lab.S.owned.Add(pack.dataset);if(!lab.S.desksOpen.Contains(pack.dataset))lab.S.desksOpen.Add(pack.dataset);}
                var house=new ChapterOneSim();house.S.prologue=2;house.S.appInstallState=2;house.S.money=1000000;house.S.labState=JsonUtility.ToJson(lab.S);
                GameCalendar.Advance(house.S,GameCalendar.DayFor(6,lab.MonthProgress));house.S.rentDay=GameCalendar.CurrentDay(house.S);house.S.broadbandMonth=201612;
                runtime.SetSimulationForTests(house);typeof(XingGuangController).GetMethod("BindSave",Flags).Invoke(ctl,new object[]{house});
                // This fixture exercises desktop games, not the title/login sequence. Complete the
                // isolated DreamOS profile's boot explicitly; an empty profile otherwise waits at login.
                var boot=UnityEngine.Object.FindAnyObjectByType<Michsky.DreamOS.BootManager>(FindObjectsInactive.Include);
                if(boot!=null){boot.CancelInvoke();boot.StopAllCoroutines();if(boot.bootAnimator!=null)boot.bootAnimator.gameObject.SetActive(false);}
                var user=UnityEngine.Object.FindAnyObjectByType<Michsky.DreamOS.UserManager>(FindObjectsInactive.Include);
                Require(user!=null&&user.isActiveAndEnabled,"Desktop user manager unavailable");
                user.ShowDesktop();
                File.WriteAllText(Path.Combine(Root,"fixture-lab.json"),JsonUtility.ToJson(lab.S,true));
                Require(XgTeachingPacks.All.Length==19,"Expected manifest's 19 packs");frames=0;phase=10;return;
            }
            if(phase==10)
            {
                // Attach/BindSave runs before desktop gate Update. Never call OpenWindow while its taskbar object
                // or a desktop ancestor is still inactive; SetOpen starts its real animation coroutine there.
                var router=runtime.GetComponent<ChapterOneDesktopRouter>();var binding=router==null?null:router.Get(XingGuangController.AppId);
                var taskbar=binding==null?null:binding.TaskbarShortcut;
                bool desktopReady=binding!=null&&binding.Window!=null&&taskbar!=null&&taskbar.isActiveAndEnabled
                    &&binding.DesktopShortcut!=null&&binding.DesktopShortcut.gameObject.activeInHierarchy;
                report.desktopReadiness="binding="+(binding!=null)+", taskbar="+(taskbar!=null)+", active="+(taskbar!=null&&taskbar.gameObject.activeInHierarchy)
                    +", shortcut="+(binding!=null&&binding.DesktopShortcut!=null&&binding.DesktopShortcut.gameObject.activeInHierarchy);
                if(!desktopReady){frames=0;Save();return;}
                if(++frames<3)return;
                ctl.Open();ctl.View.Open("teach");phase=1;return;
            }
            if(phase==1)
            {
                index++;if(index>=Packs.Length){report.status="passed";Save();File.WriteAllText(Path.Combine(Root,"passed.txt"),"All "+Packs.Length+" built without exceptions; see per-pack actual interactions in report.json.");Finish();return;}
                var pack=Packs[index];Require(ctl.Sim.SelectTeachingPack(pack.id),"Fixture cannot select "+pack.id+": "+ctl.Sim.TeachingPackBlocker(pack));
                ctl.Open();ctl.View.Open("teach");ctl.View.Refresh(true);
                desk=typeof(XgLabelPage).GetField("deskGame",Flags)?.GetValue(ctl.View.Teach) as XgDeskGamePage;
                report.packs.Add(new Entry{id=pack.id,answersBefore=ctl.View.Teach.AnswerCount});frames=0;phase=2;return;
            }
            if(phase==2)
            {
                if(++frames<4)return;var game=Game();goldSlot=-1;
                // Let production Update advance the game and render every effect normally.
                // At most one actual click each 0.5 real seconds; never synthetic multi-hit Tick loops.
                var liveHoles=(XgMoleGraphic[])Field(game,"holes");var liveExpires=(float[])Field(game,"expires");var liveWrong=(bool[])Field(game,"wrong");
                for(int i=0;i<9;i++)if(liveExpires[i]>0&&liveHoles[i].Rise>=.9f&&liveHoles[i].Golden){goldSlot=i;break;}
                if(goldSlot<0){
                    if(EditorApplication.timeSinceStartup<nextRealHit)return;
                    for(int i=0;i<9;i++)if(liveExpires[i]>0&&liveHoles[i].Rise>=.9f&&!liveWrong[i]){
                        liveHoles[i].Down();nextRealHit=EditorApplication.timeSinceStartup+.5;
                        if((int)Field(game,"combo")==9)ScreenCapture.CaptureScreenshot(Path.Combine(Root,"nine-hit-real-cadence.png"));break;
                    }
                    return;
                }
                Require(goldSlot>=0,"Ten actual consecutive hits did not produce visible gold mole");Require((int)Field(game,"combo")>=10,"Gold appeared before ten-hit streak");
                ScreenCapture.CaptureScreenshot(Path.Combine(Root,"golden-mole.png"));report.checks.Add("Real rendered frames, minimum0.5second pointer cadence built10-hit streak; next golden target visible");frames=0;phase=3;Save();return;
            }
            if(phase==3)
            {
                if(++frames<2)return;var game=Game();var holes=(XgMoleGraphic[])Field(game,"holes");var record=ctl.Sim.MiniGameRecord("arith_mole");int correct=record.correct,samples=record.samples;int points=(int)Field(game,"score")+(int)Field(game,"goldenBonus");
                holes[goldSlot].Down();Require((int)Field(game,"score")+(int)Field(game,"goldenBonus")==points+2,"Golden actual hit not worth2 points");
                Require(record.correct==correct+1&&record.samples>samples,"Golden hit did not settle exactly one correct action");int paidSamples=record.samples;holes[goldSlot].Down();Require(record.correct==correct+1&&record.samples==paidSamples,"Duplicate gold hit paid again");
                int wrong=record.wrong;((XgMiniGame)game).Tick(61);Require(record.wrong==wrong,"Countdown generated a wrong answer");
                report.checks.Add("Gold hit: local2points, exactly1correct action, duplicate rejected, session timer creates no false answer");
                report.status="passed";Save();File.WriteAllText(Path.Combine(Root,"passed.txt"),"Golden mole actual scoring passed");Finish();return;
            }
        }
        catch(Exception e){report.status="failed";report.failure=e.ToString();Save();Debug.LogError("Teaching pack check failed: "+e);Finish();}
    }
    static object Game()=>typeof(XgDeskGamePage).GetField("game",Flags).GetValue(desk);
    static RectTransform Surface()=>typeof(XgDeskGamePage).GetField("surface",Flags).GetValue(desk) as RectTransform;
    static object Field(object obj,string name){for(Type t=obj.GetType();t!=null;t=t.BaseType){var f=t.GetField(name,Flags);if(f!=null)return f.GetValue(obj);}throw new Exception("Missing field "+name);}
    static Transform ActiveChild(RectTransform root,string name){foreach(Transform child in root)if(child.name==name&&child.gameObject.activeSelf)return child;return null;}
    static Button FindButton(RectTransform root,string name){foreach(var b in root.GetComponentsInChildren<Button>(true))if(b.name==name&&b.gameObject.activeInHierarchy)return b;return null;}
    static void Press(RectTransform root,string name){var b=FindButton(root,name);Require(b!=null&&b.IsActive()&&b.IsInteractable(),"Button unavailable "+name);b.onClick.Invoke();}
    static void Finish(){finishing=true;Application.logMessageReceived-=Log;EditorApplication.update-=Tick;EditorApplication.ExitPlaymode();}
}
#endif
