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

// -executeMethod IdiomChainInteractionChecks.Run, without -quit.
// This is a fully unlocked QA fixture, NOT evidence that a new player can buy every pack.
[InitializeOnLoad]
public static class IdiomChainInteractionChecks
{
    const string Key="IdiomChainInteractionChecks.Root";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static string Root=>SessionState.GetString(Key,"");
    static XingGuangController ctl;
    static ChapterOneRuntime runtime;
    static XgDeskGamePage desk;
    static int index=-1,phase,lastFrame=-1,frames;
    static double timeout;
    static bool finishing;
    static Report report;static string expectedWord;static int chain;
    static XgTeachingPack[] Packs => new[]{XgTeachingPacks.Find("idiom_chain")};
    [Serializable] sealed class Entry { public string id,gameType,clicked="",note="";public bool built,visible;public int texts,images,buttons,answersBefore,answersAfter; }
    [Serializable] sealed class Report
    {
        public string scope="Main PlayMode, isolated save, explicit stage-six/all-research QA fixture; actual teach page and Unity Button handlers. At least build every pack. Clicking a chess source/puzzle setup button does not claim solving it.";
        public string status="running",failure="",unityVersion;
        public List<Entry> packs=new List<Entry>();public List<string> runtimeErrors=new List<string>();
        public List<string> editorInfrastructureErrors=new List<string>();
        public string desktopReadiness="";public List<string> checks=new List<string>();
    }
    static IdiomChainInteractionChecks(){EditorApplication.playModeStateChanged+=State;}
    [MenuItem("Tools/Local Verification/Idiom chain continuity")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Start in Edit Mode");
        string dir=Path.GetFullPath(Path.Combine("LocalVerification","IdiomChain",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));
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
            index=-1;phase=frames=chain=0;expectedWord=null;lastFrame=-1;finishing=false;timeout=EditorApplication.timeSinceStartup+180;
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
                if(++frames<4)return;var game=Game();
                if(chain==0){game.GetType().GetField("index",Flags).SetValue(game,24);((XgMiniGame)game).Build();}
                string[] words=(string[])game.GetType().GetField("Words",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);string current=words[(int)Field(game,"index")];bool authenticity=(bool)Field(game,"authenticity");bool real=(bool)Field(game,"proposalReal");if(expectedWord!=null)Require(current==expectedWord,"Accepted idiom not used as next prompt");
                int before=ctl.Sim.MiniGameRecord("idiom_chain").correct;Choose(game,true);expectedWord=words[(int)Field(game,"index")];
                Require(ctl.Sim.MiniGameRecord("idiom_chain").correct==before+1,"Actual correct continuation not settled");
                string prior=current;if(authenticity&&!real)Require(prior==expectedWord,"Rejected fake changed current word");else Require(prior[prior.Length-1]==expectedWord[0],"Continuation first-last mismatch");
                report.checks.Add(prior+" → "+expectedWord);chain++;frames=0;phase=chain<30?2:3;Save();return;
            }
            if(phase==3)
            {
                if(++frames<4)return;var game=Game();string[] words=(string[])game.GetType().GetField("Words",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);Require(words[(int)Field(game,"index")]==expectedWord,"Last accepted prompt lost");int wrong=ctl.Sim.MiniGameRecord("idiom_chain").wrong;Choose(game,false);Require(ctl.Sim.MiniGameRecord("idiom_chain").wrong==wrong+1,"Wrong continuation accepted");
                report.checks.Add("Wrong continuation rejected after30 complete links");report.status="passed";Save();File.WriteAllText(Path.Combine(Root,"passed.txt"),"30 actual idiom rounds including authenticity checks plus wrong continuation passed");Finish();return;
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
    static void Choose(object game,bool correct){var keys=(Action[])Field(game,"keys");for(int i=0;i<keys.Length;i++)if((ClosureInt(keys[i].Target,"truthIndex")==ClosureInt(keys[i].Target,"answer"))==correct){Press(Surface(),"Choice"+i);return;}throw new Exception("Choice not found");}
    static int ClosureInt(object value,string name,int depth=0){if(value==null||depth>5)throw new MissingFieldException(name);var f=value.GetType().GetField(name,Flags);if(f!=null)return (int)f.GetValue(value);foreach(var field in value.GetType().GetFields(Flags))if(field.FieldType.Name.Contains("DisplayClass"))try{return ClosureInt(field.GetValue(value),name,depth+1);}catch(MissingFieldException){}throw new MissingFieldException(name);}
    static void Finish(){finishing=true;Application.logMessageReceived-=Log;EditorApplication.update-=Tick;EditorApplication.ExitPlaymode();}
}
#endif
