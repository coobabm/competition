using System.Collections.Generic;
using LingGuangV05.Core.Games;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.XingGuang;
using Michsky.DreamOS;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LingGuangV05.Core;
namespace LingGuangV05.Desktop.Games
{
    /// <summary>Existing chess rules and AI, with original 2D artwork and board-local presentation/input.</summary>
    public sealed class ChessPanel : HubGame
    {
        TMP_Text status, again;
        readonly Image[,] squares = new Image[8,8];
        readonly ChessPieceView[,] pieces = new ChessPieceView[8,8];
        readonly Image[,] dots = new Image[8,8];
        readonly XgRingGraphic[,] captureRings = new XgRingGraphic[8,8];
        ChessGame game = new ChessGame();
        ChessSprites art;
        ChessFeedback feedback;
        RectTransform board, areaRoot;
        WindowManager hostWindow;
        Image dragGhost;
        int selFile = -1, selRank = -1, checkedSquare = -1, hiddenSquare = -1;
        readonly List<ChessMove> targets = new List<ChessMove>();
        float aiAt = -1, ignoreClickUntil;
        int wins, losses, draws, dragPly;
        bool counted, dragging, animating, wasVisible, resetting;
        readonly List<string> log = new List<string>();
        int materialBefore;
        bool playerCaptured, playerChecked;

        static readonly Color Light = new Color32(241,245,229,255), Dark = new Color32(148,183,116,255);
        static readonly Color LastLight = new Color32(226,241,166,255), LastDark = new Color32(176,207,108,255), Pick = new Color32(196,229,103,255);
        public override string Id => "chess";
        public override string Title => Lang.T("国际象棋");
        public override string Blurb => AiPlays
            ? T("和" + AiName + "下国际象棋。它一边下一边解说；角落里还留着深蓝那行字。", "Chess against " + AiName + ". It comments as it plays; Deep Blue's line is still in the corner.")
            : Lang.T("和电脑下国际象棋。它是 1997 年「深蓝」那一套：不学习，只往后硬算几步。");
        string Opponent => AiPlays ? AiName : Lang.T("电脑");
        int Depth => AiPlays ? XgGames.ChessDepth(Stage, Lab.GameAccuracy(Id)) : 3;
        public override string Glyph => "王";
        public override Color Accent => new Color32(86,150,72,255);

