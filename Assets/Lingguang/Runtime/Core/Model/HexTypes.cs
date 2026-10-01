using System;

namespace Lingguang.Core
{
    /// <summary>六个人生阶段（规格第 9 节）。数值顺序即推进顺序。</summary>
    public enum Stage { Infant = 0, Childhood = 1, Adolescence = 2, Adult = 3, MiddleAge = 4, OldAge = 5 }

    /// <summary>灵光形状（规格第 4 节）。</summary>
    public enum ShapeKind { Conduction = 0, Cone = 1, Converge = 2, Instinct = 3 }

    /// <summary>回路型（规格第 8 节）。排名由 GameConfig.Patterns 的顺序决定。</summary>
    public enum PatternKind { None = -1, LongChain = 0, Converge = 1, Diverge = 2, ShortChain = 3, Spark = 4 }

    /// <summary>六角格坐标：尖顶朝上、奇数行右移（odd-r）。Col = 列，Row = 行（行 0 在最上方）。</summary>
    [Serializable]
    public struct Cell : IEquatable<Cell>, IComparable<Cell>
    {
        public int Col;
        public int Row;

        public Cell(int col, int row) { Col = col; Row = row; }

        public bool Equals(Cell other) => Col == other.Col && Row == other.Row;
        public override bool Equals(object obj) => obj is Cell c && Equals(c);
        public override int GetHashCode() => (Row * 397) ^ Col;
        /// <summary>按行优先排序，保证所有遍历顺序确定。</summary>
        public int CompareTo(Cell other) => Row != other.Row ? Row.CompareTo(other.Row) : Col.CompareTo(other.Col);
        public static bool operator ==(Cell a, Cell b) => a.Equals(b);
        public static bool operator !=(Cell a, Cell b) => !a.Equals(b);
        public override string ToString() => "(" + Col + "," + Row + ")";
    }

    /// <summary>两个格子之间的无向边（用于沟、窄桥）。</summary>
    public readonly struct CellEdge : IEquatable<CellEdge>, IComparable<CellEdge>
    {
        public readonly Cell A;
        public readonly Cell B;

        public CellEdge(Cell a, Cell b)
        {
            if (a.CompareTo(b) <= 0) { A = a; B = b; } else { A = b; B = a; }
        }

        public bool Equals(CellEdge other) => A == other.A && B == other.B;
        public override bool Equals(object obj) => obj is CellEdge e && Equals(e);
        public override int GetHashCode() => (A.GetHashCode() * 7919) ^ B.GetHashCode();
        public int CompareTo(CellEdge other) { int c = A.CompareTo(other.A); return c != 0 ? c : B.CompareTo(other.B); }
        public override string ToString() => A + "-" + B;
    }

    /// <summary>有向光丝的身份：源灵光 id → 目标灵光 id。旋转/移动时按 id 删除。</summary>
    public readonly struct EdgeKey : IEquatable<EdgeKey>, IComparable<EdgeKey>
    {
        public readonly int From;
        public readonly int To;

        public EdgeKey(int from, int to) { From = from; To = to; }

        public bool Equals(EdgeKey other) => From == other.From && To == other.To;
        public override bool Equals(object obj) => obj is EdgeKey k && Equals(k);
        public override int GetHashCode() => (From * 100003) ^ To;
        public int CompareTo(EdgeKey other) => From != other.From ? From.CompareTo(other.From) : To.CompareTo(other.To);
        public override string ToString() => From + "->" + To;
    }

    /// <summary>
    /// odd-r 六角格运算。方向编号按规格：0 右上、1 右、2 右下、3 左下、4 左、5 左上（顺时针）。
    /// </summary>
    public static class Hex
    {
        public const int DirectionCount = 6;

        // [方向, (dCol, dRow)]
        static readonly int[,] EvenRowOffsets = { { 0, -1 }, { 1, 0 }, { 0, 1 }, { -1, 1 }, { -1, 0 }, { -1, -1 } };
        static readonly int[,] OddRowOffsets = { { 1, -1 }, { 1, 0 }, { 1, 1 }, { 0, 1 }, { -1, 0 }, { 0, -1 } };

        public static int Mod6(int value) => ((value % DirectionCount) + DirectionCount) % DirectionCount;

        public static Cell Neighbor(Cell cell, int direction)
        {
            int d = Mod6(direction);
            int[,] table = (cell.Row & 1) == 0 ? EvenRowOffsets : OddRowOffsets;
            return new Cell(cell.Col + table[d, 0], cell.Row + table[d, 1]);
        }

        /// <summary>a → b 的方向；不相邻返回 -1。</summary>
        public static int DirectionTo(Cell a, Cell b)
        {
            for (int d = 0; d < DirectionCount; d++)
                if (Neighbor(a, d) == b) return d;
            return -1;
        }

        /// <summary>六角距离（不考虑沟）。</summary>
        public static int Distance(Cell a, Cell b)
        {
            ToCube(a, out int ax, out int ay, out int az);
            ToCube(b, out int bx, out int by, out int bz);
            return Math.Max(Math.Abs(ax - bx), Math.Max(Math.Abs(ay - by), Math.Abs(az - bz)));
        }

        static void ToCube(Cell c, out int x, out int y, out int z)
        {
            x = c.Col - (c.Row - (c.Row & 1)) / 2;
            z = c.Row;
            y = -x - z;
        }
    }

    internal static class Num
    {
        /// <summary>配置里的 float 转 double 时去掉二进制尾差（0.1f → 0.1），避免倍率在向下取整时少 1。</summary>
        public static double D(float value) => Math.Round((double)value, 6);
    }
}
