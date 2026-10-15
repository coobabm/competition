using LingGuangV05.Desktop.Story;
using LingGuangV05.XingGuang;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>The learned personality, expressed as color, movement and edge softness.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgWallpaperPersona : MaskableGraphic
    {
        const int Segments = 144;
        XingGuangController controller;
        XgBrainWallpaper wallpaper;
        XgBrainWallpaperPresentation presentation;
        XgState observed;
        Vector3 target = Vector3.one * .5f;
        float nextRead, nextDraw;
        public Vector3 Axes { get; private set; } = Vector3.one * .5f;
        public float Phase { get; private set; }
        public int MeshBuilds { get; private set; }
        public float MotionAmplitude => Mathf.Lerp(.001f, .023f, Axes.y * Axes.y) * (presentation != null ? Mathf.Lerp(.25f, 1, presentation.MotionLevel) : 1);
        public float EdgeSpread => Mathf.Lerp(24, 5, Axes.z);
        public Color HaloColor
        {
            get
            {
                var warmth = Axes.x < .5f ? Color.Lerp(new Color(.16f, .43f, 1), new Color(.15f, .72f, .68f), Axes.x * 2)
                    : Color.Lerp(new Color(.15f, .72f, .68f), new Color(1, .48f, .16f), (Axes.x - .5f) * 2);
                return Color.Lerp(warmth, new Color(.72f, .32f, 1), .28f * Axes.y * Axes.y);
            }
        }

        public static void Install(XingGuangController controller, XgBrainWallpaper wallpaper)
        {
            var rect = PrologueDesk.Rect("Personality Halo", wallpaper.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            rect.SetAsFirstSibling();
            var view = rect.gameObject.AddComponent<XgWallpaperPersona>();
            view.controller = controller; view.wallpaper = wallpaper;
            view.presentation = wallpaper.GetComponent<XgBrainWallpaperPresentation>(); view.raycastTarget = false;
        }

        void Update()
        {
            if (controller?.Sim == null || wallpaper == null) return;
            float now = Time.unscaledTime;
            if (now >= nextRead || observed != controller.Sim.S)
            {
                nextRead = now + .1f;
                target = new Vector3((float)controller.Sim.ActualAxis(0), (float)controller.Sim.ActualAxis(1), (float)controller.Sim.ActualAxis(2)) / 100;
                for (int i = 0; i < 3; i++) target[i] = float.IsNaN(target[i]) ? .5f : Mathf.Clamp01(target[i]);
                if (observed != controller.Sim.S) { observed = controller.Sim.S; Axes = target; }
            }
            Axes = Vector3.Lerp(Axes, target, 1 - Mathf.Exp(-Time.unscaledDeltaTime * 3));
            float reveal = wallpaper.Reveal.Visible ? Mathf.SmoothStep(0, 1, (wallpaper.Reveal.Age - XgWallpaperReveal.CompleteSeconds) / 1.2f) : 0;
            canvasRenderer.SetAlpha(reveal * (presentation != null ? presentation.LightLevel : 1));
            // Integrate phase so toggling Quiet never jumps to another point in the wave.
            if (controller.View?.Juice == null || !controller.View.Juice.Reduced)
                Phase += Time.unscaledDeltaTime * (presentation != null ? presentation.MotionLevel : 1);
            if (presentation != null ? !presentation.DrawDue(ref nextDraw) : now < nextDraw) return;
            if (presentation == null) nextDraw = now + 1f / 30;
            SetVerticesDirty();
        }

        public Vector2 OutlinePoint(float angle, float phase)
        {
            var area = rectTransform.rect;
            float speed = Mathf.Lerp(.18f, 1.3f, Axes.y);
            float wave = Mathf.Sin(angle * 3 + phase * speed) * .65f + Mathf.Sin(angle * 7 - phase * speed * 1.4f) * .35f;
            float edge = Mathf.Pow((Mathf.Cos(angle * 18) + 1) * .5f, 6) * .012f * Axes.z * Axes.z;
            float radial = 1 + wave * MotionAmplitude + edge;
            return area.min + Vector2.Scale(area.size, new Vector2(.52f + Mathf.Cos(angle) * .275f * radial, .54f + Mathf.Sin(angle) * .325f * radial));
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            MeshBuilds++;
            vh.Clear();
            float scale = Mathf.Min(rectTransform.rect.width / 1920, rectTransform.rect.height / 1080);
            // Six translucent bands blend into the background; opinion tightens
            // their falloff while preserving a readable inner contour.
            for (int band = 0; band < 6; band++)
            {
                float inner = (band / 6f - .5f) * 2 * EdgeSpread * scale;
                float outer = ((band + 1) / 6f - .5f) * 2 * EdgeSpread * scale;
                float alphaA = .23f * Mathf.Pow(1 - Mathf.Abs(band / 3f - 1), 2);
                float alphaB = .23f * Mathf.Pow(1 - Mathf.Abs((band + 1) / 3f - 1), 2);
                var inkA = HaloColor; inkA.a = alphaA;
                var inkB = HaloColor; inkB.a = alphaB;
                for (int i = 0; i < Segments; i++)
                {
                    float a = i * Mathf.PI * 2 / Segments, b = (i + 1) * Mathf.PI * 2 / Segments;
                    var pa = OutlinePoint(a, Phase); var pb = OutlinePoint(b, Phase);
                    var normalA = new Vector2(Mathf.Cos(a), Mathf.Sin(a)); var normalB = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
                    int index = vh.currentVertCount;
                    vh.AddVert(pa + normalA * inner, inkA, Vector2.zero);
                    vh.AddVert(pb + normalB * inner, inkA, Vector2.zero);
                    vh.AddVert(pb + normalB * outer, inkB, Vector2.zero);
                    vh.AddVert(pa + normalA * outer, inkB, Vector2.zero);
                    vh.AddTriangle(index, index + 1, index + 2); vh.AddTriangle(index, index + 2, index + 3);
                }
            }
            if (Axes.z > .5f)
            {
                var ink = HaloColor; ink.a = (Axes.z - .5f) * .5f;
                for (int i = 0; i < Segments; i++)
                    XgDraw.Seg(vh, OutlinePoint(i * Mathf.PI * 2 / Segments, Phase), OutlinePoint((i + 1) * Mathf.PI * 2 / Segments, Phase), scale, ink);
            }
        }
    }
}
