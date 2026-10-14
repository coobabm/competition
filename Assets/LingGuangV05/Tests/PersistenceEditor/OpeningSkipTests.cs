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
    /// <summary>
    /// The opening story is seen once: player-profile.json next to the save remembers that it was finished and the setup
    /// chosen in it; a new game then starts right after the opening. Temp folders only.
    /// </summary>
    public sealed class OpeningSkipTests
    {
        string directory;
        GameObject root;
        ChapterOneRuntime runtime;

        string ProfilePath => Path.Combine(directory, PlayerProfileStore.FileName);

        [SetUp] public void Setup() { directory = Path.Combine(Path.GetTempPath(), "lingguang-opening-tests-" + Guid.NewGuid().ToString("N")); }

        [TearDown] public void Cleanup()
        {
            if (root != null) Object.DestroyImmediate(root);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        void Open()
        {
            if (root != null) Object.DestroyImmediate(root);
            root = new GameObject("IsolatedOpeningTest"); root.SetActive(false);
            runtime = root.AddComponent<ChapterOneRuntime>();
            runtime.useDiskSave = true;
            runtime.ClockOverride = () => 1000;
            runtime.SaveDirectoryOverride = directory;
            runtime.EnsureInitialized();
        }

        PrologueProfile Chosen(string name, string self = "本机", string call = "老大", string mood = "嘴硬心软，爱吐槽")
        {
            var profile = PrologueProfile.Read(mood); profile.name = name; profile.self = self; profile.callMe = call;
            return profile;
        }

        void PlayTheOpening(string name = "小灯")
        {
            Open();
            Assert.That(runtime.Sim.InPrologue, Is.True, "a first-ever player gets the opening");
            Assert.That(runtime.CompletePrologue(Chosen(name)), Is.True, runtime.SaveStatus);
        }

        void AssertRightAfterTheOpening(string name = "小灯")
        {
            var s = runtime.Sim.S;
            Assert.That(runtime.Sim.InPrologue, Is.False);
            Assert.That(s.prologue, Is.EqualTo(2));
            Assert.That(runtime.Sim.AppInstalled, Is.True);
            Assert.That(s.appInstallState, Is.EqualTo(2));
            Assert.That(s.aiName, Is.EqualTo(name));
            Assert.That(s.aiSelf, Is.EqualTo("本机"));
            Assert.That(s.aiCallMe, Is.EqualTo("老大"));
            Assert.That(s.personality, Is.EqualTo("嘴硬心软，爱吐槽"));
            Assert.That(s.toneWords, Is.EquivalentTo(Chosen(name).words));
            Assert.That(s.targetOpinion, Is.EqualTo(Chosen(name).opinion));
            Assert.That(s.bankrupt, Is.False);
            Assert.That(OpeningQuiet.Rule(false, runtime.Sim.InPrologue, 1), Is.True, "the opening quiet window runs as for a first-timer");
        }

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

        [Test] public void FinishingTheOpeningWritesThePlayerProfileNextToTheSave()
        {
            Open();
            Assert.That(File.Exists(ProfilePath), Is.False, "nothing is remembered before the setup");
            Assert.That(runtime.CompletePrologue(Chosen("小灯")), Is.True, runtime.SaveStatus);
            Assert.That(File.Exists(ProfilePath), Is.True);
            Assert.That(Path.GetDirectoryName(ProfilePath), Is.EqualTo(Path.GetDirectoryName(new AtomicSaveStore(directory).SavePath)));
            var stored = new PlayerProfileStore(new AtomicSaveStore(directory)).Read();
            Assert.That(PlayerProfile.IsUsable(stored), Is.True);
            Assert.That(stored.source, Is.EqualTo("setup"));
            Assert.That(stored.setup.name, Is.EqualTo("小灯"));
            Assert.That(stored.setup.self, Is.EqualTo("本机"));
            Assert.That(stored.setup.callMe, Is.EqualTo("老大"));
            Assert.That(stored.setup.personality, Is.EqualTo("嘴硬心软，爱吐槽"));
        }

        [Test] public void AFirstEverStartStillPlaysTheOpening()
        {
            Open();
            Assert.That(runtime.Sim.InPrologue, Is.True);
            Assert.That(runtime.Sim.AppInstalled, Is.False);
            Assert.That(runtime.SaveNow(), Is.True);
            Assert.That(Directory.Exists(directory) && Directory.GetFiles(directory).Length > 0, Is.False, "the prologue is never written");
            Open();
            Assert.That(runtime.Sim.InPrologue, Is.True, "quitting before the setup starts the opening over");
            Assert.That(runtime.OpeningProfile(), Is.Null);
        }

        [Test] public void ANewSaveAfterTheOpeningWasFinishedSkipsItAndKeepsTheSetup()
        {
            PlayTheOpening();
            // The save is gone (a new save folder state) but the profile stays: the player does not watch it again.
            File.Move(new AtomicSaveStore(directory).SavePath, Path.Combine(directory, "moved-away.sav"));
            string bak = new AtomicSaveStore(directory).SavePath + ".bak";
            if (File.Exists(bak)) File.Move(bak, Path.Combine(directory, "moved-away.bak"));
            Open();
            AssertRightAfterTheOpening();
            Assert.That(File.Exists(new AtomicSaveStore(directory).SavePath), Is.True, "the new game is saved straight away");
            Open();
            AssertRightAfterTheOpening();
        }

        [Test] public void RestartingAfterBankruptcyKeepsTheBackupAndSkipsTheOpening()
        {
            PlayTheOpening();
            runtime.Sim.S.money = 4321; runtime.SaveNow(); runtime.SaveNow();
            GoBankrupt();
            var store = new AtomicSaveStore(directory);
            string ended = File.ReadAllText(store.SavePath);

            Assert.That(runtime.StartOverAfterBankruptcy(), Is.True, runtime.SaveStatus);
            AssertRightAfterTheOpening();
            var copies = Directory.GetFiles(directory, "chapter-one.sav.gameover-*");
            Assert.That(copies.Length, Is.EqualTo(1));
            Assert.That(File.ReadAllText(copies[0]), Is.EqualTo(ended), "the ended game is kept as it was");
            Assert.That(Directory.GetFiles(directory, "chapter-one.sav.bak.gameover-*").Length, Is.EqualTo(1));
            Assert.That(JsonUtility.FromJson<GameState>(ReadPayload(store.SavePath)).bankrupt, Is.False, "the new game is what the save holds now");

            Open();
            AssertRightAfterTheOpening();
        }

        [Test] public void TheOpeningCanBePlayedAgainOnRequestAndTheNewSetupIsRemembered()
        {
            PlayTheOpening("小灯");
            GoBankrupt();
            Assert.That(runtime.StartOverAfterBankruptcy(true), Is.True, runtime.SaveStatus);
            Assert.That(runtime.Sim.InPrologue, Is.True, "the opening again");
            Assert.That(runtime.Sim.AppInstalled, Is.False);
            // Quitting in the middle of it does not lose the first setup: the next start skips it.
            Open();
            AssertRightAfterTheOpening("小灯");
            GoBankrupt();
            Assert.That(runtime.StartOverAfterBankruptcy(true), Is.True);
            Assert.That(runtime.CompletePrologue(Chosen("新灯")), Is.True, runtime.SaveStatus);
            Assert.That(new PlayerProfileStore(new AtomicSaveStore(directory)).Read().setup.name, Is.EqualTo("新灯"), "the latest setup is the one remembered");
            Assert.That(File.Exists(ProfilePath + ".bak"), Is.True, "the previous profile is kept");
            GoBankrupt();
            Assert.That(runtime.StartOverAfterBankruptcy(), Is.True);
            AssertRightAfterTheOpening("新灯");
        }

        [Test] public void ResettingProgressSkipsTheOpeningToo()
        {
            PlayTheOpening();
            Assert.That(runtime.ResetProgress(), Is.True, runtime.SaveStatus);
            AssertRightAfterTheOpening();
            Assert.That(Directory.GetFiles(directory, "*.reset-*").Length, Is.GreaterThan(0), "the old save is archived, not deleted");
            Assert.That(runtime.ResetProgress(true), Is.True);
            Assert.That(runtime.Sim.InPrologue, Is.True);
        }

        [Test] public void AnOldInstallWithoutTheProfileFileLearnsItFromItsSave()
        {
            PlayTheOpening("旧灯");
            File.Move(ProfilePath, Path.Combine(directory, "old-install-had-no-profile.json"));
            Open(); // a normal start: the finished save proves it
            Assert.That(File.Exists(ProfilePath), Is.True, "remembered outside the save from now on");
            Assert.That(runtime.OpeningProfile().setup.name, Is.EqualTo("旧灯"));
            Assert.That(runtime.OpeningProfile().source, Is.EqualTo("save"));
        }

        [Test] public void AnOldBankruptSaveWithoutTheProfileFileRestartsPastTheOpening()
        {
            PlayTheOpening("旧灯");
            GoBankrupt();
            File.Move(ProfilePath, Path.Combine(directory, "old-install-had-no-profile.json"));
            Assert.That(runtime.StartOverAfterBankruptcy(), Is.True, runtime.SaveStatus);
            AssertRightAfterTheOpening("旧灯");
            Assert.That(File.Exists(ProfilePath), Is.True);
        }

        [Test] public void TheProfileIsDerivedFromABackupCopyWhenTheSaveItselfIsGone()
        {
            PlayTheOpening("旧灯");
            runtime.SaveNow();
            var store = new AtomicSaveStore(directory);
            GoBankrupt();
            Assert.That(store.CopyForRestart(out _), Is.True);
            // Only a timestamped copy of an ended game is left: no primary, no .bak, no profile file.
            File.Move(store.SavePath, Path.Combine(directory, "set-aside.sav"));
            File.Move(store.SavePath + ".bak", Path.Combine(directory, "set-aside.bak"));
            File.Move(ProfilePath, Path.Combine(directory, "set-aside.json"));
            if (File.Exists(ProfilePath + ".bak")) File.Move(ProfilePath + ".bak", Path.Combine(directory, "set-aside.json.bak"));
            Open();
            AssertRightAfterTheOpening("旧灯");
        }

        [Test] public void AnUnusableProfileFileIsIgnoredAndTheSavesAreAskedInstead()
        {
            Open();
            Directory.CreateDirectory(directory);
            File.WriteAllText(ProfilePath, "{ not json");
            Open();
            Assert.That(runtime.Sim.InPrologue, Is.True, "no usable profile and no finished save: the opening");
            Assert.That(runtime.CompletePrologue(Chosen("小灯")), Is.True);
            File.WriteAllText(ProfilePath, "{\"version\":1,\"openingFinished\":true,\"setup\":{\"name\":\"\"}}");
            Open();
            AssertRightAfterTheOpening("小灯"); // the finished save is read again
            Assert.That(PlayerProfile.IsUsable(new PlayerProfileStore(new AtomicSaveStore(directory)).Read()), Is.True);
        }

        [Test] public void ALegacySaveFromBeforeThePrologueExistedHasNoSetupToKeep()
        {
            Open();
            var state = new ChapterOneSim().S; state.prologue = 0; state.appInstallState = 0;
            Assert.That(new AtomicSaveStore(directory).TrySave(JsonUtility.ToJson(state), out _), Is.True);
            Open();
            Assert.That(runtime.OpeningProfile(), Is.Null);
            Assert.That(File.Exists(ProfilePath), Is.False);
        }

        static string ReadPayload(string path)
        {
            Assert.That(AtomicSaveStore.TryReadFile(path, out string payload), Is.True);
            return payload;
        }
    }
}
