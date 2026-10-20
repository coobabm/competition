using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using LingGuangV05.Core.Games;
namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgBoardSelfPlayVarietyTests
    {
        static readonly DateTime Night=new DateTime(2016,10,10,23,0,0);
        static XgSim Ready(string game)
        {
            var sim=new XgSim();sim.S.stage=5;
            foreach(string id in new[]{"board.logic4","gamedata",XgSim.DataEntryNode})sim.S.unlocked.Add(id);
            sim.S.boardSelfNight=20161010;sim.S.boardSelfGame=game;return sim;
        }
        static void Finish(XgSim sim)
        {for(int i=0;i<12000&&sim.S.boardSelfRounds.Count<3;i++)sim.TickBoardSelfPlay(Night,.25f);Assert.AreEqual(3,sim.S.boardSelfRounds.Count,"Actual games must reach terminal states");}
        [TestCase("ludo")][TestCase("checkers_cn")][TestCase("monopoly")]
        public void RealTerminalGamesReplayAndLearnOnlyAfterOneInstall(string game)
        {
            var sim=Ready(game);double labels=sim.Labels("logic");Finish(sim);
            Assert.AreEqual(3,sim.S.boardRounds.Count);Assert.AreEqual(0,sim.DailyProgress("games3").count);
            Assert.IsEmpty(sim.S.dataFiles);Assert.IsEmpty(sim.S.boardPolicies);Assert.AreEqual(labels,sim.Labels("logic"));
            foreach(var round in sim.S.boardRounds)
            {
                Assert.AreEqual(game,round.game);Assert.True(round.selfPlay);Assert.False(round.privateAi);
                Assert.LessOrEqual(round.samples,round.log.Count);Replay(round);
            }
            sim.TickBoardSelfPlay(Night.Date.AddDays(1).AddHours(7),.25f);
            Assert.AreEqual(1,sim.S.dataFiles.Count);Assert.AreEqual("logic",sim.S.dataFiles[0].dataset);Assert.IsEmpty(sim.S.boardPolicies);
            Assert.True(sim.InstallDataFile(sim.S.dataFiles[0].fileId));Assert.Greater(sim.Labels("logic"),labels);
            Assert.AreEqual(6,sim.S.boardPolicies.Sum(p=>p.rounds));Assert.AreEqual(3,sim.S.boardPolicies.Count);
            string learned=JsonUtility.ToJson(sim.S);Assert.False(sim.InstallDataFile(sim.S.dataFiles[0].fileId));Assert.AreEqual(learned,JsonUtility.ToJson(sim.S));
        }
        static void Replay(XgBoardRound round)
        {
            if(round.game=="ludo")
            {
                var g=new LudoGame();foreach(string line in round.log)
                {var p=line.Split(':');if(p[0]=="roll")Assert.True(g.Roll(int.Parse(p[2])));else if(p[0]=="move")Assert.True(g.Move(int.Parse(p[2])));}
                Assert.True(g.Over);CollectionAssert.AreEqual(round.log,g.S.log);Assert.AreEqual(round.moves,g.S.actions);
            }
            else if(round.game=="checkers_cn")
            {
                var g=new ChineseCheckers();foreach(string line in round.log)
                {var p=line.Split(':');if(p[0]=="pass")Assert.True(g.Pass());else {var path=p[3].Split(',');Assert.True(g.Move(int.Parse(p[2]),int.Parse(path[path.Length-1])));}}
                Assert.True(g.Over);CollectionAssert.AreEqual(round.log,g.S.log);Assert.AreEqual(round.moves,g.S.moves);
            }
            else
            {
                var g=new MonopolyGame();for(int i=0;i<round.log.Count;i++)
                {
                    var p=round.log[i].Split(':');if(p[0]=="roll")
                    {
                        int choice=0,today=20161010;
                        for(int j=i+1;j<round.log.Count&&!round.log[j].StartsWith("roll:");j++)
                        {var q=round.log[j].Split(':');if(q[0]=="chance"){today=int.Parse(q[4]);choice=MonopolyGame.EligibleChance(today).IndexOf(int.Parse(q[2]));}}
                        Assert.True(g.Roll(int.Parse(p[2]),today,choice));
                    }
                    else if(p[0]=="buy"||p[0]=="skip")Assert.True(g.ChoosePurchase(p[0]=="buy"));
                }
                Assert.True(g.Over);CollectionAssert.AreEqual(round.log,g.S.log);Assert.AreEqual(round.moves,g.S.moves);
            }
        }
        [TestCase("ludo")][TestCase("checkers_cn")][TestCase("monopoly")]
        public void SaveReloadContinuesSameActualPositionAndDoesNotInventDaytimeGames(string game)
        {
            var sim=Ready(game);for(int i=0;i<5;i++)sim.TickBoardSelfPlay(Night,.25f);
            var loaded=new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(sim.S)));
            Assert.AreEqual(ActiveState(sim,game),ActiveState(loaded,game),"The selected engine's exact board, counters and trace survive JSON restoration");
            Assert.AreEqual(sim.S.boardSelfNight,loaded.S.boardSelfNight,"Saved deterministic night seed");
            Assert.AreEqual(sim.S.boardSelfRotation,loaded.S.boardSelfRotation,"Saved rotation");
            CollectionAssert.AreEqual(sim.S.boardSelfRounds,loaded.S.boardSelfRounds,"Saved completed-round seed input");
            if(game!="ludo")Assert.IsNull(loaded.S.boardSelfLudo,"Inactive Ludo placeholder: "+JsonUtility.ToJson(loaded.S.boardSelfLudo));
            if(game!="checkers_cn")Assert.IsNull(loaded.S.boardSelfCheckers,"Inactive checkers placeholder: "+JsonUtility.ToJson(loaded.S.boardSelfCheckers));
            if(game!="monopoly")Assert.IsNull(loaded.S.boardSelfMonopoly,"Inactive Monopoly placeholder: "+JsonUtility.ToJson(loaded.S.boardSelfMonopoly));
            for(int i=0;i<5;i++)
            {
                sim.TickBoardSelfPlay(Night,.25f);loaded.TickBoardSelfPlay(Night,.25f);
                Assert.AreEqual(ActiveState(sim,game),ActiveState(loaded,game),"Every subsequent real engine action matches without reroll/reset");
            }
            CollectionAssert.AreEqual(sim.S.boardSelfLudo?.log??sim.S.boardSelfCheckers?.log??sim.S.boardSelfMonopoly?.log,
                loaded.S.boardSelfLudo?.log??loaded.S.boardSelfCheckers?.log??loaded.S.boardSelfMonopoly?.log);
            string before=JsonUtility.ToJson(loaded.S);loaded.TickBoardSelfPlay(new DateTime(2016,11,1,12,0,0),10);
            Assert.AreEqual(before,JsonUtility.ToJson(loaded.S));Assert.IsEmpty(loaded.S.dataFiles);
        }
        static string ActiveState(XgSim sim,string game)=>game=="ludo"?JsonUtility.ToJson(sim.S.boardSelfLudo):game=="checkers_cn"?JsonUtility.ToJson(sim.S.boardSelfCheckers):JsonUtility.ToJson(sim.S.boardSelfMonopoly);
        [Test] public void InactiveStatesWithActualEvidenceAreRetainedForDiagnosis()
        {
            var sim=Ready("gomoku");var ludo=new LudoGame();ludo.Roll(5);ludo.Move(0);sim.S.boardSelfLudo=ludo.S;
            var checkers=new ChineseCheckers();var move=checkers.AllMoves()[0];checkers.Move(move.piece,move.destination);sim.S.boardSelfCheckers=checkers.S;
            var monopoly=new MonopolyGame();monopoly.Roll(1,20161010,0);monopoly.ChoosePurchase(true);sim.S.boardSelfMonopoly=monopoly.S;
            var loaded=new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(sim.S)));
            foreach(string game in new[]{"ludo","checkers_cn","monopoly"})Assert.AreEqual(ActiveState(sim,game),ActiveState(loaded,game),"Inactive evidence is not discarded: "+game);
        }
        [Test] public void InactiveInitialButRealArraysAreNotTreatedAsEmptyPlaceholders()
        {
            var sim=Ready("gomoku");sim.S.boardSelfLudo=new LudoGame().S;sim.S.boardSelfCheckers=new ChineseCheckers().S;sim.S.boardSelfMonopoly=new MonopolyGame().S;
            var loaded=new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(sim.S)));
            foreach(string game in new[]{"ludo","checkers_cn","monopoly"})Assert.AreEqual(ActiveState(sim,game),ActiveState(loaded,game));
        }
        [Test] public void ObservedInitializerDefaultsWithoutAnEngineAreOnlyPlaceholders()
        {
            var sim=Ready("gomoku");
            // Exact arrays seen in root's Unity failure output; no engine has constructed them.
            sim.S.boardSelfLudo=JsonUtility.FromJson<LudoState>("{\"planes\":[-1,-1,-1,-1],\"turn\":0,\"die\":0,\"rolls\":0,\"actions\":0,\"winner\":-1,\"log\":[]}");
            sim.S.boardSelfCheckers=JsonUtility.FromJson<ChineseCheckersState>("{\"pieces\":[],\"turn\":0,\"moves\":0,\"winner\":-1,\"turns\":[0,0],\"log\":[]}");
            // Monopoly field defaults; initialized engines also have a 20-entry owners array.
            sim.S.boardSelfMonopoly=new MonopolyState{owners=new int[0]};
            var loaded=new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(sim.S)));
            Assert.IsNull(loaded.S.boardSelfLudo);Assert.IsNull(loaded.S.boardSelfCheckers);Assert.IsNull(loaded.S.boardSelfMonopoly);
            Assert.IsEmpty(loaded.S.boardRounds);Assert.IsEmpty(loaded.S.dataFiles);
        }
        [TestCase("initialized")][TestCase("position")][TestCase("shape")][TestCase("rolls")]
        [TestCase("actions")][TestCase("die")][TestCase("turn")][TestCase("winner")][TestCase("log")]
        public void InactiveLudoWithEvenOneNonDefaultEvidenceIsRetained(string evidence)
        {
            var sim=Ready("gomoku");var state=new LudoState();
            switch(evidence)
            {
                case "initialized":state=new LudoGame().S;break;
                case "position":state.planes[0]=0;break;
                case "shape":state.planes=new[]{-1,-1};break;
                case "rolls":state.rolls=1;break;
                case "actions":state.actions=1;break;
                case "die":state.die=1;break;
                case "turn":state.turn=1;break;
                case "winner":state.winner=0;break;
                case "log":state.log.Add("saved diagnostic trace");break;
            }
            sim.S.boardSelfLudo=state;string before=JsonUtility.ToJson(state);
            var loaded=new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(sim.S)));
            Assert.NotNull(loaded.S.boardSelfLudo);Assert.AreEqual(before,JsonUtility.ToJson(loaded.S.boardSelfLudo));
        }
        [Test] public void InactiveOtherGamesRetainNonDefaultArraysCountersAndOutcomes()
        {
            Action<ChineseCheckersState>[] checkersEdits={s=>s.pieces=new[]{7},s=>s.turns[1]=1,s=>s.turns=new[]{0},s=>s.moves=1,s=>s.turn=1,s=>s.winner=0,s=>s.log.Add("trace")};
            foreach(var edit in checkersEdits)
            {
                var sim=Ready("gomoku");var state=new ChineseCheckersState{pieces=new int[0]};edit(state);sim.S.boardSelfCheckers=state;string before=JsonUtility.ToJson(state);
                var loaded=new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(sim.S)));Assert.AreEqual(before,JsonUtility.ToJson(loaded.S.boardSelfCheckers));
            }
            Action<MonopolyState>[] monopolyEdits={s=>s.owners=new[]{-1},s=>s.position[0]=1,s=>s.cash[0]=1499,s=>s.turns[1]=1,s=>s.moves=1,s=>s.turn=1,s=>s.die=1,s=>s.pending=0,s=>s.winner=0,s=>s.log.Add("trace")};
            foreach(var edit in monopolyEdits)
            {
                var sim=Ready("gomoku");var state=new MonopolyState{owners=new int[0]};edit(state);sim.S.boardSelfMonopoly=state;string before=JsonUtility.ToJson(state);
                var loaded=new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(sim.S)));Assert.AreEqual(before,JsonUtility.ToJson(loaded.S.boardSelfMonopoly));
            }
        }
        [TestCase("ludo")][TestCase("checkers_cn")][TestCase("monopoly")]
        public void SelectedGameIsNeverDiscardedEvenWhenItLooksLikeAnInlineDefault(string game)
        {
            var sim=Ready(game);sim.S.boardSelfLudo=new LudoState();sim.S.boardSelfCheckers=new ChineseCheckersState{pieces=new int[0]};sim.S.boardSelfMonopoly=new MonopolyState{owners=new int[0]};
            string before=ActiveState(sim,game);var loaded=new XgSim(sim.S);Assert.AreEqual(before,ActiveState(loaded,game));
        }
        [Test] public void SubsequentOnlineNightsRotateWithoutGivingOfflineGames()
        {
            var sim=Ready("");sim.S.boardSelfNight=0;string[] games={"gomoku","ludo","checkers_cn","monopoly","gomoku"};
            for(int i=0;i<games.Length;i++){sim.TickBoardSelfPlay(Night.AddDays(i*3),.25f);Assert.AreEqual(games[i],sim.S.boardSelfGame);Assert.IsEmpty(sim.S.boardRounds);Assert.IsEmpty(sim.S.dataFiles);}
        }
        [Test] public void InstalledOutcomesSelectBoundedPolicyOnlyAtLevelFour()
        {
            var sim=Ready("ludo");sim.S.boardPolicies.Add(new XgBoardPolicy{game="ludo",style=2,rounds=8,points=14});
            Assert.AreEqual(2,sim.BoardSelfPolicy("ludo"));Assert.AreEqual(1,sim.BoardSelfPolicy("monopoly"));
            sim.S.unlocked.Remove("board.logic4");Assert.AreEqual(1,sim.BoardSelfPolicy("ludo"));
        }
    }
}