        public override void Build(RectTransform area)
        {
            areaRoot = area;
            hostWindow = GetComponentInParent<WindowManager>(true);
            PrologueDesk.Fill(area, new Color32(239,244,232,255));
            art = new ChessSprites();
            board = PrologueDesk.Centered("Chess Board", area, new Vector2(-190,0), new Vector2(600,600));
            var frame = PrologueDesk.Fill(PrologueDesk.Centered("Board Frame", board, Vector2.zero, new Vector2(616,616)), new Color32(70,103,68,255), false);
            var shadow = frame.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0,0,0,.18f); shadow.effectDistance = new Vector2(0,-6);
            for (int f = 0; f < 8; f++)
            for (int r = 0; r < 8; r++)
            {
                int cf = f, cr = r;
                var sq = PrologueDesk.Centered("Square " + (char)('a' + f) + (r + 1), board, SquarePosition(f,r), new Vector2(75,75));
                squares[f,r] = PrologueDesk.Fill(sq, (f+r)%2 == 0 ? Dark : Light);
                var button = sq.gameObject.AddComponent<Button>(); button.targetGraphic = squares[f,r]; button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => Click(cf,cr));
                var input = sq.gameObject.AddComponent<ChessSquareInput>(); input.panel = this; input.file = f; input.rank = r;
                var visual = PrologueDesk.Centered("Piece Visual", sq, Vector2.zero, new Vector2(67,67));
                pieces[f,r] = visual.gameObject.AddComponent<ChessPieceView>(); pieces[f,r].Build();
                dots[f,r] = PrologueDesk.Fill(PrologueDesk.Centered("Legal Move Dot", sq, Vector2.zero, new Vector2(16,16)), Color.clear, false);
                dots[f,r].sprite = PrologueDesk.Circle();
                var ring = PrologueDesk.Centered("Legal Capture Ring", sq, Vector2.zero, new Vector2(68,68)).gameObject.AddComponent<XgRingGraphic>();
                ring.Width = 3; ring.color = Color.clear; ring.raycastTarget = false; captureRings[f,r] = ring;
                if (r == 0) Text(PrologueDesk.Rect("File", sq, Vector2.zero, Vector2.one, new Vector2(0,1), new Vector2(-3,0)), ((char)('a'+f)).ToString(), 11, (f+r)%2 == 0 ? Light : Dark, TextAlignmentOptions.BottomRight);
                if (f == 0) Text(PrologueDesk.Rect("Rank", sq, Vector2.zero, Vector2.one, new Vector2(3,0), new Vector2(0,-1)), (r+1).ToString(), 11, (f+r)%2 == 0 ? Light : Dark, TextAlignmentOptions.TopLeft);
            }
            Text(PrologueDesk.Rect("Deep Blue", board, new Vector2(1,0), new Vector2(1,0), new Vector2(-300,-28), new Vector2(0,-7)), "IBM Deep Blue, 1997", 12, new Color32(116,132,108,255), TextAlignmentOptions.BottomRight);
            var side = PrologueDesk.Rect("Side", area, new Vector2(1,0), new Vector2(1,1), new Vector2(-370,30), new Vector2(-30,-30));
            status = Text(PrologueDesk.Rect("Status", side, new Vector2(0,.3f), Vector2.one, Vector2.zero, Vector2.zero), "", 20, new Color32(36,56,43,255));
            Button(side, "Again", new Vector2(0,.12f), new Vector2(1,.22f), Accent, NewGame, out again);
            feedback = board.gameObject.AddComponent<ChessFeedback>(); feedback.Init(board,font,BoardVisible);
            dragGhost = PrologueDesk.Fill(PrologueDesk.Centered("Dragged Piece", board, Vector2.zero, new Vector2(69,69)), Color.white, false);
            dragGhost.preserveAspect = true; dragGhost.gameObject.SetActive(false);
            Draw();
        }

        public override void Refresh() => Draw();
        bool HumanTurn => !animating && game.Result == ChessGame.Ongoing && aiAt < 0 && game.SideToMove == ChessGame.White;
        bool BoardVisible()
        {
            if (!isActiveAndEnabled || areaRoot == null || !areaRoot.gameObject.activeInHierarchy) return false;
            var group = hostWindow != null ? hostWindow.GetComponent<CanvasGroup>() : null;
            return hostWindow == null || hostWindow.isOn && (group == null || group.alpha > .02f);
        }
        void Deselect() { selFile = selRank = -1; targets.Clear(); }
        public static Vector2 SquarePosition(int file,int rank) => new Vector2(-300 + 75 * (file+.5f), -300 + 75 * (rank+.5f));
        public static bool TrySquare(Vector2 local, out int file, out int rank)
        {
            file = rank = -1;
            if (float.IsNaN(local.x) || float.IsNaN(local.y) || local.x < -300 || local.x >= 300 || local.y < -300 || local.y >= 300) return false;
            file = Mathf.FloorToInt((local.x+300)/75); rank = Mathf.FloorToInt((local.y+300)/75); return true;
        }

        void Select(int f,int r)
        {
            Deselect();
            if (!HumanTurn || game[f,r] == '.' || !char.IsUpper(game[f,r])) { Draw(); return; }
            selFile=f; selRank=r;
            foreach (var move in game.LegalMoves()) if (move.FromFile==f && move.FromRank==r) targets.Add(move);
            if (feedback != null) feedback.Selected(pieces[f,r].Visual);
            Draw();
        }
        void Click(int f,int r)
        {
            if (!BoardVisible() || !HumanTurn || dragging || Time.unscaledTime < ignoreClickUntil) return;
            if (TryTarget(f,r,out var move)) { PlayMove(move,true); return; }
            Select(f,r);
        }
        bool TryTarget(int f,int r,out ChessMove move)
        {
            move=ChessMove.None;
            if (selFile<0 || !HumanTurn) return false;
            foreach (var candidate in targets)
                if (candidate.ToFile==f && candidate.ToRank==r && (!candidate.IsPromotion || candidate.Promotion=='q')) { move=candidate; return true; }
            return false;
        }

        public void BeginPieceDrag(int f,int r,PointerEventData e)
        {
            if (!BoardVisible() || !HumanTurn || game[f,r]=='.' || !char.IsUpper(game[f,r])) return;
            CancelPieceDrag(); Select(f,r); dragging=true; dragPly=game.Plies;
            dragGhost.sprite=art.Get(game[f,r]); dragGhost.gameObject.SetActive(true); dragGhost.transform.SetAsLastSibling();
            pieces[f,r].SetSuppressed(true); DragPiece(e);
        }
        public void DragPiece(PointerEventData e)
        {
            if (!dragging || !BoardVisible()) { CancelPieceDrag(); return; }
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(board,e.position,e.pressEventCamera,out var local))
            {
                dragGhost.rectTransform.anchoredPosition=local;
                if (feedback != null) feedback.Trail(local);
            }
        }
        public void DropPiece(int f,int r,PointerEventData e)
        {
            if (!dragging || !BoardVisible() || dragPly!=game.Plies || e.pointerDrag==null) { CancelPieceDrag(); return; }
            var source=e.pointerDrag.GetComponent<ChessSquareInput>();
            if (source==null || source.panel!=this) { CancelPieceDrag(); return; }
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(board,e.position,e.pressEventCamera,out var local)
                || !TrySquare(local,out int hitFile,out int hitRank) || hitFile!=f || hitRank!=r)
            { CancelPieceDrag(); return; }
            bool legal=TryTarget(f,r,out var move);
            CancelPieceDrag(); ignoreClickUntil=Time.unscaledTime+.10f;
            if (legal) PlayMove(move,true);
        }
        public void CancelPieceDrag()
        {
            if (!dragging) return;
            dragging=false;
            if (dragGhost!=null) dragGhost.gameObject.SetActive(false);
            ignoreClickUntil=Time.unscaledTime+.10f;
            Draw();
        }

        void PlayMove(ChessMove move,bool human)
        {
            char moving=game[move.FromFile,move.FromRank];
            int victimRank=move.IsEnPassant?move.FromRank:move.ToRank;
            char captured=move.IsCapture?game[move.ToFile,victimRank]:'.';
            if (human) { materialBefore=Material(); playerCaptured=move.IsCapture; }
            if (!game.Move(move)) return;
            if (human) playerChecked=game.InCheck;
            log.Add(game.LastMove.ToString()); Moved(game.Plies);
            if (!human)
            {
                int swing=Material()-materialBefore;
                var situation=swing<=-300?XgMoveSituation.PlayerBlunder:swing>=300?XgMoveSituation.AiBlunder
                    :game.InCheck?XgMoveSituation.Check:move.IsCapture?XgMoveSituation.AiCapture
                    :playerChecked?XgMoveSituation.Checked:playerCaptured?XgMoveSituation.PlayerCapture:XgMoveSituation.Quiet;
                Comment(situation,game.Plies);
            }
            Deselect(); hiddenSquare=move.To; animating=true; aiAt=-1; Draw();
            feedback.Move(art.Get(moving),art.Get(captured),SquarePosition(move.FromFile,move.FromRank),SquarePosition(move.ToFile,move.ToRank),
                SquarePosition(move.ToFile,victimRank),char.IsUpper(moving),()=>
                {
                    animating=false; hiddenSquare=-1;
                    if(resetting)return;
                    if (human && game.Result==ChessGame.Ongoing) aiAt=Time.unscaledTime+.35f;
                    Draw();
                    if (game.InCheck && checkedSquare>=0 && game.Result==ChessGame.Ongoing) feedback.Check(SquarePosition(checkedSquare%8,checkedSquare/8));
                    Finish(); Draw();
                });
        }
        void Update()
        {
            bool visible=BoardVisible();
            if (!visible)
            {
                if (wasVisible) { CancelPieceDrag(); if (feedback!=null) feedback.ClearFeedback(); }
                wasVisible=false; return;
            }
            wasVisible=true;
            if (animating || feedback!=null&&feedback.Busy || aiAt<0 || Time.unscaledTime<aiAt) return;
            aiAt=-1;
            if (game.Result==ChessGame.Ongoing)
            {
                var move=Reply(); if (move.IsValid) { PlayMove(move,false); return; }
            }
            Finish(); Draw();
        }
        ChessMove Reply()
        {
            if (!AiPlays) return game.Best(3);
            if (Rng.NextDouble()<XgGames.ChessRandomChance(Stage,Lab.GameAccuracy(Id)))
            { var moves=game.LegalMoves(); if(moves.Count>0)return moves[Rng.Next(moves.Count)]; }
            return game.Best(Depth);
        }
        int Material()
        {
            int sum=0;
            for(int f=0;f<8;f++) for(int r=0;r<8;r++)
            { char p=game[f,r]; int v=char.ToLowerInvariant(p) switch{'p'=>100,'n'=>320,'b'=>330,'r'=>500,'q'=>900,_=>0}; sum+=char.IsUpper(p)?v:-v; }
            return sum;
        }
        void NewGame()
        {
            // A committed mate still counts if the player restarts before its short celebration finishes.
            if(game.Result!=ChessGame.Ongoing&&!counted)Finish();
            resetting=true; CancelPieceDrag();
            if(feedback!=null)feedback.ClearFeedback();
            game=new ChessGame(); aiAt=-1; animating=false; hiddenSquare=-1; counted=false;
            log.Clear(); ClearComment(); Deselect(); resetting=false; Draw();
        }
        void Finish()
        {
            if(game.Result==ChessGame.Ongoing){counted=false;return;}
            if(counted)return;
            counted=true;
            var outcome=game.Result==ChessGame.WhiteWins?XgGameOutcome.PlayerWon:game.Result==ChessGame.BlackWins?XgGameOutcome.PlayerLost:XgGameOutcome.Draw;
            if(outcome==XgGameOutcome.PlayerWon)wins++;else if(outcome==XgGameOutcome.PlayerLost)losses++;else draws++;
            ClearComment();Finished(outcome,game.Plies,log);
            if(feedback!=null)feedback.Outcome(game.Result,Vector2.zero);
        }
        void Draw()
        {
            if(status==null)return;
            checkedSquare=-1;
            if(game.InCheck)
            {
                char king=game.SideToMove==ChessGame.White?'K':'k';
                for(int f=0;f<8;f++)for(int r=0;r<8;r++)if(game[f,r]==king)checkedSquare=r*8+f;
            }
            var last=game.LastMove;
            for(int f=0;f<8;f++)for(int r=0;r<8;r++)
            {
                bool dark=(f+r)%2==0;
                bool previous=last.IsValid&&(last.FromFile==f&&last.FromRank==r||last.ToFile==f&&last.ToRank==r);
                bool selected=f==selFile&&r==selRank;
                squares[f,r].color=selected?Pick:previous?(dark?LastDark:LastLight):dark?Dark:Light;
                char p=game[f,r];
                pieces[f,r].SetPiece(p,art.Get(p),checkedSquare==r*8+f,selected);
                pieces[f,r].SetSuppressed(hiddenSquare==r*8+f || dragging&&selected);
                bool target=HumanTurn&&targets.Exists(m=>m.ToFile==f&&m.ToRank==r);
                dots[f,r].color=target&&p=='.'?new Color(.12f,.24f,.13f,.43f):Color.clear;
                captureRings[f,r].color=target&&p!='.'?new Color(.22f,.45f,.13f,.85f):Color.clear;
            }
            string state;
            if(game.Result!=ChessGame.Ongoing)state=(game.Result==ChessGame.WhiteWins?Lang.T("你赢了："):game.Result==ChessGame.BlackWins?Opponent+Lang.T("赢了："):Lang.T("和棋："))+T(game.ResultReason,game.ResultReasonEnglish)+"。";
            else if(aiAt>=0||animating&&game.SideToMove==ChessGame.Black)state=Opponent+Lang.T("在算……");
            else state=(game.InCheck?Lang.T("将军！"):"")+T("你执白。点击或拖动棋子，小点标记合法落点。","You play white. Click or drag; dots show legal moves.");
            string opponent=Lang.T("对手：")+(AiPlays?AiName+Lang.T(" · 往后算 ")+Depth+T(" 步"," plies ahead"):Lang.T("深蓝式程序 · 往后算 3 步"));
            status.text="<b><size=26>"+Title+"</size></b>\n"+opponent+"\n\n"+state+"\n\n"+Lang.T("胜 ")+wins+Lang.T(" · 负 ")+losses+Lang.T(" · 和 ")+draws+LabLines()+
                "\n\n<size=15><color=#56704A>"+Lang.T("它不会学习，每一步都把后面几步全算一遍，按子力打分。1997 年深蓝就是这样赢了卡斯帕罗夫，只是算得深得多。")+"</color></size>";
            again.text=Lang.T("再来一局");
        }
        void OnApplicationFocus(bool focused){if(!focused)CancelPieceDrag();}
        void OnDisable(){CancelPieceDrag();if(feedback!=null)feedback.ClearFeedback();}
        void OnDestroy(){resetting=true;if(feedback!=null)feedback.ClearFeedback();art?.Dispose();}
    }
}
