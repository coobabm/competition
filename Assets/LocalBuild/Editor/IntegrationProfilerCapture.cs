#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LingGuangV05.Core;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.LLM;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

// Batch entry: -batchmode -executeMethod IntegrationProfilerCapture.Run (NO -quit).
// This is an editor workload capture, not a standalone-player or model-inference benchmark.
[InitializeOnLoad]
public static class IntegrationProfilerCapture
{
    const string Key = "IntegrationProfilerCapture.Root";
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static string Root => SessionState.GetString(Key, "");
    static readonly string[] Phases = { "teach-continuous", "train-auto", "yy-tieba-switch" };
    static XingGuangController ctl;
    static ChapterOneRuntime runtime;
    static ChapterOneDesktopRouter router;
    static int renderFrame=-1;
    static int phase = -1, frame = -1, actions, completed, startsAnswers, startsEpochs, firstProfilerFrame;
    static double started, nextAction, timeout;
    static bool ready, finishing,fixtureBound,measuring,skipFirstSample,warmScreenshot,tiebaWarmScreenshot;
    static MonoBehaviour expectedView;
    static readonly List<string> screenshotFiles=new List<string>();
    static double warmingSince,expectedSince;
    static int visibleChecks;
    static string expectedApp;
    static LingGuangV05.Desktop.OS.WindowManager expectedWindow;
    static RectTransform expectedRoot;
    static TMPro.TMP_Text[] expectedLabels;
    const double WarmupSeconds=3;

