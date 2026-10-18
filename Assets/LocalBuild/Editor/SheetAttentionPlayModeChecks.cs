#if UNITY_EDITOR
using System;
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

// -executeMethod SheetAttentionPlayModeChecks.Run, without -quit.
// This is a fully unlocked QA fixture, NOT evidence that a new player can buy every pack.
[InitializeOnLoad]
public static class SheetAttentionPlayModeChecks
{
    const string Key="SheetAttentionPlayModeChecks.Root";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static string Root=>SessionState.GetString(Key,"");
    static XingGuangController ctl;
    static ChapterOneRuntime runtime;
    static XgSheetAttentionLook view; static IDisposable locale; static string language="zh-CN";
    static string hiddenSignature;static int hiddenCache;static Texture original;
    static int index=-1,phase,lastFrame=-1,frames;
    static double timeout;
    static bool finishing;
    static Report report;
    [Serializable] sealed class Metric { public string name,text;public float value,limit;public bool passed; }
    [Serializable] sealed class Report
    {
        public string scope="Isolated-save Unity PlayMode QA fixture. Actual train-page rows and Button handlers, original texture identity/pixels, 49 heat cells, architecture refresh, parent-hidden update guard and English TMP geometry. Owned vision architectures are restricted explicitly to MLP, then LeNet; the real automatic model selection performs the switch. No claim of natural progression.";
        public string status="running",failure="",unityVersion,desktopReadiness="";
        public List<Metric> metrics=new List<Metric>();public List<string> runtimeErrors=new List<string>(),editorInfrastructureErrors=new List<string>();
    }
    static SheetAttentionPlayModeChecks(){EditorApplication.playModeStateChanged+=State;}
    [MenuItem("Tools/Local Verification/Sheet original images and attention")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Start in Edit Mode");
        string dir=Path.GetFullPath(Path.Combine("LocalVerification","SheetAttention",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));
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
            index=-1;phase=frames=0;lastFrame=-1;finishing=false;timeout=EditorApplication.timeSinceStartup+180;
            report=new Report{unityVersion=Application.unityVersion};Application.logMessageReceived+=Log;EditorApplication.update+=Tick;
        }
        if(state==PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.update-=Tick;Application.logMessageReceived-=Log;SceneManager.sceneLoaded-=DisableModel;
            string path=Root;bool ok=File.Exists(Path.Combine(path,"passed.txt"));SessionState.EraseString(Key);ChapterOneRuntime.VerificationSaveDirectory=null;locale?.Dispose();locale=null;
            Debug.Log("Sheet attention "+(ok?"PASS":"FAIL/INTERRUPTED")+": "+path);if(Application.isBatchMode)EditorApplication.Exit(ok?0:1);
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
                var lab=new XgSim();lab.S.teachingFlow=false;lab.S.labels.Add(new XgLabelCount{dataset="mnist",count=1000});lab.S.owned.Add("mnist");lab.S.desksOpen.Add("mnist");lab.S.stage=6;lab.S.firstSpecialty="vision";lab.S.handCorrect=200;lab.S.epochs=20;
                foreach(var node in XgCatalog.Nodes)if(!lab.S.unlocked.Contains(node.id))lab.S.unlocked.Add(node.id);
                foreach(var pack in XgTeachingPacks.All){if(!lab.S.unlocked.Contains(pack.NodeId))lab.S.unlocked.Add(pack.NodeId);if(!lab.S.owned.Contains(pack.dataset))lab.S.owned.Add(pack.dataset);if(!lab.S.desksOpen.Contains(pack.dataset))lab.S.desksOpen.Add(pack.dataset);}
                LimitFixtureVisionArchitecture(lab,"mlp");
                var house=new ChapterOneSim();house.S.prologue=2;house.S.appInstallState=2;house.S.money=1000000;house.S.labState=JsonUtility.ToJson(lab.S);
                GameCalendar.Advance(house.S,GameCalendar.DayFor(6,lab.MonthProgress));house.S.rentDay=GameCalendar.CurrentDay(house.S);house.S.broadbandMonth=201612;
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
                ctl.Open();ctl.View.Open("train");phase=1;return;
            }
            if(phase==1)
            {
                locale=GameText.Bind(()=>language);ctl.Sim.SelectedTrack=XgTrack.Vision;
                var run=ctl.Sim.S.vision;run.dataset="mnist";SelectFixtureVisionArchitecture("mlp");run.epoch=1;run.sheetKey="mnist|"+ctl.Sim.S.stage;
                run.sheet=new List<XgSheetItem>();
                for(int i=0;i<4;i++)
                {
                    var card=new XgCard{dataset=i==1?"cifar":"mnist",digit=3+i,asked=3+i,seed=773+i,truth=true,
                        question=i==1?"这张照片是猫吗？":"这是数字 "+(3+i)+" 吗？",questionEn=i==1?"Is this a cat?":"Is this digit "+(3+i)+"?",
                        teachingImageKind=i==1?"photo":"",teachingImageKey=0};
                    run.sheet.Add(new XgSheetItem{hand=true,card=card,dataset=card.dataset,seed=card.seed,right=i%2==0});
                }
                ctl.View.Refresh(true);frames=0;phase=2;return;
            }
            if(phase==2)
            {
                if(++frames<24)return;Canvas.ForceUpdateCanvases();
                var sheet=typeof(XgTrainPage).GetField("sheetArea",Flags).GetValue(ctl.View.Train) as RectTransform;
                Require(sheet!=null&&sheet.gameObject.activeInHierarchy,"Actual sheet is hidden");
                for(int i=0;i<4;i++)
                {
                    var row=sheet.Find("RowsViewport/Rows/Question"+(i+1));Require(row!=null,"Missing sheet row");
                    var thumb=row.Find("OriginalThumbnail").GetComponent<RawImage>();
                    var expected=XgTeachingPictures.ForCard(ctl.Sim.SheetCard(ctl.Sim.S.vision.sheet[i]).source);
                    Measure("thumbnail "+i+" width",thumb.rectTransform.rect.width,48,Math.Abs(thumb.rectTransform.rect.width-48)<.01f);
                    Measure("thumbnail "+i+" height",thumb.rectTransform.rect.height,48,Math.Abs(thumb.rectTransform.rect.height-48)<.01f);
                    Measure("thumbnail "+i+" inherited alpha",thumb.canvasRenderer.GetInheritedAlpha(),.01f,thumb.canvasRenderer.GetInheritedAlpha()>.01f);
                    Measure("thumbnail "+i+" original identity",0,0,expected!=null&&ReferenceEquals(thumb.texture,expected)&&thumb.gameObject.activeInHierarchy);
                    BoundsInside(thumb.rectTransform,row as RectTransform,"thumbnail "+i+" contained in row");
                }
                original=XgTeachingPictures.ForCard(ctl.Sim.S.vision.sheet[0].card);
                sheet.Find("RowsViewport/Rows/Question1").GetComponent<Button>().onClick.Invoke();
                view=typeof(XgTrainPage).GetField("look",Flags).GetValue(ctl.View.Train) as XgSheetAttentionLook;
                Require(view!=null&&view.isActiveAndEnabled,"Row Button did not open attention viewer");frames=0;phase=3;return;
            }
            if(phase==3)
            {
                if(++frames<3)return;Canvas.ForceUpdateCanvases();CheckOverlay(false);var item=ctl.Sim.S.vision.sheet[0];
                var source=Field<RawImage>(view,"source");var texture=(Texture2D)source.texture;int hash=Pixels(texture);
                item.right=!item.right;view.Sync(ctl.Sim,ctl.Sim.S.vision.sheet);
                Measure("answer flip keeps original texture",hash,hash,ReferenceEquals(source.texture,original)&&Pixels((Texture2D)source.texture)==hash);
                language=GameText.English;GameText.PublishChanged();ctl.View.Refresh(true);frames=0;phase=4;return;
            }
            if(phase==4)
            {
                if(++frames<3)return;Canvas.ForceUpdateCanvases();CheckOverlay(true);
                hiddenSignature=Field<string>(view,"signature");hiddenCache=CacheCount();ctl.View.Open("teach");
                Require(!view.isActiveAndEnabled,"Parent page did not hide viewer");SelectFixtureVisionArchitecture("lenet");
                frames=0;phase=5;return;
            }
            if(phase==5)
            {
                view.Sync(ctl.Sim,ctl.Sim.S.vision.sheet);if(++frames<8)return;
                Measure("hidden parent retains signature",0,0,Field<string>(view,"signature")==hiddenSignature);
                Measure("hidden parent creates no occlusion cache",CacheCount(),hiddenCache,CacheCount()==hiddenCache);
                ctl.View.Open("train");ctl.View.Refresh(true);frames=0;phase=6;return;
            }
            if(phase==6)
            {
                if(++frames<3)return;
                Require(view.isActiveAndEnabled,"Train page did not restore viewer");
                Measure("visible architecture refresh",0,0,Field<string>(view,"signature")!=hiddenSignature);
                var card=ctl.Sim.S.vision.sheet[0].card;var a=XgSheetOcclusion.For(card,"mlp");var b=XgSheetOcclusion.For(card,"lenet");double delta=0;
                for(int i=0;i<49;i++)delta+=Math.Abs(a.change[i]-b.change[i]);Measure("MLP/CNN heat difference",(float)delta,0,delta>1e-8);
                view.Hide();var sheet=typeof(XgTrainPage).GetField("sheetArea",Flags).GetValue(ctl.View.Train) as RectTransform;
                sheet.Find("RowsViewport/Rows/Question2").GetComponent<Button>().onClick.Invoke();frames=0;phase=7;return;
            }
            if(phase==7)
            {
                if(++frames<3)return;var photo=XgTeachingPictures.ForCard(ctl.Sim.S.vision.sheet[1].card);
                Measure("photo stays original",0,0,photo!=null&&ReferenceEquals(Field<RawImage>(view,"source").texture,photo));
                float alpha=0;foreach(var cell in Field<Image[]>(view,"cells"))alpha+=cell.color.a;
                Measure("photo has no invented heat",alpha,0,alpha==0);
                var caption=Field<TMPro.TMP_Text>(view,"caption");Measure("photo explains missing classifier",0,0,caption.text.Contains("No matching demonstration classifier"));
                Save();
                report.status="passed";Save();File.WriteAllText(Path.Combine(Root,"passed.txt"),"Actual UI and numerical assertions passed; see report.json.");Finish();return;
            }
        }
        catch(Exception e){report.status="failed";report.failure=e.ToString();Save();Debug.LogError("Sheet attention check failed: "+e);Finish();}
    }
    // This QA setup edits research ownership, never model output. AutoConfigure is the same
    // production selector called between rounds; it stays free to select its legal best model.
    static void LimitFixtureVisionArchitecture(XgSim sim,string desired)
    {
        foreach(var arch in XgCatalog.Archs)
            if(XgSim.ArchitectureFits(arch,XgTrack.Vision)&&arch.id!=desired)sim.S.unlocked.Remove(arch.id);
        if(!sim.S.unlocked.Contains(desired))sim.S.unlocked.Add(desired);
    }
    static void SelectFixtureVisionArchitecture(string desired)
    {
        LimitFixtureVisionArchitecture(ctl.Sim,desired);
        typeof(XgRun).GetField("autoKey",Flags).SetValue(ctl.Sim.S.vision,"");
        ctl.Sim.AutoConfigure(ctl.Sim.S.vision,ctl.Host);
        Require(ctl.Sim.S.vision.arch==desired,"Legal automatic architecture selection: expected "+desired+", got "+ctl.Sim.S.vision.arch);
    }
    static T Field<T>(object value,string name)=>(T)value.GetType().GetField(name,Flags).GetValue(value);
    static int CacheCount()=>((System.Collections.IDictionary)typeof(XgSheetOcclusion).GetField("Cache",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null)).Count;
    static void Measure(string name,float value,float limit,bool passed,string text="")
    {
        report.metrics.Add(new Metric{name=name,value=value,limit=limit,passed=passed,text=text});Save();Require(passed,name+": value="+value+", limit="+limit);
    }
    static int Pixels(Texture2D texture){unchecked{int hash=17;foreach(var p in texture.GetPixels32())hash=hash*31+p.GetHashCode();return hash;}}
    static void BoundsInside(RectTransform child,RectTransform parent,string name)
    {
        var corners=new Vector3[4];child.GetWorldCorners(corners);bool inside=true;
        foreach(var corner in corners){var p=parent.InverseTransformPoint(corner);if(p.x<parent.rect.xMin-.5f||p.x>parent.rect.xMax+.5f||p.y<parent.rect.yMin-.5f||p.y>parent.rect.yMax+.5f)inside=false;}
        Measure(name,child.rect.height,parent.rect.height,inside);
    }
    static void CheckOverlay(bool english)
    {
        var source=Field<RawImage>(view,"source");Measure("viewer inherited alpha",source.canvasRenderer.GetInheritedAlpha(),.01f,source.canvasRenderer.GetInheritedAlpha()>.01f);Measure("viewer original image identity",0,0,ReferenceEquals(source.texture,original));
        var item=view.Item;var card=item==null?null:ctl.Sim.SheetCard(item)?.source;
        var arch=ctl.Sim.S.vision.arch;var result=XgSheetOcclusion.For(card,arch);double maxDelta=0;
        if(result!=null)foreach(var d in result.change)maxDelta=Math.Max(maxDelta,Math.Abs(d));
        string diagnosis="arch="+arch+", supports="+XgSheetOcclusion.Supports(card,arch)+", dataset="+card?.dataset+", seed="+card?.seed+", digit="+card?.digit+", asked="+card?.asked+", caption="+Field<TMPro.TMP_Text>(view,"caption").text;
        report.metrics.Add(new Metric{name="heat diagnostic",text=diagnosis,value=(float)maxDelta,passed=result!=null});Save();
        var cells=Field<Image[]>(view,"cells");int colored=0;float alpha=0;foreach(var cell in cells){if(cell.color.a>0)colored++;alpha+=cell.color.a;}
        Measure("49 occlusion cells",cells.Length,49,cells.Length==49);Measure("nonblank measured heat",colored,0,colored>0);Measure("total heat opacity",alpha,0,alpha>0);
        foreach(string field in new[]{"title","caption"})
        {
            var label=Field<TMPro.TMP_Text>(view,field);label.ForceMeshUpdate(true,true);var size=label.GetPreferredValues(label.text,label.rectTransform.rect.width,float.PositiveInfinity);
            Measure((english?"English ":"Chinese ")+field+" preferred height",size.y,label.rectTransform.rect.height,size.y<=label.rectTransform.rect.height+.5f,label.text);
            if(english)Measure("English "+field+" language",0,0,!System.Text.RegularExpressions.Regex.IsMatch(label.text,"[\u4e00-\u9fff]"),label.text);
            BoundsInside(label.rectTransform,view.transform as RectTransform,field+" inside viewer");
            var imageBounds=RectTransformUtility.CalculateRelativeRectTransformBounds(view.transform,source.rectTransform);
            var textBounds=RectTransformUtility.CalculateRelativeRectTransformBounds(view.transform,label.rectTransform);
            float overlapX=Mathf.Max(0,Mathf.Min(imageBounds.max.x,textBounds.max.x)-Mathf.Max(imageBounds.min.x,textBounds.min.x));
            float overlapY=Mathf.Max(0,Mathf.Min(imageBounds.max.y,textBounds.max.y)-Mathf.Max(imageBounds.min.y,textBounds.min.y));
            Measure(field+" does not cover original image",overlapX*overlapY,0,overlapX*overlapY<.5f);
        }
        BoundsInside(source.rectTransform,view.transform as RectTransform,"original image inside viewer");

    }
    static void Finish(){locale?.Dispose();locale=null;finishing=true;Application.logMessageReceived-=Log;EditorApplication.update-=Tick;EditorApplication.ExitPlaymode();}
}
#endif
