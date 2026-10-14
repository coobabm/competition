using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using LingGuangV05.Desktop.YY;
using UnityEngine;

public static class ClaudeHandoffChecks
{
    [Serializable] class Report { public string completedUtc, failure; public List<string> passed = new List<string>(); }
    class Wallet : IXgHost {
        public double money = 10000; public double Money => money; public double Compute => 1; public double VramMB => 6144; public string Blocker => null;
        public bool Spend(double x) { if (money < x) return false; money -= x; return true; }
        public void Earn(double x) { money += x; } public void Train(double seconds) { }
    }
    static Report report;
    static void Check(bool ok, string name) { if (!ok) throw new Exception(name); report.passed.Add(name); }
    public static void Run()
    {
        report = new Report { completedUtc = DateTime.UtcNow.ToString("o") };
        try {
            var sim = new XgSim(); var wallet = new Wallet();
            sim.S.trainedParamsK = 1000; sim.S.stageEpochs = 1;
            sim.S.labels.Add(new XgLabelCount { dataset = "spam", count = 100000 });
            for (int i = 0; i < 300; i++) sim.Tick(1, wallet);
            Check(sim.S.stage == 1 && sim.StageReady, "Training can fill both bars without advancing the chapter");
            int events = 0; sim.AbilityEmerged += _ => events++;
            Check(sim.ConfirmNextStage() && sim.S.stage == 2 && events == 1, "Explicit confirmation advances exactly one chapter");
            Check(!sim.ConfirmNextStage() && events == 1, "Double click cannot skip the next chapter's training");
            var loaded = new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(sim.S)));
            Check(loaded.S.stage == 2, "Confirmed chapter survives reload");
            Check(!new XgSim().FeatureVisible("contracts"), "Company orders stay locked before stage four");
            sim = new XgSim(); sim.S.combo = 7; sim.S.modelStreak = 3;
            double before = sim.Samples("spam");
            foreach (bool junk in new[] { true, false }) {
                sim.S.cards.Clear(); sim.S.cards.Add(new XgCard { dataset = "spam", level = 1, question = "吃饭没？", from = "妈", family = true, truth = false });
                var result = sim.Answer("spam", junk, wallet);
                Check(result.subjective && result.pay > 0 && result.samples == 0 && result.fine == 0, "Family choice pays without objective scoring: " + junk);
            }
            Check(sim.S.combo == 7 && sim.S.modelStreak == 3 && sim.S.handWrong == 0 && sim.S.handCorrect == 0 && sim.Samples("spam") == before, "Family opinions preserve streaks and never alter accuracy samples");
            loaded = new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(sim.S)));
            Check(loaded.S.familyAttitudes[0].important == 1 && loaded.S.familyAttitudes[0].junk == 1 && loaded.FamilyPrompt().Contains("妈"), "Sender attitudes survive reload and enter AI context");
            bool objectiveOnly = true; for (int i = 0; i < 1000; i++) objectiveOnly &= !XgMemes.Pick("spam", new System.Random(i), 3, 20161231, "", false).family;
            Check(objectiveOnly, "One thousand training draws exclude family opinions");
            var questions = new HashSet<string>(); int yes = 0;
            for (int i = 0; i < XgLogic.AvailableMemeCount(20160524); i++) { var q = XgLogic.MemeQuestion(i, 20160524, 1); questions.Add(q.text); if (q.truth) yes++; Check(!string.IsNullOrWhiteSpace(q.why), "Meme explanation " + i); }
            Check(questions.Count == 50 && yes == 25, "Opening logic pool has fifty unique balanced propositions");
            Check(XgLogic.AvailableMemeCount(20161231) == 58 && !questions.Any(q => q.Contains("蓝瘦") || q.Contains("洪荒")), "Later memes unlock only after their 2016 dates");
            foreach (string name in new[] { "ChatGPT", "CLAUDE CODE", "豆包", "DeepSeek", "千问", "文心一言" }) Check(YYGroup.FutureNameReply(name)?.Contains("\n[") == true, "Group has a multi-speaker period response to " + name);
            var house = new ChapterOneSim(); house.S.money = -100000;
            var settlement = typeof(ChapterOneSim).GetMethod("SettleDayWithRent", BindingFlags.Instance | BindingFlags.NonPublic);
            for (int i = 0; i < 5; i++) settlement.Invoke(house, null);
            Check(!house.S.bankrupt, "Debt never sells the computer without the player");
            Check(house.ConfirmComputerSale() && house.S.bankrupt && !house.ConfirmComputerSale(), "Explicit sale runs once");
            Slots();
        } catch (Exception error) { report.failure = error.ToString(); throw; }
        finally { Directory.CreateDirectory("LocalVerification/ClaudeHandoff"); File.WriteAllText("LocalVerification/ClaudeHandoff/checks.json", JsonUtility.ToJson(report, true)); }
        Debug.Log("CLAUDE_HANDOFF_CHECKS_PASS " + report.passed.Count);
    }

    public static void Validate() { Run(); LatestRequestChecks.Run(); PlayabilityChecks.Run(); }

    static void Slots()
    {
        var old = ChapterOneRuntime.VerificationSaveDirectory;
        var folder = Path.GetFullPath("LocalVerification/ClaudeHandoff/SaveTest-" + DateTime.UtcNow.Ticks);
        ChapterOneRuntime.VerificationSaveDirectory = folder;
        var go = new GameObject("Isolated slot test"); go.SetActive(false);
        try {
            var runtime = go.AddComponent<ChapterOneRuntime>(); runtime.EnsureInitialized();
            runtime.Sim.CompletePrologue(new PrologueProfile { name = "存档测试", self = "我", callMe = "你", personality = "诚实" });
            // The standalone persistence fixture bypasses only the prologue; all app state is serialized normally.
            runtime.Sim.S.prologue = 2;
            for (int i = 1; i <= 6; i++) { runtime.Sim.S.money = i * 100; Check(runtime.SaveSlot(i), "Save slot " + i); }
            runtime.Sim.S.money = 321; runtime.Sim.S.lastSeenUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 86400; Check(runtime.CaptureChapter(2), "Capture first chapter snapshot");
            runtime.Sim.S.money = 999; Check(runtime.CaptureChapter(2), "Revisiting does not overwrite chapter snapshot");
            for (int i = 2; i <= 6; i++) Check(runtime.LoadArchive(i) && runtime.Sim.S.money == i * 100, "Independent slot roundtrip " + i);
            Check(runtime.LoadArchive(2, true) && runtime.Sim.S.money == 321, "Chapter restores first-unlock progress without offline catch-up");
            Check(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - runtime.Sim.S.lastSeenUnix < 5, "Explicit chapter load resets offline clock");
            Check(runtime.ArchiveExists(2, true) && !runtime.ArchiveExists(6, true), "Only actually unlocked chapters appear");
            Check(!runtime.LoadArchive(9), "Invalid save slot is rejected");
        } finally { UnityEngine.Object.DestroyImmediate(go); ChapterOneRuntime.VerificationSaveDirectory = old; }
    }
}
