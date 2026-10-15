using System;
using System.IO;
using System.Reflection;
using HongmengOS.Aero2010;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static partial class DesignWallpaperSmoke
{
    const string Key = "DesignWallpaperSmoke.Active";
    static string Output => Path.GetFullPath("LocalVerification/DesignPass/WallpaperSmoke");
    static int step;
    static double due;
    static Color glassPixel;
    static Vector2 glassPoint;
    static bool reloadLocked;
    static int throughClicks;
    static RectTransform throughButton;
    static Color pinnedPixel;
    static Color learnedPixel;
    static string boardBeforeDraw;
    static Vector2 previousConceptPosition;
    static int personaWaits;
    static int hoverConceptId;
    static int visualFrames, badVisualFrames;
    static float quietPhase, quietTime;
    static int originalMenuCount;
    static IDisposable menuLanguage;
    static DesignWallpaperSmoke()
    {
        EditorApplication.update += Update;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
            AssetDatabase.DisallowAutoRefresh(); EditorApplication.LockReloadAssemblies(); reloadLocked = true;
        };
    }
    public static void Run() { Start("all"); }
    public static void RunPin() { Start("pin"); }
    public static void RunConcepts() { Start("concepts"); }
    public static void RunLinks() { Start("links"); }
    public static void RunRegions() { Start("regions"); }
    public static void RunSeed() { Start("seed"); }
    public static void RunOutputs() { Start("outputs"); }
    public static void RunPersona() { Start("persona"); }
    public static void RunAbilities() { Start("abilities"); }
    public static void RunFamily() { Start("family"); }
    public static void RunRules() { Start("rules"); }
    public static void RunHover() { Start("hover"); }
    public static void RunRegionCards() { Start("region-cards"); }
    public static void RunRegionVisual() { Start("region-cards-visual"); }
    public static void RunMenu() { Start("menu"); }
    public static void RunLesson() { Start("lesson"); }
    public static void RunMotion() { Start("motion"); }
    public static void RunReadOnly() { Start("readonly"); }
    public static void RunParallelBatch()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        NamingChecks.Validate();
        SkillTreeCopyChecks.Run();
        RunLesson();
    }
    static void Start(string mode)
    {
        SessionState.SetString("DesignWallpaperSmoke.Mode", mode);
        DesignWallpaperChecks.Run();
        Directory.CreateDirectory(Output);
        File.WriteAllText(Path.Combine(Output, "result.txt"), "RUNNING");
        File.WriteAllText(Path.Combine(Output, "checks.txt"), "");
        if (mode == "hover") CheckTrainingCounts();
        if (mode == "motion" || mode == "wiring-motion") EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        else EditorSceneManager.OpenScene("Assets/LingGuangV05/Scenes/Main.unity");
        var runtime = UnityEngine.Object.FindAnyObjectByType<ChapterOneRuntime>();
        if (runtime != null) runtime.useDiskSave = false;
        Application.runInBackground = true;
        EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Show();
        SessionState.SetBool(Key, true); EditorApplication.isPlaying = true;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Isolate()
    {
        if (!SessionState.GetBool(Key, false)) return;
        ChapterOneRuntime.VerificationSaveDirectory = Path.Combine(Output, "Save");
        var profile = Path.Combine(Output, "DesktopProfile") + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(profile);
        typeof(Michsky.DreamOS.DreamOSDataManager).GetField("dataPath", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, profile);
    }
    static void Check(bool ok, string message)
    {
        if (!ok) throw new Exception(message);
        File.AppendAllText(Path.Combine(Output, "checks.txt"), message + "\n");
    }
    static void Update()
    {
        if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        if (due == 0) { due = EditorApplication.timeSinceStartup + 12; return; }
        if (EditorApplication.timeSinceStartup < due) return;
        due = EditorApplication.timeSinceStartup + 1;
        try
        {
            if (SessionState.GetString("DesignWallpaperSmoke.Mode", "all") == "motion")
            {
                MotionIntegrationChecks.Run();
                StartMotionFrameChecks(); return;
            }
            if (SessionState.GetString("DesignWallpaperSmoke.Mode", "") == "wiring-motion")
            {
                StartWiringMotionChecks(); return;
            }
            var runtime = UnityEngine.Object.FindAnyObjectByType<ChapterOneRuntime>();
            var brain = UnityEngine.Object.FindAnyObjectByType<XgBrainWallpaper>();
            var controller = UnityEngine.Object.FindAnyObjectByType<XingGuangController>();
            if (SessionState.GetString("DesignWallpaperSmoke.Mode", "") == "lesson-goal" && step >= 250)
            {
                TickLessonGoal(controller, runtime); return;
            }
            if (SessionState.GetString("DesignWallpaperSmoke.Mode", "") == "sense-start" && step >= 240)
            {
                TickSenseStart(controller, runtime); return;
            }
            if (SessionState.GetString("DesignWallpaperSmoke.Mode", "") == "growth" && step >= 220)
            {
                TickGrowth(controller, runtime, brain); return;
            }
            if (SessionState.GetString("DesignWallpaperSmoke.Mode", "") == "answer-path" && step >= 210)
            {
                TickAnswerPaths(controller, runtime, brain); return;
            }
            if (SessionState.GetString("DesignWallpaperSmoke.Mode", "") == "first-minute" && step >= 200)
            {
                TickFirstMinute(controller, runtime, brain); return;
            }
            if (SessionState.GetString("DesignWallpaperSmoke.Mode", "") == "tech" && step >= 180)
            {
                TickTech(controller, runtime, brain); return;
            }
            if (SessionState.GetString("DesignWallpaperSmoke.Mode", "") == "readonly" && step >= 190)
            {
                TickReadOnly(controller, brain); return;
            }
            switch (step++)
            {
                case 0:
                    Check(runtime != null && !runtime.useDiskSave, "Isolated player save");
                    var prologue = runtime.GetComponent<PrologueDirector>();
                    if (prologue != null) { prologue.StopAllCoroutines(); prologue.enabled = false; }
                    var story = runtime.GetComponent<LingGuangV05.Runtime.Story.StoryDirector>();
                    if (story != null) story.enabled = false;
                    runtime.SetSimulationForTests(new ChapterOneSim());
                    if (SessionState.GetString("DesignWallpaperSmoke.Mode", "") == "desktop-fade")
                    {
                        StartDesktopFadeFrameChecks(); break;
                    }
                    if (SessionState.GetString("DesignWallpaperSmoke.Mode", "all") != "all") step = 11;
                    if (SessionState.GetString("DesignWallpaperSmoke.Mode", "") == "first-minute") step = 200;
                    break;
                case 1:
                    Check(brain != null && brain.Reveal.Opacity == 0, "Opening shows original wallpaper in real scene");
                    Check(brain.NoticesSent == 0, "No background notification before setup");
                    var bridge = UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>();
                    Check(brain.transform.parent == bridge.sourceCanvas.transform.Find("Desktop"), "Wallpaper lives in rendered desktop");
                    Check(brain.transform.GetSiblingIndex() > brain.transform.parent.Find("Wallpaper").GetSiblingIndex(), "Placed immediately above default wallpaper");
                    var display = bridge.inputSurface.displayRect;
                    Check(display.GetComponentInParent<Canvas>().renderMode == RenderMode.ScreenSpaceOverlay, "Direct Main entry hides physical bezel");
                    Canvas.ForceUpdateCanvases();
                    var corners = new Vector3[4]; display.GetWorldCorners(corners);
                    Check(Vector2.Distance(corners[0], Vector2.zero) < 1 && Vector2.Distance(corners[2], new Vector2(Screen.width, Screen.height)) < 1, "Actual game viewport covered on all four edges");
                    foreach (var uv in new[] { new Vector2(.01f, .01f), new Vector2(.99f, .01f), new Vector2(.01f, .99f), new Vector2(.99f, .99f) })
                    {
                        var pointer = Vector2.Scale(uv, new Vector2(Screen.width, Screen.height));
                        Check(bridge.inputSurface.TryMap(pointer, out var mapped) && Vector2.Distance(pointer, mapped) < 1, "Direct entry pointer maps at " + uv);
                    }
                    Check(!brain.GetComponent<CanvasGroup>().blocksRaycasts, "Wallpaper cannot intercept desktop input");
                    Check(runtime.CompletePrologue(new PrologueProfile { name = "小星", self = "我", callMe = "你", personality = "好奇" }), "Finish first meeting");
                    due = EditorApplication.timeSinceStartup + .35;
                    break;
                case 2:
                    Check(brain.Reveal.Opacity > 0 && brain.Reveal.Opacity < 1, "Scene samples intermediate fade");
                    Check(brain.NoticesSent == 0, "Notification waits for final point");
                    due = EditorApplication.timeSinceStartup + 4;
                    break;
                case 3:
                    Check(!brain.GetComponent<XgBrainWallpaperPin>().IsPinned, "Wallpaper pin is off by default");
                    Check(brain.Reveal.Opacity == 1, "Scene fade completes");
                    Check(brain.NoticesSent == 1, "Exactly one Windows tray notification after reveal");
                    foreach (string part in new[] { "Center", "Inputs", "Outputs", "Seed" })
                    {
                        float expected = part == "Seed" ? Mathf.Lerp(.55f, 1, XgWallpaperConcepts.Strength(brain.SeedConcept)) : 1;
                        Check(Mathf.Approximately(brain.transform.Find(part).GetComponent<CanvasGroup>().alpha, expected), "Revealed " + part);
                    }
                    Capture("wallpaper");
                    ScreenCapture.CaptureScreenshot(Path.Combine(Output, "full-game-viewport.png"));
                    break;
                case 4:
                    controller.Open();
                    break;
                case 5:
                    Check(!brain.GetComponent<XgBrainWallpaperPresentation>().Showcase, "An open application keeps the background subdued");
                    Check(!brain.GetComponent<CanvasGroup>().blocksRaycasts, "Subdued wallpaper does not receive pointer input");
                    Check(brain.gameObject.activeInHierarchy && brain.Reveal.Opacity == 1, "Opening application keeps wallpaper alive");
                    var top = controller.Window.transform;
                    while (top.parent != null && top.parent != brain.transform.parent) top = top.parent;
                    Check(top.parent == brain.transform.parent && top.GetSiblingIndex() > brain.transform.GetSiblingIndex(), "Application window is above wallpaper");
                    var backing = controller.Window.windowContainer.Find("Background").GetComponent<UnityEngine.UI.Image>();
                    Check(backing.canvasRenderer.GetAlpha() < .6f, "AI backing is translucent");
                    var point = brain.transform.Find("Outputs/Port 0/Glow0");
                    var source = UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>().sourceCamera;
                    glassPoint = source.WorldToScreenPoint(point.position);
                    glassPixel = Pixel(glassPoint);
                    ScreenCapture.CaptureScreenshot(Path.Combine(Output, "glass-window.png"));
                    // Capture first; hide the underlying point on a later frame.
                    break;
                case 6:
                    brain.transform.Find("Outputs").gameObject.SetActive(false);
                    break;
                case 7:
                    var hiddenPixel = Pixel(glassPoint);
                    Check(glassPixel.g > hiddenPixel.g + .003f, "Real wallpaper light contributes through the application backing");
                    brain.transform.Find("Outputs").gameObject.SetActive(true);
                    controller.Window.MinimizeWindow();
                    break;
                case 8:
                    Check(brain.gameObject.activeInHierarchy && brain.Reveal.Opacity == 1, "Minimizing application reveals persistent wallpaper");
                    Check(brain.GetComponent<XgBrainWallpaperPresentation>().Showcase, "Minimized windows enable desktop showcase");
                    Check(brain.GetComponent<XgBrainWallpaperPresentation>().LightLevel > .9f, "Showcase brightens smoothly");
                    var hoverPoint = brain.transform.Find("Seed/Glow0").GetComponent<LingGuangV05.Desktop.UiTip>();
                    Check(hoverPoint != null && hoverPoint.enabled && !string.IsNullOrEmpty(hoverPoint.Text()), "Showcase points explain themselves on hover");
                    var pointerEvent = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
                    var hoverBridge = UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>();
                    pointerEvent.position = hoverBridge.sourceCamera.WorldToScreenPoint(hoverPoint.transform.position);
                    var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
                    // Source raycasters are deliberately disabled in EventSystem to
                    // avoid double input. Query them exactly as the LCD relay does.
                    foreach (var raycaster in hoverBridge.inputSurface.sourceRaycasters) raycaster.Raycast(pointerEvent, hits);
                    File.WriteAllText(Path.Combine(Output, "hover-diagnostics.txt"), "Input enabled=" + hoverBridge.inputSurface.inputEnabled + ", active=" + hoverBridge.inputSurface.isActiveAndEnabled + "\n");
                    foreach (var hit in hits) File.AppendAllText(Path.Combine(Output, "hover-diagnostics.txt"), hit.gameObject.name + " parent=" + hit.gameObject.transform.parent?.name + " depth=" + hit.depth + "\n");
                    Check(hits.Exists(hit => hit.gameObject == hoverPoint.gameObject), "Showcase point participates in source desktop raycasts");
                    pointerEvent.position = Vector2.Scale(pointerEvent.position, new Vector2((float)Screen.width / hoverBridge.sourceCamera.targetTexture.width, (float)Screen.height / hoverBridge.sourceCamera.targetTexture.height));
                    hoverBridge.inputSurface.OnPointerEnter(pointerEvent);
                    var relayedHover = (UnityEngine.EventSystems.PointerEventData)typeof(AeroLcdInputSurface).GetField("hoverData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(hoverBridge.inputSurface);
                    File.AppendAllText(Path.Combine(Output, "hover-diagnostics.txt"), "Relay target=" + relayedHover?.pointerEnter?.name + "\n");
                    break;
                case 9:
                    var tipBox = brain.GetComponentInParent<Canvas>().rootCanvas.transform.Find("Hover Tip");
                    Check(tipBox != null && tipBox.gameObject.activeInHierarchy, "Hover explanation actually renders");
                    ScreenCapture.CaptureScreenshot(Path.Combine(Output, "after-minimize.png"));
                    var old = new ChapterOneSim();
                    old.CompletePrologue(new PrologueProfile { name = "旧档", self = "我", callMe = "你", personality = "好奇" });
                    runtime.SetSimulationForTests(old);
                    break;
                case 10:
                    Check(brain.Reveal.Opacity == 1 && brain.Reveal.Age >= 99, "Old save restores without replay");
                    Check(brain.NoticesSent == 1, "Loading another save does not notify again");
                    runtime.SetSimulationForTests(new ChapterOneSim());
                    if (SessionState.GetString("DesignWallpaperSmoke.Mode", "") == "desktop-fade")
                    {
                        StartDesktopFadeFrameChecks(); break;
                    }
                    break;
                case 11:
                    Check(brain.Reveal.Opacity == 0, "Restart restores original wallpaper");
                    var pinControl = brain.GetComponent<XgBrainWallpaperPin>();
                    Check(pinControl.ToggleRect != null, "Pin control is mounted in the actual taskbar");
                    Check(!pinControl.ToggleRect.GetComponent<CanvasGroup>().blocksRaycasts, "Pin control stays hidden in prologue");
                    Check(!pinControl.IsPinned, "Wallpaper pin is off by default");
                    runtime.CompletePrologue(new PrologueProfile { name = "小星", self = "我", callMe = "你", personality = "好奇" });
                    controller.Open(); due = EditorApplication.timeSinceStartup + 4;
                    var mode = SessionState.GetString("DesignWallpaperSmoke.Mode", "all");
                    if (mode == "tech") step = 180;
                    if (mode == "answer-path") step = 210;
                    if (mode == "growth") step = 220;
                    if (mode == "sense-start") step = 240;
                    if (mode == "lesson-goal") step = 250;
                    if (mode == "answer-correction") StartCorrectionChecks(controller, brain);
                    if (mode == "wiring-scene") StartWiringSceneChecks(controller, runtime);
                    if (mode == "training-scene") StartTrainingSceneChecks(controller, runtime);
                    if (mode == "readonly")
                    {
                        controller.Window.MinimizeWindow(); step = 190;
                    }
                    if (mode == "lesson")
                    {
                        controller.Window.MinimizeWindow(); step = 170;
                    }
                    if (mode == "menu" || mode == "concepts" || mode == "links" || mode == "regions" || mode == "seed" || mode == "outputs" || mode == "persona" || mode == "abilities" || mode == "family" || mode == "rules" || mode == "hover" || mode.StartsWith("region-cards"))
                    {
                        controller.Window.MinimizeWindow(); step = mode == "menu" ? 149 : mode.StartsWith("region-cards") ? 130 : mode == "hover" ? 110 : mode == "rules" ? 100 : mode == "family" ? 90 : mode == "abilities" ? 80 : mode == "persona" ? 70 : mode == "outputs" ? 60 : mode == "seed" ? 50 : mode == "regions" ? 40 : mode == "links" ? 30 : 20;
                    }
                    break;
                case 12:
                    Click(brain.GetComponent<XgBrainWallpaperPin>().ToggleRect);
                    break;
                case 13:
                    var pin = brain.GetComponent<XgBrainWallpaperPin>();
                    Check(pin.IsPinned && pin.Overlay.gameObject.activeInHierarchy, "Real tray click enables pin overlay");
                    Check(Mathf.Approximately(pin.Overlay.alpha, .3f) && !pin.Overlay.blocksRaycasts, "Pin overlay is exactly 30 percent and passes input");
                    Check(pin.Overlay.GetComponentsInChildren<UnityEngine.UI.Graphic>().Length == 0, "Overlay has no input graphics or duplicated behavior");
                    throughButton = PrologueDesk.Rect("Click-through Probe", controller.Window.windowContainer, Vector2.one * .5f, Vector2.one * .5f, -Vector2.one * 22, Vector2.one * 22);
                    throughButton.position = brain.transform.Find("Outputs/Port 0/Glow0").position;
                    var probeImage = PrologueDesk.Fill(throughButton, new Color32(12, 24, 30, 255));
                    throughButton.gameObject.AddComponent<UnityEngine.UI.Button>().onClick.AddListener(() => throughClicks++);
                    break;
                case 14:
                    Click(throughButton);
                    Check(throughClicks == 1, "Click passes through lit wallpaper to application exactly once");
                    var pinSource = UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>().sourceCamera;
                    glassPoint = pinSource.WorldToScreenPoint(throughButton.position);
                    pinnedPixel = Pixel(glassPoint);
                    var renderedOverlay = brain.GetComponent<XgBrainWallpaperPin>().Overlay;
                    File.WriteAllText(Path.Combine(Output, "pin-diagnostics.txt"), "Pinned pixel=" + pinnedPixel + ", layer=" + renderedOverlay.gameObject.layer + "\n");
                    foreach (var renderer in renderedOverlay.GetComponentsInChildren<CanvasRenderer>())
                        File.AppendAllText(Path.Combine(Output, "pin-diagnostics.txt"), renderer.name + " vertices=" + renderer.GetMesh()?.vertexCount + " color=" + renderer.GetColor() + " inherited=" + renderer.GetInheritedAlpha() + " cull=" + renderer.cull + "\n");
                    ScreenCapture.CaptureScreenshot(Path.Combine(Output, "pin-on.png"));
                    break;
                case 15:
                    Click(brain.GetComponent<XgBrainWallpaperPin>().ToggleRect);
                    break;
                case 16:
                    Check(!brain.GetComponent<XgBrainWallpaperPin>().IsPinned, "Second tray click disables pin overlay");
                    var unpinnedPixel = Pixel(glassPoint);
                    File.AppendAllText(Path.Combine(Output, "pin-diagnostics.txt"), "Unpinned pixel=" + unpinnedPixel + "\n");
                    Check(pinnedPixel.g > unpinnedPixel.g + .015f, "Overlay visibly draws brain above an opaque application control");
                    Click(throughButton);
                    Check(throughClicks == 2, "Application remains clickable after unpinning");
                    UnityEngine.Object.Destroy(throughButton.gameObject);
                    runtime.SetSimulationForTests(new ChapterOneSim());
                    if (SessionState.GetString("DesignWallpaperSmoke.Mode", "") == "desktop-fade")
                    {
                        StartDesktopFadeFrameChecks(); break;
                    }
                    break;
                case 17:
                    Check(!brain.GetComponent<XgBrainWallpaperPin>().Overlay.gameObject.activeInHierarchy, "Restart leaves no ghost overlay");
                    File.WriteAllText(Path.Combine(Output, "result.txt"), "PASS"); Finish(0); break;
                case 20:
                    // Open() completes on a later frame. Minimize only after that
                    // transition, as a player does once the title bar is visible.
                    controller.Window.MinimizeWindow();
                    controller.Sim.S.board = new LingGuangV05.XingGuang.XgBoardState();
                    controller.Sim.S.board.concepts.Add(new LingGuangV05.XingGuang.XgConcept { id = 101, region = "logic", key = "中奖", w = 4, s = 10 });
                    controller.Sim.S.board.concepts.Add(new LingGuangV05.XingGuang.XgConcept { id = 102, region = "vision", key = "猫", w = -4, s = 10 });
                    controller.Sim.S.board.concepts.Add(new LingGuangV05.XingGuang.XgConcept { id = 103, region = "sequence", key = "你好", w = 4, s = .1 });
                    controller.Sim.S.board.concepts.Add(new LingGuangV05.XingGuang.XgConcept { id = 104, region = "tone", key = "温暖", w = .1, s = 10 });
                    controller.Sim.S.board.nextId = 105;
                    boardBeforeDraw = JsonUtility.ToJson(controller.Sim.S.board);
                    break;
                case 21:
                    Check(brain.GetComponent<XgBrainWallpaperPresentation>().Showcase, "Concept color samples use the uncovered desktop");
                    var concepts = brain.GetComponentInChildren<XgWallpaperConcepts>();
                    Check(concepts != null && concepts.Cells.Count == 4, "Wallpaper renders real learned concepts without demo cells");
                    Check(JsonUtility.ToJson(controller.Sim.S.board) == boardBeforeDraw, "Drawing leaves simulation data unchanged");
                    learnedPixel = ConceptPixel(concepts, 101);
                    var noPixel = ConceptPixel(concepts, 102);
                    File.WriteAllText(Path.Combine(Output, "concept-diagnostics.txt"), "Yes=" + learnedPixel + ", No=" + noPixel + "\n");
                    Capture("concepts-inspect");
                    Check(learnedPixel.g > learnedPixel.r + .1f && learnedPixel.g > learnedPixel.b + .1f, "Positive concept actually renders green");
                    Check(noPixel.r > noPixel.g + .1f, "Negative concept actually renders red");
                    Check(learnedPixel.g > ConceptPixel(concepts, 103).g + .1f, "Activity s changes visible brightness");
                    Check(learnedPixel.g > ConceptPixel(concepts, 104).g + .1f, "Weight w changes visible brightness");
                    ScreenCapture.CaptureScreenshot(Path.Combine(Output, "concepts-real.png"));
                    controller.Sim.S.board.concepts[0].w = -4;
                    break;
                case 22:
                    var updated = ConceptPixel(brain.GetComponentInChildren<XgWallpaperConcepts>(), 101);
                    Check(updated.r > updated.g + .1f, "Changed belief updates existing concept color");
                    var card = new LingGuangV05.XingGuang.XgBoardCard { region = "vision", truth = true };
                    card.Add("真实训练的新特征");
                    controller.Sim.Board.Train(card, new LingGuangV05.XingGuang.XgKnobs { depth = 1, width = 32, lr = .1 });
                    break;
                case 23:
                    var live = brain.GetComponentInChildren<XgWallpaperConcepts>();
                    Check(live.Cells.Count == controller.Sim.Board.S.concepts.Count && live.Cells.Count > 4, "Actual training adds its learned concepts to wallpaper");
                    runtime.SetSimulationForTests(new ChapterOneSim());
                    if (SessionState.GetString("DesignWallpaperSmoke.Mode", "") == "desktop-fade")
                    {
                        StartDesktopFadeFrameChecks(); break;
                    }
                    break;
                case 24:
                    Check(brain.GetComponentInChildren<XgWallpaperConcepts>().Cells.Count == 0, "New game clears old learned concepts");
                    File.WriteAllText(Path.Combine(Output, "result.txt"), "PASS"); Finish(0); break;
                case 30:
                    controller.Window.MinimizeWindow();
                    controller.Sim.S.board = new LingGuangV05.XingGuang.XgBoardState();
                    controller.Sim.S.board.concepts.Add(new LingGuangV05.XingGuang.XgConcept { id = 1, region = "vision", key = "一", w = 4, s = 10 });
                    controller.Sim.S.board.concepts.Add(new LingGuangV05.XingGuang.XgConcept { id = 2, region = "logic", key = "二", w = -4, s = 10 });
                    controller.Sim.S.board.links.Add(new LingGuangV05.XingGuang.XgLink { a = 1, b = 2, c = .8 });
                    controller.Sim.S.board.links.Add(new LingGuangV05.XingGuang.XgLink { a = 1, b = 999, c = .8 });
                    controller.Sim.S.board.links.Add(new LingGuangV05.XingGuang.XgLink { a = 2, b = 1, c = .8 });
                    break;
                case 31:
                    var network = brain.GetComponentInChildren<XgWallpaperConcepts>();
                    Check(network.Links.Count == 1, "Draw only real valid concept pairs, without duplicates or dangling lines");
                    var line = network.Links[0];
                    var midpoint = network.rectTransform.rect.min + Vector2.Scale(network.rectTransform.rect.size, (line.a + line.b) * .5f);
                    glassPoint = UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>().sourceCamera.WorldToScreenPoint(network.transform.TransformPoint(midpoint));
                    learnedPixel = Pixel(glassPoint);
                    Capture("links-real");
                    controller.Sim.S.board.links.Clear();
                    break;
                case 32:
                    Check(brain.GetComponentInChildren<XgWallpaperConcepts>().Links.Count == 0, "Removing a relation removes its line");
                    Check(learnedPixel.b > Pixel(glassPoint).b + .03f, "Actual line pixels disappear with the relation");
                    controller.Sim.S.board.links.Add(new LingGuangV05.XingGuang.XgLink { a = 1, b = 2, c = 0 });
                    break;
                case 33:
                    Check(brain.GetComponentInChildren<XgWallpaperConcepts>().Links.Count == 1, "R2 reset correlation still shows the newly combined pair");
                    controller.Sim.S.board.concepts.RemoveAt(1);
                    break;
                case 34:
                    Check(brain.GetComponentInChildren<XgWallpaperConcepts>().Links.Count == 0, "Removing a concept leaves no ghost wire");
                    File.WriteAllText(Path.Combine(Output, "result.txt"), "PASS"); Finish(0); break;
                case 40:
                    controller.Window.MinimizeWindow(); controller.enabled = false;
                    controller.Sim.Run(LingGuangV05.XingGuang.XgTrack.Vision).dataset = "mnist";
                    controller.Sim.Run(LingGuangV05.XingGuang.XgTrack.Vision).arch = "lenet";
                    controller.Sim.Run(LingGuangV05.XingGuang.XgTrack.Sequence).dataset = "spam";
                    controller.Sim.Run(LingGuangV05.XingGuang.XgTrack.Sequence).arch = "rnn";
                    controller.Sim.S.regionWire = new System.Collections.Generic.List<string> { "lenet", "rnn", "mlp", "" };
                    controller.Sim.S.board = new LingGuangV05.XingGuang.XgBoardState();
                    var regions = new[] { "vision", "sequence", "tone", "logic" };
                    for (int region = 0; region < 4; region++) for (int cell = 0; cell < 24; cell++)
                        controller.Sim.S.board.concepts.Add(new LingGuangV05.XingGuang.XgConcept { id = region * 100 + cell + 1, region = regions[region], key = "概念" + cell, w = cell % 3 == 0 ? -3 : 3, s = 2 + cell % 9 });
                    break;
                case 41:
                    var regional = brain.GetComponentInChildren<XgWallpaperConcepts>();
                    Check(regional.Cells.Count == 96, "All four regions retain their actual concepts");
                    Check(regional.Architectures[0] == "lenet" && regional.Architectures[1] == "rnn" && regional.Architectures[2] == "tone" && regional.Architectures[3] == "mlp", "Regions use live and last remembered wiring");
                    foreach (string region in new[] { "vision", "sequence", "tone", "logic" })
                        Check(regional.transform.Find("Region " + region).GetComponent<TMPro.TMP_Text>().text.Length > 0, "Visible named brain area: " + region);
                    Check(regional.TryLocalPosition(101, out previousConceptPosition), "Reading concept has a ring position");
                    Capture("regions-grid-ring-layers");
                    controller.Sim.Run(LingGuangV05.XingGuang.XgTrack.Sequence).arch = "lstm";
                    break;
                case 42:
                    Check(brain.GetComponentInChildren<XgWallpaperConcepts>().Architectures[1] == "lstm", "Reading structure updates to gated recurrence");
                    Capture("regions-gates");
                    controller.Sim.Run(LingGuangV05.XingGuang.XgTrack.Sequence).arch = "seq2seq";
                    break;
                case 43:
                    var split = brain.GetComponentInChildren<XgWallpaperConcepts>();
                    Check(split.TryLocalPosition(101, out var splitPosition) && Vector2.Distance(previousConceptPosition, splitPosition) > 20, "Encoder-decoder rearranges the same concept IDs into two halves");
                    Capture("regions-encoder-decoder"); previousConceptPosition = splitPosition;
                    controller.Sim.Run(LingGuangV05.XingGuang.XgTrack.Sequence).arch = "transformer";
                    break;
                case 44:
                    var cloud = brain.GetComponentInChildren<XgWallpaperConcepts>();
                    Check(cloud.TryLocalPosition(101, out var cloudPosition) && Vector2.Distance(previousConceptPosition, cloudPosition) > 20, "Transformer rearranges real concepts into a cloud");
                    Check(cloud.Cells.Count == 96 && controller.Sim.S.board.concepts.Count == 96, "Structure changes neither invent nor remove learned concepts");
                    Capture("regions-transformer");
                    File.WriteAllText(Path.Combine(Output, "result.txt"), "PASS"); Finish(0); break;
                case 50:
                    controller.Window.MinimizeWindow(); controller.enabled = false;
                    Check(brain.SeedConcept != null && brain.SeedConcept.w < 0, "New game binds the actual SI no seed");
                    controller.Sim.S.board = new LingGuangV05.XingGuang.XgBoardState();
                    boardBeforeDraw = JsonUtility.ToJson(controller.Sim.S.board);
                    break;
                case 51:
                    Check(brain.SeedConcept == null && !brain.transform.Find("Seed").gameObject.activeSelf, "Missing seed leaves no decorative question or hover target");
                    Check(boardBeforeDraw == JsonUtility.ToJson(controller.Sim.S.board), "Rendering does not plant a missing seed");
                    controller.Sim.S.board = new LingGuangV05.XingGuang.XgBoardState();
                    controller.Sim.S.board.concepts.Add(new LingGuangV05.XingGuang.XgConcept { id = 900, region = "logic", key = "电源", w = 3, s = 10 });
                    controller.Sim.S.board.nextId = 901;
                    var planted = controller.Sim.Board.Plant("logic", LingGuangV05.XingGuang.XgSim.SeedKey, -3);
                    controller.Sim.S.board.links.Add(new LingGuangV05.XingGuang.XgLink { a = planted.id, b = 900, c = .8 });
                    boardBeforeDraw = JsonUtility.ToJson(controller.Sim.S.board);
                    break;
                case 52:
                    Check(brain.SeedConcept == controller.Sim.Board.Find("logic", LingGuangV05.XingGuang.XgSim.SeedKey), "Question references the exact saved seed concept");
                    Check(boardBeforeDraw == JsonUtility.ToJson(controller.Sim.S.board), "Question presentation preserves seed data");
                    var seeded = brain.GetComponentInChildren<XgWallpaperConcepts>();
                    Check(seeded.Cells.Count == 1 && seeded.Links.Count == 1 && seeded.TryLocalPosition(brain.SeedConcept.id, out var seedLocal), "Seed appears once and its real relation reaches the question");
                    var seedRect = (RectTransform)brain.transform.Find("Seed/Glow0");
                    glassPoint = UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>().sourceCamera.WorldToScreenPoint(seedRect.TransformPoint(new Vector3(12, 0, 0)));
                    learnedPixel = Pixel(glassPoint);
                    Check(learnedPixel.r > learnedPixel.b + .15f && learnedPixel.g > learnedPixel.b + .1f, "Actual question hexagon renders gold");
                    Capture("seed-real");
                    brain.SeedConcept.w = .02; brain.SeedConcept.s = .1;
                    break;
                case 53:
                    Check(brain.SeedConcept.w > 0 && brain.transform.Find("Seed").gameObject.activeSelf, "Seed remains gold question when learning changes its belief");
                    Check(learnedPixel.r > Pixel(glassPoint).r + .05f, "Seed strength updates visible question brightness");
                    controller.Sim.Board.Reinitialise();
                    break;
                case 54:
                    Check(brain.SeedConcept != null && brain.transform.Find("Seed").gameObject.activeSelf, "Real board reset preserves the protected seed and question");
                    controller.enabled = true;
                    runtime.SetSimulationForTests(new ChapterOneSim());
                    if (SessionState.GetString("DesignWallpaperSmoke.Mode", "") == "desktop-fade")
                    {
                        StartDesktopFadeFrameChecks(); break;
                    }
                    break;
                case 55:
                    Check(brain.SeedConcept == null && !brain.transform.Find("Seed").gameObject.activeSelf, "Restart removes prior save question");
                    var seedSave = new ChapterOneSim();
                    seedSave.CompletePrologue(new PrologueProfile { name = "旧种子", self = "我", callMe = "你", personality = "好奇" });
                    runtime.SetSimulationForTests(seedSave);
                    break;
                case 56:
                    Check(brain.SeedConcept != null && brain.SeedConcept == controller.Sim.Board.Find("logic", LingGuangV05.XingGuang.XgSim.SeedKey), "Loading completed save restores its own seed without replay");
                    Check(brain.Reveal.Age >= 99, "Loaded question does not replay first reveal");
                    Capture("seed-restored");
                    File.WriteAllText(Path.Combine(Output, "result.txt"), "PASS"); Finish(0); break;
                case 60:
                    controller.Window.MinimizeWindow(); controller.enabled = false;
                    controller.Sim.S.stage = 1;
                    break;
                case 61: case 62: case 63: case 64: case 65: case 66:
                    int speechStage = step - 61;
                    var outputs = brain.GetComponentInChildren<XgWallpaperOutputs>();
                    Check(outputs.Stage == speechStage && controller.Sim.S.stage == speechStage, "Output follows speech stage without changing progression: " + speechStage);
                    int expectedPorts = speechStage == 1 ? 2 : speechStage == 2 ? 4 : 1;
                    Check(outputs.PortCount == expectedPorts, "Speech output count: " + speechStage);
                    for (int port = 0; port < 4; port++)
                    {
                        var outputPort = outputs.transform.Find("Port " + port);
                        Check(outputPort.gameObject.activeSelf == (port < expectedPorts), "Output port visibility " + speechStage + "/" + port);
                        if (port < expectedPorts)
                        {
                            var portPixel = Pixel(UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>().sourceCamera.WorldToScreenPoint(outputPort.Find("Glow0").position));
                            Check(portPixel.g > portPixel.r + .1f, "Current output port actually renders: " + speechStage + "/" + port);
                            var portLabel = outputPort.Find("Form").GetComponent<TMPro.TMP_Text>();
                            Check(!string.IsNullOrEmpty(portLabel.text) && !portLabel.isTextOverflowing, "Output label fits: " + speechStage + "/" + port);
                        }
                    }
                    if (speechStage >= 3) Check(outputs.transform.Find("Port 0/Form").GetComponent<TMPro.TMP_Text>().text == outputs.FormLabel, "Visible form matches dialogue policy: " + speechStage);
                    Capture("output-stage-" + speechStage);
                    controller.Sim.S.stage = speechStage == 6 ? 1 : speechStage + 1;
                    break;
                case 67:
                    Check(brain.GetComponentInChildren<XgWallpaperOutputs>().Stage == 1, "Earlier stage restores its two outputs without leftover labels");
                    controller.enabled = true;
                    runtime.SetSimulationForTests(new ChapterOneSim());
                    if (SessionState.GetString("DesignWallpaperSmoke.Mode", "") == "desktop-fade")
                    {
                        StartDesktopFadeFrameChecks(); break;
                    }
                    break;
                case 68:
                    Check(brain.transform.Find("Outputs").GetComponent<CanvasGroup>().alpha == 0, "New prologue hides learned output display");
                    File.WriteAllText(Path.Combine(Output, "result.txt"), "PASS"); Finish(0); break;
                case 70:
                    controller.Window.MinimizeWindow(); controller.enabled = false;
                    SetPersona(controller, false, false, false);
                    due = EditorApplication.timeSinceStartup + 3;
                    break;
                case 71:
                    var cold = brain.GetComponentInChildren<XgWallpaperPersona>();
                    Check(Vector3.Distance(cold.Axes, Vector3.one * .06f) < .015f, "Halo uses actual learned axes, not setup targets");
                    Check(cold.HaloColor.b > cold.HaloColor.r + .5f && HaloPixel(cold).b > HaloPixel(cold).r + .015f, "Cold personality visibly renders blue");
                    Check(cold.MotionAmplitude < .002f && cold.EdgeSpread > 22, "Serious compliant personality is calm and soft");
                    Check(boardBeforeDraw == JsonUtility.ToJson(controller.Sim.S.board), "Personality rendering leaves learned data unchanged");
                    previousConceptPosition = cold.OutlinePoint(.7f, 0);
                    Capture("persona-cold-soft"); SetPersona(controller, true, false, false);
                    due = EditorApplication.timeSinceStartup + 3;
                    break;
                case 72:
                    var warm = brain.GetComponentInChildren<XgWallpaperPersona>();
                    if (warm.Axes.x < .935f && ++personaWaits < 10) { step--; break; }
                    var warmPixel = HaloPixel(warm);
                    Capture("persona-warm-diagnostic");
                    Check(warm.Axes.x > .93f && warm.HaloColor.r > warm.HaloColor.b + .5f && warmPixel.r > warmPixel.b + .015f, "Warmth independently changes actual glow to orange: axes=" + warm.Axes + " pixel=" + warmPixel + " actual=" + controller.Sim.ActualAxis(0));
                    Check(Vector2.Distance(previousConceptPosition, warm.OutlinePoint(.7f, 0)) < .1f, "Warmth leaves edge shape and play untouched");
                    Capture("persona-warm-soft"); SetPersona(controller, true, true, false);
                    due = EditorApplication.timeSinceStartup + 3;
                    break;
                case 73:
                    var playful = brain.GetComponentInChildren<XgWallpaperPersona>();
                    Check(playful.MotionAmplitude > .019f && playful.EdgeSpread > 22, "Play adds lively movement independently of softness");
                    Check(Vector2.Distance(playful.OutlinePoint(.7f, 0), playful.OutlinePoint(.7f, 1)) > 3, "Playful contour actually moves over time");
                    Capture("persona-playful"); SetPersona(controller, true, true, true);
                    due = EditorApplication.timeSinceStartup + 3;
                    break;
                case 74:
                    var opinion = brain.GetComponentInChildren<XgWallpaperPersona>();
                    Check(opinion.EdgeSpread < 7 && opinion.MotionAmplitude > .019f && opinion.HaloColor.r > .8f, "Opinion sharpens the edge without erasing warmth or play");
                    Check(!opinion.raycastTarget && opinion.transform.GetSiblingIndex() == 0, "Halo stays beneath concepts and never captures input");
                    Capture("persona-warm-sharp");
                    // ActualAxis rounds to whole points. One rating need not change
                    // a strongly established belief; repeated feedback should.
                    for (int rating = 0; rating < 8; rating++)
                    {
                        controller.Sim.S.chat.Add(new LingGuangV05.XingGuang.XgChatLine { from = "ai", text = "好呀，嘿嘿，我觉得可以！" });
                        Check(controller.Sim.RateReply(controller.Sim.S.chat.Count - 1, false), "Actual dislike trains the personality: " + rating);
                    }
                    due = EditorApplication.timeSinceStartup + 3;
                    break;
                case 75:
                    var feedback = brain.GetComponentInChildren<XgWallpaperPersona>();
                    Check(feedback.Axes.x < .93f && Mathf.Abs(feedback.Axes.x - (float)controller.Sim.ActualAxis(0) / 100) < .005f, "Real dialogue feedback reaches the halo smoothly: " + feedback.Axes.x + "/" + controller.Sim.ActualAxis(0));
                    controller.enabled = true; runtime.SetSimulationForTests(new ChapterOneSim());
                    break;
                case 76:
                    Check(brain.GetComponentInChildren<XgWallpaperPersona>().canvasRenderer.GetAlpha() == 0, "Restart hides previous personality halo");
                    File.WriteAllText(Path.Combine(Output, "result.txt"), "PASS"); Finish(0); break;
                case 80:
                    controller.Window.MinimizeWindow(); controller.enabled = false;
                    controller.Sim.S.stage = 1; controller.Sim.S.abilitiesEmerged = new System.Collections.Generic.List<int> { 1 };
                    controller.Sim.S.unlocked.Remove("mlp"); controller.Sim.S.trainedParamsK = 3;
                    controller.Sim.S.labels.Clear(); controller.Sim.S.labels.Add(new LingGuangV05.XingGuang.XgLabelCount { dataset = "logic", count = 1500 });
                    break;
                case 81:
                    var scale = brain.GetComponentInChildren<XgWallpaperAbilities>();
                    Check(scale.LearnedCount == 1 && scale.NextAbility == 2, "Only actually learned ability is lit");
                    Check(Mathf.Approximately(scale.ParameterProgress, .5f) && Mathf.Approximately(scale.DataProgress, (float)controller.Sim.SamplesProgress(2)), "Parameter and data bars use independent real progress");
                    for (int ability = 0; ability < 6; ability++)
                    {
                        var lamp = Pixel(UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>().sourceCamera.WorldToScreenPoint(scale.transform.TransformPoint(scale.LampPoint(ability))));
                        Check(ability == 0 ? lamp.g > lamp.r + .2f : lamp.g < .3f, "Actual ability lamp brightness: " + ability);
                        var abilityName = scale.transform.Find("Ability " + (ability + 1)).GetComponent<TMPro.TMP_Text>();
                        Check(abilityName.text.Length > 0 && !abilityName.isTextOverflowing, "Ability name fits: " + ability);
                    }
                    Capture("abilities-progress"); controller.Sim.S.unlocked.Add("mlp");
                    break;
                case 82:
                    Check(Mathf.Approximately(brain.GetComponentInChildren<XgWallpaperAbilities>().ParameterProgress, .625f), "Owned architecture discount updates the real parameter threshold");
                    controller.Sim.S.trainedParamsK = 6; controller.Sim.S.labels[0].count = 6000; controller.Sim.S.stageEpochs = 1;
                    break;
                case 83:
                    var readyScale = brain.GetComponentInChildren<XgWallpaperAbilities>();
                    Check(readyScale.ParameterProgress == 1 && readyScale.DataProgress == 1 && readyScale.LearnedCount == 1, "Full bars do not falsely light an unconfirmed ability");
                    Check(controller.Sim.ConfirmNextStage(), "Player's real chapter confirmation learns the next ability");
                    break;
                case 84:
                    var advancedScale = brain.GetComponentInChildren<XgWallpaperAbilities>();
                    Check(advancedScale.LearnedCount == 2 && advancedScale.NextAbility == 3, "Confirmed advancement lights exactly one new ability and moves both bars");
                    Check(advancedScale.transform.Find("Progress 2").GetComponent<TMPro.TMP_Text>().text == "", "Previous ability progress is removed");
                    controller.Sim.S.abilitiesEmerged = new System.Collections.Generic.List<int> { 1, 2, 3, 4, 5, 6 }; controller.Sim.S.stage = 6;
                    break;
                case 85:
                    var fullScale = brain.GetComponentInChildren<XgWallpaperAbilities>();
                    Check(fullScale.LearnedCount == 6 && fullScale.NextAbility == 0, "All six abilities remain lit with no phantom seventh target");
                    for (int ability = 1; ability <= 6; ability++) Check(fullScale.transform.Find("Progress " + ability).GetComponent<TMPro.TMP_Text>().text == "", "Completed scale has no obsolete progress: " + ability);
                    Capture("abilities-complete"); controller.enabled = true; runtime.SetSimulationForTests(new ChapterOneSim());
                    break;
                case 86:
                    Check(brain.GetComponentInChildren<XgWallpaperAbilities>().canvasRenderer.GetAlpha() == 0, "New prologue hides previous ability scale");
                    File.WriteAllText(Path.Combine(Output, "result.txt"), "PASS"); Finish(0); break;
                case 90:
                    controller.Window.MinimizeWindow(); controller.enabled = false;
                    controller.Sim.S.familyAttitudes = new System.Collections.Generic.List<LingGuangV05.XingGuang.XgFamilyAttitude>();
                    break;
                case 91:
                    var noFamily = brain.GetComponentInChildren<XgWallpaperFamilyStar>();
                    Check(!noFamily.Strong && noFamily.canvasRenderer.GetAlpha() == 0, "No learned family choices means no star");
                    controller.Sim.S.familyAttitudes.Add(new LingGuangV05.XingGuang.XgFamilyAttitude { sender = "妈妈", important = 2 });
                    break;
                case 92:
                    Check(!brain.GetComponentInChildren<XgWallpaperFamilyStar>().Strong, "Two choices are not yet a strong family belief");
                    var familyCard = controller.Sim.Card("spam"); familyCard.family = true; familyCard.from = "妈妈";
                    Check(controller.Sim.Answer("spam", false, controller.Host).subjective, "Real family SMS answer records a personal preference");
                    boardBeforeDraw = JsonUtility.ToJson(controller.Sim.S);
                    due = EditorApplication.timeSinceStartup + 2;
                    break;
                case 93:
                    var familyStar = brain.GetComponentInChildren<XgWallpaperFamilyStar>();
                    Check(familyStar.Strong && familyStar.canvasRenderer.GetAlpha() > .95f, "Third important family SMS reveals the star");
                    glassPoint = UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>().sourceCamera.WorldToScreenPoint(familyStar.transform.position);
                    learnedPixel = Pixel(glassPoint);
                    Check(learnedPixel.r > learnedPixel.b + .2f && learnedPixel.g > learnedPixel.b + .1f, "Family star actually renders warm gold");
                    Check(!familyStar.raycastTarget && controller.Sim.S.familyAttitudes[0].important == 3, "Star preserves learned family count and never intercepts input");
                    Capture("family-star"); controller.Sim.S.familyAttitudes[0].junk = 3;
                    due = EditorApplication.timeSinceStartup + 2;
                    break;
                case 94:
                    var mixedFamily = brain.GetComponentInChildren<XgWallpaperFamilyStar>();
                    Check(!mixedFamily.Strong && mixedFamily.canvasRenderer.GetAlpha() == 0 && Pixel(glassPoint).r < learnedPixel.r - .2f, "Mixed choices remove star and its actual pixels");
                    controller.Sim.S.familyAttitudes[0].junk = 0;
                    controller.enabled = true; runtime.SetSimulationForTests(new ChapterOneSim());
                    break;
                case 95:
                    Check(brain.GetComponentInChildren<XgWallpaperFamilyStar>().canvasRenderer.GetAlpha() == 0, "Restart never inherits the previous family's star");
                    File.WriteAllText(Path.Combine(Output, "result.txt"), "PASS"); Finish(0); break;
                case 100:
                    controller.Window.MinimizeWindow(); controller.enabled = false;
                    SetRules(controller, 3);
                    break;
                case 101:
                    var rules = brain.GetComponentInChildren<XgWallpaperConcepts>();
                    Check(rules.Pins.Count == 2 && rules.Cells.Count == 4, "Only true pinned concepts receive rivets, including the golden seed");
                    Capture("rules-rivets-diagnostic");
                    foreach (var rule in rules.Pins)
                        for (int corner = 0; corner < 4; corner++)
                        {
                            var pixel = Pixel(UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>().sourceCamera.WorldToScreenPoint(rules.transform.TransformPoint(rules.RivetPoint(rule, corner))));
                            Check(pixel.r > .4f && pixel.g > .4f && pixel.b > .4f, "Actual silver rivet exists: " + rule.id + "/" + corner + " pixel=" + pixel + " area=" + rules.rectTransform.rect);
                        }
                    Check(boardBeforeDraw == JsonUtility.ToJson(controller.Sim.S.board), "Rivet rendering leaves pinned rules unchanged");
                    Capture("rules-rivets");
                    controller.Sim.Board.Find("logic", "规则:尊重玩家").pinned = false;
                    break;
                case 102:
                    Check(brain.GetComponentInChildren<XgWallpaperConcepts>().Pins.Count == 1, "Removing pin state removes the rule's four rivets");
                    controller.Sim.Board.Pin("logic", "规则:尊重玩家", 3); controller.Sim.Board.Reinitialise();
                    break;
                case 103:
                    Check(brain.GetComponentInChildren<XgWallpaperConcepts>().Pins.Count == 2 && brain.GetComponentInChildren<XgWallpaperConcepts>().Cells.Count == 1, "Actual board reset preserves both pinned rules and their rivets");
                    Capture("rules-after-reset"); SetRules(controller, 520);
                    break;
                case 104:
                    var crowdedRules = brain.GetComponentInChildren<XgWallpaperConcepts>();
                    Check(crowdedRules.Cells.Count == XgWallpaperConcepts.MeshCellLimit && crowdedRules.Pins.Count == 2, "Both protected rules remain visible beyond the cell display limit");
                    Check(crowdedRules.TryLocalPosition(controller.Sim.Board.Find("logic", "规则:尊重玩家").id, out _), "Late-added endgame rule retains a foreground slot");
                    controller.enabled = true; runtime.SetSimulationForTests(new ChapterOneSim());
                    break;
                case 105:
                    Check(brain.GetComponentInChildren<XgWallpaperConcepts>().Pins.Count == 0, "New game has no previous ending's rivets");
                    File.WriteAllText(Path.Combine(Output, "result.txt"), "PASS"); Finish(0); break;
                case 110:
                    controller.Window.MinimizeWindow(); controller.enabled = false;
                    controller.Sim.S.board = new LingGuangV05.XingGuang.XgBoardState();
                    var teaching = new LingGuangV05.XingGuang.XgBoardCard { region = "vision", truth = true }.Add("中奖");
                    var learning = new LingGuangV05.XingGuang.XgKnobs { depth = 1, width = 32, lr = .1 };
                    for (int lesson = 0; lesson < 12; lesson++) controller.Sim.Board.Train(teaching, learning);
                    hoverConceptId = controller.Sim.Board.Find("vision", "中奖").id;
                    controller.Sim.Board.Find("vision", "中奖").w = .8;
                    boardBeforeDraw = JsonUtility.ToJson(controller.Sim.S.board);
                    break;
                case 111:
                    HoverConcept(brain.GetComponentInChildren<XgWallpaperConcepts>(), hoverConceptId);
                    break;
                case 112:
                    Check(HoverText(brain).Contains("中奖") && HoverText(brain).Contains("是 0.8") && HoverText(brain).Contains("12 张"), "Real pointer hover shows learned concept, direction, weight and actual twelve cards");
                    Check(boardBeforeDraw == JsonUtility.ToJson(controller.Sim.S.board), "Hover does not train or alter the concept");
                    Capture("concept-hover");
                    controller.Sim.Board.Train(new LingGuangV05.XingGuang.XgBoardCard { region = "vision", truth = false }.Add("中奖"), new LingGuangV05.XingGuang.XgKnobs { depth = 1, width = 32, lr = .1 });
                    controller.Sim.Board.Find("vision", "中奖").w = -.8;
                    break;
                case 113:
                    Check(HoverText(brain).Contains("否 0.8") && HoverText(brain).Contains("13 张"), "Stationary tooltip updates after actual training");
                    controller.Open();
                    break;
                case 114:
                    controller.Window.windowContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 400);
                    var hoverNetwork = brain.GetComponentInChildren<XgWallpaperConcepts>();
                    controller.Window.windowContainer.position = hoverNetwork.transform.TransformPoint(hoverNetwork.rectTransform.rect.min + Vector2.Scale(hoverNetwork.rectTransform.rect.size, new Vector2(.87f, .5f)));
                    break;
                case 115:
                    Check(!brain.GetComponent<XgBrainWallpaperPresentation>().Showcase, "Application remains visibly open during blank-desktop hover test");
                    HoverConcept(brain.GetComponentInChildren<XgWallpaperConcepts>(), hoverConceptId);
                    break;
                case 116:
                    Check(HoverText(brain).Contains("中奖"), "Uncovered desktop concept remains inspectable while an application is open");
                    Capture("concept-hover-uncovered");
                    var coveredNetwork = brain.GetComponentInChildren<XgWallpaperConcepts>();
                    coveredNetwork.TryLocalPosition(hoverConceptId, out var coveredPoint);
                    controller.Window.windowContainer.position = coveredNetwork.transform.TransformPoint(coveredPoint);
                    break;
                case 117:
                    HoverConcept(brain.GetComponentInChildren<XgWallpaperConcepts>(), hoverConceptId);
                    break;
                case 118:
                    Check(!HoverText(brain).Contains("中奖"), "Application covering the concept prevents wallpaper hover leaking through");
                    controller.Window.MinimizeWindow();
                    var legacy = new LingGuangV05.XingGuang.XgBoardState();
                    legacy.concepts.Add(new LingGuangV05.XingGuang.XgConcept { id = 901, region = "vision", key = "<b>旧概念</b>", w = 1, s = 3, seen = 12345 });
                    controller.Sim.S.board = legacy;
                    break;
                case 119:
                    HoverConcept(brain.GetComponentInChildren<XgWallpaperConcepts>(), 901);
                    break;
                case 120:
                    Check(HoverText(brain).Contains("历史张数未记录") && !HoverText(brain).Contains("12345") && !HoverText(brain).Contains("<b>"), "Legacy count is honestly unknown and concept text cannot inject rich text");
                    controller.Sim.S.board.concepts.Clear();
                    break;
                case 121:
                    Check(!HoverText(brain).Contains("旧概念"), "Deleting the hovered concept closes its tooltip");
                    File.WriteAllText(Path.Combine(Output, "result.txt"), "PASS"); Finish(0); break;
                case 130:
                    controller.Window.MinimizeWindow(); controller.enabled = false;
                    controller.Sim.Run(LingGuangV05.XingGuang.XgTrack.Vision).dataset = "mnist";
                    controller.Sim.Run(LingGuangV05.XingGuang.XgTrack.Vision).arch = "lenet";
                    controller.Sim.Run(LingGuangV05.XingGuang.XgTrack.Sequence).dataset = "spam";
                    controller.Sim.Run(LingGuangV05.XingGuang.XgTrack.Sequence).arch = "rnn";
                    boardBeforeDraw = JsonUtility.ToJson(controller.Sim.S.board);
                    break;
                case 131:
                    Click((RectTransform)brain.GetComponentInChildren<XgWallpaperConcepts>().transform.Find("Region vision"));
                    break;
                case 132:
                    var visionCard = brain.GetComponent<XgWallpaperRegionCard>();
                    Check(visionCard.IsOpen && visionCard.Title.Contains("看图区") && visionCard.Body == LingGuangV05.XingGuang.XgItemLore.Get("lenet", false), "Real brain-area click opens current architecture's existing lore");
                    Capture("region-card-vision-diagnostic");
                    foreach (var text in visionCard.Card.GetComponentsInChildren<TMPro.TMP_Text>()) Check(!text.isTextOverflowing, "Structure card text fits: " + text.name);
                    Check(visionCard.Card.parent == brain.transform.parent && !visionCard.Card.IsChildOf(brain.transform), "Structure card stays independent of wallpaper pin copying");
                    Capture("region-card-vision");
                    Click((RectTransform)brain.GetComponentInChildren<XgWallpaperConcepts>().transform.Find("Region sequence"));
                    break;
                case 133:
                    Check(brain.GetComponent<XgWallpaperRegionCard>().Body == LingGuangV05.XingGuang.XgItemLore.Get("rnn", false), "Clicking another area replaces the card with that area's explanation");
                    controller.Sim.Run(LingGuangV05.XingGuang.XgTrack.Sequence).arch = "lstm";
                    break;
                case 134:
                    Check(brain.GetComponent<XgWallpaperRegionCard>().Body == LingGuangV05.XingGuang.XgItemLore.Get("lstm", false), "Open card follows the area's current structure");
                    Capture("region-card-lstm");
                    if (SessionState.GetString("DesignWallpaperSmoke.Mode", "") == "region-cards-visual")
                    { controller.StartCoroutine(ObserveRenderedDesktop(brain)); step = 140; break; }
                    Click(brain.GetComponent<XgWallpaperRegionCard>().CloseButton);
                    break;
                case 135:
                    Check(!brain.GetComponent<XgWallpaperRegionCard>().IsOpen, "Actual close button dismisses the card");
                    Click((RectTransform)brain.GetComponentInChildren<XgWallpaperConcepts>().transform.Find("Region tone"));
                    break;
                case 136:
                    Check(brain.GetComponent<XgWallpaperRegionCard>().Body.Contains("温度") && brain.GetComponent<XgWallpaperRegionCard>().Body.Contains("玩心"), "Tone area explains its real three axes in plain language");
                    Check(boardBeforeDraw == JsonUtility.ToJson(controller.Sim.S.board), "Reading structures never edits learned concepts");
                    controller.enabled = true; runtime.SetSimulationForTests(new ChapterOneSim());
                    break;
                case 137:
                    Check(!brain.GetComponent<XgWallpaperRegionCard>().IsOpen, "Restart closes the previous save's structure card");
                    File.WriteAllText(Path.Combine(Output, "result.txt"), "PASS"); Finish(0); break;
                case 140:
                    if (visualFrames < 180) { step--; break; }
                    Check(badVisualFrames == 0, "180 actual rendered frames keep opaque brain wallpaper and legible card; bad frames=" + badVisualFrames);
                    File.WriteAllText(Path.Combine(Output, "result.txt"), "PASS"); Finish(0); break;
                case 149:
                    controller.Window.MinimizeWindow();
                    break;
                case 150:
                    controller.enabled = false;
                    boardBeforeDraw = JsonUtility.ToJson(controller.Sim.S.board);
                    var desktopMenu = brain.transform.parent.Find("Desktop Context Menu").GetComponent<Michsky.DreamOS.ContextMenuContent>();
                    originalMenuCount = desktopMenu.menuItems.Count - 3;
                    Check(originalMenuCount >= 2, "Original desktop menu actions are retained");
                    Click((RectTransform)brain.transform, UnityEngine.EventSystems.PointerEventData.InputButton.Right);
                    break;
                case 151:
                    Check(UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>().contextMenu.isOn, "Real desktop right click opens the native menu");
                    Check(MenuButton("查看") != null && MenuButton("安静") != null, "View and Quiet appear in native menu");
                    controller.StartCoroutine(CaptureRendered("wallpaper-menu"));
                    break;
                case 152:
                    Click(MenuButton("安静"));
                    break;
                case 153:
                    var quietView = brain.GetComponent<XgBrainWallpaperPresentation>();
                    Check(quietView.Quiet && quietView.LightLevel <= .401f && quietView.LightLevel > .2f, "Quiet dims only the living wallpaper and keeps it visible");
                    Check(!UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>().contextMenu.isOn, "Selecting Quiet closes the native menu");
                    quietPhase = brain.GetComponentInChildren<XgWallpaperPersona>().Phase; quietTime = Time.unscaledTime;
                    controller.StartCoroutine(CaptureRendered("wallpaper-quiet"));
                    break;
                case 154:
                    float phaseRate = (brain.GetComponentInChildren<XgWallpaperPersona>().Phase - quietPhase) / (Time.unscaledTime - quietTime);
                    Check(phaseRate > .1f && phaseRate < .2f, "Quiet slows continuous animation to 15 percent without stopping the game");
                    Check(boardBeforeDraw == JsonUtility.ToJson(controller.Sim.S.board), "Quiet never edits learned concepts");
                    Click((RectTransform)brain.transform, UnityEngine.EventSystems.PointerEventData.InputButton.Right);
                    break;
                case 155:
                    Check(MenuButton("安静（已开启，点击恢复）") != null, "Reopened menu explains Quiet state and restoration");
                    controller.StartCoroutine(CaptureRendered("wallpaper-menu-quiet"));
                    break;
                case 156:
                    Click(MenuButton("安静（已开启，点击恢复）"));
                    break;
                case 157:
                    Check(!brain.GetComponent<XgBrainWallpaperPresentation>().Quiet && brain.GetComponent<XgBrainWallpaperPresentation>().LightLevel > .95f, "Second click restores normal brightness; quiet=" + brain.GetComponent<XgBrainWallpaperPresentation>().Quiet + "; light=" + brain.GetComponent<XgBrainWallpaperPresentation>().LightLevel);
                    Click((RectTransform)brain.transform, UnityEngine.EventSystems.PointerEventData.InputButton.Right);
                    break;
                case 158:
                    controller.enabled = true;
                    Click(MenuButton("查看"));
                    break;
                case 159:
                    Check(controller.View.Tab == "home" && XgBrainWallpaperPresentation.IsVisible(controller.Window.windowContainer), "View opens the AI profile in its normal application window");
                    var nameRow = controller.View.NavButton("home");
                    Check(nameRow.gameObject.activeInHierarchy && Array.Exists(nameRow.GetComponentsInChildren<TMPro.TMP_Text>(), text => text.text == controller.Sim.AiDisplayName), "Profile navigation uses the player's AI name even in early chapters");
                    controller.StartCoroutine(CaptureRendered("wallpaper-view-profile"));
                    break;
                case 160:
                    controller.Window.MinimizeWindow();
                    menuLanguage = GameText.Bind(() => GameText.English); GameText.PublishChanged();
                    break;
                case 161:
                    Click((RectTransform)brain.transform, UnityEngine.EventSystems.PointerEventData.InputButton.Right);
                    break;
                case 162:
                    Check(MenuButton("View") != null && MenuButton("Quiet") != null, "Wallpaper menu follows language changes");
                    UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>().contextMenu.Close();
                    menuLanguage.Dispose(); menuLanguage = null; GameText.PublishChanged();
                    runtime.SetSimulationForTests(new ChapterOneSim());
                    if (SessionState.GetString("DesignWallpaperSmoke.Mode", "") == "desktop-fade")
                    {
                        StartDesktopFadeFrameChecks(); break;
                    }
                    break;
                case 163:
                    var resetMenu = brain.transform.parent.Find("Desktop Context Menu").GetComponent<Michsky.DreamOS.ContextMenuContent>();
                    Check(resetMenu.menuItems.Count == originalMenuCount && !brain.GetComponent<XgBrainWallpaperPresentation>().Quiet, "New game removes AI menu entries and restores Quiet state");
                    runtime.CompletePrologue(new PrologueProfile { name = "小星", self = "我", callMe = "你", personality = "好奇" });
                    due = EditorApplication.timeSinceStartup + 4;
                    break;
                case 164:
                    Check(brain.transform.parent.Find("Desktop Context Menu").GetComponent<Michsky.DreamOS.ContextMenuContent>().menuItems.Count == originalMenuCount + 3, "Completing setup again adds one copy of each menu item");
                    File.WriteAllText(Path.Combine(Output, "result.txt"), "PASS"); Finish(0); break;
                case 170:
                    DesignFirstLessonChecks.Run();
                    due = double.MaxValue;
                    controller.StartCoroutine(CheckLessonPresentation(controller, runtime, brain));
                    break;
            }
        }
        catch (Exception error)
        {
            File.WriteAllText(Path.Combine(Output, "result.txt"), "FAIL: " + error); Finish(1);
        }
    }
    static Color ConceptPixel(XgWallpaperConcepts concepts, int id)
    {
        Check(concepts.TryLocalPosition(id, out var point), "Concept position exists: " + id);
        var source = UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>().sourceCamera;
        return Pixel(source.WorldToScreenPoint(concepts.transform.TransformPoint(point)));
    }
    static System.Collections.IEnumerator ObserveRenderedDesktop(XgBrainWallpaper brain)
    {
        var bridge = UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>();
        var end = new WaitForEndOfFrame();
        File.WriteAllText(Path.Combine(Output, "rendered-frames.txt"), "");
        for (int frame = 0; frame < 180; frame++)
        {
            yield return end;
            var texture = bridge.sourceCamera.targetTexture;
            Color background = Pixel(new Vector2(texture.width * .52f, texture.height * .92f));
            var card = brain.GetComponent<XgWallpaperRegionCard>().Card;
            Color cardPixel = Pixel(bridge.sourceCamera.WorldToScreenPoint(card.TransformPoint(new Vector3(10, -10, 0))));
            // These positions are solid background regions, clear of text and glow.
            bool bad = background.r > .07f || background.g > .09f || background.b > .13f || cardPixel.b < .12f;
            if (bad) badVisualFrames++;
            File.AppendAllText(Path.Combine(Output, "rendered-frames.txt"), Time.frameCount + " background=" + background + " card=" + cardPixel + " bad=" + bad + "\n");
            if (frame == 179 || badVisualFrames == 1 && bad)
            {
                Capture(bad ? "region-card-bad-rendered-frame" : "region-card-rendered-frame");
                ScreenCapture.CaptureScreenshot(Path.Combine(Output, bad ? "region-card-bad-full-viewport.png" : "region-card-full-viewport.png"));
            }
            visualFrames++;
        }
    }
    static void SetPersona(XingGuangController controller, bool warmth, bool play, bool opinion)
    {
        var state = new LingGuangV05.XingGuang.XgBoardState();
        var keys = new[] { warmth ? "语气:热情" : "语气:冷静", play ? "语气:皮" : "语气:正经", opinion ? "语气:有主见" : "语气:顺从" };
        for (int i = 0; i < keys.Length; i++) state.concepts.Add(new LingGuangV05.XingGuang.XgConcept { id = i + 1, region = "tone", key = keys[i], w = 4, s = 10 });
        state.nextId = 4; controller.Sim.S.board = state;
        boardBeforeDraw = JsonUtility.ToJson(state);
    }
    static void SetRules(XingGuangController controller, int ordinary)
    {
        var state = new LingGuangV05.XingGuang.XgBoardState();
        for (int i = 0; i < ordinary; i++) state.concepts.Add(new LingGuangV05.XingGuang.XgConcept { id = i + 1, region = "logic", key = "普通" + i, w = 3, s = 10 });
        state.nextId = ordinary + 1; controller.Sim.S.board = state;
        controller.Sim.Board.Plant("logic", LingGuangV05.XingGuang.XgSim.SeedKey, -3);
        controller.Sim.Board.Pin("logic", LingGuangV05.XingGuang.XgSim.SeedKey, 3);
        controller.Sim.Board.Pin("logic", "规则:尊重玩家", 3);
        boardBeforeDraw = JsonUtility.ToJson(state);
    }
    static void CheckTrainingCounts()
    {
        var board = new LingGuangV05.XingGuang.XgBoard();
        var card = new LingGuangV05.XingGuang.XgBoardCard { region = "logic", truth = true }.Add("中奖");
        var knobs = new LingGuangV05.XingGuang.XgKnobs { depth = 1, width = 32, lr = .1 };
        for (int i = 0; i < 12; i++) board.Train(card, knobs);
        var concept = board.Find("logic", "中奖");
        Check(concept.trainingCountKnown && concept.trainedCards == 12, "New concept counts exactly twelve actual training cards");
        board.Predict(card, knobs, out _);
        Check(concept.trainedCards == 12, "Prediction never increments training count");
        var saved = JsonUtility.FromJson<LingGuangV05.XingGuang.XgBoardState>(JsonUtility.ToJson(board.S));
        var restored = new LingGuangV05.XingGuang.XgBoard(saved).Find("logic", "中奖");
        Check(restored.trainingCountKnown && restored.trainedCards == 12, "Real save serialization preserves concept counts");
        board.Pin("logic", "中奖"); board.Train(card, knobs);
        Check(concept.trainedCards == 12, "Protected pinned rule is not falsely reported as trained again");
        var old = new LingGuangV05.XingGuang.XgBoardState();
        old.concepts.Add(new LingGuangV05.XingGuang.XgConcept { id = 1, region = "logic", key = "中奖", w = 1, s = 1, seen = 9000 }); old.nextId = 2;
        var migrated = new LingGuangV05.XingGuang.XgBoard(old); migrated.Train(card, knobs);
        Check(!old.concepts[0].trainingCountKnown && old.concepts[0].trainedCards == 1, "Old save counts only new cards without fabricating past observations");
    }
    static string HoverText(XgBrainWallpaper brain)
    {
        var tip = brain.GetComponentInParent<Canvas>().rootCanvas.transform.Find("Hover Tip");
        return tip != null && tip.gameObject.activeInHierarchy ? tip.GetComponentInChildren<TMPro.TMP_Text>().text : "";
    }
    static void HoverConcept(XgWallpaperConcepts concepts, int id)
    {
        Check(concepts.TryLocalPosition(id, out var local), "Hover target has a real concept position: " + id);
        var bridge = UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>();
        var source = bridge.sourceCamera.WorldToScreenPoint(concepts.transform.TransformPoint(local));
        var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
        { position = new Vector2(source.x * Screen.width / bridge.sourceCamera.targetTexture.width, source.y * Screen.height / bridge.sourceCamera.targetTexture.height) };
        bridge.inputSurface.OnPointerEnter(pointer);
    }
    static Color HaloPixel(XgWallpaperPersona halo)
    {
        var point = halo.OutlinePoint(Mathf.PI * .5f, halo.Phase);
        return Pixel(UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>().sourceCamera.WorldToScreenPoint(halo.transform.TransformPoint(point)));
    }
    static RectTransform MenuButton(string label)
    {
        var menu = UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>().contextMenu;
        foreach (var button in menu.contentRect.GetComponentsInChildren<Michsky.DreamOS.ButtonManager>())
            if (button.buttonText == label) return button.transform as RectTransform;
        return null;
    }
    static System.Collections.IEnumerator CaptureRendered(string name)
    {
        yield return new WaitForEndOfFrame();
        Capture(name);
        ScreenCapture.CaptureScreenshot(Path.Combine(Output, name + "-full.png"));
    }
    static void Click(RectTransform target, UnityEngine.EventSystems.PointerEventData.InputButton button = UnityEngine.EventSystems.PointerEventData.InputButton.Left)
    {
        Check(target != null, "Pointer target exists");
        var bridge = UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>();
        var source = bridge.sourceCamera.WorldToScreenPoint(target.TransformPoint(target.rect.center));
        var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
        {
            position = new Vector2(source.x * Screen.width / bridge.sourceCamera.targetTexture.width, source.y * Screen.height / bridge.sourceCamera.targetTexture.height),
            button = button
        };
        if (SessionState.GetString("DesignWallpaperSmoke.Mode", "") == "menu" && UnityEngine.InputSystem.Mouse.current != null)
            UnityEngine.InputSystem.LowLevel.InputState.Change(UnityEngine.InputSystem.Mouse.current.position, pointer.position);
        bridge.inputSurface.OnPointerEnter(pointer); bridge.inputSurface.OnPointerDown(pointer); bridge.inputSurface.OnPointerUp(pointer);
        if (SessionState.GetString("DesignWallpaperSmoke.Mode", "") == "menu")
        {
            var data = typeof(AeroLcdInputSurface).GetField("hoverData", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(bridge.inputSurface) as UnityEngine.EventSystems.PointerEventData;
            File.AppendAllText(Path.Combine(Output, "menu-pointer.txt"), button + " position=" + pointer.position + " target=" + data?.pointerCurrentRaycast.gameObject + " menu=" + bridge.contextMenu.isOn + " rect=" + bridge.sourceCamera.WorldToScreenPoint(bridge.contextMenu.contentRect.position) + "\n");
        }
    }
    static Color Pixel(Vector2 point)
    {
        var target = UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>().sourceCamera.targetTexture;
        var previous = RenderTexture.active;
        var texture = new Texture2D(1, 1, TextureFormat.RGB24, false);
        try
        {
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(Mathf.Clamp((int)point.x, 0, target.width - 1), Mathf.Clamp((int)point.y, 0, target.height - 1), 1, 1), 0, 0);
            texture.Apply(); return texture.GetPixel(0, 0);
        }
        finally { RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(texture); }
    }
    static void Capture(string name)
    {
        var brain = UnityEngine.Object.FindAnyObjectByType<XgBrainWallpaper>();
        if (brain != null)
        {
            string diagnostic = name + " frame=" + Time.frameCount + " reveal=" + brain.Reveal.Opacity + " bg=" + brain.GetComponent<UnityEngine.UI.Image>().canvasRenderer.GetColor() + " inherited=" + brain.GetComponent<UnityEngine.UI.Image>().canvasRenderer.GetInheritedAlpha() + "\n";
            for (Transform node = brain.transform; node != null; node = node.parent)
            {
                var group = node.GetComponent<CanvasGroup>();
                if (group != null) diagnostic += "Group " + node.name + "=" + group.alpha + "\n";
            }
            foreach (var window in UnityEngine.Object.FindObjectsByType<Michsky.DreamOS.WindowManager>(FindObjectsSortMode.None))
                if (window.windowContainer != null && brain.transform.IsChildOf(window.windowContainer)) diagnostic += "Ancestor window=" + window.name + " container=" + window.windowContainer.name + " on=" + window.isOn + "\n";
            foreach (var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (camera.targetTexture != null) diagnostic += "Camera=" + camera.name + " target=" + camera.targetTexture.name + " id=" + camera.targetTexture.GetInstanceID() + " flags=" + camera.clearFlags + " mask=" + camera.cullingMask + "\n";
            File.AppendAllText(Path.Combine(Output, "capture-diagnostics.txt"), diagnostic);
        }
        var bridge = UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>();
        var target = bridge.sourceCamera.targetTexture;
        var previous = RenderTexture.active;
        var texture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        try
        {
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); texture.Apply();
            File.WriteAllBytes(Path.Combine(Output, name + ".png"), texture.EncodeToPNG());
        }
        finally { RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(texture); }
    }
    static void Finish(int code)
    {
        SessionState.SetBool(Key, false);
        if (reloadLocked) { reloadLocked = false; AssetDatabase.AllowAutoRefresh(); EditorApplication.UnlockReloadAssemblies(); }
        EditorApplication.isPlaying = false;
        EditorApplication.Exit(code);
    }
}
