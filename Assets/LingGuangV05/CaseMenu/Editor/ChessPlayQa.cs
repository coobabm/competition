#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HongmengOS.Aero2010;
using LingGuangV05.Core.Games;
using LingGuangV05.Desktop.Games;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.Runtime.Story;
using LingGuangV05.XingGuang;
using LingGuangV05.Desktop.OS;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LingGuangV05.CaseMenu.Editor
{
    /// <summary>Explicit live desktop QA. Never enters Play, loads scenes, or writes a player save.</summary>
    public static class ChessPlayQa
    {
        [Serializable] public sealed class Check { public string name; public bool passed; public string detail; }
        [Serializable] public sealed class Report
        {
            public string scope = "Editor Play; physical LCD-coordinate PointerEventData through the native LCD relay. Not hardware input, Player-build, or audible-listening certification. FEN fixtures are temporary; runtime/lab state and flags are restored.";
            public bool complete;
            public string phase, outputDirectory;
            public int passed, failed, initialFaces, spriteTypes;
            public float elapsedSeconds, maximumCaptureFlash;
            public List<Check> checks = new List<Check>();
            public List<string> errors = new List<string>();
            public List<string> screenshots = new List<string>();
        }

        public static bool Running { get; private set; }
        public static Report Result { get; private set; }
        public static string OutputDirectory { get; private set; }
        static readonly Stack<IEnumerator> routines = new Stack<IEnumerator>();
        static readonly List<(Behaviour component, bool enabled)> paused = new List<(Behaviour, bool)>();
        static readonly Dictionary<string, object> panelSnapshot = new Dictionary<string, object>();
        static readonly string[] savedFields = { "game", "aiAt", "animating", "hiddenSquare", "counted", "wins", "losses", "draws", "selFile", "selRank", "checkedSquare", "ignoreClickUntil", "materialBefore", "playerCaptured", "playerChecked", "sampleNote", "commentary" };
        static ChapterOneRuntime runtime;
        static XingGuangController labController;
        static XgSim lab;
        static string coreSnapshot, labSnapshot, previousPage;
        static List<string> savedLog;
        static List<ChessMove> savedTargets;
        static bool previousDiskSave, previousTick, previousWindowOn, motionExisted, cleaningUp, capturedPanel;
        static int previousMotion, previousFrame;
        static float previousTimeScale;
        static double startedAt;
        static CaseMenuController menu;
        static GameHubView hub;
        static WindowManager window;
        static ChessPanel panel;
        static ChessFeedback feedback;
        static RectTransform board;
        static AeroLcdInputSurface surface;
        static ChessGame Game => Read<ChessGame>(panel, "game");

        public static void Begin()
        {
            if (Running) throw new InvalidOperationException("Chess QA is already running.");
            menu = Object.FindAnyObjectByType<CaseMenuController>();
            runtime = Object.FindAnyObjectByType<ChapterOneRuntime>();
            surface = menu != null ? menu.GetComponent<CaseMenuMainDisplay>()?.InputSurface : null;
            if (!Application.isPlaying || menu == null || !menu.HasEnteredGame || runtime?.Sim == null
                || surface == null || !surface.inputEnabled || EventSystem.current == null)
                throw new InvalidOperationException("Begin requires entered Main, the retained case menu, initialized runtime, and enabled native LCD input.");

            OutputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Documentation/Chess/QA/", DateTime.Now.ToString("yyyyMMdd-HHmmss-fff")));
            Directory.CreateDirectory(OutputDirectory);
            Result = new Report { phase = "starting", outputDirectory = OutputDirectory };
            previousDiskSave = runtime.useDiskSave; previousTick = runtime.tickEnabled;
            runtime.useDiskSave = false; runtime.tickEnabled = false;
            coreSnapshot = JsonUtility.ToJson(runtime.Sim.S);
            labController = Object.FindAnyObjectByType<XingGuangController>();
            lab = labController != null ? labController.Sim : null;
            labSnapshot = lab != null ? JsonUtility.ToJson(lab.S) : null;
            paused.Clear(); Pause(labController); Pause(Object.FindAnyObjectByType<PrologueDirector>()); Pause(Object.FindAnyObjectByType<StoryDirector>());
            motionExisted = PlayerPrefs.HasKey(StoryDesktopPresenter.ReduceMotionPref);
            previousMotion = PlayerPrefs.GetInt(StoryDesktopPresenter.ReduceMotionPref, 0);
            PlayerPrefs.SetInt(StoryDesktopPresenter.ReduceMotionPref, 0);
            previousTimeScale = Time.timeScale;
            hub = null; window = null; panel = null; feedback = null; board = null;
            capturedPanel = false; panelSnapshot.Clear(); savedLog = null; savedTargets = null;
            startedAt = EditorApplication.timeSinceStartup; previousFrame = -1; cleaningUp = false; Running = true;
            routines.Clear(); routines.Push(Run());
            Application.logMessageReceived += OnLog;
            EditorApplication.update += Advance;
            EditorApplication.playModeStateChanged += OnMode;
            AssemblyReloadEvents.beforeAssemblyReload += OnReload;
            LingGuangV05.Editor.ChapterOneQaReloadGuard.LockForPlayQa(65);
            var gameView = Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(w => w.GetType().FullName == "UnityEditor.GameView");
            if (gameView != null) gameView.Focus();
            Save();
        }

        public static void Cancel()
        {
            if (!Running) return;
            Record("Operator cancelled QA", false, Result.phase);
            Finish(false);
        }

        static IEnumerator Run()
        {
            Phase("open the actual Game Hub");
            window = Object.FindObjectsByType<WindowManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(w => w.name == GameHubView.NativeWindow);
            Require("Native Game Hub window exists", window != null);
            previousWindowOn = window.isOn;
            window.OpenWindow();
            yield return Until(() => window.isOn && (window.GetComponent<CanvasGroup>() == null || window.GetComponent<CanvasGroup>().alpha > .95f), 5, "Game Hub finishes opening");
            hub = window.GetComponentInChildren<GameHubView>(true);
            Require("Installed GameHubView exists", hub != null);
            previousPage = Read<string>(hub, "showing");
            Call(hub, "Show", "chess");
            yield return Frames(3);
            panel = hub.GetComponent<ChessPanel>();
            Require("Chess board has been built and is visible", panel != null && (bool)Call(panel, "BoardVisible"));
            board = Read<RectTransform>(panel, "board"); feedback = Read<ChessFeedback>(panel, "feedback");
            Require("Presentation feedback is installed", board != null && feedback != null);
            Require("QA starts from an idle board without an in-flight user move", !feedback.Busy
                && !Read<bool>(panel, "animating") && !Read<bool>(panel, "dragging") && Read<float>(panel, "aiAt") < 0);
            foreach (string field in savedFields) panelSnapshot[field] = Field(panel.GetType(), field).GetValue(panel);
            savedLog = new List<string>(Read<List<string>>(panel, "log"));
            savedTargets = new List<ChessMove>(Read<List<ChessMove>>(panel, "targets"));
            capturedPanel = true;

            SetFen(ChessGame.StartFen);
            yield return Frames(2);
            var pieces = Read<ChessPieceView[,]>(panel, "pieces");
            var art = Read<ChessSprites>(panel, "art");
            Result.initialFaces = pieces.Cast<ChessPieceView>().Count(p => p.Face != null && p.Face.enabled && p.Face.sprite != null);
            Result.spriteTypes = "PRNBQKprnbqk".Select(art.Get).Where(s => s != null).Distinct().Count();
            Require("Initial board renders exactly 32 nonempty sprite faces", Result.initialFaces == 32, "faces=" + Result.initialFaces);
            Require("All 12 original sprite types and flash resources load", Result.spriteTypes == 12 && art.Ready
                && Resources.Load<Texture2D>("LingGuangV05/Chess/ChessPieces") != null
                && Resources.Load<Shader>("LingGuangV05/Chess/ChessPieceFlash") != null, "types=" + Result.spriteTypes);
            Capture("start"); yield return Frames(2);

            Phase("real LCD selection and illegal destination");
            Click("e2"); yield return Frames(2);
            var dots = Read<Image[,]>(panel, "dots");
            Require("Real LCD click on e2 exposes only e3 and e4 dots", dots.Cast<Image>().Count(i => i.color.a > .01f) == 2
                && dots[4,2].color.a > .01f && dots[4,3].color.a > .01f);
            Capture("e2dots"); yield return Frames(2);
            string before = Game.ToFen(); Click("e5"); yield return Frames(2);
            Require("Illegal e2 to e5 click does not move a piece", Game.ToFen() == before && !Read<bool>(panel, "animating"));

            Phase("native LCD legal drag and cancellation routes");
            SetFen(ChessGame.StartFen);
            var drag = BeginDrag("e2", "e4"); yield return Frames(1);
            Require("Native LCD threshold dispatch starts a piece drag", Read<bool>(panel, "dragging") && Read<Image>(panel, "dragGhost").gameObject.activeSelf);
            Capture("drag"); yield return Frames(1);
            drag.position = PhysicalPoint(Square("e4")); surface.OnDrag(drag); surface.OnPointerUp(drag);
            Require("Native LCD drop commits e2 to e4 once", Game.Plies == 1 && Game[4,3] == 'P' && Game[4,1] == '.');
            yield return Settled();
            Require("Quiet move restores the destination sprite", pieces[4,3].Face.enabled && !Read<bool>(panel, "dragging"));

            foreach (string cancellation in new[] { "outside", "focus", "tab", "end-drag-without-drop" })
            {
                SetFen(ChessGame.StartFen); before = Game.ToFen();
                drag = BeginDrag("e2", "e4"); yield return Frames(1);
                Require("Drag began before " + cancellation + " cancellation", Read<bool>(panel, "dragging"));
                if (cancellation == "outside")
                {
                    drag.position = new Vector2(-10000, -10000); surface.OnPointerExit(drag); surface.OnDrag(drag); surface.OnPointerUp(drag);
                }
                else if (cancellation == "focus") Call(surface, "OnApplicationFocus", false);
                else if (cancellation == "tab") { Call(hub, "Show", "home"); yield return Frames(2); surface.OnPointerUp(drag); Call(hub, "Show", "chess"); }
                else surface.CancelInteraction(); // Relay emits native EndDrag, deliberately never Drop.
                yield return Frames(2);
                surface.CancelInteraction();
                Require(cancellation + " leaves FEN unchanged and restores the original piece", Game.ToFen() == before
                    && !Read<bool>(panel, "dragging") && !Read<Image>(panel, "dragGhost").gameObject.activeSelf && pieces[4,1].Face.enabled);
            }

            Phase("checked king alone shakes and reset clears the offset");
            SetFen("4r1k1/8/8/8/8/8/8/R3K2R w KQ - 0 1");
            yield return Frames(3);
            var king = pieces[4,0];
            Require("Only e1 is marked as the checked king", Game.InCheck && pieces.Cast<ChessPieceView>().Count(p => p.InCheck) == 1 && king.InCheck);
            float maximumOffset = 0;
            for (int i = 0; i < 10; i++) { maximumOffset = Mathf.Max(maximumOffset, king.Face.rectTransform.anchoredPosition.magnitude); yield return null; }
            Require("King-only check motion is nonzero and bounded", maximumOffset > .01f && maximumOffset <= 2.1f, "maximumOffset=" + maximumOffset);
            Require("Unchecked rook faces stay still", pieces[0,0].Face.rectTransform.anchoredPosition == Vector2.zero && pieces[7,0].Face.rectTransform.anchoredPosition == Vector2.zero);
            Capture("check"); yield return Frames(2);
            SetFen(ChessGame.StartFen); yield return Frames(2);
            Require("Changing the position clears every old check offset", !pieces.Cast<ChessPieceView>().Any(p => p.InCheck)
                && pieces.Cast<ChessPieceView>().All(p => p.Face.rectTransform.anchoredPosition == Vector2.zero));

            Phase("capture silhouette flash, local hit-stop, and settling");
            SetFen("4k3/8/8/3q4/8/8/8/3RK3 w - - 0 1");
            Vector2 rest = board.anchoredPosition;
            Click("d1"); Click("d5");
            Require("Capture is committed by native LCD clicks", Game.Plies == 1 && Game[3,4] == 'R' && Game[3,0] == '.');
            var captured = Read<Image>(feedback, "capturedGhost");
            yield return Until(() =>
            {
                float flash = captured.material != null && captured.material.HasProperty("_Flash") ? captured.material.GetFloat("_Flash") : 0;
                Result.maximumCaptureFlash = Mathf.Max(Result.maximumCaptureFlash, flash);
                return flash > .01f;
            }, 2, "Captured piece reaches a positive silhouette flash");
            Require("Capture ghost is visible and hit-stop stays board-local", captured.gameObject.activeSelf
                && board.GetComponent<XgJuice>().LocalHitStop && Mathf.Approximately(Time.timeScale, previousTimeScale));
            Capture("capture"); yield return Frames(2);
            yield return Settled(); yield return Seconds(.25f);
            Require("Capture settles the board, hides both ghosts, and records combo one", Vector2.Distance(board.anchoredPosition, rest) < .05f
                && !captured.gameObject.activeSelf && !Read<Image>(feedback, "movingGhost").gameObject.activeSelf && feedback.PlayerCombo == 1);

            Phase("en passant uses the victim's original square");
            SetFen("4k3/8/8/3pP3/8/8/8/4K3 w - d6 0 1");
            Click("e5"); Click("d6");
            Require("En passant moves e5 to d6 and removes d5", Game[3,5] == 'P' && Game[3,4] == '.' && Game.LastMove.IsEnPassant);
            Require("En passant capture ghost starts on d5, not d6", captured.gameObject.activeSelf
                && Vector2.Distance(captured.rectTransform.anchoredPosition, ChessPanel.SquarePosition(3,4)) < .01f
                && Vector2.Distance(captured.rectTransform.anchoredPosition, ChessPanel.SquarePosition(3,5)) > 50);
            Capture("en-passant"); yield return Frames(2); yield return Settled();

            Phase("checkmate reward once and real new-game button");
            SetFen("6k1/5ppp/8/8/8/8/8/R5K1 w - - 0 1");
            int wins = Read<int>(panel, "wins");
            Click("a1"); Click("a8"); yield return Settled();
            Require("Rook a1 to a8 produces WhiteWins and exactly one local win", Game.Result == ChessGame.WhiteWins && Read<int>(panel, "wins") == wins + 1);
            string rewardedLab = lab != null ? JsonUtility.ToJson(lab.S) : null;
            Call(panel, "Finish"); Call(panel, "Finish");
            Require("Repeated Finish cannot repeat the win or lab reward", Read<int>(panel, "wins") == wins + 1
                && (lab == null || JsonUtility.ToJson(lab.S) == rewardedLab));
            Require("Victory has active confetti and visible outcome text", SimpleCount("Bit") > 0
                && board.GetComponentsInChildren<TMP_Text>().Any(t => t.gameObject.activeInHierarchy && (t.text.Contains("你赢了") || t.text.Contains("You win!"))));
            Capture("victory"); yield return Frames(3);
            var again = Read<RectTransform>(panel, "areaRoot").GetComponentsInChildren<Button>().Single(b => b.name == "Again");
            LingGuangV05.Editor.ChapterOnePlayQa.Click((RectTransform)again.transform); yield return Frames(3);
            Require("Native New game clears state, combo, both ghosts, and celebration", Game.ToFen() == ChessGame.StartFen && Game.Result == ChessGame.Ongoing
                && feedback.PlayerCombo == 0 && !feedback.Busy && !Read<bool>(panel, "counted")
                && !captured.gameObject.activeSelf && !Read<Image>(feedback, "movingGhost").gameObject.activeSelf && SimpleCount("Bit") == 0);

            Phase("entered-game fan deadline");
            var fan = menu.GetComponent<CaseMenuFanAudio>();
            Require("The entry fan countdown exists", fan != null);
            double deadline = Read<double>(fan, "gameplayStopAt");
            Require("Only the actual Main entry has scheduled fan shutdown", !double.IsNaN(deadline), "deadline=" + deadline);
            yield return Until(() => Time.realtimeSinceStartupAsDouble >= deadline + .05d, 11, "Ten seconds after Main entry have elapsed");
            var fanSource = Read<AudioSource>(fan, "source");
            Require("Looping fan is stopped and silent after the entry deadline", fanSource != null && fanSource.loop && !fanSource.isPlaying && fanSource.volume == 0f);
            fan.SetStable(); yield return Frames(2);
            Require("SetStable cannot resurrect an expired fan", !fanSource.isPlaying && fanSource.volume == 0f);
            Require("QA kept disk writes and simulation ticking disabled", !runtime.useDiskSave && !runtime.tickEnabled);
            Require("No runtime errors were logged during the QA window", Result.errors.Count == 0, string.Join("\n", Result.errors));
            yield return Frames(3);
            Require("Every requested screenshot was written", Result.screenshots.Count >= 7 && Result.screenshots.All(p => File.Exists(p) && new FileInfo(p).Length > 0));
        }

        static void SetFen(string fen)
        {
            surface.CancelInteraction(); Write(panel, "resetting", true); panel.CancelPieceDrag(); feedback.ClearFeedback();
            Write(panel, "game", ChessGame.FromFen(fen)); Write(panel, "aiAt", -1f); Write(panel, "animating", false);
            Write(panel, "hiddenSquare", -1); Write(panel, "counted", false); Write(panel, "ignoreClickUntil", -1f);
            Read<List<string>>(panel, "log").Clear(); Call(panel, "Deselect"); Write(panel, "resetting", false); Call(panel, "Draw");
            Canvas.ForceUpdateCanvases();
        }

        static IEnumerator Settled()
        {
            yield return Until(() => !feedback.Busy && !Read<bool>(panel, "animating"), 3, "Piece animation completes");
            Write(panel, "aiAt", -1f);
        }

        static RectTransform Square(string algebraic) => (RectTransform)board.Find("Square " + algebraic);
        static void Click(string algebraic) => LingGuangV05.Editor.ChapterOnePlayQa.Click(Square(algebraic));
        static PointerEventData BeginDrag(string from, string toward)
        {
            var data = new PointerEventData(EventSystem.current) { position = PhysicalPoint(Square(from)), button = PointerEventData.InputButton.Left, pointerId = -1 };
            Require("Drag source maps into the LCD: " + from, surface.TryMap(data.position, out _));
            surface.OnPointerEnter(data); surface.OnPointerMove(data); surface.OnPointerDown(data);
            data.position = Vector2.Lerp(data.position, PhysicalPoint(Square(toward)), .55f);
            surface.OnDrag(data);
            return data;
        }

        static Vector2 PhysicalPoint(RectTransform target)
        {
            Canvas.ForceUpdateCanvases();
            Camera source = surface.sourceCamera;
            Vector2 pixels = RectTransformUtility.WorldToScreenPoint(source, target.TransformPoint(target.rect.center));
            float width = source.targetTexture != null ? source.targetTexture.width : source.pixelWidth;
            float height = source.targetTexture != null ? source.targetTexture.height : source.pixelHeight;
            float u = pixels.x / width, v = pixels.y / height; if (surface.flipY) v = 1 - v;
            Rect bounds = surface.displayRect.rect;
            Vector3 world = surface.displayRect.TransformPoint(new Vector3(bounds.xMin + u * bounds.width, bounds.yMin + v * bounds.height));
            var canvas = surface.displayRect.GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? (canvas.worldCamera != null ? canvas.worldCamera : Camera.main) : null;
            return RectTransformUtility.WorldToScreenPoint(camera, world);
        }

        static int SimpleCount(string name) => board.GetComponentsInChildren<Transform>().Count(t => t.name == name && t.gameObject.activeInHierarchy);
        static IEnumerator Frames(int count) { for (int i = 0; i < count; i++) yield return null; }
        static IEnumerator Seconds(float seconds) { double end = EditorApplication.timeSinceStartup + seconds; while (EditorApplication.timeSinceStartup < end) yield return null; }
        static IEnumerator Until(Func<bool> predicate, float timeout, string name)
        {
            double end = EditorApplication.timeSinceStartup + timeout;
            while (!predicate() && EditorApplication.timeSinceStartup < end) yield return null;
            Require(name, predicate());
        }
        static void Capture(string name) { string path = Path.Combine(OutputDirectory, name + ".png"); ScreenCapture.CaptureScreenshot(path); Result.screenshots.Add(path); Save(); }
        static void Phase(string phase) { Result.phase = phase; Save(); }
        static void Record(string name, bool passed, string detail = "")
        {
            Result.checks.Add(new Check { name = name, passed = passed, detail = detail });
            if (passed) Result.passed++; else Result.failed++;
            Save();
        }
        static void Require(string name, bool passed, string detail = "") { Record(name, passed, detail); if (!passed) throw new InvalidOperationException(name + ": " + detail); }
        static void Pause(Behaviour component) { if (component == null) return; paused.Add((component, component.enabled)); component.enabled = false; }
        static FieldInfo Field(Type type, string name)
        {
            for (; type != null; type = type.BaseType)
            { var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly); if (field != null) return field; }
            throw new MissingFieldException(name);
        }
        static T Read<T>(object target, string name) => (T)Field(target.GetType(), name).GetValue(target);
        static void Write(object target, string name, object value) => Field(target.GetType(), name).SetValue(target, value);
        static object Call(object target, string name, params object[] values)
        {
            var method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null) throw new MissingMethodException(target.GetType().Name, name);
            return method.Invoke(target, values);
        }

        static void Advance()
        {
            if (!Running) return;
            if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup - startedAt > 60)
            { Record("QA interrupted or exceeded the 60-second deadline", false, Result.phase); Finish(false); return; }
            if (previousFrame == Time.frameCount) return;
            previousFrame = Time.frameCount;
            try
            {
                if (panel != null && capturedPanel) Write(panel, "aiAt", -1f);
                if (!Mathf.Approximately(Time.timeScale, previousTimeScale)) throw new InvalidOperationException("A board-local effect changed global Time.timeScale.");
                while (routines.Count > 0)
                {
                    var routine = routines.Peek();
                    if (!routine.MoveNext()) { routines.Pop(); continue; }
                    if (routine.Current is IEnumerator nested) { routines.Push(nested); continue; }
                    return;
                }
                Finish(true);
            }
            catch (Exception error) { Record("QA exception", false, error.ToString()); Finish(false); }
        }
        static void OnLog(string condition, string stack, LogType type)
        { if (Running && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) Result.errors.Add(condition + "\n" + stack); }
        static void OnMode(PlayModeStateChange state)
        { if (Running && state == PlayModeStateChange.ExitingPlayMode) { Record("Play stopped before completion", false, Result.phase); Finish(false); } }
        static void OnReload() { if (Running) { Record("Assembly reload interrupted QA", false, Result.phase); Finish(false); } }

        static void Finish(bool completed)
        {
            if (!Running || cleaningUp) return;
            cleaningUp = true;
            EditorApplication.update -= Advance; EditorApplication.playModeStateChanged -= OnMode;
            AssemblyReloadEvents.beforeAssemblyReload -= OnReload; Application.logMessageReceived -= OnLog;
            try
            {
                if (surface != null) surface.CancelInteraction();
                if (panel != null && capturedPanel)
                {
                    Write(panel, "resetting", true); panel.CancelPieceDrag(); if (feedback != null) feedback.ClearFeedback();
                    foreach (var pair in panelSnapshot) Write(panel, pair.Key, pair.Value);
                    var log = Read<List<string>>(panel, "log"); log.Clear(); log.AddRange(savedLog);
                    var targets = Read<List<ChessMove>>(panel, "targets"); targets.Clear(); targets.AddRange(savedTargets);
                    Write(panel, "resetting", false); Call(panel, "Draw");
                }
                if (hub != null && previousPage != null) Call(hub, "Show", previousPage);
                if (window != null && !previousWindowOn) window.CloseWindow();
            }
            catch (Exception error) { Result.failed++; Result.errors.Add("Cleanup: " + error); }
            finally
            {
                bool stateRestored = true;
                try
                {
                    if (runtime?.Sim != null && coreSnapshot != null) JsonUtility.FromJsonOverwrite(coreSnapshot, runtime.Sim.S);
                    if (lab != null && labSnapshot != null) JsonUtility.FromJsonOverwrite(labSnapshot, lab.S);
                    if (motionExisted) PlayerPrefs.SetInt(StoryDesktopPresenter.ReduceMotionPref, previousMotion); else PlayerPrefs.DeleteKey(StoryDesktopPresenter.ReduceMotionPref);
                }
                catch (Exception error)
                {
                    stateRestored = false; Result.failed++;
                    Result.errors.Add("State restoration failed; disk saving remains disabled for safety: " + error);
                }
                if (runtime != null) { runtime.useDiskSave = stateRestored && previousDiskSave; runtime.tickEnabled = previousTick; }
                foreach (var item in paused) if (item.component != null) item.component.enabled = item.enabled;
                Time.timeScale = previousTimeScale;
                routines.Clear(); paused.Clear(); Running = false;
                Result.complete = completed && Result.failed == 0 && Result.errors.Count == 0;
                Result.elapsedSeconds = (float)(EditorApplication.timeSinceStartup - startedAt);
                Result.phase = Result.complete ? "completed" : "failed";
                Save(); LingGuangV05.Editor.ChapterOneQaReloadGuard.Release();
            }
            Debug.Log("[Chess QA] " + Result.passed + " passed / " + Result.failed + " failed. " + OutputDirectory);
        }
        static void Save()
        { if (Result != null && !string.IsNullOrEmpty(OutputDirectory)) File.WriteAllText(Path.Combine(OutputDirectory, "report.json"), JsonUtility.ToJson(Result, true)); }
    }
}
#endif
