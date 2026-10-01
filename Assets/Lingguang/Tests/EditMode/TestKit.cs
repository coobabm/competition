using System.Collections.Generic;
using System.Linq;
using Lingguang.Core;
using NUnit.Framework;

namespace Lingguang.Tests
{
    /// <summary>测试工具：搭棋盘、放灵光、查事件。默认关闭灯塔与涟漪，让每个用例只测一条规则。</summary>
    public static class Kit
    {
        public static GameConfig Cfg(bool ripples = false, bool lighthouse = false)
        {
            var cfg = GameConfig.CreateDefault();
            if (!lighthouse) cfg.LighthouseCharge = 0;
            if (!ripples)
            {
                cfg.RippleCharge = 0;
                cfg.WeakLinksEnabled = false;
            }
            return cfg;
        }

        /// <summary>直接搭一个状态（无次级沟、无本能、无关口），已处于某阶段的第 roundInStage 轮。</summary>
        public static GameState State(Stage stage, GameConfig cfg = null, int roundInStage = 0)
        {
            cfg = cfg ?? Cfg();
            var board = Board.Create(cfg.Layout, null);
            var s = new GameState(cfg, board, new Rng(1), 1) { Stage = stage, RoundInStage = roundInStage };
            NewRound(s);
            return s;
        }

        /// <summary>只重置本轮状态（不触发本能衰减、拾物点等），用于重复同一操作。</summary>
        public static void NewRound(GameState s)
        {
            Lifecycle.ResetRoundTransient(s);
            s.Round.StartsLeft = EffectQuery.StartCount(s);
            s.Round.FirstConfirmPending = true;
            s.Round.PlacementAvailable = true;
        }

        public static GameSession Session(Stage stage, int round = 0, string gate = "", GameConfig cfg = null, int seed = 7)
        {
            return GameSession.NewRun(cfg ?? Cfg(), seed, new RunOptions
            {
                StartStage = stage,
                StartRoundInStage = round,
                SecondaryGrooves = false,
                PlaceInstincts = false,
                GateOverride = gate,
            });
        }

        public static Glow Put(GameState s, ShapeKind shape, int col, int row, int orientation)
        {
            var cell = new Cell(col, row);
            s.Pickups.RemoveAll(p => p.Cell == cell);
            Assert.That(s.IsCellActive(cell), Is.True, "测试格子必须已解锁: " + cell);
            return BoardOps.AddGlow(s, shape, cell, orientation);
        }

        public static Glow Conduction(GameState s, int col, int row, int orientation) => Put(s, ShapeKind.Conduction, col, row, orientation);

        public static void SetCount(GameState s, Glow from, Glow to, int count) => s.GetOrCreateEdge(new EdgeKey(from.Id, to.Id)).Count = count;

        public static SimResult Fire(GameState s, params Glow[] starters) => Simulator.Confirm(s, starters.Select(g => g.Id).ToList());

        /// <summary>某灵光的 Fire 事件所在拍；没闪返回 -1。</summary>
        public static int FireBeat(SimResult r, Glow g)
        {
            var e = r.Events.FirstOrDefault(x => x.Type == SimEventType.Fire && x.GlowId == g.Id);
            return e == null ? -1 : e.Beat;
        }

        public static int FireCount(SimResult r, Glow g) => r.Events.Count(x => x.Type == SimEventType.Fire && x.GlowId == g.Id);

        public static List<SimEvent> DeliveriesTo(SimResult r, Glow g) =>
            r.Events.Where(x => x.Type == SimEventType.Deliver && x.GlowId == g.Id).ToList();

        public static List<SimEvent> DeliveriesFrom(SimResult r, Glow g) =>
            r.Events.Where(x => x.Type == SimEventType.Deliver && x.SourceGlowId == g.Id).ToList();

        public static string Dump(SimResult r) => string.Join("\n", r.Events.Select(e => e.ToString()));
    }
}
