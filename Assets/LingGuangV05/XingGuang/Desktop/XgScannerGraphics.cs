using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>The scanner grid behind every 灵光 page: 1-pixel lines every 24 pixels on the page colour (the mockup's .page).</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgGridGraphic : MaskableGraphic
    {
        public float step = 24;
        public Color lineColor = XgDark.Grid;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            XgDraw.Box(vh, r.min, r.max, color);
            if (step < 4) return;
            for (float x = r.xMin; x <= r.xMax; x += step) XgDraw.Box(vh, new Vector2(x, r.yMin), new Vector2(x + 1, r.yMax), lineColor);
            for (float y = r.yMax; y >= r.yMin; y -= step) XgDraw.Box(vh, new Vector2(r.xMin, y - 1), new Vector2(r.xMax, y), lineColor);
        }
    }

    /// <summary>
    /// The AI's avatar in the top bar: a green orb lit from the upper left (the mockup's radial gradient) with a halo that
    /// breathes every three seconds. <see cref="Glow"/> is driven by the view; nothing flashes.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgOrbGraphic : MaskableGraphic
    {
        static readonly Color Core = new Color32(225, 245, 238, 255), Mid = new Color32(93, 202, 165, 255), Rim = new Color32(15, 61, 51, 255);
        float glow = .5f;
        public float Glow { get => glow; set { if (Mathf.Abs(value - glow) < .01f) return; glow = value; SetVerticesDirty(); } }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            float radius = Mathf.Min(r.width, r.height) * .5f;
            var c = r.center;
            var halo = Mid; halo.a = .25f + .3f * glow;
            XgSoftDraw.Halo(vh, c, radius * (1.25f + .2f * glow), halo, 28);
            // The gradient as rings from the rim inward, the centre pulled toward the upper left.
            const int Steps = 12;
            var focus = c + new Vector2(-.2f, .2f) * radius;
            for (int i = 0; i < Steps; i++)
            {
                float k = i / (float)(Steps - 1);
                float rad = radius * (1 - k * .92f);
                var at = Vector2.Lerp(c, focus, k);
                Color col = k < .45f ? Color.Lerp(Rim, Mid, k / .45f) : Color.Lerp(Mid, Core, (k - .45f) / .55f);
                XgDraw.Disc(vh, at, rad, col, 28);
            }
        }
    }
}
