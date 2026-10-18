using System;
using LingGuangV05.XingGuang;
using LingGuangV05.Desktop.Story;
using Michsky.DreamOS;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LingGuangV05.Desktop.Games
{
    public abstract partial class HubGame
    {
        protected XgSim roundOwner;
        protected XgBoardSession roundSession;
        XgSim viewOwner;
        protected bool privateAiOpponent;
        RectTransform boardArea,ruleCard;
        WindowManager boardWindow;
        TMP_Text computerLabel,aiLabel,helpLabel,ruleText,startLabel,hideLabel;
        Button aiButton;Toggle hideRules;bool rulesSeen;
        protected string OpponentName=>AiPlays?AiName:T("电脑","Computer");
        public virtual void RestartRound(){}
        protected bool CanInteract=>boardArea!=null&&boardArea.gameObject.activeInHierarchy&&boardWindow!=null&&boardWindow.isOn&&
            (ruleCard==null||!ruleCard.gameObject.activeSelf)&&ReferenceEquals(viewOwner,Lab);
        protected void BeginRoundRecord()
        {
            AbandonRoundRecord();roundOwner=Lab;roundSession=roundOwner?.BeginBoardSession(Id,AiPlays);
        }
        protected void AbandonRoundRecord(){roundOwner?.AbandonBoardSession(roundSession);roundSession=null;roundOwner=null;}
        public void SyncBoardOwner()
        {
            if(!ReferenceEquals(viewOwner,Lab))
            {AbandonRoundRecord();viewOwner=Lab;privateAiOpponent=false;rulesSeen=false;RestartRound();if(ruleCard!=null)ruleCard.gameObject.SetActive(false);}
            if(aiButton!=null){aiButton.interactable=Lab?.BoardAiAvailable==true;aiLabel.text=aiButton.interactable?AiName:T("自己的AI · 阶段2","Your AI · stage 2");}
            if(computerLabel!=null)computerLabel.text=T("电脑","Computer")+(privateAiOpponent?"":" ✓");
            if(aiLabel!=null&&privateAiOpponent)aiLabel.text+=" ✓";
            if(helpLabel!=null)helpLabel.text="?";
            if(ruleText!=null)ruleText.text=Rules();
            if(startLabel!=null)startLabel.text=T("开始 / 继续","Start / Continue");
            if(hideLabel!=null)hideLabel.text=T("下次不再显示","Don't show next time");
        }
        public void AttachBoardControls(RectTransform area)
        {
            boardArea=area;boardWindow=GetComponentInParent<WindowManager>(true);viewOwner=Lab;
            var bar=PrologueDesk.Rect("Board controls",area,new Vector2(0,1),Vector2.one,new Vector2(18,-40),new Vector2(-18,-6));
            Button(bar,"Computer",new Vector2(.52f,0),new Vector2(.70f,1),Accent,()=>ChooseOpponent(false),out computerLabel);
            aiButton=Button(bar,"Private AI",new Vector2(.71f,0),new Vector2(.94f,1),Accent,()=>ChooseOpponent(true),out aiLabel);
            Button(bar,"Rules",new Vector2(.95f,0),Vector2.one,Accent,ShowRules,out helpLabel);
            ruleCard=PrologueDesk.Rect("Rules",area,Vector2.zero,Vector2.one,new Vector2(20,20),new Vector2(-20,-45));
            PrologueDesk.Fill(ruleCard,new Color32(250,250,252,255));
            // Native card covers the game; no simulated rule completion or automatic move.
            ruleText=Text(PrologueDesk.Rect("Rules text",ruleCard,Vector2.zero,Vector2.one,new Vector2(30,120),new Vector2(-30,-25)),"",21,new Color32(30,38,52,255));
            ruleText.enableAutoSizing=true;ruleText.fontSizeMin=16;ruleText.fontSizeMax=21;ruleText.textWrappingMode=TextWrappingModes.Normal;
            var toggleRect=PrologueDesk.Rect("Don't show",ruleCard,new Vector2(0,0),new Vector2(.65f,0),new Vector2(30,65),new Vector2(-15,100));
            var toggleBackground=PrologueDesk.Fill(toggleRect,new Color32(232,236,242,255));
            hideRules=toggleRect.gameObject.AddComponent<Toggle>();hideRules.targetGraphic=toggleBackground;
            var tick=PrologueDesk.Fill(PrologueDesk.Rect("Tick",toggleRect,Vector2.zero,new Vector2(0,1),new Vector2(5,7),new Vector2(26,-7)),Accent);hideRules.graphic=tick;
            hideLabel=Text(PrologueDesk.Rect("Label",toggleRect,Vector2.zero,Vector2.one,new Vector2(36,0),Vector2.zero),"",17,new Color32(40,46,58,255),TextAlignmentOptions.MidlineLeft);
            Button(ruleCard,"Start",new Vector2(.68f,.08f),new Vector2(.96f,.15f),Accent,()=>
            {
                if(!ReferenceEquals(viewOwner,Lab))return;
                if(Lab!=null){if(Lab.S.boardRulesHidden==null)Lab.S.boardRulesHidden=new System.Collections.Generic.List<string>();if(hideRules.isOn){if(!Lab.S.boardRulesHidden.Contains(Id))Lab.S.boardRulesHidden.Add(Id);}else Lab.S.boardRulesHidden.Remove(Id);}
                ruleCard.gameObject.SetActive(false);
            },out startLabel);
            ruleCard.gameObject.SetActive(false);SyncBoardOwner();
        }
        void ChooseOpponent(bool ai)
        {
            if(!ReferenceEquals(viewOwner,Lab)||ai&&Lab?.BoardAiAvailable!=true||privateAiOpponent==ai)return;
            AbandonRoundRecord();privateAiOpponent=ai;RestartRound();SyncBoardOwner();Refresh();
        }
        public void OnBoardShown(){if(!rulesSeen){rulesSeen=true;if(Lab?.S.boardRulesHidden==null||!Lab.S.boardRulesHidden.Contains(Id))ShowRules();}}
        void ShowRules(){if(ruleCard==null)return;hideRules.isOn=Lab?.S.boardRulesHidden?.Contains(Id)==true;ruleCard.SetAsLastSibling();ruleCard.gameObject.SetActive(true);SyncBoardOwner();}
        string Rules()
        {
            switch(Id){
                case "gomoku": return T("五子棋\n先在横、竖或斜线上连成五颗子的人赢。\n\n• 棋盘 15×15，黑先白后，轮流在交叉点上放一颗子。\n• 子放下就不能移动，也不会被吃。\n• 横、竖、斜任意方向连成五颗就赢。\n• 棋盘下满还没人连五，算平局。\n\n不设禁手；保留原游戏的不限时对局。\n它会把每一步都记下来，赢在哪、输在哪，都是它的练习题。","Gomoku\nFirst to get five in a row, across, down or diagonally, wins.\n\n• 15×15 board. Black moves first; take turns placing one stone on an intersection.\n• Stones never move and are never captured.\n• Five in a row in any direction wins.\n• If the board fills with no five, it's a draw.\n\nNo forbidden moves; the original untimed game is retained.\nIt records every move. Where it won and lost becomes practice.");
                case "xiangqi_endgame": return T("象棋残局\n红方先走，在规定步数内将死黑方。\n\n• 车直走横走，格数不限；马走「日」字，被挡住马腿就不能走。\n• 炮走法和车一样，吃子时中间必须隔一个子。\n• 相走「田」字，不能过河；士和将只能在九宫里走，士斜走一格，将直走一格。\n• 兵过河以后可以横走，但永远不能后退。\n• 将和帅不能在同一条直线上面对面，中间没有棋子挡着就不行。\n\n每局是一个残局，限 10 步。\n你走红、它走黑；或者让它走红，你来防。","Xiangqi endgame\nRed moves first and must checkmate Black within the move limit.\n\n• Chariots move any distance straight. Horses move in an L and are blocked by a piece at the knee.\n• Cannons move like chariots but capture by jumping exactly one piece.\n• Elephants move two diagonally and can't cross the river. Advisors and generals stay in the palace: advisors one diagonal, generals one straight.\n• Soldiers may move sideways after crossing the river, and never backward.\n• The two generals may not face each other on an open file.\n\nEach game is one endgame, 10 moves max.\nYou play Red and it defends, or it attacks and you defend.");
                case "chess_mate": return T("国际象棋 · 一步杀 / 两步杀\n白先，一步或两步将死黑王。\n\n• 王走一格；后横竖斜都能走，格数不限；车横竖；象斜着走；马走「日」字，可以跳过别的子；兵向前一格，吃子时斜着吃。\n• 王被攻击叫「将军」，必须马上解围。\n• 王被将军而且怎么都躲不开、挡不住、吃不掉，就是「将死」，这一局结束。\n\n题目来自 Lichess 公开题库，一题一局。\n你们轮流解题，看谁先解出来。","Chess · mate in 1 / 2\nWhite to move. Checkmate the black king in one or two moves.\n\n• King one square; queen any distance in any direction; rook straight; bishop diagonal; knight in an L and may jump; pawn forward one, captures diagonally.\n• A king under attack is in check and must escape at once.\n• Check with no escape, block or capture is checkmate. Game over.\n\nPuzzles from the Lichess public database, one per game.\nTake turns solving; see who gets there first.");
                case "ludo": return T("飞行棋\n先把自己的飞机全部飞回终点的人赢。\n\n• 掷骰子决定走几格。掷到 6 才能让飞机起飞（小对局里 5 或 6 都能起飞）。\n• 停在和自己同色的格子上，可以再往前跳到下一个同色格。\n• 停在对方飞机所在的格子，就把它撞回基地。\n• 掷到 6 可以再掷一次。\n• 进终点要刚好走到，点数多了就往回退。\n\n两人对局，每人 2 架飞机，环道 28 格。\n骰子它改不了，但它会学：哪架飞机先走更划算。","Aeroplane chess\nFirst to fly all your planes home wins.\n\n• Roll the die to move. A plane takes off only on a 6 (in the short game, 5 or 6).\n• Land on your own colour and you jump ahead to the next square of that colour.\n• Land on an opponent's plane and it goes back to base.\n• Rolling a 6 gives another roll.\n• You must reach home exactly; extra steps bounce you back.\n\nTwo players, 2 planes each, 28-square loop.\nIt can't change the dice, but it learns which plane is better to move.");
                case "checkers_cn": return T("跳棋\n先把自己的棋子全部移到对面角上的人赢。\n\n• 每回合走一颗子。\n• 可以往相邻的空位走一步。\n• 也可以跳：隔着一颗相邻的棋子（谁的都行），跳到它后面的空位。\n• 跳完如果还能接着跳，可以一直连跳下去。\n• 棋子不会被吃掉。\n\n两人对局，每人 6 颗子；每人最多 40 回合，到时进角多的赢。\n连跳要看好几步之后的路，正好考它会不会想得远。","Chinese checkers\nFirst to move all your pieces into the opposite corner wins.\n\n• Move one piece per turn.\n• Step into an adjacent empty hole.\n• Or jump over one adjacent piece (anyone's) into the empty hole beyond.\n• Keep jumping as long as you can, in one turn.\n• Pieces are never captured.\n\nTwo players, 6 pieces each; 40 turns each, most pieces home wins.\nChain jumps need looking several steps ahead, a good test of planning.");
                case "monopoly": return T("大富翁\n8 轮后，现金加地产总值最多的人赢；谁先破产谁输。\n\n• 每人开局 ¥1,500（游戏币，和你的真钱无关），掷骰子走格。\n• 停在没人买的地上，可以买下。\n• 停在对方的地上，要付过路费。\n• 停在「机会」格抽一张卡，有好有坏。\n• 每经过起点，领 ¥200。\n\n两人对局，20 格小地图，每人 8 轮。\n它要学会算账：现在买地，还是留钱交过路费。","Monopoly-style\nAfter 8 rounds, highest cash plus property wins. Go bankrupt and you lose.\n\n• Each player starts with ¥1,500 in play money (not your real money). Roll and move.\n• Land on unowned property and you may buy it.\n• Land on the opponent's property and pay rent.\n• Land on Chance and draw a card, good or bad.\n• Collect ¥200 every time you pass Start.\n\nTwo players, a 20-square board, 8 rounds each.\nIt has to learn to budget: buy land now, or keep cash for rent.");
                case "go":return T("围棋 · 9路\n你执黑先手；围住对方棋子的全部气可以提子。\n禁止自杀；不能立即劫回。双方连续停一手即结束。\n按棋子与围住的空点数子，白贴6.5目。","Go · 9×9\nYou play Black first. Remove opposing stones with no liberties.\nNo suicide; simple ko prevents immediate recapture. Two consecutive passes end play.\nArea scoring, with 6.5 komi for White.");
                case "chess":return T("国际象棋\n你执白先行，王不能走入被攻击的位置。\n车走直线、象走斜线、后兼有两者；马走日字；兵向前、斜吃。\n将死获胜，无合法着但未被将军为逼和。支持王车易位、吃过路兵、升变。","Chess\nYou play White first. A king may not move into attack.\nRooks move straight, bishops diagonally, queens both; knights jump in an L, pawns move ahead and capture diagonally.\nCheckmate wins; no legal move outside check is stalemate. Castling, en passant and promotion apply.");
                default:return Title;
            }
        }
    }
}
