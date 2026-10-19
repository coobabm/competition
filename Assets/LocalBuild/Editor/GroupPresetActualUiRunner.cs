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
using LingGuangV05.Desktop.Bodu;
using TMPro;
using UnityEngine.UI;
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

// Independent serial-editor owner. Never dispatches through SidebarLayoutChecks/BookLibrary.Run.
// Main/calendar/counter prerequisites are explicit fixtures; group Choice/Send clicks and waits are real.
[InitializeOnLoad]
public static partial class GroupPresetActualUiRunner
{
    const string Key="GroupPresetActualUiRunner.";
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
        public string input="Real LCD raycast-checked Choice/Send clicks with FilesWindowUiChecks; TMP text is assigned explicitly as a fixture. Actual foreground is required. No physical keyboard/OS mouse or real model inference claim.";
        public int width,height,assertions;
        public List<string> checks=new List<string>(),screenshots=new List<string>(),errors=new List<string>(),cleanupErrors=new List<string>();
    }
    static GroupPresetActualUiRunner(){EditorApplication.playModeStateChanged+=State;}
    [MenuItem("Tools/Local Verification/Group preset actual UI")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||Root.Length>0)throw new InvalidOperationException("Run once from clean EditMode under the serial Unity owner.");
        for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save scene changes before this scene-switching QA.");
        string dir=Path.GetFullPath(Path.Combine("LocalVerification","GroupPresetActual",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));
        Directory.CreateDirectory(Path.Combine(dir,"Save"));Directory.CreateDirectory(Path.Combine(dir,"DesktopProfile"));
        Preflight(dir);
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
        string tag="GroupPresetQA-"+Guid.NewGuid().ToString("N");
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
            finally{Debug.Log("GROUP_PRESET_ACTUAL "+(passed?"PASS":"FAIL/INTERRUPTED")+": "+dir);if(Application.isBatchMode||Environment.GetCommandLineArgs().Contains("-verificationExit"))EditorApplication.Exit(passed?0:1);}
        }
    }
    static void Log(string message,string stack,LogType type)
    {
        if(report==null||finishing)return;
        if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)report.errors.Add(message+"\n"+stack);
    }
    static void Need(bool value,string message){if(report!=null)report.assertions++;if(!value)throw new InvalidOperationException("Group preset runner: "+message);}
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
        yield return Foreground();
        foreach(bool english in new[]{false,true}) yield return GroupFrames(ctl,english);
        GatherChecks();Check(report.errors.Count==0,"no runtime errors during group preset checks");
        string full="group-preset-pass-full-frame.png";ScreenCapture.CaptureScreenshot(Path.Combine(Root,full));
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
            if(n=="StoryDirector"||n=="StoryDesktopPresenter"||n=="PrologueDirector"||n=="SpiderIntroScene"||n=="LoveQuestionCutscene"||n=="StageOneTutor"||n=="LaoZhouYY"||n=="ZhouCardLoan"||n=="XgPublicDiscourseNotifier"||n=="XgDailyUnlockNotifier"||n=="CasinoDesktop"||n=="XgResidentSpider"||n=="XgHoloCard"||n=="XgAppAcquisitionHost"||n=="XgEndingPreparationHost"||n=="XgDailyLifeHost"||n=="XgStageCurtain"||n=="OriginCurtain")
            {c.CancelInvoke();c.StopAllCoroutines();c.enabled=false;}
        }
    }
    static void IsolateTicks(XingGuangController ctl,bool disk)
    {
        var rt=ctl.runtime;rt.tickEnabled=false;rt.useDiskSave=disk;ctl.enabled=false;
        typeof(ChapterOneRuntime).GetProperty("TestMode",F).GetSetMethod(true).Invoke(rt,new object[]{true});
        Need(rt.TestMode&&(LocalLlm.Instance==null||!LocalLlm.Instance.enabled&&!LocalLlm.Instance.Ready)&&(LocalEmbedder.Instance==null||!LocalEmbedder.Instance.enabled&&!LocalEmbedder.Instance.Ready),"isolated paused TestMode with both model services disabled");
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
        string name="group-preset-failure-full-frame.png";ScreenCapture.CaptureScreenshot(Path.Combine(Root,name));
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
        mouse=InputSystem.AddDevice<Mouse>("GroupPresetActualQaMouse");keyboard=InputSystem.AddDevice<Keyboard>("GroupPresetActualQaKeyboard");mouse.MakeCurrent();keyboard.MakeCurrent();
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
        if(ok)File.WriteAllText(Path.Combine(Root,"passed.txt"),"Group preset actual UI passed; see report for explicit calendar/counter/text fixtures, actual clicks, real elapsed waits, isolated slot6 roundtrip and callback/enable lifecycle checks. Not natural progression, physical input or Player/model inference.");
        EditorApplication.ExitPlaymode();
    }
    static void Reload()
    {
        if(Root.Length==0)return;finishing=true;if(report!=null){report.status="interrupted";report.failure="Assembly reload interrupted group preset QA";}
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
    const string Scope="Isolated Main/calendar/stage and weekly-count fixtures; real LCD Choice/Send clicks with exact raycast target checks and actual foreground; TMP text assigned as a declared fixture. Both languages exercise same-day 3-to-2/no refill and next-date refresh. Normal cold-turn waits use actual unscaled 8+3 seconds. Author-priority case uses actual preset replies and timers. Slot6 saves/restores partial cold scene through production archive IO inside the unique verification directory. Stale button callback is explicitly replayed, not a physical click; no actual model inference callback claim. No natural calendar progression, physical keyboard or Player claim.";
    static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,F|BindingFlags.Static).GetValue(target);
    static void Field(object target,string name,object value)=>target.GetType().GetField(name,F).SetValue(target,value);
    static YYConversation Group(YYChatHub hub)=>hub.Conversation(YYChatHub.EraGroup);
    static string ChoiceText(Button button)=>button.GetComponentInChildren<TMP_Text>(true).text;
    static Button[] Bubbles(YYChatView view)=>Field<RectTransform>(view,"choiceStrip").GetComponentsInChildren<Button>(false).Where(x=>x.name=="Choice").ToArray();
    static bool WindowReady(WindowManager w)
    {
        if(w==null||!w.isOn||!w.gameObject.activeInHierarchy)return false;
        foreach(var g in w.GetComponentsInParent<CanvasGroup>(true))if(g.alpha<.95f||!g.blocksRaycasts||!g.interactable)return false;
        var a=w.windowAnimator;return a==null||!a.isActiveAndEnabled||!a.IsInTransition(0)&&a.GetCurrentAnimatorStateInfo(0).normalizedTime>=1;
    }
    static IEnumerator Foreground()
    {
        // Let startup/window activation settle before measuring continuous foreground time.
        // This observes real focus; it never replaces Application.isFocused or the window probe.
        double deadline=Time.realtimeSinceStartupAsDouble+60,stableSince=-1;
        while(stableSince<0||Time.realtimeSinceStartupAsDouble-stableSince<3)
        {
            Need(Time.realtimeSinceStartupAsDouble<deadline,"actual foreground must stabilize; never fabricate focus");
            if(Application.isFocused&&!PlayerWindowState.IsMinimized)
            {if(stableSince<0)stableSince=Time.realtimeSinceStartupAsDouble;}
            else stableSince=-1;
            yield return null;
        }
    }
    static IEnumerator WaitSeconds(double seconds)
    {
        double end=Time.realtimeSinceStartupAsDouble+seconds;
        while(Time.realtimeSinceStartupAsDouble<end)yield return null;
    }
    static IEnumerator VisibleUntil(Func<bool> condition,string message,double seconds=25)
    {
        double end=Time.realtimeSinceStartupAsDouble+seconds;
        while(!condition())
        {
            Need(Application.isFocused&&!PlayerWindowState.IsMinimized,"foreground retained during observed timing: "+message);
            Need(Time.realtimeSinceStartupAsDouble<end,message);yield return null;
        }
    }
    static ChapterOneSim Fixture(bool english)
    {
        var lab=new XgSim();lab.BeginOpening();lab.English=english;
        lab.S.stage=lab.S.stageVision=lab.S.stageSequence=2;lab.S.abilitiesEmerged=new List<int>{1,2};
        lab.S.openingFocus=false;lab.S.teachingFlow=false;lab.S.spiderIntroDone=lab.S.firstWordsSaid=true;
        lab.S.teachIntroSaid=lab.S.teachThirdSaid=lab.S.teachTreeSaid=lab.S.teachTrainSaid=true;lab.MarkAfterthought("first.drift");
        var yy=new YYState{selected=YYChatHub.EraGroup,seedZhouHand=true,seedJieAsk=true};
        foreach(var c in YYChatHub.Contacts)yy.conversations.Add(new YYConversation{id=c.id});
        var house=new ChapterOneSim();house.S.prologue=2;house.S.appInstallState=2;house.S.money=1000000;
        house.S.labState=JsonUtility.ToJson(lab.S);house.S.chatState=JsonUtility.ToJson(yy);
        GameCalendar.Advance(house.S,GameCalendar.DayIndex(new DateTime(2016,6,6)));house.S.rentDay=GameCalendar.CurrentDay(house.S);house.S.broadbandMonth=201612;
        return house;
    }
    static IEnumerator BindGroup(XingGuangController ctl,bool english)
    {
        SetLanguage(english);var house=Fixture(english);ctl.runtime.SetSimulationForTests(house);
        typeof(XingGuangController).GetMethod("BindSave",F).Invoke(ctl,new object[]{house});
        IsolateTicks(ctl,false);QuietDrivers();
        var hub=YYChatHub.Instance;Need(hub!=null&&hub.BoundTo(house),"YY bound to exact isolated save");
        hub.enabled=true;
        var router=ctl.runtime.GetComponent<ChapterOneDesktopRouter>();router.Open(YYChatHub.AppId,YYChatHub.EraGroup);
        yield return Until(()=>hub.View!=null&&hub.View.EnsureReady()&&WindowReady(Field<WindowManager>(hub.View,"window")),"YY native open animation completes");
        yield return Frames(4);yield return Foreground();
        Need(hub.S.selected==YYChatHub.EraGroup&&hub.View.IsOpen,"actual selected group rendered");
    }
    static void TextFixtureAndSend(YYChatHub hub,string text)
    {
        var input=Field<TMP_InputField>(hub.View,"input");input.SetTextWithoutNotify(text);
        var send=hub.View.GetComponentsInChildren<Button>(false).Single(b=>b.name=="Send");
        int before=Group(hub).messages.Count;
        CaptureClick((RectTransform)send.transform);
        Need(Group(hub).messages.Count==before+1&&Group(hub).messages.Last().from==YYChatHub.Me&&Group(hub).messages.Last().text==text,"LCD Send submits fixture text through actual view and hub");
    }
    static void SecondFreeFixture(YYChatHub hub)
    {
        hub.S.groupPresets.weekKey=GroupPresetLifecycle.WeekKeyForDay(GameCalendar.CurrentDay(hub.runtime.Sim.S));
        hub.S.groupPresets.weeklyFreeInputs=1;
        Check(true,"Declared fixture: first weekly free input count=1; only the second free line is actually clicked in this case");
    }
    static void IsolatedDisk(ChapterOneRuntime runtime)
    {
        string expected=Path.GetFullPath(Path.Combine(Root,"Save"))+Path.DirectorySeparatorChar;
        Need(Path.GetFullPath(runtime.SavePath).StartsWith(expected,StringComparison.OrdinalIgnoreCase),"slot IO confined to this run's isolated save root");
        Need(Path.GetFullPath(ChapterOneRuntime.VerificationSaveDirectory).TrimEnd(Path.DirectorySeparatorChar)==expected.TrimEnd(Path.DirectorySeparatorChar),"verification path matches unique run directory");
    }
    static IEnumerator GroupFrames(XingGuangController ctl,bool english)
    {
        string tag=english?"en":"zh";
        yield return BindGroup(ctl,english);var hub=YYChatHub.Instance;
        yield return Until(()=>Bubbles(hub.View).Length==3,"initial three visible preset choices");
        var choices=Bubbles(hub.View);var text=choices.Select(ChoiceText).ToArray();var ids=hub.S.groupPresets.dailyIds.ToArray();
        Check(ids.Length==3&&text.Distinct().Count()==3,"three distinct actual daily preset bubbles: "+tag);
        CaptureLcd(tag+"-01-three.png");
        CaptureClick((RectTransform)choices[0].transform);
        yield return Until(()=>Bubbles(hub.View).Length==2,"clicked preset removed without refill");
        var sent=hub.S.groupPresets.sentIds.Single();
        Check(hub.S.groupPresets.dailyIds.SequenceEqual(ids)&&Bubbles(hub.View).Select(ChoiceText).All(x=>text.Contains(x)),"same-day frozen IDs persist and only two original bubbles remain: "+tag);
        yield return WaitSeconds(.8);
        Check(Bubbles(hub.View).Length==2,"no delayed same-day refill: "+tag);CaptureLcd(tag+"-02-two.png");
        SetLanguage(!english);yield return Frames(5);
        Check(hub.S.groupPresets.dailyIds.SequenceEqual(ids)&&Bubbles(hub.View).Length==2,"language swap uses same unsent IDs: "+tag);
        SetLanguage(english);yield return Frames(4);
        int day=GameCalendar.CurrentDay(ctl.runtime.Sim.S);
        Need(GameCalendar.Advance(ctl.runtime.Sim.S,day+1),"declared next-day calendar fixture advanced once");
        yield return Until(()=>hub.S.groupPresets.selectionDay==day+1&&Bubbles(hub.View).Length==3,"production Update refreshes next day without player send");
        Check(!hub.S.groupPresets.dailyIds.Contains(sent)&&hub.S.groupPresets.sentIds.Contains(sent),"sent ID never cycles into next-day choices: "+tag);CaptureLcd(tag+"-03-next-day.png");
        yield return VisibleUntil(()=>!hub.GroupScriptPlaying,"first preset's actual author queue drains",25);

        // Natural 8+3-second timing, no coroutine stepping or timer injection.
        yield return BindGroup(ctl,english);hub=YYChatHub.Instance;SecondFreeFixture(hub);
        string first=GroupPresetLifecycle.ColdLine(1,0,english),second=GroupPresetLifecycle.ColdLine(1,1,english);
        double started=Time.realtimeSinceStartupAsDouble;
        TextFixtureAndSend(hub,english?"Anyone around this evening?":"今晚有人在吗？");
        Need(hub.S.groupPresets.weeklyFreeInputs==2&&hub.S.groupPresets.pendingColdScenes.Count==1,"real second free Send queues one silent continuation");
        yield return VisibleUntil(()=>Group(hub).messages.Any(m=>m.text==first),"first natural cold line",20);
        double firstAt=Time.realtimeSinceStartupAsDouble;
        Check(firstAt-started>=7.8,"real first cold delay seconds="+(firstAt-started).ToString("F3")+" "+tag);
        Need(Group(hub).messages.Count==2,"silence has no generic reply before first public continuation");CaptureLcd(tag+"-04-first-cold.png");
        yield return VisibleUntil(()=>Group(hub).messages.Any(m=>m.text==second),"second natural cold line",10);
        double secondAt=Time.realtimeSinceStartupAsDouble;
        Check(secondAt-firstAt>=2.8&&hub.S.groupPresets.pendingColdScenes.Count==0,"real second cold delay seconds="+(secondAt-firstAt).ToString("F3")+" "+tag);CaptureLcd(tag+"-05-complete-cold.png");

        // Start a real author queue just before cold eligibility; it must keep the floor.
        yield return BindGroup(ctl,english);hub=YYChatHub.Instance;SecondFreeFixture(hub);
        TextFixtureAndSend(hub,english?"How was your day?":"今天过得怎么样？");
        yield return WaitSeconds(6);
        var authorButton=Bubbles(hub.View)[0];CaptureClick((RectTransform)authorButton.transform);
        var authored=Field<Queue<string>>(hub,"presetReplyQueue").ToArray();
        Need(authored.Length>0&&Field<bool>(hub,"presetReplyRunning"),"actual preset author queue active before cold eight-second boundary");
        while(Field<bool>(hub,"presetReplyRunning"))
        {
            Need(!Group(hub).messages.Any(m=>m.text==first),"cold continuation yields to actual authored replies");
            Need(Application.isFocused&&!PlayerWindowState.IsMinimized,"foreground retained during author priority");yield return null;
        }
        yield return VisibleUntil(()=>Group(hub).messages.Any(m=>m.text==first),"cold first line after real author queue",20);
        var group=Group(hub);int coldIndex=group.messages.FindIndex(m=>m.text==first);
        int previous=-1;
        foreach(var line in authored)
        {
            int index=group.messages.FindIndex(m=>m.text==line);Need(index>previous&&index<coldIndex,"every authored line arrives in order before cold scene");previous=index;
        }
        Check(true,"actual authored queue wins priority without losing cold continuation: "+tag);CaptureLcd(tag+"-06-authors-first.png");

        // Pause component after actual first line, write real slot6, switch owner, replay only a stale callback fixture.
        hub.enabled=false;
        Need(hub.S.groupPresets.pendingColdScenes.Count==1&&hub.S.groupPresets.pendingColdScenes[0].nextLine==1,"component disabled with exactly one cold line remaining");
        int oldCount=Group(hub).messages.Count;var oldOwner=hub.S;
        var staleButton=Bubbles(hub.View).First().onClick;
        IsolatedDisk(ctl.runtime);ctl.runtime.useDiskSave=true;
        try{Need(ctl.runtime.SaveSlot(6),"real partial scene saved in isolated slot6: "+ctl.runtime.SaveStatus);}
        finally{ctl.runtime.useDiskSave=false;}
        Need(File.Exists(Path.Combine(Root,"Save","slots","6","chapter-one.sav")),"actual slot6 file exists");
        yield return WaitSeconds(3.4);
        Check(Group(hub).messages.Count==oldCount&&hub.S.groupPresets.pendingColdScenes[0].nextLine==1,"disabled component does not deliver remaining line across full delay: "+tag);
        var other=Fixture(english);ctl.runtime.SetSimulationForTests(other);
        typeof(XingGuangController).GetMethod("BindSave",F).Invoke(ctl,new object[]{other});IsolateTicks(ctl,false);QuietDrivers();
        Need(!ReferenceEquals(oldOwner,hub.S),"runtime switch establishes new YY state owner");
        int freshCount=Group(hub).messages.Count;staleButton.Invoke();
        Check(Group(hub).messages.Count==freshCount&&hub.S.groupPresets.sentIds.Count==0,"explicitly replayed stale Choice callback refuses new save (callback fixture, not UI click): "+tag);
        hub.enabled=true;yield return WaitSeconds(3.4);
        Check(Group(hub).messages.Count==freshCount&&hub.S.groupPresets.pendingColdScenes.Count==0,"enabled new owner receives no old continuation: "+tag);
        hub.enabled=false;
        IsolatedDisk(ctl.runtime);ctl.runtime.useDiskSave=true;
        try{Need(ctl.runtime.LoadArchive(6),"real partial scene restored from isolated slot6: "+ctl.runtime.SaveStatus);}
        finally{ctl.runtime.useDiskSave=false;ctl.runtime.tickEnabled=false;}
        IsolateTicks(ctl,false);QuietDrivers();
        Need(hub.S.groupPresets.pendingColdScenes[0].nextLine==1&&Group(hub).messages.Count==oldCount,"disk restore preserves exact first-line cursor and history");
        ctl.runtime.GetComponent<ChapterOneDesktopRouter>().Open(YYChatHub.AppId,YYChatHub.EraGroup);
        yield return Until(()=>WindowReady(Field<WindowManager>(hub.View,"window")),"YY window restored for remaining line");
        yield return Foreground();double resumedAt=Time.realtimeSinceStartupAsDouble;hub.enabled=true;
        yield return VisibleUntil(()=>hub.S.groupPresets.pendingColdScenes.Count==0,"remaining line resumes after component enabled",10);
        Check(Time.realtimeSinceStartupAsDouble-resumedAt>=2.8&&Group(hub).messages.Count==oldCount+1&&Group(hub).messages.Last().text==second,"real reenable resumes only second line after full 3-second wait: "+tag);
        Need(Group(hub).messages.Count(m=>m.text==first)==1,"disk resume never repeats first continuation");
        CaptureLcd(tag+"-07-slot6-resumed.png");
    }
    static void Preflight(string directory)
    {
        Need(YYChatHub.Instance==null&&!StoryFocus.Active,"preflight requires isolated EditMode with no initialized YY hub/story");
        const BindingFlags sf=BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public;
        var presets=typeof(YYChatHub).GetField("groupPresets",sf);object oldPresets=presets.GetValue(null);
        var loaded=typeof(BoduView).GetField("newsLoaded",sf);object oldLoaded=loaded.GetValue(null);
        var articles=(List<XgNewsArticle>)typeof(XgNewsCatalog).GetField("articles",sf).GetValue(null);var oldArticles=articles.ToArray();
        var random=UnityEngine.Random.state;var clock=GameCalendar.WallClock;
        var lines=new List<string>();
        Application.LogCallback capture=(message,stack,type)=>{if(message.Contains("LAO_ZHOU_NEWS_PASS")||message.Contains("WorldYyGiftRoutingChecks PASS")||type==LogType.Error||type==LogType.Exception)lines.Add(type+": "+message+"\n"+stack);};
        Application.logMessageReceived+=capture;
        try
        {
            // Both create inactive fixture GameObjects, use SetSimulationForTests, and destroy their fixtures.
            // LaoZhou restores its own static caches; this wrapper also restores WorldInteractions resource caches/RNG.
            LaoZhouNewsChecks.Run();WorldYyGiftRoutingChecks.RunInteractions();
            Need(!lines.Any(x=>x.StartsWith("Error:")||x.StartsWith("Exception:")),"preflight consumers emitted no runtime errors");
            Need(lines.Any(x=>x.Contains("LAO_ZHOU_NEWS_PASS"))&&lines.Any(x=>x.Contains("WorldYyGiftRoutingChecks PASS")),"both EditMode consumers emitted actual success markers");
            lines.Insert(0,"Preflight consumers only; not counted as UI assertions. LZ marker includes count; World checker emits a pass marker without a count.");
        }
        finally
        {
            Application.logMessageReceived-=capture;presets.SetValue(null,oldPresets);loaded.SetValue(null,oldLoaded);articles.Clear();articles.AddRange(oldArticles);
            UnityEngine.Random.state=random;GameCalendar.WallClock=clock;
            File.WriteAllLines(Path.Combine(directory,"preflight-consumers.log"),lines);
        }
    }

}
#endif

