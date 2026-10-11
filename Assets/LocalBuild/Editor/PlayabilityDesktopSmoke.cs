using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Core;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Local authoring check only. Never included in a player and never reads the player's save.
[InitializeOnLoad]
public static class PlayabilityDesktopSmoke
{
    const string Key = "PlayabilityDesktopSmoke.Active";
    static int step;
    static double due, started, trainStarted;
    static bool modelAnswered, modelSucceeded;
    static int yyBefore;
    static int wallpaperBefore, wallpaperAfter;
    static string discussionId;
    static double discussionSent;
    static string output => Path.GetFullPath("LocalVerification/ClaudeHandoff/DesktopSmoke");
    static PlayabilityDesktopSmoke() { EditorApplication.update += Update; }

    public static void Run()
    {
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "result.txt"), "RUNNING");
        EditorSceneManager.OpenScene("Assets/LingGuangV05/Scenes/Main.unity");
        var runtime = UnityEngine.Object.FindAnyObjectByType<ChapterOneRuntime>();
        if (runtime == null) throw new Exception("Main runtime missing");
        runtime.useDiskSave = false;
        Application.runInBackground = true;
        var gameView = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
        gameView.Show(); gameView.Focus();
        SessionState.SetBool(Key, true);
        EditorApplication.isPlaying = true;
    }

    public static void Validate()
    {
        ClaudeHandoffChecks.Validate();
        Run();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void IsolateDesktop()
    {
        if (!SessionState.GetBool(Key, false)) return;
        ChapterOneRuntime.VerificationSaveDirectory = Path.Combine(output, "Save");
        var path = Path.Combine(output, "DesktopProfile") + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(path);
        typeof(Michsky.DreamOS.DreamOSDataManager).GetField("dataPath", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, path);
    }

    static void Shot(string name)
    {
        Canvas.ForceUpdateCanvases();
        ScreenCapture.CaptureScreenshot(Path.Combine(output, name + ".png"));
    }

    static void Update()
    {
        if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        if (started == 0) { started = EditorApplication.timeSinceStartup; due = started + 12; }
        if (EditorApplication.timeSinceStartup < due) return;
        due = EditorApplication.timeSinceStartup + 4;
        try
        {
            var runtime = UnityEngine.Object.FindAnyObjectByType<ChapterOneRuntime>();
            var controller = UnityEngine.Object.FindAnyObjectByType<XingGuangController>();
            switch (step++)
            {
                case 0:
                    if (runtime == null || runtime.useDiskSave) throw new Exception("Save isolation failed");
                    var prologue = UnityEngine.Object.FindAnyObjectByType<PrologueDirector>();
                    if (prologue != null) { prologue.StopAllCoroutines(); prologue.enabled = false; }
                    var story = UnityEngine.Object.FindAnyObjectByType<LingGuangV05.Runtime.Story.StoryDirector>();
                    if (story != null) story.enabled = false;
                    if (!runtime.CompletePrologue(new PrologueProfile { name = "灵光", self = "我", callMe = "你", personality = "好奇，诚实" })) throw new Exception("Setup failed");
                    YYChatHub.Instance.S.lingguangUnlocked = true;
                    controller.Open(); controller.View.ShowTab("teach");
                    break;
                case 1:
                    DumpUi(controller);
                    Shot("01-teach");
                    break;
                case 2:
                    for (int i = 0; i < 4; i++) { var c = controller.Sim.Card("spam"); controller.Sim.Answer("spam", c.truth, controller.Host); }
                    if (!controller.Sim.BeginEpoch(XgTrack.Sequence, controller.Host)) throw new Exception("Real host cannot begin training");
                    trainStarted = EditorApplication.timeSinceStartup;
                    break;
                case 3:
                    if (controller.Sim.S.epochs < 1 && EditorApplication.timeSinceStartup - trainStarted < 60) { step--; return; }
                    if (controller.Sim.S.epochs < 1) throw new Exception("Real host training did not complete");
                    controller.View.ShowTab("train");
                    break;
                case 4:
                    Shot("02-train");
                    break;
                case 5:
                    XgYyTalk.Open();
                    break;
                case 6:
                    Shot("03-chat");
                    break;
                case 7:
                    controller.View.ShowTab("board");
                    break;
                case 8:
                    Shot("04-brain");
                    break;
                case 9:
                    // Explicitly synthetic stage-4 fixture, only for a live local-model conversation check.
                    controller.Sim.S.abilitiesEmerged = new List<int> { 1, 2, 3, 4 };
                    controller.Sim.S.stage = 4;
                    controller.Sim.S.firstWordsSaid = true;
                    XgYyTalk.Open();
                    var llm = LingGuangV05.Desktop.LLM.LocalLlm.Instance;
                    if (llm == null || !llm.Ready) throw new Exception("Local model is not ready for live dialogue check");
                    string question = "刚才我们练了识别中奖短信。你想问我什么？";
                    controller.Sim.AddLine("me", question);
                    llm.Chat(new List<KeyValuePair<string, string>> {
                        new KeyValuePair<string, string>("system", XgYyTalk.StableSystem(controller.Sim, new DateTime(2016, 8, 1))),
                        new KeyValuePair<string, string>("user", question)
                    }, 64, .7f, raw => {
                        modelAnswered = true;
                        modelSucceeded = !string.IsNullOrWhiteSpace(raw);
                        string shown = controller.Sim.Gate(raw, question, new System.Random(42));
                        File.WriteAllText(Path.Combine(output, "local-model.txt"), "Synthetic stage-4 dialogue fixture. Raw local-model reply:\n" + raw + "\nDisplayed after learned ability gate:\n" + shown);
                        controller.Sim.AddLine("ai", shown);
                        controller.View.Refresh(true);
                    }, seat: LingGuangV05.Core.Chat.LlmSeat.LingGuang);
                    due = EditorApplication.timeSinceStartup + 30;
                    break;
                case 10:
                    if (!modelAnswered && EditorApplication.timeSinceStartup - started < 240) { step--; return; }
                    if (!modelAnswered) throw new Exception("Local-model reply timed out");
                    if (!modelSucceeded) throw new Exception("Local model returned no text");
                    Shot("05-live-dialogue");
                    break;
                case 11:
                    yyBefore = YYChatHub.Instance.Conversation(YYChatHub.LingGuangId).messages.Count;
                    YYChatHub.Instance.Send(YYChatHub.LingGuangId, "你是怎么分辨垃圾短信的？");
                    due = EditorApplication.timeSinceStartup + 25;
                    break;
                case 12:
                    var added = YYChatHub.Instance.Conversation(YYChatHub.LingGuangId).messages.Skip(yyBefore).ToList();
                    if (!added.Any(m => m.from == YYChatHub.LingGuangId) && EditorApplication.timeSinceStartup - started < 260) { step--; return; }
                    if (added.Count(m => m.from == YYChatHub.Me) != 1 || added.Count(m => m.from == YYChatHub.LingGuangId) != 1) throw new Exception("YY route must deliver exactly one question and one reply");
                    File.WriteAllText(Path.Combine(output, "yy-dialogue.txt"), string.Join("\n", added.Select(m => m.from + ": " + m.text)));
                    Shot("06-yy-dialogue");
                    break;
                case 13:
                    var saves = runtime.GetComponent<SaveSlotsPanel>();
                    if (saves == null) throw new Exception("Settings save panel missing");
                    saves.Open(); break;
                case 14:
                    if (!SaveSlotsPanel.IsOpen) throw new Exception("Save panel failed to open");
                    Shot("07-save-slots"); break;
                case 15:
                    typeof(SaveSlotsPanel).GetMethod("Close", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(runtime.GetComponent<SaveSlotsPanel>(), null);
                    controller.Open(); controller.View.ShowTab("items");
                    var itemPages = (Dictionary<string, XgPage>)typeof(XingGuangView).GetField("pages", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controller.View);
                    var items = itemPages["items"];
                    typeof(XgItemsPage).GetMethod("OpenDetail", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(items, new object[] { "LSTM", "记住有用的，忘掉无关的", XgItemLore.Get("lstm", false) });
                    break;
                case 16:
                    Shot("08-item-lore"); break;
                case 17:
                    controller.Open(); controller.View.ShowTab("train");
                    var fundsButton=controller.View.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b=>b.name=="OpenFundsBook");
                    fundsButton.onClick.Invoke();
                    break;
                case 18:
                    Shot("09-funds-book"); break;
                case 19:
                    runtime.transform.Find("FundsCanvas")?.gameObject.SetActive(false);
                    var wallpapers=UnityEngine.Object.FindAnyObjectByType<Michsky.DreamOS.WallpaperManager>();
                    if(wallpapers==null || wallpapers.wallpaperLibrary.wallpapers.Count<2) throw new Exception("Wallpaper library missing");
                    wallpaperBefore=wallpapers.wallpaperIndex; wallpaperAfter=(wallpaperBefore+1)%wallpapers.wallpaperLibrary.wallpapers.Count;
                    wallpapers.SetWallpaper(wallpaperAfter,true);
                    break;
                case 20:
                    var bridge=UnityEngine.Object.FindAnyObjectByType<HongmengOS.Aero2010.AeroLcdSourceBridge>();
                    var wm=UnityEngine.Object.FindAnyObjectByType<Michsky.DreamOS.WallpaperManager>();
                    var background=bridge.sourceCanvas.transform.Find("Desktop/Wallpaper").GetComponent<UnityEngine.UI.Image>();
                    if(background.sprite!=wm.GetWallpaper(wallpaperAfter) || wm.GetWallpaper(wallpaperBefore)==wm.GetWallpaper(wallpaperAfter)) throw new Exception("Visible desktop wallpaper did not change");
                    Shot("10-changed-wallpaper");
                    break;
                case 21:
                    var forum=LingGuangV05.Desktop.Tieba.TiebaHub.Instance;
                    var thread=forum.Library.Threads.First(t=>!t.dead && LingGuangV05.Core.Forum.ForumLibrary.Visible(t,forum.Context()));
                    discussionId="thread:"+thread.id;
                    if(!forum.CanMessage(discussionId) || forum.CanMessage("thread:missing")) throw new Exception("Discussion access gate incorrect");
                    forum.View.Open("chat",discussionId);
                    forum.Send(discussionId,"主楼我看了，你觉得新手应该先注意什么？");
                    discussionSent=EditorApplication.timeSinceStartup; due=discussionSent+12;
                    break;
                case 22:
                    var fh=LingGuangV05.Desktop.Tieba.TiebaHub.Instance;
                    var conversation=fh.Conversation(discussionId);
                    if(!conversation.messages.Any(m=>m.from!=LingGuangV05.Core.Forum.ForumLibrary.Me)) {
                        if(EditorApplication.timeSinceStartup-discussionSent<120){step--;return;}
                        throw new Exception("Public discussion never replied");
                    }
                    if(conversation.messages.Count!=2) throw new Exception("Discussion duplicated a reply");
                    var restored=JsonUtility.FromJson<LingGuangV05.Core.Forum.ForumState>(JsonUtility.ToJson(fh.S));
                    if(restored.Conversation(discussionId).messages.Count!=2) throw new Exception("Discussion persistence failed");
                    Shot("11-public-discussion"); break;
                case 23:
                    GameExit.ReturnToTitle(); due = EditorApplication.timeSinceStartup + 16;
                    break;
                case 24:
                    var menu = UnityEngine.Object.FindAnyObjectByType<LingGuangV05.CaseMenu.CaseMenuController>();
                    if (menu == null || menu.HasEnteredGame || !SceneManager.GetSceneByName(GameExit.TitleScene).isLoaded) throw new Exception("Shutdown did not return to title");
                    Shot("12-title-after-shutdown");
                    menu.BeginGame();due=EditorApplication.timeSinceStartup+16;break;
                case 25:
                    var entered=UnityEngine.Object.FindAnyObjectByType<LingGuangV05.CaseMenu.CaseMenuController>();
                    if(entered==null || !entered.HasEnteredGame) throw new Exception("Real menu transition did not enter desktop");
                    var display=entered.GetComponent<LingGuangV05.CaseMenu.CaseMenuMainDisplay>();
                    var camera=entered.menuCamera;
                    if(camera.clearFlags!=CameraClearFlags.SolidColor) throw new Exception("Room backdrop still visible");
                    Shot("13-menu-entered-no-room-gap");
                    File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: isolated desktop; first lessons; real GPU timed training; Qwen dialogue and exactly-once YY reply; save slots; item lore; funds book; actual desktop wallpaper sprite switch; visible forum thread discussion and persistence; shutdown and real menu-to-desktop transition. Screenshots require visual inspection.");
                    break;
                default: Finish(); break;
            }
        }
        catch (Exception e) { File.WriteAllText(Path.Combine(output, "result.txt"), "FAIL: " + e); Debug.LogException(e); Finish(1); }
    }

    static void DumpUi(XingGuangController controller)
    {
        string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
        var lines = new List<string>();
        foreach (var g in UnityEngine.Object.FindObjectsByType<CanvasGroup>(FindObjectsSortMode.None))
            if (g.alpha < .99f) lines.Add("GROUP " + PathOf(g.transform) + " alpha=" + g.alpha);
        foreach (var graphic in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Graphic>(FindObjectsSortMode.None))
        {
            if (graphic.rectTransform.rect.width > 700 && graphic.rectTransform.rect.height > 400)
                lines.Add("GRAPHIC " + PathOf(graphic.transform) + " color=" + graphic.color + " material=" + graphic.material?.name);
        }
        foreach (var t in controller.View.Teach.root.GetComponentsInChildren<TMPro.TMP_Text>(true))
            lines.Add("TEXT " + PathOf(t.transform) + " color=" + t.color + " face=" + t.faceColor + " material=" + t.fontSharedMaterial?.name);
        File.WriteAllLines(Path.Combine(output, "ui-diagnostics.txt"), lines);
        foreach (var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (camera.targetTexture == null) continue;
            var target = camera.targetTexture; var old = RenderTexture.active;
            RenderTexture.active = target;
            var shot = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            shot.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); shot.Apply();
            File.WriteAllBytes(Path.Combine(output, "camera-" + camera.name.Replace("/", "_") + ".png"), shot.EncodeToPNG());
            UnityEngine.Object.Destroy(shot); RenderTexture.active = old;
        }
    }

    static void Finish(int exitCode = 0)
    {
        SessionState.SetBool(Key, false);
        EditorApplication.isPlaying = false;
        EditorApplication.Exit(exitCode);
    }
}
