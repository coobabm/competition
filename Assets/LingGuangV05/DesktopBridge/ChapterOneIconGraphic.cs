using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop
{
    /// <summary>Original, texture-free icon shared by the shortcut, title and taskbar.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ChapterOneIconGraphic : MaskableGraphic
    {
        public string applicationId = "lingguang";

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            var rect = GetPixelAdjustedRect();
            float unit = Mathf.Min(rect.width, rect.height);
            if (unit <= 0) return;
            var center = rect.center;
            if (applicationId != "lingguang")
            {
                DrawApplication(helper, center, unit);
                return;
            }
            Rounded(helper, center, unit * .48f, unit * .11f,
                new Color32(99, 212, 246, 255), new Color32(13, 64, 118, 255));
            Rounded(helper, center, unit * .425f, unit * .09f,
                new Color32(38, 125, 181, 255), new Color32(16, 52, 101, 255));
            Vector2 a = center + new Vector2(-.24f, -.18f) * unit;
            Vector2 b = center + new Vector2(-.21f, .19f) * unit;
            Vector2 c = center + new Vector2(.23f, .22f) * unit;
            Vector2 d = center + new Vector2(.24f, -.19f) * unit;
            Color wire = new Color32(142, 231, 248, 230);
            Line(helper, a, center, unit * .036f, wire);
            Line(helper, b, center, unit * .036f, wire);
            Line(helper, c, center, unit * .036f, wire);
            Line(helper, d, center, unit * .036f, wire);
            Disc(helper, a, unit * .057f, Color.white);
            Disc(helper, b, unit * .057f, Color.white);
            Disc(helper, c, unit * .057f, Color.white);
            Disc(helper, d, unit * .057f, Color.white);
            Disc(helper, center, unit * .145f, new Color(1, .84f, .35f, .20f));
            Disc(helper, center, unit * .103f, new Color32(255, 218, 105, 255));
            Disc(helper, center, unit * .051f, new Color32(255, 253, 219, 255));
        }

        private void DrawApplication(VertexHelper helper, Vector2 center, float unit)
        {
            bool shop = applicationId == "xunbao", home = applicationId == "home", mail = applicationId == "mail";
            Color upper = shop ? new Color32(255, 168, 71, 255) : home ? new Color32(120, 205, 151, 255)
                : mail ? new Color32(156, 211, 246, 255) : new Color32(136, 197, 246, 255);
            Color lower = shop ? new Color32(194, 66, 22, 255) : home ? new Color32(24, 117, 86, 255)
                : mail ? new Color32(42, 102, 172, 255) : new Color32(68, 97, 188, 255);
            Rounded(helper, center, unit * .48f, unit * .11f, upper, lower);
            Color ink = new Color32(248, 251, 255, 255);
            if (shop)
            {
                Quad(helper, center + new Vector2(-.25f, -.25f) * unit,
                    center + new Vector2(.25f, .16f) * unit, ink);
                Line(helper, center + new Vector2(-.13f, .12f) * unit,
                    center + new Vector2(-.13f, .28f) * unit, unit * .048f, ink);
                Line(helper, center + new Vector2(-.13f, .28f) * unit,
                    center + new Vector2(.13f, .28f) * unit, unit * .048f, ink);
                Line(helper, center + new Vector2(.13f, .28f) * unit,
                    center + new Vector2(.13f, .12f) * unit, unit * .048f, ink);
                Disc(helper, center + new Vector2(-.1f, -.02f) * unit, unit * .035f, lower);
                Disc(helper, center + new Vector2(.1f, -.02f) * unit, unit * .035f, lower);
            }
            else if (home)
            {
                Quad(helper, center + new Vector2(-.22f, -.25f) * unit,
                    center + new Vector2(.22f, .06f) * unit, ink);
                Line(helper, center + new Vector2(-.31f, .02f) * unit,
                    center + new Vector2(0, .3f) * unit, unit * .075f, ink);
                Line(helper, center + new Vector2(0, .3f) * unit,
                    center + new Vector2(.31f, .02f) * unit, unit * .075f, ink);
                Quad(helper, center + new Vector2(-.065f, -.25f) * unit,
                    center + new Vector2(.065f, -.02f) * unit, lower);
            }
            else if (mail)
            {
                Quad(helper, center + new Vector2(-.31f, -.2f) * unit,
                    center + new Vector2(.31f, .22f) * unit, ink);
                Line(helper, center + new Vector2(-.29f, .2f) * unit, center + new Vector2(0, -.02f) * unit, unit * .035f, lower);
                Line(helper, center + new Vector2(.29f, .2f) * unit, center + new Vector2(0, -.02f) * unit, unit * .035f, lower);
            }
            else
            {
                Disc(helper, center + new Vector2(-.06f, .05f) * unit, unit * .28f, ink);
                Line(helper, center + new Vector2(-.12f, -.12f) * unit,
                    center + new Vector2(-.23f, -.29f) * unit, unit * .08f, ink);
                Disc(helper, center + new Vector2(.2f, -.16f) * unit, unit * .15f, new Color32(255, 211, 85, 255));
                Disc(helper, center + new Vector2(-.16f, .055f) * unit, unit * .027f, lower);
                Disc(helper, center + new Vector2(-.06f, .055f) * unit, unit * .027f, lower);
                Disc(helper, center + new Vector2(.04f, .055f) * unit, unit * .027f, lower);
            }
        }

        private void Quad(VertexHelper helper, Vector2 minimum, Vector2 maximum, Color fill)
        {
            int first = helper.currentVertCount;
            helper.AddVert(minimum, fill * color, Vector2.zero);
            helper.AddVert(new Vector2(minimum.x, maximum.y), fill * color, Vector2.zero);
            helper.AddVert(maximum, fill * color, Vector2.zero);
            helper.AddVert(new Vector2(maximum.x, minimum.y), fill * color, Vector2.zero);
            helper.AddTriangle(first, first + 1, first + 2);
            helper.AddTriangle(first, first + 2, first + 3);
        }

        private void Rounded(VertexHelper helper, Vector2 center, float half, float radius,
            Color upper, Color lower)
        {
            int first = helper.currentVertCount;
            helper.AddVert(center, color * Color.Lerp(lower, upper, .5f), Vector2.zero);
            const int steps = 5;
            for (int corner = 0; corner < 4; corner++)
            {
                var origin = center + new Vector2(corner == 0 || corner == 3 ? half - radius : -half + radius,
                    corner < 2 ? half - radius : -half + radius);
                for (int i = 0; i <= steps; i++)
                {
                    float angle = (corner * 90f + i * 90f / steps) * Mathf.Deg2Rad;
                    var position = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    helper.AddVert(position, color * Color.Lerp(lower, upper,
                        Mathf.InverseLerp(center.y - half, center.y + half, position.y)), Vector2.zero);
                }
            }
            const int count = 4 * (steps + 1);
            for (int i = 0; i < count; i++) helper.AddTriangle(first, first + i + 1, first + (i + 1) % count + 1);
        }

        private void Disc(VertexHelper helper, Vector2 center, float radius, Color fill)
        {
            int first = helper.currentVertCount;
            helper.AddVert(center, fill * color, Vector2.zero);
            const int segments = 20;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2 / segments;
                helper.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius,
                    fill * color, Vector2.zero);
            }
            for (int i = 0; i < segments; i++) helper.AddTriangle(first, first + i + 1, first + (i + 1) % segments + 1);
        }

        private void Line(VertexHelper helper, Vector2 from, Vector2 to, float width, Color fill)
        {
            var normal = new Vector2(-(to - from).y, (to - from).x).normalized * width * .5f;
            int first = helper.currentVertCount;
            helper.AddVert(from - normal, fill * color, Vector2.zero);
            helper.AddVert(from + normal, fill * color, Vector2.zero);
            helper.AddVert(to + normal, fill * color, Vector2.zero);
            helper.AddVert(to - normal, fill * color, Vector2.zero);
            helper.AddTriangle(first, first + 1, first + 2);
            helper.AddTriangle(first, first + 2, first + 3);
        }
    }
}
