#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LingGuangV05.Core;
using LingGuangV05.XingGuang;
using UnityEditor;
using UnityEngine;

/// <summary>Isolated synchronous EditMode checks. No scene, runtime, player profile or PlayerPrefs access.</summary>
public static class TaskQueueIntegrationChecks
{
    [Serializable] sealed class Report
    {
        public string status = "running", error = "", completedUtc = "";
        public int taskCount;
        public List<string> checks = new List<string>();
        public List<string> uncovered = new List<string> {
            "GuideNote UI geometry, actual routed controls, open/close callbacks and raycasts are not instantiated.",
            "Toast merging, blink timing, reduced motion, menu/cutscene suppression and audio require a PlayMode UI fixture.",
            "JSON checks exercise the real GameState.labState envelope on isolated disk, not archive-store transactions."
        };
    }
    static void Need(Report r, bool ok, string label)
    {
        if (!ok) throw new InvalidOperationException("Task queue: " + label);
        r.checks.Add(label);
    }
    static string OutputRoot()
    {
        string[] args = Environment.GetCommandLineArgs();
        string value = null;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--verification-save-dir" && i + 1 < args.Length) value = args[++i];
            else if (args[i].StartsWith("--verification-save-dir=", StringComparison.Ordinal)) value = args[i].Substring("--verification-save-dir=".Length);
        }
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Supply --verification-save-dir with an external isolated output directory.");
        string root = Path.GetFullPath(value), project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string prefix = project.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (root.Equals(project, StringComparison.OrdinalIgnoreCase) || root.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Verification output must be outside the project.");
        // A fresh child prevents overwriting any existing verification or player files.
        return Path.Combine(root, "TaskQueue-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + "-" + Guid.NewGuid().ToString("N"));
    }
    [MenuItem("Tools/Local Verification/Task queue integration")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Run in EditMode.");
        string dir = OutputRoot(); Directory.CreateDirectory(dir);
        var report = new Report();
        try
        {
            CheckCatalog(report);
            CheckRoundtrip(report, dir);
            CheckQueue(report);
            report.status = "passed";
        }
        catch (Exception ex) { report.status = "failed"; report.error = ex.ToString(); throw; }
        finally
        {
            report.completedUtc = DateTime.UtcNow.ToString("O");
            File.WriteAllText(Path.Combine(dir, "report.json"), JsonUtility.ToJson(report, true));
            Debug.Log("TASK_QUEUE_INTEGRATION " + report.status + " " + dir);
        }
    }
    static void CheckCatalog(Report r)
    {
        r.taskCount = XgTaskBook.All.Count;
        Need(r, r.taskCount == 60, "Exactly 60 authored task definitions");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var apps = new HashSet<string> { XgGuide.Lab, XgGuide.Crowd, XgGuide.Home, XgGuide.Shop, XgGuide.YY, XgGuide.Games, XgGuide.Tieba, XgGuide.Bodu, XgGuide.Desktop };
        foreach (var d in XgTaskBook.All)
        {
            Need(r, !string.IsNullOrWhiteSpace(d.id) && ids.Add(d.id) && ReferenceEquals(XgTaskBook.Find(d.id), d), d.id + ": unique indexed ID");
            Need(r, d.stage >= 1 && d.stage <= 6 && d.trigger != null && d.done != null, d.id + ": stage and predicates");
            Need(r, !string.IsNullOrWhiteSpace(d.zh) && !string.IsNullOrWhiteSpace(d.en) && !string.IsNullOrWhiteSpace(d.bodyZh) && !string.IsNullOrWhiteSpace(d.bodyEn)
                && Regex.IsMatch(d.zh, @"[\u4e00-\u9fff]") && Regex.IsMatch(d.en, "[A-Za-z]")
                && d.Title(false) == d.zh && d.Title(true) == d.en && d.Body(false) == d.bodyZh && d.Body(true) == d.bodyEn, d.id + ": Chinese and English copy");
            var jump = d.Jump();
            if (string.IsNullOrEmpty(d.app)) { Need(r, jump == null, d.id + ": no jump for empty app"); continue; }
            Need(r, apps.Contains(d.app) && jump != null && jump.id == "task." + d.id && jump.app == d.app && jump.tab == d.tab && jump.target == d.target && jump.arg != null, d.id + ": jump fields and known app");
            if (d.app == XgGuide.Lab) Need(r, new[] { "teach", "train", "tree", "final", "" }.Contains(d.tab), d.id + ": known lab tab");
            if (d.app == XgGuide.Crowd) Need(r, new[] { "label", "contracts", "" }.Contains(d.tab), d.id + ": known crowd tab");
            if (!string.IsNullOrEmpty(d.target))
            {
                int colon = d.target.IndexOf(':');
                Need(r, colon > 0 && colon < d.target.Length - 1 && new[] { "node", "name", "label", "contract" }.Contains(d.target.Substring(0, colon)), d.id + ": supported nonempty locator");
                if (d.target.StartsWith("node:", StringComparison.Ordinal)) Need(r, XgCatalog.Node(d.target.Substring(5)) != null, d.id + ": referenced technology exists");
            }
        }
        // Invoke predicates directly: unlike TickTasks.Safe, this surfaces exceptions.
        for (int stage = 1; stage <= 6; stage++)
        {
            var sim = new XgSim(); sim.S.stage = stage; var context = new XgTaskContext(sim);
            string before = JsonUtility.ToJson(sim.S);
            foreach (var d in XgTaskBook.All)
            {
                d.trigger(context); d.done(context);
                if (!d.HasProgress) continue;
                double current = d.current(context), fraction = d.Fraction(context);
                Need(r, !double.IsNaN(current) && !double.IsInfinity(current) && !double.IsNaN(fraction) && fraction >= 0 && fraction <= 1 && !string.IsNullOrWhiteSpace(d.ProgressText(context)), d.id + ": finite progress at stage " + stage);
            }
            Need(r, JsonUtility.ToJson(sim.S) == before, "Task predicates do not mutate state at stage " + stage);
        }
    }
    static void CheckRoundtrip(Report r, string dir)
    {
        var sim = new XgSim();
        sim.S.taskVersion = 1;
        sim.S.taskActive = new List<string> { "train.first", "labels.hundred" };
        sim.S.taskWaiting = new List<string> { "streak.nine", "mlp" };
        sim.S.taskDone = new List<string> { "tree.open" };
        var envelope = new GameState { labState = JsonUtility.ToJson(sim.S) };
        string file = Path.Combine(dir, "task-queue-roundtrip.json");
        File.WriteAllText(file, JsonUtility.ToJson(envelope, true));
        var loaded = JsonUtility.FromJson<GameState>(File.ReadAllText(file));
        var state = JsonUtility.FromJson<XgState>(loaded.labState);
        var resumed = new XgSim(state);
        Need(r, resumed.S.taskVersion == 1 && resumed.S.taskActive.SequenceEqual(sim.S.taskActive) && resumed.S.taskWaiting.SequenceEqual(sim.S.taskWaiting) && resumed.S.taskDone.SequenceEqual(sim.S.taskDone), "GameState disk JSON roundtrip retains version, order and all three queue lists through XgSim repair");
        var legacy = new XgSim(JsonUtility.FromJson<XgState>("{\"stage\":1}"));
        Need(r, legacy.TickTasks().Count == 0 && legacy.S.taskVersion == 1, "Legacy JSON without queue fields initializes silently");
        var nullLists = new XgSim(); nullLists.S.taskActive = null; nullLists.S.taskWaiting = null; nullLists.S.taskDone = null;
        nullLists.TickTasks();
        Need(r, nullLists.S.taskActive != null && nullLists.S.taskWaiting != null && nullLists.S.taskDone != null, "Null legacy queue lists repair safely");
    }
    static void CheckQueue(Report r)
    {
        var sim = new XgSim(); sim.S.taskVersion = 1;
        sim.S.taskActive = new List<string> { "train.first", "labels.hundred", "streak.nine", "mlp", "assess.first" };
        sim.S.taskWaiting = new List<string> { "raise.two", "ckpt.sixty" };
        sim.S.taskDone = new List<string> { "train.first" };
        var events = sim.ClearFinishedTasks();
        Need(r, sim.TasksShown.Count == XgSim.TaskCapacity && sim.S.taskActive.Last() == "raise.two" && sim.S.taskWaiting.SequenceEqual(new[] { "ckpt.sixty" }) && events.Count == 1 && events[0].kind == XgTaskEventKind.Arrived, "Clearing finished rows preserves capacity and FIFO waiting order");
        var completion = new XgSim(); completion.S.taskVersion = 1; completion.S.taskActive.Add("train.first"); completion.S.epochs = 1;
        var first = completion.TickTasks(); var second = completion.TickTasks();
        Need(r, first.Count(e => e.kind == XgTaskEventKind.Completed && e.task.id == "train.first") == 1 && !second.Any(e => e.kind == XgTaskEventKind.Completed && e.task.id == "train.first") && completion.TaskDone("train.first"), "Shown completion is recorded and announced exactly once");
        var expired = new XgSim(); expired.S.taskVersion = 1; expired.S.stage = 3; expired.S.taskActive.Add("train.first"); expired.S.epochs = 0;
        expired.TickTasks();
        Need(r, !expired.S.taskActive.Contains("train.first") && expired.TaskDone("train.first"), "Unfinished stage-one task retires two stages later");
    }
}
#endif
