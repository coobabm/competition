using System;
using System.Collections.Generic;
using System.Linq;
using LingGuang.Core;

namespace LingGuang.Game
{
    /// <summary>§15 (P1, lite): five fetal-stage lessons, each teaches one thing. Boards are tiny sandboxes.</summary>
    public sealed class TutorialLevel
    {
        public string title;
        public string instruction;
        public string goal;
        public Func<GameConfig, RunState> build;
        public Func<RunState, ConfirmResult, bool> check;
        public int hintCell = -1;
    }

    public static class Tutorial
    {
        static RunState Sandbox(GameConfig cfg, int stage, int starts, bool lighthouse)
        {
            var c = cfg.Clone();
            c.charactersEnabled = false;
            c.insightEnabled = false;
            var s = RunState.CreateSandbox(c, stage, 7);
            s.startsLeft = starts;
            s.movesLeft = 1;
            s.firstConfirmDone = !lighthouse;
            return s;
        }

        public static List<TutorialLevel> Levels(GameConfig cfg)
        {
            var list = new List<TutorialLevel>();

            list.Add(new TutorialLevel
            {
                title = "心跳",
                instruction = "点击发光的本能灵光，把它选为起点，然后按 Enter 发动。\n电点亮灵光，到达阈值就会闪烁。",
                goal = "让灵光闪一次",
                build = c =>
                {
                    var s = Sandbox(c, 0, 3, false);
                    s.AddSpark(Shape.Instinct, 4, 6, 0);
                    return s;
                },
                check = (s, r) => r.events.Any(e => e.type == SimEventType.Fire),
            });

            list.Add(new TutorialLevel
            {
                title = "传递",
                instruction = "闪烁的灵光通过光丝把电传给它指向的灵光。\n点下方的候选“传导”，放到两个灵光之间，用滚轮把它转向右边的灵光，再发动。",
                goal = "点亮最远的那颗灵光",
                build = c =>
                {
                    var s = Sandbox(c, 0, 3, false);
                    s.AddSpark(Shape.Instinct, 2, 5, 0);
                    var t = s.AddSpark(Shape.Conduct, 4, 5, 1);
                    t.placedByPlayer = false;
                    s.candidates.Add(Shape.Conduct);
                    return s;
                },
                check = (s, r) => r.events.Any(e => e.type == SimEventType.Fire && !e.isStart && e.cell == s.board.Index(4, 5)),
            });

            list.Add(new TutorialLevel
            {
                title = "同时",
                instruction = "锥体的阈值更高，需要两条光丝在同一拍把电送到。\n已经有一条路了，再放一颗传导，补上第二条。",
                goal = "让锥体闪烁",
                build = c =>
                {
                    var cc = c.Clone();
                    cc.rippleCharge = 0;
                    var s = Sandbox(cc, 1, 3, false);
                    s.AddSpark(Shape.Instinct, 3, 4, 0);
                    s.AddSpark(Shape.Conduct, 4, 4, 2);
                    s.AddSpark(Shape.Cone, 4, 5, 1);
                    s.candidates.Add(Shape.Conduct);
                    return s;
                },
                check = (s, r) => r.events.Any(e => e.type == SimEventType.Fire && !e.isStart && e.amount >= 2 && e.cell == s.board.Index(4, 5)),
            });

            list.Add(new TutorialLevel
            {
                title = "熟悉",
                instruction = "不传电的光丝变细，传电的光丝变粗。\n用同一条路反复发动，看着光丝一点点变粗。",
                goal = "让一条光丝导通 3 次（加粗）",
                build = c =>
                {
                    var s = Sandbox(c, 0, 4, false);
                    s.AddSpark(Shape.Conduct, 3, 5, 1);
                    s.AddSpark(Shape.Conduct, 4, 5, 1);
                    s.AddSpark(Shape.Conduct, 5, 5, 1);
                    return s;
                },
                check = (s, r) => s.edges.Values.Any(e => e.count >= s.cfg.thickAt),
            });

            list.Add(new TutorialLevel
            {
                title = "出生",
                instruction = "灯塔在每轮第一次发动时照亮附近的灵光。\n汇聚要在同一拍收到三处来的电才会爆发。找到三束电同时到达的位置，放下汇聚。",
                goal = "让汇聚在同一拍收到 3 个来源而闪",
                build = c =>
                {
                    var s = Sandbox(c, 1, 3, true);
                    s.AddSpark(Shape.Instinct, 4, 4, 0);
                    s.AddSpark(Shape.Instinct, 4, 6, 0);
                    s.AddSpark(Shape.Instinct, 3, 5, 0);
                    s.candidates.Add(Shape.Converge);
                    return s;
                },
                check = (s, r) => r.stats.convergeBurst,
            });

            // stuck hints (shown after 10 s without progress)
            var probe = RunState.CreateSandbox(cfg.Clone(), 1);
            list[0].hintCell = probe.board.Index(4, 6);
            list[1].hintCell = probe.board.Index(3, 5);
            list[2].hintCell = probe.board.Index(3, 5);
            list[3].hintCell = probe.board.Index(3, 5);
            list[4].hintCell = probe.board.Index(4, 5);
            return list;
        }
    }
}
