using System;
using System.IO;
using NUnit.Framework;
using LingGuangV05.Runtime.Persistence;

namespace LingGuangV05.Tests
{
    public sealed class SaveStoreTests
    {
        string directory;
        AtomicSaveStore store;
        [SetUp] public void Setup() { directory=Path.Combine(Path.GetTempPath(), "lingguang-save-tests-"+Guid.NewGuid().ToString("N")); store=new AtomicSaveStore(directory); }
        [TearDown] public void Cleanup() { if(Directory.Exists(directory)) Directory.Delete(directory,true); }
        [Test] public void MissingSaveIsANewGameWithoutWritingFiles() { var r=store.Load(); Assert.That(r.Success,Is.False); Assert.That(r.WriteBlocked,Is.False); Assert.That(Directory.Exists(directory),Is.False); }
        [Test] public void ChinesePayloadRoundTripsExactly() { const string data="{\"aiName\":\"灵光\",\"money\":123.45}"; Assert.That(store.TrySave(data,out var message),Is.True,message); var r=store.Load(); Assert.That(r.Success,Is.True,r.Message); Assert.That(r.Payload,Is.EqualTo(data)); }
        [Test] public void SecondSaveRetainsPreviousGoodBackup() { Assert.That(store.TrySave("{\"v\":1}",out _),Is.True); Assert.That(store.TrySave("{\"v\":2}",out _),Is.True); File.WriteAllText(store.SavePath,"damaged"); var r=store.Load(); Assert.That(r.Success,Is.True); Assert.That(r.Recovered,Is.True); Assert.That(r.Payload,Is.EqualTo("{\"v\":1}")); }
        [Test] public void SavingAfterRecoveryDoesNotReplaceGoodBackupWithCorruption() { store.TrySave("{\"v\":1}",out _); store.TrySave("{\"v\":2}",out _); Directory.CreateDirectory(directory); File.WriteAllText(store.SavePath,"damaged"); store.Load(); Assert.That(store.TrySave("{\"v\":3}",out _),Is.True); File.WriteAllText(store.SavePath,"damaged again"); Assert.That(store.Load().Payload,Is.EqualTo("{\"v\":1}")); }
        [Test] public void BothCorruptSavesBlockAutomaticOverwrite() { Directory.CreateDirectory(directory); File.WriteAllText(store.SavePath,"not a save"); var r=store.Load(); Assert.That(r.Success,Is.False); Assert.That(r.WriteBlocked,Is.True); Assert.That(store.TrySave("{}",out _),Is.False); Assert.That(File.ReadAllText(store.SavePath),Is.EqualTo("not a save")); }
        [Test] public void HashTamperingIsRejected() { store.TrySave("{\"money\":1}",out _); Directory.CreateDirectory(directory); if(File.Exists(store.SavePath)) File.WriteAllText(store.SavePath,File.ReadAllText(store.SavePath).Replace("\"money\":1","\"money\":9")); Assert.That(store.Load().Success,Is.False); }
        [Test] public void FutureFormatDoesNotRollbackToBackup() { store.TrySave("{}",out _); store.TrySave("{}",out _); Directory.CreateDirectory(directory); File.WriteAllText(store.SavePath,"LINGGUANG-CHAPTER1:99\nfuture\ndata"); var r=store.Load(); Assert.That(r.Success,Is.False); Assert.That(r.WriteBlocked,Is.True); }
        [Test] public void OversizedAndEmptyPayloadsAreRejected() { Assert.That(store.TrySave("",out _),Is.False); Assert.That(store.TrySave(null,out _),Is.False); Assert.That(store.TrySave(new string('x',AtomicSaveStore.MaxPayloadBytes+1),out _),Is.False); Assert.That(File.Exists(store.SavePath),Is.False); }
        [Test] public void TemporaryFileIsNotLoadedAfterInterruptedFirstSave() { Directory.CreateDirectory(directory); File.WriteAllText(store.SavePath+".tmp","incomplete"); Assert.That(store.Load().Success,Is.False); Assert.That(store.Load().WriteBlocked,Is.False); }
        [Test] public void SaveFailureDoesNotThrow() { Directory.CreateDirectory(directory); Directory.CreateDirectory(store.SavePath); Assert.DoesNotThrow(()=>store.TrySave("{}",out _)); Assert.That(store.TrySave("{}",out _),Is.False); }
        [Test] public void InvalidPayloadWithValidHashFallsBackToValidatedBackup() { store.TrySave("{\"valid\":true}",out _); store.TrySave("{}",out _); var result=store.Load(payload=>payload.Contains("valid")); Assert.That(result.Success,Is.True); Assert.That(result.Recovered,Is.True); Assert.That(result.Payload,Does.Contain("valid")); }
        [Test] public void ConfirmedResetArchivesRatherThanDeletingOriginal() { store.TrySave("{\"progress\":10}",out _); Assert.That(store.ArchiveForReset(out _),Is.True); Assert.That(File.Exists(store.SavePath),Is.False); Assert.That(Directory.GetFiles(directory,"*.reset-*").Length,Is.EqualTo(1)); Assert.That(store.TrySave("{\"progress\":0}",out _),Is.True); }
    }
}
