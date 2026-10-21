using System;
using System.IO;
using LingGuangV05.Core;
using LingGuangV05.Runtime.Persistence;
using NUnit.Framework;
using UnityEngine;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgNarrationReceiptTests
    {
        string folder;
        [SetUp] public void SetUp(){folder=Path.Combine(Path.GetTempPath(),"LingGuang-Narration-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);}
        [TearDown] public void TearDown(){if(Directory.Exists(folder))Directory.Delete(folder,true);}

        [Test] public void OlderDiskSnapshotInSameRunKeepsHeardReceiptsButNewRunStillHasFirsts()
        {
            var house=new GameState{narrationRunId=Guid.NewGuid().ToString("N")};
            var sim=new XgSim();sim.S.pendingAnswerMoneyThoughts.AddRange(new[]{"pay","fine"});
            sim.S.answerPayThoughtRecorded=sim.S.answerFineThoughtRecorded=true;
            sim.QueueAfterthought("first.income");sim.QueueAfterthought("first.fine");
            house.labState=JsonUtility.ToJson(sim.S);
            var slot=new AtomicSaveStore(Path.Combine(folder,"old-slot"));
            Assert.IsTrue(slot.TrySave(JsonUtility.ToJson(house),out _));
            Assert.IsTrue(sim.TakeTeachingThought(true,()=>NarrationReceiptStore.Merge(folder,house.narrationRunId,sim.S,out _)).HasValue);
            Assert.IsTrue(sim.TakeTeachingThought(true,()=>NarrationReceiptStore.Merge(folder,house.narrationRunId,sim.S,out _)).HasValue);
            foreach(string key in new[]{"first.income","first.fine"})
                Assert.IsTrue(sim.CommitAfterthought(key,()=>NarrationReceiptStore.Merge(folder,house.narrationRunId,sim.S,out _)));
            for(int reload=0;reload<2;reload++)
            {
                var disk=slot.Load(_=>true);Assert.IsTrue(disk.Success);
                var old=JsonUtility.FromJson<GameState>(disk.Payload);
                var restored=new XgSim(JsonUtility.FromJson<XgState>(old.labState));
                Assert.IsTrue(NarrationReceiptStore.Merge(folder,old.narrationRunId,restored.S,out _));
                Assert.IsNull(restored.TakeTeachingThought(true));
                Assert.IsNull(restored.TakeAfterthought("first.income"));Assert.IsNull(restored.TakeAfterthought("first.fine"));
                Assert.IsEmpty(restored.S.pendingAfterthoughts);Assert.IsEmpty(restored.S.pendingAnswerMoneyThoughts);
                Assert.AreEqual(house.labState,old.labState,"loading receipts never deletes or rewrites the historical snapshot");
            }
            var fresh=new XgSim();fresh.S.pendingAnswerMoneyThoughts.Add("pay");
            Assert.IsTrue(NarrationReceiptStore.Merge(folder,Guid.NewGuid().ToString("N"),fresh.S,out _));
            Assert.IsTrue(fresh.TakeTeachingThought(true).HasValue);
            Assert.IsNotNull(fresh.TakeAfterthought("first.income"));
        }

        [Test] public void FailedReceiptWriteKeepsBothManualAndAutomaticThoughtsPending()
        {
            string id=Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(Path.Combine(folder,"narration",id,"chapter-one.sav")); // Deliberate unwritable file destination.
            var sim=new XgSim();sim.S.pendingAnswerMoneyThoughts.Add("pay");sim.S.answerPayThoughtRecorded=true;
            sim.QueueAfterthought("first.fine");
            Assert.IsNull(sim.TakeTeachingThought(true,()=>NarrationReceiptStore.Merge(folder,id,sim.S,out _)));
            Assert.IsFalse(sim.S.answerPayThoughtConsumed);CollectionAssert.AreEqual(new[]{"pay"},sim.S.pendingAnswerMoneyThoughts);
            Assert.IsFalse(sim.CommitAfterthought("first.fine",()=>NarrationReceiptStore.Merge(folder,id,sim.S,out _)));
            Assert.IsFalse(sim.AfterthoughtSaid("first.fine"));Assert.AreEqual("first.fine",sim.S.pendingAfterthoughts[0].key);
        }

        [Test] public void PendingAutomaticThoughtArgumentsSurviveActualJsonReload()
        {
            var sim=new XgSim();sim.QueueAfterthought("first.drift","洪荒之力","primordial power");
            var store=new AtomicSaveStore(folder);Assert.IsTrue(store.TrySave(JsonUtility.ToJson(sim.S),out _));
            var copy=new XgSim(JsonUtility.FromJson<XgState>(store.Load(_=>true).Payload));
            Assert.AreEqual(1,copy.S.pendingAfterthoughts.Count);
            StringAssert.Contains("洪荒之力",copy.S.pendingAfterthoughts[0].zh);
            StringAssert.Contains("primordial power",copy.S.pendingAfterthoughts[0].en);
            Assert.IsFalse(copy.S.answerPayThoughtRecorded||copy.S.answerFineThoughtRecorded);
        }

        [Test] public void DamagedPrimaryDoesNotRollHeardHistoryBackToOlderBackup()
        {
            string id=Guid.NewGuid().ToString("N");
            var sim=new XgSim();sim.MarkAfterthought("first.income");
            Assert.IsTrue(NarrationReceiptStore.Merge(folder,id,sim.S,out _));
            sim.MarkAfterthought("first.fine");
            Assert.IsTrue(NarrationReceiptStore.Merge(folder,id,sim.S,out _));
            string path=Path.Combine(folder,"narration",id,"chapter-one.sav");
            Assert.IsTrue(File.Exists(path+".bak"));
            File.WriteAllText(path,"damaged primary");
            var restored=new XgSim();restored.QueueAfterthought("first.fine");
            Assert.IsFalse(NarrationReceiptStore.Merge(folder,id,restored.S,out string error));
            Assert.IsNotEmpty(error);Assert.AreEqual("damaged primary",File.ReadAllText(path));
            Assert.AreEqual(1,restored.S.pendingAfterthoughts.Count);
        }

        [Test] public void ReflectionWaitsForReceiptAndDoesNotConsumeOnWriteFailure()
        {
            string id=Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(Path.Combine(folder,"narration",id,"chapter-one.sav"));
            var sim=new XgSim();sim.S.stage=5;sim.S.autoCorrect=XgSim.ReflectionLabels;
            Assert.IsNull(sim.TakeReflectionLine(()=>NarrationReceiptStore.Merge(folder,id,sim.S,out _)));
            Assert.IsFalse(sim.AfterthoughtSaid(XgSim.ReflectionKey));
            string working=Guid.NewGuid().ToString("N");
            Assert.IsNotNull(sim.TakeReflectionLine(()=>NarrationReceiptStore.Merge(folder,working,sim.S,out _)));
            var restored=new XgSim();restored.S.stage=5;restored.S.autoCorrect=XgSim.ReflectionLabels;
            Assert.IsTrue(NarrationReceiptStore.Merge(folder,working,restored.S,out _));
            Assert.IsNull(restored.TakeReflectionLine());
        }

        [Test] public void LegacyIdentityUsesExistingLineageOrExactSnapshotWithoutGuessingRelationships()
        {
            string lineage=Guid.NewGuid().ToString("N");
            var a=new GameState();a.offlineIncome.lineage=lineage;
            var b=new GameState{day=50};b.offlineIncome.lineage=lineage;
            NarrationReceiptStore.EnsureIdentity(a);NarrationReceiptStore.EnsureIdentity(b);
            Assert.AreEqual(lineage,a.narrationRunId);Assert.AreEqual(a.narrationRunId,b.narrationRunId);
            var old=new GameState();var same=new GameState();var different=new GameState();
            NarrationReceiptStore.EnsureIdentity(old,"old snapshot A");NarrationReceiptStore.EnsureIdentity(same,"old snapshot A");
            NarrationReceiptStore.EnsureIdentity(different,"old snapshot B");
            Assert.AreEqual(old.narrationRunId,same.narrationRunId);Assert.AreNotEqual(old.narrationRunId,different.narrationRunId);
            string stable=old.narrationRunId;old.day=99;NarrationReceiptStore.EnsureIdentity(old,"later snapshot");
            Assert.AreEqual(stable,old.narrationRunId,"once migrated, later snapshots retain the same playthrough id");
        }
    }
}
