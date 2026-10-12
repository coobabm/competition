using System;
using System.IO;
using System.Linq;
using System.Reflection;
using HongmengOS.Aero2010;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.YY;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Scene verification and screenshots use isolated saves. Run without -quit or -nographics.
[InitializeOnLoad]
public static class NamingDesktopSmoke
{
    const string Key = "NamingDesktopSmoke.Active";
    static string output => Path.GetFullPath("LocalVerification/Naming/DesktopSmoke");
    static int step;
    static double due;
    static string taskbarKey;
    static XgSetupOverlay setup;
    static NamingDesktopSmoke() { EditorApplication.update += Update; }
    public static void Run()
    {
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "result.txt"), "RUNNING");
        EditorSceneManager.OpenScene("Assets/LingGuangV05/Scenes/Main.unity");
        UnityEngine.Object.FindAnyObjectByType<ChapterOneRuntime>().useDiskSave = false;
        Application.runInBackground = true;
        var view = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
        view.Show(); view.Focus();
        SessionState.SetBool(Key, true);
        EditorApplication.isPlaying = true;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Isolate()
    {
        if (!SessionState.GetBool(Key, false)) return;
        ChapterOneRuntime.VerificationSaveDirectory = Path.Combine(output, "Save");
        var path = Path.Combine(output, "DesktopProfile") + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(path);
        typeof(Michsky.DreamOS.DreamOSDataManager).GetField("dataPath", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, path);
    }
    static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); File.AppendAllText(Path.Combine(output, "checks.txt"), message + "\n"); }
    static void Shot(string name) { Canvas.ForceUpdateCanvases(); ScreenCapture.CaptureScreenshot(Path.Combine(output, name + ".png")); }
    static ChapterOneDesktopBridge Bridge() => UnityEngine.Object.FindObjectsByType<ChapterOneDesktopBridge>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(b => b.ApplicationId == "lingguang");
    static void VerifyIdentity(string name)
    {
        var b = Bridge(); var controller = UnityEngine.Object.FindAnyObjectByType<XingGuangController>();
        Check(b.DesktopShortcut.GetComponentsInChildren<TMP_Text>(true).Any(t => t.text == name + ".exe"), "Desktop shows " + name);
        Check(b.TaskbarShortcut.headerButton.buttonText == name + ".exe", "Taskbar shows " + name);
        Check(b.TaskbarShortcut.buttonTitle == taskbarKey, "Taskbar persistence key unchanged");
        Check(controller.Sim.AiDisplayName == name && controller.View.AiName == name, "AI view and dialogue profile agree");
        Check(controller.Window.GetComponentsInChildren<TMP_Text>(true).Any(t => t.text.StartsWith(name + ".exe")), "Window caption follows name");
        var menu = UnityEngine.Object.FindAnyObjectByType<AeroStartMenu>(FindObjectsInactive.Include);
        var entry = menu.programs.First(p => p.window == b.Window);
        Check(entry.title == name + ".exe", "Start menu follows name");
        menu.SetSearch(name); Check(entry.row.activeSelf, "Start menu finds new name"); menu.SetSearch("");
        Check(YYChatHub.Contact(YYChatHub.LingGuangId).name == name, "YY contact follows name");
    }
    static void Update()
    {
        if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        if (due == 0) { due = EditorApplication.timeSinceStartup + 12; return; }
        if (EditorApplication.timeSinceStartup < due) return;
        due = EditorApplication.timeSinceStartup + 4;
        try
        {
            var runtime = UnityEngine.Object.FindAnyObjectByType<ChapterOneRuntime>();
            var controller = UnityEngine.Object.FindAnyObjectByType<XingGuangController>();
            switch (step++)
            {
                case 0:
                    Check(runtime != null && !runtime.useDiskSave, "Player save is isolated");
                    var prologue = UnityEngine.Object.FindAnyObjectByType<PrologueDirector>();
                    if (prologue != null) { prologue.StopAllCoroutines(); prologue.enabled = false; }
                    var story = UnityEngine.Object.FindAnyObjectByType<LingGuangV05.Runtime.Story.StoryDirector>();
                    if (story != null) story.enabled = false;
                    runtime.Sim.InstallApp(); controller.Open();
                    taskbarKey = Bridge().TaskbarShortcut.buttonTitle;
                    break;
                case 1:
                    setup = Field<XgSetupOverlay>(controller.View, "setup");
                    Field<TMP_InputField>(setup, "name").text = "小白.exe";
                    Field<TMP_InputField>(setup, "mood").text = "好奇又诚实";
                    typeof(XgSetupOverlay).GetMethod("Submit", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(setup, null);
                    Check(runtime.Sim.InPrologue && Field<TMP_Text>(setup, "error").text.Length > 0, "Invalid opening name shows a visible error");
                    Shot("01-name-error"); break;
                case 2:
                    Field<TMP_InputField>(setup, "name").text = "小白";
                    typeof(XgSetupOverlay).GetMethod("Submit", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(setup, null);
                    due += 12; break;
                case 3:
                    Check(!runtime.Sim.InPrologue && runtime.Sim.S.aiName == "小白", "Opening form accepts chosen name");
                    VerifyIdentity("小白"); Shot("02-personal-name");
                    Check(runtime.RecordName("小星"), "Rename accepted"); break;
                case 4:
                    VerifyIdentity("小星"); Shot("03-renamed");
                    Check(runtime.RecordName("一二三四五六七八九十一二三四五六"), "Sixteen character name accepted"); break;
                case 5:
                    VerifyIdentity("一二三四五六七八九十一二三四五六"); Shot("04-long-name");
                    var restored = new ChapterOneSim(JsonUtility.FromJson<GameState>(JsonUtility.ToJson(runtime.Sim.S)));
                    restored.S.aiName = "读档测试";
                    runtime.SetSimulationForTests(restored); break;
                case 6:
                    VerifyIdentity("读档测试"); Shot("05-restored-name");
                    GameExit.ReturnToTitle(); due += 15; break;
                case 7:
                    var hud = UnityEngine.Object.FindAnyObjectByType<LingGuangV05.CaseMenu.CaseMenuHud>();
                    var title = hud.GetComponentsInChildren<TMP_Text>(true).First(t => t.text == AppNames.GameZh);
                    Canvas.ForceUpdateCanvases(); title.ForceMeshUpdate();
                    Check(!title.isTextOverflowing, "Fixed game title fits menu");
                    Shot("06-game-title");
                    File.WriteAllText(Path.Combine(output, "result.txt"), "PASS: opening validation, personal name, rename, long name, restored state, stable app IDs, menu title. Inspect screenshots visually.");
                    break;
                default: Finish(0); break;
            }
        }
        catch (Exception e) { File.WriteAllText(Path.Combine(output, "result.txt"), "FAIL: " + e); Debug.LogException(e); Finish(1); }
    }
    static void Finish(int code) { SessionState.SetBool(Key, false); EditorApplication.isPlaying = false; EditorApplication.Exit(code); }
}
