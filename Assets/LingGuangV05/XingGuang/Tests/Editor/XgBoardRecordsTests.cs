using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgBoardRecordsTests
    {
        static XgSim Sim(bool recording=false){var s=new XgSim();s.S.stage=2;if(recording)s.S.unlocked.Add(XgBoardRecords.Node);return s;}
        static List<string> Trace(int n=40){var r=new List<string>();for(int i=0;i<n;i++)r.Add("fixture action "+i);return r;}
        static void Complete(XgSim s,bool ai=true,string game="ludo"){var r=s.BeginBoardSession(game,ai);Assert.NotNull(r);Assert.True(s.FinishBoardSession(r,XgGameOutcome.PlayerWon,40,Trace()));}
        [Test] public void OpponentAndRecorderHaveSeparateGates()
        {
            var s=new XgSim();Assert.IsNull(s.BeginBoardSession("ludo",true));Assert.NotNull(s.BeginBoardSession("ludo",false));
            s.S.stage=2;Complete(s);Assert.True(s.DailyProgress("game1").complete);Assert.IsNull(s.BoardRecordsBlocker(XgBoardRecords.Node));Assert.IsEmpty(s.S.dataFiles);
        }
        [Test] public void PurchaseMidGameDoesNotRetroactivelyRecordTheGame()
        {
            var s=Sim();var r=s.BeginBoardSession("ludo",true);s.S.unlocked.Add(XgBoardRecords.Node);
            Assert.True(s.FinishBoardSession(r,XgGameOutcome.PlayerWon,40,Trace()));Complete(s);Complete(s);
            Assert.IsEmpty(s.S.dataFiles);Complete(s);Assert.AreEqual(1,s.S.dataFiles.Count);
        }
        [Test] public void AbandonedDuplicateAndForeignSessionsCannotFinish()
        {
            var s=Sim(true);var r=s.BeginBoardSession("ludo",true);Assert.False(Sim(true).FinishBoardSession(r,XgGameOutcome.PlayerWon,40,Trace()));
            s.AbandonBoardSession(r);Assert.False(s.FinishBoardSession(r,XgGameOutcome.PlayerWon,40,Trace()));
            r=s.BeginBoardSession("ludo",true);Assert.True(s.FinishBoardSession(r,XgGameOutcome.PlayerWon,40,Trace()));Assert.False(s.FinishBoardSession(r,XgGameOutcome.PlayerWon,40,Trace()));
            Assert.AreEqual(1,s.DailyProgress("games3").count);
        }
        [Test] public void ComputerGamesNeverProduceTrainingPacksOrAiReceipts()
        {
            var s=Sim(true);for(int i=0;i<6;i++)Complete(s,false);
            Assert.IsEmpty(s.S.dataFiles);Assert.AreEqual(0,s.DailyProgress("games3").count);Assert.AreEqual(6,s.S.boardRounds.Count);
        }
        [Test] public void ThreePostPurchaseGamesCreateOnePackThenInstallExactlyOnce()
        {
            var s=Sim(true);double before=s.Labels("logic");Complete(s);Complete(s);Assert.IsEmpty(s.S.dataFiles);Complete(s);
            Assert.AreEqual(before,s.Labels("logic"));Assert.AreEqual(1,s.S.dataFiles.Count);var file=s.S.dataFiles[0];Assert.AreEqual(90,file.samples);
            Assert.False(s.InstallDataFile(file.fileId));s.S.unlocked.Add(XgSim.DataEntryNode);
            Assert.True(s.InstallDataFile(file.fileId));Assert.AreEqual(before+90,s.Labels("logic"));Assert.False(s.InstallDataFile(file.fileId));
            Assert.AreEqual(1,s.LearningRecordsNewest().Count(r=>r.sourceKind=="game"));
        }
        [Test] public void ReloadPreservesReadyPackAndInstalledPackNeverPaysAgain()
        {
            var s=Sim(true);s.S.unlocked.Add(XgSim.DataEntryNode);for(int i=0;i<3;i++)Complete(s);
            var loaded=new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(s.S)));string id=s.S.dataFiles[0].fileId;
            Assert.True(loaded.DataFileReady(id));Assert.True(loaded.InstallDataFile(id));double labels=loaded.Labels("logic");
            var again=new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(loaded.S)));Assert.False(again.InstallDataFile(id));Assert.AreEqual(labels,again.Labels("logic"));
        }
        [Test] public void LegacyAggregatesAndSamplesRemainButNeverBecomeNewPacks()
        {
            var s=Sim(true);s.S.games.Add(new XgGameRecord{game="ludo",played=999,samples=999});
            var loaded=new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(s.S)));
            Assert.IsEmpty(loaded.S.dataFiles);Assert.AreEqual(999,loaded.GameRecord("ludo").samples);Assert.AreEqual(0,loaded.DailyProgress("games3").count);
        }
        [Test] public void PackSamplesNeverExceedTheActualSuppliedTrace()
        {
            var s=Sim(true);for(int i=0;i<3;i++){var r=s.BeginBoardSession("ludo",true);Assert.True(s.FinishBoardSession(r,XgGameOutcome.PlayerWon,2,Trace(2)));}
            Assert.AreEqual(6,s.S.dataFiles[0].samples);
        }
    }
}
