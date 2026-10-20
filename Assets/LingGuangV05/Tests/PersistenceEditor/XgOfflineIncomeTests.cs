using System;
using System.IO;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using LingGuangV05.Runtime.Persistence;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace LingGuangV05.Tests
{
    public sealed class XgOfflineIncomeTests
    {
        string directory;
        long now;
        GameObject root;
        ChapterOneRuntime runtime;
        [SetUp] public void Setup()
        {
            directory=Path.Combine(Path.GetTempPath(),"xg-offline-tests-"+Guid.NewGuid().ToString("N"));
            now=1000;Open();
        }
        [TearDown] public void Cleanup()
        {
            if(root!=null)Object.DestroyImmediate(root);
            if(Directory.Exists(directory))Directory.Delete(directory,true);
        }
        void Open()
        {
            if(root!=null)Object.DestroyImmediate(root);
            root=new GameObject("OfflineIncomeIsolatedTest");root.SetActive(false);
            runtime=root.AddComponent<ChapterOneRuntime>();
            runtime.useDiskSave=true;runtime.ClockOverride=()=>now;runtime.SaveDirectoryOverride=directory;
            runtime.OfflineIncomeQuoteProvider=()=>new XgOfflineQuote{unlocked=true,eligible=true,incomePerSecond=2};
            runtime.EnsureInitialized();
            if(runtime.Sim.InPrologue)
            {
                var profile=PrologueProfile.Read("嘴硬心软");profile.name="测试";profile.self="我";profile.callMe="你";
                Assert.IsTrue(runtime.CompletePrologue(profile),runtime.SaveStatus);
            }
        }
        AtomicSaveStore ReceiptStore()=>new AtomicSaveStore(Path.Combine(directory,"offline-income",runtime.Sim.S.offlineIncome.lineage));

        [Test] public void DefaultIsOffAndLegacyTimeDoesNotEarnAnything()
        {
            Assert.IsFalse(runtime.Sim.S.offlineIncome.enabled);
            double balance=runtime.Sim.S.money;now+=86400;Open();
            Assert.AreEqual(balance,runtime.Sim.S.money);Assert.AreEqual(0,runtime.OfflineIncome);
        }
        [Test] public void EnabledSettlementIsHalfRateAndChangesOnlyFunds()
        {
            Assert.IsTrue(runtime.SetOfflineIncomeEnabled(true));
            var before=JsonUtility.FromJson<GameState>(JsonUtility.ToJson(runtime.Sim.S));
            now+=600;Open();
            Assert.AreEqual(600,runtime.OfflineIncome);
            Assert.AreEqual(before.money+600,runtime.Sim.S.money);
            Assert.AreEqual(before.samples,runtime.Sim.S.samples);
            Assert.AreEqual(before.steps,runtime.Sim.S.steps);
            Assert.AreEqual(before.day,runtime.Sim.S.day);
            Assert.AreEqual(before.gameSeconds,runtime.Sim.S.gameSeconds);
            Assert.AreEqual(before.labState,runtime.Sim.S.labState);
        }
        [Test] public void EightHourCapConsumesTheWholeInterval()
        {
            Assert.IsTrue(runtime.SetOfflineIncomeEnabled(true));now+=86400;Open();
            Assert.AreEqual(28800,runtime.OfflineIncome);
            double money=runtime.Sim.S.money;Open();
            Assert.AreEqual(0,runtime.OfflineIncome);Assert.AreEqual(money,runtime.Sim.S.money);
        }
        [Test] public void ReloadDuringTheSameSessionCannotEarnForOnlineTime()
        {
            Assert.IsTrue(runtime.SetOfflineIncomeEnabled(true));now+=600;
            Assert.IsTrue(runtime.ReloadSave());Assert.AreEqual(0,runtime.OfflineIncome);
        }
        [Test] public void InterruptedWalletSaveRecoversTheSameReceiptOnlyOnce()
        {
            Assert.IsTrue(runtime.SetOfflineIncomeEnabled(true));double before=runtime.Sim.S.money;
            var receipt=new XgOfflineReceipt{lineage=runtime.Sim.S.offlineIncome.lineage,serial=1,watermark=1200,amount=200,seconds=200};
            Assert.IsTrue(ReceiptStore().TrySave(JsonUtility.ToJson(receipt),out _));
            now=1200;Open();Assert.AreEqual(before+200,runtime.Sim.S.money);
            Open();Assert.AreEqual(before+200,runtime.Sim.S.money);Assert.AreEqual(0,runtime.OfflineIncome);
        }
        [Test] public void LoadingAnOlderManualSlotDoesNotReplayAConsumedReceipt()
        {
            Assert.IsTrue(runtime.SetOfflineIncomeEnabled(true));Assert.IsTrue(runtime.SaveSlot(2));
            double before=runtime.Sim.S.money;now+=600;Open();Assert.AreEqual(before+600,runtime.Sim.S.money);
            Assert.IsTrue(runtime.LoadArchive(2));Assert.AreEqual(before,runtime.Sim.S.money);
            Open();Assert.AreEqual(before,runtime.Sim.S.money);Assert.AreEqual(0,runtime.OfflineIncome);
        }
        [Test] public void ClockRollbackNeverRewindsTheOfflineWatermark()
        {
            Assert.IsTrue(runtime.SetOfflineIncomeEnabled(true));double before=runtime.Sim.S.money;
            now=900;Open();Assert.AreEqual(0,runtime.OfflineIncome);
            now=1100;Open();Assert.AreEqual(before+100,runtime.Sim.S.money);
        }
        [Test] public void CorruptLedgerDisablesSettlementWithoutDiscardingGameProgress()
        {
            Assert.IsTrue(runtime.SetOfflineIncomeEnabled(true));double before=runtime.Sim.S.money;
            var receipts=ReceiptStore();Directory.CreateDirectory(receipts.Folder);
            File.WriteAllText(receipts.SavePath,"corrupt");File.WriteAllText(receipts.SavePath+".bak","corrupt");
            now+=600;Open();Assert.AreEqual(before,runtime.Sim.S.money);
            Assert.IsNotEmpty(runtime.OfflineIncomeStatus);Assert.AreEqual(0,runtime.OfflineIncome);
        }
        [Test] public void PausedOrIneligibleSnapshotCannotEarn()
        {
            Assert.IsTrue(runtime.SetOfflineIncomeEnabled(true));
            runtime.OfflineIncomeQuoteProvider=()=>new XgOfflineQuote{unlocked=true,eligible=false,incomePerSecond=2};
            Assert.IsTrue(runtime.SaveNow());double before=runtime.Sim.S.money;
            now+=600;Open();Assert.AreEqual(before,runtime.Sim.S.money);Assert.AreEqual(0,runtime.OfflineIncome);
        }
        [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)] [TestCase(-1)]
        public void InvalidRatesNeverCreateMoney(double rate)
        { Assert.AreEqual(0,XgOfflineRules.Estimate(rate,600)); }
    }
}
