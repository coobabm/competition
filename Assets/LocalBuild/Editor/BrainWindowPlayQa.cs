#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using LingGuangV05.Desktop.XingGuang;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.LLM;
using UnityEngine;
using UnityEngine.UI;
/// <summary>Run only in an already isolated Play session. Does not start Unity or touch a normal player's save.</summary>
[InitializeOnLoad]
public static class BrainWindowPlayQa
{
    const string Key="BrainWindowPlayQa.Root";
    static string Root=>SessionState.GetString(Key,"");
    static double bootDeadline;static bool fixtureReady;static readonly List<string> errors=new List<string>();
    static readonly List<string> editorInfrastructureErrors=new List<string>();
    static BrainWindowPlayQa(){EditorApplication.playModeStateChanged+=State;}
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Start in Edit Mode");
        string dir=Path.GetFullPath(Path.Combine("LocalVerification","BrainWindow",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));
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
    {if(string.IsNullOrEmpty(Root))return;foreach(var root in scene.GetRootGameObjects())foreach(var rt in root.GetComponentsInChildren<ChapterOneRuntime>(true)){var llm=rt.GetComponent<LocalLlm>()??rt.gameObject.AddComponent<LocalLlm>();llm.enabled=false;}}
    static void State(PlayModeStateChange state)
    {
        if(string.IsNullOrEmpty(Root))return;
        if(state==PlayModeStateChange.EnteredPlayMode){output=Root;errors.Clear();fixtureReady=false;bootDeadline=EditorApplication.timeSinceStartup+120;Application.logMessageReceived+=Log;EditorApplication.update+=Boot;}
        if(state==PlayModeStateChange.EnteredEditMode){EditorApplication.update-=Boot;EditorApplication.update-=Tick;Application.logMessageReceived-=Log;SceneManager.sceneLoaded-=DisableModel;bool ok=File.Exists(Path.Combine(Root,"passed.txt"));SessionState.EraseString(Key);ChapterOneRuntime.VerificationSaveDirectory=null;if(Application.isBatchMode)EditorApplication.Exit(ok?0:1);}
    }
    static void Log(string message,string stack,LogType type)
    {
        if(type!=LogType.Error&&type!=LogType.Exception&&type!=LogType.Assert)return;
        if(stack.Contains("UnityEditor.Search.SearchDatabase")&&!stack.Contains("Assets/"))
            editorInfrastructureErrors.Add(message+"\n"+stack);
        else errors.Add(message+"\n"+stack);
    }
    static void Boot()
    {
        try
        {
            if(EditorApplication.timeSinceStartup>bootDeadline)throw new TimeoutException("Desktop readiness timed out");
            if(errors.Count>0)throw new Exception(errors[0]);
            var rt=UnityEngine.Object.FindAnyObjectByType<ChapterOneRuntime>();ctl=rt==null?null:rt.GetComponent<XingGuangController>();if(ctl?.Sim==null||ctl.View==null)return;
            if(!fixtureReady)
            {
                rt.useDiskSave=false;var lab=new XgSim();var house=new ChapterOneSim();house.S.prologue=2;house.S.appInstallState=2;house.S.money=100000;house.S.gpuModels.Add("gtx980ti");house.S.gpuCount=1;house.S.labState=JsonUtility.ToJson(lab.S);
                rt.SetSimulationForTests(house);ctl.GetType().GetMethod("BindSave",Flags).Invoke(ctl,new object[]{house});
                var boot=UnityEngine.Object.FindAnyObjectByType<Michsky.DreamOS.BootManager>(FindObjectsInactive.Include);if(boot!=null){boot.CancelInvoke();boot.StopAllCoroutines();if(boot.bootAnimator!=null)boot.bootAnimator.gameObject.SetActive(false);}
                var user=UnityEngine.Object.FindAnyObjectByType<Michsky.DreamOS.UserManager>(FindObjectsInactive.Include);if(user==null||!user.isActiveAndEnabled)throw new Exception("Desktop user unavailable");user.ShowDesktop();fixtureReady=true;return;
            }
            var binding=rt.GetComponent<ChapterOneDesktopRouter>()?.Get(XingGuangController.AppId);if(binding?.TaskbarShortcut==null||!binding.TaskbarShortcut.isActiveAndEnabled||binding.DesktopShortcut==null||!binding.DesktopShortcut.gameObject.activeInHierarchy)return;
            EditorApplication.update-=Boot;Start();
        }
        catch(Exception e){EditorApplication.update-=Boot;Finish("failed",e.ToString());}
    }
    const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static readonly Queue<Action> steps=new Queue<Action>();
    static readonly List<string> passed=new List<string>();
    static XingGuangController ctl;static XgBrainWindow window;static string output;static double next,deadline;
    static int hiddenMesh,feedbackBefore;static Vector2 beforeSize;static bool running;
    [Serializable] sealed class Report{public string status,detail;public string[] passed;public List<string> editorInfrastructureErrors=BrainWindowPlayQa.editorInfrastructureErrors;}
    [MenuItem("LingGuang/Verification/Brain window (isolated Play only)")]
    public static void Start()
    {
        if(running)throw new InvalidOperationException("Brain window QA already running");
        var rt=UnityEngine.Object.FindAnyObjectByType<ChapterOneRuntime>();
        if(!EditorApplication.isPlaying||rt==null)throw new InvalidOperationException("Requires Play Mode and a loaded desktop");
        string save=Path.GetFullPath(rt.SavePath);
        if(rt.useDiskSave&&!save.Contains(Path.DirectorySeparatorChar+"LocalVerification"+Path.DirectorySeparatorChar))throw new InvalidOperationException("Refused: isolate the save before running this test");
        rt.useDiskSave=false;ctl=rt.GetComponent<XingGuangController>();if(ctl==null)throw new InvalidOperationException("Controller missing");
        output=string.IsNullOrEmpty(Root)?Path.GetFullPath(Path.Combine("LocalVerification","BrainWindow",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"))):Root;Directory.CreateDirectory(output);
        var lab=new XgSim();var house=new ChapterOneSim();house.S.prologue=2;house.S.appInstallState=2;house.S.money=100000;house.S.gpuModels.Add("gtx980ti");house.S.gpuCount=1;house.S.labState=JsonUtility.ToJson(lab.S);
        rt.SetSimulationForTests(house);ctl.GetType().GetMethod("BindSave",Flags).Invoke(ctl,new object[]{house});
        EnableFixtureEconomy(rt);
        window=XgBrainWindow.Install(ctl);ctl.Open();passed.Clear();steps.Clear();
        steps.Enqueue(()=>{Check(ctl.Host.Money==house.S.money&&ctl.Host.Money>0,"Production host sees isolated wallet");Check(!window.Visible,"Not visible before purchase");Check(window.TaskButton==null||!window.TaskButton.gameObject.activeSelf,"No taskbar entry before purchase");var layer=LingGuangV05.Desktop.Story.PrologueDirector.Desk.layer.parent;Check(layer.Find("Brain Wallpaper")==null&&layer.Find("Brain Wallpaper Overlay")==null,"Legacy wallpaper roots removed");for(int i=0;i<12;i++){var first=ctl.Sim.Card(XgMemes.SenseDesk);ctl.Sim.Answer(XgMemes.SenseDesk,first.truth,ctl.Host);} double walletBefore=house.S.money;Check(ctl.Sim.BuyNode("train.curve",ctl.Host),"Buy prerequisite: "+ctl.Sim.Why(XgCatalog.Node("train.curve"),ctl.Host));Check(house.S.money<walletBefore,"Training curve purchase deducts actual wallet");walletBefore=house.S.money;Check(ctl.Sim.BuyNode("brain.window",ctl.Host),"Buy brain window");Check(house.S.money<walletBefore,"Brain window purchase deducts actual wallet");});
        steps.Enqueue(()=>{Check(window.Visible,"Purchase opens window");Check(Vector2.Distance(window.Window.sizeDelta,new Vector2(320,240))<.1f,"Default size 320x240");Check(window.AlwaysOnTop,"Default pinned");Check(window.TaskButton!=null,"Taskbar reopen button exists");ScreenCapture.CaptureScreenshot(Path.Combine(output,"purchased.png"));});
        steps.Enqueue(()=>{beforeSize=window.Window.sizeDelta;window.MoveResize(new Vector2(120,-80),true);Check(window.Window.sizeDelta.x>beforeSize.x&&window.Window.sizeDelta.y>beforeSize.y,"Resize grows window");window.MoveResize(new Vector2(99999,99999),false);var parent=(RectTransform)window.Window.parent;Check(window.Window.anchoredPosition.x+window.Window.rect.width*.5f<=parent.rect.xMax+.1f,"Drag clamps inside desktop");});
        steps.Enqueue(()=>{window.Window.Find("Title/Pin").GetComponent<Button>().onClick.Invoke();Check(!window.AlwaysOnTop,"Pin button releases topmost");window.Window.Find("Title/Minimize").GetComponent<Button>().onClick.Invoke();Check(!window.Visible,"Minimize hides window");hiddenMesh=window.Drawing.GetComponentInChildren<XgWallpaperConcepts>(true).MeshBuilds;});
        steps.Enqueue(()=>{Check(window.Drawing.GetComponentInChildren<XgWallpaperConcepts>(true).MeshBuilds==hiddenMesh,"Hidden drawing stops rebuilding");window.TaskButton.GetComponent<Button>().onClick.Invoke();Check(window.Visible,"Taskbar restores minimized window");window.Window.Find("Title/Close").GetComponent<Button>().onClick.Invoke();Check(!window.Visible&&window.Window!=null,"Close hides without destroying");});
        steps.Enqueue(()=>{window.TaskButton.GetComponent<Button>().onClick.Invoke();Check(window.Visible,"Taskbar reopens closed window");ctl.Sim.Profile.name="QA脑";});
        steps.Enqueue(()=>{var title=window.Window.Find("Title/Text").GetComponent<TMPro.TMP_Text>();Check(title.text.Contains("QA脑"),"Title follows player AI name");Check(window.TaskButton.GetComponentInChildren<TMPro.TMP_Text>().text.Contains("QA脑"),"Reopen entry follows player AI name");var f=window.Window.GetComponentInChildren<XgBrainWindowFeedback>();feedbackBefore=f.FeedbackCount;var card=ctl.Sim.Card(XgMemes.SenseDesk);ctl.Sim.Answer(XgMemes.SenseDesk,card.truth,ctl.Host);});
        steps.Enqueue(()=>{var f=window.Window.GetComponentInChildren<XgBrainWindowFeedback>();Check(f.FeedbackCount>feedbackBefore,"Real settled answer triggers feedback");ScreenCapture.CaptureScreenshot(Path.Combine(output,"answer-feedback.png"));});
        next=EditorApplication.timeSinceStartup+1;deadline=next+45;running=true;EditorApplication.update+=Tick;
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
    static void Check(bool condition,string name){if(!condition)throw new Exception(name);passed.Add(name);}
    static void Tick()
    {
        if(!running)return;
        try{if(errors.Count>0)throw new Exception(errors[0]);if(!EditorApplication.isPlaying)throw new Exception("Play Mode ended");double now=EditorApplication.timeSinceStartup;if(now>deadline)throw new TimeoutException();if(now<next)return;next=now+.8;if(steps.Count==0){Finish("passed","Window lifecycle and actual settled-answer path checked. Training/concept/ability effects still need the manual scenario list.");return;}steps.Dequeue()();}
        catch(Exception e){Finish("failed",e.ToString());}
    }
    static void Finish(string status,string detail){running=false;EditorApplication.update-=Tick;File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(new Report{status=status,detail=detail,passed=passed.ToArray()},true));Debug.Log("BrainWindow QA "+status+": "+output);if(!string.IsNullOrEmpty(Root)){if(status=="passed")File.WriteAllText(Path.Combine(output,"passed.txt"),detail);Application.logMessageReceived-=Log;EditorApplication.ExitPlaymode();}}
}
#endif
