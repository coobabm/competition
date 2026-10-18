using System;
using System.Collections.Generic;
namespace LingGuangV05.XingGuang
{
    public sealed class XgBoardSession
    {
        internal string game, ticket;
        internal bool privateAi, recording;
        public string Ticket=>ticket;
    }
    [Serializable] public sealed class XgBoardRound
    {
        public string game="",ticket="",fileId="";
        public bool privateAi,recording,selfPlay;
        public int outcome,moves,samples;
        public int selfPolicy0=-1,selfPolicy1=-1;
        public List<string> log=new List<string>();
    }
    [Serializable] public sealed class XgBoardBatch
    {
        public string fileId="",game="";
        public int ordinal,samples;
        public bool installed;
        public List<string> rounds=new List<string>();
    }
    public sealed partial class XgState
    {
        public List<XgBoardRound> boardRounds=new List<XgBoardRound>();
        public List<XgBoardBatch> boardBatches=new List<XgBoardBatch>();
        public List<string> boardRulesHidden=new List<string>();
    }
    public static class XgBoardRecords
    {
        public const string Node="gamedata";
        public static bool Known(string id)=>id=="gomoku"||id=="go"||id=="chess"||id=="ludo"||id=="checkers_cn"||id=="monopoly"||id=="xiangqi_endgame"||id=="chess_mate";
        public static int BatchSize(string id)=>id=="chess_mate"?5:3;
        public static int SampleCap(string id)=>id=="gomoku"?80:id=="xiangqi_endgame"?50:id=="chess_mate"?20:id=="ludo"?30:id=="checkers_cn"?60:id=="monopoly"?40:30;
        public static string Dataset(string id)=>id=="gomoku"||id=="go"?"go":id=="chess"||id=="chess_mate"||id=="xiangqi_endgame"?"chess":"logic";
        public static string Name(string id,bool en)
        {switch(id){case "ludo":return en?"Aeroplane chess":"飞行棋";case "checkers_cn":return en?"Chinese checkers":"跳棋";case "monopoly":return en?"Monopoly-style":"大富翁";case "go":return en?"Go":"围棋";case "chess":return en?"Chess":"国际象棋";case "xiangqi_endgame":return en?"Xiangqi endgame":"象棋残局";case "chess_mate":return en?"Chess mates":"将杀题";default:return en?"Gomoku":"五子棋";}}
        public static void AddNodes(List<XgNode> nodes)
        {nodes.Add(new XgNode{id=Node,tree="research",lane="boardlogic",target=Node,stage=2,cost=500,kind=XgNodeKind.Research,name="对局记录",nameEn="Game records",note="和自己的AI下完一局后可买。购买后新开的AI对局每3局产包，拖入练页安装；不直接加样本。",noteEn="Buy after one completed game against your AI. New AI games then create a pack every 3 games; drag it into Train to install. No direct samples."});}
    }
    public sealed partial class XgSim
    {
        readonly Dictionary<string,XgBoardSession> boardSessions=new Dictionary<string,XgBoardSession>();
        public bool BoardAiAvailable=>S.stage>=2;
        // This is not the old labelled-logic-desk difficulty. Future logic-tech integration owns these IDs.
        public int BoardLogicLevel=>Has("board.logic4")?4:Has("board.logic3")?3:Has("board.logic2")?2:Has("board.logic1")?1:0;
        public string BoardRecordsBlocker(string id)=>id!=XgBoardRecords.Node?null:S.stage<2?T("第2阶段开放","Opens in stage 2"):DailyProgress("game1").complete?null:T("先与自己的AI下完一局","First finish one game against your AI");
        public XgBoardSession BeginBoardSession(string game,bool privateAi)
        {
            if(!XgBoardRecords.Known(game)||privateAi&&!BoardAiAvailable)return null;
            var session=new XgBoardSession{game=game,ticket=Guid.NewGuid().ToString("N"),privateAi=privateAi,recording=privateAi&&Has(XgBoardRecords.Node)};
            boardSessions[session.ticket]=session;return session;
        }
        public void AbandonBoardSession(XgBoardSession session){if(session!=null)boardSessions.Remove(session.ticket);}
        public bool FinishBoardSession(XgBoardSession session,XgGameOutcome outcome,int moves,IList<string> log)
        {
            if((int)outcome<0||(int)outcome>2||session==null||!boardSessions.TryGetValue(session.ticket,out var current)||!ReferenceEquals(session,current)||moves<=0||log==null||log.Count==0)return false;
            boardSessions.Remove(session.ticket);
            if(S.boardRounds==null)S.boardRounds=new List<XgBoardRound>();
            foreach(var old in S.boardRounds)if(old!=null&&old.ticket==session.ticket)return false;
            var r=new XgBoardRound{game=session.game,ticket=session.ticket,privateAi=session.privateAi,recording=session.recording&&Has(XgBoardRecords.Node),moves=moves,outcome=(int)outcome};
            foreach(var line in log)if(!string.IsNullOrEmpty(line)&&r.log.Count<1024)r.log.Add(line);
            if(r.log.Count==0)return false;
            // Never credit more records than the genuine trace contains. These are simulation samples, not a claimed learned policy.
            r.samples=Math.Min(XgBoardRecords.SampleCap(r.game),r.log.Count);S.boardRounds.Add(r);
            RecordDailyGameCompleted(r.game,r.ticket,r.privateAi);
            if(r.privateAi&&outcome==XgGameOutcome.PlayerWon)Earn("life.game.won");
            if(r.privateAi&&outcome==XgGameOutcome.PlayerLost)Earn("life.game.lost");
            if(r.recording)BuildBoardBatch(r.game);
            return true;
        }
        void BuildBoardBatch(string game)
        {
            var waiting=new List<XgBoardRound>();foreach(var r in S.boardRounds)if(r!=null&&r.game==game&&r.privateAi&&!r.selfPlay&&r.recording&&string.IsNullOrEmpty(r.fileId))waiting.Add(r);
            int size=XgBoardRecords.BatchSize(game);if(waiting.Count<size)return;
            if(S.boardBatches==null)S.boardBatches=new List<XgBoardBatch>();int ordinal=1;
            foreach(var batch in S.boardBatches)if(batch!=null&&batch.game==game)ordinal=Math.Max(ordinal,batch.ordinal+1);
            var next=new XgBoardBatch{game=game,fileId="board-"+Guid.NewGuid().ToString("N"),ordinal=ordinal};
            for(int i=0;i<size;i++){waiting[i].fileId=next.fileId;next.rounds.Add(waiting[i].ticket);next.samples+=waiting[i].samples;}
            S.boardBatches.Add(next);
            if(S.dataFiles==null)S.dataFiles=new List<XgDownload>();
            S.dataFiles.Add(new XgDownload{fileId=next.fileId,key=next.fileId,dataset=XgBoardRecords.Dataset(game),offer="board.records",nodeId="board.records",sizeMB=0,doneMB=0,samples=next.samples});
            Say(T("对局记录已打包，拖进练页安装。","Game records packed. Drag the pack into Train to install."));
            DownloadFinished?.Invoke(next.fileId);
        }
        XgBoardBatch BoardBatch(string fileId)
        {if(S.boardBatches!=null)foreach(var b in S.boardBatches)if(b!=null&&b.fileId==fileId)return b;return null;}
        bool ValidBoardDataFile(XgDownload file)
        {
            if(file==null||file.offer!="board.records")return false;var batch=BoardBatch(file.fileId);
            return batch!=null&&XgBoardRecords.Known(batch.game)&&batch.rounds!=null&&batch.rounds.Count==XgBoardRecords.BatchSize(batch.game)&&batch.samples>0&&file.samples==batch.samples&&file.dataset==XgBoardRecords.Dataset(batch.game);
        }
        void InstallBoardDataFile(XgDownload file)
        {
            if(!ValidBoardDataFile(file))return;var batch=BoardBatch(file.fileId);if(batch.installed)return;
            batch.installed=true;LearnBoardSelfPlay(batch);SetCount(S.labels,file.dataset,Labels(file.dataset)+batch.samples);
            string zh="对局 · "+XgBoardRecords.Name(batch.game,false)+" × "+batch.rounds.Count;
            string en="Games · "+XgBoardRecords.Name(batch.game,true)+" × "+batch.rounds.Count;
            AppendLearning(new XgAskedEntry{sourceKind="game",bankId=batch.game,question=zh,questionEn=en,eventKey="board-install:"+batch.fileId});
        }
        public string BoardDataFileName(string fileId)
        {var b=BoardBatch(fileId);return b==null?null:T("对局记录_","Game_records_")+XgBoardRecords.Name(b.game,English)+"_"+b.ordinal+".zip ("+b.rounds.Count+T("局 · "," games · ")+b.samples+T("条)"," records)");}
    }
}
