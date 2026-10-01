using System;
using System.Collections.Generic;

namespace Lingguang.Core
{
    /// <summary>开局选项（调试面板与测试用）。</summary>
    public sealed class RunOptions
    {
        public Stage StartStage = Stage.Infant;
        public int StartRoundInStage = 0;
        public bool SecondaryGrooves = true;
        public bool PlaceInstincts = true;
        /// <summary>null：按种子抽关口；\"\"：无关口；其他：指定关口 id。</summary>
        public string GateOverride;
        /// <summary>局外解锁：是否已经到达过童年（“分离”关口才会出现）。</summary>
        public bool SeparationUnlocked;
    }

    public sealed class ConfirmOutcome
    {
        public SimResult Sim;
        /// <summary>起点次数归零时自动结算；否则为 null。</summary>
        public RoundSettlement Settlement;
    }

    /// <summary>
    /// 一局游戏的入口（Presenter / 调试面板 / 测试都只调用这里）。
    /// 负责次数限制与 Esc 撤销：上一次确认之后的操作，在下一次确认前都可撤销（快照 = GameState 深拷贝）。
    /// </summary>
    public sealed class GameSession
    {
        public GameState State { get; private set; }
        public GameConfig Config => State.Config;
        public bool CanUndo => undo.Count > 0;
        /// <summary>一旦到达童年即为真，供局外进度保存（解锁“分离”关口）。</summary>
        public bool ReachedChildhood => State.Stage >= Stage.Childhood;

        readonly Stack<GameState> undo = new Stack<GameState>();

        GameSession(GameState state) { State = state; }

        public static GameSession NewRun(GameConfig config, int seed, RunOptions options = null)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            options = options ?? new RunOptions();
            var rng = new Rng(seed);
            var board = Board.Create(config.Layout, options.SecondaryGrooves ? rng : null);
            var s = new GameState(config, board, rng, seed) { SeparationUnlocked = options.SeparationUnlocked };
            if (options.PlaceInstincts) PlaceInstincts(s);
            Lifecycle.EnterStage(s, options.StartStage, options.StartRoundInStage, options.GateOverride);
            return new GameSession(s);
        }

        static void PlaceInstincts(GameState s)
        {
            var cfg = s.Config;
            if (string.IsNullOrEmpty(cfg.InstinctRegion)) return;
            var cells = new List<Cell>();
            foreach (var c in s.Board.Cells)
                if (s.Board.RegionOf(c) == cfg.InstinctRegion[0]) cells.Add(c);
            for (int i = 0; i < cfg.InstinctCount && cells.Count > 0; i++)
            {
                int k = s.Rng.Next(cells.Count);
                BoardOps.AddGlow(s, ShapeKind.Instinct, cells[k], 0, true);
                cells.RemoveAt(k);
            }
        }

        /// <summary>
        /// 运行时热重载：地图形状不变时直接换用新配置继续本局（下一次确认起生效），并清空撤销栈；
        /// 地图形状改了则返回 false，需要用新配置重新开局。
        /// </summary>
        public bool ApplyConfig(GameConfig config, out string reason)
        {
            reason = null;
            if (config == null) { reason = "配置为空"; return false; }
            if (config.Layout == null || config.Layout.ShapeSignature() != State.Config.Layout.ShapeSignature())
            {
                reason = "地图形状已改变，需要重新开局";
                return false;
            }
            State = State.CloneWithConfig(config);
            undo.Clear();
            return true;
        }

        // ================= 操作 =================

        public bool Place(int candidateIndex, Cell cell, int orientation, out string reason)
        {
            reason = null;
            var r = State.Round;
            if (!CheckRunning(out reason)) return false;
            if (!r.PlacementAvailable) { reason = "本轮已放置或放弃"; return false; }
            if (candidateIndex < 0 || candidateIndex >= r.Candidates.Count) { reason = "候选不存在"; return false; }
            if (!BoardOps.CanPlace(State, cell)) { reason = "该格不可放置"; return false; }

            Snapshot();
            BoardOps.AddGlow(State, r.Candidates[candidateIndex], cell, orientation);
            r.PlacementAvailable = false;
            r.Candidates.Clear();
            return true;
        }

        public bool Discard(out string reason)
        {
            if (!CheckRunning(out reason)) return false;
            if (!State.Round.PlacementAvailable) { reason = "本轮已放置或放弃"; return false; }
            Snapshot();
            State.Round.PlacementAvailable = false;
            State.Round.Candidates.Clear();
            return true;
        }

        public bool Rotate(int glowId, int orientation, out string reason)
        {
            if (!CheckRunning(out reason)) return false;
            Snapshot();
            if (BoardOps.Rotate(State, glowId, orientation, out reason)) return true;
            undo.Pop();
            return false;
        }

        public bool Move(int glowId, Cell to, out string reason)
        {
            if (!CheckRunning(out reason)) return false;
            if (State.Round.MovesLeft <= 0) { reason = "本轮移动次数已用完"; return false; }
            Snapshot();
            if (BoardOps.Move(State, glowId, to, out reason))
            {
                State.Round.MovesLeft--;
                return true;
            }
            undo.Pop();
            return false;
        }

        public bool Undo()
        {
            if (undo.Count == 0) return false;
            State = undo.Pop();
            return true;
        }

        /// <summary>确认发动（Enter）。起点次数归零时自动结算本轮。</summary>
        public ConfirmOutcome Confirm(IList<int> starterGlowIds)
        {
            if (!CheckRunning(out string reason)) throw new InvalidOperationException(reason);
            if (State.Round.StartsLeft <= 0) throw new InvalidOperationException("起点次数已用完");
            if (!Simulator.ValidateStarters(State, starterGlowIds, out _, out reason)) throw new InvalidOperationException(reason);

            undo.Clear();
            var outcome = new ConfirmOutcome { Sim = Simulator.Confirm(State, starterGlowIds) };
            State.Round.StartsLeft--;
            State.Round.ConfirmsDone++;
            if (State.Round.StartsLeft <= 0) outcome.Settlement = Lifecycle.SettleRound(State);
            return outcome;
        }

        /// <summary>点击“结束本轮”。</summary>
        public RoundSettlement EndRound()
        {
            if (!CheckRunning(out string reason)) throw new InvalidOperationException(reason);
            undo.Clear();
            return Lifecycle.SettleRound(State);
        }

        /// <summary>试运行（物件“电脑”/ 镜片预演用）：在副本上完整模拟，不改变当前状态。</summary>
        public SimResult TrialRun(IList<int> starterGlowIds)
        {
            var copy = State.Clone();
            return Simulator.Confirm(copy, starterGlowIds);
        }

        void Snapshot() => undo.Push(State.Clone());

        bool CheckRunning(out string reason)
        {
            reason = State.RunOver ? "本局已结束" : null;
            return !State.RunOver;
        }
    }
}
