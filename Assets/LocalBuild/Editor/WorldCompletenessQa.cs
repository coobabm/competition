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
using TMPro;using Michsky.DreamOS;
using System.Linq;
using LingGuangV05.Desktop.Bodu;
/// <summary>Run only in an already isolated Play session. Does not start Unity or touch a normal player's save.</summary>
[InitializeOnLoad]
public static class WorldCompletenessQa
{
    const string Key="WorldCompletenessQa.Root";
    static string Root=>SessionState.GetString(Key,"");
    static double bootDeadline;static bool fixtureReady;static readonly List<string> errors=new List<string>();
    static readonly List<string> editorInfrastructureErrors=new List<string>();
    static WorldCompletenessQa(){EditorApplication.playModeStateChanged+=State;}
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Start in Edit Mode");
        string dir=Path.GetFullPath(Path.Combine("LocalVerification","WorldCompleteness",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));
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
    static XingGuangController ctl;static BoduView browser;static string output;static bool running;static int phase;static double next;static IDisposable locale;static RectTransform windowRect;static Vector2 originalSize;static readonly List<string> passed=new List<string>();static readonly List<string> screenshots=new List<string>();
    [Serializable]sealed class Report{public string status,detail;public string[] checks,screenshots,errors;}
    sealed class Host:IXgHost,IXgHousehold{public double Compute=>100;public double VramMB=>99999;public double Money=>999999;public string Blocker=>null;public bool Spend(double n)=>true;public void Earn(double n){}public void Train(double n){}public bool BillsActive=>true;public double MonthKwh=>0;public double HighKwh=>100;public double hour=12;public double SecondsOfDay=>hour*3600;}
    static void Check(bool ok,string detail){if(!ok)throw new Exception(detail);passed.Add(detail);}
    static void LifeTick(XgSim s,Host h,double seconds)=>typeof(XgSim).GetMethod("TickLifeEvents",Flags).Invoke(s,new object[]{seconds,h});
    static XgSim EventSim(int stage){var s=new XgSim();s.S.stage=stage;s.Today=20161201;s.S.openingFocus=false;return s;}
    static void CoreChecks(){
      XgSim.LoadLifeData(Resources.Load<TextAsset>("LingGuangV05/Life/life_events_100").text);XgNewsCatalog.Load(Resources.Load<TextAsset>("LingGuangV05/Browser/browser_news").text);var h=new Host();
      for(int stage=1;stage<=6;stage++){var s=EventSim(stage);Check(s.LifeEventsUnlocked,"event fixture enabled stage "+stage);LifeTick(s,h,.01);s.S.lifeNext=0;LifeTick(s,h,1);Check(s.LifeEventOpen&&s.S.lifeDrawCount==1,"natural eligible-pool draw stage "+stage);string first=s.LifeEventId;Check(s.AnswerLifeEvent(1,h),"natural event answer stage "+stage);Check(s.LifeEventNext>=360&&s.LifeEventNext<=480,"next interval >=6 minutes stage "+stage);s.S.lifeNext=0;LifeTick(s,h,1);Check(s.LifeEventOpen&&s.LifeEventId!=first&&s.S.lifeDrawCount==2,"second distinct natural draw stage "+stage);s.AnswerLifeEvent(1,h);s.S.lifeNext=0;LifeTick(s,h,999);Check(!s.LifeEventOpen&&s.S.lifeDrawCount==2,"third timer expiry cannot exceed stage cap "+stage);}
      var condition=EventSim(1);condition.Today=20160608;Check(condition.LifeEventsEligible(h).Contains("e016"),"June/stage1 event eligible");condition.Today=20160701;Check(!condition.LifeEventsEligible(h).Contains("e016"),"month gate rejects July");condition.Today=20160608;condition.S.stage=3;Check(!condition.LifeEventsEligible(h).Contains("e016"),"stage gate rejects stage3");
      condition=EventSim(4);Check(!condition.LifeEventsEligible(h).Contains("e062"),"reply event absent before player posting");Check(condition.OpenLifeEventNow("e025",h),"posting opportunity opens through real eligibility");Check(condition.AnswerLifeEvent(0,h)&&condition.S.lifeFlags.Contains("posted_progress"),"actual post choice records history");Check(condition.LifeEventsEligible(h).Contains("e062"),"reply event becomes eligible after posting");
      var decline=EventSim(4);decline.OpenLifeEventNow("e025",h);decline.AnswerLifeEvent(1,h);Check(!decline.LifeEventsEligible(h).Contains("e062"),"declining post never invents replies");
      var held=EventSim(3);held.QueueStagePresentation(3);LifeTick(held,h,.01);held.S.lifeNext=0;LifeTick(held,h,1);Check(!held.LifeEventOpen&&held.S.lifeDrawCount==0,"curtain holds natural event without consuming quota");held.CompleteStagePresentation(3);held.S.lifeNext=0;LifeTick(held,h,1);Check(held.LifeEventOpen,"natural event resumes after curtain");
      Check(XgNewsCatalog.All.Count==45,"45 bundled news articles");for(int month=6;month<=12;month++){var date=new DateTime(2016,month,1);Check(XgNewsCatalog.Available(date).All(x=>DateTime.Parse(x.date)<=date),"no future stories month "+month);}
      var news=EventSim(3);news.Today=20160921;Check(!news.TeachNews("n0922"),"unpublished article cannot be taught");news.Today=20160922;Check(news.TeachNews("n0922")&&!news.TeachNews("n0922"),"published article taught only once");var reload=new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(news.S)));Check(reload.NewsAlreadyTaught("n0922")&&!reload.TeachNews("n0922"),"actual Unity save serialization retains teaching receipt");
    }
    public static void Start(){if(running)throw new Exception("Already running");running=true;passed.Clear();screenshots.Clear();CoreChecks();browser=UnityEngine.Object.FindAnyObjectByType<BoduView>(FindObjectsInactive.Include)??BoduView.Install();Check(browser!=null,"real browser exists");browser.Open("");windowRect=browser.GetComponentInParent<WindowManager>(true).GetComponent<RectTransform>();originalSize=windowRect.sizeDelta;phase=0;next=EditorApplication.timeSinceStartup+1;EditorApplication.update+=Tick;}
    static void SetPage(bool english,int stage,bool article){locale?.Dispose();locale=GameText.Bind(()=>english?GameText.English:GameText.Chinese);ctl.Sim.English=english;ctl.Sim.S.stage=stage;ctl.runtime.tickEnabled=false;GameCalendar.Advance(ctl.runtime.Sim.S,GameCalendar.DayIndex(new DateTime(2016,12,20)));ctl.Sim.Today=20161220;typeof(BoduView).GetField("page",Flags).SetValue(browser,article?"news":"home");typeof(BoduView).GetField("newsId",Flags).SetValue(browser,"n0922");typeof(BoduView).GetField("signature",Flags).SetValue(browser,"");typeof(BoduView).GetMethod("Redraw",Flags).Invoke(browser,new object[]{ctl.runtime.Sim.S,ctl.Sim});}
    static void LayoutCheck(string name,bool columns){Canvas.ForceUpdateCanvases();var content=(RectTransform)typeof(BoduView).GetField("content",Flags).GetValue(browser);LayoutRebuilder.ForceRebuildLayoutImmediate(content);Canvas.ForceUpdateCanvases();int count=0;foreach(var t in content.GetComponentsInChildren<TMP_Text>()){if(string.IsNullOrEmpty(t.text))continue;t.ForceMeshUpdate();Check(t.rectTransform.rect.width>0&&t.rectTransform.rect.height>0,name+" text has positive layout: "+t.text.Substring(0,Math.Min(20,t.text.Length)));Check(!t.isTextOverflowing,name+" TMP fully fits: "+t.text.Substring(0,Math.Min(20,t.text.Length)));count++;}Check(count>5,name+" rendered meaningful article/list");if(columns){var row=content.Find("News and trends") as RectTransform;Check(row!=null,"stage6 two-column container exists");var left=row.Find("Latest news") as RectTransform;var right=row.Find("Hot searches") as RectTransform;var a=RectTransformUtility.CalculateRelativeRectTransformBounds(content,left);var b=RectTransformUtility.CalculateRelativeRectTransformBounds(content,right);Check(a.max.x<=b.min.x+1,"stage6 columns do not overlap");Check(row.rect.height>100,"stage6 columns contribute actual scroll height");}var path=Path.Combine(output,name+".png");ScreenCapture.CaptureScreenshot(path);screenshots.Add(path);}
    static void Tick(){if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+1;try{if(errors.Count>0)throw new Exception(errors[0]);switch(phase++){
      case 0:SetPage(false,1,false);break;case 1:LayoutCheck("news-stage1-zh",false);SetPage(true,6,false);break;case 2:LayoutCheck("news-stage6-en",true);windowRect.sizeDelta=new Vector2(720,540);SetPage(false,6,false);break;case 3:LayoutCheck("news-stage6-720-zh",true);SetPage(true,6,true);break;case 4:LayoutCheck("news-article-en",false);var content=(RectTransform)typeof(BoduView).GetField("content",Flags).GetValue(browser);var button=content.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.GetComponent<TMP_Text>()?.text.Contains("Show it this article")==true);Check(button!=null,"actual article teach button present");button.onClick.Invoke();Check(ctl.Sim.NewsAlreadyTaught("n0922"),"actual UI click teaches current AI");SetPage(true,6,true);break;case 5:var c=(RectTransform)typeof(BoduView).GetField("content",Flags).GetValue(browser);Check(c.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("Already given to")),"article UI marks existing receipt");Check(!c.GetComponentsInChildren<Button>().Any(b=>b.GetComponent<TMP_Text>()?.text.Contains("Show it this article")==true),"article UI cannot reward duplicate teaching");screenshots.RemoveAll(path=>!File.Exists(path));Finish("passed",screenshots.Count==0?"Batch capture produced no PNG; actual TMP/layout/buttons checked, visual screenshot review remains pending.":"Screenshots saved for review.");break;}}
      catch(Exception e){Finish("failed",e.ToString());}}
    static void Finish(string status,string detail){EditorApplication.update-=Tick;EditorApplication.update-=Boot;running=false;locale?.Dispose();locale=null;if(windowRect!=null)windowRect.sizeDelta=originalSize;if(string.IsNullOrEmpty(output))output=Root;Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(new Report{status=status,detail=detail,checks=passed.ToArray(),screenshots=screenshots.ToArray(),errors=errors.ToArray()},true));if(status=="passed")File.WriteAllText(Path.Combine(output,"passed.txt"),"passed");EditorApplication.ExitPlaymode();}
}
#endif


