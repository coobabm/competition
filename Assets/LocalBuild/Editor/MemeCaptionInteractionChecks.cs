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

// -executeMethod MemeCaptionInteractionChecks.Run, without -quit.
// This is a fully unlocked QA fixture, NOT evidence that a new player can buy every pack.
[InitializeOnLoad]
public static class MemeCaptionInteractionChecks
{
    const string Key="MemeCaptionInteractionChecks.Root";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static string Root=>SessionState.GetString(Key,"");
    static XingGuangController ctl;
    static ChapterOneRuntime runtime;
    static XgDeskGamePage desk;
    static int index=-1,phase,lastFrame=-1,frames;
    static double timeout;
    static bool finishing;
    static Report report;
    static XgTeachingPack[] Packs => new[]{XgTeachingPacks.Find("meme_word")};
    [Serializable] sealed class Entry { public string id,gameType,clicked="",note="";public bool built,visible;public int texts,images,buttons,answersBefore,answersAfter; }
    [Serializable] sealed class Report
    {
        public string scope="Main PlayMode, isolated save, explicit stage-six/all-research QA fixture; actual teach page and Unity Button handlers. At least build every pack. Clicking a chess source/puzzle setup button does not claim solving it.";
        public string status="running",failure="",unityVersion;
        public List<Entry> packs=new List<Entry>();public List<string> runtimeErrors=new List<string>();
        public List<string> editorInfrastructureErrors=new List<string>();
        public string desktopReadiness="";public List<string> checks=new List<string>();
    }
    static MemeCaptionInteractionChecks(){EditorApplication.playModeStateChanged+=State;}
    [MenuItem("Tools/Local Verification/Meme caption")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Start in Edit Mode");
        string dir=Path.GetFullPath(Path.Combine("LocalVerification","MemeCaption",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));
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
            index=-1;phase=frames=0;lastFrame=-1;finishing=false;timeout=EditorApplication.timeSinceStartup+180;
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
                index++;if(index>=Packs.Length){report.status="passed";Save();File.WriteAllText(Path.Combine(Root,"passed.txt"),"All "+Packs.Length+" built without exceptions; see per-pack actual interactions in report.json.");Finish();return;}
                ctl.Sim.S.unlocked.Remove("caption");
                var pack=Packs[index];Require(ctl.Sim.SelectTeachingPack(pack.id),"Fixture cannot select "+pack.id+": "+ctl.Sim.TeachingPackBlocker(pack));
                ctl.Open();ctl.View.Open("teach");ctl.View.Refresh(true);
                desk=typeof(XgLabelPage).GetField("deskGame",Flags)?.GetValue(ctl.View.Teach) as XgDeskGamePage;
                report.packs.Add(new Entry{id=pack.id,answersBefore=ctl.View.Teach.AnswerCount});frames=0;phase=2;return;
            }
            if(phase==2)
            {
                if(++frames<4)return;ValidatePeriodArt();var game=Game();var surface=Surface();int mood=(int)Field(game,"mood");
                Require(surface.GetComponentInChildren<TMPro.TMP_InputField>()==null,"Caption input unlocked too early");int choices=0;Button selected=null;
                foreach(var b in surface.GetComponentsInChildren<Button>())if(b.name.StartsWith("Choice")){
                    choices++;string label=b.GetComponentInChildren<TMPro.TMP_Text>().text;
                    Require(!label.Contains("手柄")&&!label.Contains("牡丹")&&!label.Contains("显卡"),"Object label still in emotion question");
                    if(label.EndsWith(XgTeachingMoods.Chinese[mood])||label.EndsWith(XgTeachingMoods.English[mood]))selected=b;
                }
                Require(choices==6&&selected!=null,"Six matching emotion labels absent");int before=ctl.Sim.MiniGameRecord("meme_word").correct;selected.onClick.Invoke();Require(ctl.Sim.MiniGameRecord("meme_word").correct==before+1,"Emotion choice not scored");
                ctl.Sim.S.unlocked.Add("caption");report.checks.Add("Six emotion choices before caption research, correct mood accepted");
                var atlas=new Texture2D(128*6,128,TextureFormat.RGBA32,false);for(int i=0;i<6;i++)atlas.SetPixels(i*128,0,128,128,XgTeachingPictures.Face(i).GetPixels());atlas.Apply();File.WriteAllBytes(Path.Combine(Root,"six-emotions.png"),atlas.EncodeToPNG());UnityEngine.Object.Destroy(atlas);
                frames=0;phase=3;Save();return;
            }
            if(phase==3)
            {
                if(++frames<4)return;var game=Game();var input=Surface().GetComponentInChildren<TMPro.TMP_InputField>();Require(input!=null,"Caption research did not unlock typed input");
                int before=ctl.Sim.MiniGameRecord("meme_word").correct,wrong=ctl.Sim.MiniGameRecord("meme_word").wrong;
                Press(Surface(),"SubmitCaption");Require(ctl.Sim.MiniGameRecord("meme_word").correct==before&&ctl.Sim.MiniGameRecord("meme_word").wrong==wrong,"Empty input consumed question");
                int mood=(int)Field(game,"mood");input.text=XgTeachingMoods.English[mood].ToUpperInvariant();Press(Surface(),"SubmitCaption");
                Require(ctl.Sim.MiniGameRecord("meme_word").correct==before+1,"English mood not accepted in Chinese UI");
                report.checks.Add("Caption research unlocks native text input, empty rejected without settlement, English case accepted");frames=0;phase=4;Save();return;
            }
            if(phase==4)
            {
                if(++frames<4)return;var input=Surface().GetComponentInChildren<TMPro.TMP_InputField>();input.text="猫";int wrong=ctl.Sim.MiniGameRecord("meme_word").wrong;Press(Surface(),"SubmitCaption");Require(ctl.Sim.MiniGameRecord("meme_word").wrong==wrong+1,"Wrong object caption accepted");
                var loaded=new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(ctl.Sim.S)));var note=loaded.S.teachingNotebook[loaded.S.teachingNotebook.Count-1];Require((note.teachingImageKind=="face"||note.teachingImageKind=="meme_couch_2016"||note.teachingImageKind=="meme_power_2016")&&note.answerText=="猫"&&!note.truth,"Typed answer/image missing after reload");Require(XgTeachingPictures.ForNote(note)!=null,"Saved illustration did not restore");
                report.checks.Add("Incorrect typed caption rejected; saved face and entered answer restored");report.status="passed";Save();File.WriteAllText(Path.Combine(Root,"passed.txt"),"Meme emotion choice, typed caption and save passed");Finish();return;
            }
        }
        catch(Exception e){report.status="failed";report.failure=e.ToString();Save();Debug.LogError("Teaching pack check failed: "+e);Finish();}
    }
    static void ValidatePeriodArt()
    {
        int[] moods={4,4,0,0};int[] dates={20160724,20160725,20160807,20160808};
        string[] kinds={"face","meme_couch_2016","face","meme_power_2016"};
        for(int i=0;i<dates.Length;i++){
            string kind=XgTeachingMoods.ImageKind(moods[i],dates[i]);Require(kind==kinds[i],"Period date boundary incorrect");
            var card=new XgCard{dataset="meme",teachingImageKind=kind,teachingImageKey=moods[i]};var texture=XgTeachingPictures.ForCard(card);Require(texture!=null,"Question image missing");
            if(kind!="face"){Require(texture==XgTeachingPictures.PeriodMeme(kind),"Period resource silently fell back");Require(texture.width==384&&texture.height==256,"Period illustration dimensions");}
            var note=JsonUtility.FromJson<XgTeachingNote>(JsonUtility.ToJson(new XgTeachingNote{dataset="meme",teachingImageKind=kind,teachingImageKey=moods[i]}));
            Require(XgTeachingPictures.ForNote(note)==texture,"Saved note did not restore the same actual texture");
        }
        var image=Surface().Find("QuestionImage").GetComponent<RawImage>();int currentMood=(int)Field(Game(),"mood");string currentKind=XgTeachingMoods.ImageKind(currentMood,ctl.Sim.Today);
        Require(image.texture==XgTeachingPictures.ForCard(new XgCard{dataset="meme",teachingImageKind=currentKind,teachingImageKey=currentMood}),"Live question did not show selected illustration");
        var size=image.rectTransform.rect.size;Require(Mathf.Abs(size.x/size.y-(currentKind=="face"?1f:1.5f))<.01f,"Question image stretched");
        report.checks.Add("Period illustrations: exact July25/Aug8 gates; actual resources384x256; restored note preserves image; live question native aspect");
        ScreenCapture.CaptureScreenshot(Path.Combine(Root,"meme-period-desktop.png"));
    }
    static object Game()=>typeof(XgDeskGamePage).GetField("game",Flags).GetValue(desk);
    static RectTransform Surface()=>typeof(XgDeskGamePage).GetField("surface",Flags).GetValue(desk) as RectTransform;
    static object Field(object obj,string name){for(Type t=obj.GetType();t!=null;t=t.BaseType){var f=t.GetField(name,Flags);if(f!=null)return f.GetValue(obj);}throw new Exception("Missing field "+name);}
    static Transform ActiveChild(RectTransform root,string name){foreach(Transform child in root)if(child.name==name&&child.gameObject.activeSelf)return child;return null;}
    static Button FindButton(RectTransform root,string name){foreach(var b in root.GetComponentsInChildren<Button>(true))if(b.name==name&&b.gameObject.activeInHierarchy)return b;return null;}
    static void Press(RectTransform root,string name){var b=FindButton(root,name);Require(b!=null&&b.IsActive()&&b.IsInteractable(),"Button unavailable "+name);b.onClick.Invoke();}
    static void Finish(){finishing=true;Application.logMessageReceived-=Log;EditorApplication.update-=Tick;EditorApplication.ExitPlaymode();}
}
#endif
