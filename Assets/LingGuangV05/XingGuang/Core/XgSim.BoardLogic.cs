using System;
using System.Collections.Generic;
using LingGuangV05.Core.Games;
namespace LingGuangV05.XingGuang
{
    public static class XgBoardLogic
    {
        public static bool IsNode(string id)=>id=="nextmove"||id=="board.logic1"||id=="board.logic2"||id=="board.logic3"||id=="board.logic4";
        public static void AddNodes(List<XgNode> nodes)
        {
            string[] zh={"一步判断","看懂棋盘截图","想两步以上","自己跟自己下"},en={"One-move judgment","Read board pictures","Plan ahead","Self-play"};
            string[] note={"用棋盘数字找直接连五或挡四。前置多层感知机、2层网络。","游戏中心开放读图入口；把摆渡或贴吧的本站标准五子棋原图拖入，再从像素计算走法。前置卷积网络。","五子棋搜索对方应手与后续；飞行棋避险、跳棋规划、大富翁留现金。前置门控记忆。","22:00–06:00在线时轮换自对弈五子棋、飞行棋、跳棋和大富翁；每夜同类最多3局，早晨记录包须拖入练页安装才更新偏好。需对局记录和与自己的AI完成任意3局。"};
            string[] noteEn={"Find an immediate win or block from board cells. Needs MLP and two network layers.","Open the Game Hub reader; drag a standard local Gomoku image from Bodu or Tieba to compute a move from its pixels. Requires convolution.","Search an opposing reply and continuation in Gomoku; assess plane risk, checker routes and property reserves. Needs gated memory.","While running 22:00–06:00, rotate Gomoku, aeroplane chess, Chinese checkers and Monopoly-style self-play: up to 3 same-game rounds per night. Morning packs update preferences only after drag-to-install. Needs recorder and 3 games against your AI."};
            int[] costs={800,2500,6000,15000};
            for(int i=0;i<4;i++)nodes.Add(new XgNode{id="board.logic"+(i+1),tree="research",lane="boardlogic",kind=XgNodeKind.Research,stage=i+2,cost=costs[i],parent=i>0?"board.logic"+i:null,needs=i==0?new[]{"mlp","s.d2"}:i==1?new[]{"lenet"}:i==2?new[]{"lstm"}:new[]{"gamedata"},name=zh[i],nameEn=en[i],note=note[i]+" 支线，不影响主线。",noteEn=noteEn[i]+" Optional side branch."});
            nodes.Add(new XgNode{id="nextmove",tree="research",lane="boardlogic",kind=XgNodeKind.Research,stage=3,cost=1000,parent="board.logic1",name="下一步走哪",nameEn="Next move",note="在教页开放独立棋局四选一题桌。",noteEn="Opens a separate four-choice board question desk in Teach."});
        }
    }
    public sealed partial class XgState
    {
        public int boardSelfNight,boardSelfPublishedNight;
        public List<string> boardSelfMoves=new List<string>();
        public List<string> boardSelfRounds=new List<string>();
        public int[] boardLearned=new int[225];
    }
    public sealed partial class XgSim
    {
        float boardSelfClock;
        public string BoardLogicBlocker(string id)
        {
            if(!XgBoardLogic.IsNode(id))return null;
            int stage=id=="nextmove"?3:id[id.Length-1]-'0'+1;
            if(S.stage<stage)return T("第"+stage+"阶段开放","Opens in stage "+stage);
            if(id=="board.logic4"&&!DailyProgress("games3").complete)return T("先与自己的AI完成任意3局（"+DailyProgress("games3").count+"/3）","First finish any 3 games against your AI ("+DailyProgress("games3").count+"/3)");
            return null;
        }
        public int[] BoardLearnedBias()
        {
            var bias=new int[225];if(BoardLogicLevel>=4&&S.boardLearned!=null&&S.boardLearned.Length==225)Array.Copy(S.boardLearned,bias,225);
            if(BoardLogicLevel>=3&&S.boardRounds!=null)foreach(var round in S.boardRounds)
            {
                if(round==null||!round.privateAi||round.game!="gomoku"||round.log==null)continue;
                for(int i=0;i<round.log.Count;i+=2)if(GomokuLogic.ReadPoint(round.log[i],out int x,out int y))bias[y*15+x]=Math.Min(20,bias[y*15+x]+1);
            }
            return bias;
        }
        public string BoardSelfPlayStatus=>S.boardSelfNight==0?T("今晚在线时开始自对弈。","Self-play starts while running tonight."):
            XgBoardRecords.Name(SelfPlayGame,English)+T("自对弈："," self-play: ")+(S.boardSelfRounds?.Count??0)+T("/3局；当前 ","/3 games; current: ")+SelfPlayMoves+T("步。安装记录包后更新偏好。"," actions. Install the record pack to update preferences.");
        public void TickBoardSelfPlay(DateTime now,float dt)
        {
            if(BoardLogicLevel<4||!Has(XgBoardRecords.Node)||float.IsNaN(dt)||dt<=0)return;
            int today=now.Year*10000+now.Month*100+now.Day;bool night=now.Hour>=22||now.Hour<6;
            DateTime nightDate=now.Hour<6?now.Date.AddDays(-1):now.Date;
            int key=nightDate.Year*10000+nightDate.Month*100+nightDate.Day;
            if(S.boardSelfRounds==null)S.boardSelfRounds=new List<string>();if(S.boardSelfMoves==null)S.boardSelfMoves=new List<string>();
            // A new calendar date never fabricates a game for skipped nights.
            if(S.boardSelfNight>0&&S.boardSelfNight!=S.boardSelfPublishedNight&&(today>S.boardSelfNight&&now.Hour>=6||key>S.boardSelfNight))PublishSelfPlay();
            if(!night)return;
            if(key>S.boardSelfNight)
            {S.boardSelfNight=key;S.boardSelfMoves.Clear();S.boardSelfRounds.Clear();ResetSelfPlayGame();
                string[] games={"gomoku","ludo","checkers_cn","monopoly"};int rotation=Math.Max(0,S.boardSelfRotation)%games.Length;
                S.boardSelfGame=games[rotation];S.boardSelfRotation=(rotation+1)%games.Length;boardSelfClock=0;}
            if(key!=S.boardSelfNight||S.boardSelfRounds.Count>=3)return;
            boardSelfClock+=Math.Min(dt,2);if(boardSelfClock<.25f)return;boardSelfClock=0;
            if(SelfPlayGame!="gomoku"){TickOtherBoardSelfPlay(key,today);return;}
            var board=GomokuLogic.Replay(S.boardSelfMoves);if(board==null){S.boardSelfMoves.Clear();return;}
            int stone=board.Moves%2+1;
            var rng=new Random(unchecked(key*397+board.Moves*31+S.boardSelfRounds.Count));
            // Small real opening variation; all following moves use the same local search as the game.
            int level=board.Moves<2?0:4;
            if(board.Winner==0&&!board.Full&&GomokuLogic.Pick(board,stone,level,rng,S.boardLearned,out int x,out int y))
            {board.Place(x,y,stone);S.boardSelfMoves.Add(GomokuLogic.Point(x,y));}
            if(board.Winner==0&&!board.Full)return;
            if(S.boardRounds==null)S.boardRounds=new List<XgBoardRound>();
            var round=new XgBoardRound{game="gomoku",ticket="self-"+Guid.NewGuid().ToString("N"),selfPlay=true,recording=true,privateAi=false,moves=board.Moves,outcome=board.Winner==1?(int)XgGameOutcome.PlayerWon:board.Winner==2?(int)XgGameOutcome.PlayerLost:(int)XgGameOutcome.Draw,samples=Math.Min(80,S.boardSelfMoves.Count)};
            round.log.AddRange(S.boardSelfMoves);S.boardRounds.Add(round);S.boardSelfRounds.Add(round.ticket);S.boardSelfMoves.Clear();
        }
        void PublishSelfPlay()
        {
            // Incomplete nights remain genuine unfinished work; only three finished games form the authored batch.
            if(S.boardSelfRounds.Count!=3)return;
            var rounds=new List<XgBoardRound>();foreach(string ticket in S.boardSelfRounds)foreach(var r in S.boardRounds)if(r!=null&&r.ticket==ticket&&r.selfPlay&&string.IsNullOrEmpty(r.fileId))rounds.Add(r);
            if(rounds.Count!=3)return;string game=rounds[0].game;foreach(var r in rounds)if(r.game!=game)return;
            if(S.boardBatches==null)S.boardBatches=new List<XgBoardBatch>();int ordinal=1;
            foreach(var b in S.boardBatches)if(b!=null&&b.game==game)ordinal=Math.Max(ordinal,b.ordinal+1);
            var batch=new XgBoardBatch{game=game,ordinal=ordinal,fileId="board-"+Guid.NewGuid().ToString("N")};
            foreach(var r in rounds){r.fileId=batch.fileId;batch.rounds.Add(r.ticket);batch.samples+=r.samples;}
            S.boardBatches.Add(batch);if(S.dataFiles==null)S.dataFiles=new List<XgDownload>();
            S.dataFiles.Add(new XgDownload{fileId=batch.fileId,key=batch.fileId,dataset=XgBoardRecords.Dataset(game),offer="board.records",nodeId="board.records",samples=batch.samples});
            S.boardSelfPublishedNight=S.boardSelfNight;Say(T("昨夜3局自对弈记录已打包，拖入练页安装。","Three real self-play games from last night are packed. Drag into Train to install."));DownloadFinished?.Invoke(batch.fileId);
        }
        void LearnBoardSelfPlay(XgBoardBatch batch)
        {
            if(batch==null)return;if(batch.game!="gomoku"){LearnOtherBoardSelfPlay(batch);return;}if(S.boardLearned==null||S.boardLearned.Length!=225)S.boardLearned=new int[225];
            foreach(var r in S.boardRounds)
            {
                if(r==null||!r.selfPlay||r.fileId!=batch.fileId)continue;
                int winner=r.outcome==(int)XgGameOutcome.PlayerWon?1:r.outcome==(int)XgGameOutcome.PlayerLost?2:0;if(winner==0)continue;
                for(int i=0;i<r.log.Count;i++)if(GomokuLogic.ReadPoint(r.log[i],out int x,out int y))
                {int at=y*15+x;S.boardLearned[at]=Math.Max(-20,Math.Min(20,S.boardLearned[at]+(i%2+1==winner?1:-1)));}
            }
        }
    }
}
