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
/// <summary>Run only in an already isolated Play session. Does not start Unity or touch a normal player's save.</summary>
[InitializeOnLoad]
public static class StageCurtainPlayQa
{
    const string Key="StageCurtainPlayQa.Root";
    static string Root=>SessionState.GetString(Key,"");
    static double bootDeadline;static bool fixtureReady;static readonly List<string> errors=new List<string>();
    static readonly List<string> editorInfrastructureErrors=new List<string>();
    static StageCurtainPlayQa(){EditorApplication.playModeStateChanged+=State;}
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Start in Edit Mode");
        string dir=Path.GetFullPath(Path.Combine("LocalVerification","StageCurtain",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));
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
                rt.useDiskSave=false;var lab=new XgSim();lab.S.stage=2;lab.S.handCorrect=24;var house=new ChapterOneSim();house.S.prologue=2;house.S.appInstallState=2;house.S.money=100000;house.S.gpuModels.Add("gtx980ti");house.S.gpuCount=1;house.S.labState=JsonUtility.ToJson(lab.S);
                rt.SetSimulationForTests(house);ctl.GetType().GetMethod("BindSave",Flags).Invoke(ctl,new object[]{house});
                var boot=UnityEngine.Object.FindAnyObjectByType<Michsky.DreamOS.BootManager>(FindObjectsInactive.Include);if(boot!=null){boot.CancelInvoke();boot.StopAllCoroutines();if(boot.bootAnimator!=null)boot.bootAnimator.gameObject.SetActive(false);}
                var user=UnityEngine.Object.FindAnyObjectByType<Michsky.DreamOS.UserManager>(FindObjectsInactive.Include);if(user==null||!user.isActiveAndEnabled)throw new Exception("Desktop user unavailable");user.ShowDesktop();fixtureReady=true;return;
            }
            var binding=rt.GetComponent<ChapterOneDesktopRouter>()?.Get(XingGuangController.AppId);if(binding?.TaskbarShortcut==null||!binding.TaskbarShortcut.isActiveAndEnabled||binding.DesktopShortcut==null||!binding.DesktopShortcut.gameObject.activeInHierarchy)return;
            EditorApplication.update-=Boot;BeginChecks();
        }
        catch(Exception e){EditorApplication.update-=Boot;Finish("failed",e.ToString());}
    }
    const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static readonly int[] stages={2,4,5,6};static int stageIndex;static int TestStage=>stages[stageIndex];
    static XingGuangController ctl;static string output;static bool running;static int phase;static double next,deadline;static XgStageCurtain curtain;
    static readonly List<string> passed=new List<string>();
    [Serializable]sealed class Report{public string status,detail;public List<string> passed,editorInfrastructureErrors;}
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);passed.Add(message);}
    static T Field<T>(string key)=>(T)curtain.GetType().GetField(key,Flags).GetValue(curtain);
    static void AdvanceFixture(){ctl.Sim.S.stage=TestStage-1;typeof(XgSim).GetMethod("AdvanceStage",Flags).Invoke(ctl.Sim,new object[]{TestStage-1});Check(ctl.Sim.StageUnlockSnapshot(TestStage)!=null,"Actual advance captures stage "+TestStage+" unlock history (eligibility bypassed in fixture)");}
    static void BeginChecks(){curtain=ctl.GetComponent<XgStageCurtain>();if(curtain==null)throw new Exception("Curtain not installed");passed.Clear();stageIndex=0;AdvanceFixture();phase=0;running=true;next=EditorApplication.timeSinceStartup+.5;deadline=next+60;EditorApplication.update+=Tick;}
    static void Tick(){if(!running)return;try{if(errors.Count>0)throw new Exception(errors[0]);double now=EditorApplication.timeSinceStartup;if(now>deadline)throw new TimeoutException();if(now<next)return;next=now+.7;
      if(phase==0){Check(XgStageCurtain.Busy&&XgStageCurtain.Step==0,"Month appears first and owns exclusive focus");Check(StoryFocus.Exclusive,"Global popup gate active");Check(!ctl.Sim.BeginEpoch(XgTrack.Vision,ctl.Host),"Real manual training blocked");ScreenCapture.CaptureScreenshot(Path.Combine(output,"01-month.png"));next=now+3;}
      else if(phase==1){Check(XgStageCurtain.Step==1,"Month automatically advances to demo");var demo=Field<RectTransform>("demoRoot");Check(demo.gameObject.activeInHierarchy&&demo.rect.size==new Vector2(960,540),"Centered demo surface is 960x540");if(TestStage<4)Check(demo.GetComponentsInChildren<XgTinyPixels>().Length>0,"Live demo has actual tiny-network picture/weight renderers");
        else{var sequence=Field<XgStageSequenceDemo>("sequence");Check(sequence.Visible,"Stage "+TestStage+" owns distinct sequence demo");
         if(TestStage==4)Check(sequence.Remembered(5)>.9f&&sequence.Ungated(5)<.1f,"Memory gate retains location while ordinary recurrence fades");
         else{for(int q=0;q<6;q++){float sum=0;for(int k=0;k<6;k++)sum+=sequence.Weight(q,k);Check(Mathf.Abs(sum-1)<.0001f,"Attention row normalized "+TestStage+":"+q);}
          Check(TestStage==5?sequence.Weight(3,3)>sequence.Weight(3,0):sequence.Weight(3,0)>sequence.Weight(3,3),"Stage-specific translation/reference focus");}}
Check(!Field<TMPro.TMP_Text>("body").gameObject.activeSelf,"First-line panel absent during demo");ScreenCapture.CaptureScreenshot(Path.Combine(output,"02-demo.png"));curtain.Next();}
      else if(phase==2){Check(XgStageCurtain.Step==2&&!Field<RectTransform>("demoRoot").gameObject.activeSelf,"First line replaces demo");Check(Field<RectTransform>("card").rect.width==720,"First line card width 720");curtain.Next();if(XgStageCurtain.Step==2)curtain.Next();}
      else if(phase==3){Check(XgStageCurtain.Step==3,"Unlock list follows first line");Check(Field<TMPro.TMP_Text>("body").text.Split('\n').Length<=6,"Unlock list at most six lines");ScreenCapture.CaptureScreenshot(Path.Combine(output,"04-unlocks.png"));int pages=0;while(XgStageCurtain.Step==3){Check(Field<TMPro.TMP_Text>("body").text.Split('\n').Length<=6,"Page "+pages+" respects six-line limit for stage "+TestStage);var pageText=Field<TMPro.TMP_Text>("body");pageText.ForceMeshUpdate();Check(pageText.textInfo.lineCount<=6&&!pageText.isTextOverflowing,"Actual TMP page fits six rendered lines for stage "+TestStage+" page "+pages);curtain.Next();if(++pages>100)throw new Exception("Unlock pages did not terminate");}Check(XgStageCurtain.Step==4&&ctl.Sim.StagePresentationHeld,"Fade keeps pending save ownership");}
      else {Check(!ctl.Sim.StagePresentationHeld&&!XgStageCurtain.Showing,"Fade completion clears persisted pending stage");if(stageIndex+1<stages.Length){stageIndex++;ctl.Sim.S.stage=TestStage;ctl.Sim.English=TestStage==5;AdvanceFixture();phase=0;next=now+.5;deadline=now+60;return;}
       int screenshots=Directory.GetFiles(output,"*.png").Length;Finish("passed","Actual stage controller, staged sequence calculations and paginated unlocks checked. PNG files actually found: "+screenshots+". No physical input automation.");return;}phase++;
     }catch(Exception e){Finish("failed",e.ToString());}}
    static void Finish(string status,string detail){running=false;EditorApplication.update-=Tick;File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(new Report{status=status,detail=detail,passed=passed,editorInfrastructureErrors=editorInfrastructureErrors},true));if(status=="passed")File.WriteAllText(Path.Combine(output,"passed.txt"),detail);Application.logMessageReceived-=Log;EditorApplication.ExitPlaymode();}
}
#endif
