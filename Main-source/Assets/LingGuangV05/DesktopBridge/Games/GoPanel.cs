using System.Collections.Generic;
using LingGuangV05.Core.Games;
using LingGuangV05.Desktop.Story;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Games
{
    /// <summary>
    /// 围棋 9 路 against the built-in program of 2016: it counts liberties, captures, saves stones in atari and keeps
    /// its eyes, nothing more. Two passes end the game; area scoring with 6.5 komi. From stage 4 the lab's AI takes
    /// over the white stones: it plays the program's move as often as its stage and go accuracy allow and a beginner's
    /// move otherwise. Every few moves and every finished game feed the 围棋局面 dataset.
    /// </summary>
    public sealed class GoPanel : HubGame
    {
        TMP_Text status, again, pass;
        readonly Image[,] stones = new Image[GoGame.Size, GoGame.Size];
        readonly Image[,] marks = new Image[GoGame.Size, GoGame.Size];
        GoGame game = new GoGame();
        readonly List<string> log = new List<string>();
        float aiAt = -1;
        int wins, losses;

        public override string Id => "go";
        public override string Title => T("围棋", "Go");
        public override string Blurb => AiPlays
            ? T("9 路小棋盘。现在执白的是" + AiName + "：它从「围棋局面」里学会了数气，越练越稳。", "A small 9 × 9 board. White is now " + AiName + ": it learned to count liberties from the Go positions it labelled, and steadies as it trains.")
            : T("9 路小棋盘，和电脑的围棋程序下。今年 3 月 AlphaGo 4:1 赢了李世石，可这个程序还只会数气。", "A small 9 × 9 board against the computer's Go program. In March AlphaGo beat Lee Sedol 4–1; this program can only count liberties.");

        /// <summary>The white player: the built-in program until stage 4, then the lab's AI.</summary>
        string Opponent => AiPlays ? AiName : T("电脑", "The computer");
        public override string Glyph => "◉";
        public override Color Accent => new Color32(70, 110, 90, 255);

        public override void Build(RectTransform area)
        {
            PrologueDesk.Fill(area, new Color32(232, 236, 230, 255));
            const float size = 600;
            var board = PrologueDesk.Centered("Board", area, new Vector2(-190, 0), new Vector2(size, size));
            PrologueDesk.Fill(board, new Color32(220, 179, 107, 255));
            float step = size / GoGame.Size, half = size / 2;
            var ink = new Color32(70, 45, 20, 255);
            for (int i = 0; i < GoGame.Size; i++)
            {
                PrologueDesk.Fill(PrologueDesk.Centered("H", board, new Vector2(0, -half + step * (i + .5f)), new Vector2(size - step, 2)), ink, false);
                PrologueDesk.Fill(PrologueDesk.Centered("V", board, new Vector2(-half + step * (i + .5f), 0), new Vector2(2, size - step)), ink, false);
            }
            // Star points of a 9 × 9 board.
            foreach (var (sx, sy) in new[] { (2, 2), (6, 2), (4, 4), (2, 6), (6, 6) })
            {
                var star = PrologueDesk.Centered("Star", board, new Vector2(-half + step * (sx + .5f), -half + step * (sy + .5f)), new Vector2(10, 10));
                PrologueDesk.Fill(star, ink, false).sprite = PrologueDesk.Circle();
            }
            for (int x = 0; x < GoGame.Size; x++)
                for (int y = 0; y < GoGame.Size; y++)
                {
                    int cx = x, cy = y;
                    var cell = PrologueDesk.Centered("Cell", board, new Vector2(-half + step * (x + .5f), -half + step * (y + .5f)), new Vector2(step, step));
                    var img = PrologueDesk.Fill(cell, new Color(0, 0, 0, 0));
                    var b = cell.gameObject.AddComponent<Button>(); b.targetGraphic = img; b.onClick.AddListener(() => Play(cx, cy));
                    stones[x, y] = PrologueDesk.Fill(PrologueDesk.Centered("Stone", cell, Vector2.zero, new Vector2(step * .9f, step * .9f)), Color.clear, false);
                    stones[x, y].sprite = PrologueDesk.Circle();
                    marks[x, y] = PrologueDesk.Fill(PrologueDesk.Centered("Last", cell, Vector2.zero, new Vector2(step * .25f, step * .25f)), Color.clear, false);
                    marks[x, y].sprite = PrologueDesk.Circle();
                }
            var side = PrologueDesk.Rect("Side", area, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-370, 30), new Vector2(-30, -30));
            status = Text(PrologueDesk.Rect("Status", side, new Vector2(0, .34f), Vector2.one, Vector2.zero, Vector2.zero), "", 20, new Color32(30, 50, 40, 255));
            Button(side, "Pass", new Vector2(0, .24f), new Vector2(1, .32f), new Color32(120, 130, 125, 255), Pass, out pass);
            Button(side, "Again", new Vector2(0, .12f), new Vector2(1, .22f), Accent, () => { game = new GoGame(); aiAt = -1; log.Clear(); ClearComment(); Draw(); }, out again);
            Draw();
        }

        public override void Refresh() => Draw();

        bool HumanTurn => !game.Ended && aiAt < 0 && game.ToMove == GoGame.Black;

        // Go coordinates skip the letter I.
        static string Point(int x, int y) => "ABCDEFGHJ"[x] + (y + 1).ToString();

        void Play(int x, int y)
        {
            if (!HumanTurn || !game.Place(x, y)) return;
            log.Add(Point(x, y));
            Moved(game.Moves);
            aiAt = Time.unscaledTime + .5f;
            Draw();
        }

        void Pass()
        {
            if (!HumanTurn || !game.Pass()) return;
            log.Add(T("停", "pass"));
            Moved(game.Moves);
            if (!game.Ended) aiAt = Time.unscaledTime + .5f;
            Finish();
            Draw();
        }

        void Update()
        {
            if (aiAt < 0 || Time.unscaledTime < aiAt) return;
            aiAt = -1;
            if (!game.Ended)
            {
                int taken = game.Captures(GoGame.White), lost = game.Captures(GoGame.Black);
                if (game.Best(out int x, out int y))
                {
                    // The lab's AI plays the program's move as often as its skill allows, and a beginner's move otherwise.
                    if (AiPlays && Rng.NextDouble() > Skill && game.Casual(Rng, out int cx, out int cy)) { x = cx; y = cy; }
                    game.Place(x, y);
                    log.Add(Point(x, y));
                    var situation = game.Captures(GoGame.White) > taken ? XgMoveSituation.AiCapture
                        : game.Captures(GoGame.Black) > lost ? XgMoveSituation.PlayerCapture
                        : InAtari(x, y) ? XgMoveSituation.Threat : XgMoveSituation.Quiet;
                    Comment(situation, game.Moves);
                }
                else { game.Pass(); log.Add(T("停", "pass")); }
                Moved(game.Moves);
            }
            Finish();
            Draw();
        }

        /// <summary>Whether the stone just played left one of your groups next to it with a single liberty.</summary>
        bool InAtari(int x, int y)
        {
            foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= GoGame.Size || ny >= GoGame.Size || game[nx, ny] != GoGame.Black) continue;
                if (game.Liberties(nx, ny) == 1) return true;
            }
            return false;
        }

        bool counted;
        void Finish()
        {
            if (!game.Ended || counted) return;
            counted = true;
            var outcome = game.Winner == GoGame.Black ? XgGameOutcome.PlayerWon : game.Winner == GoGame.White ? XgGameOutcome.PlayerLost : XgGameOutcome.Draw;
            if (outcome == XgGameOutcome.PlayerWon) wins++; else if (outcome == XgGameOutcome.PlayerLost) losses++;
            ClearComment();
            Finished(outcome, game.Moves, log);
        }

        void Draw()
        {
            if (status == null) return;
            if (!game.Ended) counted = false;
            for (int x = 0; x < GoGame.Size; x++)
                for (int y = 0; y < GoGame.Size; y++)
                {
                    int s = game[x, y];
                    stones[x, y].color = s == GoGame.Black ? new Color32(25, 25, 25, 255) : s == GoGame.White ? new Color32(248, 248, 244, 255) : Color.clear;
                    bool last = x == game.LastX && y == game.LastY && s != 0;
                    marks[x, y].color = last ? (s == GoGame.Black ? new Color32(240, 240, 240, 255) : new Color32(30, 30, 30, 255)) : Color.clear;
                }
            string state;
            if (game.Ended)
            {
                game.Score(out double b, out double w);
                state = (game.Winner == GoGame.Black ? T("你赢了。", "You win.") : Opponent + T("赢了。", " wins.")) + "\n" + T("黑 ", "Black ") + b.ToString("0.#") + T(" · 白 ", " · White ") + w.ToString("0.#") + T("（含贴目 6.5）", " (6.5 komi)");
            }
            else if (aiAt >= 0) state = Opponent + T("在想……", " is thinking…");
            else state = game.LastX < 0 && game.Moves > 0 ? Opponent + T("停了一手。你也停一手，就结束数子。", " passed. Pass too to end and count.") : T("你执黑，先手。", "You play black and move first.");
            string opponent = T("对手：", "Opponent: ") + (AiPlays ? AiName + T(" · 下对 ", " · plays it right ") + Mathf.RoundToInt((float)Skill * 100) + "%" : T("电脑的围棋程序", "the computer's Go program"));
            status.text = "<b><size=26>" + Title + "</size></b>\n" + opponent + "\n\n" + state + "\n\n" + T("提子 ", "Captured ") + game.Captures(GoGame.Black) + T(" · 被提 ", " · lost ") + game.Captures(GoGame.White) +
                "\n" + T("胜 ", "Won ") + wins + T(" · 负 ", " · lost ") + losses + LabLines() + "\n\n<size=15><color=#4A6656>" +
                T("把对方棋子的「气」（相邻空点）全部堵上就能提走。双方都停一手时结束，按地盘和棋子数子。", "Fill every liberty (empty neighbour) of a group to capture it. When both sides pass, the game ends and area is counted.") + "</color></size>";
            pass.text = T("停一手", "Pass");
            again.text = T("再来一局", "New game");
        }
    }
}