    static readonly List<Counter> counters = new List<Counter>();
    static readonly List<float> frameTimes = new List<float>(4096);
    [Serializable] sealed class Metric { public string name, unit; public int samples; public double mean, max; }
    sealed class Counter { public ProfilerRecorder recorder; public Metric metric; public double sum; }
    [Serializable] sealed class Report
    {
        public string scenario, scope = "Unity Editor PlayMode; actual visible quiz control callbacks (not physical Aero pointer input); one capture tick per rendered frame; isolated save; local LLM disabled; scheduled BeginEpoch requests are automation workload, not purchased automation";
        public string sourceCommit, dirtyFiles, unityVersion, audioDriver;
        public bool batchMode, audioListenerPaused;
        public int visibleChecks;public double warmupSeconds=WarmupSeconds;
        public List<string> verifiedScreenshots=new List<string>();
        public string visibility="Actual active window + active content + rendered TMP glyph checks; screenshots during warmup; first sample after recorder setup discarded. Previous captures lacked this guarantee and are not a visible-UI baseline.";
        public int renderedFrames, actionAttempts, completedActions, answersBefore, answersAfter, epochsBefore, epochsAfter, audioSampleRate;
        public double wallSeconds, frameMeanMs, frameP95Ms, frameMaxMs;
        public List<Metric> metrics = new List<Metric>();
    }
    static IntegrationProfilerCapture() { EditorApplication.playModeStateChanged += State; }
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Capture must start in Edit Mode");
        string path = Path.GetFullPath(Path.Combine("LocalVerification", "IntegrationProfiler", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + "-" + Guid.NewGuid().ToString("N").Substring(0,6)));
        Directory.CreateDirectory(path); Directory.CreateDirectory(Path.Combine(path, "Save")); Directory.CreateDirectory(Path.Combine(path, "DesktopProfile"));
        File.WriteAllText(Path.Combine(path, "source.txt"), "HEAD\n" + Git("rev-parse HEAD") + "\nDIRTY\n" + Git("status --short") + "\nNo claim of pristine/pre-change baseline. Capture excludes local-model inference. Audio counters only prove values when valid/nonzero; batch audio may be inactive.");
        SessionState.SetString(Key, path);
        UnityEditorInternal.ProfilerDriver.deepProfiling = false;
        EditorSceneManager.OpenScene("Assets/LingGuangV05/Scenes/Main.unity");
        EditorApplication.EnterPlaymode();
    }
    static string Git(string args)
    {
        try { var p = new System.Diagnostics.Process { StartInfo = new System.Diagnostics.ProcessStartInfo("git", args) { RedirectStandardOutput=true, UseShellExecute=false, CreateNoWindow=true } }; p.Start(); string s=p.StandardOutput.ReadToEnd(); p.WaitForExit(); return s.Trim(); }
        catch(Exception e) { return "unavailable: " + e.Message; }
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Isolate()
    {
        if (string.IsNullOrEmpty(Root)) return;
        ChapterOneRuntime.VerificationSaveDirectory = Path.Combine(Root, "Save");
        typeof(LingGuangV05.Desktop.OS.DreamOSDataManager).GetField("dataPath", BindingFlags.Static|BindingFlags.NonPublic)?.SetValue(null, Path.Combine(Root,"DesktopProfile") + Path.DirectorySeparatorChar);
        SceneManager.sceneLoaded += DisableModel;
    }
    static void DisableModel(Scene scene, LoadSceneMode mode)
    {
        if (string.IsNullOrEmpty(Root)) return;
        foreach(var root in scene.GetRootGameObjects()) foreach(var rt in root.GetComponentsInChildren<ChapterOneRuntime>(true))
        { var llm=rt.GetComponent<LocalLlm>() ?? rt.gameObject.AddComponent<LocalLlm>(); llm.enabled=false; }
    }
    static void State(PlayModeStateChange s)
    {
        if(string.IsNullOrEmpty(Root)) return;
        if(s==PlayModeStateChange.EnteredPlayMode) { XgSimulationMarker.Begin=name=>Profiler.BeginSample(name);XgSimulationMarker.End=()=>Profiler.EndSample();timeout=EditorApplication.timeSinceStartup+240; EditorApplication.update+=Tick; }
        if(s==PlayModeStateChange.ExitingPlayMode) { XgSimulationMarker.Begin=null;XgSimulationMarker.End=null; }
        if(s==PlayModeStateChange.EnteredEditMode && (finishing || File.Exists(Path.Combine(Root,"complete.txt")))) { SessionState.EraseString(Key); EditorApplication.Exit(0); }
    }
    static void Tick()
    {
        if (!EditorApplication.isPlaying || finishing || Time.frameCount==renderFrame) return;
        renderFrame=Time.frameCount;
        try {
            double now=EditorApplication.timeSinceStartup;
            if(now>timeout) throw new TimeoutException("Capture did not complete within 180 seconds");
            if(!ready) {
                runtime=UnityEngine.Object.FindAnyObjectByType<ChapterOneRuntime>();
                ctl=runtime!=null ? runtime.GetComponent<XingGuangController>() : null;
                if(ctl==null || ctl.Sim==null || ctl.View==null) return;
                if(fixtureBound){
                    var binding=router?.Get(XingGuangController.AppId);
                    if(binding?.Window==null||binding.TaskbarShortcut==null||!binding.TaskbarShortcut.isActiveAndEnabled||binding.DesktopShortcut==null||!binding.DesktopShortcut.gameObject.activeInHierarchy)return;
                    ready=true;PreparePhase(0);return;
                }
                StopNarrativeDrivers();
                runtime.useDiskSave=false;
                var lab=new XgSim(); lab.S.gradualSystems=true; lab.BeginOpening();
                var house=new ChapterOneSim(); house.S.prologue=2; house.S.appInstallState=2; house.S.money=100000;
                house.S.gpuModels.Add("gtx980ti"); house.S.gpuCount=house.S.gpuModels.Count; house.S.labState=JsonUtility.ToJson(lab.S);
                runtime.GetType().GetMethod("Attach",Flags).Invoke(runtime,new object[]{house});
                ctl.GetType().GetMethod("BindSave",Flags).Invoke(ctl,new object[]{house});
                if(YYChatHub.Instance?.S!=null) YYChatHub.Instance.S.lingguangUnlocked=true;
                router=runtime.GetComponent<ChapterOneDesktopRouter>();
                var boot=UnityEngine.Object.FindAnyObjectByType<LingGuangV05.Desktop.OS.BootManager>(FindObjectsInactive.Include);
                if(boot!=null){boot.CancelInvoke();boot.StopAllCoroutines();if(boot.bootAnimator!=null)boot.bootAnimator.gameObject.SetActive(false);}
                var user=UnityEngine.Object.FindAnyObjectByType<LingGuangV05.Desktop.OS.UserManager>(FindObjectsInactive.Include);
                Require(user!=null&&user.isActiveAndEnabled,"Desktop user unavailable");user.ShowDesktop();
                fixtureBound=true;return;
            }
            if(!measuring){
                if(now-warmingSince>20)throw new TimeoutException("Visible UI warmup failed for "+expectedApp);
                if(now-warmingSince>1&&!warmScreenshot&&ValidateVisible(false)){warmScreenshot=SaveDesktopScreenshot(Phases[phase]+"-visible-warmup.png");}
                if(phase==2&&warmScreenshot&&!tiebaWarmScreenshot){
                    if(expectedApp=="yy"&&now-warmingSince>1.4){var tieba=LingGuangV05.Desktop.Tieba.TiebaHub.Instance;Require(tieba?.View!=null,"Tieba warmup unavailable");tieba.View.Open("home");Expect(tieba.View,"tieba");}
                    if(expectedApp=="tieba"&&now-expectedSince>.6&&ValidateVisible(false)){tiebaWarmScreenshot=SaveDesktopScreenshot("tieba-visible-warmup.png");}
                    return;
                }
                if(now-warmingSince<WarmupSeconds||!warmScreenshot||!ValidateVisible(false))return;
                StartPhase(phase);measuring=true;skipFirstSample=true;frame=Time.frameCount;return;
            }
            if(now-started>=30) { ValidateVisible(true);EndPhase(); if(phase==2) { Finish(); return; } PreparePhase(phase+1); return; }
            if(now-expectedSince>.6)ValidateVisible(true);
            if(Time.frameCount!=frame) {
                frame=Time.frameCount;
                if(skipFirstSample)skipFirstSample=false;
                else {frameTimes.Add(Time.unscaledDeltaTime*1000f);
                    foreach(var c in counters) if(c.recorder.Valid) { long v=c.recorder.LastValue; c.sum+=v; c.metric.samples++; c.metric.max=Math.Max(c.metric.max,v); }
                }
            }
            if(now<nextAction) return;
            nextAction=now+(phase==0 ? .35 : phase==1 ? 1 : 1.5); actions++;
            if(phase==0) {
                int before=ctl.View.Teach.AnswerCount,teachingBefore=ctl.Sim.TeachingAnswers;
                ClickTeachingCorrect();
                Require(ctl.View.Teach.AnswerCount==before+1&&ctl.Sim.TeachingAnswers==teachingBefore+1,"Visible quiz action did not settle exactly one answer");
                completed++;
            } else if(phase==1) { if(ctl.Sim.BeginEpoch(XgTrack.Sequence,ctl.Host,false)) completed++; }
            else { if(actions%2==0) { Require(router!=null&&router.Open("yy","netbar"),"YY failed to open");Expect(YYChatHub.Instance.View,"yy");completed++; }
                else { var tieba=LingGuangV05.Desktop.Tieba.TiebaHub.Instance;Require(tieba?.View!=null,"Tieba unavailable");tieba.View.Open("home");Expect(tieba.View,"tieba");completed++; } }
        } catch(Exception e) { Fail(e); }
    }
    static void ClickTeachingCorrect()
    {
        // Read the actual on-screen quiz's statement, not the hidden classic card.
        // This workload invokes the real control callback; separate opening QA covers Aero pointer input.
        object desk=typeof(XgLabelPage).GetField("deskGame",Flags)?.GetValue(ctl.View.Teach);
        Require(desk!=null,"Teaching desk game unavailable");
        var deskPage=desk as LingGuangV05.Desktop.XingGuang.MiniGames.XgDeskGamePage;
        Require(deskPage!=null&&deskPage.root.gameObject.activeInHierarchy&&deskPage.Focused,"Quiz is not the visible focused teaching surface");
        object game=desk.GetType().GetField("game",Flags)?.GetValue(desk);
        Require(game!=null&&game.GetType().Name=="XgQuizGame","Expected actual common-sense quiz");
        var statement=game.GetType().GetField("statement",Flags)?.GetValue(game) as XgPhrase;
        Require(statement!=null,"Displayed statement missing");
        var choice=game.GetType().GetField(statement.yes?"yes":"no",Flags)?.GetValue(game) as XgBtn;
        Require(choice!=null&&choice.button.isActiveAndEnabled&&choice.button.gameObject.activeInHierarchy&&choice.button.IsInteractable(),"Displayed quiz answer control unavailable");
        choice.button.onClick.Invoke();
    }
    static bool SaveDesktopScreenshot(string name)
    {
        string path=Path.Combine(Root,name);
        // Existing helper reads the actual rendered LCD RenderTexture in batch mode.
        // A queued GameView ScreenCapture call is not proof that a PNG was written.
        if(!QaDesktopCapture.Save(path)||!File.Exists(path)||new FileInfo(path).Length<=100)return false;
        if(!screenshotFiles.Contains(path))screenshotFiles.Add(path);
        return true;
    }
    static void Require(bool test,string reason){if(!test)throw new InvalidOperationException(reason);}
    static void StopNarrativeDrivers()
    {
        foreach(var component in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
            string name=component.GetType().Name;
            if(name=="StoryDirector"||name=="StoryDesktopPresenter"||name=="PrologueDirector"||name=="SpiderIntroScene"||name=="LoveQuestionCutscene"||name=="StageOneTutor"){
                component.CancelInvoke();component.StopAllCoroutines();component.enabled=false;
            }
        }
    }
    static void PreparePhase(int n)
    {
        phase=n;measuring=false;screenshotFiles.Clear();warmScreenshot=false;tiebaWarmScreenshot=false;visibleChecks=0;expectedView=null;warmingSince=EditorApplication.timeSinceStartup;
        if(n<2){
            if(n==1){ctl.Sim.BuyNode("train.curve",ctl.Host);Require(ctl.Sim.TeachingTrainOpen,"Training page remains locked");}
            ctl.Open();ctl.View.Open(n==0?"teach":"train");ctl.View.Refresh(true);
            expectedApp=n==0?"teach":"train";expectedWindow=ctl.Window;
            expectedRoot=n==0?ctl.View.Teach.root:ctl.View.Train.root;expectedLabels=null;expectedSince=warmingSince;
        }else{Require(router.Open("yy","netbar"),"YY fixture could not open");Expect(YYChatHub.Instance.View,"yy");}
    }
    static void Expect(MonoBehaviour view,string app)
    {
        Require(view!=null,"View unavailable: "+app);expectedApp=app;expectedView=view;
        expectedWindow=view.GetType().GetField("window",Flags)?.GetValue(view) as LingGuangV05.Desktop.OS.WindowManager;
        expectedRoot=view.GetType().GetField("root",Flags)?.GetValue(view) as RectTransform;
        expectedLabels=null;expectedSince=EditorApplication.timeSinceStartup;
    }
    static bool ValidateVisible(bool fail)
    {
        if(expectedRoot==null&&expectedView!=null)expectedRoot=expectedView.GetType().GetField("root",Flags)?.GetValue(expectedView) as RectTransform;
        bool visible=expectedWindow!=null&&expectedWindow.isOn&&expectedWindow.isActiveAndEnabled
            &&expectedRoot!=null&&expectedRoot.gameObject.activeInHierarchy&&expectedRoot.rect.width>0&&expectedRoot.rect.height>0;
        if(visible&&(expectedApp=="teach"||expectedApp=="train"))visible=ctl.View.Visible&&ctl.View.Tab==expectedApp;
        if(visible&&expectedWindow.transform.parent!=null){
            var parent=expectedWindow.transform.parent;
            for(int i=parent.childCount-1;i>=0;i--){var child=parent.GetChild(i);if(!child.gameObject.activeInHierarchy)continue;
                var other=child.GetComponent<LingGuangV05.Desktop.OS.WindowManager>();if(other==null||!other.isOn)continue;visible=other==expectedWindow;break;}
        }
        if(visible){
            if(expectedLabels==null)expectedLabels=expectedRoot.GetComponentsInChildren<TMPro.TMP_Text>(true);
            bool glyph=false;
            foreach(var label in expectedLabels)if(label!=null&&label.isActiveAndEnabled&&!label.canvasRenderer.cull
                &&label.canvasRenderer.GetInheritedAlpha()>.05f&&label.textInfo!=null&&label.textInfo.characterCount>0){glyph=true;break;}
            visible=glyph;
        }
        if(!visible&&fail)throw new InvalidOperationException("Measured UI became invisible or has no rendered glyphs: "+expectedApp);
        if(visible)visibleChecks++;return visible;
    }
    static void StartPhase(int n)
    {
        phase=n; actions=completed=0; frame=-1; frameTimes.Clear();
        startsAnswers=ctl.View.Teach.AnswerCount; startsEpochs=ctl.Sim.S.epochs;
        var handles=new List<ProfilerRecorderHandle>(); ProfilerRecorderHandle.GetAvailable(handles);
        var available=new List<string>();
        foreach(var h in handles) {
            var d=ProfilerRecorderHandle.GetDescription(h); available.Add(d.Category.Name+" | "+d.Name+" | "+d.UnitType);
            string name=d.Name;
            if(!(name=="Main Thread" || name=="GC Allocated In Frame" || name.StartsWith("Canvas.") || name.IndexOf("Audio",StringComparison.OrdinalIgnoreCase)>=0 || name.IndexOf("DSP",StringComparison.OrdinalIgnoreCase)>=0 || name.Contains("BehaviourUpdate") || name.Contains("Xg") || name.Contains("YYChat") || name.Contains("Tieba"))) continue;
            try { var r=ProfilerRecorder.StartNew(d.Category,name,1); if(r.Valid) counters.Add(new Counter{recorder=r,metric=new Metric{name=name,unit=d.UnitType.ToString()}}); else r.Dispose(); } catch { }
        }
        File.WriteAllLines(Path.Combine(Root,Phases[n]+"-available-markers.txt"),available);
        Profiler.enabled=false; Profiler.enableBinaryLog=false; Profiler.logFile=Path.Combine(Root,Phases[n]+".raw"); Profiler.enableBinaryLog=true; Profiler.enabled=true;
        firstProfilerFrame = UnityEditorInternal.ProfilerDriver.lastFrameIndex + 1;
        started=EditorApplication.timeSinceStartup; nextAction=started+1;
        Debug.Log("Integration profiler started: "+Phases[n]+" -> "+Root);
    }
    static void EndPhase()
    {
        Require(completed>0,"No successful actions in measured "+Phases[phase]);
        if(phase==0)Require(completed==actions&&ctl.View.Teach.AnswerCount-startsAnswers==completed,"Teaching workload incomplete");
        Profiler.enabled=false; Profiler.enableBinaryLog=false;
        var r=new Report{scenario=Phases[phase], sourceCommit=Git("rev-parse HEAD"),dirtyFiles=Git("status --short"),unityVersion=Application.unityVersion,audioDriver=AudioSettings.driverCapabilities.ToString(),batchMode=Application.isBatchMode,audioListenerPaused=AudioListener.pause,audioSampleRate=AudioSettings.outputSampleRate,visibleChecks=visibleChecks,verifiedScreenshots=new List<string>(screenshotFiles),renderedFrames=frameTimes.Count,actionAttempts=actions,completedActions=completed,answersBefore=startsAnswers,answersAfter=ctl.View.Teach.AnswerCount,epochsBefore=startsEpochs,epochsAfter=ctl.Sim.S.epochs,wallSeconds=EditorApplication.timeSinceStartup-started};
        frameTimes.Sort(); double sum=0;foreach(float f in frameTimes)sum+=f;
        if(frameTimes.Count>0) {r.frameMeanMs=sum/frameTimes.Count;r.frameMaxMs=frameTimes[frameTimes.Count-1];r.frameP95Ms=frameTimes[Math.Min(frameTimes.Count-1,(int)(frameTimes.Count*.95))];}
        foreach(var c in counters){c.metric.mean=c.metric.samples>0?c.sum/c.metric.samples:0;r.metrics.Add(c.metric);c.recorder.Dispose();} counters.Clear();
        File.WriteAllText(Path.Combine(Root,Phases[phase]+".json"),JsonUtility.ToJson(r,true));
        SaveHotspots();
    }
    static void SaveHotspots()
    {
        try {
            var times = new Dictionary<string, double>();
            int frames = 0;
            int first = Math.Max(firstProfilerFrame, UnityEditorInternal.ProfilerDriver.firstFrameIndex);
            int last = UnityEditorInternal.ProfilerDriver.lastFrameIndex;
            for(int f=first; f<=last; f++) using(var data=UnityEditorInternal.ProfilerDriver.GetRawFrameDataView(f,0)) {
                if(!data.valid) continue; frames++;
                for(int i=0;i<data.sampleCount;i++) {
                    string name=data.GetSampleName(i);
                    double ms=data.GetSampleTimeMs(i);
                    if(ms<=0) continue;
                    times.TryGetValue(name,out double total); times[name]=total+ms;
                }
            }
            var rows=new List<KeyValuePair<string,double>>(times);rows.Sort((a,b)=>b.Value.CompareTo(a.Value));
            var lines=new List<string>{"Main-thread inclusive CPU markers; parent/child timings overlap. Editor ring-buffer range only, raw file is full capture.","Frames: "+frames+"; first: "+first+"; last: "+last,"Rank\tMarker\tMean milliseconds per buffered frame"};
            for(int i=0;i<Math.Min(10,rows.Count);i++)lines.Add((i+1)+"\t"+rows[i].Key+"\t"+(rows[i].Value/Math.Max(1,frames)).ToString("F4",System.Globalization.CultureInfo.InvariantCulture));
            lines.Add("All markers (inclusive):"); foreach(var row in rows) lines.Add(row.Key+"\t"+(row.Value/Math.Max(1,frames)).ToString("F4",System.Globalization.CultureInfo.InvariantCulture));
            File.WriteAllLines(Path.Combine(Root,Phases[phase]+"-cpu-hotspots.tsv"),lines);
        } catch(Exception e) {File.WriteAllText(Path.Combine(Root,Phases[phase]+"-cpu-hotspots-unavailable.txt"),e.ToString());}
    }
    static void Finish()
    {
        File.WriteAllText(Path.Combine(Root,"complete.txt"),"Three visibility-checked 30-second workload captures after separate3-second warmups complete. Earlier captures without visible-window assertions are not a valid visible-UI baseline. Inspect action/epoch counts and raw samples before using as evidence. Audio may be inactive in batch mode. No local model inference was measured.");
        finishing=true; EditorApplication.update-=Tick; SceneManager.sceneLoaded-=DisableModel; EditorApplication.ExitPlaymode();
    }
    static void Fail(Exception e)
    {
        Profiler.enabled=false; foreach(var c in counters)c.recorder.Dispose();counters.Clear();
        File.WriteAllText(Path.Combine(Root,"failure.txt"),e.ToString()); Debug.LogException(e); SessionState.EraseString(Key);EditorApplication.Exit(1);
    }
}
#endif



