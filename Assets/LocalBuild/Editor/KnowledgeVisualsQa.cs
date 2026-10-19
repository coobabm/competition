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
using LingGuangV05.Desktop.Story;using TMPro;using LingGuangV05.Desktop.OS;
using System.Linq;
using LingGuangV05.Desktop.Bodu;
/// <summary>Run only in an already isolated Play session. Does not start Unity or touch a normal player's save.</summary>
[InitializeOnLoad]
public static class KnowledgeVisualsQa
{
    const string Key="KnowledgeVisualsQa.Root";
    static string Root=>SessionState.GetString(Key,"");
    static double bootDeadline;static bool fixtureReady;static readonly List<string> errors=new List<string>();
    static readonly List<string> editorInfrastructureErrors=new List<string>();
    static KnowledgeVisualsQa(){EditorApplication.playModeStateChanged+=State;}
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Start in Edit Mode");
        string dir=Path.GetFullPath(Path.Combine("LocalVerification","KnowledgeVisuals",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));
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
                var boot=UnityEngine.Object.FindAnyObjectByType<LingGuangV05.Desktop.OS.BootManager>(FindObjectsInactive.Include);if(boot!=null){boot.CancelInvoke();boot.StopAllCoroutines();if(boot.bootAnimator!=null)boot.bootAnimator.gameObject.SetActive(false);}
                var user=UnityEngine.Object.FindAnyObjectByType<LingGuangV05.Desktop.OS.UserManager>(FindObjectsInactive.Include);if(user==null||!user.isActiveAndEnabled)throw new Exception("Desktop user unavailable");user.ShowDesktop();fixtureReady=true;return;
            }
            var binding=rt.GetComponent<ChapterOneDesktopRouter>()?.Get(XingGuangController.AppId);if(binding?.TaskbarShortcut==null||!binding.TaskbarShortcut.isActiveAndEnabled||binding.DesktopShortcut==null||!binding.DesktopShortcut.gameObject.activeInHierarchy)return;
            EditorApplication.update-=Boot;Start();
        }
        catch(Exception e){EditorApplication.update-=Boot;Finish("failed",e.ToString());}
    }
    const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static XingGuangController ctl;static XgTrainPage page;static string output;static bool running;static int phase,ticketBefore;static double next;static IDisposable locale;static readonly List<string> passed=new List<string>();
    [Serializable]sealed class Report{public string status,detail;public string[] checks,errors;}
    static T Field<T>(string name)=>(T)typeof(XgTrainPage).GetField(name,Flags).GetValue(page);
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);passed.Add(text);}
    static int Ticket=>(int)typeof(InnerVoice).GetField("nextTicket",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
    public static void Start(){if(running)throw new Exception("Already running");running=true;passed.Clear();InnerVoice.Clear();page=ctl.View.Train;ctl.runtime.tickEnabled=false;ctl.Sim.S.teachingFlow=false;ctl.Sim.S.openingFocus=false;if(!ctl.Sim.Has("train.curve"))ctl.Sim.S.unlocked.Add("train.curve");ctl.Sim.InitializeTrainingLessons();ctl.Sim.S.lessonPending.Clear();ctl.Sim.S.quizQueued.Clear();var run=ctl.Sim.Run(ctl.Sim.SelectedTrack);run.histTrain.Clear();run.histVal.Clear();run.histTrain.AddRange(new[]{.4f,.6f});run.histVal.AddRange(new[]{.3f,.5f});run.epoch=2;typeof(XgSim).GetMethod("ObserveTrainingEpoch",Flags).Invoke(ctl.Sim,new object[]{new XgEpoch{track=run.track}});Check(ctl.Sim.S.lessonPending.Contains("grokking"),"production observer detects controlled 20-point jump history");ctl.Open();ctl.View.ShowTab("train");phase=0;next=EditorApplication.timeSinceStartup+1;EditorApplication.update+=Tick;}
    static void Tick(){if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.8;try{if(errors.Count>0)throw new Exception(errors[0]);switch(phase++){
      case 0:Canvas.ForceUpdateCanvases();var layer=Field<RectTransform>("lessonLayer");var box=Field<RectTransform>("lessonBox");var body=Field<TMP_Text>("lessonBody");var title=Field<TMP_Text>("lessonTitle");Check(layer.gameObject.activeInHierarchy&&ctl.Sim.TrainingLessonModalOpen,"knowledge modal visible and training paused");Check(box.rect.height>box.rect.width,"knowledge card is portrait");title.ForceMeshUpdate();body.ForceMeshUpdate();Check(title.text.Contains("#E8B65A")&&title.text.Contains("Grokking"),"technical term amber bold with original English");Check(!title.isTextOverflowing&&!body.isTextOverflowing,"Chinese card TMP fits actual geometry");Check(!body.text.Contains(XgTrainingLessons.Card("grokking").voice),"collect self-talk absent before collection");Check(Field<XgKnowledgeHalo>("knowledgeHalo").gameObject.activeInHierarchy,"halo only on actual recorded curve point");Check(!ctl.Sim.BeginEpoch(ctl.Sim.SelectedTrack,ctl.Host),"manual train cannot run under card");ticketBefore=Ticket;Field<XgBtn>("lessonCollect").button.onClick.Invoke();Check(ctl.Sim.TrainingAtlasLit("lesson.grokking")&&ctl.Sim.S.lessonAtlasUnread,"actual collect button commits atlas and unread flag immediately");Check(Field<bool>("knowledgeFlying"),"collect creates visible flight");Check(Ticket==ticketBefore,"self-talk waits until card arrives");Check(Field<RectTransform>("knowledgeFlightVisual")!=null,"flight ghost exists above app/nav common parent");break;
      case 1:Check(!Field<bool>("knowledgeFlying")&&Field<RectTransform>("knowledgeFlightVisual")==null,"flight settles and removes transient ghost");Check(Ticket==ticketBefore+1,"collect queues exactly one self-talk line after flight");var instance=typeof(InnerVoice).GetField("instance",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);var current=instance.GetType().GetField("current",Flags).GetValue(instance);object found=null;if((int)current.GetType().GetField("ticket").GetValue(current)==ticketBefore+1)found=current;foreach(var line in (System.Collections.IEnumerable)instance.GetType().GetField("queue",Flags).GetValue(instance))if((int)line.GetType().GetField("ticket").GetValue(line)==ticketBefore+1)found=line;Check(found!=null&&!(bool)found.GetType().GetField("voiced").GetValue(found),"new self-talk is explicitly text-only");Check(ctl.View.NavButton("tree").Find("Badge").GetComponent<TMP_Text>().text.Contains("●"),"actual Tech nav displays red unread dot");var saved=JsonUtility.FromJson<XgState>(JsonUtility.ToJson(ctl.Sim.S));Check(saved.lessonAtlasUnread&&saved.lessonCurveStamps.Count>0,"Unity serialization preserves badge and honest point snapshot");ctl.View.ShowTab("tree");Check(!ctl.Sim.S.lessonAtlasUnread,"opening Tech consumes unread badge");ticketBefore=Ticket;locale=GameText.Bind(()=>GameText.English);ctl.Sim.English=true;Check(page.ShowTrainingReview("lesson.grokking"),"actual atlas card review opens full lesson");break;
      case 2:Canvas.ForceUpdateCanvases();var b=Field<TMP_Text>("lessonBody");var t=Field<TMP_Text>("lessonTitle");b.ForceMeshUpdate();t.ForceMeshUpdate();Check(GameText.IsEnglish&&t.text.Contains("Grokking"),"review uses actual English provider");Check(!b.isTextOverflowing&&!t.isTextOverflowing,"English portrait card TMP fits");Field<XgBtn>("lessonCollect").button.onClick.Invoke();Check(!ctl.Sim.S.lessonAtlasUnread&&Ticket==ticketBefore,"review return never replays badge or self-talk");var r=ctl.Sim.Run(ctl.Sim.SelectedTrack);r.histVal.Add(.51f);r.histTrain.Add(.62f);page.Refresh();page.ShowTrainingReview("lesson.grokking");break;
      case 3:Check(!Field<XgKnowledgeHalo>("knowledgeHalo").gameObject.activeSelf,"changed curve review never invents old incident position");page.HideTrainingLessons();Check(!ctl.Sim.TrainingLessonModalOpen,"hide clears modal and animation ownership");Finish("passed","Actual Main card, collect button, animation/nav badge/text-only self-talk, bilingual TMP, and Unity snapshot serialization. Curve values are a controlled 20-point jump fixture processed by the production observer, not measured player learning; no screenshot or physical mouse claim.");break;}}
      catch(Exception e){Finish("failed",e.ToString());}}
    static void Finish(string status,string detail){EditorApplication.update-=Tick;EditorApplication.update-=Boot;running=false;locale?.Dispose();locale=null;if(string.IsNullOrEmpty(output))output=Root;Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(new Report{status=status,detail=detail,checks=passed.ToArray(),errors=errors.ToArray()},true));if(status=="passed")File.WriteAllText(Path.Combine(output,"passed.txt"),"passed");EditorApplication.ExitPlaymode();}
}
#endif
