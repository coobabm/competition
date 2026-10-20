#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LingGuangV05.Core.Games;
using LingGuangV05.Desktop.Zhongbao;
using LingGuangV05.Runtime.Persistence;
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

// -executeMethod SelectionIsolationInteractionChecks.Run, without -quit.
// This is a fully unlocked QA fixture, NOT evidence that a new player can buy every pack.
[InitializeOnLoad]
public static class SelectionIsolationInteractionChecks
{
    const string Key="SelectionIsolationInteractionChecks.Root";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static string Root=>SessionState.GetString(Key,"");
    static XingGuangController ctl;
    static ChapterOneRuntime runtime;
    static XgDeskGamePage desk;
    static int index=-1,phase,lastFrame=-1,frames;
    static double timeout;
    static bool finishing;
    static Report report;
    [Serializable] sealed class Entry { public string id,gameType,clicked="",note="";public bool built,visible;public int texts,images,buttons,answersBefore,answersAfter; }
    [Serializable] sealed class Report
    {
        public string scope="Actual Main teach/company selection isolation, pointer submission and background focus gate; fully unlocked isolated QA fixture.";
        public string status="running",failure="",unityVersion;
        public List<Entry> packs=new List<Entry>();public List<string> runtimeErrors=new List<string>();
        public List<string> editorInfrastructureErrors=new List<string>();
        public string desktopReadiness="";public List<string> checks=new List<string>();
    }
    static SelectionIsolationInteractionChecks(){EditorApplication.playModeStateChanged+=State;}
    [MenuItem("Tools/Local Verification/Selection isolation and focus")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Start in Edit Mode");
        string dir=Path.GetFullPath(Path.Combine("LocalVerification","SelectionIsolation",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));
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
            index=-1;phase=frames=0;lastFrame=-1;finishing=false;timeout=EditorApplication.timeSinceStartup+300;
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
                // Bind a post-prologue desktop only after canceling any coroutine started on the initial new save.
                // TestMode makes PrologueDirector.Update return early; it does not stop an already running Run().
                foreach(var prologue in UnityEngine.Object.FindObjectsByType<LingGuangV05.Desktop.Story.PrologueDirector>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                {
                    report.checks.Add("Fixture prologue isolation before TestMode: running="+prologue.Running+", step="+prologue.Step);
                    prologue.enabled=false; // OnDisable calls production StopPrologue, cleans owned Boot/Title and releases input.
                    prologue.CancelInvoke();prologue.StopAllCoroutines();
                    Require(!prologue.Running,"Prologue failed to stop during isolated fixture setup");
                }
                runtime.SetSimulationForTests(house);typeof(XingGuangController).GetMethod("BindSave",Flags).Invoke(ctl,new object[]{house});
                // This fixture exercises desktop games, not the title/login sequence. Complete the
                // isolated DreamOS profile's boot explicitly; an empty profile otherwise waits at login.
                var boot=UnityEngine.Object.FindAnyObjectByType<LingGuangV05.Desktop.OS.BootManager>(FindObjectsInactive.Include);
                if(boot!=null){boot.CancelInvoke();boot.StopAllCoroutines();if(boot.bootAnimator!=null)boot.bootAnimator.gameObject.SetActive(false);}
                var user=UnityEngine.Object.FindAnyObjectByType<LingGuangV05.Desktop.OS.UserManager>(FindObjectsInactive.Include);
                Require(user!=null&&user.isActiveAndEnabled,"Desktop user manager unavailable");
                user.ShowDesktop();
                File.WriteAllText(Path.Combine(Root,"fixture-lab.json"),JsonUtility.ToJson(lab.S,true));
                Require(XgTeachingPacks.All.Length==20,"Expected manifest's 20 packs");frames=0;phase=10;return;
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
                if(++frames<4)return;
                var hub=ZhongbaoHub.Instance;if(hub==null||hub.View==null||!hub.View.EnsureReady())return;
                CheckHiddenSurfaceLifecycle();
                Require(ctl.Sim.SelectTeachingPack("headline_swipe"),"Headline fixture pack unavailable");
                ctl.View.Teach.Refresh();desk=(XgDeskGamePage)Field(ctl.View.Teach,"deskGame");
                Require(desk.SelectedGame=="headline_swipe"&&desk.Playing,"Teach pack selection not immediately reflected");
                AssertSingleSelection(ctl.View.Teach,"headline_swipe");
                Require(runtime.GetComponent<ChapterOneDesktopRouter>().Open(ZhongbaoHub.AppId,"label"),"Cannot open real crowd window");
                frames=0;phase=2;return;
            }
            if(phase==2)
            {
                if(++frames<4)return;
                var crowd=ZhongbaoHub.Instance.View;var work=crowd.Label;
                Require(!desk.Focused,"Crowd window did not take focus; test cannot assert background gate");
                // Switching a visible background page must update its surface immediately, while input remains gated.
                Require(ctl.Sim.SelectTeachingPack("spam_swipe"),"SMS fixture pack unavailable");
                ctl.View.Teach.Refresh();Require(desk.SelectedGame=="spam_swipe"&&desk.Playing,"Unfocused teach page retained stale game");
                Require(!desk.Focused,"Refresh stole input focus");
                var before=JsonUtility.ToJson(ctl.Sim.S);
                typeof(XgDeskGamePage).GetMethod("Settle",Flags).Invoke(desk,new object[]{true,1.0});
                Require(JsonUtility.ToJson(ctl.Sim.S)==before,"Unfocused game settled an answer");
                report.checks.Add("Background selection refresh is immediate; actual Settle rejects unfocused input");
                // Real hall callback: work MNIST while the private lesson still points to SMS.
                Press(work.root,"Desk mnist");AssertWork(work,"mnist","spam_swipe");
                ctl.Sim.SelectTeachingPack("mnist_write");ctl.View.Teach.Refresh();
                Press(work.root,"Desk headline");AssertWork(work,"headline","mnist_write");
                frames=0;phase=3;return;
            }
            if(phase==3)
            {
                if(++frames<2)return;
                var work=ZhongbaoHub.Instance.View.Label;var sim=ctl.Sim;
                int submitted=sim.S.workSubmitted,correct=sim.S.workCorrect,hand=sim.S.handCorrect+sim.S.handWrong;
                string lessons=JsonUtility.ToJson(new Notes{items=sim.S.teachingNotebook});
                string minis=JsonUtility.ToJson(new Minis{items=sim.S.miniGames});
                double samples=sim.Samples("headline");
                var card=sim.WorkCard("headline");var button=(XgBtn)Field(work,card.truth?"yes":"no");
                var input=button.button.GetComponent<XgLessonAnswerInput>();
                Require(input!=null&&button.button.IsInteractable(),"Work answer input unavailable");
                var data=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){button=UnityEngine.EventSystems.PointerEventData.InputButton.Left,pointerId=71};
                input.OnPointerDown(data);input.OnPointerClick(data);
                Require(sim.S.workSubmitted==submitted+1&&sim.S.workCorrect==correct+1,"Real company answer did not settle");
                Require(sim.S.handCorrect+sim.S.handWrong==hand&&sim.Samples("headline")==samples,"Work answer changed private teaching counts/samples");
                Require(JsonUtility.ToJson(new Notes{items=sim.S.teachingNotebook})==lessons&&JsonUtility.ToJson(new Minis{items=sim.S.miniGames})==minis,"Work answer entered teaching record/game path");
                Require(sim.S.teachingPack=="mnist_write"&&sim.S.desk=="mnist","Work answer changed lesson selection");
                report.checks.Add("Company real pointer answer increments work counters only, keeps lesson selection/history/mini records/samples");
                ctl.Open();ctl.View.Open("teach");frames=0;phase=4;return;
            }
            if(phase==4)
            {
                if(++frames<3)return;
                var teach=ctl.View.Teach;teach.Refresh();
                Require(desk.SelectedGame=="mnist_write","Restored teach window shows wrong game");
                Require(desk.Focused,"Restored foreground game did not recover input focus");
                AssertSingleSelection(teach,"mnist_write");
                Press(teach.root,"Desk sense");
                Require(ctl.Sim.S.desk=="sense"&&string.IsNullOrEmpty(ctl.Sim.S.teachingPack),"Base desk did not clear pack");
                AssertBaseDeskSurface(teach,"sense");
                Require(ctl.Sim.S.workDesk=="headline","Teach selection changed company desk");
                report.checks.Add("Real base-desk callback updates game immediately and preserves independent company selection");
                PrepareClassicTheme(teach,"sense",false);frames=0;phase=5;return;
            }
            if(phase==5)
            {
                if(++frames<5)return; // distinct rendered frames, not EditorApplication update iterations
                CaptureClassicTheme(ctl.View.Teach,false,false,"classic-text");
                PrepareClassicTheme(ctl.View.Teach,"mnist",false);frames=0;phase=6;return;
            }
            if(phase==6)
            {
                if(++frames<5)return;
                CaptureClassicTheme(ctl.View.Teach,false,true,"classic-digit-paper");
                Require(runtime.GetComponent<ChapterOneDesktopRouter>().Open(ZhongbaoHub.AppId,"label"),"Cannot open crowd theme control");
                frames=0;phase=7;return;
            }
            if(phase==7)
            {
                if(++frames<5)return;
                PrepareClassicTheme(ZhongbaoHub.Instance.View.Label,"headline",true);frames=0;phase=8;return;
            }
            if(phase==8)
            {
                if(++frames<5)return;
                CaptureClassicTheme(ZhongbaoHub.Instance.View.Label,true,false,"crowd-light-control");
                ctl.Open();ctl.View.Open("teach");footerSize=0;PrepareFooterSize();frames=0;phase=9;return;
            }
            if(phase==9)
            {
                if(++frames<5)return;
                SubmitFooterAnswer(true);frames=0;phase=11;return;
            }
            if(phase==11)
            {
                if(++frames<5)return;
                CaptureFooter("correct");SubmitFooterAnswer(false);frames=0;phase=12;return;
            }
            if(phase==12)
            {
                if(++frames<5)return;
                CaptureFooter("wrong");
                if(++footerSize<2){PrepareFooterSize();frames=0;phase=9;return;}
                report.status="passed";Save();File.WriteAllText(Path.Combine(Root,"passed.txt"),"Selection/context/focus, classic theme and footer layout checks passed");Finish();return;
            }
        }
        catch(Exception e){report.status="failed";report.failure=e.ToString();Save();Debug.LogError("Selection isolation check failed: "+e);Finish();}
    }
    static void AssertDesktopUnobscured(XgLabelPage page)
    {
        Require(runtime.TestMode&&!runtime.Sim.InPrologue,"Screenshot fixture is not a post-prologue desktop");
        foreach(var prologue in UnityEngine.Object.FindObjectsByType<LingGuangV05.Desktop.Story.PrologueDirector>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            Require(!prologue.Running,"Screenshot covered by running prologue at "+prologue.Step);
        foreach(var boot in UnityEngine.Object.FindObjectsByType<LingGuangV05.Desktop.OS.BootManager>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            Require(boot.bootAnimator==null||!boot.bootAnimator.gameObject.activeInHierarchy,"DreamOS boot overlay is visible");
        // These full-desktop blockers are outside WindowManager siblings, which the old focus check did not see.
        foreach(var image in UnityEngine.Object.FindObjectsByType<Image>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if(image.name!="Boot"&&image.name!="Shutdown"&&image.name!="Boot Fade Animation (Temp)"&&image.name!="Title")continue;
            if(image.name=="Title"&&(image.transform.parent==null||image.transform.parent.name!="LingGuang Prologue Screen"))continue;
            float alpha=image.color.a*image.canvasRenderer.GetAlpha();
            foreach(var group in image.GetComponentsInParent<CanvasGroup>())alpha*=group.alpha;
            Require(!image.enabled||alpha<.01f,"Visible fullscreen blocker: "+image.name+" alpha="+alpha);
        }
        float pageAlpha=1;
        foreach(var group in page.root.GetComponentsInParent<CanvasGroup>())pageAlpha*=group.alpha;
        Require(page.root.gameObject.activeInHierarchy&&pageAlpha>.95f,"Page has not finished fading in: alpha="+pageAlpha);
        var window=((XgUi)Field(page,"ui")).window;
        Require(window!=null&&window.isOn&&window.gameObject.activeInHierarchy,"Screenshot window closed");
        for(int i=window.transform.GetSiblingIndex()+1;i<window.transform.parent.childCount;i++)
        {
            var other=window.transform.parent.GetChild(i).GetComponent<LingGuangV05.Desktop.OS.WindowManager>();
            Require(other==null||!other.isOn||!other.gameObject.activeInHierarchy,"Screenshot window covered by "+(other==null?"":other.name));
        }
    }
    static int footerSize;
    static void PrepareFooterSize()
    {
        var frame=ctl.Window.windowContainer;
        frame.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,footerSize==0?960:1400);
        frame.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,footerSize==0?600:900);
        PrepareClassicTheme(ctl.View.Teach,"sense",false);
    }
    static void SubmitFooterAnswer(bool correct)
    {
        var page=ctl.View.Teach;var sim=ctl.Sim;
        var card=(XgCard)typeof(XgLabelPage).GetProperty("CurrentCard",Flags).GetValue(page);
        Require(XgSim.DirectTruthJudgment(card),"Footer fixture must judge the current statement, not AI opinion");
        bool answer=correct?card.truth:!card.truth;
        var button=(XgBtn)Field(page,answer?"yes":"no");
        var input=button.button.GetComponent<XgLessonAnswerInput>();
        Require(input!=null&&button.button.IsInteractable(),"Footer answer input unavailable");
        int right=sim.S.handCorrect,wrong=sim.S.handWrong;
        var data=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){button=UnityEngine.EventSystems.PointerEventData.InputButton.Left,pointerId=82};
        input.OnPointerDown(data);input.OnPointerClick(data);
        Require(sim.S.handCorrect==right+(correct?1:0)&&sim.S.handWrong==wrong+(correct?0:1),"Footer pointer answer did not settle exactly once");
    }
    static Rect FooterRect(RectTransform rt,RectTransform local)
    {
        var corners=new Vector3[4];rt.GetWorldCorners(corners);
        Vector2 min=local.InverseTransformPoint(corners[0]),max=min;
        for(int i=1;i<4;i++){Vector2 p=local.InverseTransformPoint(corners[i]);min=Vector2.Min(min,p);max=Vector2.Max(max,p);}
        return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
    }
    static void CaptureFooter(string result)
    {
        var page=ctl.View.Teach;AssertDesktopUnobscured(page);var body=(RectTransform)Field(page,"body");
        var toggle=(XgBtn)Field(page,"gameToggle");var frame=(RectTransform)toggle.rt.parent;
        var feedback=(TMPro.TMP_Text)Field(page,"feedback");var steps=(TMPro.TMP_Text)Field(page,"steps");
        Require(frame.gameObject.activeInHierarchy&&!string.IsNullOrWhiteSpace(feedback.text),"Footer or actual answer feedback hidden");
        var buttonRect=FooterRect(frame,body);var feedbackRect=FooterRect(feedback.rectTransform,body);var stepsRect=FooterRect(steps.rectTransform,body);
        Require(!buttonRect.Overlaps(feedbackRect)&&!buttonRect.Overlaps(stepsRect),"Return button overlaps feedback/info at size "+footerSize);
        Require(!feedback.text.Contains("#2F9E44")&&!feedback.text.Contains("#D63031")&&!feedback.text.Contains("#0F8C7E"),"Light-only rich feedback colour leaked into dark mode");
        feedback.ForceMeshUpdate();steps.ForceMeshUpdate();
        Require(feedback.textInfo.characterCount>0,"Actual answer feedback has no glyphs");
        string name="classic-footer-"+(footerSize==0?"960x600":"1400x900")+"-"+result;
        Require(QaDesktopCapture.Save(Path.Combine(Root,name+".png")),"Footer LCD capture failed");
        report.checks.Add(name+": real pointer answer, five rendered frames, button/feedback/info rectangles disjoint, dark rich colours");Save();
    }
    static long themeCardId;
    static int themeAnswers, themeCombo, themeWork;
    static void PrepareClassicTheme(XgLabelPage page,string id,bool work)
    {
        // The modern teaching hall exposes digits through its purchased pack, not a duplicate base row.
        Press(page.root,!work&&id=="mnist" ? "TeachingPack mnist_write" : "Desk "+id);
        if(!work)
        {
            var native=(XgDeskGamePage)Field(page,"deskGame");
            if(native.Playing)
            {
                var classic=(XgBtn)Field(native,"classic");
                Require(classic.button.IsInteractable(),"Classic theme fixture button unavailable: "+id);
                classic.button.onClick.Invoke();
            }
            Require(!native.Playing&&!native.root.gameObject.activeSelf,"Classic theme fixture still covered by native game");
        }
        // Only the isolated fixture removes transient gold/story variants. Keep original question/id/answer.
        var card=(XgCard)typeof(XgLabelPage).GetProperty("CurrentCard",Flags).GetValue(page);
        card.gold=false;card.kind="";card.timeLimit=0;card.age=0;
        card.patternA="";card.patternB="";card.sourceText="";card.candidateText="";
        typeof(XgLabelPage).GetField("redTimer",Flags).SetValue(page,0f);
        page.Refresh();
        themeCardId=card.id;themeAnswers=ctl.Sim.TeachingAnswers;themeCombo=ctl.Sim.S.combo;themeWork=ctl.Sim.S.workSubmitted;
        Require(((RectTransform)Field(page,"workspace")).gameObject.activeInHierarchy,"Classic workspace hidden");
    }
    static void CaptureClassicTheme(XgLabelPage page,bool work,bool visual,string name)
    {
        AssertDesktopUnobscured(page);
        Require(page.root.gameObject.activeInHierarchy,"Capture page not visible: "+name);
        var card=(XgCard)typeof(XgLabelPage).GetProperty("CurrentCard",Flags).GetValue(page);
        Require(card.id==themeCardId&&ctl.Sim.TeachingAnswers==themeAnswers&&ctl.Sim.S.combo==themeCombo&&ctl.Sim.S.workSubmitted==themeWork,
            "Theme wait changed question/answers/streak/work counters: "+name);
        var window=((XgUi)Field(page,"ui")).window;
        Require(window!=null&&window.isOn&&window.gameObject.activeInHierarchy,"Theme window is closed: "+name);
        for(int i=window.transform.GetSiblingIndex()+1;i<window.transform.parent.childCount;i++)
        {
            var other=window.transform.parent.GetChild(i).GetComponent<LingGuangV05.Desktop.OS.WindowManager>();
            Require(other==null||!other.isOn||!other.gameObject.activeInHierarchy,"Another app covers theme capture: "+name);
        }
        ClassicThemeChecks.Assert(page,work,visual);
        var text=(TMPro.TMP_Text)Field(page,visual?"question":"text");
        text.ForceMeshUpdate();Require(!string.IsNullOrWhiteSpace(text.text)&&text.textInfo.characterCount>0,"Theme caption has no rendered glyphs: "+name);
        string image=Path.Combine(Root,name+".png");
        Require(QaDesktopCapture.Save(image),"Actual LCD capture unavailable: "+name);
        report.checks.Add("Classic theme "+name+": current card and counters preserved; contextual colours asserted; five rendered frames; LCD screenshot "+image);Save();
    }
    [Serializable] sealed class Notes{public List<XgTeachingNote> items;}
    [Serializable] sealed class Minis{public List<XgMiniRecord> items;}
    static void AssertWork(XgLabelPage page,string id,string pack)
    {
        var sim=ctl.Sim;Require(sim.S.workDesk==id&&sim.S.teachingPack==pack,"Selections are not isolated");
        var card=(XgCard)typeof(XgLabelPage).GetProperty("CurrentCard",Flags).GetValue(page);
        Require(card.dataset==id&&card.id==sim.WorkCard(id).id,"Work content does not match selected company desk");
        Require((long)Field(page,"displayedId")==card.id,"Work rendered card not refreshed immediately");
        var mini=(XgDeskGamePage)Field(page,"deskGame");
        Require(!mini.root.gameObject.activeSelf&&!mini.Playing,"Teaching game leaked into company surface");
        Require(!((RectTransform)Field(page,"drawRoot")).gameObject.activeSelf,"Private handwriting leaked into company surface");
        var selected=FindButton(page.root,"Desk "+id);Require(selected!=null&&selected.transform.Find("Selected").gameObject.activeSelf,"Selected company row does not match content");
        report.checks.Add("Company "+id+" matches rendered WorkCard while private pack="+pack);
    }
    static void CheckHiddenSurfaceLifecycle()
    {
        var page=ctl.View.Teach;var sim=ctl.Sim;
        desk=(XgDeskGamePage)Field(page,"deskGame");
        var classics=(HashSet<string>)Field(page,"classicDesks");
        // A controlled default-mode fixture: clear only the session preference for the desk under test.
        classics.Remove("sense");sim.S.teachingPack="";sim.SelectDesk("sense");
        page.root.gameObject.SetActive(false);
        page.Refresh();
        Require(!desk.Visible&&!desk.Playing&&desk.SelectedGame==null,"Hidden refresh started a game");
        Require(!classics.Contains("sense"),"Hidden refresh silently selected classic mode");
        page.root.gameObject.SetActive(true);page.Refresh();
        Require(desk.Playing&&desk.SelectedGame==XgMiniGames.ForDesk("sense")&&desk.Visible,"Visible refresh did not start pending default game");
        // The real classic button must still be respected across an invisible refresh.
        var classic=(XgBtn)Field(desk,"classic");Require(classic.button.IsInteractable(),"Classic mode button unavailable");
        classic.button.onClick.Invoke();Require(classics.Contains("sense")&&!desk.Playing,"Explicit classic selection failed");
        page.root.gameObject.SetActive(false);page.Refresh();page.root.gameObject.SetActive(true);page.Refresh();
        Require(classics.Contains("sense")&&!desk.Playing&&!desk.root.gameObject.activeSelf,"Hidden cycle overwrote explicit classic preference");
        AssertBaseDeskSurface(page,"sense");
        var back=(XgBtn)Field(page,"gameToggle");Require(back.button.IsActive()&&back.button.IsInteractable(),"Return-to-game button unavailable");
        back.button.onClick.Invoke();Require(!classics.Contains("sense")&&desk.Playing&&desk.SelectedGame==XgMiniGames.ForDesk("sense"),"Explicit return-to-game failed");
        report.checks.Add("Hidden refresh defers default game without recording classic mode; visible refresh starts it; explicit classic/return choices survive visibility cycle");
    }
    static void AssertBaseDeskSurface(XgLabelPage page,string id)
    {
        var card=(XgCard)typeof(XgLabelPage).GetProperty("CurrentCard",Flags).GetValue(page);
        var classics=(HashSet<string>)Field(page,"classicDesks");
        string expected=XgMiniGames.ForDesk(id);
        bool eligible=(bool)typeof(XgLabelPage).GetMethod("DeskGameEligible",Flags).Invoke(page,new object[]{expected});
        string detail="Base desk="+id+", game="+(desk.SelectedGame??"<none>")+", playing="+desk.Playing+", surface="+desk.root.gameObject.activeSelf
            +", eligible="+eligible+", classic="+classics.Contains(id)+", card="+card.id+", dataset="+card.dataset+", kind="+card.kind
            +", family="+card.family+", law="+card.law+", preview="+card.bottleneckPreview;
        report.checks.Add(detail);Save();
        if(eligible&&!classics.Contains(id))
            Require(desk.SelectedGame==expected&&desk.Playing&&desk.root.gameObject.activeInHierarchy,"Base game stale: "+detail);
        else
        {
            // Existing classic-mode choice and story/review cards intentionally use the original card renderer.
            Require(desk.SelectedGame==null&&!desk.Playing&&!desk.root.gameObject.activeSelf,"Old game remained over classic desk: "+detail);
            Require(card.dataset==id&&(long)Field(page,"displayedId")==card.id,"Classic desk rendered another card: "+detail);
            var yes=(XgBtn)Field(page,"yes");var no=(XgBtn)Field(page,"no");
            Require(yes.button.gameObject.activeInHierarchy&&no.button.gameObject.activeInHierarchy,"Classic answer surface not visible: "+detail);
            var text=(TMPro.TMP_Text)Field(page,"question");
            Require(!string.IsNullOrEmpty(text.text)&&text.gameObject.activeInHierarchy,"Classic question is blank/hidden: "+detail);
        }
        var row=FindButton(page.root,"Desk "+id);
        Require(row!=null&&row.transform.Find("Selected").gameObject.activeSelf,"Base row highlight missing: "+detail);
    }
    static void AssertSingleSelection(XgLabelPage page,string pack)
    {
        int bars=0;foreach(var t in page.root.GetComponentsInChildren<Transform>(true))if(t.name=="Selected"&&t.gameObject.activeInHierarchy)bars++;
        Require(bars==0,"Base desk incorrectly highlighted with selected pack");
        var row=FindButton(page.root,"TeachingPack "+pack);Require(row!=null,"Selected pack row unavailable");
        Require(row.GetComponent<Image>().color!=Color.white,"Selected pack has no highlight");
    }
    static object Field(object obj,string name){for(Type t=obj.GetType();t!=null;t=t.BaseType){var f=t.GetField(name,Flags);if(f!=null)return f.GetValue(obj);}throw new Exception("Missing field "+name);}
    static Button FindButton(RectTransform root,string name){foreach(var b in root.GetComponentsInChildren<Button>(true))if(b.name==name&&b.gameObject.activeInHierarchy)return b;return null;}
    static void Press(RectTransform root,string name){var b=FindButton(root,name);Require(b!=null&&b.IsActive()&&b.IsInteractable(),"Button unavailable "+name);b.onClick.Invoke();}
    static void Finish(){finishing=true;Application.logMessageReceived-=Log;EditorApplication.update-=Tick;EditorApplication.ExitPlaymode();}
}
#endif
