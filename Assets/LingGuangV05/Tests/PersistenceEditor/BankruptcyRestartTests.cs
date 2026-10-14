using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using LingGuangV05.Runtime.Persistence;
using Object = UnityEngine.Object;

namespace LingGuangV05.Tests
{
    /// <summary>重新开始 after the failure ending (经济压力与破产 §3): the save is backed up, never deleted, and a new game starts.</summary>
    public sealed class BankruptcyRestartTests
    {
        string directory;
        GameObject root;
        ChapterOneRuntime runtime;

        [SetUp] public void Setup()
        {
            directory = Path.Combine(Path.GetTempPath(), "lingguang-restart-tests-" + Guid.NewGuid().ToString("N"));
            Open();
            var profile = PrologueProfile.Read("嘴硬心软"); profile.name = "测试"; profile.self = "我"; profile.callMe = "你";
            Assert.That(runtime.CompletePrologue(profile), Is.True, runtime.SaveStatus);
        }

        [TearDown] public void Cleanup()
        {
            if (root != null) Object.DestroyImmediate(root);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        void Open()
        {
            if (root != null) Object.DestroyImmediate(root);
            root = new GameObject("IsolatedRestartTest"); root.SetActive(false);
            runtime = root.AddComponent<ChapterOneRuntime>();
            runtime.useDiskSave = true;
            runtime.ClockOverride = () => 1000;
            runtime.SaveDirectoryOverride = directory;
            runtime.EnsureInitialized();
        }

        /// <summary>Three days below zero on the lab's calendar, then the player agrees to sell: the computer is sold.</summary>
        void GoBankrupt()
        {
            var sim = runtime.Sim;
            GameCalendar.Advance(sim.S, GameCalendar.DayIndex(new DateTime(2016, 6, 3)));
            sim.Tick(.25);
            sim.S.money = -800;
            for (int i = 0; i < ChapterOneSim.DebtSaleDay; i++) sim.Tick(sim.Config.dayLengthSeconds);
            Assert.That(sim.S.bankrupt, Is.False, "the third day only asks: nothing is sold without the player's word");
            Assert.That(sim.ConfirmComputerSale(), Is.True);
            Assert.That(sim.S.bankrupt, Is.True);
            Assert.That(runtime.SaveNow(), Is.True);
        }

        [Test] public void StartingOverBacksTheSaveUpAndDeletesNothing()
        {
            Assert.That(runtime.StartOverAfterBankruptcy(), Is.False, "only after the computer was sold");
            runtime.Sim.S.money = 4321; runtime.SaveNow(); runtime.SaveNow(); // a .bak exists too
            GoBankrupt();
            var store = new AtomicSaveStore(directory);
            string ended = File.ReadAllText(store.SavePath), endedBackup = File.ReadAllText(store.SavePath + ".bak");
            var before = Directory.GetFiles(directory);

            Assert.That(runtime.StartOverAfterBankruptcy(), Is.True, runtime.SaveStatus);
            Assert.That(runtime.Sim.S.bankrupt, Is.False);
            Assert.That(runtime.Sim.InPrologue, Is.False, "the opening was finished before: a new game starts right after it");
            Assert.That(runtime.Sim.S.aiName, Is.EqualTo("测试"), "with the setup the player chose");

            foreach (var file in before) Assert.That(File.Exists(file), Is.True, "nothing deleted: " + Path.GetFileName(file));
            var copies = Directory.GetFiles(directory, "chapter-one.sav.gameover-*");
            Assert.That(copies.Length, Is.EqualTo(1), "the save, copied with a timestamp in the same folder");
            Assert.That(File.ReadAllText(copies[0]), Is.EqualTo(ended), "the copy is the ended game as it was");
            var backupCopies = Directory.GetFiles(directory, "chapter-one.sav.bak.gameover-*");
            Assert.That(backupCopies.Length, Is.EqualTo(1));
            Assert.That(File.ReadAllText(backupCopies[0]), Is.EqualTo(endedBackup));

            // The next start continues the new game; the ended one stays on disk.
            Open();
            Assert.That(runtime.Sim.InPrologue, Is.False);
            Assert.That(runtime.Sim.S.bankrupt, Is.False);
            Assert.That(File.Exists(copies[0]), Is.True);
        }

        [Test] public void QuittingOnTheGameOverCardKeepsTheEndedGameUntilTheChoice()
        {
            GoBankrupt();
            Open();
            Assert.That(runtime.Sim.S.bankrupt, Is.True, "the card shows again; nothing is reset without the player");
            Assert.That(Directory.GetFiles(directory, "*.gameover-*"), Is.Empty);
        }

        [Test] public void TwoRestartsInTheSameSecondKeepBothCopies()
        {
            var store = new AtomicSaveStore(directory);
            var moment = new DateTime(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc);
            Assert.That(store.CopyForRestart(out _, moment), Is.True);
            Assert.That(store.CopyForRestart(out _, moment), Is.True);
            Assert.That(Directory.GetFiles(directory, "chapter-one.sav.gameover-20261008T120000*").Length, Is.EqualTo(2));
            Assert.That(File.Exists(store.SavePath), Is.True);
        }
    }
}
