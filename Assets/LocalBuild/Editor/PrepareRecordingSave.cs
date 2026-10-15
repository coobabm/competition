using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LingGuangV05.Core;
using LingGuangV05.Core.Hardware;
using LingGuangV05.Runtime.Persistence;
using LingGuangV05.XingGuang;
using UnityEditor;
using UnityEngine;

// Explicit isolated preview fixture, never a migration of the player's own progress.
public static class PrepareRecordingSave
{
    [MenuItem("Tools/LingGuang/Prepare Stage Four Recording Save")]
    public static void Run()
    {
        string folder = Path.GetFullPath("Build/Recording-Stage4/UserData");
        var game = new ChapterOneSim();
        if (!game.CompletePrologue(new PrologueProfile { name = "小星", self = "我", callMe = "你", personality = "好奇，热情，有一点调皮" }))
            throw new InvalidOperationException("Recording profile could not be created.");
        game.S.appInstallState = 2;
        game.S.money = 200000;
        game.S.gpuModels = new List<string> { HardwareCatalog.Gtx1080 };
        game.S.gpuCount = 1;
        game.S.caseCount = 1;
        game.S.nvme = true;
        game.S.jobEnabled = false;
        game.S.lastSeenUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var sim = new XgSim();
        sim.S.stage = sim.S.stageVision = sim.S.stageSequence = 4;
        sim.S.abilitiesEmerged = new List<int> { 1, 2, 3, 4 };
        sim.S.emerged = new List<int> { 1, 2, 3 };
        sim.S.trainingIntroDelivered = sim.S.treeIntroDelivered = sim.S.paidAssessmentReached = true;
        sim.S.gradualSystems = true;
        sim.S.spiderIntroDone = true;
        sim.S.residentQuiet = false;
        sim.S.epochs = 48;
        sim.S.reduceFx = sim.S.mute = false;
        var wallet = new PreviewWallet(game);
        var purchased = new List<string>();
        for (int pass = 0; pass < 20; pass++)
        {
            int before = purchased.Count;
            foreach (var node in XgCatalog.Nodes)
            {
                if (node.stage > 4 || node.kind == XgNodeKind.Project || node.kind == XgNodeKind.Breakthrough
                    || node.kind == XgNodeKind.Secret || node.kind == XgNodeKind.Ability || node.kind == XgNodeKind.Phenomenon
                    || node.kind == XgNodeKind.Dataset || sim.Has(node.id)) continue;
                if (sim.BuyNode(node.id, wallet)) purchased.Add(node.id);
            }
            if (before == purchased.Count) break;
        }
        // Keep progression below the fifth ability; money and raw examples are preview resources.
        sim.S.trainedParamsK = 145;
        sim.S.labels.Clear();
        foreach (string desk in new[] { "mnist", "poems", "sense", "spam" })
        {
            if (XgCatalog.Dataset(desk) == null) continue;
            sim.S.labels.Add(new XgLabelCount { dataset = desk, count = 32000 });
            var data = XgCatalog.Dataset(desk);
            var run = sim.Run(data.track);
            var knobs = sim.Knobs(run);
            knobs.lr = .01;
            knobs.clip = true;
            // Real board learning from authored examples, not invented decorative concepts.
            var cards = sim.TestSet(desk);
            for (int repeat = 0; repeat < 3; repeat++)
                foreach (var card in cards.Take(32)) sim.Board.Train(card, knobs);
        }
        foreach (var run in sim.Runs)
        {
            run.running = run.epochActive = false;
            run.epoch = 24;
            run.epochProgress = 0;
        }
        sim.S.desk = "mnist";
        sim.S.stageStartParamsK = sim.S.trainedParamsK;
        sim.S.stageStartSamples = sim.TrainedSamples;
        sim.S.monthProgress = .12;
        sim.S.stageClock = 0;
        game.S.money = 12888;
        game.S.labState = JsonUtility.ToJson(sim.S);
        var store = new AtomicSaveStore(folder);
        if (!store.TrySave(JsonUtility.ToJson(game.S), out string error)) throw new IOException(error);
        var result = store.Load(payload => JsonUtility.FromJson<GameState>(payload) != null);
        if (!result.Success) throw new IOException(result.Message);
        var restoredGame = new ChapterOneSim(JsonUtility.FromJson<GameState>(result.Payload));
        var restored = new XgSim(JsonUtility.FromJson<XgState>(restoredGame.S.labState));
        if (restored.S.stage != 4 || !restored.SpiderAround || restored.HasAbility(5) || restored.S.chapterComplete
            || restored.S.board.concepts.Count == 0 || restored.TrainedSamples >= restored.SamplesThreshold(5))
            throw new InvalidOperationException("Preview save validation failed.");
        File.WriteAllText(Path.Combine(folder, "录制存档说明.txt"),
            "第四阶段独立录制档（人为准备的演示进度，不是自然通关记录）\n" +
            "小星；前四项能力；灵宠已出场；完整特效及声音；自动训练初始关闭。\n" +
            "¥12888；GTX 1080；NVMe；阶段四及以前可购买的大部分非数据技能已购买。\n" +
            "第五阶段和终局未解锁。存档保存在本包 UserData，不替换玩家普通存档。\n" +
            "板上概念由真实学习规则处理内置例题生成，资源/阶段由录制夹具设置。\n" +
            "已购节点：" + string.Join(", ", purchased) + "\n" +
            "概念数：" + restored.S.board.concepts.Count + "；有效样本：" + restored.TrainedSamples + "\n");
        Debug.Log("RECORDING_SAVE_PASS stage=4 spider=true concepts=" + restored.S.board.concepts.Count + " purchased=" + purchased.Count);
    }

    sealed class PreviewWallet : IXgHost
    {
        readonly ChapterOneSim game;
        public PreviewWallet(ChapterOneSim game) { this.game = game; }
        public double Compute => 20;
        public double VramMB => 8192;
        public double Money => game.S.money;
        public string Blocker => null;
        public bool Spend(double amount) { if (amount < 0 || Money < amount) return false; game.S.money -= amount; return true; }
        public void Earn(double amount) { game.S.money += amount; }
        public void Train(double seconds) { }
    }
}
