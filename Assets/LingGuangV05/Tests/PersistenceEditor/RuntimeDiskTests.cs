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
    public sealed class RuntimeDiskTests
    {
        string directory;
        long clock;
        GameObject root;
        ChapterOneRuntime runtime;
        [SetUp] public void Setup()
        {
            directory=Path.Combine(Path.GetTempPath(),"lingguang-runtime-tests-"+Guid.NewGuid().ToString("N"));
            clock=1000;
            OpenRuntime();
        }
        [TearDown] public void Cleanup()
        {
            if(root!=null) Object.DestroyImmediate(root);
            if(Directory.Exists(directory)) Directory.Delete(directory,true);
        }
        void OpenRuntime(bool finishPrologue=true)
        {
            if(root!=null) Object.DestroyImmediate(root);
            root=new GameObject("IsolatedRuntimeDiskTest"); root.SetActive(false);
            runtime=root.AddComponent<ChapterOneRuntime>();
            runtime.useDiskSave=true;
            runtime.ClockOverride=()=>clock;
            runtime.SaveDirectoryOverride=directory;
            runtime.EnsureInitialized();
            // A new game is in the prologue, which is never saved; these tests start from the first real save.
            if(finishPrologue&&runtime.Sim.InPrologue)
            {
                var profile=PrologueProfile.Read("嘴硬心软"); profile.name="测试"; profile.self="我"; profile.callMe="你";
                Assert.That(runtime.CompletePrologue(profile),Is.True,runtime.SaveStatus);
            }
        }
        [Test] public void ThePrologueIsNeverWrittenToDiskAndTheSetupStartsTheSave()
        {
            Cleanup(); directory=Path.Combine(Path.GetTempPath(),"lingguang-runtime-tests-"+Guid.NewGuid().ToString("N"));
            OpenRuntime(false);
            Assert.That(runtime.Sim.InPrologue,Is.True);
            runtime.Sim.InstallApp();
            Assert.That(runtime.SaveNow(),Is.True);
            Assert.That(Directory.Exists(directory)&&Directory.GetFiles(directory,"*",SearchOption.AllDirectories).Length>0,Is.False,"nothing written during the prologue");
            OpenRuntime(false);
            Assert.That(runtime.Sim.InPrologue,Is.True,"quitting in the prologue starts it over");
            Assert.That(runtime.Sim.AppInstalled,Is.False);
            var profile=PrologueProfile.Read("爱吐槽"); profile.name="小灯"; profile.self="本机"; profile.callMe="老大";
            Assert.That(runtime.CompletePrologue(profile),Is.True,runtime.SaveStatus);
            OpenRuntime(false);
            Assert.That(runtime.Sim.InPrologue,Is.False);
            Assert.That(runtime.Sim.S.aiName,Is.EqualTo("小灯"));
            Assert.That(runtime.Sim.S.aiCallMe,Is.EqualTo("老大"));
        }
        [Test] public void RealReloadRestoresSaveNotUnsavedPurchases()
        {
            runtime.Sim.S.money = runtime.Sim.Config.gpuPrice;
            Assert.That(runtime.SaveNow(),Is.True,runtime.SaveStatus);
            double balance=runtime.Sim.S.money;
            Assert.That(runtime.Sim.BuyGpu(),Is.True);
            Assert.That(runtime.ReloadSave(),Is.True,runtime.SaveStatus);
            Assert.That(runtime.Sim.S.money,Is.EqualTo(balance));
            Assert.That(runtime.Sim.S.gpuCount,Is.EqualTo(1));
        }
        [Test] public void OfflineStartupSettlesDaysAndIncomeOnlyOnce()
        {
            runtime.Sim.SetJobEnabled(true);
            Assert.That(runtime.SaveNow(),Is.True);
            var expected=new ChapterOneSim(JsonUtility.FromJson<GameState>(JsonUtility.ToJson(runtime.Sim.S)));
            expected.Tick(300);
            clock+=300; OpenRuntime();
            Assert.That(runtime.OfflineSeconds,Is.EqualTo(300));
            Assert.That(runtime.Sim.S.day,Is.EqualTo(expected.S.day));
            Assert.That(runtime.Sim.S.money,Is.EqualTo(expected.S.money).Within(1e-7));
            Assert.That(runtime.Sim.S.steps,Is.EqualTo(expected.S.steps));
            double balance=runtime.Sim.S.money, steps=runtime.Sim.S.steps;
            OpenRuntime();
            Assert.That(runtime.OfflineSeconds,Is.Zero);
            Assert.That(runtime.Sim.S.money,Is.EqualTo(balance));
            Assert.That(runtime.Sim.S.steps,Is.EqualTo(steps));
        }
        [Test] public void BackwardsUtcDoesNotRewindSavedAnchorOrGrantOfflineIncome()
        {
            runtime.Sim.SetJobEnabled(true); runtime.SaveNow();
            double balance=runtime.Sim.S.money;
            clock=900; OpenRuntime();
            Assert.That(runtime.OfflineSeconds,Is.Zero);
            Assert.That(runtime.Sim.S.lastSeenUnix,Is.EqualTo(1000));
            Assert.That(runtime.Sim.S.money,Is.EqualTo(balance));
        }
        [Test] public void LargeOfflineGapIsCappedAtThirtyMinutes()
        {
            runtime.SaveNow(); clock+=864000; OpenRuntime();
            Assert.That(runtime.OfflineSeconds,Is.EqualTo(1800));
            Assert.That(runtime.Sim.S.gameSeconds,Is.EqualTo(1800).Within(1e-6));
        }
        [Test] public void MalformedJsonWithGoodEnvelopeRecoversPreviousValidPayload()
        {
            runtime.Sim.S.money = 123.45;
            runtime.SaveNow();
            var store=new AtomicSaveStore(directory);
            Assert.That(store.TrySave("{}",out _),Is.True);
            OpenRuntime();
            Assert.That(runtime.SaveStatus,Does.Contain("备份"));
            Assert.That(runtime.Sim.S.money,Is.EqualTo(123.45));
            Assert.That(runtime.SaveNow(),Is.True);
        }
        [Test] public void FutureStateSchemaProtectsFileUntilExplicitReset()
        {
            runtime.SaveNow();
            var state=runtime.Sim.S; state.version=99;
            var store=new AtomicSaveStore(directory);
            store.TrySave(JsonUtility.ToJson(state),out _);
            string future=File.ReadAllText(store.SavePath);
            OpenRuntime(false); // the protected save is not replaced, not even by a finished prologue
            Assert.That(runtime.SaveNow(),Is.False);
            Assert.That(File.ReadAllText(store.SavePath),Is.EqualTo(future));
            Assert.That(runtime.ResetProgress(),Is.True);
            Assert.That(Directory.GetFiles(directory,"*.reset-*").Length,Is.GreaterThan(0));
            Assert.That(runtime.Sim.S.version,Is.EqualTo(1));
        }
    }
}
