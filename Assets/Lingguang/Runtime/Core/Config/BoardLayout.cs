using System;

namespace Lingguang.Core
{
    [Serializable]
    public class RegionDef
    {
        /// <summary>地图里使用的单个字母。</summary>
        public string Letter;
        public string Name;
        public Stage UnlockStage;
    }

    /// <summary>中线上的窄桥：该行内中线左右两格之间的左右相邻边。</summary>
    [Serializable]
    public class BridgeDef
    {
        public int Row;
        public Stage OpenStage;
    }

    [Serializable]
    public class CellPair
    {
        public Cell A;
        public Cell B;

        public CellPair() { }
        public CellPair(int colA, int rowA, int colB, int rowB) { A = new Cell(colA, rowA); B = new Cell(colB, rowB); }
        public CellEdge ToEdge() => new CellEdge(A, B);
    }

    /// <summary>
    /// 棋盘形状资源（规格第 3 节）。这是“第一版草稿”，所有内容都可在 Inspector / JSON 中修改：
    /// 地图行、区域与解锁阶段、中线位置与窄桥、封闭区域（外侧沟）与缺口、额外固定沟、灯塔锚点、次级沟数量。
    /// </summary>
    [Serializable]
    public class BoardLayout
    {
        /// <summary>每行一个字符串，格子之间用空格分隔；"." 表示无格子，字母表示区域。</summary>
        public string[] Rows;
        public RegionDef[] Regions;

        /// <summary>中线纵裂位于“此列”与“此列 + 1”之间；设为 -1 关闭中线。</summary>
        public int MidlineLeftColumn = 4;
        public BridgeDef[] MidlineBridges;

        /// <summary>这些区域与其他区域之间的所有边都是沟（外侧沟），缺口见 SealedRegionGaps。</summary>
        public string[] SealedRegions;
        public CellPair[] SealedRegionGaps;
        public CellPair[] ExtraFixedGrooves;

        public Cell[] LighthouseAnchors;

        public int SecondaryGroovesMin = 3;
        public int SecondaryGroovesMax = 4;
        public int SecondaryGrooveMaxAttempts = 200;

        /// <summary>形状签名：两份布局签名相同，则同一局可以直接换用（热重载不需要重开）。</summary>
        public string ShapeSignature()
        {
            var sb = new System.Text.StringBuilder();
            if (Rows != null) foreach (var r in Rows) sb.Append(r).Append('|');
            sb.Append('#');
            if (Regions != null) foreach (var r in Regions) sb.Append(r.Letter).Append(':').Append((int)r.UnlockStage).Append(',');
            sb.Append('#').Append(MidlineLeftColumn).Append('#');
            if (MidlineBridges != null) foreach (var b in MidlineBridges) sb.Append(b.Row).Append(':').Append((int)b.OpenStage).Append(',');
            sb.Append('#');
            if (SealedRegions != null) foreach (var r in SealedRegions) sb.Append(r).Append(',');
            AppendPairs(sb, SealedRegionGaps);
            AppendPairs(sb, ExtraFixedGrooves);
            sb.Append('#');
            if (LighthouseAnchors != null) foreach (var a in LighthouseAnchors) sb.Append(a).Append(',');
            sb.Append('#').Append(SecondaryGroovesMin).Append('-').Append(SecondaryGroovesMax);
            return sb.ToString();
        }

        static void AppendPairs(System.Text.StringBuilder sb, CellPair[] pairs)
        {
            sb.Append('#');
            if (pairs != null) foreach (var p in pairs) sb.Append(p.A).Append(p.B).Append(',');
        }

        public static BoardLayout CreateDefault()
        {
            return new BoardLayout
            {
                Rows = new[]
                {
                    ". . . F F F F . . .",
                    ". . F F F F F F . .",
                    "T T C C C C C C T T",
                    "T T K C C C K . T T",
                    ". . K K K K K . . .",
                    ". . I I I I I I . .",
                    ". . . . I I . . . .",
                },
                Regions = new[]
                {
                    new RegionDef { Letter = "I", Name = "后部感觉区", UnlockStage = Stage.Infant },
                    new RegionDef { Letter = "K", Name = "感觉运动带", UnlockStage = Stage.Childhood },
                    new RegionDef { Letter = "C", Name = "中央奖赏区", UnlockStage = Stage.Adolescence },
                    new RegionDef { Letter = "F", Name = "前额区", UnlockStage = Stage.Adult },
                    new RegionDef { Letter = "T", Name = "左右颞区", UnlockStage = Stage.MiddleAge },
                },
                MidlineLeftColumn = 4,
                MidlineBridges = new[]
                {
                    new BridgeDef { Row = 5, OpenStage = Stage.Infant },
                    new BridgeDef { Row = 3, OpenStage = Stage.Adolescence },
                    new BridgeDef { Row = 1, OpenStage = Stage.Adult },
                },
                SealedRegions = new[] { "T" },
                SealedRegionGaps = new[] { new CellPair(1, 2, 2, 2), new CellPair(7, 2, 8, 2) },
                ExtraFixedGrooves = new CellPair[0],
                LighthouseAnchors = new[] { new Cell(4, 6), new Cell(5, 6) },
                SecondaryGroovesMin = 3,
                SecondaryGroovesMax = 4,
                SecondaryGrooveMaxAttempts = 200,
            };
        }
    }
}
