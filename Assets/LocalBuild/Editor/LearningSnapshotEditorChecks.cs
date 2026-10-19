#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using LingGuangV05.Runtime.Persistence;
using LingGuangV05.XingGuang;
using UnityEngine;

/// <summary>Prepared isolated EditMode verification of real JsonUtility + Runtime save/slot/export paths.</summary>
public static class LearningSnapshotEditorChecks
{
    [Serializable] sealed class Report { public string status, detail, directory; public List<string> passed; }
    public static void Run()
    {
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Run in Edit Mode.");
        if (Application.isBatchMode)
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
        if (UnityEngine.Object.FindAnyObjectByType<ChapterOneRuntime>(FindObjectsInactive.Include) != null)
            throw new InvalidOperationException("Run this check in an empty scene, with no active or inactive game runtime.");
        string root = Path.GetFullPath(Path.Combine("LocalVerification", "LearningSnapshots", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));
        Directory.CreateDirectory(root); var passed = new List<string>();
        string priorDirectory = ChapterOneRuntime.VerificationSaveDirectory;
        var priorValidator = ChapterOneRuntime.LabStateReadable;
        GameObject host = null; ChapterOneRuntime runtime = null; Action snapshot = null;
        void Need(bool ok, string label) { if (!ok) throw new Exception(label); passed.Add(label); }
        try
        {
            int coreChecks = LearningRecordChecks.Run(s => JsonUtility.FromJson<XgState>(JsonUtility.ToJson(s)));
            Need(coreChecks > 0, "Learning record core checks with Unity JsonUtility: " + coreChecks);
            ChapterOneRuntime.VerificationSaveDirectory = Path.Combine(root, "Save");
            ChapterOneRuntime.LabStateReadable = json => { try { return JsonUtility.FromJson<XgState>(json)?.version == 1; } catch { return false; } };
            host = new GameObject("IsolatedLearningSnapshotQa"); host.SetActive(false);
            runtime = host.AddComponent<ChapterOneRuntime>(); runtime.useDiskSave = true; runtime.tickEnabled = false; runtime.EnsureInitialized();
            var profile = PrologueProfile.Read("嘴硬心软"); profile.name = "记录测试"; profile.self = "我"; profile.callMe = "你";
            Need(runtime.CompletePrologue(profile), "Isolated real runtime finishes setup");
            var lab = new XgSim(); lab.LearningClock = () => new XgLearningTime(20160101, 3600);
            for (int i = 0; i < 5000; i++) lab.RecordTeachingQuestion("真实题目记录 " + i + "：保留问题、答案和解释。", "Recorded question " + i + ": keep the prompt, answer and explanation.", true, false, "原题解释", "Original explanation", "sense", null, "玩家选择", "AI当时回答");
            snapshot = () => runtime.Sim.S.labState = JsonUtility.ToJson(lab.S); runtime.Saving += snapshot;
            bool saved = runtime.SaveNow();
            Need(saved, "Actual Runtime.SaveNow stores >2MiB history: " + runtime.SaveStatus);
            Need(new FileInfo(runtime.SavePath).Length < 512, "Main file is bounded v2 descriptor");
            Need(runtime.SaveSlot(2), "Actual SaveSlot persists independent archive");
            Need(runtime.CaptureChapter(1), "Actual chapter snapshot persists archive");
            string export = Path.Combine(root, "Export"); Need(runtime.ExportSaveSnapshot(export), "Actual runtime export is self-contained");
            runtime.Saving -= snapshot; snapshot = null;
            runtime.Sim.S.money = 123; Need(runtime.SaveNow(), "Main state can change after slot capture");
            Need(runtime.LoadArchive(2), "Actual LoadArchive restores slot through chunk materialization");
            var restored = new XgSim(JsonUtility.FromJson<XgState>(runtime.Sim.S.labState));
            Need(restored.QueryLearningRecords().total == 5000 && restored.QueryTeachingNotes().total == 5000, "All5000 records and snapshots survive slot reload");
            Need(restored.QueryLearningRecords(new XgLearningQuery { offset = 4999, limit = 1 }).records[0].id == 1, "Oldest page remains accessible");
            Need(runtime.ImportSaveSnapshot(export), "Actual runtime import restores exported archive");
            Need(runtime.ReloadSave(), "Actual runtime disk reload supports v2");
            restored = new XgSim(JsonUtility.FromJson<XgState>(runtime.Sim.S.labState));
            Need(restored.QueryLearningRecords().total == 5000 && restored.LearningTimestamp(restored.LearningRecord(1)) == "2016-01-01 01:00:00", "JsonUtility preserves all history and game timestamps");
            Need(new AtomicSaveStore(export).Load().Success, "Export remains independently checksum-valid");
            File.WriteAllText(Path.Combine(root, "result.json"), JsonUtility.ToJson(new Report { status = "passed", directory = root, passed = passed, detail = "Actual isolated Runtime/JsonUtility disk, slots, chapter snapshot and export/import paths; no Main UI or Windows native behavior tested." }, true));
            File.WriteAllText(Path.Combine(root, "passed.txt"), passed.Count.ToString());
        }
        catch (Exception error)
        {
            File.WriteAllText(Path.Combine(root, "result.json"), JsonUtility.ToJson(new Report { status = "failed", directory = root, passed = passed, detail = error.ToString() }, true)); throw;
        }
        finally
        {
            if (runtime != null && snapshot != null) runtime.Saving -= snapshot;
            if (runtime != null) runtime.useDiskSave = false;
            if (host != null) UnityEngine.Object.DestroyImmediate(host);
            ChapterOneRuntime.VerificationSaveDirectory = priorDirectory; ChapterOneRuntime.LabStateReadable = priorValidator;
        }
    }
}
#endif
