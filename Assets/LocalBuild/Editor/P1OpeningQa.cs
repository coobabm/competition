#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HongmengOS.Aero2010;
using LingGuangV05.Core;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.LLM;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 技能树与自动化 v4 Phase P1 QA (2026-10-22). Main PlayMode, isolated save and DreamOS profile, no model.
//   -executeMethod P1OpeningQa.RunNatural   new game at the shipped pace (0.6); scripted "player" answers after 1 s
//   -executeMethod P1OpeningQa.RunWritten   the same at the written pace (1.0) = the timing before P1
//   -executeMethod P1OpeningQa.RunSkip      Esc "skip the opening" right after the title, then the first-time spotlight
//   -executeMethod P1OpeningQa.RunSegments  "hold to skip" on every segment in turn (questions included) down to the setup
//   -executeMethod P1OpeningQa.RunDesk      F10: a desk game with 自动答题 bought shows its state, 交给它/我来做 and 去训练
// Without -quit. Writes report.json, timing and LCD captures under LocalVerification/p1-20261022/qa-<mode>-<time>.
// The scripted answers press the real buttons' onClick (否, 0.txt's open, 否); no OS input is synthesised.
[InitializeOnLoad]
public static class P1OpeningQa
{
    const string Key = "P1OpeningQa.Root", ModeKey = "P1OpeningQa.Mode";
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    const string OutBase = "C:/Users/xomem/Documents/ChatGPT/games/LocalVerification/p1-20261022";
    static string Root => SessionState.GetString(Key, "");
    static string Mode => SessionState.GetString(ModeKey, "");

    [Serializable] sealed class Mark { public string step; public float at; }
    [Serializable] sealed class Report
    {
        public string mode, status = "running", failure = "", unityVersion;
        public float pace;
        public List<Mark> steps = new List<Mark>();
        public List<string> checks = new List<string>(), timing = new List<string>(), runtimeErrors = new List<string>();
    }
    static Report report;
    static ChapterOneRuntime runtime;
    static XingGuangController ctl;
    static PrologueDirector director;
    static ChapterOneDesktopRouter router;
    static int phase, lastFrame = -1, frames;
    static double timeout;
    static float t0, marked, lastStepAt;
    static string lastStep = "";
    static bool finishing, booted;
    static readonly Dictionary<string, float> seen = new Dictionary<string, float>();

    static P1OpeningQa() { EditorApplication.playModeStateChanged += State; }

    public static void RunNatural() => Begin("natural");
    public static void RunWritten() => Begin("written");
    public static void RunSkip() => Begin("skip");
    public static void RunDesk() => Begin("desk");
    public static void RunSegments() => Begin("segments");

    static void Begin(string mode)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start in Edit Mode");
        string dir = Path.Combine(OutBase, "qa-" + mode + "-" + DateTime.Now.ToString("HHmmss"));
        Directory.CreateDirectory(Path.Combine(dir, "Save")); Directory.CreateDirectory(Path.Combine(dir, "DesktopProfile"));
        SessionState.SetString(Key, dir); SessionState.SetString(ModeKey, mode);
        EditorSceneManager.OpenScene("Assets/LingGuangV05/Scenes/Main.unity");
        EditorApplication.EnterPlaymode();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Isolate()
    {
        if (string.IsNullOrEmpty(Root)) return;
        ChapterOneRuntime.VerificationSaveDirectory = Path.Combine(Root, "Save");
        typeof(LingGuangV05.Desktop.OS.DreamOSDataManager).GetField("dataPath", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, Path.Combine(Root, "DesktopProfile") + Path.DirectorySeparatorChar);
        SceneManager.sceneLoaded += DisableModel;
        PrologueDirector.OpeningPace = Mode == "written" ? 1f : .6f;
    }

