#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HongmengOS.Aero2010;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using LingGuangV05.Runtime.Persistence;
using LingGuangV05.XingGuang;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.LLM;
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
public static class LifeResultUiPlayQa
{
    const string Key = "LifeResultUiPlayQa.Root";
    const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static string Root => SessionState.GetString(Key, "");
    static readonly List<string> passed = new List<string>(), failed = new List<string>(), errors = new List<string>(), infrastructure = new List<string>();
    static readonly List<TextResult> labels = new List<TextResult>();
    static readonly Queue<Action> steps = new Queue<Action>();
    static ChapterOneRuntime runtime;
    static XingGuangController ctl;
    static XgLifeEventCard card;
    static IDisposable locale;
    static string language = "zh-CN", output, resultZh, resultEn;
    static double deadline, next, walletBefore, amount, timerBefore;
    static long rngAfter;
    static int historyAfter, returnsAfter;
    static bool fixtureReady, running;
    [Serializable] sealed class Report { public string status, detail; public List<string> passed, failed, errors, infrastructure; public List<TextResult> labels; }
    [Serializable] sealed class TextResult { public string field, language, text; public int characters, visible; public bool truncated, overflow, missingGlyph, contained; }
    static LifeResultUiPlayQa() { EditorApplication.playModeStateChanged += State; }
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start in Edit Mode");
        string dir = Path.GetFullPath(Path.Combine("LocalVerification", "LifeResultUI", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));
        Directory.CreateDirectory(dir); Directory.CreateDirectory(Path.Combine(dir, "Save")); Directory.CreateDirectory(Path.Combine(dir, "DesktopProfile"));
        SessionState.SetString(Key, dir);
        EditorSceneManager.OpenScene("Assets/LingGuangV05/Scenes/Main.unity");
        EditorApplication.EnterPlaymode();
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Isolate()
    {
        if (string.IsNullOrEmpty(Root)) return;
        ChapterOneRuntime.VerificationSaveDirectory = Path.Combine(Root, "Save");
        typeof(Michsky.DreamOS.DreamOSDataManager).GetField("dataPath", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, Path.Combine(Root, "DesktopProfile") + Path.DirectorySeparatorChar);
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
        if (name == "PrologueDirector" || name == "StoryDirector" || name == "StoryDesktopPresenter" || name == "StageOneTutor" || name == "SpiderIntroScene")
        { component.CancelInvoke(); component.StopAllCoroutines(); component.enabled = false; }
    }
    static void State(PlayModeStateChange state)
    {
        if (string.IsNullOrEmpty(Root)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            output = Root; passed.Clear(); failed.Clear(); errors.Clear(); infrastructure.Clear(); labels.Clear(); steps.Clear();
            fixtureReady = false; deadline = EditorApplication.timeSinceStartup + 120;
            Application.logMessageReceived += Log; EditorApplication.update += Boot;
        }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.update -= Boot; EditorApplication.update -= Tick; Application.logMessageReceived -= Log; SceneManager.sceneLoaded -= OnScene;
            bool ok = File.Exists(Path.Combine(Root, "passed.txt")); SessionState.EraseString(Key); ChapterOneRuntime.VerificationSaveDirectory = null;
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }
    }
    static void Log(string message, string stack, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        if (stack.Contains("UnityEditor.Search.SearchDatabase") && !stack.Contains("Assets/")) infrastructure.Add(message + "\n" + stack);
        else errors.Add(message + "\n" + stack);
    }
    static T Field<T>(object value, string name) => (T)value.GetType().GetField(name, Flags).GetValue(value);
    static void Check(bool ok, string message) { (ok ? passed : failed).Add(message); }
    static void Require(bool ok, string message) { Check(ok, message); if (!ok) throw new InvalidOperationException(message); }
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
                house.S.gpuModels.Clear(); house.S.gpuModels.Add("gtx980ti"); house.S.gpuCount = house.S.gpuModels.Count; house.S.labState = JsonUtility.ToJson(lab.S);
                Rebind(house);
                var boot = UnityEngine.Object.FindAnyObjectByType<Michsky.DreamOS.BootManager>(FindObjectsInactive.Include);
                if (boot != null) { boot.CancelInvoke(); boot.StopAllCoroutines(); if (boot.bootAnimator != null) boot.bootAnimator.gameObject.SetActive(false); }
                var user = UnityEngine.Object.FindAnyObjectByType<Michsky.DreamOS.UserManager>(FindObjectsInactive.Include);
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
        language = "zh-CN"; locale = GameText.Bind(() => language); GameText.PublishChanged(); ctl.Sim.English = false;
        ctl.Open(); ctl.View.Open("tree"); card = runtime.GetComponent<XgLifeEventCard>();
        Require(card != null, "Real Main XgLifeEventCard exists");
        steps.Enqueue(() =>
        {
            Require(!runtime.TestMode && ctl.Host.Money == 100000, "Real host wallet available with frozen clocks");
            walletBefore = ctl.Host.Money;
            Require(ctl.Sim.OpenLifeEventNow("zhou_loan", ctl.Host, true), "Real native loan event opens");
            amount = ctl.Sim.S.lifeArg; timerBefore = ctl.Sim.LifeEventNext;
            Require(amount == 5000, "Real event commits deterministic 5000 principal");
        });
        steps.Enqueue(() => { CheckPresentation(false); Capture("choice-zh.png"); Click(Field<XgBtn[]>(card, "buttons")[0], "Loan choice"); });
        steps.Enqueue(() =>
        {
            Require(!ctl.Sim.LifeEventOpen && ctl.Sim.LifeEventResultPending, "Choice transitions to pending result");
            Require(ctl.Host.Money == walletBefore - amount, "Choice charges wallet exactly once");
            historyAfter = ctl.Sim.LifeHistory.Count; returnsAfter = ctl.Sim.S.lifeReturns.Count; rngAfter = ctl.Sim.S.lifeRng;
            Require(historyAfter == 1 && returnsAfter == 1, "One history record and one future repayment");
            resultZh = ctl.Sim.S.lifeResultText; resultEn = ctl.Sim.S.lifeResultTextEn;
            Require(!string.IsNullOrEmpty(resultZh) && !string.IsNullOrEmpty(resultEn), "Both committed result translations saved");
            CheckPresentation(true); Capture("result-zh.png");
            Check(ctl.Sim.LifeEventNext == timerBefore, "Showing result does not start a fresh countdown");
            Check(!ctl.Sim.AnswerLifeEvent(0, ctl.Host), "Repeated answer is rejected after result");
            AssertCommitted("Repeated answer");
            var disk = ReadDisk(); var lab = JsonUtility.FromJson<XgState>(disk.labState);
            Require(disk.money == walletBefore - amount && lab.lifeResultId == "zhou_loan" && lab.lifeReturns.Count == 1 && lab.lifeHistory.Count == 1, "Choice callback atomically persisted wallet and result snapshot");
            // Use the actual atomic-store payload for a JSON reload. The card rebinds on its next Update.
            Rebind(new ChapterOneSim(disk)); ctl.Open(); ctl.View.Open("tree");
            // Old card confirm callback must not acknowledge the newly rebound sim before its own Update.
            Field<XgBtn>(card, "confirm").rt.GetComponent<Button>().onClick.Invoke();
            Check(ctl.Sim.LifeEventResultPending, "Stale card confirm cannot acknowledge newly rebound save");
        });
        steps.Enqueue(() =>
        {
            AssertCommitted("Disk JSON rebind"); CheckPresentation(true);
            Require(Field<TMP_Text>(card, "body").text == resultZh, "Reload renders saved Chinese result verbatim");
            language = GameText.English; GameText.PublishChanged(); ctl.Sim.English = true;
        });
        steps.Enqueue(() =>
        {
            AssertCommitted("English refresh"); CheckPresentation(true);
            Require(Field<TMP_Text>(card, "body").text == resultEn, "English renders saved result without applying effects");
            Capture("result-en.png");
            // Freeze a non-default sentinel to distinguish acknowledgement from an unchanged initial interval.
            ctl.Sim.S.lifeNext = 73; Click(Field<XgBtn>(card, "confirm"), "Result confirmation");
            Require(!ctl.Sim.LifeEventResultPending && !ctl.Sim.LifeEventOpen, "Confirmation closes result");
            Require(ctl.Sim.LifeEventNext == 480, "Confirmation begins full 480 active seconds");
            var disk = ReadDisk(); var lab = JsonUtility.FromJson<XgState>(disk.labState);
            Require(lab.lifeResultId == "" && lab.lifeNext == 480 && disk.money == walletBefore - amount, "Confirmation persisted cleared result and full interval");
            ctl.Sim.S.lifeNext = 111; Field<XgBtn>(card, "confirm").rt.GetComponent<Button>().onClick.Invoke();
            Check(ctl.Sim.LifeEventNext == 111, "Duplicate hidden confirm does not restart countdown");
        });
        steps.Enqueue(() =>
        {
            Check(!Field<RectTransform>(card, "overlay").gameObject.activeSelf, "Result overlay remains closed after confirmation");
            Check(ctl.Host.Money == walletBefore - amount && ctl.Sim.LifeHistory.Count == historyAfter && ctl.Sim.S.lifeReturns.Count == returnsAfter, "Confirmation never repeats consequences");
            Require(ctl.Sim.OpenLifeEventNow("zhou_loan", ctl.Host, true), "Second forced event opens for stale choice regression");
        });
        steps.Enqueue(() =>
        {
            Require(ctl.Sim.LifeEventOpen, "Old card has a displayed choice");
            // Replace household and lab, then invoke the previous callback before card.Update can rebind.
            var replacement = new ChapterOneSim(); replacement.S.prologue = 2; replacement.S.appInstallState = 2; replacement.S.money = 77777;
            var lab = new XgSim(); lab.S.stage = 1; lab.S.unlocked.Add("train.curve"); replacement.S.labState = JsonUtility.ToJson(lab.S);
            Rebind(replacement); Field<XgBtn[]>(card, "buttons")[0].rt.GetComponent<Button>().onClick.Invoke();
            Check(ctl.Host.Money == 77777 && ctl.Sim.LifeHistory.Count == 0 && !ctl.Sim.LifeEventResultPending, "Stale choice callback cannot charge replacement household");
        });
        running = true; next = EditorApplication.timeSinceStartup + 1.5; deadline = next + 90; EditorApplication.update += Tick;
    }
    static GameState ReadDisk()
    {
        var result = new AtomicSaveStore(Path.Combine(Root, "Save")).Load();
        Require(result.Success && !result.Recovered, "Primary isolated disk save passes checksum");
        return JsonUtility.FromJson<GameState>(result.Payload);
    }
    static void AssertCommitted(string phase)
    {
        Check(ctl.Host.Money == walletBefore - amount && ctl.Sim.LifeHistory.Count == historyAfter && ctl.Sim.S.lifeReturns.Count == returnsAfter && ctl.Sim.S.lifeRng == rngAfter, phase + " preserves wallet/history/repayment/RNG");
        Check(ctl.Sim.S.lifeResultText == resultZh && ctl.Sim.S.lifeResultTextEn == resultEn && ctl.Sim.LifeEventResultPending, phase + " preserves committed bilingual result");
    }
    static void Click(XgBtn item, string name)
    {
        Canvas.ForceUpdateCanvases(); var button = item.rt.GetComponent<Button>();
        Require(button != null && button.IsActive() && button.IsInteractable(), name + " button active/interactable");
        var bridge = UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>();
        var surface = bridge.inputSurface;
        var source = bridge.sourceCamera.WorldToScreenPoint(item.rt.TransformPoint(item.rt.rect.center));
        var display = surface.displayRect; var area = display.rect; var texture = bridge.sourceCamera.targetTexture;
        var camera = (Camera)typeof(AeroLcdInputSurface).GetMethod("DisplayEventCamera", Flags).Invoke(surface, null);
        var screen = RectTransformUtility.WorldToScreenPoint(camera, display.TransformPoint(new Vector3(
            area.xMin + source.x / texture.width * area.width, area.yMin + source.y / texture.height * area.height, 0)));
        var pointer = new PointerEventData(EventSystem.current) { position = screen, button = PointerEventData.InputButton.Left };
        Require(surface.TryMap(screen, out var mapped) && Vector2.Distance(mapped, source) < 1, name + " maps through actual LCD geometry");
        Require(surface.inputEnabled && surface.isActiveAndEnabled, name + " LCD input gate open");
        surface.OnPointerEnter(pointer);
        var routed = Field<PointerEventData>(surface, "hoverData");
        var hit = routed?.pointerCurrentRaycast.gameObject;
        var receiver = hit != null ? ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit) : null;
        File.AppendAllText(Path.Combine(output, "pointer.txt"), name + " hit=" + hit + " receiver=" + receiver + " expected=" + button.gameObject + "\n");
        Require(receiver == button.gameObject, name + " is actual LCD click receiver");
        surface.OnPointerDown(pointer); surface.OnPointerUp(pointer);
    }
    static void CheckPresentation(bool result)
    {
        Require(Field<RectTransform>(card, "overlay").gameObject.activeInHierarchy, "Life card visible " + language);
        Check(Field<XgBtn>(card, "confirm").rt.gameObject.activeSelf == result, "Confirmation visibility matches result mode " + language);
        foreach (var choice in Field<XgBtn[]>(card, "buttons")) Check(choice.rt.gameObject.activeSelf == !result, "Choice visibility matches mode " + language);
        Inspect(Field<TMP_Text>(card, "who"), "who"); Inspect(Field<TMP_Text>(card, "title"), "title"); Inspect(Field<TMP_Text>(card, "body"), "body");
        if (result) Inspect(Field<XgBtn>(card, "confirm").label, "confirm");
        else foreach (var choice in Field<XgBtn[]>(card, "buttons")) Inspect(choice.label, "choice");
    }
    static void Inspect(TMP_Text label, string name)
    {
        label.ForceMeshUpdate(true, true); var info = label.textInfo; var rect = label.rectTransform.rect;
        var row = new TextResult { field = name, language = language, text = label.text, characters = info.characterCount, truncated = label.isTextTruncated, overflow = label.isTextOverflowing, contained = true };
        for (int i = 0; i < info.characterCount; i++)
        {
            var c = info.characterInfo[i];
            if (!char.IsWhiteSpace(c.character) && (c.textElement == null || c.textElement.unicode != c.character)) row.missingGlyph = true;
            if (!c.isVisible) continue; row.visible++;
            if (c.bottomLeft.x < rect.xMin - .5f || c.topRight.x > rect.xMax + .5f || c.bottomLeft.y < rect.yMin - .5f || c.topRight.y > rect.yMax + .5f) row.contained = false;
        }
        labels.Add(row);
        Check(row.visible > 0 && !row.truncated && !row.overflow && !row.missingGlyph && row.contained, "Rendered complete glyphs inside " + name + " " + language);
    }
    static void Capture(string name)
    {
        var camera = Field<RectTransform>(card, "overlay").GetComponentInParent<Canvas>().rootCanvas.worldCamera;
        var target = camera != null ? camera.targetTexture : null; Require(target != null, "Actual Main LCD render target available");
        var previous = RenderTexture.active; var texture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        try { RenderTexture.active = target; texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); texture.Apply(); File.WriteAllBytes(Path.Combine(output, name), texture.EncodeToPNG()); }
        finally { RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(texture); }
    }
    static void Tick()
    {
        if (!running) return;
        try
        {
            if (errors.Count > 0) throw new Exception(errors[0]); if (!EditorApplication.isPlaying) throw new Exception("Play ended early");
            double now = EditorApplication.timeSinceStartup; if (now > deadline) throw new TimeoutException(); if (now < next) return; next = now + 1.5;
            if (steps.Count == 0) { Finish(failed.Count == 0 ? "passed" : "failed", "Actual Main UI, TMP meshes, raycast targets, pointer-click handlers and isolated atomic disk save tested. Physical mouse, OS minimize and full process restart are not covered."); return; }
            steps.Dequeue()();
        }
        catch (Exception error) { Finish("failed", error.ToString()); }
    }
    static void Finish(string status, string detail)
    {
        running = false; EditorApplication.update -= Boot; EditorApplication.update -= Tick; locale?.Dispose(); locale = null;
        File.WriteAllText(Path.Combine(output, "result.json"), JsonUtility.ToJson(new Report { status = status, detail = detail, passed = passed, failed = failed, errors = errors, infrastructure = infrastructure, labels = labels }, true));
        if (status == "passed") File.WriteAllText(Path.Combine(output, "passed.txt"), detail);
        Application.logMessageReceived -= Log; EditorApplication.ExitPlaymode();
    }
}
#endif
