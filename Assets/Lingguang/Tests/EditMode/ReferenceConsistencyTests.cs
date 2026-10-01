using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Lingguang.Core;
using NUnit.Framework;

namespace Lingguang.Tests
{
    /// <summary>
    /// 跨实现一致性：规则基准测试的参考模型 sim.py（放到真实棋盘上，board_ref.py）与 C# Simulator，
    /// 在同样的布局、同样的起点下逐轮比较每一次闪光（第几次确认、第几拍、哪一格）和轮末每条光丝的导通次数。
    /// 参考模型不含涟漪、灯塔、窄桥惩罚、记忆等，所以这里在 C# 侧把它们关掉，只比三条底层规则 + 漏电、疲劳、扰动、废退。
    /// 改规则时：先改 sim.py，运行 board_ref.py 重新生成 ReferenceCases.txt，再让这个测试通过。
    /// </summary>
    public class ReferenceConsistencyTests
    {
        sealed class RefRound
        {
            public List<List<Cell>> Confirms = new List<List<Cell>>();
            public List<string> Fires = new List<string>();
            public List<string> Counts = new List<string>();
        }

        sealed class RefCase
        {
            public int Seed;
            public List<(Cell cell, ShapeKind shape, int orientation)> Glows = new List<(Cell, ShapeKind, int)>();
            public List<RefRound> Rounds = new List<RefRound>();
        }

        static string DataPath([CallerFilePath] string here = "")
        {
            var candidates = new List<string>();
            if (!string.IsNullOrEmpty(here)) candidates.Add(Path.Combine(Path.GetDirectoryName(here) ?? "", "ReferenceCases.txt"));
            candidates.Add(Path.Combine("Assets", "Lingguang", "Tests", "EditMode", "ReferenceCases.txt"));
            foreach (var c in candidates) if (File.Exists(c)) return c;
            Assert.Fail("找不到 ReferenceCases.txt：" + string.Join(" | ", candidates));
            return null;
        }

        static ShapeKind ParseShape(string s)
        {
            switch (s)
            {
                case "conduct": return ShapeKind.Conduction;
                case "cone": return ShapeKind.Cone;
                case "conv": return ShapeKind.Converge;
                default: throw new FormatException("未知形状 " + s);
            }
        }

        static List<RefCase> Load()
        {
            var cases = new List<RefCase>();
            RefCase cur = null;
            RefRound round = null;
            foreach (var raw in File.ReadAllLines(DataPath()))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == '#') continue;
                var p = line.Split(' ');
                switch (p[0])
                {
                    case "case": cur = new RefCase { Seed = int.Parse(p[1]) }; cases.Add(cur); break;
                    case "glow": cur.Glows.Add((new Cell(int.Parse(p[1]), int.Parse(p[2])), ParseShape(p[3]), int.Parse(p[4]))); break;
                    case "round": round = new RefRound(); cur.Rounds.Add(round); break;
                    case "confirm":
                        var cells = new List<Cell>();
                        for (int i = 1; i + 1 < p.Length; i += 2) cells.Add(new Cell(int.Parse(p[i]), int.Parse(p[i + 1])));
                        round.Confirms.Add(cells);
                        break;
                    case "fire": round.Fires.Add(string.Join(" ", p.Skip(1))); break;
                    case "count": round.Counts.Add(string.Join(" ", p.Skip(1))); break;
                    default: throw new FormatException(line);
                }
            }
            return cases;
        }

        /// <summary>与参考模型对齐的配置：只保留三条底层规则及其属性。</summary>
        public static GameConfig ReferenceConfig()
        {
            var cfg = Kit.Cfg();                 // 灯塔、涟漪、微弱连接关闭
            cfg.BridgeChargePenalty = 0;          // 参考模型没有窄桥惩罚
            cfg.BridgeExtraDelay = 0;
            cfg.AllowFiredStarters = true;        // 参考模型允许同一轮再次从已闪灵光发起
            return cfg;
        }

        [Test]
        public void CSharpSimulator_MatchesReferenceModel_OnRealBoard()
        {
            var cases = Load();
            Assert.That(cases.Count, Is.GreaterThan(0));
            int rounds = 0, fires = 0;
            foreach (var c in cases)
            {
                var s = Kit.State(Stage.MiddleAge, ReferenceConfig());
                foreach (var (cell, shape, o) in c.Glows) BoardOps.AddGlow(s, shape, cell, o);

                for (int r = 0; r < c.Rounds.Count; r++)
                {
                    var expected = c.Rounds[r];
                    Kit.NewRound(s);
                    var got = new List<string>();
                    for (int ci = 0; ci < expected.Confirms.Count; ci++)
                    {
                        var ids = expected.Confirms[ci].Select(cell => s.CellGlow[cell]).ToList();
                        var result = Simulator.Confirm(s, ids);
                        foreach (var e in result.Events)
                            if (e.Type == SimEventType.Fire) got.Add(ci + " " + e.Beat + " " + e.Cell.Col + " " + e.Cell.Row);
                    }
                    Lifecycle.ApplyDisuse(s);

                    var gotFires = got.OrderBy(x => x, StringComparer.Ordinal).ToList();
                    var expFires = expected.Fires.OrderBy(x => x, StringComparer.Ordinal).ToList();
                    string where = "布局 " + c.Seed + " 第 " + (r + 1) + " 轮";
                    CollectionAssert.AreEqual(expFires, gotFires, where + " 的闪光不一致\n期望：" + string.Join(" | ", expFires) + "\n实际：" + string.Join(" | ", gotFires));

                    var gotCounts = new List<string>();
                    foreach (var kv in s.Edges)
                    {
                        if (kv.Value.Count <= 0) continue;
                        var a = s.Glows[kv.Key.From].Cell;
                        var b = s.Glows[kv.Key.To].Cell;
                        gotCounts.Add(a.Col + " " + a.Row + " " + b.Col + " " + b.Row + " " + kv.Value.Count);
                    }
                    CollectionAssert.AreEquivalent(expected.Counts, gotCounts, where + " 的光丝导通次数不一致");
                    rounds++;
                    fires += got.Count;
                }
            }
            TestContext.WriteLine(cases.Count + " 个布局，" + rounds + " 轮，" + fires + " 次闪光全部一致");
        }
    }
}