    static void DisableModel(Scene scene, LoadSceneMode mode)
    {
        if (string.IsNullOrEmpty(Root)) return;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var rt in root.GetComponentsInChildren<ChapterOneRuntime>(true))
            { var llm = rt.GetComponent<LocalLlm>() ?? rt.gameObject.AddComponent<LocalLlm>(); llm.enabled = false; }
    }

    static void State(PlayModeStateChange state)
    {
        if (string.IsNullOrEmpty(Root)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            phase = frames = 0; lastFrame = -1; finishing = booted = false; seen.Clear(); lastStep = "";
            timeout = EditorApplication.timeSinceStartup + 300;
            report = new Report { mode = Mode, unityVersion = Application.unityVersion, pace = PrologueDirector.OpeningPace };
            Application.logMessageReceived += Log; EditorApplication.update += Tick;
        }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.update -= Tick; Application.logMessageReceived -= Log; SceneManager.sceneLoaded -= DisableModel;
            string path = Root; bool ok = File.Exists(Path.Combine(path, "passed.txt"));
            SessionState.EraseString(Key); SessionState.EraseString(ModeKey); ChapterOneRuntime.VerificationSaveDirectory = null;
            PrologueDirector.OpeningPace = .6f;
            Debug.Log("P1_OPENING_QA " + (ok ? "PASSED" : "FAILED") + ": " + path);
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }
    }

    static void Log(string message, string stack, LogType type)
    {
        if (report == null || finishing) return;
        if (message.StartsWith("[PrologueTiming] ")) report.timing.Add((Time.realtimeSinceStartup - t0).ToString("0.00") + " s | " + message.Substring(17));
        if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert) return;
        if (stack.Contains("UnityEditor.Search.SearchDatabase") && !stack.Contains("Assets/")) return;
        report.runtimeErrors.Add(message + "\n" + stack);
    }

    static void Need(bool ok, string what) { if (!ok) throw new InvalidOperationException(what); report.checks.Add(what); }
    static void Save() => File.WriteAllText(Path.Combine(Root, "report.json"), JsonUtility.ToJson(report, true));
    static float Now => Time.realtimeSinceStartup - t0;

    static void Finish(string error)
    {
        if (finishing) return;
        finishing = true;
        if (error == null && report.runtimeErrors.Count > 0) error = "runtime error: " + report.runtimeErrors[0];
        report.status = error == null ? "passed" : "failed"; report.failure = error ?? "";
        Save();
        if (error == null) File.WriteAllText(Path.Combine(Root, "passed.txt"), "P1 " + Mode + " QA passed; see report.json");
        else Debug.LogError("P1 QA failed: " + error);
        EditorApplication.isPlaying = false;
    }

    static void Capture(string name)
    {
        var bridge = UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>();
        var target = bridge != null && bridge.sourceCamera != null ? bridge.sourceCamera.targetTexture : null;
        if (target == null) { report.checks.Add("capture skipped (no LCD target): " + name); return; }
        bridge.sourceCamera.Render();
        var previous = RenderTexture.active;
        var texture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        try
        {
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); texture.Apply();
            File.WriteAllBytes(Path.Combine(Root, name + ".png"), texture.EncodeToPNG());
            report.checks.Add("captured " + name + ".png");
        }
        finally { RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(texture); }
    }

    static bool Boot()
    {
        runtime = UnityEngine.Object.FindAnyObjectByType<ChapterOneRuntime>();
        ctl = runtime == null ? null : runtime.GetComponent<XingGuangController>();
        if (ctl == null || ctl.Sim == null || ctl.View == null) return false;
        if (!booted)
        {
            var boot = UnityEngine.Object.FindAnyObjectByType<LingGuangV05.Desktop.OS.BootManager>(FindObjectsInactive.Include);
            if (boot != null) { boot.CancelInvoke(); boot.StopAllCoroutines(); if (boot.bootAnimator != null) boot.bootAnimator.gameObject.SetActive(false); }
            var user = UnityEngine.Object.FindAnyObjectByType<LingGuangV05.Desktop.OS.UserManager>(FindObjectsInactive.Include);
            if (user == null || !user.isActiveAndEnabled) return false;
            user.ShowDesktop(); booted = true; t0 = Time.realtimeSinceStartup;
        }
        router = runtime.GetComponent<ChapterOneDesktopRouter>() ?? UnityEngine.Object.FindAnyObjectByType<ChapterOneDesktopRouter>(FindObjectsInactive.Include);
        director = UnityEngine.Object.FindAnyObjectByType<PrologueDirector>();
        return router != null && director != null;
    }

    static Button ButtonIn(Transform root, string label)
    {
        if (root == null) return null;
        foreach (var b in root.GetComponentsInChildren<Button>(true))
        {
            var t = b.GetComponentInChildren<TMP_Text>(true);
            if (t != null && t.text == label && b.gameObject.activeInHierarchy) return b;
        }
        return null;
    }

    static bool ExeOpen()
    {
        var app = router.Get(LingGuangInstallFlow.AppId);
        return app != null && app.Window != null && app.Window.isOn;
    }

    /// <summary>Waits <paramref name="seconds"/> in a step before doing <paramref name="action"/> once.</summary>
    static void After(string key, float seconds, Action action)
    {
        if (!seen.TryGetValue(key, out var since)) { seen[key] = Now; return; }
        if (since < 0 || Now - since < seconds) return;
        seen[key] = -1; action();
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying || finishing) return;
        try
        {
            if (EditorApplication.timeSinceStartup > timeout) throw new TimeoutException("P1 QA timeout in phase " + phase + ", step " + (director != null ? director.Step : "?"));
            if (Time.frameCount == lastFrame) return; lastFrame = Time.frameCount;
            if (report.runtimeErrors.Count > 0) throw new InvalidOperationException("Runtime error: " + report.runtimeErrors[0]);
            if (Mode == "desk") { TickDesk(); return; }
            if (!Boot()) return;
            string step = director.Step ?? "";
            if (step != lastStep) { report.steps.Add(new Mark { step = step, at = Now }); lastStep = step; lastStepAt = Now; Save(); }
            if (Mode == "skip") TickSkip(step); else if (Mode == "segments") TickSegments(step); else TickNatural(step);
        }
        catch (Exception e) { Finish(e.ToString()); }
    }

    static Transform Window(string name) => PrologueDirector.Desk != null && PrologueDirector.Desk.windows != null ? PrologueDirector.Desk.windows.Find(name) : null;

    /// <summary>A new game at its own pace: each question is answered by the real button a second after it appears.</summary>
    static void TickNatural(string step)
    {
        string no = PrologueContent.L("no");
        if (step == "virus" && Window("Prologue Virus Alert") != null)
            After("virus", 1, () => { var b = ButtonIn(Window("Prologue Virus Alert"), no); Need(b != null, "360 alert has its 否 button"); b.onClick.Invoke(); });
        if (step == "txt")
        {
            var icon = PrologueDirector.Desk.icons.Find("Prologue 0.txt");
            if (icon != null) After("txt", 1, () => { var click = icon.GetComponent<PrologueClick>(); Need(click != null && click.Open != null, "0.txt opens"); click.Open(); });
        }
        if (step == "shutdown" && Window("Prologue Shutdown") != null)
            After("shutdown", 1, () => { var b = ButtonIn(Window("Prologue Shutdown"), no); Need(b != null, "shutdown dialog has its 否 button"); b.onClick.Invoke(); });
        if (step != "setup") return;
        if (!seen.ContainsKey("setup-at")) { seen["setup-at"] = Now; report.checks.Add("reached setup at " + Now.ToString("0.0") + " s after the desktop showed (pace " + PrologueDirector.OpeningPace + ")"); report.checks.Add("background music started on the desktop before the setup: " + GameMusic.Started + " (needs audio; batch mode may have none)"); }
        After("before-shot", 2.5f, () => { Need(!director.SpotlightOn, "no spotlight in the first seconds"); Capture("spotlight-before"); });
        if (director.SpotlightOn) After("spot-shot", 3f, () =>
        {
            var spider = XgResidentSpider.Instance;
            Need(spider != null && spider.Beckoning, "the spider is out early for the beat");
            Need(spider.BeckonArrived, "the spider reached the icon");
            Need(spider.RubRounds >= 1, "the spider rubs its legs (" + spider.RubRounds + " rounds)");
            Capture("spotlight-after");
            router.Open(LingGuangInstallFlow.AppId);
        });
        if (seen.TryGetValue("spot-shot", out var opened) && opened < 0 && ExeOpen())
            After("opened", 1f, () =>
            {
                Need(!director.SpotlightOn, "opening the exe lifts the dim");
                Need(XgResidentSpider.Instance == null || !XgResidentSpider.Instance.Beckoning, "the spider leaves after the hop");
                Capture("setup-opened");
                Finish(null);
            });
    }

    /// <summary>Where the dim sits and how big it is (the capture alone cannot say why it would not show).</summary>
    static void Diagnose()
    {
        var mask = UnityEngine.Object.FindAnyObjectByType<PrologueSpotlightMask>();
        if (mask == null) { report.checks.Add("diag: no mask"); return; }
        string path = mask.name; for (var t = mask.transform.parent; t != null; t = t.parent) path = t.name + "/" + path;
        var parent = mask.transform.parent;
        string siblings = ""; for (int i = 0; i < parent.childCount; i++) siblings += i + ":" + parent.GetChild(i).name + (parent.GetChild(i).gameObject.activeSelf ? "" : "(off)") + " ";
        var r = mask.rectTransform.rect;
        report.checks.Add("diag: " + path + " rect " + r.width + "x" + r.height + " dim " + mask.Dim + " ring " + mask.RingAlpha + " center " + mask.Center + " canvas " + (mask.canvas != null ? mask.canvas.name : "none") + " depth " + mask.depth + " | " + siblings);
    }

    static bool InputFree()
    {
        var surface = UnityEngine.Object.FindAnyObjectByType<AeroLcdInputSurface>(FindObjectsInactive.Include);
        return surface == null || surface.inputEnabled;
    }

    /// <summary>Every segment skipped by the same call a 0.8 s hold makes, one after another.</summary>
    static void TickSegments(string step)
    {
        if (Array.IndexOf(new[] { "title", "music-intent", "virus", "txt", "hijack", "grab", "shutdown", "extract" }, step) >= 0)
            After("seg-" + step, .3f, () =>
            {
                typeof(PrologueDirector).GetMethod("SkipSegment", Flags).Invoke(director, null);
                report.checks.Add("skipped " + step + " at " + Now.ToString("0.00") + " s");
            });
        if (step != "setup") return;
        After("seg-end", 1f, () =>
        {
            Need(runtime.Sim.AppInstalled && runtime.Sim.InPrologue, "segment skips end at the setup with 灵光.exe installed");
            Need(InputFree(), "no input lock is left after the skips");
            Need(!director.Skipping && !director.Pacing, "skip state is cleared at the setup");
            Need(PrologueDirector.Desk.icons.Find("Prologue 0.txt") == null && Window("Prologue Virus Alert") == null && Window("Prologue Shutdown") == null && Window("Prologue Browser") == null, "no scripted window or icon is left behind");
            Need(UnityEngine.Cursor.visible, "the real cursor is visible");
            report.checks.Add("reached setup at " + Now.ToString("0.0") + " s after the desktop showed");
            Capture("segments-setup");
            Finish(null);
        });
    }

    /// <summary>Esc's "skip the opening" right after the title card, then the spotlight's timeout path.</summary>
    static void TickSkip(string step)
    {
        if (step == "title")
            After("skip", .5f, () =>
            {
                typeof(PrologueDirector).GetMethod("SkipOpening", Flags).Invoke(director, null);
                Need(director.Skipping, "skip-all is running");
                seen["skip-at"] = Now;
            });
        if (step != "setup") return;
        if (!seen.ContainsKey("setup-at"))
        {
            seen["setup-at"] = Now;
            float from = seen.TryGetValue("skip-at", out var s) ? s : 0;
            report.checks.Add("skip reached setup " + (Now - from).ToString("0.00") + " s after the skip (" + Now.ToString("0.0") + " s after the desktop showed)");
            Need(runtime.Sim.AppInstalled, "the skipped opening installed 灵光.exe");
            Need(PrologueDirector.Desk.icons.Find("Prologue 0.txt") == null && Window("Prologue Browser") == null, "no scripted window or icon is left behind");
        }
        After("exe-open", .6f, () =>
        {
            Need(ExeOpen(), "a skipped opening opens 灵光.exe for the setup");
            Need(runtime.Sim.InPrologue, "the setup (naming) is not skipped");
            Need(!director.Pacing, "pacing ends at the setup");
            Need(InputFree(), "no input lock is left after the skip");
            Capture("skip-setup");
            router.Get(LingGuangInstallFlow.AppId).CloseApp();
            seen["closed-at"] = Now;
        });
        if (!seen.TryGetValue("closed-at", out var closedAt)) return;
        if (director.SpotlightOn && !seen.ContainsKey("spot-on")) { seen["spot-on"] = Now; report.checks.Add("spotlight after " + (Now - closedAt).ToString("0.0") + " s closed"); }
        if (seen.TryGetValue("spot-on", out var on))
        {
            After("spot-shot", 3, () => { Need(XgResidentSpider.Instance != null && XgResidentSpider.Instance.BeckonArrived, "the spider is at the icon"); Diagnose(); Capture("skip-spotlight"); });
            if (Now - on > PrologueDirector.SpotlightSeconds + 1.5f && !seen.ContainsKey("timeout-checked"))
            {
                seen["timeout-checked"] = Now;
                Need(!director.SpotlightOn, "the dim lifts by itself after 20 s");
                Need(XgResidentSpider.Instance != null && XgResidentSpider.Instance.Beckoning, "after the timeout the spider stays by the icon");
                Capture("skip-after-timeout");
                router.Open(LingGuangInstallFlow.AppId);
            }
            if (seen.ContainsKey("timeout-checked"))
                After("end", 1.2f, () => { Need(!XgResidentSpider.Instance.Beckoning, "opening the exe sends the spider back"); Finish(null); });
        }
    }

    // ───────────── F10 ─────────────

    static void TickDesk()
    {
        if (phase == 0)
        {
            runtime = UnityEngine.Object.FindAnyObjectByType<ChapterOneRuntime>(); ctl = runtime == null ? null : runtime.GetComponent<XingGuangController>();
            if (ctl == null || ctl.Sim == null || ctl.View == null) return;
            runtime.useDiskSave = false;
            // The fresh save started a prologue; a test-mode runtime freezes its director mid-run, so switch it off.
            var prologueDirector = UnityEngine.Object.FindAnyObjectByType<PrologueDirector>();
            if (prologueDirector != null) prologueDirector.enabled = false;
            var lab = new XgSim(); lab.S.stage = 2; lab.S.handCorrect = 200; lab.S.epochs = 10;
            foreach (var d in new[] { "sense", "spam", "logic", "mnist" }) if (!lab.S.desksOpen.Contains(d)) lab.S.desksOpen.Add(d);
            lab.S.best.Add(new XgBest { dataset = "spam", acc = .82, arch = "perceptron" });
            lab.S.autoLevel = 2; lab.S.autoLabelRevealed = true; lab.S.desk = "spam";
            var house = new ChapterOneSim(); house.S.prologue = 2; house.S.appInstallState = 2; house.S.money = 100000; house.S.labState = JsonUtility.ToJson(lab.S);
            runtime.SetSimulationForTests(house); typeof(XingGuangController).GetMethod("BindSave", Flags).Invoke(ctl, new object[] { house });
            var boot = UnityEngine.Object.FindAnyObjectByType<LingGuangV05.Desktop.OS.BootManager>(FindObjectsInactive.Include);
            if (boot != null) { boot.CancelInvoke(); boot.StopAllCoroutines(); if (boot.bootAnimator != null) boot.bootAnimator.gameObject.SetActive(false); }
            var user = UnityEngine.Object.FindAnyObjectByType<LingGuangV05.Desktop.OS.UserManager>(FindObjectsInactive.Include);
            Need(user != null && user.isActiveAndEnabled, "desktop user manager");
            user.ShowDesktop(); phase = 1; frames = 0; return;
        }
        var routerNow = runtime.GetComponent<ChapterOneDesktopRouter>();
        var binding = routerNow == null ? null : routerNow.Get(XingGuangController.AppId);
        if (phase == 1)
        {
            if (binding == null || binding.TaskbarShortcut == null || !binding.TaskbarShortcut.isActiveAndEnabled) { frames = 0; return; }
            if (++frames < 3) return;
            ctl.Sim.S.desk = "spam"; ctl.Open(); ctl.View.Open("teach"); ctl.View.Refresh(true); phase = 2; frames = 0; return;
        }
        if (phase == 2) { if (++frames < 40) return; phase = 3; }
        var page = ctl.View.Teach;
        if (phase == 3)
        {
            Need(page != null && page.DeskGameVisible, "垃圾短信 opens its desk game (the new background) by default");
            var strip = (RectTransform)typeof(XgLabelPage).GetField("autoStrip", Flags).GetValue(page);
            var text = (TMP_Text)typeof(XgLabelPage).GetField("autoStripText", Flags).GetValue(page);
            var toggle = (XgBtn)typeof(XgLabelPage).GetField("autoStripToggle", Flags).GetValue(page);
            var train = (XgBtn)typeof(XgLabelPage).GetField("autoStripTrain", Flags).GetValue(page);
            Need(strip != null && strip.gameObject.activeInHierarchy, "the 自动答题 strip shows over the desk game");
            var plan = ctl.Sim.AutoDeskPlans(ctl.Host).Find(x => x.desk.id == "spam");
            report.checks.Add("desk plan state " + plan.state + ", strip: " + text.text);
            // The fixture's machine may have no running GPU (NoPower); the strip must then say why, never stay blank.
            Need(plan.Earning ? text.text.Contains(GameText.T("自动中", "Auto on")) : text.text.Length > 0 && !text.text.Contains(GameText.T("自动中", "Auto on")), "the strip states the desk's 自动答题 state: " + text.text);
            Need(toggle.rt.gameObject.activeInHierarchy && toggle.label.text == GameText.T("我来做", "I'll do it"), "「我来做」 is offered while it runs");
            bool trainFeature = ctl.FeatureVisible("train");
            Need(train.rt.gameObject.activeInHierarchy == trainFeature, "去训练 → is on the strip whenever training is (train feature visible: " + trainFeature + ")");
            var deskGame = typeof(XgLabelPage).GetField("deskGame", Flags).GetValue(page);
            var gameRoot = (RectTransform)deskGame.GetType().GetField("root", Flags).GetValue(deskGame);
            var sc = new Vector3[4]; var gc = new Vector3[4]; strip.GetWorldCorners(sc); gameRoot.GetWorldCorners(gc);
            Need(gc[1].y <= sc[0].y + .01f, "the game sits below the strip, not over it");
            Capture("f10-after");
            toggle.button.onClick.Invoke();
            Need(ctl.Sim.AutoPaused, "「我来做」 pauses 自动答题");
            typeof(XgLabelPage).GetMethod("RefreshDeskGameAuto", Flags).Invoke(page, null);
            Need(toggle.label.text == GameText.T("交给它", "Hand it over"), "then the button offers 「交给它」");
            Capture("f10-after-manual");
            toggle.button.onClick.Invoke();
            Need(!ctl.Sim.AutoPaused, "「交给它」 resumes it");
            // The pre-P1 layout for comparison: the game over the right column, no strip.
            var offsetMax = gameRoot.offsetMax;
            strip.localScale = Vector3.zero; gameRoot.offsetMax = Vector2.zero; // the page re-activates the strip each refresh; a zero scale hides it
            phase = 4; frames = 0;
            seen["offset"] = offsetMax.y;
            return;
        }
        if (phase == 4)
        {
            if (++frames < 3) return;
            Capture("f10-before-simulated");
            var deskGame = typeof(XgLabelPage).GetField("deskGame", Flags).GetValue(page);
            var gameRoot = (RectTransform)deskGame.GetType().GetField("root", Flags).GetValue(deskGame);
            gameRoot.offsetMax = new Vector2(0, seen["offset"]);
            ((RectTransform)typeof(XgLabelPage).GetField("autoStrip", Flags).GetValue(page)).localScale = Vector3.one;
            Finish(null);
        }
    }

}
#endif
