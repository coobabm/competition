#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Dedicated batch: -executeMethod TechPanMotionChecks.RunAndExit (without -quit).
// Uses an unsaved empty Canvas only; no Main scene, game runtime, PlayerPrefs or save files.
[InitializeOnLoad]
public static class TechPanMotionChecks
{
    const string DirectoryKey = "TechPanMotionChecks.Directory";
    const string OldSaveKey = "TechPanMotionChecks.OldSave";
    static string DirectoryPath => SessionState.GetString(DirectoryKey, "");
    [Serializable] sealed class Report
    {
        public string status = "running", failure = "", unityVersion;
        public List<string> checks = new List<string>();
        public string scope = "Deterministic XgPan motion and pointer conversion on empty overlay/camera Canvases. Not real desktop LCD or visual acceptance.";
    }
    static readonly MethodInfo Advance = typeof(XgPan).GetMethod("AdvanceMotion", BindingFlags.Instance | BindingFlags.NonPublic);
    static Report report;
    static GameObject canvasObject, eventObject, cameraObject;
    static XgPan pan;
    static RectTransform viewport, content;
    static EventSystem events;
    static GraphicRaycaster raycaster;
    static int starts, finishes;

    static TechPanMotionChecks() { EditorApplication.playModeStateChanged += State; }

