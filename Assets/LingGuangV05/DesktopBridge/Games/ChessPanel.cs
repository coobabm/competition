using System.Collections.Generic;
using LingGuangV05.Core.Games;
using LingGuangV05.Desktop.Story;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using LingGuangV05.Core;
namespace LingGuangV05.Desktop.Games
{
    /// <summary>
    /// 国际象棋 against the built-in program: the 1997 Deep Blue recipe in miniature. It learns nothing; it searches a
    /// few moves ahead and counts material. Click a piece, then a highlighted square. Pawns promote to queens.
    /// At stage 6 the lab's AI takes the black pieces: it searches 1–3 plies depending on its sequence accuracy, slips
    /// in a random move now and then while it is weak, and comments on the game with canned lines. A corner of the
    /// board keeps the 1997 credit. Every few moves and every finished game feed the lab's reasoning dataset.
    /// </summary>
    public sealed class ChessPanel : HubGame
    {
        TMP_Text status, again;
        readonly Image[,] squares = new Image[8, 8];
        readonly TMP_Text[,] pieces = new TMP_Text[8, 8];
        readonly Image[,] tokens = new Image[8, 8];
        readonly Image[,] dots = new Image[8, 8];
        ChessGame game = new ChessGame();
        int selFile = -1, selRank = -1;
        readonly List<ChessMove> targets = new List<ChessMove>();
        float aiAt = -1;
        int wins, losses, draws;
        bool counted;
        readonly List<string> log = new List<string>();
        // The player's last move, for the commentary on the reply: material before it, and whether it captured or checked.
        int materialBefore;
        bool playerCaptured, playerChecked;

        static readonly Color Light = new Color32(238, 238, 210, 255), Dark = new Color32(118, 150, 86, 255);
        static readonly Color LastLight = new Color32(246, 246, 130, 255), LastDark = new Color32(186, 202, 68, 255), Pick = new Color32(130, 170, 230, 255);

        public override string Id => "chess";
        public override string Title => Lang.T("国际象棋");
        public override string Blurb => AiPlays
            ? T("和" + AiName + "下国际象棋。它一边下一边解说；角落里还留着深蓝那行字。", "Chess against " + AiName + ". It comments as it plays; Deep Blue's line is still in the corner.")
            : Lang.T("和电脑下国际象棋。它是 1997 年「深蓝」那一套：不学习，只往后硬算几步。");

        /// <summary>The black player: the built-in Deep Blue-style program until stage 6, then the lab's AI.</summary>
        string Opponent => AiPlays ? AiName : Lang.T("电脑");

        int Depth => AiPlays ? XgGames.ChessDepth(Stage, Lab.GameAccuracy(Id)) : 3;
        public override string Glyph => "王";
        public override Color Accent => new Color32(90, 110, 150, 255);

