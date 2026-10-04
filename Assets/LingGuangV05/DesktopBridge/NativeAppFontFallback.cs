using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LingGuangV05.Desktop
{
    /// <summary>Missing-glyph support only; never changes native typography, materials or layout.</summary>
    public sealed class NativeAppFontFallback : IDisposable
    {
        private const string Probe = "灵光寻宝家庭邮件训练网络显卡机箱账单电费收入考试保存姓名";
        private readonly Dictionary<TMP_FontAsset, TMP_FontAsset> _clones = new Dictionary<TMP_FontAsset, TMP_FontAsset>();
        private readonly Dictionary<TMP_Text, Binding> _bindings = new Dictionary<TMP_Text, Binding>();
        private TMP_FontAsset _fallback;
        private bool _disposed;

        public void Apply(TMP_Text label)
        {
            if (_disposed || label == null || label.font == null) return;
            if (_bindings.TryGetValue(label, out Binding prior) && label.font == prior.applied) return;
            TMP_FontAsset original = label.font;
            if (original.HasCharacters(Probe + label.text, out uint[] missing, true, false)) return;
            if (!_clones.TryGetValue(original, out TMP_FontAsset clone))
            {
                TMP_FontAsset fallback = GetFallback();
                if (fallback == null) return;
                clone = UnityEngine.Object.Instantiate(original);
                clone.name = original.name + "_LingGuangNativeAppFallback";
                clone.hideFlags = HideFlags.DontSave;
                // Borrow the native primary atlas/material read-only. New glyphs are owned by fallback.
                clone.atlasPopulationMode = AtlasPopulationMode.Static;
                clone.fallbackFontAssetTable = original.fallbackFontAssetTable == null
                    ? new List<TMP_FontAsset>() : new List<TMP_FontAsset>(original.fallbackFontAssetTable);
                clone.fallbackFontAssetTable.Add(fallback);
                _clones.Add(original, clone);
            }
            Material material = label.fontSharedMaterial;
            _bindings[label] = new Binding { original = original, applied = clone, material = material };
            label.font = clone;
            if (material != null) label.fontSharedMaterial = material;
        }

        private TMP_FontAsset GetFallback()
        {
            if (_fallback != null) return _fallback;
            Font source = Resources.Load<Font>("Fonts/NotoSansCJKsc-Regular");
            if (source == null) return null;
            _fallback = TMP_FontAsset.CreateFontAsset(source, 48, 5,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            _fallback.name = "LingGuangV05_NativeApp_CjkFallback";
            _fallback.hideFlags = HideFlags.DontSave;
            _fallback.isMultiAtlasTexturesEnabled = true;
            _fallback.TryAddCharacters(Probe);
            return _fallback;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (KeyValuePair<TMP_Text, Binding> item in _bindings)
            {
                if (item.Key == null || item.Key.font != item.Value.applied) continue;
                item.Key.font = item.Value.original;
                if (item.Value.material != null) item.Key.fontSharedMaterial = item.Value.material;
            }
            foreach (TMP_FontAsset clone in _clones.Values)
            {
                if (clone == null) continue;
                // TMP destroys its referenced material/textures in OnDestroy even for Static fonts.
                // The clone must relinquish borrowed native resources before its own destruction.
                clone.atlasTextures = Array.Empty<Texture2D>();
                clone.material = null;
                DestroyOwned(clone);
            }
            if (_fallback != null) DestroyOwned(_fallback); // TMP owns and releases its resources once.
            _bindings.Clear(); _clones.Clear(); _fallback = null;
        }

        private static void DestroyOwned(UnityEngine.Object value)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(value, .1f);
            else UnityEngine.Object.DestroyImmediate(value);
        }

        private struct Binding { public TMP_FontAsset original, applied; public Material material; }
    }
}
