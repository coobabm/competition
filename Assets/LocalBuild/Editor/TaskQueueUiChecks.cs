#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LingGuangV05.CaseMenu;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Real builders and Button callbacks on an inactive, isolated desktop. No runtime or player save.</summary>
public static class TaskQueueUiChecks
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    static FieldInfo Field(object o, string n) => o.GetType().GetField(n, Flags) ?? throw new MissingFieldException(o.GetType().Name, n);
    static T Get<T>(object o, string n) => (T)Field(o, n).GetValue(o);
    static void Set(object o, string n, object v) => Field(o, n).SetValue(o, v);
    static object Call(object o, string n, params object[] args) => o.GetType().GetMethod(n, Flags).Invoke(o, args);
    [Serializable] sealed class Report
    {
        public string status = "running", error = "";
        public List<string> checks = new List<string>();
        public List<string> uncovered = new List<string> {
            "Inactive EditMode fixture: no rendered glyph clipping, physical pointer raycasts or screen screenshots.",
            "No actual app routing, runtime save, timed toast/blink playback, audio or menu/cutscene transition test.",
            "Taskbar exclusion uses a 42-pixel reserved desktop strip, not an instantiated native taskbar."
        };
    }
    static void Need(Report r, bool ok, string text) { if (!ok) throw new InvalidOperationException(text); r.checks.Add(text); }
    static string Output()
    {
        string value = null; var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--verification-save-dir" && i + 1 < args.Length) value = args[++i];
            else if (args[i].StartsWith("--verification-save-dir=", StringComparison.Ordinal)) value = args[i].Substring(24);
        }
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Supply external --verification-save-dir.");
        string root = Path.GetFullPath(value), project = Path.GetFullPath(Path.Combine(Application.dataPath, "..")).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (root.Equals(project, StringComparison.OrdinalIgnoreCase) || root.StartsWith(project + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Output must be outside project.");
        return Path.Combine(root, "TaskQueueUi-" + Guid.NewGuid().ToString("N"));
    }
    static RectTransform Child(string name, Transform parent)
    {
        var g = new GameObject(name, typeof(RectTransform)); g.transform.SetParent(parent, false); return (RectTransform)g.transform;
    }
    static void AssignSim(XingGuangController c, XgSim sim) => typeof(XingGuangController).GetProperty("Sim", Flags).GetSetMethod(true).Invoke(c, new object[] { sim });
    [MenuItem("Tools/Local Verification/Task queue UI isolated")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Run in EditMode.");
        string output = Output(); Directory.CreateDirectory(output);
        var r = new Report(); GameObject root = null; GuideNote note = null;
        var oldController = XingGuangController.Instance; var oldNote = GuideNote.Instance;
        bool reducedBefore = GameSettings.ReducedMotion;
        try
        {
            root = new GameObject("Inactive task queue fixture", typeof(RectTransform)); root.SetActive(false);
            var desktop = (RectTransform)root.transform; desktop.pivot = Vector2.zero; desktop.sizeDelta = new Vector2(1440, 900);
            var controller = Child("Inactive lab", desktop).gameObject.AddComponent<XingGuangController>();
            note = Child("Inactive guide", desktop).gameObject.AddComponent<GuideNote>();
            var sim = new XgSim(); sim.S.mute = true; sim.S.taskVersion = 1;
            AssignSim(controller, sim); Set(note, "lab", controller);
            var desk = new PrologueDesk(desktop, Child("Icons", desktop), Child("Windows", desktop), XgUiV4Fonts.Sans);
            Set(note, "body", Child("Hidden note body", desk.layer)); Set(note, "desk", desk);
            Call(note, "BuildTaskNotifications", desk);
            Set(note, "taskMenu", null); // No discovered scene menu participates in this fixture.
            Call(note, "BindTaskOwner");
            Need(r, XingGuangController.Instance == oldController && GuideNote.Instance == oldNote && !root.activeInHierarchy, "Inactive component construction does not replace live singleton instances");
            Need(r, (bool)Call(note, "TaskPresentationAllowed"), "Ambient presentation gates permit isolated callback checks (no globals overridden)");
            var entry = Get<RectTransform>(note, "taskButtonRoot"); var panel = Get<RectTransform>(note, "taskPanel");
            Need(r, entry.anchorMin == Vector2.one && entry.anchorMax == Vector2.one && entry.offsetMin == new Vector2(-128, -50) && entry.offsetMax == new Vector2(-16, -16), "Task entry is anchored to the top-right desktop corner");
            Need(r, panel.anchorMax == Vector2.one && panel.pivot == Vector2.one && panel.offsetMax.y <= entry.offsetMin.y - 8, "Task panel opens below the top-right entry and grows downwards");
            Need(r, Mathf.Approximately(panel.rect.width, 380), "Real panel is 380 pixels wide");
            Need(r, entry.GetComponent<Image>().raycastTarget && entry.GetComponent<Button>() != null, "Entry has an actual raycast target and Button callback");
            sim.S.taskActive.AddRange(new[] { "train.first", "streak.three", "labels.hundred", "streak.nine", "mlp" });
            sim.S.taskWaiting.Add("raise.two"); sim.S.bestCombo = 1;
            entry.GetComponent<Button>().onClick.Invoke();
            Need(r, Get<bool>(note, "taskListOpen") && panel.gameObject.activeSelf, "Real entry Button opens task panel");
            var rows = Get<Array>(note, "taskRows"); Need(r, rows.Length == 5, "Five actual task rows built");
            Need(r, !Get<TMP_Text>(note, "panelSub").gameObject.activeSelf, "Redundant task explanation is hidden");
            float previousBottom = 0;
            for (int i = 0; i < rows.Length; i++)
            {
                object row = rows.GetValue(i); var rt = Get<RectTransform>(row, "rt"); var task = Get<XgTaskDef>(row, "task");
                Need(r, rt.gameObject.activeSelf && task.id == sim.S.taskActive[i] && rt.rect.height >= 40 && (i == 0 || rt.offsetMax.y <= previousBottom - 7.9f), "Row " + i + " binds correct task and does not overlap previous row");
                Need(r, !Get<TMP_Text>(row, "body").gameObject.activeSelf, "Task row retains title and progress without gray explanation");
                previousBottom = rt.offsetMin.y;
                Need(r, !string.IsNullOrWhiteSpace(Get<TMP_Text>(row, "title").text) && Get<Button>(row, "go").gameObject.activeSelf, "Row " + i + " has title and live jump button");
            }
            var progress = rows.GetValue(1);
            Need(r, Mathf.Approximately(Get<RectTransform>(progress, "fill").anchorMax.x, 1f / 3f), "Actual progress fill binds one of three correct answers");
            Need(r, Get<TMP_Text>(note, "panelWaiting").gameObject.activeSelf, "Waiting summary is visible for queued task");
            sim.S.taskDone.Add("streak.three"); Call(note, "RefreshTaskPanel");
            Need(r, !Get<Button>(progress, "go").gameObject.activeSelf && Mathf.Approximately(Get<CanvasGroup>(progress, "group").alpha, .6f), "Completed row hides jump and dims");
            entry.GetComponent<Button>().onClick.Invoke();
            Need(r, !Get<bool>(note, "taskListOpen") && !panel.gameObject.activeSelf, "Real entry Button closes task panel");
            entry.GetComponent<Button>().onClick.Invoke();
            Need(r, !sim.S.taskActive.Contains("streak.three") && sim.S.taskActive.Contains("raise.two"), "Reopening actual panel removes completed row and promotes waiting task");
            panel.Find("Close").GetComponent<Button>().onClick.Invoke();
            Need(r, !panel.gameObject.activeSelf, "Actual panel Close button closes it");
            sim.S.reduceFx = true;
            Need(r, (bool)typeof(GuideNote).GetProperty("TaskReduced", Flags).GetValue(note), "Saved reduced-motion flag is read by actual UI property");
            sim.S.reduceFx = false;
            Need(r, (bool)typeof(GuideNote).GetProperty("TaskReduced", Flags).GetValue(note) == reducedBefore && GameSettings.ReducedMotion == reducedBefore, "Global reduced-motion preference is read without modification");
            Set(note, "taskUnread", true); Set(note, "toastLive", true); Set(note, "taskListOpen", true);
            Set(note, "firstPendingAt", 12f); Set(note, "finishedSince", 12f); Set(note, "blinkSince", 12f); Set(note, "nextTaskTick", 12f); Set(note, "toastAge", 12f);
            Get<IList>(note, "pendingEvents").Add(new XgTaskEvent(XgTaskEventKind.Arrived, XgTaskBook.Find("mlp")));
            panel.gameObject.SetActive(true); Get<RectTransform>(note, "taskToast").gameObject.SetActive(true);
            var replacement = new XgSim(); replacement.S.mute = true; AssignSim(controller, replacement); Call(note, "BindTaskOwner");
            Need(r, ReferenceEquals(Get<XgSim>(note, "taskOwner"), replacement) && Get<IList>(note, "pendingEvents").Count == 0 && !Get<bool>(note, "taskUnread") && !Get<bool>(note, "toastLive") && !Get<bool>(note, "taskListOpen"), "Switching save owner discards old notifications and unread/open flags");
            Need(r, Get<float>(note, "firstPendingAt") == -1 && Get<float>(note, "finishedSince") == -1 && Get<float>(note, "blinkSince") == -100 && Get<float>(note, "nextTaskTick") == 0 && Get<float>(note, "toastAge") == 0 && !panel.gameObject.activeSelf && !Get<RectTransform>(note, "taskToast").gameObject.activeSelf, "Switching owner resets timers and hides old panel and toast");
            r.status = "passed";
        }
        catch (Exception ex) { r.status = "failed"; r.error = ex.ToString(); throw; }
        finally
        {
            // GuideNote.OnDestroy uses deferred Destroy; clear owned references before immediate fixture teardown.
            if (note != null) { Set(note, "taskButtonRoot", null); Set(note, "taskToast", null); Set(note, "taskPanel", null); Set(note, "note", null); }
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
            File.WriteAllText(Path.Combine(output, "report.json"), JsonUtility.ToJson(r, true));
            Debug.Log("TASK_QUEUE_UI " + r.status + " " + output);
        }
    }
}
#endif
