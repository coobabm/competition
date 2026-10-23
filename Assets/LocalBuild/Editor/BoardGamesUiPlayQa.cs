#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using HongmengOS.Aero2010;
using LingGuangV05.Core.Games;
using LingGuangV05.Desktop.Games;
using System.IO;
using System.Reflection;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using LingGuangV05.Runtime.Persistence;
using LingGuangV05.XingGuang;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.LLM;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

/// <summary>Prepared Main-scene QA. Run in a separate Editor; saves/profile are isolated before Main loads.</summary>
[InitializeOnLoad]
public static partial class BoardGamesUiPlayQa
{
    const string Key = "BoardGamesUiPlayQa.Root";
    const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static string Root => SessionState.GetString(Key, "");
    static readonly List<string> passed = new List<string>(), failed = new List<string>(), errors = new List<string>(), infrastructure = new List<string>();
    static readonly List<TextResult> labels = new List<TextResult>();
    static ChapterOneRuntime runtime;
    static XingGuangController ctl;
    static GameHubView hub;
    static LingGuangV05.Desktop.OS.WindowManager gameWindow;
    static IEnumerator routine;
    static int lastFrame;
    static IDisposable locale;
    static string language = "zh-CN", output;
    static double deadline;
    static bool fixtureReady, running;
    [Serializable] sealed class Report { public string status, detail; public List<string> passed, failed, errors, infrastructure; public List<TextResult> labels; }
    [Serializable] sealed class TextResult { public string field, language, text, boundsFailure; public int characters, visible; public bool truncated, overflow, missingGlyph, contained; }
    static BoardGamesUiPlayQa() { EditorApplication.playModeStateChanged += State; }
    public static void Run()=>RunMode("games");
    public static void RunWithLogicTools()=>RunMode("all");
    public static void RunLogicTools()=>RunMode("tools");
    static void RunMode(string mode)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start in Edit Mode");
        string dir = Path.GetFullPath(Path.Combine("LocalVerification", "BoardGamesUI", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));
        Directory.CreateDirectory(dir); Directory.CreateDirectory(Path.Combine(dir, "Save")); Directory.CreateDirectory(Path.Combine(dir, "DesktopProfile"));
        SessionState.SetString(Key, dir);SessionState.SetString(Key+".Mode",mode);
        EditorSceneManager.OpenScene("Assets/LingGuangV05/Scenes/Main.unity");
        EditorApplication.EnterPlaymode();
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Isolate()
    {
        if (string.IsNullOrEmpty(Root)) return;
        ChapterOneRuntime.VerificationSaveDirectory = Path.Combine(Root, "Save");
        typeof(LingGuangV05.Desktop.OS.DreamOSDataManager).GetField("dataPath", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, Path.Combine(Root, "DesktopProfile") + Path.DirectorySeparatorChar);
        SceneManager.sceneLoaded += OnScene;
    }
    static void OnScene(Scene scene, LoadSceneMode mode)
    {
        if (string.IsNullOrEmpty(Root)) return;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var rt in root.GetComponentsInChildren<ChapterOneRuntime>(true))
            { var llm = rt.GetComponent<LocalLlm>() ?? rt.gameObject.AddComponent<LocalLlm>(); llm.enabled = false; }
            foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true)) StopStory(component);
        }
    }
    static void StopStory(MonoBehaviour component)
    {
        if (component == null) return;
        string name = component.GetType().Name;
        if (name == "PrologueDirector" || name == "StoryDirector" || name == "StoryDesktopPresenter" || name == "StageOneTutor" || name == "SpiderIntroScene" || name == "LaoZhouYY" || name == "AiJoinsYy")
        { component.CancelInvoke(); component.StopAllCoroutines(); component.enabled = false; }
    }
    static void State(PlayModeStateChange state)
    {
        if (string.IsNullOrEmpty(Root)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            Application.targetFrameRate=30;QualitySettings.vSyncCount=0;
            output = Root; passed.Clear(); failed.Clear(); errors.Clear(); infrastructure.Clear(); labels.Clear();
            fixtureReady = false; deadline = EditorApplication.timeSinceStartup + 180;
            Application.logMessageReceived += Log; EditorApplication.update += Boot;
        }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.update -= Boot; EditorApplication.update -= Tick; Application.logMessageReceived -= Log; SceneManager.sceneLoaded -= OnScene;
            bool ok = File.Exists(Path.Combine(Root, "passed.txt")); SessionState.EraseString(Key);SessionState.EraseString(Key+".Mode"); ChapterOneRuntime.VerificationSaveDirectory = null;
            if (Application.isBatchMode || Array.IndexOf(Environment.GetCommandLineArgs(), "-verificationExit") >= 0) EditorApplication.Exit(ok ? 0 : 1);
        }
    }
    static void Log(string message, string stack, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        if (stack.Contains("UnityEditor.Search.SearchDatabase") && !stack.Contains("Assets/")) infrastructure.Add(message + "\n" + stack);
        else errors.Add(message + "\n" + stack);
    }
    static FieldInfo FindField(Type type,string name){while(type!=null){var field=type.GetField(name,Flags|BindingFlags.DeclaredOnly);if(field!=null)return field;type=type.BaseType;}throw new MissingFieldException(name);}
    static T Field<T>(object value,string name)=>(T)FindField(value.GetType(),name).GetValue(value);
    static object Call(object value,string name,params object[] args){var type=value.GetType();while(type!=null){var method=type.GetMethod(name,Flags|BindingFlags.DeclaredOnly);if(method!=null)return method.Invoke(value,args);type=type.BaseType;}throw new MissingMethodException(name);}
    static void Check(bool ok, string message) { (ok ? passed : failed).Add(message); }
    static void Require(bool ok, string message) { Check(ok, message); if (!ok) throw new InvalidOperationException(message); }
    /// <summary>Which XgEndingPreparationHost.NarrativeAvailable / PollNarrative gates currently hold the shutdown question back.</summary>
    static string NarrativeBlockers(XgEndingPreparationHost host,XgEndingPreparationFlow flow)
    {
        var parts=new List<string>();var lab=ctl.Sim;
        void Add(string name,Func<bool> blocked){try{if(blocked())parts.Add(name);}catch(Exception e){parts.Add(name+"?"+e.GetType().Name);}}
        Add("notCurrent",()=>!(bool)typeof(XgEndingPreparationHost).GetProperty("Current",Flags).GetValue(host));
        Add("prologue",()=>runtime.Sim.InPrologue);
        Add("minimized",()=>PlayerWindowState.IsMinimized);
        Add("storyFocus",()=>StoryFocus.Active);
        Add("innerVoice",()=>InnerVoice.Busy);
        Add("stagePresentation",()=>lab.StagePresentationHeld);
        Add("gamePaused",()=>XgLifeHostActivity.GamePaused);
        Add("readingWindow",()=>XgLifeHostActivity.ReadingWindowVisible);
        Add("groupScript",()=>LingGuangV05.Desktop.YY.YYChatHub.Instance?.GroupScriptPlaying==true);
        Add("saveSlots",()=>SaveSlotsPanel.IsOpen);
        Add("caseMenuSettings",()=>LingGuangV05.CaseMenu.CaseMenuHud.AnySettingsOpen);
        Add("chapterComplete",()=>lab.S.chapterComplete);
        Add("endingAreaShowing",()=>host.Showing);
        Add("stageConsumed",()=>flow.ShutdownPromptStageConsumed);
        Add("phase="+(lab.S.endingPreparation?.phase??"testing"),()=>(lab.S.endingPreparation?.phase??"testing")!="testing");
        parts.Add("stage="+lab.S.stage+" waited="+lab.S.shutdownNarrativeElapsed+"/"+flow.ShutdownPromptDelay+" waitStage="+lab.S.shutdownNarrativeStage);
        return string.Join(", ",parts);
    }
    static void Boot()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Main desktop readiness timed out");
            if (errors.Count > 0) throw new Exception(errors[0]);
            runtime = UnityEngine.Object.FindAnyObjectByType<ChapterOneRuntime>(); ctl = runtime == null ? null : runtime.GetComponent<XingGuangController>();
            if (ctl?.Sim == null || ctl.View == null) return;
            if (!fixtureReady)
            {
                foreach (var component in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)) StopStory(component);
                string saveRoot = Path.GetFullPath(Path.Combine(Root, "Save")) + Path.DirectorySeparatorChar;
                Require(Path.GetFullPath(runtime.SavePath).StartsWith(saveRoot, StringComparison.OrdinalIgnoreCase), "Runtime store is inside isolated QA save folder");
                runtime.tickEnabled = false; ctl.enabled = false;
                var lab = new XgSim(); lab.S.stage = 1; lab.S.handCorrect = 12; lab.S.unlocked.Add("train.curve"); lab.LifeEventIntervalMode = 0;
                var house = new ChapterOneSim(); house.S.prologue = 2; house.S.appInstallState = 2; house.S.money = 100000;
                house.S.gpuModels.Clear(); house.S.gpuModels.Add("gtx980ti"); house.S.gpuCount = 1; house.S.labState = JsonUtility.ToJson(lab.S);
                // The fresh simulation already owns a starter card. Validate the fixture before binding it.
                house = new ChapterOneSim(JsonUtility.FromJson<GameState>(JsonUtility.ToJson(house.S)));
                Rebind(house);
                var boot = UnityEngine.Object.FindAnyObjectByType<LingGuangV05.Desktop.OS.BootManager>(FindObjectsInactive.Include);
                if (boot != null) { boot.CancelInvoke(); boot.StopAllCoroutines(); if (boot.bootAnimator != null) boot.bootAnimator.gameObject.SetActive(false); }
                var user = UnityEngine.Object.FindAnyObjectByType<LingGuangV05.Desktop.OS.UserManager>(FindObjectsInactive.Include);
                Require(user != null && user.isActiveAndEnabled, "Desktop user available"); user.ShowDesktop(); fixtureReady = true; return;
            }
            var binding = runtime.GetComponent<ChapterOneDesktopRouter>()?.Get(XingGuangController.AppId);
            if (binding?.TaskbarShortcut == null || !binding.TaskbarShortcut.isActiveAndEnabled || binding.DesktopShortcut == null || !binding.DesktopShortcut.gameObject.activeInHierarchy) return;
            EditorApplication.update -= Boot; BeginChecks();
        }
        catch (Exception error) { Finish("failed", error.ToString()); }
    }
    static void Rebind(ChapterOneSim house)
    {
        runtime.SetSimulationForTests(house);
        // Host refuses wallet writes in TestMode. All paths were checked above; enable real isolated persistence.
        typeof(ChapterOneRuntime).GetProperty("TestMode", Flags).GetSetMethod(true).Invoke(runtime, new object[] { false });
        runtime.useDiskSave = true; runtime.tickEnabled = false;
        typeof(XingGuangController).GetMethod("BindSave", Flags).Invoke(ctl, new object[] { house });
        ctl.enabled = false;
    }

    static void BeginChecks()
    {
        language="zh-CN";locale=GameText.Bind(()=>language);GameText.PublishChanged();ctl.Sim.English=false;
        hub=UnityEngine.Object.FindAnyObjectByType<GameHubView>(FindObjectsInactive.Include);
        Require(hub!=null,"Actual Main GameHubView exists");gameWindow=hub.GetComponentInParent<LingGuangV05.Desktop.OS.WindowManager>(true);
        Require(gameWindow!=null,"Native Game Hub window exists");gameWindow.OpenWindow();
        Check(true,"Prepared-desktop fixture isolates AiJoinsYy alongside the existing story hosts; native LCD and board rules remain live.");
        running=true;deadline=EditorApplication.timeSinceStartup+(LogicMode=="all"?1080:LogicMode=="tools"?360:720);lastFrame=-1;routine=SelectedChecks();EditorApplication.update+=Tick;
    }
    static IEnumerator Checks()
    {
        yield return null;
        double wait=EditorApplication.timeSinceStartup+1.5;while(EditorApplication.timeSinceStartup<wait)yield return null;
        var games=hub.GetComponents<HubGame>();
        Require(games.Select(g=>g.Id).OrderBy(x=>x).SequenceEqual(new[]{"gomoku","go","chess","ludo","checkers_cn","monopoly"}.OrderBy(x=>x)),"All original and three new native games remain installed");
        Capture("game-hub-start.png",hub.transform as RectTransform);
        var ludo=hub.GetComponent<LudoPanel>();
        ClickNamed(hub.transform,"Nav ludo");yield return null;
        Require(Field<RectTransform>(ludo,"ruleCard").gameObject.activeInHierarchy,"First opening displays real rule card");
        Inspect(Field<TMP_Text>(ludo,"ruleText"),"ludo rules zh");Capture("ludo-rules-zh.png",Area(ludo));
        Require(!Field<Button>(ludo,"aiButton").interactable,"Stage 1 personal AI remains disabled, computer is available");
        ClickNamed(Field<RectTransform>(ludo,"ruleCard"),"Start");yield return null;
        ctl.Sim.S.stage=2;foreach(var game in games)game.SyncBoardOwner();
        ClickNamed(Area(ludo),"Private AI");yield return null;
        Require(Field<bool>(ludo,"privateAiOpponent"),"Stage 2 personal AI selected without recorder or logic upgrade");
        int rolls=Field<LudoGame>(ludo,"game").S.rolls;ClickNamed(Area(ludo),"Roll");yield return null;
        Require(Field<LudoGame>(ludo,"game").S.rolls==rolls+1,"Native Roll commits an actual random die");
        int before=Field<LudoGame>(ludo,"game").S.log.Count;
        ClickNamed(Area(ludo),"Rules");wait=EditorApplication.timeSinceStartup+1;while(EditorApplication.timeSinceStartup<wait)yield return null;
        Require(Field<LudoGame>(ludo,"game").S.log.Count==before,"Rule overlay pauses native game actions");
        var toggle=Field<Toggle>(ludo,"hideRules");ClickRect((RectTransform)toggle.transform);
        ClickNamed(Field<RectTransform>(ludo,"ruleCard"),"Start");Require(ctl.Sim.S.boardRulesHidden.Contains("ludo"),"Do not show rules persists only after Start");
        ClickNamed(Area(ludo),"Rules");ClickRect((RectTransform)toggle.transform);ClickNamed(Field<RectTransform>(ludo,"ruleCard"),"Start");
        Require(!ctl.Sim.S.boardRulesHidden.Contains("ludo"),"Unchecked rules toggle restores future first-open rules");
        ClickNamed(hub.transform,"Nav home");before=Field<LudoGame>(ludo,"game").S.log.Count;wait=EditorApplication.timeSinceStartup+1;while(EditorApplication.timeSinceStartup<wait)yield return null;
        Require(Field<LudoGame>(ludo,"game").S.log.Count==before,"Hidden game page pauses queued AI");
        ClickNamed(hub.transform,"Nav ludo");yield return null;
        int startRounds=ctl.Sim.S.boardRounds.Count;double goSamples=ctl.Sim.Labels("go"),logicSamples=ctl.Sim.Labels("logic");
        double gameDeadline=EditorApplication.timeSinceStartup+360;
        while(!Field<LudoGame>(ludo,"game").Over)
        {
            if(EditorApplication.timeSinceStartup>gameDeadline)throw new TimeoutException("Actual unrigged Ludo completion exceeded 360 seconds");
            var g=Field<LudoGame>(ludo,"game");
            if(g.S.turn==0){if(g.S.die==0)ClickNamed(Area(ludo),"Roll");else {
                int plane=g.PickPlane(1,new System.Random(g.S.rolls));
                var ready=WaitLudoPlane(ludo,g,plane);while(ready.MoveNext())yield return ready.Current;
                ClickRect((RectTransform)Field<Button[]>(ludo,"tokenButtons")[plane].transform);}}
            yield return null;
        }
        Require(ctl.Sim.S.boardRounds.Count==startRounds+1,"Entire actual Ludo game commits one completed round");
        Require(ctl.Sim.S.boardRounds.Last().privateAi&&!ctl.Sim.S.boardRounds.Last().recording,"Pre-purchase personal game qualifies but is not recording");
        Require(ctl.Sim.S.dataFiles.Count==0&&ctl.Sim.Labels("logic")==logicSamples,"Pre-purchase completion creates neither a pack nor samples");
        Capture("ludo-real-complete.png",Area(ludo));Inspect(Field<TMP_Text>(ludo,"status"),"ludo complete zh");
        wait=EditorApplication.timeSinceStartup+6;while(EditorApplication.timeSinceStartup<wait)yield return null;
        Require(ctl.Sim.BuyNode("gamedata",ctl.Host),"Real purchase transaction buys recorder after completed personal game (core purchase, not tech-button UI)");
        Require(ctl.Sim.S.boardRounds.Last().recording==false,"Buying recorder does not backfill earlier game");

        var checkers=hub.GetComponent<ChineseCheckersPanel>();ClickNamed(hub.transform,"Nav checkers_cn");yield return null;
        ClickNamed(Field<RectTransform>(checkers,"ruleCard"),"Start");ClickNamed(Area(checkers),"Private AI");yield return null;
        Require(Field<ChineseCheckers>(checkers,"game").S.moves==0,"Chinese checkers starts a fresh actual board");
        Capture("checkers-start.png",Area(checkers));gameDeadline=EditorApplication.timeSinceStartup+100;
        while(!Field<ChineseCheckers>(checkers,"game").Over)
        {
            if(EditorApplication.timeSinceStartup>gameDeadline)throw new TimeoutException("Chinese checkers full game exceeded 100 seconds");
            var g=Field<ChineseCheckers>(checkers,"game");if(g.S.turn==0)
            {
                var move=g.Pick(1,new System.Random(g.S.moves+10));
                if(move==null)ClickNamed(Area(checkers),"No legal move");
                else{var images=Field<Image[]>(checkers,"holes");ClickRect(images[g.S.pieces[move.piece]].rectTransform);ClickRect(images[move.destination].rectTransform);}
            }
            yield return null;
        }
        Require(ctl.Sim.S.boardRounds.Last().game=="checkers_cn"&&ctl.Sim.S.boardRounds.Last().recording,"Actual full checkers game records after purchase");
        Capture("checkers-real-complete.png",Area(checkers));Inspect(Field<TMP_Text>(checkers,"status"),"checkers complete zh");

        wait=EditorApplication.timeSinceStartup+6;while(EditorApplication.timeSinceStartup<wait)yield return null;
        var monopoly=hub.GetComponent<MonopolyPanel>();ClickNamed(hub.transform,"Nav monopoly");yield return null;
        ClickNamed(Field<RectTransform>(monopoly,"ruleCard"),"Start");ClickNamed(Area(monopoly),"Private AI");yield return null;
        for(int round=0;round<3;round++)
        {
            if(round>0)ClickNamed(Area(monopoly),"Restart");yield return null;gameDeadline=EditorApplication.timeSinceStartup+65;
            while(!Field<MonopolyGame>(monopoly,"game").Over)
            {
                if(EditorApplication.timeSinceStartup>gameDeadline)throw new TimeoutException("Monopoly full game exceeded 65 seconds");
                var g=Field<MonopolyGame>(monopoly,"game");if(g.S.turn==0)
                {if(g.S.pending<0)ClickNamed(Area(monopoly),"Roll");else ClickNamed(Area(monopoly),g.ShouldBuy(3)?"Buy property":"Skip purchase");}
                yield return null;
            }
            Require(ctl.Sim.S.boardRounds.Last().game=="monopoly"&&ctl.Sim.S.boardRounds.Last().recording,"Actual Monopoly round "+(round+1)+" records once");
            Require(ctl.Sim.Labels("logic")==logicSamples,"No immediate samples after Monopoly round "+(round+1));
            if(round<2)Require(ctl.Sim.S.boardBatches.Count==0,"Fewer than three same-game rounds do not issue a pack");
            wait=EditorApplication.timeSinceStartup+6;while(EditorApplication.timeSinceStartup<wait)yield return null;
        }
        Require(ctl.Sim.S.boardBatches.Count==1&&ctl.Sim.S.boardBatches[0].game=="monopoly","Exactly three completed Monopoly games create exactly one actual pack");
        Require(ctl.Sim.S.boardBatches[0].rounds.Count==3,"Pack references three actual session tickets");
        Capture("monopoly-real-complete-zh.png",Area(monopoly));Inspect(Field<TMP_Text>(monopoly,"status"),"monopoly complete zh");Inspect(Field<TMP_Text>(monopoly,"account"),"monopoly accounts zh");
        var narrative=runtime.GetComponent<XgEndingPreparationHost>();
        Require(narrative!=null,"Actual shutdown narrative host exists");
        Require(PrologueDirector.Desk.layer.Find("EndingNotesLauncher")==null,"Desktop has no permanent narrative launcher");
        // Explicit timer fixture: the real dispatch/save/UI path still creates the question.
        // This regression does not claim a naturally elapsed random waiting period.
        var shutdownFlow=Field<XgEndingPreparationFlow>(narrative,"flow");
        shutdownFlow.TickShutdownPromptWait(shutdownFlow.ShutdownPromptDelay);
        // Reaching stage 2 starts a YY group introduction (apps now arrive through group links). NarrativeAvailable holds
        // the question while group playback runs, so it never interrupts the chat; it must appear once playback ends.
        // The introduction is interactive and waits for the player, who here uses its own skip control.
        var groupChat=LingGuangV05.Desktop.YY.YYChatHub.Instance;
        if(groupChat!=null&&groupChat.GroupScriptPlaying)
        {
            wait=EditorApplication.timeSinceStartup+1;while(EditorApplication.timeSinceStartup<wait)yield return null;
            if(groupChat.GroupScriptPlaying)Require(Field<RectTransform>(narrative,"seedPanel")==null,"Due shutdown question waits while a YY group script plays");
            wait=EditorApplication.timeSinceStartup+60;
            while(groupChat.GroupScriptPlaying&&EditorApplication.timeSinceStartup<wait)
            {
                if(groupChat.IntroPlaying&&groupChat.IntroHost.Held==false)groupChat.IntroHost.Skip(groupChat.IntroRevision);
                yield return null;
            }
            Require(!groupChat.GroupScriptPlaying,"YY group playback finishes before the shutdown-question regression");
        }
        wait=EditorApplication.timeSinceStartup+3;
        while(Field<RectTransform>(narrative,"seedPanel")==null&&EditorApplication.timeSinceStartup<wait)
        {
            // A further introduction may queue right behind the skipped one; it is skipped the same way.
            if(groupChat!=null&&groupChat.IntroPlaying&&groupChat.IntroHost.Held==false)groupChat.IntroHost.Skip(groupChat.IntroRevision);
            yield return null;
        }
        var currentSeed=narrative==null?null:Field<RectTransform>(narrative,"seedPanel");
        if(currentSeed==null||!currentSeed.gameObject.activeInHierarchy)Debug.LogWarning("[BoardGamesUiPlayQa] shutdown question blocked by: "+NarrativeBlockers(narrative,shutdownFlow));
        Require(currentSeed!=null&&currentSeed.gameObject.activeInHierarchy,"Actual pending shutdown question is available for language-switch regression");
        string seedId=Field<string>(narrative,"shownSeed");double seedWait=ctl.Sim.S.shutdownNarrativeElapsed;string seedDispatch=ctl.Sim.S.endingPreparation.seedDispatches.Single(d=>d.stage==ctl.Sim.S.stage).receipt;
        var seedQuestion=Field<TMP_Text>(narrative,"seedQuestionLabel");
        var seedOptions=Field<TMP_Text[]>(narrative,"seedOptionLabels");
        string completedBoard=JsonUtility.ToJson(Field<MonopolyGame>(monopoly,"game").S);
        int completedRounds=ctl.Sim.S.boardRounds.Count,completedPacks=ctl.Sim.S.boardBatches.Count;
        language=GameText.English;ctl.Sim.English=true;GameText.PublishChanged();yield return null;
        yield return null;
        Require(ReferenceEquals(currentSeed,Field<RectTransform>(narrative,"seedPanel"))&&ReferenceEquals(seedQuestion,Field<TMP_Text>(narrative,"seedQuestionLabel"))&&ReferenceEquals(seedOptions,Field<TMP_Text[]>(narrative,"seedOptionLabels")),"Language switch retains the live question and option controls");
        Require(seedId==Field<string>(narrative,"shownSeed")&&seedWait==ctl.Sim.S.shutdownNarrativeElapsed&&seedDispatch==ctl.Sim.S.endingPreparation.seedDispatches.Single(d=>d.stage==ctl.Sim.S.stage).receipt,"Language switch preserves unanswered prompt, saved dispatch receipt and timer");
        Require(seedQuestion.text.StartsWith("No right answer")||seedQuestion.text.StartsWith("A remembered day"),"Existing question text refreshes to English");
        var seedPrompt=Field<XgEndingSeedPrompt>(narrative,"shownSeedPrompt");
        for(int i=0;i<seedOptions.Length;i++)Require(seedOptions[i].text==seedPrompt.options[i].en,"Existing question option refreshes to English: "+i);
        Require(Field<TMP_Text>(narrative,"seedLaterLabel").text=="Skip this question","Existing question skip control refreshes to English");
        var yyView=UnityEngine.Object.FindAnyObjectByType<LingGuangV05.Desktop.YY.YYChatView>(FindObjectsInactive.Include);
        Require(yyView!=null&&Field<TMP_Text>(yyView,"nativeStatus").text==Lang.T("YY 2016 · 消息只保存在本机"),"Native YY footer refreshes with the shared language event");
        Capture("monopoly-real-complete-en.png",Area(monopoly));Inspect(Field<TMP_Text>(monopoly,"status"),"monopoly complete en");Inspect(Field<TMP_Text>(monopoly,"account"),"monopoly accounts en");
        Require(Field<TMP_Text>(monopoly,"status").text.Contains("Game complete.")&&!Field<TMP_Text>(monopoly,"status").text.Contains("对局已结束"),"Completed game receipt relocalizes to English without another game");
        Require(Field<TMP_Text>(monopoly,"account").text.Contains(ctl.Sim.AiDisplayName),"Private opponent uses the same AI display name as the lab");
        language="zh-CN";ctl.Sim.English=false;GameText.PublishChanged();
        yield return null;yield return null;
        Require(ReferenceEquals(currentSeed,Field<RectTransform>(narrative,"seedPanel"))&&seedId==Field<string>(narrative,"shownSeed")&&seedWait==ctl.Sim.S.shutdownNarrativeElapsed&&seedDispatch==ctl.Sim.S.endingPreparation.seedDispatches.Single(d=>d.stage==ctl.Sim.S.stage).receipt,"Switching back retains the same unanswered shutdown prompt");
        Require(seedQuestion.text.StartsWith("没有标准答案")||seedQuestion.text.StartsWith("记得的那一天"),"Existing question text switches back to Chinese");
        int seedAnswerCount=ctl.Sim.S.endingPreparation.seedAnswers.Count;
        var skipQuestion=Field<TMP_Text>(narrative,"seedLaterLabel").GetComponentInParent<Button>();
        Require(skipQuestion!=null&&Field<TMP_Text>(narrative,"seedLaterLabel").text=="这次先不答","Skip explicitly closes this stage's one question");
        ClickRect((RectTransform)skipQuestion.transform);yield return null;
        shutdownFlow.TickShutdownPromptWait(200);wait=EditorApplication.timeSinceStartup+1;
        while(EditorApplication.timeSinceStartup<wait)yield return null;
        Require(Field<RectTransform>(narrative,"seedPanel")==null&&ctl.Sim.S.endingPreparation.seedDispatches.Count(d=>d.stage==ctl.Sim.S.stage)==1,"Skipping a shown question cannot dispatch it again in the same stage");
        Require(ctl.Sim.S.endingPreparation.seedAnswers.Count==seedAnswerCount,"Skipping does not invent an answer or ending vote");
        Require(Field<TMP_Text>(yyView,"nativeStatus").text=="YY 2016 · 消息只保存在本机","Native YY footer switches back to Chinese");
        Require(JsonUtility.ToJson(Field<MonopolyGame>(monopoly,"game").S)==completedBoard&&ctl.Sim.S.boardRounds.Count==completedRounds&&ctl.Sim.S.boardBatches.Count==completedPacks,"Actual language events redraw the completed board without changing result, rounds or packs");
        Require(Field<TMP_Text>(monopoly,"status").text.Contains("对局已结束")&&!Field<TMP_Text>(monopoly,"status").text.Contains("Game complete."),"Completed receipt switches back to Chinese without changing its result");
        ctl.Sim.S.unlocked.Add(XgSim.DataEntryNode);ctl.Open();ctl.View.Open("train");gameWindow.CloseWindow();yield return null;
        var collect=CollectVisibleCards();while(collect.MoveNext())yield return collect.Current;
        var files=runtime.GetComponent<XgFilesWindow>();Require(files!=null,"Actual Files window exists");files.Bind(ctl.Sim);files.Show();
        wait=EditorApplication.timeSinceStartup+.8;while(EditorApplication.timeSinceStartup<wait)yield return null;
        Capture("records-before-window-arrangement.png",files.client);
        var arrange=DragFilesToLeft(files);while(arrange.MoveNext())yield return arrange.Current;
        Capture("records-after-window-arrangement.png",files.client);
        var folder=files.client.GetComponentsInChildren<XgFilesRow>().First(r=>r.folder&&r.fileId=="packs");ClickRect((RectTransform)folder.transform,2);yield return null;
        var file=ctl.Sim.S.dataFiles.Single(f=>f.offer=="board.records");var row=files.client.GetComponentsInChildren<XgFilesRow>().First(r=>r.fileId==file.fileId);
        Require(row.ready,"Actual completed records pack is ready in Files");
        var drop=ctl.View.GetComponentsInChildren<XgFilesTrainDrop>(true).First(d=>d.isActiveAndEnabled&&d.TargetVisible());
        var destination=DropPoint(drop);var e=Pointer((RectTransform)row.transform);RequireTop((RectTransform)row.transform,e);
        var surface=LcdSurface();surface.OnPointerDown(e);e.position+=new Vector2(20,8);surface.OnDrag(e);
        Require(row.Payload!=null&&row.Payload.IsCurrent,"Real Files row begins current drag payload");
        var payload=row.Payload;e.position=destination;surface.OnDrag(e);
        Require(LcdHit(e.position).gameObject?.GetComponentInParent<XgFilesTrainDrop>()==drop,"Actual LCD drop still targets exposed Train root");
        surface.OnPointerUp(e);
        Require(drop.LastInstalled==1&&file.installed,"Actual Train drop installs the records pack once");
        Require(ctl.Sim.Labels("logic")==logicSamples+file.samples,"Only successful drop adds exactly the recorded samples");
        double installed=ctl.Sim.Labels("logic");Require(!payload.TryConsume(ctl.Sim),"Consumed real drag payload cannot be claimed twice (adversarial guard)");
        e.pointerDrag=row.gameObject;ExecuteEvents.Execute(drop.gameObject,e,ExecuteEvents.dropHandler); // Adversarial duplicate only; first drop used LCD release.
        Require(drop.LastInstalled==0&&ctl.Sim.Labels("logic")==installed,"Repeated same drop cannot award samples twice");
        Require(ctl.Sim.LearningRecordsNewest().Count(x=>x.sourceKind=="game")==1,"One game-install learning receipt is visible to the learning log");
        Capture("records-real-drop-installed.png",drop.transform as RectTransform);
        Require(ctl.Sim.Labels("go")==goSamples,"Unrelated dataset samples remain unchanged");
        // Retained receipts belong to their original save even before a hidden board is revisited.
        Rebind(new ChapterOneSim(JsonUtility.FromJson<GameState>(JsonUtility.ToJson(runtime.Sim.S))));
        Require(!((string)Call(monopoly,"LabLines")).Contains("对局已结束"),"A new save owner cannot inherit the previous board's completion receipt");
    }
    static RectTransform Area(HubGame game)=>Field<RectTransform>(game,"boardArea");
    static IEnumerator WaitLudoPlane(LudoPanel panel,LudoGame expected,int plane)
    {
        int rolls=expected.S.rolls,actions=expected.S.actions,die=expected.S.die;
        Require(plane>=0&&plane<2&&expected.S.turn==0&&die>0&&expected.LegalPlanes().Contains(plane),"Selected Ludo plane is legal in the current player turn");
        var rect=(RectTransform)Field<Button[]>(panel,"tokenButtons")[plane].transform;
        // Permit at most four real frame boundaries for native canvas/render/input registration.
        // Do not force raycasters, masks, focus, hierarchy, alpha, or business/game state.
        for(int frame=1;frame<=4;frame++)
        {
            yield return null;
            var current=Field<LudoGame>(panel,"game");
            Require(ReferenceEquals(current,expected)&&current.S.turn==0&&current.S.rolls==rolls&&current.S.actions==actions&&current.S.die==die
                &&current.LegalPlanes().Contains(plane)&&Field<Button[]>(panel,"tokenButtons")[plane].transform==rect,
                "Ludo turn, die, action count, legal plane and target stay unchanged during actual-hit readiness wait");
            var e=Pointer(rect);var hit=LcdHit(e.position).gameObject;
            if(hit!=null&&(hit==rect.gameObject||hit.transform.IsChildOf(rect)))
            {passed.Add("Ludo plane actual LCD target ready after "+frame+" fresh frame(s)");yield break;}
            if(frame==1)SaveHitEvidence(rect,e,"ludo-plane-first-miss");
            if(frame==4)RequireTop(rect,e); // Original strict hit requirement; no fallback click.
        }
    }
    static void ClickNamed(Transform root,string name)
    {var target=root.GetComponentsInChildren<Button>(true).FirstOrDefault(b=>b.name==name&&b.gameObject.activeInHierarchy);Require(target!=null,"Visible native button "+name);ClickRect((RectTransform)target.transform);}
    static PointerEventData Pointer(RectTransform rect,int count=1)
    {
        Canvas.ForceUpdateCanvases();
        return new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,clickCount=count,position=LcdPoint(rect,rect.rect.center)};
    }
    static AeroLcdInputSurface LcdSurface()
    {
        var bridge=UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>();
        if(bridge?.inputSurface==null||!bridge.inputSurface.isActiveAndEnabled||!bridge.inputSurface.inputEnabled)
        {
            var surface=bridge==null?null:bridge.inputSurface;
            var lcd=bridge==null?null:bridge.controller;
            var source=surface==null?null:surface.sourceCamera;
            var prologue=UnityEngine.Object.FindAnyObjectByType<LingGuangV05.Desktop.Story.PrologueDirector>(FindObjectsInactive.Include);
            var join=UnityEngine.Object.FindAnyObjectByType<LingGuangV05.Desktop.Story.AiJoinsYy>(FindObjectsInactive.Include);
            var title=UnityEngine.Object.FindAnyObjectByType<LingGuangV05.CaseMenu.CaseMenuMainDisplay>(FindObjectsInactive.Include);
            throw new InvalidOperationException("Actual Main LCD input is unavailable; scene="+SceneManager.GetActiveScene().path+
                "; bridge="+(bridge!=null)+"; surface="+(surface!=null)+"; surfaceActive="+(surface!=null&&surface.isActiveAndEnabled)+
                "; inputEnabled="+(surface!=null&&surface.inputEnabled)+"; sourceCamera="+(source!=null)+"; cameraActive="+(source!=null&&source.isActiveAndEnabled)+
                "; lcdActive="+(lcd!=null&&lcd.isActiveAndEnabled)+"; powered="+(lcd!=null&&lcd.IsPowered)+"; powerTransition="+(lcd!=null&&lcd.IsTransitioning)+
                "; eventSystem="+(EventSystem.current!=null&&EventSystem.current.isActiveAndEnabled)+
                "; prologue="+(prologue==null?"absent":prologue.enabled+"/"+prologue.Running+"/"+prologue.Step)+
                "; AiJoinsYy="+(join==null?"absent":join.enabled.ToString())+"/playing="+LingGuangV05.Desktop.Story.AiJoinsYy.Playing+
                "; AutoLabelPlaying="+LingGuangV05.Desktop.Story.AutoLabelEpiphany.Playing+
                "; titleDisplay="+(title==null?"absent":title.HasEnteredDesktop+"/"+title.IsScreenPowered+"/"+title.IsRevealingDesktop));
        }
        return bridge.inputSurface;
    }
    static Vector2 LcdPoint(RectTransform rect,Vector2 local)
    {
        if(!TryLcdPoint(rect,local,out var point))throw new InvalidOperationException("Target outside actual source viewport: "+rect.name);
        return point;
    }
    static bool TryLcdPoint(RectTransform rect,Vector2 local,out Vector2 point)
    {
        point=Vector2.zero;
        var surface=LcdSurface();var source=surface.sourceCamera.WorldToScreenPoint(rect.TransformPoint(local));
        var texture=surface.sourceCamera.targetTexture;
        if(texture==null||source.z<=0||source.x<0||source.y<0||source.x>=texture.width||source.y>=texture.height)
            return false;
        var display=surface.displayRect;var r=display.rect;float u=source.x/texture.width,v=source.y/texture.height;if(surface.flipY)v=1-v;
        var canvas=display.GetComponentInParent<Canvas>();var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
        point=RectTransformUtility.WorldToScreenPoint(camera,display.TransformPoint(new Vector3(r.xMin+u*r.width,r.yMin+v*r.height,0)));
        if(!surface.TryMap(point,out var mapped)||Vector2.Distance(mapped,new Vector2(source.x,source.y))>=1)
            throw new InvalidOperationException("Actual LCD coordinate roundtrip failed: "+rect.name);
        return true;
    }
    static RaycastResult LcdHit(Vector2 point)
    {
        var surface=LcdSurface();surface.OnPointerEnter(new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left});
        var hover=Field<PointerEventData>(surface,"hoverData");return hover==null?default:hover.pointerCurrentRaycast;
    }
    static void RequireTop(RectTransform rect,PointerEventData e)
    {
        var hit=LcdHit(e.position).gameObject;
        bool ok=hit!=null&&(hit==rect.gameObject||hit.transform.IsChildOf(rect));
        if(!ok)SaveHitEvidence(rect,e,"actual-hit-failure");
        Require(ok,"Top actual LCD target: "+rect.name+" / hit="+(hit==null?"none":hit.name)+" / physical="+e.position);
    }
    static void SaveHitEvidence(RectTransform target,PointerEventData e,string tag)
    {
        string stem=tag+"-"+Time.frameCount;
        var text=new System.Text.StringBuilder();var surface=LcdSurface();
        surface.TryMap(e.position,out var source);
        text.AppendLine("frame="+Time.frameCount+" physical="+e.position+" mappedSource="+source+" target="+Hierarchy(target));
        for(Transform t=target;t!=null;t=t.parent)
        {
            text.Append("ancestor="+Hierarchy(t)+" active="+t.gameObject.activeInHierarchy+" layer="+t.gameObject.layer);
            var r=t as RectTransform;if(r!=null)text.Append(" rect="+r.rect+" position="+r.anchoredPosition+" scale="+r.lossyScale);
            var group=t.GetComponent<CanvasGroup>();if(group!=null)text.Append(" group alpha="+group.alpha+" interactable="+group.interactable+" blocks="+group.blocksRaycasts+" ignoreParents="+group.ignoreParentGroups);
            var mask=t.GetComponent<RectMask2D>();if(mask!=null)text.Append(" mask active="+mask.isActiveAndEnabled);
            var canvas=t.GetComponent<Canvas>();if(canvas!=null)text.Append(" canvas enabled="+canvas.enabled+" override="+canvas.overrideSorting+" layer="+canvas.sortingLayerID+" order="+canvas.sortingOrder+" camera="+(canvas.worldCamera==null?"none":canvas.worldCamera.name));
            var graphic=t.GetComponent<Graphic>();if(graphic!=null)text.Append(" graphic enabled="+graphic.enabled+" target="+graphic.raycastTarget+" depth="+graphic.depth+" culled="+graphic.canvasRenderer.cull+" alpha="+graphic.color.a);
            text.AppendLine();
        }
        var best=Field<PointerEventData>(surface,"hoverData")?.pointerCurrentRaycast;
        text.AppendLine("selected="+(best.HasValue&&best.Value.gameObject!=null?Hierarchy(best.Value.gameObject.transform):"none"));
        foreach(var hit in Field<List<RaycastResult>>(surface,"results"))
            text.AppendLine("candidate="+(hit.gameObject==null?"none":Hierarchy(hit.gameObject.transform))+" depth="+hit.depth+" sortingLayer="+hit.sortingLayer+" order="+hit.sortingOrder+" distance="+hit.distance+" module="+(hit.module==null?"none":Hierarchy(hit.module.transform)));
        foreach(var raycaster in surface.sourceRaycasters)
            if(raycaster!=null)text.AppendLine("sourceRaycaster="+Hierarchy(raycaster.transform)+" active="+raycaster.gameObject.activeInHierarchy+" enabled="+raycaster.enabled);
        File.WriteAllText(Path.Combine(output,stem+".txt"),text.ToString());
        Capture(stem+".png",hub.transform as RectTransform);
    }
    static string Hierarchy(Transform t)
    {string path=t.name;while(t.parent!=null){t=t.parent;path=t.name+"/"+path;}return path;}
    static void ClickRect(RectTransform rect,int count=1)
    {
        var button=rect.GetComponent<Button>();if(button!=null)Require(button.IsActive()&&button.IsInteractable(),"Native control enabled: "+rect.name);
        for(int i=0;i<count;i++){var e=Pointer(rect);RequireTop(rect,e);FilesWindowUiChecks.Click(rect);}
    }
    // Arrange the existing overlapping windows by the same native caption drag a player uses.
    // Never change frame rectangles, sorting, raycast flags, focus, or the installation transaction here.
    static IEnumerator DragFilesToLeft(XgFilesWindow files)
    {
        var window=files.window;var dragger=window.windowDragger;
        Require(dragger!=null&&dragger.isActiveAndEnabled&&!window.isFullscreen,"Files has a live normal-window caption dragger");
        var caption=(RectTransform)dragger.transform;var frame=dragger.dragObject;var area=dragger.dragArea;
        Require(frame!=null&&area!=null,"Native caption declares its actual window frame and drag bounds");
        Canvas.ForceUpdateCanvases();var corners=new Vector3[4];frame.GetWorldCorners(corners);
        float beforeLeft=corners.Min(p=>area.InverseTransformPoint(p).x);
        float deltaX=area.rect.xMin+16-beforeLeft;var beforeSize=frame.rect.size;
        if(Mathf.Abs(deltaX)<2)yield break;
        Vector2 start=Vector2.zero;bool exposed=false;
        foreach(float fraction in new[]{.25f,.4f,.55f,.7f})
        {
            var local=new Vector2(Mathf.Lerp(caption.rect.xMin,caption.rect.xMax,fraction),caption.rect.center.y);
            if(!TryLcdPoint(caption,local,out var point))continue;
            var hit=LcdHit(point).gameObject;
            if(hit==null||ExecuteEvents.GetEventHandler<IDragHandler>(hit)!=dragger.gameObject)continue;
            start=point;exposed=true;break;
        }
        if(!exposed)SaveHitEvidence(caption,Pointer(caption),"files-caption-occluded");
        Require(exposed,"Actual LCD top hit owns the native Files caption drag handler");
        var surface=LcdSurface();Require(surface.TryMap(start,out var sourceStart),"Files caption maps into source pixels");
        Require(RectTransformUtility.ScreenPointToLocalPointInRectangle(area,sourceStart,surface.sourceCamera,out var startLocal),"Files caption maps into native drag coordinates");
        var e=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=start};Vector2 dragOrigin=startLocal;
        RequireTop(caption,e);surface.OnPointerDown(e);
        try
        {
            // The native begin-drag captures its origin at the threshold-crossing sample.
            // The LCD drag threshold is in physical screen pixels scaled to render-texture pixels (2026-10-22 mouse fix),
            // so it depends on the Game view size: step right until the real dragger reports a live drag, then use
            // that sample as the origin and move by the desired window offset across real frames.
            bool begun=false;
            for(int probe=1;probe<=12&&!begun;probe++)
            {
                dragOrigin=startLocal+new Vector2(20*probe,0);
                e.position=LcdPoint(area,dragOrigin);surface.OnDrag(e);yield return null;
                // The surface's own left-button state (WindowDragger keeps its flag after a previous drag).
                begun=Field<PointerEventData>(((Array)Field<object>(surface,"buttons")).GetValue(0),"data").dragging;
            }
            Require(begun,"Native caption drag crosses the LCD drag threshold");
            for(int step=1;step<=8;step++)
            {
                e.position=LcdPoint(area,dragOrigin+new Vector2(deltaX*step/8f,0));
                surface.OnDrag(e);yield return null;
            }
        }
        finally{surface.OnPointerUp(e);}
        yield return null;Canvas.ForceUpdateCanvases();frame.GetWorldCorners(corners);
        float afterLeft=corners.Min(p=>area.InverseTransformPoint(p).x);
        if(Mathf.Abs(afterLeft-(area.rect.xMin+16))>=2)Debug.LogWarning("[BoardGamesUiPlayQa] Files drag: before="+beforeLeft+" after="+afterLeft+" target="+(area.rect.xMin+16)+" origin="+dragOrigin);
        Require(Mathf.Abs(afterLeft-(area.rect.xMin+16))<2,"Actual LCD title drag places Files at the desktop left margin");
        Require(Vector2.Distance(frame.rect.size,beforeSize)<.1f,"Native window drag preserves its full original size");
        Require(window.isOn&&ctl.Window.isOn,"Files and Train remain open after native window arrangement");
    }
    // Explicit fixtures may legitimately discover cards when the lab first opens.
    // Accept them through their exposed background control before starting a drag.
    static IEnumerator CollectVisibleCards()
    {
        double until=EditorApplication.timeSinceStartup+45;
        int quietFrames=0;
        while(quietFrames<3)
        {
            Require(EditorApplication.timeSinceStartup<until,"Pending cards finish through their real collect controls");
            var overlay=ctl.View.GetComponentsInChildren<RectTransform>(false).FirstOrDefault(r=>r.name=="Card Overlay");
            if(overlay==null){quietFrames++;yield return null;continue;}
            quietFrames=0;
            double ready=EditorApplication.timeSinceStartup+.8;
            while(EditorApplication.timeSinceStartup<ready)yield return null;
            if(!overlay.gameObject.activeInHierarchy)continue;
            bool clicked=false;
            for(int y=1;y<10&&!clicked;y++)for(int x=1;x<10&&!clicked;x++)
            {
                var local=new Vector2(Mathf.Lerp(overlay.rect.xMin,overlay.rect.xMax,x/10f),Mathf.Lerp(overlay.rect.yMin,overlay.rect.yMax,y/10f));
                if(!TryLcdPoint(overlay,local,out var point)||LcdHit(point).gameObject!=overlay.gameObject)continue;
                var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=point};
                var surface=LcdSurface();surface.OnPointerDown(pointer);surface.OnPointerUp(pointer);clicked=true;
            }
            Require(clicked,"Real card background has an exposed LCD collect point");
            ready=EditorApplication.timeSinceStartup+.8;
            while(EditorApplication.timeSinceStartup<ready)yield return null;
        }
    }
    static Vector2 DropPoint(XgFilesTrainDrop target)
    {
        var rect=(RectTransform)target.transform;
        for(int y=1;y<10;y++)for(int x=1;x<10;x++)
        {
            if(!TryLcdPoint(rect,new Vector2(Mathf.Lerp(rect.rect.xMin,rect.rect.xMax,x/10f),Mathf.Lerp(rect.rect.yMin,rect.rect.yMax,y/10f)),out var point))continue;
            var hit=LcdHit(point).gameObject;
            if(hit!=null&&hit.GetComponentInParent<XgFilesTrainDrop>()==target)return point;
        }
        SaveHitEvidence(rect,Pointer(rect),"train-drop-occluded");
        throw new InvalidOperationException("No exposed native Train drop point; do not bypass window occlusion");
    }
    static void Inspect(TMP_Text label,string name)
    {
        label.ForceMeshUpdate(true,true);var info=label.textInfo;var rect=label.rectTransform.rect;
        var row=new TextResult{field=name,language=language,text=label.text,characters=info.characterCount,truncated=label.isTextTruncated,overflow=label.isTextOverflowing,contained=true};
        for(int i=0;i<info.characterCount;i++)
        {
            var c=info.characterInfo[i];if(!char.IsWhiteSpace(c.character)&&(c.textElement==null||c.textElement.unicode!=c.character))row.missingGlyph=true;
            if(!c.isVisible)continue;row.visible++;if(c.bottomLeft.x<rect.xMin-.5f||c.topRight.x>rect.xMax+.5f||c.bottomLeft.y<rect.yMin-.5f||c.topRight.y>rect.yMax+.5f){row.contained=false;row.boundsFailure+="U+"+((int)c.character).ToString("X4")+" bottomLeft="+c.bottomLeft+" topRight="+c.topRight+" rect="+rect+"; ";}
        }
        labels.Add(row);Check(row.visible>0&&!row.truncated&&!row.overflow&&!row.missingGlyph&&row.contained,"Complete visible glyphs contained: "+name);
    }
    static void Capture(string name,RectTransform view)
    {
        Canvas.ForceUpdateCanvases();var camera=view.GetComponentInParent<Canvas>().rootCanvas.worldCamera;var target=camera!=null?camera.targetTexture:null;Require(target!=null,"Actual Main LCD render target");
        var previous=RenderTexture.active;var texture=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        try{RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(output,name),texture.EncodeToPNG());}
        finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(texture);}
    }
    static void Tick()
    {
        if(!running||lastFrame==Time.frameCount)return;lastFrame=Time.frameCount;
        try
        {
            if(errors.Count>0)throw new Exception(errors[0]);if(!EditorApplication.isPlaying)throw new Exception("Play ended early");if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Board native QA exceeded total deadline");
            if(!routine.MoveNext())Finish(failed.Count==0?"passed":"failed",SelectedDetail);
        }
        catch(Exception error){Finish("failed",error.ToString());}
    }
    static void Finish(string status,string detail)
    {
        running=false;EditorApplication.update-=Boot;EditorApplication.update-=Tick;locale?.Dispose();locale=null;
        File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(new Report{status=status,detail=detail,passed=passed,failed=failed,errors=errors,infrastructure=infrastructure,labels=labels},true));
        if(status=="passed")File.WriteAllText(Path.Combine(output,"passed.txt"),detail);Application.logMessageReceived-=Log;EditorApplication.ExitPlaymode();
    }
}
#endif
