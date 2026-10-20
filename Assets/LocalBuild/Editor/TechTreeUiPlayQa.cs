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
using static LingGuangV05.Desktop.XingGuang.XgUi;
/// <summary>Run only in an already isolated Play session. Does not start Unity or touch a normal player's save.</summary>
[InitializeOnLoad]
public static class TechTreeUiPlayQa
{
    const string Key="TechTreeUiPlayQa.Root";
    static string Root=>SessionState.GetString(Key,"");
    static IDisposable locale;static string language="zh-CN";
    static double bootDeadline;static bool fixtureReady;static readonly List<string> errors=new List<string>();
    static readonly List<string> editorInfrastructureErrors=new List<string>();
    static TechTreeUiPlayQa(){EditorApplication.playModeStateChanged+=State;}
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Start in Edit Mode");
        string dir=Path.GetFullPath(Path.Combine("LocalVerification","TechTreeUI",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));
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
        foreach(var root in scene.GetRootGameObjects())
        {
            foreach(var rt in root.GetComponentsInChildren<ChapterOneRuntime>(true)){var llm=rt.GetComponent<LocalLlm>()??rt.gameObject.AddComponent<LocalLlm>();llm.enabled=false;}
            // This all-unlocked layout fixture must not run a new game's prologue over the cards.
            foreach(var component in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if(component==null)continue;
                string name=component.GetType().Name;
                if(name=="PrologueDirector"||name=="StoryDirector"||name=="StoryDesktopPresenter"||name=="StageOneTutor"||name=="SpiderIntroScene")
                {component.CancelInvoke();component.StopAllCoroutines();component.enabled=false;}
            }
        }
    }
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
                // Runtime installs these components after sceneLoaded; stop them after installation too.
                foreach(var component in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    string kind=component.GetType().Name;
                    if(kind=="PrologueDirector"||kind=="StoryDirector"||kind=="StoryDesktopPresenter"||kind=="StageOneTutor"||kind=="SpiderIntroScene")
                    {component.CancelInvoke();component.StopAllCoroutines();component.enabled=false;}
                }
                rt.useDiskSave=false;var lab=new XgSim();lab.S.stage=6;lab.S.handCorrect=120;lab.S.epochs=10;foreach(var n in XgCatalog.Nodes)if(!lab.S.unlocked.Contains(n.id))lab.S.unlocked.Add(n.id);var house=new ChapterOneSim();house.S.prologue=2;house.S.appInstallState=2;house.S.money=100000;house.S.gpuModels.Add("gtx980ti");house.S.gpuCount=1;house.S.labState=JsonUtility.ToJson(lab.S);
                rt.SetSimulationForTests(house);ctl.GetType().GetMethod("BindSave",Flags).Invoke(ctl,new object[]{house});
                var boot=UnityEngine.Object.FindAnyObjectByType<LingGuangV05.Desktop.OS.BootManager>(FindObjectsInactive.Include);if(boot!=null){boot.CancelInvoke();boot.StopAllCoroutines();if(boot.bootAnimator!=null)boot.bootAnimator.gameObject.SetActive(false);}
                var user=UnityEngine.Object.FindAnyObjectByType<LingGuangV05.Desktop.OS.UserManager>(FindObjectsInactive.Include);if(user==null||!user.isActiveAndEnabled)throw new Exception("Desktop user unavailable");user.ShowDesktop();fixtureReady=true;return;
            }
            var binding=rt.GetComponent<ChapterOneDesktopRouter>()?.Get(XingGuangController.AppId);if(binding?.TaskbarShortcut==null||!binding.TaskbarShortcut.isActiveAndEnabled||binding.DesktopShortcut==null||!binding.DesktopShortcut.gameObject.activeInHierarchy)return;
            EditorApplication.update-=Boot;BeginChecks();
        }
        catch(Exception e){EditorApplication.update-=Boot;Finish("failed",e.ToString());}
    }
    const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static readonly Queue<Action> steps=new Queue<Action>();static readonly List<string> passed=new List<string>(),failed=new List<string>();
    static XingGuangController ctl;static XgTechTreePage page;static string output;static double next,deadline;static bool running;
    static UnityEngine.EventSystems.PointerEventData pointer;static Vector2 anchorBefore;static int paintBefore,meshBefore;
    [Serializable] sealed class Report{public string status,detail;public List<string> passed,failed,editorInfrastructureErrors;public List<TextResult> labels;}
    [Serializable] sealed class TextResult{public string id,language,text;public int lines,characters;public bool truncated,overflow;public float width,height,preferredWidth,preferredHeight;}
    static readonly List<TextResult> labels=new List<TextResult>();
    static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,Flags).GetValue(target);
    static int Counter(object target,string name)=>(int)target.GetType().GetProperty(name,Flags).GetValue(target);
    static void Check(bool ok,string message){(ok?passed:failed).Add(message);}
    static void BeginChecks()
    {
        language="zh-CN";locale=GameText.Bind(()=>language);GameText.PublishChanged();page=ctl.View.Tree;ctl.Open();ctl.View.Open("tree");passed.Clear();failed.Clear();labels.Clear();steps.Clear();
        steps.Enqueue(()=>{page.Pan.SetView(1,new Vector2(-200,200));page.Refresh();Canvas.ForceUpdateCanvases();});
        steps.Enqueue(()=>{CheckMeshes(false);language=GameText.English;GameText.PublishChanged();ctl.Sim.English=true;page.Refresh();});
        steps.Enqueue(()=>{CheckMeshes(true);language="zh-CN";GameText.PublishChanged();ctl.Sim.English=false;page.Refresh();var views=Field<List<XgTechNodeView>>(page,"views");var v=views.Find(x=>x.gameObject.activeInHierarchy&&x.label.text.Contains("\n"));if(v==null)throw new Exception("No real three-line node found");
            foreach(var b in v.GetComponentsInChildren<Button>(true)){var l=b.GetComponentInChildren<TMPro.TMP_Text>();Check(l==null||(!l.text.Contains("详情")&&!l.text.Contains("Details")),"No separate Details text action");}
            var iconPointer=Pointer();v.OnPointerDown(iconPointer);v.OnPointerUp(iconPointer);v.OnPointerClick(iconPointer);});
        steps.Enqueue(()=>{var tip=Field<RectTransform>(page,"tooltip");var body=Field<TMPro.TMP_Text>(page,"tipBody");body.ForceMeshUpdate(true,true);Check(tip.gameObject.activeInHierarchy&&body.textInfo.characterCount>0,"Icon click opens rendered science text");CaptureLcd("explanation.png");
            page.Pan.SetView(1,new Vector2(-400,400));pointer=Pointer();anchorBefore=ContentPoint();pointer.scrollDelta=Vector2.up;page.Pan.OnScroll(pointer);});
        steps.Enqueue(()=>{Check(Vector2.Distance(anchorBefore,ContentPoint())<1,"Wheel zoom preserves content under pointer");Check(Mathf.Abs(page.Pan.content.localScale.x-1.1f)<.003f,"Wheel increases by ten percent");for(int i=0;i<20;i++)page.Pan.OnScroll(pointer);});
        steps.Enqueue(()=>{Check(Mathf.Abs(page.Pan.content.localScale.x-1.5f)<.003f,"Wheel maximum 150 percent");pointer.scrollDelta=Vector2.down;for(int i=0;i<30;i++)page.Pan.OnScroll(pointer);});
        steps.Enqueue(()=>{Check(Mathf.Abs(page.Pan.content.localScale.x-.5f)<.003f,"Wheel minimum 50 percent");pointer.clickCount=2;page.Pan.OnPointerClick(pointer);Check(Mathf.Abs(page.Pan.content.localScale.x-1)<.001f,"Double click returns 100 percent");ClickToolbar(5);});
        steps.Enqueue(()=>{float fit=Mathf.Clamp(Mathf.Min(page.Pan.viewport.rect.width/page.Pan.content.sizeDelta.x,page.Pan.viewport.rect.height/page.Pan.content.sizeDelta.y),.5f,1.5f);Check(Mathf.Abs(page.Pan.content.localScale.x-fit)<.001f,"Fit button calculates bounded viewport fit");ClickToolbar(4);});
        steps.Enqueue(()=>{Check(page.Pan.content.localScale.x>.5f,"Plus button zooms in");ClickToolbar(3);});
        steps.Enqueue(()=>{Check(Mathf.Abs(page.Pan.content.localScale.x-.5f)<.003f,"Minus button zooms out");page.Pan.SetView(1,new Vector2(-300,300));pointer=Pointer();page.Pan.OnBeginDrag(pointer);Check(page.Pan.Dragging,"Pointer begin drag entered dragging");Canvas.ForceUpdateCanvases();paintBefore=Counter(page,"PaintPasses");meshBefore=Counter(Field<XgTechLinksGraphic>(page,"batchedLinks"),"MeshBuilds");});
        steps.Enqueue(()=>{pointer.position+=new Vector2(30,-20);page.Pan.OnDrag(pointer);page.Refresh();Canvas.ForceUpdateCanvases();Check(Counter(page,"PaintPasses")==paintBefore,"Requested refresh deferred while dragging");Check(Counter(Field<XgTechLinksGraphic>(page,"batchedLinks"),"MeshBuilds")==meshBefore,"Dragging does not rebuild connection mesh");Check(!Field<RectTransform>(page,"tooltip").gameObject.activeSelf,"Dragging closes hover/details");page.Pan.OnEndDrag(pointer);});
        steps.Enqueue(()=>{CaptureLcd("tree.png");});
        running=true;next=EditorApplication.timeSinceStartup+1.5;deadline=next+90;EditorApplication.update+=Tick;
    }
    static void ClickToolbar(int index){var buttons=Field<List<XgBtn>>(page,"toolbar");buttons[index].rt.GetComponent<Button>().onClick.Invoke();}
    static void CaptureLcd(string name)
    {
        var camera = page.Pan.viewport.GetComponentInParent<Canvas>().rootCanvas.worldCamera;
        var target = camera != null ? camera.targetTexture : null;
        if (target == null) throw new InvalidOperationException("Actual desktop LCD render target unavailable");
        var previous = RenderTexture.active;
        var texture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        try
        {
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); texture.Apply();
            File.WriteAllBytes(Path.Combine(output, name), texture.EncodeToPNG());
        }
        finally { RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(texture); }
    }
    static UnityEngine.EventSystems.PointerEventData Pointer()
    {
        var canvas=page.Pan.viewport.GetComponentInParent<Canvas>();var camera=canvas.rootCanvas.worldCamera;
        Vector2 screen=RectTransformUtility.WorldToScreenPoint(camera,page.Pan.viewport.TransformPoint(page.Pan.viewport.rect.center));
        var ray=page.Pan.viewport.GetComponentInParent<GraphicRaycaster>();
        return new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=screen,pointerCurrentRaycast=new UnityEngine.EventSystems.RaycastResult{module=ray},pointerPressRaycast=new UnityEngine.EventSystems.RaycastResult{module=ray}};
    }
    static Vector2 ContentPoint(){RectTransformUtility.ScreenPointToLocalPointInRectangle(page.Pan.content,pointer.position,pointer.pressEventCamera,out var p);return p;}
    static void CheckMeshes(bool en)
    {
        Canvas.ForceUpdateCanvases();Check(GameText.IsEnglish==en,"Actual UI locale matches requested language");int count=0;
        foreach(var v in Field<List<XgTechNodeView>>(page,"views"))
        {
            if(!v.gameObject.activeInHierarchy||!v.label.text.Contains("\n"))continue;
            var text=v.label;text.ForceMeshUpdate(true,true);var info=text.textInfo;var row=new TextResult{id=v.entry.members[0].id,language=en?"en":"zh",text=text.text,lines=info.lineCount,characters=info.characterCount,truncated=text.isTextTruncated,overflow=text.isTextOverflowing,width=text.rectTransform.rect.width,height=text.rectTransform.rect.height,preferredWidth=text.preferredWidth,preferredHeight=text.preferredHeight};labels.Add(row);count++;
            Check(info.lineCount==3&&!row.truncated&&!row.overflow,"Three rendered unclipped lines "+row.language+" "+row.id);
            bool secondaryStyle=true;
            for(int i=0;i<info.characterCount;i++){var c=info.characterInfo[i];if(!c.isVisible||c.lineNumber==0)continue;Color32 muted=XgDark.Muted;if(Mathf.Abs(c.pointSize-12)>.1f||c.color.r!=muted.r||c.color.g!=muted.g||c.color.b!=muted.b)secondaryStyle=false;}
            Check(secondaryStyle,"Second and third lines use 12px muted text "+row.language+" "+row.id);
            var rect=text.rectTransform.rect;bool contained=true;for(int i=0;i<info.characterCount;i++){var c=info.characterInfo[i];if(!c.isVisible)continue;if(c.bottomLeft.x<rect.xMin-.5f||c.topRight.x>rect.xMax+.5f||c.bottomLeft.y<rect.yMin-.5f||c.topRight.y>rect.yMax+.5f)contained=false;}
            Check(contained,"Glyph vertices within card bounds "+row.language+" "+row.id);
        }
        Check(count>20,"Checked more than 20 actual node meshes "+(en?"en":"zh"));
    }
    static void Tick(){if(!running)return;try{if(errors.Count>0)throw new Exception(errors[0]);if(!EditorApplication.isPlaying)throw new Exception("Play ended");double now=EditorApplication.timeSinceStartup;if(now>deadline)throw new TimeoutException();if(now<next)return;next=now+1.2;if(steps.Count==0){Finish(failed.Count==0?"passed":"failed","Actual TMP meshes, Unity Button handlers and PointerEventData handlers tested; pointer injection is not physical mouse automation.");return;}steps.Dequeue()();}catch(Exception e){Finish("failed",e.ToString());}}
    static void Finish(string status,string detail){running=false;locale?.Dispose();locale=null;EditorApplication.update-=Tick;File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(new Report{status=status,detail=detail,passed=passed,failed=failed,labels=labels,editorInfrastructureErrors=editorInfrastructureErrors},true));if(status=="passed")File.WriteAllText(Path.Combine(output,"passed.txt"),detail);Application.logMessageReceived-=Log;EditorApplication.ExitPlaymode();}
}
#endif
