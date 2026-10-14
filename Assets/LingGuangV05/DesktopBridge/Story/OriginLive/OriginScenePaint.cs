using LingGuangV05.XingGuang;
using UnityEngine;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>The film's palette (lib.tsx C) plus small drawing helpers shared by the stage 4–6 and finale scenes.</summary>
    public static class OriginPal
    {
        public static readonly Color Ink = OriginInk.Ink, Lime = OriginInk.Lime, Red = OriginInk.Red, Amber = OriginInk.Amber, Hot = OriginInk.Hot;
        public static readonly Color Lav = new Color32(185, 195, 255, 255), Cyan = new Color32(127, 230, 255, 255);
        public static readonly Color DimRgb = OriginInk.DimRgb;
        public const float DimA = OriginInk.DimAlpha;
        public static readonly Color Faint = new Color32(200, 232, 212, 255);
        public const float FaintA = .12f;
        public static readonly Color Frame = new Color32(220, 240, 225, 255);
        public static readonly Color Panel = new Color32(12, 18, 14, 255);

        public static Color Hex(string h)
        {
            Color c; ColorUtility.TryParseHtmlString("#" + h, out c); return c;
        }

        public static Color Lerp(Color a, Color b, float t) { return Color.Lerp(a, b, t); }
    }
}
