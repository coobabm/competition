using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// A pool of single-line texts for the film's labelled graphics (SVG text with a position, size, colour and optional blur glow).
    /// Call <see cref="Begin"/>, then <see cref="Text"/> for every label in draw order, then <see cref="End"/>; unused texts are hidden.
    /// Positions are the film's reference space; y is the vertical centre of the line (SVG dominant-baseline: central).
    /// </summary>
    /// <summary>One blurred copy behind a text: SDF softness (0..1), dilation (-1..1) and an alpha factor.</summary>
    public struct OriginGlowSpec
    {
        public float Soft, Dilate, Alpha;
        public OriginGlowSpec(float soft, float dilate, float alpha) { Soft = soft; Dilate = dilate; Alpha = alpha; }
        /// <summary>The film's blur filters g1 (σ 2.5), g2 (σ 7), g3 (σ 16) as halo presets.</summary>
        public static readonly OriginGlowSpec G1 = new OriginGlowSpec(.3f, 0f, .55f), G2 = new OriginGlowSpec(.6f, 0f, .4f), G3 = new OriginGlowSpec(.85f, .05f, .35f);
        public static OriginGlowSpec Preset(int n) { return n == 1 ? G1 : n == 2 ? G2 : G3; }
    }

    public sealed class OriginTextPool
    {
        public enum Anchor { Start, Middle, End }

        static float SerifDy { get { return OriginLive.SerifDy; } }
        static float MonoDy { get { return OriginLive.MonoDy; } }
        static float SerifWeight { get { return OriginLive.SerifWeight; } }
        static float MonoWeight { get { return OriginLive.MonoWeight; } }

        sealed class Item
        {
            public RectTransform Root; public CanvasGroup Group;
            public TextMeshProUGUI Main;
            public readonly List<TextMeshProUGUI> Halos = new List<TextMeshProUGUI>();
            public readonly List<OriginGlowSpec> HaloSpecs = new List<OriginGlowSpec>();
            public string Str; public bool Serif; public float Size, Spacing, Weight = float.NaN, Blur = float.NaN;
            public Anchor Anchor = (Anchor)(-1);
            public bool Active;
        }

        readonly RectTransform parent;
        readonly List<Item> items = new List<Item>();
        readonly List<Item> serifItems = new List<Item>(), monoItems = new List<Item>();
        int usedSerif, usedMono;
        /// <summary>A shake offset in reference units applied to every label drawn while it is set.</summary>
        public float Dx, Dy;
        TextMeshProUGUI meter;
        readonly Dictionary<string, float> widths = new Dictionary<string, float>();

        public OriginTextPool(RectTransform parent) { this.parent = parent; }

        public void Begin() { usedSerif = usedMono = 0; Dx = Dy = 0; }

        /// <summary>The laid-out width of a single line (reference px), for the film's inline layouts.</summary>
        public float Width(string s, float size, bool serif, float spacing)
        {
            string key = (serif ? "S" : "M") + size + "|" + spacing + "|" + s;
            float w;
            if (widths.TryGetValue(key, out w)) return w;
            if (meter == null)
            {
                var go = new GameObject("Meter", typeof(RectTransform)); go.layer = parent.gameObject.layer;
                var rt = (RectTransform)go.transform; rt.SetParent(parent, false); rt.sizeDelta = new Vector2(2000, 100);
                meter = go.AddComponent<TextMeshProUGUI>();
                meter.textWrappingMode = TextWrappingModes.NoWrap; meter.overflowMode = TextOverflowModes.Overflow; meter.raycastTarget = false; meter.color = new Color(0, 0, 0, 0);
            }
            meter.font = serif ? OriginFonts.Serif : OriginFonts.Mono; meter.fontSize = size;
            string tm = spacing != 0 ? "<cspace=" + spacing.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + ">" + s : s;
            w = meter.GetPreferredValues(tm).x;
            widths[key] = w;
            return w;
        }

        public void End()
        {
            Hide(serifItems, usedSerif); Hide(monoItems, usedMono);
        }

        static void Hide(List<Item> list, int used)
        {
            for (int i = used; i < list.Count; i++)
                if (list[i].Active) { list[i].Root.gameObject.SetActive(false); list[i].Active = false; }
        }

        Item Make(bool serif)
        {
            var it = new Item();
            it.Serif = serif;
            var go = new GameObject("T" + items.Count, typeof(RectTransform), typeof(CanvasGroup));
            go.layer = parent.gameObject.layer;
            it.Root = (RectTransform)go.transform; it.Root.SetParent(parent, false);
            it.Root.anchorMin = it.Root.anchorMax = new Vector2(0, 1);
            it.Group = go.GetComponent<CanvasGroup>(); it.Group.blocksRaycasts = false; it.Group.interactable = false;
            it.Main = Layer(it.Root, "Main");
            it.Main.font = serif ? OriginFonts.Serif : OriginFonts.Mono;
            items.Add(it); (serif ? serifItems : monoItems).Add(it);
            return it;
        }

        static TextMeshProUGUI Layer(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform; rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            var t = go.AddComponent<TextMeshProUGUI>();
            t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false; t.richText = true; t.extraPadding = false; t.margin = Vector4.zero;
            t.enableAutoSizing = false; t.enableKerning = true;
            return t;
        }

        /// <summary>
        /// One label. rgb + alpha become the vertex colour through OriginInk.Over. glow 1/2/3 adds a blurred copy behind
        /// (the film's text filters g1/g2/g3); halos overrides it with explicit layers (back to front). blur softens the text itself
        /// (reference px of Gaussian blur, for the title's blur-in).
        /// </summary>
        public void Text(string s, float x, float y, float size, Color rgb, float alpha, Anchor anchor = Anchor.Middle, bool serif = false, float spacing = 0, int glow = 0, float blur = 0, OriginGlowSpec[] halos = null, Color haloRgb = default(Color))
        {
            if (alpha <= .002f || string.IsNullOrEmpty(s)) return;
            var list = serif ? serifItems : monoItems;
            int slot = serif ? usedSerif++ : usedMono++;
            Item it = slot < list.Count ? list[slot] : Make(serif);
            if (!it.Active) { it.Root.gameObject.SetActive(true); it.Active = true; }
            var font = serif ? OriginFonts.Serif : OriginFonts.Mono;
            int nh = halos != null ? halos.Length : glow > 0 ? 1 : 0;
            while (it.Halos.Count < nh)
            {
                var h = Layer(it.Root, "Halo" + it.Halos.Count);
                h.font = font;
                h.transform.SetAsFirstSibling();
                it.Halos.Add(h); it.HaloSpecs.Add(new OriginGlowSpec(-1, -1, 0));
                it.Str = null; it.Size = -1; it.Anchor = (Anchor)(-1);
            }
                        if (it.Str != s || it.Spacing != spacing)
            {
                string tm = spacing != 0 ? "<cspace=" + spacing.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + ">" + s : s;
                it.Main.text = tm; foreach (var h in it.Halos) h.text = tm;
                it.Str = s; it.Spacing = spacing;
            }
            if (it.Size != size) { it.Main.fontSize = size; foreach (var h in it.Halos) h.fontSize = size; it.Size = size; }
            if (it.Anchor != anchor)
            {
                var al = anchor == Anchor.Start ? TextAlignmentOptions.Left : anchor == Anchor.End ? TextAlignmentOptions.Right : TextAlignmentOptions.Center;
                it.Main.alignment = al; foreach (var h in it.Halos) h.alignment = al;
                it.Anchor = anchor;
            }
            float w = Mathf.Max(200, s.Length * (size + Mathf.Abs(spacing)) * 1.1f + 60), hgt = size * 1.6f;
            it.Root.pivot = new Vector2(anchor == Anchor.Start ? 0 : anchor == Anchor.End ? 1 : .5f, .5f);
            it.Root.sizeDelta = new Vector2(w, hgt);
            it.Root.anchoredPosition = new Vector2(x + Dx, -(y + Dy + (serif ? SerifDy : MonoDy)));
            float weight = (serif ? SerifWeight : MonoWeight);
            if (!Mathf.Approximately(it.Weight, weight) || !Mathf.Approximately(it.Blur, blur))
            {
                var m = it.Main.fontMaterial;
                m.SetFloat(ShaderUtilities.ID_FaceDilate, weight);
                m.SetFloat(ShaderUtilities.ID_OutlineSoftness, BlurSoftness(blur, size));
                TMPro_EventManager.ON_MATERIAL_PROPERTY_CHANGED(true, m);
                it.Weight = weight; it.Blur = blur;
            }
            it.Main.color = OriginInk.Over(rgb, alpha);
            for (int i = 0; i < it.Halos.Count; i++)
            {
                var h = it.Halos[i];
                if (i >= nh) { h.gameObject.SetActive(false); continue; }
                var spec = halos != null ? halos[i] : OriginGlowSpec.Preset(glow);
                if (!h.gameObject.activeSelf) h.gameObject.SetActive(true);
                var prev = it.HaloSpecs[i];
                if (!Mathf.Approximately(prev.Soft, spec.Soft) || !Mathf.Approximately(prev.Dilate, spec.Dilate))
                {
                    var m = h.fontMaterial;
                    m.SetFloat(ShaderUtilities.ID_FaceDilate, spec.Dilate); m.SetFloat(ShaderUtilities.ID_OutlineSoftness, spec.Soft);
                    TMPro_EventManager.ON_MATERIAL_PROPERTY_CHANGED(true, m);
                    it.HaloSpecs[i] = spec;
                }
                h.color = OriginInk.Over(haloRgb.a > 0 ? haloRgb : rgb, alpha * spec.Alpha);
            }
        }

        /// <summary>SDF softness (0..1) that reads as a Gaussian blur of the given reference px on a glyph of this size.</summary>
        public static float BlurSoftness(float blurPx, float size)
        {
            if (blurPx <= 0) return 0;
            return Mathf.Clamp01(blurPx / (size * .22f));
        }

        public void Destroy()
        {
            foreach (var it in items) if (it.Root != null) Object.DestroyImmediate(it.Root.gameObject);
            items.Clear(); serifItems.Clear(); monoItems.Clear(); usedSerif = usedMono = 0;
        }
    }
}
