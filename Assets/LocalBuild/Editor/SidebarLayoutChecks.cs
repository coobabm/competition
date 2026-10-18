#if UNITY_EDITOR
using System;
using System.Collections;
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
using UnityEngine.EventSystems;
using HongmengOS.Aero2010;

// -executeMethod SidebarLayoutChecks.Run, without -quit.
// This is a fully unlocked QA fixture, NOT evidence that a new player can buy every pack.
[InitializeOnLoad]
public static class SidebarLayoutChecks
{
    const string Key="SidebarLayoutChecks.Root";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static string Root=>SessionState.GetString(Key,"");
    static XingGuangController ctl;
    static ChapterOneRuntime runtime;
    static int scenario;static IDisposable locale;static string language="zh-CN";
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
    static IEnumerator crawlerCheck,ledgerCheck,filesCheck,relicCheck;
    const string FilesKey="SidebarLayoutChecks.FilesOnly";
    const string RelicsKey="SidebarLayoutChecks.RelicsOnly";
    const string ProfileKey="SidebarLayoutChecks.ProfileOnly";
    public static void RunFiles(){SessionState.SetBool(FilesKey,true);Run();}
    public static void RunRelics(){SessionState.SetBool(RelicsKey,true);Run();}
    public static void RunProfile(){SessionState.SetBool(ProfileKey,true);RunRelics();}
    static Report report;
    [Serializable] sealed class Report
    {
        public string scope="Isolated PlayMode fixtures: four explicit unlock states, 960x600 and 1400x900 windows, Chinese and English. Measures actual RectTransforms/TMP geometry and genuine LCD pointer routing for four permanent rows; explicit states are not natural progression.";
        public string status="running",failure="",unityVersion;
        public List<string> checks=new List<string>(),runtimeErrors=new List<string>(),editorInfrastructureErrors=new List<string>();
        public string desktopReadiness="";
        public List<string> screenshots=new List<string>();
    }
    static SidebarLayoutChecks(){EditorApplication.playModeStateChanged+=State;}
    [MenuItem("Tools/Local Verification/Exe sidebar layout")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Start in Edit Mode");
        string dir=Path.GetFullPath(Path.Combine("LocalVerification","SidebarLayout",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));
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
            index=-1;scenario=0;phase=frames=0;lastFrame=-1;finishing=false;timeout=EditorApplication.timeSinceStartup+180;
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
                foreach(var prologue in UnityEngine.Object.FindObjectsByType<LingGuangV05.Desktop.Story.PrologueDirector>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                {prologue.enabled=false;prologue.CancelInvoke();prologue.StopAllCoroutines();Require(!prologue.Running,"Isolated fixture prologue did not stop");}
                runtime.SetSimulationForTests(house);typeof(XingGuangController).GetMethod("BindSave",Flags).Invoke(ctl,new object[]{house});
                // This fixture exercises desktop games, not the title/login sequence. Complete the
                // isolated DreamOS profile's boot explicitly; an empty profile otherwise waits at login.
                var boot=UnityEngine.Object.FindAnyObjectByType<Michsky.DreamOS.BootManager>(FindObjectsInactive.Include);
                if(boot!=null){boot.CancelInvoke();boot.StopAllCoroutines();if(boot.bootAnimator!=null)boot.bootAnimator.gameObject.SetActive(false);}
                var user=UnityEngine.Object.FindAnyObjectByType<Michsky.DreamOS.UserManager>(FindObjectsInactive.Include);
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
                ctl.Open();ctl.View.Open("train");train=ctl.View.Train;originalStoryProbe=StoryFocus.Probe;
                if(SessionState.GetBool(RelicsKey,false))
                {
                    locale=GameText.Bind(()=>language);
                    report.scope="Isolated Main equipment and profile UI, real LCD input and host purchases; explicit unlock fixtures, not natural progression.";
                    relicCheck=RelicExpansionUiChecks.Frames(ctl,
                        en=>{language=en?GameText.English:"zh-CN";GameText.PublishChanged();}, ClickSidebar,
                        filename=>{Check(QaDesktopCapture.Save(Path.Combine(Root,filename)),"relic LCD capture "+filename);report.screenshots.Add(filename);Save();},SessionState.GetBool(ProfileKey,false));
                    phase=9;frames=0;return;
                }
                if(SessionState.GetBool(FilesKey,false))
                {
                    locale=GameText.Bind(()=>language);
                    report.scope="Isolated Main data-import UI: official real-host purchase, actual LCD input/captures; notebook/data rows are explicit fixture records, not natural progression. Ctrl key automation not yet covered.";
                    filesCheck=FilesWindowUiChecks.Frames(ctl,
                        en=>{language=en?GameText.English:"zh-CN";GameText.PublishChanged();},
                        filename=>{Check(QaDesktopCapture.Save(Path.Combine(Root,filename)),"files LCD capture "+filename);report.screenshots.Add(filename);Save();},
                        name=>{report.checks.Add(name);Save();});
                    phase=8;frames=0;return;
                }
                phase=1;frames=0;return;
            }
            if(phase==9)
            {
                if(relicCheck.MoveNext())return;
                (relicCheck as IDisposable)?.Dispose();relicCheck=null;
                report.checks.Add("Relic expansion actual LCD route, purchase, replacement and stale-save checks passed");
                report.status="passed";Save();File.WriteAllText(Path.Combine(Root,"passed.txt"),"Relic UI checks passed; screenshots require review");Finish();return;
            }
            if(phase==8)
            {
                if(filesCheck.MoveNext())return;
                (filesCheck as IDisposable)?.Dispose();filesCheck=null;
                report.status="passed";Save();File.WriteAllText(Path.Combine(Root,"passed.txt"),"Files UI covered checks passed; see report scope for remaining coverage");Finish();return;
            }
            if(phase==1)
            {
                if(scenario>=16){ctl.View.Open("train");crawlerCheck=CrawlerVisibleMeshChecks.Frames(ctl);phase=6;return;}
                int progress=scenario%4;bool large=(scenario/4)%2!=0,english=scenario>=8;
                if(locale==null)locale=GameText.Bind(()=>language);language=english?GameText.English:"zh-CN";GameText.PublishChanged();
                ctl.Sim.S.teachingFlow=true;ctl.Sim.S.handCorrect=progress>=1?12:0;ctl.Sim.S.handWrong=0;ctl.Sim.S.chatTurns=progress>=3?1:0;ctl.Sim.S.spiderIntroDone=true;
                ctl.Sim.S.unlocked.Remove("train.curve");if(progress>=2)ctl.Sim.S.unlocked.Add("train.curve");
                foreach(string node in new[]{"train.holdout","shared.lr","train.checkpoint","train.monitor","train.weights"})
                {ctl.Sim.S.unlocked.Remove(node);if(progress>=3)ctl.Sim.S.unlocked.Add(node);}
                var frame=ctl.Window.windowContainer;frame.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,large?1400:960);frame.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,large?900:600);
                ctl.View.Open("teach");ctl.View.Refresh(true);frames=0;phase=2;return;
            }
            if(phase==2)
            {
                if(++frames<4)return;Canvas.ForceUpdateCanvases();
                if(scenario==0)
                {
                    TeachingLanguageChecks.Run(Field<LingGuangV05.Desktop.XingGuang.MiniGames.XgDeskGamePage>(ctl.View.Teach,"deskGame"), english=>language=english?GameText.English:"zh-CN");
                    report.checks.Add("Language switch preserves the current question, timer, answer count and streak");
                }
                var nav=Field<RectTransform>(ctl.View,"nav");var menu=Field<RectTransform>(ctl.View,"navMenu");var list=new List<RectTransform>();
                foreach(var id in XingGuangView.NavIds)
                {
                    var row=ctl.View.NavButton(id);Check(row.gameObject.activeInHierarchy,"fixture "+scenario+" permanent row "+id);bool open=ctl.View.FeatureOpen(id);Check(row.GetComponent<Button>().interactable==open,"fixture "+scenario+" functional gate "+id);
                    list.Add(row);
                    var icon=row.Find("Icon "+id)?.GetComponent<Image>();Check(icon!=null&&icon.sprite!=null&&icon.sprite.texture.name.StartsWith("Original navigation"),"fixture "+scenario+" original icon "+id);
                    Check(icon.gameObject.activeSelf==open && row.Find("LockIcon").gameObject.activeSelf==!open,"fixture "+scenario+" actual lock/icon state "+id);
                    Check(Mathf.Abs(icon.rectTransform.rect.width-50)<.1f&&Mathf.Abs(icon.rectTransform.rect.height-50)<.1f,"fixture "+scenario+" fifty pixel icon "+id);
                    var hint=row.Find("LockedHint").GetComponent<TMPro.TMP_Text>();
                    Check(hint.gameObject.activeSelf==!open,"fixture "+scenario+" locked explanation presence "+id);
                    if(!open){hint.ForceMeshUpdate(true,true);Check(!string.IsNullOrEmpty(hint.text)&&!hint.isTextOverflowing,"fixture "+scenario+" readable lock reason "+id);}
                    var label=row.Find("Label").GetComponent<TMPro.TMP_Text>();label.ForceMeshUpdate(true,true);
                    var preferred=label.GetPreferredValues(label.text);Check(preferred.y<=label.rectTransform.rect.height+.5f,"fixture "+scenario+" label height "+id);
                    if(id!="home")Check(preferred.x<=label.rectTransform.rect.width+.5f,"fixture "+scenario+" bilingual label width "+id);
                    var corners=new Vector3[4];row.GetWorldCorners(corners);foreach(var c in corners){var q=menu.InverseTransformPoint(c);Check(q.x>=menu.rect.xMin-.5f&&q.x<=menu.rect.xMax+.5f&&q.y>=menu.rect.yMin-.5f&&q.y<=menu.rect.yMax+.5f,"fixture "+scenario+" row contained");}
                }
                Check(list.Count==4,"fixture "+scenario+" all four rows remain present");
                Check(Mathf.Abs(nav.rect.width-112)<.1f,"fixture "+scenario+" exact 112 logical pixel rail");
                Check(Field<RectTransform>(ctl.View,"root").Find("Grid")==null,"fixture "+scenario+" no obsolete scanner grid");
                float spacing=0;for(int i=1;i<list.Count;i++){Check(Mathf.Abs(list[i].rect.height-list[0].rect.height)<.1f&&Mathf.Abs(list[i].rect.width-list[0].rect.width)<.1f,"fixture "+scenario+" equal button sizes");float d=list[i-1].anchoredPosition.y-list[i].anchoredPosition.y;
                    var a=menu.InverseTransformPoint(list[i-1].TransformPoint(list[i-1].rect.center));var b=menu.InverseTransformPoint(list[i].TransformPoint(list[i].rect.center));d=a.y-b.y;if(i==1)spacing=d;else Check(Mathf.Abs(d-spacing)<.1f,"fixture "+scenario+" equal spacing");Check(d>list[i].rect.height,"fixture "+scenario+" positive gap");}
                var bottom=menu.TransformPoint(new Vector3(0,menu.rect.yMin,0));foreach(string field in new[]{"crowdLink","soundToggle","fxToggle","residentToggle","residentRecall"}){object value=typeof(XingGuangView).GetField(field,Flags).GetValue(ctl.View);RectTransform r=value is RectTransform rect?rect:((XgBtn)value).rt;if(!r.gameObject.activeSelf)continue;var top=r.TransformPoint(new Vector3(0,r.rect.yMax,0));Check(nav.InverseTransformPoint(bottom).y>=nav.InverseTransformPoint(top).y+3,"fixture "+scenario+" footer separation "+field);}
                foreach(string field in new[]{"soundToggle","fxToggle","residentToggle","residentRecall"})
                {var control=Field<XgBtn>(ctl.View,field);if(!control.rt.gameObject.activeSelf)continue;control.label.ForceMeshUpdate(true,true);var size=control.label.GetPreferredValues(control.label.text);Check(size.x<=control.label.rectTransform.rect.width+.5f&&size.y<=control.label.rectTransform.rect.height+.5f,"fixture "+scenario+" footer text fits "+field);}
                foreach(string id in XingGuangView.NavIds)
                {
                    ctl.View.Open("teach");Canvas.ForceUpdateCanvases();bool open=ctl.View.FeatureOpen(id);string before=ctl.View.Tab;
                    ClickSidebar(ctl.View.NavButton(id));
                    Check(ctl.View.Tab==(open?id:before),"fixture "+scenario+" actual pointer route/gate "+id);
                    if(open){var expected=id=="teach"?ctl.View.Teach.root:id=="train"?ctl.View.Train.root:id=="tree"?ctl.View.Tree.root:ctl.View.Home.root;Check(expected.gameObject.activeInHierarchy,"fixture "+scenario+" intended page actually visible "+id);}
                }
                ctl.View.Open("teach");Canvas.ForceUpdateCanvases();
                bool muted=ctl.Sim.S.mute;ClickSidebar(Field<XgBtn>(ctl.View,"soundToggle").rt);Check(ctl.Sim.S.mute!=muted,"fixture "+scenario+" sound control changes real setting");ClickSidebar(Field<XgBtn>(ctl.View,"soundToggle").rt);
                bool reduced=ctl.Sim.S.reduceFx;ClickSidebar(Field<XgBtn>(ctl.View,"fxToggle").rt);Check(ctl.Sim.S.reduceFx!=reduced,"fixture "+scenario+" FX control changes real setting");ClickSidebar(Field<XgBtn>(ctl.View,"fxToggle").rt);
                if(ctl.Sim.SpiderAround){bool quiet=ctl.Sim.S.residentQuiet;ClickSidebar(Field<XgBtn>(ctl.View,"residentToggle").rt);Check(ctl.Sim.S.residentQuiet!=quiet,"fixture "+scenario+" existing spider rest is functional");ClickSidebar(Field<XgBtn>(ctl.View,"residentToggle").rt);}
                frames=0;phase=3;return;
            }
            if(phase==3)
            {
                // Tick is gated by Time.frameCount: allow two actual rendered frames after
                // the final pointer interaction before reading the desktop LCD texture.
                if(++frames<2)return;
                if(scenario==0||scenario==7||scenario==8||scenario==15)
                {
                    string filename="sidebar-"+scenario.ToString("D2")+"-"+(scenario>=8?"en":"zh")+"-"+((scenario/4)%2!=0?"1400x900":"960x600")+".png";
                    string path=Path.Combine(Root,filename);
                    Check(QaDesktopCapture.Save(path)&&File.Exists(path)&&new FileInfo(path).Length>100,"fixture "+scenario+" actual LCD screenshot saved");
                    report.screenshots.Add(filename);Save();
                }
                if(ctl.View.FeatureOpen("train")){ctl.View.Open("train");ctl.View.Refresh(true);frames=0;phase=4;return;}
                scenario++;phase=1;return;
            }
            if(phase==4)
            {
                if(++frames<4)return;Canvas.ForceUpdateCanvases();
                if(scenario==3)
                {
                    string priorLanguage=language;
                    try{RateStripLabelChecks.Run(ctl.View.Train,ctl.Sim,english=>{language=english?GameText.English:"zh-CN";ctl.Sim.English=english;GameText.PublishChanged();});}
                    finally{language=priorLanguage;ctl.Sim.English=language==GameText.English;GameText.PublishChanged();ctl.View.Refresh(true);}
                }
                string filename="training-modules-"+scenario.ToString("D2")+".png";
                Check(QaDesktopCapture.Save(Path.Combine(Root,filename)),"training modules screenshot "+scenario);report.screenshots.Add(filename);
                TrainingSealChecks.AssertLayout(ctl.View);
                foreach(var seal in Field<XgTrainingSeal[]>(ctl.View.Train,"moduleSeals"))
                {
                    if(seal.NodeId!="shared.lr"||!seal.gameObject.activeInHierarchy)continue;
                    double beforeMoney=runtime.Sim.S.money;
                    ClickSidebar(seal.Control.rt);
                    Check(ctl.View.Tab=="tree","sealed module actual LCD click opens technology "+scenario);
                    var detail=typeof(XgTechTreePage).GetField("detailShown",Flags).GetValue(ctl.View.Tree);
                    Check(detail!=null&&(bool)detail.GetType().GetMethod("Covers",Flags).Invoke(detail,new object[]{"shared.lr"}),"sealed module opens exact details "+scenario);
                    Check(runtime.Sim.S.money==beforeMoney&&!ctl.Sim.Has("shared.lr"),"sealed module navigation does not buy "+scenario);
                }
                TrainingSealChecks.AssertDetails(ctl.View);
                scenario++;phase=1;return;
            }
            if(phase==6)
            {
                if(crawlerCheck.MoveNext())return;
                (crawlerCheck as IDisposable)?.Dispose();crawlerCheck=null;
                ledgerCheck=LedgerUiChecks.Frames(ctl,
                    en=>{language=en?GameText.English:"zh-CN";GameText.PublishChanged();},
                    ClickSidebar,
                    filename=>{Check(QaDesktopCapture.Save(Path.Combine(Root,filename)),"ledger LCD capture "+filename);report.screenshots.Add(filename);Save();});
                phase=7;return;
            }
            if(phase==7)
            {
                if(ledgerCheck.MoveNext())return;
                (ledgerCheck as IDisposable)?.Dispose();ledgerCheck=null;
                report.status="passed";Save();File.WriteAllText(Path.Combine(Root,"passed.txt"),"All 16 sidebar fixtures, crawler lifecycle and ledger UI passed");Finish();return;
            }
        }
        catch(Exception e){report.status="failed";report.failure=e.ToString();Save();Debug.LogError("Training lesson check failed: "+e);Finish();}
    }
    static void ClickSidebar(RectTransform target)
    {
        var bridge=UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>();Require(bridge!=null&&bridge.inputSurface!=null,"LCD input bridge missing");
        var surface=bridge.inputSurface;var source=bridge.sourceCamera.WorldToScreenPoint(target.TransformPoint(target.rect.center));
        var display=surface.displayRect;var r=display.rect;var texture=bridge.sourceCamera.targetTexture;
        float u=source.x/texture.width,v=source.y/texture.height;if(surface.flipY)v=1-v;
        var camera=(Camera)typeof(AeroLcdInputSurface).GetMethod("DisplayEventCamera",Flags).Invoke(surface,null);
        var screen=RectTransformUtility.WorldToScreenPoint(camera,display.TransformPoint(new Vector3(r.xMin+u*r.width,r.yMin+v*r.height,0)));
        Require(surface.TryMap(screen,out var roundtrip)&&Vector2.Distance(roundtrip,new Vector2(source.x,source.y))<1,"LCD pointer roundtrip failed");
        Require(surface.inputEnabled&&surface.isActiveAndEnabled,"LCD input disabled");
        var pointer=new PointerEventData(EventSystem.current){position=screen,button=PointerEventData.InputButton.Left};surface.OnPointerEnter(pointer);
        var routed=Field<PointerEventData>(surface,"hoverData");var hit=routed?.pointerCurrentRaycast.gameObject;
        string hitPath=hit==null?"none":UnityEditor.AnimationUtility.CalculateTransformPath(hit.transform,null);
        Require(hit!=null&&ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit)==target.gameObject&&ExecuteEvents.GetEventHandler<IPointerDownHandler>(hit)==target.gameObject,"Sidebar pointer intercepted: "+target.name+" by "+hitPath);
        surface.OnPointerDown(pointer);surface.OnPointerUp(pointer);
    }
    // QA keeps the isolated save, but real purchases/rewards require the production host.
    // Stop only unrelated narrative drivers; neither the host nor its balance methods are replaced.
    static void EnableFixtureEconomy(ChapterOneRuntime rt)
    {
        foreach(var component in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            string name=component.GetType().Name;
            if(name=="StoryDirector"||name=="StoryDesktopPresenter"||name=="PrologueDirector"||name=="SpiderIntroScene"||name=="LoveQuestionCutscene"||name=="StageOneTutor"||name=="LaoZhouYY")
            {component.CancelInvoke();component.StopAllCoroutines();component.enabled=false;}
        }
        rt.useDiskSave=false;rt.tickEnabled=false;
        typeof(ChapterOneRuntime).GetProperty("TestMode").GetSetMethod(true).Invoke(rt,new object[]{false});
        if(rt.TestMode||rt.useDiskSave)throw new InvalidOperationException("Live host fixture isolation failed");
    }
    static void Finish(){(relicCheck as IDisposable)?.Dispose();relicCheck=null;SessionState.EraseBool(RelicsKey);SessionState.EraseBool(ProfileKey);(filesCheck as IDisposable)?.Dispose();filesCheck=null;SessionState.EraseBool(FilesKey);(ledgerCheck as IDisposable)?.Dispose();ledgerCheck=null;(crawlerCheck as IDisposable)?.Dispose();crawlerCheck=null;locale?.Dispose();locale=null;StoryFocus.Probe=originalStoryProbe;if(emergenceQueue!=null)emergenceQueue.Clear();finishing=true;Application.logMessageReceived-=Log;EditorApplication.update-=Tick;EditorApplication.ExitPlaymode();}
}
#endif
