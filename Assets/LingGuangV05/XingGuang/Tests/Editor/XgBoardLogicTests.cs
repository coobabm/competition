using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using LingGuangV05.Core.Games;
namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgBoardLogicTests
    {
        static XgSim Ready(){var s=new XgSim();s.S.stage=5;foreach(string id in new[]{"board.logic1","board.logic2","board.logic3","board.logic4","gamedata"})s.S.unlocked.Add(id);return s;}
        [Test] public void NodesHaveApprovedPricesStagesAndNoMainlineDependents()
        {
            int[] prices={800,2500,6000,15000};for(int i=1;i<=4;i++){var n=XgCatalog.Node("board.logic"+i);Assert.NotNull(n);Assert.AreEqual(i+1,n.stage);Assert.AreEqual(prices[i-1],n.cost);Assert.AreEqual("boardlogic",n.lane);}
            Assert.AreEqual(4,XgTechTree.EntryOf("board.logic1").members.Count);Assert.AreSame(XgTechTree.EntryOf("board.logic1"),XgTechTree.EntryOf("board.logic4"));
            Assert.AreEqual(1000,XgCatalog.Node("nextmove").cost);Assert.AreEqual(3,XgCatalog.Node("nextmove").stage);
            foreach(var n in XgCatalog.Nodes)if(!XgBoardLogic.IsNode(n.id))Assert.False((n.parent??"").StartsWith("board.logic")||n.needs.Any(x=>x.StartsWith("board.logic")),n.id);
        }
        [Test] public void ThirdPersonalGameClearsLevelFourGateAcrossAnyGameTypes()
        {
            var s=new XgSim();s.S.stage=5;Assert.NotNull(s.BoardLogicBlocker("board.logic4"));
            s.RecordDailyGameCompleted("go","one",false);Assert.AreEqual(0,s.DailyProgress("games3").count);
            s.RecordDailyGameCompleted("go","one",true);s.RecordDailyGameCompleted("ludo","two",true);Assert.NotNull(s.BoardLogicBlocker("board.logic4"));
            s.RecordDailyGameCompleted("chess","three",true);Assert.IsNull(s.BoardLogicBlocker("board.logic4"));
        }
        [Test] public void NextMoveDeskNeedsSeparatePurchaseAndStageThree()
        {
            var s=new XgSim();s.S.stage=3;s.S.unlocked.Add("board.logic1");Assert.False(s.TeachingPackAvailable("board_nextmove"));
            s.S.unlocked.Add("nextmove");Assert.True(s.TeachingPackAvailable("board_nextmove"));s.S.stage=2;Assert.False(s.TeachingPackAvailable("board_nextmove"));
        }
        [Test] public void NightPlayUsesLegalMovesAndDoesNotFarmPersonalReceiptsOrSamples()
        {
            var s=Ready();double labels=s.Labels("go");var night=new DateTime(2016,10,10,23,0,0);
            for(int i=0;i<700&&s.S.boardSelfRounds.Count<3;i++)s.TickBoardSelfPlay(night,.25f);
            Assert.AreEqual(3,s.S.boardSelfRounds.Count);Assert.AreEqual(0,s.DailyProgress("games3").count);Assert.IsEmpty(s.S.dataFiles);Assert.AreEqual(labels,s.Labels("go"));
            foreach(var r in s.S.boardRounds){Assert.True(r.selfPlay);Assert.False(r.privateAi);var board=GomokuLogic.Replay(r.log);Assert.NotNull(board);Assert.True(board.Full||board.Winner!=0);Assert.AreEqual(board.Moves,r.moves);}
            s.TickBoardSelfPlay(new DateTime(2016,10,11,7,0,0),.25f);Assert.AreEqual(1,s.S.dataFiles.Count);
            s.TickBoardSelfPlay(new DateTime(2016,10,11,8,0,0),.25f);Assert.AreEqual(1,s.S.dataFiles.Count);Assert.AreEqual(labels,s.Labels("go"));
            s.S.unlocked.Add(XgSim.DataEntryNode);Assert.True(s.InstallDataFile(s.S.dataFiles[0].fileId));Assert.Greater(s.Labels("go"),labels);Assert.False(s.InstallDataFile(s.S.dataFiles[0].fileId));
        }
        [Test] public void ReloadContinuesOnlyActualNightWorkAndDaytimeDoesNotInventGames()
        {
            var s=Ready();var night=new DateTime(2016,10,10,23,0,0);for(int i=0;i<5;i++)s.TickBoardSelfPlay(night,.25f);Assert.AreEqual(5,s.S.boardSelfMoves.Count);
            var loaded=new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(s.S)));loaded.TickBoardSelfPlay(night,.25f);Assert.AreEqual(6,loaded.S.boardSelfMoves.Count);
            loaded.TickBoardSelfPlay(new DateTime(2016,11,1,12,0,0),10);Assert.AreEqual(6,loaded.S.boardSelfMoves.Count);Assert.IsEmpty(loaded.S.boardBatches);
        }
        [Test] public void PastOpponentMovesAffectOnlyUnlockedMemoryBias()
        {
            var s=new XgSim();s.S.boardRounds.Add(new XgBoardRound{game="gomoku",privateAi=true,log=new List<string>{"C4","A1","D4"}});
            Assert.AreEqual(0,s.BoardLearnedBias()[3*15+2]);s.S.unlocked.Add("board.logic3");Assert.AreEqual(1,s.BoardLearnedBias()[3*15+2]);Assert.AreEqual(0,s.BoardLearnedBias()[0]);
        }
    }
}
