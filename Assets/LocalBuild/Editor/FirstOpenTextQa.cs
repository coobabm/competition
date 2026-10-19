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
using TMPro;using LingGuangV05.Desktop.OS;
using System.Linq;
using LingGuangV05.Desktop.YY;
/// <summary>Run only in an already isolated Play session. Does not start Unity or touch a normal player's save.</summary>
[InitializeOnLoad]
public static class FirstOpenTextQa
{
    const string Key="FirstOpenTextQa.Root";
    static string Root=>SessionState.GetString(Key,"");
    static double bootDeadline;static bool fixtureReady;static readonly List<string> errors=new List<string>();
    static readonly List<string> editorInfrastructureErrors=new List<string>();
    static FirstOpenTextQa(){EditorApplication.playModeStateChanged+=State;}
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Start in Edit Mode");
        string dir=Path.GetFullPath(Path.Combine("LocalVerification","FirstOpenText",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));
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
                rt.useDiskSave=false;var lab=new XgSim();lab.S.stage=4;lab.S.teachingFlow=false;lab.S.openingFocus=false;lab.S.unlocked.Add("train.curve");var house=new ChapterOneSim();house.S.prologue=2;house.S.appInstallState=2;house.S.money=100000;house.S.gpuModels.Add("gtx980ti");house.S.gpuCount=1;house.S.labState=JsonUtility.ToJson(lab.S);
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
    static XingGuangController ctl;static string output;static bool running;static int phase;static double next;static IDisposable locale;static readonly List<string> passed=new List<string>();
    [Serializable]sealed class Report{public string status,detail;public string[] checks,errors;}
    static object Get(object o,string n)=>o.GetType().GetField(n,Flags).GetValue(o);
    static void Check(bool yes,string why){if(!yes)throw new Exception(why);passed.Add(why);}
    static void Start(){passed.Clear();phase=0;running=true;locale=GameText.Bind(()=>GameText.Chinese);ctl.Open();ctl.View.ShowTab("teach");next=EditorApplication.timeSinceStartup+2;EditorApplication.update+=Tick;}
    static void Rendered(TMP_Text text,string label){Check(text!=null&&text.gameObject.activeInHierarchy,label+" active");Check(text.rectTransform.rect.width>0&&text.rectTransform.rect.height>0,label+" positive layout");Check(!string.IsNullOrEmpty(text.text)&&text.textInfo.characterCount>0&&text.textInfo.meshInfo.Any(x=>x.vertexCount>0),label+" actual generated text mesh without QA ForceMeshUpdate");}
    static void Hall(string name){var p=ctl.View.Teach;Check(p.root.gameObject.activeInHierarchy,"Teach is visible");Check(!(bool)Get(p,"hallMeshPending"),"shown callback refreshed hall after visible frame");Rendered((TMP_Text)Get(p,"hallTitle"),name+" title");var hall=(RectTransform)Get(p,"hallContent");var first=hall.GetComponentsInChildren<TMP_Text>().FirstOrDefault(t=>!string.IsNullOrEmpty(t.text));Rendered(first,name+" first row");ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));}
    static void OpenYy(){var hub=YYChatHub.Instance;Check(hub?.S!=null,"YY state ready");hub.S.lingguangUnlocked=true;if(hub.Conversation(YYChatHub.LingGuangId)==null)hub.S.conversations.Add(new YYConversation{id=YYChatHub.LingGuangId});hub.Receive(YYChatHub.LingGuangId,GameText.T("我能看见你发来的消息。","I can read your message."),false);Check(XgYyTalk.Open(),"actual YY router opens AI contact");}
    static void Yy(string name){var hub=YYChatHub.Instance;var v=hub.View;Check(v!=null&&v.IsOpen,"YY actually visible");Rendered((TMP_Text)Get(v,"peerName"),name+" contact name");var content=(RectTransform)Get(v,"scrollContent");var wanted=GameText.T("我能看见你发来的消息。","I can read your message.");var text=content.GetComponentsInChildren<TMP_Text>().FirstOrDefault(t=>t.text.Contains(wanted));Rendered(text,name+" first message");ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));}
    static void Tick(){if(!running||EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+2;try{if(errors.Count>0)throw new Exception(errors[0]);switch(phase++){
      case 0:Hall("teach-first-zh");OpenYy();break;
      case 1:Yy("yy-first-zh");locale.Dispose();locale=GameText.Bind(()=>GameText.English);GameText.PublishChanged();ctl.Open();ctl.View.ShowTab("teach");break;
      case 2:Hall("teach-reopen-en");OpenYy();break;
      case 3:Yy("yy-reopen-en");Finish("passed","Visible task hall and YY text generated across real frames without a QA mesh refresh. PNG captures are supporting evidence when supported.");break;
    }}catch(Exception e){Finish("failed",e.ToString());}}
    static void Finish(string status,string detail){EditorApplication.update-=Tick;EditorApplication.update-=Boot;running=false;locale?.Dispose();locale=null;if(string.IsNullOrEmpty(output))output=Root;Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(new Report{status=status,detail=detail,checks=passed.ToArray(),errors=errors.ToArray()},true));if(status=="passed")File.WriteAllText(Path.Combine(output,"passed.txt"),"passed");EditorApplication.ExitPlaymode();}
}
#endif
