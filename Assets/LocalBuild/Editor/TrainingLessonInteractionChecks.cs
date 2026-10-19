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

// -executeMethod TrainingLessonInteractionChecks.Run, without -quit.
// This is a fully unlocked QA fixture, NOT evidence that a new player can buy every pack.
[InitializeOnLoad]
public static class TrainingLessonInteractionChecks
{
    const string Key="TrainingLessonInteractionChecks.Root";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static string Root=>SessionState.GetString(Key,"");
    static XingGuangController ctl;
    static ChapterOneRuntime runtime;
    static XgTrainPage train;
    static XgStageQuiz quiz;
    static double incomeBefore,walletBefore;
    static Func<bool> originalStoryProbe;
    static Queue<int> emergenceQueue;
    static T Field<T>(object obj,string name)=>(T)obj.GetType().GetField(name,Flags).GetValue(obj);
    static void RefreshLessons(){typeof(XgTrainPage).GetMethod("RefreshLessons",Flags).Invoke(train,null);}
    static void Button(string name){var b=Field<XgBtn>(train,name).button;Require(b.IsActive()&&b.IsInteractable(),"Button unavailable: "+name);b.onClick.Invoke();}
    static void Check(bool ok,string name){Require(ok,name);report.checks.Add(name);Save();}
    static void QueueCard(string id){typeof(XgSim).GetMethod("DiscoverTrainingLesson",Flags).Invoke(ctl.Sim,new object[]{id});}
    static bool CardVisible=>Field<RectTransform>(train,"lessonLayer").gameObject.activeInHierarchy;
    static int index=-1,phase,lastFrame=-1,frames;
    static double timeout;
    static bool finishing;
    static Report report;
    [Serializable] sealed class Report
    {
        public string scope="Main PlayMode with isolated save/profile and explicit unlocked stage-six fixture. Quizzes and knowledge phenomena are arranged using core state/API; this does not claim natural progression coverage. Uses actual Unity Button handlers and actual view hide/rebind. Emergence queue/StoryFocus are temporarily injected solely to check modal exclusion.";
        public string status="running",failure="",unityVersion;
        public List<string> checks=new List<string>(),runtimeErrors=new List<string>(),editorInfrastructureErrors=new List<string>();
        public string desktopReadiness="";
    }
    static TrainingLessonInteractionChecks(){EditorApplication.playModeStateChanged+=State;}
    [MenuItem("Tools/Local Verification/Training lessons")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Start in Edit Mode");
        string dir=Path.GetFullPath(Path.Combine("LocalVerification","TrainingLessons",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));
        Directory.CreateDirectory(dir);Directory.CreateDirectory(Path.Combine(dir,"Save"));Directory.CreateDirectory(Path.Combine(dir,"DesktopProfile"));
        SessionState.SetString(Key,dir);EditorSceneManager.OpenScene("Assets/LingGuangV05/Scenes/Main.unity");EditorApplication.EnterPlaymode();
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Isolate()
    {
        if(string.IsNullOrEmpty(Root))return;
        ChapterOneRuntime.VerificationSaveDirectory=Path.Combine(Root,"Save");
        typeof(LingGuangV05.Desktop.OS.DreamOSDataManager).GetField("dataPath",BindingFlags.Static|BindingFlags.NonPublic)?.SetValue(null,Path.Combine(Root,"DesktopProfile")+Path.DirectorySeparatorChar);
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
            Debug.Log("Training lessons "+(ok?"PASS":"FAIL/INTERRUPTED")+": "+path);if(Application.isBatchMode)EditorApplication.Exit(ok?0:1);
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
            if(EditorApplication.timeSinceStartup>timeout)throw new TimeoutException("Lesson check timeout at "+index+", phase "+phase);
            if(Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;
            if(report.runtimeErrors.Count>0)throw new InvalidOperationException("Runtime error during lesson "+index+": "+report.runtimeErrors[0]);
            if(phase==0)
            {
                runtime=UnityEngine.Object.FindAnyObjectByType<ChapterOneRuntime>();ctl=runtime==null?null:runtime.GetComponent<XingGuangController>();
                if(ctl==null||ctl.Sim==null||ctl.View==null)return;
                runtime.useDiskSave=false;
                var lab=new XgSim();lab.S.stage=6;lab.S.firstSpecialty="vision";lab.S.handCorrect=200;lab.S.epochs=20;
                foreach(var node in XgCatalog.Nodes)if(!lab.S.unlocked.Contains(node.id))lab.S.unlocked.Add(node.id);
                var house=new ChapterOneSim();house.S.prologue=2;house.S.appInstallState=2;house.S.money=1000000;house.S.labState=JsonUtility.ToJson(lab.S);
                GameCalendar.Advance(house.S,GameCalendar.DayFor(6,lab.MonthProgress));house.S.rentDay=GameCalendar.CurrentDay(house.S);house.S.broadbandMonth=201612;
                runtime.SetSimulationForTests(house);typeof(XingGuangController).GetMethod("BindSave",Flags).Invoke(ctl,new object[]{house});
                // This fixture exercises desktop games, not the title/login sequence. Complete the
                // isolated DreamOS profile's boot explicitly; an empty profile otherwise waits at login.
                var boot=UnityEngine.Object.FindAnyObjectByType<LingGuangV05.Desktop.OS.BootManager>(FindObjectsInactive.Include);
                if(boot!=null){boot.CancelInvoke();boot.StopAllCoroutines();if(boot.bootAnimator!=null)boot.bootAnimator.gameObject.SetActive(false);}
                var user=UnityEngine.Object.FindAnyObjectByType<LingGuangV05.Desktop.OS.UserManager>(FindObjectsInactive.Include);
                Require(user!=null&&user.isActiveAndEnabled,"Desktop user manager unavailable");
                user.ShowDesktop();
                File.WriteAllText(Path.Combine(Root,"fixture-lab.json"),JsonUtility.ToJson(lab.S,true));
                frames=0;phase=10;return;
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
                EnableFixtureEconomy(runtime);
                ctl.Open();ctl.View.Open("train");train=ctl.View.Train;originalStoryProbe=StoryFocus.Probe;phase=1;frames=0;return;
            }
            if(++frames<4)return;frames=0;
            if(phase==1)
            {
                // Suppress unrelated narrative only inside this explicit interaction fixture.
                StoryFocus.Probe=()=>false;typeof(XgTrainPage).GetField("cardT",Flags).SetValue(train,-1f);
                ctl.Sim.S.lessonPending.Clear();ctl.Sim.S.lessonDiscovered.Clear();ctl.Sim.S.lessonCollected.Clear();
                RefreshLessons();quiz=ctl.Sim.PendingTrainingQuiz();Require(quiz!=null,"Queued stage quiz missing");
                var choices=Field<XgBtn[]>(train,"quizChoices");var choice=choices[quiz.answer].button;
                Require(choice.IsActive()&&choice.IsInteractable(),"Correct quiz option unavailable");
                Check(ctl.Host.Money==runtime.Sim.S.money&&ctl.Host.Money>0,"Production host sees isolated wallet");
                incomeBefore=ctl.Sim.S.totalIncome;walletBefore=runtime.Sim.S.money;choice.onClick.Invoke();
                Check(Math.Abs(ctl.Sim.S.totalIncome-incomeBefore-20)<.001,"Actual choice button awards exactly 20");
                Check(Math.Abs(runtime.Sim.S.money-walletBefore-20)<.001,"Actual wallet receives exactly 20");
                // The UI hides the button; invoking its stale callback checks double-dispatch too.
                choice.onClick.Invoke();Check(Math.Abs(ctl.Sim.S.totalIncome-incomeBefore-20)<.001,"Stale duplicate callback awards nothing");
                Check(Math.Abs(runtime.Sim.S.money-walletBefore-20)<.001,"Duplicate callback does not pay wallet twice");
                Button("quizNext");Check(!Field<RectTransform>(train,"root").gameObject.activeInHierarchy,"Next button navigates away from Train");
                Check(!ctl.Sim.TrainingLessonModalOpen,"Technology navigation does not leave blocking modal");
                phase=2;return;
            }
            if(phase==2)
            {
                ctl.View.Open("train");QueueCard("nan");RefreshLessons();
                Check(CardVisible&&ctl.Sim.TrainingLessonModalOpen,"Queued knowledge card appears on Train");
                Button("lessonCollect");Check(ctl.Sim.TrainingAtlasLit("lesson.nan")&&!ctl.Sim.TrainingLessonModalOpen,"Actual collect button stores card once and releases modal");
                QueueCard("overfit");RefreshLessons();Check(CardVisible,"Second knowledge card appears");
                ctl.View.enabled=false;Check(!ctl.Sim.TrainingLessonModalOpen,"Disabling window clears modal immediately");
                ctl.View.enabled=true;ctl.View.Open("train");RefreshLessons();
                Check(CardVisible&&ctl.Sim.TrainingLessonModalOpen,"Reopening restores pending card and blocking state");
                phase=3;return;
            }
            if(phase==3)
            {
                StoryFocus.Probe=()=>true;RefreshLessons();
                Check(!CardVisible&&!ctl.Sim.TrainingLessonModalOpen,"StoryFocus queues knowledge instead of displaying");
                Check(ctl.Sim.S.lessonPending.Contains("overfit"),"Story queue preserves pending card");
                StoryFocus.Probe=()=>false;
                var active=typeof(XgEmergenceCutscene).GetField("active",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
                Require(active!=null,"Emergence component not bound");emergenceQueue=Field<Queue<int>>(active,"pending");
                Require(emergenceQueue.Count==0&&!XgEmergenceCutscene.Busy,"Fixture has unrelated emergence in progress");
                emergenceQueue.Enqueue(1);RefreshLessons();
                Check(XgEmergenceCutscene.Busy&&!CardVisible&&!ctl.Sim.TrainingLessonModalOpen,"Queued emergence holds knowledge card");
                emergenceQueue.Clear();emergenceQueue=null;RefreshLessons();
                Check(CardVisible&&ctl.Sim.TrainingLessonModalOpen,"Card resumes after emergence queue clears");
                phase=4;return;
            }
            if(phase==4)
            {
                var old=ctl.Sim;
                var lab=new XgSim();lab.S.stage=6;lab.S.handCorrect=200;lab.S.epochs=20;
                foreach(var node in XgCatalog.Nodes)if(!lab.S.unlocked.Contains(node.id))lab.S.unlocked.Add(node.id);
                var house=new ChapterOneSim();house.S.prologue=2;house.S.appInstallState=2;house.S.money=1000000;house.S.labState=JsonUtility.ToJson(lab.S);
                GameCalendar.Advance(house.S,GameCalendar.DayFor(6,0));house.S.rentDay=GameCalendar.CurrentDay(house.S);house.S.broadbandMonth=201612;
                runtime.SetSimulationForTests(house);typeof(XingGuangController).GetMethod("BindSave",Flags).Invoke(ctl,new object[]{house});
                EnableFixtureEconomy(runtime);
                ctl.View.Open("train");train=ctl.View.Train;RefreshLessons();
                Check(!ReferenceEquals(old,ctl.Sim),"Save switch creates distinct simulation");
                Check(!old.TrainingLessonModalOpen&&!ctl.Sim.TrainingLessonModalOpen&&!CardVisible,"Save switch clears old modal without leaking its card");
                Check(!ctl.Sim.S.lessonCollected.Contains("nan")&&!ctl.Sim.S.lessonPending.Contains("overfit"),"Save switch does not copy prior knowledge progress");
                report.status="passed";Save();File.WriteAllText(Path.Combine(Root,"passed.txt"),"Training lesson actual-button fixture passed. See report.json for exact checks and injected scope.");Finish();return;
            }
        }
        catch(Exception e){report.status="failed";report.failure=e.ToString();Save();Debug.LogError("Training lesson check failed: "+e);Finish();}
    }
    // QA keeps the isolated save, but real purchases/rewards require the production host.
    // Stop only unrelated narrative drivers; neither the host nor its balance methods are replaced.
    static void EnableFixtureEconomy(ChapterOneRuntime rt)
    {
        foreach(var component in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            string name=component.GetType().Name;
            if(name=="StoryDirector"||name=="StoryDesktopPresenter"||name=="PrologueDirector"||name=="SpiderIntroScene"||name=="LoveQuestionCutscene"||name=="StageOneTutor")
            {component.CancelInvoke();component.StopAllCoroutines();component.enabled=false;}
        }
        rt.useDiskSave=false;rt.tickEnabled=false;
        typeof(ChapterOneRuntime).GetProperty("TestMode").GetSetMethod(true).Invoke(rt,new object[]{false});
        if(rt.TestMode||rt.useDiskSave)throw new InvalidOperationException("Live host fixture isolation failed");
    }
    static void Finish(){StoryFocus.Probe=originalStoryProbe;if(emergenceQueue!=null)emergenceQueue.Clear();finishing=true;Application.logMessageReceived-=Log;EditorApplication.update-=Tick;EditorApplication.ExitPlaymode();}
}
#endif