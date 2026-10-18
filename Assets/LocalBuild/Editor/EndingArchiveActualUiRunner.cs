#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
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

// Independent serial-editor owner. Never dispatches through SidebarLayoutChecks/BookLibrary.Run.
// Source pre-choice receipts come from a copied real earlier UI run with declared historical fixtures. No night is rerun.
[InitializeOnLoad]
public static class EndingArchiveActualUiRunner
{
    const string Key="EndingArchiveActualUiRunner.";
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
    static bool preparingPreChoice,preChoiceBootstrapped;
    static string preChoiceBootstrapFailure;
    static int lastFrame;
    static double deadline;
    static string language="zh-CN",titleCapture;
    static Report report;
    [Serializable] sealed class SceneEntry {public string path;public bool loaded,active;}
    [Serializable] sealed class Setup {public List<SceneEntry> scenes=new List<SceneEntry>();}
    [Serializable] sealed class Report
    {
        public string status="running",failure="",unityVersion,completionReceipt="";
        public string scope="Copied real 20261018-215048-664 completed save and its actual pre-choice slot 4; upstream stage/fullOpen, historical AI lines/teaching records remain declared fixtures. Real title archive read-only checks; actual three-route and deterministic Ask it choice buttons with snapshot/save faults; real local-only pin/release/read/finish earns a fresh completion and checkpoint. No online night rerun, new model replies or natural history claim.";
        public string coverageSplit="Title archive replay/three public letters belongs here. Native mailbox replay Close/Minimize and completed-presentation receipt-observer races belong to the separate completed-native replay suite; not claimed passed here.";
        public string input="Title UI and physical power use queued InputSystem mouse events with actual ray checks. Main controls use the real LCD surface relay. Public-letter scroll position, fixture reload, branch money marker and shutdown command invocation are explicitly identified fixtures.";
        public string sourceReportStatus="",sourceCompletionReceipt="",sourceTreeDigest="";
        public int width,height,prerequisiteAssertions,finalAssertions;
        public List<string> checks=new List<string>(),screenshots=new List<string>(),errors=new List<string>(),cleanupErrors=new List<string>();
        public List<string> lifecycleDiagnostics=new List<string>();
        public BranchPersistenceDiagnostic branchPersistence;
        public BranchResumeDiagnostic branchResume;
    }
    [Serializable] sealed class BranchPersistenceDiagnostic
    {
        public string primaryPath,branchPath,markerBeforeSave,markerAfterSave,markerFromDisk;
        public string primaryBefore,primaryAfter,slotsBefore,slotsAfter,checkpointsBefore,checkpointsAfter;
        public bool markerExact,primaryExact,slotsExact,checkpointsExact;
        public List<string> protectedBefore,protectedAfter;
    }
    [Serializable] sealed class BranchResumeDiagnostic
    {
        public string expectedPath,expectedBranch,expectedCheckpoint,expectedMarker,diskMarkerBefore,diskBeforeHash;
        public string boundPath,boundBranch,boundCheckpoint,boundMarker,boundEarned,afterMarker,afterEarned,moneyDelta,earningsDelta;
        public string primaryBefore,primaryAfter,slotsAfter,checkpointsAfter;
        public bool bindingObserved,boundBeforeGameplay,boundMarkerExact,boundIdentityExact,sameBoundSimulation,primaryExact,slotsExact,checkpointsExact;
    }
    static EndingArchiveActualUiRunner(){EditorApplication.playModeStateChanged+=State;}
    [MenuItem("Tools/Local Verification/Ending archive actual UI")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||Root.Length>0)throw new InvalidOperationException("Run once from clean EditMode under the serial Unity owner.");
        Need(!EndingPresentationActualUiChecks.Requested,"complete or clear the separate presentation suite request before this independent archive run");
        for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save scene changes before this scene-switching QA.");
        string dir=Path.GetFullPath(Path.Combine("LocalVerification","EndingArchiveActual",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));
        Directory.CreateDirectory(Path.Combine(dir,"Save"));Directory.CreateDirectory(Path.Combine(dir,"DesktopProfile"));
        CopyEvidence(dir);
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
            EditorSceneManager.OpenScene(CaseMenuController.ScenePath,OpenSceneMode.Single);
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
        if(preparingPreChoice&&scene.path==MainSceneEntry.ScenePath)
        {
            CaptureLifecycle("pre-choice sceneLoaded before bootstrap");
            try
            {
                // Match CaseMenuMainDisplay.Configure: attach custom views before native bridge.Start.
                Need(SceneManager.GetActiveScene()==scene,"explicit Single Main is active during sceneLoaded");
                preChoiceBootstrapped=MainSceneEntry.EnsureInitialized();
                Need(preChoiceBootstrapped,"production bootstrap during pre-choice sceneLoaded");
                var rt=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<ChapterOneRuntime>(true)).Single();
                rt.GetComponent<StoryDesktopPresenter>()?.PrepareDesktopShell();
                // This historical fixture must not run story progress while its native views warm.
                QuietDrivers();rt.tickEnabled=false;
                var ctl=rt.GetComponent<XingGuangController>();if(ctl!=null)ctl.enabled=false;
                DesktopNotifications.Install(rt).enabled=false;
            }
            catch(Exception ex){preChoiceBootstrapFailure=ex.ToString();}
            CaptureLifecycle("pre-choice sceneLoaded after bootstrap before Start");
        }
    }
    static void State(PlayModeStateChange state)
    {
        if(Root.Length==0)return;
        if(state==PlayModeStateChange.EnteredPlayMode)
        {
            finishing=failing=false;lastFrame=-1;deadline=EditorApplication.timeSinceStartup+900;
            report=new Report{unityVersion=Application.unityVersion,sourceTreeDigest=SessionState.GetString(Key+"SourceDigest","")};
            try
            {
                language="zh-CN";titleCapture=null;
                var sourceReport=JsonUtility.FromJson<SourceReport>(File.ReadAllText(Path.Combine(Root,"source-report.json")));
                Need(sourceReport!=null,"copied source report readable");
                report.sourceReportStatus=sourceReport.status;report.sourceCompletionReceipt=sourceReport.completionReceipt;
                EndingArchiveActualUiChecks.Reset();EndingFinalRouteUiChecks.ResetReport();routines.Clear();routines.Push(RunFrames());
                oldProbe=StoryFocus.Probe;oldExclusive=StoryFocus.ExclusiveProbe;focusCaptured=true;
                Application.logMessageReceived+=Log;EditorApplication.update+=Tick;AssemblyReloadEvents.beforeAssemblyReload+=Reload;Save();
            }
            catch(Exception ex){report.failure=ex.ToString();Finish(false);}
        }
        if(state==PlayModeStateChange.ExitingPlayMode&&!finishing)
        {
            if(report!=null){report.status="interrupted";report.failure="PlayMode stopped before archive suite completion";}
            CleanupPlay();Save();
        }
        if(state==PlayModeStateChange.EnteredEditMode)
        {
            string dir=Root;bool passed=File.Exists(Path.Combine(dir,"passed.txt"));
            try{CleanupPlay();VerifySource();if(report!=null&&report.cleanupErrors.Count>0)passed=false;}
            catch(Exception ex){passed=false;report?.cleanupErrors.Add(ex.ToString());File.AppendAllText(Path.Combine(dir,"cleanup-errors.txt"),ex+Environment.NewLine);}
            finally
            {
                try{RestoreEditor();}
                catch(Exception ex){passed=false;report?.cleanupErrors.Add(ex.ToString());File.AppendAllText(Path.Combine(dir,"cleanup-errors.txt"),ex+Environment.NewLine);}
                if(!passed&&File.Exists(Path.Combine(dir,"passed.txt")))File.Delete(Path.Combine(dir,"passed.txt"));
                if(report!=null)
                {
                    if(!passed&&report.status=="passed"){report.status="failed";report.failure="Final evidence verification or editor cleanup failed; see cleanupErrors.";}
                    File.WriteAllText(Path.Combine(dir,"report.json"),JsonUtility.ToJson(report,true));
                }
                Debug.Log("ENDING_ARCHIVE_ACTUAL "+(passed?"PASS":"FAIL/INTERRUPTED")+": "+dir);if(Application.isBatchMode)EditorApplication.Exit(passed?0:1);
            }
        }
    }
    static void Log(string message,string stack,LogType type)
    {
        if(report==null||finishing)return;
        if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)report.errors.Add(message+"\n"+stack);
    }
    static void Need(bool value,string message){if(!value)throw new InvalidOperationException("Ending archive runner: "+message);}
    static void Check(bool value,string message){Need(value,message);report.checks.Add(message);}
    static void Save(){if(report!=null&&Root.Length>0)File.WriteAllText(Path.Combine(Root,"report.json"),JsonUtility.ToJson(report,true));}
    static IEnumerator Frames(int count=3){while(count-->0)yield return null;}
    static IEnumerator Until(Func<bool> condition,string message,double seconds=20)
    {
        double until=EditorApplication.timeSinceStartup+seconds;
        while(!condition())
        {
            if(EditorApplication.timeSinceStartup>=until){CaptureLifecycle("timeout: "+message);Need(false,message);}
            yield return null;
        }
        yield return Frames();
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying||finishing||lastFrame==Time.frameCount)return;lastFrame=Time.frameCount;
        try
        {
            if(!failing){Need(EditorApplication.timeSinceStartup<deadline,"900-second overall deadline");Need(report.errors.Count==0,"runtime error: "+report.errors.FirstOrDefault());}
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
    static readonly string SourceSave = Path.GetFullPath("LocalVerification/EndingFinalRoute/20261018-215048-664/Save");
    [Serializable] sealed class SourceReport { public string status, completionReceipt; }
    static string Digest(byte[] bytes) { using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant(); }
    static string TreeDigest(string root)
    {
        Need(Directory.Exists(root),"evidence directory exists");
        var lines=new List<string>();
        foreach(string path in Directory.GetDirectories(root,"*",SearchOption.AllDirectories).Concat(new[]{root}))
            Need((File.GetAttributes(path)&FileAttributes.ReparsePoint)==0,"evidence has no redirected directories");
        foreach(string path in Directory.GetFiles(root,"*",SearchOption.AllDirectories).OrderBy(x=>x,StringComparer.Ordinal))
        {Need((File.GetAttributes(path)&FileAttributes.ReparsePoint)==0,"evidence file is ordinary");lines.Add(path.Substring(root.Length).Replace('\\','/')+":"+Digest(File.ReadAllBytes(path)));}
        return Digest(Encoding.UTF8.GetBytes(string.Join("\n",lines)));
    }
    static void CopyTree(string source,string destination)
    {
        Need(!Directory.Exists(destination)||Directory.GetFileSystemEntries(destination).Length==0,"fresh evidence destination");
        Directory.CreateDirectory(destination);
        foreach(string path in Directory.GetFiles(source,"*",SearchOption.AllDirectories))
        {
            string target=Path.Combine(destination,path.Substring(source.Length).TrimStart(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(path,target,false);
        }
    }
    static void CopyEvidence(string destination)
    {
        string digest=TreeDigest(SourceSave);
        var source=new AtomicSaveStore(SourceSave).Load();Need(source.Success&&!source.Recovered,"real completed evidence primary is readable");
        var house=JsonUtility.FromJson<GameState>(source.Payload);var lab=JsonUtility.FromJson<XgState>(house.labState);
        Need(XgEndingArchiveRules.Completed(lab)&&lab.epilogue.completionReceipt=="epilogue:08e66e41d5ad4cf08491ca7b2a7c2208","exact actual earlier completion receipt");
        var before=new AtomicSaveStore(Path.Combine(SourceSave,"slots","4")).Load();Need(before.Success&&!before.Recovered,"actual earlier pre-choice slot 4 readable");
        var beforeLab=JsonUtility.FromJson<XgState>(JsonUtility.FromJson<GameState>(before.Payload).labState);
        Need(XgEndingArchiveRules.BeforeChoice(beforeLab)&&beforeLab.endingPreparation.memoryReviewReceipt==lab.endingPreparation.memoryReviewReceipt,"real completed evidence and pre-choice source share actual memory-review receipt");
        CopyTree(SourceSave,Path.Combine(destination,"EvidenceCopy"));CopyTree(SourceSave,Path.Combine(destination,"Save"));
        Need(TreeDigest(SourceSave)==digest&&TreeDigest(Path.Combine(destination,"EvidenceCopy"))==digest&&TreeDigest(Path.Combine(destination,"Save"))==digest,"all original snapshot blocks/slots copied exactly; source unchanged");
        File.Copy(Path.Combine(Path.GetDirectoryName(SourceSave),"report.json"),Path.Combine(destination,"source-report.json"),false);
        File.WriteAllText(Path.Combine(destination,"source-sha256.txt"),digest+"\n"+SourceSave+"\nOriginal report is failed in later replay; its actual completion receipt is validated, not reclassified as a passed suite.\n");
        SessionState.SetString(Key+"SourceDigest",digest);
    }
    static void VerifySource()
    {
        string expected=SessionState.GetString(Key+"SourceDigest","");
        if(TreeDigest(SourceSave)!=expected||TreeDigest(Path.Combine(Root,"EvidenceCopy"))!=expected)
            throw new InvalidOperationException("Original evidence or immutable EvidenceCopy changed during QA.");
    }
    static IEnumerator CaptureTitle(string name)
    {ScreenCapture.CaptureScreenshot(Path.Combine(Root,name));yield return ValidateScreenshot(name,true);}
    static IEnumerator RunFrames()
    {
        SetupInput();locale=GameText.Bind(()=>language);
        yield return Until(()=>Object.FindAnyObjectByType<CaseMenuController>()?.IsMainReady==true,"copied completed source preloaded in actual title",60);
        var menu=Object.FindAnyObjectByType<CaseMenuController>();
        yield return EndingArchiveActualUiChecks.CompletedTitleFrames(menu,report.sourceCompletionReceipt,Pointer,CaptureTitle,"real-source-zh");
        var hud=menu.GetComponent<CaseMenuHud>();
        Check(!EndingArchiveActualUiChecks.Named(hud,"CreateEndingBranch",false).IsInteractable(),"old real completion without automatic checkpoint does not fabricate one");
        yield return EndingArchiveActualUiChecks.TitleTap(hud,"CloseEndings",Pointer);
        // Explicit lifecycle fixture: preserve EvidenceCopy, write its real pre-choice slot into the separate working store.
        foreach(var active in Object.FindObjectsByType<ChapterOneRuntime>(FindObjectsInactive.Include,FindObjectsSortMode.None))active.useDiskSave=false;
        var before=new AtomicSaveStore(Path.Combine(Root,"EvidenceCopy","slots","4")).Load();Need(before.Success,"copied actual pre-choice payload readable");
        Need(new AtomicSaveStore(Path.Combine(Root,"Save")).TrySaveSnapshot(before.Payload,out var message),message);
        preparingPreChoice=true;preChoiceBootstrapped=false;preChoiceBootstrapFailure=null;
        try{yield return SceneLoad(MainSceneEntry.ScenePath);}
        finally{preparingPreChoice=false;}
        CaptureLifecycle("pre-choice after native Start frames");
        Need(preChoiceBootstrapped&&preChoiceBootstrapFailure==null,"pre-choice sceneLoaded bootstrap completed: "+preChoiceBootstrapFailure);
        var controllers=Object.FindObjectsByType<XingGuangController>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        Need(controllers.Length==1&&controllers[0].gameObject.scene==SceneManager.GetActiveScene(),"exactly one scene-local pre-choice controller including inactive objects");
        var loaded=controllers[0].runtime;
        var expected=JsonUtility.FromJson<GameState>(before.Payload);var expectedLab=JsonUtility.FromJson<XgState>(expected.labState);
        Need(loaded!=null&&loaded.Sim!=null&&loaded.Sim.S.prologue==expected.prologue&&loaded.Sim.S.appInstallState==expected.appInstallState
            &&loaded.SavePath==new AtomicSaveStore(Path.Combine(Root,"Save")).SavePath,"pre-choice runtime retains source prologue/install state and isolated primary identity");
        Need(controllers[0].Sim!=null&&controllers[0].Sim.S.stage==expectedLab.stage&&XgEndingArchiveRules.BeforeChoice(controllers[0].Sim.S)
            &&controllers[0].Sim.S.endingPreparation.memoryReviewReceipt==expectedLab.endingPreparation.memoryReviewReceipt,"pre-choice controller bound to actual source stage and memory-review receipt");
        var binding=loaded.GetComponent<ChapterOneDesktopRouter>()?.Get(XingGuangController.AppId);
        Need(binding!=null&&ReferenceEquals(binding.CustomView,controllers[0])&&binding.Window==controllers[0].Window&&binding.Content!=null,"native LingGuang binding owns attached controller and content");
        yield return Until(()=>Object.FindAnyObjectByType<XingGuangController>()?.View!=null,"pre-choice Main loaded",60);
        var ctl=Object.FindAnyObjectByType<XingGuangController>();var rt=ctl.runtime;
        QuietDrivers();var boot=Object.FindAnyObjectByType<BootManager>(FindObjectsInactive.Include);
        if(boot!=null){boot.CancelInvoke();boot.StopAllCoroutines();if(boot.bootAnimator!=null)boot.bootAnimator.gameObject.SetActive(false);}
        var user=Object.FindAnyObjectByType<UserManager>(FindObjectsInactive.Include);Need(user!=null&&user.isActiveAndEnabled,"native user manager available");user.ShowDesktop();
        yield return Until(()=>DesktopReady(rt),"pre-choice desktop ready",30);
        // PrologueDirector owns the shared LCD desk even for saves whose prologue is complete.
        // Its normal Update builds that layer; the fixture paused it before its first Update.
        var prologue=rt.GetComponent<PrologueDirector>();
        Need(prologue!=null&&!rt.Sim.InPrologue&&(string.IsNullOrEmpty(ctl.Sim.S.ending)||rt.Sim.S.endingPlayed),"restored fixture permits desk initialization without replaying prologue or legacy ending");
        try
        {
            prologue.enabled=true;
            yield return Until(()=>PrologueDirector.Desk?.layer!=null,"native shared LCD desk initialized",15);
        }
        finally{prologue.CancelInvoke();prologue.StopAllCoroutines();prologue.enabled=false;}
        Need(ReferenceEquals(typeof(PrologueDirector).GetField("desk",F).GetValue(prologue),PrologueDirector.Desk)
            &&PrologueDirector.Desk.layer.gameObject.scene==rt.gameObject.scene,"shared LCD desk belongs to the current pre-choice Main owner");
        CaptureLifecycle("pre-choice shared native desk ready and frozen");
        IsolateTicks(ctl,true);DesktopNotifications.Install(rt).enabled=false;StoryFocus.Probe=()=>true;StoryFocus.ExclusiveProbe=()=>false;
        ctl.Window.CloseWindow();var host=XgEndingPreparationHost.Install(ctl);host.enabled=true;
        yield return EndingArchiveActualUiChecks.ChoiceTransactions(ctl,CaptureClick,CaptureLcd);
        yield return EndingArchiveActualUiChecks.PublishChosenLocal(ctl,CaptureClick);
        // Existing native letter checks earn real saved/opened/read geometry and completion; no fake flags or second night.
        yield return EndingFinalRouteUiChecks.LetterFailureFrames(ctl,CaptureClick,CaptureLcd);
        yield return EndingFinalRouteUiChecks.CompleteReturnFrames(ctl,CaptureClick,name=>{titleCapture=name;ScreenCapture.CaptureScreenshot(Path.Combine(Root,name));},CaptureLcd);
        yield return ValidateScreenshot(titleCapture,true);
        menu=Object.FindAnyObjectByType<CaseMenuController>();string receipt=EndingFinalRouteUiChecks.CompletionReceipt;
        yield return EndingArchiveActualUiChecks.CompletedTitleFrames(menu,receipt,Pointer,CaptureTitle,"new-local-zh");
        hud=menu.GetComponent<CaseMenuHud>();yield return EndingArchiveActualUiChecks.TitleTap(hud,"CloseEndings",Pointer);
        SetLanguage(true);
        yield return EndingArchiveActualUiChecks.CompletedTitleFrames(menu,receipt,Pointer,CaptureTitle,"new-local-en");
        yield return BranchLifecycle(menu,receipt);
        VerifySource();Check(report.errors.Count==0,"no runtime errors and original evidence tree unchanged");GatherChecks();Save();
    }
    static IEnumerator SceneLoad(string path)
    {var operation=SceneManager.LoadSceneAsync(path,LoadSceneMode.Single);Need(operation!=null,"explicit isolated lifecycle scene load started");while(!operation.isDone)yield return null;yield return Frames();}
    static string PrivateState(object value,string name)=>value==null?"null":Convert.ToString(value.GetType().GetField(name,F)?.GetValue(value))??"missing";
    static string ObjectState(Component value)=>value==null?"null":value.GetType().Name+"#"+value.GetInstanceID()+" scene="+value.gameObject.scene.path+" active="+value.gameObject.activeInHierarchy+(value is Behaviour b?" enabled="+b.enabled:"");
    static void CaptureLifecycle(string phase)
    {
        if(report==null||Root.Length==0)return;
        var text=new StringBuilder();text.AppendLine(phase+" frame="+Time.frameCount+" activeScene="+SceneManager.GetActiveScene().path);
        text.AppendLine("bootstrap="+preChoiceBootstrapped+" failure="+preChoiceBootstrapFailure);
        foreach(var rt in Object.FindObjectsByType<ChapterOneRuntime>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            text.AppendLine(ObjectState(rt)+" initialized="+PrivateState(rt,"initialized")+" testMode="+rt.TestMode+" tick="+rt.tickEnabled+" disk="+rt.useDiskSave+" save="+rt.SavePath+" status="+rt.SaveStatus);
            if(rt.Sim!=null)text.AppendLine("house prologue="+rt.Sim.S.prologue+" install="+rt.Sim.S.appInstallState+" branch="+rt.Sim.S.endingBranchId+" labSHA256="+Digest(Encoding.UTF8.GetBytes(rt.Sim.S.labState??"")));
            var presenter=rt.GetComponent<StoryDesktopPresenter>();text.AppendLine(ObjectState(presenter)+" shellPrepared="+PrivateState(presenter,"shellPrepared"));
            var prologue=rt.GetComponent<PrologueDirector>();text.AppendLine(ObjectState(prologue)+" running="+(prologue!=null&&prologue.Running)+" sharedDesk="+ObjectState(PrologueDirector.Desk?.layer));
            var router=rt.GetComponent<ChapterOneDesktopRouter>();text.AppendLine("router="+ObjectState(router));
            if(router!=null)foreach(var binding in router.Bindings)if(binding!=null)
                text.AppendLine("binding="+binding.ApplicationId+" "+ObjectState(binding)+" initialized="+binding.IsInitialized+" custom="+(binding.CustomView?.GetType().Name??"null")+" window="+ObjectState(binding.Window)+" content="+ObjectState(binding.Content));
            foreach(var window in DesktopWindowIndex.Get(rt))if(window!=null)text.AppendLine("indexed window="+window.name+" "+ObjectState(window)+" open="+window.isOn);
        }
        foreach(var ctl in Object.FindObjectsByType<XingGuangController>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            text.AppendLine("controller "+ObjectState(ctl)+" initialized="+PrivateState(ctl,"initialized")+" runtime="+ObjectState(ctl.runtime)+" window="+ObjectState(ctl.Window)+" view="+ObjectState(ctl.View)+" stage="+(ctl.Sim==null?-1:ctl.Sim.S.stage));
        string filename="lifecycle-"+report.lifecycleDiagnostics.Count.ToString("D2")+".txt";
        File.WriteAllText(Path.Combine(Root,filename),text.ToString());report.lifecycleDiagnostics.Add(filename+": "+phase);Save();
    }
    static IEnumerator BranchLifecycle(CaseMenuController menu,string receipt)
    {
        var hud=menu.GetComponent<CaseMenuHud>();var rt=menu.GetComponent<CaseMenuMainDisplay>().Runtime;
        string primaryPath=rt.SavePath,root=Path.GetDirectoryName(primaryPath),checkpoint=rt.Sim.S.endingCheckpointId;
        var original=rt.Sim;string originalLab=original.S.labState;byte[] primary=File.ReadAllBytes(primaryPath);
        string checkpointDigest=TreeDigest(Path.Combine(root,"endings","checkpoints"));
        string slotsDigest=TreeDigest(Path.Combine(root,"slots"));
        var persistence=new BranchPersistenceDiagnostic { primaryPath=primaryPath,primaryBefore=Digest(primary),
            slotsBefore=slotsDigest,checkpointsBefore=checkpointDigest,protectedBefore=ProtectedBranchFiles(root) };
        report.branchPersistence=persistence;Save();
        Need(rt.HasEndingChoice(original,s=>XgEndingArchiveRules.BeforeChoice(JsonUtility.FromJson<XgState>(s))),"new completion has actual automatic before-choice snapshot");
        // Destination failure is an explicit filesystem fixture in this run's private directory only.
        string branches=Path.Combine(root,"endings","branches");Need(!Directory.Exists(branches)&&!File.Exists(branches),"no previous branch directory before failure fixture");
        File.WriteAllText(branches,"QA destination obstruction");
        try{yield return EndingArchiveActualUiChecks.TitleTap(hud,"CreateEndingBranch",Pointer);}
        finally{File.Delete(branches);}
        Check(ReferenceEquals(rt.Sim,original)&&rt.SavePath==primaryPath&&File.ReadAllBytes(primaryPath).SequenceEqual(primary)&&hud.IsArchiveOpen&&!menu.HasEnteredGame,
            "actual branch button write failure preserves completed identity/primary and stays retryable in archive");
        yield return CaptureTitle("ending-archive-branch-write-failure.png");
        yield return EndingArchiveActualUiChecks.TitleTap(hud,"CreateEndingBranch",Pointer);
        Need(!ReferenceEquals(rt.Sim,original)&&rt.SavePath!=primaryPath,"actual branch button switches to a distinct store/Sim");
        string branchId=rt.Sim.S.endingBranchId,branchPath=rt.SavePath;
        persistence.branchPath=branchPath;
        Need(branchPath.StartsWith(branches+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)&&rt.Sim.S.endingCheckpointId==checkpoint,"branch identity lives in its dedicated root");
        yield return Until(()=>menu.HasEnteredGame&&!menu.IsTransitioning,"branch button enters real Main through physical title flight",60);
        var ctl=rt.GetComponent<XingGuangController>();QuietDrivers();IsolateTicks(ctl,true);DesktopNotifications.Install(rt).enabled=false;
        Need(XgEndingArchiveRules.BeforeChoice(EndingArchiveActualUiChecks.Lab(Path.GetDirectoryName(branchPath))),"branch disk restores real pre-choice state without clearing the completed source");
        // Explicit fixture, kept integral so exact equality checks branch persistence rather than arbitrary floating-point JSON precision.
        // The real desktop transition above is allowed to run; no product income or timing is changed.
        double marker=Math.Floor(rt.Sim.S.money)+7;
        Need(!double.IsNaN(marker)&&!double.IsInfinity(marker)&&Math.Abs(marker)<9007199254740992d,"branch marker is an exactly representable integer");
        rt.Sim.S.money=marker;persistence.markerBeforeSave=ExactDouble(marker);Save();
        Need(rt.SaveNow(),"explicit branch-only persistence marker saved");
        double diskMarker=EndingArchiveActualUiChecks.House(Path.GetDirectoryName(branchPath)).money;
        var primaryAfter=File.ReadAllBytes(primaryPath);
        persistence.markerAfterSave=ExactDouble(rt.Sim.S.money);persistence.markerFromDisk=ExactDouble(diskMarker);
        persistence.primaryAfter=Digest(primaryAfter);persistence.slotsAfter=TreeDigest(Path.Combine(root,"slots"));
        persistence.checkpointsAfter=TreeDigest(Path.Combine(root,"endings","checkpoints"));persistence.protectedAfter=ProtectedBranchFiles(root);
        persistence.markerExact=diskMarker==marker;persistence.primaryExact=primaryAfter.SequenceEqual(primary);
        persistence.slotsExact=persistence.slotsAfter==slotsDigest;persistence.checkpointsExact=persistence.checkpointsAfter==checkpointDigest;
        Save(); // Persist every sub-result and per-file hash before the strict assertion can fail.
        Check(persistence.markerExact&&persistence.primaryExact&&persistence.slotsExact&&persistence.checkpointsExact,
            "actual branch store writes its own full save; original completed primary, manual slots and immutable snapshots remain byte-identical");
        yield return CaptureTitle("ending-archive-independent-branch-main.png");
        int oldMenu=menu.GetInstanceID();
        // Production save+return command is invoked explicitly; this is not a claim of clicking a desktop shutdown control.
        GameExit.ReturnToTitle();
        yield return Until(()=>Object.FindAnyObjectByType<CaseMenuController>()!=null&&Object.FindAnyObjectByType<CaseMenuController>().GetInstanceID()!=oldMenu&&Object.FindAnyObjectByType<CaseMenuController>().IsMainReady,"production restart returned to fresh title and original primary",60);
        menu=Object.FindAnyObjectByType<CaseMenuController>();rt=menu.GetComponent<CaseMenuMainDisplay>().Runtime;hud=menu.GetComponent<CaseMenuHud>();
        Need(rt.SavePath==primaryPath&&rt.Sim.S.labState==originalLab&&string.IsNullOrEmpty(rt.Sim.S.endingBranchId),"fresh title defaults to the unchanged completed primary, not branch");
        Check(EndingArchiveActualUiChecks.Lab(root).epilogue.completionReceipt==receipt,"real completion preserved across branch shutdown/restart; normal lastSeen initialization is outside read-only hash measurement");
        yield return EndingArchiveActualUiChecks.Power(menu,Pointer);
        yield return EndingArchiveActualUiChecks.TitleTap(hud,"EndingBranches",Pointer);
        yield return CaptureTitle("ending-archive-branch-list-after-restart.png");
        var restoredPrimary=File.ReadAllBytes(primaryPath);
        var branchDisk=EndingArchiveActualUiChecks.House(Path.GetDirectoryName(branchPath));
        var resume=new BranchResumeDiagnostic { expectedPath=branchPath,expectedBranch=branchId,expectedCheckpoint=checkpoint,
            expectedMarker=ExactDouble(marker),diskMarkerBefore=ExactDouble(branchDisk.money),diskBeforeHash=Digest(File.ReadAllBytes(branchPath)),
            primaryBefore=Digest(restoredPrimary) };
        report.branchResume=resume;Save();
        Need(branchDisk.endingBranchId==branchId&&branchDisk.endingCheckpointId==checkpoint&&branchDisk.money==marker,
            "saved branch retains its exact marker and identity before the actual branch row click");
        // Observe Attach's synchronous notification. The row callback invokes archiveStart only after ResumeEndingBranch returns.
        // This read-only observer never asserts, writes a save, or interrupts the real desktop transition.
        var resumeRuntime=rt;var sourceSimulation=rt.Sim;ChapterOneSim resumedSimulation=null;
        var resumeDisplay=menu.GetComponent<CaseMenuMainDisplay>();var resumeMenu=menu;
        double boundMoney=0,boundEarned=0;
        Action observeBinding=()=>
        {
            if(resume.bindingObserved||ReferenceEquals(resumeRuntime.Sim,sourceSimulation))return;
            resumedSimulation=resumeRuntime.Sim;if(resumedSimulation==null)return;
            resume.bindingObserved=true;resume.boundPath=resumeRuntime.SavePath;
            resume.boundBranch=resumedSimulation.S.endingBranchId;resume.boundCheckpoint=resumedSimulation.S.endingCheckpointId;
            boundMoney=resumedSimulation.S.money;boundEarned=resumedSimulation.S.totalEarned;
            resume.boundMarker=ExactDouble(boundMoney);resume.boundEarned=ExactDouble(boundEarned);
            resume.boundBeforeGameplay=!resumeMenu.HasEnteredGame&&!resumeMenu.IsTransitioning&&!resumeDisplay.HasEnteredDesktop;
            resume.boundMarkerExact=boundMoney==marker;
            resume.boundIdentityExact=resume.boundPath==branchPath&&resume.boundBranch==branchId&&resume.boundCheckpoint==checkpoint;
        };
        resumeRuntime.Changed+=observeBinding;
        try { yield return EndingArchiveActualUiChecks.TitleTap(hud,"Branch_"+branchId,Pointer); }
        finally { resumeRuntime.Changed-=observeBinding; }
        Save();
        Check(resume.bindingObserved&&resume.boundBeforeGameplay&&resume.boundIdentityExact&&resume.boundMarkerExact,
            "actual branch row restores the exact disk marker and independent identity before gameplay resumes");
        yield return Until(()=>menu.HasEnteredGame&&!menu.IsTransitioning,"actual saved branch row resumes its independent Main",60);
        rt=menu.GetComponent<CaseMenuMainDisplay>().Runtime;IsolateTicks(rt.GetComponent<XingGuangController>(),true);QuietDrivers();
        // Gameplay hosts really ran during EnterDesktop. Their earnings are evidence, not a failed load-marker comparison.
        resume.afterMarker=ExactDouble(rt.Sim.S.money);resume.afterEarned=ExactDouble(rt.Sim.S.totalEarned);
        resume.moneyDelta=ExactDouble(rt.Sim.S.money-boundMoney);resume.earningsDelta=ExactDouble(rt.Sim.S.totalEarned-boundEarned);
        resume.sameBoundSimulation=ReferenceEquals(rt.Sim,resumedSimulation);
        var resumedPrimary=File.ReadAllBytes(primaryPath);resume.primaryAfter=Digest(resumedPrimary);
        resume.primaryExact=resumedPrimary.SequenceEqual(restoredPrimary);
        resume.slotsAfter=TreeDigest(Path.Combine(root,"slots"));resume.checkpointsAfter=TreeDigest(Path.Combine(root,"endings","checkpoints"));
        resume.slotsExact=resume.slotsAfter==slotsDigest;resume.checkpointsExact=resume.checkpointsAfter==checkpointDigest;Save();
        Check(rt.SavePath==branchPath&&rt.Sim.S.endingBranchId==branchId&&rt.Sim.S.endingCheckpointId==checkpoint
            &&resume.sameBoundSimulation&&resume.primaryExact&&resume.slotsExact&&resume.checkpointsExact,
            "real desktop transition keeps the restored branch identity and leaves original primary, slots and checkpoints byte-identical; live earnings are recorded separately");
    }
    static string ExactDouble(double value)=>value.ToString("R",System.Globalization.CultureInfo.InvariantCulture)
        +" bits=0x"+BitConverter.DoubleToInt64Bits(value).ToString("X16");
    static List<string> ProtectedBranchFiles(string root)
    {
        string branches=Path.Combine(root,"endings","branches")+Path.DirectorySeparatorChar;
        return Directory.GetFiles(root,"*",SearchOption.AllDirectories)
            .Where(path=>!path.StartsWith(branches,StringComparison.OrdinalIgnoreCase)).OrderBy(path=>path,StringComparer.Ordinal)
            .Select(path=>path.Substring(root.Length).Replace('\\','/')+":"+Digest(File.ReadAllBytes(path))).ToList();
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
            if(n=="StoryDirector"||n=="StoryDesktopPresenter"||n=="PrologueDirector"||n=="SpiderIntroScene"||n=="LoveQuestionCutscene"||n=="StageOneTutor"||n=="LaoZhouYY"||n=="ZhouCardLoan")
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
        string name="ending-archive-failure-full-frame.png";ScreenCapture.CaptureScreenshot(Path.Combine(Root,name));
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
        mouse=InputSystem.AddDevice<Mouse>("EndingArchiveActualQaMouse");keyboard=InputSystem.AddDevice<Keyboard>("EndingArchiveActualQaKeyboard");mouse.MakeCurrent();keyboard.MakeCurrent();
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
        foreach(var item in EndingArchiveActualUiChecks.Checks.Concat(EndingFinalRouteUiChecks.Checks))if(!report.checks.Contains(item))report.checks.Add(item);
        report.prerequisiteAssertions=EndingArchiveActualUiChecks.Assertions;report.finalAssertions=EndingFinalRouteUiChecks.AssertionCount;
        report.completionReceipt=EndingFinalRouteUiChecks.CompletionReceipt;
    }
    static void DisposeRoutines()
    {
        while(routines.Count>0)try{(routines.Pop() as IDisposable)?.Dispose();}catch(Exception ex){report?.cleanupErrors.Add("Iterator disposal: "+ex);}
    }
    static void CleanupPlay()
    {
        EditorApplication.update-=Tick;Application.logMessageReceived-=Log;AssemblyReloadEvents.beforeAssemblyReload-=Reload;SceneManager.sceneLoaded-=DisableModels;
        DisposeRoutines();
        // Disable disk first: ExitPlaymode callbacks must not overwrite the persisted final receipt.
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
        if(ok)File.WriteAllText(Path.Combine(Root,"passed.txt"),"Actual ending archive UI passed; see source/fixture/coverage limits in report. No online night rerun or native mailbox replay claim.");
        EditorApplication.ExitPlaymode();
    }
    static void Reload()
    {
        if(Root.Length==0)return;finishing=true;if(report!=null){report.status="interrupted";report.failure="Assembly reload interrupted ending-archive QA";}
        CleanupPlay();Save();
    }
    static void RestoreEditor()
    {
        try
        {
        string previous=SessionState.GetString(Key+"SaveOverride","");ChapterOneRuntime.VerificationSaveDirectory=previous.Length==0?null:previous;
        string profile=SessionState.GetString(Key+"Profile","");ProfileField()?.SetValue(null,profile.Length==0?null:profile);
        Application.targetFrameRate=SessionState.GetInt(Key+"FPS",-1);QualitySettings.vSyncCount=SessionState.GetInt(Key+"VSync",0);Application.runInBackground=SessionState.GetBool(Key+"Background",false);
        if(SessionState.GetBool(Key+"CreatedView",false))Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(x=>x.GetType().FullName=="UnityEditor.GameView")?.Close();
        var setup=JsonUtility.FromJson<Setup>(SessionState.GetString(Key+"Setup","{}"));
        if(setup?.scenes!=null&&setup.scenes.Count>0)EditorSceneManager.RestoreSceneManagerSetup(setup.scenes.Select(x=>new SceneSetup{path=x.path,isLoaded=x.loaded,isActive=x.active}).ToArray());
        else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        }
        finally
        {
            SessionState.EraseString(Key+"Root");
            SessionState.EraseString(Key+"SourceDigest");
        }
    }
}
#endif