    public static void RunAndExit()
    {
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Use a dedicated batch Editor in Edit Mode.");
        string directory = Path.Combine(Path.GetTempPath(), "LingGuang-TechPan-QA-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        SessionState.SetString(DirectoryKey, directory);
        SessionState.SetString(OldSaveKey, ChapterOneRuntime.VerificationSaveDirectory ?? "");
        ChapterOneRuntime.VerificationSaveDirectory = Path.Combine(directory, "UnusedIsolatedSave");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void IsolateBeforeLoad()
    {
        if (!string.IsNullOrEmpty(DirectoryPath))
            ChapterOneRuntime.VerificationSaveDirectory = Path.Combine(DirectoryPath, "UnusedIsolatedSave");
    }

    static void State(PlayModeStateChange state)
    {
        if (string.IsNullOrEmpty(DirectoryPath)) return;
        if (state == PlayModeStateChange.EnteredPlayMode) RunChecksInPlayMode();
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            string directory = DirectoryPath;
            bool passed = File.Exists(Path.Combine(directory, "passed.txt"));
            string previous = SessionState.GetString(OldSaveKey, "");
            ChapterOneRuntime.VerificationSaveDirectory = previous.Length == 0 ? null : previous;
            SessionState.EraseString(DirectoryKey); SessionState.EraseString(OldSaveKey);
            Debug.Log("TechPanMotionChecks " + (passed ? "PASS" : "FAIL") + ": " + Path.Combine(directory, "report.json"));
            EditorApplication.Exit(passed ? 0 : 1);
        }
    }

    static void RunChecksInPlayMode()
    {
        string directory = DirectoryPath;
        report = new Report { unityVersion = Application.unityVersion };
        bool passed = false;
        try
        {
            Check(UnityEngine.Object.FindAnyObjectByType<ChapterOneRuntime>(FindObjectsInactive.Include) == null,
                "empty-scene fixture contains no game runtime");
            Check(Advance != null, "deterministic motion entry exists");
            Setup(false); RunChecks(); DestroyFixture();
            Setup(true); AnchorChecks(); DestroyFixture();
            passed = true; report.status = "passed";
        }
        catch (Exception ex) { report.status = "failed"; report.failure = ex.ToString(); }
        finally
        {
            DestroyFixture();
            File.WriteAllText(Path.Combine(directory, "report.json"), JsonUtility.ToJson(report, true));
        }
        if (passed) File.WriteAllText(Path.Combine(directory, "passed.txt"), "Pan empty-Canvas checks passed");
        EditorApplication.ExitPlaymode();
    }

    static void Setup(bool cameraMode)
    {
        canvasObject = new GameObject("Isolated pan canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = cameraMode ? RenderMode.ScreenSpaceCamera : RenderMode.ScreenSpaceOverlay;
        canvas.scaleFactor = 1.25f;
        if (cameraMode)
        {
            cameraObject = new GameObject("Isolated event camera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.transform.position = new Vector3(0, 0, -10);
            camera.orthographic = true; camera.orthographicSize = 5;
            canvas.worldCamera = camera; canvas.planeDistance = 5;
        }
        raycaster = canvasObject.GetComponent<GraphicRaycaster>();
        viewport = Rect("Viewport", canvasObject.transform, new Vector2(800, 600));
        content = Rect("Content", viewport, new Vector2(2400, 1600));
        content.anchorMin = content.anchorMax = new Vector2(0, 1); content.pivot = new Vector2(0, 1);
        pan = viewport.gameObject.AddComponent<XgPan>(); pan.content = content; pan.viewport = viewport;
        // Production tech-page instance owns its 50–150% range; the shared default is unchanged.
        Check(Near(pan.minZoom, .5f) && Near(pan.maxZoom, 1.6f), "shared default zoom range remains 50–160%");
        pan.minZoom = .5f; pan.maxZoom = 1.5f;
        pan.MotionStarted = () => starts++;
        pan.MotionFinished = () => { Check(!pan.Moving, "finish callback sees settled Moving=false"); finishes++; };
        eventObject = new GameObject("Isolated EventSystem", typeof(EventSystem));
        events = eventObject.GetComponent<EventSystem>();
        Canvas.ForceUpdateCanvases();
        Reset();
    }

    static RectTransform Rect(string name, Transform parent, Vector2 size)
    {
        var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent, false); rt.sizeDelta = size; return rt;
    }

    static void Reset()
    {
        pan.CancelMotion(); pan.ReduceMotion = false;
        viewport.sizeDelta = new Vector2(800, 600); content.sizeDelta = new Vector2(2400, 1600);
        pan.SetView(1, new Vector2(-300, 200)); starts = finishes = 0;
    }

    static PointerEventData Pointer(Vector2 local, float wheel = 0)
    {
        var hit = new RaycastResult { module = raycaster, gameObject = viewport.gameObject };
        return new PointerEventData(events)
        {
            position = RectTransformUtility.WorldToScreenPoint(raycaster.eventCamera, viewport.TransformPoint(local)),
            scrollDelta = new Vector2(0, wheel), pointerCurrentRaycast = hit, pointerPressRaycast = hit
        };
    }

    static void RunChecks()
    {
        Check(!pan.ReduceMotion, "reduced motion defaults off");
        AnchorChecks();
        Reset();
        pan.GlideTo(new Vector2(-10000, 10000));
        Check(pan.Moving && starts == 1 && finishes == 0, "normal boundary glide starts once");
        Step();
        Check(pan.Moving, "normal glide remains animated after first step");
        Drain();
        Check(Close(content.anchoredPosition, new Vector2(40, -40)), "upper/right unreachable target settles at boundary");
        Check(starts == 1 && finishes == 1, "boundary navigation finishes exactly once");
        Step(10); Check(finishes == 1, "idle updates do not repeat boundary finish");

        Reset(); pan.GlideTo(new Vector2(10000, -10000)); Drain();
        Check(Close(content.anchoredPosition, new Vector2(-1640, 1040)), "lower/left unreachable target settles at boundary");
        Reset(); pan.FitColumn(100000); Drain();
        Check(!pan.Moving && finishes == 1, "FitColumn outside bounds terminates");
        Reset(); pan.GlideTo(new Vector2(10000, -10000)); Step();
        viewport.sizeDelta = new Vector2(1100, 700); Drain();
        Check(Close(content.anchoredPosition, new Vector2(-1340, 940)), "resizing during navigation uses current bounds");

        Reset(); pan.ReduceMotion = true;
        pan.GlideTo(new Vector2(10000, -10000));
        Check(!pan.Moving && Close(content.anchoredPosition, new Vector2(-1640, 1040)), "reduced navigation settles synchronously");
        Check(starts == 1 && finishes == 1, "reduced navigation has one start/finish pair");
        pan.FitColumn(-10000);
        Check(!pan.Moving && Near(content.anchoredPosition.x, 40), "reduced FitColumn settles synchronously");

        Reset(); pan.ReduceMotion = true;
        var e = Pointer(Vector2.zero); pan.OnBeginDrag(e);
        e = Pointer(new Vector2(20, 12)); pan.OnDrag(e);
        Check(pan.Dragging && Close(content.anchoredPosition, new Vector2(-280, 212)), "reduced drag follows direct input");
        pan.OnEndDrag(e); var stopped = content.anchoredPosition; Step(10);
        Check(!pan.Moving && Close(content.anchoredPosition, stopped), "reduced release has no inertia");
        Check(starts == 1 && finishes == 1, "reduced drag ends once");

        Reset(); e = Pointer(Vector2.zero); pan.OnBeginDrag(e);
        pan.OnDrag(Pointer(new Vector2(30, 20))); pan.OnEndDrag(e);
        Check(pan.Moving, "default release retains inertia");
        stopped = content.anchoredPosition; Step();
        Check(!Close(content.anchoredPosition, stopped), "default inertia advances content");
        stopped = content.anchoredPosition; pan.ReduceMotion = true; Step(10);
        Check(!pan.Moving && Close(content.anchoredPosition, stopped), "enabling reduced during inertia stops without drifting");
        Check(finishes == 1, "inertia cancellation ends once");

        Reset(); pan.OnBeginDrag(Pointer(Vector2.zero)); pan.OnDrag(Pointer(new Vector2(20, 10)));
        pan.ReduceMotion = true;
        Check(pan.Dragging && pan.Moving && finishes == 0, "mid-drag reduce toggle preserves direct drag");
        pan.OnDrag(Pointer(new Vector2(40, 20)));
        Check(Close(content.anchoredPosition, new Vector2(-260, 220)), "drag continues after reduce toggle");
        pan.OnEndDrag(Pointer(new Vector2(40, 20)));
        Check(!pan.Moving && finishes == 1, "mid-drag toggle removes release inertia");

        Reset(); pan.GlideTo(new Vector2(10000, -10000)); Step(); pan.ReduceMotion = true;
        Check(!pan.Moving && Close(content.anchoredPosition, new Vector2(-1640, 1040)), "mid-glide reduce toggle completes destination");
        Check(finishes == 1, "mid-glide reduce toggle finishes once");

        Reset(); pan.GlideTo(new Vector2(10000, -10000)); pan.ZoomBy(1.2f); Step();
        var scale = content.localScale.x; stopped = content.anchoredPosition;
        pan.CancelMotion(); pan.CancelMotion(); Step(10);
        Check(!pan.Moving && Close(content.anchoredPosition, stopped) && Near(content.localScale.x, scale), "cancel keeps current view and discards all destinations");
        Check(finishes == 1, "repeated cancellation finishes once");

        Reset(); pan.GlideTo(new Vector2(10000, -10000)); Step(); stopped = content.anchoredPosition;
        viewport.gameObject.SetActive(false);
        Check(!pan.Moving && finishes == 1, "OnDisable cancels hidden motion");
        viewport.gameObject.SetActive(true); Step(10);
        Check(!pan.Moving && Close(content.anchoredPosition, stopped) && finishes == 1, "reopen never resumes stale navigation");

        Reset(); pan.OnBeginDrag(Pointer(Vector2.zero)); pan.CancelMotion();
        Check(!pan.Dragging && !pan.Moving && finishes == 1, "cancel also clears active drag");
        Reset(); pan.GlideTo(new Vector2(10000, -10000)); pan.SetView(1, new Vector2(-300, 200));
        Check(!pan.Moving && finishes == 1, "explicit reset ends pending navigation once");

        Reset(); pan.ReduceMotion = true;
        for (int i = 0; i < 30; i++) pan.OnScroll(Pointer(Vector2.zero, 1));
        Check(Near(content.localScale.x, 1.5f) && !pan.Moving, "instance maximum remains 150%");
        for (int i = 0; i < 30; i++) pan.OnScroll(Pointer(Vector2.zero, -1));
        Check(Near(content.localScale.x, .5f) && !pan.Moving, "instance minimum remains 50%");
        Check(Near(pan.minZoom, .5f) && Near(pan.maxZoom, 1.5f), "motion mode never rewrites instance limits");
    }

    static void AnchorChecks()
    {
        Reset();
        var local = new Vector2(-100, 80);
        var e = Pointer(local, 1);
        Check(RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, e.position, e.pressEventCamera, out var actual)
            && Close(actual, local), "pointer converts through actual canvas/event camera");
        var relative = actual - new Vector2(viewport.rect.xMin, viewport.rect.yMax);
        var underPointer = (relative - content.anchoredPosition) / content.localScale.x;
        pan.OnScroll(e);
        Check(pan.Moving && Near(content.localScale.x, 1), "default wheel queues smooth zoom");
        Drain();
        Check(Near(content.localScale.x, 1.1f), "wheel notch changes scale by 10%");
        Check(Close((relative - content.anchoredPosition) / content.localScale.x, underPointer), "smooth wheel preserves pointer anchor away from clamped edges");
        Check(starts == 1 && finishes == 1, "smooth wheel produces one motion pair");

        Reset(); pan.ReduceMotion = true;
        underPointer = (relative - content.anchoredPosition) / content.localScale.x;
        pan.OnScroll(e);
        Check(!pan.Moving && Near(content.localScale.x, 1.1f), "reduced wheel zoom is immediate");
        Check(Close((relative - content.anchoredPosition) / content.localScale.x, underPointer), "reduced wheel retains pointer anchor");
        Check(starts == 1 && finishes == 1, "reduced wheel produces one motion pair");

        Reset(); underPointer = (relative - content.anchoredPosition) / content.localScale.x;
        pan.OnScroll(e); Step(); pan.ReduceMotion = true;
        Check(!pan.Moving && Near(content.localScale.x, 1.1f), "mid-zoom reduce toggle finishes pending zoom");
        Check(Close((relative - content.anchoredPosition) / content.localScale.x, underPointer), "mid-zoom toggle retains pointer anchor");
        Check(finishes == 1, "mid-zoom toggle finishes once");

        Reset(); pan.ReduceMotion = true;
        relative = viewport.rect.center - new Vector2(viewport.rect.xMin, viewport.rect.yMax);
        underPointer = (relative - content.anchoredPosition) / content.localScale.x;
        pan.ZoomBy(1.2f);
        Check(!pan.Moving && Near(content.localScale.x, 1.2f), "reduced zoom button is immediate");
        Check(Close((relative - content.anchoredPosition) / content.localScale.x, underPointer), "button preserves viewport-centre anchor");
    }

    static void Step(int count = 1) { for (int i = 0; i < count; i++) Advance.Invoke(pan, new object[] { .05f }); }
    static void Drain()
    {
        int frames = 0; while (pan.Moving && frames++ < 240) Step();
        Check(!pan.Moving, "motion terminates within 12 simulated seconds");
    }
    static bool Near(float a, float b) => Mathf.Abs(a - b) < .002f;
    static bool Close(Vector2 a, Vector2 b) => (a - b).sqrMagnitude < .01f;
    static void Check(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException(label);
        report.checks.Add(label);
    }
    static void DestroyFixture()
    {
        if (pan != null) { pan.MotionStarted = null; pan.MotionFinished = null; }
        if (canvasObject != null) UnityEngine.Object.DestroyImmediate(canvasObject);
        if (eventObject != null) UnityEngine.Object.DestroyImmediate(eventObject);
        if (cameraObject != null) UnityEngine.Object.DestroyImmediate(cameraObject);
        canvasObject = eventObject = cameraObject = null; pan = null;
    }
}
#endif
