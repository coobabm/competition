using System;
using System.Collections.Generic;
using System.IO;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using UnityEngine;

// Uses a disposable save directory. Never reads or modifies a player's progress.
public static class NamingChecks
{
    [Serializable] class Report { public string completedUtc, failure; public List<string> passed = new List<string>(); }
    static Report report;
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); report.passed.Add(message); }
    static PrologueProfile Profile(string name) => new PrologueProfile { name = name, self = "我", callMe = "你", personality = "好奇又诚实" };
    public static void Validate() { Run(); ClaudeHandoffChecks.Validate(); }
    public static void Run()
    {
        report = new Report { completedUtc = DateTime.UtcNow.ToString("o") };
        string previous = ChapterOneRuntime.VerificationSaveDirectory;
        var folder = Path.GetFullPath("LocalVerification/Naming/Save-" + DateTime.UtcNow.Ticks);
        GameObject go = null;
        try
        {
            foreach (string name in new[] { "小白", "小星", "  小白  ", "一二三四五六七八九十一二三四五六", "ｅｃｈｏ", "e\u0301", "Scunthorpe", "Cassian", "星辰-01" })
                Check(AiNamePolicy.Problem(name) == null, "Accept name: " + name);
            foreach (string name in new[] { "", "   ", "一二三四五六七八九十一二三四五六七", "小白.exe", "CON", "LPT1", "小白.", "a/b", "a\\b", "a:b", "a?b", "<b>AI</b>", "[AI]", "a\nb", "a\u200bb", "a\u202eb", "傻逼", "傻·逼", "ｆｕｃｋ", "Bitch", "the code", "\ud800" })
                Check(AiNamePolicy.Problem(name) != null, "Reject invalid name: " + name);
            Check(AiNamePolicy.Normalize("  ｅｃｈｏ  ") == "echo", "Normalize before persisting");
            Check(AppNames.PersonalExe("小白", false) == "小白.exe", "One executable suffix");
            Check(AppNames.PersonalExe("小白.exe", false) == "小白.exe", "Legacy suffix is not doubled");
            Check(AppNames.PersonalExe("<b>AI</b>", false) == "灵光.exe", "Legacy markup has safe display fallback");
            Check(PrologueProfile.Problem(Profile("小星")) == null, "Opening accepts a normal profile");
            var bad = Profile("小星"); bad.self = "傻逼";
            Check(PrologueProfile.Problem(bad) != null, "Self reference uses identity policy");
            bad = Profile("小星"); bad.callMe = "一二三四五六七八九";
            Check(PrologueProfile.Problem(bad) != null, "Player address has eight character limit");
            var legacy = Profile("CON");
            Check(PlayerProfile.FromSetup(legacy) == null, "New profile rejects reserved name");
            var stored = PlayerProfile.FromSetup(legacy, "save");
            Check(PlayerProfile.IsUsable(stored), "Existing profile remains usable");
            var newGame = new ChapterOneSim();
            Check(newGame.CompletePrologue(stored.CopyOfSetup(), legacyProfile: true) && newGame.S.aiName == "CON", "Legacy profile can skip opening without changing stored identity");
            ChapterOneRuntime.VerificationSaveDirectory = folder;
            go = new GameObject("Isolated naming persistence check"); go.SetActive(false);
            var runtime = go.AddComponent<ChapterOneRuntime>(); runtime.EnsureInitialized();
            Check(runtime.CompletePrologue(Profile("小白")), "Opening persists name");
            runtime.Sim.S.money = 12345;
            // Slot 1 is the active autosave. Use an independent manual slot for this roundtrip.
            Check(runtime.SaveSlot(2), "Save original named slot");
            Check(!runtime.RecordName("a/b") && runtime.Sim.S.aiName == "小白", "Rejected rename leaves existing name intact");
            Check(runtime.RecordName("  小星  ") && runtime.Sim.S.aiName == "小星", "Rename persists normalized identity");
            Check(runtime.OpeningProfile().setup.name == "小星", "Next game profile follows rename");
            Check(runtime.ReloadSave() && runtime.Sim.S.aiName == "小星" && runtime.Sim.S.money == 12345, "Reload preserves new name and progress");
            Check(runtime.LoadArchive(2) && runtime.Sim.S.aiName == "小白", "Another slot restores its own name");
            runtime.Sim.S.aiName = "CON";
            Check(runtime.SaveNow() && runtime.ReloadSave() && runtime.Sim.S.aiName == "CON" && runtime.Sim.S.money == 12345, "Old disallowed name still loads without progress loss");
            UnityEngine.Object.DestroyImmediate(go); go = null;
            go = new GameObject("Restarted naming check"); go.SetActive(false);
            runtime = go.AddComponent<ChapterOneRuntime>(); runtime.EnsureInitialized();
            Check(runtime.Sim.S.aiName == "CON" && runtime.Sim.S.money == 12345, "Fresh runtime preserves legacy save");
            Check(Application.productName == "LingGuang-Playability-20261008", "Existing save identity unchanged");
        }
        catch (Exception error) { report.failure = error.ToString(); throw; }
        finally
        {
            if (go != null) UnityEngine.Object.DestroyImmediate(go);
            ChapterOneRuntime.VerificationSaveDirectory = previous;
            Directory.CreateDirectory("LocalVerification/Naming");
            File.WriteAllText("LocalVerification/Naming/checks.json", JsonUtility.ToJson(report, true));
        }
        Debug.Log("NAMING_CHECKS_PASS " + report.passed.Count);
    }
}
