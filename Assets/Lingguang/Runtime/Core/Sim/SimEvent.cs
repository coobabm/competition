using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Lingguang.Core
{
    public enum SimEventType
    {
        Fire,
        Deliver,
        Conduct,
        Wasted,
        Lost,
        RippleSpawn,
        RippleMerge,
        RippleApply,
        /// <summary>被涟漪/微弱连接推到阈值而闪（计入拍数与回路型统计，不加导通、不产生涟漪）。</summary>
        RippleConduct,
        PickupCollected,
        /// <summary>灯塔从棋盘外照进来（Centers = 锚点，Cells = 覆盖格，Amount = 电量）。</summary>
        LighthouseShine,
        /// <summary>每拍结束（漏电之后）：Cells/Values = 所有电量不为 0 的单位所在格及其电量，供电量环与调试面板。</summary>
        BeatEnd,
        EdgeLevelUp,
        PatternRecognized,
        ScoreDelta,
        ChainEnd,
    }

    public enum DeliveryKind { Edge, Lost, Ripple, WeakLink, Lighthouse }

    /// <summary>
    /// 模拟器输出的一拍事件。表现层只读这些字段做动画，不做规则判断。
    /// 未用到的字段保持默认值（-1 / null）。
    /// </summary>
    public sealed class SimEvent
    {
        public SimEventType Type;
        public int Beat;

        /// <summary>主格子：闪光的灵光、投递目标、涟漪中心等。</summary>
        public Cell Cell;
        /// <summary>来源格子：Deliver / Conduct 的源灵光。</summary>
        public Cell From;
        public int GlowId = -1;
        public int SourceGlowId = -1;

        /// <summary>光量（Fire）/ 电量（Deliver）/ 导通后次数（Conduct）。</summary>
        public int Amount;
        public int SendBeat = -1;
        public DeliveryKind Kind;
        public int Level = -1;

        public int Radius;
        public int Count;
        public int GroupId = -1;
        public List<Cell> Cells;
        public List<Cell> Centers;
        /// <summary>BeatEnd：与 Cells 一一对应的电量。</summary>
        public List<int> Values;
        /// <summary>Deliver：本次送电中扰动带来的增量；Fire：该单位本轮第几次闪（从 1 起）。</summary>
        public int Perturb;

        public PatternKind Pattern = PatternKind.None;
        public int LightDelta;
        public double MultDelta;
        public string Source;
        public string ItemId;
        public bool Error;

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Type).Append('@').Append(Beat).Append(' ').Append(Cell);
            switch (Type)
            {
                case SimEventType.Deliver:
                case SimEventType.Conduct:
                    sb.Append(" from").Append(From).Append(" src").Append(SourceGlowId);
                    break;
            }
            if (GlowId >= 0) sb.Append(" g").Append(GlowId);
            if (Type == SimEventType.Deliver) sb.Append(' ').Append(Kind).Append(" sent@").Append(SendBeat);
            if (Amount != 0) sb.Append(" amt=").Append(Amount);
            if (Level >= 0) sb.Append(" lv=").Append(Level);
            if (GroupId >= 0) sb.Append(" grp=").Append(GroupId).Append(" r=").Append(Radius).Append(" n=").Append(Count);
            if (Centers != null) sb.Append(" centers=").Append(Join(Centers));
            if (Cells != null) sb.Append(" cells=").Append(Join(Cells));
            if (Values != null) sb.Append(" values=").Append(string.Join(";", Values));
            if (Perturb != 0) sb.Append(" p=").Append(Perturb);
            if (Pattern != PatternKind.None) sb.Append(" pattern=").Append(Pattern);
            if (LightDelta != 0 || MultDelta != 0)
                sb.Append(" +light=").Append(LightDelta).Append(" +mult=").Append(MultDelta.ToString("0.######", CultureInfo.InvariantCulture));
            if (Source != null) sb.Append(" src=").Append(Source);
            if (ItemId != null) sb.Append(" item=").Append(ItemId);
            if (Error) sb.Append(" ERROR");
            return sb.ToString();
        }

        static string Join(List<Cell> cells)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < cells.Count; i++) { if (i > 0) sb.Append(';'); sb.Append(cells[i]); }
            return sb.ToString();
        }
    }
}