        public override void Build(RectTransform area)
        {
            PrologueDesk.Fill(area, new Color32(234, 236, 242, 255));
            const float size = 600;
            var board = PrologueDesk.Centered("Board", area, new Vector2(-190, 0), new Vector2(size, size));
            float step = size / 8, half = size / 2;
            for (int f = 0; f < 8; f++)
                for (int r = 0; r < 8; r++)
                {
                    int cf = f, cr = r;
                    var sq = PrologueDesk.Centered("Square", board, new Vector2(-half + step * (f + .5f), -half + step * (r + .5f)), new Vector2(step, step));
                    squares[f, r] = PrologueDesk.Fill(sq, (f + r) % 2 == 0 ? Dark : Light);
                    var b = sq.gameObject.AddComponent<Button>(); b.targetGraphic = squares[f, r]; b.transition = Selectable.Transition.None;
                    b.onClick.AddListener(() => Click(cf, cr));
                    dots[f, r] = PrologueDesk.Fill(PrologueDesk.Centered("Dot", sq, Vector2.zero, new Vector2(step * .3f, step * .3f)), Color.clear, false);
                    dots[f, r].sprite = PrologueDesk.Circle();
                    // Pieces are round tokens with the Chinese name (王 后 车 象 马 兵): the CJK font has no chess glyphs.
                    tokens[f, r] = PrologueDesk.Fill(PrologueDesk.Centered("Token", sq, Vector2.zero, new Vector2(step * .8f, step * .8f)), Color.clear, false);
                    tokens[f, r].sprite = PrologueDesk.Circle();
                    var ring = tokens[f, r].gameObject.AddComponent<Outline>(); ring.effectColor = new Color32(60, 60, 60, 200); ring.effectDistance = new Vector2(1.5f, -1.5f);
                    pieces[f, r] = Text(PrologueDesk.Rect("Piece", sq, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", step * .42f, Color.black, TextAlignmentOptions.Center);
                    pieces[f, r].fontStyle = FontStyles.Bold;
                    if (r == 0) Text(PrologueDesk.Rect("File", sq, Vector2.zero, Vector2.one, new Vector2(0, 2), new Vector2(-4, 0)), ((char)('a' + f)).ToString(), 13, (f + r) % 2 == 0 ? Light : Dark, TextAlignmentOptions.BottomRight);
                    if (f == 0) Text(PrologueDesk.Rect("Rank", sq, Vector2.zero, Vector2.one, new Vector2(4, 0), new Vector2(0, -2)), (r + 1).ToString(), 13, (f + r) % 2 == 0 ? Light : Dark, TextAlignmentOptions.TopLeft);
                }
            // Design v1.1 §10.3: a small line in the corner of the board.
            Text(PrologueDesk.Rect("Deep Blue", board, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-300, -26), new Vector2(0, -4)), "IBM Deep Blue, 1997", 13, new Color32(130, 138, 156, 255), TextAlignmentOptions.BottomRight);
            var side = PrologueDesk.Rect("Side", area, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-370, 30), new Vector2(-30, -30));
            status = Text(PrologueDesk.Rect("Status", side, new Vector2(0, .3f), Vector2.one, Vector2.zero, Vector2.zero), "", 20, new Color32(30, 36, 52, 255));
            Button(side, "Again", new Vector2(0, .12f), new Vector2(1, .22f), Accent, () => { game = new ChessGame(); aiAt = -1; log.Clear(); ClearComment(); Deselect(); Draw(); }, out again);
            Draw();
        }

        public override void Refresh() => Draw();

        bool HumanTurn => game.Result == ChessGame.Ongoing && aiAt < 0 && game.SideToMove == ChessGame.White;

        void Deselect() { selFile = selRank = -1; targets.Clear(); }

        void Click(int f, int r)
        {
            if (!HumanTurn) return;
            if (selFile >= 0)
            {
                foreach (var m in targets)
                    if (m.ToFile == f && m.ToRank == r)
                    {
                        materialBefore = Material();
                        playerCaptured = m.IsCapture;
                        game.Move(m.FromFile, m.FromRank, f, r, 'q');
                        playerChecked = game.InCheck;
                        log.Add(game.LastMove.ToString());
                        Moved(game.Plies);
                        Deselect();
                        if (game.Result == ChessGame.Ongoing) aiAt = Time.unscaledTime + .4f;
                        Finish(); Draw();
                        return;
                    }
            }
            char p = game[f, r];
            Deselect();
            if (p != '.' && char.IsUpper(p))
            {
                selFile = f; selRank = r;
                foreach (var m in game.LegalMoves()) if (m.FromFile == f && m.FromRank == r) targets.Add(m);
            }
            Draw();
        }

        void Update()
        {
            if (aiAt < 0 || Time.unscaledTime < aiAt) return;
            aiAt = -1;
            if (game.Result == ChessGame.Ongoing)
            {
                var m = Reply();
                if (m.IsValid)
                {
                    bool capture = m.IsCapture;
                    game.Move(m);
                    log.Add(m.ToString());
                    Moved(game.Plies);
                    int swing = Material() - materialBefore;
                    var situation = swing <= -300 ? XgMoveSituation.PlayerBlunder : swing >= 300 ? XgMoveSituation.AiBlunder
                        : game.InCheck ? XgMoveSituation.Check : capture ? XgMoveSituation.AiCapture
                        : playerChecked ? XgMoveSituation.Checked : playerCaptured ? XgMoveSituation.PlayerCapture : XgMoveSituation.Quiet;
                    Comment(situation, game.Plies);
                }
            }
            Finish(); Draw();
        }

