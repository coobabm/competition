using System.Collections.Generic;
using System.IO;
using System.Text;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// Fonts for the film (the Remotion render sets captions in Songti SC Bold and counters in Menlo). Resolution order, each with dynamic SDF
    /// atlases (wide padding so glows have room) and the desktop's own CJK font as the glyph fallback:
    ///   serif: the bundled Source Han Serif SC Bold (Localization/Fonts/OriginSerif*, as the TMP asset Resources/LingGuangV05/Fonts/OriginSerif SDF
    ///          or built on the fly from the .otf in the editor) → macOS Songti SC Bold / Windows SimSun → Desktop CJK;
    ///   mono:  the bundled Roboto Mono (Resources/LingGuangV05/Fonts/OriginMono SDF, or the Feel demo font in the editor) → Menlo / Consolas → Desktop CJK.
    /// The editor menu Tools / LingGuang / Origin live – build font assets writes the two Resources assets.
    /// </summary>
    public static class OriginFonts
    {
        static TMP_FontAsset serif, mono;
        static bool built;
        public static bool SystemSerif { get; private set; }
        public static bool SystemMono { get; private set; }
        /// <summary>Which face each role resolved to: "bundled", "system" or "cjk".</summary>
        public static string SerifSource { get; private set; } = "";
        public static string MonoSource { get; private set; } = "";
        /// <summary>Set false to skip the OS faces (what a build without them gets).</summary>
        public static bool UseSystemFaces = true;
        /// <summary>Set false to skip the bundled faces (to compare against the system Songti).</summary>
        public static bool UseBundledFaces = true;

        public const string SerifResource = "LingGuangV05/Fonts/OriginSerif SDF", MonoResource = "LingGuangV05/Fonts/OriginMono SDF";
        public const string SerifFontAssetPath = "Assets/LingGuangV05/Localization/Fonts/OriginSerif-SourceHanSerifSC-Bold.otf";
        public const string MonoFontAssetPath = "Assets/LingGuangV05/Localization/Fonts/RobotoMono-Medium.ttf";

        public static TMP_FontAsset Serif { get { Build(); return serif; } }
        public static TMP_FontAsset Mono { get { Build(); return mono; } }

        // (file, face index): Songti SC Bold / SimSun, Menlo Regular / Consolas.
        static readonly (string path, int face)[] SerifFaces = { ("/System/Library/Fonts/Supplemental/Songti.ttc", 1), ("C:/Windows/Fonts/simsun.ttc", 0) };
        static readonly (string path, int face)[] MonoFaces = { ("/System/Library/Fonts/Menlo.ttc", 0), ("C:/Windows/Fonts/consola.ttf", 0) };

        static TMP_FontAsset FromSystem(IEnumerable<(string path, int face)> faces)
        {
            if (!UseSystemFaces) return null;
            foreach (var (path, face) in faces)
            {
                if (!File.Exists(path)) continue;
                try
                {
                    var asset = TMP_FontAsset.CreateFontAsset(path, face, 48, 20, GlyphRenderMode.SDFAA, 1024, 1024);
                    if (asset != null) { asset.hideFlags = HideFlags.HideAndDontSave; return asset; }
                }
                catch (System.Exception e) { Debug.LogWarning("OriginFonts: " + path + ": " + e.Message); }
            }
            return null;
        }

        /// <summary>A dynamic SDF asset made from a font file (64 px sampling would only cost atlas space; 48 with 20 padding is what the film was tuned with).</summary>
        public static TMP_FontAsset FromFont(Font font)
        {
            var asset = TMP_FontAsset.CreateFontAsset(font, 48, 20, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            if (asset != null) asset.hideFlags = HideFlags.HideAndDontSave;
            return asset;
        }

        static TMP_FontAsset Bundled(string resource, string editorFontPath)
        {
            if (!UseBundledFaces) return null;
            var asset = Resources.Load<TMP_FontAsset>(resource);
            if (asset != null) return asset;
#if UNITY_EDITOR
            var font = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(editorFontPath);
            if (font != null) return FromFont(font);
#endif
            return null;
        }

        static void Build()
        {
            if (built && serif != null && mono != null) return;
            built = true;
            var cjk = PrologueDesk.CjkFont();
            serif = Bundled(SerifResource, SerifFontAssetPath); SerifSource = serif != null ? "bundled" : "";
            if (serif == null) { serif = FromSystem(SerifFaces); SystemSerif = serif != null; SerifSource = SystemSerif ? "system" : ""; }
            mono = Bundled(MonoResource, MonoFontAssetPath); MonoSource = mono != null ? "bundled" : "";
            if (mono == null) { mono = FromSystem(MonoFaces); SystemMono = mono != null; MonoSource = SystemMono ? "system" : ""; }
            if (serif == null) { serif = cjk; SerifSource = "cjk"; }
            else if (cjk != null && cjk != serif) AddFallback(serif, cjk);
            if (mono == null) { mono = cjk; MonoSource = "cjk"; }
            else if (cjk != null && cjk != mono) AddFallback(mono, cjk);
        }

        static void AddFallback(TMP_FontAsset font, TMP_FontAsset fallback)
        {
            if (font.fallbackFontAssetTable == null) font.fallbackFontAssetTable = new List<TMP_FontAsset>();
            // The bundled fonts are project assets, so in the editor they keep the runtime CJK fallback of an earlier
            // Play session after it is destroyed; TMP then throws on the dead entry. Drop dead entries first.
            font.fallbackFontAssetTable.RemoveAll(f => f == null || f.material == null);
            if (!font.fallbackFontAssetTable.Contains(fallback)) font.fallbackFontAssetTable.Add(fallback);
        }

        /// <summary>Rasterises every character of the text into the film fonts' dynamic atlases now (the serif gets all of it, the mono its ASCII and symbols).</summary>
        public static void Warm(string chars)
        {
            Build();
            try
            {
                if (serif != null && serif.atlasPopulationMode == AtlasPopulationMode.Dynamic) serif.TryAddCharacters(chars, out _);
                if (mono != null && mono.atlasPopulationMode == AtlasPopulationMode.Dynamic)
                {
                    var sb = new StringBuilder();
                    foreach (char c in chars) if (c < 0x2000) sb.Append(c);
                    mono.TryAddCharacters(sb.ToString(), out _);
                }
            }
            catch (System.Exception e) { Debug.LogWarning("OriginFonts.Warm: " + e.Message); }
        }

        /// <summary>Forget the cached assets (after a font change or between test runs).</summary>
        public static void Reset()
        {
            if (serif != null && (SystemSerif || SerifSource == "bundled" && serif.hideFlags != HideFlags.None)) Object.DestroyImmediate(serif);
            if (mono != null && (SystemMono || MonoSource == "bundled" && mono.hideFlags != HideFlags.None)) Object.DestroyImmediate(mono);
            serif = mono = null; built = false; SystemSerif = SystemMono = false; SerifSource = MonoSource = "";
        }
    }

    /// <summary>One extra copy of a text drawn behind the main one: a soft, dilated halo that stands in for CSS text-shadow.</summary>
    public struct OriginTextLayer
    {
        public Color Rgb;
        public float Alpha;
        /// <summary>SDF dilation (-1..1) and softness (0..1): how fat and how blurred the halo is.</summary>
        public float Dilate, Softness;
        /// <summary>Only the [lime] and {red} spans glow; false glows the whole line.</summary>
        public bool HighlightsOnly;
        public float DeltaY;
    }

    /// <summary>
    /// A line of film text: the main TMP text plus optional halo layers, all laid out identically. The markup is the film's own:
    /// [lime] and {red} spans. Layout is in the stage's reference pixels.
    /// </summary>
    public sealed class OriginText
    {
        public readonly RectTransform Root;
        public readonly TextMeshProUGUI Main;
        readonly CanvasGroup group;
        readonly List<Halo> halos = new List<Halo>();
        /// <summary>One halo copy: the layer spec, its TMP text, and which highlight kind it shows (null = the whole line). A highlights-only layer is two copies, lime and red, each in its own colour.</summary>
        sealed class Halo { public TextMeshProUGUI Tmp; public OriginTextLayer Spec; public int SpecIndex; public XgOriginColdOpen.Kind? Only; public Color Rgb; }
        readonly float size, spacing;
        readonly TextAlignmentOptions align;
        readonly TMP_FontAsset font;
        string markup;
        int lastVisible = int.MinValue;

        public OriginText(string name, Transform parent, TMP_FontAsset font, float size, Color color, float letterSpacing, TextAlignmentOptions align, params OriginTextLayer[] layers)
        {
            this.size = size; spacing = letterSpacing; this.align = align; this.font = font;
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
            Root = (RectTransform)go.transform; Root.SetParent(parent, false);
            group = go.GetComponent<CanvasGroup>(); group.blocksRaycasts = false; group.interactable = false;
            // Halo layers go first so they sit behind the main text.
            for (int k = 0; k < layers.Length; k++)
            {
                var spec = layers[k];
                if (spec.HighlightsOnly)
                {
                    halos.Add(new Halo { Tmp = Layer("Halo", spec.Rgb, 1), Spec = spec, SpecIndex = k, Only = XgOriginColdOpen.Kind.Lime, Rgb = spec.Rgb });
                    halos.Add(new Halo { Tmp = Layer("Halo Red", OriginInk.Red, 1), Spec = spec, SpecIndex = k, Only = XgOriginColdOpen.Kind.Red, Rgb = OriginInk.Red });
                }
                else halos.Add(new Halo { Tmp = Layer("Halo", spec.Rgb, 1), Spec = spec, SpecIndex = k, Only = null, Rgb = spec.Rgb });
            }
            Main = Layer("Main", color, 1);
            foreach (var h in halos)
            {
                var mat = h.Tmp.fontMaterial;
                mat.SetFloat(ShaderUtilities.ID_FaceDilate, h.Spec.Dilate);
                mat.SetFloat(ShaderUtilities.ID_OutlineSoftness, h.Spec.Softness);
                TMPro_EventManager.ON_MATERIAL_PROPERTY_CHANGED(true, mat);
                var rt = h.Tmp.rectTransform; rt.anchoredPosition = new Vector2(0, -h.Spec.DeltaY);
            }
        }

        TextMeshProUGUI Layer(string name, Color color, float alpha)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform; rt.SetParent(Root, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            var t = go.AddComponent<TextMeshProUGUI>();
            t.font = font; t.fontSize = size; t.color = color; t.alignment = align;
            t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false; t.richText = true; t.extraPadding = false; t.margin = Vector4.zero;
            t.enableAutoSizing = false; t.enableKerning = true;
            return t;
        }

        static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);

        /// <summary>The film's [lime] / {red} markup as TMP rich text. A halo copy hides everything that is not of the kind it glows for (only = null: nothing hidden, no colours).</summary>
        public static string ToTmp(string text, float spacing, XgOriginColdOpen.Kind? only = null, bool halo = false)
        {
            var sb = new StringBuilder();
            if (spacing != 0) sb.Append("<cspace=").Append(spacing.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)).Append('>');
            foreach (var seg in XgOriginColdOpen.Parse(text))
            {
                if (halo)
                {
                    if (only.HasValue && seg.Kind != only.Value) sb.Append("<alpha=#00>").Append(seg.Text).Append("<alpha=#FF>");
                    else sb.Append(seg.Text);
                    continue;
                }
                if (seg.Kind == XgOriginColdOpen.Kind.Lime) sb.Append("<color=#").Append(Hex(OriginInk.Lime)).Append('>').Append(seg.Text).Append("</color>");
                else if (seg.Kind == XgOriginColdOpen.Kind.Red) sb.Append("<color=#").Append(Hex(OriginInk.Red)).Append('>').Append(seg.Text).Append("</color>");
                else sb.Append(seg.Text);
            }
            return sb.ToString();
        }

        /// <summary>Sets the text and how many plain characters are visible (-1 = all). Highlight halos follow the film's lime/red colour.</summary>
        public void Set(string text, int visible = -1)
        {
            if (markup == text && lastVisible == visible) return;
            markup = text; lastVisible = visible;
            Main.text = ToTmp(text, spacing);
            Main.maxVisibleCharacters = visible < 0 ? 99999 : visible;
            foreach (var h in halos)
            {
                h.Tmp.text = ToTmp(text, spacing, h.Only, true);
                h.Tmp.maxVisibleCharacters = Main.maxVisibleCharacters;
            }
        }

        /// <summary>Width of the whole line in reference pixels (letter-spacing included).</summary>
        public float Width(string text)
        {
            return Main.GetPreferredValues(ToTmp(text, spacing)).x;
        }

        /// <summary>Group opacity; for fades pass the already-curved value (<see cref="OriginInk.Fade"/>).</summary>
        public void Alpha(float a)
        {
            group.alpha = a;
            if (Root.gameObject.activeSelf != a > .001f) Root.gameObject.SetActive(a > .001f);
        }

        public void SetHaloStrength(int index, float alpha, float dilate, float softness)
        {
            foreach (var h in halos)
            {
                if (h.SpecIndex != index) continue;
                h.Tmp.color = OriginInk.Over(h.Rgb, alpha);
                var mat = h.Tmp.fontMaterial;
                mat.SetFloat(ShaderUtilities.ID_FaceDilate, dilate);
                mat.SetFloat(ShaderUtilities.ID_OutlineSoftness, softness);
                TMPro_EventManager.ON_MATERIAL_PROPERTY_CHANGED(true, mat);
            }
        }

        public void SetColor(Color c) { Main.color = c; }

        float faceDilate = float.NaN, faceSoft = float.NaN;
        /// <summary>The main text's own SDF weight (dilate, -1..1) and edge softness (0..1; the film's blur-in).</summary>
        public void SetFace(float dilate, float softness)
        {
            if (Mathf.Approximately(dilate, faceDilate) && Mathf.Approximately(softness, faceSoft)) return;
            faceDilate = dilate; faceSoft = softness;
            var mat = Main.fontMaterial;
            mat.SetFloat(ShaderUtilities.ID_FaceDilate, dilate);
            mat.SetFloat(ShaderUtilities.ID_OutlineSoftness, softness);
            TMPro_EventManager.ON_MATERIAL_PROPERTY_CHANGED(true, mat);
        }

        /// <summary>Position by a point in the parent's top-left, y-down space. pivotX 0 = x is the left edge, .5 = centre, 1 = right edge; y is the vertical centre of the line box.</summary>
        public void Place(float x, float centerY, float pivotX, float width = 1400, float height = 80)
        {
            Root.anchorMin = Root.anchorMax = new Vector2(0, 1);
            Root.pivot = new Vector2(pivotX, .5f);
            Root.sizeDelta = new Vector2(width, height);
            Root.anchoredPosition = new Vector2(x, -centerY);
        }

        /// <summary>Right edge (reference px, from the box's left) of the n-th visible plain character, or 0 for n = 0.</summary>
        public float CaretX(int visible, string text)
        {
            if (visible <= 0) return 0;
            if (!Root.gameObject.activeInHierarchy) Root.gameObject.SetActive(true);
            Main.ForceMeshUpdate(true, true);
            var info = Main.textInfo;
            int plain = XgOriginColdOpen.PlainLength(text);
            if (info.characterCount < Mathf.Min(visible, plain))
            {
                // The mesh is not ready (the object was inactive a moment ago): measure the visible prefix instead.
                return Main.GetPreferredValues(ToTmp(Prefix(text, visible), spacing)).x;
            }
            int idx = Mathf.Min(visible, info.characterCount) - 1;
            var rect = Main.rectTransform;
            return info.characterInfo[idx].xAdvance + rect.rect.width * rect.pivot.x;
        }

        /// <summary>The marked-up text cut after n plain characters.</summary>
        public static string Prefix(string text, int n)
        {
            var sb = new StringBuilder();
            foreach (var seg in XgOriginColdOpen.Parse(text))
            {
                if (n <= 0) break;
                string part = seg.Text.Length <= n ? seg.Text : seg.Text.Substring(0, n);
                n -= part.Length;
                sb.Append(seg.Kind == XgOriginColdOpen.Kind.Lime ? "[" + part + "]" : seg.Kind == XgOriginColdOpen.Kind.Red ? "{" + part + "}" : part);
            }
            return sb.ToString();
        }

        public void Destroy() { if (Root != null) Object.DestroyImmediate(Root.gameObject); }
    }
}
