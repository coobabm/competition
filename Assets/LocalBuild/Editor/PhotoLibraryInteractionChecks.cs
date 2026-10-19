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

// -executeMethod PhotoLibraryInteractionChecks.Run, without -quit.
// This is a fully unlocked QA fixture, NOT evidence that a new player can buy every pack.
[InitializeOnLoad]
public static class PhotoLibraryInteractionChecks
{
    const string Key="PhotoLibraryInteractionChecks.Root";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static string Root=>SessionState.GetString(Key,"");
    static XingGuangController ctl;
    static ChapterOneRuntime runtime;
    static XgDeskGamePage desk;
    static int index=-1,phase,lastFrame=-1,frames;
    static double timeout;
    static bool finishing;
    static Report report;
    static XgTeachingPack[] Packs => new[]{XgTeachingPacks.Find("stage_photo"),XgTeachingPacks.Find("captcha")};
    [Serializable] sealed class Entry { public string id,gameType,clicked="",note="";public bool built,visible;public int texts,images,buttons,answersBefore,answersAfter; }
    [Serializable] sealed class Report
    {
        public string scope="Main PlayMode, isolated save, explicit stage-six/all-research QA fixture; actual teach page and Unity Button handlers. At least build every pack. Exercises digit choices and captcha on-screen keyboard, backspace to empty, correct and incorrect submissions, grid alternation, and serialized notebook image reconstruction. Physical hardware keyboard remains manual. All 80 source assets rendered and checked for distinct pixels; ImageNet purchase/stage gating checked.";
        public string status="running",failure="",unityVersion;
        public List<Entry> packs=new List<Entry>();public List<string> runtimeErrors=new List<string>();
        public List<string> editorInfrastructureErrors=new List<string>();
        public string desktopReadiness="";public List<string> checks=new List<string>();
    }
    static PhotoLibraryInteractionChecks(){EditorApplication.playModeStateChanged+=State;}
    [MenuItem("Tools/Local Verification/Real photo library interactions")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Start in Edit Mode");
        string dir=Path.GetFullPath(Path.Combine("LocalVerification","PhotoLibrary",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));
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
                lab.S.stage=3;Require(!lab.TeachingPackAvailable("stage_photo"),"Photo desk must require ImageNet");lab.S.owned.Add("imagenet");Require(lab.TeachingPackAvailable("stage_photo"),"Owned ImageNet unlocks photo desk");lab.S.stage=2;Require(!lab.TeachingPackAvailable("stage_photo"),"Photo desk cannot open before stage three");lab.S.stage=6;
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
                Require(XgTeachingPacks.All.Length==19,"Expected manifest's 19 packs");frames=0;phase=10;return;
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
                CheckLibrary();ctl.Open();ctl.View.Open("teach");phase=1;return;
            }
            if(phase==1)
            {
                index++;if(index>=Packs.Length){report.status="passed";Save();File.WriteAllText(Path.Combine(Root,"passed.txt"),"All "+Packs.Length+" built without exceptions; see per-pack actual interactions in report.json.");Finish();return;}
                var pack=Packs[index];Require(ctl.Sim.SelectTeachingPack(pack.id),"Fixture cannot select "+pack.id+": "+ctl.Sim.TeachingPackBlocker(pack));
                ctl.Open();ctl.View.Open("teach");ctl.View.Refresh(true);
                desk=typeof(XgLabelPage).GetField("deskGame",Flags)?.GetValue(ctl.View.Teach) as XgDeskGamePage;
                report.packs.Add(new Entry{id=pack.id,answersBefore=ctl.View.Teach.AnswerCount});frames=0;phase=2;return;
            }
            if(phase==2)
            {
                if(++frames<4)return;var row=report.packs[index];
                Require(desk!=null&&desk.Playing&&desk.SelectedGame==row.id,"Actual teaching page did not construct "+row.id);
                var game=typeof(XgDeskGamePage).GetField("game",Flags)?.GetValue(desk);var surface=typeof(XgDeskGamePage).GetField("surface",Flags)?.GetValue(desk) as RectTransform;
                Require(game!=null&&surface!=null,"Game surface missing");row.gameType=game.GetType().Name;row.built=true;row.visible=desk.Visible;Require(row.visible,"Game not visible");
                row.texts=surface.GetComponentsInChildren<TMPro.TMP_Text>().Length;row.images=surface.GetComponentsInChildren<RawImage>().Length;var buttons=surface.GetComponentsInChildren<Button>();row.buttons=buttons.Length;
                Require(row.texts>0,"Question text missing");
                if(index==0)CheckDigit(surface,true);else CheckCaptcha(surface,true);
                frames=0;phase=3;Save();return;
            }
            if(phase==3)
            {
                if(++frames<4)return;
                var surface=Surface();
                if(index==0)CheckDigit(surface,false);else CheckGrid(surface);
                frames=0;phase=4;Save();return;
            }
            if(phase==4)
            {
                if(++frames<4)return;
                if(index==1){CheckCaptcha(Surface(),false);frames=0;phase=5;return;}
                report.packs[index].answersAfter=ctl.View.Teach.AnswerCount;Save();phase=1;
            }
            if(phase==5)
            {
                if(++frames<4)return;
                Require(Surface().Find("Cell0")!=null,"Wrong captcha must also advance to grid");
                report.checks.Add("Incorrect captcha advances to grid");report.packs[index].answersAfter=ctl.View.Teach.AnswerCount;Save();phase=1;
            }
        }
        catch(Exception e){report.status="failed";report.failure=e.ToString();Save();Debug.LogError("Teaching pack check failed: "+e);Finish();}
    }
    static RectTransform Surface()=>typeof(XgDeskGamePage).GetField("surface",Flags).GetValue(desk) as RectTransform;
    static object Game()=>typeof(XgDeskGamePage).GetField("game",Flags).GetValue(desk);
    static object Field(string name){for(Type t=Game().GetType();t!=null;t=t.BaseType){var f=t.GetField(name,Flags);if(f!=null)return f.GetValue(Game());}throw new Exception("Missing field "+name);}
    static void Press(RectTransform surface,string name)
    {
        var node=surface.Find(name);Require(node!=null,"Missing "+name);var b=node.GetComponent<Button>();
        Require(b!=null&&b.IsActive()&&b.IsInteractable(),"Disabled "+name);b.onClick.Invoke();
    }
    static void CheckLibrary()
    {
        var sheet=new Texture2D(960,768,TextureFormat.RGBA32,false);
        var fingerprints=new HashSet<string>();
        for(int i=0;i<80;i++)
        {
            var photo=XgTeachingPictures.LibraryPhoto(i);Require(photo!=null&&photo.width==192&&photo.height==192,"Missing library photo "+i);
            byte[] bytes=photo.EncodeToPNG();string hash;using(var sha=System.Security.Cryptography.SHA256.Create())hash=Convert.ToBase64String(sha.ComputeHash(bytes));
            Require(fingerprints.Add(hash),"Duplicate/blank rendered photo "+i);
            var note=new XgTeachingNote{dataset="imagenet",teachingImageKind="photo80",teachingImageKey=i};
            Require(XgTeachingPictures.ForNote(JsonUtility.FromJson<XgTeachingNote>(JsonUtility.ToJson(note)))==photo,"Photo note index roundtrip "+i);
            int col=i%10,row=i/10;for(int y=0;y<96;y++)for(int x=0;x<96;x++)sheet.SetPixel(col*96+x,(7-row)*96+y,photo.GetPixelBilinear((x+.5f)/96,(y+.5f)/96));
        }
        sheet.Apply();File.WriteAllBytes(Path.Combine(Root,"all-80-rendered.png"),sheet.EncodeToPNG());UnityEngine.Object.Destroy(sheet);
        // Old saved image kinds retain their old category mapping.
        for(int i=0;i<6;i++)Require(XgTeachingPictures.ForNote(new XgTeachingNote{teachingImageKind="photo",teachingImageKey=i})==XgTeachingPictures.Photo(i),"Legacy photo mapping "+i);
        report.checks.Add("80 resources readable and distinct; all notebook indices roundtrip; six legacy mappings preserved");
    }
    static void CheckDigit(RectTransform surface,bool correct)
    {
        var card=(XgCard)Field("QuestionCard");Require(card.teachingImageKind=="photo80","Expected actual photograph");
        var raw=surface.Find("QuestionImage").GetComponent<RawImage>();
        Require(raw.texture!=null&&raw.rectTransform.rect.width==200&&raw.rectTransform.rect.height==200,"Photo visible without distortion");
        Button choice=null;foreach(var b in surface.GetComponentsInChildren<Button>())if(b.name.StartsWith("Choice"))
        {string text=b.GetComponentInChildren<TMPro.TMP_Text>().text;int category=card.teachingImageKey/10;bool matches=text.EndsWith(". "+XgUi.T(XgTeachingPictures.LibraryWords[category],XgTeachingPictures.LibraryWordsEn[category]));if(matches==correct){choice=b;break;}}
        Require(choice!=null,"Digit answer button");int n=ctl.Sim.S.teachingNotebook.Count;choice.onClick.Invoke();CheckNote(n,correct,raw.texture as Texture2D,"photo-"+correct);
        report.checks.Add("Actual photo choice "+(correct?"correct":"incorrect")+"; original image survives save");
    }
    static void CheckCaptcha(RectTransform surface,bool correct)
    {
        Require(surface.Find("CaptchaImage")!=null,"Expected text captcha after grid");
        var raw=surface.Find("CaptchaImage").GetComponent<RawImage>();Require(raw.texture.width==256&&raw.texture.height==80,"Native captcha size");
        string code=(string)Field("captchaCode");int n=ctl.Sim.S.teachingNotebook.Count;
        Press(surface,"SubmitCode");Require(ctl.Sim.S.teachingNotebook.Count==n,"Empty input must not submit");
        foreach(char c in code)Press(surface,"Type"+c);
        Press(surface,"Type"+code[0]);Require((string)Field("captchaInput")==code,"Four-character input cap");
        Press(surface,"Backspace");Require(((string)Field("captchaInput")).Length==3,"Backspace removes one");
        for(int i=0;i<4;i++)Press(surface,"Backspace");Require((string)Field("captchaInput")=="","Backspace clears safely to empty");
        string answer=correct?code:(code[0]=='2'?"3":"2")+code.Substring(1);
        foreach(char c in answer)Press(surface,"Type"+c);
        Press(surface,"SubmitCode");CheckNote(n,correct,raw.texture as Texture2D,"captcha-"+correct);
        Press(surface,"SubmitCode");Require(ctl.Sim.S.teachingNotebook.Count==n+1,"Duplicate submit ignored");
        report.checks.Add("Captcha on-screen input, cap, backspace, clear, "+(correct?"correct":"incorrect")+", duplicate guard and save image");
    }
    static void CheckGrid(RectTransform surface)
    {
        Require(surface.Find("Cell0")!=null,"Captcha alternates to grid");int[] items=(int[])Field("items");int target=(int)Field("target");
        for(int i=0;i<9;i++)if(items[i]==target)Press(surface,"Cell"+i);
        int n=ctl.Sim.S.teachingNotebook.Count;Press(surface,"Submit");Require(ctl.Sim.S.teachingNotebook.Count==n+1&&ctl.Sim.S.teachingNotebook[n].truth,"Real grid selection correct");
        report.checks.Add("Text captcha alternates to nine-photo grid; selected exact matching cells");
    }
    static void CheckNote(int before,bool correct,Texture2D original,string file)
    {
        Require(ctl.Sim.S.teachingNotebook.Count==before+1,"Exactly one notebook entry");
        var note=ctl.Sim.S.teachingNotebook[before];Require(note.truth==correct,"Verdict matches submitted choice");
        string json=JsonUtility.ToJson(note);File.WriteAllText(Path.Combine(Root,file+".json"),json);
        var restored=JsonUtility.FromJson<XgTeachingNote>(json);var image=XgTeachingPictures.ForNote(restored);
        Require(image!=null&&original!=null&&image.width==original.width&&image.height==original.height,"Saved image dimensions");
        var a=image.GetPixels32();var b=original.GetPixels32();Require(a.Length==b.Length,"Saved image pixels");
        for(int i=0;i<a.Length;i++)Require(a[i].Equals(b[i]),"Saved image pixel mismatch at "+i);
        File.WriteAllBytes(Path.Combine(Root,file+".png"),image.EncodeToPNG());
    }
    static void Finish(){finishing=true;Application.logMessageReceived-=Log;EditorApplication.update-=Tick;EditorApplication.ExitPlaymode();}
}
#endif
