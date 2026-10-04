using System;
using System.Globalization;

namespace LingGuangV05.Core.Incremental
{
    /// <summary>Chinese big-number formatting: 万 / 亿 / 万亿, then scientific. One standard everywhere.</summary>
    public static class Fmt
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static string Num(double v)
        {
            if (double.IsNaN(v)) return "—";
            if (double.IsInfinity(v)) return "∞";
            if (v < 0) return "-" + Num(-v);
            if (v < 10) return Trim(v.ToString("0.#", Inv));
            if (v < 1e4) return Math.Floor(v).ToString("0", Inv);
            if (v < 1e8) return Short(v / 1e4) + "万";
            if (v < 1e12) return Short(v / 1e8) + "亿";
            if (v < 1e16) return Short(v / 1e12) + "万亿";
            return v.ToString("0.00e0", Inv);
        }

        public static string Money(double v)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) return Num(v);
            if (Math.Abs(v) < 1e4) return v.ToString("0.00", Inv);
            return Num(v);
        }

        public static string Rate(double perSecond) { return Num(perSecond) + "/秒"; }

        public static string Duration(double seconds)
        {
            if (double.IsInfinity(seconds) || double.IsNaN(seconds)) return "暂时无法达到";
            if (seconds <= 0) return "现在";
            if (seconds < 60) return Math.Ceiling(seconds).ToString("0", Inv) + " 秒";
            if (seconds < 3600) return Math.Floor(seconds / 60).ToString("0", Inv) + " 分 " + Math.Floor(seconds % 60).ToString("00", Inv) + " 秒";
            if (seconds < 86400) return (seconds / 3600).ToString("0.0", Inv) + " 小时";
            return (seconds / 86400).ToString("0.0", Inv) + " 天";
        }

        private static string Short(double v) { return Trim(v < 100 ? v.ToString("0.00", Inv) : v.ToString("0.0", Inv)); }
        private static string Trim(string s) { return s.Contains(".") ? s.TrimEnd('0').TrimEnd('.') : s; }
    }
}
