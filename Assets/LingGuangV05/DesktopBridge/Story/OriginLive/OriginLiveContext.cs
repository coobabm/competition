using System.Collections.Generic;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// One scene of the origin film. The host (<see cref="OriginLive"/>) builds every scene once, then calls <see cref="Render"/> with the GLOBAL
    /// film time for the scene that is active at that time (Start ≤ t &lt; End), after setting its fade, drift and visibility. A scene's picture must
    /// be a pure function of t (no state carried between frames), so any frame can be drawn without Play mode and scrubbing is just another t.
    /// </summary>
    public interface IOriginScene
    {
        /// <summary>First and last second of the scene in the film (set by the host from <see cref="XgOriginFilm.Spans"/>).</summary>
        float Start { get; }
        float End { get; }
        /// <summary>Create the scene's objects under root (a full-stage 1920×1080 rect; the host fades and scales it).</summary>
        void Build(OriginLiveContext ctx, RectTransform root);
        /// <summary>Draw the scene at global time t.</summary>
        void Render(float t);
        void SetVisible(bool visible);
    }

    /// <summary>Convenience base: stores the context and root, the scene's span, and gives the local time.</summary>
    public abstract class OriginScene : IOriginScene
    {
        public float Start { get; private set; }
        public float End { get; private set; }
        protected OriginLiveContext Ctx;
        protected RectTransform Root;
        /// <summary>The film's scene table entry (fade, drift) for this scene.</summary>
        protected XgOriginFilm.Span Span;

        /// <summary>Called by the host before Build.</summary>
        public void Place(XgOriginFilm.Span span) { Span = span; Start = (float)span.Start; End = (float)span.End; }
        public void Build(OriginLiveContext ctx, RectTransform root) { Ctx = ctx; Root = root; OnBuild(); }
        protected abstract void OnBuild();
        public abstract void Render(float t);
        public virtual void SetVisible(bool visible) { if (Root != null && Root.gameObject.activeSelf != visible) Root.gameObject.SetActive(visible); }
        /// <summary>Seconds since the scene started.</summary>
        protected float Local(float t) => t - Start;
        /// <summary>Characters the scene's text uses, so the dynamic font atlases are filled before the film starts. Optional.</summary>
        public virtual IEnumerable<string> WarmText() { yield break; }
    }

    /// <summary>
    /// What the host gives every scene: the 1920×1080 stage, fonts, factories for rects/graphics/texts, the pen canvas, the label pool and the
    /// player's numbers. Everything is in the Remotion reference space (origin top-left, y down).
    /// </summary>
    public sealed class OriginLiveContext
    {
        public RectTransform Stage { get; internal set; }
        public TMP_FontAsset Serif { get; internal set; }
        public TMP_FontAsset Mono { get; internal set; }
        /// <summary>The player's final parameter count (the film's 2.13e8 when there is nothing real to show).</summary>
        public double FinalParams { get; internal set; } = XgOriginFilm.FilmFinalParams;
        public XgSim Sim { get; internal set; }
        /// <summary>One screen pixel in reference units (the feather width of anti-aliased edges).</summary>
        public float Pixel { get; internal set; } = 1;
        internal readonly List<OriginGraphic> Graphics = new List<OriginGraphic>();

        public static string T(string zh, string en) => GameText.T(zh, en);
        public static bool English => GameText.IsEnglish;
        public static string Pick(string zh, string en) => English && !string.IsNullOrEmpty(en) ? en : zh;

        /// <summary>A child rect; stretch fills the parent.</summary>
        public RectTransform Rect(string name, Transform parent, bool stretch = true)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform; rt.SetParent(parent, false);
            if (stretch) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }
            return rt;
        }

        /// <summary>A full-stretch graphic. OriginGraphics get the screen-pixel feather automatically.</summary>
        public T Gfx<T>(string name, Transform parent) where T : Graphic
        {
            var rt = Rect(name, parent);
            var g = rt.gameObject.AddComponent<T>();
            g.raycastTarget = false;
            if (g is OriginGraphic og) { og.Pixel = Pixel; Graphics.Add(og); }
            return g;
        }

        public RawImage Raw(string name, Transform parent, Texture tex)
        {
            var r = Gfx<RawImage>(name, parent);
            r.texture = tex;
            return r;
        }

        /// <summary>A stage-sized canvas for pen drawing (see <see cref="OriginPen"/>).</summary>
        public OriginCanvas Canvas(string name, Transform parent) => Gfx<OriginCanvas>(name, parent);

        /// <summary>
        /// A pen canvas clipped to a rectangle of the stage (an SVG clipPath). The returned canvas still draws in full-stage coordinates.
        /// </summary>
        public OriginCanvas ClippedCanvas(string name, Transform parent, float x, float y, float w, float h)
        {
            var clip = Rect(name + " Clip", parent, false);
            clip.anchorMin = clip.anchorMax = new Vector2(0, 1); clip.pivot = new Vector2(0, 1);
            clip.sizeDelta = new Vector2(w, h); clip.anchoredPosition = new Vector2(x, -y);
            clip.gameObject.AddComponent<RectMask2D>();
            var holder = Rect(name + " Stage", clip, false);
            holder.anchorMin = holder.anchorMax = new Vector2(0, 1); holder.pivot = new Vector2(0, 1);
            holder.sizeDelta = new Vector2(1920, 1080); holder.anchoredPosition = new Vector2(-x, y);
            return Canvas(name, holder);
        }

        public OriginText Text(string name, Transform parent, TMP_FontAsset font, float size, Color color, float letterSpacing, TextAlignmentOptions align, params OriginTextLayer[] layers)
            => new OriginText(name, parent, font, size, color, letterSpacing, align, layers);

        /// <summary>A pool of labels for a scene's many small texts.</summary>
        public OriginLabels Labels(string name, Transform parent) => new OriginLabels(this, Rect(name, parent));
    }

    /// <summary>Text anchor, as SVG text-anchor.</summary>
    public enum OriginAnchor { Start, Middle, End }

    /// <summary>A glow behind a label (a CSS text-shadow): colour, strength, SDF dilation and softness.</summary>
    public struct OriginGlow
    {
        public Color Rgb; public float Alpha, Dilate, Soft;
        public OriginGlow(Color rgb, float alpha, float dilate = 0, float soft = .6f) { Rgb = rgb; Alpha = alpha; Dilate = dilate; Soft = soft; }
        public bool Any => Alpha > 0;
    }

    /// <summary>
    /// A pool of film texts. Each frame: <see cref="Begin"/>, then any number of <see cref="Mono"/> / <see cref="Serif"/> calls (SVG-style text:
    /// x, y = the vertical centre, anchor start/middle/end), then <see cref="End"/>, which hides what was not used. Texts are reused by size/style,
    /// so a scene with hundreds of labels over time only ever builds the ones it shows at once.
    /// </summary>
    public sealed class OriginLabels
    {
        readonly OriginLiveContext ctx;
        readonly RectTransform parent;
        sealed class Bucket { public readonly List<OriginText> Items = new List<OriginText>(); public int Used; }
        readonly Dictionary<string, Bucket> buckets = new Dictionary<string, Bucket>();
        readonly List<Bucket> all = new List<Bucket>();
        int count;

        public OriginLabels(OriginLiveContext ctx, RectTransform parent) { this.ctx = ctx; this.parent = parent; }

        public void Begin() { foreach (var b in all) b.Used = 0; }

        public void End()
        {
            foreach (var b in all)
                for (int i = b.Used; i < b.Items.Count; i++) b.Items[i].Alpha(0);
        }

        /// <summary>A Menlo-style label (rotate = SVG degrees, clockwise positive, about the label centre; scale likewise). opacity is the SVG opacity (it gets the film's fade curve); colour may carry its own alpha (use OriginInk.Dim).</summary>
        public OriginText Mono(string text, float x, float y, float size, Color color, float opacity = 1, OriginAnchor anchor = OriginAnchor.Start, float spacing = 1.5f, OriginGlow glow = default, float rotate = 0, float scale = 1)
            => Put(ctx.Mono, "m", text, x, y, size, color, opacity, anchor, spacing, 0, glow, rotate, scale);

        /// <summary>A Songti-style label; weight 400/700/900 (the bundled face is one bold cut, heavier weights thicken it).</summary>
        public OriginText Serif(string text, float x, float y, float size, Color color, float opacity = 1, OriginAnchor anchor = OriginAnchor.Middle, float spacing = 0, int weight = 700, OriginGlow glow = default, float rotate = 0, float scale = 1)
            => Put(ctx.Serif, "s" + weight, text, x, y, size, color, opacity, anchor, spacing, weight, glow, rotate, scale);

        OriginText Put(TMP_FontAsset font, string kind, string text, float x, float y, float size, Color color, float opacity, OriginAnchor anchor, float spacing, int weight, OriginGlow glow, float rotate = 0, float scale = 1)
        {
            var align = anchor == OriginAnchor.Start ? TextAlignmentOptions.Left : anchor == OriginAnchor.End ? TextAlignmentOptions.Right : TextAlignmentOptions.Center;
            string key = kind + "|" + size + "|" + spacing + "|" + (int)align + "|" + (glow.Any ? 1 : 0);
            if (!buckets.TryGetValue(key, out var b)) { b = new Bucket(); buckets[key] = b; all.Add(b); }
            OriginText t;
            if (b.Used < b.Items.Count) t = b.Items[b.Used];
            else
            {
                t = glow.Any
                    ? new OriginText("L" + (count++), parent, font, size, color, spacing, align, new OriginTextLayer { Rgb = glow.Rgb, Alpha = glow.Alpha, Dilate = glow.Dilate, Softness = glow.Soft })
                    : new OriginText("L" + (count++), parent, font, size, color, spacing, align);
                if (weight > 0) t.SetFace(weight >= 900 ? OriginLive.SerifWeight + .1f : weight >= 700 ? OriginLive.SerifWeight : 0, 0);
                b.Items.Add(t);
            }
            b.Used++;
            t.Set(text);
            t.SetColor(color);
            if (glow.Any) t.SetHaloStrength(0, glow.Alpha, glow.Dilate, glow.Soft);
            float pv = anchor == OriginAnchor.Start ? 0 : anchor == OriginAnchor.End ? 1 : .5f;
            float dy = weight > 0 ? OriginLive.SerifDy : OriginLive.MonoDy;
            t.Place(x, y + dy, pv, 1400, size * 1.5f);
            t.Root.localRotation = rotate == 0 ? Quaternion.identity : Quaternion.Euler(0, 0, -rotate);
            t.Root.localScale = new Vector3(scale, scale, 1);
            t.Alpha(OriginInk.Fade(opacity));
            return t;
        }
    }
}
