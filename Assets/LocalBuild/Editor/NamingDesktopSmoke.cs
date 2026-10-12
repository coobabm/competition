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
using UnityEngine.UI;
using UnityEngine.EventSystems;
using LingGuangV05.CaseMenu;

// Scene verification and screenshots use isolated saves. Run without -quit or -nographics.
[InitializeOnLoad]
public static class NamingDesktopSmoke
{
    const string Key = "NamingDesktopSmoke.Active";
    static string output => Path.GetFullPath("LocalVerification/Naming/DesktopSmoke");
    static int step;
    static int expansionSamples;
    static RenderTexture expansionTexture;
    static float previousExpansion;
    static CaseMenuMainDisplay observedDisplay;
    static double due;
    static string taskbarKey;
    static XgSetupOverlay setup;
    static Michsky.DreamOS.WindowManager browser;
    static Vector2 browserSize, browserPosition;
    static NamingDesktopSmoke() { EditorApplication.update += Update; }
    public static void Validate() { NamingChecks.Validate(); Run(); }
    public static void RunFullscreen() { SessionState.SetBool("NamingDesktopSmoke.FullscreenOnly", true); Run(); }
    public static void Run()
    {
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "result.txt"), "RUNNING");
        File.WriteAllText(Path.Combine(output, "checks.txt"), "");
        EditorSceneManager.OpenScene(SessionState.GetBool("NamingDesktopSmoke.FullscreenOnly", false) ? "Assets/LingGuangV05/Scenes/ComputerCaseMenu.unity" : "Assets/LingGuangV05/Scenes/Main.unity");
        var initialRuntime = UnityEngine.Object.FindAnyObjectByType<ChapterOneRuntime>();
        if (initialRuntime != null) initialRuntime.useDiskSave = false;
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
    // CaptureScreenshot completes at the end of the rendered frame. Defer viewport/scene
    // changes so their intermediate layout cannot contaminate the screenshot just requested.
    static void AfterShot(Action action) => UnityEngine.Object.FindAnyObjectByType<ChapterOneRuntime>().StartCoroutine(AfterCapture(action));
    static System.Collections.IEnumerator AfterCapture(Action action)
    {
        yield return new WaitForEndOfFrame();
        yield return null;
        action();
    }
    static ChapterOneDesktopBridge Bridge() => UnityEngine.Object.FindObjectsByType<ChapterOneDesktopBridge>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(b => b.ApplicationId == "lingguang");
    static void VerifyIdentity(string name)
    {
        var b = Bridge(); var controller = UnityEngine.Object.FindAnyObjectByType<XingGuangController>();
        Check(b.DesktopShortcut.GetComponentsInChildren<TMP_Text>(true).Any(t => t.text == name + ".exe"), "Desktop shows " + name);
        Check(b.TaskbarShortcut.headerButton.buttonText == name + ".exe", "Taskbar shows " + name);
        Check(b.TaskbarShortcut.buttonTitle == taskbarKey, "Taskbar persistence key unchanged");
        Check(controller.Sim.AiDisplayName == name && controller.View.AiName == name, "AI view and dialogue profile agree");
        Check(controller.Window.GetComponentsInChildren<TMP_Text>(true).Any(t => t != null && (t.text ?? "").StartsWith(name + ".exe")), "Window caption follows name");
        var menu = UnityEngine.Object.FindAnyObjectByType<AeroStartMenu>(FindObjectsInactive.Include);
        var entry = menu.programs.First(p => p.window == b.Window);
        Check(entry.title == name + ".exe", "Start menu follows name");
        menu.SetSearch(name); Check(entry.row.activeSelf, "Start menu finds new name"); menu.SetSearch("");
        Check(YYChatHub.Contact(YYChatHub.LingGuangId).name == name, "YY contact follows name");
    }
    static void Update()
    {
        if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        try { ObserveExpansion(); }
        catch (Exception e) { File.WriteAllText(Path.Combine(output, "result.txt"), "FAIL: " + e); Debug.LogException(e); Finish(1); return; }
        if (due == 0) { if (SessionState.GetBool("NamingDesktopSmoke.FullscreenOnly", false)) step = 12; due = EditorApplication.timeSinceStartup + 12; return; }
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
                    foreach (var window in UnityEngine.Object.FindObjectsByType<Michsky.DreamOS.WindowManager>(FindObjectsInactive.Include))
                    {
                        if (window.windowContainer == null) continue;
                        var area = ((RectTransform)window.windowContainer.parent).rect.size;
                        var actual = window.windowContainer.rect.size;
                        Check(Vector2.Distance(actual, DesktopWindowDefaults.SizeFor(window.name, area)) < 3, "Default size: " + window.name + " = " + actual);
                        Check(actual.x < area.x * .9f && actual.y < area.y * .9f, "Desktop remains visible around " + window.name);
                    }
                    controller.View.ShowTab("train");
                    browser = UnityEngine.Object.FindObjectsByType<Michsky.DreamOS.WindowManager>(FindObjectsInactive.Include).First(w => w.name == "Web Browser");
                    browser.OpenWindow();
                    UnityEngine.Object.FindAnyObjectByType<ChapterOneDesktopRouter>().Open("yy");
                    break;
                case 7:
                    Shot("06-stacked-windows");
                    browserSize = browser.windowContainer.sizeDelta; browserPosition = browser.windowContainer.anchoredPosition;
                    browser.FullscreenWindow(); break;
                case 8:
                    Check(browser.isFullscreen && browser.windowContainer.offsetMin.sqrMagnitude < 1 && browser.windowContainer.offsetMax.sqrMagnitude < 1, "Maximize fills the work area");
                    Shot("07-browser-maximized"); browser.FullscreenWindow(); break;
                case 9:
                    Check(!browser.isFullscreen && Vector2.Distance(browser.windowContainer.sizeDelta, browserSize) < 2 && Vector2.Distance(browser.windowContainer.anchoredPosition, browserPosition) < 2, "Restore returns to previous rectangle");
                    browser.windowContainer.sizeDelta -= new Vector2(100, 70);
                    browser.windowContainer.anchoredPosition += new Vector2(20, -12);
                    browserSize = browser.windowContainer.sizeDelta; browserPosition = browser.windowContainer.anchoredPosition;
                    browser.CloseWindow(); break;
                case 10: browser.OpenWindow(); break;
                case 11:
                    Check(Vector2.Distance(browser.windowContainer.sizeDelta, browserSize) < 2 && Vector2.Distance(browser.windowContainer.anchoredPosition, browserPosition) < 2, "Reopen preserves player-adjusted size and position");
                    Shot("08-resized-browser");
                    AfterShot(GameExit.ReturnToTitle); due += 15; break;
                case 12:
                    var hud = UnityEngine.Object.FindAnyObjectByType<LingGuangV05.CaseMenu.CaseMenuHud>();
                    var title = hud.GetComponentsInChildren<TMP_Text>(true).First(t => t.text == AppNames.GameZh);
                    Canvas.ForceUpdateCanvases(); title.ForceMeshUpdate();
                    Check(!title.isTextOverflowing, "Fixed game title fits menu");
                    Shot("09-game-title");
                    UnityEngine.Object.FindAnyObjectByType<LingGuangV05.CaseMenu.CaseMenuController>().BeginGame();
                    due += 16;
                    break;
                case 13:
                    Check(runtime != null && runtime.GetComponent<DesktopWindowDefaults>() != null, "Window defaults also install through the real title-to-game path");
                    var ai = Bridge().Window.windowContainer;
                    Check(Vector2.Distance(ai.rect.size, DesktopWindowDefaults.SizeFor(Bridge().Window.name, ((RectTransform)ai.parent).rect.size)) < 3, "Title-to-game AI window uses normal size");
                    VerifyFullscreen();
                    Shot("10-title-entered-desktop");
                    AfterShot(() => SetViewSize(1600, 1000)); break;
                case 14:
                    Check(Screen.width == 1600 && Screen.height == 1000, "16:10 viewport selected");
                    VerifyFullscreen(); Shot("11-full-desktop-16x10");
                    runtime.Sim.InstallApp(); controller.Open(); break;
                case 15:
                    VerifyFullscreen(); VerifyDrag(); Shot("12-full-desktop-app");
                    AfterShot(() => SetViewSize(1920, 1080)); break;
                case 16:
                    Check(Screen.width == 1920 && Screen.height == 1080, "16:9 viewport selected");
                    VerifyFullscreen(); Shot("13-full-desktop-16x9");
                    AfterShot(GameExit.ReturnToTitle); due += 12; break;
                case 17:
                    Check(GameObject.Find("Fullscreen Desktop") == null, "Fullscreen overlay released when returning to title");
                    UnityEngine.Object.FindAnyObjectByType<CaseMenuController>().BeginGame(); due += 16; break;
                case 18:
                    VerifyFullscreen(); Shot("14-full-desktop-reentered");
                    File.WriteAllText(Path.Combine(output, "result.txt"), "PASS: opening validation, personal name, rename, long name, restored state, stable app IDs, all window defaults, maximize/restore, adjusted size after reopening, menu title and actual title-to-game startup, opaque black screen during the single camera flight, full-screen arrival before chapter reveal, fullscreen coverage and relay click/scroll/drag at 16:10 and 16:9, return and reenter. Inspect screenshots visually.");
                    break;
                default: Finish(0); break;
            }
        }
        catch (Exception e) { File.WriteAllText(Path.Combine(output, "result.txt"), "FAIL: " + e); Debug.LogException(e); Finish(1); }
    }
    static void ObserveExpansion()
    {
        var display = UnityEngine.Object.FindAnyObjectByType<CaseMenuMainDisplay>();
        if (display == null || !display.IsExpandingDesktop) return;
        if (observedDisplay != display) { observedDisplay = display; expansionSamples = 0; previousExpansion = 0; expansionTexture = display.InputSurface.sourceCamera.targetTexture; }
        float progress = display.DesktopExpansionProgress;
        if (display.InputSurface.inputEnabled || progress < previousExpansion || progress - previousExpansion > .2f) throw new Exception("Desktop expansion skipped a large step or enabled pointer input early");
        if (progress > previousExpansion) expansionSamples++;
        previousExpansion = progress;
        if (!display.IsBootScreenOpaque) throw new Exception("Desktop was exposed before the camera reached fullscreen");
        if (display.Runtime.enabled || display.InputSurface.sourceCamera.targetTexture != expansionTexture) throw new Exception("Story resumed or render target reallocated during the camera flight");
        // Do not encode screenshots during the animation: synchronous PNG capture itself
        // stalls the editor. The final stills are captured only after motion completes.
    }

    static void SetViewSize(int width, int height)
    {
        var assembly = typeof(EditorWindow).Assembly;
        var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
        var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var sizes = singleton.GetProperty("instance").GetValue(null);
        var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new object[] { GameViewSizeGroupType.Standalone });
        var sizeType = assembly.GetType("UnityEditor.GameViewSize");
        var modeType = assembly.GetType("UnityEditor.GameViewSizeType");
        var size = Activator.CreateInstance(sizeType, new object[] { Enum.Parse(modeType, "FixedResolution"), width, height, "Desktop verification" });
        group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
        int count = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
        var view = EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
        view.GetType().GetProperty("selectedSizeIndex", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(view, count - 1);
    }

    static void VerifyFullscreen()
    {
        var display = UnityEngine.Object.FindAnyObjectByType<CaseMenuMainDisplay>();
        Check(display != null && display.HasEnteredDesktop, "Real menu has entered desktop");
        Check(expansionSamples >= 3 && display.ExpansionStartErrorPixels < 1, "Desktop expands over multiple frames with no initial position jump (" + expansionSamples + " samples, " + display.ExpansionStartErrorPixels + " pixels)");
        var input = display.InputSurface;
        var canvas = input.displayRect.GetComponentInParent<Canvas>();
        Check(canvas.renderMode == RenderMode.ScreenSpaceOverlay, "Desktop is a fullscreen overlay");
        var corners = new Vector3[4]; input.displayRect.GetWorldCorners(corners);
        Check(Vector2.Distance(corners[0], Vector2.zero) < 1 && Vector2.Distance(corners[2], new Vector2(Screen.width, Screen.height)) < 1, "Desktop covers every viewport edge");
        var texture = input.sourceCamera.targetTexture;
        Check(texture.width == Screen.width && texture.height == Screen.height, "Desktop rendered at native viewport resolution " + texture.width + "x" + texture.height);
        Check(input.inputEnabled, "Fullscreen desktop input enabled");
        Check(!display.IsBootScreenVisible && !display.IsRevealingDesktop, "Boot cover reveals the chapter only after full-screen arrival");
        var wallpaper = UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>().sourceCanvas.transform.Find("Desktop/Wallpaper").GetComponent<RectTransform>();
        wallpaper.GetWorldCorners(corners);
        var wallpaperMin = input.sourceCamera.WorldToScreenPoint(corners[0]);
        var wallpaperMax = input.sourceCamera.WorldToScreenPoint(corners[2]);
        Check(wallpaperMin.x <= 1 && wallpaperMin.y <= 1 && wallpaperMax.x >= Screen.width - 1 && wallpaperMax.y >= Screen.height - 1, "Wallpaper covers the viewport without letterboxing");
        foreach (var uv in new[] { new Vector2(.025f,.025f), new Vector2(.975f,.025f), new Vector2(.025f,.975f), new Vector2(.975f,.975f) })
        {
            var point = Vector2.Scale(uv, new Vector2(Screen.width, Screen.height));
            Check(input.TryMap(point, out var mapped) && Vector2.Distance(point, mapped) < 1, "Pointer maps correctly at " + uv);
            string probeName = "Temporary input probe " + uv;
            var probe = GameObject.Find(probeName);
            if (probe == null)
            {
                var source = UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>().sourceCanvas;
                probe = new GameObject(probeName, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(Image), typeof(Button), typeof(DesktopPointerProbe));
                probe.transform.SetParent(source.transform, false); probe.layer = 5;
                var probeCanvas = probe.GetComponent<Canvas>(); probeCanvas.overrideSorting = true; probeCanvas.sortingOrder = 32000; probeCanvas.worldCamera = input.sourceCamera;
                var rect = probe.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = uv; rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(60, 40);
                probe.GetComponent<Image>().color = Color.clear;
                // uGUI registers new graphics on the next frame. Exercise these probes on
                // subsequent verification steps, after source-bridge discovery and layout.
                continue;
            }
            int clicks = 0;
            var button = probe.GetComponent<Button>(); button.onClick.RemoveAllListeners(); button.onClick.AddListener(() => clicks++);
            var counter = probe.GetComponent<DesktopPointerProbe>(); int scrolls = counter.scrolls;
            var data = new PointerEventData(EventSystem.current) { position = point, button = PointerEventData.InputButton.Left, pointerId = -1, scrollDelta = Vector2.up };
            input.OnPointerEnter(data); input.OnPointerDown(data); input.OnPointerUp(data); input.OnScroll(data);
            Check(clicks == 1 && counter.scrolls == scrolls + 1, "Corner click and scroll are delivered once at " + uv + " (clicks=" + clicks + ", scrolls=" + (counter.scrolls - scrolls) + ", depth=" + probe.GetComponent<Image>().depth + ")");
        }
    }

    static void VerifyDrag()
    {
        var input = UnityEngine.Object.FindAnyObjectByType<CaseMenuMainDisplay>().InputSurface;
        var window = Bridge().Window;
        var rect = window.windowDragger.transform as RectTransform;
        var point = (Vector2)input.sourceCamera.WorldToScreenPoint(rect.TransformPoint(rect.rect.center));
        var before = window.windowContainer.anchoredPosition;
        var data = new PointerEventData(EventSystem.current) { position = point, button = PointerEventData.InputButton.Left, pointerId = -1 };
        input.OnPointerEnter(data); input.OnPointerDown(data);
        data.position += new Vector2(20,-15); input.OnDrag(data);
        data.position += new Vector2(40,-25); input.OnDrag(data); input.OnPointerUp(data);
        Check(Vector2.Distance(before, window.windowContainer.anchoredPosition) > 10, "Real application caption drag moves window through fullscreen relay");
    }

    static void Finish(int code) { SessionState.SetBool(Key, false); SessionState.SetBool("NamingDesktopSmoke.FullscreenOnly", false); EditorApplication.isPlaying = false; EditorApplication.Exit(code); }
}

public sealed class DesktopPointerProbe : MonoBehaviour, IScrollHandler
{
    public int scrolls;
    public void OnScroll(PointerEventData data) { scrolls++; }
}