        /// <summary>The program searches 3 plies; the lab's AI searches by its accuracy and sometimes just moves something.</summary>
        ChessMove Reply()
        {
            if (!AiPlays) return game.Best(3);
            if (Rng.NextDouble() < XgGames.ChessRandomChance(Stage, Lab.GameAccuracy(Id)))
            {
                var moves = game.LegalMoves();
                if (moves.Count > 0) return moves[Rng.Next(moves.Count)];
            }
            return game.Best(Depth);
        }

        /// <summary>White's material minus Black's, in centipawns.</summary>
        int Material()
        {
            int sum = 0;
            for (int f = 0; f < 8; f++)
                for (int r = 0; r < 8; r++)
                {
                    char p = game[f, r];
                    int v = char.ToLowerInvariant(p) switch { 'p' => 100, 'n' => 320, 'b' => 330, 'r' => 500, 'q' => 900, _ => 0 };
                    sum += char.IsUpper(p) ? v : -v;
                }
            return sum;
        }

        void Finish()
        {
            if (game.Result == ChessGame.Ongoing) { counted = false; return; }
            if (counted) return;
            counted = true;
            var outcome = game.Result == ChessGame.WhiteWins ? XgGameOutcome.PlayerWon : game.Result == ChessGame.BlackWins ? XgGameOutcome.PlayerLost : XgGameOutcome.Draw;
            if (outcome == XgGameOutcome.PlayerWon) wins++; else if (outcome == XgGameOutcome.PlayerLost) losses++; else draws++;
            ClearComment();
            Finished(outcome, game.Plies, log);
        }

        static string PieceGlyph(char p)
        {
            switch (char.ToLowerInvariant(p))
            {
                case 'k': return Lang.T("王"); case 'q': return Lang.T("后"); case 'r': return Lang.T("车");
                case 'b': return Lang.T("象"); case 'n': return Lang.T("马"); case 'p': return Lang.T("兵");
                default: return "";
            }
        }

        void Draw()
        {
            if (status == null) return;
            var last = game.LastMove;
            for (int f = 0; f < 8; f++)
                for (int r = 0; r < 8; r++)
                {
                    bool dark = (f + r) % 2 == 0;
                    bool lastSquare = last.IsValid && (last.FromFile == f && last.FromRank == r || last.ToFile == f && last.ToRank == r);
                    squares[f, r].color = f == selFile && r == selRank ? Pick : lastSquare ? (dark ? LastDark : LastLight) : dark ? Dark : Light;
                    char p = game[f, r];
                    pieces[f, r].text = PieceGlyph(p);
                    bool white = char.IsUpper(p);
                    pieces[f, r].color = white ? new Color32(30, 30, 34, 255) : new Color32(246, 240, 226, 255);
                    tokens[f, r].color = p == '.' ? Color.clear : white ? new Color32(250, 248, 240, 255) : new Color32(36, 36, 42, 255);
                    bool target = targets.Exists(m => m.ToFile == f && m.ToRank == r);
                    dots[f, r].color = target ? new Color(0, 0, 0, p == '.' ? .22f : .4f) : Color.clear;
                }
            string state;
            if (game.Result != ChessGame.Ongoing)
                state = (game.Result == ChessGame.WhiteWins ? Lang.T("你赢了：") : game.Result == ChessGame.BlackWins ? Opponent + Lang.T("赢了：") : Lang.T("和棋：")) + T(game.ResultReason, game.ResultReasonEnglish) + "。";
            else if (aiAt >= 0) state = Opponent + Lang.T("在算……");
            else state = (game.InCheck ? Lang.T("将军！") : "") + Lang.T("你执白，先走。点棋子，再点亮着的格子。");
            string opponent = Lang.T("对手：") + (AiPlays ? AiName + Lang.T(" · 往后算 ") + Depth + T(" 步", " plies ahead") : Lang.T("深蓝式程序 · 往后算 3 步"));
            status.text = "<b><size=26>" + Title + "</size></b>\n" + opponent + "\n\n" + state + "\n\n" + Lang.T("胜 ") + wins + Lang.T(" · 负 ") + losses + Lang.T(" · 和 ") + draws + LabLines() +
                "\n\n<size=15><color=#4A5670>" + Lang.T("它不会学习，每一步都把后面几步全算一遍，按子力打分。1997 年深蓝就是这样赢了卡斯帕罗夫，只是算得深得多。") + "</color></size>";
            again.text = Lang.T("再来一局");
        }
    }
}
