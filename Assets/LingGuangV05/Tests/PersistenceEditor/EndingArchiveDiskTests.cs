using System;
using System.IO;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using LingGuangV05.Runtime.Persistence;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LingGuangV05.Tests
{
    // Isolated filesystem fixtures. No model, game-clock advancement, production save or completion fabrication.
    public sealed class EndingArchiveDiskTests
    {
        string directory, previousVerification;
        GameObject root;
        ChapterOneRuntime runtime;
        Func<string, bool> previousLab;
        static bool PreChoice(string json) => json == "asked";
        [SetUp] public void Setup()
        {
            directory = Path.Combine(Path.GetTempPath(), "lingguang-ending-archive-tests-" + Guid.NewGuid().ToString("N"));
            previousVerification = ChapterOneRuntime.VerificationSaveDirectory; ChapterOneRuntime.VerificationSaveDirectory = null;
            previousLab = ChapterOneRuntime.LabStateReadable; ChapterOneRuntime.LabStateReadable = _ => true;
            Open();
        }
        void Open()
        {
            if (root != null) Object.DestroyImmediate(root);
            root = new GameObject("IsolatedEndingArchive"); root.SetActive(false);
            runtime = root.AddComponent<ChapterOneRuntime>(); runtime.SaveDirectoryOverride = directory;
            runtime.ClockOverride = () => 1000; runtime.EnsureInitialized();
            if (runtime.Sim.InPrologue)
            {
                var profile = PrologueProfile.Read("嘴硬心软"); profile.name = "测试"; profile.self = "我"; profile.callMe = "你";
                Assert.IsTrue(runtime.CompletePrologue(profile), runtime.SaveStatus);
            }
        }
        [TearDown] public void Cleanup()
        {
            if (root != null) Object.DestroyImmediate(root);
            ChapterOneRuntime.VerificationSaveDirectory = previousVerification; ChapterOneRuntime.LabStateReadable = previousLab;
            // The fixture owns this freshly generated absolute temp directory only.
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
        string Checkpoint()
        {
            runtime.Sim.S.labState = "asked";
            Assert.IsTrue(runtime.CaptureEndingChoice(runtime.Sim, PreChoice, out var id), runtime.SaveStatus);
            runtime.Sim.S.endingCheckpointId = id;
            return id;
        }
        string CheckpointFolder(string id) => Path.Combine(directory, "endings", "checkpoints", id);
        byte[] Primary() => File.ReadAllBytes(Path.Combine(directory, "chapter-one.sav"));
        void AssertUnchanged(ChapterOneSim identity, string path, byte[] disk)
        {
            Assert.AreSame(identity, runtime.Sim); Assert.AreEqual(path, runtime.SavePath);
            CollectionAssert.AreEqual(disk, Primary());
        }
        [Test] public void CaptureCollectsAppsWithoutWritingPrimaryOrClaimingAnAssociation()
        {
            var disk = Primary(); var save = runtime.Sim; string path = runtime.SavePath;
            Action collect = () => { runtime.Sim.S.labState = "asked"; runtime.Sim.S.chatState = "collected history"; };
            runtime.Saving += collect;
            Assert.IsTrue(runtime.CaptureEndingChoice(save, PreChoice, out var id), runtime.SaveStatus);
            runtime.Saving -= collect;
            Assert.IsTrue(string.IsNullOrEmpty(save.S.endingCheckpointId));
            var stored = new AtomicSaveStore(CheckpointFolder(id)).Load(); Assert.IsTrue(stored.Success);
            var copy = JsonUtility.FromJson<GameState>(stored.Payload);
            Assert.AreEqual("asked", copy.labState); Assert.AreEqual("collected history", copy.chatState);
            Assert.AreEqual(id, copy.endingCheckpointId); AssertUnchanged(save, path, disk);
            Assert.IsFalse(Directory.Exists(Path.Combine(directory, "slots")));
        }
        [Test] public void EachChoiceGetsANewImmutableSnapshot()
        {
            string first = Checkpoint(); byte[] bytes = File.ReadAllBytes(Path.Combine(CheckpointFolder(first), "chapter-one.sav"));
            runtime.Sim.S.money += 41;
            string second = Checkpoint(); Assert.AreNotEqual(first, second);
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(Path.Combine(CheckpointFolder(first), "chapter-one.sav")));
        }
        [Test] public void FailedCollectionReturnsNoCheckpointAndPreservesPrimary()
        {
            var disk = Primary(); var save = runtime.Sim; string path = runtime.SavePath;
            Action fail = () => throw new InvalidOperationException("fixture app collection failure"); runtime.Saving += fail;
            Assert.IsFalse(runtime.CaptureEndingChoice(save, PreChoice, out var id)); runtime.Saving -= fail;
            Assert.IsNull(id); AssertUnchanged(save, path, disk);
        }
        [Test] public void FailedCheckpointDestinationPreservesIdentityAndPrimary()
        {
            var disk = Primary(); var save = runtime.Sim; string path = runtime.SavePath; save.S.labState = "asked";
            File.WriteAllText(Path.Combine(directory, "endings"), "fixture obstruction");
            Assert.IsFalse(runtime.CaptureEndingChoice(save, PreChoice, out var id)); Assert.IsNull(id); AssertUnchanged(save, path, disk);
        }
        [Test] public void SelectedRoutePayloadCannotBeCapturedOrForkedAsBeforeChoice()
        {
            runtime.Sim.S.labState = "already selected";
            Assert.IsFalse(runtime.CaptureEndingChoice(runtime.Sim, PreChoice, out _));
            string id = Checkpoint(); var copy = JsonUtility.FromJson<GameState>(new AtomicSaveStore(CheckpointFolder(id)).Load().Payload);
            copy.labState = "already selected";
            var forged = new AtomicSaveStore(CheckpointFolder(id)); Assert.IsTrue(forged.TrySaveSnapshot(JsonUtility.ToJson(copy), out _));
            // Remove the old valid backup in this test-owned directory, so this exercises the selected payload.
            File.Delete(forged.SavePath + ".bak");
            var disk = Primary(); var save = runtime.Sim; string path = runtime.SavePath;
            Assert.IsFalse(runtime.CreateEndingBranch(save, id, PreChoice)); AssertUnchanged(save, path, disk);
        }
        [Test] public void BranchWritesAndReloadsIndependentlyWhileSixManualSlotsStayIntact()
        {
            for (int slot = 2; slot <= 6; slot++) Assert.IsTrue(runtime.SaveSlot(slot));
            var slotBytes = new byte[5][];
            for (int slot = 2; slot <= 6; slot++) slotBytes[slot - 2] = File.ReadAllBytes(Path.Combine(directory, "slots", slot.ToString(), "chapter-one.sav"));
            string id = Checkpoint(); runtime.Sim.S.labState = "source completed"; Assert.IsTrue(runtime.SaveNow());
            var original = Primary(); var source = runtime.Sim;
            Assert.IsTrue(runtime.CreateEndingBranch(source, id, PreChoice), runtime.SaveStatus);
            Assert.AreNotSame(source, runtime.Sim); Assert.AreEqual("asked", runtime.Sim.S.labState);
            string branchId = runtime.Sim.S.endingBranchId; runtime.Sim.S.money = 321;
            Assert.IsTrue(runtime.SaveNow()); Assert.IsTrue(runtime.ReloadSave()); Assert.AreEqual(321, runtime.Sim.S.money);
            CollectionAssert.AreEqual(original, Primary());
            for (int slot = 2; slot <= 6; slot++) CollectionAssert.AreEqual(slotBytes[slot - 2], File.ReadAllBytes(Path.Combine(directory, "slots", slot.ToString(), "chapter-one.sav")));
            Open(); Assert.AreEqual("source completed", runtime.Sim.S.labState, "boot starts original primary");
            Assert.AreEqual(1, runtime.EndingBranches().Count);
            var beforeResume = Primary();
            Assert.IsTrue(runtime.ResumeEndingBranch(runtime.Sim, branchId)); Assert.AreEqual(321, runtime.Sim.S.money);
            CollectionAssert.AreEqual(beforeResume, Primary());
        }
        [Test] public void LargeBranchOwnsAllBlocksAndSurvivesMissingSourceBlocks()
        {
            runtime.Sim.S.chatState = new string('x', AtomicSaveStore.MaxPayloadBytes + 60000);
            string id = Checkpoint(); string checkpointFolder = CheckpointFolder(id);
            Assert.That(File.ReadAllText(Path.Combine(checkpointFolder, "chapter-one.sav")), Does.StartWith("LINGGUANG-CHAPTER1:2"));
            Assert.IsTrue(runtime.CreateEndingBranch(runtime.Sim, id, PreChoice), runtime.SaveStatus);
            string expected = runtime.Sim.S.chatState;
            Directory.Delete(Path.Combine(checkpointFolder, "snapshot-archive"), true);
            Assert.IsTrue(runtime.ReloadSave(), runtime.SaveStatus); Assert.AreEqual(expected, runtime.Sim.S.chatState);
        }
        [Test] public void MissingSourceBlockRefusesBranchAndPreservesOriginal()
        {
            runtime.Sim.S.chatState = new string('z', AtomicSaveStore.MaxPayloadBytes + 60000); string id = Checkpoint();
            string objects = Path.Combine(CheckpointFolder(id), "snapshot-archive", "objects");
            File.Delete(Directory.GetFiles(objects, "*.chunk")[0]);
            var disk = Primary(); var save = runtime.Sim; string path = runtime.SavePath;
            Assert.IsFalse(runtime.HasEndingChoice(save, PreChoice)); Assert.IsFalse(runtime.CreateEndingBranch(save, id, PreChoice));
            AssertUnchanged(save, path, disk); Assert.AreEqual(0, runtime.EndingBranches().Count);
        }
        [Test] public void FailedBranchWriteKeepsOriginalStoreAndProfile()
        {
            string id = Checkpoint(); var disk = Primary(); var save = runtime.Sim; string path = runtime.SavePath;
            string profilePath = Path.Combine(directory, PlayerProfileStore.FileName); var profile = File.ReadAllBytes(profilePath);
            File.WriteAllText(Path.Combine(directory, "endings", "branches"), "fixture obstruction");
            Assert.IsFalse(runtime.CreateEndingBranch(save, id, PreChoice)); AssertUnchanged(save, path, disk);
            CollectionAssert.AreEqual(profile, File.ReadAllBytes(profilePath));
        }
        [Test] public void RebindFailureRollsBackIdentityAndBlocksReentrantSave()
        {
            string id = Checkpoint(); var disk = Primary(); var save = runtime.Sim; string path = runtime.SavePath;
            bool reentrant = true; Action fail = () => { reentrant = runtime.SaveNow(); throw new InvalidOperationException("fixture rebind failure"); };
            runtime.Changed += fail; Assert.IsFalse(runtime.CreateEndingBranch(save, id, PreChoice)); runtime.Changed -= fail;
            Assert.IsFalse(reentrant); AssertUnchanged(save, path, disk); Assert.AreEqual(0, runtime.EndingBranches().Count);
        }
        [Test] public void StaleIdentityCannotCreateOrResumeBranch()
        {
            string id = Checkpoint(); var stale = runtime.Sim; Assert.IsTrue(runtime.SaveNow()); Assert.IsTrue(runtime.ReloadSave());
            var disk = Primary(); var save = runtime.Sim; string path = runtime.SavePath;
            Assert.IsFalse(runtime.CreateEndingBranch(stale, id, PreChoice));
            Assert.IsFalse(runtime.ResumeEndingBranch(stale, Guid.NewGuid().ToString("N"))); AssertUnchanged(save, path, disk);
        }
        [Test] public void MissingCheckpointDoesNotManufactureOneForOlderSave()
        {
            var disk = Primary(); var save = runtime.Sim; string path = runtime.SavePath;
            Assert.IsFalse(runtime.HasEndingChoice(save, PreChoice)); Assert.IsFalse(runtime.CreateEndingBranch(save, "", PreChoice));
            AssertUnchanged(save, path, disk); Assert.IsFalse(Directory.Exists(Path.Combine(directory, "endings")));
        }
        [TestCase("../other")]
        [TestCase("..\\other")]
        [TestCase("C:\\another-save")]
        public void BranchIdCannotEscapeItsDirectory(string id)
        {
            var disk = Primary(); var save = runtime.Sim; string path = runtime.SavePath;
            Assert.IsFalse(runtime.ResumeEndingBranch(save, id)); AssertUnchanged(save, path, disk);
        }
        [Test] public void PreOpeningLegacyStateCanBranchWithoutInventingPlayerProfile()
        {
            runtime.Sim.S.prologue = 0;
            File.Delete(Path.Combine(directory, PlayerProfileStore.FileName));
            string id = Checkpoint(); Assert.IsTrue(runtime.CreateEndingBranch(runtime.Sim, id, PreChoice), runtime.SaveStatus);
            Assert.AreEqual(0, runtime.Sim.S.prologue);
            Assert.IsFalse(File.Exists(Path.Combine(Path.GetDirectoryName(runtime.SavePath), PlayerProfileStore.FileName)));
            Assert.AreEqual(1, runtime.EndingBranches().Count);
        }
    }
}
