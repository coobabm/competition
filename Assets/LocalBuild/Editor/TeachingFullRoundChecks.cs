#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
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
using TMPro;

// -executeMethod TeachingFullRoundChecks.Run, without -quit.
// This is an explicitly unlocked/installed QA fixture, NOT evidence of player purchase, download or installation.
[InitializeOnLoad]
public static partial class TeachingFullRoundChecks
{
    const string Key="TeachingFullRoundChecks.Root";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static string Root=>SessionState.GetString(Key,"");
    static XingGuangController ctl;
    static ChapterOneRuntime runtime;
    static XgDeskGamePage desk;
    static int index=-1,phase,lastFrame=-1,frames;
    static double timeout;
    static bool finishing;
    static IDisposable locale;static bool english;
    static FeedbackSnapshot pendingFeedback;
    static int unknownFeedback,knownDisagreement;
    sealed class FeedbackSnapshot { public object game;public bool playerCorrect,visible;public bool? aiCorrect;public string aiAnswer,playerText,aiText; }
    static Report report;
    static int correctBefore,wrongBefore,samplesBefore;static double incomeBefore;
    static readonly string[] CoveredPackIds = { "spam_swipe","arith_mole","minesweeper_yn","make24","headline_swipe","odd_one","sequence","danmu_mood","mnist_write","chess_mate1","gomoku_win","captcha","meme_word","poem_next","idiom_chain","chess_mate2","spy","review_swipe","chinglish" };
    // Sudoku has completion-only settlement; it requires its separate interaction checks.
    static XgTeachingPack[] Packs => CoveredPackIds.Select(id => XgTeachingPacks.All.Single(p => p.id == id)).ToArray();
    [Serializable] sealed class Entry { public string id,language,gameType,clicked="",note="";public bool built,visible;public int texts,images,buttons,answersBefore,answersAfter; }
    [Serializable] sealed class Report
    {
        public string scope="Main PlayMode, isolated save, explicit stage-six/all-research/all-paid-teaching-nodes-installed QA fixture (installation receipts are fixture state, not acquisition coverage); the explicitly listed 19 pre-Sudoku packs in Chinese and English (Sudoku excluded and covered by separate checks) through existing Unity Button callbacks and explicit action-game ticks, not LCD or physical pointer coverage. Correct/wrong settlement and JsonUtility record reload; pre-submit displayed AI prediction, retained two-line footer after natural next-frame rebuild, unknown gray and rendered text/mask bounds. Hidden truth selects deterministic QA answers and independently checks supported prediction types. Other prediction verdicts are snapshot-propagation checks, not model accuracy claims. Actual current window size, not a forced small-window matrix.";
        public string status="running",failure="",unityVersion;
        public List<Entry> packs=new List<Entry>();public List<string> runtimeErrors=new List<string>();
        public List<string> editorInfrastructureErrors=new List<string>();
        public string desktopReadiness="";public List<string> checks=new List<string>();
        public List<string> screenshots=new List<string>();
    }
    static TeachingFullRoundChecks(){EditorApplication.playModeStateChanged+=State;}
    [MenuItem("Tools/Local Verification/Teaching full rounds")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Start in Edit Mode");
        string dir=Path.GetFullPath(Path.Combine("LocalVerification","TeachingFullRounds",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));
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
            index=-1;phase=frames=0;lastFrame=-1;finishing=false;timeout=EditorApplication.timeSinceStartup+1200;
            english=false;unknownFeedback=knownDisagreement=0;pendingFeedback=null;
            report=new Report{unityVersion=Application.unityVersion};Application.logMessageReceived+=Log;EditorApplication.update+=Tick;
        }
        if(state==PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.update-=Tick;Application.logMessageReceived-=Log;SceneManager.sceneLoaded-=DisableModel;locale?.Dispose();locale=null;
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
                foreach(var pack in XgTeachingPacks.All)
                {
                    if(!lab.S.unlocked.Contains(pack.NodeId))lab.S.unlocked.Add(pack.NodeId);
                    if(!lab.S.owned.Contains(pack.dataset))lab.S.owned.Add(pack.dataset);
                    if(!lab.S.desksOpen.Contains(pack.dataset))lab.S.desksOpen.Add(pack.dataset);
                    // Explicit all-packs fixture: ownership and installation are separate prerequisites.
                    // Seed the same receipt read by the production gate; do not bypass SelectTeachingPack.
                    if(pack.buy&&!lab.S.installedTeachingNodes.Contains(pack.NodeId))lab.S.installedTeachingNodes.Add(pack.NodeId);
                }
                var house=new ChapterOneSim();house.S.prologue=2;house.S.appInstallState=2;house.S.money=1000000;house.S.labState=JsonUtility.ToJson(lab.S);
                GameCalendar.Advance(house.S,GameCalendar.DayFor(6,lab.MonthProgress));house.S.rentDay=GameCalendar.CurrentDay(house.S);house.S.broadbandMonth=201612;
                runtime.SetSimulationForTests(house);typeof(XingGuangController).GetMethod("BindSave",Flags).Invoke(ctl,new object[]{house});
                foreach(var pack in XgTeachingPacks.All)
                {
                    Require(!pack.buy||ctl.Sim.TeachingNodeInstalled(pack.NodeId),"Installed teaching fixture receipt lost after bind: "+pack.id);
                    Require(ctl.Sim.TeachingPackAvailable(pack.id),"Installed all-packs fixture blocked after bind: "+pack.id+": "+ctl.Sim.TeachingPackBlocker(pack));
                }
                report.checks.Add("Explicit QA installation receipts survived save binding; all "+Packs.Length+" teaching packs pass the unchanged production availability gate. Purchase/download/install UI is not exercised by this fixture.");
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
                locale=GameText.Bind(()=>english?GameText.English:GameText.Chinese);ctl.Sim.English=english;GameText.PublishChanged();
                ctl.Open();ctl.View.Open("teach");phase=1;return;
            }
            if(phase==1)
            {
                index++;if(index>=Packs.Length){
                    Require(unknownFeedback>0,"No actual unknown prediction observed; gray feedback remains unverified");
                    report.checks.Add((english?"en":"zh")+": unknown feedback="+unknownFeedback+", known AI/player disagreement="+knownDisagreement+(knownDisagreement==0?" (known opposite outcomes not observed; not claimed)":""));
                    if(!english){english=true;index=-1;unknownFeedback=knownDisagreement=0;ctl.Sim.English=true;GameText.PublishChanged();return;}
                    report.status="passed";Save();File.WriteAllText(Path.Combine(Root,"passed.txt"),"All "+Packs.Length+" packs in both languages completed correct/wrong settlements and record reload; see exact feedback coverage in report.json.");Finish();return;}
                Require(GameText.IsEnglish==english&&ctl.Sim.English==english,"Requested teaching language must be active");
                var pack=Packs[index];Require(ctl.Sim.SelectTeachingPack(pack.id),"Fixture cannot select "+pack.id+": "+ctl.Sim.TeachingPackBlocker(pack));
                ctl.Open();ctl.View.Open("teach");ctl.View.Refresh(true);
                desk=typeof(XgLabelPage).GetField("deskGame",Flags)?.GetValue(ctl.View.Teach) as XgDeskGamePage;
                report.packs.Add(new Entry{id=pack.id,language=english?"en":"zh",answersBefore=ctl.View.Teach.AnswerCount});frames=0;phase=2;return;
            }
            if(phase==2)
            {
                if(++frames<4)return;var game=Game();Require(game!=null,"Missing game "+Packs[index].id);
                var row=report.packs.Last();row.gameType=game.GetType().Name;row.built=true;row.visible=desk.Visible;
                Require(row.visible&&desk.Playing&&desk.SelectedGame==Packs[index].id,"Wrong or hidden game surface");
                row.texts=Surface().GetComponentsInChildren<TMPro.TMP_Text>().Length;row.images=Surface().GetComponentsInChildren<RawImage>().Length;row.buttons=Surface().GetComponentsInChildren<Button>().Length;
                var r=ctl.Sim.MiniGameRecord(Packs[index].id);correctBefore=r.correct;wrongBefore=r.wrong;samplesBefore=r.samples;incomeBefore=r.income;
                pendingFeedback=null;Complete(game,true);r=ctl.Sim.MiniGameRecord(Packs[index].id);
                Require(r.correct>correctBefore&&r.samples>samplesBefore&&r.income>incomeBefore,"Correct round did not settle "+Packs[index].id);
                report.checks.Add(Packs[index].id+": successful round via actual controls; samples and income increased");frames=0;phase=3;Save();return;
            }
            if(phase==3)
            {
                if(++frames<4)return;CheckRetainedFeedback(true);var game=Game();
                if(Packs[index].id=="arith_mole")Press(Surface(),"Restart");
                else if(Packs[index].id=="spy")((XgMiniGame)game).Build();
                var r=ctl.Sim.MiniGameRecord(Packs[index].id);wrongBefore=r.wrong;correctBefore=r.correct;samplesBefore=r.samples;
                pendingFeedback=null;Complete(game,false);r=ctl.Sim.MiniGameRecord(Packs[index].id);
                Require(r.wrong>wrongBefore&&r.correct==correctBefore&&r.samples==samplesBefore,"Wrong round settlement mismatch "+Packs[index].id);
                var loaded=new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(ctl.Sim.S)));var saved=loaded.MiniGameRecord(Packs[index].id);
                Require(saved.correct==r.correct&&saved.wrong==r.wrong&&saved.samples==r.samples&&Math.Abs(saved.income-r.income)<.001,"Record lost on JsonUtility reload "+Packs[index].id);
                report.checks.Add(Packs[index].id+": wrong round via controls; no sample; full result survives JsonUtility reload");
                frames=0;phase=4;Save();return;
            }
            if(phase==4){if(++frames<4)return;CheckRetainedFeedback(false);report.packs.Last().answersAfter=ctl.View.Teach.AnswerCount;Save();phase=1;}

        }
        catch(Exception e){report.status="failed";report.failure=e.ToString();Save();Debug.LogError("Teaching pack check failed: "+e);Finish();}
    }
    static object Game()=>typeof(XgDeskGamePage).GetField("game",Flags).GetValue(desk);
    static RectTransform Surface()=>typeof(XgDeskGamePage).GetField("surface",Flags).GetValue(desk) as RectTransform;
    static object Field(object obj,string name){for(Type t=obj.GetType();t!=null;t=t.BaseType){var f=t.GetField(name,Flags);if(f!=null)return f.GetValue(obj);}throw new Exception("Missing field "+name);}
    static Transform ActiveChild(RectTransform root,string name){foreach(Transform child in root)if(child.name==name&&child.gameObject.activeSelf)return child;return null;}
    static Button FindButton(RectTransform root,string name){foreach(var b in root.GetComponentsInChildren<Button>(true))if(b.name==name&&b.gameObject.activeInHierarchy)return b;return null;}
    static void Press(RectTransform root,string name)
    {
        var b=FindButton(root,name);Require(b!=null&&b.IsActive()&&b.IsInteractable(),"Button unavailable "+name);
        var game=Game();var snapshot=SnapshotPrediction(game);int before=ctl.Sim.MiniGameRecord(Packs[index].id).correct;
        b.onClick.Invoke();
        if(snapshot!=null&&(bool)Field(game,"submitted")){
            snapshot.playerCorrect=ctl.Sim.MiniGameRecord(Packs[index].id).correct>before;pendingFeedback=snapshot;CheckFeedback(snapshot,false);}
    }
    static FeedbackSnapshot SnapshotPrediction(object game)
    {
        bool question=false;for(var t=game.GetType();t!=null;t=t.BaseType)if(t.Name=="XgPackQuestion")question=true;
        if(!question||(bool)Field(game,"submitted"))return null;
        var s=new FeedbackSnapshot{game=game,visible=(bool)Field(game,"predictionCaptured"),aiAnswer=(string)Field(game,"predictedAnswer"),aiCorrect=(bool?)Field(game,"predictionCorrect")};
        if(!s.visible)return s;
        var ai=Surface().GetComponentsInChildren<TMP_Text>().FirstOrDefault(t=>(t.name=="Ai"||t.name=="AiAnswer"||t.name=="PackAiAnswer")&&t.text.Contains(s.aiAnswer));
        Require(ai!=null,"Captured prediction must have actually been shown before submission: "+Packs[index].id+" / "+s.aiAnswer);
        bool unknown=new[]{"……不知道","…not sure","不知道","Not sure","无法判断","Cannot determine"}.Contains(s.aiAnswer);
        Require(unknown==!s.aiCorrect.HasValue,"Unknown text must not carry a correct/incorrect prediction verdict");
        if(unknown){Require(Gray(ai.color,game),"Current unknown prediction must be gray: "+Packs[index].id);return s;}
        // Existing answer-selection truth supplies an independent oracle for these displayed predictions.
        bool? oracle=null;string id=Packs[index].id;
        if(id.EndsWith("_swipe")){var phrase=(XgPhrase)Field(game,"phrase");oracle=(s.aiAnswer=="←")==phrase.yes;}
        else if(id=="minesweeper_yn") {var card=(XgCard)Field(game,"QuestionCard");oracle=(s.aiAnswer=="是雷"||s.aiAnswer=="Mine")== (card.teachingImageKeys[card.teachingImageKey]==1);}
        else if(id=="gomoku_win"&&s.aiAnswer.Length>=2){int k=(int.Parse(s.aiAnswer.Substring(1))-1)*9+s.aiAnswer[0]-'A';oracle=GomokuWins((int[])Field(game,"board"),k);}
        else if(id=="chess_mate1"||id=="chess_mate2"){
            var board=(LingGuangV05.Core.Games.ChessGame)Field(game,"board");var move=board.LegalMoves().First(m=>m.ToString()==s.aiAnswer);
            oracle=ForceMate(board,move,(int)Field(game,"maxMoves")-(int)Field(game,"moves"));}
        else {
            var keys=(Action[])Field(game,"keys");
            for(int i=0;i<keys.Length;i++)if(ClosureInt(keys[i].Target,"answer")==ClosureInt(keys[i].Target,"truthIndex")){
                var text=FindButton(Surface(),"Choice"+i).GetComponentInChildren<TMP_Text>().text;
                string answer=s.aiAnswer;int confidence=answer.IndexOf(" · ",StringComparison.Ordinal);if(confidence>=0)answer=answer.Substring(0,confidence);
                oracle=answer==text.Substring(text.IndexOf(". ",StringComparison.Ordinal)+2);break;}
        }
        if(oracle.HasValue)Require(s.aiCorrect==oracle,"AI prediction verdict differs from independent current question truth: "+id);
        return s;
    }
    static bool Gray(Color color,object game)
    {
        var context=(XgMiniContext)Field(game,"C");Color expected=context.Ui.dark?XgDark.Muted:new Color32(93,101,115,255);
        return Mathf.Abs(color.r-expected.r)<.002f&&Mathf.Abs(color.g-expected.g)<.002f&&Mathf.Abs(color.b-expected.b)<.002f;
    }
    static void CheckFeedback(FeedbackSnapshot s,bool retained)
    {
        var player=(TMP_Text)Field(s.game,"lastAnswerLabel");var ai=(TMP_Text)Field(s.game,"lastAiLabel");
        Require(player!=null&&ai!=null&&player.gameObject.activeInHierarchy&&ai.gameObject.activeInHierarchy,"Both feedback lines exist in the current question");
        Require(player.text.StartsWith(s.playerCorrect?"✓ ":"× ",StringComparison.Ordinal),"Player result must follow player settlement, independently of AI");
        string verdict=s.aiCorrect.HasValue?(s.aiCorrect.Value?(english?"✓ correct":"✓ 答对"):(english?"× incorrect":"× 答错")):(english?"… not sure":"… 不知道");
        string expected=s.visible?(english?"Previous: ":"上题 ")+ctl.Sim.AiDisplayName+(english?": ":"：")+verdict:"";
        Require(ai.text==expected,"Previous AI footer must match the prediction captured before settlement: "+Packs[index].id+" / expected="+expected+" / actual="+ai.text);
        if(s.visible&&!s.aiCorrect.HasValue)Require(Gray(ai.color,s.game),"Unknown previous AI verdict must remain gray");
        Require(!ai.raycastTarget&&!ai.richText,"Static AI footer cannot steal input or parse prediction markup");
        if(retained){Require(player.text==s.playerText&&ai.text==s.aiText,"Next question retains both previous feedback lines");CheckTextFits(player);if(s.visible)CheckTextFits(ai);}
        else{s.playerText=player.text;s.aiText=ai.text;}
    }
    static void CheckRetainedFeedback(bool correct)
    {
        if(pendingFeedback==null){Require(Packs[index].id=="arith_mole"||Packs[index].id=="spy","Question pack submitted without a captured pre-answer snapshot");return;}
        var s=pendingFeedback;Require(s.playerCorrect==correct,"Feedback snapshot belongs to expected player settlement");
        Require(ReferenceEquals(Game(),s.game)&&!(bool)Field(s.game,"submitted"),"Normal next-frame update opened next question without an added feedback dwell");
        string tag=Packs[index].id+"-"+(english?"en":"zh")+(correct?"-correct":"-wrong");CaptureFeedback(tag+".png");CheckFeedback(s,true);
        if(s.visible&&!s.aiCorrect.HasValue)unknownFeedback++;
        if(s.visible&&s.aiCorrect.HasValue&&s.aiCorrect.Value!=correct)knownDisagreement++;
        report.checks.Add(tag+": next question active; player="+correct+", prior shown AI="+s.aiAnswer+", AI verdict="+(s.visible?(s.aiCorrect.HasValue?s.aiCorrect.Value.ToString():"unknown"):"not displayed")+"; both footer lines retained and contained");
    }
    static void CheckTextFits(TMP_Text text)
    {
        Canvas.ForceUpdateCanvases();text.ForceMeshUpdate(true,true);var r=text.rectTransform.rect;
        Require(!string.IsNullOrEmpty(text.text)&&!text.isTextTruncated&&!text.isTextOverflowing,"Footer text must not truncate/overflow: "+text.name+" / "+text.text);
        foreach(var c in text.textInfo.characterInfo.Take(text.textInfo.characterCount))if(c.isVisible){
            Require(c.textElement!=null&&c.textElement.unicode==c.character,"Footer glyph missing or replaced: "+text.name+" / "+c.character);
            Require(c.bottomLeft.x>=r.xMin-.5f&&c.topRight.x<=r.xMax+.5f&&c.bottomLeft.y>=r.yMin-.5f&&c.topRight.y<=r.yMax+.5f,"Footer glyph outside text rectangle: "+text.name);}
        var corners=new Vector3[4];text.rectTransform.GetWorldCorners(corners);
        foreach(var mask in text.GetComponentsInParent<RectMask2D>().Where(m=>m.isActiveAndEnabled))foreach(var corner in corners){
            var p=mask.rectTransform.InverseTransformPoint(corner);var bounds=mask.rectTransform.rect;
            Require(p.x>=bounds.xMin-.5f&&p.x<=bounds.xMax+.5f&&p.y>=bounds.yMin-.5f&&p.y<=bounds.yMax+.5f,"Footer outside active mask: "+text.name);}
        report.checks.Add((english?"en":"zh")+" "+Packs[index].id+" "+text.name+": text="+text.text+" rect="+r+" preferred="+text.GetPreferredValues()+" fontSize="+text.fontSize);
    }
    static void CaptureFeedback(string name)
    {
        var camera=Surface().GetComponentInParent<Canvas>().rootCanvas.worldCamera;var target=camera==null?null:camera.targetTexture;
        Require(target!=null,"Actual teaching LCD source render target required for feedback screenshot");
        var previous=RenderTexture.active;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        try{RenderTexture.active=target;image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Root,name),image.EncodeToPNG());report.screenshots.Add(name);}
        finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);}
    }
    static void Finish(){finishing=true;locale?.Dispose();locale=null;Application.logMessageReceived-=Log;EditorApplication.update-=Tick;EditorApplication.ExitPlaymode();}
}
#endif
