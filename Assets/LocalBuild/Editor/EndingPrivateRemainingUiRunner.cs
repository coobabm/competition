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
using LingGuangV05.Runtime.Persistence;
using LingGuangV05.XingGuang;
using Michsky.DreamOS;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

// D1 candidate (Claude round 2). Independent serial-editor owner for EndingPrivateRemainingUiChecks.
// Same proven shell as EndingSmallAActualUiRunner (fresh isolated save/profile, models disabled, fixed 1280x720
// GameView, owned virtual devices, real foreground observation). Never dispatches through SidebarLayoutChecks.
// -executeMethod EndingPrivateRemainingUiRunner.Run -verificationExit   (needs an actually focused editor; not -batchmode)
[InitializeOnLoad]
public static class EndingPrivateRemainingUiRunner
{
    const string Key="EndingPrivateRemainingUiRunner.";
    const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static string Root=>SessionState.GetString(Key+"Root","");
    static readonly Stack<IEnumerator> routines=new Stack<IEnumerator>();
    static readonly List<InputDevice> muted=new List<InputDevice>();
    static Mouse mouse,previousMouse;
    static Keyboard keyboard,previousKeyboard;
    static InputSettings oldInput,qaInput;
    static IDisposable locale;
    static Func<bool> oldProbe,oldExclusive;
    static bool focusCaptured,finishing,failing,failureCaptured;
    static int lastFrame;
    static double deadline;
    static string language="zh-CN";
    static Report report;
    [Serializable] sealed class SceneEntry {public string path;public bool loaded,active;}
    [Serializable] sealed class Setup {public List<SceneEntry> scenes=new List<SceneEntry>();}
    [Serializable] sealed class Report
    {
        public string status="running",failure="",unityVersion;
        public string scope=EndingPrivateRemainingUiChecks.Scope;
        public string input="Fixed 1280x720 desktop fixture. Every click is a production LCD hit-test on the intended button, refused unless the editor is actually focused and not minimized; screenshots wait for three continuous seconds of actual foreground. [real disk] = production SaveNow primary read back via AtomicSaveStore and slot-6 SaveSlot/LoadArchive in the isolated directory. [simulated snapshot] = declared release progression built through production XgEpilogueFlow with an in-memory saveNow. No physical OS input, inference or natural full progression claim.";
        public int width,height,assertions;
        public List<string> namedResults=new List<string>();
        public List<string> checks=new List<string>(),screenshots=new List<string>(),errors=new List<string>(),cleanupErrors=new List<string>();
    }
    static EndingPrivateRemainingUiRunner(){EditorApplication.playModeStateChanged+=State;}
    [MenuItem("Tools/Local Verification/Ending private remaining branches UI")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||Root.Length>0)throw new InvalidOperationException("Run once from clean EditMode under the serial Unity owner.");
        for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save scene changes before this scene-switching QA.");
        string dir=Path.GetFullPath(Path.Combine("LocalVerification","EndingPrivateRemaining",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));
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
        string tag="EndingPrivateRemainingQA-"+Guid.NewGuid().ToString("N");
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
            finishing=failing=failureCaptured=false;lastFrame=-1;deadline=EditorApplication.timeSinceStartup+1500;
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
            string dir=Root;bool passed=report!=null&&report.status=="awaiting_editor_restore";
            try
            {
                // Attempt editor restoration even if the second PlayMode cleanup fails.
                try{CleanupPlay();}finally{RestoreEditor();}
                passed=passed&&report.cleanupErrors.Count==0&&report.errors.Count==0;
            }
            catch(Exception ex)
            {
                passed=false;report?.cleanupErrors.Add("Editor restoration: "+ex);
                File.AppendAllText(Path.Combine(dir,"cleanup-errors.txt"),ex+Environment.NewLine);
            }
            finally
            {
                try
                {
                    // RestoreEditor clears Root, so finalize against the captured directory only.
                    if(report!=null){if(passed)report.status="passed";else if(report.status!="interrupted")report.status="failed";File.WriteAllText(Path.Combine(dir,"report.json"),JsonUtility.ToJson(report,true));}
                    if(passed)File.WriteAllText(Path.Combine(dir,"passed.txt"),"Ending private remaining-branch suite passed (LP1/LP2/R1/R2 zh/en named results). Contact/stage/SI/release/launch-night/public-scene are declared fixtures; see report scope. Not model, R3/AI-decided route, natural progression or Player coverage.");
                    else if(File.Exists(Path.Combine(dir,"passed.txt")))File.Delete(Path.Combine(dir,"passed.txt"));
                }
                catch(Exception ex)
                {
                    passed=false;
                    if(report!=null){report.status="failed";report.cleanupErrors.Add("Final report: "+ex);}
                    try{if(File.Exists(Path.Combine(dir,"passed.txt")))File.Delete(Path.Combine(dir,"passed.txt"));if(report!=null)File.WriteAllText(Path.Combine(dir,"report.json"),JsonUtility.ToJson(report,true));File.AppendAllText(Path.Combine(dir,"cleanup-errors.txt"),ex+Environment.NewLine);}
                    catch(Exception writeError){Debug.LogError("Unable to persist failed final result: "+writeError);}
                }
                finally{Debug.Log("ENDING_PRIVATE_REMAINING "+(passed?"PASS":"FAIL/INTERRUPTED")+": "+dir);if(Application.isBatchMode||Array.IndexOf(Environment.GetCommandLineArgs(),"-verificationExit")>=0)EditorApplication.Exit(passed?0:1);}
            }
        }
    }
    static void Log(string message,string stack,LogType type)
    {
        if(report==null||finishing)return;
        if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)report.errors.Add(message+"\n"+stack);
    }
    static void Need(bool value,string message){if(!value)throw new InvalidOperationException("Ending private remaining runner: "+message);}
    static void Check(bool value,string message){Need(value,message);report.checks.Add(message);}
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
            if(!failing){Need(EditorApplication.timeSinceStartup<deadline,"1500-second overall deadline");Need(report.errors.Count==0,"runtime error: "+report.errors.FirstOrDefault());}
            var view=Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(x=>x.GetType().FullName=="UnityEditor.GameView");if(view!=null)view.Repaint();
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
            failing=true;report.status="failed";report.failure=ex.ToString();GatherChecks();CaptureFailureBeforeCleanup();DisposeRoutines();Save();
            if(failureCaptured)routines.Push(CaptureFailure());else Finish(false);
        }
    }
    static IEnumerator RunFrames()
    {
        SetupInput();locale=GameText.Bind(()=>language);
        yield return Until(()=>Object.FindAnyObjectByType<XingGuangController>()?.View!=null,"initial Main controller ready",60);
        var ctl=Object.FindAnyObjectByType<XingGuangController>();var rt=ctl.runtime;
        Need(rt!=null&&ctl.Sim!=null,"initial runtime and lab ready");rt.useDiskSave=false;
        // Bootstrap desktop only; the suite binds its own declared fixtures before any assertion.
        var house=EndingSmallAActualUiChecks.Fixture();
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
        IsolateTicks(ctl);QuietDrivers();
        var notifications=rt.GetComponent<DesktopNotifications>();if(notifications!=null)notifications.enabled=false;
        StoryFocus.Probe=()=>false;StoryFocus.ExclusiveProbe=()=>false;
        yield return Until(()=>Screen.width==1280&&Screen.height==720,"fixed 1280x720 GameView fixture applied",10);
        Check(Screen.width==1280&&Screen.height==720,"explicit 1280x720 GameView fixture available; source LCD input remains production-mapped");
        yield return EndingSmallAActualUiChecks.Foreground();
        yield return EndingPrivateRemainingUiChecks.Frames(ctl,SetLanguage,CaptureClick,CaptureLcd,CaptureFailureBeforeCleanup);
        GatherChecks();Check(report.errors.Count==0,"no runtime errors during ending private remaining checks");
        Check(EndingPrivateRemainingUiChecks.MissingResults().Length==0,"all named LP1/LP2/R1/R2 zh/en results recorded");
        yield return EndingSmallAActualUiChecks.Foreground();
        Need(Application.isFocused&&!PlayerWindowState.IsMinimized,"actual foreground required for final screenshot");
        string full="ending-private-remaining-pass-full-frame.png";ScreenCapture.CaptureScreenshot(Path.Combine(Root,full));
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
            if(n=="YYGirlfriend"||n=="XgStageCurtain"||n=="ShopNotices"||n=="XgLifeEventCard"||n=="XgAppAcquisitionHost"||n=="GameEraClock"||n=="XgEraDeliveryDesk"||n=="XgEraAdvantageBridge"||n=="AiJoinsYy"||n=="XgEndingPreparationHost"||n=="OriginCurtain"||n=="StoryDirector"||n=="StoryDesktopPresenter"||n=="PrologueDirector"||n=="SpiderIntroScene"||n=="LoveQuestionCutscene"||n=="StageOneTutor"||n=="LaoZhouYY"||n=="ZhouCardLoan"||n=="XgPublicDiscourseNotifier"||n=="XgDailyUnlockNotifier"||n=="CasinoDesktop"||n=="XgResidentSpider"||n=="XgHoloCard")
            {c.CancelInvoke();c.StopAllCoroutines();c.enabled=false;}
        }
    }
    static void IsolateTicks(XingGuangController ctl)
    {
        var rt=ctl.runtime;rt.tickEnabled=false;rt.useDiskSave=false;ctl.enabled=false;
        typeof(ChapterOneRuntime).GetProperty("TestMode",F).GetSetMethod(true).Invoke(rt,new object[]{true});
        Need(rt.TestMode&&(LocalLlm.Instance==null||!LocalLlm.Instance.enabled&&!LocalLlm.Instance.Ready)&&(LocalEmbedder.Instance==null||!LocalEmbedder.Instance.enabled&&!LocalEmbedder.Instance.Ready),"isolated paused TestMode with both model services disabled");
    }
    static void SetLanguage(bool english){language=english?GameText.English:"zh-CN";GameText.PublishChanged();}
    static void CaptureClick(RectTransform target){Need(Application.isFocused&&!PlayerWindowState.IsMinimized,"actual foreground required for LCD click");FilesWindowUiChecks.Click(target);}
    static void CaptureLcd(string name)
    {Need(Application.isFocused&&!PlayerWindowState.IsMinimized,"actual foreground required for LCD screenshot");Need(QaDesktopCapture.Save(Path.Combine(Root,name)),"actual LCD image saved: "+name);report.screenshots.Add(name);Save();}
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
    static void CaptureFailureBeforeCleanup()
    {
        if(failureCaptured)return;
        Texture2D texture=null;
        try
        {
            // Capture synchronously: queued screenshots can run after iterator finally has restored UI.
            texture=ScreenCapture.CaptureScreenshotAsTexture();
            Need(texture!=null,"failure frame captured before cleanup");
            File.WriteAllBytes(Path.Combine(Root,"ending-private-remaining-failure-full-frame.png"),texture.EncodeToPNG());
            failureCaptured=true;
        }
        catch(Exception ex){report?.cleanupErrors.Add("Failure capture before cleanup: "+ex);}
        finally{if(texture!=null)Object.Destroy(texture);}
    }
    static IEnumerator CaptureFailure()
    {
        yield return ValidateScreenshot("ending-private-remaining-failure-full-frame.png",true);
    }
    static void SetupInput()
    {
        oldInput=InputSystem.settings;qaInput=Object.Instantiate(oldInput);qaInput.hideFlags=HideFlags.HideAndDontSave;
        qaInput.backgroundBehavior=oldInput.backgroundBehavior; // Observe real focus; never ignore it for UI acceptance.
        qaInput.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        qaInput.updateMode=InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;InputSystem.settings=qaInput;
        previousMouse=Mouse.current;previousKeyboard=Keyboard.current;muted.Clear();
        foreach(var d in InputSystem.devices.Where(x=>x.enabled).ToArray()){muted.Add(d);InputSystem.DisableDevice(d);}
        mouse=InputSystem.AddDevice<Mouse>("EndingPrivateRemainingQaMouse");keyboard=InputSystem.AddDevice<Keyboard>("EndingPrivateRemainingQaKeyboard");mouse.MakeCurrent();keyboard.MakeCurrent();
        InputSystem.onDeviceChange+=MuteExternal;
    }
    static void MuteExternal(InputDevice device,InputDeviceChange change)
    {
        if(finishing||device==mouse||device==keyboard||!device.enabled)return;
        if(change!=InputDeviceChange.Added&&change!=InputDeviceChange.Enabled&&change!=InputDeviceChange.Reconnected)return;
        if(!muted.Contains(device))muted.Add(device);InputSystem.DisableDevice(device);
    }
    static void GatherChecks()
    {
        if(report==null)return;
        foreach(var item in EndingPrivateRemainingUiChecks.Checks)if(!report.checks.Contains(item))report.checks.Add(item);
        foreach(var item in EndingPrivateRemainingUiChecks.NamedResults)if(!report.namedResults.Contains(item))report.namedResults.Add(item);
        report.assertions=EndingPrivateRemainingUiChecks.AssertionCount;
    }
    static void DisposeRoutines()
    {
        while(routines.Count>0)try{(routines.Pop() as IDisposable)?.Dispose();}catch(Exception ex){report?.cleanupErrors.Add("Iterator disposal: "+ex);}
    }
    static void CleanupPlay()
    {
        EditorApplication.update-=Tick;Application.logMessageReceived-=Log;AssemblyReloadEvents.beforeAssemblyReload-=Reload;SceneManager.sceneLoaded-=DisableModels;
        DisposeRoutines();
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
        ok=ok&&report.cleanupErrors.Count==0&&report.errors.Count==0&&EndingPrivateRemainingUiChecks.MissingResults().Length==0;
        report.status=ok?"awaiting_editor_restore":"failed";Save();
        EditorApplication.ExitPlaymode();
    }
    static void Reload()
    {
        if(Root.Length==0)return;finishing=true;if(report!=null){report.status="interrupted";report.failure="Assembly reload interrupted ending private remaining QA";}
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
}
#endif
