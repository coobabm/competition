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
    /// 五子棋 (design v1.1 §3): from stage 3 the AI plays you; it reads the board with one scoring window over every
    /// line (a convolution). How often it reads the board right follows the lab's vision accuracy; every few moves
    /// and every finished game go back into the lab as samples.
    /// </summary>
    public sealed class GomokuPanel : HubGame
    {
        TMP_Text status, again;
        readonly Image[,] stones = new Image[Gomoku.Size, Gomoku.Size];
        Gomoku game = new Gomoku();
        readonly List<string> log = new List<string>();
        float aiAt = -1;
        int wins, losses;
        bool counted;

        public override string Id => "gomoku";
        public override string Title => GameReactions.GomokuTitle ?? T("五子棋", "Gomoku");
        public override string Blurb => T("15 路棋盘，连成五子就赢。和" + LingGuangV05.Core.AppNames.AiZh + "下：它用同一个小窗口扫过整盘棋。", "15 × 15, five in a row wins. Play the lab's AI: it slides one small window over the whole board.");
        public override string Glyph => "●";
        public override Color Accent => new Color32(196, 140, 70, 255);
        public override string Locked => Stage >= 3 ? null : T("第 3 阶段（卷积）以后它才看得懂棋盘", "It can read the board from stage 3 (convolutions)");

        public override void Build(RectTransform area)
        {
            PrologueDesk.Fill(area, new Color32(238, 232, 220, 255));
            var board = PrologueDesk.Centered("Board", area, new Vector2(-190, 0), new Vector2(620, 620));
            board.anchorMin = board.anchorMax = new Vector2(.5f, .5f);
            PrologueDesk.Fill(board, new Color32(222, 184, 120, 255));
            float step = 620f / Gomoku.Size, half = 310;
            for (int i = 0; i < Gomoku.Size; i++)
            {
                PrologueDesk.Fill(PrologueDesk.Centered("H", board, new Vector2(0, -half + step * (i + .5f)), new Vector2(620 - step, 1.5f)), new Color32(120, 80, 40, 255), false);
                PrologueDesk.Fill(PrologueDesk.Centered("V", board, new Vector2(-half + step * (i + .5f), 0), new Vector2(1.5f, 620 - step)), new Color32(120, 80, 40, 255), false);
            }
            for (int x = 0; x < Gomoku.Size; x++)
                for (int y = 0; y < Gomoku.Size; y++)
                {
                    int cx = x, cy = y;
                    var cell = PrologueDesk.Centered("Cell", board, new Vector2(-half + step * (x + .5f), -half + step * (y + .5f)), new Vector2(step, step));
                    var img = PrologueDesk.Fill(cell, new Color(0, 0, 0, 0));
                    var b = cell.gameObject.AddComponent<Button>(); b.targetGraphic = img; b.onClick.AddListener(() => Play(cx, cy));
                    var stone = PrologueDesk.Centered("Stone", cell, Vector2.zero, new Vector2(step * .86f, step * .86f));
                    stones[x, y] = PrologueDesk.Fill(stone, Color.clear, false);
                    stones[x, y].sprite = PrologueDesk.Circle();
                }
            var side = PrologueDesk.Rect("Side", area, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-370, 30), new Vector2(-30, -30));
            status = Text(PrologueDesk.Rect("Status", side, new Vector2(0, .3f), Vector2.one, Vector2.zero, Vector2.zero), "", 20, new Color32(60, 40, 20, 255));
            Button(side, "Again", new Vector2(0, .12f), new Vector2(1, .22f), new Color32(160, 110, 60, 255), NewGame, out again);
            Draw();
        }

        public override void Refresh() => Draw();

        void NewGame()
        {
            game = new Gomoku(); aiAt = -1; counted = false; log.Clear(); ClearComment();
            Draw();
        }

        static string Point(int x, int y) => (char)('A' + x) + (y + 1).ToString();

        bool Over => game.Winner != Gomoku.Empty || game.Full;

        void Play(int x, int y)
        {
            if (Locked != null || Over || aiAt >= 0 || !game.Place(x, y, Gomoku.Black)) return;
            log.Add(Point(x, y));
            Moved(game.Moves);
            if (!Finish()) aiAt = Time.unscaledTime + .6f;
            Draw();
        }

        void Update()
        {
            if (aiAt < 0 || Time.unscaledTime < aiAt) return;
            aiAt = -1;
            // It reads the board right as often as its vision accuracy allows; otherwise it takes a weaker point.
            if (!Over && game.Pick(Gomoku.White, Skill, Rng, out int x, out int y))
            {
                var situation = game.Score(x, y, Gomoku.White) >= 900 ? XgMoveSituation.Threat : game.Score(x, y, Gomoku.Black) >= 900 ? XgMoveSituation.Block : XgMoveSituation.Quiet;
                game.Place(x, y, Gomoku.White);
                log.Add(Point(x, y));
                Moved(game.Moves);
                Comment(situation, game.Moves);
            }
            Finish();
            Draw();
        }

        /// <summary>Counts a finished game once and hands it to the lab. True if the game is over.</summary>
        bool Finish()
        {
            if (!Over) return false;
            if (counted) return true;
            counted = true;
            var outcome = game.Winner == Gomoku.Black ? XgGameOutcome.PlayerWon : game.Winner == Gomoku.White ? XgGameOutcome.PlayerLost : XgGameOutcome.Draw;
            if (outcome == XgGameOutcome.PlayerWon) wins++; else if (outcome == XgGameOutcome.PlayerLost) losses++;
            ClearComment();
            Finished(outcome, game.Moves, log);
            return true;
        }

        void Draw()
        {
            if (status == null) return;
            for (int x = 0; x < Gomoku.Size; x++)
                for (int y = 0; y < Gomoku.Size; y++)
                {
                    int s = game[x, y];
                    stones[x, y].color = s == Gomoku.Black ? new Color32(20, 20, 20, 255) : s == Gomoku.White ? new Color32(250, 250, 250, 255) : Color.clear;
                    if (x == game.LastX && y == game.LastY && s != 0) stones[x, y].color = s == Gomoku.Black ? new Color32(60, 60, 90, 255) : new Color32(255, 240, 200, 255);
                }
            string head = "<b><size=26>" + Title + "</size></b>\n\n";
            if (Locked != null) status.text = head + T("它还看不懂棋盘。\n第 3 阶段（卷积）以后，它就能和你下了。", "It can't read the board yet.\nFrom stage 3 (convolutions) it can play you.");
            else
            {
                string state = game.Winner == Gomoku.Black ? T("你赢了。", "You win.") : game.Winner == Gomoku.White ? AiName + T("赢了。", " wins.") : game.Full ? T("棋盘下满了，和棋。", "The board is full: a draw.")
                    : aiAt >= 0 ? AiName + T("在看棋盘……", " is reading the board…") : T("你执黑，先手。", "You play black and move first.");
                status.text = head + T("对手：", "Opponent: ") + AiName + T(" · 看对棋盘 ", " · reads the board right ") + Mathf.RoundToInt((float)Skill * 100) + "%\n\n" + state + "\n\n" + T("胜 ", "Won ") + wins + T(" · 负 ", " · lost ") + losses + LabLines() + "\n\n<size=15><color=#806040>" +
                    T("它用同一个小窗口扫过整个棋盘：每个空点看四条线，自己能连几个、你能连几个。这就是卷积在干的事。视觉准确率越高，它越少看错。", "It slides one small window over the whole board: for every empty point it reads four lines, how many it can connect and how many you can. That is what a convolution does. The better its vision accuracy, the fewer points it misreads.") + "</color></size>";
            }
            again.text = T("再来一局", "New game");
        }
    }
}
