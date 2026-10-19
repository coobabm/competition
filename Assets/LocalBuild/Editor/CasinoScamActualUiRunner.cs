#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LingGuangV05.CaseMenu;
using LingGuangV05.Core;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.LLM;
using LingGuangV05.Desktop.Media;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Desktop.Casino;
using TMPro;
using UnityEngine.UI;
using LingGuangV05.Runtime.Persistence;
using LingGuangV05.XingGuang;
using LingGuangV05.Desktop.OS;
using LingGuangV05.Desktop.Apps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

// Independent serial-editor owner. Never dispatches through SidebarLayoutChecks/BookLibrary.Run.
// Main/calendar/history prerequisites are explicit fixtures; casino LCD clicks and gossip waits are real.
[InitializeOnLoad]
public static partial class CasinoScamActualUiRunner
{
    const string Key="CasinoScamActualUiRunner.";
    const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static string Root=>SessionState.GetString(Key+"Root","");
    static readonly Stack<IEnumerator> routines=new Stack<IEnumerator>();
    static readonly List<InputDevice> muted=new List<InputDevice>();
    static Mouse mouse,previousMouse;
    static Keyboard keyboard,previousKeyboard;
    static InputSettings oldInput,qaInput;
    static IDisposable locale;
    static Func<bool> oldProbe,oldExclusive;
    static bool focusCaptured,finishing,failing;
    static int lastFrame;
    static double deadline;
    static string language="zh-CN";
    static Report report;
    [Serializable] sealed class SceneEntry {public string path;public bool loaded,active;}
    [Serializable] sealed class Setup {public List<SceneEntry> scenes=new List<SceneEntry>();}
    [Serializable] sealed class Report
    {
        public string status="running",failure="",unityVersion;
        public string scope=Scope;
        public List<string> focusObservations=new List<string>();
        public string input="Real LCD raycast-checked casino buttons via FilesWindowUiChecks. Actual foreground required. Browser/YY navigation and stage/history/date prerequisites are explicit fixtures. No physical input or model inference claim.";
        public int width,height,featureChecks,assertionCallsIncludingFrameGuards;
        public List<string> checks=new List<string>(),screenshots=new List<string>(),errors=new List<string>(),cleanupErrors=new List<string>();
    }
    static CasinoScamActualUiRunner(){EditorApplication.playModeStateChanged+=State;}
    [MenuItem("Tools/Local Verification/Casino scam actual UI")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||Root.Length>0)throw new InvalidOperationException("Run once from clean EditMode under the serial Unity owner.");
        for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save scene changes before this scene-switching QA.");
        string dir=Path.GetFullPath(Path.Combine("LocalVerification","CasinoScamActual",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));
        Directory.CreateDirectory(Path.Combine(dir,"Save"));Directory.CreateDirectory(Path.Combine(dir,"DesktopProfile"));
        SessionState.SetString(Key+"Setup",JsonUtility.ToJson(new Setup{scenes=EditorSceneManager.GetSceneManagerSetup().Select(x=>new SceneEntry{path=x.path,loaded=x.isLoaded,active=x.isActive}).ToList()}));
        SessionState.SetString(Key+"SaveOverride",ChapterOneRuntime.VerificationSaveDirectory??"");
        SessionState.SetString(Key+"Profile",ProfileField()?.GetValue(null) as string??"");
        SessionState.SetBool(Key+"HadReduce",PlayerPrefs.HasKey(StoryDesktopPresenter.ReduceMotionPref));
        SessionState.SetInt(Key+"Reduce",PlayerPrefs.GetInt(StoryDesktopPresenter.ReduceMotionPref,0));
        PlayerPrefs.SetInt(StoryDesktopPresenter.ReduceMotionPref,0);
        SessionState.SetInt(Key+"FPS",Application.targetFrameRate);SessionState.SetInt(Key+"VSync",QualitySettings.vSyncCount);SessionState.SetBool(Key+"Background",Application.runInBackground);
        SessionState.SetString(Key+"Root",dir);
        try
        {
            var type=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            Need(type!=null,"GameView editor type available");
            SessionState.SetBool(Key+"CreatedView",!Resources.FindObjectsOfTypeAll<EditorWindow>().Any(x=>x.GetType()==type));
            var view=EditorWindow.GetWindow(type);PrepareGameView(view);view.Show();view.Focus();view.Repaint();
            EditorSceneManager.OpenScene("Assets/LingGuangV05/Scenes/Main.unity",OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }
        catch{RestoreEditor();throw;}
    }
    static FieldInfo ProfileField()=>typeof(DreamOSDataManager).GetField("dataPath",BindingFlags.Static|BindingFlags.NonPublic);
    // Same fixed-resolution fixture API used by PlayerKnowledgeMenuInputQa; preserve the user's previous selection.
    static object ViewSizeGroup()
    {
        var type=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizes");
        var singleton=typeof(ScriptableSingleton<>).MakeGenericType(type);
        return type.GetMethod("GetGroup").Invoke(singleton.GetProperty("instance").GetValue(null),new object[]{GameViewSizeGroupType.Standalone});
    }
    static void PrepareGameView(EditorWindow view)
    {
        var selected=view.GetType().GetProperty("selectedSizeIndex",F);
        SessionState.SetInt(Key+"ViewIndex",(int)selected.GetValue(view));
        SessionState.SetBool(Key+"ViewSizePrepared",true);
        var group=ViewSizeGroup();int total=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);
        int builtIn=(int)group.GetType().GetMethod("GetBuiltinCount").Invoke(group,null);
        string tag="CasinoScamQA-"+Guid.NewGuid().ToString("N");
        SessionState.SetString(Key+"ViewTag",tag);SessionState.SetInt(Key+"ViewCustom",total-builtIn);
        var assembly=typeof(EditorWindow).Assembly;
        var mode=assembly.GetType("UnityEditor.GameViewSizeType");
        var size=Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"),new object[]{Enum.Parse(mode,"FixedResolution"),1280,720,tag});
        group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});selected.SetValue(view,total);
    }
    static void RestoreGameView()
    {
        if(!SessionState.GetBool(Key+"ViewSizePrepared",false))return;
        var group=ViewSizeGroup();var view=Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(x=>x.GetType().FullName=="UnityEditor.GameView");
        int total=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);
        int builtIn=(int)group.GetType().GetMethod("GetBuiltinCount").Invoke(group,null);
        if(view!=null)view.GetType().GetProperty("selectedSizeIndex",F).SetValue(view,Mathf.Clamp(SessionState.GetInt(Key+"ViewIndex",0),0,Mathf.Max(0,total-1)));
        int custom=SessionState.GetInt(Key+"ViewCustom",-1);string tag=SessionState.GetString(Key+"ViewTag","");
        var labels=group.GetType().GetMethod("GetDisplayTexts").Invoke(group,null) as string[];
        if(custom>=0&&tag.Length>0&&labels!=null&&builtIn+custom<labels.Length&&labels[builtIn+custom].Contains(tag))
            group.GetType().GetMethod("RemoveCustomSize",F).Invoke(group,new object[]{custom});
        SessionState.EraseBool(Key+"ViewSizePrepared");SessionState.EraseString(Key+"ViewTag");
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Isolate()
    {
        if(Root.Length==0)return;
        ChapterOneRuntime.VerificationSaveDirectory=Path.Combine(Root,"Save");
        ProfileField()?.SetValue(null,Path.Combine(Root,"DesktopProfile")+Path.DirectorySeparatorChar);
        Application.targetFrameRate=30;QualitySettings.vSyncCount=0;Application.runInBackground=true;
        SceneManager.sceneLoaded-=DisableModels;SceneManager.sceneLoaded+=DisableModels;
    }
    static void DisableModels(Scene scene,LoadSceneMode mode)
    {
        if(Root.Length==0)return;
        foreach(var rt in scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<ChapterOneRuntime>(true)))
        {
            // Scene callback precedes AfterSceneLoad model Attach/Begin and runtime Start.
            var llm=rt.GetComponent<LocalLlm>()??rt.gameObject.AddComponent<LocalLlm>();llm.enabled=false;
            var embed=rt.GetComponent<LocalEmbedder>()??rt.gameObject.AddComponent<LocalEmbedder>();embed.enabled=false;
            rt.tickEnabled=false;
        }
    }
    static void State(PlayModeStateChange state)
    {
        if(Root.Length==0)return;
        if(state==PlayModeStateChange.EnteredPlayMode)
        {
            finishing=failing=false;lastFrame=-1;deadline=EditorApplication.timeSinceStartup+600;
            report=new Report{unityVersion=Application.unityVersion};routines.Clear();routines.Push(RunFrames());
            oldProbe=StoryFocus.Probe;oldExclusive=StoryFocus.ExclusiveProbe;focusCaptured=true;
            Application.logMessageReceived+=Log;EditorApplication.update+=Tick;AssemblyReloadEvents.beforeAssemblyReload+=Reload;Save();
        }
        if(state==PlayModeStateChange.ExitingPlayMode&&!finishing)
        {
            if(report!=null){report.status="interrupted";report.failure="PlayMode stopped before completion";}
            CleanupPlay();Save();
        }
        if(state==PlayModeStateChange.EnteredEditMode)
        {
            string dir=Root;bool passed=File.Exists(Path.Combine(dir,"passed.txt"));
            try{CleanupPlay();if(report!=null&&report.cleanupErrors.Count>0)passed=false;Save();RestoreEditor();}
            catch(Exception ex){passed=false;File.AppendAllText(Path.Combine(dir,"cleanup-errors.txt"),ex+Environment.NewLine);}
            finally{Debug.Log("CASINO_SCAM_ACTUAL "+(passed?"PASS":"FAIL/INTERRUPTED")+": "+dir);if(Application.isBatchMode||Environment.GetCommandLineArgs().Contains("-verificationExit"))EditorApplication.Exit(passed?0:1);}
        }
    }
    static void Log(string message,string stack,LogType type)
    {
        if(report==null||finishing)return;
        if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)report.errors.Add(message+"\n"+stack);
    }
    static void Need(bool value,string message){if(report!=null)report.assertionCallsIncludingFrameGuards++;if(!value)throw new InvalidOperationException("Casino scam runner: "+message);}
    static void Check(bool value,string message){Need(value,message);report.checks.Add(message);report.featureChecks=report.checks.Count;}
    static void Save(){if(report!=null&&Root.Length>0)File.WriteAllText(Path.Combine(Root,"report.json"),JsonUtility.ToJson(report,true));}
    static IEnumerator Frames(int count=3){while(count-->0)yield return null;}
    static IEnumerator Until(Func<bool> condition,string message,double seconds=20)
    {
        double until=EditorApplication.timeSinceStartup+seconds;
        while(!condition()){Need(EditorApplication.timeSinceStartup<until,message);yield return null;}
        yield return Frames();
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying||finishing||lastFrame==Time.frameCount)return;lastFrame=Time.frameCount;
        try
        {
            if(!failing){Need(EditorApplication.timeSinceStartup<deadline,"600-second overall deadline");Need(report.errors.Count==0,"runtime error: "+report.errors.FirstOrDefault());}
            var view=Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(x=>x.GetType().FullName=="UnityEditor.GameView");if(view!=null)view.Repaint();
            // Nested enumerators are owned/disposed here; one ordinary yield always waits a real frame.
            while(routines.Count>0)
            {
                var routine=routines.Peek();
                if(!routine.MoveNext()){routines.Pop();(routine as IDisposable)?.Dispose();continue;}
                if(routine.Current is IEnumerator child){routines.Push(child);continue;}
                return;
            }
            Finish(!failing);
        }
        catch(Exception ex)
        {
            if(failing){report.cleanupErrors.Add("Failure capture: "+ex);Finish(false);return;}
            failing=true;report.status="failed";report.failure=ex.ToString();GatherChecks();DisposeRoutines();Save();
            routines.Push(CaptureFailure());
        }
    }
    static IEnumerator RunFrames()
    {
        SetupInput();locale=GameText.Bind(()=>language);
        yield return Until(()=>Object.FindAnyObjectByType<XingGuangController>()?.View!=null,"initial Main controller ready",60);
        var ctl=Object.FindAnyObjectByType<XingGuangController>();var rt=ctl.runtime;
        Need(rt!=null&&ctl.Sim!=null,"initial runtime and lab ready");rt.useDiskSave=false;
        var house=Fixture(false);
        QuietDrivers();rt.SetSimulationForTests(house);
        typeof(XingGuangController).GetMethod("BindSave",F).Invoke(ctl,new object[]{house});
        var boot=Object.FindAnyObjectByType<BootManager>(FindObjectsInactive.Include);
        if(boot!=null){boot.CancelInvoke();boot.StopAllCoroutines();if(boot.bootAnimator!=null)boot.bootAnimator.gameObject.SetActive(false);}
        var user=Object.FindAnyObjectByType<UserManager>(FindObjectsInactive.Include);Need(user!=null&&user.isActiveAndEnabled,"native desktop user manager ready");user.ShowDesktop();
        yield return Until(()=>DesktopReady(rt),"initial desktop shortcuts ready",30);
        if(PrologueDirector.Desk==null)
        {
            var director=Object.FindAnyObjectByType<PrologueDirector>(FindObjectsInactive.Include);
            Need(director!=null&&(bool)typeof(PrologueDirector).GetMethod("BuildDesk",F).Invoke(director,null),"production desktop presentation host built for isolated fixture");
        }
        IsolateTicks(ctl,false);QuietDrivers();var notifications=rt.GetComponent<DesktopNotifications>();if(notifications!=null)notifications.enabled=false;
        StoryFocus.Probe=()=>false; // Explicit independent-story isolation, never a focus override.
        yield return Until(()=>Screen.width==1280&&Screen.height==720,"fixed 1280x720 GameView fixture applied",10);
        Check(Screen.width==1280&&Screen.height==720,"explicit 1280x720 GameView fixture available; source LCD input remains production-mapped");
        yield return Foreground("initial-main");
        foreach(bool english in new[]{false,true}) yield return CasinoFrames(ctl,english);
        yield return StaleGossip(ctl);
        GatherChecks();Check(report.errors.Count==0,"no runtime errors during casino scam checks");
        string full="casino-scam-pass-full-frame.png";ScreenCapture.CaptureScreenshot(Path.Combine(Root,full));
        yield return ValidateScreenshot(full,true);
    }
    static bool DesktopReady(ChapterOneRuntime rt)
    {
        if(rt==null)return false;var app=rt.GetComponent<ChapterOneDesktopRouter>()?.Get(XingGuangController.AppId);
        return app?.Window!=null&&app.TaskbarShortcut!=null&&app.TaskbarShortcut.isActiveAndEnabled&&app.DesktopShortcut!=null&&app.DesktopShortcut.gameObject.activeInHierarchy;
    }
    static void QuietDrivers()
    {
        foreach(var c in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
        {
            string n=c.GetType().Name;
            if(n=="YYChatHub"||n=="YYGirlfriend"||n=="AiJoinsYy"||n=="StoryDirector"||n=="StoryDesktopPresenter"||n=="PrologueDirector"||n=="SpiderIntroScene"||n=="LoveQuestionCutscene"||n=="StageOneTutor"||n=="LaoZhouYY"||n=="ZhouCardLoan"||n=="XgPublicDiscourseNotifier"||n=="XgDailyUnlockNotifier"||n=="XgResidentSpider"||n=="XgHoloCard"||n=="XgAppAcquisitionHost"||n=="XgEndingPreparationHost"||n=="XgDailyLifeHost"||n=="XgStageCurtain"||n=="OriginCurtain")
            {c.CancelInvoke();c.StopAllCoroutines();c.enabled=false;}
        }
    }
    static void IsolateTicks(XingGuangController ctl,bool disk)
    {
        var rt=ctl.runtime;rt.tickEnabled=false;rt.useDiskSave=disk;ctl.enabled=false;
        // Keep the real household host live: TestMode deliberately returns zero money.
        // Economy ticks, disk writes and both model services remain separately isolated.
        typeof(ChapterOneRuntime).GetProperty("TestMode",F).GetSetMethod(true).Invoke(rt,new object[]{false});
        Need(!rt.TestMode&&(LocalLlm.Instance==null||!LocalLlm.Instance.enabled&&!LocalLlm.Instance.Ready)&&(LocalEmbedder.Instance==null||!LocalEmbedder.Instance.enabled&&!LocalEmbedder.Instance.Ready),"isolated paused live household host with both model services disabled");
        Need(ctl.Host.Money==rt.Sim.S.money,"real host exposes the isolated household wallet");
    }
    static void SetLanguage(bool english){language=english?GameText.English:"zh-CN";GameText.PublishChanged();}
    static void CaptureClick(RectTransform target){Need(Application.isFocused&&!PlayerWindowState.IsMinimized,"actual foreground required for LCD click");FilesWindowUiChecks.Click(target);}
    static void CaptureLcd(string name)
    {Need(QaDesktopCapture.Save(Path.Combine(Root,name)),"actual LCD image saved: "+name);report.screenshots.Add(name);Save();}
    static IEnumerator ValidateScreenshot(string name,bool add)
    {
        Need(!string.IsNullOrEmpty(name),"full-frame screenshot callback ran");string path=Path.Combine(Root,name);
        yield return Until(()=>File.Exists(path)&&new FileInfo(path).Length>100,"full-frame PNG completed",12);
        var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
        try
        {
            Need(ImageConversion.LoadImage(texture,File.ReadAllBytes(path)),"whole GameView PNG decodes");
            Need(texture.width==Screen.width&&texture.height==Screen.height&&texture.width>320&&texture.height>200,"whole GameView PNG uses actual screen dimensions");
            var pixels=texture.GetPixels32();int min=765,max=0;
            for(int i=0;i<pixels.Length;i+=Math.Max(1,pixels.Length/10000)){int sum=pixels[i].r+pixels[i].g+pixels[i].b;min=Math.Min(min,sum);max=Math.Max(max,sum);}
            Need(max>30&&max-min>30,"whole GameView PNG is not blank or uniformly black");
            report.width=texture.width;report.height=texture.height;if(add)report.screenshots.Add(name);
        }
        finally{Object.Destroy(texture);}
        Save();
    }
    static IEnumerator CaptureFailure()
    {
        string name="casino-scam-failure-full-frame.png";ScreenCapture.CaptureScreenshot(Path.Combine(Root,name));
        yield return ValidateScreenshot(name,true);
    }
    static void SetupInput()
    {
        oldInput=InputSystem.settings;qaInput=Object.Instantiate(oldInput);qaInput.hideFlags=HideFlags.HideAndDontSave;
        qaInput.backgroundBehavior=oldInput.backgroundBehavior; // Preserve actual focus handling; all interactions have a foreground gate.
        qaInput.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        qaInput.updateMode=InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;InputSystem.settings=qaInput;
        previousMouse=Mouse.current;previousKeyboard=Keyboard.current;muted.Clear();
        foreach(var d in InputSystem.devices.Where(x=>x.enabled).ToArray()){muted.Add(d);InputSystem.DisableDevice(d);}
        mouse=InputSystem.AddDevice<Mouse>("CasinoScamActualQaMouse");keyboard=InputSystem.AddDevice<Keyboard>("CasinoScamActualQaKeyboard");mouse.MakeCurrent();keyboard.MakeCurrent();
        InputSystem.onDeviceChange+=MuteExternal;
    }
    static void MuteExternal(InputDevice device,InputDeviceChange change)
    {
        if(finishing||device==mouse||device==keyboard||!device.enabled)return;
        if(change!=InputDeviceChange.Added&&change!=InputDeviceChange.Enabled&&change!=InputDeviceChange.Reconnected)return;
        if(!muted.Contains(device))muted.Add(device);InputSystem.DisableDevice(device);
    }
    static void Pointer(Vector2 point,bool down)
    {Need(mouse!=null&&mouse.added,"owned virtual mouse available");mouse.MakeCurrent();InputSystem.QueueStateEvent(mouse,new MouseState{position=point}.WithButton(MouseButton.Left,down));}
    static void GatherChecks() { Save(); }
    static void DisposeRoutines()
    {
        while(routines.Count>0)try{(routines.Pop() as IDisposable)?.Dispose();}catch(Exception ex){report?.cleanupErrors.Add("Iterator disposal: "+ex);}
    }
    static void CleanupPlay()
    {
        EditorApplication.update-=Tick;Application.logMessageReceived-=Log;AssemblyReloadEvents.beforeAssemblyReload-=Reload;SceneManager.sceneLoaded-=DisableModels;
        DisposeRoutines();
        // Disable disk first: ExitPlaymode callbacks must not overwrite the checked archive snapshot.
        foreach(var rt in Object.FindObjectsByType<ChapterOneRuntime>(FindObjectsInactive.Include))if(rt!=null){rt.useDiskSave=false;rt.tickEnabled=false;}
        InputSystem.onDeviceChange-=MuteExternal;
        if(mouse!=null&&mouse.added)InputSystem.RemoveDevice(mouse);mouse=null;
        if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);keyboard=null;
        foreach(var d in muted)if(d!=null&&d.added)InputSystem.EnableDevice(d);muted.Clear();
        if(previousMouse!=null&&previousMouse.added)previousMouse.MakeCurrent();if(previousKeyboard!=null&&previousKeyboard.added)previousKeyboard.MakeCurrent();previousMouse=null;previousKeyboard=null;
        if(oldInput!=null)InputSystem.settings=oldInput;oldInput=null;if(qaInput!=null)Object.DestroyImmediate(qaInput);qaInput=null;
        locale?.Dispose();locale=null;
        if(focusCaptured){StoryFocus.Probe=oldProbe;StoryFocus.ExclusiveProbe=oldExclusive;focusCaptured=false;}
    }
    static void Finish(bool ok)
    {
        finishing=true;GatherChecks();
        try{CleanupPlay();}catch(Exception ex){report.cleanupErrors.Add(ex.ToString());ok=false;}
        ok=ok&&report.cleanupErrors.Count==0&&report.errors.Count==0;
        report.status=ok?"passed":"failed";Save();
        if(ok)File.WriteAllText(Path.Combine(Root,"passed.txt"),"Casino scam actual UI passed; see report for explicit history/date/legacy hand fixtures, LCD clicks, real elapsed gossip waits, isolated slot6 IO and stale callback checks. Not natural progression, physical input or model inference.");
        EditorApplication.ExitPlaymode();
    }
    static void Reload()
    {
        if(Root.Length==0)return;finishing=true;if(report!=null){report.status="interrupted";report.failure="Assembly reload interrupted casino scam QA";}
        CleanupPlay();Save();
    }
    static void RestoreEditor()
    {
        string previous=SessionState.GetString(Key+"SaveOverride","");ChapterOneRuntime.VerificationSaveDirectory=previous.Length==0?null:previous;
        string profile=SessionState.GetString(Key+"Profile","");ProfileField()?.SetValue(null,profile.Length==0?null:profile);
        Application.targetFrameRate=SessionState.GetInt(Key+"FPS",-1);QualitySettings.vSyncCount=SessionState.GetInt(Key+"VSync",0);Application.runInBackground=SessionState.GetBool(Key+"Background",false);
        if(SessionState.GetBool(Key+"HadReduce",false))PlayerPrefs.SetInt(StoryDesktopPresenter.ReduceMotionPref,SessionState.GetInt(Key+"Reduce",0));
        else PlayerPrefs.DeleteKey(StoryDesktopPresenter.ReduceMotionPref);
        RestoreGameView();
        if(SessionState.GetBool(Key+"CreatedView",false))Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(x=>x.GetType().FullName=="UnityEditor.GameView")?.Close();
        var setup=JsonUtility.FromJson<Setup>(SessionState.GetString(Key+"Setup","{}"));
        if(setup?.scenes!=null&&setup.scenes.Count>0)EditorSceneManager.RestoreSceneManagerSetup(setup.scenes.Select(x=>new SceneSetup{path=x.path,isLoaded=x.loaded,isActive=x.active}).ToArray());
        else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        SessionState.EraseString(Key+"Root");
    }
    const string Scope="Independent Main fixture at casino unlock stage, fixed calendar and 29 historical watched rounds. Actual LCD final bet triggers freeze. Both languages cover paid/refused/ignored, isolated slot6 freeze/closed roundtrip and stale captured pay/quit UnityEvents. Ignored uses an explicitly advanced game date and live CasinoDesktop.Update. Accepted blackjack uses a declared legacy frozen-active-hand state, real disk restore and LCD Stand. Ambient YY Update/coroutines, girlfriend and AI-join cutscene are frozen; real CasinoDesktop delivery, YY Receive, view and save subscriptions remain active. Real 40-second gossip delivery is checked against exact public text and persisted receipts; old-owner pending gossip is checked across a save switch. No natural progression, physical input, model inference, auto-save success or save-failure recovery claim.";
    static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,F|BindingFlags.Static).GetValue(target);
    static YYConversation Group=>YYChatHub.Instance.Conversation(YYChatHub.EraGroup);
    static CasinoPage Page=>Object.FindObjectsByType<CasinoPage>(FindObjectsInactive.Exclude).FirstOrDefault(p=>p.isActiveAndEnabled);
    static Button Button(string name)=>Page?.GetComponentsInChildren<Button>(true).FirstOrDefault(b=>b.name==name);
    static bool Layer(string name)=>Page!=null&&Page.GetComponentsInChildren<RectTransform>(true).Any(x=>x.name==name&&x.gameObject.activeInHierarchy);
    static bool ReadyButton(string name){var b=Button(name);return b!=null&&b.gameObject.activeInHierarchy&&b.IsInteractable();}
    static void Click(string name){Need(ReadyButton(name),"visible interactable casino button: "+name);CaptureClick((RectTransform)Button(name).transform);}
    static bool WindowReady(WindowManager w)
    {
        if(w==null||!w.isOn||!w.gameObject.activeInHierarchy)return false;
        foreach(var g in w.GetComponentsInParent<CanvasGroup>(true))if(g.alpha<.95f||!g.blocksRaycasts||!g.interactable)return false;
        var a=w.windowAnimator;return a==null||!a.isActiveAndEnabled||!a.IsInTransition(0)&&a.GetCurrentAnimatorStateInfo(0).normalizedTime>=1;
    }
    static string FocusState(string phase,double started,int firstFrame,bool focused,bool minimized)
    {
        var window=EditorWindow.focusedWindow;
        return "phase="+phase+" language="+language+" utc="+DateTime.UtcNow.ToString("o")+
            " elapsed="+(Time.realtimeSinceStartupAsDouble-started).ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+
            " frame="+Time.frameCount+" frameDelta="+(Time.frameCount-firstFrame)+
            " unscaledTime="+Time.unscaledTime.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+
            " isFocused="+focused+" minimized="+minimized+" minimizedProbePresent="+(PlayerWindowState.EditorMinimizedProbe!=null)+
            " editorPaused="+EditorApplication.isPaused+" playing="+EditorApplication.isPlaying+
            " focusedEditorWindow="+(window==null?"none":window.GetType().FullName);
    }
    static void FocusSnapshot(string phase,double started,int firstFrame)
    {report.focusObservations.Add(FocusState(phase,started,firstFrame,Application.isFocused,PlayerWindowState.IsMinimized));}
    static void RequireForeground(string phase,double started,int firstFrame)
    {
        bool focused=Application.isFocused,minimized=PlayerWindowState.IsMinimized;
        // Keep the same strict predicate and assertion. Persist the failed sample in the existing
        // report list as well as the exception; Tick already saves that report on failure.
        string failure=focused&&!minimized?null:FocusState(phase,started,firstFrame,focused,minimized);
        if(failure!=null)report.focusObservations.Add(failure);
        Need(focused&&!minimized,failure??phase);
    }
    static IEnumerator Foreground(string phase="foreground-stabilization")
    {
        double started=Time.realtimeSinceStartupAsDouble,end=started+60,stableSince=-1;int firstFrame=Time.frameCount;
        while(stableSince<0||Time.realtimeSinceStartupAsDouble-stableSince<3)
        {
            Need(Time.realtimeSinceStartupAsDouble<end,"actual foreground must stabilize for three seconds; never fabricate focus | "+FocusState(phase,started,firstFrame,Application.isFocused,PlayerWindowState.IsMinimized));
            if(Application.isFocused&&!PlayerWindowState.IsMinimized)
            {if(stableSince<0)stableSince=Time.realtimeSinceStartupAsDouble;}
            else stableSince=-1;
            yield return null;
        }
    }
    static IEnumerator WaitSeconds(double seconds,string phase)
    {
        double started=Time.realtimeSinceStartupAsDouble,end=started+seconds;int firstFrame=Time.frameCount;
        FocusSnapshot(phase+"/begin",started,firstFrame);
        while(Time.realtimeSinceStartupAsDouble<end){RequireForeground("actual foreground retained during timed observation: "+phase,started,firstFrame);yield return null;}
        FocusSnapshot(phase+"/end",started,firstFrame);
    }
    static IEnumerator VisibleUntil(Func<bool> condition,string message,double seconds=25)
    {
        double started=Time.realtimeSinceStartupAsDouble,end=started+seconds;int firstFrame=Time.frameCount;
        FocusSnapshot("visible-until/"+message+"/begin",started,firstFrame);
        while(!condition()){RequireForeground("visible-until/"+message,started,firstFrame);Need(Time.realtimeSinceStartupAsDouble<end,message);yield return null;}
        FocusSnapshot("visible-until/"+message+"/end",started,firstFrame);
    }
    static ChapterOneSim Fixture(bool english)
    {
        var lab=new XgSim();lab.BeginOpening();lab.English=english;
        int stage=XgSim.FeatureUnlock["casino"];
        lab.S.stage=lab.S.stageVision=lab.S.stageSequence=stage;lab.S.abilitiesEmerged=Enumerable.Range(1,stage).ToList();
        lab.S.openingFocus=false;lab.S.teachingFlow=false;lab.S.spiderIntroDone=lab.S.firstWordsSaid=true;
        lab.S.teachIntroSaid=lab.S.teachThirdSaid=lab.S.teachTreeSaid=lab.S.teachTrainSaid=true;lab.MarkAfterthought("first.drift");
        var yy=new YYState{selected=YYChatHub.EraGroup,seedZhouHand=true,seedJieAsk=true};
        foreach(var c in YYChatHub.Contacts)yy.conversations.Add(new YYConversation{id=c.id});
        var house=new ChapterOneSim();house.S.prologue=2;house.S.appInstallState=2;house.S.money=913579;house.S.aiName="PRIVATE_AI_7391";
        house.S.labState=JsonUtility.ToJson(lab.S);house.S.chatState=JsonUtility.ToJson(yy);
        GameCalendar.Advance(house.S,GameCalendar.DayIndex(new DateTime(2016,6,6)));house.S.rentDay=GameCalendar.CurrentDay(house.S);house.S.broadbandMonth=201612;
        return house;
    }
    static void Bind(XingGuangController ctl,bool english)
    {
        SetLanguage(english);var house=Fixture(english);ctl.runtime.SetSimulationForTests(house);
        typeof(XingGuangController).GetMethod("BindSave",F).Invoke(ctl,new object[]{house});
        IsolateTicks(ctl,false);QuietDrivers();ctl.Sim.Clock=100;
        Need(ctl.BoundTo(house)&&YYChatHub.Instance.BoundTo(house),"lab and YY share exact isolated house owner");
        // The hub stays bound/subscribed: live CasinoDesktop.Receive and YY rendering need no ambient Update.
        Need(!YYChatHub.Instance.enabled,"ambient YY updates stay frozen while real Receive and persistence remain bound");
        Need(CasinoDesktop.Instance!=null,"production casino desktop installed");CasinoDesktop.Instance.enabled=true;
    }
    static IEnumerator OpenCasino(XingGuangController ctl)
    {
        Need(CasinoDesktop.Instance.OpenSite(),"native local-only casino browser navigation fixture");
        yield return Until(()=>Page!=null&&ReferenceEquals(Field<XgSim>(Page,"bound"),ctl.Sim),"casino page bound to current save");
        var browser=Object.FindAnyObjectByType<WebBrowserApp>(FindObjectsInactive.Include);
        yield return Until(()=>WindowReady(browser.GetComponentInParent<WindowManager>(true)),"browser open animation completed");
        yield return Frames(4);yield return Foreground("casino-open");
    }
    static void NearFreeze(XgSim sim)
    {
        sim.S.casinoWatching=true;sim.S.casinoWatchRound=0;sim.S.casinoWatchNet=0;sim.S.casinoSaidFirst=true;
        sim.Casino.rounds=29;sim.Casino.wagered=290;sim.Casino.returned=290;sim.Casino.rng=12345;
        // Prior public gossip receipts are fixtures. Only beat 3's delivery is under observation.
        sim.MarkCasinoGossip(1);sim.MarkCasinoGossip(2);
        Check(true,"Declared prerequisite: 29 watched historical rounds, wagered=returned=290, prior public gossip receipts 1/2; the final stake is an actual LCD click");
    }
    static IEnumerator FreezeByClick(XingGuangController ctl,bool english)
    {
        Bind(ctl,english);NearFreeze(ctl.Sim);yield return OpenCasino(ctl);
        yield return VisibleUntil(()=>ReadyButton("Game slots"),"game selector ready");Click("Game slots");yield return Frames();Click("Stake 10");yield return Frames();
        yield return VisibleUntil(()=>ReadyButton("Place Bet"),"final real bet ready");
        double before=ctl.runtime.Sim.S.money;Click("Place Bet");
        Need(ctl.Sim.Casino.rounds==30&&ctl.Sim.CasinoFrozenNow,"actual accepted final bet triggers product freeze");
        var round=ctl.Sim.Casino.history.Last();
        Check(ctl.runtime.Sim.S.money==before-round.stake+round.returned-ctl.Sim.S.casinoFrozen,"real settlement and frozen wallet deduction agree");
        yield return VisibleUntil(()=>Layer("Risk Notice")&&ReadyButton("Pay Deposit")&&ReadyButton("Quit Casino"),"natural reveal completes and risk notice appears",30);
        Check(!ReadyButton("Place Bet"),"risk notice prohibits new stake");
    }
    static void IsolatedDisk(ChapterOneRuntime rt)
    {
        string expected=Path.GetFullPath(Path.Combine(Root,"Save"))+Path.DirectorySeparatorChar;
        Need(Path.GetFullPath(rt.SavePath).StartsWith(expected,StringComparison.OrdinalIgnoreCase),"slot IO confined to unique verification root");
        Need(Path.GetFullPath(ChapterOneRuntime.VerificationSaveDirectory).TrimEnd(Path.DirectorySeparatorChar)==expected.TrimEnd(Path.DirectorySeparatorChar),"save override matches verified root");
    }
    static void SaveSlot(XingGuangController ctl,string label)
    {
        IsolatedDisk(ctl.runtime);ctl.runtime.useDiskSave=true;
        try{Need(ctl.runtime.SaveSlot(6),"actual slot6 save "+label+": "+ctl.runtime.SaveStatus);}
        finally{ctl.runtime.useDiskSave=false;}
        string slot=Path.Combine(Root,"Save","slots","6","chapter-one.sav");Need(File.Exists(slot)&&new FileInfo(slot).Length>100,"actual nonempty slot6 file: "+label);
        Directory.CreateDirectory(Path.Combine(Root,"disk-evidence"));File.Copy(slot,Path.Combine(Root,"disk-evidence",label+".sav"),true);
    }
    static void LoadSlot(XingGuangController ctl)
    {
        IsolatedDisk(ctl.runtime);ctl.runtime.useDiskSave=true;
        try{Need(ctl.runtime.LoadArchive(6),"actual isolated slot6 load: "+ctl.runtime.SaveStatus);}
        finally{ctl.runtime.useDiskSave=false;ctl.runtime.tickEnabled=false;}
        IsolateTicks(ctl,false);QuietDrivers();ctl.Sim.Clock=100;CasinoDesktop.Instance.enabled=true;
        Need(ctl.BoundTo(ctl.runtime.Sim)&&YYChatHub.Instance.BoundTo(ctl.runtime.Sim),"disk load rebinds both lab and YY to restored owner");
    }
    static string Snapshot(XingGuangController ctl)
    {
        var s=ctl.Sim.S;var c=ctl.Sim.Casino;
        return string.Join("|",ctl.runtime.Sim.S.money,s.casinoScam,s.casinoFrozen,s.casinoFrozenDay,s.casinoFeePaid,s.casinoScamOutcome,s.casinoLearned,
            s.casinoWatching,s.casinoWatchRound,s.casinoWatchNet,s.casinoAiSeq,s.casinoAiFreezeLine,c.rounds,c.wagered,c.returned,
            JsonUtility.ToJson(c.blackjack),ctl.Sim.ExtraSamples("spam"),string.Join(";",s.askedLog.Where(x=>x.sourceKind==XgSim.CasinoSourceKind).Select(x=>JsonUtility.ToJson(x))),
            string.Join(";",s.tutorSeen.Where(x=>x.StartsWith("casino.gossip"))),JsonUtility.ToJson(Group));
    }
    static IEnumerator FrozenDiskAndStale(XingGuangController ctl,bool english,string tag)
    {
        var old=ctl.Sim;var oldHouse=ctl.runtime.Sim;var oldPage=Page;var pay=Button("Pay Deposit").onClick;var quit=Button("Quit Casino").onClick;
        string expected=Snapshot(ctl);SaveSlot(ctl,tag+"-frozen");
        Bind(ctl,english);
        // Both owners deliberately remain frozen: a missing owner guard would close one and/or charge the new wallet.
        ctl.Sim.S.casinoScam=1;ctl.Sim.S.casinoFrozenDay=ctl.Sim.Today;ctl.Sim.MarkCasinoGossip(1);ctl.Sim.MarkCasinoGossip(2);
        // Keep the same visible page component: old closures must fail their owner guard, not merely page visibility.
        yield return VisibleUntil(()=>ReferenceEquals(Page,oldPage)&&ReferenceEquals(Field<XgSim>(Page,"bound"),ctl.Sim)&&Layer("Risk Notice"),"same visible page rebuilt for new frozen owner");
        double oldMoney=oldHouse.S.money,newMoney=ctl.runtime.Sim.S.money;double oldSamples=old.ExtraSamples("spam"),newSamples=ctl.Sim.ExtraSamples("spam");
        pay.Invoke();quit.Invoke();yield return Frames();
        Check(old.CasinoFrozenNow&&ctl.Sim.CasinoFrozenNow&&oldHouse.S.money==oldMoney&&ctl.runtime.Sim.S.money==newMoney&&old.ExtraSamples("spam")==oldSamples&&ctl.Sim.ExtraSamples("spam")==newSamples,
            "captured old Pay/Quit callbacks cannot mutate either frozen owner (explicit late-callback fixture, not a physical click): "+tag);
        LoadSlot(ctl);Check(Snapshot(ctl)==expected,"actual disk restore preserves frozen wallet/scam/hand/receipts/group history: "+tag);
        yield return OpenCasino(ctl);yield return VisibleUntil(()=>Layer("Risk Notice"),"loaded freeze notice visible");
    }
    static IEnumerator CasinoFrames(XingGuangController ctl,bool english)
    {
        string lang=english?"en":"zh";
        foreach(string outcome in new[]{XgSim.CasinoPaid,XgSim.CasinoRefused,XgSim.CasinoIgnored})
        {
            string tag=lang+"-"+outcome;yield return FreezeByClick(ctl,english);CaptureLcd(tag+"-01-frozen.png");
            yield return FrozenDiskAndStale(ctl,english,tag);
            double before=ctl.runtime.Sim.S.money;long fee=ctl.Sim.CasinoFee;double samples=ctl.Sim.ExtraSamples("spam");
            if(outcome==XgSim.CasinoIgnored)
            {
                int day=GameCalendar.CurrentDay(ctl.runtime.Sim.S);Need(GameCalendar.Advance(ctl.runtime.Sim.S,day+1),"explicit next-day calendar fixture applied");
                ctl.Sim.Today=GameCalendar.Yyyymmdd(GameCalendar.Now(ctl.runtime.Sim.S));
                Check(true,"Declared date fixture: controller clock paused; game calendar advanced one day and Today synchronized, live CasinoDesktop.Update performs ignored transition");
            }
            else Click(outcome==XgSim.CasinoPaid?"Pay Deposit":"Quit Casino");
            yield return VisibleUntil(()=>Layer("Site Closed")&&ctl.Sim.CasinoClosed,"actual closed-site presentation");
            Check(ctl.Sim.S.casinoScamOutcome==outcome&&ctl.runtime.Sim.S.money==before-(outcome==XgSim.CasinoPaid?fee:0),"actual outcome and exact fee wallet delta: "+tag);
            Check(ctl.Sim.S.casinoFeePaid==(outcome==XgSim.CasinoPaid?fee:0)&&ctl.Sim.S.casinoLearned&&ctl.Sim.ExtraSamples("spam")==samples+XgSim.CasinoScamSamples,"exact fee and one spam-learning award: "+tag);
            var receipt=ctl.Sim.S.askedLog.Where(x=>x.sourceKind==XgSim.CasinoSourceKind).ToArray();
            Check(receipt.Length==1&&receipt[0].eventKey.Contains(":"+outcome+":"),"one outcome-specific learning receipt: "+tag);
            if(outcome==XgSim.CasinoIgnored)Check(receipt[0].answerSource=="system"&&receipt[0].playerAnswer=="","ignored records system action without invented player answer: "+tag);
            CaptureLcd(tag+"-02-closed.png");
            string expected=Snapshot(ctl);SaveSlot(ctl,tag+"-closed");Bind(ctl,english);LoadSlot(ctl);
            Check(Snapshot(ctl)==expected,"actual disk restore preserves closed outcome, fee, wallet, spam, receipt and group history: "+tag);
            yield return OpenCasino(ctl);yield return VisibleUntil(()=>Layer("Site Closed"),"closed site persists after disk rebind");
            yield return WaitSeconds(.5,tag+"/closed-reload-idempotence");Check(Snapshot(ctl)==expected,"repeated live ticks do not duplicate wallet/sample/learning outcome: "+tag);
            if(outcome==XgSim.CasinoPaid)yield return Gossip(ctl,english,tag);
        }
        yield return FrozenBlackjack(ctl,english,lang);
    }
    static IEnumerator FrozenBlackjack(XingGuangController ctl,bool english,string tag)
    {
        Bind(ctl,english);ctl.Sim.Casino.rng=12345;ctl.Sim.MarkCasinoGossip(1);ctl.Sim.MarkCasinoGossip(2);yield return OpenCasino(ctl);
        yield return VisibleUntil(()=>ReadyButton("Game blackjack"),"blackjack selector ready");Click("Game blackjack");yield return Frames();Click("Stake 10");yield return Frames();
        yield return VisibleUntil(()=>ReadyButton("Place Bet"),"blackjack deal ready");double before=ctl.runtime.Sim.S.money;Click("Place Bet");
        Need(ctl.Sim.BlackjackActive&&ctl.runtime.Sim.S.money==before-10,"real LCD blackjack deal accepts exactly one stake");
        ctl.Sim.S.casinoScam=1;ctl.Sim.S.casinoFrozenDay=ctl.Sim.Today;
        Check(true,"Declared legacy compatibility fixture: frozen flag set only after real accepted active hand; this combination is not claimed as naturally generated by current settlement");
        string expected=Snapshot(ctl);SaveSlot(ctl,tag+"-frozen-active-blackjack");Bind(ctl,english);LoadSlot(ctl);Need(Snapshot(ctl)==expected,"accepted frozen hand survives actual disk roundtrip");
        yield return OpenCasino(ctl);yield return VisibleUntil(()=>ReadyButton("Game blackjack"),"restored blackjack selector ready");Click("Game blackjack");
        ctl.Sim.Clock+=2; // Explicit logical debounce clock fixture, never animation time.
        yield return VisibleUntil(()=>ReadyButton("Blackjack stand"),"restored card motion finishes and accepted hand can stand");
        Check(!Layer("Risk Notice")&&!ReadyButton("Place Bet")&&!ReadyButton("Blackjack double"),"frozen accepted hand keeps Stand visible while new stake/double remain blocked: "+tag);
        CaptureLcd(tag+"-blackjack-active.png");int rounds=ctl.Sim.Casino.rounds;double held=ctl.runtime.Sim.S.money;
        Click("Blackjack stand");Need(!ctl.Sim.BlackjackActive&&ctl.Sim.Casino.rounds==rounds+1,"LCD Stand settles loaded frozen hand once");
        var settled=ctl.Sim.Casino.history.Last();Check(ctl.runtime.Sim.S.money==held+settled.returned,"frozen hand preserves original gross payout exactly: "+tag);
        yield return VisibleUntil(()=>Layer("Risk Notice"),"risk notice waits for actual accepted hand settlement",30);
        yield return WaitSeconds(.5,tag+"/blackjack-settlement-idempotence");Check(ctl.Sim.Casino.rounds==rounds+1&&ctl.runtime.Sim.S.money==held+settled.returned,"settled hand does not settle twice on further frames: "+tag);
        CaptureLcd(tag+"-blackjack-settled.png");
        expected=Snapshot(ctl);SaveSlot(ctl,tag+"-settled-blackjack");Bind(ctl,english);LoadSlot(ctl);Check(Snapshot(ctl)==expected,"settled frozen blackjack persists exactly through disk: "+tag);
    }
    static IEnumerator OpenGroup(XingGuangController ctl)
    {
        ctl.runtime.GetComponent<ChapterOneDesktopRouter>().Open(YYChatHub.AppId,YYChatHub.EraGroup);
        var hub=YYChatHub.Instance;
        yield return Until(()=>hub.View!=null&&hub.View.EnsureReady()&&WindowReady(Field<WindowManager>(hub.View,"window")),"YY native window opened for public gossip");
        yield return Foreground("yy-group-open");
    }
    static IEnumerator Gossip(XingGuangController ctl,bool english,string tag)
    {
        Need(ctl.Sim.CasinoGossipDue()==3,"only public closed-site gossip is pending");
        yield return VisibleUntil(()=>Field<int>(CasinoDesktop.Instance,"gossipPending")==3,"live desktop queues closed-site gossip");
        float scheduled=Field<float>(CasinoDesktop.Instance,"gossipAt");
        Check(scheduled-Time.unscaledTime>30,"public gossip retains real forty-second schedule (no timer injection): "+tag);
        double observedStart=Time.realtimeSinceStartupAsDouble;int observedFrame=Time.frameCount;
        FocusSnapshot(tag+"/gossip-before-OpenGroup",observedStart,observedFrame);
        yield return OpenGroup(ctl);int before=Group.messages.Count;
        FocusSnapshot(tag+"/gossip-wait/begin",observedStart,observedFrame);
        string exact=XgSim.GamblingGossip(3,english);
        while(Time.unscaledTime<scheduled-.1f){RequireForeground(tag+"/gossip-wait",observedStart,observedFrame);Need(Group.messages.Count==before,"no early group message before actual gossip deadline");yield return null;}
        FocusSnapshot(tag+"/gossip-wait/end",observedStart,observedFrame);
        yield return VisibleUntil(()=>Group.messages.Count>before,"actual delayed public gossip arrives",10);
        Check(Group.messages.Count==before+1&&Group.messages.Last().text==exact&&Group.messages.Last().from==YYChatHub.EraGroup,"group receives only exact public site report, not private AI/player amounts/training: "+tag);
        Check(!Group.messages.Last().text.Contains("PRIVATE_AI_7391")&&ctl.Sim.CasinoGossipDue()==0,"private name absent and receipt only consumed after real receive: "+tag);
        CaptureLcd(tag+"-03-public-gossip.png");string expected=Snapshot(ctl);
        double roundtripStart=Time.realtimeSinceStartupAsDouble;int roundtripFrame=Time.frameCount;
        FocusSnapshot(tag+"/gossip-before-SaveSlot",roundtripStart,roundtripFrame);
        SaveSlot(ctl,tag+"-public-gossip");FocusSnapshot(tag+"/gossip-after-SaveSlot",roundtripStart,roundtripFrame);
        Bind(ctl,english);FocusSnapshot(tag+"/gossip-after-Bind",roundtripStart,roundtripFrame);
        LoadSlot(ctl);FocusSnapshot(tag+"/gossip-after-LoadSlot",roundtripStart,roundtripFrame);
        Check(Snapshot(ctl)==expected&&ctl.Sim.CasinoGossipDue()==0,"disk restores exact public message and dedup receipt: "+tag);
        int count=Group.messages.Count;yield return WaitSeconds(41,tag+"/gossip-post-reload-dedup");
        Check(Group.messages.Count==count&&ctl.Sim.CasinoGossipDue()==0,"full new delay after disk rebind never repeats delivered gossip: "+tag);
    }
    static IEnumerator StaleGossip(XingGuangController ctl)
    {
        yield return FreezeByClick(ctl,false);Click("Quit Casino");yield return VisibleUntil(()=>ctl.Sim.CasinoClosed,"old owner closed for pending gossip");
        yield return VisibleUntil(()=>Field<int>(CasinoDesktop.Instance,"gossipPending")==3,"old owner actual gossip timer starts");
        var old=ctl.Sim;var oldGroup=Group;int oldCount=oldGroup.messages.Count;float oldAt=Field<float>(CasinoDesktop.Instance,"gossipAt");
        double observedStart=Time.realtimeSinceStartupAsDouble;int observedFrame=Time.frameCount;
        FocusSnapshot("stale-gossip/before-Bind",observedStart,observedFrame);
        Bind(ctl,false);FocusSnapshot("stale-gossip/after-Bind-before-OpenGroup",observedStart,observedFrame);
        yield return OpenGroup(ctl);int freshCount=Group.messages.Count;
        FocusSnapshot("stale-gossip/wait/begin",observedStart,observedFrame);
        while(Time.unscaledTime<oldAt+1){RequireForeground("stale-gossip/wait",observedStart,observedFrame);Need(Group.messages.Count==freshCount,"new owner receives no old pending gossip before/at deadline");yield return null;}
        FocusSnapshot("stale-gossip/wait/end",observedStart,observedFrame);
        Check(old.CasinoGossipDue()==3&&oldGroup.messages.Count==oldCount&&ctl.Sim.CasinoGossipDue()==0&&Group.messages.Count==freshCount,
            "real old-owner gossip delay expires after save switch without message leak or false receipt on either owner");
        CaptureLcd("zh-old-gossip-owner-isolated.png");
    }
}
#endif
