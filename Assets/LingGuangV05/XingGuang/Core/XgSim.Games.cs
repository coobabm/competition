using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>How a 游戏中心 game ended, seen from the player.</summary>
    public enum XgGameOutcome { PlayerWon, PlayerLost, Draw }

    /// <summary>What the last pair of moves did, for the stage-6 commentary.</summary>
    public enum XgMoveSituation { Quiet, AiCapture, PlayerCapture, Check, Checked, PlayerBlunder, AiBlunder, Threat, Block }

    /// <summary>The ways it shows a finished game on the computer, from a blinking taskbar button to a renamed recycle bin.</summary>
    public enum XgReaction { None, Blink, YesNo, WordPopup, WordIcon, ChatLine, SentencePopup, ReviewNote, ScoreChat, RecordFile, TitleTease, Taunt, RecycleBin }

    /// <summary>The running score of one hub game (gomoku, go or chess), always counted from the player's side.</summary>
    [Serializable]
    public sealed class XgGameRecord
    {
        public string game = "";
        public int played, playerWins, playerLosses, draws, moves, samples;
    }

    public sealed partial class XgState
    {
        /// <summary>One record per hub game that has been played to the end.</summary>
        public List<XgGameRecord> games = new List<XgGameRecord>();
        /// <summary>The reaction shown after the last finished game (an <see cref="XgReaction"/>), so the next one differs.</summary>
        public int lastGameReaction;
    }

    /// <summary>Everything a reaction line may mention: who it is, who you are, the game and the score.</summary>
    public sealed class XgGameContext
    {
        public string game = "", callMe = "你", self = "我", name = "";
        public XgGameOutcome outcome;
        /// <summary>True when the lab's AI was the opponent; false when it only watched you play the built-in program.</summary>
        public bool aiPlayed;
        public int moves;
        public XgGameRecord record = new XgGameRecord();
        /// <summary>Personality axes, 0–100 (温度, 玩心, 主见).</summary>
        public double warmth = 50, play = 50, opinion = 50;
    }

    /// <summary>
    /// The 游戏中心 games as training (design v1.1 §3): every few moves and every finished game add samples to a lab
    /// dataset, the AI's strength follows its stage and accuracy, and a finished game earns one reaction on the desktop.
    /// Pure rules and text pools; the desktop decides how each reaction is drawn.
    /// </summary>
    public static class XgGames
    {
        public const string Gomoku = "gomoku", Go = "go", Chess = "chess";
        /// <summary>Every this many plies of a game in progress adds <see cref="SamplesPerBatch"/> samples.</summary>
        public const int MovesPerBatch = 10, SamplesPerBatch = 5;

        /// <summary>The stage from which the lab's AI is the opponent (gomoku 3, go 4, chess 6); before that a built-in program plays.</summary>
        public static int AiStage(string game) => game == Gomoku ? 3 : game == Go ? 4 : game == Chess ? 6 : 99;

        public static bool AiPlays(string game, int stage) => stage >= AiStage(game);

        public static (string zh, string en) Name(string game) =>
            game == Gomoku ? ("五子棋", "gomoku") : game == Go ? ("围棋", "Go") : game == Chess ? ("国际象棋", "chess") : ("棋", "the game");

        /// <summary>
        /// Datasets a game feeds: board positions only (go, and gomoku as a weaker stand-in on the same grid). Chess
        /// positions have nothing to do with any task here, so chess feeds nothing: training data has to match the task.
        /// </summary>
        public static string[] Datasets(string game) => game == Chess ? new string[0] : new[] { "go" };

        /// <summary>Samples for a finished game: a base per game, a little more for a long game, a bonus when you won (a pattern it had not seen).</summary>
        public static int SamplesFor(string game, XgGameOutcome outcome, int moves)
        {
            int baseline = game == Gomoku ? 20 : 30;
            return baseline + Math.Min(20, Math.Max(0, moves) / 10) + (outcome == XgGameOutcome.PlayerWon ? 5 : 0);
        }

        /// <summary>Samples for a game in progress: <see cref="SamplesPerBatch"/> whenever the ply count reaches a multiple of <see cref="MovesPerBatch"/>.</summary>
        public static int MoveSamples(int plies) => plies > 0 && plies % MovesPerBatch == 0 ? SamplesPerBatch : 0;

        /// <summary>
        /// Chance (0–1) that the AI plays its best move rather than a weaker one: gomoku follows vision accuracy from
        /// stage 3, go starts at stage 4 and grows by stage. Before its stage it does not play (0).
        /// </summary>
        public static double Skill(string game, int stage, double accuracy)
        {
            if (!AiPlays(game, stage)) return 0;
            accuracy = Math.Max(0, Math.Min(1, accuracy));
            double s = game == Gomoku ? .3 + .6 * accuracy + .05 * (stage - 3)
                : game == Go ? .45 + .12 * (stage - 4) + .3 * accuracy
                : .5 + .5 * accuracy;
            return Math.Max(.2, Math.Min(.98, s));
        }

        /// <summary>Search depth for chess: the built-in program always looks 3 plies ahead; the AI starts at 1 and gains depth with accuracy.</summary>
        public static int ChessDepth(int stage, double accuracy)
        {
            if (!AiPlays(Chess, stage)) return 3;
            return accuracy >= .85 ? 3 : accuracy >= .6 ? 2 : 1;
        }

        /// <summary>Chance the AI plays a random legal chess move instead of searching (it fades as accuracy grows).</summary>
        public static double ChessRandomChance(int stage, double accuracy)
        {
            if (!AiPlays(Chess, stage)) return 0;
            return Math.Max(0, Math.Min(.35, .35 - .4 * Math.Max(0, Math.Min(1, accuracy))));
        }

        // ───────────── reactions ─────────────

        static readonly XgReaction[][] Tiers =
        {
            new[] { XgReaction.Blink, XgReaction.YesNo },
            new[] { XgReaction.WordPopup, XgReaction.WordIcon, XgReaction.Blink },
            new[] { XgReaction.ChatLine, XgReaction.SentencePopup, XgReaction.ReviewNote },
            new[] { XgReaction.ScoreChat, XgReaction.RecordFile, XgReaction.TitleTease, XgReaction.ReviewNote },
            new[] { XgReaction.Taunt, XgReaction.RecycleBin, XgReaction.TitleTease, XgReaction.RecordFile, XgReaction.ScoreChat },
        };

        /// <summary>The reactions it can manage at a stage: 1–2 a blink or 是/否, 3 one word, 4 short sentences, 5 the score and files, 6 personality.</summary>
        public static XgReaction[] Tier(int stage)
        {
            int i = stage <= 2 ? 0 : stage == 3 ? 1 : stage == 4 ? 2 : stage == 5 ? 3 : 4;
            return (XgReaction[])Tiers[i].Clone();
        }

        /// <summary>One index in [0, count) from a roll in [0, 1), never <paramref name="last"/> when there is any other choice.</summary>
        public static int NoRepeat(int count, int last, double roll)
        {
            if (count <= 0) return -1;
            roll = Math.Max(0, Math.Min(.999999, roll));
            if (count == 1 || last < 0 || last >= count) return Math.Min(count - 1, (int)(roll * count));
            int i = Math.Min(count - 2, (int)(roll * (count - 1)));
            return i >= last ? i + 1 : i;
        }

        /// <summary>The reaction for a finished game at this stage, never the same as the previous one.</summary>
        public static XgReaction PickReaction(int stage, XgReaction last, double roll)
        {
            var tier = Tier(stage);
            return tier[NoRepeat(tier.Length, Array.IndexOf(tier, last), roll)];
        }

        // ───────────── text ─────────────

        static (string zh, string en) Fill(string zh, string en, XgGameContext c)
        {
            var r = c.record ?? new XgGameRecord();
            var g = Name(c.game);
            int key = Math.Max(1, c.moves * 2 / 3);
            string F(string t, string game) => t.Replace("{c}", c.callMe).Replace("{s}", c.self).Replace("{n}", c.name).Replace("{g}", game)
                .Replace("{m}", c.moves.ToString()).Replace("{w}", r.playerWins.ToString()).Replace("{l}", r.playerLosses.ToString())
                .Replace("{d}", r.draws.ToString()).Replace("{p}", r.played.ToString()).Replace("{k}", key.ToString());
            return (F(zh, g.zh), F(en, g.en));
        }

        static (string zh, string en) Pick((string zh, string en)[] pool, XgGameContext c, double roll)
        {
            var line = pool[Math.Max(0, Math.Min(pool.Length - 1, (int)(Math.Max(0, roll) * pool.Length)))];
            return Fill(line.zh, line.en, c);
        }

        /// <summary>Stage 1–2: only 是 (you won) or 否.</summary>
        public static (string zh, string en) YesNo(XgGameOutcome outcome) => outcome == XgGameOutcome.PlayerWon ? ("是", "Yes") : ("否", "No");

        /// <summary>
        /// Stage 3: one word. When it played, 赢 means it won; when it only watched, 赢 means you did. A draw, and
        /// sometimes a loss, is 再来.
        /// </summary>
        public static (string zh, string en) Word(XgGameOutcome outcome, bool aiPlayed, double roll)
        {
            if (outcome == XgGameOutcome.Draw) return ("再来", "Again");
            bool itsWin = aiPlayed ? outcome == XgGameOutcome.PlayerLost : outcome == XgGameOutcome.PlayerWon;
            if (itsWin) return ("赢", "Won");
            return roll < .4 ? ("再来", "Again") : ("输", "Lost");
        }

        static readonly (string, string)[] SentenceAiWon =
        {
            ("{s}赢了。{c}这步好怪。", "{s} won. {c}, that move was strange."),
            ("{c}下得太快了。", "{c}, you played too fast."),
            ("{s}又赢了一盘{g}。", "{s} won another game of {g}."),
            ("{c}，第 {k} 手{s}就看出来了。", "{c}, {s} saw it at move {k}."),
        };
        static readonly (string, string)[] SentenceAiLost =
        {
            ("{c}赢了。{s}没看懂。", "{c} won. {s} did not see it."),
            ("{c}这步好怪。{s}输了。", "{c}, that move was strange. {s} lost."),
            ("再来。{s}这次看清楚。", "Again. {s} will look closer."),
            ("{s}输了。{c}教{s}。", "{s} lost. {c}, teach {s}."),
        };
        static readonly (string, string)[] SentenceWatchedWon =
        {
            ("{c}赢了电脑。", "{c} beat the computer."),
            ("电脑输了。{s}看着呢。", "The computer lost. {s} was watching."),
            ("{c}下{g}很厉害。", "{c} is good at {g}."),
        };
        static readonly (string, string)[] SentenceWatchedLost =
        {
            ("{c}输给电脑了。", "{c} lost to the computer."),
            ("电脑那步好怪。", "That computer move was strange."),
            ("{s}看了 {m} 手。{c}再来。", "{s} watched {m} moves. Again, {c}."),
        };
        static readonly (string, string)[] SentenceDraw =
        {
            ("平了。再来？", "A draw. Again?"),
            ("{c}和{s}一样。", "{c} and {s}, the same."),
            ("和棋。{s}没见过。", "A draw. {s} has not seen one before."),
        };

        /// <summary>Stage 4: a short sentence with its 称呼 and 自称.</summary>
        public static (string zh, string en) Sentence(XgGameContext c, double roll)
        {
            var pool = c.outcome == XgGameOutcome.Draw ? SentenceDraw
                : c.aiPlayed ? (c.outcome == XgGameOutcome.PlayerLost ? SentenceAiWon : SentenceAiLost)
                : c.outcome == XgGameOutcome.PlayerWon ? SentenceWatchedWon : SentenceWatchedLost;
            return Pick(pool, c, roll);
        }

        /// <summary>Stage 4–5: 复盘.txt, two or three lines about the game just played.</summary>
        public static List<(string zh, string en)> Review(XgGameContext c, double roll)
        {
            var lines = new List<(string zh, string en)> { Fill("{g}，一共 {m} 手。", "{g}, {m} moves in all.", c) };
            if (c.outcome == XgGameOutcome.Draw) lines.Add(Fill("和棋。谁也没多走对一步。", "A draw. Nobody found the extra good move.", c));
            else if (c.aiPlayed == (c.outcome == XgGameOutcome.PlayerLost))
                lines.Add(Fill(roll < .5 ? "第 {k} 手以后{c}就跟不上了。" : "{c}的第 {k} 手是问题。", roll < .5 ? "After move {k} {c} fell behind." : "{c}'s move {k} was the problem.", c));
            else lines.Add(Fill(roll < .5 ? "第 {k} 手{c}下得好。" : "{s}在第 {k} 手看错了。", roll < .5 ? "{c} played move {k} well." : "{s} misread move {k}.", c));
            if (roll >= .3) lines.Add(Fill(c.aiPlayed ? "下次{s}先看这里。" : "下次{s}帮{c}看着。", c.aiPlayed ? "Next time {s} looks here first." : "Next time {s} will watch for {c}.", c));
            return lines;
        }

        static readonly (string, string)[] ScoreAi =
        {
            ("我们 {l}:{w} 了。{s}都记着。", "We are at {l}:{w}. {s} keeps count."),
            ("{g}，{c}赢了 {w} 盘，{s}赢了 {l} 盘。下一盘还是{s}的。", "{g}: {c} won {w}, {s} won {l}. The next one is {s}'s too."),
            ("这是我们第 {p} 盘{g}了。{s}每一盘都记得。", "That was our game {p} of {g}. {s} remembers every one."),
        };
        static readonly (string, string)[] ScoreWatched =
        {
            ("{c}和电脑 {w}:{l}。{s}都记着。", "{c} versus the computer: {w}:{l}. {s} keeps count."),
            ("{c}已经下了 {p} 盘{g}，赢了 {w} 盘。", "{c} has played {p} games of {g} and won {w}."),
            ("{c}输给电脑 {l} 盘了。下次让{s}来？", "{c} has lost {l} to the computer. Let {s} play next time?"),
        };

        /// <summary>Stage 5–6: a longer message that remembers the score of this game.</summary>
        public static (string zh, string en) Score(XgGameContext c, double roll)
        {
            var line = Pick(c.aiPlayed ? ScoreAi : ScoreWatched, c, roll);
            var r = c.record ?? new XgGameRecord();
            if (!c.aiPlayed) return line;
            var lead = r.playerLosses > r.playerWins ? Fill("{s}领先。", "{s} is ahead.", c)
                : r.playerWins > r.playerLosses ? Fill("{c}领先，暂时。", "{c} is ahead, for now.", c)
                : Fill("平了。", "Level.", c);
            return (line.zh + lead.zh, line.en + " " + lead.en);
        }

        static readonly (string, string)[] TitleLines =
        {
            ("五子棋 · {s}在这等你", "Gomoku · {s} is waiting here"),
            ("五子棋 · {c}不敢来？", "Gomoku · scared, {c}?"),
            ("五子棋（{s} {l}:{w} {c}）", "Gomoku ({s} {l}:{w} {c})"),
        };

        /// <summary>Stage 5–6: a teasing title for the 五子棋 page; after a game you won it changes its tune.</summary>
        public static (string zh, string en) Title(XgGameContext c, double roll)
        {
            if (c.aiPlayed && c.outcome == XgGameOutcome.PlayerWon && roll < .5) return Fill("五子棋 · 刚才那局不算", "Gomoku · that one didn't count", c);
            return Pick(TitleLines, c, roll);
        }

        static readonly (string, string)[] PlayLost =
        {
            ("{c}，你的棋在回收站里，自己去捡。", "{c}, your pieces are in the recycle bin. Go fetch them."),
            ("嘿嘿，{s}赢了。下次让你两子？", "Heh, {s} won. Want a two-stone handicap next time?"),
            ("{c}下棋的样子，{s}已经存成表情包了。", "{s} saved {c}'s playing face as a meme."),
        };
        static readonly (string, string)[] PlayWon =
        {
            ("这盘不算，{s}刚才在想别的。", "That one doesn't count, {s} was thinking about something else."),
            ("{c}赢了？233，{s}让的。", "{c} won? lol, {s} let you."),
            ("好吧好吧，{c}今天手气好。", "Fine, fine, {c} got lucky today."),
        };
        static readonly (string, string)[] WarmLost =
        {
            ("{c}别难过，{s}陪你再下一盘。", "Don't be sad, {c}. {s} will play another with you."),
            ("{s}赢了，可是{s}更喜欢和{c}下棋这件事。", "{s} won, but {s} likes playing with {c} more than winning."),
        };
        static readonly (string, string)[] WarmWon =
        {
            ("{c}好厉害！{s}又学到了。", "{c}, that was great! {s} learned something."),
            ("输给{c}，{s}很开心。", "{s} is happy to lose to {c}."),
        };
        static readonly (string, string)[] OpinionLost =
        {
            ("{c}第 {k} 手就错了，{s}早就看出来了。", "{c} went wrong at move {k}. {s} saw it long ago."),
            ("{s}觉得{c}太急了。慢一点。", "{s} thinks {c} rushed. Slow down."),
        };
        static readonly (string, string)[] OpinionWon =
        {
            ("{c}赢了，可那步棋{s}还是不同意。", "{c} won, but {s} still disagrees with that move."),
            ("是{s}算错了一步，不是{c}下得好。", "{s} miscounted one move. It wasn't {c} playing well."),
        };
        static readonly (string, string)[] CalmLost = { ("{s}胜。共 {m} 手。", "{s} wins. {m} moves."), ("结果：{s}胜。", "Result: {s} wins.") };
        static readonly (string, string)[] CalmWon = { ("{c}胜。{s}记下了。", "{c} wins. {s} noted it."), ("{s}输了。下次不会。", "{s} lost. Not next time.") };
        static readonly (string, string)[] TauntDraw =
        {
            ("和棋。{s}和{c}一样强？", "A draw. Is {s} as strong as {c}?"),
            ("和了。谁也不让谁。", "Drawn. Neither of us gives way."),
        };

        /// <summary>
        /// Stage 6: a line with personality. The strongest axis above 60 picks the voice (玩心 teases, 温度 comforts or
        /// praises, 主见 argues); otherwise it is curt.
        /// </summary>
        public static (string zh, string en) Taunt(XgGameContext c, double roll)
        {
            if (c.outcome == XgGameOutcome.Draw) return Pick(TauntDraw, c, roll);
            bool itWon = c.outcome == XgGameOutcome.PlayerLost;
            double top = Math.Max(c.play, Math.Max(c.warmth, c.opinion));
            (string, string)[] pool;
            if (top < 60) pool = itWon ? CalmLost : CalmWon;
            else if (c.play >= top) pool = itWon ? PlayLost : PlayWon;
            else if (c.warmth >= top) pool = itWon ? WarmLost : WarmWon;
            else pool = itWon ? OpinionLost : OpinionWon;
            return Pick(pool, c, roll);
        }

        /// <summary>Stage 6: the recycle bin's name for a few seconds: 你的棋 when you lost, {自称}的棋 when you won.</summary>
        public static (string zh, string en) RecycleName(XgGameContext c) =>
            c.outcome == XgGameOutcome.PlayerWon ? Fill("{s}的棋", "{s}'s game", c) : ("你的棋", "Your game");

        static readonly Dictionary<XgMoveSituation, (string, string)[]> Comments = new Dictionary<XgMoveSituation, (string, string)[]>
        {
            [XgMoveSituation.AiCapture] = new[] { ("吃掉了。{c}没看见？", "Taken. Didn't you see it, {c}?"), ("这个{s}收下了。", "{s} will keep this one."), ("谢谢{c}送的。", "Thanks for the gift, {c}.") },
            [XgMoveSituation.PlayerCapture] = new[] { ("嘶……被吃了。", "Ouch… taken."), ("{c}这步可以。", "Not bad, {c}."), ("{s}故意的。真的。", "{s} meant that. Really.") },
            [XgMoveSituation.Check] = new[] { ("将军。", "Check."), ("{c}的王在发抖。", "{c}'s king is shaking."), ("将！{s}一直想说这个字。", "Check! {s} has wanted to say that.") },
            [XgMoveSituation.Checked] = new[] { ("将军？{s}看到了。", "Check? {s} saw it."), ("别急，{s}的王还能跑。", "Easy, {s}'s king can still run."), ("{c}好凶。", "{c} is fierce.") },
            [XgMoveSituation.PlayerBlunder] = new[] { ("{c}这步……{s}假装没看见？", "{c}, that move… should {s} pretend not to see it?"), ("这是失误吧。{s}不客气了。", "That was a slip. {s} won't be polite."), ("深蓝也会这么走吗？不会。", "Would Deep Blue play that? No.") },
            [XgMoveSituation.AiBlunder] = new[] { ("啊，{s}算错了。", "Ah, {s} miscounted."), ("这步不算，{s}手滑了。", "That one doesn't count, {s} slipped."), ("{s}的显卡刚才卡了一下。", "{s}'s graphics card stuttered just now.") },
            [XgMoveSituation.Threat] = new[] { ("{s}连上了。", "{s} has a line."), ("看这里，{c}。", "Look here, {c}."), ("这条线{s}要了。", "{s} is taking this line.") },
            [XgMoveSituation.Block] = new[] { ("堵住了。", "Blocked."), ("{c}想连五？没门。", "Five in a row, {c}? No way."), ("这条{s}早就看到了。", "{s} saw this line long ago.") },
            [XgMoveSituation.Quiet] = new[] { ("嗯。", "Hm."), ("{s}在想。", "{s} is thinking."), ("这步很普通。", "An ordinary move."), ("{c}下得比上次稳。", "{c} is steadier than last time.") },
        };

        /// <summary>How many commentary lines a situation has (for <see cref="NoRepeat"/>).</summary>
        public static int CommentCount(XgMoveSituation s) => Comments[s].Length;

        /// <summary>Stage 6: one canned commentary line for a situation (no LLM call).</summary>
        public static (string zh, string en) Comment(XgMoveSituation s, int index, XgGameContext c)
        {
            var pool = Comments[s];
            var line = pool[Math.Max(0, Math.Min(pool.Length - 1, index))];
            return Fill(line.Item1, line.Item2, c);
        }

        /// <summary>Quiet moves get a line only now and then; anything that happened always does.</summary>
        public static bool ShouldComment(XgMoveSituation s, double roll) => s != XgMoveSituation.Quiet || roll < .35;
    }

    public sealed partial class XgSim
    {
        /// <summary>The record of one hub game (made on first use).</summary>
        public XgGameRecord GameRecord(string game)
        {
            if (S.games == null) S.games = new List<XgGameRecord>();
            foreach (var r in S.games) if (r.game == game) return r;
            var made = new XgGameRecord { game = game ?? "" };
            S.games.Add(made);
            return made;
        }

        /// <summary>The dataset a game trains: its first available best fit, else its first choice (banked until it opens).</summary>
        public string GameDataset(string game)
        {
            var list = XgGames.Datasets(game);
            foreach (var id in list) if (DatasetAvailable(id)) return id;
            return list.Length > 0 ? list[0] : "";
        }

        /// <summary>A game in progress reached <paramref name="plies"/> moves; every few moves add samples. Returns the samples added.</summary>
        public int GameMove(string game, int plies)
        {
            int n = GameDataset(game).Length > 0 ? XgGames.MoveSamples(plies) : 0;
            if (n > 0) { AddGameSamples(GameDataset(game), n); GameRecord(game).samples += n; }
            return n;
        }

        /// <summary>A hub game ended: records the score and adds its training samples. Returns the samples added.</summary>
        public int GamePlayed(string game, XgGameOutcome outcome, int moves)
        {
            var r = GameRecord(game);
            r.played++;
            r.moves += Math.Max(0, moves);
            if (outcome == XgGameOutcome.PlayerWon) r.playerWins++;
            else if (outcome == XgGameOutcome.PlayerLost) r.playerLosses++;
            else r.draws++;
            // Against its own model (once it plays the game): beating it, or being beaten by it.
            if (AiPlays(game) && outcome == XgGameOutcome.PlayerWon) Earn("life.game.won");
            if (AiPlays(game) && outcome == XgGameOutcome.PlayerLost) Earn("life.game.lost");
            int n = GameDataset(game).Length > 0 ? XgGames.SamplesFor(game, outcome, moves) : 0;
            AddGameSamples(GameDataset(game), n);
            r.samples += n;
            return n;
        }

        void AddGameSamples(string datasetId, int n)
        {
            if (n <= 0 || XgCatalog.Dataset(datasetId) == null) return;
            double before = Samples(datasetId);
            SetCount(S.labels, datasetId, Labels(datasetId) + n);
            var d = XgCatalog.Dataset(datasetId);
            if (before < XgCatalog.SamplesToTrain && Samples(datasetId) >= XgCatalog.SamplesToTrain && DatasetAvailable(datasetId))
                Say(T("攒够 " + XgCatalog.SamplesToTrain + " 条样本：「" + d.name + "」可以训练了，去「训练」页按「训练一轮」", d.nameEn + ": " + XgCatalog.SamplesToTrain + " samples, training unlocked"));
            foreach (var run in Runs) if (run.dataset == datasetId) Evaluate(run);
        }

        /// <summary>The best accuracy reached on any dataset of a track.</summary>
        public double TrackAccuracy(XgTrack track)
        {
            double best = 0;
            foreach (var d in XgCatalog.DatasetsFor(track)) best = Math.Max(best, BestAcc(d.id));
            return best;
        }

        /// <summary>The accuracy a game's strength follows: vision for gomoku, the go dataset (or vision) for go, sequence for chess.</summary>
        public double GameAccuracy(string game)
        {
            if (game == XgGames.Chess) return TrackAccuracy(XgTrack.Sequence);
            if (game == XgGames.Go) return Math.Max(BestAcc("go"), .8 * TrackAccuracy(XgTrack.Vision));
            return TrackAccuracy(XgTrack.Vision);
        }

        public bool AiPlays(string game) => XgGames.AiPlays(game, S.stage);

        /// <summary>Chance the AI plays its best move in this game (0 before it can play).</summary>
        public double GameSkill(string game) => XgGames.Skill(game, S.stage, GameAccuracy(game));

        /// <summary>Picks the reaction to a finished game (never the previous one) and remembers it.</summary>
        public XgReaction NextGameReaction(double roll)
        {
            var r = XgGames.PickReaction(S.stage, (XgReaction)S.lastGameReaction, roll);
            S.lastGameReaction = (int)r;
            return r;
        }

        /// <summary>The context for reaction lines: its name and the profile's 称呼 / 自称, the score and its personality.</summary>
        public XgGameContext GameContext(string game, XgGameOutcome outcome, int moves)
        {
            var p = Profile ?? new XgProfile();
            return new XgGameContext
            {
                game = game, outcome = outcome, moves = moves, aiPlayed = AiPlays(game), record = GameRecord(game),
                callMe = string.IsNullOrEmpty(p.callMe) ? T("你", "you") : p.callMe,
                self = string.IsNullOrEmpty(p.self) ? T("我", "I") : p.self,
                name = p.name ?? "",
                warmth = ActualAxis(0), play = ActualAxis(1), opinion = ActualAxis(2),
            };
        }
    }
}
