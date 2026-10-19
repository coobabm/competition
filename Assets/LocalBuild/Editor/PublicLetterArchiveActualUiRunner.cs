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
using LingGuangV05.Desktop.OS;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

// Independent serial-editor owner. Never dispatches through SidebarLayoutChecks/BookLibrary.Run.
// Main/stage-6/release/letter prerequisites are explicit fixtures; archive interactions and saves are real.
[InitializeOnLoad]
public static class PublicLetterArchiveActualUiRunner
{
    const string Key="PublicLetterArchiveActualUiRunner.";
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
        public string scope=PublicLetterArchiveActualUiChecks.Scope;
        public string input="Article/title/archive/save/native Close/Minimize controls use FilesWindowUiChecks.Click through the production LCD surface. Stage-6/release/letter are explicit prerequisites. Stale delegate invocation is explicitly a race fixture; language changes use the real localization publisher. Reload uses ChapterOneRuntime.ReloadSave.";
        public int width,height,assertions;
        public List<string> checks=new List<string>(),screenshots=new List<string>(),errors=new List<string>(),cleanupErrors=new List<string>();
    }
    static PublicLetterArchiveActualUiRunner(){EditorApplication.playModeStateChanged+=State;}
    [MenuItem("Tools/Local Verification/Public letter archive actual UI")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||Root.Length>0)throw new InvalidOperationException("Run once from clean EditMode under the serial Unity owner.");
        for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save scene changes before this scene-switching QA.");
        string dir=Path.GetFullPath(Path.Combine("LocalVerification","PublicLetterArchiveActual",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));
        Directory.CreateDirectory(Path.Combine(dir,"Save"));Directory.CreateDirectory(Path.Combine(dir,"DesktopProfile"));
        SessionState.SetString(Key+"Setup",JsonUtility.ToJson(new Setup{scenes=EditorSceneManager.GetSceneManagerSetup().Select(x=>new SceneEntry{path=x.path,loaded=x.isLoaded,active=x.isActive}).ToList()}));
        SessionState.SetString(Key+"SaveOverride",ChapterOneRuntime.VerificationSaveDirectory??"");
        SessionState.SetString(Key+"Profile",ProfileField()?.GetValue(null) as string??"");
        SessionState.SetInt(Key+"FPS",Application.targetFrameRate);SessionState.SetInt(Key+"VSync",QualitySettings.vSyncCount);SessionState.SetBool(Key+"Background",Application.runInBackground);
        SessionState.SetString(Key+"Root",dir);
        try
        {
            var type=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            Need(type!=null,"GameView editor type available");
            SessionState.SetBool(Key+"CreatedView",!Resources.FindObjectsOfTypeAll<EditorWindow>().Any(x=>x.GetType()==type));
            var view=EditorWindow.GetWindow(type);view.Show();view.Focus();view.Repaint();
            EditorSceneManager.OpenScene("Assets/LingGuangV05/Scenes/Main.unity",OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }
        catch{RestoreEditor();throw;}
    }
    static FieldInfo ProfileField()=>typeof(DreamOSDataManager).GetField("dataPath",BindingFlags.Static|BindingFlags.NonPublic);
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
            finishing=failing=false;lastFrame=-1;deadline=EditorApplication.timeSinceStartup+180;
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
            finally{Debug.Log("PUBLIC_LETTER_ARCHIVE_ACTUAL "+(passed?"PASS":"FAIL/INTERRUPTED")+": "+dir);if(Application.isBatchMode)EditorApplication.Exit(passed?0:1);}
        }
    }
    static void Log(string message,string stack,LogType type)
    {
        if(report==null||finishing)return;
        if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)report.errors.Add(message+"\n"+stack);
    }
    static void Need(bool value,string message){if(!value)throw new InvalidOperationException("Public letter archive runner: "+message);}
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
            if(!failing){Need(EditorApplication.timeSinceStartup<deadline,"180-second overall deadline");Need(report.errors.Count==0,"runtime error: "+report.errors.FirstOrDefault());}
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
        var house=PublicLetterArchiveActualUiChecks.Fixture();
        QuietDrivers();rt.SetSimulationForTests(house);
        typeof(XingGuangController).GetMethod("BindSave",F).Invoke(ctl,new object[]{house});
        var boot=Object.FindAnyObjectByType<BootManager>(FindObjectsInactive.Include);
        if(boot!=null){boot.CancelInvoke();boot.StopAllCoroutines();if(boot.bootAnimator!=null)boot.bootAnimator.gameObject.SetActive(false);}
        var user=Object.FindAnyObjectByType<UserManager>(FindObjectsInactive.Include);Need(user!=null&&user.isActiveAndEnabled,"native desktop user manager ready");user.ShowDesktop();
        yield return Until(()=>DesktopReady(rt),"initial desktop shortcuts ready",30);
        IsolateTicks(ctl,false);QuietDrivers();DesktopNotifications.Install(rt).enabled=false;
        StoryFocus.Probe=()=>true;StoryFocus.ExclusiveProbe=()=>false;
        Check(Screen.width>320&&Screen.height>200,"graphics-capable GameView available");
        yield return PublicLetterArchiveActualUiChecks.Frames(ctl,SetLanguage,CaptureClick,CaptureLcd);
        GatherChecks();Check(report.errors.Count==0,"no runtime errors during public-letter archive checks");
        string full="public-letter-archive-pass-full-frame.png";ScreenCapture.CaptureScreenshot(Path.Combine(Root,full));
        yield return ValidateScreenshot(full,true);
    }
    static bool DesktopReady(ChapterOneRuntime rt)
    {
        if(rt==null)return false;var app=rt.GetComponent<ChapterOneDesktopRouter>()?.Get(XingGuangController.AppId);
        return app?.Window!=null&&app.TaskbarShortcut!=null&&app.TaskbarShortcut.isActiveAndEnabled&&app.DesktopShortcut!=null&&app.DesktopShortcut.gameObject.activeInHierarchy;
    }
    static void QuietDrivers()
    {
        foreach(var c in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            string n=c.GetType().Name;
            if(n=="StoryDirector"||n=="StoryDesktopPresenter"||n=="PrologueDirector"||n=="SpiderIntroScene"||n=="LoveQuestionCutscene"||n=="StageOneTutor"||n=="LaoZhouYY"||n=="ZhouCardLoan"||n=="XgPublicDiscourseNotifier"||n=="XgDailyUnlockNotifier")
            {c.CancelInvoke();c.StopAllCoroutines();c.enabled=false;}
        }
    }
    static void IsolateTicks(XingGuangController ctl,bool disk)
    {
        var rt=ctl.runtime;rt.tickEnabled=false;rt.useDiskSave=disk;ctl.enabled=false;
        typeof(ChapterOneRuntime).GetProperty("TestMode",F).GetSetMethod(true).Invoke(rt,new object[]{false});
        Need(!rt.TestMode&&(LocalLlm.Instance==null||!LocalLlm.Instance.enabled&&!LocalLlm.Instance.Ready)&&(LocalEmbedder.Instance==null||!LocalEmbedder.Instance.enabled&&!LocalEmbedder.Instance.Ready),"production host with both model services disabled");
    }
    static void SetLanguage(bool english){language=english?GameText.English:"zh-CN";GameText.PublishChanged();}
    static void CaptureClick(RectTransform target)=>FilesWindowUiChecks.Click(target);
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
        string name="public-letter-archive-failure-full-frame.png";ScreenCapture.CaptureScreenshot(Path.Combine(Root,name));
        yield return ValidateScreenshot(name,true);
    }
    static void SetupInput()
    {
        oldInput=InputSystem.settings;qaInput=Object.Instantiate(oldInput);qaInput.hideFlags=HideFlags.HideAndDontSave;
        qaInput.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        qaInput.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        qaInput.updateMode=InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;InputSystem.settings=qaInput;
        previousMouse=Mouse.current;previousKeyboard=Keyboard.current;muted.Clear();
        foreach(var d in InputSystem.devices.Where(x=>x.enabled).ToArray()){muted.Add(d);InputSystem.DisableDevice(d);}
        mouse=InputSystem.AddDevice<Mouse>("PublicLetterArchiveActualQaMouse");keyboard=InputSystem.AddDevice<Keyboard>("PublicLetterArchiveActualQaKeyboard");mouse.MakeCurrent();keyboard.MakeCurrent();
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
    static void GatherChecks()
    {
        if(report==null)return;
        foreach(var item in PublicLetterArchiveActualUiChecks.Checks)if(!report.checks.Contains(item))report.checks.Add(item);
        report.assertions=PublicLetterArchiveActualUiChecks.AssertionCount;
    }
    static void DisposeRoutines()
    {
        while(routines.Count>0)try{(routines.Pop() as IDisposable)?.Dispose();}catch(Exception ex){report?.cleanupErrors.Add("Iterator disposal: "+ex);}
    }
    static void CleanupPlay()
    {
        EditorApplication.update-=Tick;Application.logMessageReceived-=Log;AssemblyReloadEvents.beforeAssemblyReload-=Reload;SceneManager.sceneLoaded-=DisableModels;
        DisposeRoutines();
        // Disable disk first: ExitPlaymode callbacks must not overwrite the checked archive snapshot.
        foreach(var rt in Object.FindObjectsByType<ChapterOneRuntime>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(rt!=null){rt.useDiskSave=false;rt.tickEnabled=false;}
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
        if(ok)File.WriteAllText(Path.Combine(Root,"passed.txt"),"Actual public-letter archive UI passed; see report for explicit stage/release/letter fixtures, real SaveNow checks, and screenshots. No earned prerequisites, natural night, or ending completion claim.");
        EditorApplication.ExitPlaymode();
    }
    static void Reload()
    {
        if(Root.Length==0)return;finishing=true;if(report!=null){report.status="interrupted";report.failure="Assembly reload interrupted public-letter archive QA";}
        CleanupPlay();Save();
    }
    static void RestoreEditor()
    {
        string previous=SessionState.GetString(Key+"SaveOverride","");ChapterOneRuntime.VerificationSaveDirectory=previous.Length==0?null:previous;
        string profile=SessionState.GetString(Key+"Profile","");ProfileField()?.SetValue(null,profile.Length==0?null:profile);
        Application.targetFrameRate=SessionState.GetInt(Key+"FPS",-1);QualitySettings.vSyncCount=SessionState.GetInt(Key+"VSync",0);Application.runInBackground=SessionState.GetBool(Key+"Background",false);
        if(SessionState.GetBool(Key+"CreatedView",false))Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(x=>x.GetType().FullName=="UnityEditor.GameView")?.Close();
        var setup=JsonUtility.FromJson<Setup>(SessionState.GetString(Key+"Setup","{}"));
        if(setup?.scenes!=null&&setup.scenes.Count>0)EditorSceneManager.RestoreSceneManagerSetup(setup.scenes.Select(x=>new SceneSetup{path=x.path,isLoaded=x.loaded,isActive=x.active}).ToArray());
        else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        SessionState.EraseString(Key+"Root");
    }
}
#endif
