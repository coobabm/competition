using System;
using LingGuangV05.XingGuang;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.OS;
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
        Button aiButton,recordsButton;Toggle hideRules;bool rulesSeen;TMP_Text recordsLabel;
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
            if(recordsButton!=null)
            {
                // 2026-10-21: only the records purchase turns games with it into training data, so the rules card points there.
                bool offer=Lab!=null&&Lab.BoardAiAvailable&&!Lab.Has(XgBoardRecords.Node);
                recordsButton.gameObject.SetActive(offer);
                if(offer){string blocker=Lab.BoardRecordsBlocker(XgBoardRecords.Node);recordsLabel.text=blocker==null?T("去科技买「对局记录」 ›","Buy Game records in Tech ›"):T("「对局记录」· ","Game records · ")+blocker+" ›";}
            }
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
            var viewport=PrologueDesk.Rect("Rules viewport",ruleCard,Vector2.zero,Vector2.one,new Vector2(30,165),new Vector2(-30,-25));
            PrologueDesk.Fill(viewport,new Color32(250,250,252,255));
            viewport.gameObject.AddComponent<RectMask2D>();
            var content=PrologueDesk.Rect("Rules text",viewport,new Vector2(0,1),Vector2.one,Vector2.zero,Vector2.zero);
            content.pivot=new Vector2(.5f,1);
            ruleText=Text(content,"",20,new Color32(30,38,52,255));
            ruleText.textWrappingMode=TextWrappingModes.Normal;
            var fit=content.gameObject.AddComponent<ContentSizeFitter>();fit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;
            scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=28;
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
            recordsButton=Button(ruleCard,"Game records",new Vector2(.68f,.17f),new Vector2(.96f,.23f),Accent,OpenRecordsNode,out recordsLabel);
            recordsLabel.enableAutoSizing=true;recordsLabel.fontSizeMin=12;recordsLabel.fontSizeMax=17;
            ruleCard.gameObject.SetActive(false);SyncBoardOwner();
        }
        void OpenRecordsNode()
        {
            var owner=global::LingGuangV05.Desktop.XingGuang.XingGuangController.Instance;
            if(owner==null||!ReferenceEquals(viewOwner,Lab)||!ReferenceEquals(owner.Sim,Lab)||Lab?.BoardAiAvailable!=true||Lab.Has(XgBoardRecords.Node))return;
            owner.Open();
            if(owner.View==null)return;
            owner.View.ShowTab("tree");
            if(owner.View.Tree?.FocusNodeDetails(XgBoardRecords.Node)!=true)
                owner.View.ShowToast(T("暂时无法找到「对局记录」科技。","Game records technology is currently unavailable."),3f);
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
                case "gomoku": return T("五子棋\n先在横、竖或斜线上连成五颗子的人赢。\n\n• 15×15 棋盘，黑先白后，轮流在交叉点上放一颗子。\n• 子放下就不动，也不会被吃。\n• 任意方向连成五颗就赢。\n• 下满还没人连五，算平局。\n\n这一局：不设禁手，不限时。\n\n和它下：第 2 阶段起，可以在右上角把对手从「电脑」换成它。和电脑下只是消遣；和它下，第一次赢或输各得一张卡片。在科技里买了「对局记录」后，之后新开的同一种棋，每完成 3 局就打包一份训练数据，放进资料文件夹。","Gomoku\nFive in a row, across, down or diagonally, wins.\n\n• 15×15 board; Black first, then take turns placing one stone on an intersection.\n• Stones never move and are never captured.\n• Five in a row in any direction wins.\n• A full board with no five is a draw.\n\nThis game: no restricted moves, no clock.\n\nWith it: from stage 2, switch the opponent from Computer to your AI at the top right. Games against the computer are just for fun; your first win and first loss against it each earn a card. Once you buy Game records in Tech, every 3 newly started games of the same type against your AI create a training file in your Files folder.");
                case "xiangqi_endgame": return T("象棋残局\n红方先走，在规定步数内将死黑方。\n\n• 车走直线，格数不限；马走「日」字，马腿被挡住就不能走。\n• 炮走法和车一样，吃子时中间必须隔一个子。\n• 相走「田」字，不能过河；士和将只在九宫里走，士斜走一格，将直走一格。\n• 兵过河后可以横走，永远不能后退。\n• 将帅不能在同一条线上直接照面。\n\n这一局：每局一个残局，限 10 步。你走红、它走黑；也可以让它走红，你来防。\n\n和它下：在科技里买了「对局记录」后，新开的 AI 对局每完成 3 局打包一份象棋训练数据。","Xiangqi endgame\nRed moves first and must checkmate Black within the move limit.\n\n• Chariots move straight any distance; horses move one-then-diagonal and are blocked by a piece at the leg.\n• Cannons move like chariots but capture by jumping exactly one piece.\n• Elephants move two diagonally and never cross the river; advisors and the general stay in the palace, advisors diagonal, general straight.\n• Soldiers may move sideways after crossing the river and never retreat.\n• The two generals may not face each other on an open file.\n\nThis game: one endgame per game, 10 moves. You play Red and it plays Black, or let it attack while you defend.\n\nWith it: once you buy Game records in Tech, every 3 games become a xiangqi training file.");
                case "chess_mate": return T("国际象棋 · 一步杀 / 两步杀\n白方先走，一步或两步将死黑王。\n\n• 王走一格；后横竖斜都能走，格数不限；车横竖；象斜着走；马走「日」字，可以跳子；兵向前一格，吃子时斜着吃。\n• 王被攻击叫「将军」，必须马上解围。\n• 被将军而且躲不开、挡不住、吃不掉，就是「将死」。\n\n这一局：题目来自 Lichess 公开题库，一题一局，你们轮流解，看谁先解出来。\n\n和它下：在科技里买了「对局记录」后，新开的 AI 对局每完成 5 题打包一份象棋训练数据。","Chess · mate in 1 / 2\nWhite to move; checkmate the black king in one or two moves.\n\n• King one square; queen any distance in any direction; rook straight; bishop diagonal; knight in an L and can jump; pawn one square forward, capturing diagonally.\n• A king under attack is in check and must escape at once.\n• In check with no escape, block or capture: checkmate.\n\nThis game: puzzles from the Lichess public database, one per game; take turns and see who solves first.\n\nWith it: once you buy Game records in Tech, every 5 puzzles become a chess training file.");
                case "ludo": return T("飞行棋\n先把自己的飞机全部飞回终点的人赢。\n\n• 掷骰子决定走几格；掷到 5 或 6，才能让一架飞机起飞。\n• 掷到 6 可以再掷一次。\n• 停在和自己同色的格子上，可以往前跳到下一个同色格。\n• 停在对方飞机所在的格子，就把它撞回基地。\n• 进终点要刚好走到，点数多了就往回退。\n\n这一局：两人，每人 2 架飞机，环道 28 格。骰子谁也改不了，要比的是先走哪一架。\n\n和它下：第 2 阶段起，可以在右上角把对手从「电脑」换成它。和电脑下只是消遣；和它下，第一次赢或输各得一张卡片。在科技里买了「对局记录」后，之后新开的同一种棋，每完成 3 局就打包一份训练数据，放进资料文件夹。","Aeroplane chess\nFirst to fly all planes home wins.\n\n• Roll to move; a plane takes off only on a 5 or 6.\n• A 6 earns another roll.\n• Land on your own colour and hop ahead to the next square of that colour.\n• Land on a rival plane and send it back to base.\n• You must reach home exactly; extra pips bounce you back.\n\nThis game: two players, two planes each, a 28-cell loop. Nobody controls the die; the choice is which plane to move.\n\nWith it: from stage 2, switch the opponent from Computer to your AI at the top right. Games against the computer are just for fun; your first win and first loss against it each earn a card. Once you buy Game records in Tech, every 3 newly started games of the same type against your AI create a training file in your Files folder.");
                case "checkers_cn": return T("跳棋\n先把自己的棋子全部移到对面角上的人赢。\n\n• 每回合走一颗子。\n• 可以往相邻的空位走一步。\n• 也可以跳：隔着一颗相邻的棋子（谁的都行），跳到它后面的空位。\n• 跳完还能接着跳，就可以一直连跳。\n• 棋子不会被吃掉。\n\n这一局：两人，每人 6 颗子；每人最多 40 回合，到时进角多的赢。\n\n和它下：第 2 阶段起，可以在右上角把对手从「电脑」换成它。和电脑下只是消遣；和它下，第一次赢或输各得一张卡片。在科技里买了「对局记录」后，之后新开的同一种棋，每完成 3 局就打包一份训练数据，放进资料文件夹。","Chinese checkers\nFirst to move every piece into the opposite corner wins.\n\n• Move one piece per turn.\n• Step into an adjacent empty hole,\n• or jump over one adjacent piece (anyone's) into the hole behind it.\n• Keep jumping as long as you can: chain jumps are allowed.\n• Pieces are never captured.\n\nThis game: two players, six pieces each; after 40 turns each, most pieces in the corner wins.\n\nWith it: from stage 2, switch the opponent from Computer to your AI at the top right. Games against the computer are just for fun; your first win and first loss against it each earn a card. Once you buy Game records in Tech, every 3 newly started games of the same type against your AI create a training file in your Files folder.");
                case "monopoly": return T("大富翁\n8 轮后，现金加地产最多的人赢；谁先破产谁输。\n\n• 掷骰子走格。\n• 停在没人买的地上，可以买下。\n• 停在对方的地上，要付过路费。\n• 停在「机会」格抽一张卡，有好有坏。\n• 每经过起点，领 ¥200。\n\n这一局：两人，20 格地图，每人 8 轮；开局各 ¥1,500 游戏币，和你的真钱无关。\n\n和它下：第 2 阶段起，可以在右上角把对手从「电脑」换成它。和电脑下只是消遣；和它下，第一次赢或输各得一张卡片。在科技里买了「对局记录」后，之后新开的同一种棋，每完成 3 局就打包一份训练数据，放进资料文件夹。","Monopoly-style\nAfter 8 rounds, most cash plus property wins; go bankrupt and you lose.\n\n• Roll and move.\n• Land on unowned land and you may buy it.\n• Land on a rival's land and pay rent.\n• A Chance square draws a card, good or bad.\n• Collect ¥200 every time you pass Start.\n\nThis game: two players, 20 squares, 8 rounds each; ¥1,500 in play money each, not your real money.\n\nWith it: from stage 2, switch the opponent from Computer to your AI at the top right. Games against the computer are just for fun; your first win and first loss against it each earn a card. Once you buy Game records in Tech, every 3 newly started games of the same type against your AI create a training file in your Files folder.");
                case "go": return T("围棋 · 9 路\n地盘加上棋子更多的一方赢。\n\n• 你执黑先下，轮流在交叉点上放一颗子。\n• 一块棋周围的空点叫「气」，气被对方全部堵住，这块棋就被提掉。\n• 不能下在一放下就没气的地方，除非这一手能提掉对方的子。\n• 不能马上提回刚被提的那一颗子（打劫要先在别处下一手）。\n• 双方连续各停一手，就结束数子。\n\n这一局：按棋子和围住的空点数子，白方贴 6.5 目。\n\n和它下：第 2 阶段起，可以在右上角把对手从「电脑」换成它。和电脑下只是消遣；和它下，第一次赢或输各得一张卡片。在科技里买了「对局记录」后，之后新开的同一种棋，每完成 3 局就打包一份训练数据，放进资料文件夹。","Go · 9×9\nMost stones plus territory wins.\n\n• You play Black first; take turns placing a stone on an intersection.\n• The empty points around a group are its liberties; fill them all and the group is captured.\n• You may not play where your stone would have no liberties, unless it captures.\n• You may not immediately recapture a single stone just taken (ko: play elsewhere first).\n• Two passes in a row end the game.\n\nThis game: area scoring, 6.5 komi for White.\n\nWith it: from stage 2, switch the opponent from Computer to your AI at the top right. Games against the computer are just for fun; your first win and first loss against it each earn a card. Once you buy Game records in Tech, every 3 newly started games of the same type against your AI create a training file in your Files folder.");
                case "chess": return T("国际象棋\n将死对方的王就赢。\n\n• 王走一格；后横竖斜都能走，格数不限；车走横竖，象走斜线；马走「日」字，可以跳过别的子；兵向前一格，第一步可走两格，吃子时斜着吃。\n• 王被攻击叫「将军」，必须马上解围；不能主动走进被攻击的格子。\n• 被将军且无路可逃，就是将死；没被将军却无棋可走，算和棋。\n• 支持王车易位、吃过路兵、兵到底线升变。\n\n这一局：你执白先行。\n\n和它下：第 2 阶段起，可以在右上角把对手从「电脑」换成它。和电脑下只是消遣；和它下，第一次赢或输各得一张卡片。在科技里买了「对局记录」后，之后新开的同一种棋，每完成 3 局就打包一份训练数据，放进资料文件夹。","Chess\nCheckmate the king to win.\n\n• King one square; queen any distance in any direction; rook straight, bishop diagonal; knight in an L and can jump; pawn one square forward (two on its first move), capturing diagonally.\n• A king under attack is in check and must escape at once; it may never move into attack.\n• In check with no escape is checkmate; no legal move while not in check is a draw.\n• Castling, en passant and promotion apply.\n\nThis game: you play White and move first.\n\nWith it: from stage 2, switch the opponent from Computer to your AI at the top right. Games against the computer are just for fun; your first win and first loss against it each earn a card. Once you buy Game records in Tech, every 3 newly started games of the same type against your AI create a training file in your Files folder.");
                default:return Title;
            }
        }
    }
}
